using NOMINA_2025;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Nomina_2026_NET8
{
    public partial class FormAcumulados : Form
    {
        // ── Propiedades públicas ───────────────────────────────────────────
        public string RutaOrigen { get; set; }
        public bool ModoNOM { get; set; } = false;

        // ── Estado interno ────────────────────────────────────────────────
        private List<EmpleadoAcumulado> _empleados = new();
        private List<EmpleadoAcumulado> _empleadosFiltrados = new();
        private bool _excluirUltimaDic = false;
        private bool _excluirEspeciales = false;
        private bool _soloActivos = false;

        private string RutaSeleccionada => Properties.Settings.Default.RutaSeleccionada;
        private string BaseRuta => !string.IsNullOrWhiteSpace(RutaOrigen) ? RutaOrigen : RutaSeleccionada;

        // ── Constantes ────────────────────────────────────────────────────
        private const int NOM_RECORD_SIZE = 192;
        private const int CMP_RECORD_SIZE = 106;
        private const string FMT_ACUM = "#,##0.00";

        private static readonly Regex PatronNormal = new(@"^[A-Z]{3}\d{5}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PatronNormalCorto = new(@"^[A-Z]{3}\d{2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Dictionary<string, int> OrdenMes = new()
        {
            {"ENE",1},{"FEB",2},{"MAR",3},{"ABR",4},
            {"MAY",5},{"JUN",6},{"JUL",7},{"AGO",8},
            {"SEP",9},{"OCT",10},{"NOV",11},{"DIC",12}
        };

        private static readonly Dictionary<string, string> NombreAAbrev = new(StringComparer.OrdinalIgnoreCase)
        {
            {"ENERO","ENE"},{"FEBRERO","FEB"},{"MARZO","MAR"},{"ABRIL","ABR"},
            {"MAYO","MAY"},{"JUNIO","JUN"},{"JULIO","JUL"},{"AGOSTO","AGO"},
            {"SEPTIEMBRE","SEP"},{"OCTUBRE","OCT"},{"NOVIEMBRE","NOV"},{"DICIEMBRE","DIC"}
        };

        private static readonly (int MesInicio, int MesFin)[] RangosPredefinidos =
        {
            (1,2),(3,4),(5,6),(7,8),(9,10),(11,12)
        };

        // ── Estructuras binarias VB6 ──────────────────────────────────────
        private struct NomRegistro
        {
            public decimal Dias, HsNorAcum, HsNoAcum, HsDblAcum, HsDbAcum;
            public decimal HsTriAcum, HsTrAcum, Ispt, CrdSal, Imss;
            public decimal Sueldo, HsNor, HsDbl, HsTri, Viaticos;
            public decimal PVac, Otras, Aguin, Ptu, Exentos;
            public decimal Prestamos, Fonacot, Telefono, OtraDed;

            public bool TieneIngresos => Sueldo + HsNor + HsDbl + HsTri + Viaticos + PVac + Otras + Exentos + Aguin + Ptu > 0m;
        }

        private struct CmpRegistro
        {
            public decimal PSubDi, Subdio, SubApl, SubNap, CreTot, CredNe, ImpTot;
        }

        public FormAcumulados()
        {
            InitializeComponent();
        }

        private void FormAcumulados_Load(object sender, EventArgs e)
        {
            try
            {
                InicializarGrid();
                ConfigurarRangos();
                CargarListaEmpleados();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Eventos de grid ───────────────────────────────────────────────
        private void dgvPersonalEncontrado_SelectionChanged(object sender, EventArgs e)
        {
            if (chckboxTotalAcumulado?.Checked == true) return;
            if (dgvPersonalEncontrado.SelectedRows.Count == 0) return;
            if (dgvPersonalEncontrado.SelectedRows[0].DataBoundItem is EmpleadoAcumulado emp)
                CargarAcumuladosEmpleado(emp.Id);
        }

        private void txtBuscar_TextChanged(object sender, EventArgs e) => AplicarFiltro();

        private void txtBuscar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AplicarFiltro();
                e.SuppressKeyPress = true;
            }
        }

        private void btnBuscar_Click(object sender, EventArgs e) => AplicarFiltro();

        // ── Eventos de ordenamiento ───────────────────────────────────────
        private void tsNumAscend_Click(object sender, EventArgs e)
        {
            _empleadosFiltrados = _empleadosFiltrados.OrderBy(x => x.Id).ToList();
            RefrescarDgvPersonal();
        }

        private void tsNumDescend_Click(object sender, EventArgs e)
        {
            _empleadosFiltrados = _empleadosFiltrados.OrderByDescending(x => x.Id).ToList();
            RefrescarDgvPersonal();
        }

        private void tsAlfAscend_Click(object sender, EventArgs e)
        {
            _empleadosFiltrados = _empleadosFiltrados.OrderBy(x => x.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList();
            RefrescarDgvPersonal();
        }

        private void tsAlfDescend_Click(object sender, EventArgs e)
        {
            _empleadosFiltrados = _empleadosFiltrados.OrderByDescending(x => x.Nombre, StringComparer.CurrentCultureIgnoreCase).ToList();
            RefrescarDgvPersonal();
        }

        // ── Eventos de toolbar ────────────────────────────────────────────
        private void tsSinUltimaNomina_Click(object sender, EventArgs e)
        {
            _excluirUltimaDic = true;
            RefrescarAcumuladoActual();
        }

        private void tsConUltimaNomina_Click(object sender, EventArgs e)
        {
            _excluirUltimaDic = false;
            RefrescarAcumuladoActual();
        }

        private void tsSelecTodoListadoDePersonal_Click(object sender, EventArgs e)
        {
            dgvPersonalEncontrado.SelectAll();
            CopiarGridConDialogo(dgvPersonalEncontrado, "lista de personal");
        }

        private void tsSelectTodoNominasEncontradas_Click(object sender, EventArgs e)
        {
            dgvAcumulado.SelectAll();
            CopiarGridConDialogo(dgvAcumulado, "acumulado de nómina");
        }

        private void tsGenerarArchivo_Click(object sender, EventArgs e) => GenerarArchivoInformativo();

        private void tsOtrosIngresos_Click(object sender, EventArgs e) => MostrarOtrosIngresos();

        private void tsCalculoIntegral_Click(object sender, EventArgs e) => AplicarCalculoIntegral();

        private void tsCalculoISR_Click(object sender, EventArgs e) => MostrarCalculoISR();

        private void tsBuscarDuplicados_Click(object sender, EventArgs e) => BuscarDuplicados();

        private void tsImpresion_Click(object sender, EventArgs e) => ImprimirViaNavegador();

        private void chckboxSoloEmpleadosActivos_CheckedChanged(object sender, EventArgs e)
        {
            _soloActivos = chckboxSoloEmpleadosActivos.Checked;
            AplicarFiltro();
        }

        private void chckboxTotalAcumulado_CheckedChanged(object sender, EventArgs e)
        {
            if (chckboxTotalAcumulado.Checked)
            {
                dgvPersonalEncontrado.Enabled = false;
                CargarAcumuladoTotal();
            }
            else
            {
                dgvPersonalEncontrado.Enabled = true;
                dgvAcumulado.Rows.Clear();
                SeleccionarPrimerEmpleado();
            }
        }

        private void chckboxExcluirEspeciales_CheckedChanged(object sender, EventArgs e)
        {
            _excluirEspeciales = chckboxExcluirEspeciales.Checked;
            RefrescarAcumuladoActual();
        }

        // ── Eventos de rangos ─────────────────────────────────────────────
        private void ChkRangoPredefinido_Changed(object sender, EventArgs e)
        {
            var chk = (CheckBox)sender;
            var todos = new[]
            {
                chckboxRango1, chckboxRango2, chckboxRango3,
                chckboxRango4, chckboxRango5, chckboxRango6
            };

            if (!chk.Checked)
            {
                if (!todos.Any(c => c.Checked) && !chckboxRangoPersonalizado.Checked)
                {
                    SetComboBoxPorMes(cbMesInicio, 1);
                    SetComboBoxPorMes(cbMesFinal, 12);
                }
                ActualizarEstadoComboboxes();
                RefrescarAcumuladoActual();
                return;
            }

            // Desmarcar los demás rangos predefinidos
            foreach (var c in todos)
            {
                if (c == chk) continue;
                c.CheckedChanged -= ChkRangoPredefinido_Changed;
                c.Checked = false;
                c.CheckedChanged += ChkRangoPredefinido_Changed;
            }

            chckboxRangoPersonalizado.CheckedChanged -= ChkRangoPersonalizado_Changed;
            chckboxRangoPersonalizado.Checked = false;
            chckboxRangoPersonalizado.CheckedChanged += ChkRangoPersonalizado_Changed;

            int idx = Array.IndexOf(todos, chk);
            var rango = RangosPredefinidos[idx];
            SetComboBoxPorMes(cbMesInicio, rango.MesInicio);
            SetComboBoxPorMes(cbMesFinal, rango.MesFin);

            ActualizarEstadoComboboxes();
            RefrescarAcumuladoActual();
        }

        private void ChkRangoPersonalizado_Changed(object sender, EventArgs e)
        {
            var todos = new[]
            {
                chckboxRango1, chckboxRango2, chckboxRango3,
                chckboxRango4, chckboxRango5, chckboxRango6
            };

            foreach (var c in todos)
                c.Enabled = !chckboxRangoPersonalizado.Checked;

            ActualizarEstadoComboboxes();
            RefrescarAcumuladoActual();
        }

        private void CbRango_Changed(object sender, EventArgs e)
        {
            int idxInicio = cbMesInicio.SelectedIndex;
            int idxFinal = cbMesFinal.SelectedIndex;
            if (idxInicio < 0 || idxFinal < 0) return;

            if (sender == cbMesInicio && idxInicio > idxFinal)
            {
                cbMesFinal.SelectedIndexChanged -= CbRango_Changed;
                cbMesFinal.SelectedIndex = idxInicio;
                cbMesFinal.SelectedIndexChanged += CbRango_Changed;
            }
            else if (sender == cbMesFinal && idxFinal < idxInicio)
            {
                cbMesInicio.SelectedIndexChanged -= CbRango_Changed;
                cbMesInicio.SelectedIndex = idxFinal;
                cbMesInicio.SelectedIndexChanged += CbRango_Changed;
            }

            RefrescarAcumuladoActual();
        }

        // ── Inicialización ────────────────────────────────────────────────
        private void InicializarGrid()
        {
            dgvPersonalEncontrado.AutoGenerateColumns = false;
            dgvAcumulado.AutoGenerateColumns = false;

            SetDataPropertyName(dgvPersonalEncontrado, "Id", nameof(EmpleadoAcumulado.Id));
            SetDataPropertyName(dgvPersonalEncontrado, "NombreCompleto", nameof(EmpleadoAcumulado.Nombre));
            SetDataPropertyName(dgvPersonalEncontrado, "RFCEmpleadoE", nameof(EmpleadoAcumulado.RFCE));
            SetDataPropertyName(dgvPersonalEncontrado, "IMSSE", nameof(EmpleadoAcumulado.IMSSE));
            SetDataPropertyName(dgvPersonalEncontrado, "CURPE", nameof(EmpleadoAcumulado.CURPE));
            SetDataPropertyName(dgvPersonalEncontrado, "FechaAlta", nameof(EmpleadoAcumulado.FechaAltaE));
            SetDataPropertyName(dgvPersonalEncontrado, "FechaBaja", nameof(EmpleadoAcumulado.FechaBajaE));
            SetDataPropertyName(dgvPersonalEncontrado, "SalarioDiarioE", nameof(EmpleadoAcumulado.SalarioDiarioE));
            SetDataPropertyName(dgvPersonalEncontrado, "ViaticosE", nameof(EmpleadoAcumulado.ViaticosE));
            SetDataPropertyName(dgvPersonalEncontrado, "OtrasE", nameof(EmpleadoAcumulado.OtrasE));
            SetDataPropertyName(dgvPersonalEncontrado, "IntegradoE", nameof(EmpleadoAcumulado.IntegradoE));
            SetDataPropertyName(dgvPersonalEncontrado, "BanamexE", nameof(EmpleadoAcumulado.BanamexE));

            dgvPersonalEncontrado.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPersonalEncontrado.MultiSelect = false;
            dgvPersonalEncontrado.ReadOnly = true;
            dgvAcumulado.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAcumulado.MultiSelect = false;
            dgvAcumulado.ReadOnly = true;

            dgvPersonalEncontrado.DoubleBuffered(true);
            dgvAcumulado.DoubleBuffered(true);

            // Formato numérico en columnas de acumulado
            foreach (DataGridViewColumn col in dgvAcumulado.Columns)
            {
                if (col.Name == "Archivo") continue;
                col.DefaultCellStyle.Format = "N2";
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            // Formato numérico en columnas de personal
            foreach (var colName in new[]
                { "SalarioDiarioE","ViaticosE","OtrasE","IntegradoE" })
            {
                if (!dgvPersonalEncontrado.Columns.Contains(colName)) continue;
                dgvPersonalEncontrado.Columns[colName].DefaultCellStyle.Format = "N2";
                dgvPersonalEncontrado.Columns[colName].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }

        private void ConfigurarRangos()
        {
            cbMesInicio.SelectedIndex = 0;
            cbMesFinal.SelectedIndex = cbMesFinal.Items.Count - 1;

            var chkRangos = new[]
            {
                chckboxRango1, chckboxRango2, chckboxRango3,
                chckboxRango4, chckboxRango5, chckboxRango6
            };

            foreach (var chk in chkRangos)
                chk.CheckedChanged += ChkRangoPredefinido_Changed;

            chckboxRangoPersonalizado.CheckedChanged += ChkRangoPersonalizado_Changed;
            chckboxExcluirEspeciales.CheckedChanged += chckboxExcluirEspeciales_CheckedChanged;

            ActualizarEstadoComboboxes();
            cbMesInicio.SelectedIndexChanged += CbRango_Changed;
            cbMesFinal.SelectedIndexChanged += CbRango_Changed;
        }

        // ── Carga de empleados ────────────────────────────────────────────

        private void CargarListaEmpleados()
        {
            _empleados = LeerEmpleados(Path.Combine(BaseRuta, "personal.dno"),
                Path.Combine(BaseRuta, "Bnxcla.dno"));
            _empleadosFiltrados = new List<EmpleadoAcumulado>(_empleados);
            RefrescarDgvPersonal();
        }

        // Privado — solo lo usa CargarListaEmpleados
        private Dictionary<int, string> LeerCurp(string rutaPerOtre)
        {
            var dict = new Dictionary<int, string>();
            if (!File.Exists(rutaPerOtre)) return dict;

            const int recordSize = 120;
            using var fs = new FileStream(rutaPerOtre, FileMode.Open, FileAccess.Read);
            using var br = new System.IO.BinaryReader(fs, Encoding.Default);

            for (int index = 1; fs.Position + recordSize <= fs.Length; index++)
            {
                byte[] raw = br.ReadBytes(recordSize);
                if (raw.All(b => b == 0)) continue;
                string curp = Encoding.Default.GetString(raw, 0, 30).Trim();
                if (!string.IsNullOrWhiteSpace(curp))
                    dict[index] = curp;
            }

            return dict;
        }

        // Privado — solo lo usa CargarListaEmpleados
        private List<EmpleadoAcumulado> LeerEmpleados(
            string rutaPersonal, string rutaBnxcla)
        {
            var lista = new List<EmpleadoAcumulado>();

            if (!File.Exists(rutaPersonal) || !File.Exists(rutaBnxcla))
            {
                MessageBox.Show("No se encontraron personal.dno o Bnxcla.dno.", "Archivos no encontrados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return lista;
            }

            string rutaCurp = Path.Combine(Path.GetDirectoryName(rutaPersonal), "PerOtre.dno");
            var dictCurp = LeerCurp(rutaCurp);

            const int lenPer = 152;
            const int lenBnx = 16;

            using var fsPer = new FileStream(rutaPersonal, FileMode.Open, FileAccess.Read);
            using var rdrPer = new System.IO.BinaryReader(fsPer, Encoding.Default);
            using var fsBnx = new FileStream(rutaBnxcla, FileMode.Open, FileAccess.Read);
            using var rdrBnx = new System.IO.BinaryReader(fsBnx, Encoding.Default);

            long total = fsPer.Length / lenPer;

            for (int i = 0; i < total; i++)
            {
                byte[] bufPer = rdrPer.ReadBytes(lenPer);
                byte[] bufBnx = fsBnx.Position + lenBnx <= fsBnx.Length ? rdrBnx.ReadBytes(lenBnx) : new byte[lenBnx];

                if (bufPer.All(b => b == 0)) continue;

                string n = Encoding.Default.GetString(bufPer, 0, 20).Trim();
                string a1 = Encoding.Default.GetString(bufPer, 20, 20).Trim();
                string a2 = Encoding.Default.GetString(bufPer, 40, 20).Trim();
                string nombre = $"{a1} {a2} {n}".Trim();

                if (string.IsNullOrWhiteSpace(nombre)) continue;

                dictCurp.TryGetValue(i + 1, out string curp);

                lista.Add(new EmpleadoAcumulado
                {
                    Id = i + 1,
                    Nombre = nombre,
                    RFCE = Encoding.Default.GetString(bufPer, 60, 18).Trim(),
                    IMSSE = Encoding.Default.GetString(bufPer, 78, 18).Trim(),
                    CURPE = curp ?? "",
                    FechaAltaE = Encoding.Default.GetString(bufPer, 96, 12).Trim(),
                    FechaBajaE = Encoding.Default.GetString(bufPer, 108, 12).Trim(),
                    SalarioDiarioE = BitConverter.ToInt64(bufPer, 120) / 10000m,
                    ViaticosE = BitConverter.ToInt64(bufPer, 128) / 10000m,
                    OtrasE = BitConverter.ToInt64(bufPer, 136) / 10000m,
                    IntegradoE = BitConverter.ToInt64(bufPer, 144) / 10000m,
                    BanamexE = Encoding.Default.GetString(bufBnx).Trim()
                });
            }

            return lista;
        }

        // ── Acumulados ────────────────────────────────────────────────────
        private void CargarAcumuladosEmpleado(int idEmpleado)
        {
            dgvAcumulado.Rows.Clear();
            dgvAcumulado.SuspendLayout();
            var archivos = ObtenerArchivosValidos();

            if (ModoNOM) CargarFilasNOM(idEmpleado, archivos);
            else CargarFilasJSON(idEmpleado, archivos);

            AgregarFilaAcumulado();
            dgvAcumulado.ResumeLayout();
        }

        private void CargarAcumuladoTotal()
        {
            dgvAcumulado.Rows.Clear();
            dgvAcumulado.SuspendLayout();
            var archivos = ObtenerArchivosValidos();

            if (ModoNOM) CargarTotalesNOM(archivos);
            else CargarTotalesJSON(archivos);

            AgregarFilaAcumulado();
            dgvAcumulado.ResumeLayout();
        }

        // ── Lectura NOM ───────────────────────────────────────────────────
        private void CargarFilasNOM(int idEmpleado, List<string> archivos)
        {
            foreach (string rutaNom in archivos)
            {
                var regN = LeerRegistroNOM(rutaNom, idEmpleado);
                if (regN == null || !regN.Value.TieneIngresos) continue;

                var nom = regN.Value;
                var regC = LeerRegistroCMP(rutaNom, idEmpleado);

                // Cálculo de totales extraído a método compartido
                var t = CalcularTotalesNom(nom, regC);
                AgregarFilaDatos(Path.GetFileName(rutaNom), nom.Dias, nom.Sueldo, t.PremPunt, nom.Viaticos, nom.PVac, nom.Otras, nom.Aguin, 
                    nom.Ptu, nom.Exentos, t.TotalIngr, t.Istp, t.SubPEmpl, t.CrApl, nom.Ispt, t.CrPag, t.SubNoApl, nom.Imss, nom.Prestamos, 
                    nom.Telefono, nom.Fonacot, nom.OtraDed);
            }
        }

        private void CargarTotalesNOM(List<string> archivos)
        {
            foreach (string rutaNom in archivos)
            {
                if (!File.Exists(rutaNom)) continue;

                byte[] rawNom = File.ReadAllBytes(rutaNom);
                int totalReg = rawNom.Length / NOM_RECORD_SIZE;
                string rutaCmp = Path.ChangeExtension(rutaNom, ".cmp");
                byte[] rawCmp = File.Exists(rutaCmp) ? File.ReadAllBytes(rutaCmp) : null;

                // Acumuladores del archivo completo
                decimal dias = 0, sueldo = 0, premPunt = 0, of = 0, pvac = 0, otras = 0, aguin = 0, ptu = 0, exenta = 0, istp = 0, subPEmpl = 0,
                    crApl = 0, imptoRet = 0, crPag = 0, subNoApl = 0, imss = 0, prest = 0, pension = 0, fonacot = 0, infonavit = 0;

                for (int id = 1; id <= totalReg; id++)
                {
                    int baseOff = (id - 1) * NOM_RECORD_SIZE;
                    if (baseOff + NOM_RECORD_SIZE > rawNom.Length) break;
                    if (rawNom.Skip(baseOff).Take(NOM_RECORD_SIZE).All(b => b == 0))
                        continue;

                    decimal N(int idx) => BitConverter.ToInt64(rawNom, baseOff + idx * 8) / 10_000m;

                    decimal s = N(10), hn = N(11), hd = N(12), ht = N(13);
                    decimal v = N(14), p = N(15), o = N(16), a = N(17), pt = N(18), ex = N(19);
                    decimal ingr = s + hn + hd + ht + v + p + o + a + pt + ex;
                    if (ingr == 0) continue;

                    dias += N(0); sueldo += s; premPunt += hn + hd + ht;
                    of += v; pvac += p; otras += o; aguin += a; ptu += pt;
                    exenta += ex; imptoRet += N(7); imss += N(9);
                    prest += N(20); fonacot += N(21); pension += N(22);
                    infonavit += N(23);

                    bool cmpLeido = false;
                    if (rawCmp != null)
                    {
                        int cmpOff = (id - 1) * CMP_RECORD_SIZE;
                        if (cmpOff + CMP_RECORD_SIZE <= rawCmp.Length && !rawCmp.Skip(cmpOff + 50).Take(CMP_RECORD_SIZE - 50).All(b => b == 0))
                        {
                            decimal C(int fi) => BitConverter.ToInt64(rawCmp, cmpOff + 50 + fi * 8) / 10_000m;
                            istp += C(6); subPEmpl += C(2);
                            crApl += C(4); crPag += C(5); subNoApl += C(3);
                            cmpLeido = true;
                        }
                    }
                    if (!cmpLeido) { istp += N(7); subPEmpl += N(8); }
                }

                decimal totalIngr = sueldo + premPunt + of + pvac + otras + aguin + ptu + exenta;
                if (totalIngr == 0) continue;

                AgregarFilaDatos(Path.GetFileName(rutaNom), dias, sueldo, premPunt, of, pvac, otras, aguin, ptu, exenta, totalIngr, istp, 
                    subPEmpl, crApl, imptoRet, crPag, subNoApl, imss, prest, pension, fonacot, infonavit);
            }
        }

        // ── Lectura JSON ──────────────────────────────────────────────────

        private void CargarFilasJSON(int idEmpleado, List<string> archivos)
        {
            foreach (string rutaJson in archivos)
            {
                try
                {
                    // Usa NominaJsonService en lugar del método estático del Form
                    var lista = NominaJsonService.Cargar(rutaJson);
                    var reg = lista.FirstOrDefault(x => x.idNomina == idEmpleado.ToString());
                    if (reg == null) continue;

                    decimal premPunt = reg.hsNorm + reg.hsDobles + reg.hsTriples;
                    AgregarFilaDatos(Path.GetFileName(rutaJson), reg.DiasT, reg.Sueldo, premPunt, reg.OF, reg.Pvacacional, reg.Otras, 0m, 0m, 
                        reg.PercExenta, reg.TotalIngr, reg.ISTP, reg.SubPEmpl, 0m, reg.ISTP, 0m, 0m, reg.IMSS, reg.Prestamos, reg.PensionAlimenticia,
                        reg.Fonacot, reg.Infonavit);
                }
                catch { }
            }
        }

        private void CargarTotalesJSON(List<string> archivos)
        {
            foreach (string rutaJson in archivos)
            {
                try
                {
                    var lista = NominaJsonService.Cargar(rutaJson);
                    if (lista?.Count == 0) continue;

                    decimal dias = 0, sueldo = 0, premPunt = 0, of = 0, pvac = 0, otras = 0, exenta = 0, istp = 0, subPEmpl = 0, imss = 0, 
                        prest = 0, pension = 0, fonacot = 0, infonavit = 0;

                    foreach (var reg in lista)
                    {
                        dias += reg.DiasT;
                        sueldo += reg.Sueldo;
                        premPunt += reg.hsNorm + reg.hsDobles + reg.hsTriples;
                        of += reg.OF;
                        pvac += reg.Pvacacional;
                        otras += reg.Otras;
                        exenta += reg.PercExenta;
                        istp += reg.ISTP;
                        subPEmpl += reg.SubPEmpl;
                        imss += reg.IMSS;
                        prest += reg.Prestamos;
                        pension += reg.PensionAlimenticia;
                        fonacot += reg.Fonacot;
                        infonavit += reg.Infonavit;
                    }

                    decimal totalIngr = sueldo + premPunt + of + pvac + otras + exenta;
                    if (totalIngr == 0) continue;

                    AgregarFilaDatos(Path.GetFileName(rutaJson), dias, sueldo, premPunt, of, pvac, otras, 0m, 0m, exenta, totalIngr, istp, 
                        subPEmpl, 0m, istp, 0m, 0m, imss, prest, pension, fonacot, infonavit);
                }
                catch { }
            }
        }

        // ── Helpers de lectura binaria ────────────────────────────────────
        private NomRegistro? LeerRegistroNOM(string rutaNom, int idEmpleado)
        {
            if (!File.Exists(rutaNom)) return null;
            long offset = (long)(idEmpleado - 1) * NOM_RECORD_SIZE;

            using var fs = new FileStream(rutaNom, FileMode.Open, FileAccess.Read);
            if (offset + NOM_RECORD_SIZE > fs.Length) return null;
            fs.Seek(offset, SeekOrigin.Begin);

            byte[] buf = new byte[NOM_RECORD_SIZE];
            fs.Read(buf, 0, NOM_RECORD_SIZE);
            if (buf.All(b => b == 0)) return null;

            decimal C(int idx) => BitConverter.ToInt64(buf, idx * 8) / 10000m;

            return new NomRegistro
            {
                Dias = C(0),
                HsNorAcum = C(1),
                HsNoAcum = C(2),
                HsDblAcum = C(3),
                HsDbAcum = C(4),
                HsTriAcum = C(5),
                HsTrAcum = C(6),
                Ispt = C(7),
                CrdSal = C(8),
                Imss = C(9),
                Sueldo = C(10),
                HsNor = C(11),
                HsDbl = C(12),
                HsTri = C(13),
                Viaticos = C(14),
                PVac = C(15),
                Otras = C(16),
                Aguin = C(17),
                Ptu = C(18),
                Exentos = C(19),
                Prestamos = C(20),
                Fonacot = C(21),
                Telefono = C(22),
                OtraDed = C(23)
            };
        }

        private CmpRegistro? LeerRegistroCMP(string rutaNom, int idEmpleado)
        {
            string rutaCmp = Path.ChangeExtension(rutaNom, ".cmp");
            if (!File.Exists(rutaCmp)) return null;
            long offset = (long)(idEmpleado - 1) * CMP_RECORD_SIZE;

            using var fs = new FileStream(rutaCmp, FileMode.Open, FileAccess.Read);
            if (offset + CMP_RECORD_SIZE > fs.Length) return null;
            fs.Seek(offset, SeekOrigin.Begin);

            byte[] buf = new byte[CMP_RECORD_SIZE];
            fs.Read(buf, 0, CMP_RECORD_SIZE);
            if (buf.All(b => b == 0)) return null;

            decimal C(int fi) => BitConverter.ToInt64(buf, 50 + fi * 8) / 10000m;

            return new CmpRegistro
            {
                PSubDi = C(0),
                Subdio = C(1),
                SubApl = C(2),
                SubNap = C(3),
                CreTot = C(4),
                CredNe = C(5),
                ImpTot = C(6)
            };
        }

        // Extraído para eliminar duplicación entre CargarFilasNOM y CargarTotalesNOM.        
        private static (decimal PremPunt, decimal TotalIngr, decimal Istp, decimal SubPEmpl, decimal CrApl, decimal CrPag, decimal SubNoApl)
            CalcularTotalesNom(NomRegistro nom, CmpRegistro? regC)
        {
            decimal premPunt = nom.HsNor + nom.HsDbl + nom.HsTri;
            decimal totalIngr = nom.Sueldo + premPunt + nom.Viaticos + nom.PVac + nom.Otras + nom.Aguin + nom.Ptu + nom.Exentos;
            decimal istp = regC.HasValue ? regC.Value.ImpTot : nom.Ispt;
            decimal subPEmpl = regC.HasValue ? regC.Value.SubApl : nom.CrdSal;
            decimal crApl = regC.HasValue ? regC.Value.CreTot : 0m;
            decimal crPag = regC.HasValue ? regC.Value.CredNe : 0m;
            decimal subNoApl = regC.HasValue ? regC.Value.SubNap : 0m;

            return (premPunt, totalIngr, istp, subPEmpl, crApl, crPag, subNoApl);
        }

        // ── Grid de acumulados ────────────────────────────────────────────
        private void AgregarFilaDatos(string archivo, decimal dias, decimal sueldo, decimal premPunt, decimal of, decimal pvac, decimal otras, 
            decimal aguin, decimal ptu, decimal exenta, decimal totalIngr, decimal istp, decimal subPEmpl, decimal crApl, decimal imptoRet, 
            decimal crPag, decimal subNoApl, decimal imss, decimal prest, decimal pension, decimal fonacot, decimal infonavit)
        {
            int r = dgvAcumulado.Rows.Add();
            var row = dgvAcumulado.Rows[r];

            row.Cells["Archivo"].Value = archivo;
            row.Cells["DiasT"].Value = dias;
            row.Cells["Sueldo"].Value = sueldo;
            row.Cells["PremPunt"].Value = premPunt;
            row.Cells["OF"].Value = of;
            row.Cells["Pvacacional"].Value = pvac;
            row.Cells["Otras"].Value = otras;
            row.Cells["Aguinaldo"].Value = aguin;
            row.Cells["PTU"].Value = ptu;
            row.Cells["PercExenta"].Value = exenta;
            row.Cells["TotalIngr"].Value = totalIngr;
            row.Cells["ISTP"].Value = istp;
            row.Cells["SubPEmpl"].Value = subPEmpl;
            row.Cells["CrApl"].Value = crApl;
            row.Cells["ImptoRet"].Value = imptoRet;
            row.Cells["CrPag"].Value = crPag;
            row.Cells["SubsidioNoApl"].Value = subNoApl;
            row.Cells["IMSS"].Value = imss;
            row.Cells["Prestamos"].Value = prest;
            row.Cells["PensionAlimenticia"].Value = pension;
            row.Cells["Fonacot"].Value = fonacot;
            row.Cells["Infonavit"].Value = infonavit;
        }

        private void AgregarFilaAcumulado()
        {
            if (dgvAcumulado.Rows.Count == 0) return;

            int rowIdx = dgvAcumulado.Rows.Add();
            var filaAcum = dgvAcumulado.Rows[rowIdx];
            filaAcum.Cells["Archivo"].Value = "ACUMULADO";

            for (int ci = 1; ci < dgvAcumulado.Columns.Count; ci++)
            {
                decimal suma = 0m;
                for (int ri = 0; ri < rowIdx; ri++)
                {
                    var cell = dgvAcumulado.Rows[ri].Cells[ci];
                    if (cell.Value != null && decimal.TryParse(cell.Value.ToString(), out decimal v)) suma += v;
                }
                filaAcum.Cells[ci].Value = suma.ToString(FMT_ACUM);
            }

            filaAcum.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 180);
            filaAcum.DefaultCellStyle.Font = new Font(dgvAcumulado.Font, FontStyle.Bold);
            filaAcum.DefaultCellStyle.ForeColor = Color.Black;

            dgvAcumulado.FirstDisplayedScrollingRowIndex = Math.Max(0, dgvAcumulado.Rows.Count - 1);

            for (int ci = 1; ci < dgvAcumulado.Columns.Count; ci++)
                filaAcum.Cells[ci].Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        // ── Acciones de toolbar ───────────────────────────────────────────
        private void GenerarArchivoInformativo()
        {
            if (_empleadosFiltrados.Count == 0)
            {
                MessageBox.Show("No hay empleados para generar el archivo.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Title = "Guardar archivo informativo",
                Filter = "Archivo de texto (*.txt)|*.txt",
                FileName = $"Inf{DateTime.Now.Year}.txt"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            try
            {
                var archivos = ObtenerArchivosValidos();
                using var sw = new StreamWriter(sfd.FileName, false, Encoding.Default);

                foreach (var emp in _empleadosFiltrados)
                {
                    decimal dias = 0, sueldo = 0, premPunt = 0, of = 0, pvac = 0, otras = 0, aguin = 0, ptu = 0, exenta = 0, istp = 0, 
                        subEmp = 0, crApl = 0, imptoRet = 0, crPag = 0, subNoApl = 0, imss = 0, prest = 0, pension = 0, fonacot = 0, 
                        infonavit = 0;

                    foreach (string rutaNom in archivos)
                    {
                        var regN = LeerRegistroNOM(rutaNom, emp.Id);
                        if (regN == null || !regN.Value.TieneIngresos) continue;

                        var nom = regN.Value;
                        var regC = LeerRegistroCMP(rutaNom, emp.Id);
                        var t = CalcularTotalesNom(nom, regC);

                        dias += nom.Dias; sueldo += nom.Sueldo;
                        premPunt += t.PremPunt; of += nom.Viaticos;
                        pvac += nom.PVac; otras += nom.Otras;
                        aguin += nom.Aguin; ptu += nom.Ptu;
                        exenta += nom.Exentos; imptoRet += nom.Ispt;
                        imss += nom.Imss; prest += nom.Prestamos;
                        pension += nom.Telefono; fonacot += nom.Fonacot;
                        infonavit += nom.OtraDed;
                        subEmp += t.SubPEmpl; subNoApl += t.SubNoApl;
                        crApl += t.CrApl; crPag += t.CrPag;
                        istp += t.Istp;
                    }

                    decimal totalIngr = sueldo + premPunt + of + pvac + otras + aguin + ptu + exenta;
                    if (totalIngr == 0) continue;

                    sw.WriteLine($"{emp.RFCE}|{emp.CURPE}|{emp.IMSSE}|{emp.Nombre}|" + $"{dias:0}|{sueldo:0}|{premPunt:0}|{of:0}|{pvac:0}|" +
                        $"{otras:0}|{aguin:0}|{ptu:0}|{exenta:0}|{totalIngr:0}|" + $"{istp:0}|{subEmp:0}|{crApl:0}|{imptoRet:0}|" +
                        $"{crPag:0}|{subNoApl:0}|" + $"{imss:0}|{prest:0}|{pension:0}|{fonacot:0}|{infonavit:0}");
                }

                MessageBox.Show($"Archivo generado:\n{sfd.FileName}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MostrarOtrosIngresos()
        {
            if (dgvPersonalEncontrado.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un empleado primero.", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(dgvPersonalEncontrado.SelectedRows[0].DataBoundItem
                is EmpleadoAcumulado emp)) return;

            decimal aguin = 0, ptu = 0;
            foreach (string rutaNom in ObtenerArchivosValidos())
            {
                var regN = LeerRegistroNOM(rutaNom, emp.Id);
                if (regN == null) continue;
                aguin += regN.Value.Aguin;
                ptu += regN.Value.Ptu;
            }

            MessageBox.Show($"Empleado : {emp.Nombre}\n\n" + $"Aguinaldo acumulado : {aguin.ToString(FMT_ACUM)}\n" + 
                $"PTU acumulado       : {ptu.ToString(FMT_ACUM)}", "Otros Ingresos", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AplicarCalculoIntegral()
        {
            if (_empleadosFiltrados.Count == 0)
            {
                MessageBox.Show("No hay empleados cargados.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idSel = -1;
            if (dgvPersonalEncontrado.SelectedRows.Count > 0 && dgvPersonalEncontrado.SelectedRows[0].DataBoundItem is EmpleadoAcumulado empSel)
                idSel = empSel.Id;

            foreach (var emp in _empleadosFiltrados)
                emp.IntegradoE = emp.SalarioDiarioE + emp.ViaticosE + emp.OtrasE;

            RefrescarDgvPersonal();

            if (idSel >= 0)
            {
                foreach (DataGridViewRow row in dgvPersonalEncontrado.Rows)
                {
                    if (row.DataBoundItem is EmpleadoAcumulado e2 && e2.Id == idSel)
                    {
                        row.Selected = true;
                        dgvPersonalEncontrado.CurrentCell = row.Cells[0];
                        break;
                    }
                }
            }

            MessageBox.Show("Cálculo integrado completado.\n\n" + "Fórmula: Salario Diario + Viáticos Diarios + Otras Diarias", 
                "Cálculo Integral", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MostrarCalculoISR()
        {
            try
            {
                var tablas = TarifasRepository.Obtener();

                // Usa CalculadoraISR en lugar del método local duplicado
                var calcISR = new CalculadoraISR(tablas);
                var tablaISR = tablas?.tablas?.ISR_MENSUAL;

                if (tablaISR == null || !tablaISR.Any())
                {
                    MessageBox.Show("No se encontraron tablas ISR_MENSUAL.\n\nImporta las tablas fiscales.", "Tablas no disponibles",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"{"Empleado",-40} {"Ing.Mensual",13} {"ISR Mensual",13}");
                sb.AppendLine(new string('─', 68));

                foreach (var emp in _empleadosFiltrados)
                {
                    decimal ingresoMensual = (emp.SalarioDiarioE + emp.ViaticosE + emp.OtrasE) * 30m;

                    // CalcularISRBruto reemplaza al método local duplicado
                    decimal impuesto = calcISR.CalcularISRBruto(ingresoMensual);

                    sb.AppendLine($"{emp.Nombre,-40} {ingresoMensual,13:N2} {impuesto,13:N2}");
                }

                MostrarResultadosEnVentana("Cálculo ISR Mensual", sb.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en cálculo ISR:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuscarDuplicados()
        {
            foreach (DataGridViewRow row in dgvPersonalEncontrado.Rows)
                row.DefaultCellStyle.BackColor = dgvPersonalEncontrado.DefaultCellStyle.BackColor;

            var ids = _empleadosFiltrados.GroupBy(emp => emp.Nombre.ToUpper().Trim()).Where(g => g.Count() > 1).SelectMany(g => g)
                .Select(emp => emp.Id).ToHashSet();

            if (ids.Count == 0)
            {
                MessageBox.Show("No se encontraron registros con nombre duplicado.", "Buscar Duplicados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (DataGridViewRow row in dgvPersonalEncontrado.Rows)
                if (row.DataBoundItem is EmpleadoAcumulado emp && ids.Contains(emp.Id))
                    row.DefaultCellStyle.BackColor = Color.LightSalmon;

            MessageBox.Show($"Se encontraron {ids.Count} registros con nombre duplicado.", "Buscar Duplicados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ImprimirViaNavegador()
        {
            if (dgvAcumulado.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para imprimir.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string nombreEmp = "(sin selección)";
            if (dgvPersonalEncontrado.SelectedRows.Count > 0 && dgvPersonalEncontrado.SelectedRows[0].DataBoundItem is EmpleadoAcumulado empImp)
                nombreEmp = empImp.Nombre;

            string empresa = FormPrincipal.EmpresaNombre ?? "Nómina Empresarial";
            string html = GenerarHTML(empresa, nombreEmp);
            string tmp = Path.Combine(Path.GetTempPath(), "acumulado_nomina.html");

            File.WriteAllText(tmp, html, Encoding.UTF8);

            // Respaldo automático
            string fecha = DateTime.Now.ToString("yyyyMMdd_HHmm");
            string nomLimpio = Regex.Replace(nombreEmp, @"[^\w\s]", "")
                                   .Replace(" ", "_");
            BackupService.GuardarAcumuladoHTML(html, $"Acumulado_{nomLimpio}_{fecha}.html");

            // Segunda llamada a Process.Start eliminada — era duplicado exacto
            Process.Start(new ProcessStartInfo(tmp) { UseShellExecute = true });
        }

        // ── Generación HTML ───────────────────────────────────────────────

        private string GenerarHTML(string empresa, string empleado)
        {
            string fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            var colsVisibles = dgvAcumulado.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
            var sb = new StringBuilder();
            
            sb.Append(@"<!DOCTYPE html>
                <html lang='es'>
                <head>
                <meta charset='UTF-8'>
                <title>Acumulado de Nómina</title>
                <style>
                  @page { size: landscape; margin: 1cm; }
                  body  { font-family: Arial, sans-serif; font-size: 9px; color: #111; }
                  h2    { font-size: 13px; margin: 0 0 2px 0; }
                  h4    { font-size: 10px; margin: 0 0 8px 0; font-weight: normal; color: #555; }
                  table { width: 100%; border-collapse: collapse; margin-top: 4px; }
                  th    { background: #2c4870; color: #fff; padding: 3px 5px;
                          border: 1px solid #1a2e4a; font-size: 8px; text-align: center; }
                  td    { padding: 2px 5px; border: 1px solid #d0d0d0;
                          text-align: right; white-space: nowrap; }
                  td:first-child { text-align: left; }
                  tr:nth-child(even) td { background: #f4f7fb; }
                  .acum td { background: #ffffc0 !important; font-weight: bold; }
                  .pie  { font-size: 8px; color: #888; margin-top: 8px; text-align: right; }
                </style>
                </head><body>");

            sb.Append($"<h2>{EscHtml(empresa)}</h2>");
            sb.Append($"<h4>Acumulado de Sueldos — {EscHtml(empleado)}</h4>");
            sb.Append("<table><thead><tr>");

            foreach (var col in colsVisibles)
                sb.Append($"<th>{EscHtml(col.HeaderText)}</th>");

            sb.Append("</tr></thead><tbody>");

            foreach (DataGridViewRow row in dgvAcumulado.Rows)
            {
                bool esAcum = row.Cells["Archivo"].Value?.ToString() == "ACUMULADO";
                sb.Append(esAcum ? "<tr class='acum'>" : "<tr>");
                foreach (var col in colsVisibles)
                    sb.Append($"<td>{EscHtml(row.Cells[col.Index].Value?.ToString() ?? "")}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append($"<div class='pie'>Generado: {fecha}</div>");
            sb.Append("<script>window.onload=function(){{window.print();}}</script>");
            sb.Append("</body></html>");

            return sb.ToString();
        }

        // System.Net.WebUtility en lugar de System.Web.HttpUtility
        private static string EscHtml(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");

        // ── Helpers de UI ─────────────────────────────────────────────────
        private void RefrescarDgvPersonal()
        {
            dgvPersonalEncontrado.DataSource = null;
            dgvPersonalEncontrado.DataSource = _empleadosFiltrados;
            dgvPersonalEncontrado.ClearSelection();

            foreach (DataGridViewRow row in dgvPersonalEncontrado.Rows)
            {
                if (row.DataBoundItem is EmpleadoAcumulado emp && !string.IsNullOrWhiteSpace(emp.FechaBajaE))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(220, 220, 220);
                    row.DefaultCellStyle.ForeColor = Color.Gray;
                }
            }
        }

        private void RefrescarAcumuladoActual()
        {
            if (dgvPersonalEncontrado.SelectedRows.Count == 0) return;

            var row = dgvPersonalEncontrado.SelectedRows[0];
            if (row.DataBoundItem is EmpleadoAcumulado emp)
            {
                CargarAcumuladosEmpleado(emp.Id);
                return;
            }

            if (dgvPersonalEncontrado.Columns.Contains("Id") && row.Cells["Id"].Value != null && int.TryParse(row.Cells["Id"].Value.ToString(), out int id))
                CargarAcumuladosEmpleado(id);
        }

        private void AplicarFiltro()
        {
            string t = txtBuscar?.Text?.Trim().ToUpper() ?? string.Empty;

            IEnumerable<EmpleadoAcumulado> resultado = string.IsNullOrWhiteSpace(t) ? _empleados : _empleados.Where(emp =>
            emp.Nombre.ToUpper().Contains(t) || emp.RFCE.ToUpper().Contains(t) || emp.CURPE.ToUpper().Contains(t));

            if (_soloActivos)
                resultado = resultado.Where(emp => string.IsNullOrWhiteSpace(emp.FechaBajaE));

            _empleadosFiltrados = resultado.ToList();
            RefrescarDgvPersonal();

            if (_empleadosFiltrados.Count == 0)
                dgvAcumulado.Rows.Clear();
        }

        private void SeleccionarPrimerEmpleado()
        {
            if (dgvPersonalEncontrado.Rows.Count == 0) return;

            DataGridViewRow filaMin = null;
            int minId = int.MaxValue;

            foreach (DataGridViewRow row in dgvPersonalEncontrado.Rows)
            {
                if (row.DataBoundItem is EmpleadoAcumulado emp && emp.Id < minId)
                {
                    minId = emp.Id;
                    filaMin = row;
                }
            }

            if (filaMin != null)
            {
                dgvPersonalEncontrado.ClearSelection();
                filaMin.Selected = true;
                dgvPersonalEncontrado.CurrentCell = filaMin.Cells[0];
            }
        }

        private void CopiarGridConDialogo(DataGridView dgv, string etiqueta)
        {
            if (dgv.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para copiar.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var resp = MessageBox.Show($"¿Copiar la {etiqueta} con encabezados?\n\n" + "Sí → Con encabezados\nNo → Solo datos\nCancelar → No copiar",
                "Copiar al portapapeles", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (resp == DialogResult.Cancel) return;

            var sb = new StringBuilder();
            var colsVisibles = dgv.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();

            if (resp == DialogResult.Yes)
                sb.AppendLine(string.Join("\t", colsVisibles.Select(c => c.HeaderText)));

            foreach (DataGridViewRow row in dgv.Rows)
                sb.AppendLine(string.Join("\t", colsVisibles.Select(c => (row.Cells[c.Index].Value?.ToString() ?? "").Replace("\t", " "))));

            Clipboard.SetText(sb.ToString());
            MessageBox.Show("Datos copiados.\nPuede pegarlos en Excel (Ctrl+V).", "Copiado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MostrarResultadosEnVentana(string titulo, string texto)
        {
            using var f = new Form
            {
                Text = titulo,
                Size = new Size(680, 480),
                StartPosition = FormStartPosition.CenterParent
            };

            var txt = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Courier New", 9),
                Dock = DockStyle.Fill,
                Text = texto,
                WordWrap = false
            };

            var btnCopiar = new Button
            {
                Text = "Copiar al portapapeles",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            btnCopiar.Click += (s, e) =>
            {
                Clipboard.SetText(texto);
                MessageBox.Show("Copiado.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            f.Controls.Add(txt);
            f.Controls.Add(btnCopiar);
            f.ShowDialog(this);
        }

        // ── Helpers de archivos ───────────────────────────────────────────
        private List<string> ObtenerArchivosValidos()
        {
            if (!Directory.Exists(BaseRuta)) return new List<string>();

            var paresCompletos = Directory.GetFiles(BaseRuta, "*.NOM").Select(f => Path.GetFileNameWithoutExtension(f).ToUpper()).Where(n =>
            File.Exists(Path.Combine(BaseRuta, n + ".cmp")) || File.Exists(Path.Combine(BaseRuta, n + ".CMP"))).ToHashSet();

            string patron = ModoNOM ? "*.NOM" : "*.json";

            return Directory.GetFiles(BaseRuta, patron).Where(f =>
                {
                    string nombre = Path.GetFileNameWithoutExtension(f);
                    string nombreUp = nombre.ToUpper();
                    bool esNormal = PatronNormal.IsMatch(nombre);
                    bool esCorto = PatronNormalCorto.IsMatch(nombre);
                    bool tienePar = paresCompletos.Contains(nombreUp);

                    if (ModoNOM && !esNormal && !esCorto && !tienePar) return false;
                    if (_excluirUltimaDic && nombreUp.StartsWith("DIC2", StringComparison.OrdinalIgnoreCase))
                        return false;
                    if (!ArchivoEnRango(nombre)) return false;
                    if (_excluirEspeciales && !esNormal && !esCorto) return false;

                    return true;
                }).OrderBy(f => OrdenarArchivo(Path.GetFileNameWithoutExtension(f).ToUpper())).ToList();
        }

        // Extraído para eliminar la lambda compleja en OrderBy.
        private static (int anio, int mes, int quincena) OrdenarArchivo(string nombre)
        {
            if (PatronNormal.IsMatch(nombre) && nombre.Length >= 8)
            {
                string mes = nombre[..3];
                string quin = nombre.Substring(3, 1);
                string anio = nombre.Substring(4, 4);
                return (int.TryParse(anio, out int y) ? y : 9999, OrdenMes.TryGetValue(mes, out int om) ? om : 99, int.TryParse(quin, out int q) ? q : 9);
            }
            if (PatronNormalCorto.IsMatch(nombre) && nombre.Length == 5)
            {
                string mes = nombre[..3];
                string anio = nombre.Substring(3, 2);
                return (int.TryParse(anio, out int y) ? 2000 + y : 9998, OrdenMes.TryGetValue(mes, out int om) ? om : 99, 9);
            }
            return (9999, 99, 9);
        }

        private bool ArchivoEnRango(string nombreSinExt)
        {
            if (!PatronNormal.IsMatch(nombreSinExt) || nombreSinExt.Length < 8)
                return true;

            string mesAbrev = nombreSinExt[..3].ToUpper();
            if (!OrdenMes.TryGetValue(mesAbrev, out int mesMes)) return true;

            int mesInicio = MesSeleccionado(cbMesInicio, 1);
            int mesFinal = MesSeleccionado(cbMesFinal, 12);

            return mesMes >= mesInicio && mesMes <= mesFinal;
        }

        private int MesSeleccionado(ComboBox cb, int defecto)
        {
            if (cb.SelectedItem == null) return defecto;
            string nombre = cb.SelectedItem.ToString();
            return NombreAAbrev.TryGetValue(nombre, out string abrev) && OrdenMes.TryGetValue(abrev, out int num) ? num : defecto;
        }

        private void SetComboBoxPorMes(ComboBox cb, int mes)
        {
            string nombre = NombreAAbrev.FirstOrDefault(kv => OrdenMes.TryGetValue(kv.Value, out int m) && m == mes).Key;
            if (nombre == null) return;

            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (string.Equals(cb.Items[i].ToString(), nombre, StringComparison.OrdinalIgnoreCase))
                {
                    cb.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ActualizarEstadoComboboxes()
        {
            bool habilitado = chckboxRangoPersonalizado.Checked;
            cbMesInicio.Enabled = habilitado;
            cbMesFinal.Enabled = habilitado;
        }

        private static void SetDataPropertyName(DataGridView dgv, string col, string prop)
        {
            if (dgv.Columns.Contains(col))
                dgv.Columns[col].DataPropertyName = prop;
        }
    }
}

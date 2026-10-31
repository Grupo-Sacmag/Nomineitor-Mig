using ClosedXML.Excel;   // reemplaza Microsoft.Office.Interop.Excel
using Newtonsoft.Json;
using Nomina_2026_NET8;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Reflection;


namespace NOMINA_2025
{
    public partial class FormCaptura : Form
    {
        // ── Variables de estado ───────────────────────────────────────────
        private int anio = DateTime.Now.Year;
        private int mesSeleccionado = DateTime.Now.Month;
        private bool _recalculandoFila = false;
        private int DiasPeriodo = 15;

        // ── Propiedades públicas ──────────────────────────────────────────
        public string RutaArchivoACargar { get; set; }
        public string RutaTrabajo { get; set; }
        public string RutaRUEP { get; set; }
        public bool RequiereRUEP { get; set; } = true;
        public decimal SalarioMinimo { get; set; }
        public int AnioEmpresa { get; set; }
        public bool MostrarSimboloPesos { get; set; } = false;
        public bool ModoSoloLectura { get; set; } = false;

        // ── Servicios y datos ─────────────────────────────────────────────
        private bool _formularioCargado = false;
        private RuepService _personalService;
        private List<EmpleadoCompleto> _empleadosActivos;
        private List<EmpleadoCompleto> _todosEmpleadosRUEP;
        private ServicioCalculoNomina _servicioCalculo;
        private readonly Dictionary<int, decimal> _subsidioCausadoPorEmpleado = new Dictionary<int, decimal>();
        private RuepRoot _datosEmpresa;
        private readonly HashSet<int> _empleadosNuevosSinPagoAnterior = new HashSet<int>();
        private readonly Dictionary<int, int> _diasMaximosSugeridos = new Dictionary<int, int>();
        private readonly HashSet<string> _columnasBloqueadasPorTipo = new HashSet<string>();

        // ── Override de campos manuales ───────────────────────────────────
        // Indexado por idEmpleado (no por rowIndex) — seguro al reordenar
        private readonly HashSet<int> _otrasManualOverride = new HashSet<int>();
        // Exentas capturadas manualmente por empleado.
        // La clave es el ID, nunca la posición del grid.
        private readonly Dictionary<int, decimal> _exentaManualPorEmpleado = new();
        private bool _actualizandoExentas = false;
        private readonly HashSet<string> _fiscalManualOverride = new HashSet<string>();
        private readonly HashSet<string> _columnasOverrideFiscal = new HashSet<string> { "Ispt", "SubEmp", "IMSS" };

        // ── Columnas ──────────────────────────────────────────────────────
        private readonly HashSet<string> ColumnasQueRecalculan = new HashSet<string> { "DiasTrabajados", "Salario","hsNorm", "hsDobles", "hsTriples", "OF", "PVacac", "PercExenta", "Otras", "Prestamos",
            "FONACOT", "PensionAliment", "INFONAVIT" };
        private readonly string[] _columnasVisiblesPorDefecto = { "NumeroEmpleado", "NombreEmpleado", "DiasTrabajados", "Salario", "hsNorm", "hsDobles", "hsTriples", "OF", "PVacac", "Otras",
            "PercExenta", "TotIngr", "Ispt", "SubEmp", "IMSS", "Prestamos", "FONACOT", "PensionAliment", "INFONAVIT", "TotDeduc", "NETO", "BANAMEX" };
        private readonly string[] _columnasMovimientoNominaEspecial = { "DiasTrabajados", "Salario", "hsNorm", "hsDobles", "hsTriples", "OF", "PVacac", "Otras",
            "PercExenta", "TotIngr", "Ispt", "SubEmp", "IMSS", "Prestamos", "FONACOT", "PensionAliment", "INFONAVIT", "TotDeduc", "NETO" };
        private static readonly string[] _columnasDeConcepto = { "DiasTrabajados", "Salario", "hsNorm", "hsDobles", "hsTriples", "OF", "PVacac", "Otras", "PercExenta" };

        // ── Ordenamiento ──────────────────────────────────────────────────
        private int _ultimaColumnaOrdenada = -1;
        private SortOrder _ultimoOrden = SortOrder.None;
        private int _filaResaltada = -1;
        private TipoNominaCaptura _tipoColumnasActual = TipoNominaCaptura.Ordinaria;
        private int _indiceMotivoAnterior = -1;
        private bool _revirtiendoMotivo = false;

        public FormCaptura()
        {
            InitializeComponent();
            this.DpiChanged += FormCaptura_DpiChanged;
            PersonalizacionInicial();
            panelSuperior.Height = (int)(155 * (this.DeviceDpi / 96.0f));

            ConfigurarModuloImpresionNomina();
        }

        // ── Load ──────────────────────────────────────────────────────────
        private void FormCaptura_Load(object sender, EventArgs e)
        {
            dgvMostrarPersonal.DoubleBuffered(true);
            ConfigurarComportamientoGrid();
            CargarOpcionesMostrarColumnas();
            SeleccionarQuincenaPorFecha();

            if (RequiereRUEP)
            {
                if (string.IsNullOrWhiteSpace(RutaRUEP) || !File.Exists(RutaRUEP))
                {
                    MessageBox.Show("No se encontró RUEP.dat", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                _personalService = new RuepService(RutaRUEP);
                _datosEmpresa = _personalService.DatosEmpresa;

                _todosEmpleadosRUEP = _personalService.ObtenerTodos().Where(emp => !string.IsNullOrWhiteSpace(emp.Nombre) && !string.IsNullOrWhiteSpace(emp.ApellidoP))
                    .OrderBy(emp => emp.ApellidoP).ThenBy(emp => emp.ApellidoM).ThenBy(emp => emp.Nombre).ToList();

                _empleadosActivos = _personalService.ObtenerTodos().Where(emp => !string.IsNullOrWhiteSpace(emp.Nombre) && !string.IsNullOrWhiteSpace(emp.ApellidoP) &&
                EsVigenteEnPeriodo(emp.FechaBaja, AnioEmpresa)).OrderBy(emp => emp.ApellidoP).ThenBy(emp => emp.ApellidoM).ThenBy(emp => emp.Nombre).ToList();

                InicializarServicioCalculo();
                CargarEmpleadosEnGrid();
                EscalarColumnasSegunDpi();
                AplicarCalculosIniciales();
                DetectarYConfirmarEmpleadosNuevosSinPagoAnterior();
                ActualizarLblDatos();

                _formularioCargado = true;
            }

            if (!string.IsNullOrEmpty(RutaArchivoACargar) && File.Exists(RutaArchivoACargar))
            {
                string ext = Path.GetExtension(RutaArchivoACargar).ToLower();
                if (ext == ".json")
                    CargarDatosDesdeJSON(RutaArchivoACargar);
                else if (ext == ".nom")
                    CargarDatosDesdeNOM(RutaArchivoACargar);
            }
        }

        // ── Eventos de tipo de nómina ─────────────────────────────────────

        private void rdiobtnNormal_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdiobtnNormal.Checked) return;
            cbMotivo.SelectedIndex = -1;
            txtOtroRazon.Visible = false;
            txtOtroRazon.Text = "";
            AjustarColumnasParaTipoNomina("ENE12025");
            ActualizarVisibilidadCE("");
            AplicarVisibilidadPorTipoNomina(forzarActualizarGrid: true);
        }

        private void rdiobtnEspecial_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdiobtnEspecial.Checked) return;
            ActualizarVisibilidadCE(cbMotivo.SelectedItem?.ToString() ?? "");
            AplicarVisibilidadPorTipoNomina(forzarActualizarGrid: true);
            AjustarColumnasParaTipoNomina(cbMotivo.SelectedItem?.ToString() ?? "OTROS");
        }

        private void cbMotivo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_revirtiendoMotivo) return;

            // Cambiar el motivo cambia el significado de las columnas: no mezclar capturas.
            if (_formularioCargado && rdiobtnEspecial.Checked && cbMotivo.SelectedIndex >= 0 && cbMotivo.SelectedIndex != _indiceMotivoAnterior && HayCapturaEnGrid())
            {
                if (MessageBox.Show("Cambiar el motivo cambia el significado de las columnas.\n\nSe borrará lo capturado en la tabla. ¿Deseas continuar?", "Cambiar motivo",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
                {
                    _revirtiendoMotivo = true;
                    cbMotivo.SelectedIndex = _indiceMotivoAnterior;
                    _revirtiendoMotivo = false;
                    return;
                }
            }
            _indiceMotivoAnterior = cbMotivo.SelectedIndex;

            if (_formularioCargado && rdiobtnEspecial.Checked && cbMotivo.SelectedIndex >= 0)
            {
                _otrasManualOverride.Clear();
                CargarEmpleadosEnGrid();
                AplicarCalculosIniciales();
            }

            string motivo = cbMotivo.SelectedItem?.ToString() ?? "";

            if (motivo.Equals("OTROS...", StringComparison.OrdinalIgnoreCase))
            {
                txtOtroRazon.Visible = true;
                txtOtroRazon.Focus();
                lblDatos.Text = "Nómina Especial — OTROS";
                AjustarColumnasParaTipoNomina("OTROS");
            }
            else
            {
                txtOtroRazon.Visible = false;
                txtOtroRazon.Text = "";
                if (!string.IsNullOrWhiteSpace(motivo))
                {
                    lblDatos.Text = $"Nómina Especial — {motivo}";
                    AjustarColumnasParaTipoNomina(motivo);
                }
            }

            ActualizarVisibilidadCE(motivo);
        }

        // ── Crédito/Exención especial (equivalente a Text3/"exento" de VB6) ─ Solo aplica para PTU y Aguinaldo, igual que en checar() (Case 5 / Case 6 del VB6).
        // Para cualquier otro motivo (Liquidación, Finiquito, Bono, etc.) el control permanece oculto, tal como en el original: esos conceptos pasan sin repartir (Case 4 en VB6 — "dato_sal = dato_ent" directo, sin exención).
        // La regla vive en ConstantesNomina.EsMotivoPtuOAguinaldo (CalculadoraNomina.cs) para que la UI y el motor de cálculo nunca se desincronicen sobre qué motivos aplican.
        private void ActualizarVisibilidadCE(string motivo)
        {
            TipoNominaCaptura tipo = PerfilColumnasNomina.Clasificar(motivo);

            bool primaVacacionalEspecial = tipo == TipoNominaCaptura.Otra && PerfilColumnasNomina.Normalizar(motivo).Contains("VACAC");
            bool aplicaGlobal = !rdiobtnEspecial.Checked || tipo == TipoNominaCaptura.PTU || tipo == TipoNominaCaptura.Aguinaldo || primaVacacionalEspecial;

            chkBoxCE.Visible = aplicaGlobal;

            if (!aplicaGlobal)
            {
                chkBoxCE.Checked = false;
                txtCE.Visible = false;
                lblCE.Visible = false;
            }
            else
            {
                txtCE.Visible = chkBoxCE.Checked;
                lblCE.Visible = chkBoxCE.Checked;
            }
        }

        // ── Eventos de quincena ───────────────────────────────────────────

        private void rdiobtnPrimera_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdiobtnPrimera.Checked) return;

            if (_formularioCargado && dgvMostrarPersonal.Rows.Count > 0 && MessageBox.Show("¿Deseas cambiar a Primera Quincena?\n\nLos datos no guardados se perderán.", "Cambiar Quincena",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                rdiobtnSegunda.CheckedChanged -= rdiobtnSegunda_CheckedChanged;
                rdiobtnSegunda.Checked = true;
                rdiobtnSegunda.CheckedChanged += rdiobtnSegunda_CheckedChanged;
                return;
            }

            RefrescarGrid();
        }

        private void rdiobtnSegunda_CheckedChanged(object sender, EventArgs e)
        {
            if (!rdiobtnSegunda.Checked) return;

            if (_formularioCargado && dgvMostrarPersonal.Rows.Count > 0 && MessageBox.Show("¿Deseas cambiar a Segunda Quincena?\n\nLos datos no guardados se perderán.", "Cambiar Quincena",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                rdiobtnPrimera.CheckedChanged -= rdiobtnPrimera_CheckedChanged;
                rdiobtnPrimera.Checked = true;
                rdiobtnPrimera.CheckedChanged += rdiobtnPrimera_CheckedChanged;
                return;
            }

            RefrescarGrid();
        }

        // Extraído — ambas quincenas hacen lo mismo
        private void RefrescarGrid()
        {
            _otrasManualOverride.Clear();
            CargarEmpleadosEnGrid();
            AplicarCalculosIniciales();
            DetectarYConfirmarEmpleadosNuevosSinPagoAnterior();
            ActualizarLblDatos();
        }

        private bool TryObtenerExentaGlobal(out decimal monto)
        {
            monto = 0m;
            string texto = (txtCE.Text ?? "").Replace("$", "").Replace(",", "").Trim();

            if (!decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out monto))
            {
                return false;
            }

            return monto >= 0m && decimal.Round(monto, 2) == monto;
        }

        private decimal ObtenerBrutoAplicableExenta(DataGridViewRow fila)
        {
            decimal primaVacacional = GetDecimalSeguro(fila, "PVacac");

            if (!rdiobtnEspecial.Checked)
                return primaVacacional;

            string motivo = cbMotivo.SelectedItem?.ToString() ?? "";

            TipoNominaCaptura tipo = PerfilColumnasNomina.Clasificar(motivo);

            if (tipo == TipoNominaCaptura.PTU || tipo == TipoNominaCaptura.Aguinaldo)
            {
                return GetDecimalSeguro(fila, "Otras");
            }

            if (tipo == TipoNominaCaptura.Otra && PerfilColumnasNomina.Normalizar(motivo).Contains("VACAC"))
            {
                return GetDecimalSeguro(fila, "Otras");
            }

            // En separaciones, permitir capturar el total exento individual sujeto al límite del bruto.
            // Su distribución por concepto quedará para el modelo detallado de JSON.
            if (PerfilColumnasNomina.EsSeparacion(tipo))
            {
                // Formato nuevo de separación de C#:
                //
                // hsDobles = Aguinaldo
                // hsTriples = Prima de antigüedad
                // PVacac = Prima vacacional
                //
                // OF = Indemnización en liquidaciones
                // OF = Bono en finiquitos
                //
                // No considerar automáticamente como exentables
                // salario, compensaciones, sueldo de vacaciones,
                // bono ni otros conceptos sin clasificación.

                decimal aguinaldo = Math.Max(0m, GetDecimalSeguro(fila, "hsDobles"));
                decimal primaAntiguedad = Math.Max(0m, GetDecimalSeguro(fila, "hsTriples"));
                primaVacacional = Math.Max(0m, primaVacacional);
                decimal indemnizacion = tipo == TipoNominaCaptura.Liquidacion ? Math.Max(0m, GetDecimalSeguro(fila, "OF")) : 0m;

                return aguinaldo + primaAntiguedad + primaVacacional + indemnizacion;
            }

            return 0m;
        }

        private void AplicarExentaGlobal()
        {
            if (_actualizandoExentas || !_formularioCargado)
                return;

            decimal monto = 0m;

            if (chkBoxCE.Checked && !TryObtenerExentaGlobal(out monto))
            {
                return;
            }

            var filas = dgvMostrarPersonal.Rows.Cast<DataGridViewRow>().Where(f => !f.IsNewRow && f.Tag?.ToString() != "Totales").ToList();

            // No aplicar una exenta superior al concepto bruto de alguno de los empleados afectados.
            if (chkBoxCE.Checked)
            {
                var invalidas = filas.Where(f =>
                {
                    if (!int.TryParse(f.Cells["NumeroEmpleado"].Value?.ToString(), out int id))
                        return false;

                    if (_exentaManualPorEmpleado.ContainsKey(id))
                        return false;

                    decimal bruto = ObtenerBrutoAplicableExenta(f);

                    return bruto > 0m && monto > bruto;
                }).ToList();

                if (invalidas.Count > 0)
                {
                    MessageBox.Show("El importe exento global excede el concepto bruto de uno o más empleados.\n\nNo se aplicó el cambio global.\nRevisa el importe o captura una exenta " +
                        "individual para esos empleados.", "Importe exento no aplicable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            _actualizandoExentas = true;

            try
            {
                foreach (DataGridViewRow fila in filas)
                {
                    if (!int.TryParse(fila.Cells["NumeroEmpleado"].Value?.ToString(), out int id))
                        continue;

                    // Una excepción manual siempre tiene prioridad.
                    if (_exentaManualPorEmpleado.ContainsKey(id))
                        continue;

                    decimal bruto = ObtenerBrutoAplicableExenta(fila);
                    decimal exentaNueva = chkBoxCE.Checked && bruto > 0m ? monto : 0m;

                    if (GetDecimalSeguro(fila, "PercExenta") == exentaNueva)
                        continue;

                    fila.Cells["PercExenta"].Value = exentaNueva.ToString("F2", CultureInfo.InvariantCulture);
                    RecalcularFilaCompleta(fila.Index, "PercExenta");
                }
            }
            finally
            {
                _actualizandoExentas = false;
            }

            AgregarFilaTotales();
        }
        private void chkBoxCE_CheckedChanged(object sender, EventArgs e)
        {
            lblCE.Visible = chkBoxCE.Checked;
            txtCE.Visible = chkBoxCE.Checked;

            if (!_formularioCargado)
                return;

            if (chkBoxCE.Checked)
            {
                MessageBox.Show("La cantidad exenta global se aplicará a los empleados que tengan el concepto de percepción correspondiente.\n\nSi modificas la columna Percepción Exenta " +
                    "de un empleado, solamente ese empleado conservará su importe individual.", "Percepción exenta global", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            AplicarExentaGlobal();
        }

        private void cbMes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbMes.SelectedIndex >= 0)
                mesSeleccionado = ObtenerMesNomina();
            RefrescarGrid();
        }

        // ── Eventos de toolbar ────────────────────────────────────────────

        private void tsmCFDIEdicion_Click(object sender, EventArgs e)
        {
            if (dgvMostrarPersonal.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos de nómina para generar la plantilla CFDI.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int mes = ObtenerMesNomina();
            bool esPrimera = rdiobtnPrimera.Checked;

            DateTime fechaPago = esPrimera ? new DateTime(anio, mes, 15) : new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes));

            // Aquí ya ejecutará CalculoRengVB6 para cada empleado.
            var datos = ConstruirDatosCFDI();

            if (datos.Count == 0)
            {
                MessageBox.Show("No se pudieron obtener datos de empleados para la plantilla.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.Enabled = false;

            try
            {
                using var formCFDI = new FormNOMCFDIPlantillas();

                var configCfdi = _personalService.ObtenerConfiguracionCfdiNomina();
                var datosInicio = CapturaCfdiNominaVB6.Capturar(configCfdi);

                // Primer Put equivalente
                _personalService.GuardarConfiguracionCfdiNomina(
                    datosInicio.FolioAnterior,
                    datosInicio.Serie,
                    datosInicio.Consecutivo,
                    datosInicio.RegistroPatronal,
                    datosInicio.RiesgoImss);

                formCFDI.LlenarDesdeCaptura(
                    datos,
                    esExtraordinaria: rdiobtnEspecial.Checked,
                    fechaPago: fechaPago,
                    esPrimeraQuincena: esPrimera,
                    anioEmpresa: anio,
                    datosInicio: datosInicio);

                formCFDI.ShowDialog();

                // Segundo Put equivalente
                _personalService.GuardarConfiguracionCfdiNomina(
                    formCFDI.FolioFinal,
                    datosInicio.Serie,
                    datosInicio.Consecutivo,
                    datosInicio.RegistroPatronal,
                    datosInicio.RiesgoImss);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void tsmEditarPersonal_Click(object sender, EventArgs e)
        {
            var form = new FormEDICIONCENTRALPERSONAL
            {
                RutaRUEP = RutaRUEP
            };

            form.FormClosed += (s, args) =>
            {
                if (form.HuboCambios) RecargarDatosRUEP();
            };

            form.ShowDialog();
        }

        // tsmDistribucionNominaEdicion_Click ELIMINADO — estaba vacío

        private void tsArchivarNomina_Click(object sender, EventArgs e) => GuardarNomina();
        private void tsmAlfabetoAscendente_Click(object sender, EventArgs e) => OrdenarGrid(asc: true, porNombre: true);
        private void tsmAlfabetoDescendente_Click(object sender, EventArgs e) => OrdenarGrid(asc: false, porNombre: true);
        private void tsmNumericoAscendente_Click(object sender, EventArgs e) => OrdenarGrid(asc: true, porNombre: false);
        private void tsmNumericoDescendente_Click(object sender, EventArgs e) => OrdenarGrid(asc: false, porNombre: false);

        private void tsmArchivoBanamexEdicion_Click(object sender, EventArgs e)
        {
            if (dgvMostrarPersonal.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos de nómina para generar el archivo Banamex.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var datosDispersion = new List<DatoDispersionBancaria>();
            int sinCuenta = 0;

            foreach (DataGridViewRow row in dgvMostrarPersonal.Rows)
            {
                if (row.IsNewRow || row.Tag?.ToString() == "Totales") continue;

                string idStr = row.Cells["NumeroEmpleado"].Value?.ToString() ?? "0";
                if (!int.TryParse(idStr, out int idEmp)) continue;

                string cuenta = row.Cells["BANAMEX"].Value?.ToString()?.Trim() ?? "";
                decimal NETO = GetDecimal(row, "NETO");

                if (string.IsNullOrEmpty(cuenta) || NETO <= 0m)
                {
                    sinCuenta++;
                    continue;
                }

                var emp = _empleadosActivos?.FirstOrDefault(x => x.Id == idEmp);
                if (emp == null) { sinCuenta++; continue; }

                datosDispersion.Add(new DatoDispersionBancaria
                {
                    NumeroRegistro = idEmp,
                    CuentaTarjeta = cuenta,
                    Nombre = emp.Nombre,
                    ApellidoP = emp.ApellidoP,
                    ApellidoM = emp.ApellidoM,
                    Importe = NETO
                });
            }

            if (datosDispersion.Count == 0)
            {
                MessageBox.Show($"Ningún empleado tiene cuenta bancaria o importe > $0.\n\n" + $"Sin cuenta: {sinCuenta}", "Sin registros", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int mesFinal = ObtenerMesNomina();
            string nomEmpresa = _datosEmpresa?.nombre ?? "EMPRESA";
            string etiquetaQ = $"{AbrevMes(mesFinal)}-{(rdiobtnPrimera.Checked ? 1 : 2)}Q";

            this.Enabled = false;
            try
            {
                var formBanco = new FormTraspasoNomBanamex();
                formBanco.CargarDatos(datosDispersion, nomEmpresa, mesFinal, anio, rdiobtnPrimera.Checked, etiquetaQ, _datosEmpresa, RutaTrabajo);
                formBanco.ShowDialog(this);
            }
            finally { this.Enabled = true; }
        }

        private void tsmMostrarSimboloPesos_Click(object sender, EventArgs e)
        {
            MostrarSimboloPesos = tsMostrarSimboloPesos.Checked;
            dgvMostrarPersonal.Refresh();
        }

        private void tsMostrarColumna_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is string colName && dgvMostrarPersonal.Columns.Contains(colName))
            {
                if (_columnasBloqueadasPorTipo.Contains(colName)) { item.Checked = false; return; }
                dgvMostrarPersonal.Columns[colName].Visible = item.Checked;
            }
        }

        private void tsReestablecerTabla_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn col in dgvMostrarPersonal.Columns)
                col.Visible = _columnasVisiblesPorDefecto.Contains(col.Name);

            foreach (ToolStripMenuItem item in tsMostrarColumna.DropDownItems)
            {
                if (item.Tag is string colName)
                {
                    var column = dgvMostrarPersonal.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.Name == colName);
                    if (column != null)
                        item.Checked = column.Visible;
                }
            }

            AplicarColumnasVisiblesPorTipo(_tipoColumnasActual);
        }

        private void tsSeleccionarTodo_Click(object sender, EventArgs e) => dgvMostrarPersonal.SelectAll();
        private void tsCopiarSinEncabezados_Click(object sender, EventArgs e) => CopyDataGridView(false);
        private void tsCopiarConEncabezados_Click(object sender, EventArgs e) => CopyDataGridView(true);

        // Exportar a Excel con ClosedXML — reemplaza Office.Interop
        private void tsAtajoExcel_Click(object sender, EventArgs e) => ExportarAExcel();

        // ── Eventos del grid ──────────────────────────────────────────────

        private void dgvMostrarPersonal_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var columnasConFormato = new HashSet<string> { "Salario","hsNorm","hsDobles","hsTriples","OF","PVacac", "Otras","PercExenta","TotIngr","Ispt","SubEmp","IMSS", "Prestamos",
                "FONACOT","PensionAliment","INFONAVIT", "TotDeduc","NETO" };

            string colNombre = dgvMostrarPersonal.Columns[e.ColumnIndex].Name;
            if (e.Value == null || !columnasConFormato.Contains(colNombre)) return;

            if (decimal.TryParse(e.Value.ToString(), out decimal val))
            {
                e.Value = val == 0 ? "" : MostrarSimboloPesos ? val.ToString("C2", new CultureInfo("es-MX")) : val.ToString("N2", new CultureInfo("es-MX"));

                if (colNombre == "SubEmp" && val < 0)
                    e.CellStyle.ForeColor = Color.Red;

                e.FormattingApplied = true;
            }
        }

        private void dgvMostrarPersonal_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_recalculandoFila) return;
            if (e.RowIndex < 0 || dgvMostrarPersonal.Rows[e.RowIndex].Tag?.ToString() == "Totales")
                return;

            string colName = dgvMostrarPersonal.Columns[e.ColumnIndex].Name;
            var filaActual = dgvMostrarPersonal.Rows[e.RowIndex];

            if (!int.TryParse(filaActual.Cells["NumeroEmpleado"].Value?.ToString(), out int idEmpleadoExenta))
            {
                return;
            }

            if (colName == "PercExenta")
            {
                // Si se dejó la celda vacía, quitar la excepción individual de este empleado.
                string textoExenta = filaActual.Cells["PercExenta"].Value?.ToString()?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(textoExenta))
                {
                    _exentaManualPorEmpleado.Remove(idEmpleadoExenta);

                    decimal exentaRestaurada = 0m;

                    if (chkBoxCE.Checked && TryObtenerExentaGlobal(out decimal exentaGlobal))
                    {
                        decimal bruto = ObtenerBrutoAplicableExenta(filaActual);

                        if (bruto > 0m && exentaGlobal <= bruto)
                        {
                            exentaRestaurada = exentaGlobal;
                        }
                        else if (bruto > 0m && exentaGlobal > bruto)
                        {
                            MessageBox.Show("La percepción exenta global excede el bruto aplicable de este empleado.\n\nSe dejará su exenta en cero hasta que corrijas el importe.",
                                "Revisar exenta global", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }

                    filaActual.Cells["PercExenta"].Value = exentaRestaurada.ToString("F2", CultureInfo.InvariantCulture);

                    RecalcularFilaCompleta(e.RowIndex, "PercExenta");

                    return;
                }

                decimal exentaCapturada = GetDecimalSeguro(filaActual, "PercExenta");
                decimal brutoAplicable = ObtenerBrutoAplicableExenta(filaActual);

                if (exentaCapturada < 0m || exentaCapturada > brutoAplicable)
                {
                    MessageBox.Show("La percepción exenta no puede ser negativa ni exceder el importe bruto aplicable.\n\nRevisa el concepto y su cantidad exenta.", "Percepción exenta inválida",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    filaActual.Cells["PercExenta"].Value = "0.00";
                    _exentaManualPorEmpleado[idEmpleadoExenta] = 0m;
                }
                else
                {
                    bool primeraExcepcion = !_exentaManualPorEmpleado.ContainsKey(idEmpleadoExenta);
                    _exentaManualPorEmpleado[idEmpleadoExenta] = exentaCapturada;

                    if (chkBoxCE.Checked && primeraExcepcion)
                    {
                        MessageBox.Show($"Se aplicará una percepción exenta individual " + $"al empleado {idEmpleadoExenta}.\n\nEl importe global seguirá aplicándose a los demás empleados.",
                            "Exenta individual", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                RecalcularFilaCompleta(e.RowIndex, "PercExenta");
                return;
            }

            if (_columnasOverrideFiscal.Contains(colName))
            {
                if (rdiobtnNormal.Checked)
                {
                    RecalcularFilaCompleta(e.RowIndex);
                    return;
                }

                if (!int.TryParse(dgvMostrarPersonal.Rows[e.RowIndex].Cells["NumeroEmpleado"].Value?.ToString(), out int idFiscal))
                {
                    return;
                }

                // Identificar el cambio por empleado y concepto, nunca por la posición de la fila.
                string clave = $"{idFiscal}:{colName}";
                if (MessageBox.Show("Si modifica esta celda ya no se efectuará el cálculo automático de la retención.\n\n¿Mantener el valor ingresado?", "Modificación Manual",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {
                    _fiscalManualOverride.Add(clave);
                    RecalcularTotalesConFiscalFijo(e.RowIndex);
                }
                else
                {
                    _fiscalManualOverride.Remove(clave);
                    RecalcularFilaCompleta(e.RowIndex);
                }
                return;
            }

            // Actualizar el importe exento al modificar un concepto que puede contener una parte exenta.
            if (colName == "Otras" || colName == "PVacac")
            {
                decimal brutoAplicable = ObtenerBrutoAplicableExenta(filaActual);

                if (_exentaManualPorEmpleado.TryGetValue(idEmpleadoExenta, out decimal exentaManual))
                {
                    if (exentaManual > brutoAplicable)
                    {
                        filaActual.Cells["PercExenta"].Value = "0.00";
                        _exentaManualPorEmpleado[idEmpleadoExenta] = 0m;

                        MessageBox.Show("El concepto bruto cambió y la exenta individual anterior ya no es válida.\n\nSe restableció a cero para este empleado.", "Revisar percepción exenta",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else if (chkBoxCE.Checked && TryObtenerExentaGlobal(out decimal exentaGlobal))
                {
                    if (brutoAplicable > 0m && exentaGlobal > brutoAplicable)
                    {
                        filaActual.Cells["PercExenta"].Value = "0.00";

                        MessageBox.Show("El importe exento global supera el concepto bruto de este empleado.\n\nCorrige el importe global o captura una exenta individual.",
                            "Exenta global no aplicable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        decimal exentaAplicar = brutoAplicable > 0m ? exentaGlobal : 0m;
                        filaActual.Cells["PercExenta"].Value = exentaAplicar.ToString("F2", CultureInfo.InvariantCulture);
                    }
                }
            }

            if (ColumnasQueRecalculan.Contains(colName))
                RecalcularFilaCompleta(e.RowIndex, colName);
        }

        private void dgvMostrarPersonal_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (!(e.Control is System.Windows.Forms.TextBox tb)) return;

            tb.KeyPress -= SoloNumerosYPunto;
            tb.TextChanged -= ValidarDosDecimales;
            tb.TextChanged -= FormatearTarjeta;

            string colName = dgvMostrarPersonal.Columns[dgvMostrarPersonal.CurrentCell.ColumnIndex].Name;

            if (colName == "DiasTrabajados" || ColumnasQueRecalculan.Contains(colName))
            {
                tb.KeyPress += SoloNumerosYPunto;
                tb.TextChanged += ValidarDosDecimales;
            }

            if (colName == "BANAMEX")
                tb.TextChanged += FormatearTarjeta;
        }

        private void dgvMostrarPersonal_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control || e.KeyCode != Keys.V) return;

            var columnasSeleccionadas = dgvMostrarPersonal.SelectedCells.Cast<DataGridViewCell>().Select(c => c.ColumnIndex).Distinct().Count();

            if (columnasSeleccionadas > 1)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                MessageBox.Show("Solo se permite pegar dentro de una celda a la vez.", "Pegado restringido", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void dgvMostrarPersonal_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvMostrarPersonal.Rows[e.RowIndex].Tag?.ToString() == "Totales")
                return;
            dgvMostrarPersonal.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.FromArgb(128, 255, 128);
        }

        private void dgvMostrarPersonal_CellLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            dgvMostrarPersonal.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.Empty;
        }

        private void dgvMostrarPersonal_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var dgv = dgvMostrarPersonal;
            string col = dgv.Columns[e.ColumnIndex].Name;

            if (col == "NumeroEmpleado" || col == "NombreEmpleado")
            {
                dgv.ClearSelection();
                PintarFilaVerde(e.RowIndex);
                dgv.CurrentCell = dgv.Rows[e.RowIndex].Cells["DiasTrabajados"];
            }
            else if (_filaResaltada >= 0)
            {
                LimpiarResaltadoFila(_filaResaltada);
                _filaResaltada = -1;
            }
        }

        private void FormCaptura_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            float factor = e.DeviceDpiNew / (float)e.DeviceDpiOld;
            this.Scale(new System.Drawing.SizeF(factor, factor));
            foreach (DataGridViewColumn col in dgvMostrarPersonal.Columns)
                col.Width = (int)(col.Width * factor);
        }

        private void FormCaptura_FormClosing(object sender, FormClosingEventArgs e)
        {
            dgvMostrarPersonal.Rows.Clear();
            dgvMostrarPersonal.Columns.Clear();
            dgvMostrarPersonal.Dispose();
            GC.Collect();
        }

        private void txtOtroRazon_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !Regex.IsMatch(e.KeyChar.ToString(), @"[A-Za-z_]"))
                e.Handled = true;
        }

        private void txtOtroRazon_TextChanged(object sender, EventArgs e)
        {
            int pos = txtOtroRazon.SelectionStart;
            txtOtroRazon.SelectionStart = pos;

            string texto = txtOtroRazon.Text.Trim().ToUpper();
            lblDatos.Text = string.IsNullOrWhiteSpace(texto)
                ? "Nómina Especial — OTROS"
                : $"Nómina Especial — {texto}";

            if (!string.IsNullOrWhiteSpace(texto))
                AjustarColumnasParaTipoNomina(texto, tipoForzado: TipoNominaCaptura.Otra);
        }

        // ── Carga de datos externos ───────────────────────────────────────

        public void CargarDatosDesdeNOM(string rutaArchivo)
        {
            try
            {
                dgvMostrarPersonal.Rows.Clear();
                dgvMostrarPersonal.SuspendLayout();

                byte[] raw = File.ReadAllBytes(rutaArchivo);
                const int REC = 192;
                int totalReg = raw.Length / REC;

                string dir = Path.GetDirectoryName(rutaArchivo) ?? "";
                var nombres = LeerNombresDesdePersonal(Path.Combine(dir, "personal.dno"));
                var cuentas = LeerBanamexDesdeArchivo(Path.Combine(dir, "bnxcla.dno"));
                string nomArch = Path.GetFileNameWithoutExtension(rutaArchivo).ToUpper();
                var tipoNom = PerfilColumnasNomina.Clasificar(nomArch);
                bool esSeparacion = PerfilColumnasNomina.EsSeparacion(tipoNom);
                // Offset 40 = campo 5 (.hstri) del primer registro: marca de formato nuevo.
                bool separacionNueva = esSeparacion && raw.Length >= REC && BitConverter.ToInt64(raw, 5 * 8) == PerfilColumnasNomina.MarcaSeparacion2026Raw;

                long ReadI64(int offset) => offset + 8 <= raw.Length ? BitConverter.ToInt64(raw, offset) : 0L;

                for (int i = 0; i < totalReg; i++)
                {
                    int baseOff = i * REC;
                    long[] vals = new long[24];
                    for (int j = 0; j < 24; j++)
                        vals[j] = ReadI64(baseOff + j * 8);

                    // En formato nuevo, .hstri (idx 5) es la MARCA, no un importe.
                    if (separacionNueva) vals[5] = 0;

                    if (vals.All(v => v == 0)) continue;

                    decimal D(int idx) => vals[idx] / 10_000m;

                    decimal dias = D(0);
                    decimal bonoSep = D(1);                       // .hsnor
                    decimal ispt = D(7), crdsal = D(8), imss = D(9);
                    decimal sueldo = D(10);
                    decimal hs_nor = D(11), hs_dbl = D(12), hs_tri = D(13);
                    decimal viaticos = D(14), pvac = D(15), otras = D(16);
                    decimal aguin = D(17), ptu = D(18), exentos = D(19);
                    decimal prestamos = D(20), fonacot = D(21);
                    decimal telefono = D(22), otraded = D(23);

                    decimal gNor, gDbl, gTri, gOF, otrasDisplay, totIngr;

                    if (separacionNueva)
                    {
                        // Mapeo de GridANomina (VB6): Comp=hs_nor, Agui=aguin, Antig=hs_dbl,
                        // Indem=hs_tri, SdoVac=viaticos, Bono=hsnor, PV=pvac
                        gNor = tipoNom == TipoNominaCaptura.Finiquito ? viaticos : hs_nor;
                        gDbl = aguin;
                        gTri = hs_dbl;
                        gOF = tipoNom == TipoNominaCaptura.Finiquito ? bonoSep : hs_tri;
                        otrasDisplay = otras;
                        // En formato nuevo la exenta NO se suma (igual que carganom de VB6).
                        totIngr = sueldo + gNor + gDbl + gTri + gOF + pvac + otras;
                    }
                    else
                    {
                        // Comportamiento original: ordinarias, especiales y separación legacy.
                        gNor = hs_nor; gDbl = hs_dbl; gTri = hs_tri; gOF = viaticos;
                        if (nomArch.StartsWith("PRE") && tipoNom != TipoNominaCaptura.Bono) { gTri = viaticos; gOF = 0m; }

                        decimal otrasTotal = otras + aguin + ptu + (esSeparacion ? bonoSep : 0m);
                        otrasDisplay = nomArch.StartsWith("PTU") ? ptu : nomArch.StartsWith("AGUIN") ? aguin : otrasTotal;
                        totIngr = sueldo + gNor + gDbl + gTri + gOF + pvac + otrasTotal + exentos;
                    }
                    decimal isrNETO = Math.Max(0m, ispt - crdsal);
                    decimal totDeduc = isrNETO + imss + prestamos + fonacot + telefono + otraded;
                    decimal NETO = totIngr - totDeduc;

                    string nombre = i < nombres?.Count ? nombres[i] : "";
                    string banamex = i < cuentas?.Count ? cuentas[i] : "";

                    int rowIdx = dgvMostrarPersonal.Rows.Add();
                    var row = dgvMostrarPersonal.Rows[rowIdx];

                    SetRow(row, i + 1, nombre, (int)dias, sueldo, gNor, gDbl, gTri, gOF, pvac, otrasDisplay, exentos, totIngr, ispt, crdsal, imss, prestamos, fonacot, telefono, otraded, totDeduc, NETO, banamex);
                }

                dgvMostrarPersonal.ResumeLayout();
                ActualizarLblDatosPorNombre(Path.GetFileNameWithoutExtension(rutaArchivo));
                AjustarColumnasParaTipoNomina(Path.GetFileNameWithoutExtension(rutaArchivo), separacionLegacy: esSeparacion && !separacionNueva);
                AgregarFilaTotales();
                AplicarModoSoloLectura();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir archivo NOM:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Usa NominaJsonService.Cargar — elimina lógica de descifrado duplicada
        public void CargarDatosDesdeJSON(string rutaArchivo)
        {
            try
            {
                var datos = NominaJsonService.Cargar(rutaArchivo);

                dgvMostrarPersonal.Rows.Clear();
                dgvMostrarPersonal.SuspendLayout();

                string nomArch = Path.GetFileNameWithoutExtension(rutaArchivo).ToUpper();

                // Metadatos del sobre; los archivos anteriores no los traen y se deduce por nombre.
                int formato = 1;
                TipoNominaCaptura tipo = PerfilColumnasNomina.Clasificar(nomArch);
                try
                {
                    var sobre = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(rutaArchivo, Encoding.UTF8));
                    formato = sobre.Value<int?>("FormatoColumnas") ?? 1;
                    if (Enum.TryParse(sobre.Value<string>("TipoCFDI"), out TipoNominaCaptura tEnv)) tipo = tEnv;
                }
                catch { }

                bool sep = PerfilColumnasNomina.EsSeparacion(tipo);
                bool sepNueva = sep && formato >= 2;

                foreach (var item in datos)
                {
                    decimal gNor, gDbl, gTri, gOF, otrasDisplay;

                    if (sepNueva)
                    {
                        gNor = tipo == TipoNominaCaptura.Finiquito ? item.SueldoVacaciones : item.Compensaciones;
                        gDbl = item.Aguinaldo;
                        gTri = item.PrimaAntiguedad;
                        gOF = tipo == TipoNominaCaptura.Finiquito ? item.Bono : item.Indemnizacion;
                        otrasDisplay = item.Otras;
                    }
                    else if (sep)   // C# anterior (formato 1)
                    {
                        gNor = item.Compensaciones;
                        gDbl = item.PrimaAntiguedad;
                        gTri = item.Indemnizacion;
                        gOF = item.SueldoVacaciones;
                        otrasDisplay = item.Otras;
                    }
                    else
                    {
                        gNor = item.hsNorm; gDbl = item.hsDobles; gTri = item.hsTriples; gOF = item.OF;
                        otrasDisplay = tipo == TipoNominaCaptura.PTU ? item.PTU : tipo == TipoNominaCaptura.Aguinaldo ? item.Aguinaldo : item.Otras;
                    }

                    int rowIndex = dgvMostrarPersonal.Rows.Add();
                    var row = dgvMostrarPersonal.Rows[rowIndex];

                    row.Cells["NumeroEmpleado"].Value = item.idNomina;
                    row.Cells["NombreEmpleado"].Value = item.Nombre;
                    row.Cells["DiasTrabajados"].Value = item.DiasT;
                    row.Cells["Salario"].Value = item.Sueldo.ToString("N2");
                    row.Cells["hsNorm"].Value = gNor.ToString("F2");
                    row.Cells["hsDobles"].Value = gDbl.ToString("F2");
                    row.Cells["hsTriples"].Value = gTri.ToString("F2");
                    row.Cells["OF"].Value = gOF.ToString("F2");
                    row.Cells["PVacac"].Value = item.Pvacacional.ToString("F2");
                    row.Cells["Otras"].Value = otrasDisplay.ToString("N2");
                    row.Cells["PercExenta"].Value = item.PercExenta.ToString("F2");
                    row.Cells["TotIngr"].Value = item.TotalIngr.ToString("N2");
                    row.Cells["Ispt"].Value = item.ISTP.ToString("N2");
                    row.Cells["SubEmp"].Value = item.SubPEmpl.ToString("N2");
                    row.Cells["IMSS"].Value = item.IMSS.ToString("N2");
                    row.Cells["Prestamos"].Value = item.Prestamos.ToString("F2");
                    row.Cells["FONACOT"].Value = item.Fonacot.ToString("F2");
                    row.Cells["PensionAliment"].Value = item.PensionAlimenticia.ToString("F2");
                    row.Cells["INFONAVIT"].Value = item.Infonavit.ToString("F2");
                    row.Cells["TotDeduc"].Value = item.TotalDeduc.ToString("N2");
                    row.Cells["NETO"].Value = item.Neto.ToString("N2");
                    row.Cells["BANAMEX"].Value = item.Banamex;
                }

                dgvMostrarPersonal.ResumeLayout();

                ActualizarLblDatosPorNombre(Path.GetFileNameWithoutExtension(rutaArchivo));
                AjustarColumnasParaTipoNomina(Path.GetFileNameWithoutExtension(rutaArchivo), separacionLegacy: sep && !sepNueva, tipoForzado: tipo);
                AgregarFilaTotales();
                AplicarModoSoloLectura();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir JSON:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Exportación a Excel ───────────────────────────────────────────

        // Reemplaza ExportarDataGridViewAExcel (Office.Interop), con ClosedXML — no requiere Office instalado
        private void ExportarAExcel()
        {
            this.Enabled = false;
            try
            {
                using var sfd = new SaveFileDialog
                {
                    Title = "Exportar nómina a Excel",
                    Filter = "Excel (*.xlsx)|*.xlsx",
                    FileName = $"Nomina_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                    DefaultExt = "xlsx"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return;

                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("Exportación");

                // Encabezados
                int colIdx = 1;
                foreach (DataGridViewColumn col in dgvMostrarPersonal.Columns)
                {
                    if (!col.Visible) continue;
                    sheet.Cell(1, colIdx).Value = col.HeaderText;
                    sheet.Cell(1, colIdx).Style.Font.Bold = true;
                    sheet.Cell(1, colIdx).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    colIdx++;
                }

                // Filas
                int rowIdx = 2;
                foreach (DataGridViewRow row in dgvMostrarPersonal.Rows)
                {
                    if (row.IsNewRow || row.Tag?.ToString() == "Totales")
                        continue;

                    colIdx = 1;
                    foreach (DataGridViewColumn col in dgvMostrarPersonal.Columns)
                    {
                        if (!col.Visible) continue;
                        string valor = row.Cells[col.Index].Value?.ToString() ?? "";

                        // Intentar guardar como número para que Excel sume
                        if (decimal.TryParse(valor.Replace(",", "").Replace("$", ""), out decimal num))
                            sheet.Cell(rowIdx, colIdx).Value = num;
                        else
                            sheet.Cell(rowIdx, colIdx).Value = valor;

                        colIdx++;
                    }
                    rowIdx++;
                }

                sheet.Columns().AdjustToContents();
                workbook.SaveAs(sfd.FileName);

                // Abrir el archivo exportado
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName)
                { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al exportar a Excel:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        // ── Guardado de nómina ────────────────────────────────────────────

        private void GuardarNomina()
        {
            if (MessageBox.Show("La nómina será guardada, ¿Deseas Continuar?\n\nEl archivo contendrá los datos en esta tabla.", "AVISO", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            {
                MessageBox.Show("La nómina no ha sido guardada.\n\nNingún cambio fue aplicado.", "AVISO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int mesParaArchivo = ObtenerMesNomina();
            string mesAbrev = AbrevMes(mesParaArchivo);
            int quincenaNum = rdiobtnPrimera.Checked ? 1 : 2;
            string fileName;
            string tipoNomina;
            string motivoRegistro = null;
            string nombreCarpetaEmpresa = RegistroEmpresas.ObtenerNombreCarpeta(RutaTrabajo);
            if (string.IsNullOrWhiteSpace(nombreCarpetaEmpresa))
            {
                MessageBox.Show("Esta carpeta de trabajo aún no tiene una empresa asignada para el respaldo.Debe asignarse antes de poder guardar la nómina.", "Empresa no asignada",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (rdiobtnNormal.Checked)
            {
                fileName = $"{mesAbrev}{quincenaNum}{anio}.json";
                tipoNomina = "Normal";
            }
            else if (rdiobtnEspecial.Checked)
            {
                string motivo = cbMotivo.SelectedItem?.ToString() ?? "";

                if (string.IsNullOrWhiteSpace(motivo))
                {
                    MessageBox.Show("Debe seleccionar un motivo para la nómina especial.", "AVISO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (motivo.Equals("OTROS...", StringComparison.OrdinalIgnoreCase))
                {
                    string nombreOtro = txtOtroRazon.Text.Trim().ToUpper();
                    if (!Regex.IsMatch(nombreOtro, @"^[A-Z_]+$"))
                    {
                        MessageBox.Show("El nombre solo puede contener letras y guiones bajos.", "Nombre Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string conceptoOtro = nombreOtro.Replace("_", "");
                    if (conceptoOtro.Length < 3)
                    {
                        MessageBox.Show("El nombre debe tener al menos 3 letras.", "Nombre Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Premios/bonos y los demás conceptos con motivo propio no se capturan como "Otros".
                    if (PerfilColumnasNomina.Clasificar(conceptoOtro) != TipoNominaCaptura.Otra)
                    {
                        MessageBox.Show("Ese concepto tiene su propio motivo en la lista (los premios se capturan como BONO).\n\nSelecciónalo en lugar de usar \"Otros...\".",
                            "Usar el motivo correspondiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    fileName = PerfilColumnasNomina.SiguienteNombreEspecial(RutaTrabajo, conceptoOtro, mesParaArchivo, anio) + ".json";
                    motivoRegistro = nombreOtro;
                }
                else
                {
                    fileName = PerfilColumnasNomina.SiguienteNombreEspecial(RutaTrabajo, PerfilColumnasNomina.NombreArchivoMotivo(motivo), mesParaArchivo, anio) + ".json";
                    motivoRegistro = motivo;
                }

                tipoNomina = "Especial";
            }
            else
            {
                MessageBox.Show("Debe seleccionar el tipo de nómina (Normal o Especial).", "AVISO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string rutaFinal = Path.Combine(RutaTrabajo, fileName);

            if (File.Exists(rutaFinal))
            {
                var opcion = MessageBox.Show($"El archivo ya existe en:\n{rutaFinal}\n\n¿Qué deseas hacer?", "Archivo Existente", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button3);

                if (opcion == DialogResult.Cancel) return;

                if (opcion == DialogResult.No)
                {
                    using var sfd = new SaveFileDialog
                    {
                        Title = "Guardar Nómina en Nueva Ubicación",
                        Filter = "Archivos JSON (*.json)|*.json",
                        FileName = fileName,
                        InitialDirectory = RutaTrabajo,
                        OverwritePrompt = true,
                        AddExtension = true,
                        DefaultExt = "json"
                    };

                    if (sfd.ShowDialog() != DialogResult.OK)
                    {
                        MessageBox.Show("Guardado cancelado.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    rutaFinal = sfd.FileName;
                }

                if (File.Exists(rutaFinal))
                    File.SetAttributes(rutaFinal, FileAttributes.Normal);
            }

            if (_empleadosActivos == null || !_empleadosActivos.Any())
            {
                MessageBox.Show("No hay empleados activos para generar nómina.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ── Construir lista JSON ──────────────────────────────────────
            var listaJson = new List<NominaJSON>();
            var colsEspecial = new[] { "DiasTrabajados","Salario","hsNorm","hsDobles","hsTriples", "OF","PVacac","Otras","PercExenta","Ispt","SubEmp","IMSS", "Prestamos","FONACOT",
                "PensionAliment","INFONAVIT" };

            foreach (DataGridViewRow row in dgvMostrarPersonal.Rows)
            {
                if (row.IsNewRow || row.Tag?.ToString() == "Totales") continue;

                if (rdiobtnEspecial.Checked)
                {
                    bool tieneAlgo = colsEspecial.Any(col =>
                    {
                        var v = row.Cells[col].Value?.ToString() ?? "";
                        return decimal.TryParse(v.Replace(",", "").Replace("$", ""), out decimal d) && d != 0m;
                    });
                    if (!tieneAlgo) continue;
                }

                string motivo = cbMotivo.SelectedItem?.ToString() ?? "";
                var tipoCap = PerfilColumnasNomina.Clasificar(motivo);
                bool esPTU = tipoCap == TipoNominaCaptura.PTU;
                bool esAguinaldo = tipoCap == TipoNominaCaptura.Aguinaldo;
                bool esBono = tipoCap == TipoNominaCaptura.Bono;
                bool esFiniquito = tipoCap == TipoNominaCaptura.Finiquito;
                bool esLiquidacion = tipoCap == TipoNominaCaptura.Liquidacion;
                bool esSep = esFiniquito || esLiquidacion;

                listaJson.Add(new NominaJSON
                {
                    idNomina = row.Cells["NumeroEmpleado"].Value?.ToString().TrimStart('0') ?? "",
                    Nombre = row.Cells["NombreEmpleado"].Value?.ToString() ?? "",
                    DiasT = GetInt(row, "DiasTrabajados"),
                    Sueldo = GetDecimal(row, "Salario"),
                    hsNorm = esSep ? 0m : GetDecimal(row, "hsNorm"),
                    hsDobles = esSep ? 0m : GetDecimal(row, "hsDobles"),
                    hsTriples = esSep ? 0m : GetDecimal(row, "hsTriples"),
                    OF = esSep ? 0m : GetDecimal(row, "OF"),
                    Pvacacional = GetDecimal(row, "PVacac"),
                    Otras = GetDecimal(row, "Otras"),
                    PTU = esPTU ? GetDecimal(row, "Otras") : 0m,
                    Aguinaldo = esAguinaldo ? GetDecimal(row, "Otras") : esSep ? GetDecimal(row, "hsDobles") : 0m,
                    Bono = (esBono || esFiniquito) ? GetDecimal(row, "OF") : 0m,
                    Compensaciones = esLiquidacion ? GetDecimal(row, "hsNorm") : 0m,
                    PrimaAntiguedad = esSep ? GetDecimal(row, "hsTriples") : 0m,
                    Indemnizacion = esLiquidacion ? GetDecimal(row, "OF") : 0m,
                    SueldoVacaciones = esFiniquito ? GetDecimal(row, "hsNorm") : 0m,
                    PercExenta = GetDecimal(row, "PercExenta"),
                    TotalIngr = GetDecimal(row, "TotIngr"),
                    ISTP = GetDecimal(row, "Ispt"),
                    SubPEmpl = GetDecimal(row, "SubEmp"),
                    IMSS = GetDecimal(row, "IMSS"),
                    Prestamos = GetDecimal(row, "Prestamos"),
                    Fonacot = GetDecimal(row, "FONACOT"),
                    PensionAlimenticia = GetDecimal(row, "PensionAliment"),
                    Infonavit = GetDecimal(row, "INFONAVIT"),
                    TotalDeduc = GetDecimal(row, "TotDeduc"),
                    Neto = GetDecimal(row, "NETO"),
                    Banamex = row.Cells["BANAMEX"].Value?.ToString() ?? "",
                });
            }

            // ── Cifrar y guardar ──────────────────────────────────────────
            string jsonPlano = JsonConvert.SerializeObject(listaJson, Formatting.None);
            byte[] cipher = CryptoService.Encrypt(jsonPlano, out byte[] iv);

            byte[] ivPlusCipher = new byte[iv.Length + cipher.Length];
            iv.CopyTo(ivPlusCipher, 0);
            cipher.CopyTo(ivPlusCipher, iv.Length);

            byte[] hmacBytes = CryptoService.ComputeHmac(ivPlusCipher);

            var paquete = new
            {
                TipoNomina = tipoNomina,
                Data = Convert.ToBase64String(ivPlusCipher),
                Hmac = Convert.ToBase64String(hmacBytes),
                FechaGeneracion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UsuarioModificacion = Environment.UserName,
                Quincena = rdiobtnPrimera.Checked ? "Primera" : "Segunda",
                FormatoColumnas = 2,
                TipoCFDI = PerfilColumnasNomina.Clasificar(rdiobtnEspecial.Checked ? (cbMotivo.SelectedItem?.ToString() ?? "") : "").ToString(),
                Motivo = motivoRegistro ?? "",
                Mes = mesParaArchivo,
                Anio = anio
            };

            try
            {
                File.WriteAllText(rutaFinal, JsonConvert.SerializeObject(paquete, Formatting.Indented), Encoding.UTF8);
                File.SetAttributes(rutaFinal, FileAttributes.ReadOnly);
                BackupService.RespaldarNomina(rutaFinal, nombreCarpetaEmpresa, anio);

                // ── Bitácora ─────────────────────────────────────────
                decimal totalPagado = listaJson.Sum(x => x.Neto);
                BackupService.RegistrarNomina(tipoNomina, fileName, totalPagado, nombreCarpetaEmpresa, anio, motivoRegistro);

                RegistrarPagosEspecialesSiAplica(nombreCarpetaEmpresa, anio);

                MessageBox.Show($"Se guardó la nómina '{fileName}'.", "Nómina Guardada", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DeshabilitarControlesPostGuardado();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el archivo:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Métodos de cálculo ────────────────────────────────────────────

        private void InicializarServicioCalculo()
        {
            try
            {
                if (_datosEmpresa == null)
                {
                    MessageBox.Show("No se pudieron cargar los datos de la empresa desde RUEP.dat", "Error de Inicialización", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var tablasISR = TarifasRepository.Obtener();

                if (tablasISR?.tablas?.ISR_113 == null ||
                    !tablasISR.tablas.ISR_113.Any())
                {
                    MessageBox.Show("ADVERTENCIA: No se encontraron tablas de ISR.\n\nLos cálculos de impuestos NO funcionarán correctamente.\n\nConfigure las tablas fiscales desde el menú.",
                        "Tablas Fiscales Faltantes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _servicioCalculo = new ServicioCalculoNomina(_datosEmpresa.uma_por_dia, _datosEmpresa.salario_minimo, _datosEmpresa.ano_fiscal, tablasISR);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al inicializar servicio de cálculo:\n\n{ex.Message}", "Error de Inicialización", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AplicarCalculosIniciales()
        {
            if (_servicioCalculo == null) return;

            try
            {
                dgvMostrarPersonal.SuspendLayout();
                for (int i = 0; i < dgvMostrarPersonal.Rows.Count; i++)
                    if (!dgvMostrarPersonal.Rows[i].IsNewRow)
                        CalcularNominaParaFila(i);
                dgvMostrarPersonal.ResumeLayout();
                AgregarFilaTotales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar cálculos iniciales:\n\n{ex.Message}", "Error de Cálculo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CalcularNominaParaFila(int rowIndex)
        {
            if (_servicioCalculo == null) return;

            try
            {
                _recalculandoFila = true;
                var row = dgvMostrarPersonal.Rows[rowIndex];

                if (!int.TryParse(
                    row.Cells["NumeroEmpleado"].Value?.ToString(), out int idEmpleado))
                    return;

                var empleado = _empleadosActivos?.FirstOrDefault(e => e.Id == idEmpleado);
                if (empleado == null) return;

                int diasTrabajados = ObtenerEntero(row, "DiasTrabajados");
                decimal otrasCalculadas = Math.Round(empleado.Otras * diasTrabajados, 0, MidpointRounding.AwayFromZero);

                var datosCapturados = new DatosCapturados
                {
                    EsSegundaQuincena = rdiobtnSegunda.Checked,
                    EsNominaEspecial = rdiobtnEspecial.Checked,
                    MotivoEspecial = rdiobtnEspecial.Checked ? (cbMotivo.SelectedItem?.ToString() ?? "") : "",
                    DiasTrabajados = diasTrabajados,
                    SalarioDiario = empleado.SalarioDiario,
                    SalarioCapturadoEspecial = GetDecimal(row, "Salario"),
                    HorasNormales = GetDecimal(row, "hsNorm"),
                    HorasDobles = GetDecimal(row, "hsDobles"),
                    HorasTriples = GetDecimal(row, "hsTriples"),
                    OF = GetDecimal(row, "OF"),
                    PrimaVacacional = GetDecimal(row, "PVacac"),
                    OtrasPercepciones = otrasCalculadas,
                    PercepcionExenta = GetDecimal(row, "PercExenta"),
                    // En el formato nuevo, el concepto contiene el bruto completo y la exenta es un desglose.
                    ExentoIncluidoEnConceptos = true,
                    Prestamos = GetDecimal(row, "Prestamos"),
                    FONACOT = GetDecimal(row, "FONACOT"),
                    PensionAlimenticia = GetDecimal(row, "PensionAliment"),
                    INFONAVIT = GetDecimal(row, "INFONAVIT")
                };

                DateTime fechaCorteNomina = ObtenerFechaCorteNomina();

                var resultado = _servicioCalculo.CalcularNomina(empleado, datosCapturados, fechaCorteNomina);
                _subsidioCausadoPorEmpleado[idEmpleado] = resultado.SubsidioCausado;

                row.Cells["Salario"].Value = resultado.SalarioPeriodo.ToString("N2");
                row.Cells["TotIngr"].Value = resultado.TotalIngresos.ToString("N2");
                row.Cells["Otras"].Value = otrasCalculadas.ToString("N0");
                row.Cells["Ispt"].Value = resultado.ISR.ToString("N2");
                row.Cells["SubEmp"].Value = resultado.Subsidio.ToString("N2");
                row.Cells["IMSS"].Value = resultado.IMSS.ToString("N2");
                row.Cells["TotDeduc"].Value = resultado.TotalDeducciones.ToString("N2");
                row.Cells["NETO"].Value = resultado.Neto.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular nómina (fila {rowIndex + 1}):\n\n{ex.Message}", "Error de Cálculo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _recalculandoFila = false;
            }
        }

        private void RecalcularFilaCompleta(int rowIndex, string columnaOrigen = null)
        {
            if (_servicioCalculo == null) return;

            try
            {
                _recalculandoFila = true;
                var row = dgvMostrarPersonal.Rows[rowIndex];

                if (!int.TryParse(row.Cells["NumeroEmpleado"].Value?.ToString(), out int idEmpleado))
                    return;

                var empleado = _empleadosActivos?.FirstOrDefault(e => e.Id == idEmpleado);

                if (empleado == null)
                {
                    empleado = _todosEmpleadosRUEP?.FirstOrDefault(e => e.Id == idEmpleado);
                }

                if (empleado == null)
                    return;

                int diasTrabajados = ObtenerEntero(row, "DiasTrabajados");
                decimal otrasCalculadas;

                if (columnaOrigen == "Otras")
                {
                    // Override indexado por idEmpleado — seguro al reordenar grid
                    _otrasManualOverride.Add(idEmpleado);
                    otrasCalculadas = GetDecimal(row, "Otras");
                }
                else if (columnaOrigen == "DiasTrabajados" && _otrasManualOverride.Contains(idEmpleado))
                {
                    decimal otrasActuales = GetDecimal(row, "Otras");
                    decimal otrasSegunTarifa = empleado.Otras * diasTrabajados;

                    _recalculandoFila = false;
                    var respuesta = MessageBox.Show($"'Otras' tiene un valor manual (${otrasActuales:N2}).\n\n" + $"¿Recalcular proporcionalmente?\n" + $"   Sí  →  ${otrasSegunTarifa:N2}\n" +
                        $"   No  →  Se mantiene ${otrasActuales:N2}", "Recalcular 'Otras'", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                    _recalculandoFila = true;

                    if (respuesta == DialogResult.Yes)
                    {
                        _otrasManualOverride.Remove(idEmpleado);
                        otrasCalculadas = Math.Round(empleado.Otras * diasTrabajados, 0, MidpointRounding.AwayFromZero);
                        row.Cells["Otras"].Value = otrasCalculadas.ToString("N0");
                    }
                    else
                    {
                        otrasCalculadas = otrasActuales;
                    }
                }
                else if (_otrasManualOverride.Contains(idEmpleado))
                {
                    otrasCalculadas = GetDecimal(row, "Otras");
                }
                else
                {
                    otrasCalculadas = Math.Round(empleado.Otras * diasTrabajados, 0, MidpointRounding.AwayFromZero);
                    row.Cells["Otras"].Value = otrasCalculadas.ToString("N0");
                }

                var datosCapturados = new DatosCapturados
                {
                    EsSegundaQuincena = rdiobtnSegunda.Checked,
                    EsNominaEspecial = rdiobtnEspecial.Checked,
                    MotivoEspecial = rdiobtnEspecial.Checked ? (cbMotivo.SelectedItem?.ToString() ?? "") : "",
                    DiasTrabajados = diasTrabajados,
                    SalarioDiario = empleado.SalarioDiario,
                    SalarioCapturadoEspecial = GetDecimal(row, "Salario"),
                    HorasNormales = GetDecimal(row, "hsNorm"),
                    HorasDobles = GetDecimal(row, "hsDobles"),
                    HorasTriples = GetDecimal(row, "hsTriples"),
                    OF = GetDecimal(row, "OF"),
                    PrimaVacacional = GetDecimal(row, "PVacac"),
                    OtrasPercepciones = otrasCalculadas,
                    PercepcionExenta = GetDecimal(row, "PercExenta"),
                    ExentoIncluidoEnConceptos = true,
                    Prestamos = GetDecimal(row, "Prestamos"),
                    FONACOT = GetDecimal(row, "FONACOT"),
                    PensionAlimenticia = GetDecimal(row, "PensionAliment"),
                    INFONAVIT = GetDecimal(row, "INFONAVIT")
                };

                DateTime fechaCorteNomina = ObtenerFechaCorteNomina();

                var resultado = _servicioCalculo.CalcularNomina(empleado, datosCapturados, fechaCorteNomina);
                _subsidioCausadoPorEmpleado[idEmpleado] = resultado.SubsidioCausado;

                // ================================================
                // IMPORTES DEL PERIODO
                // ================================================

                row.Cells["Salario"].Value =
                    resultado.SalarioPeriodo.ToString("N2");

                row.Cells["TotIngr"].Value =
                    resultado.TotalIngresos.ToString("N2");

                // ================================================
                // RESPETAR RETENCIONES MANUALES
                // ================================================

                bool isrManual = _fiscalManualOverride.Contains(
                    $"{idEmpleado}:Ispt");

                bool subsidioManual = _fiscalManualOverride.Contains(
                    $"{idEmpleado}:SubEmp");

                bool imssManual = _fiscalManualOverride.Contains(
                    $"{idEmpleado}:IMSS");

                // Solamente actualizar el campo fiscal si
                // NO fue fijado manualmente para este empleado.

                if (!isrManual)
                {
                    row.Cells["Ispt"].Value =
                        resultado.ISR.ToString("N2");
                }

                if (!subsidioManual)
                {
                    row.Cells["SubEmp"].Value =
                        resultado.Subsidio.ToString("N2");
                }

                if (!imssManual)
                {
                    row.Cells["IMSS"].Value =
                        resultado.IMSS.ToString("N2");
                }

                // ================================================
                // TOTAL DE DEDUCCIONES Y NETO
                // ================================================

                // Cuando existe alguna retención manual, los
                // totales deben utilizar lo que realmente quedó
                // en las celdas, no los impuestos automáticos.

                if (isrManual || subsidioManual || imssManual)
                {
                    decimal ingresosFinales = GetDecimal(row, "TotIngr");

                    decimal deduccionesFinales = Math.Round(
                        GetDecimal(row, "Ispt") +
                        GetDecimal(row, "SubEmp") +
                        GetDecimal(row, "IMSS") +
                        GetDecimal(row, "Prestamos") +
                        GetDecimal(row, "FONACOT") +
                        GetDecimal(row, "PensionAliment") +
                        GetDecimal(row, "INFONAVIT"),
                        2);

                    decimal netoFinal = Math.Round(ingresosFinales - deduccionesFinales, 2);

                    row.Cells["TotDeduc"].Value = deduccionesFinales.ToString("N2");
                    row.Cells["NETO"].Value = netoFinal.ToString("N2");
                }
                else
                {
                    row.Cells["TotDeduc"].Value = resultado.TotalDeducciones.ToString("N2");
                    row.Cells["NETO"].Value = resultado.Neto.ToString("N2");
                }

                dgvMostrarPersonal.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al recalcular fila {rowIndex + 1}:\n\n{ex.Message}", "Error de Cálculo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _recalculandoFila = false;
                AgregarFilaTotales();
            }
        }

        private void RecalcularTotalesConFiscalFijo(int rowIndex)
        {
            try
            {
                _recalculandoFila = true;
                var row = dgvMostrarPersonal.Rows[rowIndex];

                decimal totIngr = GetDecimal(row, "TotIngr");
                decimal ispt = GetDecimal(row, "Ispt");
                decimal subEmp = GetDecimal(row, "SubEmp");
                decimal imss = GetDecimal(row, "IMSS");
                decimal prestamos = GetDecimal(row, "Prestamos");
                decimal fonacot = GetDecimal(row, "FONACOT");
                decimal pension = GetDecimal(row, "PensionAliment");
                decimal infonavit = GetDecimal(row, "INFONAVIT");

                decimal totDeduc = Math.Round(ispt + subEmp + imss + prestamos + fonacot + pension + infonavit, 2);
                decimal NETO = Math.Round(totIngr - totDeduc, 2);

                row.Cells["TotDeduc"].Value = totDeduc.ToString("N2");
                row.Cells["NETO"].Value = NETO.ToString("N2");

                dgvMostrarPersonal.Refresh();
            }
            finally
            {
                _recalculandoFila = false;
                AgregarFilaTotales();
            }
        }

        // ── Carga de empleados en grid ────────────────────────────────────
        private void CargarEmpleadosEnGrid()
        {
            // Una nueva captura comienza sin excepciones individuales de la captura anterior.
            _exentaManualPorEmpleado.Clear();
            dgvMostrarPersonal.Rows.Clear();

            bool esEspecial = rdiobtnEspecial.Checked;

            var fuente = esEspecial ? _todosEmpleadosRUEP?.Where(emp => string.IsNullOrWhiteSpace(emp.FechaBaja) || ObtenerAnioFecha(emp.FechaBaja) >= _datosEmpresa.ano_fiscal)
                .ToList() : _empleadosActivos;

            if (fuente == null || !fuente.Any()) return;

            ObtenerRangoQuincena(out DateTime inicioQ, out DateTime finQ);

            if (!esEspecial)
            {
                var sinSalario = fuente.Where(emp => emp.SalarioDiario == 0m && string.IsNullOrWhiteSpace(emp.FechaBaja)).ToList();

                if (sinSalario.Count > 0)
                    MessageBox.Show($"Los siguientes empleados no tienen salario y " + $"no serán incluidos:\n\n• " + string.Join("\n• ", sinSalario.Select(e => $"{e.ApellidoP} {e.ApellidoM} {e.Nombre} (ID: {e.Id})")),
                        "Empleados sin salario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            foreach (var emp in fuente)
            {
                if (!esEspecial && emp.SalarioDiario == 0m) continue;

                int diasTrabajados;

                if (esEspecial)
                {
                    diasTrabajados = 0;
                }
                else
                {
                    if (!DateTime.TryParseExact(emp.FechaAlta, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaAlta))
                        continue;

                    if (fechaAlta > finQ)
                        continue;

                    if (!string.IsNullOrWhiteSpace(emp.FechaBaja) && DateTime.TryParseExact(emp.FechaBaja, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None,
                        out DateTime fechaBaja) && fechaBaja < inicioQ)
                        continue;

                    DateTime inicioReal = fechaAlta > inicioQ ? fechaAlta : inicioQ;
                    diasTrabajados = inicioReal == inicioQ ? 15 : (finQ - inicioReal).Days + 1;
                }

                int rowIndex = dgvMostrarPersonal.Rows.Add();
                var row = dgvMostrarPersonal.Rows[rowIndex];

                row.Cells["NumeroEmpleado"].Value = emp.Id;
                row.Cells["Obra"].Value = NormalizarTextoObra(ObtenerValorPropiedadObra(emp));
                row.Cells["NombreEmpleado"].Value = $"{emp.ApellidoP} {emp.ApellidoM} {emp.Nombre}";
                row.Cells["FechaAl"].Value = emp.FechaAlta;
                row.Cells["DiasTrabajados"].Value = diasTrabajados;
                row.Cells["Salario"].Value = (emp.SalarioDiario * diasTrabajados).ToString("N2");
                row.Cells["hsNorm"].Value = "0.00";
                row.Cells["hsDobles"].Value = "0.00";
                row.Cells["hsTriples"].Value = "0.00";
                row.Cells["OF"].Value = "0.00";
                row.Cells["PVacac"].Value = "0.00";

                decimal otrasIni = esEspecial ? 0m : Math.Round(emp.Otras * diasTrabajados, 0, MidpointRounding.AwayFromZero);
                row.Cells["Otras"].Value = otrasIni.ToString("N0");
                row.Cells["PercExenta"].Value = "0.00";
                row.Cells["Ispt"].Value = "0.00";
                row.Cells["SubEmp"].Value = "0.00";
                row.Cells["IMSS"].Value = "0.00";
                row.Cells["Prestamos"].Value = "0.00";
                row.Cells["FONACOT"].Value = "0.00";
                row.Cells["PensionAliment"].Value = "0.00";
                row.Cells["INFONAVIT"].Value = "0.00";
                row.Cells["TotIngr"].Value = (emp.SalarioDiario * diasTrabajados).ToString("N2");
                row.Cells["TotDeduc"].Value = "0.00";
                row.Cells["NETO"].Value = (emp.SalarioDiario * diasTrabajados).ToString("N2");
                row.Cells["BANAMEX"].Value = emp.Banamex;
            }

            _otrasManualOverride.Clear();
            _fiscalManualOverride.Clear();
        }

        // RecargarDatosRUEP sin filtro de Banamex incorrecto
        private void RecargarDatosRUEP()
        {
            try
            {
                _personalService = new RuepService(RutaRUEP);
                _datosEmpresa = _personalService.DatosEmpresa;

                _todosEmpleadosRUEP = _personalService.ObtenerTodos().Where(emp => !string.IsNullOrWhiteSpace(emp.Nombre) && !string.IsNullOrWhiteSpace(emp.ApellidoP)).OrderBy(emp => emp.ApellidoP)
                    .ThenBy(emp => emp.ApellidoM).ThenBy(emp => emp.Nombre).ToList();

                _empleadosActivos = _personalService.ObtenerTodos().Where(emp => !string.IsNullOrWhiteSpace(emp.Nombre) && !string.IsNullOrWhiteSpace(emp.ApellidoP) &&
                EsVigenteEnPeriodo(emp.FechaBaja, AnioEmpresa)).OrderBy(emp => emp.ApellidoP).ThenBy(emp => emp.ApellidoM).ThenBy(emp => emp.Nombre).ToList();

                InicializarServicioCalculo();
                CargarEmpleadosEnGrid();
                AplicarCalculosIniciales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al recargar RUEP.dat:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Construcción de CFDI ──────────────────────────────────────────
        private List<DatosCFDIEmpleado> ConstruirDatosCFDI()
        {
            var lista = new List<DatosCFDIEmpleado>();

            bool esEspecial = rdiobtnEspecial.Checked;
            string motivoEspecial = esEspecial ? ObtenerMotivoNominaEspecial() : "";

            bool esAguinaldo = esEspecial && !string.IsNullOrWhiteSpace(motivoEspecial) && motivoEspecial.StartsWith("AGUIN", StringComparison.OrdinalIgnoreCase);
            bool esPTU = esEspecial && !string.IsNullOrWhiteSpace(motivoEspecial) && motivoEspecial.StartsWith("PTU", StringComparison.OrdinalIgnoreCase);

            foreach (DataGridViewRow row in dgvMostrarPersonal.Rows)
            {
                if (row.IsNewRow || row.Tag?.ToString() == "Totales")
                    continue;

                // En especial no generar CFDI para filas completamente vacías.
                if (esEspecial && !TieneMovimientoNominaEspecial(row))
                    continue;

                if (!int.TryParse(row.Cells["NumeroEmpleado"].Value?.ToString(), out int idEmp))
                {
                    continue;
                }

                // IMPORTANTE:
                // nómina especial puede contener empleados que no están
                // en _empleadosActivos.
                var emp = _empleadosActivos?.FirstOrDefault(x => x.Id == idEmp);

                if (emp == null)
                {
                    emp = _todosEmpleadosRUEP?.FirstOrDefault(x => x.Id == idEmp);
                }

                if (emp == null)
                    continue;

                // -------------------------------------------------------------
                // Valores ORIGINALES de la grid antes de aplicar reng()
                // -------------------------------------------------------------
                decimal salarioOriginal = GetDecimal(row, "Salario");
                decimal ofOriginal = GetDecimal(row, "OF");
                decimal primaVacacionalOriginal = GetDecimal(row, "PVacac");
                decimal otrasOriginal = GetDecimal(row, "Otras");
                decimal exentoOriginal = GetDecimal(row, "PercExenta");

                decimal isrOriginal = GetDecimal(row, "Ispt");

                // En tu C# SubEmp se maneja como importe que reduce las
                // deducciones, por eso normalmente viene negativo.
                decimal subsidioGrid = GetDecimal(row, "SubEmp");

                decimal imssOriginal = GetDecimal(row, "IMSS");
                decimal prestamosOriginal = GetDecimal(row, "Prestamos");
                decimal fonacotOriginal = GetDecimal(row, "FONACOT");
                decimal pensionOriginal = GetDecimal(row, "PensionAliment");
                decimal infonavitOriginal = GetDecimal(row, "INFONAVIT");

                // -------------------------------------------------------------
                // Reconstrucción de las posiciones que VB6 habría tenido
                // en ConNom1.
                //
                // En tu UI especial Aguinaldo/PTU se capturan en "Otras".
                // En VB6:
                //
                // columna 5 = Aguinaldo
                // columna 6 = PTU
                // columna 9 = Otras
                // columna 10 = Exento
                // -------------------------------------------------------------
                decimal otrasParaReng = otrasOriginal;
                decimal aguinaldoParaReng = 0m;
                decimal ptuParaReng = 0m;

                bool tieneAguinaldoOriginal = false;
                bool tienePTUOriginal = false;

                if (esAguinaldo)
                {
                    aguinaldoParaReng = otrasOriginal;
                    otrasParaReng = 0m;

                    tieneAguinaldoOriginal = otrasOriginal != 0m;
                }
                else if (esPTU)
                {
                    ptuParaReng = otrasOriginal;
                    otrasParaReng = 0m;

                    tienePTUOriginal = otrasOriginal != 0m || exentoOriginal != 0m;
                }

                // VB6 carganom() deja ISR vacío cuando es 0.
                // Nuestra grid muestra "0.00", así que para reproducir
                // el efecto de IsNumeric del VB6 usamos valor distinto de cero.
                bool tieneISROriginal = isrOriginal != 0m;
                bool tieneExentoOriginal = exentoOriginal != 0m;

                decimal subsidioCausado = 0m;

                if (_subsidioCausadoPorEmpleado.TryGetValue(idEmp, out decimal subCausado))
                {
                    subsidioCausado = subCausado;
                }

                var dto = new DatosCFDIEmpleado
                {
                    NumeroEmpleado = idEmp,
                    Nombre = $"{emp.Nombre} {emp.ApellidoP} {emp.ApellidoM}".Trim(),

                    DiasTrabajados = GetInt(row, "DiasTrabajados"),

                    // ---------------------------------------------------------
                    // Valores que equivalen a las columnas que recibe reng()
                    // ---------------------------------------------------------
                    Salario = salarioOriginal,                 // sue3
                    OF = ofOriginal,                           // via7
                    PrimaVacacional = primaVacacionalOriginal,// pva8
                    Otras = otrasParaReng,                     // otr9

                    Compensaciones = GetDecimal(row, "hsNorm"),
                    PrimaAntiguedad = GetDecimal(row, "hsDobles"),
                    Indemnizacion = GetDecimal(row, "hsTriples"),
                    SueldoVacaciones = GetDecimal(row, "OF"),

                    // Valor original de columna 10
                    PercExenta = exentoOriginal,
                    ExentoOriginal = exentoOriginal,

                    Aguinaldo = aguinaldoParaReng,

                    // PTU1 ENTRANDO a reng() equivale al valor de columna 6.
                    PTU1 = ptuParaReng,

                    // Si la captura ya separó gravado/exento:
                    PTU2 = exentoOriginal,

                    // ---------------------------------------------------------
                    // Fiscal ORIGINAL
                    // ---------------------------------------------------------
                    ISR = isrOriginal,

                    // VB6:
                    // sub13 = ConNom1(I7,13) * -1
                    //
                    // En FormCaptura SubEmp puede venir negativo.
                    Subsidio = subsidioGrid * -1m,

                    SubsidioCausado = subsidioCausado,

                    IMSS = imssOriginal,
                    Prestamos = prestamosOriginal,
                    FONACOT = fonacotOriginal,
                    PensionAlimenticia = pensionOriginal,
                    INFONAVIT = infonavitOriginal,

                    // ---------------------------------------------------------
                    // Flags equivalentes a IsNumeric/celda con concepto VB6
                    // ---------------------------------------------------------
                    TieneISROriginal = tieneISROriginal,
                    TieneAguinaldoOriginal = tieneAguinaldoOriginal,
                    TienePTUOriginal = tienePTUOriginal,
                    TieneExentoOriginal = tieneExentoOriginal,

                    // ---------------------------------------------------------
                    // Empleado / RUEP
                    // ---------------------------------------------------------
                    BANAMEX = row.Cells["BANAMEX"].Value?.ToString() ?? "",

                    RFC = emp.RFC ?? "",
                    CURP = emp.CURP ?? "",
                    NSS = emp.NSS ?? "",
                    FechaAlta = emp.FechaAlta ?? "",

                    SalarioDiario = emp.SalarioDiario,

                    // SDI directamente del RUEP.
                    // NO Archivo.cg.
                    SalarioDiarioInteg = emp.SalarioIntegrado,

                    RiesgoIMSS = emp.RiesgoIMSS ?? "1",

                    RFCLabora = "",

                    RegistroPatronal = _datosEmpresa?.datos_fiscales?.registro_patronal ?? "",

                    MetodoPago = "PUE",
                    FormaPagoSAT = emp.FormaPagoSAT ?? "28",

                    TipoRegimen = emp.TipoRegimen ?? "02 Sueldos",

                    TipoContrato = emp.TipoContrato ?? "01 Contrato de trabajo por tiempo indeterminado",

                    TipoJornada = emp.TipoJornada ?? "01 Diurna",

                    PeriodicidadPago = esEspecial ? "99 OTRA PERIODICIDAD" : "04 QUINCENAL",

                    Departamento = emp.Departamento ?? "ADMINISTRACION",

                    Puesto = emp.Puesto ?? "Administracion",

                    Direccion = emp.Direccion ?? "",
                    Colonia = emp.Colonia ?? "",

                    Ciudad = !string.IsNullOrWhiteSpace(emp.Ciudad) ? emp.Ciudad : emp.Municipio ?? "",

                    Estado = emp.Estado ?? "",
                    Delegacion = emp.Municipio ?? "",
                    CodigoPostal = emp.CP ?? "",

                    Correo = emp.CorreoEmpresaPredeterminado ? emp.EmailEmpresa ?? "" : emp.Email ?? ""

                };

                // -------------------------------------------------------------
                // AQUÍ está el equivalente directo a VB6:
                //
                // reng
                // MdAbr_1
                //
                // Primero recalculamos el DTO como lo hacía reng().
                // -------------------------------------------------------------
                CalculoRengVB6.Calcular(dto, esEspecial, _datosEmpresa?.salario_minimo ?? SalarioMinimo);
                lista.Add(dto);
            }

            return lista;
        }

        // ── Helpers del grid ──────────────────────────────────────────────

        private void ConfigurarComportamientoGrid()
        {
            var columnasReadOnly = new[] { "NumeroEmpleado", "NombreEmpleado", "FechaAl", "Salario", "TotIngr", "TotDeduc", "NETO", "BANAMEX" };

            foreach (var colName in columnasReadOnly)
            {
                var col = dgvMostrarPersonal.Columns[colName];
                if (col == null) continue;
                col.ReadOnly = true;
                col.DefaultCellStyle.Font = new Font(dgvMostrarPersonal.Font, FontStyle.Bold);
            }

            foreach (DataGridViewColumn col in dgvMostrarPersonal.Columns)
                col.SortMode = DataGridViewColumnSortMode.Programmatic;

            dgvMostrarPersonal.Columns["NumeroEmpleado"].ReadOnly = true;
            dgvMostrarPersonal.Columns["NombreEmpleado"].ReadOnly = true;

            foreach (var colName in new[] { "NumeroEmpleado", "NombreEmpleado" })
            {
                dgvMostrarPersonal.Columns[colName].DefaultCellStyle.BackColor = Color.LightGray;
                dgvMostrarPersonal.Columns[colName].DefaultCellStyle.SelectionBackColor = Color.LightGray;
            }
        }

        private void CargarOpcionesMostrarColumnas()
        {
            var columnasDGV = new Dictionary<string, string>
            {
                { "NumeroEmpleado",  "Número de Empleado" },
                { "NombreEmpleado",  "Nombre del Empleado" },
                { "FechaAl",         "Fecha Alta" },
                { "DiasTrabajados",  "Días Trabajados" },
                { "Salario",         "Salario" },
                { "hsNorm",          "Horas Normales" },
                { "hsDobles",        "Horas Dobles" },
                { "hsTriples",       "Horas Triples" },
                { "OF",              "OF" },
                { "PVacac",          "Prima Vacacional" },
                { "Otras",           "Otras Percepciones" },
                { "PercExenta",      "Percepción Exenta" },
                { "TotIngr",         "Total Ingresos" },
                { "Ispt",            "ISPT" },
                { "SubEmp",          "Subsidio Empleado" },
                { "IMSS",            "IMSS" },
                { "Prestamos",       "Préstamos" },
                { "FONACOT",         "FONACOT" },
                { "PensionAliment",  "Pensión Alimenticia" },
                { "INFONAVIT",       "INFONAVIT" },
                { "TotDeduc",        "Total Deducciones" },
                { "NETO",            "NETO a Pagar" },
                { "BANAMEX",         "Cuenta BANAMEX" }
            };

            tsMostrarColumna.DropDownItems.Clear();

            foreach (var kvp in columnasDGV)
            {
                bool porDefecto = kvp.Key != "FechaAl";
                var item = new ToolStripMenuItem(kvp.Value)
                {
                    CheckOnClick = true,
                    Checked = porDefecto,
                    Tag = kvp.Key
                };
                item.Click += tsMostrarColumna_Click;
                tsMostrarColumna.DropDownItems.Add(item);
            }
        }

        private void AgregarFilaTotales()
        {
            // Quitar fila anterior
            for (int i = dgvMostrarPersonal.Rows.Count - 1; i >= 0; i--)
            {
                if (dgvMostrarPersonal.Rows[i].Tag?.ToString() == "Totales")
                {
                    dgvMostrarPersonal.Rows.RemoveAt(i);
                    break;
                }
            }

            var columnasNumericas = new[] { "Salario","hsNorm","hsDobles","hsTriples","OF","PVacac", "Otras","PercExenta","TotIngr","Ispt","SubEmp","IMSS", "Prestamos","FONACOT",
                "PensionAliment","INFONAVIT", "TotDeduc","NETO" };

            var sumas = columnasNumericas.ToDictionary(c => c, c => 0m);
            int totalEmpleados = 0;

            foreach (DataGridViewRow row in dgvMostrarPersonal.Rows)
            {
                if (row.IsNewRow || row.Tag?.ToString() == "Totales") continue;
                totalEmpleados++;

                foreach (var col in columnasNumericas)
                {
                    if (!dgvMostrarPersonal.Columns.Contains(col)) continue;
                    string raw = (row.Cells[col].Value?.ToString() ?? "").Replace("$", "").Replace(",", "").Trim();
                    if (decimal.TryParse(raw, out decimal v))
                        sumas[col] += v;
                }
            }

            int rowIndex = dgvMostrarPersonal.Rows.Add();
            var totRow = dgvMostrarPersonal.Rows[rowIndex];
            totRow.Tag = "Totales";

            var verde = Color.FromArgb(198, 239, 206);
            var negrita = new Font(dgvMostrarPersonal.Font, FontStyle.Bold);
            foreach (DataGridViewCell cell in totRow.Cells)
            {
                cell.Style.BackColor = verde;
                cell.Style.Font = negrita;
            }

            if (dgvMostrarPersonal.Columns.Contains("NombreEmpleado"))
                totRow.Cells["NombreEmpleado"].Value = $"Total: {totalEmpleados} Empleados";

            foreach (var col in columnasNumericas)
            {
                if (!dgvMostrarPersonal.Columns.Contains(col)) continue;
                decimal s = sumas[col];
                totRow.Cells[col].Value = s == 0m ? "" : s.ToString("N2");
            }

            foreach (var col in new[] { "NumeroEmpleado", "FechaAl", "BANAMEX", "DiasTrabajados" })
                if (dgvMostrarPersonal.Columns.Contains(col))
                    totRow.Cells[col].Value = "";
        }

        private void OrdenarGrid(bool asc, bool porNombre)
        {
            var filas = dgvMostrarPersonal.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow && r.Tag?.ToString() != "Totales").ToList();

            if (filas.Count == 0) return;

            List<DataGridViewRow> ordenadas = porNombre ? (asc ? filas.OrderBy(r => r.Cells["NombreEmpleado"].Value?.ToString() ?? "").ToList() : filas.OrderByDescending(r => r.Cells["NombreEmpleado"]
            .Value?.ToString() ?? "").ToList()) : (asc ? filas.OrderBy(ObtenerIdOrden).ToList() : filas.OrderByDescending(ObtenerIdOrden).ToList());

            var snapshot = ordenadas.Select(r => r.Cells.Cast<DataGridViewCell>().Select(c => c.Value).ToArray()).ToList();

            dgvMostrarPersonal.SuspendLayout();
            dgvMostrarPersonal.Rows.Clear();
            foreach (var vals in snapshot)
                dgvMostrarPersonal.Rows.Add(vals);
            dgvMostrarPersonal.ResumeLayout();

            AgregarFilaTotales();
        }

        private int ObtenerIdOrden(DataGridViewRow r)
        {
            int.TryParse(r.Cells["NumeroEmpleado"].Value?.ToString(), out int n);
            return n;
        }

        // ── Columnas por tipo de nómina ───────────────────────────────────
        private void AjustarColumnasParaTipoNomina(string nombreSinExtension, bool separacionLegacy = false, TipoNominaCaptura? tipoForzado = null)
        {
            SetColHeader("hsNorm", "Hs. Norm.");
            SetColHeader("hsDobles", "Hs. Dobles");
            SetColHeader("hsTriples", "Hs. Triples");
            SetColHeader("OF", "OF");
            SetColHeader("PVacac", "P. Vacac.");
            SetColHeader("Otras", "Otras");
            SetColHeader("PercExenta", "Perc. Exenta");
            SetColHeader("Ispt", "ISPT");
            SetColHeader("SubEmp", "Subsidio");

            string nombre = (nombreSinExtension ?? "").Trim();
            TipoNominaCaptura tipo = tipoForzado ?? PerfilColumnasNomina.Clasificar(nombre);

            switch (tipo)
            {
                case TipoNominaCaptura.Ordinaria:
                    break;

                case TipoNominaCaptura.Liquidacion:
                case TipoNominaCaptura.Finiquito:
                    foreach (var kv in PerfilColumnasNomina.EncabezadosSeparacion(tipo, separacionLegacy))
                        SetColHeader(kv.Key, kv.Value);
                    AjustarAnchoColumnasEncabezados();
                    break;

                case TipoNominaCaptura.Bono:
                    SetColHeader("OF", "Bono");
                    break;

                case TipoNominaCaptura.PTU:
                    SetColHeader("Otras", "P.T.U.");
                    break;

                case TipoNominaCaptura.Aguinaldo:
                    SetColHeader("Otras", "Aguinaldo");
                    break;

                default: // Otra: Prima Vacacional, OTROS..., archivos "PRE*" antiguos
                    string n = PerfilColumnasNomina.Normalizar(nombre);
                    if (n.StartsWith("PRE"))
                    {
                        SetColHeader("hsTriples", "Viáticos");
                        SetColHeader("OF", "—");
                    }
                    else if (n.Contains("VACAC"))
                        SetColHeader("Otras", "P. Vacacional");
                    else if (n.Length > 0 && n != "OTROS")
                        SetColHeader("Otras", nombre.Length > 11 ? nombre[..11] : nombre);
                    break;
            }

            AplicarColumnasVisiblesPorTipo(tipo);
        }

        // Oculta las columnas de captura que el tipo no usa (y bloquea que el menú las vuelva a mostrar).
        private void AplicarColumnasVisiblesPorTipo(TipoNominaCaptura tipo)
        {
            _tipoColumnasActual = tipo;

            foreach (string col in _columnasBloqueadasPorTipo)
                if (dgvMostrarPersonal.Columns.Contains(col))
                    dgvMostrarPersonal.Columns[col].Visible = true;
            _columnasBloqueadasPorTipo.Clear();

            var visibles = PerfilColumnasNomina.ColumnasConcepto(tipo);
            foreach (string col in _columnasDeConcepto)
            {
                if (!dgvMostrarPersonal.Columns.Contains(col) || visibles.Contains(col)) continue;
                if (ColumnaTieneValores(col)) continue;   // nunca ocultar datos reales (reapertura de archivos)

                dgvMostrarPersonal.Columns[col].Visible = false;
                _columnasBloqueadasPorTipo.Add(col);
            }

            SincronizarMenuColumnas();
        }

        private bool ColumnaTieneValores(string col) => dgvMostrarPersonal.Rows.Cast<DataGridViewRow>().Any(r => !r.IsNewRow && r.Tag?.ToString() != "Totales" && GetDecimalSeguro(r, col) != 0m);
        private bool HayCapturaEnGrid() => dgvMostrarPersonal.Rows.Cast<DataGridViewRow>().Any(TieneMovimientoNominaEspecial);

        private void SincronizarMenuColumnas()
        {
            foreach (ToolStripItem it in tsMostrarColumna.DropDownItems)
            {
                if (it is ToolStripMenuItem mi && mi.Tag is string col && dgvMostrarPersonal.Columns.Contains(col))
                {
                    mi.Enabled = !_columnasBloqueadasPorTipo.Contains(col);
                    mi.Checked = dgvMostrarPersonal.Columns[col].Visible;
                }
            }
        }

        private bool ExentoIncluidoEnConceptosActual() => rdiobtnEspecial.Checked && PerfilColumnasNomina.EsSeparacion(PerfilColumnasNomina.Clasificar(cbMotivo.SelectedItem?.ToString() ?? ""));

        private void SetColHeader(string colName, string headerText)
        {
            if (dgvMostrarPersonal.Columns.Contains(colName))
                dgvMostrarPersonal.Columns[colName].HeaderText = headerText;
        }

        private void AjustarAnchoColumnasEncabezados()
        {
            if (dgvMostrarPersonal.Columns.Contains("hsNorm"))
                dgvMostrarPersonal.Columns["hsNorm"].Width = 115;

            if (dgvMostrarPersonal.Columns.Contains("hsDobles"))
                dgvMostrarPersonal.Columns["hsDobles"].Width = 105;

            if (dgvMostrarPersonal.Columns.Contains("hsTriples"))
                dgvMostrarPersonal.Columns["hsTriples"].Width = 120;

            if (dgvMostrarPersonal.Columns.Contains("OF"))
                dgvMostrarPersonal.Columns["OF"].Width = 125;
        }

        // ── Helpers de lectura binaria VB6 ────────────────────────────────

        private List<string> LeerNombresDesdePersonal(string rutaPersonal)
        {
            var list = new List<string>();
            if (!File.Exists(rutaPersonal)) return list;

            const int lenPer = 152;
            using var fs = new FileStream(rutaPersonal, FileMode.Open, FileAccess.Read);
            using var br = new System.IO.BinaryReader(fs, Encoding.Default);

            long total = fs.Length / lenPer;
            for (int i = 0; i < total; i++)
            {
                var buf = br.ReadBytes(lenPer);
                if (buf.All(b => b == 0)) continue;
                string n = Encoding.Default.GetString(buf, 0, 20).Trim();
                string a1 = Encoding.Default.GetString(buf, 20, 20).Trim();
                string a2 = Encoding.Default.GetString(buf, 40, 20).Trim();
                string nombre = $"{a1} {a2} {n}".Trim();
                list.Add(string.IsNullOrWhiteSpace(nombre) ? $"Empleado {i + 1}" : nombre);
            }
            return list;
        }

        private List<string> LeerBanamexDesdeArchivo(string rutaBnx)
        {
            var list = new List<string>();
            if (!File.Exists(rutaBnx)) return list;

            using var fs = new FileStream(rutaBnx, FileMode.Open, FileAccess.Read);
            using var br = new System.IO.BinaryReader(fs, Encoding.ASCII);

            long total = fs.Length / 16;
            for (int i = 0; i < total; i++)
            {
                byte[] buf = br.ReadBytes(16);
                list.Add(Encoding.ASCII.GetString(buf).Trim('\x00', ' '));
            }
            return list;
        }

        // ── Helpers varios ────────────────────────────────────────────────
        // Unifica ObtenerDecimal (instancia) y GetDecimal (estático)
        private static decimal GetDecimal(DataGridViewRow row, string col)
        {
            var v = row.Cells[col].Value?.ToString() ?? "";
            v = v.Replace("$", "").Replace(",", "").Trim();
            return decimal.TryParse(v, out decimal r) ? r : 0m;
        }

        private static int GetInt(DataGridViewRow row, string col)
        {
            var v = row.Cells[col].Value?.ToString() ?? "";
            return int.TryParse(v, out int r) ? r : 0;
        }

        private int ObtenerEntero(DataGridViewRow row, string nombreColumna)
        {
            var v = row.Cells[nombreColumna].Value;
            if (v == null || string.IsNullOrWhiteSpace(v.ToString())) return 0;
            return int.TryParse(v.ToString(), out int r) ? r : 0;
        }

        private bool EsVigenteEnPeriodo(string fechaBaja, int anioFiscal)
        {
            if (string.IsNullOrWhiteSpace(fechaBaja)) return true;
            return DateTime.TryParseExact(fechaBaja, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime baja) && baja.Year >= anioFiscal;
        }

        private int ObtenerAnioFecha(string fecha)
        {
            if (DateTime.TryParse(fecha, out DateTime d)) return d.Year;
            var m = Regex.Match(fecha ?? "", @"\d{4}");
            return m.Success ? int.Parse(m.Value) : 0;
        }

        private void ObtenerRangoQuincena(out DateTime inicio, out DateTime fin)
        {
            int mes = rdiobtnNormal.Checked && cbMes.SelectedIndex >= 0 ? cbMes.SelectedIndex + 1 : DateTime.Now.Month;

            if (rdiobtnPrimera.Checked)
            {
                inicio = new DateTime(anio, mes, 1);
                fin = new DateTime(anio, mes, 15);
            }
            else
            {
                inicio = new DateTime(anio, mes, 16);
                fin = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes));
            }
        }

        private DateTime ObtenerFechaCorteNomina()
        {
            ObtenerRangoQuincena(out _, out DateTime finQ);
            return finQ.Date;
        }

        private int ObtenerMesNomina() => rdiobtnNormal.Checked && cbMes.SelectedIndex >= 0 ? cbMes.SelectedIndex + 1 : DateTime.Now.Month;

        private void SeleccionarQuincenaPorFecha()
        {
            if (DateTime.Now.Day <= 15)
                rdiobtnPrimera.Checked = true;
            else
                rdiobtnSegunda.Checked = true;
        }

        private void ActualizarLblDatos()
        {
            int mes = ObtenerMesNomina();
            string mesText = CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetMonthName(mes).ToUpper();
            string ord = rdiobtnPrimera.Checked ? "1ra" : "2da";
            lblDatos.Text = $"{ord} Nómina de {mesText} del {anio}";
        }

        private void ActualizarLblDatosPorNombre(string nombreArchivoSinExt)
        {
            string nombre = nombreArchivoSinExt.ToUpper();

            if (Regex.IsMatch(nombre, @"^[A-Z]{3}\d{5}$"))
            {
                string mesAbrev = nombre[..3];
                int quincena = int.Parse(nombre.Substring(3, 1));
                int year = int.Parse(nombre[4..]);
                var dtfi = CultureInfo.GetCultureInfo("es-ES").DateTimeFormat;
                int mesNum = Array.FindIndex(dtfi.AbbreviatedMonthNames, m => m.ToUpper().StartsWith(mesAbrev)) + 1;
                string mesFull = mesNum > 0 ? dtfi.GetMonthName(mesNum).ToUpper() : mesAbrev;

                lblDatos.Text = $"{(quincena == 1 ? "1ra" : "2da")} Nómina de {mesFull} del {year}";
            }
            else if (Regex.Match(nombre, @"^(?<n>\d{2,})(?<c>[A-Z]+?)(?<m>ENE|FEB|MAR|ABR|MAY|JUN|JUL|AGO|SEP|OCT|NOV|DIC)(?<a>\d{4})$") is { Success: true } esp)
            {
                lblDatos.Text = $"Nómina Especial — {esp.Groups["c"].Value} · {esp.Groups["m"].Value} {esp.Groups["a"].Value} · No. {esp.Groups["n"].Value}";
            }
            else if (Regex.IsMatch(nombre, @"^[A-Z_]+$") && nombre.Length > 2)
            {
                lblDatos.Text = $"Nómina Especial — {nombre}";
            }
            else
            {
                lblDatos.Text = $"Nómina: {nombre}";
            }
        }

        private static string AbrevMes(int mes)
        {
            string abrev = CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetAbbreviatedMonthName(mes).ToUpper().Replace(".", "");
            return abrev.Length > 3 ? abrev[..3] : abrev;
        }

        private void PersonalizacionInicial()
        {
            lblMotivo.Visible = false;
            lblSelecMes.Visible = true;
            cbMotivo.Visible = false;
            cbMes.Visible = true;
            txtOtroRazon.Visible = false;
            chkBoxCE.Visible = false;
            txtCE.Visible = false;
            lblCE.Visible = false;
            rdiobtnNormal.Checked = true;

            lblDatos.BackColor = Color.Yellow;
            lblDatos.ForeColor = Color.Black;
            lblDatos.Font = new Font(lblDatos.Font, FontStyle.Bold);
            lblDatos.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblDatos.AutoSize = false;
            lblDatos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            cbMes.Items.Clear();
            var dtfi = new CultureInfo("es-ES").DateTimeFormat;
            for (int m = 1; m <= 12; m++)
                cbMes.Items.Add(dtfi.GetMonthName(m).ToUpper());
            cbMes.SelectedIndex = DateTime.Now.Month - 1;

            ConfigurarPanelSuperiorCaptura();
        }

        private void AplicarVisibilidadPorTipoNomina(bool forzarActualizarGrid = false)
        {
            bool esNormal = rdiobtnNormal.Checked;

            rdiobtnPrimera.Enabled = esNormal;
            rdiobtnSegunda.Enabled = esNormal;
            lblSelecMes.Visible = esNormal;
            cbMes.Visible = esNormal;
            cbMes.Enabled = esNormal;
            lblMotivo.Visible = !esNormal;
            cbMotivo.Visible = !esNormal;
            cbMotivo.Enabled = !esNormal;

            bool mostrarOtro = !esNormal && cbMotivo.SelectedItem?.ToString().Equals("OTROS...", StringComparison.OrdinalIgnoreCase) == true;
            txtOtroRazon.Visible = mostrarOtro;
            if (esNormal) txtOtroRazon.Text = "";

            if (esNormal && cbMes.SelectedIndex < 0)
                cbMes.SelectedIndex = DateTime.Now.Month - 1;

            mesSeleccionado = ObtenerMesNomina();
            ActualizarLblDatos();

            if (forzarActualizarGrid && _formularioCargado)
            {
                _otrasManualOverride.Clear();
                CargarEmpleadosEnGrid();
                AplicarCalculosIniciales();
                ActualizarLblDatos();
            }

            if (!esNormal)
            {
                if (cbMotivo.SelectedItem != null)
                {
                    string m = cbMotivo.SelectedItem.ToString();
                    string label = m.Equals("OTROS...", StringComparison.OrdinalIgnoreCase) ? (string.IsNullOrWhiteSpace(txtOtroRazon.Text) ? "OTROS" : txtOtroRazon.Text.ToUpper()) : m;
                    lblDatos.Text = $"Nómina Especial — {label}";
                }
                else
                {
                    lblDatos.Text = "Nómina Especial";
                }
            }
        }

        // ── Helpers de KeyPress ───────────────────────────────────────────

        private void SoloNumerosYPunto(object sender, KeyPressEventArgs e)
        {
            if (!(sender is System.Windows.Forms.TextBox tb)) return;
            char tecla = e.KeyChar;
            string colName = dgvMostrarPersonal.Columns[dgvMostrarPersonal.CurrentCell.ColumnIndex].Name;

            if (char.IsControl(tecla)) return;

            if (colName == "DiasTrabajados")
            {
                if (!char.IsDigit(tecla)) e.Handled = true;
                return;
            }

            if (tecla == '-' && tb.SelectionStart == 0 && !tb.Text.Contains('-')) return;
            if (char.IsDigit(tecla)) return;
            if (tecla == '.' && !tb.Text.Contains('.')) return;

            e.Handled = true;
        }

        private void ValidarDosDecimales(object sender, EventArgs e)
        {
            if (!(sender is System.Windows.Forms.TextBox tb)) return;

            string colName = dgvMostrarPersonal.Columns[dgvMostrarPersonal.CurrentCell.ColumnIndex].Name;

            if (string.IsNullOrWhiteSpace(tb.Text)) return;

            if (colName == "DiasTrabajados")
            {
                if (rdiobtnEspecial.Checked)
                    return; // sin tope en nómina especial — decisión de negocio confirmada

                int topeDias = 15;
                if (int.TryParse(tb.Text, out int valor) && valor > topeDias)
                {
                    tb.Text = topeDias.ToString();
                    tb.SelectionStart = tb.Text.Length;
                }
                return;
            }

            int puntoIndex = tb.Text.IndexOf('.');
            if (puntoIndex >= 0)
            {
                int decimales = tb.Text.Length - puntoIndex - 1;
                if (decimales > 2)
                {
                    int cursor = tb.SelectionStart;
                    tb.Text = tb.Text[..(puntoIndex + 3)];
                    tb.SelectionStart = Math.Min(cursor, tb.Text.Length);
                }
            }
        }

        private void FormatearTarjeta(object sender, EventArgs e)
        {
            if (!(sender is System.Windows.Forms.TextBox tb)) return;

            string limpio = tb.Text.Replace(" ", "");
            if (limpio.Length > 16) limpio = limpio[..16];

            string formateado = string.Join(" ", Enumerable.Range(0, limpio.Length).GroupBy(i => i / 4).Select(g => new string(g.Select(i => limpio[i]).ToArray())));

            int cursor = tb.SelectionStart;
            tb.Text = formateado;
            tb.SelectionStart = Math.Min(cursor, tb.Text.Length);
        }

        // ── Resaltado del grid ────────────────────────────────────────────
        private void PintarFilaVerde(int rowIndex)
        {
            if (_filaResaltada >= 0 && _filaResaltada < dgvMostrarPersonal.Rows.Count)
                LimpiarResaltadoFila(_filaResaltada);

            _filaResaltada = rowIndex;

            foreach (DataGridViewCell c in dgvMostrarPersonal.Rows[rowIndex].Cells)
            {
                c.Style.BackColor = Color.LightGreen;
                c.Style.ForeColor = Color.Black;
            }

            dgvMostrarPersonal.Rows[rowIndex].Cells["NumeroEmpleado"].Style.BackColor = Color.Gray;
            dgvMostrarPersonal.Rows[rowIndex].Cells["NombreEmpleado"].Style.BackColor = Color.Gray;
        }

        private void LimpiarResaltadoFila(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvMostrarPersonal.Rows.Count) return;

            foreach (DataGridViewCell c in dgvMostrarPersonal.Rows[rowIndex].Cells)
            {
                c.Style.BackColor = Color.White;
                c.Style.ForeColor = Color.Black;
            }

            dgvMostrarPersonal.Rows[rowIndex].Cells["NumeroEmpleado"].Style.BackColor = Color.LightGray;
            dgvMostrarPersonal.Rows[rowIndex].Cells["NombreEmpleado"].Style.BackColor = Color.LightGray;
        }

        // ── Helpers de estado ─────────────────────────────────────────────

        private void CopyDataGridView(bool includeHeaders)
        {
            var originalMode = dgvMostrarPersonal.ClipboardCopyMode;
            dgvMostrarPersonal.ClipboardCopyMode = includeHeaders ? DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText : DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

            var data = dgvMostrarPersonal.GetClipboardContent();
            if (data != null) Clipboard.SetDataObject(data);

            dgvMostrarPersonal.ClipboardCopyMode = originalMode;
        }

        // Helper para SetRow en CargarDatosDesdeNOM
        private void SetRow(DataGridViewRow row, int id, string nombre, int dias, decimal sueldo, decimal hsNor, decimal hsDbl, decimal hsTri, decimal viaticos, decimal pvac, decimal otras,
            decimal exentos, decimal totIngr, decimal ispt, decimal crdsal, decimal imss, decimal prestamos, decimal fonacot, decimal telefono, decimal otraDed, decimal totDeduc, decimal NETO, string banamex)
        {
            row.Cells["NumeroEmpleado"].Value = id.ToString();
            row.Cells["NombreEmpleado"].Value = nombre;
            row.Cells["DiasTrabajados"].Value = dias.ToString();
            row.Cells["Salario"].Value = sueldo.ToString("N2");
            row.Cells["hsNorm"].Value = hsNor.ToString("N2");
            row.Cells["hsDobles"].Value = hsDbl.ToString("N2");
            row.Cells["hsTriples"].Value = hsTri.ToString("N2");
            row.Cells["OF"].Value = viaticos.ToString("N2");
            row.Cells["PVacac"].Value = pvac.ToString("N2");
            row.Cells["Otras"].Value = otras.ToString("N0");
            row.Cells["PercExenta"].Value = exentos.ToString("N2");
            row.Cells["TotIngr"].Value = totIngr.ToString("N2");
            row.Cells["Ispt"].Value = ispt.ToString("N2");
            row.Cells["SubEmp"].Value = crdsal.ToString("N2");
            row.Cells["IMSS"].Value = imss.ToString("N2");
            row.Cells["Prestamos"].Value = prestamos.ToString("N2");
            row.Cells["FONACOT"].Value = fonacot.ToString("N2");
            row.Cells["PensionAliment"].Value = telefono.ToString("N2");
            row.Cells["INFONAVIT"].Value = otraDed.ToString("N2");
            row.Cells["TotDeduc"].Value = totDeduc.ToString("N2");
            row.Cells["NETO"].Value = NETO.ToString("N2");
            row.Cells["BANAMEX"].Value = banamex;
        }

        private void AplicarModoSoloLectura()
        {
            if (!ModoSoloLectura) return;

            dgvMostrarPersonal.ReadOnly = true;
            dgvMostrarPersonal.AllowUserToAddRows = false;
            dgvMostrarPersonal.AllowUserToDeleteRows = false;
            gbNomina.Enabled = false;
            gbQuincena.Enabled = false;
            cbMes.Visible = false;
            lblSelecMes.Visible = false;
            cbMotivo.Visible = false;
            lblMotivo.Visible = false;

            foreach (ToolStripItem item in tsOpciones.Items)
                item.Enabled = false;

            // Permitir en modo lectura
            tsAtajoExcel.Enabled = true;
            tsOrdenarPor.Enabled = true;
            tsHerramientas.Enabled = true;
            tsMostrarSimboloPesos.Enabled = true;
            tsCopiarSinEncabezados.Enabled = true;
            tsCopiarConEncabezados.Enabled = true;
            tsSeleccionarTodo.Enabled = true;
            tsMostrarColumna.Enabled = true;
            tsReestablecerTabla.Enabled = true;
        }

        private void DeshabilitarControlesPostGuardado()
        {
            gbNomina.Enabled = false;
            gbQuincena.Enabled = false;
            tsOpciones.Enabled = false;
            cbMotivo.Enabled = false;
            cbMes.Enabled = false;
            lblMotivo.Visible = false;
            lblSelecMes.Visible = false;
        }

        private void EscalarColumnasSegunDpi()
        {
            float factor = this.DeviceDpi / 96.0f;
            foreach (DataGridViewColumn col in dgvMostrarPersonal.Columns)
                col.Width = (int)(col.Width * factor);
        }

        // ── Módulo de impresión de nómina ─────────────────────────────────────────

        private enum TipoImpresionNomina
        {
            Completa,
            AgrupadaPorObra,
            ObraIndividual
        }

        private sealed class ItemImpresionNomina
        {
            public string Obra { get; set; } = "";
            public DataGridViewRow Row { get; set; }
            public bool EsEncabezadoObra => Row == null;
        }

        private List<DataGridViewColumn> _columnasImpresionNomina = new List<DataGridViewColumn>();
        private List<ItemImpresionNomina> _itemsImpresionNomina = new List<ItemImpresionNomina>();

        private int _indiceImpresionNomina = 0;
        private int _paginaImpresionNomina = 0;
        private int _totalEmpleadosImpresion = 0;
        private TipoImpresionNomina _tipoImpresionNomina;
        private string _tituloImpresionNomina = "";
        private string _obraActualImpresion = "";


        private void ConfigurarModuloImpresionNomina()
        {
            AsegurarColumnaObraOculta();

            // Evita duplicar opciones si por alguna razón se llama más de una vez.
            if (tsNomina.DropDownItems["tsmImprimirGridCompleta"] == null)
            {
                var itemGridCompleta = new ToolStripMenuItem
                {
                    Name = "tsmImprimirGridCompleta",
                    Text = "Grid completa"
                };
                itemGridCompleta.Click += tsmImprimirNominaCompleta_Click;

                var itemAgrupada = new ToolStripMenuItem
                {
                    Name = "tsmImprimirAgrupadaPorObra",
                    Text = "Agrupada por obra"
                };
                itemAgrupada.Click += tsmImprimirNominaAgrupadaPorObra_Click;

                var separador = new ToolStripSeparator
                {
                    Name = "tssSeparadorImpresionNomina"
                };

                tsNomina.DropDownItems.Insert(0, separador);
                tsNomina.DropDownItems.Insert(0, itemAgrupada);
                tsNomina.DropDownItems.Insert(0, itemGridCompleta);
            }

            // Conecta los items ya existentes: Obra 0, Obra 1, Obra 2, Obra 47...
            foreach (ToolStripMenuItem item in tsNomina.DropDownItems.OfType<ToolStripMenuItem>())
            {
                if (item.Name.StartsWith("tsmObra", StringComparison.OrdinalIgnoreCase))
                {
                    item.Click -= tsmImprimirNominaObraIndividual_Click;
                    item.Click += tsmImprimirNominaObraIndividual_Click;
                }
            }
        }

        private void AsegurarColumnaObraOculta()
        {
            if (dgvMostrarPersonal.Columns.Contains("Obra")) return;

            var colObra = new DataGridViewTextBoxColumn
            {
                Name = "Obra",
                HeaderText = "Obra",
                ReadOnly = true,
                Visible = false
            };

            dgvMostrarPersonal.Columns.Add(colObra);
        }

        private void tsmImprimirNominaCompleta_Click(object sender, EventArgs e)
        {
            IniciarImpresionNomina(TipoImpresionNomina.Completa);
        }

        private void tsmImprimirNominaAgrupadaPorObra_Click(object sender, EventArgs e)
        {
            IniciarImpresionNomina(TipoImpresionNomina.AgrupadaPorObra);
        }

        private void tsmImprimirNominaObraIndividual_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item)
                IniciarImpresionNomina(TipoImpresionNomina.ObraIndividual, item.Text);
        }

        private void IniciarImpresionNomina(TipoImpresionNomina tipo, string obraFiltro = "")
        {
            if (dgvMostrarPersonal.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos de nómina para imprimir.", "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (rdiobtnEspecial.Checked && !ValidarNominaEspecialParaImpresion())
                return;

            _tipoImpresionNomina = tipo;
            _indiceImpresionNomina = 0;
            _paginaImpresionNomina = 0;
            _obraActualImpresion = "";

            _columnasImpresionNomina = dgvMostrarPersonal.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();

            if (_columnasImpresionNomina.Count == 0)
            {
                MessageBox.Show("No hay columnas visibles para imprimir.", "Sin columnas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _itemsImpresionNomina = ConstruirItemsImpresionNomina(tipo, obraFiltro);

            bool tieneFilas = _itemsImpresionNomina.Any(x => !x.EsEncabezadoObra && x.Row != null && x.Row.Tag?.ToString() != "Totales"
            );

            if (!tieneFilas)
            {
                string mensaje;

                if (rdiobtnEspecial.Checked)
                {
                    mensaje = tipo == TipoImpresionNomina.ObraIndividual ? $"No hay empleados con movimientos para imprimir en {obraFiltro}."
                        : "No hay empleados con movimientos para imprimir en esta nómina especial.";
                }
                else
                {
                    mensaje = tipo == TipoImpresionNomina.ObraIndividual ? $"No se encontraron trabajadores para {obraFiltro}."
                        : "No se encontraron trabajadores para imprimir.";
                }

                MessageBox.Show(mensaje, "Sin registros", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _totalEmpleadosImpresion = ContarEmpleadosImpresion();

            _tituloImpresionNomina = ObtenerTituloImpresionNomina(tipo, obraFiltro);

            using var documento = new PrintDocument();

            documento.DocumentName = _tituloImpresionNomina;
            documento.DefaultPageSettings.Landscape = true;
            documento.DefaultPageSettings.Margins = new Margins(25, 25, 35, 35);

            var papelCarta = documento.PrinterSettings.PaperSizes.Cast<PaperSize>().FirstOrDefault(p => p.Kind == PaperKind.Letter);

            if (papelCarta != null)
                documento.DefaultPageSettings.PaperSize = papelCarta;

            documento.BeginPrint += DocumentoNomina_BeginPrint;
            documento.PrintPage += DocumentoNomina_PrintPage;

            using var vistaPrevia = new PrintPreviewDialog
            {
                Document = documento,
                WindowState = FormWindowState.Maximized,
                StartPosition = FormStartPosition.CenterParent
            };

            vistaPrevia.ShowDialog(this);
        }

        private void DocumentoNomina_BeginPrint(object sender, PrintEventArgs e)
        {
            _indiceImpresionNomina = 0;
            _paginaImpresionNomina = 0;
            _obraActualImpresion = "";

            _totalEmpleadosImpresion = ContarEmpleadosImpresion();
        }

        private List<ItemImpresionNomina> ConstruirItemsImpresionNomina(TipoImpresionNomina tipo, string obraFiltro)
        {
            var filas = dgvMostrarPersonal.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Where(r => r.Tag?.ToString() != "Totales")
                .Where(r => !rdiobtnEspecial.Checked || TieneMovimientoNominaEspecial(r)).ToList();

            if (tipo == TipoImpresionNomina.Completa)
            {
                var resultadoCompleto = filas.Select(r => new ItemImpresionNomina { Row = r }).ToList();

                DataGridViewRow filaTotales = ObtenerFilaTotalesParaImpresion();

                if (filaTotales != null && resultadoCompleto.Count > 0)
                    resultadoCompleto.Add(new ItemImpresionNomina { Row = filaTotales });

                return resultadoCompleto;
            }

            var filasConObra = filas.Select(r => new
            {
                Row = r,
                Obra = NormalizarTextoObra(ObtenerObraDeFila(r))
            }).ToList();

            if (tipo == TipoImpresionNomina.ObraIndividual)
            {
                filasConObra = filasConObra.Where(x => ObrasCoinciden(x.Obra, obraFiltro)).ToList();
            }

            var resultado = new List<ItemImpresionNomina>();

            var grupos = filasConObra.GroupBy(x => x.Obra).OrderBy(g => ObtenerNumeroObra(g.Key)).ThenBy(g => g.Key);

            foreach (var grupo in grupos)
            {
                resultado.Add(new ItemImpresionNomina
                {
                    Obra = grupo.Key,
                    Row = null
                });

                foreach (var item in grupo.OrderBy(x => x.Row.Cells["NombreEmpleado"].Value?.ToString()))
                {
                    resultado.Add(new ItemImpresionNomina
                    {
                        Obra = grupo.Key,
                        Row = item.Row
                    });
                }
            }

            return resultado;
        }

        private string ObtenerObraDeFila(DataGridViewRow row)
        {
            string[] columnasPosibles = { "Obra", "NoObra", "NumeroObra", "NumObra", "IdObra", "Frente", "FrenteTrabajo", "FrenteDeTrabajo", "CentroCosto",
                "CentroDeCosto", "Ceco", "CCosto" };

            foreach (string col in columnasPosibles)
            {
                if (dgvMostrarPersonal.Columns.Contains(col))
                {
                    string valor = row.Cells[col].Value?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(valor))
                        return valor;
                }
            }

            if (!dgvMostrarPersonal.Columns.Contains("NumeroEmpleado"))
                return "Sin obra";

            if (!int.TryParse(row.Cells["NumeroEmpleado"].Value?.ToString(), out int idEmpleado))
                return "Sin obra";

            object emp = null;

            if (_empleadosActivos != null)
                emp = _empleadosActivos.FirstOrDefault(x => x.Id == idEmpleado);

            if (emp == null && _todosEmpleadosRUEP != null)
                emp = _todosEmpleadosRUEP.FirstOrDefault(x => x.Id == idEmpleado);

            string obraEmpleado = ObtenerValorPropiedadObra(emp);

            return string.IsNullOrWhiteSpace(obraEmpleado) ? "Sin obra" : obraEmpleado;
        }

        private string ObtenerValorPropiedadObra(object emp)
        {
            if (emp == null) return "";

            string[] propiedadesPosibles = { "Obra", "NoObra", "NumeroObra", "NumObra", "IdObra", "Frente", "FrenteTrabajo", "FrenteDeTrabajo", "CentroCosto",
                "CentroDeCosto", "Ceco", "CCosto" };

            Type tipo = emp.GetType();

            foreach (string nombrePropiedad in propiedadesPosibles)
            {
                PropertyInfo prop = tipo.GetProperty(nombrePropiedad, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null) continue;

                string valor = prop.GetValue(emp)?.ToString() ?? "";

                if (!string.IsNullOrWhiteSpace(valor))
                    return valor;
            }

            return "";
        }

        private bool ObrasCoinciden(string obraA, string obraB)
        {
            return NormalizarTextoObra(obraA).Equals(NormalizarTextoObra(obraB), StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizarTextoObra(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return "Sin obra";

            valor = valor.Trim();

            Match match = Regex.Match(valor, @"\d+");

            if (match.Success && int.TryParse(match.Value, out int numeroObra))
                return $"Obra {numeroObra}";

            return valor;
        }

        private int ObtenerNumeroObra(string obra)
        {
            Match match = Regex.Match(obra ?? "", @"\d+");

            if (match.Success && int.TryParse(match.Value, out int numero))
                return numero;

            return int.MaxValue;
        }

        private void DocumentoNomina_PrintPage(object sender, PrintPageEventArgs e)
        {
            _paginaImpresionNomina++;

            Graphics g = e.Graphics;
            RectangleF area = e.MarginBounds;

            using var fuenteTitulo = new Font("Arial", 11, FontStyle.Bold);
            using var fuenteSubtitulo = new Font("Arial", 8, FontStyle.Regular);
            using var fuenteGrupo = new Font("Arial", 9, FontStyle.Bold);

            float anchoOriginal = _columnasImpresionNomina.Sum(c => Math.Max(c.Width, 1));
            float escala = area.Width / anchoOriginal;
            float tamanoFuente = Math.Max(5.0f, Math.Min(8.0f, 7.0f * escala * 1.25f));

            using var fuenteHeader = new Font("Arial", tamanoFuente, FontStyle.Bold);
            using var fuenteCelda = new Font("Arial", tamanoFuente, FontStyle.Regular);

            float altoFila = fuenteCelda.GetHeight(g) + 6;
            float altoHeader = fuenteHeader.GetHeight(g) + 8;

            List<float> anchos = CalcularAnchosColumnas(area.Width);

            float y = DibujarEncabezadoReporte(g, area, fuenteTitulo, fuenteSubtitulo);
            float inicioContenido = y;

            if (_tipoImpresionNomina == TipoImpresionNomina.Completa)
            {
                y = DibujarEncabezadosColumnas(g, area.Left, y, altoHeader, anchos, fuenteHeader);
            }
            else
            {
                bool continuaObra = _indiceImpresionNomina < _itemsImpresionNomina.Count && !_itemsImpresionNomina[_indiceImpresionNomina].EsEncabezadoObra
                    && !string.IsNullOrWhiteSpace(_obraActualImpresion);

                if (continuaObra)
                {
                    y = DibujarTituloObra(g, area, y, $"{_obraActualImpresion} (continuación)", fuenteGrupo);
                    y = DibujarEncabezadosColumnas(g, area.Left, y, altoHeader, anchos, fuenteHeader);
                }
            }

            while (_indiceImpresionNomina < _itemsImpresionNomina.Count)
            {
                ItemImpresionNomina item = _itemsImpresionNomina[_indiceImpresionNomina];

                if (item.EsEncabezadoObra)
                {
                    // Cada obra inicia en nueva página si ya se imprimió contenido.
                    if (y > inicioContenido + 1)
                    {
                        e.HasMorePages = true;
                        return;
                    }

                    _obraActualImpresion = item.Obra;

                    y = DibujarTituloObra(g, area, y, item.Obra, fuenteGrupo);
                    y = DibujarEncabezadosColumnas(g, area.Left, y, altoHeader, anchos, fuenteHeader);

                    _indiceImpresionNomina++;
                    continue;
                }

                if (y + altoFila > area.Bottom)
                {
                    e.HasMorePages = true;
                    return;
                }

                DibujarFilaDatos(g, item.Row, area.Left, y, altoFila, anchos, fuenteCelda);
                y += altoFila;

                _indiceImpresionNomina++;
            }

            e.HasMorePages = false;
            _obraActualImpresion = "";
        }

        private float DibujarEncabezadoReporte(Graphics g, RectangleF area, Font fuenteTitulo, Font fuenteSubtitulo)
        {
            string empresa = _datosEmpresa?.nombre ?? "EMPRESA";
            string datosNomina = lblDatos?.Text ?? "";
            string pagina = $"Página {_paginaImpresionNomina}";
            string totalEmpleados = $"Empleados impresos: {_totalEmpleadosImpresion}";

            using var formatoDerecha = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Near
            };

            g.DrawString(_tituloImpresionNomina, fuenteTitulo, Brushes.Black, area.Left, area.Top);
            g.DrawString(pagina, fuenteSubtitulo, Brushes.Black, area, formatoDerecha);

            float y = area.Top + fuenteTitulo.GetHeight(g) + 4;

            g.DrawString(empresa, fuenteSubtitulo, Brushes.Black, area.Left, y);
            y += fuenteSubtitulo.GetHeight(g) + 2;

            g.DrawString(datosNomina, fuenteSubtitulo, Brushes.Black, area.Left, y);
            y += fuenteSubtitulo.GetHeight(g) + 2;

            g.DrawString(totalEmpleados, fuenteSubtitulo, Brushes.Black, area.Left, y);
            y += fuenteSubtitulo.GetHeight(g) + 2;

            g.DrawString($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}", fuenteSubtitulo, Brushes.Black, area.Left, y);
            y += fuenteSubtitulo.GetHeight(g) + 8;

            g.DrawLine(Pens.Black, area.Left, y, area.Right, y);
            y += 6;

            return y;
        }

        private float DibujarTituloObra(Graphics g, RectangleF area, float y, string textoObra, Font fuente)
        {
            float alto = fuente.GetHeight(g) + 8;

            RectangleF rect = new RectangleF(area.Left, y, area.Width, alto);

            g.FillRectangle(Brushes.Gainsboro, rect);
            g.DrawRectangle(Pens.Black, rect.X, rect.Y, rect.Width, rect.Height);

            using var formato = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };

            RectangleF textoRect = new RectangleF(rect.X + 5, rect.Y, rect.Width - 10, rect.Height);
            g.DrawString(textoObra, fuente, Brushes.Black, textoRect, formato);

            return y + alto;
        }

        private float DibujarEncabezadosColumnas(Graphics g, float xInicial, float y, float alto, List<float> anchos, Font fuente)
        {
            float x = xInicial;

            using var formato = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };

            for (int i = 0; i < _columnasImpresionNomina.Count; i++)
            {
                DataGridViewColumn col = _columnasImpresionNomina[i];

                RectangleF rect = new RectangleF(x, y, anchos[i], alto);

                g.FillRectangle(Brushes.LightGray, rect);
                g.DrawRectangle(Pens.Black, rect.X, rect.Y, rect.Width, rect.Height);

                RectangleF textoRect = new RectangleF(rect.X + 2, rect.Y, Math.Max(1, rect.Width - 4), rect.Height);
                g.DrawString(col.HeaderText, fuente, Brushes.Black, textoRect, formato);

                x += anchos[i];
            }

            return y + alto;
        }

        private void DibujarFilaDatos(Graphics g, DataGridViewRow row, float xInicial, float y, float alto, List<float> anchos, Font fuente)
        {
            float x = xInicial;
            bool esFilaTotales = row.Tag?.ToString() == "Totales";

            using var fuenteFila = esFilaTotales ? new Font(fuente, FontStyle.Bold) : new Font(fuente, FontStyle.Regular);

            for (int i = 0; i < _columnasImpresionNomina.Count; i++)
            {
                DataGridViewColumn col = _columnasImpresionNomina[i];
                RectangleF rect = new RectangleF(x, y, anchos[i], alto);

                if (esFilaTotales)
                    g.FillRectangle(Brushes.WhiteSmoke, rect);
                else
                    g.FillRectangle(Brushes.White, rect);

                g.DrawRectangle(Pens.Black, rect.X, rect.Y, rect.Width, rect.Height);

                string texto = "";

                try
                {
                    texto = row.Cells[col.Index].FormattedValue?.ToString() ?? row.Cells[col.Index].Value?.ToString() ?? "";
                }
                catch
                {
                    texto = row.Cells[col.Index].Value?.ToString() ?? "";
                }

                using var formato = new StringFormat
                {
                    Alignment = ObtenerAlineacionHorizontal(col),
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };

                RectangleF textoRect = new RectangleF(rect.X + 2, rect.Y, Math.Max(1, rect.Width - 4), rect.Height);
                g.DrawString(texto, fuenteFila, Brushes.Black, textoRect, formato);

                x += anchos[i];
            }
        }

        private List<float> CalcularAnchosColumnas(float anchoDisponible)
        {
            float anchoOriginal = _columnasImpresionNomina.Sum(c => Math.Max(c.Width, 1));
            return _columnasImpresionNomina.Select(c => Math.Max(c.Width, 1) * anchoDisponible / anchoOriginal).ToList();
        }

        private StringAlignment ObtenerAlineacionHorizontal(DataGridViewColumn col)
        {
            DataGridViewContentAlignment alineacion = col.DefaultCellStyle.Alignment;

            if (alineacion == DataGridViewContentAlignment.NotSet)
                alineacion = dgvMostrarPersonal.DefaultCellStyle.Alignment;

            return alineacion switch
            {
                DataGridViewContentAlignment.MiddleRight => StringAlignment.Far,
                DataGridViewContentAlignment.TopRight => StringAlignment.Far,
                DataGridViewContentAlignment.BottomRight => StringAlignment.Far,

                DataGridViewContentAlignment.MiddleCenter => StringAlignment.Center,
                DataGridViewContentAlignment.TopCenter => StringAlignment.Center,
                DataGridViewContentAlignment.BottomCenter => StringAlignment.Center,

                _ => StringAlignment.Near
            };
        }

        private bool ValidarNominaEspecialParaImpresion()
        {
            string motivo = ObtenerMotivoNominaEspecial();

            if (string.IsNullOrWhiteSpace(motivo))
            {
                MessageBox.Show("Selecciona el motivo de la nómina especial antes de imprimir.", "Nómina especial", MessageBoxButtons.OK, MessageBoxIcon.Warning
                );

                cbMotivo.Focus();
                return false;
            }

            if (motivo.Equals("OTROS", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(txtOtroRazon.Text))
            {
                MessageBox.Show("Captura el nombre o razón de la nómina especial.", "Nómina especial", MessageBoxButtons.OK, MessageBoxIcon.Warning
                );

                txtOtroRazon.Focus();
                return false;
            }

            return true;
        }

        private string ObtenerTituloImpresionNomina(TipoImpresionNomina tipo, string obraFiltro)
        {
            string tipoNomina = rdiobtnEspecial.Checked ? $"Nómina Especial — {ObtenerMotivoNominaEspecial()}" : "Nómina";

            return tipo switch
            {
                TipoImpresionNomina.Completa => $"{tipoNomina} - Grid completa",
                TipoImpresionNomina.AgrupadaPorObra => $"{tipoNomina} - Agrupada por obra",
                TipoImpresionNomina.ObraIndividual => $"{tipoNomina} - {NormalizarTextoObra(obraFiltro)}",
                _ => tipoNomina
            };
        }

        private string ObtenerMotivoNominaEspecial()
        {
            if (!rdiobtnEspecial.Checked)
                return "";

            string motivo = cbMotivo.SelectedItem?.ToString()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(motivo))
                return "";

            if (motivo.StartsWith("OTROS", StringComparison.OrdinalIgnoreCase))
            {
                string otro = txtOtroRazon.Text.Trim().ToUpper();

                return string.IsNullOrWhiteSpace(otro) ? "OTROS" : otro;
            }

            return motivo.ToUpper();
        }

        private bool TieneMovimientoNominaEspecial(DataGridViewRow row)
        {
            if (row == null || row.IsNewRow)
                return false;

            if (row.Tag?.ToString() == "Totales")
                return false;

            foreach (string colName in _columnasMovimientoNominaEspecial)
            {
                if (!dgvMostrarPersonal.Columns.Contains(colName))
                    continue;

                decimal valor = GetDecimalSeguro(row, colName);

                if (valor != 0m)
                    return true;
            }

            return false;
        }

        private decimal GetDecimalSeguro(DataGridViewRow row, string colName)
        {
            if (row == null || !dgvMostrarPersonal.Columns.Contains(colName))
                return 0m;

            string texto = row.Cells[colName].Value?.ToString() ?? "";

            texto = texto.Replace("$", "").Replace(",", "").Trim();

            if (decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valorInvariant))
                return valorInvariant;

            if (decimal.TryParse(texto, NumberStyles.Any, new CultureInfo("es-MX"), out decimal valorMx))
                return valorMx;

            return 0m;
        }

        private DataGridViewRow ObtenerFilaTotalesParaImpresion()
        {
            return dgvMostrarPersonal.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => !r.IsNewRow && r.Tag?.ToString() == "Totales");
        }

        private int ContarEmpleadosImpresion()
        {
            if (_itemsImpresionNomina == null)
                return 0;

            return _itemsImpresionNomina.Count(x => !x.EsEncabezadoObra && x.Row != null && !x.Row.IsNewRow && x.Row.Tag?.ToString() != "Totales");
        }

        private void ConfigurarPanelSuperiorCaptura()
        {
            if (panelSuperior == null || lblDatos == null) return;

            // ── Configuración visual de lblDatos ───────────────────────────────
            lblDatos.AutoSize = false;
            lblDatos.TextAlign = ContentAlignment.MiddleCenter;
            lblDatos.BackColor = Color.Yellow;
            lblDatos.ForeColor = Color.Black;
            lblDatos.Font = new Font(lblDatos.Font, FontStyle.Bold);
            lblDatos.Anchor = AnchorStyles.Top;

            // ── Reacomodar cuando el panel cambie de tamaño ───────────────────
            panelSuperior.Resize -= panelSuperior_Resize;
            panelSuperior.Resize += panelSuperior_Resize;

            AcomodarControlesPanelSuperior();
        }

        private void txtBuscarEnGrid_TextChanged(object sender, EventArgs e)
        {
            BuscarEmpleadoEnCaptura();
        }

        private void BuscarEmpleadoEnCaptura()
        {
            if (dgvMostrarPersonal == null || dgvMostrarPersonal.Rows.Count == 0)
                return;

            string texto = (txtBuscarEnGrid?.Text ?? "").Trim();

            if (_filaResaltada >= 0 && _filaResaltada < dgvMostrarPersonal.Rows.Count)
            {
                LimpiarResaltadoFila(_filaResaltada);
                _filaResaltada = -1;
            }

            if (string.IsNullOrWhiteSpace(texto))
                return;

            foreach (DataGridViewRow row in dgvMostrarPersonal.Rows)
            {
                if (row.IsNewRow || row.Tag?.ToString() == "Totales")
                    continue;

                if (!CoincideBusquedaCaptura(row, texto))
                    continue;

                dgvMostrarPersonal.ClearSelection();

                if (row.Index >= 0)
                {
                    dgvMostrarPersonal.FirstDisplayedScrollingRowIndex = row.Index;

                    string columnaFoco = dgvMostrarPersonal.Columns.Contains("DiasTrabajados")
                        ? "DiasTrabajados"
                        : "NombreEmpleado";

                    dgvMostrarPersonal.CurrentCell = row.Cells[columnaFoco];

                    PintarFilaVerde(row.Index);
                }

                return;
            }
        }

        private bool CoincideBusquedaCaptura(DataGridViewRow row, string texto)
        {
            if (row == null || row.IsNewRow || row.Tag?.ToString() == "Totales")
                return false;

            string textoNormalizado = NormalizarBusqueda(texto);

            if (string.IsNullOrWhiteSpace(textoNormalizado))
                return true;

            string[] palabras = textoNormalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (palabras.Length == 0)
                return true;

            string idGrid = row.Cells["NumeroEmpleado"].Value?.ToString() ?? "";
            string nombreGrid = row.Cells["NombreEmpleado"].Value?.ToString() ?? "";
            string fechaAltaGrid = row.Cells["FechaAl"].Value?.ToString() ?? "";
            string banamexGrid = row.Cells["BANAMEX"].Value?.ToString() ?? "";

            EmpleadoCompleto emp = null;

            if (int.TryParse(idGrid, out int idEmpleado))
            {
                emp = _empleadosActivos?.FirstOrDefault(x => x.Id == idEmpleado) ?? _todosEmpleadosRUEP?.FirstOrDefault(x => x.Id == idEmpleado);
            }

            string bolsa = NormalizarBusqueda(string.Join(" ", new[]
            {
                idGrid,
                nombreGrid,
                fechaAltaGrid,
                banamexGrid,

                emp?.Id.ToString(),
                emp?.Nombre,
                emp?.ApellidoP,
                emp?.ApellidoM,
                $"{emp?.Nombre} {emp?.ApellidoP} {emp?.ApellidoM}",
                $"{emp?.ApellidoP} {emp?.ApellidoM} {emp?.Nombre}",
                emp?.RFC,
                emp?.CURP,
                emp?.NSS,
                emp?.FechaAlta,
                emp?.FechaBaja,
                emp?.Banamex,
                emp?.Email,
                emp?.EmailEmpresa,
                emp?.Departamento,
                emp?.Puesto
            }));

            return palabras.All(p => bolsa.Contains(p));
        }

        private static string NormalizarBusqueda(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "";

            string normalizado = texto.Trim().ToUpperInvariant();

            normalizado = normalizado.Normalize(NormalizationForm.FormD);
            normalizado = new string(normalizado.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
            normalizado = normalizado.Normalize(NormalizationForm.FormC);
            normalizado = Regex.Replace(normalizado, @"\s+", " ");

            return normalizado.Trim();
        }

        private void panelSuperior_Resize(object sender, EventArgs e)
        {
            AcomodarControlesPanelSuperior();
        }

        private void AcomodarControlesPanelSuperior()
        {
            if (panelSuperior == null || lblDatos == null) return;

            int margenIzquierdo = 36;

            // ── lblDatos centrado en el panel ─────────────────────────────────
            int anchoLblDatos = Math.Min(760, Math.Max(420, panelSuperior.ClientSize.Width - 80));

            lblDatos.Width = anchoLblDatos;
            lblDatos.Height = 32;
            lblDatos.Left = (panelSuperior.ClientSize.Width - lblDatos.Width) / 2;
            lblDatos.Top = 18;

            lblDatos.BringToFront();
        }

        private static int ContarDiasHabiles(DateTime inicio, DateTime fin)
        {
            if (fin < inicio) return 0;
            int dias = 0;
            for (DateTime d = inicio.Date; d <= fin.Date; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                    dias++;
            return dias;
        }

        private void ObtenerRangoQuincenaAnterior(out DateTime inicio, out DateTime fin, out int mes, out int anioResultado, out bool esPrimera)
        {
            int mesActual = ObtenerMesNomina();
            if (rdiobtnPrimera.Checked)
            {
                mes = mesActual == 1 ? 12 : mesActual - 1;
                anioResultado = mesActual == 1 ? anio - 1 : anio;
                esPrimera = false;
                inicio = new DateTime(anioResultado, mes, 16);
                fin = new DateTime(anioResultado, mes, DateTime.DaysInMonth(anioResultado, mes));
            }
            else
            {
                mes = mesActual;
                anioResultado = anio;
                esPrimera = true;
                inicio = new DateTime(anioResultado, mes, 1);
                fin = new DateTime(anioResultado, mes, 15);
            }
        }

        private void DetectarYConfirmarEmpleadosNuevosSinPagoAnterior()
        {
            _empleadosNuevosSinPagoAnterior.Clear();
            _diasMaximosSugeridos.Clear();

            if (!rdiobtnNormal.Checked) return;
            if (string.IsNullOrWhiteSpace(RutaTrabajo)) return;   // ← nuevo: aún no está inicializado (se dispara desde el constructor)

            ObtenerRangoQuincenaAnterior(out DateTime inicioAnt, out DateTime finAnt, out int mesAnt, out int anioAnt, out bool antEsPrimera);
            string archivoAnterior = Path.Combine(RutaTrabajo, $"{AbrevMes(mesAnt)}{(antEsPrimera ? 1 : 2)}{anioAnt}.json");


            var idsEnQuincenaAnterior = new HashSet<string>();
            if (File.Exists(archivoAnterior))
            {
                try { idsEnQuincenaAnterior = NominaJsonService.Cargar(archivoAnterior).Select(d => d.idNomina).ToHashSet(); }
                catch { /* si no se puede leer, se trata como si no existiera */ }
            }

            ObtenerRangoQuincena(out _, out DateTime finAct);

            foreach (var emp in _empleadosActivos ?? Enumerable.Empty<EmpleadoCompleto>())
            {
                if (!DateTime.TryParseExact(emp.FechaAlta, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaAlta))
                    continue;

                bool altaEnQuincenaAnterior = fechaAlta >= inicioAnt && fechaAlta <= finAnt;
                bool noApareceEnAnterior = !idsEnQuincenaAnterior.Contains(emp.Id.ToString());
                if (!altaEnQuincenaAnterior || !noApareceEnAnterior) continue;

                string nombreCompleto = $"{emp.Nombre} {emp.ApellidoP} {emp.ApellidoM}".Trim();
                var respuesta = MessageBox.Show(
                    $"El empleado \"{nombreCompleto}\" no se encontró en la quincena anterior.\n\n" +
                    "¿Se realizará el pago correspondiente en esta captura?",
                    "Empleado sin pago en quincena anterior",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (respuesta != DialogResult.Yes) continue;

                _empleadosNuevosSinPagoAnterior.Add(emp.Id);
                _diasMaximosSugeridos[emp.Id] = ContarDiasHabiles(fechaAlta, finAct);

                // resaltado discreto, se mantiene como recordatorio visual mientras captura
                var row = dgvMostrarPersonal.Rows.Cast<DataGridViewRow>()
                    .FirstOrDefault(r => r.Cells["NumeroEmpleado"].Value?.ToString() == emp.Id.ToString());
                if (row != null)
                {
                    row.Cells["DiasTrabajados"].Style.BackColor = Color.FromArgb(255, 243, 205);
                    row.Cells["DiasTrabajados"].ToolTipText = $"Días hábiles disponibles: {_diasMaximosSugeridos[emp.Id]}";
                }
            }
        }

        private void RegistrarPagosEspecialesSiAplica(string nombreCarpetaEmpresa, int anio)
        {
            if (_empleadosNuevosSinPagoAnterior.Count == 0) return;

            foreach (DataGridViewRow row in dgvMostrarPersonal.Rows)
            {
                if (row.IsNewRow || row.Tag?.ToString() == "Totales") continue;
                if (!int.TryParse(row.Cells["NumeroEmpleado"].Value?.ToString(), out int idEmp)) continue;
                if (!_empleadosNuevosSinPagoAnterior.Contains(idEmp)) continue;

                int diasCapturados = GetInt(row, "DiasTrabajados");
                string nombre = row.Cells["NombreEmpleado"].Value?.ToString() ?? $"Empleado {idEmp}";
                int diasMax = _diasMaximosSugeridos.TryGetValue(idEmp, out int d) ? d : 15;

                ResultadoPagoEspecial resultado;
                if (diasCapturados > diasMax)
                    resultado = ResultadoPagoEspecial.Excedido;
                else if (diasCapturados > 15)
                    resultado = ResultadoPagoEspecial.Correspondiente;
                else
                    resultado = ResultadoPagoEspecial.NoRecibido; // confirmó pero no le pagó lo pendiente

                LogService.RegistrarEventoNomina(nombre, resultado, nombreCarpetaEmpresa, anio);

                if (resultado == ResultadoPagoEspecial.Excedido)
                    _ = EmailService.NotificarPagoExcedidoAsync(Environment.MachineName, _datosEmpresa?.nombre ?? "EMPRESA", nombre);
            }
        }

        private void chkBoxCE_Leave(object sender, EventArgs e)
        {
            if (!chkBoxCE.Checked)
                return;

            if (!TryObtenerExentaGlobal(out _))
            {
                MessageBox.Show("Introduce una cantidad exenta válida, mayor o igual a cero y con máximo dos decimales.", "Cantidad exenta inválida", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            AplicarExentaGlobal();
        }

        private void dgvMostrarPersonal_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            DataGridViewRow fila = dgvMostrarPersonal.Rows[e.RowIndex];

            if (fila.IsNewRow || fila.Tag?.ToString() == "Totales")
                return;

            string columna = dgvMostrarPersonal.Columns[e.ColumnIndex].Name;

            if (columna != "PercExenta")
                return;

            string texto = (e.FormattedValue?.ToString() ?? "").Trim();

            // Dejar vacío significa quitar la excepción individual y recuperar la exenta global, si está habilitada.
            if (string.IsNullOrWhiteSpace(texto))
                return;

            texto = texto.Replace("$", "").Trim();

            bool esNumero = decimal.TryParse(texto, NumberStyles.Number, CultureInfo.GetCultureInfo("es-MX"), out decimal exenta);

            if (!esNumero)
            {
                esNumero = decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out exenta);
            }

            if (!esNumero || exenta < 0m || decimal.Round(exenta, 2) != exenta)
            {
                MessageBox.Show( "Introduce una percepción exenta válida, mayor o igual a cero y con máximo dos decimales.", "Percepción exenta inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            decimal brutoAplicable = ObtenerBrutoAplicableExenta(fila);

            if (exenta > brutoAplicable)
            {
                MessageBox.Show($"La exenta capturada (${exenta:N2}) " + $"supera el importe bruto aplicable " + $"(${brutoAplicable:N2}).\n\n" + "Corrige la cantidad antes de continuar.",
                    "Percepción exenta excedida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }
    }
}
using Nomina_2026_NET8;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NOMINA_2025
{
    public partial class FormNOMCFDIPlantillas : Form
    {
        private const int COL_SEG_RFC = 5;
        private const int COL_SEG_BANDERA = 20;

        public long FolioFinal { get; private set; }
        private int _anioEmpresa;

        private long _folioAnterior;
        private string _serie;
        private string _metodoPagoDescripcion;
        private string _registroPatronal;
        private string _riesgoImss;
        private int _consecutivoNomina;

        // ── Enum ──────────────────────────────────────────────────────────
        private enum TipoPlantilla { Primera, Segunda }

        // ── Estado ────────────────────────────────────────────────────────
        private List<DatosCFDIEmpleado> _empleados;
        private bool _esExtraordinaria;
        private DateTime _fechaPago;
        private bool _esPrimeraQuincena;

        // Fuente de verdad del tipo actual — no depende del estado del control
        private TipoPlantilla _tipoActual = TipoPlantilla.Primera;

        public FormNOMCFDIPlantillas()
        {
            InitializeComponent();
        }

        private void FormNOMCFDIPlantillas_Load(object sender, EventArgs e)
        {
            tsEliminar.Visible = false;
            tsSDI.Visible = false;

            // Defensivo: garantiza el layout aunque el diseñador reseteé el Dock
            dgvPlantillas.Dock = DockStyle.Fill;
            dgvPlantillas.ColumnHeadersVisible = true;

            dgvPlantillas.DoubleBuffered(true);
            CambiarVista(TipoPlantilla.Primera);
        }

        private void FormNOMCFDIPlantillas_Shown(object sender, EventArgs e)
        {
            AjustarColumnasParaVisualizacion();
        }

        // ── Eventos ───────────────────────────────────────────────────────
        private void tsPrimeraPlantilla_Click(object sender, EventArgs e) => CambiarVista(TipoPlantilla.Primera);
        private void tsSegundaPlantilla_Click(object sender, EventArgs e) => CambiarVista(TipoPlantilla.Segunda);
        private void tsSeleccionarYCopiarTodoPrimera_Click(object sender, EventArgs e) => CopiarGridComoTexto(dgvPlantillas, colInicio: 0, colFin: dgvPlantillas.Columns.Count - 1);
        private void tsSeleccionarYCopiarTodoSegunda_Click(object sender, EventArgs e) => CopiarGridComoTexto(dgvPlantillas, colInicio: COL_SEG_RFC, colFin: COL_SEG_BANDERA);

        // ── API pública ───────────────────────────────────────────────────        

        public void LlenarDesdeCaptura(List<DatosCFDIEmpleado> empleados, bool esExtraordinaria, DateTime fechaPago, bool esPrimeraQuincena, int anioEmpresa, DatosInicioCfdiNomina datosInicio)
        {
            _empleados = empleados;
            _esExtraordinaria = esExtraordinaria;
            _fechaPago = fechaPago;
            _esPrimeraQuincena = esPrimeraQuincena;
            _anioEmpresa = anioEmpresa;

            _folioAnterior = datosInicio.FolioAnterior;
            _serie = datosInicio.Serie ?? "";
            _metodoPagoDescripcion = datosInicio.MetodoPagoDescripcion ?? "";
            _registroPatronal = datosInicio.RegistroPatronal ?? "";
            _riesgoImss = datosInicio.RiesgoImss ?? "";
            _consecutivoNomina = datosInicio.Consecutivo;
        }

        // ── Métodos privados ──────────────────────────────────────────────

        // Reemplaza los dos handlers + CambiarPlantilla(TipoPlantilla) 
        private void CambiarVista(TipoPlantilla tipo)
        {
            _tipoActual = tipo;
            bool esPrimera = tipo == TipoPlantilla.Primera;

            ConfigurarColumnas(esPrimera ? ColumnasPrimeraPlantilla : ColumnasSegundaPlantilla);

            tsPrimeraPlantilla.Checked = esPrimera;
            tsSegundaPlantilla.Checked = !esPrimera;

            tsSeleccionarYCopiarTodoPrimera.Visible = esPrimera;
            tsSeleccionarYCopiarTodoSegunda.Visible = !esPrimera;

            if (_empleados?.Count > 0)
                LlenarGrid(); // ya llama AjustarColumnasParaVisualizacion() internamente
            else
                AjustarColumnasParaVisualizacion(); // solo encabezados
        }

        private void ConfigurarColumnas(string[] columnas)
        {
            dgvPlantillas.Columns.Clear();
            dgvPlantillas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            foreach (var nombre in columnas)
            {
                int indice = dgvPlantillas.Columns.Add(nombre, nombre);

                // Desactiva completamente el ordenamiento al hacer clic
                // sobre el encabezado.
                dgvPlantillas.Columns[indice].SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        private void CopiarGridComoTexto(DataGridView grid, int colInicio, int colFin)
        {
            var sb = new StringBuilder();

            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow) continue; // evita la fila fantasma de "nueva fila"

                for (int f = colInicio; f <= colFin; f++)
                {
                    sb.Append(row.Cells[f].Value?.ToString() ?? string.Empty);
                    if (f < colFin)
                        sb.Append('\t');
                }
                sb.Append("\r\n");
            }

            Clipboard.SetText(sb.ToString(), TextDataFormat.Text); // texto plano, sin formato
            MessageBox.Show("Contenido copiado en el portapapeles.");
        }

        private void LlenarGrid()
        {
            if (_empleados == null || _empleados.Count == 0)
                return;

            dgvPlantillas.Rows.Clear();

            bool esPrimera = _tipoActual == TipoPlantilla.Primera;

            long folio = _folioAnterior;

            foreach (var e in _empleados)
            {
                // VB6: Folio = Folio + 1 ANTES de MdAbr_1
                folio++;

                decimal grav = e.Gravado;

                string sFechaPago = _fechaPago.ToString("yyyy-MM-dd");
                string sFechaInicio = FechaInicialQuincenal(_fechaPago);
                string sFechaAlta = ConvertirFechaCFDI(e.FechaAlta);

                int antiguedad = CalcularAntiguedadVB6(e.FechaAlta, _anioEmpresa, _fechaPago.Month, _esPrimeraQuincena);

                string numCuenta = e.BANAMEX?.Length >= 4 ? e.BANAMEX[^4..] : e.BANAMEX ?? "";
                string tipoNomina = _esExtraordinaria ? "EXTRAORDINARIA" : "ORDINARIA";
                string periodicidad = _esExtraordinaria ? "99 OTRA PERIODICIDAD" : "04 QUINCENAL";

                int diasPagados = _esExtraordinaria ? 1 : e.DiasTrabajados;
                int rowIdx = dgvPlantillas.Rows.Add();
                var row = dgvPlantillas.Rows[rowIdx];

                if (esPrimera)
                {
                    LlenarPrimeraPlantilla(
                        row,
                        folio,
                        e,
                        grav,
                        sFechaPago,
                        sFechaInicio,
                        sFechaAlta,
                        antiguedad,
                        numCuenta,
                        tipoNomina,
                        periodicidad,
                        diasPagados);
                }
                else
                {
                    LlenarSegundaPlantilla(row, e);
                }
            }

            FolioFinal = folio;

            AjustarColumnasParaVisualizacion();
        }

        private void LlenarPrimeraPlantilla(DataGridViewRow row, long folio, DatosCFDIEmpleado e, decimal grav, string sFechaPago, string sFechaInicio, string sFechaAlta,
            int antiguedad, string numCuenta, string tipoNomina, string periodicidad, int diasPagados)
        {
            decimal tNeto = e.Neto;
            decimal tPer = e.TotalIngresos;
            decimal tDed = e.TotalDeducciones;
            decimal sub13 = e.Subsidio;
            decimal isr12 = e.ISR;

            // ── Bloque 1: Encabezado CFDI ─────────────────────────────
            Set(row, "Folio", folio);
            Set(row, "Serie", _serie);
            Set(row, "Nombre", e.Nombre);
            Set(row, "Dirección", e.Direccion ?? "");
            Set(row, "Colonia", e.Colonia ?? "");
            Set(row, "Ciudad", e.Ciudad ?? "");
            Set(row, "Estado", e.Estado ?? "");
            Set(row, "Delegación", e.Delegacion ?? "");
            Set(row, "CP", (e.CodigoPostal ?? "").PadLeft(5, '0'));
            Set(row, "RFC", e.RFC ?? "");
            Set(row, "Pais", "MEXICO");
            Set(row, "Correo", e.Correo ?? "");
            Set(row, "Observaciones", "");
            Set(row, "Moneda", "PESOS");
            Set(row, "TipoDeCambio", Fmt(1m));
            Set(row, "Total", Fmt(tNeto));
            Set(row, "Subtotal", Fmt(tPer));
            Set(row, "Descuento", Fmt(tDed));
            Set(row, "TotalGravadoPercepciones", Fmt(grav));
            Set(row, "TotalExcentoPercepciones", Fmt(e.Exento));
            Set(row, "TotalPercepciones", Fmt(tPer - sub13));
            Set(row, "TotalDeducciones", Fmt(tDed));
            Set(row, "TotalOtrosPagos", Fmt(sub13));
            Set(row, "TotalSueldos", Fmt(tPer - sub13));
            Set(row, "TotalSeparacionIndemnizacion", Fmt(0m));
            Set(row, "TotalJubilacionPensionRetiro", Fmt(0m));
            Set(row, "TotalOtrasDeducciones", Fmt(e.OtrasDeducciones));
            Set(row, "TotalImpuestosRetenidos", Fmt(isr12));
            Set(row, "ValorUnitario", Fmt(tPer));
            Set(row, "Importe", Fmt(tPer));

            // Usa NumerosEnLetras.Convertir en lugar del método local
            Set(row, "TotalConLetra", NumerosEnLetras.Convertir(tNeto));

            Set(row, "TipoDeNomina", tipoNomina);
            Set(row, "Sindicalizado", "No");
            Set(row, "FormaPago", _metodoPagoDescripcion);
            Set(row, "LugarExpedicion", "DIF");
            Set(row, "Regimen", "601");
            Set(row, "UsoCFDI", "G03 Gastos en general");
            Set(row, "NumCtaPag", numCuenta);
            Set(row, "RegistroPatronal", _registroPatronal);
            Set(row, "NumEmpleado", e.NumeroEmpleado);
            Set(row, "CURP", e.CURP ?? "");

            // Nombres sin sufijo SAT — consistente con EmpleadoCompleto
            Set(row, "TipoRegimen", e.TipoRegimen ?? "02 Sueldos");
            Set(row, "NumSeguridadSocial", e.NSS ?? "");
            Set(row, "FechaPago", sFechaPago);
            Set(row, "FechaInicialPago", sFechaInicio);
            Set(row, "FechaFinalPago", sFechaPago);
            Set(row, "NumDiasPagados", diasPagados);
            Set(row, "Departamento", e.Departamento ?? "ADMINISTRACION");
            Set(row, "CLABE", "");
            Set(row, "Banco", "002");
            Set(row, "FechaInicioRelLaboral", sFechaAlta);
            Set(row, "Antiguedad", antiguedad);
            Set(row, "Puesto", e.Puesto ?? "Administracion");
            Set(row, "TipoContrato", e.TipoContrato ?? "01 Contrato de trabajo por tiempo indeterminado");
            Set(row, "TipoJornada", e.TipoJornada ?? "01 Diurna");
            Set(row, "PeriocidadPago", e.PeriodicidadPago ?? periodicidad);
            Set(row, "SalarioBaseCotaPor", Fmt(e.SalarioDiario));
            Set(row, "RiesgoPuesto", _riesgoImss);
            Set(row, "SalarioDiarioIntegrado", Fmt(e.SalarioDiarioInteg));
            Set(row, "EntidadFederativa", "CMX");
            Set(row, "RFCLabora", e.RFCLabora ?? "");
            Set(row, "PorcentajeTiempo", 100);

            // ── Bloque 2: Incapacidades (todos cero) ─────────────────
            SetZero(row, "Dincapacidadr", "Dincapacidadi", "Dincapacidadm", "Discapacidadc", "P014DINCAR", "P014DINCAI", "P014DINCAM", "P014DINCAC", "DIASDOM",
                "DIASHE", "HORASDOBLES", "DIASHETRIPLES");

            // ── Bloque 3: Campos auxiliares (todos cero) ─────────────
            SetZero(row, "HORASTRIPLES", "DIASHESIMPLES", "HORASSIMPLES", "AT_VMERCADO", "AT_POTORGARSE", "JB_TEXHIBICION", "JB_TPARCIAL", "JB_MDIARIO", "JB_IACUM",
                "JB_INOACUM", "SI_TPAGADO", "SI_ASERVICIO", "SI_USUELDO", "SI_IACUM", "SI_INOACUM", "CSF_SALFAV", "CSF_ANIO", "CSF_RSFAV");

            Set(row, "SE_SCAUSADO", Fmt(e.SubsidioCausado));
            Set(row, "UUID_DOCREL", "");
            SetZero(row, "H2TOT", "H3TOT", "HSTOT");
            Set(row, "BANDERA", 1);
        }

        private void LlenarSegundaPlantilla(DataGridViewRow row, DatosCFDIEmpleado e)
        {
            Set(row, "RFC", e.RFC ?? "");
            Set(row, "P001G", Fmt(e.Salario));          // sueldos gravados

            // ── Percepciones especiales ───────────────────────────────
            if (_esExtraordinaria && e.TieneAguinaldoOriginal)
            {
                Set(row, "P002G", Fmt(e.Aguinaldo));

                if (e.TienePTUOriginal)
                    Set(row, "P002E", Fmt(0m));
                else
                    Set(row, "P002E", Fmt(e.ExentoOriginal));
            }
            else
            {
                Set(row, "P002G", Fmt(0m));
                Set(row, "P002E", Fmt(0m));
            }

            if (_esExtraordinaria && e.TienePTUOriginal)
            {
                Set(row, "P003G", Fmt(e.PTU1));
                Set(row, "P003E", Fmt(e.PTU2));
            }
            else
            {
                Set(row, "P003G", Fmt(0m));
                Set(row, "P003E", Fmt(0m));
            }

            Set(row, "P010G", Fmt(0m));

            if (_esExtraordinaria && e.OF != 0m)
            {
                Set(row, "P004G", Fmt(e.OF + e.ExentoOriginal));
            }
            else
            {
                Set(row, "P004G", Fmt(0m));
            }

            // ── P004-P020 en cero (excepto P010G ya asignado) ────────
            SetZero(row, "P005G", "P005E", "P006G", "P006E", "P009G", "P011G", "P011E", "P012G", "P012E", "P013G", "P014R", "P014I", "P014M", "P014C",
                "P015E", "P019G", "P019E", "P020G", "P020E");

            // ── P021: Prima vacacional ────────────────────────────────
            Set(row, "P021G", Fmt(e.PrimaVacacional));

            if (!_esExtraordinaria)
                Set(row, "P021E", Fmt(e.Exento));
            else
                Set(row, "P021E", Fmt(0m));

            // ── P022-P037 en cero ─────────────────────────────────────
            SetZero(row, "P022G", "P022E", "P023G", "P024G", "P024E", "P025G", "P025E", "P026G", "P026E", "P027E", "P028G", "P029E", "P030E", "P031E", "P032E", "P033E",
                "P034E", "P035E", "P036E", "P037E");

            // ── P038: Otros ingresos (viáticos + otras) ───────────────
            Set(row, "P038G", Fmt(e.Otras + e.OF));
            SetZero(row, "P038E");

            // ── P039-P055 en cero ─────────────────────────────────────
            SetZero(row, "P039E", "P044E", "P045E", "P046G", "P047G", "P047E", "P048G", "P048E", "P049G", "P050G", "P050E", "P051G", "P051E", "P052G", "P052E", "P053G",
                "P053E", "P054G", "P054E", "P055G", "P055E");

            // ── Deducciones ───────────────────────────────────────────
            Set(row, "D001", Fmt(e.IMSS));
            Set(row, "D002", Fmt(e.ISR));
            Set(row, "D003", Fmt(0m));
            Set(row, "D004", Fmt(e.Prestamos));
            SetZero(row, "D005", "D006R", "D006I", "D006M", "D006C");
            Set(row, "D007", Fmt(e.PensionAlimenticia));
            SetZero(row, "D008", "D009");
            Set(row, "D010", Fmt(e.INFONAVIT));
            Set(row, "D011", Fmt(e.FONACOT));

            // ── D012-D111 en cero ─────────────────────────────────────
            SetZero(row, "D012", "D013", "D014", "D015", "D016", "D017", "D018", "D019", "D020", "D021", "D022", "D023", "D024", "D025", "D026", "D027", "D028", "D029", "D030",
                "D031", "D032", "D033", "D034", "D035", "D036", "D037", "D038", "D039", "D040", "D041", "D042", "D043", "D044", "D045", "D046", "D047", "D048", "D049", "D050",
                "D051", "D052", "D053", "D054", "D055", "D056", "D057", "D058", "D059", "D060", "D061", "D062", "D063", "D064", "D065", "D066", "D067", "D068", "D069", "D070",
                "D071", "D072", "D073", "D074", "D075", "D076", "D077", "D078", "D079", "D080", "D081", "D082", "D083", "D084", "D085", "D086", "D087", "D088", "D089", "D090",
                "D091", "D092", "D093", "D094", "D095", "D096", "D097", "D098", "D099", "D100", "D101", "D102", "D103", "D104", "D105", "D106", "D107", "D108", "D109", "D110",
                "D111");

            // ── Otros pagos ───────────────────────────────────────────
            SetZero(row, "OP001");
            Set(row, "OP002", Fmt(e.Subsidio));   // Subsidio entregado
            SetZero(row, "OP003", "OP004", "OP005", "OP006", "OP007", "OP008", "OP009", "OP999");

            Set(row, "BANDERA", 1);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static string Fmt(decimal v) => v.ToString("#,##0.00", CultureInfo.InvariantCulture);

        private static void Set(DataGridViewRow row, string col, object val)
        {
            if (row.DataGridView.Columns.Contains(col))
                row.Cells[col].Value = val;
        }

        private static void SetZero(DataGridViewRow row, params string[] cols)
        {
            foreach (var col in cols)
                Set(row, col, Fmt(0m));
        }

        private static string FechaInicialQuincenal(DateTime fecha)
        {
            int diaInicio = fecha.Day < 16 ? 1 : 16;
            return new DateTime(fecha.Year, fecha.Month, diaInicio).ToString("yyyy-MM-dd");
        }

        private static string ConvertirFechaCFDI(string fechaDDMMYYYY)
        {
            if (DateTime.TryParseExact(fechaDDMMYYYY, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                return dt.ToString("yyyy-MM-dd");
            return "";
        }

        // ── Arrays de columnas ────────────────────────────────────────────

        private static readonly string[] ColumnasPrimeraPlantilla =
        {
            // ── Datos generales del CFDI ──────────────────────────────
            "Folio", "Serie", "Nombre", "Dirección", "Colonia", "Ciudad", "Estado", "Delegación", "CP", "RFC", "Pais", "Correo", "Observaciones", "Moneda",
            "TipoDeCambio", "Total", "Subtotal", "Descuento",

            // ── Totales de percepciones ───────────────────────────────
            "TotalGravadoPercepciones", "TotalExcentoPercepciones", "TotalPercepciones", "TotalDeducciones", "TotalOtrosPagos", "TotalSueldos",
            "TotalSeparacionIndemnizacion", "TotalJubilacionPensionRetiro", "TotalOtrasDeducciones", "TotalImpuestosRetenidos", "ValorUnitario", "Importe", "TotalConLetra",

            // ── Datos de la nómina ────────────────────────────────────
            "TipoDeNomina", "Sindicalizado", "FormaPago", "LugarExpedicion", "Regimen", "UsoCFDI", "NumCtaPag", "RegistroPatronal", "NumEmpleado", "CURP", "TipoRegimen",
            "NumSeguridadSocial", "FechaPago", "FechaInicialPago", "FechaFinalPago", "NumDiasPagados", "Departamento", "CLABE", "Banco", "FechaInicioRelLaboral",
            "Antiguedad", "Puesto", "TipoContrato", "TipoJornada", "PeriocidadPago", "SalarioBaseCotaPor", "RiesgoPuesto", "SalarioDiarioIntegrado", "EntidadFederativa",
            "RFCLabora", "PorcentajeTiempo",

            // ── Incapacidades ─────────────────────────────────────────
            "Dincapacidadr", "Dincapacidadi", "Dincapacidadm", "Discapacidadc", "P014DINCAR", "P014DINCAI", "P014DINCAM", "P014DINCAC", "DIASDOM", "DIASHE", "HORASDOBLES",
            "DIASHETRIPLES",

            // ── Campos auxiliares ─────────────────────────────────────
            "HORASTRIPLES", "DIASHESIMPLES", "HORASSIMPLES", "AT_VMERCADO", "AT_POTORGARSE", "JB_TEXHIBICION", "JB_TPARCIAL", "JB_MDIARIO", "JB_IACUM", "JB_INOACUM",
            "SI_TPAGADO", "SI_ASERVICIO", "SI_USUELDO", "SI_IACUM", "SI_INOACUM", "CSF_SALFAV", "CSF_ANIO", "CSF_RSFAV", "SE_SCAUSADO", "UUID_DOCREL", "H2TOT", "H3TOT",
            "HSTOT", "BANDERA"
        };

        private static readonly string[] ColumnasSegundaPlantilla =
        {
            // ── Identificación ────────────────────────────────────────
            "RFC",

            // ── Percepciones (P001-P055) ──────────────────────────────
            "P001G",
            "P002G","P002E",                               // Aguinaldo
            "P003G","P003E",                               // PTU
            "P004G","P004E","P005G","P005E", "P006G","P006E","P009G",
            "P010G",                                       // Bono
            "P011G","P011E","P012G","P012E","P013G", "P014R","P014I","P014M","P014C", "P015E","P019G","P019E","P020G","P020E",
            "P021G","P021E",                               // Prima vacacional
            "P022G","P022E","P023G","P024G","P024E", "P025G","P025E","P026G","P026E","P027E", "P028G","P029E","P030E","P031E","P032E", "P033E","P034E","P035E","P036E","P037E",
            "P038G","P038E",                               // Otros ingresos
            "P039E","P044E","P045E","P046G", "P047G","P047E","P048G","P048E","P049G", "P050G","P050E","P051G","P051E", "P052G","P052E","P053G","P053E", "P054G","P054E","P055G","P055E",

            // ── Deducciones (D001-D111) ───────────────────────────────
            "D001",                                        // IMSS
            "D002",                                        // ISR
            "D003","D004",                                 // Otros/Préstamos
            "D005","D006R","D006I","D006M","D006C",
            "D007",                                        // Pensión alimenticia
            "D008","D009",
            "D010",                                        // INFONAVIT
            "D011",                                        // FONACOT
            "D012","D013","D014","D015","D016","D017","D018","D019","D020", "D021","D022","D023","D024","D025","D026","D027","D028","D029","D030", "D031","D032","D033",
            "D034","D035","D036","D037","D038","D039","D040", "D041","D042","D043","D044","D045","D046","D047","D048","D049","D050", "D051","D052","D053","D054","D055",
            "D056","D057","D058","D059","D060", "D061","D062","D063","D064","D065","D066","D067","D068","D069","D070", "D071","D072","D073","D074","D075","D076","D077",
            "D078","D079","D080", "D081","D082","D083","D084","D085","D086","D087","D088","D089","D090", "D091","D092","D093","D094","D095","D096","D097","D098","D099",
            "D100", "D101","D102","D103","D104","D105","D106","D107","D108","D109","D110", "D111",

            // ── Otros pagos (OP001-OP999) ─────────────────────────────
            "OP001",
            "OP002",                                       // Subsidio entregado
            "OP003","OP004","OP005","OP006","OP007","OP008","OP009","OP999", "BANDERA"
        };

        private static int CalcularAntiguedadVB6(string fechaAlta, int anioEmpresa, int mesNomina, bool esPrimeraQuincena)
        {
            try
            {
                int dIngr = 0;
                int mIngr = 0;
                int yIngr = 0;

                if (!string.IsNullOrWhiteSpace(fechaAlta) &&
                    fechaAlta.Length >= 10)
                {
                    int.TryParse(fechaAlta.Substring(0, 2), out dIngr);
                    int.TryParse(fechaAlta.Substring(3, 2), out mIngr);
                    int.TryParse(fechaAlta.Substring(6, 4), out yIngr);
                }

                if (yIngr < 1900)
                    return 1;

                int dCalc = esPrimeraQuincena ? 15 : 30;
                int mCalc = mesNomina;
                int yCalc = anioEmpresa;

                if (yCalc < 1900)
                    yCalc = DateTime.Today.Year;

                int antig = yCalc - yIngr;

                if (mCalc < mIngr)
                {
                    antig--;
                }
                else if (mCalc == mIngr && dCalc < dIngr)
                {
                    antig--;
                }

                antig++;

                if (antig < 1)
                    antig = 1;

                return antig;
            }
            catch
            {
                return 1;
            }
        }

        private void AjustarColumnasParaVisualizacion()
        {
            if (dgvPlantillas.Columns.Count == 0) return;

            dgvPlantillas.SuspendLayout();

            dgvPlantillas.ColumnHeadersVisible = true;

            // TEMPORAL — fuente 100% garantizada del sistema, para descartar problema con "Century"
            var fuenteSegura = new Font("Segoe UI", 12F, FontStyle.Bold);
            dgvPlantillas.ColumnHeadersDefaultCellStyle.Font = fuenteSegura;
            dgvPlantillas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvPlantillas.ColumnHeadersDefaultCellStyle.BackColor = Color.Gray;
            dgvPlantillas.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvPlantillas.EnableHeadersVisualStyles = false;

            using (Graphics g = dgvPlantillas.CreateGraphics())
            {
                Font cellFont = dgvPlantillas.DefaultCellStyle.Font ?? dgvPlantillas.Font;

                foreach (DataGridViewColumn col in dgvPlantillas.Columns)
                {
                    int headerWidth = TextRenderer.MeasureText(g, col.HeaderText, fuenteSegura).Width;

                    int maxCellWidth = 0;
                    foreach (DataGridViewRow row in dgvPlantillas.Rows)
                    {
                        if (row.IsNewRow) continue;
                        string valor = row.Cells[col.Index].Value?.ToString() ?? "";
                        int w = TextRenderer.MeasureText(g, valor, cellFont).Width;
                        if (w > maxCellWidth) maxCellWidth = w;
                    }

                    int anchoFinal = Math.Max(headerWidth, maxCellWidth) + 25;
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.MinimumWidth = 60;
                    col.Width = Math.Max(anchoFinal, 60);
                    //col.Resizable = DataGridViewTriState.True;
                }
            }

            dgvPlantillas.ColumnHeadersHeight = 80;

            dgvPlantillas.ResumeLayout();
            dgvPlantillas.Refresh();
        }
    }
}

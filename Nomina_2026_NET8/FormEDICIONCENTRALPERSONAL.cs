using Nomina_2026_NET8;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NOMINA_2025
{
    public partial class FormEDICIONCENTRALPERSONAL : Form
    {
        // ── Márgenes de fecha ─────────────────────────────────────────────
        private static readonly TimeSpan MARGEN_FECHA_INGRESO = TimeSpan.FromDays(45);
        private static readonly TimeSpan MARGEN_FECHA_BAJA = TimeSpan.FromDays(45);

        // ── Estado ────────────────────────────────────────────────────────
        private List<EmpleadoCompleto> _empleados;
        private List<EmpleadoCompleto> _empleadosFiltrados;
        private Dictionary<Label, Control> _mapaCampos;
        private RuepService _personalService;
        private Dictionary<Control, string> _originalValues = new();

        private bool _mostrarConBaja = false;
        private bool _mostrarSinBaja = true;
        private EstadoUI _estadoActual;

        public string RutaRUEP { get; set; }
        public bool HuboCambios { get; private set; }

        // ── Flags de control ─────────────────────────────────────────────
        private bool _autogenerando = false;
        private bool _recalculando = false;
        private bool _cambiandoCorreo = false;
        private bool _emailEmpresaAutogenerado = false;
        private bool _modoCapturaBaja = false;
        private bool _fechaBajaPendienteConfirmacion = false;

        // UMA desde RUEP — no hardcodeada
        private decimal UmaDiaria => _personalService?.DatosEmpresa?.uma_por_dia ?? 117.31m;

        // ── Calculadores ──────────────────────────────────────────────────
        private TablasTarifaRoot _tablasISR;
        private CalculadoraISR _calcISR;

        // ── Constantes para cálculos ──────────────────────────────────────
        private const int DECIMALES_CURRENCY_VB6 = 4;

        private enum EstadoUI { Lectura, VerCFDI, Edicion }

        public FormEDICIONCENTRALPERSONAL()
        {
            InitializeComponent();
        }

        private void FormEDICIONCENTRALPERSONAL_Load(object sender, EventArgs e)
        {
            DateTime hoy = DateTime.Now.Date;
            InicializarMapaCampos();
            chckbCorreoEmpresa.Enabled = false;
            chckbCorreoPersonal.Enabled = false;

            if (string.IsNullOrEmpty(RutaRUEP) || !System.IO.File.Exists(RutaRUEP))
            {
                MessageBox.Show("No se encontró el archivo RUEP.dat en el directorio seleccionado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            dtpFechaIngreso.MinDate = new DateTime(1940, 1, 1);
            dtpFechaIngreso.MaxDate = DateTime.Today.AddYears(1);
            dtpFechaIngreso.Value = hoy;
            dtpFechaBaja.Value = hoy;

            lblAntiguedadDelEmpleado.Text = "0 días";

            MostrarCampoFechaBaja(false, false);
            ConfigurarRangoFechaBaja();
            _modoCapturaBaja = false;

            _personalService = new RuepService(RutaRUEP);
            InicializarCalculadores();

            _empleados = _personalService.ObtenerTodos();
            _empleadosFiltrados = new List<EmpleadoCompleto>(_empleados);

            HabilitarCamposPersonales(true);
            InicializarDgvEditPersonal();
            CambiarEstadoUI(EstadoUI.Lectura);

            AplicarToUpperRecursivo(this);
            AplicarCursorInicioRecursivo(this);

            tsMostrarSinBaja.Checked = true;
            tsMostrarConBaja.Checked = false;
            tsMostrarAmbos.Checked = false;

            AplicarFiltros();
            AplicarRestriccionesCampos();
            ConfigurarTabIndex();
            ActualizarEstadoBotonBaja();
            ActualizarEstadoBotonReingreso();
        }

        // ── Eventos de botones ────────────────────────────────────────────
        private void btnReingresoEmpleado_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtIDActual.Text, out int id))
            {
                MessageBox.Show("Seleccione un empleado válido para reingresar.", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var emp = _empleados.FirstOrDefault(x => x.Id == id);

            if (emp == null)
            {
                MessageBox.Show(
                    "No se encontró el empleado seleccionado.",
                    "Empleado no encontrado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            if (!EmpleadoTieneBaja(emp))
            {
                MessageBox.Show(
                    "Este empleado no tiene baja registrada, por lo tanto no aplica reingreso.",
                    "Reingreso no aplicable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            string nombreCompleto = $"{emp.ApellidoP} {emp.ApellidoM} {emp.Nombre}".Trim();

            DialogResult fechaRespuesta = MessageBox.Show(
                $"Se realizará el reingreso del siguiente empleado:\n\n" +
                $"ID: {emp.Id}\n" +
                $"Nombre: {nombreCompleto}\n\n" +
                $"Fecha de alta original: {emp.FechaAlta}\n" +
                $"Fecha de baja vigente: {FechaBajaVigente(emp)}\n\n" +
                "¿Deseas usar la fecha de hoy como fecha de reingreso?\n\n" +
                "Sí = usar fecha de hoy\n" +
                "No = seleccionar una fecha específica\n" +
                "Cancelar = no realizar reingreso",
                "Reingresar empleado",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1
            );

            if (fechaRespuesta == DialogResult.Cancel)
                return;

            DateTime fechaReingreso;

            if (fechaRespuesta == DialogResult.Yes)
            {
                fechaReingreso = DateTime.Today;
            }
            else
            {
                DateTime? fechaSeleccionada = PedirFechaReingreso(DateTime.Today);

                if (!fechaSeleccionada.HasValue)
                    return;

                fechaReingreso = fechaSeleccionada.Value.Date;
            }

            DialogResult antiguedadRespuesta = MessageBox.Show(
                "¿Deseas respetar la antigüedad original del empleado?\n\n" +
                "Sí = conservar antigüedad desde la primera fecha de alta\n" +
                "No = calcular antigüedad desde la fecha de reingreso\n" +
                "Cancelar = no realizar reingreso",
                "Antigüedad del reingreso",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1
            );

            if (antiguedadRespuesta == DialogResult.Cancel)
                return;

            bool respetaAntiguedad = antiguedadRespuesta == DialogResult.Yes;

            DialogResult confirmar = MessageBox.Show(
                $"Confirma el reingreso:\n\n" +
                $"Empleado: {nombreCompleto}\n" +
                $"Fecha de reingreso: {fechaReingreso:dd/MM/yyyy}\n" +
                $"Respeta antigüedad: {(respetaAntiguedad ? "Sí" : "No")}\n\n" +
                "La fecha de alta original y la fecha de baja original se conservarán.",
                "Confirmar reingreso",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question
            );

            if (confirmar != DialogResult.OK)
                return;

            ReingresarEmpleado(emp, fechaReingreso, respetaAntiguedad);
        }

        private void btnGuardarCambios_Click(object sender, EventArgs e)
        {
            RestaurarLabels();

            if (!ValidarFormulario()) return;

            if (IntentoBorrarDatosGenerales())
            {
                MessageBox.Show("No es posible eliminar los Datos Generales del empleado.\n\nPara dar de baja al empleado, utilice el botón 'Dar de Baja'.",
                    "Acción no permitida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (DatosGeneralesInvalidos())
            {
                MessageBox.Show("El Nombre y los Apellidos no pueden eliminarse.", "Datos obligatorios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (TodosCamposVacios())
            {
                MessageBox.Show("No es posible guardar un registro completamente vacío.", "Registro vacío", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtIDActual.Text, out int id))
            {
                MessageBox.Show("ID inválido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool esNuevo = !_empleados.Any(x => x.Id == id);

            if (esNuevo)
            {
                var faltantesAlta = ValidarRequeridosAlta();
                if (faltantesAlta.Count > 0)
                {
                    MostrarErroresEnLabels(faltantesAlta);
                    MessageBox.Show("No se puede dar de alta. Faltan datos indispensables:\n\n• " + string.Join("\n• ", faltantesAlta),
                        "Datos obligatorios faltantes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var duplicados = ValidarDuplicados(mskdRFC.Text.Trim().ToUpper(), mskdCURP.Text.Trim().ToUpper(), mskdNSS.Text.Trim());

                if (duplicados.Count > 0)
                {
                    MessageBox.Show("No se puede dar de alta. Ya existe un empleado con:\n\n• " + string.Join("\n• ", duplicados), "Registro duplicado",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // ── Campos informativos (no bloquean) ─────────────────────────
            var faltantesInfo = esNuevo ? ObtenerCamposInformativos() : new List<string>();
            if (faltantesInfo.Count > 0)
            {
                MostrarAdvertenciasEnLabels(faltantesInfo);
                var res = MessageBox.Show("Los siguientes datos están incompletos y podrán capturarse después:\n\n• " + string.Join("\n• ", faltantesInfo) +
                    "\n\n¿Deseas continuar con el alta de todas formas?", "Datos incompletos", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

                if (res == DialogResult.No) return;
            }
            else
            {
                if (MessageBox.Show("¿Deseas guardar los cambios realizados?", "Confirmación", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            }

            // ── Persistir cambios ──────────────────────────────────────────
            var emp = _empleados.FirstOrDefault(x => x.Id == id);
            if (emp == null)
            {
                emp = new EmpleadoCompleto { Id = id };
                _empleados.Add(emp);
            }

            AplicarCambiosAlEmpleado(emp, esNuevo);
            _personalService.GuardarCambios();
            HuboCambios = true;

            CaptureOriginalValues();
            MessageBox.Show(esNuevo ? "Empleado agregado correctamente" : "Cambios guardados correctamente", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            RestaurarLabels();
            HabilitarCamposPersonales(true);
            AplicarFiltros();
            SeleccionarEmpleadoEnGrid(id);

            _modoCapturaBaja = false;
            _fechaBajaPendienteConfirmacion = false;

            CambiarEstadoUI(EstadoUI.Lectura);
            ActualizarEstadoBotonReingreso();
        }

        private void btnCancelarCambios_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Cancelar cambios? No se guardará ninguna modificación.", "Cancelar", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;

            RestaurarLabels();
            ConfigurarRangoHistorico();

            if (dgvEditPersonal.SelectedRows.Count > 0)
                dgvEditPersonal_SelectionChanged(null, null);

            MessageBox.Show("No se realizó ningún cambio.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _modoCapturaBaja = false;
            _fechaBajaPendienteConfirmacion = false;
            CambiarEstadoUI(EstadoUI.Lectura);
            ActualizarEstadoBotonBaja();
            ActualizarEstadoBotonReingreso();

            if (int.TryParse(txtIDActual.Text, out int idCancel))
            {
                var empCancel = _empleados.FirstOrDefault(x => x.Id == idCancel);
                MostrarCampoFechaBaja(empCancel != null && EmpleadoTieneBaja(empCancel), false);
            }
            else
            {
                MostrarCampoFechaBaja(false, false);
            }
        }

        private void btnMostrarCFDI_Click(object sender, EventArgs e)
        {
            txtBuscarPersonal.Enabled = false;
            btnBuscarEnDGV.Enabled = false;

            CambiarEstadoUI(_estadoActual == EstadoUI.Lectura ? EstadoUI.VerCFDI : EstadoUI.Lectura);
        }

        private void btnModificarRegistro_Click(object sender, EventArgs e)
        {
            ConfigurarTabIndex();
            HabilitarCamposPersonales(false);
            CambiarEstadoUI(EstadoUI.Edicion);
            CaptureOriginalValues();
        }

        private void btnAgregarEmpleado_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Deseas Agregar un nuevo Registro?\n\nLos Campos serán puestos en blanco para su llenado manual.", "Agregar Nuevo Registro",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            LimpiarFormulario();
            ConfigurarRangoNuevoIngreso();
            HabilitarCamposPersonales(false);

            txtIDActual.Text = ObtenerSiguienteId().ToString();

            _modoCapturaBaja = false;
            MostrarCampoFechaBaja(false, false);

            ConfigurarTabIndex();
            CambiarEstadoUI(EstadoUI.Edicion);

            txtNombre?.Focus();
            CaptureOriginalValues();
            ActualizarEstadoBotonBaja();

            btnReingresoEmpleado.Visible = false;
            btnReingresoEmpleado.Enabled = false;
        }

        private void btnBuscarEnDGV_Click(object sender, EventArgs e) => AplicarFiltros();

        private void btnDarDeBajaEmpleado_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtIDActual.Text, out int id))
            {
                MessageBox.Show("Seleccione un empleado válido para dar de baja.", "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var emp = _empleados.FirstOrDefault(x => x.Id == id);

            if (emp == null)
            {
                MessageBox.Show("No es posible dar de baja un registro nuevo que aún no ha sido guardado.", "Registro no válido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EmpleadoEstaActivo(emp))
            {
                MessageBox.Show("Este empleado ya se encuentra dado de baja.", "Empleado ya dado de baja", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Segundo clic: confirmar fecha específica previamente seleccionada.
            if (_fechaBajaPendienteConfirmacion)
            {
                DialogResult confirmarFecha = MessageBox.Show($"Se asignará como fecha de baja el día {dtpFechaBaja.Value:dd/MM/yyyy}.\n\n" +
                    "El registro no se borrará, solamente se asignará una fecha de baja para no aparecer en las próximas quincenas.\n\n" + "¿Deseas confirmar esta fecha de baja?",
                    "Confirmar fecha de baja", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                if (confirmarFecha != DialogResult.Yes)
                    return;

                PrepararBajaConfirmada(dtpFechaBaja.Value.Date, editable: false);
                return;
            }

            DialogResult respuesta = MessageBox.Show("El registro no se borrará, solamente se asignará una fecha de baja para no aparecer en las próximas quincenas.\n\n" +
                "¿Deseas usar la fecha del día de hoy como fecha de baja?\n\nSí = usar fecha de hoy\nNo = seleccionar una fecha específica\nCancelar = no realizar baja",
                "Dar de baja empleado", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

            if (respuesta == DialogResult.Cancel)
            {
                _modoCapturaBaja = false;
                _fechaBajaPendienteConfirmacion = false;
                ActualizarEstadoBotonBaja();
                return;
            }

            if (respuesta == DialogResult.Yes)
            {
                PrepararBajaConfirmada(DateTime.Today, editable: false);
                return;
            }

            // Fecha específica: todavía NO se confirma baja.
            // El botón permanece visible para confirmar después de elegir fecha.
            _modoCapturaBaja = false;
            _fechaBajaPendienteConfirmacion = true;

            ConfigurarRangoFechaBaja();

            dtpFechaBaja.ShowCheckBox = false;
            dtpFechaBaja.Value = DateTime.Today;

            MostrarCampoFechaBaja(true, true);

            btnDarDeBajaEmpleado.Visible = true;
            btnDarDeBajaEmpleado.Enabled = true;

            dtpFechaBaja.Focus();

            MessageBox.Show("Selecciona la fecha de baja y vuelve a presionar el botón de baja para confirmar.", "Seleccionar fecha de baja", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ── Eventos de grid y búsqueda ────────────────────────────────────
        private void dgvEditPersonal_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvEditPersonal.SelectedRows.Count == 0) return;

            var row = dgvEditPersonal.SelectedRows[0];
            if (row.Cells["IdArchivo"].Value == null) return;

            int id = Convert.ToInt32(row.Cells["IdArchivo"].Value);
            var emp = _empleados.FirstOrDefault(x => x.Id == id);
            if (emp == null) return;

            CargarEmpleadoEnControles(emp);

            if (_estadoActual != EstadoUI.Edicion)
                CaptureOriginalValues();

            ActualizarEstadoBotonBaja();
            ActualizarEstadoBotonReingreso();
        }

        private void txtBuscarPersonal_TextChanged(object sender, EventArgs e) => AplicarFiltros();

        // ── Eventos de toolbar (ordenamiento / filtros) ───────────────────
        private void tsAscendente_Click(object sender, EventArgs e)
        {
            _empleadosFiltrados = _empleadosFiltrados.OrderBy(emp => emp.ApellidoP).ThenBy(emp => emp.ApellidoM).ThenBy(emp => emp.Nombre).ToList();
            CargarDgvDesdeLista(_empleadosFiltrados);
        }


        private void tsDescendente_Click(object sender, EventArgs e)
        {
            _empleadosFiltrados = _empleadosFiltrados.OrderByDescending(emp => emp.ApellidoP).ThenByDescending(emp => emp.ApellidoM).ThenByDescending(emp => emp.Nombre).ToList();
            CargarDgvDesdeLista(_empleadosFiltrados);
        }

        private void tsMenorAMayor_Click(object sender, EventArgs e)
        {
            _empleadosFiltrados = _empleadosFiltrados.OrderBy(emp => emp.Id).ToList();
            CargarDgvDesdeLista(_empleadosFiltrados);
        }

        private void tsMayorAMenor_Click(object sender, EventArgs e)
        {
            _empleadosFiltrados = _empleadosFiltrados.OrderByDescending(emp => emp.Id).ToList();
            CargarDgvDesdeLista(_empleadosFiltrados);
        }

        private void tsMasAntiguos_Click(object sender, EventArgs e)
        {
            OrdenarPorAntiguedad(masAntiguosPrimero: true);
        }

        private void tsMasRecientes_Click(object sender, EventArgs e)
        {
            OrdenarPorAntiguedad(masAntiguosPrimero: false);
        }

        private void tsMostrarConBaja_Click(object sender, EventArgs e)
        {
            _mostrarConBaja = true; _mostrarSinBaja = false;
            tsMostrarConBaja.Checked = true;
            tsMostrarSinBaja.Checked = tsMostrarAmbos.Checked = false;
            AplicarFiltros();
        }

        private void tsMostrarSinBaja_Click(object sender, EventArgs e)
        {
            _mostrarConBaja = false; _mostrarSinBaja = true;
            tsMostrarSinBaja.Checked = true;
            tsMostrarConBaja.Checked = tsMostrarAmbos.Checked = false;
            AplicarFiltros();
        }

        private void tsMostrarAmbos_Click(object sender, EventArgs e)
        {
            _mostrarConBaja = _mostrarSinBaja = true;
            tsMostrarAmbos.Checked = true;
            tsMostrarConBaja.Checked = tsMostrarSinBaja.Checked = false;
            AplicarFiltros();
        }

        // ── Eventos de cambio de texto ────────────────────────────────────
        private void txtNombre_TextChanged(object sender, EventArgs e)
        {
            ConvertirMayusculas(sender, e);
            AutoGenerarClaves();
        }

        private void txtApellidoPaterno_TextChanged(object sender, EventArgs e)
        {
            ConvertirMayusculas(sender, e);
            AutoGenerarClaves();
        }

        private void txtApellidoMaterno_TextChanged(object sender, EventArgs e)
        {
            ConvertirMayusculas(sender, e);
            AutoGenerarClaves();
        }

        private void ChckbCorreo_CheckedChanged(object sender, EventArgs e)
        {
            if (_cambiandoCorreo) return;
            _cambiandoCorreo = true;

            if (sender == chckbCorreoEmpresa && chckbCorreoEmpresa.Checked)
                chckbCorreoPersonal.Checked = false;
            else if (sender == chckbCorreoPersonal && chckbCorreoPersonal.Checked)
                chckbCorreoEmpresa.Checked = false;

            if (!chckbCorreoEmpresa.Checked && !chckbCorreoPersonal.Checked)
                chckbCorreoEmpresa.Checked = true;

            _cambiandoCorreo = false;
        }

        // ── Métodos privados principales ──────────────────────────────────
        // Formatea un ingreso diario igual que VB6: sin decimales si es entero, con los decimales que tenga si no lo es. Sin relleno de ceros.
        // Ejemplos: 450 → "450", 450.23 → "450.23", 1266.67 → "1266.67"

        private static decimal RedondearCurrencyVB6(decimal valor)
        {
            return Math.Round(valor, DECIMALES_CURRENCY_VB6, MidpointRounding.AwayFromZero);
        }

        private static string NormalizarNumeroDecimal(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "";

            string s = texto.Trim().Replace("$", "").Replace(" ", "");

            int lastDot = s.LastIndexOf('.');
            int lastComma = s.LastIndexOf(',');

            if (lastDot >= 0 && lastComma >= 0)
            {
                // Si trae ambos, el último separador se toma como decimal.
                s = lastComma > lastDot ? s.Replace(".", "").Replace(',', '.') : s.Replace(",", "");
            }
            else if (lastComma >= 0 && lastDot < 0)
            {
                // Solo coma: tratarla como decimal.
                s = s.Replace(",", ".");
            }
            else
            {
                // Solo punto o ninguno.
                s = s.Replace(",", "");
            }

            return s;
        }

        private static bool TryParseCurrencyVB6(string texto, out decimal valor)
        {
            valor = 0m;

            string normalizado = NormalizarNumeroDecimal(texto);

            if (string.IsNullOrWhiteSpace(normalizado))
                return true;

            if (!decimal.TryParse(normalizado, NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out valor))
            {
                return false;
            }

            valor = RedondearCurrencyVB6(valor);
            return true;
        }

        private static decimal ParseCurrencyVB6(string texto)
        {
            return TryParseCurrencyVB6(texto, out decimal valor) ? valor : 0m;
        }

        // Para campos editables: muestra hasta 4 decimales, sin rellenar ceros.
        private static string FormatearIngreso(decimal valor)
        {
            valor = RedondearCurrencyVB6(valor);

            if (valor == 0m)
                return "";

            return valor.ToString("0.####", CultureInfo.InvariantCulture);
        }

        // Centraliza la carga de un empleado en los controles del Form. Elimina la duplicación entre dgvEditPersonal_SelectionChanged y otras partes del Form.
        private void CargarEmpleadoEnControles(EmpleadoCompleto emp)
        {
            txtIDActual.Text = emp.Id.ToString();
            txtNombre.Text = emp.Nombre;
            txtApellidoPaterno.Text = emp.ApellidoP;
            txtApellidoMaterno.Text = emp.ApellidoM;
            mskdRFC.Text = emp.RFC;
            mskdCURP.Text = emp.CURP;
            mskdNSS.Text = emp.NSS;

            ConfigurarRangoHistorico();

            if (DateTime.TryParseExact(emp.FechaAlta, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fa))
                dtpFechaIngreso.Value = fa;

            ActualizarAntiguedadEmpleado();

            string fechaBajaMostrar = FechaBajaVigente(emp);

            if (EmpleadoTieneBaja(emp) && !string.IsNullOrWhiteSpace(fechaBajaMostrar) && DateTime.TryParseExact(fechaBajaMostrar, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime fb))
            {
                dtpFechaBaja.ShowCheckBox = false;
                dtpFechaBaja.MinDate = new DateTime(1990, 1, 1);
                dtpFechaBaja.MaxDate = DateTime.Today.AddDays(45);
                dtpFechaBaja.Value = fb;

                MostrarCampoFechaBaja(true, false);
            }
            else
            {
                dtpFechaBaja.ShowCheckBox = false;
                dtpFechaBaja.Value = DateTime.Today;
                dtpFechaBaja.Checked = false;

                MostrarCampoFechaBaja(false, false);
            }

            txtIngresoDiarioNormal.Text = FormatearIngreso(emp.SalarioDiario);
            txtViaticosDiarios.Text = FormatearIngreso(emp.Viaticos);
            txtOtrosDiarios.Text = FormatearIngreso(emp.Otras);

            txtCP.Text = emp.CP ?? "";
            txtEstado.Text = emp.Estado ?? "";
            txtMunicipio.Text = emp.Municipio ?? "";
            txtColonia.Text = emp.Colonia ?? "";
            txtCorreoElectronicoPersonal.Text = emp.Email ?? "";
            txtEmailEmpresa.Text = emp.EmailEmpresa ?? "";
            txtEstadoDomicilio.Text = emp.EstadoDomicilio ?? "";
            txtEstadoContDomicilio.Text = emp.EstadoContDomicilio ?? "";
            mskdBANAMEX.Text = emp.Banamex ?? "";
            txtReferencias.Text = emp.Referencias ?? "";
            mskdtxtNoExterior.Text = emp.NoExterior ?? "";
            mskdtxtNoInterior.Text = emp.NoInterior ?? "";
            mskdTelFijo.Text = emp.TelefonoFijo ?? "";
            mskdTelMovil.Text = emp.TelefonoMovil ?? "";
            mskdtxtLADATel.Text = (emp.LADA ?? "").Replace("+", "");
            txtEmailEmpresa.Text = emp.EmailEmpresa ?? "";
            txtTelEmpr.Text = emp.TelefonoEmpresa ?? "";
            txtExt.Text = emp.Extension ?? "";
            txtActividadEcon.Text = emp.ActividadEconomica ?? "";
            txtRegimen.Text = emp.RegimenFiscal ?? "";

            // ── Ciudad / Departamento / Puesto / Riesgo IMSS / Link SAT ──────
            // Ya existían en EmpleadoCompleto pero no estaban conectados a controles.
            txtCiudad.Text = emp.Ciudad ?? "";
            txtDepartamento.Text = emp.Departamento ?? "";
            txtPuesto.Text = emp.Puesto ?? "";
            txtRiesgoIMSS.Text = emp.RiesgoIMSS ?? "";
            linklblSAT.Text = string.IsNullOrWhiteSpace(emp.LinkSAT) ? "" : emp.LinkSAT;
            linklblSAT.Tag = emp.LinkSAT ?? "";

            chckbCorreoEmpresa.Checked = emp.CorreoEmpresaPredeterminado;
            chckbCorreoPersonal.Checked = !emp.CorreoEmpresaPredeterminado;

            lblFechaUltimaMod.Text = string.IsNullOrWhiteSpace(emp.FechaUltimaMod) ? "Sin modificaciones registradas" : $"{emp.FechaUltimaMod}  —  {emp.ModificadoPor}";

            // Un solo método para recalcular y mostrar SDI/ISR
            RecalcularSDIeISR();
        }

        // Aplica los valores de los controles al objeto EmpleadoCompleto. Extraído de btnGuardarCambios para mayor claridad.
        private void AplicarCambiosAlEmpleado(EmpleadoCompleto emp, bool esNuevo)
        {
            string fechaAltaAnterior = emp.FechaAlta ?? "";
            string fechaAltaNueva = dtpFechaIngreso.Value.ToString("dd/MM/yyyy");

            emp.Nombre = txtNombre.Text.Trim();
            emp.ApellidoP = txtApellidoPaterno.Text.Trim();
            emp.ApellidoM = txtApellidoMaterno.Text.Trim();
            emp.RFC = mskdRFC.Text.Trim();
            emp.CURP = mskdCURP.Text.Trim();
            emp.NSS = mskdNSS.Text.Trim();
            emp.FechaAlta = fechaAltaNueva;

            if (esNuevo)
            {
                emp.EsReingreso = false;
                emp.FechaReingresoAlta = "";
                emp.FechaReingresoBaja = "";
                emp.RespetaAntiguedad = true;
                emp.EstatusLaboral = "ACTIVO";
            }

            if (_modoCapturaBaja)
            {
                string fechaBaja = dtpFechaBaja.Value.ToString("dd/MM/yyyy");

                if (emp.EsReingreso)
                {
                    emp.FechaReingresoBaja = fechaBaja;
                    emp.EstatusLaboral = "REINGRESO_BAJA";
                }
                else
                {
                    emp.FechaBaja = fechaBaja;
                    emp.EstatusLaboral = "BAJA";
                }
            }
            else if (string.IsNullOrWhiteSpace(emp.EstatusLaboral))
            {
                emp.EstatusLaboral = string.IsNullOrWhiteSpace(emp.FechaBaja) ? "ACTIVO" : "BAJA";
            }

            decimal salario = ParseCurrencyVB6(txtIngresoDiarioNormal.Text);
            decimal viaticos = ParseCurrencyVB6(txtViaticosDiarios.Text);
            decimal otros = ParseCurrencyVB6(txtOtrosDiarios.Text);

            emp.SalarioDiario = salario;
            emp.Viaticos = viaticos;
            emp.Otras = otros;

            // Recalcular SDI/ISR si ingresos o fecha de alta cambiaron.
            bool fechaIngresoCambio = !string.Equals(fechaAltaAnterior, fechaAltaNueva, StringComparison.Ordinal);

            bool datosBaseSDICambiaron = esNuevo || fechaIngresoCambio || ValorCambio(txtIngresoDiarioNormal) || ValorCambio(txtViaticosDiarios) || ValorCambio(txtOtrosDiarios);

            if (datosBaseSDICambiaron)
            {
                decimal totalDiario = RedondearCurrencyVB6(salario + viaticos + otros);

                int anioFiscal = _personalService?.DatosEmpresa?.ano_fiscal ?? DateTime.Today.Year;

                // VB6 Form alta:
                // antiguedad = empresa.ao - anoIngreso
                // If antiguedad < 1 Then antiguedad = 1
                int antiguedadAlta = ConstantesNomina.AntiguedadAltaEmpleadoVB6(dtpFechaIngreso.Value, anioFiscal);

                decimal factor = ConstantesNomina.FactorSDIPorAnioBase(antiguedadAlta);

                // VB6:
                // Text12.Text = Format((totalIngr * facto), z1$)
                decimal sdi = Math.Round(totalDiario * factor, 2, MidpointRounding.AwayFromZero);
                decimal tope = Math.Round(25m * UmaDiaria, 2, MidpointRounding.AwayFromZero);

                emp.SalarioIntegrado = Math.Min(sdi, tope);
                emp.ISR = 0m;

                if (_calcISR != null)
                {
                    try
                    {
                        var resultado = _calcISR.CalcularSegundaQuincena(totalDiario * 30m);
                        emp.ISR = Math.Max(resultado.ImpuestoCalculado, 0m);
                    }
                    catch
                    {
                        emp.ISR = 0m;
                    }
                }

                txtSalarioDiarioIntegrado.Text = emp.SalarioIntegrado.ToString("N2");
                txtISRMensual.Text = emp.ISR.ToString("N2");
            }

            emp.CP = txtCP.Text.Trim();
            emp.Estado = txtEstado.Text.Trim();
            emp.Municipio = txtMunicipio.Text.Trim();
            emp.Colonia = txtColonia.Text.Trim();
            emp.Email = txtCorreoElectronicoPersonal.Text.Trim();
            emp.Referencias = txtReferencias.Text.Trim();
            emp.NoExterior = mskdtxtNoExterior.Text.Trim();
            emp.NoInterior = mskdtxtNoInterior.Text.Trim();
            emp.EstadoDomicilio = txtEstadoDomicilio.Text.Trim();
            emp.EstadoContDomicilio = txtEstadoContDomicilio.Text.Trim();
            emp.TelefonoFijo = mskdTelFijo.Text.Trim();
            emp.TelefonoMovil = mskdTelMovil.Text.Trim();
            emp.LADA = mskdtxtLADATel.Text.Trim();
            emp.EmailEmpresa = txtEmailEmpresa.Text.Trim();
            emp.TelefonoEmpresa = txtTelEmpr.Text.Trim();
            emp.Extension = txtExt.Text.Trim();
            emp.ActividadEconomica = txtActividadEcon.Text.Trim();
            emp.RegimenFiscal = txtRegimen.Text.Trim();
            emp.Banamex = mskdBANAMEX.Text.Trim();

            // ── Ciudad / Departamento / Puesto / Riesgo IMSS / Link SAT ──────
            emp.Ciudad = txtCiudad.Text.Trim();
            emp.Departamento = txtDepartamento.Text.Trim();
            emp.Puesto = txtPuesto.Text.Trim();
            emp.RiesgoIMSS = txtRiesgoIMSS.Text.Trim();
            emp.LinkSAT = (linklblSAT.Tag?.ToString() ?? linklblSAT.Text)?.Trim() ?? "";

            emp.CorreoEmpresaPredeterminado = chckbCorreoEmpresa.Checked;
            emp.FechaUltimaMod = DateTime.Now.ToString("dd/MM/yyyy");
            emp.ModificadoPor = Environment.UserName;
        }

        // Unifica RecalcularSalarios + RecalcularYMostrar en un solo método
        private void RecalcularSDIeISR()
        {
            if (_recalculando) return;
            _recalculando = true;

            try
            {
                decimal salario = ParseCurrencyVB6(txtIngresoDiarioNormal.Text);
                decimal viaticos = ParseCurrencyVB6(txtViaticosDiarios.Text);
                decimal otras = ParseCurrencyVB6(txtOtrosDiarios.Text);
                decimal totalDiario = RedondearCurrencyVB6(salario + viaticos + otras);

                int anioFiscal = _personalService?.DatosEmpresa?.ano_fiscal ?? DateTime.Today.Year;
                int antiguedadAlta = ConstantesNomina.AntiguedadAltaEmpleadoVB6(dtpFechaIngreso.Value, anioFiscal);

                decimal factor = ConstantesNomina.FactorSDIPorAnioBase(antiguedadAlta);
                decimal sdi = Math.Round(totalDiario * factor, 2, MidpointRounding.AwayFromZero);
                decimal tope = Math.Round(25m * UmaDiaria, 2, MidpointRounding.AwayFromZero);
                decimal integrado = Math.Min(sdi, tope);

                txtSalarioDiarioIntegrado.Text = integrado.ToString("N2");
                txtISRMensual.Text = "0.00";

                if (_calcISR != null)
                {
                    try
                    {
                        var resultado = _calcISR.CalcularSegundaQuincena(totalDiario * 30m);
                        decimal isrMensual = resultado.ImpuestoCalculado;
                        txtISRMensual.Text = isrMensual > 0m ? isrMensual.ToString("N2") : "0.00";
                    }
                    catch (InvalidOperationException)
                    {
                        txtISRMensual.Text = "0.00";
                    }
                }
            }
            finally
            {
                _recalculando = false;
            }
        }

        // Handler que conecta los TextBox de ingreso al recálculo
        private void RecalcularSalarios(object sender, EventArgs e) => RecalcularSDIeISR();

        private void InicializarCalculadores()
        {
            try
            {
                _tablasISR = TarifasRepository.Obtener();
                _calcISR = new CalculadoraISR(_tablasISR);
            }
            catch (Exception ex)
            {
                _tablasISR = null;
                _calcISR = null;
                System.Diagnostics.Debug.WriteLine("[Calculadores] " + ex.Message);
            }
        }

        // ── Grid de empleados ─────────────────────────────────────────────

        private void InicializarDgvEditPersonal()
        {
            dgvEditPersonal.SuspendLayout();
            dgvEditPersonal.ReadOnly = true;
            dgvEditPersonal.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEditPersonal.MultiSelect = false;
            dgvEditPersonal.RowHeadersVisible = false;
            dgvEditPersonal.AutoGenerateColumns = false;
            dgvEditPersonal.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            foreach (DataGridViewColumn col in dgvEditPersonal.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Resizable = DataGridViewTriState.True;
            }

            if (dgvEditPersonal.Columns.Contains("IdArchivo"))
                dgvEditPersonal.Columns["IdArchivo"].Visible = false;

            dgvEditPersonal.ResumeLayout();
        }

        private void CargarDgvDesdeLista(List<EmpleadoCompleto> lista)
        {
            dgvEditPersonal.Rows.Clear();
            foreach (var emp in lista)
                dgvEditPersonal.Rows.Add(emp.Id, emp.Id, $"{emp.ApellidoP} {emp.ApellidoM} {emp.Nombre}", emp.RFC, emp.CURP, emp.NSS);
        }

        private void SeleccionarEmpleadoEnGrid(int id)
        {
            foreach (DataGridViewRow row in dgvEditPersonal.Rows)
            {
                if (Convert.ToInt32(row.Cells["IdArchivo"].Value) == id)
                {
                    row.Selected = true;
                    break;
                }
            }
        }

        // ── Estado de la UI ───────────────────────────────────────────────

        private void CambiarEstadoUI(EstadoUI nuevoEstado)
        {
            _estadoActual = nuevoEstado;

            switch (nuevoEstado)
            {
                case EstadoUI.Lectura:
                    _modoCapturaBaja = false;
                    HabilitarCamposPersonales(true);
                    dgvEditPersonal.Visible = true;
                    panelCFDICompleto.Visible = false;
                    btnMostrarCFDI.Visible = true;
                    btnModificarRegistro.Visible = true;
                    btnGuardarCambios.Visible = false;
                    btnCancelarCambios.Visible = false;
                    txtBuscarPersonal.Enabled = true;
                    btnBuscarEnDGV.Enabled = true;
                    btnAgregarEmpleado.Enabled = true;
                    tsFiltrar.Enabled = true;
                    btnMostrarCFDI.Text = "Ver CFDI Completo";
                    break;

                case EstadoUI.VerCFDI:
                    _modoCapturaBaja = false;
                    HabilitarCamposPersonales(true);
                    dgvEditPersonal.Visible = false;
                    panelCFDICompleto.Visible = true;
                    btnMostrarCFDI.Visible = true;
                    btnModificarRegistro.Visible = true;
                    btnGuardarCambios.Visible = false;
                    btnCancelarCambios.Visible = false;
                    tsFiltrar.Enabled = false;
                    btnMostrarCFDI.Text = "Ocultar CFDI";
                    break;

                case EstadoUI.Edicion:
                    HabilitarCamposPersonales(false);
                    dgvEditPersonal.Visible = false;
                    panelCFDICompleto.Visible = true;
                    btnMostrarCFDI.Visible = false;
                    btnModificarRegistro.Visible = false;
                    btnGuardarCambios.Visible = true;
                    btnCancelarCambios.Visible = true;
                    txtBuscarPersonal.Enabled = false;
                    btnBuscarEnDGV.Enabled = false;
                    btnAgregarEmpleado.Enabled = false;
                    tsFiltrar.Enabled = false;
                    break;
            }

            // Lógica de fecha baja extraída — ya no se repite en los 3 casos
            ActualizarVisibilidadFechaBaja();
            ActualizarEstadoBotonBaja();
            ActualizarEstadoBotonReingreso();
        }

        /// Extraído de CambiarEstadoUI — eliminaba 90 líneas duplicadas.
        private void ActualizarVisibilidadFechaBaja()
        {
            if (_modoCapturaBaja || _fechaBajaPendienteConfirmacion)
                return;

            bool tieneBaja = false;
            DateTime? fechaBajaValor = null;

            if (int.TryParse(txtIDActual.Text, out int id))
            {
                var emp = _empleados.FirstOrDefault(x => x.Id == id);

                tieneBaja = emp != null && EmpleadoTieneBaja(emp);

                string fechaBajaMostrar = emp != null ? FechaBajaVigente(emp) : "";

                if (tieneBaja && !string.IsNullOrWhiteSpace(fechaBajaMostrar) && DateTime.TryParseExact(fechaBajaMostrar, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime fb))
                {
                    fechaBajaValor = fb;
                }
            }

            MostrarCampoFechaBaja(tieneBaja, false);

            if (tieneBaja && fechaBajaValor.HasValue)
            {
                dtpFechaBaja.Value = fechaBajaValor.Value;
            }
            else
            {
                dtpFechaBaja.Value = DateTime.Today;
            }
        }

        private void ActualizarEstadoBotonBaja()
        {
            if (btnDarDeBajaEmpleado == null)
                return;

            if (_estadoActual != EstadoUI.Edicion)
            {
                btnDarDeBajaEmpleado.Visible = false;
                btnDarDeBajaEmpleado.Enabled = false;
                return;
            }

            if (_modoCapturaBaja)
            {
                // Baja ya preparada. Debe obligar a Guardar o Cancelar.
                btnDarDeBajaEmpleado.Visible = false;
                btnDarDeBajaEmpleado.Enabled = false;
                return;
            }

            if (_fechaBajaPendienteConfirmacion)
            {
                // Se eligió fecha específica, pero falta confirmar.
                btnDarDeBajaEmpleado.Visible = true;
                btnDarDeBajaEmpleado.Enabled = true;
                return;
            }

            if (!int.TryParse(txtIDActual.Text, out int id))
            {
                btnDarDeBajaEmpleado.Visible = false;
                btnDarDeBajaEmpleado.Enabled = false;
                return;
            }

            var emp = _empleados.FirstOrDefault(x => x.Id == id);
            bool puedeDarBaja = emp != null && EmpleadoEstaActivo(emp);

            btnDarDeBajaEmpleado.Visible = puedeDarBaja;
            btnDarDeBajaEmpleado.Enabled = puedeDarBaja;
        }

        private void ActualizarEstadoBotonReingreso()
        {
            if (btnReingresoEmpleado == null)
                return;

            if (_estadoActual != EstadoUI.Edicion)
            {
                btnReingresoEmpleado.Visible = false;
                btnReingresoEmpleado.Enabled = false;
                return;
            }

            if (_modoCapturaBaja || _fechaBajaPendienteConfirmacion)
            {
                btnReingresoEmpleado.Visible = false;
                btnReingresoEmpleado.Enabled = false;
                return;
            }

            if (!int.TryParse(txtIDActual.Text, out int id))
            {
                btnReingresoEmpleado.Visible = false;
                btnReingresoEmpleado.Enabled = false;
                return;
            }

            var emp = _empleados.FirstOrDefault(x => x.Id == id);

            bool puedeReingresar =
                emp != null &&
                EmpleadoTieneBaja(emp);

            btnReingresoEmpleado.Visible = puedeReingresar;
            btnReingresoEmpleado.Enabled = puedeReingresar;
        }

        private void HabilitarCamposPersonales(bool soloLectura)
        {
            dtpFechaIngreso.Enabled = !soloLectura;
            txtNombre.ReadOnly = soloLectura;
            txtApellidoPaterno.ReadOnly = soloLectura;
            txtApellidoMaterno.ReadOnly = soloLectura;
            mskdRFC.ReadOnly = soloLectura;
            mskdCURP.ReadOnly = soloLectura;
            mskdNSS.ReadOnly = soloLectura;
            txtIngresoDiarioNormal.ReadOnly = soloLectura;
            txtViaticosDiarios.ReadOnly = soloLectura;
            txtOtrosDiarios.ReadOnly = soloLectura;
            txtSalarioDiarioIntegrado.ReadOnly = true;  // siempre calculado
            txtISRMensual.ReadOnly = true;  // siempre calculado
            txtReferencias.ReadOnly = soloLectura;
            mskdtxtNoExterior.ReadOnly = soloLectura;
            mskdtxtNoInterior.ReadOnly = soloLectura;
            txtCP.ReadOnly = soloLectura;
            txtEstado.ReadOnly = soloLectura;
            txtMunicipio.ReadOnly = soloLectura;
            txtColonia.ReadOnly = soloLectura;
            txtEstadoDomicilio.ReadOnly = soloLectura;
            txtEstadoContDomicilio.ReadOnly = soloLectura;
            txtCorreoElectronicoPersonal.ReadOnly = soloLectura;
            mskdTelFijo.ReadOnly = soloLectura;
            mskdTelMovil.ReadOnly = soloLectura;
            mskdtxtLADATel.ReadOnly = soloLectura;
            txtEmailEmpresa.ReadOnly = soloLectura;
            txtTelEmpr.ReadOnly = soloLectura;
            txtExt.ReadOnly = soloLectura;
            txtActividadEcon.ReadOnly = soloLectura;
            txtRegimen.ReadOnly = soloLectura;
            mskdBANAMEX.ReadOnly = soloLectura;

            // ── Ciudad / Departamento / Puesto / Riesgo IMSS ──────────────
            txtCiudad.ReadOnly = soloLectura;
            txtDepartamento.ReadOnly = soloLectura;
            txtPuesto.ReadOnly = soloLectura;
            txtRiesgoIMSS.ReadOnly = soloLectura;
            linklblSAT.Enabled = !soloLectura;

            bool hayRegistro = !string.IsNullOrWhiteSpace(txtIDActual.Text);
            chckbCorreoEmpresa.Enabled = !soloLectura && hayRegistro;
            chckbCorreoPersonal.Enabled = !soloLectura && hayRegistro;
        }

        // ── Filtros de personal ───────────────────────────────────────────

        private void AplicarFiltros()
        {
            IEnumerable<EmpleadoCompleto> query = _empleados.Where(EmpleadoValidoParaMostrar);

            if (_mostrarConBaja ^ _mostrarSinBaja)
            {
                bool buscarConBaja = _mostrarConBaja;
                query = query.Where(e => buscarConBaja ? EmpleadoTieneBaja(e) : EmpleadoEstaActivo(e));
            }

            string texto = txtBuscarPersonal.Text.Trim();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                query = query.Where(e => CoincideBusquedaEmpleado(e, texto));
            }

            _empleadosFiltrados = query.ToList();
            CargarDgvDesdeLista(_empleadosFiltrados);
        }

        private void OrdenarPorAntiguedad(bool masAntiguosPrimero)
        {
            if (_empleadosFiltrados == null)
                _empleadosFiltrados = _empleados.Where(EmpleadoValidoParaMostrar).ToList();

            _empleadosFiltrados = masAntiguosPrimero ? _empleadosFiltrados.OrderBy(emp => ObtenerFechaAltaParaOrden(emp) ?? DateTime.MaxValue).ThenBy(emp => emp.ApellidoP).ThenBy(emp => emp.ApellidoM)
                .ThenBy(emp => emp.Nombre).ToList() : _empleadosFiltrados.OrderByDescending(emp => ObtenerFechaAltaParaOrden(emp) ?? DateTime.MinValue).ThenBy(emp => emp.ApellidoP)
                .ThenBy(emp => emp.ApellidoM).ThenBy(emp => emp.Nombre).ToList();

            CargarDgvDesdeLista(_empleadosFiltrados);
        }

        private static DateTime? ObtenerFechaAltaParaOrden(EmpleadoCompleto emp)
        {
            if (emp == null || string.IsNullOrWhiteSpace(emp.FechaAlta))
                return null;

            if (DateTime.TryParseExact(emp.FechaAlta.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaAlta))
            {
                return fechaAlta;
            }

            return null;
        }

        private static bool EmpleadoEstaActivo(EmpleadoCompleto emp)
        {
            if (emp == null)
                return false;

            string estatus = (emp.EstatusLaboral ?? "").Trim().ToUpperInvariant();

            if (estatus == "ACTIVO" || estatus == "REINGRESO_ACTIVO")
                return true;

            if (estatus == "BAJA" || estatus == "REINGRESO_BAJA")
                return false;

            // Compatibilidad con RUEP antiguos.
            return string.IsNullOrWhiteSpace(emp.FechaBaja);
        }

        private static bool EmpleadoTieneBaja(EmpleadoCompleto emp)
        {
            return !EmpleadoEstaActivo(emp);
        }

        private static string FechaBajaVigente(EmpleadoCompleto emp)
        {
            if (emp == null)
                return "";

            if (emp.EsReingreso)
                return emp.FechaReingresoBaja ?? "";

            return emp.FechaBaja ?? "";
        }

        private bool EmpleadoValidoParaMostrar(EmpleadoCompleto e) => !string.IsNullOrWhiteSpace(e.Nombre) && !string.IsNullOrWhiteSpace(e.ApellidoP);

        // ── Validaciones ──────────────────────────────────────────────────

        private bool ValidarFormulario()
        {
            var errores = new StringBuilder();

            bool Cambio(Control c) => !string.Equals(_originalValues.ContainsKey(c) ? (_originalValues[c] ?? "") : "", (c.Text ?? "").Trim(), StringComparison.Ordinal);

            string rfc = GetControlValue(mskdRFC).ToUpper();
            string curp = GetControlValue(mskdCURP).ToUpper();
            string telMov = GetControlValue(mskdTelMovil);
            string telFij = GetControlValue(mskdTelFijo);
            string clabe = GetControlValue(mskdBANAMEX);
            string nss = GetControlValue(mskdNSS);
            string emailP = txtCorreoElectronicoPersonal.Text.Trim();
            string emailE = txtEmailEmpresa.Text.Trim();

            if (Cambio(mskdRFC) && !EsVacioOGuion(rfc) && !ValidarRFC(rfc))
                errores.AppendLine("• RFC inválido o incompleto.");

            if (Cambio(mskdCURP) && !EsVacioOGuion(curp) && !ValidarCURP(curp))
                errores.AppendLine("• CURP inválida o incompleta.");

            if (Cambio(mskdNSS) && !EsVacioOGuion(nss) && nss.Length < 11)
                errores.AppendLine("• NSS incompleto (requiere 11 dígitos).");

            if (Cambio(mskdTelMovil) && !EsVacioOGuion(telMov) &&
                !ValidarTelefono(telMov))
                errores.AppendLine("• Teléfono móvil incompleto (requiere 10 dígitos).");

            if (Cambio(mskdTelFijo) && !EsVacioOGuion(telFij) &&
                !ValidarTelefono(telFij))
                errores.AppendLine("• Teléfono fijo incompleto (requiere 10 dígitos).");

            if (Cambio(mskdBANAMEX) && !EsVacioOGuion(clabe) &&
                !ValidarNumTarjeta(clabe))
                errores.AppendLine("• Tarjeta Banamex incompleta (requiere 16 dígitos).");

            if (Cambio(txtCorreoElectronicoPersonal) && !EsVacioOGuion(emailP) &&
                !ValidarEmail(emailP))
                errores.AppendLine("• Correo personal con formato inválido.");

            if (Cambio(txtEmailEmpresa) && !EsVacioOGuion(emailE) &&
                !ValidarEmail(emailE))
                errores.AppendLine("• Correo empresa con formato inválido.");

            if (Cambio(txtIngresoDiarioNormal) && !EsVacioOGuion(txtIngresoDiarioNormal.Text) && !TryParseCurrencyVB6(txtIngresoDiarioNormal.Text, out _))
            {
                errores.AppendLine("• Ingreso diario normal inválido.");
            }

            if (Cambio(txtViaticosDiarios) && !EsVacioOGuion(txtViaticosDiarios.Text) && !TryParseCurrencyVB6(txtViaticosDiarios.Text, out _))
            {
                errores.AppendLine("• Viáticos diarios inválidos.");
            }

            if (Cambio(txtOtrosDiarios) && !EsVacioOGuion(txtOtrosDiarios.Text) && !TryParseCurrencyVB6(txtOtrosDiarios.Text, out _))
            {
                errores.AppendLine("• Otros diarios fijos inválidos.");
            }

            if (Cambio(txtTelEmpr) && !EsVacioOGuion(txtTelEmpr.Text) && !Regex.IsMatch(txtTelEmpr.Text, @"^\d{10}$"))
                errores.AppendLine("• Teléfono empresa inválido (10 dígitos).");

            if (!string.IsNullOrWhiteSpace(txtExt.Text) && !Regex.IsMatch(txtExt.Text, @"^\d{1,5}$"))
                errores.AppendLine("• Extensión inválida (máximo 5 dígitos).");

            // CP — validación con mensaje que antes estaba incompleta
            if (Cambio(txtCP) && !EsVacioOGuion(txtCP.Text) && !Regex.IsMatch(txtCP.Text, @"^\d{5}$"))
                errores.AppendLine("• Código Postal inválido (requiere 5 dígitos).");

            if (errores.Length > 0)
            {
                MessageBox.Show(errores.ToString(), "Errores de validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private List<string> ValidarRequeridosAlta()
        {
            var faltan = new List<string>();

            void Checar(Control ctrl, Label lbl)
            {
                if (string.IsNullOrWhiteSpace(GetControlValue(ctrl)))
                    faltan.Add(lbl.Text.Replace("* ", "").Trim());
            }

            Checar(txtNombre, lblNombres);
            Checar(txtApellidoPaterno, lblApellidoPaterno);
            Checar(txtApellidoMaterno, lblApellidoMaterno);
            Checar(mskdCURP, lblCURP);
            Checar(mskdRFC, lblRFC);
            Checar(txtIngresoDiarioNormal, lblIngresoDiarioNormal);

            return faltan;
        }

        private List<string> ObtenerCamposInformativos()
        {
            var faltantes = new List<string>();

            void Checar(Control ctrl, Label lbl)
            {
                if (string.IsNullOrWhiteSpace(GetControlValue(ctrl)))
                    faltantes.Add(lbl.Text.Replace("* ", "").Trim());
            }

            Checar(mskdNSS, lblNSS);
            Checar(txtMunicipio, lblMunicipio);
            Checar(txtCP, lblCP);
            Checar(mskdtxtNoExterior, lblNoExterior);
            Checar(txtCorreoElectronicoPersonal, lblEmailPersonal);

            if (string.IsNullOrWhiteSpace(GetControlValue(mskdTelFijo)) && string.IsNullOrWhiteSpace(GetControlValue(mskdTelMovil)))
                faltantes.Add("Teléfono (fijo o móvil)");

            return faltantes;
        }

        private List<string> ValidarDuplicados(string rfc, string curp, string nss)
        {
            var conflictos = new List<string>();

            foreach (var emp in _empleados)
            {
                if (!string.IsNullOrWhiteSpace(rfc) && string.Equals(emp.RFC, rfc, StringComparison.OrdinalIgnoreCase))
                    conflictos.Add($"RFC '{rfc}' ya existe → " + $"{emp.ApellidoP} {emp.ApellidoM} {emp.Nombre}".Trim());

                if (!string.IsNullOrWhiteSpace(curp) && string.Equals(emp.CURP, curp, StringComparison.OrdinalIgnoreCase))
                    conflictos.Add($"CURP '{curp}' ya existe → " + $"{emp.ApellidoP} {emp.ApellidoM} {emp.Nombre}".Trim());

                if (!string.IsNullOrWhiteSpace(nss) && string.Equals(emp.NSS, nss, StringComparison.OrdinalIgnoreCase))
                    conflictos.Add($"NSS '{nss}' ya existe → " + $"{emp.ApellidoP} {emp.ApellidoM} {emp.Nombre}".Trim());
            }

            string claveEmpresa = BDSyncService.ResolverClaveEmpresa(_personalService?.DatosEmpresa?.nombre);

            if (!string.IsNullOrWhiteSpace(claveEmpresa) && BDSyncService.TestConexion(out _))
                conflictos.AddRange(BDSyncService.VerificarDuplicadoEnBD(claveEmpresa, rfc, curp));

            return conflictos;
        }

        // ── Labels de error y advertencia ────────────────────────────────

        private void InicializarMapaCampos()
        {
            _mapaCampos = new Dictionary<Label, Control>
            {
                { lblNombres,             txtNombre },
                { lblApellidoPaterno,     txtApellidoPaterno },
                { lblApellidoMaterno,     txtApellidoMaterno },
                { lblRFC,                 mskdRFC },
                { lblCURP,                mskdCURP },
                { lblNSS,                 mskdNSS },
                { lblIngresoDiarioNormal, txtIngresoDiarioNormal },
                { lblViaticosDiarios,     txtViaticosDiarios },
                { lblOtrosDiariosFijos,   txtOtrosDiarios },
                { lblSalarioDiarioIntegrado, txtSalarioDiarioIntegrado },
                { lblMunicipio,           txtMunicipio },
                { lblNoExterior,          mskdtxtNoExterior },
                { lblNoInterior,          mskdtxtNoInterior },
                { lblColonia,             txtColonia },
                { lblCP,                  txtCP },
                { lblReferencias,         txtReferencias },
                { lblTelefonoMovilPersonal, mskdTelMovil },
                { lblTelefonoFijoPersonal,  mskdTelFijo },
                { lblTelefonoEmpresa,     txtTelEmpr },
                { lblExtensionEmpresa,    txtExt },
                { lblLADA,                mskdtxtLADATel },
                { lblActividadEconomica,  txtActividadEcon },
                { lblRegimen,             txtRegimen },
                { lblEmailPersonal,       txtCorreoElectronicoPersonal },
                { lblEmailEmpresa,        txtEmailEmpresa },
                { lblBanamex,             mskdBANAMEX },
            };

            foreach (var ctrl in _mapaCampos.Values)
            {
                ctrl.TextChanged -= Control_TextChanged_QuitarError;
                ctrl.TextChanged += Control_TextChanged_QuitarError;
            }
        }

        // Separados: errores (rojo) vs advertencias (naranja)
        private void MostrarErroresEnLabels(List<string> campos) => MarcarLabels(campos, Color.Red);
        private void MostrarAdvertenciasEnLabels(List<string> campos) => MarcarLabels(campos, Color.DarkOrange);

        private void MarcarLabels(List<string> campos, Color color)
        {
            foreach (var campo in campos)
            {
                var pair = _mapaCampos.FirstOrDefault(kv => kv.Key.Text.Replace("* ", "").Trim().Equals(campo, StringComparison.OrdinalIgnoreCase));

                if (!pair.Equals(default(KeyValuePair<Label, Control>)))
                {
                    pair.Key.Text = "* " + pair.Key.Text.Replace("* ", "");
                    pair.Key.ForeColor = color;
                }
            }
        }

        private void RestaurarLabels()
        {
            foreach (var lbl in _mapaCampos.Keys)
            {
                lbl.ForeColor = SystemColors.ControlText;
                lbl.Text = lbl.Text.Replace("* ", "");
            }
        }

        private void Control_TextChanged_QuitarError(object sender, EventArgs e)
        {
            var ctrl = sender as Control;
            if (ctrl == null) return;

            var pair = _mapaCampos.FirstOrDefault(kv => kv.Value == ctrl);
            if (!pair.Equals(default(KeyValuePair<Label, Control>)))
            {
                pair.Key.ForeColor = SystemColors.ControlText;
                pair.Key.Text = pair.Key.Text.Replace("* ", "");
            }
        }

        // ── Snapshot para detección de cambios ────────────────────────────

        private void CaptureOriginalValues()
        {
            _originalValues.Clear();
            foreach (var kv in _mapaCampos)
                _originalValues[kv.Value] = (kv.Value.Text ?? "").Trim();
            _originalValues[txtIDActual] = (txtIDActual.Text ?? "").Trim();
        }

        // Helper para comparar valor actual vs original
        private bool ValorCambio(Control c) => !string.Equals(_originalValues.ContainsKey(c) ? (_originalValues[c] ?? "") : "", (c.Text ?? "").Trim(), StringComparison.Ordinal);
        private bool TodosCamposVacios() => _mapaCampos.Values.All(c => string.IsNullOrWhiteSpace(c.Text));

        // ── Autogeneración de claves ──────────────────────────────────────

        private void AutoGenerarClaves()
        {
            if (_estadoActual != EstadoUI.Edicion || _autogenerando) return;
            _autogenerando = true;

            string prefijo = GenerarRFCBasico(txtNombre.Text, txtApellidoPaterno.Text, txtApellidoMaterno.Text);

            if (prefijo.Length == 4)
            {
                ActualizarPrefijo(mskdRFC, prefijo);
                ActualizarPrefijo(mskdCURP, prefijo);
            }

            _autogenerando = false;
        }

        private void AutoGenerarEmailEmpresa()
        {
            if (_estadoActual != EstadoUI.Edicion) return;
            if (!int.TryParse(txtIDActual.Text, out int id)) return;
            if (_empleados.Any(x => x.Id == id)) return; // solo en alta nueva

            string primerNombre = LimpiarTextoEmail(txtNombre.Text.Trim().Split(' ').FirstOrDefault() ?? "");
            string apP = LimpiarTextoEmail(txtApellidoPaterno.Text.Trim().Split(' ').FirstOrDefault() ?? "");

            if (string.IsNullOrWhiteSpace(primerNombre) || string.IsNullOrWhiteSpace(apP)) return;

            string emailGenerado = $"{primerNombre}.{apP}@grupo-sacmag.com.mx";
            string actual = txtEmailEmpresa.Text.Trim().ToLower();
            bool campoVacio = string.IsNullOrWhiteSpace(actual);
            bool tieneGenerado = _emailEmpresaAutogenerado && actual.EndsWith("@grupo-sacmag.com.mx");

            if (!campoVacio && !tieneGenerado) return;
            if (string.Equals(actual, emailGenerado.ToLower(), StringComparison.OrdinalIgnoreCase)) return;

            txtEmailEmpresa.Text = emailGenerado;
            _emailEmpresaAutogenerado = true;

            MessageBox.Show($"Se generó automáticamente el correo empresarial:\n\n" + $"   {emailGenerado}\n\nVerifique que sea el correo oficial asignado antes de guardar.\n" +
                "Puede editarlo directamente en el campo.", "Correo empresarial generado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string GenerarRFCBasico(string nombre, string apP, string apM)
        {
            nombre = LimpiarTexto(nombre);
            apP = LimpiarTexto(apP);
            apM = LimpiarTexto(apM);

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apP) || string.IsNullOrWhiteSpace(apM)) return "";

            string nombreUsado = nombre.Split(' ')[0];
            if (nombreUsado is "JOSE" or "MARIA")
            {
                var partes = nombre.Split(' ');
                if (partes.Length > 1) nombreUsado = partes[1];
            }

            return $"{apP[0]}{ObtenerVocalInterna(apP)}{apM[0]}{nombreUsado[0]}";
        }

        private static string ObtenerVocalInterna(string texto)
        {
            foreach (char c in texto.Skip(1))
                if ("AEIOU".Contains(c)) return c.ToString();
            return "X";
        }

        private static string LimpiarTexto(string texto) => texto.ToUpper().Trim().Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U");

        private static string LimpiarTextoEmail(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            return Regex.Replace(texto.ToLower().Trim().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ü", "u").Replace("ñ", "n"), @"[^a-z0-9]", "");
        }

        private void ActualizarPrefijo(MaskedTextBox mtb, string nuevoPrefijo)
        {
            string actual = mtb.Text ?? "";
            string resto = actual.Length > 4 ? actual[4..] : "";
            string nuevo = nuevoPrefijo + resto;

            if (mtb.Text == nuevo) return;

            mtb.Text = nuevo;
            mtb.SelectionStart = Math.Min(nuevoPrefijo.Length, mtb.Text.Length);
            mtb.SelectionLength = 0;
        }

        // ── Helpers de formulario ─────────────────────────────────────────

        private void LimpiarFormulario()
        {
            // ── Datos generales ───────────────────────────────────────────────
            txtIDActual.Text = "";
            txtNombre.Text = "";
            txtApellidoPaterno.Text = "";
            txtApellidoMaterno.Text = "";

            mskdRFC.Text = "";
            mskdCURP.Text = "";
            mskdNSS.Text = "";

            // ── Fechas ────────────────────────────────────────────────────────
            dtpFechaIngreso.Value = DateTime.Today;
            dtpFechaBaja.Value = DateTime.Today;
            dtpFechaBaja.Checked = false;

            _modoCapturaBaja = false;

            // Nuevo empleado: solo mostrar Fecha de Alta en la posición baja.
            MostrarCampoFechaBaja(false, false);
            ActualizarAntiguedadEmpleado();

            // ── Ingresos ──────────────────────────────────────────────────────
            txtIngresoDiarioNormal.Text = "";
            txtViaticosDiarios.Text = "";
            txtOtrosDiarios.Text = "";
            txtSalarioDiarioIntegrado.Text = "0.00";
            txtISRMensual.Text = "0.00";

            // ── Domicilio ─────────────────────────────────────────────────────
            txtCP.Text = "";
            txtEstado.Text = "";
            txtMunicipio.Text = "";
            txtColonia.Text = "";
            txtReferencias.Text = "";
            txtDireccionCompleta.Text = "";

            mskdtxtNoExterior.Text = "";
            mskdtxtNoInterior.Text = "";

            txtEstadoDomicilio.Text = "";
            txtEstadoContDomicilio.Text = "";

            // ── Contacto personal ─────────────────────────────────────────────
            txtCorreoElectronicoPersonal.Text = "";
            mskdTelFijo.Text = "";
            mskdTelMovil.Text = "";
            mskdtxtLADATel.Text = "";

            // ── Contacto empresa ──────────────────────────────────────────────
            txtEmailEmpresa.Text = "";
            txtTelEmpr.Text = "";
            txtExt.Text = "";

            // ── Fiscales ──────────────────────────────────────────────────────
            txtActividadEcon.Text = "";
            txtRegimen.Text = "";

            // ── Banco ─────────────────────────────────────────────────────────
            mskdBANAMEX.Text = "";

            // ── Ciudad / Departamento / Puesto / Riesgo IMSS / Link SAT ──────
            txtCiudad.Text = "";
            txtDepartamento.Text = "";
            txtPuesto.Text = "";
            txtRiesgoIMSS.Text = "";
            linklblSAT.Text = "";
            linklblSAT.Tag = "";

            // ── Preferencias / estado visual ──────────────────────────────────
            chckbCorreoEmpresa.Checked = true;
            chckbCorreoPersonal.Checked = false;

            lblFechaUltimaMod.Text = "Sin modificaciones registradas";
            _emailEmpresaAutogenerado = false;

            RestaurarLabels();
        }

        private int ObtenerSiguienteId() => _empleados.Count == 0 ? 1 : _empleados.Max(e => e.Id) + 1;

        private bool IntentoBorrarDatosGenerales()
        {
            int vacios = 0;
            if (string.IsNullOrWhiteSpace(txtNombre.Text)) vacios++;
            if (string.IsNullOrWhiteSpace(txtApellidoPaterno.Text)) vacios++;
            if (string.IsNullOrWhiteSpace(txtApellidoMaterno.Text)) vacios++;
            if (string.IsNullOrWhiteSpace(mskdRFC.Text)) vacios++;
            if (string.IsNullOrWhiteSpace(mskdCURP.Text)) vacios++;
            if (string.IsNullOrWhiteSpace(mskdNSS.Text)) vacios++;
            return vacios >= 2;
        }

        private bool DatosGeneralesInvalidos() => string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtApellidoPaterno.Text) ||
            string.IsNullOrWhiteSpace(txtApellidoMaterno.Text);

        private void MostrarCampoFechaBaja(bool visible, bool editable = false)
        {
            if (labelFechaIngreso == null || dtpFechaIngreso == null || lblFechaBaja == null || dtpFechaBaja == null)
                return;

            labelFechaIngreso.Visible = true;
            dtpFechaIngreso.Visible = true;

            if (visible)
            {
                labelFechaIngreso.Location = new Point(1036, 50);
                dtpFechaIngreso.Location = new Point(1170, 47);

                lblFechaBaja.Location = new Point(1056, 78);
                dtpFechaBaja.Location = new Point(1170, 76);

                lblFechaBaja.Visible = true;
                dtpFechaBaja.Visible = true;
                dtpFechaBaja.Enabled = editable;
            }
            else
            {
                labelFechaIngreso.Location = new Point(1036, 78);
                dtpFechaIngreso.Location = new Point(1170, 76);

                lblFechaBaja.Visible = false;
                dtpFechaBaja.Visible = false;
                dtpFechaBaja.Enabled = false;
            }
        }

        private void ConfigurarRangoFechaBaja()
        {
            DateTime hoy = DateTime.Today;

            dtpFechaBaja.MinDate = hoy - MARGEN_FECHA_BAJA;
            dtpFechaBaja.MaxDate = hoy + MARGEN_FECHA_BAJA;
            dtpFechaBaja.Value = hoy;
        }

        private void ConfigurarRangoNuevoIngreso()
        {
            DateTime hoy = DateTime.Today;
            dtpFechaIngreso.MinDate = hoy - MARGEN_FECHA_INGRESO;
            dtpFechaIngreso.MaxDate = hoy + MARGEN_FECHA_INGRESO;
            dtpFechaIngreso.Value = hoy;
        }

        private void ConfigurarRangoHistorico()
        {
            dtpFechaIngreso.MinDate = new DateTime(1940, 1, 1);
            dtpFechaIngreso.MaxDate = DateTime.Today.AddYears(5);
        }

        // ── Restricciones de campos ───────────────────────────────────────
        private void AplicarRestriccionesCampos()
        {
            try
            {
                // Nombres descriptivos en lugar de KP, TC, LV de una letra
                void OnKeyPress(Control c, KeyPressEventHandler h)
                {
                    if (c == null) return;
                    c.KeyPress -= h; c.KeyPress += h;
                }
                void OnTextChanged(Control c, EventHandler h)
                {
                    if (c == null) return;
                    c.TextChanged -= h; c.TextChanged += h;
                }
                void OnLeave(Control c, EventHandler h)
                {
                    if (c == null) return;
                    c.Leave -= h; c.Leave += h;
                }

                if (txtCP != null)
                {
                    txtCP.MaxLength = 5;
                    txtCP.KeyPress += SoloNumeros_KeyPress;
                }

                if (mskdNSS is MaskedTextBox mNss)
                {
                    mNss.Mask = "00000000000";
                    mNss.TextMaskFormat = MaskFormat.ExcludePromptAndLiterals;
                    OnLeave(mNss, ValidarMaskedAlSalir);
                }

                foreach (var (mtb, mask) in new[]
                {
                    (mskdTelFijo,  "0000000000"),
                    (mskdTelMovil, "0000000000"),
                    (mskdBANAMEX,  "0000000000000000")
                })
                {
                    if (mtb is MaskedTextBox m)
                    {
                        m.Mask = mask;
                        m.TextMaskFormat = MaskFormat.ExcludePromptAndLiterals;
                        OnLeave(m, ValidarMaskedAlSalir);
                    }
                }

                OnKeyPress(mskdRFC, SoloAlfaNum_KeyPress);
                OnLeave(mskdRFC, ValidarRFCAlSalir);
                OnKeyPress(mskdCURP, SoloAlfaNum_KeyPress);
                OnLeave(mskdCURP, ValidarCURPAlSalir);

                OnKeyPress(mskdtxtNoExterior, SoloNumeros_KeyPress);
                OnKeyPress(mskdtxtNoInterior, SoloNumeros_KeyPress);

                if (txtTelEmpr != null)
                {
                    txtTelEmpr.MaxLength = 10; txtTelEmpr.Tag = 10;
                    OnKeyPress(txtTelEmpr, SoloNumerosConMax_KeyPress);
                }
                if (txtExt != null)
                {
                    txtExt.MaxLength = 5; txtExt.Tag = 5;
                    OnKeyPress(txtExt, SoloNumerosConMax_KeyPress);
                }

                OnKeyPress(mskdtxtLADATel, SoloNumeros_KeyPress);

                foreach (var c in new Control[]
                    { txtNombre, txtApellidoPaterno, txtApellidoMaterno, txtRegimen })
                    OnKeyPress(c, SoloLetras_KeyPress);

                foreach (var c in new Control[]
                    { txtEstado, txtMunicipio, txtColonia, txtReferencias,
                      txtActividadEcon })
                    OnKeyPress(c, SoloAlfaNumEspacio_KeyPress);

                foreach (var c in new Control[]
                    { txtIngresoDiarioNormal, txtViaticosDiarios, txtOtrosDiarios })
                    OnKeyPress(c, ValidarCampoDecimal);

                txtSalarioDiarioIntegrado.ReadOnly = true;
                txtISRMensual.ReadOnly = true;

                OnLeave(txtCorreoElectronicoPersonal, ValidarEmailAlSalir);
                OnLeave(txtEmailEmpresa, ValidarEmailAlSalir);

                // Recálculo automático SDI/ISR al cambiar ingresos
                foreach (var c in new Control[]
                    { txtIngresoDiarioNormal, txtViaticosDiarios, txtOtrosDiarios })
                    OnTextChanged(c, RecalcularSalarios);

                OnLeave(txtApellidoPaterno, (s, ev) =>
                {
                    if (_estadoActual == EstadoUI.Edicion)
                        AutoGenerarEmailEmpresa();
                });

                // Recálculo también al cambiar fecha de alta (afecta antigüedad → factor SDI)
                if (dtpFechaIngreso != null)
                {
                    dtpFechaIngreso.ValueChanged -= RecalcularSalarios;
                    dtpFechaIngreso.ValueChanged += RecalcularSalarios;

                    dtpFechaIngreso.ValueChanged -= dtpFechaIngreso_ValueChanged_ActualizarAntiguedad;
                    dtpFechaIngreso.ValueChanged += dtpFechaIngreso_ValueChanged_ActualizarAntiguedad;
                }
            }
            catch { /* defensivo */ }
        }

        private void dtpFechaIngreso_ValueChanged_ActualizarAntiguedad(object sender, EventArgs e)
        {
            ActualizarAntiguedadEmpleado();
        }

        // ── Validaciones de campos individuales ───────────────────────────

        private void ValidarMaskedAlSalir(object sender, EventArgs e)
        {
            if (_estadoActual != EstadoUI.Edicion) return;
            if (!(sender is MaskedTextBox mb)) return;

            string valor = (mb.Text ?? "").Replace(" ", "").Replace("_", "").Trim();
            if (string.IsNullOrWhiteSpace(valor)) return;

            if (!mb.MaskCompleted)
            {
                string nombre = ObtenerNombreCampo(mb);
                MessageBox.Show($"El campo \"{nombre}\" tiene datos incompletos.", "Campo incompleto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mb.Focus();
            }
        }

        private void ValidarRFCAlSalir(object sender, EventArgs e)
        {
            if (_estadoActual != EstadoUI.Edicion) return;
            string rfc = GetControlValue(mskdRFC).ToUpper();
            if (string.IsNullOrWhiteSpace(rfc)) return;

            if (!ValidarRFC(rfc))
            {
                MessageBox.Show("RFC incompleto o con formato incorrecto.\nFormato esperado: 4 letras · 6 dígitos · 3 alfanuméricos", "RFC inválido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mskdRFC.Focus();
            }
        }

        private void ValidarCURPAlSalir(object sender, EventArgs e)
        {
            if (_estadoActual != EstadoUI.Edicion) return;
            string curp = GetControlValue(mskdCURP).ToUpper();
            if (string.IsNullOrWhiteSpace(curp)) return;

            if (!ValidarCURP(curp))
            {
                MessageBox.Show("CURP incompleta o con formato incorrecto (18 caracteres).", "CURP inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mskdCURP.Focus();
            }
        }

        private void ValidarEmailAlSalir(object sender, EventArgs e)
        {
            if (_estadoActual != EstadoUI.Edicion) return;
            if (!(sender is System.Windows.Forms.TextBox tb)) return;
            string email = tb.Text.Trim();
            if (string.IsNullOrWhiteSpace(email)) return;

            if (!ValidarEmail(email))
            {
                MessageBox.Show("El correo electrónico no tiene un formato válido.", "Email inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tb.Focus();
            }
        }

        private string ObtenerNombreCampo(Control ctrl)
        {
            var par = _mapaCampos.FirstOrDefault(kv => kv.Value == ctrl);
            return !par.Equals(default(KeyValuePair<Label, Control>)) ? par.Key.Text.Replace("* ", "").Trim() : ctrl.Name;
        }

        // ── Helpers de validación ─────────────────────────────────────────

        private static bool ValidarRFC(string rfc) => Regex.IsMatch(rfc.ToUpper(), @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$");
        private static bool ValidarCURP(string curp) => Regex.IsMatch(curp.ToUpper(), @"^[A-Z][AEIOU][A-Z]{2}\d{6}[HM][A-Z]{5}[A-Z0-9]\d$");
        private static bool ValidarTelefono(string t) => Regex.IsMatch(t, @"^\d{10}$");
        private static bool ValidarNumTarjeta(string c) => Regex.IsMatch(c, @"^\d{16}$");
        private static bool ValidarEmail(string e) => Regex.IsMatch(e, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static bool EsVacioOGuion(string valor) => string.IsNullOrWhiteSpace(valor) || valor.Trim() == "-";
        private string GetControlValue(Control ctrl)
        {
            if (ctrl == null) return string.Empty;
            if (ctrl is MaskedTextBox mb)
                return (mb.Text ?? "").Replace(" ", "").Replace("_", "").Trim();
            return (ctrl.Text ?? "").Trim();
        }

        private bool CoincideBusquedaEmpleado(EmpleadoCompleto emp, string busqueda)
        {
            if (emp == null) return false;

            string textoNormalizado = NormalizarBusqueda(busqueda);

            if (string.IsNullOrWhiteSpace(textoNormalizado))
                return true;

            string[] palabras = textoNormalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (palabras.Length == 0)
                return true;

            string bolsa = NormalizarBusqueda(string.Join(" ", new[]
            {
                emp.Id.ToString(),
                emp.Nombre,
                emp.ApellidoP,
                emp.ApellidoM,
                $"{emp.Nombre} {emp.ApellidoP} {emp.ApellidoM}",
                $"{emp.ApellidoP} {emp.ApellidoM} {emp.Nombre}",
                emp.RFC,
                emp.CURP,
                emp.NSS,
                emp.FechaAlta,
                emp.FechaBaja,
                emp.Banamex,
                emp.Email,
                emp.EmailEmpresa,
                emp.Departamento,
                emp.Puesto
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

        // ── Handlers de teclado ───────────────────────────────────────────

        private void SoloNumeros_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsDigit(e.KeyChar)) e.Handled = true;
        }

        private void SoloNumerosConMax_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsDigit(e.KeyChar)) { e.Handled = true; return; }

            var tb = sender as System.Windows.Forms.TextBox;
            if (tb == null) return;
            if (tb.Tag is int max && tb.SelectionLength == 0 && tb.Text.Length >= max)
                e.Handled = true;
        }

        private void SoloLetras_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsLetter(e.KeyChar) && e.KeyChar != ' ') e.Handled = true;
        }

        private void SoloAlfaNum_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsLetterOrDigit(e.KeyChar)) e.Handled = true;
        }

        private void SoloAlfaNumEspacio_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (!char.IsLetterOrDigit(e.KeyChar) && e.KeyChar != ' ')
                e.Handled = true;
        }

        private void ValidarCampoDecimal(object sender, KeyPressEventArgs e)
        {
            if (!(sender is System.Windows.Forms.TextBox txt))
                return;

            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
            {
                string texto = txt.Text ?? "";
                int sepIndex = texto.IndexOfAny(new[] { '.', ',' });

                if (sepIndex >= 0 && txt.SelectionLength == 0)
                {
                    int cursor = txt.SelectionStart;

                    if (cursor > sepIndex)
                    {
                        int decimalesActuales = texto.Length - sepIndex - 1;

                        if (decimalesActuales >= DECIMALES_CURRENCY_VB6)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }

                return;
            }

            if (e.KeyChar == '.' || e.KeyChar == ',')
            {
                string texto = txt.Text ?? "";
                bool yaTieneSeparador = texto.Contains('.') || texto.Contains(',');

                if (yaTieneSeparador && txt.SelectionLength == 0)
                {
                    e.Handled = true;
                    return;
                }

                return;
            }

            e.Handled = true;
        }

        private void ConvertirMayusculas(object sender, EventArgs e)
        {
            var txt = sender as System.Windows.Forms.TextBox;
            if (txt == null) return;
            int pos = txt.SelectionStart;
            string m = txt.Text.ToUpper();
            if (txt.Text != m) { txt.Text = m; txt.SelectionStart = pos; }
        }

        // ── Helpers de cursor y ToUpper ───────────────────────────────────

        private void Control_MouseUp_CursorInicio(object sender, MouseEventArgs e)
        {
            if (sender is System.Windows.Forms.TextBoxBase txt && string.IsNullOrWhiteSpace((txt.Text ?? "").Replace("_", "")))
            {
                txt.SelectionStart = 0;
                txt.SelectionLength = 0;
            }
        }

        private void AplicarCursorInicioRecursivo(Control control)
        {
            if (control is System.Windows.Forms.TextBoxBase)
            {
                control.MouseUp -= Control_MouseUp_CursorInicio;
                control.MouseUp += Control_MouseUp_CursorInicio;
            }
            foreach (Control child in control.Controls)
                AplicarCursorInicioRecursivo(child);
        }

        private void TextBox_Leave_ToUpper(object sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.TextBox txt)
                txt.Text = txt.Text.ToUpper().Trim();
        }

        private void AplicarToUpperRecursivo(Control control)
        {
            if (control is System.Windows.Forms.TextBox txt)
            {
                // Excluir campos numéricos de la conversión a mayúsculas
                if (txt != txtIngresoDiarioNormal && txt != txtViaticosDiarios && txt != txtOtrosDiarios)
                    txt.Leave += TextBox_Leave_ToUpper;
            }
            foreach (Control child in control.Controls)
                AplicarToUpperRecursivo(child);
        }

        // ── TabIndex ──────────────────────────────────────────────────────

        private void ConfigurarTabIndex()
        {
            var orden = new Control[]
            {
                txtNombre, txtApellidoPaterno, txtApellidoMaterno, mskdCURP, mskdRFC, mskdBANAMEX, txtEstado, txtMunicipio, txtCP, txtColonia, mskdtxtNoExterior,
                mskdtxtNoInterior, txtReferencias, txtCorreoElectronicoPersonal, mskdTelFijo, mskdTelMovil, mskdtxtLADATel, mskdNSS, txtEmailEmpresa, txtTelEmpr,
                txtExt, txtActividadEcon, txtRegimen, txtIngresoDiarioNormal, txtViaticosDiarios, txtOtrosDiarios
            };

            int idx = 0;
            foreach (var c in orden)
            {
                if (c == null) continue;
                c.TabStop = true;
                c.TabIndex = idx++;
            }

            if (btnMostrarCFDI != null) btnMostrarCFDI.TabIndex = idx++;
            if (btnModificarRegistro != null) btnModificarRegistro.TabIndex = idx++;
            if (btnAgregarEmpleado != null) btnAgregarEmpleado.TabIndex = idx++;
            if (txtBuscarPersonal != null) txtBuscarPersonal.TabIndex = idx++;
            if (btnBuscarEnDGV != null) btnBuscarEnDGV.TabIndex = idx++;
            if (dgvEditPersonal != null) dgvEditPersonal.TabIndex = idx++;
        }

        private void ActualizarAntiguedadEmpleado()
        {
            if (lblAntiguedadDelEmpleado == null)
                return;

            DateTime fechaAlta = dtpFechaIngreso.Value.Date;
            DateTime fechaCorte = DateTime.Today;

            lblAntiguedadDelEmpleado.Text = FormatearAntiguedad(fechaAlta, fechaCorte);
        }

        private static string FormatearAntiguedad(DateTime fechaAlta, DateTime fechaCorte)
        {
            fechaAlta = fechaAlta.Date;
            fechaCorte = fechaCorte.Date;

            if (fechaAlta > fechaCorte)
                return "Ingreso futuro";

            int anios = fechaCorte.Year - fechaAlta.Year;

            if (fechaAlta.AddYears(anios) > fechaCorte)
                anios--;

            DateTime fechaBase = fechaAlta.AddYears(anios);

            int meses = 0;

            while (fechaBase.AddMonths(meses + 1) <= fechaCorte)
                meses++;

            DateTime fechaBaseMeses = fechaBase.AddMonths(meses);
            int dias = (fechaCorte - fechaBaseMeses).Days;

            var partes = new List<string>();

            if (anios > 0)
                partes.Add(anios == 1 ? "1 año" : $"{anios} años");

            if (meses > 0)
                partes.Add(meses == 1 ? "1 mes" : $"{meses} meses");

            if (partes.Count == 0)
            {
                if (dias <= 0)
                    return "0 días";

                return dias == 1 ? "1 día" : $"{dias} días";
            }

            return string.Join(", ", partes) + ".";
        }

        private void PrepararBajaConfirmada(DateTime fechaBaja, bool editable)
        {
            _modoCapturaBaja = true;
            _fechaBajaPendienteConfirmacion = false;

            ConfigurarRangoFechaBaja();

            dtpFechaBaja.ShowCheckBox = false;
            dtpFechaBaja.Value = fechaBaja.Date;

            MostrarCampoFechaBaja(true, editable);

            // Ya quedó preparada la baja.
            // Ahora el usuario debe Guardar o Cancelar.
            btnDarDeBajaEmpleado.Visible = false;
            btnDarDeBajaEmpleado.Enabled = false;

            MessageBox.Show($"La fecha de baja {fechaBaja:dd/MM/yyyy} quedó preparada.\n\nPresiona Guardar para aplicar la baja o Cancelar para descartarla.", "Baja preparada",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ReingresarEmpleado(EmpleadoCompleto emp, DateTime fechaReingreso, bool respetaAntiguedad)
        {
            if (emp == null)
                return;

            if (EmpleadoEstaActivo(emp))
            {
                MessageBox.Show("El empleado ya se encuentra activo.", "Reingreso no aplicable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Conservamos SIEMPRE:
            // emp.FechaAlta  = primera alta
            // emp.FechaBaja  = primera baja
            emp.EsReingreso = true;
            emp.FechaReingresoAlta = fechaReingreso.ToString("dd/MM/yyyy");
            emp.FechaReingresoBaja = "";
            emp.RespetaAntiguedad = respetaAntiguedad;
            emp.EstatusLaboral = "REINGRESO_ACTIVO";

            emp.FechaUltimaMod = DateTime.Now.ToString("dd/MM/yyyy");
            emp.ModificadoPor = Environment.UserName;

            _personalService.GuardarCambios();

            HuboCambios = true;

            _modoCapturaBaja = false;
            _fechaBajaPendienteConfirmacion = false;

            AplicarFiltros();
            SeleccionarEmpleadoEnGrid(emp.Id);

            MostrarCampoFechaBaja(false, false);

            btnReingresoEmpleado.Visible = false;
            btnReingresoEmpleado.Enabled = false;

            btnDarDeBajaEmpleado.Visible = false;
            btnDarDeBajaEmpleado.Enabled = false;

            CambiarEstadoUI(EstadoUI.Lectura);

            MessageBox.Show(respetaAntiguedad ? "El empleado fue reingresado respetando su antigüedad original." : "El empleado fue reingresado tomando como base la nueva fecha de reingreso.",
                "Reingreso registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private DateTime? PedirFechaReingreso(DateTime fechaInicial)
        {
            using Form frm = new Form();
            using DateTimePicker dtp = new DateTimePicker();
            using Button btnAceptar = new Button();
            using Button btnCancelar = new Button();
            using Label lbl = new Label();

            frm.Text = "Fecha de reingreso";
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(330, 135);

            lbl.Text = "Selecciona la fecha de reingreso:";
            lbl.AutoSize = true;
            lbl.Location = new Point(15, 15);

            dtp.Format = DateTimePickerFormat.Custom;
            dtp.CustomFormat = "dd/MM/yyyy";
            dtp.Location = new Point(18, 45);
            dtp.Width = 130;

            DateTime hoy = DateTime.Today;
            dtp.MinDate = hoy - MARGEN_FECHA_INGRESO;
            dtp.MaxDate = hoy + MARGEN_FECHA_INGRESO;
            dtp.Value = fechaInicial.Date < dtp.MinDate ? hoy : fechaInicial.Date > dtp.MaxDate ? hoy : fechaInicial.Date;

            btnAceptar.Text = "Aceptar";
            btnAceptar.DialogResult = DialogResult.OK;
            btnAceptar.Location = new Point(155, 90);

            btnCancelar.Text = "Cancelar";
            btnCancelar.DialogResult = DialogResult.Cancel;
            btnCancelar.Location = new Point(240, 90);

            frm.Controls.Add(lbl);
            frm.Controls.Add(dtp);
            frm.Controls.Add(btnAceptar);
            frm.Controls.Add(btnCancelar);

            frm.AcceptButton = btnAceptar;
            frm.CancelButton = btnCancelar;

            return frm.ShowDialog(this) == DialogResult.OK ? dtp.Value.Date : null;
        }
    }
}
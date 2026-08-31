using Newtonsoft.Json;
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

namespace Nomina_2026_NET8
{
    public partial class FormEmpresaUnificado : Form
    {
        // Servicio único — no se recrea en cada operación
        private RuepService _service;
        private RuepRoot _empresaOriginal;

        // Regex compartido para RFC (empresa y representante)
        private static readonly Regex RegexRFC = new(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", RegexOptions.Compiled);
        private static readonly Regex RegexCURP = new(@"^[A-Z]{4}\d{6}[HM][A-Z]{5}[A-Z0-9]\d$", RegexOptions.Compiled);
        private static readonly CultureInfo FormatoPesos = new("es-MX");

        public string RutaArchivoEmpresa { get; set; }


        public FormEmpresaUnificado()
        {
            InitializeComponent();
            btnGuardar.Visible = false;
            btnCancelar.Visible = false;
        }

        private void FormEmpresaUnificado_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(RutaArchivoEmpresa) ||
                !System.IO.File.Exists(RutaArchivoEmpresa))
            {
                MessageBox.Show("No se encontró el archivo RUEP.dat");
                Close();
                return;
            }

            try
            {
                // 🔧 Un solo servicio para toda la vida del Form
                _service = new RuepService(RutaArchivoEmpresa);
                var empresa = _service.ObtenerEmpresa();

                // Guardar copia para restaurar si el usuario cancela
                _empresaOriginal = JsonConvert.DeserializeObject<RuepRoot>(JsonConvert.SerializeObject(empresa));

                CargarDatosEnControles(empresa);
                SalirModoEdicion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos: " + ex.Message);
                Close();
            }
        }

        // ── Eventos de botones ────────────────────────────────────────────
        private void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!ValidarCampos()) return;

            try
            {
                // 🔧 Modificamos DatosEmpresa directamente — es la misma
                //    referencia que usa el servicio internamente
                var empresa = _service.ObtenerEmpresa();

                empresa.ano_fiscal = int.Parse(mskdtxtAnio.Text);
                empresa.salario_minimo = ParseDecimalInput(txtSalarioMinimoVigente.Text);
                empresa.uma_por_dia = ParseDecimalInput(txtUMA.Text);
                empresa.fecha_actualizacion = DateTime.Now.ToString("dd/MM/yyyy");
                empresa.modificado_por = Environment.UserName;

                empresa.datos_fiscales.rfc = txtRFCEmpresa.Text.Trim();
                empresa.datos_fiscales.registro_patronal = txtRegistroPatronal.Text.Trim();
                empresa.datos_fiscales.direccion = txtDireccion.Text.Trim();

                empresa.datos_fiscales.representante_legal.apellido_paterno = txtAPaterno.Text.Trim();
                empresa.datos_fiscales.representante_legal.apellido_materno = txtAMaterno.Text.Trim();
                empresa.datos_fiscales.representante_legal.nombre = txtNombre.Text.Trim();
                empresa.datos_fiscales.representante_legal.rfc = txtRFCRepresentante.Text.Trim();
                empresa.datos_fiscales.representante_legal.curp = txtCURPRrepresentante.Text.Trim();

                empresa.datos_fiscales.banamex.sucursal = txtSucursal.Text.Trim();
                empresa.datos_fiscales.banamex.cuenta = txtNoCuenta.Text.Trim();
                empresa.datos_fiscales.banamex.cliente = txtCliente.Text.Trim();

                _service.GuardarCambios();

                // Actualizar snapshot para próxima cancelación
                _empresaOriginal = JsonConvert.DeserializeObject<RuepRoot>(JsonConvert.SerializeObject(empresa));

                lblFechaMod.Text = empresa.fecha_actualizacion; // 🗑️ línea duplicada eliminada

                MessageBox.Show("Datos guardados correctamente en RUEP.dat");
                SalirModoEdicion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void btnModificar_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Desea modificar los datos?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
                EntrarModoEdicion();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            CargarDatosEnControles(_empresaOriginal);
            SalirModoEdicion();
            MessageBox.Show("No se realizaron cambios.");
        }

        // ── Métodos privados ──────────────────────────────────────────────

        // Elimina la duplicación entre Load y RestaurarDatosOriginales.
        private void CargarDatosEnControles(RuepRoot e)
        {
            lblEmpresa.Text = e.nombre;
            mskdtxtAnio.Text = e.ano_fiscal.ToString();
            txtSalarioMinimoVigente.Text = e.salario_minimo.ToString("C", FormatoPesos);
            txtUMA.Text = e.uma_por_dia.ToString("F2", CultureInfo.InvariantCulture);
            lblFechaMod.Text = e.fecha_actualizacion ?? string.Empty;

            txtRegistroPatronal.Text = e.datos_fiscales?.registro_patronal ?? string.Empty;
            txtRFCEmpresa.Text = e.datos_fiscales?.rfc ?? string.Empty;
            txtDireccion.Text = e.datos_fiscales?.direccion ?? string.Empty;

            txtAPaterno.Text = e.datos_fiscales?.representante_legal?.apellido_paterno ?? string.Empty;
            txtAMaterno.Text = e.datos_fiscales?.representante_legal?.apellido_materno ?? string.Empty;
            txtNombre.Text = e.datos_fiscales?.representante_legal?.nombre ?? string.Empty;
            txtRFCRepresentante.Text = e.datos_fiscales?.representante_legal?.rfc ?? string.Empty;
            txtCURPRrepresentante.Text = e.datos_fiscales?.representante_legal?.curp ?? string.Empty;

            txtSucursal.Text = e.datos_fiscales?.banamex?.sucursal ?? string.Empty;
            txtNoCuenta.Text = e.datos_fiscales?.banamex?.cuenta ?? string.Empty;
            txtCliente.Text = e.datos_fiscales?.banamex?.cliente ?? string.Empty;
        }

        // Reemplaza CambiarModoEdicion(bool) con dos métodos con nombre claro
        private void EntrarModoEdicion()
        {
            btnGuardar.Visible = true;
            btnCancelar.Visible = true;
            btnModificar.Visible = false;
            EstablecerReadOnly(false);
        }

        private void SalirModoEdicion()
        {
            btnGuardar.Visible = false;
            btnCancelar.Visible = false;
            btnModificar.Visible = true;
            EstablecerReadOnly(true);
        }

        private void EstablecerReadOnly(bool soloLectura)
        {
            mskdtxtAnio.ReadOnly = soloLectura;
            txtUMA.ReadOnly = soloLectura;
            txtSalarioMinimoVigente.ReadOnly = soloLectura;
            txtRegistroPatronal.ReadOnly = soloLectura;
            txtRFCEmpresa.ReadOnly = soloLectura;
            txtDireccion.ReadOnly = soloLectura;
            txtAPaterno.ReadOnly = soloLectura;
            txtAMaterno.ReadOnly = soloLectura;
            txtNombre.ReadOnly = soloLectura;
            txtRFCRepresentante.ReadOnly = soloLectura;
            txtCURPRrepresentante.ReadOnly = soloLectura;
            txtSucursal.ReadOnly = soloLectura;
            txtNoCuenta.ReadOnly = soloLectura;
            txtCliente.ReadOnly = soloLectura;
        }

        private bool ValidarCampos()
        {
            // Año
            if (!int.TryParse(mskdtxtAnio.Text.Trim(), out int anio) ||
                anio > DateTime.Now.Year || anio < 1900)
            {
                MessageBox.Show($"El año debe ser un número entre 1900 y {DateTime.Now.Year}.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // UMA y Salario mínimo
            if (!TryParseDecimalInput(txtUMA.Text, out _))
            {
                MessageBox.Show("La UMA debe ser un número decimal válido (ej. 400.35).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!TryParseDecimalInput(txtSalarioMinimoVigente.Text, out _))
            {
                MessageBox.Show("El salario mínimo debe ser un número decimal válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Registro patronal — 10 dígitos
            if (!Regex.IsMatch(txtRegistroPatronal.Text ?? "", @"^\d{10}$"))
            {
                MessageBox.Show("El registro patronal debe tener exactamente 10 dígitos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // RFC empresa — 🔧 usa la constante compartida
            if (!RegexRFC.IsMatch(
                (txtRFCEmpresa.Text ?? "").Trim().ToUpper()))
            {
                MessageBox.Show("RFC de la empresa inválido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // RFC representante — opcional pero validado si tiene valor
            string rfcRep = txtRFCRepresentante.Text.Trim().ToUpper();
            if (!string.IsNullOrEmpty(rfcRep) && !RegexRFC.IsMatch(rfcRep))
            {
                MessageBox.Show("RFC del representante inválido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // CURP representante — opcional pero validado si tiene valor
            string curp = txtCURPRrepresentante.Text.Trim().ToUpper();
            if (!string.IsNullOrEmpty(curp) && !RegexCURP.IsMatch(curp))
            {
                MessageBox.Show("CURP del representante inválida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Nombres — solo letras y espacios
            foreach (var txt in new[] { txtAPaterno, txtAMaterno, txtNombre })
            {
                if (!Regex.IsMatch((txt.Text ?? "").Trim(), @"^[\p{L}\s'-]+$"))
                {
                    MessageBox.Show("Los nombres y apellidos deben contener solo letras y espacios.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            // Dirección — caracteres permitidos
            if (!Regex.IsMatch((txtDireccion.Text ?? ""), @"^[\w\-\.\#\s,\/\\ºªáéíóúÁÉÍÓÚÑñ]*$"))
            {
                MessageBox.Show("La dirección contiene caracteres no permitidos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Sucursal / Cuenta / Cliente — solo números si tienen valor
            foreach (var (txt, nombre) in new[]
            {
                (txtSucursal, "Sucursal"),
                (txtNoCuenta, "Número de cuenta"),
                (txtCliente,  "Cliente")
            })
            {
                if (!string.IsNullOrWhiteSpace(txt.Text) && !Regex.IsMatch(txt.Text, @"^\d+$"))
                {
                    MessageBox.Show($"{nombre} debe contener solo números.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            return true;
        }

        // ── Helpers numéricos ─────────────────────────────────────────────
        // Normaliza cadenas numéricas aceptando: "$1,234.56", "1.234,56","1234.56", "1234,56", "1234".
        private static string NormalizarNumero(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            string s = input.Trim().Replace("$", "").Replace(" ", "");

            int lastDot = s.LastIndexOf('.');
            int lastComma = s.LastIndexOf(',');

            if (lastDot >= 0 && lastComma >= 0)
            {
                // Ambos presentes → el último es el decimal
                s = lastComma > lastDot
                    ? s.Replace(".", "").Replace(',', '.')   // coma es decimal
                    : s.Replace(",", "");                    // punto es decimal
            }
            else if (lastComma >= 0 && lastDot < 0)
            {
                // Solo coma → asumimos decimal
                s = s.Replace(",", ".");
            }
            else
            {
                // Solo punto o ninguno
                s = s.Replace(",", "");
            }

            return s;
        }

        private static bool TryParseDecimalInput(string input, out decimal value)
        {
            value = 0m;
            string norm = NormalizarNumero(input);
            if (string.IsNullOrWhiteSpace(norm)) return false;
            return decimal.TryParse(norm, NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        private static decimal ParseDecimalInput(string input)
        {
            if (TryParseDecimalInput(input, out decimal v))
                return Math.Round(v, 2);
            throw new FormatException("Valor numérico inválido: " + input);
        }

        // ── Handlers de teclado ───────────────────────────────────────────

        private void IntegerOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void DecimalOrCurrency_KeyPress(object sender, KeyPressEventArgs e)
        {
            var tb = sender as System.Windows.Forms.TextBox;
            if (char.IsControl(e.KeyChar)) return;

            if (e.KeyChar == '$')
            {
                e.Handled = !(tb.SelectionStart == 0 && !tb.Text.Contains('$'));
                return;
            }

            if (char.IsDigit(e.KeyChar)) return;

            if ((e.KeyChar == '.' || e.KeyChar == ',') && !tb.Text.Contains('.') && !tb.Text.Contains(','))
                return;

            e.Handled = true;
        }

        private void Decimal_KeyPress(object sender, KeyPressEventArgs e)
        {
            var tb = sender as System.Windows.Forms.TextBox;
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;
            if ((e.KeyChar == '.' || e.KeyChar == ',') && !tb.Text.Contains('.') && !tb.Text.Contains(','))
                return;
            e.Handled = true;
        }

        private void DigitsOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void RfcCurp_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsLetterOrDigit(e.KeyChar)) return;
            e.Handled = true;
        }

        private void LettersOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsLetter(e.KeyChar) || char.IsWhiteSpace(e.KeyChar) || e.KeyChar == '-' || e.KeyChar == '\'')
                return;
            e.Handled = true;
        }
    }
}

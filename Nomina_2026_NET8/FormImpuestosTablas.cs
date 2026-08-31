using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nomina_2026_NET8
{
    // Enum fuera de namespace de Form — más accesible si se necesita en otro lado
    public enum VistaTabla
    {
        Articulo113, Subsidio114, ISRMensual2da, ISRAnual
    }

    public partial class FormImpuestosTablas : Form
    {
        private VistaTabla _vistaActual;
        private bool _enEdicion = false;
        private bool _cancelandoEdicion = false;
        private TablasTarifaRoot _modelo;
        private BindingSource _bsTabla = new();

        // Contraseña desde Settings — no hardcodeada
        private static string EditPassword => Properties.Settings.Default.EditPassword;

        private Dictionary<VistaTabla, (Func<List<RangoTarifa>> GetTabla, string Titulo)> _mapaVistas;


        public FormImpuestosTablas()
        {
            InitializeComponent();
        }

        private void FormImpuestosTablas_Load(object sender, EventArgs e)
        {
            CentrarLabel();
            dgvTablaImpuestos.DoubleBuffered(true);
            dgvTablaImpuestos.ReadOnly = true;

            btnModificar.Visible = true;
            btnGuardar.Visible = false;
            btnCancelar.Visible = false;

            try
            {
                _modelo = TarifasRepository.Obtener();

                // 🔧 Inicializar mapa aquí — _modelo ya está cargado
                _mapaVistas = new()
                {
                    [VistaTabla.Articulo113] = (() => _modelo.tablas.ISR_113, "ISR Quincenal — Art. 113 (1ra quincena)"),
                    [VistaTabla.Subsidio114] = (() => _modelo.tablas.SUBSIDIO_114, "Subsidio al Empleo — Art. 114 (1ra quincena)"),
                    [VistaTabla.ISRMensual2da] = (() => _modelo.tablas.ISR_MENSUAL, "ISR Mensual — 2da quincena (Tab08Mes.ISR)"),
                    [VistaTabla.ISRAnual] = (() => _modelo.tablas.ISR_ANUAL_117, "ISR Anual — Art. 117 (ajuste diciembre)"),
                };

                CargarVista(VistaTabla.Articulo113);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando tablas:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        // ── Menú de tablas ────────────────────────────────────────────────
        private void tsArticulo113_Click(object sender, EventArgs e) => CargarVista(VistaTabla.Articulo113);
        private void tsSubsidio114_Click(object sender, EventArgs e) => CargarVista(VistaTabla.Subsidio114);
        private void tsISRAnual117_Click(object sender, EventArgs e) => CargarVista(VistaTabla.ISRAnual);
        private void tsISRMensual_Click(object sender, EventArgs e) => CargarVista(VistaTabla.ISRMensual2da);

        // ── Edición ───────────────────────────────────────────────────────
        private void btnModificar_Click(object sender, EventArgs e)
        {
            if (_enEdicion) return;

            using var pwd = new FormPassword();
            if (pwd.ShowDialog(this) != DialogResult.OK) return;

            if (pwd.Password != EditPassword)
            {
                MessageBox.Show("Contraseña incorrecta.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EntrarModoEdicion();
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            dgvTablaImpuestos.EndEdit();
            _bsTabla.EndEdit();

            if (!ValidarRangos()) return;

            try
            {
                TarifasRepository.Guardar(_modelo);
                MessageBox.Show("Cambios guardados correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SalirModoEdicion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Se perderán los cambios. ¿Continuar?", "Cancelar edición", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No) return;

            // try/finally garantiza que el flag siempre se restaura
            try
            {
                _cancelandoEdicion = true;
                dgvTablaImpuestos.CancelEdit();
                _bsTabla.CancelEdit();

                // Recargar modelo desde disco (descarta cambios en memoria)
                _modelo = TarifasRepository.Obtener();

                // Primero actualizar DataSource, luego forzar refresco del grid
                CargarVista(_vistaActual);
                _bsTabla.ResetBindings(false);

                SalirModoEdicion();
            }
            finally
            {
                // Siempre se restaura aunque haya excepción
                _cancelandoEdicion = false;
            }
        }

        // ── Validación del grid ───────────────────────────────────────────
        private void dgvTablaImpuestos_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is System.Windows.Forms.TextBox txt)
            {
                txt.KeyPress -= TxtDecimal_KeyPress;
                txt.KeyPress += TxtDecimal_KeyPress;
            }
        }

        private void dgvTablaImpuestos_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (!_enEdicion || _cancelandoEdicion) return;

            if (!EsDecimalValido(e.FormattedValue?.ToString()))
            {
                MessageBox.Show("Solo se permiten números con máximo 2 decimales.\nEjemplo: 1234.56", "Valor inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        // ── Métodos privados ──────────────────────────────────────────────
        private void CargarVista(VistaTabla vista)
        {
            if (_enEdicion)
            {
                MessageBox.Show("No puedes cambiar de tabla mientras estás editando.", "Edición activa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _vistaActual = vista;

            // Usa el mapa en lugar del switch — agregar una tabla nueva, solo requiere una entrada en _mapaVistas
            var (getTabla, titulo) = _mapaVistas[vista];
            lblTablaMostrada.Text = titulo;
            _bsTabla.DataSource = getTabla();
            dgvTablaImpuestos.DataSource = _bsTabla;

            CentrarLabel();
        }

        private void EntrarModoEdicion()
        {
            _enEdicion = true;
            dgvTablaImpuestos.ReadOnly = false;
            tsMostrarTabla.Enabled = false;
            btnModificar.Visible = false;
            btnGuardar.Visible = true;
            btnCancelar.Visible = true;
        }

        private void SalirModoEdicion()
        {
            _enEdicion = false;
            dgvTablaImpuestos.ReadOnly = true;
            tsMostrarTabla.Enabled = true;
            btnModificar.Visible = true;
            btnGuardar.Visible = false;
            btnCancelar.Visible = false;
        }

        private static bool EsDecimalValido(string valor) => !string.IsNullOrWhiteSpace(valor) && Regex.IsMatch(valor, @"^\d+(\.\d{1,2})?$");

        private void TxtDecimal_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (e.KeyChar == '-') { e.Handled = true; return; }
            if (char.IsDigit(e.KeyChar)) return;

            if (e.KeyChar == '.')
            {
                var tb = sender as System.Windows.Forms.TextBox;
                if (tb?.Text.Contains('.') == true) e.Handled = true;
                return;
            }

            e.Handled = true;
        }

        private bool ValidarRangos()
        {
            var lista = _bsTabla.List.Cast<RangoTarifa>().ToList();
            if (lista.Count == 0) return true;

            for (int i = 0; i < lista.Count; i++)
            {
                decimal inferior = lista[i].limInf;
                decimal superior = lista[i].limSup;

                // Mensaje mejorado — muestra los valores en conflicto
                if (inferior >= superior)
                {
                    MessageBox.Show($"Error en fila {i + 1}: " + $"límite inferior ({inferior:N2}) no puede ser ≥ " + $"límite superior ({superior:N2}).", "Error de rango", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (i > 0)
                {
                    decimal superiorAnterior = lista[i - 1].limSup;

                    if (inferior < superiorAnterior)
                    {
                        MessageBox.Show($"Error en fila {i + 1}: " + $"el rango ({inferior:N2}) se solapa con el anterior " + $"({superiorAnterior:N2}).", "Rangos solapados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    if (inferior > superiorAnterior)
                    {
                        MessageBox.Show($"Error en fila {i + 1}: " + $"existe un hueco entre {superiorAnterior:N2} " + $"y {inferior:N2}.", "Hueco entre rangos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }
            }

            return true;
        }

        private void CentrarLabel()
        {
            lblTablaMostrada.Left = (ClientSize.Width - lblTablaMostrada.Width) / 2;
        }
    }
}

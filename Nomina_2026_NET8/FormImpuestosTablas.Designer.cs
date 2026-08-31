namespace Nomina_2026_NET8
{
    partial class FormImpuestosTablas
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormImpuestosTablas));
            tsOpcionesMostrarTabla = new ToolStrip();
            tsMostrarTabla = new ToolStripDropDownButton();
            tsArticulo113 = new ToolStripMenuItem();
            tsSubsidio114 = new ToolStripMenuItem();
            tsISRAnual117 = new ToolStripMenuItem();
            tsISRMensual = new ToolStripMenuItem();
            lblTablaMostrada = new Label();
            dgvTablaImpuestos = new DataGridView();
            LimInferior = new DataGridViewTextBoxColumn();
            LimSuperior = new DataGridViewTextBoxColumn();
            CuotaFija = new DataGridViewTextBoxColumn();
            PorcentajeExcedente = new DataGridViewTextBoxColumn();
            btnCancelar = new Button();
            btnModificar = new Button();
            btnGuardar = new Button();
            tsOpcionesMostrarTabla.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTablaImpuestos).BeginInit();
            SuspendLayout();
            // 
            // tsOpcionesMostrarTabla
            // 
            tsOpcionesMostrarTabla.Items.AddRange(new ToolStripItem[] { tsMostrarTabla });
            tsOpcionesMostrarTabla.Location = new Point(0, 0);
            tsOpcionesMostrarTabla.Name = "tsOpcionesMostrarTabla";
            tsOpcionesMostrarTabla.Size = new Size(507, 25);
            tsOpcionesMostrarTabla.TabIndex = 0;
            tsOpcionesMostrarTabla.Text = "toolStrip1";
            // 
            // tsMostrarTabla
            // 
            tsMostrarTabla.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsMostrarTabla.DropDownItems.AddRange(new ToolStripItem[] { tsArticulo113, tsSubsidio114, tsISRAnual117, tsISRMensual });
            tsMostrarTabla.Image = (Image)resources.GetObject("tsMostrarTabla.Image");
            tsMostrarTabla.ImageTransparentColor = Color.Magenta;
            tsMostrarTabla.Name = "tsMostrarTabla";
            tsMostrarTabla.Size = new Size(76, 22);
            tsMostrarTabla.Text = "Ver Tabla...";
            // 
            // tsArticulo113
            // 
            tsArticulo113.Name = "tsArticulo113";
            tsArticulo113.Size = new Size(145, 22);
            tsArticulo113.Text = "Artículo 113";
            tsArticulo113.Click += tsArticulo113_Click;
            // 
            // tsSubsidio114
            // 
            tsSubsidio114.Name = "tsSubsidio114";
            tsSubsidio114.Size = new Size(145, 22);
            tsSubsidio114.Text = "Subsidio 114";
            tsSubsidio114.Click += tsSubsidio114_Click;
            // 
            // tsISRAnual117
            // 
            tsISRAnual117.Name = "tsISRAnual117";
            tsISRAnual117.Size = new Size(145, 22);
            tsISRAnual117.Text = "ISR Anual 117";
            tsISRAnual117.Click += tsISRAnual117_Click;
            // 
            // tsISRMensual
            // 
            tsISRMensual.Name = "tsISRMensual";
            tsISRMensual.Size = new Size(145, 22);
            tsISRMensual.Text = "ISR Mensual";
            tsISRMensual.Click += tsISRMensual_Click;
            // 
            // lblTablaMostrada
            // 
            lblTablaMostrada.AutoSize = true;
            lblTablaMostrada.Font = new Font("Lucida Bright", 12F, FontStyle.Bold);
            lblTablaMostrada.Location = new Point(184, 34);
            lblTablaMostrada.Name = "lblTablaMostrada";
            lblTablaMostrada.Size = new Size(132, 18);
            lblTablaMostrada.TabIndex = 1;
            lblTablaMostrada.Text = "Tabla Mostrada";
            // 
            // dgvTablaImpuestos
            // 
            dgvTablaImpuestos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTablaImpuestos.Columns.AddRange(new DataGridViewColumn[] { LimInferior, LimSuperior, CuotaFija, PorcentajeExcedente });
            dgvTablaImpuestos.Location = new Point(12, 64);
            dgvTablaImpuestos.Name = "dgvTablaImpuestos";
            dgvTablaImpuestos.RowHeadersVisible = false;
            dgvTablaImpuestos.Size = new Size(483, 344);
            dgvTablaImpuestos.TabIndex = 2;
            dgvTablaImpuestos.CellValidating += dgvTablaImpuestos_CellValidating;
            dgvTablaImpuestos.EditingControlShowing += dgvTablaImpuestos_EditingControlShowing;
            // 
            // LimInferior
            // 
            LimInferior.DataPropertyName = "limInf";
            LimInferior.HeaderText = "Límite Inferior";
            LimInferior.MaxInputLength = 15;
            LimInferior.Name = "LimInferior";
            LimInferior.Width = 119;
            // 
            // LimSuperior
            // 
            LimSuperior.DataPropertyName = "limSup";
            LimSuperior.HeaderText = "Límite Superior";
            LimSuperior.MaxInputLength = 15;
            LimSuperior.Name = "LimSuperior";
            LimSuperior.Width = 119;
            // 
            // CuotaFija
            // 
            CuotaFija.DataPropertyName = "cuotaFija";
            CuotaFija.HeaderText = "Cuota Fija";
            CuotaFija.MaxInputLength = 15;
            CuotaFija.Name = "CuotaFija";
            CuotaFija.Width = 119;
            // 
            // PorcentajeExcedente
            // 
            PorcentajeExcedente.DataPropertyName = "porcentaje";
            PorcentajeExcedente.HeaderText = "Porcentaje Excedente (%)";
            PorcentajeExcedente.MaxInputLength = 15;
            PorcentajeExcedente.Name = "PorcentajeExcedente";
            PorcentajeExcedente.Width = 120;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(37, 415);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(78, 39);
            btnCancelar.TabIndex = 3;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // btnModificar
            // 
            btnModificar.Location = new Point(214, 415);
            btnModificar.Name = "btnModificar";
            btnModificar.Size = new Size(78, 39);
            btnModificar.TabIndex = 4;
            btnModificar.Text = "Modificar";
            btnModificar.UseVisualStyleBackColor = true;
            btnModificar.Click += btnModificar_Click;
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(392, 415);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(78, 39);
            btnGuardar.TabIndex = 5;
            btnGuardar.Text = "Guardar";
            btnGuardar.UseVisualStyleBackColor = true;
            btnGuardar.Click += btnGuardar_Click;
            // 
            // FormImpuestosTablas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(507, 466);
            Controls.Add(btnGuardar);
            Controls.Add(btnModificar);
            Controls.Add(btnCancelar);
            Controls.Add(dgvTablaImpuestos);
            Controls.Add(lblTablaMostrada);
            Controls.Add(tsOpcionesMostrarTabla);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormImpuestosTablas";
            Text = "Tablas de Impuestos";
            Load += FormImpuestosTablas_Load;
            tsOpcionesMostrarTabla.ResumeLayout(false);
            tsOpcionesMostrarTabla.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvTablaImpuestos).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip tsOpcionesMostrarTabla;
        private ToolStripDropDownButton tsMostrarTabla;
        private ToolStripMenuItem tsArticulo113;
        private ToolStripMenuItem tsSubsidio114;
        private ToolStripMenuItem tsISRAnual117;
        private ToolStripMenuItem tsISRMensual;
        private Label lblTablaMostrada;
        private DataGridView dgvTablaImpuestos;
        private Button btnCancelar;
        private Button btnModificar;
        private Button btnGuardar;
        private DataGridViewTextBoxColumn LimInferior;
        private DataGridViewTextBoxColumn LimSuperior;
        private DataGridViewTextBoxColumn CuotaFija;
        private DataGridViewTextBoxColumn PorcentajeExcedente;
    }
}
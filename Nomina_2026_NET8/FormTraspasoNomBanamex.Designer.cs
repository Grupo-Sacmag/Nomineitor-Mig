namespace Nomina_2026_NET8
{
    partial class FormTraspasoNomBanamex
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTraspasoNomBanamex));
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            toolStrip1 = new ToolStrip();
            tsOpciones = new ToolStripDropDownButton();
            tsCopiarTabla1 = new ToolStripMenuItem();
            tsCopiarTabla2 = new ToolStripMenuItem();
            tsGenerarTXT = new ToolStripButton();
            dgvDatosEmpleadosEntrada = new DataGridView();
            NumCuenta = new DataGridViewTextBoxColumn();
            NombreEmpleado = new DataGridViewTextBoxColumn();
            ApellidoPaterno = new DataGridViewTextBoxColumn();
            ApellidoMaterno = new DataGridViewTextBoxColumn();
            Importe = new DataGridViewTextBoxColumn();
            Trabajado = new DataGridViewTextBoxColumn();
            RefAlfanumerica = new DataGridViewTextBoxColumn();
            ConceptoPago = new DataGridViewTextBoxColumn();
            MismoDia = new DataGridViewTextBoxColumn();
            Cons = new DataGridViewTextBoxColumn();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            txtSecuencial = new TextBox();
            mskdFecha = new MaskedTextBox();
            mskdNumCliente = new MaskedTextBox();
            mskdNombreCliente = new MaskedTextBox();
            mskdNumUsuario = new MaskedTextBox();
            dgvEspejo = new DataGridView();
            M = new DataGridViewTextBoxColumn();
            BAN = new DataGridViewTextBoxColumn();
            TC = new DataGridViewTextBoxColumn();
            PRO = new DataGridViewTextBoxColumn();
            IN = new DataGridViewTextBoxColumn();
            SUC = new DataGridViewTextBoxColumn();
            CUENTA = new DataGridViewTextBoxColumn();
            PER = new DataGridViewTextBoxColumn();
            Nombre = new DataGridViewTextBoxColumn();
            Alias = new DataGridViewTextBoxColumn();
            M2 = new DataGridViewTextBoxColumn();
            Importe2 = new DataGridViewTextBoxColumn();
            P = new DataGridViewTextBoxColumn();
            RFC = new DataGridViewTextBoxColumn();
            Vacio1 = new DataGridViewTextBoxColumn();
            Vacio2 = new DataGridViewTextBoxColumn();
            Vacio3 = new DataGridViewTextBoxColumn();
            Vacio4 = new DataGridViewTextBoxColumn();
            btnCerrar = new Button();
            btnGenerar = new Button();
            lblResumen = new Label();
            saveFileDialog1 = new SaveFileDialog();
            panelDGVDatosArriba = new Panel();
            panelDGVEspejo = new Panel();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDatosEmpleadosEntrada).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvEspejo).BeginInit();
            panelDGVDatosArriba.SuspendLayout();
            panelDGVEspejo.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsOpciones, tsGenerarTXT });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(915, 25);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsOpciones
            // 
            tsOpciones.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsOpciones.DropDownItems.AddRange(new ToolStripItem[] { tsCopiarTabla1, tsCopiarTabla2 });
            tsOpciones.Image = (Image)resources.GetObject("tsOpciones.Image");
            tsOpciones.ImageTransparentColor = Color.Magenta;
            tsOpciones.Name = "tsOpciones";
            tsOpciones.Size = new Size(70, 22);
            tsOpciones.Text = "Opciones";
            // 
            // tsCopiarTabla1
            // 
            tsCopiarTabla1.Name = "tsCopiarTabla1";
            tsCopiarTabla1.Size = new Size(292, 22);
            tsCopiarTabla1.Text = "Seleccionar Y Copiar Todo Primer Tabla";
            tsCopiarTabla1.Click += tsCopiarTabla1_Click;
            // 
            // tsCopiarTabla2
            // 
            tsCopiarTabla2.Name = "tsCopiarTabla2";
            tsCopiarTabla2.Size = new Size(292, 22);
            tsCopiarTabla2.Text = "Seleccionar Y Copiar Todo Segunda Tabla";
            tsCopiarTabla2.Click += tsCopiarTabla2_Click;
            // 
            // tsGenerarTXT
            // 
            tsGenerarTXT.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsGenerarTXT.Image = (Image)resources.GetObject("tsGenerarTXT.Image");
            tsGenerarTXT.ImageTransparentColor = Color.Magenta;
            tsGenerarTXT.Name = "tsGenerarTXT";
            tsGenerarTXT.Size = new Size(76, 22);
            tsGenerarTXT.Text = "Generar TXT";
            tsGenerarTXT.Click += tsGenerarTXT_Click;
            // 
            // dgvDatosEmpleadosEntrada
            // 
            dgvDatosEmpleadosEntrada.AllowUserToAddRows = false;
            dgvDatosEmpleadosEntrada.AllowUserToDeleteRows = false;
            dgvDatosEmpleadosEntrada.AllowUserToOrderColumns = true;
            dgvDatosEmpleadosEntrada.AllowUserToResizeRows = false;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = SystemColors.Control;
            dataGridViewCellStyle3.Font = new Font("Microsoft New Tai Lue", 8.25F, FontStyle.Bold);
            dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            dgvDatosEmpleadosEntrada.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvDatosEmpleadosEntrada.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDatosEmpleadosEntrada.Columns.AddRange(new DataGridViewColumn[] { NumCuenta, NombreEmpleado, ApellidoPaterno, ApellidoMaterno, Importe, Trabajado, RefAlfanumerica, ConceptoPago, MismoDia, Cons });
            dgvDatosEmpleadosEntrada.Dock = DockStyle.Fill;
            dgvDatosEmpleadosEntrada.Location = new Point(0, 0);
            dgvDatosEmpleadosEntrada.Name = "dgvDatosEmpleadosEntrada";
            dgvDatosEmpleadosEntrada.RowHeadersVisible = false;
            dgvDatosEmpleadosEntrada.Size = new Size(891, 350);
            dgvDatosEmpleadosEntrada.TabIndex = 1;
            // 
            // NumCuenta
            // 
            NumCuenta.Frozen = true;
            NumCuenta.HeaderText = "Núm. de Cta.";
            NumCuenta.Name = "NumCuenta";
            NumCuenta.Width = 120;
            // 
            // NombreEmpleado
            // 
            NombreEmpleado.Frozen = true;
            NombreEmpleado.HeaderText = "Nombre";
            NombreEmpleado.Name = "NombreEmpleado";
            NombreEmpleado.Width = 125;
            // 
            // ApellidoPaterno
            // 
            ApellidoPaterno.Frozen = true;
            ApellidoPaterno.HeaderText = "Apellido Paterno";
            ApellidoPaterno.Name = "ApellidoPaterno";
            ApellidoPaterno.Width = 85;
            // 
            // ApellidoMaterno
            // 
            ApellidoMaterno.Frozen = true;
            ApellidoMaterno.HeaderText = "Apellido Materno";
            ApellidoMaterno.Name = "ApellidoMaterno";
            ApellidoMaterno.Width = 85;
            // 
            // Importe
            // 
            Importe.Frozen = true;
            Importe.HeaderText = "Importe";
            Importe.Name = "Importe";
            Importe.Width = 75;
            // 
            // Trabajado
            // 
            Trabajado.Frozen = true;
            Trabajado.HeaderText = "Trabajado";
            Trabajado.Name = "Trabajado";
            Trabajado.Width = 70;
            // 
            // RefAlfanumerica
            // 
            RefAlfanumerica.Frozen = true;
            RefAlfanumerica.HeaderText = "Referencia Alfanumérica";
            RefAlfanumerica.Name = "RefAlfanumerica";
            RefAlfanumerica.Width = 80;
            // 
            // ConceptoPago
            // 
            ConceptoPago.Frozen = true;
            ConceptoPago.HeaderText = "Concepto de Pago";
            ConceptoPago.Name = "ConceptoPago";
            ConceptoPago.Width = 80;
            // 
            // MismoDia
            // 
            MismoDia.Frozen = true;
            MismoDia.HeaderText = "Mismo Día";
            MismoDia.Name = "MismoDia";
            MismoDia.Width = 80;
            // 
            // Cons
            // 
            Cons.HeaderText = "Cons.";
            Cons.Name = "Cons";
            Cons.Width = 70;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft Sans Serif", 8.25F);
            label1.Location = new Point(72, 388);
            label1.Name = "label1";
            label1.Size = new Size(106, 13);
            label1.TabIndex = 2;
            label1.Text = "Secuencial 4 Dígitos";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft Sans Serif", 8.25F);
            label2.Location = new Point(235, 388);
            label2.Name = "label2";
            label2.Size = new Size(37, 13);
            label2.TabIndex = 3;
            label2.Text = "Fecha";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft Sans Serif", 8.25F);
            label3.Location = new Point(342, 388);
            label3.Name = "label3";
            label3.Size = new Size(123, 13);
            label3.TabIndex = 4;
            label3.Text = "# de Cliente a 12 dígitos";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Microsoft Sans Serif", 8.25F);
            label4.Location = new Point(511, 388);
            label4.Name = "label4";
            label4.Size = new Size(153, 13);
            label4.TabIndex = 5;
            label4.Text = "Nombre de Cliente a 36 dígitos";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Microsoft Sans Serif", 8.25F);
            label5.Location = new Point(705, 388);
            label5.Name = "label5";
            label5.Size = new Size(127, 13);
            label5.TabIndex = 6;
            label5.Text = "# de Usuario a 12 dígitos";
            // 
            // txtSecuencial
            // 
            txtSecuencial.Location = new Point(72, 404);
            txtSecuencial.Name = "txtSecuencial";
            txtSecuencial.Size = new Size(106, 23);
            txtSecuencial.TabIndex = 7;
            // 
            // mskdFecha
            // 
            mskdFecha.Location = new Point(193, 404);
            mskdFecha.Name = "mskdFecha";
            mskdFecha.Size = new Size(117, 23);
            mskdFecha.TabIndex = 8;
            // 
            // mskdNumCliente
            // 
            mskdNumCliente.Location = new Point(325, 404);
            mskdNumCliente.Name = "mskdNumCliente";
            mskdNumCliente.Size = new Size(152, 23);
            mskdNumCliente.TabIndex = 9;
            // 
            // mskdNombreCliente
            // 
            mskdNombreCliente.Location = new Point(493, 404);
            mskdNombreCliente.Name = "mskdNombreCliente";
            mskdNombreCliente.Size = new Size(196, 23);
            mskdNombreCliente.TabIndex = 10;
            // 
            // mskdNumUsuario
            // 
            mskdNumUsuario.Location = new Point(705, 404);
            mskdNumUsuario.Name = "mskdNumUsuario";
            mskdNumUsuario.Size = new Size(127, 23);
            mskdNumUsuario.TabIndex = 11;
            // 
            // dgvEspejo
            // 
            dgvEspejo.AllowUserToAddRows = false;
            dgvEspejo.AllowUserToDeleteRows = false;
            dgvEspejo.AllowUserToOrderColumns = true;
            dgvEspejo.AllowUserToResizeColumns = false;
            dgvEspejo.AllowUserToResizeRows = false;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.BackColor = SystemColors.Control;
            dataGridViewCellStyle4.Font = new Font("Microsoft New Tai Lue", 8.25F, FontStyle.Bold);
            dataGridViewCellStyle4.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.True;
            dgvEspejo.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            dgvEspejo.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvEspejo.Columns.AddRange(new DataGridViewColumn[] { M, BAN, TC, PRO, IN, SUC, CUENTA, PER, Nombre, Alias, M2, Importe2, P, RFC, Vacio1, Vacio2, Vacio3, Vacio4 });
            dgvEspejo.Dock = DockStyle.Fill;
            dgvEspejo.Location = new Point(0, 0);
            dgvEspejo.Name = "dgvEspejo";
            dgvEspejo.RowHeadersVisible = false;
            dgvEspejo.Size = new Size(891, 350);
            dgvEspejo.TabIndex = 12;
            // 
            // M
            // 
            M.HeaderText = "M";
            M.Name = "M";
            M.Width = 80;
            // 
            // BAN
            // 
            BAN.HeaderText = "BAN";
            BAN.Name = "BAN";
            BAN.Width = 80;
            // 
            // TC
            // 
            TC.HeaderText = "TC";
            TC.Name = "TC";
            TC.Width = 80;
            // 
            // PRO
            // 
            PRO.HeaderText = "PRO";
            PRO.Name = "PRO";
            PRO.Width = 80;
            // 
            // IN
            // 
            IN.HeaderText = "IN";
            IN.Name = "IN";
            IN.Width = 80;
            // 
            // SUC
            // 
            SUC.HeaderText = "SUC";
            SUC.Name = "SUC";
            SUC.Width = 80;
            // 
            // CUENTA
            // 
            CUENTA.HeaderText = "CUENTA";
            CUENTA.Name = "CUENTA";
            CUENTA.Width = 80;
            // 
            // PER
            // 
            PER.HeaderText = "PER";
            PER.Name = "PER";
            PER.Width = 80;
            // 
            // Nombre
            // 
            Nombre.HeaderText = "NombreEmpleado";
            Nombre.Name = "Nombre";
            Nombre.Width = 80;
            // 
            // Alias
            // 
            Alias.HeaderText = "Alias";
            Alias.Name = "Alias";
            Alias.Width = 80;
            // 
            // M2
            // 
            M2.HeaderText = "M";
            M2.Name = "M2";
            // 
            // Importe2
            // 
            Importe2.HeaderText = "Importe";
            Importe2.Name = "Importe2";
            // 
            // P
            // 
            P.HeaderText = "P";
            P.Name = "P";
            // 
            // RFC
            // 
            RFC.HeaderText = "RFC";
            RFC.Name = "RFC";
            // 
            // Vacio1
            // 
            Vacio1.HeaderText = "";
            Vacio1.Name = "Vacio1";
            // 
            // Vacio2
            // 
            Vacio2.HeaderText = "";
            Vacio2.Name = "Vacio2";
            // 
            // Vacio3
            // 
            Vacio3.HeaderText = "";
            Vacio3.Name = "Vacio3";
            // 
            // Vacio4
            // 
            Vacio4.HeaderText = "";
            Vacio4.Name = "Vacio4";
            // 
            // btnCerrar
            // 
            btnCerrar.Location = new Point(738, 793);
            btnCerrar.Name = "btnCerrar";
            btnCerrar.Size = new Size(75, 23);
            btnCerrar.TabIndex = 13;
            btnCerrar.Text = "Cerrar";
            btnCerrar.UseVisualStyleBackColor = true;
            btnCerrar.Click += btnCerrar_Click;
            // 
            // btnGenerar
            // 
            btnGenerar.Location = new Point(828, 793);
            btnGenerar.Name = "btnGenerar";
            btnGenerar.Size = new Size(75, 23);
            btnGenerar.TabIndex = 14;
            btnGenerar.Text = "Generar";
            btnGenerar.UseVisualStyleBackColor = true;
            btnGenerar.Click += btnGenerar_Click;
            // 
            // lblResumen
            // 
            lblResumen.AutoSize = true;
            lblResumen.Location = new Point(12, 797);
            lblResumen.Name = "lblResumen";
            lblResumen.Size = new Size(12, 15);
            lblResumen.TabIndex = 15;
            lblResumen.Text = "-";
            // 
            // panelDGVDatosArriba
            // 
            panelDGVDatosArriba.Controls.Add(dgvDatosEmpleadosEntrada);
            panelDGVDatosArriba.Location = new Point(12, 35);
            panelDGVDatosArriba.Name = "panelDGVDatosArriba";
            panelDGVDatosArriba.Size = new Size(891, 350);
            panelDGVDatosArriba.TabIndex = 16;
            // 
            // panelDGVEspejo
            // 
            panelDGVEspejo.Controls.Add(dgvEspejo);
            panelDGVEspejo.Location = new Point(12, 437);
            panelDGVEspejo.Name = "panelDGVEspejo";
            panelDGVEspejo.Size = new Size(891, 350);
            panelDGVEspejo.TabIndex = 17;
            // 
            // FormTraspasoNomBanamex
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScroll = true;
            ClientSize = new Size(915, 821);
            Controls.Add(lblResumen);
            Controls.Add(btnGenerar);
            Controls.Add(btnCerrar);
            Controls.Add(mskdNumUsuario);
            Controls.Add(mskdNombreCliente);
            Controls.Add(mskdNumCliente);
            Controls.Add(mskdFecha);
            Controls.Add(txtSecuencial);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(toolStrip1);
            Controls.Add(panelDGVDatosArriba);
            Controls.Add(panelDGVEspejo);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "FormTraspasoNomBanamex";
            Text = "Traspaso de Nómina Banamex";
            Load += FormTraspasoNomBanamex_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvDatosEmpleadosEntrada).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvEspejo).EndInit();
            panelDGVDatosArriba.ResumeLayout(false);
            panelDGVEspejo.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip1;
        private ToolStripDropDownButton tsOpciones;
        private ToolStripMenuItem tsCopiarTabla1;
        private ToolStripMenuItem tsCopiarTabla2;
        private ToolStripButton tsGenerarTXT;
        private DataGridView dgvDatosEmpleadosEntrada;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private TextBox txtSecuencial;
        private MaskedTextBox mskdFecha;
        private MaskedTextBox mskdNumCliente;
        private MaskedTextBox mskdNombreCliente;
        private MaskedTextBox mskdNumUsuario;
        private DataGridViewTextBoxColumn NumCuenta;
        private DataGridViewTextBoxColumn NombreEmpleado;
        private DataGridViewTextBoxColumn ApellidoPaterno;
        private DataGridViewTextBoxColumn ApellidoMaterno;
        private DataGridViewTextBoxColumn Importe;
        private DataGridViewTextBoxColumn Trabajado;
        private DataGridViewTextBoxColumn RefAlfanumerica;
        private DataGridViewTextBoxColumn ConceptoPago;
        private DataGridViewTextBoxColumn MismoDia;
        private DataGridViewTextBoxColumn Cons;
        private DataGridView dgvEspejo;
        private DataGridViewTextBoxColumn M;
        private DataGridViewTextBoxColumn BAN;
        private DataGridViewTextBoxColumn TC;
        private DataGridViewTextBoxColumn PRO;
        private DataGridViewTextBoxColumn IN;
        private DataGridViewTextBoxColumn SUC;
        private DataGridViewTextBoxColumn CUENTA;
        private DataGridViewTextBoxColumn PER;
        private DataGridViewTextBoxColumn Nombre;
        private DataGridViewTextBoxColumn Alias;
        private DataGridViewTextBoxColumn M2;
        private DataGridViewTextBoxColumn Importe2;
        private DataGridViewTextBoxColumn P;
        private DataGridViewTextBoxColumn RFC;
        private DataGridViewTextBoxColumn Vacio1;
        private DataGridViewTextBoxColumn Vacio2;
        private DataGridViewTextBoxColumn Vacio3;
        private DataGridViewTextBoxColumn Vacio4;
        private Button btnCerrar;
        private Button btnGenerar;
        private Label lblResumen;
        private SaveFileDialog saveFileDialog1;
        private Panel panelDGVDatosArriba;
        private Panel panelDGVEspejo;
    }
}
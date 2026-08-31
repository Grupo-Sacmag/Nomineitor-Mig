namespace Nomina_2026_NET8
{
    partial class FormEmpresaUnificado
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormEmpresaUnificado));
            panelNombreEmpresa = new Panel();
            lblEmpresa = new Label();
            lblAnioEmpresa = new Label();
            lblSalarioMinimo = new Label();
            lblUMAxDIA = new Label();
            lblUltimaModificacion = new Label();
            lblFechaMod = new Label();
            label1 = new Label();
            lblRegistroPatronal = new Label();
            lblRFCEmpresa = new Label();
            lblDireccion = new Label();
            ldlDatosRepresentanteLegal = new Label();
            lblApellidoPaterno = new Label();
            lblApellidoMaterno = new Label();
            lblNombre = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            mskdtxtAnio = new MaskedTextBox();
            txtSalarioMinimoVigente = new TextBox();
            txtUMA = new TextBox();
            txtRegistroPatronal = new TextBox();
            txtRFCEmpresa = new TextBox();
            txtDireccion = new TextBox();
            txtAPaterno = new TextBox();
            txtAMaterno = new TextBox();
            txtNombre = new TextBox();
            txtRFCRepresentante = new TextBox();
            txtCURPRrepresentante = new TextBox();
            txtSucursal = new TextBox();
            txtNoCuenta = new TextBox();
            txtCliente = new TextBox();
            btnCancelar = new Button();
            btnModificar = new Button();
            btnGuardar = new Button();
            panelNombreEmpresa.SuspendLayout();
            SuspendLayout();
            // 
            // panelNombreEmpresa
            // 
            panelNombreEmpresa.Controls.Add(lblEmpresa);
            panelNombreEmpresa.Font = new Font("Constantia", 14.25F);
            panelNombreEmpresa.Location = new Point(12, 12);
            panelNombreEmpresa.Name = "panelNombreEmpresa";
            panelNombreEmpresa.Size = new Size(517, 76);
            panelNombreEmpresa.TabIndex = 0;
            // 
            // lblEmpresa
            // 
            lblEmpresa.Font = new Font("Constantia", 14.25F);
            lblEmpresa.Location = new Point(207, 28);
            lblEmpresa.Name = "lblEmpresa";
            lblEmpresa.Size = new Size(102, 23);
            lblEmpresa.TabIndex = 1;
            lblEmpresa.Text = "lblEmpresa";
            lblEmpresa.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblAnioEmpresa
            // 
            lblAnioEmpresa.AutoSize = true;
            lblAnioEmpresa.Font = new Font("Constantia", 14.25F, FontStyle.Bold | FontStyle.Italic);
            lblAnioEmpresa.Location = new Point(135, 115);
            lblAnioEmpresa.Name = "lblAnioEmpresa";
            lblAnioEmpresa.Size = new Size(51, 23);
            lblAnioEmpresa.TabIndex = 1;
            lblAnioEmpresa.Text = "Año:";
            // 
            // lblSalarioMinimo
            // 
            lblSalarioMinimo.AutoSize = true;
            lblSalarioMinimo.Font = new Font("Constantia", 14.25F, FontStyle.Bold | FontStyle.Italic);
            lblSalarioMinimo.Location = new Point(34, 157);
            lblSalarioMinimo.Name = "lblSalarioMinimo";
            lblSalarioMinimo.Size = new Size(152, 23);
            lblSalarioMinimo.TabIndex = 2;
            lblSalarioMinimo.Text = "Salario Mínimo:";
            // 
            // lblUMAxDIA
            // 
            lblUMAxDIA.AutoSize = true;
            lblUMAxDIA.Font = new Font("Constantia", 14.25F, FontStyle.Bold | FontStyle.Italic);
            lblUMAxDIA.Location = new Point(76, 202);
            lblUMAxDIA.Name = "lblUMAxDIA";
            lblUMAxDIA.Size = new Size(110, 23);
            lblUMAxDIA.TabIndex = 3;
            lblUMAxDIA.Text = "UMA x Día:";
            // 
            // lblUltimaModificacion
            // 
            lblUltimaModificacion.AutoSize = true;
            lblUltimaModificacion.Font = new Font("Constantia", 12F, FontStyle.Bold | FontStyle.Italic);
            lblUltimaModificacion.Location = new Point(346, 115);
            lblUltimaModificacion.Name = "lblUltimaModificacion";
            lblUltimaModificacion.Size = new Size(163, 19);
            lblUltimaModificacion.TabIndex = 4;
            lblUltimaModificacion.Text = "Última Modificación";
            // 
            // lblFechaMod
            // 
            lblFechaMod.AutoSize = true;
            lblFechaMod.Font = new Font("Constantia", 14.25F);
            lblFechaMod.Location = new Point(372, 157);
            lblFechaMod.Name = "lblFechaMod";
            lblFechaMod.Size = new Size(110, 23);
            lblFechaMod.TabIndex = 5;
            lblFechaMod.Text = "aaaa mm dd";
            lblFechaMod.TextAlign = ContentAlignment.TopRight;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Constantia", 9F, FontStyle.Bold | FontStyle.Italic);
            label1.Location = new Point(372, 134);
            label1.Name = "label1";
            label1.Size = new Size(103, 14);
            label1.TabIndex = 6;
            label1.Text = "(aaaa / mm / dd)";
            // 
            // lblRegistroPatronal
            // 
            lblRegistroPatronal.AutoSize = true;
            lblRegistroPatronal.Font = new Font("Constantia", 14.25F, FontStyle.Bold | FontStyle.Italic);
            lblRegistroPatronal.Location = new Point(12, 248);
            lblRegistroPatronal.Name = "lblRegistroPatronal";
            lblRegistroPatronal.Size = new Size(174, 23);
            lblRegistroPatronal.TabIndex = 7;
            lblRegistroPatronal.Text = "Registro Patronal:";
            // 
            // lblRFCEmpresa
            // 
            lblRFCEmpresa.AutoSize = true;
            lblRFCEmpresa.Font = new Font("Constantia", 14.25F, FontStyle.Bold | FontStyle.Italic);
            lblRFCEmpresa.Location = new Point(53, 278);
            lblRFCEmpresa.Name = "lblRFCEmpresa";
            lblRFCEmpresa.Size = new Size(133, 23);
            lblRFCEmpresa.TabIndex = 8;
            lblRFCEmpresa.Text = "RFC Empresa:";
            lblRFCEmpresa.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDireccion
            // 
            lblDireccion.AutoSize = true;
            lblDireccion.Font = new Font("Constantia", 14.25F, FontStyle.Bold | FontStyle.Italic);
            lblDireccion.Location = new Point(85, 314);
            lblDireccion.Name = "lblDireccion";
            lblDireccion.Size = new Size(101, 23);
            lblDireccion.TabIndex = 9;
            lblDireccion.Text = "Dirección:";
            // 
            // ldlDatosRepresentanteLegal
            // 
            ldlDatosRepresentanteLegal.AutoSize = true;
            ldlDatosRepresentanteLegal.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            ldlDatosRepresentanteLegal.Location = new Point(155, 361);
            ldlDatosRepresentanteLegal.Name = "ldlDatosRepresentanteLegal";
            ldlDatosRepresentanteLegal.Size = new Size(223, 16);
            ldlDatosRepresentanteLegal.TabIndex = 10;
            ldlDatosRepresentanteLegal.Text = "Datos del Representante Legal";
            // 
            // lblApellidoPaterno
            // 
            lblApellidoPaterno.AutoSize = true;
            lblApellidoPaterno.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            lblApellidoPaterno.Location = new Point(50, 389);
            lblApellidoPaterno.Name = "lblApellidoPaterno";
            lblApellidoPaterno.Size = new Size(123, 16);
            lblApellidoPaterno.TabIndex = 11;
            lblApellidoPaterno.Text = "Apellido Paterno";
            // 
            // lblApellidoMaterno
            // 
            lblApellidoMaterno.AutoSize = true;
            lblApellidoMaterno.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            lblApellidoMaterno.Location = new Point(206, 389);
            lblApellidoMaterno.Name = "lblApellidoMaterno";
            lblApellidoMaterno.Size = new Size(125, 16);
            lblApellidoMaterno.TabIndex = 12;
            lblApellidoMaterno.Text = "Apellido Materno";
            // 
            // lblNombre
            // 
            lblNombre.AutoSize = true;
            lblNombre.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            lblNombre.Location = new Point(403, 389);
            lblNombre.Name = "lblNombre";
            lblNombre.Size = new Size(62, 16);
            lblNombre.TabIndex = 13;
            lblNombre.Text = "Nombre";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            label2.Location = new Point(34, 452);
            label2.Name = "label2";
            label2.Size = new Size(190, 16);
            label2.TabIndex = 14;
            label2.Text = "RFC Representante Legal:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            label3.Location = new Point(22, 492);
            label3.Name = "label3";
            label3.Size = new Size(202, 16);
            label3.TabIndex = 15;
            label3.Text = "CURP Representante Legal:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            label4.Location = new Point(135, 525);
            label4.Name = "label4";
            label4.Size = new Size(274, 16);
            label4.TabIndex = 16;
            label4.Text = "Datos de Cuenta Bancaria (BANAMEX)";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            label5.Location = new Point(102, 552);
            label5.Name = "label5";
            label5.Size = new Size(67, 16);
            label5.TabIndex = 17;
            label5.Text = "Sucursal";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            label6.Location = new Point(199, 552);
            label6.Name = "label6";
            label6.Size = new Size(135, 16);
            label6.TabIndex = 18;
            label6.Text = "Número de Cuenta";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            label7.Location = new Point(357, 552);
            label7.Name = "label7";
            label7.Size = new Size(55, 16);
            label7.TabIndex = 19;
            label7.Text = "Cliente";
            // 
            // mskdtxtAnio
            // 
            mskdtxtAnio.Cursor = Cursors.IBeam;
            mskdtxtAnio.Location = new Point(192, 115);
            mskdtxtAnio.Name = "mskdtxtAnio";
            mskdtxtAnio.ReadOnly = true;
            mskdtxtAnio.Size = new Size(100, 23);
            mskdtxtAnio.TabIndex = 20;
            // 
            // txtSalarioMinimoVigente
            // 
            txtSalarioMinimoVigente.Location = new Point(192, 157);
            txtSalarioMinimoVigente.Name = "txtSalarioMinimoVigente";
            txtSalarioMinimoVigente.ReadOnly = true;
            txtSalarioMinimoVigente.Size = new Size(95, 23);
            txtSalarioMinimoVigente.TabIndex = 21;
            // 
            // txtUMA
            // 
            txtUMA.Location = new Point(192, 202);
            txtUMA.Name = "txtUMA";
            txtUMA.ReadOnly = true;
            txtUMA.Size = new Size(100, 23);
            txtUMA.TabIndex = 22;
            // 
            // txtRegistroPatronal
            // 
            txtRegistroPatronal.Location = new Point(192, 248);
            txtRegistroPatronal.Name = "txtRegistroPatronal";
            txtRegistroPatronal.ReadOnly = true;
            txtRegistroPatronal.Size = new Size(317, 23);
            txtRegistroPatronal.TabIndex = 23;
            // 
            // txtRFCEmpresa
            // 
            txtRFCEmpresa.Location = new Point(192, 281);
            txtRFCEmpresa.Name = "txtRFCEmpresa";
            txtRFCEmpresa.ReadOnly = true;
            txtRFCEmpresa.Size = new Size(317, 23);
            txtRFCEmpresa.TabIndex = 24;
            // 
            // txtDireccion
            // 
            txtDireccion.Location = new Point(192, 314);
            txtDireccion.Name = "txtDireccion";
            txtDireccion.ReadOnly = true;
            txtDireccion.Size = new Size(317, 23);
            txtDireccion.TabIndex = 25;
            // 
            // txtAPaterno
            // 
            txtAPaterno.Location = new Point(29, 408);
            txtAPaterno.Name = "txtAPaterno";
            txtAPaterno.ReadOnly = true;
            txtAPaterno.Size = new Size(157, 23);
            txtAPaterno.TabIndex = 26;
            // 
            // txtAMaterno
            // 
            txtAMaterno.Location = new Point(192, 408);
            txtAMaterno.Name = "txtAMaterno";
            txtAMaterno.ReadOnly = true;
            txtAMaterno.Size = new Size(157, 23);
            txtAMaterno.TabIndex = 27;
            // 
            // txtNombre
            // 
            txtNombre.Location = new Point(355, 408);
            txtNombre.Name = "txtNombre";
            txtNombre.ReadOnly = true;
            txtNombre.Size = new Size(157, 23);
            txtNombre.TabIndex = 28;
            // 
            // txtRFCRepresentante
            // 
            txtRFCRepresentante.Location = new Point(230, 450);
            txtRFCRepresentante.Name = "txtRFCRepresentante";
            txtRFCRepresentante.ReadOnly = true;
            txtRFCRepresentante.Size = new Size(282, 23);
            txtRFCRepresentante.TabIndex = 29;
            // 
            // txtCURPRrepresentante
            // 
            txtCURPRrepresentante.Location = new Point(230, 488);
            txtCURPRrepresentante.Name = "txtCURPRrepresentante";
            txtCURPRrepresentante.ReadOnly = true;
            txtCURPRrepresentante.Size = new Size(282, 23);
            txtCURPRrepresentante.TabIndex = 30;
            // 
            // txtSucursal
            // 
            txtSucursal.Location = new Point(82, 571);
            txtSucursal.Name = "txtSucursal";
            txtSucursal.Size = new Size(111, 23);
            txtSucursal.TabIndex = 31;
            // 
            // txtNoCuenta
            // 
            txtNoCuenta.Location = new Point(210, 571);
            txtNoCuenta.Name = "txtNoCuenta";
            txtNoCuenta.Size = new Size(111, 23);
            txtNoCuenta.TabIndex = 32;
            // 
            // txtCliente
            // 
            txtCliente.Location = new Point(334, 571);
            txtCliente.Name = "txtCliente";
            txtCliente.Size = new Size(111, 23);
            txtCliente.TabIndex = 33;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(95, 606);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(98, 33);
            btnCancelar.TabIndex = 34;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // btnModificar
            // 
            btnModificar.Location = new Point(219, 606);
            btnModificar.Name = "btnModificar";
            btnModificar.Size = new Size(98, 33);
            btnModificar.TabIndex = 35;
            btnModificar.Text = "Modificar";
            btnModificar.UseVisualStyleBackColor = true;
            btnModificar.Click += btnModificar_Click;
            // 
            // btnGuardar
            // 
            btnGuardar.Location = new Point(334, 606);
            btnGuardar.Name = "btnGuardar";
            btnGuardar.Size = new Size(98, 33);
            btnGuardar.TabIndex = 36;
            btnGuardar.Text = "Guardar";
            btnGuardar.UseVisualStyleBackColor = true;
            btnGuardar.Click += btnGuardar_Click;
            // 
            // FormEmpresaUnificado
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(541, 651);
            Controls.Add(panelNombreEmpresa);
            Controls.Add(btnGuardar);
            Controls.Add(btnModificar);
            Controls.Add(btnCancelar);
            Controls.Add(txtCliente);
            Controls.Add(txtNoCuenta);
            Controls.Add(txtSucursal);
            Controls.Add(txtCURPRrepresentante);
            Controls.Add(txtRFCRepresentante);
            Controls.Add(txtNombre);
            Controls.Add(txtAMaterno);
            Controls.Add(txtAPaterno);
            Controls.Add(txtDireccion);
            Controls.Add(txtRFCEmpresa);
            Controls.Add(txtRegistroPatronal);
            Controls.Add(txtUMA);
            Controls.Add(txtSalarioMinimoVigente);
            Controls.Add(mskdtxtAnio);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(lblNombre);
            Controls.Add(lblApellidoMaterno);
            Controls.Add(lblApellidoPaterno);
            Controls.Add(ldlDatosRepresentanteLegal);
            Controls.Add(lblDireccion);
            Controls.Add(lblRFCEmpresa);
            Controls.Add(lblRegistroPatronal);
            Controls.Add(label1);
            Controls.Add(lblFechaMod);
            Controls.Add(lblUltimaModificacion);
            Controls.Add(lblUMAxDIA);
            Controls.Add(lblSalarioMinimo);
            Controls.Add(lblAnioEmpresa);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "FormEmpresaUnificado";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Datos de la Empresa";
            Load += FormEmpresaUnificado_Load;
            panelNombreEmpresa.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelNombreEmpresa;
        private Label lblAnioEmpresa;
        private Label lblSalarioMinimo;
        private Label lblUMAxDIA;
        private Label lblUltimaModificacion;
        private Label lblFechaMod;
        private Label label1;
        private Label lblRegistroPatronal;
        private Label lblRFCEmpresa;
        private Label lblDireccion;
        private Label ldlDatosRepresentanteLegal;
        private Label lblApellidoPaterno;
        private Label lblApellidoMaterno;
        private Label lblNombre;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private MaskedTextBox mskdtxtAnio;
        private TextBox txtSalarioMinimoVigente;
        private TextBox txtUMA;
        private TextBox txtRegistroPatronal;
        private TextBox txtRFCEmpresa;
        private TextBox txtDireccion;
        private TextBox txtAPaterno;
        private TextBox txtAMaterno;
        private TextBox txtNombre;
        private TextBox txtRFCRepresentante;
        private TextBox txtCURPRrepresentante;
        private TextBox txtSucursal;
        private TextBox txtNoCuenta;
        private TextBox txtCliente;
        private Button btnCancelar;
        private Button btnModificar;
        private Button btnGuardar;
        private Label lblEmpresa;
    }
}
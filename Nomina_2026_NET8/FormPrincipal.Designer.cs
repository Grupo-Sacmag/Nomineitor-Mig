using System.Windows.Forms;

namespace NOMINA_2025
{
    partial class FormPrincipal
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormPrincipal));
            tsOpcionesNom = new ToolStrip();
            toolStripDropDownButton1 = new ToolStripDropDownButton();
            tsCRUDPersonal = new ToolStripMenuItem();
            tsmImpresion = new ToolStripMenuItem();
            toolStripDropDownButton2 = new ToolStripDropDownButton();
            tsmIniciarCaptura = new ToolStripMenuItem();
            tsmImpresionNomina = new ToolStripMenuItem();
            toolStripDropDownButton5 = new ToolStripDropDownButton();
            tsDatosDeLaEmpresa = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            tsMostrarTarifasImpuestos = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            tsAcercaDe = new ToolStripMenuItem();
            tsMostrar = new ToolStripDropDownButton();
            tsFiltrarArchivos = new ToolStripMenuItem();
            tsMostrarJSON = new ToolStripMenuItem();
            tsMostrarNOM = new ToolStripMenuItem();
            tsAcumulado = new ToolStripDropDownButton();
            AcumJSON = new ToolStripMenuItem();
            AcumNOM = new ToolStripMenuItem();
            iniciarCapturaToolStripMenuItem1 = new ToolStripMenuItem();
            panelEmpresaNombre = new Panel();
            Label = new Label();
            lblEmpresa = new Label();
            panelDatosEmpresa = new Panel();
            lblUMA = new Label();
            lblSalarioMinimo = new Label();
            lblAnio = new Label();
            lblNUMA = new Label();
            lblNSalarioMinimo = new Label();
            lblNAnio = new Label();
            treeViewDirectorios = new TreeView();
            imageListTree = new ImageList(components);
            listBoxArchivos = new ListBox();
            label5 = new Label();
            panel1 = new Panel();
            btnBuscarRutaDirectorio = new Button();
            txtBuscarRutaDirectorio = new TextBox();
            lblInsertarRuta = new Label();
            label2 = new Label();
            cbUnidades = new ComboBox();
            label3 = new Label();
            lblRutaDirectorio = new Label();
            label1 = new Label();
            tsOpcionesNom.SuspendLayout();
            panelEmpresaNombre.SuspendLayout();
            panelDatosEmpresa.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tsOpcionesNom
            // 
            tsOpcionesNom.Items.AddRange(new ToolStripItem[] { toolStripDropDownButton1, toolStripDropDownButton2, toolStripDropDownButton5, tsMostrar, tsAcumulado });
            tsOpcionesNom.Location = new Point(0, 0);
            tsOpcionesNom.Name = "tsOpcionesNom";
            tsOpcionesNom.Size = new Size(762, 25);
            tsOpcionesNom.TabIndex = 0;
            tsOpcionesNom.Text = "toolStrip1";
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripDropDownButton1.DropDownItems.AddRange(new ToolStripItem[] { tsCRUDPersonal, tsmImpresion });
            toolStripDropDownButton1.ImageTransparentColor = Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.Size = new Size(65, 22);
            toolStripDropDownButton1.Text = "Personal";
            // 
            // tsCRUDPersonal
            // 
            tsCRUDPersonal.Name = "tsCRUDPersonal";
            tsCRUDPersonal.ShortcutKeys = Keys.Control | Keys.Shift | Keys.E;
            tsCRUDPersonal.Size = new Size(317, 22);
            tsCRUDPersonal.Text = "Agregar o Editar Personal";
            tsCRUDPersonal.Click += tsCRUDPersonal_Click;
            // 
            // tsmImpresion
            // 
            tsmImpresion.Name = "tsmImpresion";
            tsmImpresion.Size = new Size(317, 22);
            tsmImpresion.Text = "Impresión";
            tsmImpresion.Click += tsmImpresion_Click;
            // 
            // toolStripDropDownButton2
            // 
            toolStripDropDownButton2.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripDropDownButton2.DropDownItems.AddRange(new ToolStripItem[] { tsmIniciarCaptura, tsmImpresionNomina });
            toolStripDropDownButton2.Image = (Image)resources.GetObject("toolStripDropDownButton2.Image");
            toolStripDropDownButton2.ImageTransparentColor = Color.Magenta;
            toolStripDropDownButton2.Name = "toolStripDropDownButton2";
            toolStripDropDownButton2.Size = new Size(63, 22);
            toolStripDropDownButton2.Text = "Nómina";
            // 
            // tsmIniciarCaptura
            // 
            tsmIniciarCaptura.Name = "tsmIniciarCaptura";
            tsmIniciarCaptura.ShortcutKeys = Keys.Control | Keys.I;
            tsmIniciarCaptura.Size = new Size(238, 22);
            tsmIniciarCaptura.Text = "Captura";
            tsmIniciarCaptura.Click += tsmIniciarCaptura_Click;
            // 
            // tsmImpresionNomina
            // 
            tsmImpresionNomina.Name = "tsmImpresionNomina";
            tsmImpresionNomina.ShortcutKeys = Keys.Control | Keys.Shift | Keys.P;
            tsmImpresionNomina.Size = new Size(238, 22);
            tsmImpresionNomina.Text = "Impresión";
            tsmImpresionNomina.Click += tsmImpresionNomina_Click;
            // 
            // toolStripDropDownButton5
            // 
            toolStripDropDownButton5.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripDropDownButton5.DropDownItems.AddRange(new ToolStripItem[] { tsDatosDeLaEmpresa, toolStripSeparator2, tsMostrarTarifasImpuestos, toolStripSeparator3, tsAcercaDe });
            toolStripDropDownButton5.Image = (Image)resources.GetObject("toolStripDropDownButton5.Image");
            toolStripDropDownButton5.ImageTransparentColor = Color.Magenta;
            toolStripDropDownButton5.Name = "toolStripDropDownButton5";
            toolStripDropDownButton5.Size = new Size(96, 22);
            toolStripDropDownButton5.Text = "Configuración";
            // 
            // tsDatosDeLaEmpresa
            // 
            tsDatosDeLaEmpresa.Name = "tsDatosDeLaEmpresa";
            tsDatosDeLaEmpresa.Size = new Size(181, 22);
            tsDatosDeLaEmpresa.Text = "Datos de la Empresa";
            tsDatosDeLaEmpresa.Click += tsDatosDeLaEmpresa_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(178, 6);
            // 
            // tsMostrarTarifasImpuestos
            // 
            tsMostrarTarifasImpuestos.Name = "tsMostrarTarifasImpuestos";
            tsMostrarTarifasImpuestos.Size = new Size(181, 22);
            tsMostrarTarifasImpuestos.Text = "Tablas de Impuestos";
            tsMostrarTarifasImpuestos.Click += tsMostrarTarifasImpuestos_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(178, 6);
            // 
            // tsAcercaDe
            // 
            tsAcercaDe.Name = "tsAcercaDe";
            tsAcercaDe.Size = new Size(181, 22);
            tsAcercaDe.Text = "Acerca de...";
            tsAcercaDe.Click += tsAcercaDe_Click;
            // 
            // tsMostrar
            // 
            tsMostrar.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsMostrar.DropDownItems.AddRange(new ToolStripItem[] { tsFiltrarArchivos });
            tsMostrar.Image = (Image)resources.GetObject("tsMostrar.Image");
            tsMostrar.ImageTransparentColor = Color.Magenta;
            tsMostrar.Name = "tsMostrar";
            tsMostrar.Size = new Size(61, 22);
            tsMostrar.Text = "Mostrar";
            // 
            // tsFiltrarArchivos
            // 
            tsFiltrarArchivos.DropDownItems.AddRange(new ToolStripItem[] { tsMostrarJSON, tsMostrarNOM });
            tsFiltrarArchivos.Name = "tsFiltrarArchivos";
            tsFiltrarArchivos.Size = new Size(153, 22);
            tsFiltrarArchivos.Text = "Filtrar Archivos";
            // 
            // tsMostrarJSON
            // 
            tsMostrarJSON.Name = "tsMostrarJSON";
            tsMostrarJSON.Size = new Size(147, 22);
            tsMostrarJSON.Text = "Mostrar JSON";
            tsMostrarJSON.Click += tsMostrarJSON_Click;
            // 
            // tsMostrarNOM
            // 
            tsMostrarNOM.Name = "tsMostrarNOM";
            tsMostrarNOM.Size = new Size(147, 22);
            tsMostrarNOM.Text = "Mostrar NOM";
            tsMostrarNOM.Click += tsMostrarNOM_Click;
            // 
            // tsAcumulado
            // 
            tsAcumulado.DropDownItems.AddRange(new ToolStripItem[] { AcumJSON, AcumNOM });
            tsAcumulado.Name = "tsAcumulado";
            tsAcumulado.Size = new Size(82, 22);
            tsAcumulado.Text = "Acumulado";
            // 
            // AcumJSON
            // 
            AcumJSON.Name = "AcumJSON";
            AcumJSON.Size = new Size(155, 22);
            AcumJSON.Text = "Archivos .JSON";
            AcumJSON.Click += AcumJSON_Click;
            // 
            // AcumNOM
            // 
            AcumNOM.Name = "AcumNOM";
            AcumNOM.Size = new Size(155, 22);
            AcumNOM.Text = "Archivos .NOM";
            AcumNOM.Click += AcumNOM_Click;
            // 
            // iniciarCapturaToolStripMenuItem1
            // 
            iniciarCapturaToolStripMenuItem1.Name = "iniciarCapturaToolStripMenuItem1";
            iniciarCapturaToolStripMenuItem1.Size = new Size(151, 22);
            iniciarCapturaToolStripMenuItem1.Text = "Iniciar Captura";
            // 
            // panelEmpresaNombre
            // 
            panelEmpresaNombre.BorderStyle = BorderStyle.FixedSingle;
            panelEmpresaNombre.Controls.Add(Label);
            panelEmpresaNombre.Controls.Add(lblEmpresa);
            panelEmpresaNombre.Font = new Font("Microsoft Sans Serif", 8.25F);
            panelEmpresaNombre.Location = new Point(16, 118);
            panelEmpresaNombre.Margin = new Padding(4, 3, 4, 3);
            panelEmpresaNombre.Name = "panelEmpresaNombre";
            panelEmpresaNombre.Size = new Size(350, 225);
            panelEmpresaNombre.TabIndex = 3;
            // 
            // Label
            // 
            Label.AutoSize = true;
            Label.Font = new Font("Century Schoolbook", 15.75F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            Label.Location = new Point(106, 16);
            Label.Margin = new Padding(4, 0, 4, 0);
            Label.Name = "Label";
            Label.Size = new Size(115, 25);
            Label.TabIndex = 16;
            Label.Text = "Empresa:";
            // 
            // lblEmpresa
            // 
            lblEmpresa.Dock = DockStyle.Fill;
            lblEmpresa.Font = new Font("Lucida Fax", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblEmpresa.Location = new Point(0, 0);
            lblEmpresa.Margin = new Padding(4, 0, 4, 0);
            lblEmpresa.Name = "lblEmpresa";
            lblEmpresa.Size = new Size(348, 223);
            lblEmpresa.TabIndex = 8;
            lblEmpresa.Text = "-";
            lblEmpresa.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelDatosEmpresa
            // 
            panelDatosEmpresa.BorderStyle = BorderStyle.FixedSingle;
            panelDatosEmpresa.Controls.Add(lblUMA);
            panelDatosEmpresa.Controls.Add(lblSalarioMinimo);
            panelDatosEmpresa.Controls.Add(lblAnio);
            panelDatosEmpresa.Controls.Add(lblNUMA);
            panelDatosEmpresa.Controls.Add(lblNSalarioMinimo);
            panelDatosEmpresa.Controls.Add(lblNAnio);
            panelDatosEmpresa.Font = new Font("Microsoft Sans Serif", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            panelDatosEmpresa.Location = new Point(16, 368);
            panelDatosEmpresa.Margin = new Padding(4, 3, 4, 3);
            panelDatosEmpresa.Name = "panelDatosEmpresa";
            panelDatosEmpresa.Size = new Size(350, 242);
            panelDatosEmpresa.TabIndex = 0;
            // 
            // lblUMA
            // 
            lblUMA.AutoSize = true;
            lblUMA.Font = new Font("Lucida Fax", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblUMA.Location = new Point(220, 156);
            lblUMA.Margin = new Padding(4, 0, 4, 0);
            lblUMA.Name = "lblUMA";
            lblUMA.Size = new Size(17, 24);
            lblUMA.TabIndex = 5;
            lblUMA.Text = "-";
            // 
            // lblSalarioMinimo
            // 
            lblSalarioMinimo.AutoSize = true;
            lblSalarioMinimo.Font = new Font("Lucida Fax", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSalarioMinimo.Location = new Point(220, 104);
            lblSalarioMinimo.Margin = new Padding(4, 0, 4, 0);
            lblSalarioMinimo.Name = "lblSalarioMinimo";
            lblSalarioMinimo.Size = new Size(17, 24);
            lblSalarioMinimo.TabIndex = 4;
            lblSalarioMinimo.Text = "-";
            // 
            // lblAnio
            // 
            lblAnio.AutoSize = true;
            lblAnio.Font = new Font("Lucida Fax", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblAnio.Location = new Point(220, 58);
            lblAnio.Margin = new Padding(4, 0, 4, 0);
            lblAnio.Name = "lblAnio";
            lblAnio.Size = new Size(17, 24);
            lblAnio.TabIndex = 3;
            lblAnio.Text = "-";
            // 
            // lblNUMA
            // 
            lblNUMA.AutoSize = true;
            lblNUMA.Font = new Font("Century Schoolbook", 15.75F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lblNUMA.Location = new Point(34, 155);
            lblNUMA.Margin = new Padding(4, 0, 4, 0);
            lblNUMA.Name = "lblNUMA";
            lblNUMA.Size = new Size(161, 25);
            lblNUMA.TabIndex = 2;
            lblNUMA.Text = "UMA por Día:";
            // 
            // lblNSalarioMinimo
            // 
            lblNSalarioMinimo.AutoSize = true;
            lblNSalarioMinimo.Font = new Font("Century Schoolbook", 15.75F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lblNSalarioMinimo.Location = new Point(4, 103);
            lblNSalarioMinimo.Margin = new Padding(4, 0, 4, 0);
            lblNSalarioMinimo.Name = "lblNSalarioMinimo";
            lblNSalarioMinimo.Size = new Size(187, 25);
            lblNSalarioMinimo.TabIndex = 1;
            lblNSalarioMinimo.Text = "Salario Mínimo:";
            // 
            // lblNAnio
            // 
            lblNAnio.AutoSize = true;
            lblNAnio.Font = new Font("Century Schoolbook", 15.75F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lblNAnio.Location = new Point(152, 57);
            lblNAnio.Margin = new Padding(4, 0, 4, 0);
            lblNAnio.Name = "lblNAnio";
            lblNAnio.Size = new Size(60, 25);
            lblNAnio.TabIndex = 0;
            lblNAnio.Text = "Año:";
            // 
            // treeViewDirectorios
            // 
            treeViewDirectorios.ImageIndex = 0;
            treeViewDirectorios.ImageList = imageListTree;
            treeViewDirectorios.Location = new Point(391, 118);
            treeViewDirectorios.Margin = new Padding(4, 3, 4, 3);
            treeViewDirectorios.Name = "treeViewDirectorios";
            treeViewDirectorios.SelectedImageIndex = 1;
            treeViewDirectorios.Size = new Size(349, 224);
            treeViewDirectorios.TabIndex = 1;
            treeViewDirectorios.AfterCollapse += treeViewDirectorios_AfterCollapse;
            treeViewDirectorios.BeforeExpand += treeViewDirectorios_BeforeExpand;
            treeViewDirectorios.DrawNode += treeViewDirectorios_DrawNode;
            treeViewDirectorios.AfterSelect += treeViewDirectorios_AfterSelect;
            treeViewDirectorios.KeyDown += treeViewDirectorios_KeyDown;
            // 
            // imageListTree
            // 
            imageListTree.ColorDepth = ColorDepth.Depth8Bit;
            imageListTree.ImageStream = (ImageListStreamer)resources.GetObject("imageListTree.ImageStream");
            imageListTree.TransparentColor = Color.Transparent;
            imageListTree.Images.SetKeyName(0, "FOLDER.png");
            imageListTree.Images.SetKeyName(1, "FOLDERABIERTO.png");
            // 
            // listBoxArchivos
            // 
            listBoxArchivos.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            listBoxArchivos.FormattingEnabled = true;
            listBoxArchivos.ItemHeight = 17;
            listBoxArchivos.Location = new Point(391, 368);
            listBoxArchivos.Margin = new Padding(4, 3, 4, 3);
            listBoxArchivos.Name = "listBoxArchivos";
            listBoxArchivos.Size = new Size(349, 242);
            listBoxArchivos.TabIndex = 2;
            listBoxArchivos.SelectedIndexChanged += listBoxArchivos_SelectedIndexChanged;
            listBoxArchivos.DoubleClick += listBoxArchivos_DoubleClick;
            listBoxArchivos.KeyDown += listBoxArchivos_KeyDown;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label5.Location = new Point(391, 352);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(54, 13);
            label5.TabIndex = 13;
            label5.Text = "Archivos: ";
            // 
            // panel1
            // 
            panel1.Controls.Add(btnBuscarRutaDirectorio);
            panel1.Controls.Add(txtBuscarRutaDirectorio);
            panel1.Controls.Add(lblInsertarRuta);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(cbUnidades);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(lblRutaDirectorio);
            panel1.Controls.Add(label1);
            panel1.Location = new Point(12, 28);
            panel1.Name = "panel1";
            panel1.Size = new Size(738, 84);
            panel1.TabIndex = 14;
            // 
            // btnBuscarRutaDirectorio
            // 
            btnBuscarRutaDirectorio.Location = new Point(646, 6);
            btnBuscarRutaDirectorio.Margin = new Padding(4, 3, 4, 3);
            btnBuscarRutaDirectorio.Name = "btnBuscarRutaDirectorio";
            btnBuscarRutaDirectorio.Size = new Size(88, 27);
            btnBuscarRutaDirectorio.TabIndex = 20;
            btnBuscarRutaDirectorio.Text = "Buscar";
            btnBuscarRutaDirectorio.UseVisualStyleBackColor = true;
            btnBuscarRutaDirectorio.Click += btnBuscarDirectorio_Click;
            // 
            // txtBuscarRutaDirectorio
            // 
            txtBuscarRutaDirectorio.Location = new Point(236, 9);
            txtBuscarRutaDirectorio.Margin = new Padding(4, 3, 4, 3);
            txtBuscarRutaDirectorio.Name = "txtBuscarRutaDirectorio";
            txtBuscarRutaDirectorio.Size = new Size(402, 23);
            txtBuscarRutaDirectorio.TabIndex = 19;
            txtBuscarRutaDirectorio.KeyDown += txtBuscarRutaDirectorio_KeyDown;
            // 
            // lblInsertarRuta
            // 
            lblInsertarRuta.AutoSize = true;
            lblInsertarRuta.Font = new Font("Microsoft YaHei UI", 9.75F);
            lblInsertarRuta.Location = new Point(4, 9);
            lblInsertarRuta.Margin = new Padding(4, 0, 4, 0);
            lblInsertarRuta.Name = "lblInsertarRuta";
            lblInsertarRuta.Size = new Size(236, 19);
            lblInsertarRuta.TabIndex = 25;
            lblInsertarRuta.Text = "Pega y busca el directorio completo: ";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft YaHei UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.Location = new Point(587, 42);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(56, 19);
            label2.TabIndex = 24;
            label2.Text = "Unidad:";
            // 
            // cbUnidades
            // 
            cbUnidades.FormattingEnabled = true;
            cbUnidades.Location = new Point(659, 40);
            cbUnidades.Margin = new Padding(4, 3, 4, 3);
            cbUnidades.Name = "cbUnidades";
            cbUnidades.Size = new Size(74, 23);
            cbUnidades.TabIndex = 23;
            cbUnidades.SelectionChangeCommitted += cbUnidades_SelectionChangeCommitted;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft YaHei UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.Location = new Point(87, 1);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(0, 19);
            label3.TabIndex = 21;
            // 
            // lblRutaDirectorio
            // 
            lblRutaDirectorio.AutoSize = true;
            lblRutaDirectorio.Font = new Font("Microsoft YaHei UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRutaDirectorio.Location = new Point(5, 59);
            lblRutaDirectorio.Margin = new Padding(4, 0, 4, 0);
            lblRutaDirectorio.Name = "lblRutaDirectorio";
            lblRutaDirectorio.Size = new Size(15, 19);
            lblRutaDirectorio.TabIndex = 22;
            lblRutaDirectorio.Text = "-";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft YaHei UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(4, 40);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(158, 19);
            label1.TabIndex = 18;
            label1.Text = "Directorio Seleccionado:";
            // 
            // FormPrincipal
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(762, 629);
            Controls.Add(panel1);
            Controls.Add(label5);
            Controls.Add(listBoxArchivos);
            Controls.Add(treeViewDirectorios);
            Controls.Add(tsOpcionesNom);
            Controls.Add(panelDatosEmpresa);
            Controls.Add(panelEmpresaNombre);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            Name = "FormPrincipal";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Nómina Empresarial 2026 V1.0.0";
            FormClosing += FormPrincipal_FormClosing;
            Load += FormPrincipal_Load;
            Shown += FormPrincipal_Shown;
            tsOpcionesNom.ResumeLayout(false);
            tsOpcionesNom.PerformLayout();
            panelEmpresaNombre.ResumeLayout(false);
            panelEmpresaNombre.PerformLayout();
            panelDatosEmpresa.ResumeLayout(false);
            panelDatosEmpresa.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip tsOpcionesNom;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem tsmImpresion;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton2;
        private System.Windows.Forms.ToolStripMenuItem iniciarCapturaToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem tsmImpresionNomina;
        private System.Windows.Forms.ToolStripDropDownButton tsMostrar;
        private System.Windows.Forms.Panel panelEmpresaNombre;
        private System.Windows.Forms.Panel panelDatosEmpresa;
        private System.Windows.Forms.TreeView treeViewDirectorios;
        private System.Windows.Forms.ListBox listBoxArchivos;
        private System.Windows.Forms.ImageList imageListTree;
        private System.Windows.Forms.Label lblNAnio;
        private System.Windows.Forms.Label label5;
        private ToolStripMenuItem tsmIniciarCaptura;
        private Label lblUMA;
        private Label lblSalarioMinimo;
        private Label lblAnio;
        private Label lblNUMA;
        private Label lblNSalarioMinimo;
        private Label lblEmpresa;
        private Label Label;
        private ToolStripMenuItem tsFiltrarArchivos;
        private ToolStripMenuItem tsMostrarJSON;
        private ToolStripMenuItem tsMostrarNOM;
        private ToolStripDropDownButton tsAcumulado;
        private ToolStripMenuItem AcumJSON;
        private ToolStripMenuItem AcumNOM;
        private ToolStripMenuItem tsCRUDPersonal;
        private ToolStripDropDownButton toolStripDropDownButton5;
        private ToolStripMenuItem tsDatosDeLaEmpresa;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem tsMostrarTarifasImpuestos;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem tsAcercaDe;
        private Panel panel1;
        private Button btnBuscarRutaDirectorio;
        private TextBox txtBuscarRutaDirectorio;
        private Label lblInsertarRuta;
        private Label label2;
        private ComboBox cbUnidades;
        private Label label3;
        private Label lblRutaDirectorio;
        private Label label1;
    }
}


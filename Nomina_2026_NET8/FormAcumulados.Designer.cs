namespace Nomina_2026_NET8
{
    partial class FormAcumulados
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAcumulados));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            toolStrip1 = new ToolStrip();
            tsArchivo = new ToolStripDropDownButton();
            tsOrdenar = new ToolStripMenuItem();
            tsNumAscend = new ToolStripMenuItem();
            tsNumDescend = new ToolStripMenuItem();
            tsAlfAscend = new ToolStripMenuItem();
            tsAlfDescend = new ToolStripMenuItem();
            tsImpresion = new ToolStripMenuItem();
            tsOpciones = new ToolStripDropDownButton();
            tsSeleccionarTodo = new ToolStripMenuItem();
            tsSelecTodoListadoDePersonal = new ToolStripMenuItem();
            tsSelectTodoNominasEncontradas = new ToolStripMenuItem();
            opcionesDeAcumulaciónToolStripMenuItem = new ToolStripMenuItem();
            tsSinUltimaNomina = new ToolStripMenuItem();
            tsConUltimaNomina = new ToolStripMenuItem();
            tsOtrosIngresos = new ToolStripMenuItem();
            tsBuscarDuplicados = new ToolStripMenuItem();
            tsGenerarArchivo = new ToolStripMenuItem();
            tsCalculos = new ToolStripDropDownButton();
            tsCalculoIntegral = new ToolStripMenuItem();
            tsCalculoISR = new ToolStripMenuItem();
            label1 = new Label();
            lblBuscar = new Label();
            txtBuscar = new TextBox();
            btnBuscar = new Button();
            lblListadoDePersonal = new Label();
            chckboxRango1 = new CheckBox();
            chckboxRango2 = new CheckBox();
            chckboxRango3 = new CheckBox();
            chckboxRango4 = new CheckBox();
            chckboxRango5 = new CheckBox();
            chckboxRango6 = new CheckBox();
            chckboxRangoPersonalizado = new CheckBox();
            chckboxExcluirEspeciales = new CheckBox();
            chckboxTotalAcumulado = new CheckBox();
            chckboxSoloEmpleadosActivos = new CheckBox();
            label2 = new Label();
            label3 = new Label();
            cbMesInicio = new ComboBox();
            cbMesFinal = new ComboBox();
            dgvPersonalEncontrado = new DataGridView();
            Id = new DataGridViewTextBoxColumn();
            NombreCompleto = new DataGridViewTextBoxColumn();
            RFCEmpleadoE = new DataGridViewTextBoxColumn();
            IMSSE = new DataGridViewTextBoxColumn();
            CURPE = new DataGridViewTextBoxColumn();
            FechaAltaE = new DataGridViewTextBoxColumn();
            FechaBajaE = new DataGridViewTextBoxColumn();
            SalarioDiarioE = new DataGridViewTextBoxColumn();
            ViaticosE = new DataGridViewTextBoxColumn();
            OtrasE = new DataGridViewTextBoxColumn();
            IntegradoE = new DataGridViewTextBoxColumn();
            BanamexE = new DataGridViewTextBoxColumn();
            lblNominasEncontradas = new Label();
            dgvAcumulado = new DataGridView();
            Archivo = new DataGridViewTextBoxColumn();
            DiasT = new DataGridViewTextBoxColumn();
            Sueldo = new DataGridViewTextBoxColumn();
            PremPunt = new DataGridViewTextBoxColumn();
            OF = new DataGridViewTextBoxColumn();
            Pvacacional = new DataGridViewTextBoxColumn();
            Otras = new DataGridViewTextBoxColumn();
            Aguinaldo = new DataGridViewTextBoxColumn();
            PTU = new DataGridViewTextBoxColumn();
            PercExenta = new DataGridViewTextBoxColumn();
            TotalIngr = new DataGridViewTextBoxColumn();
            ISTP = new DataGridViewTextBoxColumn();
            SubPEmpl = new DataGridViewTextBoxColumn();
            CrApl = new DataGridViewTextBoxColumn();
            ImptoRet = new DataGridViewTextBoxColumn();
            CrPag = new DataGridViewTextBoxColumn();
            SubsidioNoApl = new DataGridViewTextBoxColumn();
            IMSS = new DataGridViewTextBoxColumn();
            Prestamos = new DataGridViewTextBoxColumn();
            PensionAlimenticia = new DataGridViewTextBoxColumn();
            Fonacot = new DataGridViewTextBoxColumn();
            Infonavit = new DataGridViewTextBoxColumn();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPersonalEncontrado).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvAcumulado).BeginInit();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsArchivo, tsOpciones, tsCalculos });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(1904, 25);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsArchivo
            // 
            tsArchivo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsArchivo.DropDownItems.AddRange(new ToolStripItem[] { tsOrdenar, tsImpresion });
            tsArchivo.Image = (Image)resources.GetObject("tsArchivo.Image");
            tsArchivo.ImageTransparentColor = Color.Magenta;
            tsArchivo.Name = "tsArchivo";
            tsArchivo.Size = new Size(61, 22);
            tsArchivo.Text = "Archivo";
            // 
            // tsOrdenar
            // 
            tsOrdenar.DropDownItems.AddRange(new ToolStripItem[] { tsNumAscend, tsNumDescend, tsAlfAscend, tsAlfDescend });
            tsOrdenar.Name = "tsOrdenar";
            tsOrdenar.Size = new Size(127, 22);
            tsOrdenar.Text = "Ordenar";
            // 
            // tsNumAscend
            // 
            tsNumAscend.Name = "tsNumAscend";
            tsNumAscend.Size = new Size(244, 22);
            tsNumAscend.Text = "Numérico Ascendente (1 - 100)";
            tsNumAscend.Click += tsNumAscend_Click;
            // 
            // tsNumDescend
            // 
            tsNumDescend.Name = "tsNumDescend";
            tsNumDescend.Size = new Size(244, 22);
            tsNumDescend.Text = "Numérico Descendente (100 - 1)";
            tsNumDescend.Click += tsNumDescend_Click;
            // 
            // tsAlfAscend
            // 
            tsAlfAscend.Name = "tsAlfAscend";
            tsAlfAscend.Size = new Size(244, 22);
            tsAlfAscend.Text = "Alfabético Ascendente ( A - Z)";
            tsAlfAscend.Click += tsAlfAscend_Click;
            // 
            // tsAlfDescend
            // 
            tsAlfDescend.Name = "tsAlfDescend";
            tsAlfDescend.Size = new Size(244, 22);
            tsAlfDescend.Text = "Alfabético Descendente (Z - A)";
            tsAlfDescend.Click += tsAlfDescend_Click;
            // 
            // tsImpresion
            // 
            tsImpresion.Name = "tsImpresion";
            tsImpresion.Size = new Size(127, 22);
            tsImpresion.Text = "Impresión";
            tsImpresion.Click += tsImpresion_Click;
            // 
            // tsOpciones
            // 
            tsOpciones.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsOpciones.DropDownItems.AddRange(new ToolStripItem[] { tsSeleccionarTodo, opcionesDeAcumulaciónToolStripMenuItem, tsOtrosIngresos, tsBuscarDuplicados, tsGenerarArchivo });
            tsOpciones.Image = (Image)resources.GetObject("tsOpciones.Image");
            tsOpciones.ImageTransparentColor = Color.Magenta;
            tsOpciones.Name = "tsOpciones";
            tsOpciones.Size = new Size(70, 22);
            tsOpciones.Text = "Opciones";
            // 
            // tsSeleccionarTodo
            // 
            tsSeleccionarTodo.DropDownItems.AddRange(new ToolStripItem[] { tsSelecTodoListadoDePersonal, tsSelectTodoNominasEncontradas });
            tsSeleccionarTodo.Name = "tsSeleccionarTodo";
            tsSeleccionarTodo.Size = new Size(214, 22);
            tsSeleccionarTodo.Text = "Seleccionar Todo";
            // 
            // tsSelecTodoListadoDePersonal
            // 
            tsSelecTodoListadoDePersonal.Name = "tsSelecTodoListadoDePersonal";
            tsSelecTodoListadoDePersonal.Size = new Size(190, 22);
            tsSelecTodoListadoDePersonal.Text = "Listado de Personal";
            tsSelecTodoListadoDePersonal.Click += tsSelecTodoListadoDePersonal_Click;
            // 
            // tsSelectTodoNominasEncontradas
            // 
            tsSelectTodoNominasEncontradas.Name = "tsSelectTodoNominasEncontradas";
            tsSelectTodoNominasEncontradas.Size = new Size(190, 22);
            tsSelectTodoNominasEncontradas.Text = "Nóminas Encontradas";
            tsSelectTodoNominasEncontradas.Click += tsSelectTodoNominasEncontradas_Click;
            // 
            // opcionesDeAcumulaciónToolStripMenuItem
            // 
            opcionesDeAcumulaciónToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { tsSinUltimaNomina, tsConUltimaNomina });
            opcionesDeAcumulaciónToolStripMenuItem.Name = "opcionesDeAcumulaciónToolStripMenuItem";
            opcionesDeAcumulaciónToolStripMenuItem.Size = new Size(214, 22);
            opcionesDeAcumulaciónToolStripMenuItem.Text = "Opciones de Acumulación";
            // 
            // tsSinUltimaNomina
            // 
            tsSinUltimaNomina.Name = "tsSinUltimaNomina";
            tsSinUltimaNomina.Size = new Size(177, 22);
            tsSinUltimaNomina.Text = "Sin última nómina";
            tsSinUltimaNomina.Click += tsSinUltimaNomina_Click;
            // 
            // tsConUltimaNomina
            // 
            tsConUltimaNomina.Name = "tsConUltimaNomina";
            tsConUltimaNomina.Size = new Size(177, 22);
            tsConUltimaNomina.Text = "Con última nómina";
            tsConUltimaNomina.Click += tsConUltimaNomina_Click;
            // 
            // tsOtrosIngresos
            // 
            tsOtrosIngresos.Name = "tsOtrosIngresos";
            tsOtrosIngresos.Size = new Size(214, 22);
            tsOtrosIngresos.Text = "Otros Ingresos";
            tsOtrosIngresos.Click += tsOtrosIngresos_Click;
            // 
            // tsBuscarDuplicados
            // 
            tsBuscarDuplicados.Name = "tsBuscarDuplicados";
            tsBuscarDuplicados.Size = new Size(214, 22);
            tsBuscarDuplicados.Text = "Buscar Duplicados";
            tsBuscarDuplicados.Click += tsBuscarDuplicados_Click;
            // 
            // tsGenerarArchivo
            // 
            tsGenerarArchivo.Name = "tsGenerarArchivo";
            tsGenerarArchivo.Size = new Size(214, 22);
            tsGenerarArchivo.Text = "Generar Archivo";
            tsGenerarArchivo.Click += tsGenerarArchivo_Click;
            // 
            // tsCalculos
            // 
            tsCalculos.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsCalculos.DropDownItems.AddRange(new ToolStripItem[] { tsCalculoIntegral, tsCalculoISR });
            tsCalculos.Image = (Image)resources.GetObject("tsCalculos.Image");
            tsCalculos.ImageTransparentColor = Color.Magenta;
            tsCalculos.Name = "tsCalculos";
            tsCalculos.Size = new Size(65, 22);
            tsCalculos.Text = "Cálculos";
            // 
            // tsCalculoIntegral
            // 
            tsCalculoIntegral.Name = "tsCalculoIntegral";
            tsCalculoIntegral.Size = new Size(157, 22);
            tsCalculoIntegral.Text = "Cálculo Integral";
            tsCalculoIntegral.Click += tsCalculoIntegral_Click;
            // 
            // tsCalculoISR
            // 
            tsCalculoISR.Name = "tsCalculoISR";
            tsCalculoISR.Size = new Size(157, 22);
            tsCalculoISR.Text = "Cálculo ISR";
            tsCalculoISR.Click += tsCalculoISR_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Bold);
            label1.Location = new Point(39, 60);
            label1.Name = "label1";
            label1.Size = new Size(210, 18);
            label1.TabIndex = 1;
            label1.Text = "Filtrar por Rango de Fecha";
            // 
            // lblBuscar
            // 
            lblBuscar.AutoSize = true;
            lblBuscar.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Bold);
            lblBuscar.Location = new Point(297, 33);
            lblBuscar.Name = "lblBuscar";
            lblBuscar.Size = new Size(66, 18);
            lblBuscar.TabIndex = 2;
            lblBuscar.Text = "Buscar:";
            // 
            // txtBuscar
            // 
            txtBuscar.Location = new Point(369, 31);
            txtBuscar.Name = "txtBuscar";
            txtBuscar.Size = new Size(193, 23);
            txtBuscar.TabIndex = 3;
            txtBuscar.TextChanged += txtBuscar_TextChanged;
            txtBuscar.KeyDown += txtBuscar_KeyDown;
            // 
            // btnBuscar
            // 
            btnBuscar.BackgroundImage = (Image)resources.GetObject("btnBuscar.BackgroundImage");
            btnBuscar.BackgroundImageLayout = ImageLayout.Stretch;
            btnBuscar.Location = new Point(568, 29);
            btnBuscar.Name = "btnBuscar";
            btnBuscar.Size = new Size(31, 28);
            btnBuscar.TabIndex = 4;
            btnBuscar.UseVisualStyleBackColor = true;
            btnBuscar.Click += btnBuscar_Click;
            // 
            // lblListadoDePersonal
            // 
            lblListadoDePersonal.AutoSize = true;
            lblListadoDePersonal.Font = new Font("Arial Rounded MT Bold", 12F);
            lblListadoDePersonal.Location = new Point(872, 39);
            lblListadoDePersonal.Name = "lblListadoDePersonal";
            lblListadoDePersonal.Size = new Size(166, 18);
            lblListadoDePersonal.TabIndex = 5;
            lblListadoDePersonal.Text = "Listado de Personal";
            // 
            // chckboxRango1
            // 
            chckboxRango1.AutoSize = true;
            chckboxRango1.Font = new Font("Microsoft Sans Serif", 8.25F);
            chckboxRango1.Location = new Point(74, 91);
            chckboxRango1.Name = "chckboxRango1";
            chckboxRango1.Size = new Size(124, 17);
            chckboxRango1.TabIndex = 6;
            chckboxRango1.Text = "ENERO - FEBRERO";
            chckboxRango1.UseVisualStyleBackColor = true;
            // 
            // chckboxRango2
            // 
            chckboxRango2.AutoSize = true;
            chckboxRango2.Font = new Font("Microsoft Sans Serif", 8.25F);
            chckboxRango2.Location = new Point(74, 114);
            chckboxRango2.Name = "chckboxRango2";
            chckboxRango2.Size = new Size(105, 17);
            chckboxRango2.TabIndex = 7;
            chckboxRango2.Text = "MARZO - ABRIL";
            chckboxRango2.UseVisualStyleBackColor = true;
            // 
            // chckboxRango3
            // 
            chckboxRango3.AutoSize = true;
            chckboxRango3.Font = new Font("Microsoft Sans Serif", 8.25F);
            chckboxRango3.Location = new Point(74, 137);
            chckboxRango3.Name = "chckboxRango3";
            chckboxRango3.Size = new Size(98, 17);
            chckboxRango3.TabIndex = 8;
            chckboxRango3.Text = "MAYO - JUNIO";
            chckboxRango3.UseVisualStyleBackColor = true;
            // 
            // chckboxRango4
            // 
            chckboxRango4.AutoSize = true;
            chckboxRango4.Font = new Font("Microsoft Sans Serif", 8.25F);
            chckboxRango4.Location = new Point(74, 160);
            chckboxRango4.Name = "chckboxRango4";
            chckboxRango4.Size = new Size(110, 17);
            chckboxRango4.TabIndex = 9;
            chckboxRango4.Text = "JULIO - AGOSTO";
            chckboxRango4.UseVisualStyleBackColor = true;
            // 
            // chckboxRango5
            // 
            chckboxRango5.AutoSize = true;
            chckboxRango5.Font = new Font("Microsoft Sans Serif", 8.25F);
            chckboxRango5.Location = new Point(74, 183);
            chckboxRango5.Name = "chckboxRango5";
            chckboxRango5.Size = new Size(156, 17);
            chckboxRango5.TabIndex = 10;
            chckboxRango5.Text = "SEPTIEMBRE - OCTUBRE";
            chckboxRango5.UseVisualStyleBackColor = true;
            // 
            // chckboxRango6
            // 
            chckboxRango6.AutoSize = true;
            chckboxRango6.Font = new Font("Microsoft Sans Serif", 8.25F);
            chckboxRango6.Location = new Point(74, 206);
            chckboxRango6.Name = "chckboxRango6";
            chckboxRango6.Size = new Size(158, 17);
            chckboxRango6.TabIndex = 11;
            chckboxRango6.Text = "NOVIEMBRE - DICIEMBRE";
            chckboxRango6.UseVisualStyleBackColor = true;
            // 
            // chckboxRangoPersonalizado
            // 
            chckboxRangoPersonalizado.AutoSize = true;
            chckboxRangoPersonalizado.Font = new Font("Microsoft Sans Serif", 8.25F);
            chckboxRangoPersonalizado.Location = new Point(27, 239);
            chckboxRangoPersonalizado.Name = "chckboxRangoPersonalizado";
            chckboxRangoPersonalizado.Size = new Size(92, 17);
            chckboxRangoPersonalizado.TabIndex = 12;
            chckboxRangoPersonalizado.Text = "Personalizado";
            chckboxRangoPersonalizado.UseVisualStyleBackColor = true;
            // 
            // chckboxExcluirEspeciales
            // 
            chckboxExcluirEspeciales.AutoSize = true;
            chckboxExcluirEspeciales.Location = new Point(27, 352);
            chckboxExcluirEspeciales.Name = "chckboxExcluirEspeciales";
            chckboxExcluirEspeciales.Size = new Size(167, 19);
            chckboxExcluirEspeciales.TabIndex = 13;
            chckboxExcluirEspeciales.Text = "Excluir Nóminas Especiales";
            chckboxExcluirEspeciales.UseVisualStyleBackColor = true;
            chckboxExcluirEspeciales.CheckedChanged += chckboxExcluirEspeciales_CheckedChanged;
            // 
            // chckboxTotalAcumulado
            // 
            chckboxTotalAcumulado.AutoSize = true;
            chckboxTotalAcumulado.Font = new Font("Microsoft Sans Serif", 8.25F);
            chckboxTotalAcumulado.Location = new Point(40, 393);
            chckboxTotalAcumulado.Name = "chckboxTotalAcumulado";
            chckboxTotalAcumulado.Size = new Size(229, 17);
            chckboxTotalAcumulado.TabIndex = 14;
            chckboxTotalAcumulado.Text = "Acumulado Total (ARCHIVOS ACTUALES)";
            chckboxTotalAcumulado.UseVisualStyleBackColor = true;
            chckboxTotalAcumulado.CheckedChanged += chckboxTotalAcumulado_CheckedChanged;
            // 
            // chckboxSoloEmpleadosActivos
            // 
            chckboxSoloEmpleadosActivos.AutoSize = true;
            chckboxSoloEmpleadosActivos.Font = new Font("Microsoft Sans Serif", 8.25F);
            chckboxSoloEmpleadosActivos.Location = new Point(58, 433);
            chckboxSoloEmpleadosActivos.Name = "chckboxSoloEmpleadosActivos";
            chckboxSoloEmpleadosActivos.Size = new Size(140, 17);
            chckboxSoloEmpleadosActivos.TabIndex = 15;
            chckboxSoloEmpleadosActivos.Text = "Solo Empleados Activos";
            chckboxSoloEmpleadosActivos.UseVisualStyleBackColor = true;
            chckboxSoloEmpleadosActivos.CheckedChanged += chckboxSoloEmpleadosActivos_CheckedChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft Sans Serif", 9.75F);
            label2.Location = new Point(24, 272);
            label2.Name = "label2";
            label2.Size = new Size(89, 16);
            label2.TabIndex = 18;
            label2.Text = "Mes de Inicio:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft Sans Serif", 9.75F);
            label3.Location = new Point(37, 309);
            label3.Name = "label3";
            label3.Size = new Size(76, 16);
            label3.TabIndex = 19;
            label3.Text = "Mes de Fin:";
            // 
            // cbMesInicio
            // 
            cbMesInicio.FormattingEnabled = true;
            cbMesInicio.Items.AddRange(new object[] { "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO", "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE" });
            cbMesInicio.Location = new Point(119, 271);
            cbMesInicio.Name = "cbMesInicio";
            cbMesInicio.Size = new Size(129, 23);
            cbMesInicio.TabIndex = 20;
            // 
            // cbMesFinal
            // 
            cbMesFinal.FormattingEnabled = true;
            cbMesFinal.Items.AddRange(new object[] { "ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO", "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE" });
            cbMesFinal.Location = new Point(119, 308);
            cbMesFinal.Name = "cbMesFinal";
            cbMesFinal.Size = new Size(129, 23);
            cbMesFinal.TabIndex = 21;
            // 
            // dgvPersonalEncontrado
            // 
            dgvPersonalEncontrado.AllowUserToDeleteRows = false;
            dgvPersonalEncontrado.AllowUserToResizeColumns = false;
            dgvPersonalEncontrado.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgvPersonalEncontrado.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvPersonalEncontrado.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPersonalEncontrado.Columns.AddRange(new DataGridViewColumn[] { Id, NombreCompleto, RFCEmpleadoE, IMSSE, CURPE, FechaAltaE, FechaBajaE, SalarioDiarioE, ViaticosE, OtrasE, IntegradoE, BanamexE });
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("Microsoft Tai Le", 9.75F);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dgvPersonalEncontrado.DefaultCellStyle = dataGridViewCellStyle2;
            dgvPersonalEncontrado.Location = new Point(299, 60);
            dgvPersonalEncontrado.Name = "dgvPersonalEncontrado";
            dgvPersonalEncontrado.RowHeadersVisible = false;
            dgvPersonalEncontrado.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPersonalEncontrado.Size = new Size(1593, 380);
            dgvPersonalEncontrado.TabIndex = 22;
            dgvPersonalEncontrado.SelectionChanged += dgvPersonalEncontrado_SelectionChanged;
            // 
            // Id
            // 
            Id.HeaderText = "ID";
            Id.Name = "Id";
            Id.Width = 45;
            // 
            // NombreCompleto
            // 
            NombreCompleto.HeaderText = "Nombre del Empleado";
            NombreCompleto.Name = "NombreCompleto";
            NombreCompleto.Width = 300;
            // 
            // RFCEmpleadoE
            // 
            RFCEmpleadoE.HeaderText = "RFC";
            RFCEmpleadoE.Name = "RFCEmpleadoE";
            RFCEmpleadoE.Width = 150;
            // 
            // IMSSE
            // 
            IMSSE.HeaderText = "IMSS";
            IMSSE.Name = "IMSSE";
            IMSSE.Width = 110;
            // 
            // CURPE
            // 
            CURPE.HeaderText = "CURP";
            CURPE.Name = "CURPE";
            CURPE.Width = 170;
            // 
            // FechaAltaE
            // 
            FechaAltaE.HeaderText = "Fecha Alta";
            FechaAltaE.Name = "FechaAltaE";
            // 
            // FechaBajaE
            // 
            FechaBajaE.HeaderText = "Fecha Baja";
            FechaBajaE.Name = "FechaBajaE";
            // 
            // SalarioDiarioE
            // 
            SalarioDiarioE.HeaderText = "Salario Diario";
            SalarioDiarioE.Name = "SalarioDiarioE";
            SalarioDiarioE.Width = 110;
            // 
            // ViaticosE
            // 
            ViaticosE.HeaderText = "O.F.";
            ViaticosE.Name = "ViaticosE";
            ViaticosE.Width = 105;
            // 
            // OtrasE
            // 
            OtrasE.HeaderText = "Otras";
            OtrasE.Name = "OtrasE";
            OtrasE.Width = 120;
            // 
            // IntegradoE
            // 
            IntegradoE.HeaderText = "Integrado";
            IntegradoE.Name = "IntegradoE";
            IntegradoE.Width = 120;
            // 
            // BanamexE
            // 
            BanamexE.HeaderText = "Banamex";
            BanamexE.Name = "BanamexE";
            BanamexE.Width = 140;
            // 
            // lblNominasEncontradas
            // 
            lblNominasEncontradas.AutoSize = true;
            lblNominasEncontradas.Font = new Font("Arial Rounded MT Bold", 12F);
            lblNominasEncontradas.Location = new Point(863, 456);
            lblNominasEncontradas.Name = "lblNominasEncontradas";
            lblNominasEncontradas.Size = new Size(184, 18);
            lblNominasEncontradas.TabIndex = 23;
            lblNominasEncontradas.Text = "Nóminas Encontradas";
            // 
            // dgvAcumulado
            // 
            dgvAcumulado.AllowUserToDeleteRows = false;
            dgvAcumulado.AllowUserToResizeColumns = false;
            dgvAcumulado.AllowUserToResizeRows = false;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = SystemColors.Control;
            dataGridViewCellStyle3.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold);
            dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            dgvAcumulado.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvAcumulado.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAcumulado.Columns.AddRange(new DataGridViewColumn[] { Archivo, DiasT, Sueldo, PremPunt, OF, Pvacacional, Otras, Aguinaldo, PTU, PercExenta, TotalIngr, ISTP, SubPEmpl, CrApl, ImptoRet, CrPag, SubsidioNoApl, IMSS, Prestamos, PensionAlimenticia, Fonacot, Infonavit });
            dgvAcumulado.Location = new Point(15, 477);
            dgvAcumulado.Name = "dgvAcumulado";
            dgvAcumulado.ReadOnly = true;
            dgvAcumulado.RowHeadersVisible = false;
            dgvAcumulado.Size = new Size(1877, 527);
            dgvAcumulado.TabIndex = 24;
            // 
            // Archivo
            // 
            Archivo.HeaderText = "Archivo";
            Archivo.Name = "Archivo";
            Archivo.ReadOnly = true;
            Archivo.Width = 110;
            // 
            // DiasT
            // 
            DiasT.HeaderText = "Días Trabajados";
            DiasT.Name = "DiasT";
            DiasT.ReadOnly = true;
            DiasT.Width = 90;
            // 
            // Sueldo
            // 
            Sueldo.HeaderText = "Sueldo Pagado";
            Sueldo.Name = "Sueldo";
            Sueldo.ReadOnly = true;
            Sueldo.Width = 90;
            // 
            // PremPunt
            // 
            PremPunt.HeaderText = "Premio de Puntualidad";
            PremPunt.Name = "PremPunt";
            PremPunt.ReadOnly = true;
            PremPunt.Width = 90;
            // 
            // OF
            // 
            OF.HeaderText = "O.F.";
            OF.Name = "OF";
            OF.ReadOnly = true;
            OF.Width = 90;
            // 
            // Pvacacional
            // 
            Pvacacional.HeaderText = "Prima Vacacional";
            Pvacacional.Name = "Pvacacional";
            Pvacacional.ReadOnly = true;
            Pvacacional.Width = 90;
            // 
            // Otras
            // 
            Otras.HeaderText = "Otras";
            Otras.Name = "Otras";
            Otras.ReadOnly = true;
            Otras.Width = 90;
            // 
            // Aguinaldo
            // 
            Aguinaldo.HeaderText = "Aguinaldo";
            Aguinaldo.Name = "Aguinaldo";
            Aguinaldo.ReadOnly = true;
            Aguinaldo.Width = 90;
            // 
            // PTU
            // 
            PTU.HeaderText = "P.T.U";
            PTU.Name = "PTU";
            PTU.ReadOnly = true;
            PTU.Width = 90;
            // 
            // PercExenta
            // 
            PercExenta.HeaderText = "Exentos";
            PercExenta.Name = "PercExenta";
            PercExenta.ReadOnly = true;
            PercExenta.Width = 90;
            // 
            // TotalIngr
            // 
            TotalIngr.HeaderText = "Total de Ingresos";
            TotalIngr.Name = "TotalIngr";
            TotalIngr.ReadOnly = true;
            TotalIngr.Width = 90;
            // 
            // ISTP
            // 
            ISTP.HeaderText = "ISTP";
            ISTP.Name = "ISTP";
            ISTP.ReadOnly = true;
            ISTP.Width = 90;
            // 
            // SubPEmpl
            // 
            SubPEmpl.HeaderText = "Subsidio Empleo";
            SubPEmpl.Name = "SubPEmpl";
            SubPEmpl.ReadOnly = true;
            SubPEmpl.Width = 90;
            // 
            // CrApl
            // 
            CrApl.HeaderText = "Crédito Aplicado";
            CrApl.Name = "CrApl";
            CrApl.ReadOnly = true;
            // 
            // ImptoRet
            // 
            ImptoRet.HeaderText = "Impuesto Retenido";
            ImptoRet.Name = "ImptoRet";
            ImptoRet.ReadOnly = true;
            // 
            // CrPag
            // 
            CrPag.HeaderText = "Cr. Pag.";
            CrPag.Name = "CrPag";
            CrPag.ReadOnly = true;
            // 
            // SubsidioNoApl
            // 
            SubsidioNoApl.HeaderText = "Subsidio No Aplicado";
            SubsidioNoApl.Name = "SubsidioNoApl";
            SubsidioNoApl.ReadOnly = true;
            // 
            // IMSS
            // 
            IMSS.HeaderText = "IMSS";
            IMSS.Name = "IMSS";
            IMSS.ReadOnly = true;
            IMSS.Width = 90;
            // 
            // Prestamos
            // 
            Prestamos.HeaderText = "Préstamos";
            Prestamos.Name = "Prestamos";
            Prestamos.ReadOnly = true;
            Prestamos.Width = 90;
            // 
            // PensionAlimenticia
            // 
            PensionAlimenticia.HeaderText = "Pensión Alimenticia";
            PensionAlimenticia.Name = "PensionAlimenticia";
            PensionAlimenticia.ReadOnly = true;
            PensionAlimenticia.Width = 90;
            // 
            // Fonacot
            // 
            Fonacot.HeaderText = "FONACOT";
            Fonacot.Name = "Fonacot";
            Fonacot.ReadOnly = true;
            Fonacot.Width = 90;
            // 
            // Infonavit
            // 
            Infonavit.HeaderText = "INFONAVIT";
            Infonavit.Name = "Infonavit";
            Infonavit.ReadOnly = true;
            Infonavit.Width = 90;
            // 
            // FormAcumulados
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1904, 1016);
            Controls.Add(dgvAcumulado);
            Controls.Add(lblNominasEncontradas);
            Controls.Add(dgvPersonalEncontrado);
            Controls.Add(cbMesFinal);
            Controls.Add(cbMesInicio);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(chckboxSoloEmpleadosActivos);
            Controls.Add(chckboxTotalAcumulado);
            Controls.Add(chckboxExcluirEspeciales);
            Controls.Add(chckboxRangoPersonalizado);
            Controls.Add(chckboxRango6);
            Controls.Add(chckboxRango5);
            Controls.Add(chckboxRango4);
            Controls.Add(chckboxRango3);
            Controls.Add(chckboxRango2);
            Controls.Add(chckboxRango1);
            Controls.Add(lblListadoDePersonal);
            Controls.Add(btnBuscar);
            Controls.Add(txtBuscar);
            Controls.Add(lblBuscar);
            Controls.Add(label1);
            Controls.Add(toolStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "FormAcumulados";
            Text = "FormAcumulados";
            Load += FormAcumulados_Load;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPersonalEncontrado).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvAcumulado).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip1;
        private ToolStripDropDownButton tsArchivo;
        private ToolStripMenuItem tsOrdenar;
        private ToolStripMenuItem tsNumAscend;
        private ToolStripMenuItem tsNumDescend;
        private ToolStripMenuItem tsAlfAscend;
        private ToolStripMenuItem tsAlfDescend;
        private ToolStripMenuItem tsImpresion;
        private ToolStripDropDownButton tsOpciones;
        private ToolStripMenuItem tsSeleccionarTodo;
        private ToolStripMenuItem tsSelecTodoListadoDePersonal;
        private ToolStripMenuItem tsSelectTodoNominasEncontradas;
        private ToolStripMenuItem opcionesDeAcumulaciónToolStripMenuItem;
        private ToolStripMenuItem tsSinUltimaNomina;
        private ToolStripMenuItem tsConUltimaNomina;
        private ToolStripMenuItem tsOtrosIngresos;
        private ToolStripMenuItem tsBuscarDuplicados;
        private ToolStripMenuItem tsGenerarArchivo;
        private ToolStripDropDownButton tsCalculos;
        private ToolStripMenuItem tsCalculoIntegral;
        private ToolStripMenuItem tsCalculoISR;
        private Label label1;
        private Label lblBuscar;
        private TextBox txtBuscar;
        private Button btnBuscar;
        private Label lblListadoDePersonal;
        private CheckBox chckboxRango1;
        private CheckBox chckboxRango2;
        private CheckBox chckboxRango3;
        private CheckBox chckboxRango4;
        private CheckBox chckboxRango5;
        private CheckBox chckboxRango6;
        private CheckBox chckboxRangoPersonalizado;
        private CheckBox chckboxExcluirEspeciales;
        private CheckBox chckboxTotalAcumulado;
        private CheckBox chckboxSoloEmpleadosActivos;
        private Label label2;
        private Label label3;
        private ComboBox cbMesInicio;
        private ComboBox cbMesFinal;
        private DataGridView dgvPersonalEncontrado;
        private DataGridViewTextBoxColumn Id;
        private DataGridViewTextBoxColumn NombreCompleto;
        private DataGridViewTextBoxColumn RFCEmpleadoE;
        private DataGridViewTextBoxColumn IMSSE;
        private DataGridViewTextBoxColumn CURPE;
        private DataGridViewTextBoxColumn FechaAltaE;
        private DataGridViewTextBoxColumn FechaBajaE;
        private DataGridViewTextBoxColumn SalarioDiarioE;
        private DataGridViewTextBoxColumn ViaticosE;
        private DataGridViewTextBoxColumn OtrasE;
        private DataGridViewTextBoxColumn IntegradoE;
        private DataGridViewTextBoxColumn BanamexE;
        private Label lblNominasEncontradas;
        private DataGridView dgvAcumulado;
        private DataGridViewTextBoxColumn Archivo;
        private DataGridViewTextBoxColumn DiasT;
        private DataGridViewTextBoxColumn Sueldo;
        private DataGridViewTextBoxColumn PremPunt;
        private DataGridViewTextBoxColumn OF;
        private DataGridViewTextBoxColumn Pvacacional;
        private DataGridViewTextBoxColumn Otras;
        private DataGridViewTextBoxColumn Aguinaldo;
        private DataGridViewTextBoxColumn PTU;
        private DataGridViewTextBoxColumn PercExenta;
        private DataGridViewTextBoxColumn TotalIngr;
        private DataGridViewTextBoxColumn ISTP;
        private DataGridViewTextBoxColumn SubPEmpl;
        private DataGridViewTextBoxColumn CrApl;
        private DataGridViewTextBoxColumn ImptoRet;
        private DataGridViewTextBoxColumn CrPag;
        private DataGridViewTextBoxColumn SubsidioNoApl;
        private DataGridViewTextBoxColumn IMSS;
        private DataGridViewTextBoxColumn Prestamos;
        private DataGridViewTextBoxColumn PensionAlimenticia;
        private DataGridViewTextBoxColumn Fonacot;
        private DataGridViewTextBoxColumn Infonavit;
    }
}
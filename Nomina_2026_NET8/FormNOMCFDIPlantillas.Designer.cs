namespace NOMINA_2025
{
    partial class FormNOMCFDIPlantillas
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormNOMCFDIPlantillas));
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            toolStrip1 = new ToolStrip();
            tsMostrarPlantilla = new ToolStripDropDownButton();
            tsPrimeraPlantilla = new ToolStripMenuItem();
            tsSeleccionarYCopiarTodoPrimera = new ToolStripMenuItem();
            tsSegundaPlantilla = new ToolStripMenuItem();
            tsSeleccionarYCopiarTodoSegunda = new ToolStripMenuItem();
            tsEliminar = new ToolStripDropDownButton();
            tsGenerarExcel = new ToolStripDropDownButton();
            tsSDI = new ToolStripDropDownButton();
            dgvPlantillas = new DataGridView();
            panelDGV = new Panel();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPlantillas).BeginInit();
            panelDGV.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsMostrarPlantilla, tsEliminar, tsGenerarExcel, tsSDI });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(2221, 25);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsMostrarPlantilla
            // 
            tsMostrarPlantilla.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsMostrarPlantilla.DropDownItems.AddRange(new ToolStripItem[] { tsPrimeraPlantilla, tsSegundaPlantilla });
            tsMostrarPlantilla.Image = (Image)resources.GetObject("tsMostrarPlantilla.Image");
            tsMostrarPlantilla.ImageTransparentColor = Color.Magenta;
            tsMostrarPlantilla.Name = "tsMostrarPlantilla";
            tsMostrarPlantilla.Size = new Size(61, 22);
            tsMostrarPlantilla.Text = "Mostrar";
            // 
            // tsPrimeraPlantilla
            // 
            tsPrimeraPlantilla.DropDownItems.AddRange(new ToolStripItem[] { tsSeleccionarYCopiarTodoPrimera });
            tsPrimeraPlantilla.Name = "tsPrimeraPlantilla";
            tsPrimeraPlantilla.Size = new Size(165, 22);
            tsPrimeraPlantilla.Text = "Primera Plantilla";
            tsPrimeraPlantilla.Click += tsPrimeraPlantilla_Click;
            // 
            // tsSeleccionarYCopiarTodoPrimera
            // 
            tsSeleccionarYCopiarTodoPrimera.Name = "tsSeleccionarYCopiarTodoPrimera";
            tsSeleccionarYCopiarTodoPrimera.Size = new Size(270, 22);
            tsSeleccionarYCopiarTodoPrimera.Text = "Seleccionar y Copiar Primera Plantilla";
            tsSeleccionarYCopiarTodoPrimera.Click += tsSeleccionarYCopiarTodoPrimera_Click;
            // 
            // tsSegundaPlantilla
            // 
            tsSegundaPlantilla.DropDownItems.AddRange(new ToolStripItem[] { tsSeleccionarYCopiarTodoSegunda });
            tsSegundaPlantilla.Name = "tsSegundaPlantilla";
            tsSegundaPlantilla.Size = new Size(165, 22);
            tsSegundaPlantilla.Text = "Segunda Plantilla";
            tsSegundaPlantilla.Click += tsSegundaPlantilla_Click;
            // 
            // tsSeleccionarYCopiarTodoSegunda
            // 
            tsSeleccionarYCopiarTodoSegunda.Name = "tsSeleccionarYCopiarTodoSegunda";
            tsSeleccionarYCopiarTodoSegunda.Size = new Size(275, 22);
            tsSeleccionarYCopiarTodoSegunda.Text = "Seleccionar y Copiar Segunda Plantilla";
            tsSeleccionarYCopiarTodoSegunda.Click += tsSeleccionarYCopiarTodoSegunda_Click;
            // 
            // tsEliminar
            // 
            tsEliminar.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsEliminar.Image = (Image)resources.GetObject("tsEliminar.Image");
            tsEliminar.ImageTransparentColor = Color.Magenta;
            tsEliminar.Name = "tsEliminar";
            tsEliminar.Size = new Size(63, 22);
            tsEliminar.Text = "Eliminar";
            // 
            // tsGenerarExcel
            // 
            tsGenerarExcel.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsGenerarExcel.Image = (Image)resources.GetObject("tsGenerarExcel.Image");
            tsGenerarExcel.ImageTransparentColor = Color.Magenta;
            tsGenerarExcel.Name = "tsGenerarExcel";
            tsGenerarExcel.Size = new Size(90, 22);
            tsGenerarExcel.Text = "Generar Excel";
            // 
            // tsSDI
            // 
            tsSDI.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsSDI.Image = (Image)resources.GetObject("tsSDI.Image");
            tsSDI.ImageTransparentColor = Color.Magenta;
            tsSDI.Name = "tsSDI";
            tsSDI.Size = new Size(37, 22);
            tsSDI.Text = "SDI";
            // 
            // dgvPlantillas
            // 
            dgvPlantillas.AllowUserToAddRows = false;
            dgvPlantillas.AllowUserToDeleteRows = false;
            dgvPlantillas.AllowUserToResizeColumns = false;
            dgvPlantillas.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = SystemColors.Control;
            dataGridViewCellStyle1.Font = new Font("Century", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgvPlantillas.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvPlantillas.ColumnHeadersHeight = 30;
            dgvPlantillas.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvPlantillas.Dock = DockStyle.Fill;
            dgvPlantillas.EnableHeadersVisualStyles = false;
            dgvPlantillas.Location = new Point(0, 0);
            dgvPlantillas.Margin = new Padding(4, 3, 4, 3);
            dgvPlantillas.Name = "dgvPlantillas";
            dgvPlantillas.ReadOnly = true;
            dgvPlantillas.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvPlantillas.RowHeadersVisible = false;
            dgvPlantillas.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dgvPlantillas.Size = new Size(2221, 1172);
            dgvPlantillas.TabIndex = 1;
            // 
            // panelDGV
            // 
            panelDGV.Controls.Add(dgvPlantillas);
            panelDGV.Dock = DockStyle.Fill;
            panelDGV.Location = new Point(0, 0);
            panelDGV.Name = "panelDGV";
            panelDGV.Size = new Size(2221, 1172);
            panelDGV.TabIndex = 2;
            // 
            // FormNOMCFDIPlantillas
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2221, 1172);
            Controls.Add(toolStrip1);
            Controls.Add(panelDGV);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            Name = "FormNOMCFDIPlantillas";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Generación de CFDI";
            WindowState = FormWindowState.Maximized;
            Load += FormNOMCFDIPlantillas_Load;
            Shown += FormNOMCFDIPlantillas_Shown;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPlantillas).EndInit();
            panelDGV.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton tsMostrarPlantilla;
        private System.Windows.Forms.ToolStripMenuItem tsPrimeraPlantilla;
        private System.Windows.Forms.ToolStripDropDownButton tsEliminar;
        private System.Windows.Forms.ToolStripDropDownButton tsGenerarExcel;
        private System.Windows.Forms.ToolStripDropDownButton tsSDI;
        private System.Windows.Forms.DataGridView dgvPlantillas;
        private System.Windows.Forms.ToolStripMenuItem tsSeleccionarYCopiarTodoPrimera;
        private System.Windows.Forms.ToolStripMenuItem tsSegundaPlantilla;
        private System.Windows.Forms.ToolStripMenuItem tsSeleccionarYCopiarTodoSegunda;
        private Panel panelDGV;
    }
}
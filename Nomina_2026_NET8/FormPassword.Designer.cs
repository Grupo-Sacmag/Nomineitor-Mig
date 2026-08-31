namespace Nomina_2026_NET8
{
    partial class FormPassword
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
            txtPwd = new TextBox();
            btnOK = new Button();
            btnCancelar = new Button();
            SuspendLayout();
            // 
            // txtPwd
            // 
            txtPwd.Location = new Point(15, 15);
            txtPwd.Margin = new Padding(15, 15, 3, 3);
            txtPwd.Name = "txtPwd";
            txtPwd.Size = new Size(255, 23);
            txtPwd.TabIndex = 0;
            txtPwd.UseSystemPasswordChar = true;
            // 
            // btnOK
            // 
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new Point(60, 50);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(80, 23);
            btnOK.TabIndex = 1;
            btnOK.Text = "Aceptar";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancelar
            // 
            btnCancelar.DialogResult = DialogResult.Cancel;
            btnCancelar.Location = new Point(150, 50);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(80, 23);
            btnCancelar.TabIndex = 2;
            btnCancelar.Text = "Cancelar";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // FormPassword
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancelar;
            ClientSize = new Size(284, 91);
            Controls.Add(btnCancelar);
            Controls.Add(btnOK);
            Controls.Add(txtPwd);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormPassword";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Contraseña Requerida";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtPwd;
        private Button btnOK;
        private Button btnCancelar;
    }
}
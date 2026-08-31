using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nomina_2026_NET8
{
    // Diálogo simple de contraseña para proteger edición de tablas fiscales.
    public partial class FormPassword : Form
    {
        public string Password
        {
            get { return txtPwd.Text; }
        }

        public FormPassword()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {

        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {

        }
    }
}

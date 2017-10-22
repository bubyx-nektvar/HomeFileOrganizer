using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer
{
    public partial class UserDialog : Form
    {
        /// <summary>
        /// User login name, used tu discover network
        /// </summary>
        public string UserName="";
        /// <summary>
        /// Name of this pc to add to network
        /// </summary>
        public string PCName = "";
        public UserDialog()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PCName = this.textBox2.Text;
            string t = this.textBox1.Text;
            if (t.Length > 8)
            {
                int i = 0;
                foreach (char c in t) if (Char.IsWhiteSpace(c)) i++;
                if (i < t.Length / 4)
                {
                    UserName = t;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else MessageBox.Show("Too many whitespaces.");
            }
            else MessageBox.Show("Shorter than 8 charakters.");
        }
    }
}

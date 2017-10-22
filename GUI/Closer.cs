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
    public partial class Closer : Form
    {
        public Closer()
        {
            InitializeComponent();
        }

        private void Closer_FormClosing(object sender, FormClosingEventArgs e)
        {
            label2.Text = "All done.";
        }
    }
}

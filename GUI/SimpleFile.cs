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
    public partial class SimpleFile : Form
    {
        public string[] FileNames=new string[0];
        public string Category;
        public SimpleFile()
        {
            InitializeComponent();

            this.listBox1.Items.Clear();
            this.listBox1.Items.AddRange(Classes.Category.GetCategories());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.CheckFileExists = true;
            d.Multiselect = true;
            d.Title = "Add serial file";
            var res = d.ShowDialog();
            if (res == DialogResult.OK)
            {
                this.FileNames = d.FileNames;
                this.label1.Text = String.Concat(FileNames);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (FileNames.Length > 0)
            {
                if (this.listBox1.SelectedItem != null)
                {
                    Category = (string)listBox1.SelectedItem;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else MessageBox.Show("Please select category.");
            }
            else MessageBox.Show("No file selected.");
        }
    }
}

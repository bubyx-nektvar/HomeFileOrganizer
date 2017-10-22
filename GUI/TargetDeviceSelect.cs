using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HomeFileOrganizer.Managers;
using HomeFileOrganizer.Classes;
using System.IO;

namespace HomeFileOrganizer
{
    public partial class TargetDeviceSelect : Form
    {
        private HomeData data;
        public MyDevice device
        {
            get; private set;
        }
        public TargetDeviceSelect(Classes.HomeData dat)
        {
            DialogResult = DialogResult.Cancel;
            data = dat;
            InitializeComponent();
            ulong deviceId=Properties.Settings.Default.localDevId;
            foreach(var dev in dat.devices)
            {
                if (dev.Id != deviceId)
                {
                    listBox1.Items.Add(dev);
                }
            }   
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null){
                DialogResult = DialogResult.OK;
                device = (MyDevice)listBox1.SelectedItem;
                Close();
            }else{
                MessageBox.Show("Somethinks are missing.");
            }
        }
    }
}

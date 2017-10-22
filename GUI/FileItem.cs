using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HomeFileOrganizer.Classes;

namespace HomeFileOrganizer.GUI
{
    public class FileItem:ListViewItem
    {
        public MyFile file;
        public FileItem(MyFile f):base(f.GetColumns())
        {
            file = f;
        }
        public string GetMySystemTreePath()
        {
            return file.GetPath();
        }
    }
}

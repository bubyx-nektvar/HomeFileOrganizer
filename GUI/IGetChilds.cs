using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.GUI
{
    public interface IGetChilds
    {
        Task<TreeNode[]> GetChildsFolder();
        Task<FileItem[]> GetChildsFiles();
        Task<Control[]> GetInfo(ContextMenuStrip menu);
        string GetMySystemTreePath();
    }
}

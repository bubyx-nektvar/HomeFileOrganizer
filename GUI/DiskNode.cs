using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.GUI
{
    class DiskNode:TreeNode,IGetChilds
    {
        private Classes.MyDisk myDisk;

        public DiskNode(Classes.MyDisk myDisk)
        {
            this.myDisk = myDisk;
            this.Text = myDisk.Name;
            this.Nodes.Add(new TreeNode());
        }


        public Task<TreeNode[]> GetChildsFolder()
        {
            return Task<TreeNode[]>.Run(() =>
            {
                TreeNode[] tns = new TreeNode[myDisk.folders.Count];
                for (int i = 0; i < myDisk.folders.Count; i++)
                {
                    tns[i] = new FolderNode(myDisk.folders[i]);
                }
                return tns;
            });
        }


        public Task<FileItem[]> GetChildsFiles()
        {
            return Task<FileItem[]>.Run(() =>
            {
                FileItem[] list = new FileItem[myDisk.files.Count];
                for (int i = 0; i < myDisk.files.Count; i++)
                {
                    list[i]=new FileItem(myDisk.files[i]);
                }
                return list;
            }
            );
        }

        public Task<Control[]> GetInfo(ContextMenuStrip m)
        {
            return Task<Control[]>.FromResult(new Control[0]);
        }
        public string GetMySystemTreePath()
        {
            return myDisk.Id.ToString();
        }
    }
}

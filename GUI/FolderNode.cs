using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.GUI
{
    class FolderNode:TreeNode,IGetChilds
    {
        private Classes.MyFolder myFolder;

        public FolderNode(Classes.MyFolder myFolder)
        {
            this.myFolder = myFolder;
            this.Text = myFolder.Path;
            if(myFolder.folders.Count>0)this.Nodes.Add(new TreeNode());
        }

        public Task<TreeNode[]> GetChildsFolder()
        {
            return Task.Run<TreeNode[]>(() =>
            {
                TreeNode[] tns = new TreeNode[myFolder.folders.Count];
                for (int i = 0; i < myFolder.folders.Count; i++)
                {
                    tns[i] = new FolderNode(myFolder.folders[i]);
                }
                return tns;
            });
        }




        public Task<FileItem[]> GetChildsFiles()
        {
            return Task<FileItem[]>.Run(() =>
            {
                FileItem[] list = new FileItem[myFolder.files.Count];
                for (int i = 0; i < myFolder.files.Count; i++)
                {
                    list[i] = new FileItem(myFolder.files[i]);
                }
                return list;
            }
            );
        }

        public Task<Control[]> GetInfo(ContextMenuStrip menu)
        {
            return Task<Control[]>.Run(() =>
            {
                return myFolder.MyInfoFile.GetGUIContent(menu);
            });
        }
        public string GetMySystemTreePath()
        {
            return myFolder.GetPath();
        }
    }
}

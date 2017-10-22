using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HomeFileOrganizer.Classes;
namespace HomeFileOrganizer.GUI
{
    class DeviceNode:TreeNode,IGetChilds
    {
        public MyDevice Device;
        public DeviceNode(MyDevice dev)
        {
            Device = dev;
            this.Nodes.Add(new TreeNode());
            this.Text = dev.Name;
        }

        public async Task<TreeNode[]> GetChildsFolder()
        {
            if(!Device.AreDisksLoaded){
                await Device.LoadDisksAsync();
            }
            TreeNode[] tns = new TreeNode[Device.disks.Count];
            for (int i = 0; i < Device.disks.Count; i++)
            {
                tns[i] = new DiskNode(Device.disks[i]);
            }
            return tns;
        }



        public Task<FileItem[]> GetChildsFiles()
        {
            return Task<FileItem[]>.FromResult(new FileItem[0]);
        }

        public Task<Control[]> GetInfo(ContextMenuStrip menu)
        {
            return Task<Control[]>.FromResult(new Control[0]);
        }

        public string GetMySystemTreePath()
        {
            return null;
        }
    }
}

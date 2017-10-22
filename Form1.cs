using HomeFileOrganizer.Classes;
using HomeFileOrganizer.GUI;
using HomeFileOrganizer.Managers;
using HomeFileOrganizer.Properties;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace HomeFileOrganizer
{
    public partial class Form1 : Form
    {
        private HomeData data = new HomeData();
        private FileManager filesSystem;
        private Connections connectionControler;
        private Task connectionRuns;
        private ConcurrentDictionary<string, ConcurrentBag<Control>> editors = new ConcurrentDictionary<string, ConcurrentBag<Control>>();
        private ConcurrentBag<Task> allRuniingTasks = new ConcurrentBag<Task>();
        public delegate void AddMethod();
        public AddMethod invokeTarget;

        public Form1()
		{
            invokeTarget = new AddMethod(loadTreeView);
			this.InitializeComponent();
            if(Settings.Default.UserToken != "")
            {
                this.filesSystem = new FileManager(this.data);
                this.connectionControler = new Connections(this.filesSystem, this.filesSystem.GetLocalDeviceInstance(), this.filesSystem.syncManager,this);
                this.connectionRuns = this.connectionControler.RunConnection();
                this.allRuniingTasks.Add(connectionRuns);
                loadTreeView();
			}
			else
            {
                this.filesSystem = new FileManager(this.data,true);
                this.fileToolStripMenuItem.Enabled = false;
			}
		}
        public void loadTreeView()
        {
            treeView1.Nodes.Clear();
            foreach (MyDevice current in this.data.devices)
            {
                this.treeView1.Nodes.Add(new DeviceNode(current));
            }
        }
		private async void simpleFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var di = new SimpleFile();
            if (di.ShowDialog() == DialogResult.OK)
            {
                Task[] ts = new Task[di.FileNames.Length];
                for (int i = 0; i < ts.Length; i++)
                {
                    ts[i] = filesSystem.AddFile(di.FileNames[i], Category.GetCategory(di.Category));
                }
                await Task.WhenAll(ts);
            }
		}
		private async void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
            if((e.Node.Nodes.Count > 0)&&(!(e.Node.Nodes[0] is IGetChilds)))
            {
                e.Node.Nodes.Clear();
                e.Node.Nodes.AddRange(await((IGetChilds)e.Node).GetChildsFolder());
                e.Node.Expand();
            }
		}
		
		private async void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
		{
            if (e.Node is IGetChilds)
            {
                IGetChilds node = (IGetChilds)e.Node;
                listView1.Items.Clear();
                Task<FileItem[]> t = node.GetChildsFiles();
                Task<Control[]> t2 =node.GetInfo(contextMenuStrip1);
                listView1.Items.Clear();
                listView1.Items.AddRange(await t);
                flowLayoutPanel1.Controls.Clear();
                flowLayoutPanel1.Tag=node.GetMySystemTreePath();
                flowLayoutPanel1.Controls.AddRange(await t2);
            }
		}
		private async void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
            if (e.Item is FileItem && e.IsSelected)
            {
                FileItem i = (FileItem)e.Item;
                Task<Control[]> t = i.file.MyInfoFile.GetGUIContent(contextMenuStrip1);
                flowLayoutPanel1.Controls.Clear();
                flowLayoutPanel1.Tag=i.GetMySystemTreePath();
                flowLayoutPanel1.Controls.AddRange(await t);
                filesSystem.viewedFile = i.file;
            }
		}
		private void toolStripMenuItem3_Click(object sender, EventArgs e)
		{
			ToolStripMenuItem toolStripMenuItem = (ToolStripMenuItem)sender;
			ContextMenuStrip contextMenuStrip = (ContextMenuStrip)toolStripMenuItem.GetCurrentParent();
			Control sourceControl = contextMenuStrip.SourceControl;
			Item item = sourceControl.Tag as Item;
			if (item!=null)
			{
				TableLayoutPanel tableLayoutPanel = sourceControl.Parent as TableLayoutPanel;
				if( tableLayoutPanel != null)
				{
					int row = tableLayoutPanel.GetRow(sourceControl);
					if( row >= 0)
					{
						Control editableControl = item.GetEditableControl();
						this.editors.GetOrAdd(this.flowLayoutPanel1.Tag as string, new ConcurrentBag<Control>()).Add(editableControl);
						tableLayoutPanel.GetChildAtPoint(new Point(row, 1));
						tableLayoutPanel.Controls.Remove(sourceControl);
						tableLayoutPanel.Controls.Add(editableControl, 1, row);
					}
				}
			}
		}
		private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			base.WindowState = FormWindowState.Normal;
			base.Show();
		}
		private void Form1_Resize(object sender, EventArgs e)
		{
			bool flag = FormWindowState.Minimized == base.WindowState;
			if (flag)
			{
				base.Hide();
			}
		}
		private void toolStripMenuItem4_Click(object sender, EventArgs e)
		{
            Parallel.ForEach<string>(editors.Keys,(k)=>{
                ConcurrentBag<Control> v;
                if(editors.TryRemove(k,out v)){
                    allRuniingTasks.Add(filesSystem.InfoSave(v.ToArray(),k));
                }
            });
		}
		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if(this.connectionControler != null)
			{
				this.connectionControler.SetClose();
			}
			Closer closer = new Closer();
            closer.Show();
            try {
                connectionControler.Dispose().Wait();
            }catch(Exception exc) { }
            try {
                Task.WaitAll(allRuniingTasks.ToArray());
            }
            catch (Exception exc) { }
            try
            {
                filesSystem.Dispose().Wait();
            }
            catch (Exception exc) { }
            closer.Close();
		}
		
		private async void serialFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var di = new OpenFileDialog();
            di.Multiselect = true;
            di.CheckFileExists = true;
            di.CheckPathExists = true;
            di.Title = "Add serials";
            if (di.ShowDialog() == DialogResult.OK)
            {
                Task[] ts = new Task[di.FileNames.Length];
                for (int i = 0; i < ts.Length; i++)
                {
                    ts[i] = filesSystem.AddFile(di.FileNames[i], Category.GetCategory("Serials"));
                }
                await Task.WhenAll(ts);
            }
		}
		
		private async void filmFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
            var di=new OpenFileDialog();
            di.Multiselect=true;
            di.CheckFileExists=true;
            di.CheckPathExists=true;
            di.Title="Add film";
            if(di.ShowDialog()==DialogResult.OK){
                Task[] ts=new Task[di.FileNames.Length];
                for(int i=0;i<ts.Length;i++){
                    ts[i] = filesSystem.AddFile(di.FileNames[i], Category.GetCategory("Films"));
                }
                await Task.WhenAll(ts);
            }
            
		}
		private void moveToolStripMenuItem_Click(object sender, EventArgs e)
		{
            moveOrCopyFile(false);
		}
        private void moveOrCopyFile(bool createCopy)
        {

            if (listView1.SelectedItems.Count > 0)
            {
                var b = new TargetDeviceSelect(data);
                b.ShowDialog();
                if (b.DialogResult == DialogResult.OK)
                {
                    var file = ((FileItem)listView1.SelectedItems[0]).file;
                    var path = file.GetPath(data);
                    filesSystem.syncManager.AddGeneratedSyncEvent(new Managers.SyncEvents.FileMoveEv(path.Device.Id, path.Disk.Id, file.FilePath, b.device.Id,createCopy));
                }
            }
            else
            {
                MessageBox.Show("There is no file selected");
            }
        }
		[PrincipalPermission(SecurityAction.Demand, Role = "Administrators")]
		private void connetToNetworkToolStripMenuItem_Click(object sender, EventArgs e)
		{
            if (MessageBox.Show("This tool will do clear install of application. All app data will be lost. Do you want to continue?", "Override files?", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                UserDialog userDialog = new UserDialog();
                if (userDialog.ShowDialog() == DialogResult.OK)
                {
                    try {
                        Form1_FormClosing(null,null);
                        MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
                        string @string = Encoding.UTF8.GetString(mD5CryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(userDialog.UserName)));
                        Settings.Default.UserToken = @string;
                        FileManager.CreateHFOStructure();
                        this.filesSystem.ClearStart();
                        if (connectionRuns != null)
                        {
                            connectionControler.SetClose();
                            connectionRuns.Wait();
                        }
                        connectionControler = new Connections(filesSystem, new MyDevice(0, userDialog.PCName, Communication.Network, "", "", ""), filesSystem.syncManager,this);
                        connectionControler.ConnectingToNetwork(data).Wait();
                        filesSystem.dataHolder.Reload();
                        filesSystem = new FileManager(filesSystem.dataHolder);
                        filesSystem.AddLocalDiskToManager(data.devices.Find(i => i.Id == Settings.Default.localDevId));
                        //restart app
                        Settings.Default.Save();
                        DialogResult = DialogResult.Retry;
                        Close();
                    }catch(Exception exc)
                    {

                    }
                }
            }

        }
		[PrincipalPermission(SecurityAction.Demand, Role = "Administrators")]
		private void createNewNetworkToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(MessageBox.Show("This tool will do clear install of application. All app data will be lost. Do you want to continue?", "Override files?", MessageBoxButtons.YesNo) == DialogResult.Yes)
			{
				UserDialog userDialog = new UserDialog();
				if(userDialog.ShowDialog() == DialogResult.OK)
				{
                    Form1_FormClosing(null, null);
					MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
					string @string = Encoding.UTF8.GetString(mD5CryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(userDialog.UserName)));
					Settings.Default.UserToken = @string;
                    FileManager.CreateHFOStructure();
                    this.filesSystem.ClearStart();
                    filesSystem = new FileManager(filesSystem.dataHolder);
                    this.filesSystem.AddNewDevice(userDialog.PCName);

                    //restart app
                    Settings.Default.Save();
                    DialogResult = DialogResult.Retry;
                    Close();
				}
			}
		}

        private async void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                Searcher s = new Searcher(data);
                var results = await s.Search(textBox1.Text);
                
                listView1.Items.Clear();
                while (results.Count>0)
                {
                    var x=await Task.WhenAny(results.ToArray());
                    results.Remove(x);
                    if (x.Result != null)
                    {
                        var f = x.Result as MyFile;
                        if (f != null)
                        {
                            var file = new FileItem((f));
                            file.ToolTipText = f.GetPath();
                            listView1.Items.Add(file);
                        }
                    }
                }
            }
        }

        private void setCopiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            moveOrCopyFile(true);
        }
    }
}

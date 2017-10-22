using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeFileOrganizer.Classes;
using HomeFileOrganizer.XMLProcessors;
using System.Windows.Forms;
using HomeFileOrganizer.Managers.SyncEvents;
using System.IO;
using HomeFileOrganizer.Classes.Interfaces;

namespace HomeFileOrganizer.Managers
{
    public class FileManager
    {
        /// <summary>
        /// System path to directory, where all data are saved.
        /// </summary>
        public static string PathToHFOFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
            "\\"+Properties.Settings.Default.HFOFolderName;
        /// <summary>
        /// Instance that contain all program data.
        /// </summary>
        public HomeData dataHolder {get; private set; }
        /// <summary>
        /// File that is currently slected in GUI.
        /// </summary>
        public MyFile viewedFile;

        /// <summary>
        /// Class that manage synchronization.
        /// </summary>
        public Synchronization syncManager
        {
            get; private set;
        }
        public FileManager(HomeData data)
        {
            dataHolder = data;
            syncManager = new Synchronization();
        }
        /// <summary>
        /// Create new instance of FileManger, whre datHolder is set to <paramref name="data"/>.
        /// </summary>
        /// <param name="noSyncManager">Detemine if synchronization manager shoud be created. If true the <see cref="FileManager.syncManager"/> is null.</param>
        public FileManager(HomeData data, bool noSyncManager)
        {
            dataHolder = data;
            if (!noSyncManager) syncManager = new Synchronization();
        }

        /// <summary>
        /// Add from this device to application data.
        /// </summary>
        /// <param name="path">System path od adding file.</param>
        /// <param name="category">Category of file</param>
        /// <returns></returns>
        public Task AddFile(string path,Category category){
            dataHolder.Load();
            return Task.Run(() =>
            {
                FileInfo info = new FileInfo(path);
                MyDevice device = GetLocalDeviceInstance();
                MyDisk disk = GetDiskInstance(info);
                MyFile f=disk.AddFile(path, category,syncManager);
                syncManager.AddGeneratedSyncEvent(new FileAddEv(new FilesEvents(disk.Id,device.Id,f.FilePath),category,f.InfoFilePath));
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldAppTreePath"></param>
        /// <param name="newAppTreePath"></param>
        /// <param name="removeOld"></param>
        /// <returns></returns>
        public Task<MyFile> PathChanged(string oldAppTreePath,string newAppTreePath,bool removeOld)
        {
            dataHolder.Load();
            return Task.Run<MyFile>(() =>
            {
                var sO=oldAppTreePath.Split(new char[] { '\\' },3);
                var oldEnd=dataHolder.Select(sO[0],sO[1]).GetFile(sO[2]);
                var sN = newAppTreePath.Split(new char[] { '\\' }, 3);
                MyDisk d=dataHolder.Select(sN[0], sN[1]);
                MyFile f=d.AddFile(sN[2], oldEnd);

                if(removeOld)dataHolder.Select(sO[0], sO[1]).RemoveFile(sO[2]);
                
                return f;
            });
        }
        /// <summary>
        /// Get <see cref="Classes.MyDevice"/> representing this device from <see cref="FileManager.dataHolder"/>.
        /// </summary>
        internal MyDevice GetLocalDeviceInstance()
        {
            return dataHolder.devices.Find(i => i.Id == Properties.Settings.Default.localDevId);
        }

        /// <summary>
        /// Get <see cref="Classes.MyDisk"/> (from <see cref="FileManager.dataHolder"/>) representing disk where file is located.
        /// </summary>
        public MyDisk GetDiskInstance(FileInfo file)
        {
            return GetDiskInstance(file.Directory.Root);
        }
        /// <summary>
        /// Get application id of disk on this device, where the <paramref name="dir"/> is located.
        /// Should be found in root folder of disk, in file .HFO.info
        /// </summary>
        /// <returns>Id of disk</returns>
        public static ulong GetDiskId(DirectoryInfo dir)
        {
            FileInfo diskFile = dir.Root.GetFiles().First(i => i.Name == String.Format(".{0}.info",Properties.Settings.Default.HFOFolderName));
            try
            {
                StreamReader r = new StreamReader(diskFile.OpenRead());
                try
                {
                    string diskId = null;
                    string s = r.ReadLine();
                    while (s != null&&(diskId==null))
                    {
                        if (s.StartsWith("diskId"))
                        {
                            diskId = s.Split(new char[] { '=' })[1];
                            break;
                        }
                        s = r.ReadLine();
                    }
                    return ulong.Parse(diskId);
                }
                finally { r.Dispose(); }
            }
            catch (IOException e) { throw new Exceptions.DeviceSystemException("Cant acces info file.", e); }
            catch (ArgumentNullException e) { throw new Exceptions.DeviceSystemException("Error with info file", e); }
            catch (ArgumentException e)
            {
                throw new Exceptions.DeviceSystemException("Error with info file", e);
            }
            throw new Exceptions.DeviceSystemException("Something is wrong");
        }
        /// <summary>
        /// As <see cref="FileManager.GetDiskId(DirectoryInfo)"/>, only extended about selecting <see cref="Classes.MyDisk"/> instance form <see cref="FileManager.dataHolder"/> by id.
        /// </summary>
        public MyDisk GetDiskInstance(DirectoryInfo dir){
            try
            {
                var x = GetDiskId(dir);
                MyDisk disk = dataHolder.devices.Find(i => i.Id == Properties.Settings.Default.localDevId).
                    disks.Find(i => i.Id == x);
                return  disk;
            }
            catch (NullReferenceException e)
            {
                throw new Exceptions.DeviceSystemException("Missing information in info file.",e);
            }

        }
        /// <summary>
        /// Take all controls of info data, select these which has changed, add to sync manager syncEvent and rewrite InfoFile
        /// </summary>
        /// <param name="editetInformations">All controls, that can be changed</param>
        /// <param name="file">Path of file in application data tree</param>
        /// <returns>Running task</returns>
        public Task InfoSave(Control[] editetInformations, string filePath)
        {
            SyncEvents.InfoChangeEvents eventBase=new SyncEvents.InfoChangeEvents(DateTime.Now,filePath);
            return Task.Run(() =>
            {
                //zkontroluje zda se neco zmenilo
                bool changed = false;
                Task<Classes.Interfaces.IInfoGetter> ig = GetPathEnd(filePath);
                Parallel.ForEach<Control>(editetInformations,(c)=>{
                    
                    Item it = c.Tag as Item;
                    if (it != null)
                    {
                        if(it.HasChanged(c))
                        {
                            syncManager.AddGeneratedSyncEvent(new SyncEvents.InfoChangeValueEv(it.GetChangeInfoSyncString(),it.GetGroupPath(),eventBase));
                            changed=true;
                        }
                    }
                });
                if (changed)
                {
                    RewriteInfoFile(ig.Result.GetInfoFile(),ig.Result.GetInfoFilePath());
                }
            });
        }
        /// <summary>
        /// Go down through my system tree and get Folder(or File) at the end of path <paramref name="s"/>
        /// </summary>
        /// <returns>Running task thats result is founded Folder(or File).</returns>
        public Task<Classes.Interfaces.IInfoGetter> GetPathEnd(string s)
        {
            return Task<Classes.Interfaces.IInfoGetter>.Run<Classes.Interfaces.IInfoGetter>(()=>{
            Classes.Interfaces.IInfoGetter foundedEnd=null;


            int i = s.IndexOf('\\');
            ulong diskId=ulong.Parse(s.Substring(0,i));
            s = s.Remove(0, i );
            string realtiveDisk=s;
            MyDisk d = null;
            foreach (MyDevice di in dataHolder.devices)
            {
                d = di.disks.Find((x) => { return x.Id == diskId; });
                if (d != null)
                {
                    di.LoadDisks();
                    break;
                }
            }
            foundedEnd =d.files.Find((x)=>{ return x.FilePath==realtiveDisk;});
            if (foundedEnd != null) return foundedEnd;
            else
            {
                Classes.Interfaces.IInfoGetter result = null;
                foreach (MyFolder f in d.folders)
                {
                    if(s.StartsWith(f.Path)) result= f.GetEnd(s.Remove(0,f.Path.Length),realtiveDisk);
                    if(result!=null) return result;
                }
                throw new Exceptions.DeviceSystemException("Wrong path through my system tree.");
            }
            });

        }
        /// <summary>
        /// Rewrite info file<paramref name="infoFilePath"/> by data in <paramref name="file"/>.
        /// </summary>
        /// <param name="infoFilePath">Path relative from <see cref="FileManager.PathToHFOFolder"/></param>
        private void RewriteInfoFile(MyInfoFile file,string infoFilePath)
        {
            StreamWriter w = new StreamWriter(FileManager.PathToHFOFolder + infoFilePath);
            try{ file.RewriteFile(w); }
            finally { w.Dispose(); }
        }
        /// <summary>
        /// Create new file in directory with random name.
        /// Not override any other file in folder.
        /// </summary>
        /// <param name="dir">Directory where it should be created</param>
        /// <param name="extension">Extension of file name</param>
        /// <returns>new file</returns>
        public static FileInfo CreateNewFile(DirectoryInfo dir, string extension)
        {
            while (true)
            {
                string path=dir.FullName+"\\"+Path.GetRandomFileName()+extension;
                if (!File.Exists(path))
                {
                    try
                    {
                        FileStream s = File.Open(path, FileMode.CreateNew);
                        s.Close();
                        return new FileInfo(path);
                    }
                    catch (IOException e)
                    {
                        //Is file with our path is created tfrom other thread between File.Exist and File.Open, than exception will be catched and lookin for available name will continue.
                        if (!File.Exists(path)) throw;
                    }
                }
            }
            throw new NotSupportedException();
        }
        

        /// <summary>
        /// Get system path of disk.
        /// </summary>
        /// <param name="Disk"></param>
        /// <returns>null if not in this device</returns>
        public static string GetDiskPath(MyDisk Disk)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo d in drives)
            {
                try
                {
                    TextReader read = new StreamReader(new FileInfo(String.Format("{0}\\.{1}.info",d.RootDirectory.FullName ,Properties.Settings.Default.HFOFolderName)).OpenRead());
                    try
                    {
                        string line = read.ReadLine();
                        while(line != null)
                        {
                            if (line.StartsWith("diskId"))
                            {
                                if (Disk.Id == ulong.Parse(line.Split(new char[] { '=' })[1])) return d.RootDirectory.FullName;
                            }
                            line = read.ReadLine();
                        }
                    }
                    finally { read.Dispose(); }
                }
                catch (IOException) { }
                catch (ArgumentNullException) { }
                catch (ArgumentException) { }
            }
            return null;
        }

        internal void ClearStart()
        {
            this.dataHolder.devices.Clear();
        }
        /// <summary>
        /// Rewrite overview file and all device files
        /// </summary>
        /// <returns> Task that is waiting for all data to save.</returns>
        internal Task SaveStructure()
        {
            Task[] t = new Task[dataHolder.devices.Count+1];
            for(int i = 0; i < dataHolder.devices.Count; i++) {
                var d = dataHolder.devices[i];
                t[i] = Task.Run(() => { d.RewriteDeviceFile(); });
            }
            t[t.Length - 1] = Task.Run(() => { dataHolder.RewriteOverviewFile(); });
            return Task.WhenAll(t);
        }
        /// <summary>
        /// Add new device that represent this device. And add local drives.
        /// </summary>
        internal void AddNewDevice(string deviceName)
        {

            MyDevice dev=this.dataHolder.AddDevice(deviceName);
            Properties.Settings.Default.localDevId = dev.Id;
            Properties.Settings.Default.Save();
            AddLocalDiskToManager(dev);
        }

        /// <summary>
        /// Add to application local drives, and create .HFO.info in each.
        /// </summary>
        internal void AddLocalDiskToManager(MyDevice dev)
        {

            bool addFailed;
            foreach (var d in DriveInfo.GetDrives())
            {
                addFailed = false;
                MyDisk disk = dev.AddDisk(d.Name);
                try
                {
                    try {
                        var w = new StreamWriter(new FileStream(
                            String.Format("{0}\\.{1}.info",d.RootDirectory.FullName,Properties.Settings.Default.HFOFolderName)
                            , FileMode.CreateNew, System.Security.AccessControl.FileSystemRights.Modify, FileShare.None, 1024, FileOptions.None));
                        try
                        {
                            w.WriteLine("diskId={0}", disk.Id);
                        }
                        finally
                        {
                            w.Dispose();
                        }
                    }catch(IOException ex)
                    {
                        //Disk jiz obsahuje .HFO.info
                        ulong id =GetDiskId(d.RootDirectory);
                        disk.Id = id;
                    }
                }
                catch (UnauthorizedAccessException e)
                {
                    addFailed = true;
                    dev.Remove(disk);
                }
                catch (IOException e)
                {
                    addFailed = true;
                    dev.Remove(disk);
                }
                if (!addFailed)
                {
                    syncManager.AddGeneratedSyncEvent(new DiskAddEv(disk, this));
                }
            }
            dev.RewriteDeviceFile();
            foreach (var d in dev.disks)
            {
                Directory.CreateDirectory(String.Format("{0}\\FileInfo\\{1}\\Folder", FileManager.PathToHFOFolder, d.Id));
                Directory.CreateDirectory(String.Format("{0}\\FileInfo\\{1}\\File", FileManager.PathToHFOFolder, d.Id));
                Directory.CreateDirectory(String.Format("{0}\\FileInfo\\{1}\\ItemFiles", FileManager.PathToHFOFolder, d.Id));
            }
            dataHolder.RewriteOverviewFile();
        }
        /// <summary>
        /// Create directory structure in <see cref="FileManager.PathToHFOFolder"/>.
        /// </summary>
        public static void CreateHFOStructure()
        {
            List<string> notChacked = new List<string>();
            DirectoryInfo directoryInfo = new DirectoryInfo(FileManager.PathToHFOFolder);
            if( directoryInfo.Exists)
            {
                directoryInfo.Delete(true);
            }
            while (directoryInfo.Exists)
            {
                System.Threading.Thread.Sleep(100);
                directoryInfo = new DirectoryInfo(directoryInfo.FullName);
            }
            directoryInfo.Create();
            while (!directoryInfo.Exists)
            {
                System.Threading.Thread.Sleep(100);
                directoryInfo = new DirectoryInfo(directoryInfo.FullName);
            }
            DirectoryInfo directoryInfo2 = directoryInfo.CreateSubdirectory("AppFiles");
            notChacked.Add(directoryInfo2.CreateSubdirectory("Films").FullName);
            notChacked.Add(directoryInfo2.CreateSubdirectory("Folders").FullName);
            notChacked.Add(directoryInfo2.CreateSubdirectory("Serials").FullName);
            notChacked.Add(directoryInfo2.CreateSubdirectory("tmp").FullName);
            notChacked.Add(directoryInfo2.CreateSubdirectory("Uncategorized").FullName);
            notChacked.Add(directoryInfo.CreateSubdirectory("DeviceInfos").FullName);
            notChacked.Add(directoryInfo.CreateSubdirectory("FileInfo").FullName);
            notChacked.Add(directoryInfo.CreateSubdirectory("SyncFiles").FullName);

            DirectoryInfo directoryInfo3 = directoryInfo.CreateSubdirectory("FITemplates");
            while (!directoryInfo3.Exists)
            {
                System.Threading.Thread.Sleep(100);
                directoryInfo3 = new DirectoryInfo(directoryInfo3.FullName);
            }
            FileStream stream = File.Create(directoryInfo3.FullName + "\\EpisodeInfo.xml");
            StreamWriter streamWriter = new StreamWriter(stream);
            streamWriter.Write(Properties.Resources.EpisodeInfo);
            streamWriter.Close();
            stream = File.Create(directoryInfo3.FullName + "\\FilmInfo.xml");
            streamWriter = new StreamWriter(stream);
            streamWriter.Write(Properties.Resources.FilmInfo);
            streamWriter.Close();
            stream = File.Create(directoryInfo3.FullName + "\\MyStatistics.xml");
            streamWriter = new StreamWriter(stream);
            streamWriter.Write(Properties.Resources.MyStatistics);
            streamWriter.Close();
            stream = File.Create(directoryInfo3.FullName + "\\SerialInfo.xml");
            streamWriter = new StreamWriter(stream);
            streamWriter.Write(Properties.Resources.SerialInfo);
            streamWriter.Close();
            stream = File.Create(directoryInfo3.FullName + "\\System.xml");
            streamWriter = new StreamWriter(stream);
            streamWriter.Write(Properties.Resources.System);
            streamWriter.Close();

            FileStream stream2 = File.Create(directoryInfo.FullName + "\\Categories.xml");
            StreamWriter streamWriter2 = new StreamWriter(stream2);
            streamWriter2.Write(Properties.Resources.Categories);
            streamWriter2.Dispose();

            File.Create(directoryInfo.FullName + "\\OverviewFile.xml").Dispose();
            while (notChacked.Count > 0)
            {
                var l = new List<string>();
                foreach(var dir in notChacked)
                {
                    if (!Directory.Exists(dir)) l.Add(dir);
                }
                notChacked = l;
            }
        }
        /// <summary>
        /// Close all resources.
        /// </summary>
        /// <returns>Running task that finish when all work is done.</returns>
        public Task Dispose()
        {
            return Task.Run(() =>
            {
                if (this.syncManager != null)
                {
                    var ver = syncManager.Dispose();
                    var d = GetLocalDeviceInstance();
                    d.SyncedTo = ver.Result;
                    d.RunOn = ver.Result;
                }

                SaveStructure().Wait();
            });
        }
    }
}

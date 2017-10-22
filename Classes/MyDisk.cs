using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HomeFileOrganizer.Managers.SyncEvents;

namespace HomeFileOrganizer.Classes
{
    public class MyDisk
    {
        public UInt64 Id;
        public string Name;
        //public string Path;
        public List<MyFile> files = new List<MyFile>();
        public List<MyRootFolder> folders = new List<MyRootFolder>();

        public MyDisk(ulong disk, string diskName)
        {
            Id= disk;
            Name = diskName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">start with root dir</param>
        /// <param name="category"></param>
        /// <returns>null if already exist</returns>
        internal MyFile AddFile(string path, Category category, Managers.Synchronization syncer)
        {
            FileInfo file = new FileInfo(path);

            string HFOPath = String.Format("\\FileInfo\\{0}\\File", Id);
            string filePath = path.Remove(0, file.Directory.Root.Name.Length - 1);
            var foundfile = files.Find(i => { return i.FilePath == filePath; });
            if (foundfile != null) return null;
            MyFile f = new MyFile(category.GetMyFileInfoForFile(Id));
            f.Category = category;
            f.FilePath = filePath;
            if (files.Count > 0) f.Folder = files[0].Folder;
            else
            {
                f.Folder = new MyRootFolder("", this, null);
            }

            InfoChangeEvents eventBase = new InfoChangeEvents(DateTime.Now, f.GetPath());
            //set system variables
            var it = f.MyInfoFile.GetItem("system\\name");
            if (it != null) {
                it.SetValue(file.Name);
                syncer.AddGeneratedSyncEvent(new InfoChangeValueEv(it.GetChangeInfoSyncString(), it.GetGroupPath(), eventBase));
                }
            it = f.MyInfoFile.GetItem("system\\size");
            if (it != null) {
                it.SetValue(file.Length.ToString());
                syncer.AddGeneratedSyncEvent(new InfoChangeValueEv(it.GetChangeInfoSyncString(), it.GetGroupPath(), eventBase));
            }
            it = f.MyInfoFile.GetItem("system\\last change");
            if (it != null)
            {
                it.SetValue(file.LastWriteTime.ToString(System.Globalization.CultureInfo.InvariantCulture));
                syncer.AddGeneratedSyncEvent(new InfoChangeValueEv(it.GetChangeInfoSyncString(), it.GetGroupPath(), eventBase));
            }
            it = f.MyInfoFile.GetItem("system\\creation");
            if (it != null)
            {
                it.SetValue(file.CreationTime.ToString(System.Globalization.CultureInfo.InvariantCulture));
                syncer.AddGeneratedSyncEvent(new InfoChangeValueEv(it.GetChangeInfoSyncString(), it.GetGroupPath(), eventBase));
            }

            FileInfo infoFile = Managers.FileManager.CreateNewFile(new DirectoryInfo(Managers.FileManager.PathToHFOFolder+HFOPath),".xml");
            f.InfoFilePath = String.Format("{0}\\{1}",HFOPath,infoFile.Name);
            StreamWriter w = new StreamWriter(new FileStream(infoFile.FullName, FileMode.Create));
            try
            {
                f.MyInfoFile.RewriteFile(w);
            }
            finally
            {
                w.Dispose();
            }
            files.Add(f);
            return f;
        }

        internal MyFile AddFile(string pathFromDiskRoot, MyFile old)
        {
            string HFOPath = String.Format("\\FileInfo\\{0}\\File", Id);
            MyFile f = new MyFile(new MyInfoFile());
            f.Category = old.Category;
            f.FilePath = pathFromDiskRoot;
            f.MyInfoFile.CopyFrom(old.MyInfoFile);
            if (files.Count > 0) f.Folder = files[0].Folder;
            else
            {
                f.Folder = new MyRootFolder("", this, null);
            }
            
            FileInfo infoFile = Managers.FileManager.CreateNewFile(new DirectoryInfo(Managers.FileManager.PathToHFOFolder + HFOPath), ".xml");
            f.InfoFilePath = String.Format("{0}\\{1}", HFOPath, infoFile.Name);
            StreamWriter w = new StreamWriter(new FileStream(infoFile.FullName, FileMode.Create));
            try
            {
                f.MyInfoFile.RewriteFile(w);
            }
            finally
            {
                w.Dispose();
            }
            files.Add(f);
            return f;
        }

        /// <summary>
        /// Remove file from data tree and delete it's InfoFile
        /// </summary>
        /// <param name="v"></param>
        internal void RemoveFile(string v)
        {

            foreach (var f in files)
            {
                if (f.FilePath == v)
                {
                    files.Remove(f);
                    File.Delete(Managers.FileManager.PathToHFOFolder + f.InfoFilePath);
                    return;
                }
            }
            foreach (var d in folders)
            {
                if (v.StartsWith(d.Path))
                {
                    var i = d.GetEnd(v.Remove(0, d.Path.Length), v) as MyFile;
                    if (i != null)
                    {
                        i.Folder.files.Remove(i);
                        File.Delete(Managers.FileManager.PathToHFOFolder + i.InfoFilePath);
                        return;
                    }
                    
                }
            }

        }

        /// <summary>
        /// Prida soubor a vytvori fileinfo soubor, ale neprepise diskoverview soubor.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="category"></param>
        /// <param name="infoFilePath"></param>
        internal MyFile AddFile(string path, Category category, string infoFilePath)
        {
            MyFile f = new MyFile(category.GetMyFileInfoForFile(Id));
            f.InfoFilePath = infoFilePath;
            f.Category = category;
            f.FilePath = path;
            f.Folder = new MyRootFolder("", this, category);
            FileInfo fi= new FileInfo(Managers.FileManager.PathToHFOFolder+infoFilePath);
            StreamWriter w = new StreamWriter(fi.Open(FileMode.Create));
            try {
                f.MyInfoFile.RewriteFile(w);
            }
            finally { w.Dispose(); }
            files.Add(f);
            return f;
        }
        internal void RewriterDeviceFile(TextWriter w)
        {
            w.WriteLine("<disk id=\"{0}\">", this.Id);
            foreach (var x in files)
            {
                x.RewriteDeviceFile(w);
            }
            foreach (var x in folders)
            {
                x.RewriteDeviceFile(w);
            }
            w.WriteLine("</disk>");
        }


        public string GetPath()
        {
            return Managers.FileManager.GetDiskPath(this);
        }
        /// <summary>
        /// Find file by path from disk.
        /// </summary>
        /// <param name="v"></param>
        /// <returns>file instance, or null if not found</returns>
        internal MyFile GetFile(string v)
        {
            foreach(var f in files)
            {
                if (f.FilePath == v) return f;
            }
            foreach(var d in folders)
            {
                if (v.StartsWith(d.Path))
                {
                    var i=d.GetEnd(v.Remove(0, d.Path.Length), v);
                    return i as MyFile;
                }
            }
            return null;
        }
        internal Interfaces.IInfoGetter GetEnd(string v)
        {

            foreach (var f in files)
            {
                if (f.FilePath == v) return f;
            }
            foreach (var d in folders)
            {
                if (v.StartsWith(d.Path))
                {
                    var i = d.GetEnd(v.Remove(0, d.Path.Length), v);
                    return i;
                }
            }
            return null;
        }
    }
}

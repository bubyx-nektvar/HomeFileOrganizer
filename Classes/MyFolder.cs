using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeFileOrganizer.Classes
{
    public class MyFolder : Interfaces.IInfoGetter
    {
        /// <summary>
        /// Previus folder.
        /// Is null if root folder.
        /// </summary>
        public MyFolder UpperFolder;
        /// <summary>
        /// Relative path from upper folder.
        /// </summary>
        public string Path;
        /// <summary>
        /// File with information about folder. Realtive from HFO folder.
        /// Can be null, if not such file exists.
        /// </summary>
        public string InfoFilePath;
        private MyInfoFile _MyInfoFile;
        public MyInfoFile MyInfoFile
        {
            get
            {
                if (_MyInfoFile == null)
                {
                    _MyInfoFile = XMLProcessors.XmlReaders.readFileInfo(Managers.FileManager.PathToHFOFolder+InfoFilePath,GetDisk().Id).Result;
                }
                return _MyInfoFile;
            }
        }
        public List<MyFile> files = new List<MyFile>();
        public List<MyFolder> folders = new List<MyFolder>();
        /// <summary>
        /// Just for extension of this class, dont use if not nessesary.
        /// </summary>
        public MyFolder()
        {

        }
        public MyFolder(string path)
        {
            Path = path;
        }
        /// <summary>
        /// Return full path of folder.
        /// </summary>
        /// <returns></returns>
        public virtual string GetPath()
        {
            StringBuilder sb = new StringBuilder();
            MyFolder mf=this;
            while (mf != null)
            {
                sb.Insert(0, mf.Path);
                mf = mf.UpperFolder;
            }
            return sb.ToString();
        }

        internal void RewriteDeviceFile(System.IO.TextWriter w)
        {
            w.WriteLine("<folder path=\"{0}\" category=\"{1}\" infoFile=\"{2}\">");
            foreach (var x in files) x.RewriteDeviceFile(w);
            foreach (var x in folders) x.RewriteDeviceFile(w);
            w.WriteLine("</folder>");
        }

        internal MyDisk GetDisk()
        {
            MyFolder mf=this;
            while (!(mf is MyRootFolder)) mf = mf.UpperFolder;
            return ((MyRootFolder)mf).Disk;
        }
        /// <summary>
        /// Find end 
        /// </summary>
        /// <param name="relativePath">Relativ path of countinu lookup. <para>(<paramref name="fullPath"/> without this folder path)</para></param>
        /// <param name="fullPath">Full path from root of disk</param>
        /// <returns>null if path doesn't end in this folder(or subfolders)</returns>
        public Interfaces.IInfoGetter GetEnd(string relativePath, string fullPath)
        {
            if (relativePath == "") return this;
            Interfaces.IInfoGetter result = null;
            result = this.files.Find((x) => { return x.FilePath == fullPath; });
            if (result != null) return result;
            else
            {
                foreach (MyFolder f in folders)
                {
                    if (relativePath.StartsWith(f.Path))
                    {
                        result = f.GetEnd(relativePath.Remove(0, f.Path.Length + 1), fullPath);
                        if (result != null) return result;
                    }
                }
            }
            return result;
        }

        public string GetInfoFilePath()
        {
            return InfoFilePath;
        }

        public MyInfoFile GetInfoFile()
        {
            return MyInfoFile;
        }
    }
}

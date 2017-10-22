using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HomeFileOrganizer.Classes
{
    public class MyFile:Interfaces.IInfoGetter
    {
        /// <summary>
        /// Relative path from disk root folder to file.
        /// </summary>
        public string FilePath;
        /// <summary>
        /// Relative path from HFO folder to file with information about this file.
        /// </summary>
        public string InfoFilePath;
        /// <summary>
        /// Folder that contains this file
        /// </summary>
        public MyFolder Folder;
        public Category Category;
        private MyInfoFile _MyInfoFile;
        public MyInfoFile MyInfoFile
        {
            get
            {
                if (_MyInfoFile == null)
                {
                    var x =XMLProcessors.XmlReaders.readFileInfo(Managers.FileManager.PathToHFOFolder+InfoFilePath,Folder.GetDisk().Id);
                    _MyInfoFile =x.Result;
                }
                return _MyInfoFile;
            }
        }
        /// <summary>
        /// Create file that is inside <paramref name="f"/>
        /// </summary>
        public MyFile(MyFolder f)
        {
            Folder = f;
        }

        internal string[] GetColumns()
        {
            string name = MyInfoFile.GetColumn("system\\name");
            if (name == null || name == "") name = FilePath;
            return new string[] { name, MyInfoFile.GetColumn("system\\size"),
                MyInfoFile.GetColumn("system\\type"), MyInfoFile.GetColumn("system\\change"),
                MyInfoFile.GetColumn("system\\creation") };
        }

        public MyFile(Classes.MyInfoFile myInfoFile)
        {
            _MyInfoFile = myInfoFile;
        }


        internal void RewriteDeviceFile(TextWriter w)
        {
            w.WriteLine("<file path=\"{0}\" infoFile=\"{1}\" category=\"{2}\"/>",FilePath, InfoFilePath.Substring(InfoFilePath.LastIndexOf('\\') + 1), Category.Name);
        }
        /// <summary>
        /// Return my system free path strting with disk id
        /// </summary>
        /// <returns></returns>
        internal string GetPath()
        {
            return Folder.GetDisk().Id.ToString()+ this.FilePath;
        }
        internal AppTreePosition GetPath(HomeData dat)
        {
            var dis = Folder.GetDisk();
            var dev= dat.devices.Find((i) => { return i.disks.Contains(dis); });
            return new AppTreePosition(dev, dis, this);
        }
        /// <summary>
        /// Get relative path to HFO folder
        /// </summary>
        /// <returns></returns>
        string Interfaces.IInfoGetter.GetInfoFilePath()
        {
            return InfoFilePath;
        }

        MyInfoFile Interfaces.IInfoGetter.GetInfoFile()
        {
            return MyInfoFile;
        }
    }
}

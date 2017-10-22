using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HomeFileOrganizer.XMLProcessors;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class FileDeleteEv:FilesEvents
    {
        public const string Xml = "deleteFile";
        public string InfoFilePath {get; private set; }
        public static string XmlInfoFilePath = "infoFile";

        public FileDeleteEv(string infoFilePath, FilesEvents fe)
            : base(fe)
        {
            InfoFilePath = infoFilePath;
        }

        public FileDeleteEv(FilesEvents fe, XmlReader r) : base(fe)
        {
            InfoFilePath = r.GetAttribute(XmlInfoFilePath);
        }
        
        public override ElementBuilder GetXmlBuilder()
        {
            return base.GetXmlBuilder().AddElement(new ElementBuilder(Xml).AddAtribute<string>(XmlInfoFilePath, InfoFilePath));
        }
        public override void DoEvent(FileManager fm)
        {
            Classes.MyDisk d = fm.dataHolder.Select(DeviceId, DiskId);
            var f = d.files.Find((x) => { return x.FilePath == this.Path; });
            d.files.Remove(f);
            System.IO.FileInfo fi = new System.IO.FileInfo(FileManager.PathToHFOFolder + this.InfoFilePath);
            fi.Delete();
            string diskPath = d.GetPath();
            if (diskPath != null)System.IO.File.Delete(diskPath + f.FilePath);
            fm.dataHolder.devices.Find((x) => { return x.Id == this.DeviceId; }).RewriteDeviceFile();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeFileOrganizer.XMLProcessors;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class FileRemoveEvent:FilesEvents
    {
        public const string Xml = "fileRemove";
        public string InfoFilePath { get; private set; }
        public static string XmlInfoFilePath = "infoFile";
        public FileRemoveEvent(FilesEvents fe,string infoFilePath) : base(fe) {
            InfoFilePath = infoFilePath;
        }
        public FileRemoveEvent(FilesEvents fe,System.Xml.XmlReader r) : base(fe)
        {
            InfoFilePath=r.GetAttribute(XmlInfoFilePath);
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
            fm.dataHolder.devices.Find((x) => { return x.Id == this.DeviceId; }).RewriteDeviceFile();
        }
    }
}

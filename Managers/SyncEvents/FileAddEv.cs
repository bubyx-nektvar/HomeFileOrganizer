using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HomeFileOrganizer.XMLProcessors;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class FileAddEv:FilesEvents
    {
        public const string Xml = "addFile";
        public Classes.Category Category { get; private set; }
        public static string XmlCategory="category";
        /// <summary>
        /// Relative path from HFO folder
        /// </summary>
        public string InfoFilePath { get; private set; }
        public static string XmlInfoFilePath="infoFile";
        
        public FileAddEv(FilesEvents fe, Classes.Category cat, string ifPath):base(fe)
        {
            Category = cat;
            InfoFilePath = ifPath;
        }

        public FileAddEv(FilesEvents fe, XmlReader reader) : base(fe)
        {
            Category = Classes.Category.GetCategory(reader.GetAttribute(XmlCategory));
            InfoFilePath = reader.GetAttribute(XmlInfoFilePath);
        }
        public override ElementBuilder GetXmlBuilder()
        {
            var b = base.GetXmlBuilder();
            b.AddElement(new ElementBuilder(Xml).AddAtribute<string>(XmlCategory, Category.Name)
                .AddAtribute<string>(XmlInfoFilePath, InfoFilePath));
            return b;
        }
        public override void DoEvent(FileManager fm)
        {
            Classes.MyDisk d=fm.dataHolder.Select(DeviceId, DiskId);
            d.AddFile(this.Path, this.Category, this.InfoFilePath);
            fm.dataHolder.devices.Find((x) => { return x.Id == this.DeviceId; }).RewriteDeviceFile();
        }
    }
}

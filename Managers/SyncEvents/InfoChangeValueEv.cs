using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HomeFileOrganizer.XMLProcessors;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class InfoChangeValueEv:InfoChangeEvents
    {
        public const string Xml = "value";
        /// <summary>
        /// Path through info file.
        /// <example>({group.name}/)*{item.name}</example>
        /// </summary>
        public string GroupPath {get; private set; }
        public static string XmlGroupPath = "group";
        
        public string Value {get; private set; }
        public InfoChangeValueEv(string value, string groupPath, InfoChangeEvents ice)
            : base(ice)
        {
            Value = value;
            GroupPath = groupPath;
        }

        public InfoChangeValueEv(string value, InfoChangeValueEv cve):base(cve)
        {
            Value = value;
            this.GroupPath=cve.GroupPath;
        }

        public InfoChangeValueEv(string v, XmlReader reader, InfoChangeEvents infoEvent):base(infoEvent)
        {
            Value = v;
            GroupPath = reader.GetAttribute(XmlGroupPath);
        }
        public override ElementBuilder GetXmlBuilder()
        {
            return base.GetXmlBuilder().AddElement(
                new ElementBuilder(Xml).AddAtribute<string>(XmlGroupPath,GroupPath).AddValue(Value));
        }
        public override void DoEvent(FileManager fm)
        {
            var x = fm.GetPathEnd(this.Path).Result;
            x.GetInfoFile().GetItem(this.GroupPath).ChangeInfoBySyncString(Value,Time);
            var w = new System.IO.StreamWriter(FileManager.PathToHFOFolder+x.GetInfoFilePath());
            try
            {
                x.GetInfoFile().RewriteFile(w);
            }
            finally { w.Dispose(); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeFileOrganizer.XMLProcessors;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class InfoChangeEvents:Events
    {
        public const string Xml = "infoChange";
        /// <summary>
        /// Relative path to info file through my file system tree
        /// <example>{deviceId}/{diskId}/({folder}/)*{file}</example>
        /// </summary>
        public string Path {get; private set; }
        public static string XmlPath = "path";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="path">Relative path to info file through my file system tree</param>
        public InfoChangeEvents(System.Xml.XmlReader r):base(r)
        {
            Path = r.GetAttribute(XmlPath);

        }
        public InfoChangeEvents(InfoChangeEvents ice)
        {
            this.Path = ice.Path;
            this.Time = ice.Time;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="path">Relative path to info file through my file system tree</param>
        public InfoChangeEvents(DateTime time, string path)
        {
            Time = time;
            Path = path;
        }
        public override ElementBuilder GetXmlBuilder()
        {
            return base.GetXmlBuilder().SetName(Xml).AddAtribute<string>(XmlPath, Path);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class Events
    {
        public const string Xml = "event";
        public static System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.InvariantCulture;
        /// <summary>
        /// Time of event
        /// </summary>
        public DateTime Time {get; protected set; }
        public static string XmlTime = "timeStamp";
        public Events() {
            Time = DateTime.Now;
        }
        public Events(XmlReader reader)
        {
            Time = DateTime.Parse(reader.GetAttribute(XmlTime),culture);
        }

        public virtual void DoEvent(FileManager fm)
        {
            throw new NotImplementedException();
        }
        public virtual string GetXml()
        {
            return GetXmlBuilder().GetXml();
        }
        public virtual XMLProcessors.ElementBuilder GetXmlBuilder()
        {
            var b = new XMLProcessors.ElementBuilder(Xml);
            b.AddAtribute<DateTime>(XmlTime, Time);
            return b;
        }
    }
}

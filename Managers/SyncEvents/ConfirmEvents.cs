using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HomeFileOrganizer.XMLProcessors;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class ConfirmEvents:Events
    {
        /// <summary>
        /// Definition of element name for Xml file.
        /// </summary>
        public const string Xml = "confirm";
        public UInt64 DeviceId { get; private set; }
        public static string XmlDeviceId = "deviceId";
        public UInt64 DiskId {get; private set; }
        public static string XmlDiskId = "diskId";
        public string Path {get; private set; }
        public static string XmlPath = "path";

        public ConfirmEvents(DateTime time,ulong disk, ulong device, string path)
        {
            Time = time;
            DeviceId = device;
            DiskId = disk;
            Path = path;
        }
        public ConfirmEvents(ConfirmEvents ce)
        {
            this.Time = ce.Time;
            this.Path = ce.Path;
            this.DiskId = ce.DiskId;
            this.DeviceId = ce.DeviceId;
        }

        public ConfirmEvents(XmlReader reader):base(reader)
        {
            DiskId = ulong.Parse(reader.GetAttribute(XmlDiskId),culture);
            Path = reader.GetAttribute(XmlPath);
            DeviceId = ulong.Parse(reader.GetAttribute(XmlDeviceId),culture);
        }
        public override ElementBuilder GetXmlBuilder()
        {
            return base.GetXmlBuilder().SetName(Xml).AddAtribute<ulong>(XmlDeviceId, DeviceId)
                .AddAtribute<ulong>(XmlDiskId, DiskId).AddAtribute<string>(XmlPath, Path);
        }
    }
}

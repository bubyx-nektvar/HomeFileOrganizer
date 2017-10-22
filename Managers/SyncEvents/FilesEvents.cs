using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeFileOrganizer.XMLProcessors;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class FilesEvents:Events
    {
        public const string Xml = "fileEvent";
        /// <summary>
        /// Device where data was changed
        /// </summary>
        public UInt64 DeviceId { get;private set; }
        public static string XmlDeviceId = "deviceId";
        /// <summary>
        /// Disk where data was changed
        /// </summary>
        public UInt64 DiskId {get; private set;  }
        public static string XmlDiskId = "diskId";
        /// <summary>
        /// Relative path from root of disk
        /// </summary>
        public string Path {get; private set; }
        public static string XmlPath = "path";
        public FilesEvents(System.Xml.XmlReader r):base(r)
        {
            DeviceId = UInt64.Parse(r.GetAttribute(XmlDeviceId), culture);
            DiskId = UInt64.Parse(r.GetAttribute(XmlDiskId), culture);
            Path = r.GetAttribute(XmlPath);
        }
        public FilesEvents(FilesEvents fe)
        {
            this.Time = fe.Time;
            this.Path=fe.Path;
            this.DiskId=fe.DiskId;
            this.DeviceId = fe.DeviceId;
        }
        public FilesEvents(ulong diskId, ulong deviceId,string path)
        {
            Time = DateTime.Now;
            DiskId = diskId;
            DeviceId=deviceId;
            Path = path;
        }
        public override ElementBuilder GetXmlBuilder()
        {
            return base.GetXmlBuilder()
                .SetName(Xml)
                .AddAtribute<ulong>(XmlDeviceId, DeviceId)
                .AddAtribute<ulong>(XmlDiskId, DiskId)
                .AddAtribute<string>(XmlPath, Path);
        }
    }
}

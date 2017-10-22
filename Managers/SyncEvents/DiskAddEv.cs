using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using HomeFileOrganizer.XMLProcessors;
using HomeFileOrganizer.Classes;
using System.Xml;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    class DiskAddEv:Events
    {
        public const string Xml = "addDisk";
        /// <summary>
        /// Device on which was new disk discovered
        /// </summary>
        private ulong DeviceId;
        private static string Xml_DeviceId = "devId";

        private ulong DiskId;
        private static string Xml_DiskId = "diskId";

        private string DiskName;
        private static string Xml_DiskName = "diskName";
        public DiskAddEv(MyDisk d,FileManager fm)
        {
            DeviceId=fm.dataHolder.GetDevice(d).Id;
            DiskId = d.Id;
            DiskName = d.Name;
        }
        public DiskAddEv(XmlReader r) : base(r)
        {
            DeviceId = ulong.Parse(r.GetAttribute(Xml_DeviceId),culture);
            DiskId = ulong.Parse(r.GetAttribute(Xml_DiskId),culture);
            DiskName = r.GetAttribute(Xml_DiskName);
        }
        public override void DoEvent(FileManager fm)
        {
            fm.dataHolder.AddDisk(DeviceId, DiskId, DiskName);
        }
        public override ElementBuilder GetXmlBuilder()
        {
            return base.GetXmlBuilder().SetName(Xml).AddAtribute<ulong>(Xml_DeviceId, DeviceId)
                .AddAtribute<ulong>(Xml_DiskId, DiskId).AddAtribute<string>(Xml_DiskName, DiskName);
        }
    }
}

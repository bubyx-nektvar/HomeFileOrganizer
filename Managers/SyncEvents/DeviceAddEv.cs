using HomeFileOrganizer.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    class DeviceAddEv:Events
    {
        public const string Xml = "addDevice";
        private static string Xml_NewDiskId = "id";
        private ulong NewDiskId;
        private static string Xml_DiskName = "name";
        private string DiskName;
        public DeviceAddEv(XmlReader r) : base(r)
        {
            NewDiskId = ulong.Parse(r.GetAttribute(Xml_NewDiskId));
            DiskName = r.GetAttribute(Xml_DiskName);
        }
        public DeviceAddEv(MyDevice d):base()
        {
            NewDiskId = d.Id;
            DiskName = d.Name;
        }
        public override void DoEvent(FileManager fm)
        {
            fm.dataHolder.AddDevice(NewDiskId, DiskName);
        }
        public override XMLProcessors.ElementBuilder GetXmlBuilder()
        {
            return base.GetXmlBuilder().SetName(Xml).AddAtribute<string>(Xml_DiskName, DiskName).AddAtribute<ulong>(Xml_NewDiskId, NewDiskId);
        }
    }
}

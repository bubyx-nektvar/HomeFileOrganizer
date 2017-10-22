using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HomeFileOrganizer.XMLProcessors;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class FileMovedEv:ConfirmEvents
    {
        public const string Xml = "fileMoved";
        public string originAppPath {get;private set; }
        public static string XmlOriginAppPath="origin";
        public ulong originDevice { get; private set; }
        public static string XmlOriginDevice = "origDev";
        public bool copy { get; private set; }
        public static string XmlCopy = "copy";

        public FileMovedEv(ConfirmEvents ce,string origAppPath,ulong origDev,bool c)
            : base(ce)
        {
            this.originAppPath = origAppPath ;
            this.originDevice = origDev;
            copy = c;
        }

        public FileMovedEv(ConfirmEvents ce, XmlReader reader) : base(ce)
        {
            originAppPath = reader.GetAttribute(XmlOriginAppPath);
            originDevice = ulong.Parse(reader.GetAttribute(XmlOriginDevice),culture);
            copy = bool.Parse(reader.GetAttribute(XmlCopy));
        }

        public override ElementBuilder GetXmlBuilder()
        {
            return base.GetXmlBuilder().AddElement(new ElementBuilder(Xml).AddAtribute<string>(XmlOriginAppPath,originAppPath)
                .AddAtribute<ulong>(XmlOriginDevice,originDevice).AddAtribute<bool>(XmlCopy,copy));
        }
        public override void DoEvent(FileManager fm)
        {
            var s=originAppPath.Split(new char[] { '\\' }, 3);
            fm.PathChanged(originAppPath, String.Format("{0}\\{1}\\{2}", DeviceId, DiskId, Path), !copy).Wait();
            var dev=fm.GetLocalDeviceInstance();
            if (originDevice ==dev.Id && (!copy))
            {
                string d=FileManager.GetDiskPath(fm.dataHolder.Select(s[0], s[1]));
                System.IO.File.Delete(d + s[2]);
            }
        }
    }
}

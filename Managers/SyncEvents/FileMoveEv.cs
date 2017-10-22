using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HomeFileOrganizer.XMLProcessors;

namespace HomeFileOrganizer.Managers.SyncEvents
{
    public class FileMoveEv:FilesEvents
    {
        public const string Xml = "fileMove";
        public UInt64 TargetDeviceId {get; private set; }
        public static string XmlTargetDeviceId = "targetDevice";
        public bool copy
        {
            get; private set;
        }
        public static string XmlCopy = "copy";
        

        public FileMoveEv(FilesEvents fe, XmlReader reader) : base(fe)
        {
            TargetDeviceId = ulong.Parse(reader.GetAttribute(XmlTargetDeviceId),culture);
            copy = bool.Parse(reader.GetAttribute(XmlCopy));
        }
        public FileMoveEv(ulong fromDevice,ulong fromDisk,string fromPath, ulong toDevice,bool createCopy) :base(fromDisk,fromDevice,fromPath)
        {
            this.copy =createCopy;
            this.TargetDeviceId = toDevice;
        }

        public override ElementBuilder GetXmlBuilder()
        {
            return base.GetXmlBuilder().AddElement(new ElementBuilder(Xml).AddAtribute<ulong>(XmlTargetDeviceId,TargetDeviceId)
                .AddAtribute<bool>(XmlCopy,copy));
        }
        public override void DoEvent(FileManager fm)
        {
            var dis=fm.GetDiskInstance(new DirectoryInfo(FileManager.PathToHFOFolder));
            var dev = fm.GetLocalDeviceInstance();
            if (dev.Id == TargetDeviceId)
            {
                var iFile = fm.dataHolder.Select(DeviceId,DiskId).GetEnd(Path);
                var device = fm.dataHolder.devices.Find((i) => { return i.Id == DeviceId; });
                var mFile = iFile as Classes.MyFile;
                if (mFile != null)
                {
                    string ext = mFile.FilePath.Substring(mFile.FilePath.LastIndexOf('.'));
                    FileInfo fileTarget=FileManager.CreateNewFile(new DirectoryInfo(string.Format("{0}\\AppFiles\\{1}", FileManager.PathToHFOFolder, mFile.Category.Name)),ext);
                    string oldAppPath = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\\{1}\\{2}", DeviceId, DiskId, Path);
                    var nmFile=fm.PathChanged(oldAppPath, string.Format("{0}\\{1}\\{2}", dev.Id, dis.Id, fileTarget.FullName.Substring(fileTarget.Directory.Root.FullName.Length)),(!copy)).Result;
                    Task t = device.Connection.EnqueueRequestDownload(oldAppPath, fileTarget.FullName, false);
                    device.Connection.DequeueRequest().Wait();
                    fm.syncManager.AddGeneratedSyncEvent(new FileMovedEv(new ConfirmEvents(DateTime.Now, dis.Id, dev.Id, nmFile.FilePath),oldAppPath,DeviceId,copy));
                }
                else { //Jedna se o slozku
                    throw new NotImplementedException();
                }
            }
            
        }
    }
}

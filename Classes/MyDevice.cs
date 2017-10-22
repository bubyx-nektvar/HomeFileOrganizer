using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace HomeFileOrganizer.Classes
{
    public class MyDevice
    {
        /// <summary>
        /// Id of device
        /// </summary>
        public UInt64 Id;
        /// <summary>
        /// User defined name of device
        /// </summary>
        public string Name;
        /// <summary>
        /// 
        /// </summary>
        public Communication Communicate;
        /// <summary>
        /// Running PC is synced to SyncedTo file on PC represented by this instance
        /// </summary>
        public string SyncedTo;
        /// <summary>
        /// Pc represented by this instance is synced to RunOn file on running PC
        /// </summary>
        public string RunOn;
        private string deviceFile;
        /// <summary>
        /// Disk located in device
        /// </summary>
        public List<MyDisk> disks = new List<MyDisk>();
        /// <summary>
        /// True if device file was readed
        /// </summary>
        public bool AreDisksLoaded = false;
        public Communicator Connection;
        public MyDevice(UInt64 id, string name, Communication com, string synTo,string runOnVer ,string file)
        {
            Id = id;
            Name = name;
            Communicate = com;
            SyncedTo = synTo;
            RunOn = runOnVer;
            deviceFile = file;
        }
        /// <summary>
        /// Read device file with content of disks.
        /// </summary>
        /// <returns></returns>
        public void LoadDisks()
        {
            if (!AreDisksLoaded)
            {
                if (!File.Exists(Managers.FileManager.PathToHFOFolder + "\\DeviceInfos\\" + deviceFile)) RewriteDeviceFile();
                XMLProcessors.XmlReaders.readDeviceFile(deviceFile, this).Wait();
                this.AreDisksLoaded = true;
            }
        }
        public async Task LoadDisksAsync()
        {
            if (!AreDisksLoaded)
            {
                if (!File.Exists(Managers.FileManager.PathToHFOFolder + "\\DeviceInfos\\" + deviceFile)) RewriteDeviceFile();
                await XMLProcessors.XmlReaders.readDeviceFile(deviceFile, this);
                this.AreDisksLoaded = true;
            }

        }

        internal void RewriteOverviewFile(StreamWriter w)
        {
            if (this.Communicate == Communication.Network) {
                w.WriteLine("<computer deviceId=\"{0}\" deviceFile=\"{1}\"  name=\"{2}\" syncedTo=\"{3}\" runOn=\"{4}\">",this.Id,this.deviceFile,this.Name,this.SyncedTo,this.RunOn);
                foreach(var d in disks)
                {
                    w.WriteLine("<disk deviceId=\"{0}\" name=\"{1}\"/>",d.Id,d.Name);
                }
                w.WriteLine("</computer>");
            }
            else
            {
                w.WriteLine("<disk deviceId=\"{0}\" deviceFile=\"{1}\"  name=\"{2}\" syncedTo=\"{3}\" runOn=\"{4}\"/>",Id,deviceFile,Name,SyncedTo,RunOn);
            }
        }
        /// <summary>
        /// Rewrite device file with data in disks informations.
        /// Dont solve problem if disk wasnt loaded before.
        /// </summary>
        internal void RewriteDeviceFile()
        {
            TextWriter w = new StreamWriter(new FileStream(Managers.FileManager.PathToHFOFolder+ "\\DeviceInfos\\" + this.deviceFile, FileMode.Create));
            try
            {
                w.WriteLine(XMLProcessors.XmlCreator.XmlIntro);
                w.WriteLine("<device>");
                foreach (MyDisk d in disks)
                {
                    d.RewriterDeviceFile(w);
                }
                w.WriteLine("</device>");
            }
            finally
            {
                w.Dispose();
            }
        }

        internal MyDisk AddDisk(string name)
        {
            byte[] buff = new byte[sizeof(ulong)];
            Random r = new Random();
            ulong id = 0;
            do
            {
                r.NextBytes(buff);
                id = BitConverter.ToUInt64(buff, 0);
            } while (disks.Find((x) => { return x.Id == id; }) != null);
            var dis = new MyDisk(id,name);
            disks.Add(dis);
            return dis;
        }

        internal void Remove(MyDisk disk)
        {
            disks.Remove(disk);
        }
        public override string ToString()
        {
            return String.Format("{0} ({1})",Name,Id);
        }
    }
    public enum Communication
    {
        Network,
        PeriferDisk,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HomeFileOrganizer.XMLProcessors;
using HomeFileOrganizer.Classes.Interfaces;
using System.Collections;

namespace HomeFileOrganizer.Classes
{
    public class HomeData:IEnumerable<IInfoGetter>
    {
        public List<MyDevice> devices = new List<MyDevice>();
        private object rewriteOverviewLocker = new object();

        internal void AddDevice(ulong newDeviceId, string diskName)
        {
            var dev=devices.Find(i => { return i.Id == newDeviceId; });
            if (dev == null)
            {
                devices.Add(new MyDevice(newDeviceId, diskName, Communication.Network, "", "", newDeviceId + ".xml"));
                RewriteOverviewFile();
            }
        }
        internal void AddDisk(ulong device, ulong disk, string diskName)
        {
            var d = Select(device, disk);
            if (d == null)
            {
                d = new MyDisk(disk, diskName);
                devices.Find(i => { return i.Id == device; }).disks.Add(d);
                Directory.CreateDirectory(String.Format("{0}\\FileInfo\\{1}\\File", Managers.FileManager.PathToHFOFolder,d.Id));
                Directory.CreateDirectory(String.Format("{0}\\FileInfo\\{1}\\Folder", Managers.FileManager.PathToHFOFolder,d.Id));
                Directory.CreateDirectory(String.Format("{0}\\FileInfo\\{1}\\ItemFiles", Managers.FileManager.PathToHFOFolder,d.Id));
            }
            
        }

        public System.Threading.ReaderWriterLockSlim editLock = new System.Threading.ReaderWriterLockSlim();

        internal MyDevice GetDevice(MyDisk d)
        {
            return devices.Find(i => { return i.disks.Contains(d);});
        }

        public HomeData()
        {
            try
            {
                lock (rewriteOverviewLocker)
                {
                    devices = XmlReaders.readOverview("OverviewFile.xml");
                }
            }
            catch (System.Xml.XmlException e) { }
            catch (IOException e) { }
        }
        public HomeData(bool clear)
        {
            if (true)
            {

            }
            else
            {
                try
                {
                    lock (rewriteOverviewLocker)
                    {
                        devices = XmlReaders.readOverview("OverviewFile.xml");
                    }
                }
                catch (System.Xml.XmlException e) { }
                catch (IOException e) { }

            }
        }
        internal MyDisk Select(ulong deviceId, ulong diskId)
        {
            return devices.Find((x) => {
                x.LoadDisks();
                return x.Id == deviceId;
            }).disks.Find((x)=>
            {
                return x.Id == diskId;
            });
        }
        /// <summary>
        /// Select
        /// </summary>
        /// <param name="path">{deviceId}/{diskId}/{application tree path}</param>
        /// <returns></returns>
        internal Interfaces.IInfoGetter Select(string path)
        {
            string[] ss=path.Split(new char[] { '\\' },3);
            var d=Select(ulong.Parse(ss[0]), ulong.Parse(ss[1]));
            return d.GetEnd(ss[2]);
        }

        internal async void Load()
        {
            var dd = new List<Task>();
            foreach(var d in devices)
            {
                var t = d.LoadDisksAsync();
                dd.Add(t);
            }
            await Task.WhenAll(dd.ToArray());
        }
        /// <summary>
        /// Vygeneruje id pro novy disk a prida disk do systemu
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Pridany disk</returns>
        internal MyDevice AddDevice(string name)
        {
            byte[] buff = new byte[sizeof(ulong)];
            Random r = new Random();
            ulong id = 0;
            do {
                r.NextBytes(buff);
                id = BitConverter.ToUInt64(buff, 0);
            } while (devices.Find((x) => { return x.Id == id; }) != null || id==0);
            var dev = new MyDevice(id, name, Communication.Network, "0", "0",id+".xml");
            devices.Add(dev);
            return dev;
        }

        public MyDisk Select(string deviceId, string diskId)
        {
            return devices.Find((i) => { return i.Id == ulong.Parse(deviceId); }).disks.Find((i) => { return i.Id == ulong.Parse(diskId); });
        }

        internal void RewriteOverviewFile()
        {
            lock (rewriteOverviewLocker)
            {
                StreamWriter w = new StreamWriter(new FileStream(Managers.FileManager.PathToHFOFolder + "\\OverviewFile.xml", FileMode.Create));
                try
                {
                    w.WriteLine(XmlCreator.XmlIntro);
                    w.WriteLine("<dataStorage>");
                    foreach (var d in devices) d.RewriteOverviewFile(w);
                    w.WriteLine("</dataStorage>");
                }
                finally
                {
                    w.Dispose();
                }
            }
        }

        internal void Reload()
        {
            try
            {
                lock (rewriteOverviewLocker)
                {
                    devices = XmlReaders.readOverview("OverviewFile.xml");
                }
            }
            catch (System.Xml.XmlException e) { }
            catch (IOException e) { }
        }
        
        IEnumerator<IInfoGetter> IEnumerable<IInfoGetter>.GetEnumerator()
        {
            return new HDEnum(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new HDEnum(this);
        }
    }
    class AppTreePosition
    {
        public MyDisk Disk { get; private set; }
        public MyDevice Device { get; private set; }
        public IInfoGetter File { get; private set; }
        public AppTreePosition(MyDevice dev, MyDisk dis, IInfoGetter file)
        {
            Device = dev;
            Disk = dis;
            File = file;
        }
    }
    class HDEnum : IEnumerator<IInfoGetter>, IEnumerator<AppTreePosition>
    {
        private HomeData dat;
        private IEnumerator<MyDevice> dev;
        private IEnumerator<MyDisk> disk;
        private Stack<IEnumerator<MyFolder>> dir;
        private IEnumerator<MyFile> file;
        public HDEnum(HomeData dat)
        {
            this.dat = dat;
            dev = dat.devices.GetEnumerator();
        }
        AppTreePosition Current
        {
            get
            {
                if (file != null && file.Current != null) return new AppTreePosition(dev.Current, disk.Current, file.Current);
                else if (dir != null&&dir.Count>0 && dir.Peek().Current != null) return new AppTreePosition(dev.Current, disk.Current, dir.Peek().Current);
                else return null;
            }
        }
        IInfoGetter IEnumerator<IInfoGetter>.Current
        {
            get
            {
                if (file != null && file.Current != null) return file.Current;
                else if (dir != null && dir.Count>0&& dir.Peek().Current != null) return dir.Peek().Current;
                else return null;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        AppTreePosition IEnumerator<AppTreePosition>.Current
        {
            get
            {
                return Current;
            }
        }

        public void Dispose()
        {
            dat = null;
            dev = null;
            dir = null;
            disk = null;
            file = null;
        }

        public bool MoveNext()
        {
            do
            {
                if (dev != null)//1
                {
                    if (disk == null)//2
                    {
                        if (dev.MoveNext())
                        {
                            dev.Current.LoadDisks();
                            disk = dev.Current.disks.GetEnumerator();
                        }
                        else dev = null;//1
                    }
                    else
                    {
                        if (dir == null)
                        {
                            if (disk.MoveNext())
                            {
                                file = disk.Current.files.GetEnumerator();
                                dir = new Stack<IEnumerator<MyFolder>>();
                                dir.Push(disk.Current.folders.GetEnumerator());
                            }
                            else disk = null;//2
                        }
                        else
                        {
                            if (file != null)
                            {
                                if (!file.MoveNext()) file = null;
                            }
                            else
                            {
                                if (dir.Count > 0)
                                {
                                    if (dir.Peek().MoveNext())
                                    {
                                        var d = dir.Peek().Current;
                                        file = d.files.GetEnumerator();
                                        dir.Push(d.folders.GetEnumerator());
                                    }
                                    else
                                    {
                                        dir.Pop();
                                    }
                                }
                                else dir = null;//zpusobi posun v discich
                            }
                        }
                    }
                }
                else return false;
            } while (Current == null);
            return true;
        }

        public void Reset()
        {

            dev = dat.devices.GetEnumerator();
            dir = null;
            disk = null;
            file = null;
        }
    }
}

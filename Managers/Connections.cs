using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using HomeFileOrganizer.Classes;
using System.Windows.Forms;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UnitTestProject1", AllInternalsVisible = true)]
namespace HomeFileOrganizer.Managers
{
    
    public class Connections
    {
        public static StreamWriter logger = new StreamWriter("ConnectionLogger.txt",true);
        /// <summary>
        /// Port that is opened for incoming connections to this instance of program.
        /// </summary>
        private int ConnectionPort=0;
        private FileManager fileManager;
        /// <summary>
        /// Instance of devicethat run this instance of application
        /// </summary>
        private MyDevice device;
        private Synchronization synchronization;
        private List<Task> comunications = new List<Task>();
        private Form1 myForm;

        private System.Threading.CancellationToken shouldEnd = new System.Threading.CancellationToken();
        static Connections() {
            logger.AutoFlush = true;
        }
        public Connections(FileManager data,MyDevice device,Synchronization sync,Form1 form)
        {
            this.fileManager = data;
            this.device = device;
            this.synchronization = sync;
            myForm = form;
        }
        /// <summary>
        /// Start broadcaster, broadcast listener and connection listener.
        /// </summary>
        /// <returns>Task that wait for broadcaster, broadcast listener and conection listener to end.</returns>
        public Task RunConnection()
        {
            Task[] ts = new Task[3];
            ts[2] = ListenConnections();
            ts[0] = ListenBroadcast();
            ts[1] = Broadcast();
            return Task.WhenAll(ts);
        }
        /// <summary>
        /// Set information for all connection task, that they should end.
        /// </summary>
        public void SetClose()
        {
            shouldEnd = new System.Threading.CancellationToken(true);
        }
#if DEBUG
        internal int GetPort()
        {
            while (ConnectionPort == 0) { Task.Delay(100).Wait(); }
            return this.ConnectionPort;
        }
#endif
        /// <summary>
        /// Broadcast information, that instance of application on this computer is connected to local network.
        /// </summary>
        internal Task Broadcast()
        {
            return Task.Run(async() =>
            {
                UdpClient s = new UdpClient();
                s.EnableBroadcast = true;
                s.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                s.ExclusiveAddressUse = false;
                StringBuilder ips = new StringBuilder();
                foreach (IPAddress a in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    ips.Append(a.ToString());
                    ips.Append(",");
                }
                while (ConnectionPort == 0)
                {
                    Task.Delay(100).Wait();
                }
                string mes = String.Format("v1.MFO.MFF.works.bubyx;id={0};ip={1};port={2}", device.Id, ips, ConnectionPort);
                byte[] message = Communicator.coding.GetBytes(mes);
                IPEndPoint end = new IPEndPoint(IPAddress.Broadcast, Properties.Settings.Default.broadcastPort);
                while (!shouldEnd.IsCancellationRequested)
                {
                    s.Send(message,message.Length, end);
                    //Managers.Connections.logger.WriteLine("Broadcast mess {1}: {0}",mes,DateTime.Now);
                    await Task.Delay(2000);
                }
            });
        }
        /// <summary>
        /// Finding others devices in network, that are in relation with this application data.
        /// </summary>
        internal Task ListenBroadcast()
        {
            var t= new Task(() =>
            {
                UdpClient listener = new UdpClient();
                listener.EnableBroadcast = true;
                listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Client.ReceiveTimeout = 5000;
                listener.Client.Bind(new IPEndPoint(IPAddress.Any, Properties.Settings.Default.broadcastPort));
                while (!shouldEnd.IsCancellationRequested)
                {
                    var end = new IPEndPoint(IPAddress.Any, 0);
                    byte[] mess = new byte[0];
                    try
                    {
                        mess = listener.Receive(ref end);
                    }
                    catch (SocketException e) {
                        listener.Send(new byte[] { 0 }, 0, new IPEndPoint(IPAddress.Broadcast, 12200));//zajisti ze funguje
                    }//Vyprsel timeout
                    try
                    {
                        string m = Communicator.coding.GetString(mess);
                        //Managers.Connections.logger.WriteLine("Recieved broadcast ({0}):{1}",DateTime.Now,m);
                        string[] mS = m.Split(new char[] { ';', '=' });
                        if (mS[0] == "v1.MFO.MFF.works.bubyx" && mS[1] == "id" && mS[3] == "ip" && mS[5] == "port")
                        {
                            ulong deviceId = ulong.Parse(mS[2]);
                            if (deviceId != this.device.Id)
                            {
                                int port = int.Parse(mS[6]);
                                MyDevice device = fileManager.dataHolder.devices.Find((i) => { return i.Id == deviceId; });
                                if (device.Connection ==null ||device.Connection.IsNotConnected) {
                                    string[] ips = mS[4].Split(new char[] { ',' });
                                    for (int i = 0; i < ips.Length; i++)
                                    {
                                        var t1 = TaskTryConnect(device, ips[i], port);//Zkusi navazat spojeni se zarizenim, pokud se zdari tak jej uvede do device
                                        t1.Wait();
                                        try {
                                            if (t1.Result)
                                            {
                                                runCommunication(device);
                                            }
                                        }catch(Exception e)
                                        {
                                            device.Connection.Dispose();
                                            device.Connection = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)//vsechny chyby co se muzou vyskytnout krome nuceneho ukonceni prace
                    {

                    }
                }
            });
            t.Start();
            return t;
        }
        /// <summary>
        /// Do all neseseary thinks to step in running network
        /// </summary>
        /// <returns></returns>
        public async Task ConnectingToNetwork(HomeData data)
        {
            //UdpClient listener = new UdpClient(Properties.Settings.Default.broadcastPort,AddressFamily.InterNetwork);
            UdpClient listener = new UdpClient();
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.EnableBroadcast = true;
            listener.ExclusiveAddressUse = false;
            listener.Client.ReceiveTimeout = 5000;
            listener.Client.Bind(new IPEndPoint(IPAddress.Any, Properties.Settings.Default.broadcastPort));
            while (!shouldEnd.IsCancellationRequested)
            {
                var end = new IPEndPoint(IPAddress.Any, 0);
                byte[] mess = new byte[0];
                try
                {
                    mess = listener.Receive(ref end);
                    string m = Communicator.coding.GetString(mess);
                    string[] mS = m.Split(new char[] { ';', '=' });
                    if (mS[0] == "v1.MFO.MFF.works.bubyx" && mS[1] == "id" && mS[3] == "ip" && mS[5] == "port")
                    {
                        int port = int.Parse(mS[6]);
                        string[] ips = mS[4].Split(new char[] { ',' });
                        for (int i = 0; i < ips.Length; i++)
                        {
                            try
                            {
                                var d = new MyDevice(0, device.Name, Communication.Network, "0", "0", "");
                                if (await TaskTryConnect(d, ips[i], port))//Zkusi navazat spojeni se zarizenim, pokud se zdari tak jej uvede do device
                                {
                                    Properties.Settings.Default.localDevId = d.Id;
                                    Properties.Settings.Default.Save();
                                    await downLoadStartFile(d.Connection, data);
                                    shouldEnd = new System.Threading.CancellationToken(true);

                                    Managers.Connections.logger.WriteLine("Connected to network");
                                    break;
                                }
                            }
                            catch (SocketException e) {
                            }
                            catch (FormatException e) {  }
                        }
                    }
                }
                catch(SocketException e)
                {
                    Managers.Connections.logger.WriteLine("Conection failed (will try again): {0} try", e);
                    listener.Send(new byte[] { 0 }, 0, new IPEndPoint(IPAddress.Broadcast, 12200));//Zajisti ze funguje doma
                }
                catch (Exception e)//vsechny chyby co se muzou vyskytnout krome nuceneho ukonceni prace
                {

                    Managers.Connections.logger.WriteLine("Conection failed (fatal): {0}",e);
                    throw;
                }
            }
        }
        /// <summary>
        /// Try to connect to device<paramref name="dev"/>, that should listen on IP adress<paramref name="ip"/> and port<paramref name="port"/>.
        /// </summary>
        /// <returns>True if connection was established</returns>
        internal async Task<Boolean> TaskTryConnect(MyDevice dev, string ip, int port)
        {
            Socket s = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                s.DualMode = true;
                IPAddress IpAd = IPAddress.Parse(ip);
                var endPoint = new IPEndPoint(IpAd, port);
                s.Connect(endPoint);
                var com = new Communicator(s);
                if (dev.Id != 0)
                {
                    com.SendLine(String.Format("connect.v1.MFO.MFF.works.bubyx;id={0};user={1}", this.device.Id, Properties.Settings.Default.UserToken));
                    string line = await com.ReciveLineAsyncWithTimeout();
                    if (line == "connection established")
                    {
                        com.IsInformator = false;

                        dev.Connection = com;
                        com.SendLine("connection started");
                        return true;
                    }
                    else return false;
                }
                else
                {
                    com.SendLine(String.Format("connect.v1.MFO.MFF.works.bubyx;id={0};user={1}", this.device.Name, Properties.Settings.Default.UserToken));
                    string line = await com.ReciveLineAsyncWithTimeout();
                    if (line.StartsWith("connection established;"))
                    {
                        dev.Id = ulong.Parse(line.Substring("connection established;".Length));
                        com.IsInformator = false;
                        dev.Connection = com;
                        com.SendLine("connection started");
                        return true;
                    }
                    else return false;
                }
            }
            catch (ArgumentOutOfRangeException ex){ s.Dispose(); }
            catch (SocketException ex){s.Dispose();}
            catch (NotSupportedException ex){s.Dispose();}
            return false;
        }
        /// <summary>
        /// download files: overview,all device infos, all info files
        /// </summary>
        /// <param name="com">Connection manager, from with it should be downloaded/</param>
        /// <param name="dat">Data in which the dowenloaded information should be loaded.</param>
        internal async Task downLoadStartFile(Communicator com,HomeData dat)
        {
            com.SendLine("getFile;fromHFO=\\OverviewFile.xml");
            var pathToHfo = FileManager.PathToHFOFolder;
            com.ReadFile(pathToHfo + "\\OverviewFile.xml");
            dat.Reload();
            foreach(var d in dat.devices)
            {
                com.SendLine(String.Format("getFile;fromHFO=\\DeviceInfos\\{0}.xml",d.Id));
                com.ReadFile(String.Format("{0}\\DeviceInfos\\{1}.xml",pathToHfo,d.Id));
            }
            dat.Reload();
            List<string> creatingDirs = new List<string>();
            foreach(var d in dat.devices)
            {
                foreach(var di in d.disks)
                {
                    creatingDirs.Add(Directory.CreateDirectory(String.Format("{0}\\FileInfo\\{1}\\File", pathToHfo, di.Id)).FullName);
                    creatingDirs.Add(Directory.CreateDirectory(String.Format("{0}\\FileInfo\\{1}\\Folders", pathToHfo, di.Id)).FullName);
                    creatingDirs.Add(Directory.CreateDirectory(String.Format("{0}\\FileInfo\\{1}\\ItemFiles", pathToHfo, di.Id)).FullName);
                }
            }
            while (creatingDirs.Count > 0)
            {
                List<string> l = new List<string>();
                foreach(var c in creatingDirs)
                {
                    if (!Directory.Exists(c)) l.Add(c);
                }
                creatingDirs =l;
            }
            foreach(Classes.Interfaces.IInfoGetter i in dat)
            {
                string HFOPath = i.GetInfoFilePath();
                com.SendLine(String.Format("getFile;fromHFO={0}",HFOPath));
                com.ReadFile(pathToHfo+HFOPath);
            }
        }

        /// <summary>
        /// Accept connection from other applications, and check if they are valid.
        /// </summary>
        public Task ListenConnections()
        {
            return Task.Run( async() =>
            {
                TcpListener listener = new TcpListener(IPAddress.IPv6Any, 0);
                listener.Server.DualMode = true;
                listener.Start();
                ConnectionPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                while (!this.shouldEnd.IsCancellationRequested)
                {
                    try
                    {
                        Task<Socket>[] tsks = new Task<Socket>[] { listener.AcceptSocketAsync() };
                        int time=Task.WaitAny(tsks, 5000);
                        if (time != -1)
                        {
                            Socket x = tsks[0].Result;
                            Communicator comun = new Communicator(x);
                            try
                            {
                                string line = await comun.ReciveLineAsyncWithTimeout();
                                string[] lSplit = line.Split(new char[] { ';', '=' });
                                
                                if (lSplit[0] == "connect.v1.MFO.MFF.works.bubyx" && lSplit[1] == "id" && lSplit[3] == "user" && lSplit[4] == Properties.Settings.Default.UserToken)
                                {
                                    ulong deviceId;
                                    if(ulong.TryParse(lSplit[2], out deviceId))
                                    {
                                        if (deviceId != device.Id)
                                        {
                                            MyDevice dev = fileManager.dataHolder.devices.Find((i) => { return i.Id == deviceId; });
                                            if (dev != null)//zkontorlovat zda existuje zarizeni s timto nazvem
                                            {
                                                if (dev.Connection == null || dev.Connection.IsNotConnected)
                                                {
                                                    dev.Connection = comun;
                                                    dev.Connection.IsInformator = true;
                                                    dev.Connection.SendLine("connection established");
                                                    string s = await dev.Connection.ReciveLineAsyncWithTimeout();
                                                    if (s != "connection started")
                                                    {
                                                        dev.Connection = null;
                                                        comun.Dispose();
                                                        throw new Exceptions.ProtocolException(String.Format("Unexpected string recived. Expected 'connection started' recieved {0}", s));
                                                    }
                                                    Task t = runCommunication(dev);
                                                    comunications.Add(t);
                                                    t.Start();
                                                }
                                            }
                                            else
                                            {
                                                comun.SendLine("wrong informations");
                                                comun.Dispose();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var dev=fileManager.dataHolder.AddDevice(lSplit[2]);
                                        fileManager.dataHolder.RewriteOverviewFile();
                                        dev.Connection = comun;
                                        comun.IsInformator = true;
                                        comun.SendLine(String.Format("connection established;{0}",dev.Id));
                                        string s = await dev.Connection.ReciveLineAsyncWithTimeout();
                                        if (s != "connection started")
                                        {
                                            fileManager.dataHolder.devices.Remove(dev);
                                            throw new Exceptions.ProtocolException(String.Format("Unexpected string recived. Expected 'connection started' recieved {0}", s));
                                        }
                                        var locDev = device;
                                        locDev.RunOn=synchronization.lastVersion;
                                        locDev.SyncedTo =locDev.RunOn;
                                        dev.RunOn = locDev.RunOn;
                                        dev.SyncedTo = locDev.RunOn;
                                        fileManager.SaveStructure().Wait();
                                        synchronization.AddGeneratedSyncEvent(new SyncEvents.DeviceAddEv(dev));
                                        myForm.Invoke(myForm.invokeTarget);
                                        Task t=runCommunication(dev);
                                        //t.Start();
                                        comunications.Add(t);

                                    }
                                }
                                else
                                {
                                    comun.SendLine("wrong informations");
                                    comun.Dispose();
                                }

                            }
                            catch (SocketException ex) { comun.Dispose(); }
                            catch (IndexOutOfRangeException ex) { comun.Dispose(); }
                        }
                    }
                    catch (Exception ex) { }//Pochyta vsechny vyjimky 
                }
            });
        }
        /// <summary>
        /// Run the comunication between this device and <paramref name="dev"/>.
        /// </summary>
        private async Task runCommunication(MyDevice dev)
        {
            try {
                Communicator com = dev.Connection;
                while (!shouldEnd.IsCancellationRequested)
                {
                    if (com.IsInformator)//answer request
                    {
                        #region
                        try
                        {
                            string r = com.ReciveLine();

                            string[] sr = r.Split(new char[] { ';' });
                            string[] ss;
                            switch (sr[0])
                            {
                                case "getNextSyncFile":
                                    ss = sr[1].Split(new char[] { '=' }, 2);
                                    if (ss[0] == "version")
                                    {
                                        dev.RunOn = ss[1];
                                        FileInfo file = synchronization.GetNextSyncFile(ss[1]);
                                        if (file != null)
                                        {
                                            com.SendFile(file);
                                        }
                                        else
                                        {
                                            com.SendLine("fullySync");
                                        }
                                    }
                                    else throw new Exceptions.ProtocolException(String.Format("Unsoported command: {0}", r));
                                    break;
                                case "getFile":
                                    ss = sr[1].Split(new char[] { '=' }, 2);
                                    switch (ss[0])
                                    {
                                        case "fromHFO":
                                            com.SendFile(new FileInfo(Managers.FileManager.PathToHFOFolder + ss[1]));
                                            break;
                                        case "fromTree":
                                            string[] ss2 = ss[1].Split(new char[] { '\\' }, 3);
                                            if (device.Id == ulong.Parse(ss2[0]))
                                            {
                                                var disk = device.disks.Find((x) => { return x.Id == ulong.Parse(ss2[1]); });
                                                MyFile file = disk.GetFile(ss2[2]);
                                                com.SendFile(new FileInfo(disk.GetPath() + file.FilePath));
                                            }
                                            else com.SendLine("fileNotFound");
                                            break;
                                        default: throw new Exceptions.ProtocolException(String.Format("Unsoported command: {0}", r));
                                    }
                                    break;
                                case "switchRole":
                                    com.IsInformator = false;
                                    break;
                                default: throw new Exceptions.ProtocolException(String.Format("Unsoported command: {0}", r));
                            }
                        }
                        catch (IndexOutOfRangeException exc) { }
                        #endregion
                    }
                    else //send request
                    {
                        if (com.QueadReques())
                        {
                            await com.DequeueRequest();
                        }
                        else
                        {
                            com.SendLine(String.Format("getNextSyncFile;version={0}", dev.SyncedTo));
                            if (com.ReadSyncFile(FileManager.PathToHFOFolder + "\\SyncFiles\\" + dev.Id + ".tosync"))
                            {
                                StreamWriter failWriter;
                                if (File.Exists(FileManager.PathToHFOFolder + "\\SyncFiles\\" + dev.Id + ".failsync"))
                                {
                                    failWriter = new StreamWriter(new FileStream(FileManager.PathToHFOFolder + "\\SyncFiles\\" + dev.Id + ".failsync", FileMode.Open));
                                    try {
                                        ((FileStream)failWriter.BaseStream).Seek(0 - "</synchronization>".Length, SeekOrigin.End);
                                    } catch (Exception e)
                                    {
                                        ((FileStream)failWriter.BaseStream).Seek(0, SeekOrigin.Begin);
                                        failWriter.Write("<synchronization>");
                                    }
                                }
                                else
                                {
                                    failWriter = new StreamWriter(new FileStream(FileManager.PathToHFOFolder + "\\SyncFiles\\" + dev.Id + ".failsync", FileMode.CreateNew));
                                    failWriter.Write("<synchronization>");
                                }
                                var w = new StreamReader(new FileStream(FileManager.PathToHFOFolder + "\\SyncFiles\\" + dev.Id + ".tosync", FileMode.Open));
                                try
                                {
                                    var verEvents = await XMLProcessors.XmlSyncFile.readSyncFile(w);
                                    var events = verEvents.Value;
                                    dev.SyncedTo = verEvents.Key;
                                    foreach (var e in events)
                                    {
                                        try
                                        {
                                            logger.WriteLine("Do event:", e);
                                            e.DoEvent(fileManager);
                                        }
                                        catch (Exception ex)
                                        {
                                            failWriter.Write(e.GetXml());
                                        }
                                    }
                                }
                                finally
                                {
                                    w.Dispose();
                                    failWriter.Write("</synchronization>");
                                    failWriter.Close();
                                }
                            }
                            else
                            {
                                await Task.Delay(1000);
                                com.SendLine("switchRole");
                                com.IsInformator = true;
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                logger.WriteLine("Leave with exception:{0}", e.Data);
                throw;
            }
            finally
            {
                logger.WriteLine("Leaved runconnection");
            }
        }

        public Task Dispose()
        {
            return Task.Run(() =>
            {
                SetClose();
                return Task.WhenAll(comunications.ToArray());
            });
        }
    }
}

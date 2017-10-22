using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System;

namespace HomeFileOrganizer.Classes
{
    public class Communicator:IDisposable
    {
        public static Encoding coding = new UTF32Encoding(true,false);
        private Socket ConncetionSocket;
        /// <summary>
        /// Determine, if this device is in informator role in this connection.
        /// </summary>
        public bool IsInformator = false;
        private StreamWriter writer;
     //   private StreamReader reader;
        /// <summary>
        /// Queue of request, that will be send as soon as possible by task running <see cref="HomeFileOrganizer.Managers.Connections.runCommunication(MyDevice)"/>
        /// </summary>
        private System.Collections.Concurrent.ConcurrentQueue<Task> requests = new System.Collections.Concurrent.ConcurrentQueue<Task>();
        private byte[] buffer;
        private object readLock = new object();
        private NetworkStream netStream;

        /// <summary>
        /// Determine if underlinig socket is connected.
        /// </summary>
        public bool IsNotConnected { get { return !ConncetionSocket.Connected; } }
        /// <summary>
        /// Create instance for comunication with device on the other side of <paramref name="x"/>.
        /// </summary>
        public Communicator(Socket x)
        {
            ConncetionSocket = x;
            netStream = new NetworkStream(x,false);
            writer = new StreamWriter(netStream, coding);
            writer.AutoFlush = true;
        }
        /// <summary>
        /// Send line to other side of connection.
        /// </summary>
        /// <param name="v"></param>
        internal void SendLine(string v)
        {
            Managers.Connections.logger.WriteLine("Send line {0}",v);
            writer.WriteLine(v);
        }
        /// <summary>
        /// Recive line from connection in 1 sec, or exception is thrown.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">Occure when timelimit reached.</exception>
        internal Task<String> ReciveLineAsyncWithTimeout()
        {
            return ReciveLineAsyncWithTimeout(1000);
        }
        private string ReadLine()
        {
            return ReadLine(-1);
        }
        private string ReadLine(int milSecTimeout)
        {
            Managers.Connections.logger.WriteLine("Start read line.");
            lock (readLock)
            {
                StringBuilder toReturn = new StringBuilder();
                while (true)
                {
                    byte[] buff;
                    if (buffer != null)
                    {
                        buff = buffer;
                        buffer = null;
                    }
                    else
                    {
                        buff = new byte[512];
                        Task<int> readAsync=netStream.ReadAsync(buff, 0, buff.Length);
                        if (readAsync.Wait(milSecTimeout))
                        {
                            var b = buff;
                            buff= new byte[readAsync.Result];
                            for(int i = 0; i < readAsync.Result; i++)
                            {
                                buff[i] = b[i];
                            }
                        }
                        else throw new TimeoutException();
                    }
                    string data = coding.GetString(buff);
                    int endLine = data.IndexOf(Environment.NewLine);
                    if (endLine == -1) toReturn.Append(data);
                    else
                    {
                        toReturn.Append(data.Substring(0, endLine));
                        if(data.Length>endLine+1)buffer = coding.GetBytes(data.Substring(endLine +Environment.NewLine.Length));
                        return toReturn.ToString();
                    }
                }
            }
        }
        /// <summary>
        /// Read bytes from socket. In
        /// </summary>
        /// <param name="maxBytes"></param>
        /// <param name="milSecTimeout"></param>
        /// 
        /// <returns></returns>
        /// <exception cref="TimeoutException">if timeout reached</exception>
        private byte[] ReadBytes(int maxBytes,int milSecTimeout)
        {
            lock (readLock) {
                byte[] toRet;
                if (buffer != null)
                {
                    if (buffer.Length > maxBytes)
                    {
                        int i = 0;
                        toRet = new byte[maxBytes];
                        for (; i < maxBytes; i++)
                        {
                            toRet[i] = buffer[i];
                        }
                        var b = buffer;
                        buffer = new byte[buffer.Length - maxBytes];
                        for (; i < buffer.Length; i++)
                        {
                            buffer[i - maxBytes] = b[i];
                        }
                        return toRet;
                    }
                    else
                    {
                        toRet = buffer;
                        buffer = null;
                        return toRet;
                    }
                }
                else
                {
                    toRet = new byte[maxBytes];
                    Task<int> readTask = netStream.ReadAsync(toRet, 0, maxBytes);
                    if (readTask.Wait(milSecTimeout))
                    {
                        int readed = readTask.Result;
                        if (readed < maxBytes)
                        {
                            var b = toRet;
                            toRet = new byte[readed];
                            for (int i = 0; i < readed; i++)
                            {
                                toRet[i] = b[i];
                            }
                            return toRet;
                        }
                        else
                        {
                            return toRet;
                        }
                    }
                    else
                    {
                        throw new TimeoutException();
                    }

                }
            }
        }
        /// <summary>
        /// Recive line from connection in limited time by <paramref name="milsec"/>.
        /// </summary>
        /// <returns>Line</returns>
        /// <exception cref="IndexOutOfRangeException">Occure if timelimit was reached.</exception>
        internal Task<String> ReciveLineAsyncWithTimeout(int milsec)
        {
            //TODO:delete
            //milsec = -1;
            try {
                string line =ReadLine(milsec);
                Managers.Connections.logger.WriteLine("Recived line: {0}", line);
                return Task.FromResult<String>(line);
            }
            catch(TimeoutException e)
            {
                throw new IndexOutOfRangeException("",e);
            }
        }
        /// <summary>
        /// Recive line from connection.
        /// </summary>
        /// <returns></returns>
        internal Task<String> ReciveLineAsync()
        {
            return Task.FromResult<string>(ReadLine());
        }
        /// <summary>
        /// Close all resources.
        /// </summary>
        public void Dispose()
        {
            try { writer.Dispose(); }
            finally { }
            try { netStream.Dispose(); }
            finally { }
            try { ConncetionSocket.Dispose(); }
            finally { }

        }
        /// <summary>
        /// Send file to the other side of connection, with application protocol. (If some problem with file occures, than "fileError" is sended)
        /// </summary>
        /// <param name="file">File to send</param>
        internal void SendFile(FileInfo file)
        {

            try {
                Managers.Connections.logger.WriteLine("Sending file {0}", file.FullName);
                BinaryReader fr;
                try {
                     fr= new BinaryReader(file.OpenRead());
                }catch(Exception e)
                {
                    SendLine("fileError");
                    return;
                }
                long length = fr.BaseStream.Length;
                SendLine(String.Format("fileStart;{0}", length));
                BinaryWriter fw = new BinaryWriter(writer.BaseStream,Encoding.Default, true);
                try
                {
                    byte[] buff = new byte[1024 * 512];
                    int i = 0;
                    while (true)
                    {
                        i = fr.Read(buff, 0, 1024 * 512);
                        if (i > 0)
                        {
                            length -= i;
                            fw.Write(buff, 0, i);
                            fw.Flush();
                        }
                        else if (length > 0)
                        {
                            byte[] nuls = new byte[length];
                            while (length > 0)
                            {
                                fw.Write(nuls);
                                fw.Flush();
                            }
                            break;
                        }
                        else break;
                    }
                }
                finally
                {
                    fw.Dispose();
                    fr.Dispose();
                }
            }catch(Exception e)
            {
                Managers.Connections.logger.WriteLine("Sending file error{0}", e);
                Dispose();
                throw;
            }
            Managers.Connections.logger.WriteLine("File sended {0}",file.FullName);
        }
        private void ReadBytesToFile(string filePath,long lenght)
        {
            BinaryWriter bw = new BinaryWriter(new FileStream(filePath, FileMode.Create));
            try
            {
                byte[] buff = new byte[1024 * 512];
                while (lenght > 0)
                {
                    if (lenght > 1024 * 512)
                        buff = ReadBytes(1024 * 512, 1000);
                    else
                    {
                        buff = ReadBytes(Convert.ToInt32(lenght), 1000);
                    }
                    if (lenght > 0)
                    {

                        bw.Write(buff, 0, buff.Length);
                        lenght = lenght - buff.Length;
                    }
                    else throw new EndOfStreamException("File ended before expected.");
                }
            }
            finally
            {
                bw.Dispose();
            }
        }
        /// <summary>
        /// Read file sended in stream, and rewrite with it old file
        /// </summary>
        /// <exception cref="Exceptions.ProtocolException">If wrong protocol used.</exception>
        /// <exception cref="FileNotFoundException">If asked device doesnt have this file</exception>
        /// <exception cref="EndOfStreamException">If failed to read full file</exception>
        /// <exception cref="TimeoutException">If waited to long for data</exception>
        internal void ReadFile(string filePath)
        {
            string s = ReciveLineAsync().Result;
            string[] ss = s.Split(new char[] { ';' });
            switch (ss[0]) {
                case "fileStart":
                    try
                    {
                        ReadBytesToFile(filePath, long.Parse(ss[1]));
                    }
                    catch (FormatException ex)
                    {
                        throw new Exceptions.ProtocolException(ex);
                    }
                   break;
                case "fileNotFound":
                    throw new FileNotFoundException();
                default:
                    throw new Exceptions.ProtocolException(String.Format("Unexpected line:{0}", s));
            }
        }
        /// <summary>
        /// Read file sended in stream, and rewrite with it old file
        /// </summary>
        /// <exception cref="Exceptions.ProtocolException">If wrong protocol used.</exception>
        /// <exception cref="FileNotFoundException">If asked device doesnt have this file</exception>
        internal bool ReadSyncFile(string filePath)
        {
            string s = ReciveLineAsyncWithTimeout().Result;
            string[] ss = s.Split(new char[] { ';' });
            switch (ss[0])
            {
                case "fileStart":
                    try
                    {
                        ReadBytesToFile(filePath, long.Parse(ss[1]));
                        return true;
                    }
                    catch (FormatException ex)
                    {
                        throw new Exceptions.ProtocolException(ex);
                    }
                case "fullySync":
                    return false;
                default:
                    throw new Exceptions.ProtocolException(String.Format("Unexpected line:{0}", s));
            }
        }
        /// <summary>
        /// Send and wait for request that was pushed to the <see cref="Communicator.requests"/> 
        /// </summary>
        /// <returns></returns>
        internal async Task DequeueRequest()
        {
            Task t;
            if(requests.TryDequeue(out t))
            {
                t.Start();
                await t;
            }
        }

        internal bool QueadReques()
        {
            return requests.Count > 0;
        }
        /// <summary>
        /// Downloade file from device.
        /// </summary>
        /// <param name="FromPath">Path of file on connected device. If <paramref name="IsHFORelative"/>=true then the path start with FileManager.PathToHFO string on connected device, otherwise the path is in program tree format ({deviceId}\{diskId}\{path relative to root folder on specified disk})</param>
        /// <param name="ToHFOPath">Target where the file will be saved</param>
        /// <param name="IsHFORelative">If true than FromPath is relative to HFO folder, else FromPath is systemTreePath</param>
        /// <returns>Task with state WaitingToRun</returns>
        public Task EnqueueRequestDownload(string FromPath,string ToFullPath,bool IsHFORelative)
        {
            var t = new Task(() => {
                if (IsHFORelative) SendLine(String.Format("getFile;fromHFO={0}", FromPath));
                else SendLine(string.Format("getFile;fromTree={0}", FromPath));
                ReadFile(ToFullPath);
            });
            this.requests.Enqueue(t);
            return t;
        }
        /// <summary>
        /// Recive line from device on the other side of connection.
        /// </summary>
        /// <returns>String in line</returns>
        internal string ReciveLine()
        {
            return ReadLine();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using HomeFileOrganizer.XMLProcessors;
using HomeFileOrganizer.Managers.SyncEvents;
using System.Threading;

namespace HomeFileOrganizer.Managers
{
    public class Synchronization
    {
        /// <summary>
        /// Count of events in eventsToWrite, that will invoke their write to sync file.
        /// </summary>
        private static int MaxEventsCount=50;
        /// <summary>
        /// !!Is also locked by this.semLock
        /// </summary>
        private ConcurrentBag<SyncEvents.Events> eventsToWrite = new ConcurrentBag<SyncEvents.Events>();
        /// <summary>
        /// Thread is in readmode, if is trying modifi eventsToWrite collection.
        /// Thread is in write mode, if is trying to replace whole eventsToWrite collection with new empty instance.
        /// </summary>
        private ReaderWriterLockSlim semLock = new ReaderWriterLockSlim();
        private ConcurrentQueue<string> filesOrder = new ConcurrentQueue<string>();
        /// <summary>
        /// Task that is waiting at semLock for write sync file
        /// </summary>
        private Task syncWriter=null;
        /// <summary>
        /// Name of last file created by sync manager.
        /// </summary>
        public string lastVersion { get;private set; }
        private object lastVersionLock = new object();
        /// <summary>
        /// Prida do lokanich zmen novy event
        /// </summary>
        /// <param name="e"></param>
        public Synchronization()
        {
            readSyncOrder();
        }
        /// <summary>
        /// Add event<paramref name="e"/> that was created by user using this device to sync manager.
        /// </summary>
        public void AddGeneratedSyncEvent( Events e ){
            semLock.EnterReadLock();
            eventsToWrite.Add(e);
            if (eventsToWrite.Count > MaxEventsCount)
            {
                Task x=Interlocked.CompareExchange<Task>(ref syncWriter, WriteSyncFile(), (Task)null);
                if(x==null)syncWriter.Start();//Jedine jak se muze zmenit, je tak ze predtim byl syncWriter==null, tudiz takhle vim ze jsem pridal novy task.
            }
            semLock.ExitReadLock();
        }
        /// <summary>
        /// Create new sync file contataining, latest events.
        /// </summary>
        /// <returns>Running task which will be done when writing is finished</returns>
        public Task WriteSyncFile(){
            return new Task(() =>
            {
                semLock.EnterWriteLock();
                    ConcurrentBag<Events> bag = eventsToWrite;
                eventsToWrite = new ConcurrentBag<Events>();
                if (bag.Count > 0)
                {
                    FileInfo syncFile = FileManager.CreateNewFile(new DirectoryInfo(FileManager.PathToHFOFolder + "\\SyncFiles"), ".sync");
                    semLock.ExitWriteLock();
                    lock (lastVersionLock)
                    {
                        filesOrder.Enqueue(syncFile.Name);
                        lastVersion = syncFile.Name;
                    }
                    StreamWriter w = new StreamWriter(syncFile.OpenWrite());
                    try
                    {
                        w.WriteLine(XmlCreator.XmlIntro);
                        w.WriteLine("<synchronization>");
                        w.WriteLine("<versionStamp id=\"{0}\"/>", syncFile.Name);
                        foreach (var e in bag)
                        {
                            w.WriteLine(XmlSyncFile.writeEvent(e));
                        }
                        w.Write("</synchronization>");
                    }
                    finally
                    {
                        w.Dispose();
                        syncWriter = null;
                    }
                }
                else semLock.ExitWriteLock();
            });
        }
        /// <summary>
        /// Get sync file, htat follows after the <paramref name="v"/> file. If no such file, than return null. If <paramref name="v"/> is empty string, than retunt the first sync file.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        internal FileInfo GetNextSyncFile(string v)
        {
            if (v == "")
            {
                string ver;
                while (!filesOrder.TryPeek(out ver))
                {
                    Thread.Sleep(1000);
                }
                return new FileInfo(FileManager.PathToHFOFolder + "\\SyncFiles\\" + ver);
            }
            else
            {
                var e = filesOrder.GetEnumerator();
                while (e.MoveNext())
                {
                    if (e.Current == v) break;
                }
                if (e.MoveNext()) return new FileInfo(FileManager.PathToHFOFolder + "\\SyncFiles\\" + e.Current);
                else//Neni co synchronizovat
                {
                    if (this.eventsToWrite.Count > 0)
                    {
                        Task x = Interlocked.CompareExchange<Task>(ref syncWriter, WriteSyncFile(), (Task)null);
                        if (x == null) syncWriter.Start();//Jedine jak se muze zmenit, je tak ze predtim byl syncWriter==null, tudiz takhle vim ze jsem pridal novy task.
                    }
                    return null;
                }
            }
        }
        

        private void readSyncOrder()
        {
            FileInfo file;
            try {
                file = new FileInfo(FileManager.PathToHFOFolder + "\\SyncFiles\\order.txt");
            }catch(DirectoryNotFoundException ex)
            {

                var dir = Directory.CreateDirectory(FileManager.PathToHFOFolder + "\\SyncFiles");
                while (!dir.Exists) { Thread.Sleep(100); }
                file = new FileInfo(FileManager.PathToHFOFolder + "\\SyncFiles\\order.txt");
            }
            StreamReader r = new StreamReader(file.Open(FileMode.OpenOrCreate));
            try
            {
                string line = r.ReadLine();
                while (line != null)
                {
                    filesOrder.Enqueue(line);
                    line=r.ReadLine();
                }
            }
            finally
            {
                r.Dispose();
            }
        }
        /// <summary>
        /// Finish work of sync manager for secure dipose of instance.
        /// Do not add new events to sync manager, if this method is called.
        /// </summary>
        public Task<string> Dispose()
        {
            return Task.Run(() =>
            {
                Task t = WriteSyncFile();
                Task x = Interlocked.CompareExchange<Task>(ref syncWriter, t, (Task)null);
                Task[] tar;
                if (x == null)
                {
                    tar = new Task[] { syncWriter };
                    syncWriter.Start();//Jedine jak se muze zmenit, je tak ze predtim byl syncWriter==null, tudiz takhle vim ze jsem pridal novy task.
                }
                else
                {
                    tar = new Task[] { x, t };
                    t.Start();

                }
                StreamWriter w = new StreamWriter(FileManager.PathToHFOFolder + "\\SyncFiles\\order.txt");
                try
                {
                    string lastVesion = "";
                    while (!filesOrder.IsEmpty)
                    {
                        string s;
                        if (filesOrder.TryDequeue(out s))
                        {
                            lastVesion = s;
                            w.WriteLine(s);
                        }
                    }
                    Task.WaitAll(tar);
                    while (!filesOrder.IsEmpty)
                    {
                        string s;
                        if (filesOrder.TryDequeue(out s))
                        {
                            lastVesion = s;
                            w.WriteLine(s);
                        }
                    }
                    return lastVesion;
                }
                finally
                {
                    w.Dispose();
                }
            });
        }
    }
}

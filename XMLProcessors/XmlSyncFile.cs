using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HomeFileOrganizer.Managers.SyncEvents;
using System.Globalization;

namespace HomeFileOrganizer.XMLProcessors
{
    public class XmlSyncFile
    {
        /// <summary>
        /// read events from sync file
        /// </summary>
        /// <param name="fileFullLocalPath"></param>
        /// <returns>key value pair ther key is version of sync file and value is list of events</returns>
        public static async Task<KeyValuePair<string,List<Events>>> readSyncFile(System.IO.TextReader fileFullLocalPath)
        {
            XmlReader reader = XmlTextReader.Create(fileFullLocalPath, new XmlReaderSettings() { Async = true });
            try
            {
                string version = "";
                List<Events> list = new List<Events>();
                FilesEvents fileEvent = null;
                ConfirmEvents confirmEvent = null;
                InfoChangeEvents infoEvent = null;
                InfoChangeValueEv cve = null;
                while (await reader.ReadAsync())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "versionStamp":
                                    version = reader.GetAttribute("id");
                                    break;
                                case FilesEvents.Xml:
                                    fileEvent = new FilesEvents(reader);
                                    break;
                                //file commands start
                                case FileAddEv.Xml:
                                    list.Add(new FileAddEv(fileEvent, reader));
                                    break;
                                case FileMoveEv.Xml:
                                    list.Add(new FileMoveEv(fileEvent,reader));
                                    break;
                                case FileRemoveEvent.Xml:
                                    list.Add(new FileRemoveEvent(fileEvent, reader));
                                    break;
                                case FileDeleteEv.Xml:
                                    list.Add(new FileDeleteEv(fileEvent,reader));
                                    break;
                                //file commands end

                                case ConfirmEvents.Xml:
                                    confirmEvent = new ConfirmEvents(reader);
                                    break;
                                //confirm command start
                                case FileMovedEv.Xml:
                                    list.Add(new FileMovedEv(confirmEvent,reader));
                                    break;
                                //confirm command end

                                case InfoChangeEvents.Xml:
                                    infoEvent = new InfoChangeEvents(reader);
                                    break;
                                //info changes start
                                case InfoChangeValueEv.Xml:
                                    cve = new InfoChangeValueEv("", reader, infoEvent);
                                    break;
                                //info changes end

                                case DeviceAddEv.Xml:
                                    list.Add(new DeviceAddEv(reader));
                                    break;
                                case DiskAddEv.Xml:
                                    list.Add(new DiskAddEv(reader));
                                    break;
                            }
                            break;
                        case XmlNodeType.EndElement:
                            switch (reader.Name)
                            {
                                case FilesEvents.Xml:
                                    fileEvent = null;
                                    break;
                                case ConfirmEvents.Xml:
                                    confirmEvent = null;
                                    break;
                                case InfoChangeEvents.Xml:
                                    infoEvent = null;
                                    break;
                                case InfoChangeValueEv.Xml:
                                    cve = null;
                                    break;
                            }
                            break;
                        case XmlNodeType.CDATA:
                            if (cve != null) list.Add(new InfoChangeValueEv(reader.Value, cve));
                            break;
                    }
                }
                return new KeyValuePair<string,List<Events>>(version,list);
            }
            finally { reader.Dispose(); }
        }

        public static string writeEvent(Events e)
        {
            return e.GetXml();
        }
    }   
}

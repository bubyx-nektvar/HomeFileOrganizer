using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HomeFileOrganizer.Classes;

namespace HomeFileOrganizer.XMLProcessors
{
    class XmlReaders
    {
        /// <summary>
        /// Read information about file to instance of MyFileInfo
        /// </summary>
        /// <param name="filePath">file with information about file (full path)</param>
        /// <param name="diskId">id of this where is this file</param>
        /// <returns>MyFileInfo with readet information, or null if file does not exists</returns>
        public static async Task<MyInfoFile> readFileInfo(string filePath,ulong diskId){
            System.IO.FileInfo ff = new System.IO.FileInfo(filePath);
            Item inItem = null;
            if (ff.Exists)
            {
                MyInfoFile file = new MyInfoFile();
                var hideenStream=new System.IO.StreamReader(filePath);
                XmlReader reader = XmlTextReader.Create(hideenStream, new XmlReaderSettings() { Async = true });
                try
                {
                    InfoGroup actualGroup = null;
                    while (await reader.ReadAsync())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                switch (reader.Name)
                                {
                                    case "group":
                                        if (actualGroup != null) actualGroup = actualGroup.AddGroup(reader.GetAttribute("name"));
                                        else actualGroup = file.AddGroup(reader.GetAttribute("name"));

                                        break;
                                    case "value":
                                        inItem = Parsing.ParseItem(reader.GetAttribute("type"), reader.GetAttribute("name"), reader, diskId);
                                        if (actualGroup != null) actualGroup.AddItem(inItem);
                                        else file.AddItem(inItem);
                                        break;
                                }
                                break;
                            case XmlNodeType.EndElement:
                                switch (reader.Name)
                                {
                                    case "group":
                                        actualGroup = actualGroup.Up;
                                        break;
                                    case "value":
                                        inItem = null;
                                        break;
                                }
                                break;
                            case XmlNodeType.Text:
                                if (inItem != null) inItem.SetValue(reader.Value);
                                break;
                            case XmlNodeType.CDATA:
                                if (inItem != null) inItem.SetValue(reader.Value);
                                break;
                        }
                    }
                }
                finally {
                    reader.Dispose();
                    hideenStream.Dispose();
                }
                return file;
            }
            else return null;
        }
        /// <summary>
        /// Read information from device info file to instance of DeviceInfo
        /// </summary>
        /// <exception cref="HomeFileOrganizer.Exceptions.DeviceFileException"></exception>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Task readDeviceFile(string file,MyDevice device)
        {
            return Task.Run(() =>
            {
                MyDisk disk = null;
                var hiddenrStream = new System.IO.FileInfo(Managers.FileManager.PathToHFOFolder + "\\DeviceInfos\\" + file).Open(System.IO.FileMode.Open);
                XmlReader reader = XmlTextReader.Create(hiddenrStream, new XmlReaderSettings() { Async = true });
                try
                {
                    Stack<MyFolder> folderStack = new Stack<MyFolder>();
                    while (reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                switch (reader.Name)
                                {
                                    case "disk":
                                        disk = device.disks.Find(i => i.Id == UInt64.Parse(reader.GetAttribute("id")));
                                        if (disk == null) throw new Exceptions.DeviceFileException(String.Format("Not compatible overview file and device file {0}. Id {1} not found in overview file.", file, reader.GetAttribute("id")));
                                        break;
                                    case "file":
                                        MyFile actFile;
                                        if (folderStack.Count != 0) actFile = new MyFile(folderStack.Peek());
                                        else actFile = new MyFile(new MyRootFolder("", disk, null));
                                        String infoFileName = reader.GetAttribute("infoFile");
                                        if (infoFileName != null) actFile.InfoFilePath = String.Format("\\FileInfo\\{0}\\File\\{1}", disk.Id, infoFileName);
                                        actFile.Category = selectCategory(reader.GetAttribute("category"));
                                        if (folderStack.Count == 0)
                                        {
                                            actFile.FilePath = reader.GetAttribute("path");
                                            disk.files.Add(actFile);
                                        }
                                        else
                                        {
                                            actFile.FilePath = folderStack.Peek().Path + reader.GetAttribute("path");
                                            folderStack.Peek().files.Add(actFile);
                                        }
                                        break;
                                    case "folder":
                                        MyFolder actFolder = null;
                                        if (folderStack.Count == 0)
                                        {
                                            actFolder = new MyRootFolder(reader.GetAttribute("path"), disk, selectCategory(reader.GetAttribute("category")));
                                            disk.folders.Add((MyRootFolder)actFolder);
                                        }
                                        else
                                        {
                                            actFolder = new MyFolder(reader.GetAttribute("path"));
                                            actFolder.UpperFolder = folderStack.Peek();
                                            folderStack.Peek().folders.Add(actFolder);
                                        }
                                        String infoFilePath2 = reader.GetAttribute("infoFile");
                                        if (infoFilePath2 != null) actFolder.InfoFilePath = String.Format("\\FileInfo\\{0}\\Folder\\{1}", disk.Id, infoFilePath2);
                                        folderStack.Push(actFolder);
                                        break;
                                }
                                break;
                            case XmlNodeType.EndElement:
                                switch (reader.Name)
                                {
                                    case "folder":
                                        folderStack.Pop();
                                        break;
                                }
                                break;
                        }
                    }
                }
                finally
                {
                    reader.Dispose();
                    hiddenrStream.Dispose();
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file">file path, only file name</param>
        /// <returns></returns>
        public static List<MyDevice> readOverview(string file)
        {
            List<MyDevice> dil = new List<MyDevice>();
            XmlTextReader reader = new XmlTextReader(Managers.FileManager.PathToHFOFolder +"\\"+ file);
            try
            {
                MyDevice computer = null;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "disk":
                                    if (computer != null)
                                    {
                                        MyDisk disk = new MyDisk(ulong.Parse(reader.GetAttribute("deviceId")), reader.GetAttribute("name"));
                                        computer.disks.Add(disk);
                                    }
                                    else
                                    {
                                        MyDevice device = new MyDevice(ulong.Parse(reader.GetAttribute("deviceId")), reader.GetAttribute("name"), Communication.PeriferDisk, reader.GetAttribute("syncedTo"), reader.GetAttribute("runOn"), reader.GetAttribute("deviceFile"));
                                        MyDisk disk = new MyDisk(device.Id,device.Name);
                                        device.disks.Add(disk);
                                        dil.Add(device);
                                    }
                                    break;
                                case "computer":
                                    computer = new MyDevice(ulong.Parse(reader.GetAttribute("deviceId")), reader.GetAttribute("name"), Communication.Network, reader.GetAttribute("syncedTo"),reader.GetAttribute("runOn"), reader.GetAttribute("deviceFile"));
                                    dil.Add(computer);
                                    break;
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "computer") computer = null;
                            break;
                    }
                }
            }
            finally { reader.Dispose(); }
            return dil;
        }
        /// <summary>
        /// Select category from categories, if present.
        /// </summary>
        /// <returns></returns>
        private static Category selectCategory(string cat)
        {
            if (cat == null) return null;
            Category c = Category.GetCategory(cat);
            if (c == null)
            {
                throw new Exceptions.CategoryException(String.Format("Category {0} not found in category file.",cat));
            }
            return c;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateFile"></param>
        /// <param name="file"></param>
        /// <param name="diskId">realative path to files ItemFiles folder</param>
        public static void readTemplate(string templateFile,MyInfoFile file,UInt64 diskId)
        {
            XmlTextReader reader = new XmlTextReader(Managers.FileManager.PathToHFOFolder+"\\FITemplates\\" + templateFile);
            try
            {
                InfoGroup actualGroup = null;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "group":
                                    if (actualGroup != null) actualGroup = actualGroup.AddGroup(reader.GetAttribute("name"));
                                    else actualGroup = file.AddGroup(reader.GetAttribute("name"));
                                    break;
                                case "value":
                                    Item it = Parsing.ParseItem(reader.GetAttribute("type"), reader.GetAttribute("name"), reader, diskId);
                                    it.SetValue(reader.GetAttribute("default"));
                                    if (actualGroup != null) actualGroup.AddItem(it);
                                    else file.AddItem(it);
                                    break;
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "group") actualGroup = actualGroup.Up;
                            break;
                    }
                }
            }
            finally { reader.Dispose(); }
        }
       
    }
}

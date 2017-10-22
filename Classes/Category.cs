using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HomeFileOrganizer.Classes
{
    public class Category
    {
        private static List<Category> categories = new List<Category>();
        static Category(){
            XmlTextReader reader = new XmlTextReader(Managers.FileManager.PathToHFOFolder + "\\Categories.xml");
            try
            {
                Category acCat = null;
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.Name)
                            {
                                case "category":
                                    acCat = new Category(reader.GetAttribute("name"));
                                    break;
                                case "template":
                                    string tempFile = reader.GetAttribute("temp");
                                    if (Boolean.Parse(reader.GetAttribute("folder"))) acCat.TemplatesForFolder.Add(tempFile);
                                    if (Boolean.Parse(reader.GetAttribute("file"))) acCat.TemplatesForFile.Add(tempFile);
                                    break;
                            }
                            break;
                        case XmlNodeType.EndElement:
                            if (reader.Name == "category")
                            {
                                categories.Add(acCat);
                                acCat = null;
                            }
                            break;
                    }
                }
            }
            finally
            {
                reader.Dispose();
            }
        }
        public static Category GetCategory(string name){
            return categories.Find((i) => { return i.Name == name; });
        }
        public static string[] GetCategories()
        {
            string[] c = new string[categories.Count];
            for(int i = 0; i < c.Length; i++)
            {
                c[i] = categories[i].Name;
            }
            return c;
        }
        /// <summary>
        /// Name of category
        /// </summary>
        public string Name;
        /// <summary>
        /// Name of files, that specifies this category. Name are without system path.
        /// </summary>
        public List<string> TemplatesForFile=new List<string>();
        public List<string> TemplatesForFolder=new List<string>();

        public Category(string name)
        {
            Name = name;
        }
        /// <summary>
        /// Create MyInfoFile instance for category
        /// </summary>
        /// <param name="diskId"></param>
        /// <returns></returns>
        public MyInfoFile GetMyFileInfoForFile(ulong diskId)
        {
            MyInfoFile f = new MyInfoFile();
            foreach (var s in TemplatesForFile)
            {
                XMLProcessors.XmlReaders.readTemplate(s, f, diskId);
            }
            XMLProcessors.XmlReaders.readTemplate("System.xml", f, diskId);
            return f;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="diskId"></param>
        /// <returns></returns>
        public MyInfoFile GetMyFileInfoForFolder(ulong diskId)
        {
            MyInfoFile f = new MyInfoFile();
            foreach (var s in TemplatesForFile)
            {
                XMLProcessors.XmlReaders.readTemplate(s, f, diskId);
            }
            return f;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HomeFileOrganizer.Classes.ItemTypes;
namespace HomeFileOrganizer.Classes
{
    public class MyInfoFile
    {
        protected List<Item> items = new List<Item>();
        protected List<InfoGroup> groups=new List<InfoGroup>();
        /// <summary>
        /// Create group inside this file info. If already exists, than return existig group.
        /// </summary>
        /// <param name="name">Group that was added.</param>
        /// <returns></returns>
        public virtual InfoGroup AddGroup(string name)
        {
            InfoGroup g=groups.Find(i=>i.Name==name);
            if (g == null)
            {
                g = new InfoGroup(name,null);
                groups.Add(g);
            }
            return g;
        }
        /// <summary>
        /// Add item inside this file info.
        /// </summary>
        /// <exception cref="ArgumentException">Item already exists</exception>
        /// <param name="i"></param>
        public virtual void AddItem(Item i)
        {
            if (items.Exists(j => j.Name == i.Name)) throw new ArgumentException("Try to add duplicity item");
            else
            {
                items.Add(i);
                i.Up = null;
            }
        }
        /// <summary>
        /// Find value of specific item.
        /// </summary>
        /// <param name="name">Path to item<example>{group g1}\{group g2 inside g1}\{item inside g2} </example></param>
        /// <returns>value of item or "" if not found</returns>
        internal string GetColumn(string name)
        {
            Item item = GetItem(name);
            if (item != null) return item.GetValueString();
            else return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>null if not found</returns>
        internal Item GetItem(string name)
        {

            string[] path = name.Split(new char[] { '\\' });
            if (path.Length > 1)
            {
                int i = 0;
                InfoGroup group = groups.Find(it => it.Name == path[i]);
                i++;
                while (i < path.Length - 1)
                {
                    if (group != null) group = groups.Find(it => it.Name == path[i]);
                    else return null;
                    i++;
                }
                if (group != null)
                {
                    return group.items.Find(it => it.Name == path[i]);
                }
                else return null;
            }
            else
            {
                return items.Find(it => it.Name == path[0]);
            }
        }

        internal void CopyFrom(MyInfoFile myInfoFile)
        {
            foreach(var i in myInfoFile.items)
            {
                items.Add(i.Copy());
            }
            foreach(var g in myInfoFile.groups)
            {
                groups.Add(g.Copy());
            }
        }

        internal async Task<Control[]> GetGUIContent(ContextMenuStrip menu)
        {
            if(items.Count>0){
                Control[] content=new Control[groups.Count+1];
                Task<TableLayoutPanel> t = GetGUIItems(menu);
                for(int i = 1; i < groups.Count + 1; i++) { 
                    Task<Control[]> crls=groups[i-1].GetGUIContent(menu);
                    GroupBox g = new GroupBox();
                    g.Text = groups[i-1].Name;
                    g.AutoSize = true;
                    content[i] = g;

                    Control[] crls2 = await crls;
                    TableLayoutPanel tab = new TableLayoutPanel();
                    tab.RowCount = crls2.Length;
                    tab.ColumnCount = 1;
                    tab.Dock = DockStyle.Fill;
                    tab.AutoSize = true;
                    for(int j = 0; j < crls2.Length; j++)
                    {
                        tab.Controls.Add(crls2[j], 0, j);
                    }
                    
                    g.Controls.Add(tab);
                }
                content[0]=await t;
                return content;
            }
            else
            {
                Control[] content = new Control[groups.Count];
                for(int i = 0; i < groups.Count; i++) { 
                    Task<Control[]> crls = groups[i].GetGUIContent(menu);
                    GroupBox g = new GroupBox();
                    g.Text = groups[i].Name;
                    g.AutoSize = true;
                    content[i] = g;

                    Control[] crls2 = await crls;
                    TableLayoutPanel tab = new TableLayoutPanel();
                    tab.RowCount = crls2.Length;
                    tab.ColumnCount = 1;
                    tab.Dock = DockStyle.Fill;
                    tab.AutoSize = true;
                    for (int j = 0; j < crls2.Length; j++)
                    {
                        tab.Controls.Add(crls2[j], 0, j);
                    }

                    g.Controls.Add(tab);
                }
                return content;
            }
        }
        
        private Task<TableLayoutPanel> GetGUIItems(ContextMenuStrip menu)
        {
            return Task<TableLayoutPanel>.Run(() =>
            {
                TableLayoutPanel table = new TableLayoutPanel();
                table.RowCount = items.Count;
                table.ColumnCount = 2;
                table.Dock = DockStyle.Fill;
                table.AutoSize = true;
                Object locker = new Object();
                for(int i = 0; i < items.Count; i++) { 
                    Item it = items[i];
                    Label name = new Label();
                    name.Text = it.Name;
                    Control value = it.GetValueControl();
                    value.ContextMenuStrip = menu;
                    lock (locker)
                    {
                        table.Controls.Add(name, 0, i);
                        table.Controls.Add(value, 1, i);
                    }
                }
                return table;
            });
        }

        internal virtual void RewriteFile(System.IO.StreamWriter w)
        {
            w.WriteLine(XMLProcessors.XmlCreator.XmlIntro);
            w.WriteLine("<file>");
            foreach (Item i in items)
            {
                w.WriteLine(i.GetXmlSaveString());
            }
            foreach (InfoGroup g in groups)
            {
                g.RewriteFile(w);
            }
            w.WriteLine("</file>");
        }
    }
    public class InfoGroup:MyInfoFile
    {
        public string Name;
        public InfoGroup Up;
        public InfoGroup(string n,InfoGroup up)
        {
            Name = n;
            Up = up;
        }
        public override string ToString()
        {
            return Name+">>"+base.ToString();
        }
        internal string GetXmlSaveString()
        {
            StringBuilder b = new StringBuilder();
            b.Append("<group name=\"");
            b.Append(Name);
            b.Append("\">");
            foreach (InfoGroup g in groups)
            {
                b.Append(g.GetXmlSaveString());
            }
            foreach (Item i in items)
            {
                b.Append(i.GetXmlSaveString());
            }
            b.Append("</group>");
            return b.ToString();
        }

        internal override void RewriteFile(System.IO.StreamWriter w)
        {
            w.WriteLine("<group name=\"{0}\">",this.Name);
            foreach (Item i in items)
            {
                w.WriteLine(i.GetXmlSaveString());
            }
            foreach (InfoGroup g in groups)
            {
                g.RewriteFile(w);
            }
            w.WriteLine("</group>");
        }
        public override InfoGroup AddGroup(string name)
        {
            InfoGroup g = groups.Find(i => i.Name == name);
            if (g == null)
            {
                g = new InfoGroup(name, this);
                groups.Add(g);
            }
            return g;
        }

        /// <summary>
        /// Add item inside this file info.
        /// </summary>
        /// <exception cref="ArgumentException">Item already exists</exception>
        /// <param name="i"></param>
        public void AddItem(Item i)
        {
            if (items.Exists(j => j.Name == i.Name)) throw new ArgumentException("Try to add duplicity item");
            else
            {
                items.Add(i);
                i.Up = this;
            }
        }

        internal InfoGroup Copy()
        {
            var g = new InfoGroup(Name, null);
            foreach(var i in this.groups)
            {
                g.groups.Add(i.Copy(g));
            }
            foreach(var i in this.items)
            {
                g.items.Add(i.Copy());
            }
            return g;
        }
        internal InfoGroup Copy(InfoGroup g)
        {
            var o = new InfoGroup(Name, g);
            foreach (var i in this.groups) o.groups.Add(i.Copy(o));
            foreach (var i in items) o.items.Add(i.Copy());
            return o;

        }
    }
    public abstract class Item:IComparable<string>
    {
        public string Name;
        public bool IsChanged=false;
        protected DateTime LastChange;
        public InfoGroup Up;

        protected Item(string name)
        {
            this.Name = name;
        }

        public Item(Item i)
        {
            this.Name = i.Name;
            this.IsChanged = i.IsChanged;
            this.LastChange = i.LastChange;
            this.Up = i.Up;
        }

        internal abstract string GetValueString();
        /// <summary>
        /// Return read only control with item value and .Tag setted to this item.
        /// </summary>
        /// <returns></returns>
        internal abstract Control GetValueControl();
        /// <summary>
        /// Return editable control with item value, and .Tag setted to this item.
        /// </summary>
        /// <returns></returns>
        internal abstract Control GetEditableControl();
        /// <summary>
        /// Set value of item.
        /// </summary>
        /// <param name="o">is in invarianCulture format</param>
        internal abstract void SetValue(string o);
        /// <summary>
        /// Get xml string of item ({left angle bracket}value name="this.Name" type="{item type}"{right angle bracket} {this.value}{xml value end}
        /// </summary>
        /// <returns></returns>
        internal abstract string GetXmlSaveString();
        /// <summary>
        /// Determine if item has changed. And set its IsChanged field
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        internal abstract bool HasChanged(Control con);
        /// <summary>
        /// Get string with information about change of value, that is saved to sync file.
        /// The ChangeInfoByString method shoud do same type of change if called with returned string.
        /// Must be called after calling HasChanged
        /// </summary>
        /// <returns></returns>
        internal abstract string GetChangeInfoSyncString();
        /// <summary>
        /// Do change that is described in string. Shoud colerate with string that this change create if called GetChangeInfoSyncString.
        /// </summary>
        /// <param name="changeInfo">is in invarianCulture format</param>
        internal abstract void ChangeInfoBySyncString(string changeInfo,DateTime changeTime);
        /// <summary>
        /// Get path thought start of group to this item and item name.
        /// </summary>
        /// <returns><example>{group.name}/{group.name}/.../{item.name}</example></returns>
        public string GetGroupPath()
        {
            StringBuilder s = new StringBuilder();
            InfoGroup acG=this.Up;
            while(acG!=null){
                s.Insert(0,acG.Name);
                s.Insert(0, '\\');
                acG = acG.Up;
            }
            s.Remove(0, 1);
            s.Append('\\');
            s.Append(Name);
            return s.ToString();
        }

        internal abstract Item Copy();

        public abstract int CompareTo(string other);
    }
    static class Parsing
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="reader"></param>
        /// <param name="diskId"></param>
        /// <returns></returns>
        internal static Item ParseItem(string type,string name, System.Xml.XmlReader reader,UInt64 diskId)
        {
            switch (type)
            {
                case "picture": return new PictureItem(name,diskId);
                case "text": return new TextItem(name);
                case "multiline": return new MultilineItem(name);
                case "number": return new NumberItem(name);
                case "link": return new LinkItem(name,reader.GetAttribute("text"));
                case "date": return new DateItem(name);
                case "byte": return new ByteItem(name);
                case "counter": return new CounterItem(name);
                case "rating":return new Rating(name);
                default: throw new FormatException(String.Format("Format specified in xml file {0} is not supported.",reader));
            }
        }
    }
}

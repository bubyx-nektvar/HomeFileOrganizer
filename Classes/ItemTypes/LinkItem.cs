using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.Classes.ItemTypes
{
    public class LinkItem : Item
    {
        private string text = "";
        private string link="";
        private static string XType = "link";
        public LinkItem(string name, string txt)
            : base(name)
        {
            if(txt!=null)text = txt;
        }
        public LinkItem(LinkItem i):base(i)
        {
            text = i.text;
            link = i.link;
        }

        internal override string GetValueString()
        {
            return link;
        }

        internal override Control GetValueControl()
        {
            LinkLabel l = new LinkLabel();

            l.AutoSize = true;
            l.Text = text;
            l.Links.Add(0, text.Length, link);
            l.Tag = this;
            return l;
        }

        internal override void SetValue(string url)
        {
            if (text == "") text = url;
            link = url;
        }

        internal override Control GetEditableControl()
        {

            TextBox url = new TextBox();
            url.Text = this.link;
            url.Dock = DockStyle.Fill;
            TextBox txt = new TextBox();
            txt.Text = this.text;
            txt.Dock = DockStyle.Fill;

            ToolTip tip = new ToolTip();
            tip.AutoPopDelay = 1000;
            tip.SetToolTip(txt, "Showed text");
            tip.SetToolTip(url, "Destination url/link");

            TableLayoutPanel tab = new TableLayoutPanel();
            tab.ColumnCount = 1;
            tab.RowCount = 2;
            tab.Controls.Add(txt, 0, 0);
            tab.Controls.Add(url, 0, 1);

            tab.Tag = this;
            return tab;
        }
        internal override string GetXmlSaveString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture,"<value name=\"{0}\" type=\"{1}\" text=\"{2}\">{3}</value>", Name, XType, this.text, this.link);
        }

        internal override bool HasChanged(Control con)
        {
            IsChanged = false;
            TableLayoutPanel table= (TableLayoutPanel)con;
            TextBox txt =(TextBox)table.GetControlFromPosition(0, 0);
            TextBox url = (TextBox)table.GetControlFromPosition(0,1);
            if (txt.Text != this.text&&(!txt.Text.Contains('(')))
            {
                text = txt.Text;
                IsChanged = true;
            }
            if (url.Text != this.link)
            {
                link = url.Text;
                IsChanged = true;
            }
            return IsChanged;
        }

        internal override string GetChangeInfoSyncString()
        {
            return String.Format("{0}({1})", this.link, this.text);
        }

        internal override void ChangeInfoBySyncString(string changeInfo,DateTime changeTime)
        {
            if (LastChange < changeTime)
            {
                int i = changeInfo.LastIndexOf('(');
                string url = changeInfo.Substring(i);
                string txt = changeInfo.Substring(i + 1, changeInfo.Length - url.Length - 2);
                link = url;
                text = txt;
                LastChange = changeTime;
            }
        }

        internal override Item Copy()
        {
            return new LinkItem(this);
        }

        public override int CompareTo(string other)
        {
            return text.CompareTo(other);
        }
    }
}

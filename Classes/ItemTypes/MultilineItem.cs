using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.Classes.ItemTypes
{
    public class MultilineItem : Item
    {
        private string text;
        public MultilineItem(string name) : base(name) { }
        public MultilineItem(MultilineItem i) : base(i)
        {
            text = i.text;
        }
        private static string XType = "multiline";
        internal override string GetValueString()
        {
            return text;
        }

        internal override Control GetValueControl()
        {
            TextBox box = new TextBox();
            box.Multiline = true;
            box.Dock = DockStyle.Fill;
            box.Text = text;
            box.MinimumSize = new System.Drawing.Size(200, 100);
            box.ScrollBars = ScrollBars.Vertical;
            box.ReadOnly = true;
            box.Tag = this;
            return box;
        }

        internal override void SetValue(string o)
        {
            text = o;
        }

        internal override Control GetEditableControl()
        {
            TextBox box = new TextBox();
            box.Multiline = true;
            box.Dock = DockStyle.Fill;
            box.MinimumSize = new System.Drawing.Size(200, 100);
            box.ScrollBars = ScrollBars.Vertical;
            box.Text = text;
            box.Tag = this;
            return box;
        }
        internal override string GetXmlSaveString()
        {
            return String.Format("<value name=\"{0}\" type=\"{1}\"><![CDATA[{2}]]></value>", Name, XType, this.text);
        }

        internal override bool HasChanged(Control con)
        {
            IsChanged = false;
            string n = ((TextBox)con).Text;
            if (n != text)
            {
                text = n;
                IsChanged = true;
            }
            return IsChanged;
        }

        internal override string GetChangeInfoSyncString()
        {
            return text;
        }

        internal override void ChangeInfoBySyncString(string changeInfo,DateTime changeTime)
        {
            if (LastChange < changeTime)
            {
                text = changeInfo;
                LastChange = changeTime;
            }
        }

        internal override Item Copy()
        {
            return new MultilineItem(this);
        }

        public override int CompareTo(string other)
        {
            return this.text.CompareTo(other);
        }
    }
}

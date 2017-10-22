using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.Classes.ItemTypes
{
    public class TextItem : Item
    {
        private string text;
        private static string XType = "text";
        public TextItem(string name)
            : base(name)
        {
        }
        public TextItem(TextItem i) : base(i)
        {
            text = i.text;
        }

        internal override string GetValueString()
        {
            return text;
        }

        internal override Control GetValueControl()
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Tag = this;
            return l;
        }

        internal override void SetValue(string o)
        {
            text = o;
        }

        internal override Control GetEditableControl()
        {
            TextBox box = new TextBox();
            box.Text = text;
            box.Tag = this;
            return box;
        }
        internal override string GetXmlSaveString()
        {
            return String.Format("<value name=\"{0}\" type=\"{1}\">{2}</value>", Name, XType, this.text);
        }

        internal override bool HasChanged(Control con)
        {
            IsChanged = false;
            TextBox b = (TextBox)con;
            if (b.Text != text)
            {
                text = b.Text;
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
            return new TextItem(this);
        }

        public override int CompareTo(string other)
        {
            return text.CompareTo(other);
        }
    }
}

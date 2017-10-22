using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.Classes.ItemTypes
{
    public class DateItem : Item
    {
        private DateTime date;
        private static string XType = "date";
        public DateItem(string name) : base(name){ }
        public DateItem(DateItem i) : base(i)
        {
            date = i.date;
        }

        internal override string GetValueString()
        {
            return date.ToString();
        }

        internal override Control GetValueControl()
        {
            Label l = new Label();
            l.AutoSize = true;
            l.Text = date.ToString();
            l.Tag = this;
            return l;
        }

        internal override void SetValue(string o)
        {
            date = DateTime.Parse(o, System.Globalization.CultureInfo.InvariantCulture);
        }

        internal override Control GetEditableControl()
        {
            DateTimePicker d = new DateTimePicker();
            d.Value = date;
            d.Tag = this;
            return d;
        }
        internal override string GetXmlSaveString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "<value name=\"{0}\" type=\"{1}\">{2}</value>", Name, XType, this.date);
        }

        internal override bool HasChanged(Control con)
        {
            IsChanged = false;
            DateTime t=((DateTimePicker)con).Value;
            if (!t.Equals(this.date))
            {
                date = t;
                IsChanged = true;
            }

            return IsChanged;
        }

        internal override string GetChangeInfoSyncString()
        {
            System.Globalization.CultureInfo.InvariantCulture.GetFormat(typeof(DateTime));
            return date.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        internal override void ChangeInfoBySyncString(string changeInfo,DateTime changeTime)
        {
            if (LastChange < changeTime)
            {
                SetValue(changeInfo);
                LastChange = changeTime;
            }
        }

        internal override Item Copy()
        {
            return new DateItem(this);
        }

        public override int CompareTo(string other)
        {
            return this.date.CompareTo(DateTime.Parse(other));
        }
    }
}

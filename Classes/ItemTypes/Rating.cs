using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.Classes.ItemTypes
{
    class Rating : Item
    {
        public string XType = "rating";
        double val = 0;
        public Rating(string name) : base(name) { }
        public Rating(Rating i) : base(i)
        {
            val = i.val;
        }

        public override int CompareTo(string other)
        {
            return val.CompareTo(double.Parse(other));
        }

        internal override void ChangeInfoBySyncString(string changeInfo, DateTime changeTime)
        {
            if (changeTime > this.LastChange)
            {
                val=Double.Parse(changeInfo,System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        internal override Item Copy()
        {
            return new Rating(this);
        }

        internal override string GetChangeInfoSyncString()
        {
            return val.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        internal override Control GetEditableControl()
        {
            NumericUpDown n = new NumericUpDown();
            n.Maximum = 5;
            n.Minimum = 0;
            n.Value = new Decimal(val);
            n.AutoSize = true;
            n.Tag = this;
            return n;
        }

        internal override Control GetValueControl()
        {
            Label l = new Label();
            l.AutoSize = true;
            l.Text = GetValueString();
            l.Tag = this;
            return l;
        }

        internal override string GetValueString()
        {
            return String.Format("{0:2}", val);
        }

        internal override string GetXmlSaveString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "<value name=\"{0}\" type=\"{1}\">{2}</value>",
                Name, XType, this.val);
        }

        internal override bool HasChanged(Control con)
        {
            if(new Decimal(val) != ((NumericUpDown)con).Value)
            {
                return true;
            }
            return false;
        }

        internal override void SetValue(string o)
        {
            val = double.Parse(o,System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

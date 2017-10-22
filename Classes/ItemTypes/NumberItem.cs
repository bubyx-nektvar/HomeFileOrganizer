using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.Classes.ItemTypes
{
    public class NumberItem : Item
    {
        private Decimal number;
        private static string XType = "number";
        public NumberItem(string name)
            : base(name)
        { }
        public NumberItem(NumberItem i) : base(i)
        {
            number = i.number;
        }

        internal override string GetValueString()
        {
            return number.ToString();
        }

        internal override Control GetValueControl()
        {
            Label l = new Label();
            l.AutoSize = true;
            l.Text = number.ToString();
            l.Tag = this;
            return l;
        }

        internal override void SetValue(string o)
        {
            number = Decimal.Parse(o,System.Globalization.CultureInfo.InvariantCulture);
        }

        internal override Control GetEditableControl()
        {
            NumericUpDown box = new NumericUpDown();
            box.Value = number;
            box.Dock = DockStyle.Fill;
            box.AutoSize = true;
            box.Tag = this;
            return box;
        }
        internal override string GetXmlSaveString()
        {
            return String.Format("<value name=\"{0}\" type=\"{1}\">{2}</value>", Name, XType, this.number);
        }

        internal override bool HasChanged(Control con)
        {
            IsChanged = false;
            Decimal d = ((NumericUpDown)con).Value;
            if (d != number)
            {
                number = d;
                IsChanged = true;
            }
            return IsChanged;
        }

        internal override string GetChangeInfoSyncString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", number);
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
            return new NumberItem(this);
        }

        public override int CompareTo(string other)
        {
            return number.CompareTo(decimal.Parse(other));
        }
    }
}

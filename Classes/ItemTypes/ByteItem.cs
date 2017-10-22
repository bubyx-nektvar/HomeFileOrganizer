using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.Classes.ItemTypes
{
    public class ByteItem : Item,IComparable<string>
    {
        /// <summary>
        /// Count of bytes
        /// </summary>
        private ulong bytes;
        private static string XType = "byte";

        public ByteItem(string name): base(name){}

        public ByteItem(ByteItem byteItem):base(byteItem)
        {
            bytes = byteItem.bytes;
        }

        internal override string GetValueString()
        {
            ulong devide = bytes;
            int i = 0;
            while (devide != 0 && i < 5)
            {
                devide = devide / 1024;
                i++;
            }
            i--;
            double f = 0;
            if (i >= 0)
            {
                f = ((double)bytes) / (Math.Pow(1024, i));
            }
            else i = 0;
            return String.Format("{0} {1}", Math.Round(f, 2).ToString(), getUnit(i));

        }
        private static string getUnit(int i)
        {
            switch (i)
            {
                case 0: return "B";
                case 1: return "kB";
                case 2: return "MB";
                case 3: return "GB";
                case 4: return "TB";
                default: return "";
            }
        }

        internal override Control GetValueControl()
        {
            Label l = new Label();
            l.AutoSize = true;
            l.Text = GetValueString();
            l.Tag = this;
            return l;
        }

        internal override void SetValue(string o)
        {
            bytes = ulong.Parse(o, System.Globalization.CultureInfo.InvariantCulture);
        }

        internal override Control GetEditableControl()
        {
            NumericUpDown n = new NumericUpDown();
            n.Maximum = Decimal.MaxValue;
            n.Minimum = 0;
            n.Value = this.bytes;
            n.AutoSize = true;
            n.Tag = this;
            return n;
        }
        internal override string GetXmlSaveString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture,"<value name=\"{0}\" type=\"{1}\">{2}</value>",
                Name, XType, this.bytes);
        }

        internal override string GetChangeInfoSyncString()
        {
            return bytes.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        internal override void ChangeInfoBySyncString(string changeInfo,DateTime changeTime)
        {
            if (LastChange < changeTime)
            {
                SetValue(changeInfo);
                LastChange = changeTime;
            }
        }

        internal override bool HasChanged(Control con)
        {
            IsChanged = false;
            ulong i=Decimal.ToUInt64(((NumericUpDown)con).Value);
            if (i != this.bytes)
            {
                this.bytes = i;
                IsChanged = true;
            }
            return IsChanged;
        }

        internal override Item Copy()
        {
            return new ByteItem(this);
        }
        

        public override int CompareTo(string other)
        {
            return this.bytes.CompareTo(ulong.Parse(other));
        }
    }
}

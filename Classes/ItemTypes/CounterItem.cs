using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.Classes.ItemTypes
{
    class CounterItem:Item
    {
        private DateTime lastUse;
        private int count;
        private int change;
        private static string XType = "counter";
        public CounterItem(string name) : base(name) { }
        public CounterItem(CounterItem i) : base(i){
            lastUse = i.lastUse;
            count = i.count;
            change = i.change;
        }
        internal override string GetValueString()
        {
            return String.Format("{0}({1})", count, lastUse);
        }

        internal override System.Windows.Forms.Control GetValueControl()
        {
            Label l = new Label();
            l.AutoSize = true;
            l.Text = GetValueString();
            l.Tag = this;
            return l;
        }

        internal override System.Windows.Forms.Control GetEditableControl()
        {
            NumericUpDown num = new NumericUpDown();
            num.Value = count;
            num.AutoSize = true;

            num.Tag = this;
            return num;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="o">{count}({date time})</param>
        internal override void SetValue(string o)
        {
            string[] ss=o.Split(new char[] { '(',')' });
            count = int.Parse(ss[0]);
            lastUse = DateTime.Parse(ss[1], System.Globalization.CultureInfo.InvariantCulture);
        }

        internal override string GetXmlSaveString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture,"<value name=\"{0}\" type=\"{1}\">{2}({3})</value>", Name, XType, count,lastUse);
        }

        internal override bool HasChanged(System.Windows.Forms.Control con)
        {
            IsChanged = false;
            int i = Decimal.ToInt32(((NumericUpDown)con).Value);
            if (i != count)
            {
                change = i-count;
                count = i;
                lastUse = DateTime.Now;
                IsChanged = true;
            }
            return IsChanged;
        }
        internal override string GetChangeInfoSyncString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}({1})", change, lastUse);
        }

        internal override void ChangeInfoBySyncString(string changeInfo,DateTime changeTime)
        {
            string[] ss = changeInfo.Split(new char[] { '(', ')' });
            count = int.Parse(ss[0]);
            lastUse = DateTime.Parse(ss[1], System.Globalization.CultureInfo.InvariantCulture);
            LastChange = changeTime;
        }

        internal override Item Copy()
        {
            return new CounterItem(this);
        }

        public override int CompareTo(string other)
        {
            DateTime d;
            if(DateTime.TryParse(other,out d))
            {
                return lastUse.CompareTo(d);
            }
            else
            {
                return count.CompareTo(int.Parse(other));
            }
        }
    }
}

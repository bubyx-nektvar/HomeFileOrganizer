using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;

namespace HomeFileOrganizer.XMLProcessors
{
    public class ElementBuilder
    {
        private string n;
        private List<string> atributes = new List<string>();
        private List<ElementBuilder> insiders = new List<ElementBuilder>();
        private string val;
        public ElementBuilder(string name)
        {
            n = String.Format(CultureInfo.InvariantCulture,"{0}",name);
        }
        public ElementBuilder AddAtribute<T>(string s,T o)
        {
            atributes.Add(String.Format(CultureInfo.InvariantCulture," {0}=\"{1}\"",s,o));
            return this;
        }
        public ElementBuilder AddElement(ElementBuilder b)
        {
            insiders.Add(b);
            return this;
        }
        public ElementBuilder SetName(string name)
        {
            n = String.Format(CultureInfo.InvariantCulture, "{0}", name);
            return this;
        }
        public string GetXml()
        {
            StringBuilder b = new StringBuilder();
            //startElement
            b.Append("<");
            b.Append(n);
            foreach(var a in atributes)
            {
                b.Append(a);
            }
            b.Append(">");
            //value of element
            if (val == null)
            {
                foreach (var e in insiders)
                {
                    b.Append(e.GetXml());
                }
            }
            else
            {
                b.Append("<![CDATA[");
                b.Append(val);
                b.Append("]]>");
            }
            //endElement
            b.Append("</");
            b.Append(n);
            b.Append(">");
            return b.ToString();
        }

        internal ElementBuilder AddValue(string value)
        {
            val = value;
            return this;
        }
    }
}

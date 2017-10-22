using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeFileOrganizer.Exceptions
{
    class ProtocolException : Exception
    {
        private FormatException ex;

        public ProtocolException(FormatException ex):base("",ex)
        {
        }

        public ProtocolException(string message) : base(message)
        {
        }
    }
}

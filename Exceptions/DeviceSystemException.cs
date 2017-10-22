using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeFileOrganizer.Exceptions
{
    public class DeviceSystemException:Exception
    {

        public DeviceSystemException(string s) : base(s) { }

        public DeviceSystemException(string p, Exception e) : base(p, e) { }
    }
}

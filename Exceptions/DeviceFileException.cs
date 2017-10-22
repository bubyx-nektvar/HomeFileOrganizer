using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeFileOrganizer.Exceptions
{
    class DeviceFileException:Exception
    {
        public DeviceFileException(string s)
            : base(s)
        {

        }
    }
}

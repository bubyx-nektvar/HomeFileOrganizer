using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeFileOrganizer.Classes.Interfaces
{
    public interface IInfoGetter
    {
        string GetInfoFilePath();
        MyInfoFile GetInfoFile();
    }
}

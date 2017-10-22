using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeFileOrganizer.Classes
{
    public class MyRootFolder:MyFolder
    {
        public MyDisk Disk;
        /// <summary>
        /// Category of folder content. Null if not specified.
        /// </summary>
        public Category Category;

        /// <summary>
        /// Create instance of root folder.
        /// </summary>
        /// <param name="path">path from disk root</param>
        /// <param name="dsk">disk where is this folder</param>
        /// <param name="cat">Category in which is folder placed</param>
        public MyRootFolder(string path, MyDisk dsk,Category cat)
        {
            this.Path = path;
            Disk = dsk;
            Category = cat;
        }
        public override string GetPath()
        {

            return Disk.Id.ToString() + this.Path;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeFileOrganizer.Classes.ItemTypes
{
    public class PictureItem : Item
    {
        /// <summary>
        /// Maximum size of picture
        /// </summary>
        public static int MaxPicSize = 1024 * 1024 * 3;
        /// <summary>
        /// Name of picture file without path.
        /// </summary>
        private string pictureFileName="";
        /// <summary>
        /// Full system path to picture.
        /// Or empty string if not edited.
        /// </summary>
        private string newPicturePath="";
        /// <summary>
        /// Directory where is picture saved
        /// </summary>
        private DirectoryInfo directory;
        private static string XType = "picture";
        /// <param name="name">Name of item</param>
        public PictureItem(string name,UInt64 diskId)
            : base(name)
        {
            DirectoryInfo d = new DirectoryInfo(String.Format("{0}\\FileInfo\\{1}\\ItemFiles",Managers.FileManager.PathToHFOFolder,diskId));
            if (!d.Exists) d.Create();
        }
        public PictureItem(PictureItem i) : base(i)
        {
            pictureFileName = i.pictureFileName;
            newPicturePath = i.newPicturePath;
            directory = i.directory;
        }

        internal override string GetValueString()
        {
            return pictureFileName;
        }

        internal override Control GetValueControl()
        {
            PictureBox pic = new PictureBox();
            try {
                pic.LoadAsync(directory.FullName + "\\" + pictureFileName);
            }
            catch(Exception e) { }
            pic.Tag = this;
            return pic;
        }
        /// <summary>
        /// Name of file in ItemFiles folder
        /// </summary>
        /// <param name="o">Name of file in ItemFiles folder</param>
        internal override void SetValue(string o)
        {
            pictureFileName = o;
        }

        internal override Control GetEditableControl()
        {
            Button b = new Button();
            b.Text = "New picture";
            b.Click += this.selectPicture_Click;
            b.Tag = this;
            return b;
        }
        public void selectPicture_Click(object sender, EventArgs e)
        {
            OpenFileDialog di = new OpenFileDialog();
            di.CheckFileExists = true;
            di.Multiselect = false;
            di.CheckPathExists = true;
            DialogResult res = di.ShowDialog();
            if (res == DialogResult.OK)
            {
                if (di.FileName != "")
                {
                    this.newPicturePath = di.FileName;
                    if (new FileInfo(di.FileName).Length > MaxPicSize) 
                        MessageBox.Show(String.Format("Picture is too big. Maximum size is {0} B",MaxPicSize));
                }
            }
        }
        /// <summary>
        /// Get string with xml value tag in FileInfo format.
        /// </summary>
        /// <returns></returns>
        internal override string GetXmlSaveString()
        {
            return String.Format("<value name=\"{0}\" type=\"{1}\">{2}</value>", Name, XType, pictureFileName);
        }

        internal override bool HasChanged(Control con)
        {
            IsChanged = false;
            if (newPicturePath != "")
            {
                IsChanged = true;
                
                FileInfo f = new FileInfo(newPicturePath);
                if (pictureFileName != "")
                {
                    f.CopyTo(directory+"\\"+pictureFileName, true);
                }
                else
                {
                    FileInfo ff=Managers.FileManager.CreateNewFile(directory, f.Extension);
                    f.CopyTo(ff.FullName,true);
                    pictureFileName = ff.Name;
                }
            }
            return IsChanged;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>{file extension}!{picture data}</returns>
        internal override string GetChangeInfoSyncString()
        {
            string s = "";
            TextReader read = new StreamReader(directory.FullName+"\\"+pictureFileName,Encoding.ASCII);
            try
            {
                s = String.Format("{1}!{0}", read.ReadToEnd(), new FileInfo(directory.FullName + "\\" + pictureFileName).Extension);
            }
            finally { read.Dispose(); }
            return s;

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="changeInfo"></param>
        /// <param name="changeTime"></param>
        internal override void ChangeInfoBySyncString(string changeInfo,DateTime changeTime)
        {
            if (LastChange < changeTime)
            {
                int i = changeInfo.IndexOf('!');
                string exten = changeInfo.Substring(0, i);
                TextWriter writer = new StreamWriter(Managers.FileManager.CreateNewFile(directory, exten).FullName);
                try
                {
                    writer.Write(changeInfo.Remove(0, i + 1)); //+ 1 becouse of skiping '!'
                }
                finally
                {
                    writer.Dispose();
                }
                LastChange = changeTime;
            }
        }

        internal override Item Copy()
        {
            return new PictureItem(this);
        }

        public override int CompareTo(string other)
        {
            throw new NotImplementedException();
        }
    }
}

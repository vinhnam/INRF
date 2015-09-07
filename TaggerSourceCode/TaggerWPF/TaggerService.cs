using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;

namespace TaggerWPF
{
    public class TaggerService
    {
        internal bool ChooseTrainingFolder(string currentFolderPath, out string folderPath)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                dialog.SelectedPath = currentFolderPath;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    folderPath = dialog.SelectedPath;
                    return true;
                }
                folderPath = "";
                return false;
            }
        }
    }
}

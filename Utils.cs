using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptRunner
{
    public class Utils
    {
        private delegate void SafeCallDelegate(string message, string level);

        public static void WriteLog(string message, string level = "INFO")
        {
            if (level.ToUpper() == "DEBUG" && !Properties.Settings.Default.SHOW_DEBUG)          
                return;

            var output = $"[{level.ToUpper()}]: {message}{Environment.NewLine}";
        }
     
    }
}

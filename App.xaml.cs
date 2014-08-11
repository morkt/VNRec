using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace VNRec
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void ApplicationStartup (object sender, StartupEventArgs e)
        {
#if DEBUG
            string trace_dir = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly().Location);
            Trace.Listeners.Add (new TextWriterTraceListener (Path.Combine (trace_dir, "trace.log")));
            Trace.AutoFlush = true;
#endif
            if (VNRec.Properties.Settings.Default.UpgradeRequired)
            {
                VNRec.Properties.Settings.Default.Upgrade();
                VNRec.Properties.Settings.Default.UpgradeRequired = false;
                VNRec.Properties.Settings.Default.Save();
            }
        }

        void ApplicationExit (object sender, ExitEventArgs e)
        {
            VNRec.Properties.Settings.Default.Save();
        }
    }
}

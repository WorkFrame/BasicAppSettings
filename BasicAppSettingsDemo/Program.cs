using System;
using System.Threading;
using System.Windows.Forms;

namespace NetEti.DemoApplications
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            Thread.Sleep(5000); // wegen verzögertem Logging, später besser über FlushBuffers im InfoController lösen.
        }
    }
}

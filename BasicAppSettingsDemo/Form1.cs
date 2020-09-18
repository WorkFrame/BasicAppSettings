using System;
using System.Windows.Forms;
using NetEti.ApplicationControl;
using NetEti.Globals;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace NetEti.DemoApplications
{
    /// <summary>
    /// Demo
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }

        private AppSettings _appSettings;
        private Logger _appLogger;

        private string[] _toReplace = new string[]
        {
            "APPLICATIONROOTPATH"
            , "ApplicationName"
            , "BreakAlwaysAllowed"
            , "CheckVersion"
            , "DebugFile"
            , "DebugFileRegexFilter"
            , "DebugInfo"
            , "DebugMode"
            , "FRAMEWORKVERSIONMAJOR"
            , "HOME"
            , "KillWorkingDirectoryAtShutdown"
            , "MACHINENAME"
            , "OSVERSION"
            , "OSVERSIONMAJOR"
            , "PROCESSORCOUNT"
            , "PROCESSOR_ARCHITECTURE"
            , "PRODUCTNAME"
            , "PROGRAMVERSION"
            , "RegistryBasePath"
            , "SingleInstance"
            , "StatisticsFile"
            , "StatisticsFileRegexFilter"
            , "TEMP"
            , "TempDirectory"
            , "USERDOMAINNAME"
            , "USERNAME"
            , "WorkingDirectory"
        };

        private int _range;
        private Random _random;
        private volatile bool _stop;

        private void Form1_Load(object sender, EventArgs e)
        {
            this._appSettings = GenericSingletonProvider.GetInstance<AppSettings>();
            this._appLogger = new Logger();
            this._appLogger.LogTargetInfo
                = Path.Combine(this._appSettings.WorkingDirectory, Path.GetFileName(this._appLogger.LogTargetInfo));
            this._appLogger.DebugArchivingInterval = this._appSettings.DebugArchivingInterval;
            this._appLogger.DebugArchiveMaxCount = this._appSettings.DebugArchiveMaxCount;

            InfoController.GetInfoController()
                .RegisterInfoReceiver(this._appLogger, null, InfoTypes.Collection2InfoTypeArray(InfoTypes.All));

            this.fillListbox();
            this._range = this._toReplace.Length;
            this._random = new Random();
        }

        private void fillListbox()
        {

            this.listBox1.Items.Clear();
            this.listBox1.Items.Add("AppConfigUser: " + this._appSettings.AppConfigUser ?? "Nicht gesetzt.");
            this.listBox1.Items.Add("AppConfigUserLoaded: " + this._appSettings.AppConfigUserLoaded);
            this.listBox1.Items.Add("TestEinstellung: " + this._appSettings.GetStringValue("TestEinstellung", "Nicht gesetzt."));
            this.listBox1.Items.Add("Applikation-Name: " + this._appSettings.ApplicationName);
            this.listBox1.Items.Add("Programm-Version: " + this._appSettings.ProgrammVersion);
            this.listBox1.Items.Add("OS-Version: " + this._appSettings.OSVersion);
            this.listBox1.Items.Add("OS-Version-Major: " + this._appSettings.OSVersionMajor.ToString());
            this.listBox1.Items.Add("Machine-Name: " + this._appSettings.MachineName);
            this.listBox1.Items.Add("Prozessor: " + this._appSettings.Processor);
            this.listBox1.Items.Add("ApplicationRootPath: " + this._appSettings.ApplicationRootPath);
            this.listBox1.Items.Add("FrameworkVersion: " + this._appSettings.FrameworkVersionMajor.ToString());
            this.listBox1.Items.Add("User-Domain: " + this._appSettings.UserDomainName);
            this.listBox1.Items.Add("User-Name: " + this._appSettings.UserName);
            this.listBox1.Items.Add("TEMP: " + this._appSettings.TempDirectory);
            this.listBox1.Items.Add("IsClickOnce: " + this._appSettings.IsClickOnce.ToString());
            if (this._appSettings.IsClickOnce)
            {
                this.listBox1.Items.Add("ClickOnceDataDirectory: " + this._appSettings.ClickOnceDataDirectory.ToString());
            }
            this.listBox1.Items.Add("Test.ini: " + this._appSettings.TestIniFileName);
            //this.listBox1.Items.Add("DefaultSQLDataDirectory: " + this._appSettings.DefaultSqlDataDirectory);
            //this.listBox1.Items.Add("DefaultSQLLogDirectory: " + this._appSettings.DefaultSqlLogDirectory);
            //this.listBox1.Items.Add("DataSource: " + this._appSettings.DataSource);
            this.listBox1.Items.Add("WorkingDirectory: " + this._appSettings.WorkingDirectory);
            this.listBox1.Items.Add("SearchDirectory: " + this._appSettings.SearchDirectory);
            this.listBox1.Items.Add("CheckVersion: " + this._appSettings.CheckVersion.ToString());
            this.listBox1.Items.Add("BreakAlwaysAllowed: " + this._appSettings.BreakAlwaysAllowed.ToString());
            //this.listBox1.Items.Add("logSQL: " + this._appSettings.LogSql.ToString());
            this.listBox1.Items.Add("DebugFile: " + this._appSettings.DebugFile);
            this.listBox1.Items.Add("DebugInfo: " + InfoTypes.InfoTypeArray2String(this._appSettings.DebugInfo));
            this.listBox1.Items.Add("MinProgrammVersion: " + this._appSettings.MinProgrammVersion);
            this.listBox1.Items.Add("CommandLine: " + this._appSettings.GetStringValue("COMMANDLINE", ""));
            this.listBox1.Items.Add("CurrentDirectory: " + this._appSettings.GetStringValue("CURRENTDIRECTORY", ""));
            this.listBox1.Items.Add("Harry: " + this._appSettings.Harry);

            this.listBox1.Items.Add("ExitCode: " + this._appSettings.GetStringValue("EXITCODE", ""));
            this.listBox1.Items.Add("HasShutdownStarted: " + this._appSettings.GetStringValue("HASSHUTDOWNSTARTED", ""));
            this.listBox1.Items.Add("NewLine: " + this._appSettings.GetStringValue("NEWLINE", ""));
            this.listBox1.Items.Add("ProcessorCount: " + this._appSettings.GetStringValue("PROCESSORCOUNT", ""));
            this.listBox1.Items.Add("SpecialFolder.ApplicationData: " + this._appSettings.GetStringValue("APPLICATIONDATA", ""));
            this.listBox1.Items.Add("SpecialFolder.CommonApplicationData: " + this._appSettings.GetStringValue("COMMONAPPLICATIONDATA", ""));
            this.listBox1.Items.Add("SpecialFolder.CommonProgramFiles: " + this._appSettings.GetStringValue("COMMONPROGRAMFILES", ""));
            this.listBox1.Items.Add("SpecialFolder.Cookies: " + this._appSettings.GetStringValue("COOKIES", ""));
            this.listBox1.Items.Add("SpecialFolder.Desktop: " + this._appSettings.GetStringValue("DESKTOP", ""));
            this.listBox1.Items.Add("SpecialFolder.DesktopDirectory: " + this._appSettings.GetStringValue("DESKTOPDIRECTORY", ""));
            this.listBox1.Items.Add("SpecialFolder.Favorites: " + this._appSettings.GetStringValue("FAVORITES", ""));
            this.listBox1.Items.Add("SpecialFolder.History: " + this._appSettings.GetStringValue("HISTORY", ""));
            this.listBox1.Items.Add("SpecialFolder.InternetCache: " + this._appSettings.GetStringValue("INTERNETCACHE", ""));
            this.listBox1.Items.Add("SpecialFolder.LocalApplicationData: " + this._appSettings.GetStringValue("LOCALAPPLICATIONDATA", ""));
            this.listBox1.Items.Add("SpecialFolder.MyComputer: " + this._appSettings.GetStringValue("MYCOMPUTER", ""));
            this.listBox1.Items.Add("SpecialFolder.MyDocuments: " + this._appSettings.GetStringValue("MYDOCUMENTS", ""));
            this.listBox1.Items.Add("SpecialFolder.MyMusic: " + this._appSettings.GetStringValue("MYMUSIC", ""));
            this.listBox1.Items.Add("SpecialFolder.MyPictures: " + this._appSettings.GetStringValue("MYPICTURES", ""));
            this.listBox1.Items.Add("SpecialFolder.Personal: " + this._appSettings.GetStringValue("PERSONAL", ""));
            this.listBox1.Items.Add("SpecialFolder.ProgramFiles: " + this._appSettings.GetStringValue("PROGRAMFILES", ""));
            this.listBox1.Items.Add("SpecialFolder.Programs: " + this._appSettings.GetStringValue("PROGRAMS", ""));
            this.listBox1.Items.Add("SpecialFolder.Recent: " + this._appSettings.GetStringValue("RECENT", ""));
            this.listBox1.Items.Add("SpecialFolder.SendTo: " + this._appSettings.GetStringValue("SENDTO", ""));
            this.listBox1.Items.Add("SpecialFolder.StartMenu: " + this._appSettings.GetStringValue("STARTMENU", ""));
            this.listBox1.Items.Add("SpecialFolder.Startup: " + this._appSettings.GetStringValue("STARTUP", ""));
            this.listBox1.Items.Add("SpecialFolder.System: " + this._appSettings.GetStringValue("SYSTEM", ""));
            this.listBox1.Items.Add("SpecialFolder.Templates: " + this._appSettings.GetStringValue("TEMPLATES", ""));
            //this.listBox1.Items.Add("StackTrace: " + this._appSettings.GetStringValue("STACKTRACE", ""));
            this.listBox1.Items.Add("SystemDirectory: " + this._appSettings.GetStringValue("SYSTEMDIRECTORY", ""));
            this.listBox1.Items.Add("TickCount: " + this._appSettings.GetStringValue("TICKCOUNT", ""));
            this.listBox1.Items.Add("UserDomainName: " + this._appSettings.GetStringValue("USERDOMAINNAME", ""));
            this.listBox1.Items.Add("UserInteractive: " + this._appSettings.GetStringValue("USERINTERACTIVE", ""));
            this.listBox1.Items.Add("UserName: " + this._appSettings.GetStringValue("USERNAME", ""));
            this.listBox1.Items.Add("Version: " + this._appSettings.GetStringValue("VERSION", ""));
            this.listBox1.Items.Add("WorkingSet: " + this._appSettings.GetStringValue("WORKINGSET", ""));

            foreach (string key in Environment.GetEnvironmentVariables().Keys)
            {
                string val = Environment.GetEnvironmentVariable(key);
                this.listBox1.Items.Add(key + ": " + val);
            }

            this.listBox1.Items.Add("lade Anwendungseinstellungen ...");

            this.listBox1.Items.Add("xxx: " + this._appSettings.GetStringValue("NOPPES", "A_%WorkingDirectory%_E_A_%UserName%_E"));

            //DefaultDBConnectionParameters defaultDBConnectionParameters = new DefaultDBConnectionParameters(this._appSettings.DataSource, "DbDaten");

            //this.listBox1.Items.Add("DataSource: " + this._appSettings.DataSource);
            //this.listBox1.Items.Add("ConnectionString: " + this._appSettings.ConnectionString);


        }

        private void consumeMemory()
        {
            this._stop = false;
            for (int i = 0; i < Int32.MaxValue; i++)
            {
                if (this._stop)
                {
                    this._stop = false;
                    break;
                }
                int ind = this._random.Next(1, this._range) - 1;

                string replaced = _appSettings.ReplaceWildcards(">%" + this._toReplace[ind] + "%<");
                //string replaced = _appSettings.ReplaceWildcards(">%SOMETESTSTRINGS%<");
                //Console.WriteLine(this._toReplace[ind] + ": " + replaced);
                Thread.Sleep(10);
                if (i % 100 == 0)
                {
                    Application.DoEvents();
                }
            }
        }

        private void btnMemTest_Click(object sender, EventArgs e)
        {
            this.btnMemTest.Enabled = false;
            this.consumeMemory();
            this.btnMemTest.Enabled = true;
        }

        private void btnGC_Click(object sender, EventArgs e)
        {
            this._stop = true;
            this.btnGC.Enabled = false;
            int oldCacheSize = Regex.CacheSize;
            Regex.CacheSize = 0;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            Regex.CacheSize = oldCacheSize;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            this.btnGC.Enabled = true;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this._appSettings.Dispose();
            this._appLogger.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (string item in this.listBox1.Items)
            {
                InfoController.Say(item);
            }
        }
    }
}

using NetEti.ApplicationControl;
using NetEti.FileTools;
using NetEti.Globals;
using System.Diagnostics;
using System.Reflection;

namespace NetEti.ApplicationEnvironment
{
    /// <summary>
    /// Holt Applikationseinstellungen aus verschiedenen Quellen:
    /// Kommandozeile, app.config, ggf. app.config.user, Environment.<br />
    /// Implementiert auch das Lesen der Registry, nutzt dies aber selbst noch nicht.
    /// Wertet unter Umständen zuletzt auch noch öffentliche Properties aus (DumpAppSettings=true).
    /// Macht keine Datenbank-Zugriffe, stellt aber entsprechende Properties bereit.<br />
    /// Die Properties und das Füllen derselben sind nicht zwingend erforderlich, sie dienen
    /// nur der Bequemlichkeit; alternativ kann aus der Applikation auch direkt über die
    /// Schnittstellen IGetStringValue und IGetValue&lt;T&gt; auf die Applikations-/Systemumgebung zugegriffen werden.<br />
    /// Die hier implementierten Applikationseinstellungen sollen allgemeingültig für 
    /// Standalone-Anwendungen sein; Für anwendungsspezifische Einstellungen sollte diese Klasse
    /// abgeleitet werden.
    /// Quellen werden in folgender Reihenfolge ausgewertet (der 1. Treffer gewinnt):<br></br>
    ///     1. Kommandozeilen-Parameter (nicht bei .NetCore-Webanwendungen)<br></br>
    ///     2. Einstellungen in der app.Config (nicht bei .NetCore- Webanwendungen, stattdessen dann appsettings.json)<br></br>
    ///     3. Ggf. Einstellungen in der app.Config.user<br></br>
    ///     4. Environment<br></br>
    ///     5. Registry<br></br>
    ///     6. Unter Umständen öffentliche Properties (DumpAppSettings=true).<br></br>
    /// Zeitkritische Initialisierungen sollten in abgeleiteten AppSettings generell vermieden
    /// werden; die entsprechenden Properties können aber definiert werden und ggf. lazy implementiert werden.<br></br>
    /// Über die Properties "DumpAppSettings" und "DumpLoadedAssemblies" können zu Debug-Zwecken alle Properties mit
    /// ihren Quellen und die FullNames aller zur Laufzeit geladenen Assemblies geloggt werden.
    /// </summary>
    /// <remarks>
    /// File: BasicAppSettings.cs<br></br>
    /// Autor: Erik Nagel, NetEti<br></br>
    ///<br></br>
    /// 05.07.2011 Erik Nagel: erstellt.<br></br>
    /// 08.03.2012 Erik Nagel: Singleton-Funktionalität entfernt, läuft jetzt über externen
    ///                        NetEti.Globals.GenericSingletonProvider. Dadurch wird AppSettings
    ///                        vererbbar; komplett überarbeitet, in BasicAppSettings umgetauft und alle
    ///                        anwendungsspezifischen Properties rausgeschmissen (liegen
    ///                        jetzt in den anwendungsspezifischen AppSettings).<br></br>
    /// 23.01.2014 Erik Nagel: ProcessId eingebaut; Accessoren als einfache Properties in den Protected-Bereich verschoben.<br></br>
    /// 10.06.2014 Erik Nagel: IGetValue implementiert.<br></br>
    /// 31.12.2014 Erik Nagel: DebugMode implementiert.<br></br>
    /// 04.10.2015 Erik Nagel: AppConfigUser implementiert.<br></br>
    /// 14.07.2016 Erik Nagel: AppEnvAccessor von protected auf public geändert.<br></br>
    /// 15.10.2017 Erik Nagel: RegistryBasePath eingeführt.<br></br>
    /// 09.03.2019 Erik Nagel: DumpAppSettings und DumpLoadedAssemblies eingeführt.<br></br>
    /// 05.04.2020 Erik Nagel: .NetCore-fähig gemacht; Newtosoft.json integriert; Version 4.6.1.<br></br>
    /// </remarks>
    public class BasicAppSettings : IGetStringValue, IGetValue, IDisposable
    {

        #region public members

        #region Properties (alphabetic)

        /// <summary>
        /// Application.ProductName
        /// </summary>
        public string ApplicationName { get; private set; }

        /// <summary>
        /// Pfad einer XML-Datei im Format der app.config mit User-spezifischen Einstellungen.
        /// </summary>
        public string? AppConfigUser { get; private set; }

        /// <summary>
        /// Info-Text  mit erweiterten Status-Informationen zum Ladeversuch der AppConfigUser.
        /// </summary>
        public string? AppConfigUserInfo { get; private set; }

        /// <summary>
        /// True wenn eine XML-Datei im Format der app.config mit User-spezifischen Einstellungen
        /// geladen werden konnte.
        /// </summary>
        public bool AppConfigUserLoaded { get; private set; }

        /// <summary>
        /// Das Verzeichnis, in dem die Applikation gestartet wurde als absoluter Pfad.
        /// </summary>
        public string ApplicationRootPath { get; private set; }

        /// <summary>
        /// Wenn true, kann immer abgebrochen werden - zu Debug-Zwecken.<br></br>
        /// Default: false<br></br>
        /// </summary>
        public bool BreakAlwaysAllowed { get; set; }

        /// <summary>
        /// Kann ggf. bei einer spätere Versionsprüfung genutzt werden.<br></br>
        /// Default: true<br></br>
        /// </summary>
        public bool CheckVersion { get; set; }

        /// <summary>
        /// Verzeichnis, in dem die Installationsdaten bei einer
        /// ClickOnce-Installation (EnvAccess:ISNETWORKDEPLOYED = true) liegen.
        /// </summary>
        public string? ClickOnceDataDirectory { get; set; }

        /// <summary>
        /// Connection-String für eine Datenbank-Verbindung.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Bei True wird das aktuell gesetzte WorkingDirectory angelegt, wenn es noch nicht existiert.<br></br>
        /// Default: true<br></br>
        /// </summary>
        public bool CreateWorkingDirectoryIfNotExists { get; set; }

        /// <summary>
        /// Der angepasste Datenbank-Server Instanz-Name.<br></br>
        /// Default: = (local)<br></br>
        /// </summary>
        public string? DataSource { get; set; }

        /// <summary>
        /// Maximale Anzahl von archivierten Logs (DebugFiles, o.ä.).
        /// Bei Überzahl werden jeweils die ältesten gelöscht.
        /// Default: 20.
        /// </summary>
        public int DebugArchiveMaxCount { get; set; }

        /// <summary>
        /// Zeitabstand, in dem das aktuelle Log (DebugFile, o.ä.)
        /// archiviert und geleert wird.
        /// Default: 24 Stunden.
        /// </summary>
        public TimeSpan DebugArchivingInterval { get; set; }

        /// <summary>
        /// Pfad und Name des Logfiles<br></br>
        /// Default: WorkingDirectory + \ + ApplicationName + .log<br></br>
        /// </summary>
        public string DebugFile { get; set; }

        /// <summary>
        ///  Filter, der die zu loggenden Zeilen begrenzt.
        ///  Default: "" - alles wird geloggt.
        /// </summary>
        public string DebugFileRegexFilter { get; set; }

        /// <summary>
        /// Die zu loggenden Informationsarten<br></br>
        ///   public enum InfoType <br></br>
        ///   { DEBUG, INFO, WARN, MILESTONE, ERROR, EXCEPTION, USERTYPE1, USERTYPE2 };<br></br>
        ///   als String, wie z.B.: "DEBUG|INFO|WARN|MILESTONE|ERROR".<br></br>
        /// Default: wird im Startprogramm gesetzt, üblicherweise InfoType.ALL
        ///   (InfoType.ALL enthält alles außer den USERTYPEn).<br></br>
        /// </summary>
        /// <returns></returns>
        public InfoType[]? DebugInfo { get; set; }

        /// <summary>
        ///  Bei True können Anwendungen Debug-Ausgaben erzeugen.
        ///  Default: False.
        /// </summary>
        public bool DebugMode { get; set; }

        /// <summary>
        /// Die voreingestellte Datenbank.<br></br>
        /// Default: null<br></br>
        /// </summary>
        public string? DefaultDatabase { get; set; }

        /// <summary>
        /// Das Default-Verzeichnis für SQL-Server Datendateien
        /// </summary>
        public string? DefaultSqlDataDirectory { get; private set; }

        /// <summary>
        /// Das Default-Verzeichnis für die SQL-Server Logdateien
        /// </summary>
        public string? DefaultSqlLogDirectory { get; private set; }

        /// <summary>
        /// Bei true gibt BasicAppSettings über den InfoController am Ende der
        /// Verarbeitung alle AppSetting-Properties mit Wert und Quelle aus.
        /// Default: false.
        /// </summary>
        public bool DumpAppSettings { get; set; }

        /// <summary>
        /// Bei true gibt BasicAppSettings über den InfoController am Ende der
        /// Verarbeitung die Vollnamen aller in der AppDomain geladenen Assemblies aus.
        /// Default: false.
        /// </summary>
        public bool DumpLoadedAssemblies { get; set; }

        /// <summary>
        /// Die Version des höchsten installierten .Net-Frameworks
        /// </summary>
        public int FrameworkVersionMajor { get; private set; }

        /// <summary>
        /// True, wenn die Anwendung per ClickOnce installiert wurde
        /// (siehe auch ClickOnceDataDirectory).
        /// </summary>
        public bool IsClickOnce { get; private set; }

        //using System.Runtime.InteropServices;
        /// <summary>
        /// True, wenn die Anwendung auf .Net-Framework basiert.
        /// </summary>
        public bool IsFrameworkAssembly { get; private set; }

        //using System.Runtime.InteropServices;
        /// <summary>
        /// True, wenn die Anwendung auf einem vollständigen .Net-Framework basiert.
        /// </summary>
        public bool IsFullFramework { get; private set; }

        /// <summary>
        /// True, wenn die Anwendung eine .Net Core-Anwendung ist.
        /// </summary>
        public bool IsNetCore { get; private set; }

        /// <summary>
        /// True, wenn die Anwendung eine UWP-Anwendung ist.
        /// </summary>
        public bool IsNetNative { get; private set; }

        /// <summary>
        /// Bei True sollen alle Child-Prozesse der Anwendung am Programmende
        /// nach einer gewissen Wartezeit rekursiv terminiert werden.
        /// Achtung:
        ///     BasicAppSettings stellt nur die Property zur Verfügung.
        ///     Um das Beenden der Prozesse muss sich die jeweilige Anwendung
        ///     über Einbindung von NetEti.ProcessTools und Aufruf von
        ///     ProcessWorker.FinishChildProcesses() selbst kümmern.
        /// Default: false.
        /// </summary>
        public bool KillChildProcessesOnApplicationExit { get; set; }

        /// <summary>
        /// Bei True wird das WorkingDirectory am Programmende entfernt.
        /// Sollte aus Sicherheitsgründen nur erfolgen, wenn es beim
        /// Programmstart auch erzeugt wurde.
        /// Das Anlegen oder Löschen des WorkingDirectory erfolgt nicht
        /// durch BasicAppSettings sondern kann ggf. durch die Anwendung
        /// oder das Logging erfolgen.
        /// Default: true.
        /// </summary>
        public bool KillWorkingDirectoryAtShutdown { get; set; }

        /// <summary>
        /// Wenn true, soll jeder Sql-Befehl in's Logfile geschrieben werden.
        /// Zur freien Verwendung.<br></br>
        /// Default: false<br></br>
        /// </summary>
        public bool LogSql { get; set; }

        /// <summary>
        /// Der Rechnername
        /// </summary>
        public string MachineName { get; private set; }

        /// <summary>
        /// Geforderte Mindestversion für spätere Prüfung gegen die ProgrammVersion.<br></br>
        /// Default: "1.0.0.0"
        /// </summary>
        public string MinProgrammVersion { get; protected set; }

        /// <summary>
        /// Die komplette Betriebssystem-Version
        /// </summary>
        public string OSVersion { get; private set; }

        /// <summary>
        /// Numerische Haupt-Betriebssystem-Version<br></br>
        ///  > 5 => mindestens Vista
        /// </summary>
        public int OSVersionMajor { get; private set; }

        /// <summary>
        /// Die Prozess-Id des aktuellen Prozesses.
        /// </summary>
        public int ProcessId { get; private set; }

        /// <summary>
        /// Der Prozessortyp
        /// </summary>
        public string Processor { get; private set; }

        /// <summary>
        /// Die Anzahl Prozessoren
        /// </summary>
        public string ProcessorCount { get; private set; }

        /// <summary>
        /// Die aktuelle ProgrammVersion = Application.ProductVersion
        /// </summary>
        public string ProgrammVersion { get; private set; }

        /// <summary>
        /// Basis-Pfad, in dem in der Registry nach einer Einstellung gesucht wird.
        /// Enthält der Pfad eine der RegistryRoots, z.B. "HKEY_CURRENT_USER", wird
        /// die intern eingestellte RegistryRoot ebenfalls umgestellt.
        /// Default für die intern eingestellte RegistryRoot ist "HKEY_LOCAL_MACHINE".
        /// Default: ""
        /// </summary>
        public string? RegistryBasePath
        {
            get
            {
                return this.RegAccessor?.RegistryBasePath;
            }
            set
            {
                this.SetRegistryBasePath(value);
            }
        }

        /// <summary>
        /// Ein optionales Suchverzeichnis für verschiedene Zwecke.<br></br>
        /// Default: WorkingDirectory<br></br>
        /// </summary>
        public string? SearchDirectory { get; set; }

        /// <summary>
        /// Bei true kann die Applikation nur einmal gestartet werden.<br></br>
        /// Default: false<br></br>
        /// </summary>
        public bool SingleInstance { get; private set; }

        /// <summary>
        /// Pfad und Name des Statistics-Logfiles<br></br>
        /// Default: WorkingDirectory + \ + ApplicationName + .stat<br></br>
        /// </summary>
        public string? StatisticsFile { get; set; }

        /// <summary>
        ///  Filter, der die zu loggenden Zeilen begrenzt.
        ///  Default: "" - alles wird geloggt.
        ///  z.B.: @"(?:_NOPPES_)" - Nichts wird geloggt, bzw. nur Zeilen, die "_NOPPES_" enthalten
        /// </summary>
        public string StatisticsFileRegexFilter { get; set; }

        /// <summary>
        /// Environment: "TEMP"
        /// </summary>
        public string TempDirectory { get; private set; }

        /// <summary>
        /// Der Windows-Domain-Name
        /// </summary>
        public string? UserDomainName { get; private set; }

        /// <summary>
        /// Der Windows-Username
        /// </summary>
        public string? UserName { get; private set; }

        /// <summary>
        /// Das Arbeitsverzeichnis<br></br>
        /// Default:<br></br>
        ///   Bei "SingleInstance=true: C:\Users\&lt;user&gt;\AppData\Local\Temp\&lt;ApplicationName&gt;<br></br>
        ///   Bei "SingleInstance=false: C:\Users\&lt;user&gt;\AppData\Local\Temp\&lt;ApplicationName&gt;\&lt;ProcessId&gt;<br></br>
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                return this._workingDirectory;
            }
            set
            {
                if (ThreadLocker.TryLockNameGlobal("ApplicationSettingsImpactLock", 100))
                {
                    // In Multithreading-Umgebungen mit unterschiedlichen Ableitungen
                    // von BasicAppSettings kann es zu Mehrfach-Instanzen kommen.
                    // Nur wenn diese Instanz von BasicAppSettings die erste ist, ggf.
                    // Verzeichnisse neu anlegen, verschieben oder löschen.
                    if (this._workingDirectory != value)
                    {
                        string newWorkingDirectory = value;
                        if (this.WorkingDirectoryCreated)
                        {
                            try
                            {
                                string? newRoot = this.DirectoryCreate(Path.GetDirectoryName(newWorkingDirectory) ?? "");
                                if (Directory.Exists(newWorkingDirectory))
                                {
                                    newWorkingDirectory = Path.Combine(newWorkingDirectory, Guid.NewGuid().ToString());
                                }
                                Directory.Move(this._workingDirectory, newWorkingDirectory);
                                if (this.CreatedDirectoryRoot != newWorkingDirectory && Directory.Exists(this.CreatedDirectoryRoot))
                                {
                                    try
                                    {
                                        Directory.Delete(this.CreatedDirectoryRoot, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        InfoController.Say("#Directory# " + ex.Message);
                                    }
                                }
                                if (newRoot != null)
                                {
                                    this.CreatedDirectoryRoot = newRoot;
                                }
                                else
                                {
                                    this.CreatedDirectoryRoot = newWorkingDirectory;
                                }
                            }
                            catch (Exception ex)
                            {
                                InfoController.Say("#Directory# " + ex.Message);
                            }
                            this._workingDirectory = newWorkingDirectory;
                        }
                        else
                        {
                            this._workingDirectory = newWorkingDirectory;
                            if (this.CreateWorkingDirectoryIfNotExists && !Directory.Exists(this.WorkingDirectory))
                            {
                                try
                                {
                                    this.CreatedDirectoryRoot = this.DirectoryCreate(this.WorkingDirectory);
                                    this.WorkingDirectoryCreated = true;
                                }
                                catch (Exception ex)
                                {
                                    InfoController.Say("#Directory# " + ex.Message);
                                }
                            }
                        }
                        this.AppEnvAccessor.RegisterKeyValue("WorkingDirectory", this.WorkingDirectory);
                    }
                }
                else
                {
                    this._workingDirectory = value; // damit überhaupt was drin steht.
                }
            }
        }

        /// <summary>
        /// True, wenn das WorkingDirectory beim Programmstart erzeugt wurde.
        /// Das Anlegen oder Löschen des WorkingDirectory erfolgt nicht
        /// durch BasicAppSettings sondern kann ggf. durch die Anwendung
        /// oder das Logging erfolgen.
        /// Default: false.
        /// </summary>
        public bool WorkingDirectoryCreated { get; set; }

        #endregion Properties (alphabetic)

        #region IGetStringValue Implementation

        /// <summary>
        /// Liefert genau einen Wert zu einem Key. Wenn es keinen Wert zu dem
        /// Key gibt, wird defaultValue zurückgegeben.
        /// </summary>
        /// <param name="key">Der Zugriffsschlüssel (string)</param>
        /// <param name="defaultValue">Das default-Ergebnis (string)</param>
        /// <returns>Der Ergebnis-String</returns>
        public string? GetStringValue(string key, string? defaultValue)
        {
            return this.AppEnvAccessor.GetStringValue(key, defaultValue);
        }

        /// <summary>
        /// Liefert ein string-Array zu einem Key. Wenn es keinen Wert zu dem
        /// Key gibt, wird defaultValue zurückgegeben.
        /// </summary>
        /// <param name="key">Der Zugriffsschlüssel (string)</param>
        /// <param name="defaultValues">Das default-Ergebnis (string[])</param>
        /// <returns>Das Ergebnis-String-Array</returns>
        public string?[]? GetStringValues(string key, string?[]? defaultValues)
        {
            return this.AppEnvAccessor.GetStringValues(key, defaultValues);
        }

        /// <summary>
        /// Liefert einen beschreibenden Namen dieses StringValueGetters,
        /// z.B. Name plus ggf. Quellpfad.
        /// </summary>
        public string Description
        {
            get
            {
                return this.AppEnvAccessor.Description;
            }
            set
            {
            }
        }

        #endregion IGetStringValue Implementation

        #region IGetValue Implementation

        /// <summary>
        /// Liefert genau einen Wert zu einem Key. Wenn es keinen Wert zu dem
        /// Key gibt, wird defaultValue zurückgegeben.
        /// Wildcards der Form %Name% werden, wenn möglich, rekursiv ersetzt;
        /// Es wird versucht, den ermittelten String-Wert in den Rückgabetyp T zu casten.
        /// </summary>
        /// <typeparam name="T">Der gewünschte Rückgabe-Typ</typeparam>
        /// <param name="key">Der Zugriffsschlüssel (string)</param>
        /// <param name="defaultValue">Das default-Ergebnis vom Typ T</param>
        /// <returns>Wert zum key in den Rückgabe-Typ gecastet</returns>
        /// <exception cref="InvalidCastException">Typecast-Fehler</exception>
        public T? GetValue<T>(string key, T? defaultValue)
        {
            return this.AppEnvAccessor.GetValue<T>(key, defaultValue);
        }

        /// <summary>
        /// NICHT IMPLEMENTIERT!
        /// Liefert ein Array von Werten zu einem Key. Wenn es keinen Wert zu dem
        /// Key gibt, wird defaultValue zurückgegeben.
        /// Wildcards der Form %Name% werden, wenn möglich, rekursiv ersetzt;
        /// Es wird versucht, den ermittelten String-Wert in den Rückgabetyp T zu casten.
        /// </summary>
        /// <typeparam name="T">Der gewünschte Rückgabe-Typ</typeparam>
        /// <param name="key">Der Zugriffsschlüssel (string)</param>
        /// <param name="defaultValues">Das default-Ergebnis vom Typ T[]</param>
        /// <returns>Wert-Array zum key in den Rückgabe-Typ gecastet</returns>
        /// <exception cref="InvalidCastException">Typecast-Fehler</exception>
        public T?[]? GetValues<T>(string key, T?[]? defaultValues)
        {
            return this.AppEnvAccessor.GetValues<T>(key, defaultValues);
        }

        #endregion IGetValue Implementation

        #region IDisposable Member

        private bool _disposed = false;
        private string _workingDirectory;

        /// <summary>
        /// Öffentliche Methode zum Aufräumen.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Abschlussarbeiten, ggf. Timer zurücksetzen.
        /// </summary>
        /// <param name="disposing">False, wenn vom eigenen Destruktor aufgerufen.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    // Aufräumarbeiten durchführen und dann beenden.
                    if (this.DumpAppSettings)
                    {
                        try
                        {
                            SortedDictionary<string, string> allParameters = this.GetParametersSources();
                            foreach (string parameter in allParameters.Keys)
                            {
                                if (parameter != "__NOPPES__")
                                {
                                    InfoController.GetInfoPublisher().Publish(null, parameter + ": " + allParameters[parameter], InfoType.NoRegex);
                                }
                            }
                        }
                        catch { }
                    }
                    if (this.DumpLoadedAssemblies)
                    {
                        try
                        {
                            Thread.Sleep(10); // sorgt für eine Trennung von AppSettings und Assemblies
                            SortedDictionary<string, Assembly> assemblies = this.GetLoadedAssemblies();
                            foreach (string assemblyName in assemblies.Keys)
                            {
                                string? assemblyLocation = null;
                                try
                                {
                                    assemblyLocation = assemblies[assemblyName].Location;
                                }
                                catch { }
                                if (String.IsNullOrEmpty(assemblyLocation))
                                {
                                    try
                                    {
                                        assemblyLocation = assemblies[assemblyName].Location;
                                    }
                                    catch { }
                                }
                                string assemblyNamePlusLocation = assemblyName
                                    + Environment.NewLine + "Location: " + assemblyLocation ?? "---";
                                InfoController.GetInfoPublisher().Publish(null, assemblyNamePlusLocation, InfoType.NoRegex);
                            }
                        }
                        catch { }
                    }
                    InfoController.GetInfoController().Dispose();
                    if (this.KillWorkingDirectoryAtShutdown && this.WorkingDirectoryCreated)
                    {
                        try
                        {
                            if (this.CreatedDirectoryRoot != null)
                            {
                                Directory.Delete(this.CreatedDirectoryRoot, true);
                            }
                        }
                        catch { }
                    }
                    try
                    {
                        ThreadLocker.UnlockNameGlobal("ApplicationSettingsImpactLock");
                    }
                    catch { }
                }
                this._disposed = true;
            }
        }

        /// <summary>
        /// Destruktor
        /// </summary>
        ~BasicAppSettings()
        {
            this.Dispose(false);
        }

        #endregion IDisposable Member

        /// <summary>
        /// Liefert ein Dictionary, das zu jedem Parameter den Namen der Quelle enthält.
        /// Kann in bestimmten Fällen für die Fehlersuche hilfreich sein.<br></br>
        /// Hinweis: ist der Schalter "DumpAppSettings" = true, wird in "Dispose" dieses Dictionary geloggt.
        /// </summary>
        /// <returns>Dictionary, das zu jedem Parameter den Namen der Quelle enthält.</returns>
        public SortedDictionary<string, string> GetParametersSources()
        {
            return this.AppEnvAccessor.GetParametersSources();
        }

        /// <summary>
        /// Liefert ein Dictionary, das für alle in der Applikation geladenen Assemblies
        /// den FullName und die Assembly enthält.
        /// Kann in bestimmten Fällen für die Fehlersuche hilfreich sein.<br></br>
        /// Hinweis: ist der Schalter "DumpLoadedAssemblies" = true, werden in "Dispose" die Keys dieses Dictionarys (FullName) geloggt.
        /// </summary>
        /// <returns>Dictionary, das zu jeder geladenen Assembly den FullName und die Assembly enthält.</returns>
        public SortedDictionary<string, Assembly> GetLoadedAssemblies()
        {
            List<Assembly> rawAssemblies = AssemblyLoader.GetLoadedAssemblies();
            SortedDictionary<string, Assembly> assemblies = new SortedDictionary<string, Assembly>();
            foreach (Assembly assembly in rawAssemblies)
            {
                int multipleLoadedIndex = 0;
                string assemblyFullName = assembly.FullName ?? "";
                while (assemblies.ContainsKey(assemblyFullName) && multipleLoadedIndex < 9)
                {
                    multipleLoadedIndex++;
                }
                if (multipleLoadedIndex > 0)
                {
                    assemblyFullName += String.Format($"({multipleLoadedIndex})");
                }
                assemblies.Add(assemblyFullName, assembly);
            }
            return assemblies;
        }

        /// <summary>
        /// Setzt den Registry-Zugriffskey für alle nachfolgende Zugriffe auf
        /// den übergebenen Basis-Pfad, wenn sich der übergebene registryBasePath
        /// fehlerfrei in ein entsprechendes Equivalent aus Registy-Keys umwandeln lässt.
        /// </summary>
        /// <param name="registryBasePath">Pfad zum Registry-Key, unterhalb dessen zukünftige Zugriffe erfolgen sollen.</param>
        /// <exception cref="ArgumentException">Wird ausgelöst, wenn der übergebene registryBasePath nicht in einen RegistryKey konvertierbar ist.</exception>
        public void SetRegistryBasePath(string? registryBasePath)
        {
            if (registryBasePath != null)
            {
                this.RegAccessor.SetRegistryBasePath(registryBasePath);
            }
        }

        /// <summary>
        /// Lädt die Systemeinstellungen bei der Initialisierung oder lädt sie auf Anforderung erneut.
        /// </summary>
        public virtual void LoadSettings()
        {
            if (this.UserSettingsAccessor != null)
            {
                this.AppEnvAccessor.UnregisterStringValueGetter(this.UserSettingsAccessor);
                this.UserSettingsAccessor = null;
            }
            this.AppConfigUserLoaded = false;
            string? newTempDirectory = this.GetStringValue("TEMP", "");
            if (newTempDirectory != null)
            {

                this.TempDirectory = newTempDirectory;
            }
            string? configuredApplicationName = this.GetStringValue("PRODUCTNAME", "");
            if (configuredApplicationName != null)
            {
                this.ApplicationName = configuredApplicationName;
            }
            this.AppEnvAccessor.RegisterKeyValue("ApplicationName", this.ApplicationName);
            string defaultAppConfigUser =
                Path.Combine(Path.GetDirectoryName(this.TempDirectory) ?? "",
                    this.ApplicationName, String.Format($"{this.ApplicationName}.exe.config.user"));
            this.AppConfigUser = this.GetStringValue("AppConfigUser", defaultAppConfigUser);
            if (!String.IsNullOrEmpty(this.AppConfigUser))
            {
                if (File.Exists(this.AppConfigUser))
                {
                    try
                    {
                        this.UserSettingsAccessor = new XmlAccess(this.AppConfigUser);
                        this.AppEnvAccessor.RegisterStringValueGetterBefore(this.UserSettingsAccessor, this.SettingsAccessor);
                        this.AppConfigUserLoaded = true;
                        this.AppConfigUserInfo = "AppConfigUser erfolgreich geladen.";
                    }
                    catch (Exception ex)
                    {
                        this.AppConfigUserInfo = String.Format(
                            $"Fehler beim Öffnen der {this.AppConfigUser}: ")
                            + Environment.NewLine + ex.Message;
                    }
                }
                else
                {
                    this.AppConfigUserInfo = String.Format(
                        $"Die Datei {this.AppConfigUser} wurde nicht gefunden.");
                }
            }
            else
            {
                this.AppConfigUserInfo = "Es wurde keine AppConfigUser konfiguriert.";
            }
            this.RegistryBasePath = this.AppEnvAccessor.GetStringValue("RegistryBasePath", "");
            this.SingleInstance = Convert.ToBoolean(this.GetStringValue("SingleInstance", "false"),
                                                    System.Globalization.CultureInfo.CurrentCulture);
            this.MachineName = this.GetStringValue("MACHINENAME", "") ?? "";
            this.AppEnvAccessor.RegisterKeyValue("MACHINENAME", this.MachineName);
            this.Processor = this.GetStringValue("PROCESSOR_ARCHITECTURE", "") ?? "";
            this.ProcessorCount = this.GetStringValue("PROCESSORCOUNT", "") ?? "";
            this.UserDomainName = this.GetStringValue("USERDOMAINNAME", "") ?? "";
            this.UserName = this.GetStringValue("USERNAME", "") ?? "";
            this.AppEnvAccessor.RegisterKeyValue("USERNAME", this.UserName);
            this.TempDirectory = this.GetStringValue("TEMP", "") ?? "";
            this.AppEnvAccessor.RegisterKeyValue("TempDirectory", this.TempDirectory);
            this.OSVersion = this.GetStringValue("OSVERSION", "") ?? "";
            this.OSVersionMajor = Convert.ToInt16(this.GetStringValue("OSVERSIONMAJOR", "0"), System.Globalization.CultureInfo.CurrentCulture);
            this.ApplicationRootPath = this.GetStringValue("APPLICATIONROOTPATH", "") ?? "";
            this.AppEnvAccessor.RegisterKeyValue("APPLICATIONROOTPATH", this.ApplicationRootPath);
            this.AppEnvAccessor.RegisterKeyValue("HOME", this.ApplicationRootPath);
            this.IsClickOnce = this.GetValue<bool>("ISNETWORKDEPLOYED", false);
            this.AppEnvAccessor.RegisterKeyValue("IsClickOnce", this.IsClickOnce.ToString());
            this.ClickOnceDataDirectory = this.GetStringValue("CLICKONCEDATA", null);
            if (this.IsClickOnce && this.ClickOnceDataDirectory != null)
            {
                this.AppEnvAccessor.RegisterKeyValue("ClickOnceDataDirectory", this.ClickOnceDataDirectory);
            }
            this.FrameworkVersionMajor = Convert.ToInt16(this.GetStringValue("FRAMEWORKVERSIONMAJOR", ""), System.Globalization.CultureInfo.CurrentCulture);
            this.ProcessId = Process.GetCurrentProcess().Id;
            this.AppEnvAccessor.RegisterKeyValue("ProcessId", this.ProcessId.ToString());
            this.CreateWorkingDirectoryIfNotExists = Convert.ToBoolean(this.GetStringValue("CreateWorkingDirectoryIfNotExists", "true"));
            string? newWorkingDirectory = null;
            if (this.SingleInstance)
            {
                newWorkingDirectory
                    = this.GetStringValue("WorkingDirectory", Path.Combine(this.TempDirectory, this.ApplicationName)
                          .TrimEnd(Path.DirectorySeparatorChar));
            }
            else
            {
                newWorkingDirectory
                    = this.GetStringValue("WorkingDirectory", Path.Combine(Path.Combine(this.TempDirectory, this.ApplicationName),
                  this.ProcessId.ToString()).TrimEnd(Path.DirectorySeparatorChar));
            }
            if (newWorkingDirectory != null)
            {
                this.WorkingDirectory = newWorkingDirectory;
            }
            this.AppEnvAccessor.RegisterKeyValue("WorkingDirectory", this.WorkingDirectory);
            this.KillWorkingDirectoryAtShutdown = Convert.ToBoolean(this.GetStringValue("KillWorkingDirectoryAtShutdown", "false"));
            this.KillChildProcessesOnApplicationExit = Convert.ToBoolean(this.GetStringValue("KillChildProcessesOnApplicationExit", "false"));
            this.SearchDirectory = this.GetStringValue("SearchDirectory", this.WorkingDirectory)?.TrimEnd(Path.DirectorySeparatorChar);
            string? newDebugFile
                = this.GetStringValue("DebugFile", this.WorkingDirectory + Path.DirectorySeparatorChar + this.ApplicationName + @".log");
            if (newDebugFile != null)
            {
                this.DebugFile = newDebugFile;
            }
            this.SetDebugArchivingInterval(this.GetStringValue("DebugArchivingInterval", "24:00:00"));
            this.DebugArchiveMaxCount = this.GetValue<int>("DebugArchiveMaxCount", 20);
            this.AppEnvAccessor.RegisterKeyValue("DebugFile", this.DebugFile);
            string? debugInfoString = this.GetStringValue("DebugInfo", "ALL");
            this.AppEnvAccessor.RegisterKeyValue("DebugInfo", debugInfoString ?? "ALL");
            this.DebugInfo = InfoTypes.String2InfoTypeArray(debugInfoString ?? "ALL");
            this.DebugFileRegexFilter = this.GetStringValue("DebugFileRegexFilter", "") ?? "";
            this.DebugMode = this.GetValue<bool>("DebugMode", false);
            this.StatisticsFile
                = this.GetStringValue("StatisticsFile", this.WorkingDirectory + Path.DirectorySeparatorChar + this.ApplicationName + @".stat");
            try
            {
                if (File.Exists(this.StatisticsFile))
                {
                    File.Delete(this.StatisticsFile);
                }

            }
            catch { }
            this.StatisticsFileRegexFilter = this.GetStringValue("StatisticsFileRegexFilter", "") ?? "";
            this.BreakAlwaysAllowed = Convert.ToBoolean(this.GetStringValue("BreakAlwaysAllowed", "false"),
                                                        System.Globalization.CultureInfo.CurrentCulture);
            this.ProgrammVersion = this.GetStringValue("PROGRAMVERSION", "1.0.0.0") ?? "1.0.0.0";
            this.MinProgrammVersion = "1.0.0.0"; // muss ggf. in einer abgeleiteten Klasse überschrieben werden
            this.CheckVersion = Convert.ToBoolean(this.GetStringValue("CheckVersion", "true"),
                                                  System.Globalization.CultureInfo.CurrentCulture);
            this.DumpAppSettings = this.GetValue<bool>("DumpAppSettings", false);
            this.DumpLoadedAssemblies = this.GetValue<bool>("DumpLoadedAssemblies", false);

            this.SetDefaultSQLDirectories();
            this.DataSource = this.GetStringValue("DataSource", null);
            // this.AppEnvAccessor.RegisterKeyValue("DataSource", this.DataSource ?? "unknown");
            this.AppEnvAccessor.RegisterKeyValue("DataSource", this.DataSource);
            this.DefaultDatabase = null;
            this.LogSql = Convert.ToBoolean(this.GetStringValue("LogSql", "false"), System.Globalization.CultureInfo.CurrentCulture);
            this.AppEnvAccessor.RegisterKeyValue("LogSql", this.LogSql.ToString());
        }

        // Alter Kommentar: Achtung: diese String-Ersetzung verursacht Memory-Leaks! Deshalb nicht in Loops verwenden.
        // 14.01.2018 Nagel: Neue Tests konnten keine Memory-Leaks aufzeigen (siehe BasicAppSettingsDemo).
        /// <summary>
        /// Ersetzt Wildcards im Format %Name% durch ihre Laufzeit-Werte.
        /// </summary>
        /// <param name="inString">Wildcard</param>
        /// <returns>Laufzeit-Ersetzung</returns>
        public virtual string ReplaceWildcards(string inString)
        {
            return this.GetStringValue("__NOPPES__", inString) ?? inString;
        }

        /// <summary>
        /// Implementiert IGetStringValue zur Kapselung von Zugriffen über konkrete
        /// Reader wie zum Beispiel CommandLineAccess, SettingsAccess, EnvAccess.
        /// </summary>
        public AppEnvReader AppEnvAccessor { get; private set; }

        #endregion public members

        #region protected members

        /// <summary>
        /// Konstruktor, wird ggf. über Reflection vom externen statischen
        /// GenericSingletonProvider über GetInstance() aufgerufen.
        /// Holt alle Infos und stellt sie als Properties zur Verfügung.
        /// </summary>
        protected BasicAppSettings()
        {
            this.DebugFileRegexFilter = String.Empty;
            this.StatisticsFileRegexFilter = String.Empty;
            this.MachineName = String.Empty;
            this.MinProgrammVersion = String.Empty;
            this.ProgrammVersion = String.Empty;
            this.OSVersion = String.Empty;
            this.Processor = String.Empty;
            this.ProcessorCount = String.Empty;

            // this.IsFullFramework = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework",
            // StringComparison.OrdinalIgnoreCase);
            // this.IsNetNative = RuntimeInformation.FrameworkDescription.StartsWith(".NET Native",
            //     StringComparison.OrdinalIgnoreCase);
            // this.
            //
            // IsNetCore = RuntimeInformation.FrameworkDescription.StartsWith(".NET Core",
            //     StringComparison.OrdinalIgnoreCase);

            this.AppEnvAccessor = new AppEnvReader();

            this.CommandLineAccessor = new CommandLineAccess();
            this.SettingsAccessor = new SettingsAccess();
            this.EnvAccessor = new EnvAccess();
            // IniAccessor und PropertyAccessor werden hier nicht genutzt, aber mit null
            // vor-initialisiert, damit die zugehörigen NuGet-Packages dem Package
            // NetEti.BasicAppSettings.<Version>.nupkg als Referenzen hinzugefügt werden.
            // Ohne diese Vor-Initialisierung werden die Packages wegoptimiert.
            this.IniAccessor = null;
            this.PropertyAccessor = null;
            this.RegAccessor = new RegAccess();

            this.AppEnvAccessor.RegisterStringValueGetter(this.CommandLineAccessor);
            AppSettingsRegistry.RememberParameterSource("CommandLine", "CommandLine", this.CommandLineAccessor.CommandLine ?? "''");
            this.AppEnvAccessor.RegisterStringValueGetter(this.SettingsAccessor);
            this.AppEnvAccessor.RegisterStringValueGetter(this.EnvAccessor);
            this.AppEnvAccessor.RegisterStringValueGetter(this.RegAccessor);

            this.IsFrameworkAssembly = this.GetValue<bool>("IsFrameworkAssembly", false);
            this.AppEnvAccessor.RegisterKeyValue("IsFrameworkAssembly", this.IsFrameworkAssembly);
            this._workingDirectory = ".";
            this.TempDirectory = ".";
            this.ApplicationRootPath = ".";
            this.ApplicationName = "NetEti.BasicAppSettings;";
            this.DebugFile = this.ApplicationName + ".log";

            this.LoadSettings();
        }

        /// <summary>
        /// Implementiert IGetStringValue für Zugriffe auf die Kommandozeile.
        /// </summary>
        protected CommandLineAccess CommandLineAccessor { get; private set; }

        /// <summary>
        /// Implementiert IGetStringValue für Zugriffe auf die app.config.
        /// </summary>
        protected SettingsAccess SettingsAccessor { get; private set; }

        /// <summary>
        /// Implementiert IGetStringValue für Zugriffe auf die app.config.user.
        /// </summary>
        protected XmlAccess? UserSettingsAccessor { get; private set; }

        /// <summary>
        /// Implementiert IGetStringValue für Zugriffe auf das Environment.
        /// </summary>
        protected EnvAccess EnvAccessor { get; private set; }

        /// <summary>
        /// Implementiert IGetStringValue für Zugriffe auf INI-Dateien.
        /// </summary>
        protected IniAccess? IniAccessor { get; private set; }

        /// <summary>
        /// Implementiert IGetStringValue für Zugriffe auf die Registry.
        /// </summary>
        protected RegAccess RegAccessor { get; private set; }

        /// <summary>
        /// Implementiert IGetStringValue für Zugriffe auf public Properties der AppSettings.
        /// </summary>
        protected PropertyAccess? PropertyAccessor { get; private set; }

        /// <summary>
        /// Enthält den Pfad des untersten Verzeichnisses, das beim letzten
        /// "Directory.Create" neu angelegt wurde.
        /// Dieser 
        /// </summary>
        protected string? CreatedDirectoryRoot { get; set; }

        /// <summary>
        /// Erstellt einen kompletten Verzeichnispfad, wenn dieser oder ein Teil
        /// davon nicht existiert. Retourniert den Teil des Verzeichnispfades,
        /// der bis inklusive zum ersten der neu angelegten Verzeichnisse geht oder null.
        /// Das Ergebnis dieser Routine kann dafür genutzt werden, genau den Teil
        /// eines Verzeichnispfades auch wieder zu löschen, der neu angelegt wurde.
        /// </summary>
        /// <param name="directoryToCreate">Pfad des anzulegenden Verzeichnisses</param>
        /// <returns>Verzeichnispfad bis inklusive zum ersten neu angelegten Verzeichnis.</returns>
        /// <exception cref="IOException" />
        /// <exception cref="UnauthorizedAccessException" />
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="PathTooLongException" />
        /// <exception cref="DirectoryNotFoundException" />
        /// <exception cref="NotSupportedException" />
        protected string? DirectoryCreate(string directoryToCreate)
        {
            string? createdDirectoryRoot = null;
            if (!Directory.Exists(directoryToCreate))
            {
                createdDirectoryRoot = directoryToCreate;
                while (createdDirectoryRoot != null && !Directory.Exists(Path.GetDirectoryName(createdDirectoryRoot)))
                {
                    createdDirectoryRoot = Path.GetDirectoryName(createdDirectoryRoot);
                }
                if (createdDirectoryRoot != null)
                {
                    Directory.CreateDirectory(directoryToCreate);
                }
                else
                {
                    throw new ArgumentException(String.Format(
                        $"Das Verzeichnis {directoryToCreate} konnte nicht angelegt werden!"));
                }
            }
            return createdDirectoryRoot;
        }

        /// <summary>
        /// Setzt die Default-Verzeichnisse für den Microsoft SQL Server.
        /// </summary>
        protected virtual void SetDefaultSQLDirectories()
        {
            string regsqlroot = @"SOFTWARE\Microsoft\Microsoft SQL Server";

            this.DefaultSqlDataDirectory = null;
            this.DefaultSqlLogDirectory = null;
            // Liste der installierten Instanzen holen
            string?[]? vals = this.GetStringValues(regsqlroot + @"\InstalledInstances", null);
            if ((vals != null) && (vals.Length > 0))
            {
                // uns interessiert hier nur der letzte Eintrag, i.d.R. gibt es überhaupt nur einen
                // - davon den Versions-spezifischen Registry-Pfad holen
                string? val = this.GetStringValue(regsqlroot + @"\Instance Names\SQL\" + vals[vals.Length - 1], null);
                if (val != null)
                {
                    string regSqlInstanceRoot = regsqlroot + @"\" + val + @"\MSSQLServer";
                    this.DefaultSqlDataDirectory = this.GetStringValue(regSqlInstanceRoot + @"\DefaultData", this.DefaultSqlDataDirectory)?.TrimEnd(Path.DirectorySeparatorChar);
                    if (this.DefaultSqlDataDirectory != null)
                    {
                        this.AppEnvAccessor.RegisterKeyValue("DefaultSqlDataDirectory", this.DefaultSqlDataDirectory);
                    }
                    this.DefaultSqlLogDirectory = this.GetStringValue(regSqlInstanceRoot + @"\DefaultLog", this.DefaultSqlLogDirectory)?.TrimEnd(Path.DirectorySeparatorChar);
                    if (this.DefaultSqlLogDirectory != null)
                    {
                        this.AppEnvAccessor.RegisterKeyValue("DefaultSqlLogDirectory", this.DefaultSqlLogDirectory);
                    }
                }
            }
        }

        #endregion protected members

        #region private members

        // <summary>
        // Implementiert IGetStringValue zur Kapselung von Zugriffen über konkrete
        // Reader wie zum Beispiel CommandLineAccess, SettingsAccess, EnvAccess.
        // </summary>
        // private AppEnvReader _appEnvAccessor_reku;

        private void SetDebugArchivingInterval(string? timeSpanString)
        {
            TimeSpan timeSpan;
            if (TimeSpan.TryParse(timeSpanString, out timeSpan))
            {
                this.DebugArchivingInterval = timeSpan;
            }
            else
            {
                this.DebugArchivingInterval = new TimeSpan(24, 0, 0);
            }
        }

        #endregion private members

    }
}

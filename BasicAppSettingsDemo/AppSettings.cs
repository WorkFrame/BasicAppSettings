using NetEti.Globals;
using NetEti.ApplicationEnvironment;
using NetEti.FileTools;
using System.Collections.Generic;
using System;
using System.IO;

namespace NetEti.DemoApplications
{
    /// <summary>
    /// Holt Applikationseinstellungen aus verschiedenen Quellen:
    /// Kommandozeile, app.config, Environment, Registry und (zur Demo) aus einer test.ini.
    /// Erbt allgemeingültige Einstellungen von BasicAppSettings
    /// und fügt Anwendungsspezifische Properties hinzu.<br></br>
    /// <seealso cref="BasicAppSettings"/>
    /// </summary>
    /// <remarks>
    /// File: AppSettings.cs<br></br>
    /// Autor: Erik Nagel, NetEti<br></br>
    ///<br></br>
    /// 08.03.2012 Erik Nagel: erstellt<br></br>
    /// </remarks>
    public sealed class AppSettings : BasicAppSettings
    {
        #region public members

        #region Properties (alphabetic)

        // 12.03.2012 Nagel Testeinträge +
        public string? Harry { get; set; }

        /// <summary>
        /// Name und Pfad der test.ini
        /// </summary>
        public string TestIniFileName { get; set; }

        // 12.03.2012 Nagel Testeinträge -
        /// <summary>

        // 10.01.2017 Nagel Testeinträge +
        /// Liste von Strings, die zum Aufspüren und Abstellen
        /// von Memory-Leaks bei String-Operationen genutzt werden.
        /// </summary>
        public static Stack<string> SomeTestStrings = new Stack<string>();
        // 10.01.2017 Nagel Testeinträge -

        #endregion Properties (alphabetic)

        // Alter Kommentar: Achtung: diese String-Ersetzung verursacht Memory-Leaks! Deshalb nicht in Loops verwenden.
        // 14.01.2018 Nagel: Neue Tests konnten keine Memory-Leaks aufzeigen (siehe BasicAppSettingsDemo).
        /// <summary>
        /// Ersetzt hier definierte Wildcards durch ihre Laufzeit-Werte:
        /// '%HOME%': '...bin\Debug'.
        /// </summary>
        /// <param name="inString">Wildcard</param>
        /// <returns>Laufzeit-Ersetzung</returns>
        public override string? ReplaceWildcards(string inString)
        {
            try
            {
                NetEti.Globals.ThreadLocker.LockNameGlobal("ReplaceWildcards");
                string? replaced = base.ReplaceWildcards(inString);
                // Regex.Replace(inString, @"%HOME%", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'), RegexOptions.IgnoreCase);
                if (inString.ToUpper().Contains("%SOMETESTSTRINGS%"))
                {
                    replaced = String.Join(",", SomeTestStrings.ToArray());
                }
                return replaced;
            }
            finally
            {
                NetEti.Globals.ThreadLocker.UnlockNameGlobal("ReplaceWildcards");
            }
        }

        #endregion public members

        #region private members

        /// <summary>
        /// Implementiert IGetStringValue für Zugriffe auf INI-Files.
        /// </summary>
        private IniAccess? _iniAccessor;

        private PropertyAccess _propertyAccessor;

        /// <summary>
        /// Private Konstruktor, wird ggf. über Reflection vom externen statischen
        /// GenericSingletonProvider über GetInstance() aufgerufen.
        /// Holt alle Infos und stellt sie als Properties zur Verfügung.
        /// </summary>
        private AppSettings()
            : base()
        {
            this.TestIniFileName = this.ApplicationRootPath + @"\test.ini";
            if (File.Exists(this.TestIniFileName))
            {
                try
                {
                    this._iniAccessor = new IniAccess(TestIniFileName);
                    this.AppEnvAccessor.RegisterStringValueGetter(this._iniAccessor);
                }
                catch (System.IO.IOException)
                {
                    this.TestIniFileName = "* read error * " + this.TestIniFileName;
                }
            }
            else
            {
                this.TestIniFileName = "* not found * " + this.TestIniFileName;
            }

            this._propertyAccessor = new PropertyAccess(this);
            this.AppEnvAccessor.RegisterStringValueGetter(this._propertyAccessor);

            // Checken, ob in test.ini eine Mindest-Version gesetzt wurde;
            // überschreibt den Default aus BasicAppSettings
            this.MinProgrammVersion = this.GetStringValue("Info" + Global.SaveColumnDelimiter + "MyApplicationMindestVersionProg", "1.0.0.0");

            // 12.03.2012 Nagel Testeinträge +
            this.Harry = this.GetStringValue("Harry", "noppes");
            // 12.03.2012 Nagel Testeinträge -

            // 10.01.2017 Nagel Testeinträge +
            string applicationName = this.GetStringValue("PRODUCTNAME", "") ?? "unknown application";
            for (int i = 0; i < 100; i++)
            {
                SomeTestStrings.Push(string.Format($"String-100-Zeichen-Laenge ({applicationName})) ").PadRight(97, '-') + String.Format($"{ i:000}"));
            }
            // 10.01.2017 Nagel Testeinträge -

        } // private AppSettings()

        #endregion private members

    } // public sealed class AppSettings: AppSettings
}

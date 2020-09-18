# BasicAppSettings
Holt Applikationseinstellungen aus verschiedenen Quellen: Kommandozeile, app.config, ggf. app.config.user, Environment.
Implementiert auch das Lesen der Registry, nutzt dies aber selbst noch nicht. Wertet unter Umständen zuletzt auch noch öffentliche Properties aus (DumpAppSettings=true). Macht keine Datenbank-Zugriffe, stellt aber entsprechende Properties bereit.
Die Properties und das Füllen derselben sind nicht zwingend erforderlich, sie dienen nur der Bequemlichkeit; alternativ kann aus der Applikation auch direkt über die Schnittstellen IGetStringValue und IGetValue<T> auf die Applikations-/Systemumgebung zugegriffen werden.
Die hier implementierten Applikationseinstellungen sollen allgemeingültig für Standalone-Anwendungen sein; Für anwendungsspezifische Einstellungen sollte diese Klasse abgeleitet werden. Quellen werden in folgender Reihenfolge ausgewertet (der 1. Treffer gewinnt):
1. Kommandozeilen-Parameter
2. Einstellungen in der app.Config
3. Ggf. Einstellungen in der app.Config.user
4. Environment
5. Registry
6. Unter Umständen öffentliche Properties (DumpAppSettings=true).

Zeitkritische Initialisierungen sollten in abgeleiteten AppSettings generell vermieden werden; die entsprechenden Properties können aber definiert werden und ggf. lazy implementiert werden.
Über die Properties "DumpAppSettings" und "DumpLoadedAssemblies" können zu Debug-Zwecken alle Properties mit ihren Quellen und die FullNames aller zur Laufzeit geladenen Assemblies geloggt werden.
Siehe auch das enthaltene Projekt BasicAppSettingsDemo.

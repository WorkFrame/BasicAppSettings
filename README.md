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

## Einsatzbereich

  - BasicAppSettings gehört, wie auch alle anderen unter **WorkFrame** liegenden Projekte, ursprünglich zum
   Repository **Vishnu** (https://github.com/VishnuHome/Vishnu), kann aber auch eigenständig für andere Apps verwendet werden.

## Voraussetzungen

  - Läuft auf Systemen ab Windows 10.
  - Entwicklung und Umwandlung mit Visual Studio 2022 Version 17.8 oder höher.
  - .Net Runtime ab 8.0.2.

## Schnellstart

Die einzelnen Module (Projekte, Repositories) unterhalb von **WorkFrame** sind teilweise voneinander abhängig,
weshalb folgende Vorgehensweise für die erste Einrichtung empfohlen wird:
  - ### Installation:
	* Ein lokales Basisverzeichnis für alle weiteren WorkFrame-, Vishnu- und sonstigen Hilfs-Verzeichnisse anlegen, zum Beispiel c:\Users\<user>\Documents\MyVishnu.
	* [init.zip](https://github.com/VishnuHome/Setup/raw/master/Vishnu.bin/init.zip) herunterladen und in das Basisverzeichnis entpacken.

	Es entsteht dann folgende Struktur:
	
	![Verzeichnisse nach Installation](./struct.png?raw=true "Verzeichnisstruktur")

## Quellcode und Entwicklung analog zum Repository [Vishnu](https://github.com/VishnuHome/Vishnu)

Für detailliertere Ausführungen sehe bitte dort nach.

## Kurzhinweise

1. Forken des Repositories **BasicAppSettings** über den Button Fork
<br/>(Repository https://github.com/WorkFrame/BasicAppSettings)

2. Clonen des geforkten Repositories **BasicAppSettings** in das existierende Unterverzeichnis
	.../MyVishnu/**WorkFrame**
	


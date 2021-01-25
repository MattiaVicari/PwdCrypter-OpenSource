using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace PwdCrypter
{
    /// <summary>
    /// Modalità di accesso dell'App
    /// </summary>
    public enum SecurityAccess
    {
        MasterPassword,
        Fingerprint,
        TwoFactor
    };

    public class AppSettings
    {
        static readonly Newtonsoft.Json.Converters.IsoDateTimeConverter dateTimeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter
        {
            DateTimeFormat = "yyyy-MM-ddTH:mm:ss.fffK"
        };

        private const string OldSettingsFileName = "pwdcptr.ini";

        // Gruppi del vecchio file di configurazione
        private const string GroupSession = "[Session]";
        private const string GroupCopyExt = "[CopyExt]";
        private const string GroupVersion = "[Version]";
        // Campi
        private const string OptTimeout = "timeout";
        private const string OptExtInstr = "extinstr";
        private const string OptVersion = "version";

        /// <summary>
        /// Vale true se le variabili dei settaggi sono state inizializzate
        /// leggendo i valori dal file di configurazione
        /// </summary>
        private bool Initialized;

        /// <summary>
        /// Percorso del file delle impostazioni
        /// </summary>
        public string AppSettingsFileName { get; private set; }

        /// <summary>
        /// Tempo di timeout della sessione
        /// </summary>
        public int SessionTimeout { get; set; }

        /// <summary>
        /// Impostare a true per visualizzare un messaggio di aiuto quando
        /// si utilizza l'estensione sul browser.
        /// </summary>
        public bool BrowserExtensionHelp { get; set; }

        /// <summary>
        /// Impostare a true per abilitare le notifiche locali sul dispositivo
        /// </summary>
        public bool LocalNotification { get; set; }

        /// <summary>
        /// Impostare a true per abilitare le notifica push sul dispositivo
        /// </summary>
        public bool PushNotification { get; set; }

        /// <summary>
        /// Versione del file delle impostazioni
        /// </summary>
        public string VersionSettings { get; set; }

        public OneSignalSettings OneSignal { get; private set; }

        /// <summary>
        /// Piattaforma Cloud utilizzata
        /// </summary>
        public Cloud.CloudPlatform CloudPlatform { get; set; }

        /// <summary>
        /// Modalità di accesso
        /// </summary>
        public SecurityAccess Access { get; set; }

        /// <summary>
        /// Frequenza del controllo delle password
        /// </summary>
        public uint CheckPasswordFrequency { get; set; }

        /// <summary>
        /// Data e ora del primo controllo pianificato delle password
        /// </summary>
        public DateTime CheckPasswordStartDate { get; set; }

        /// <summary>
        /// Frequenza di backup
        /// </summary>
        public uint BackupFrequency { get; set; }

        /// <summary>
        /// Data del primo backup pianificato
        /// </summary>
        public DateTime BackupStartDate { get; set; }

        /// <summary>
        /// Durata dello storico dei backup
        /// </summary>
        public uint BackupHistory { get; set; }

        private readonly string AppSettingsFolder;

        public AppSettings()
        {
            Initialized = false;
            SessionTimeout = 300;
            BrowserExtensionHelp = true;
            LocalNotification = true;
            PushNotification = true;
            VersionSettings = DependencyService.Get<IAppInformant>().GetVersionNumber();
            CloudPlatform = Cloud.CloudPlatform.Unknown;
            OneSignal = new OneSignalSettings();
            Access = SecurityAccess.MasterPassword;
            CheckPasswordFrequency = 0;
            CheckPasswordStartDate = DateTime.MinValue;
            BackupFrequency = 0;
            BackupStartDate = DateTime.MinValue;
            BackupHistory = 0;

            AppSettingsFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppSettingsFileName = Path.Combine(AppSettingsFolder, "settings.conf");
        }

        public void Init()
        {
            if (!Initialized)
            {
                App.Logger.Debug("Init App settings");
                try
                {
                    ReadSettings();
                    Initialized = true;
                }
                catch(Exception e)
                {
                    App.Logger.Error(string.Format("Error occurred during the reading of the App settings. Error: {0}", e.Message));
                    throw;
                }
            }
        }

        /// <summary>
        /// Legge il file delle impostazioni dell'applicazione.
        /// </summary>
        public void ReadSettings()
        {
            // Verifica la presenza del file di configurazione
            try
            {
                OneSignal.Load();

                string json = File.ReadAllText(AppSettingsFileName, Encoding.UTF8);
                JObject jObject = JObject.Parse(json);
                if (jObject == null)
                    throw new Exception(AppResources.errReadSettings);

                if (jObject.ContainsKey(nameof(SessionTimeout)))
                    SessionTimeout = jObject.Value<int>(nameof(SessionTimeout));
                if (jObject.ContainsKey(nameof(BrowserExtensionHelp)))
                    BrowserExtensionHelp = jObject.Value<bool>(nameof(BrowserExtensionHelp));
                if (jObject.ContainsKey(nameof(LocalNotification)))
                    LocalNotification = jObject.Value<bool>(nameof(LocalNotification));
                if (jObject.ContainsKey(nameof(VersionSettings)))
                    VersionSettings = jObject.Value<string>(nameof(VersionSettings));
                if (jObject.ContainsKey(nameof(PushNotification)))
                    PushNotification = jObject.Value<bool>(nameof(PushNotification));
                if (jObject.ContainsKey(nameof(Access)))
                    Access = (SecurityAccess)jObject.Value<int>(nameof(Access));
                if (jObject.ContainsKey(nameof(CheckPasswordFrequency)))
                    CheckPasswordFrequency = jObject.Value<uint>(nameof(CheckPasswordFrequency));
                if (jObject.ContainsKey(nameof(CheckPasswordStartDate)))
                {
                    CheckPasswordStartDate = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                        jObject[nameof(CheckPasswordStartDate)].ToString(), dateTimeConverter);
                }
                if (jObject.ContainsKey(nameof(BackupFrequency)))
                    BackupFrequency = jObject.Value<uint>(nameof(BackupFrequency));
                if (jObject.ContainsKey(nameof(BackupStartDate)))
                {
                    BackupStartDate = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                        jObject[nameof(BackupStartDate)].ToString(), dateTimeConverter);
                }
                if (jObject.ContainsKey(nameof(BackupHistory)))
                    BackupHistory = jObject.Value<uint>(nameof(BackupHistory));

                CloudPlatform = Cloud.CloudPlatform.Unknown;
                if (jObject.ContainsKey(nameof(CloudPlatform)))
                {
                    string strCloudPlatform = jObject.Value<string>(nameof(CloudPlatform));
                    foreach (Cloud.CloudPlatform cloudp in Utility.EnumHelper.GetValues<Cloud.CloudPlatform>())
                    {
                        if (cloudp.ToString().ToLower().CompareTo(strCloudPlatform) == 0)
                        {
                            CloudPlatform = cloudp;
                            break;
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {
                // OK, allora dovrebbe esistere il vecchio file di configurazione
                if (ReadOldSettingsFile())
                    DeleteOldSettingsFile();

                // Scrittura del nuovo file
                WriteSettings();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error occurred during the reading of the App settings. Error: " + e.Message);
                throw;
            }
        }

        /// <summary>
        /// Elimina il vecchio file delle impostazioni
        /// </summary>
        private void DeleteOldSettingsFile()
        {
            try
            {
                string filePath = Path.Combine(AppSettingsFolder, OldSettingsFileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to delete the old file of the App settings. Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Aggiorna il file delle impostazioni
        /// </summary>
        public void WriteSettings()
        {
            try
            {
                JObject jObject = new JObject
                {
                    { nameof(SessionTimeout), new JValue(SessionTimeout) },
                    { nameof(BrowserExtensionHelp), new JValue(BrowserExtensionHelp) },
                    { nameof(LocalNotification), new JValue(LocalNotification) },
                    { nameof(VersionSettings), new JValue(VersionSettings) },
                    { nameof(PushNotification), new JValue(PushNotification) },
                    { nameof(CloudPlatform), new JValue(CloudPlatform.ToString().ToLower()) },
                    { nameof(Access), new JValue(Access) },
                    { nameof(CheckPasswordFrequency), new JValue(CheckPasswordFrequency) },
                    { nameof(CheckPasswordStartDate),
                      new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(CheckPasswordStartDate, dateTimeConverter)) },
                    { nameof(BackupFrequency), new JValue(BackupFrequency) },
                    { nameof(BackupStartDate),
                      new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(BackupStartDate, dateTimeConverter)) },
                    { nameof(BackupHistory), new JValue(BackupHistory) }
                };

                File.WriteAllText(AppSettingsFileName, jObject.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error occurred during the update of the App settings. Error: " + ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Legge il vecchio file delle impostazioni.
        /// </summary>
        /// <returns>True se la lettura va a buon fine, false altrimenti</returns>
        private bool ReadOldSettingsFile()
        {
            try
            {
                string[] content = File.ReadAllLines(Path.Combine(AppSettingsFolder, OldSettingsFileName), Encoding.UTF8);
                if (content.Length > 0)
                {
                    string Group = "";
                    foreach (string line in content)
                    {
                        // Riconosce il gruppo
                        if (line.StartsWith("[") && line.EndsWith("]"))
                            Group = line;

                        // Salva il valore nella variabile, in base al gruppo
                        if (Group == GroupSession)
                        {
                            if (line.StartsWith(OptTimeout + "="))
                                SessionTimeout = int.Parse(line.Substring(OptTimeout.Length + 1));
                        }
                        else if (Group == GroupCopyExt)
                        {
                            if (line.StartsWith(OptExtInstr + "="))
                                BrowserExtensionHelp = (int.Parse(line.Substring(OptExtInstr.Length + 1)) == 1);
                        }
                        else if (Group == GroupVersion)
                        {
                            if (line.StartsWith(OptVersion + "="))
                                VersionSettings = line.Substring(OptVersion.Length + 1);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("The old file of settings is empty");
                }
                return true;
            }
            catch (FileNotFoundException)
            {
                // OK. E' la prima volta che l'applicazione viene installata
                // sul dispositivo.
                Debug.WriteLine("Old file of settings not found. Maybe, it is your first installation.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error occurred during the reading of old file of the App settings. Error: " + ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Configurazione per OneSignal
        /// </summary>
        public class OneSignalSettings
        {
            const string key1 = @"YOUR_KEY1";
            const string key2 = @"YOUR_KEY2";
            const string key3 = @"YOUR_KEY3";

            const int id1 = 0;
            const int id2 = 0;
            const int id3 = 0;

            public string AppId { get; set; }
            public string Url { get; set; }

            public void Load()
            {
                Url = "https://onesignal.com/api/v1/players";
                AppId = Utility.GetSecretKey(new[] { key1, key2, key3 }, new[] { id1, id2, id3 });
            }
        }
    }
}

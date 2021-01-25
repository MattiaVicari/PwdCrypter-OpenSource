using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PwdCrypter
{
    public class StatisticInformer
    {
        static readonly Newtonsoft.Json.Converters.IsoDateTimeConverter dateTimeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter
        {
            DateTimeFormat = "yyyy-MM-ddTH:mm:ss.fffK"
        };

        // Nomi dei file con le statistiche
        private const string LocalStatisticsFileName = "app.stat";
        private const string CloudStatisticsFileName = "appcloud.stat";

        private DateTime CurrentLogin;
        public DateTime LastLogin { get; private set; }

        /// <summary>
        /// Impostare a true per bloccare i dati nello stato corrente.
        /// </summary>
        public bool Freeze { get; set; }

        /// <summary>
        /// Numero di password
        /// </summary>
        public int PasswordsCount { get; private set; } = 0;
        /// <summary>
        /// Id dell'ultima password inserita
        /// </summary>
        public string LastPasswordId { get; private set; } = "";
        /// <summary>
        /// Id dell'ultima password eliminata
        /// </summary>
        public string LastPasswordRemovedId { get; private set; } = "";
        /// <summary>
        /// Vale true se le statistiche sono relative alle password
        /// su Cloud, false se relative a quelle in locale
        /// </summary>
        public bool Cloud => App.PwdManager.Cloud;
        /// <summary>
        /// Percorso del file con le statistiche
        /// </summary>
        public string StatisticsFilePath
        {
            get
            {
                return GetStatisticFilePath(Cloud);
            }
        }
        /// <summary>
        /// Percorso del file locale delle statistiche
        /// </summary>
        public string StatisticsLocalFilePath => GetStatisticFilePath(false);
        /// <summary>
        /// Percorso del file delle statistiche su Cloud
        /// </summary>
        public string StatisticsCloudFilePath => GetStatisticFilePath(true);

        public readonly Dictionary<AccountType, long> PasswordByAccountType;

        /// <summary>
        /// Cartella che ospita il file delle statistiche
        /// </summary>
        public string StatisticsFolder { get; private set; }

        /// <summary>
        /// Costruttore
        /// </summary>
        public StatisticInformer()
        {
            Freeze = false;
            StatisticsFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            PasswordByAccountType = new Dictionary<AccountType, long>();
            LoadStatistics();
        }

        private string GetStatisticFilePath(bool bCloud)
        {
            return Path.Combine(StatisticsFolder, bCloud ? CloudStatisticsFileName : LocalStatisticsFileName);
        }

        /// <summary>
        /// Sincronizza le informazioni statistiche con la lista corrente delle password
        /// </summary>
        /// <returns></returns>
        public async Task SyncStatistics()
        {
            if (CheckFrozenData())
                return;

            LoadStatistics();
            await GatherStatistics();
        }

        /// <summary>
        /// Carica le informazioni statistiche dal file
        /// </summary>
        private void LoadStatistics()
        {
            if (CheckFrozenData())
                return;

            ResetData();

            try
            {
                string json = File.ReadAllText(StatisticsFilePath, System.Text.Encoding.UTF8);
                JObject jObject = JObject.Parse(json);
                if (jObject != null)
                {
                    LastLogin = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                        jObject[nameof(LastLogin)].ToString(),
                        dateTimeConverter);

                    if (int.TryParse(jObject[nameof(PasswordsCount)].ToString(), out int count))
                        PasswordsCount = count;

                    if (Utility.SafeTryGetJSONString(jObject, nameof(LastPasswordRemovedId), out string pwdRemoveId))
                        LastPasswordRemovedId = pwdRemoveId;
                }
            }
            catch (FileNotFoundException)
            {
                // Tutto ok. Il file potrebbe non esiste
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error occurred during the read of statistics: " + ex.Message);
            }
        }

        private bool CheckFrozenData()
        {
            if (Freeze)
            {
                Debug.WriteLine("Statistics: data has been frozen");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reset delle informazioni collezionate
        /// </summary>
        public void ResetData()
        {
            if (CheckFrozenData())
                return;

            LastLogin = DateTime.Now;
            CurrentLogin = DateTime.Now;
            PasswordsCount = 0;

            ResetPasswordData();
        }

        /// <summary>
        /// Reset delle informazioni raccolte sulla lista delle password
        /// </summary>
        public void ResetPasswordData()
        {
            if (CheckFrozenData())
                return;

            LastPasswordId = "";
            LastPasswordRemovedId = "";
            PasswordByAccountType.Clear();
        }

        /// <summary>
        /// Registra l'inserimento di una password
        /// </summary>
        /// <param name="item">Password inserita</param>
        public void RegisterPasswordInsert(PwdListItem item)
        {
            if (CheckFrozenData())
                return;
            LastPasswordId = item.Id;
        }

        /// <summary>
        /// Registra la rimozione di una password dalla lista
        /// </summary>
        /// <param name="item">Password cancellata dalla lista</param>
        public void RegisterPasswordRemove(PwdListItem item)
        {
            if (CheckFrozenData())
                return;
            LastPasswordRemovedId = item.Id;
        }

        /// <summary>
        /// Registra il login
        /// </summary>
        public async Task RegisterLogin()
        {
            if (CheckFrozenData())
                return;

            CurrentLogin = DateTime.Now;
            await UpdateStatistics();
        }

        /// <summary>
        /// Calcola le informazioni statistiche sulle password
        /// </summary>
        /// <returns></returns>
        public async Task GatherStatistics()
        {
            List<PwdListItem> pwds = await App.PwdManager.GetPasswordList();

            PasswordByAccountType.Clear();
            PasswordsCount = App.PwdManager.PasswordsCount;

            foreach(PwdListItem pwd in pwds)
            {
                LastPasswordId = pwd.Id;
                if (!PasswordByAccountType.ContainsKey(pwd.AccountOption))
                    PasswordByAccountType.Add(pwd.AccountOption, 1);
                else
                    PasswordByAccountType[pwd.AccountOption]++;
            }
        }

        /// <summary>
        /// Aggiornamento delle statistiche
        /// </summary>
        public async Task UpdateStatistics()
        {
            const string CloudFileName = "appcloud.stat";

            if (CheckFrozenData())
                return;

            try
            {
                // Raccoglie i dati
                await GatherStatistics();

                JObject jObject = new JObject
                {
                    {
                        nameof(LastLogin),
                        new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(CurrentLogin, dateTimeConverter))
                    },
                    { nameof(PasswordsCount), new JValue(PasswordsCount) },
                    { nameof(LastPasswordRemovedId), new JValue(LastPasswordRemovedId) }
                };

                File.WriteAllText(StatisticsFilePath, jObject.ToString(), System.Text.Encoding.UTF8);
                // Carica il file su Cloud
                if (Cloud && App.IsCloudEnabled())
                {
                    try
                    {
                        await App.CloudConnector.UploadFile(StatisticsFilePath, CloudFileName);
                    }
                    catch(Exception e)
                    {
                        Debug.WriteLine("Upload of the file {0} failed with error: {1}", StatisticsFilePath, e.Message);
                    }
                }
                Debug.WriteLine("Statistics file updated");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error occurred during the update of statistics. Error: " + ex.Message);
            }
        }
    }
}

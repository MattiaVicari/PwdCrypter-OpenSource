using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PwdCrypter.BreachChecker
{
    public class BreachesSerializer
    {
        static readonly Newtonsoft.Json.Converters.IsoDateTimeConverter dateTimeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter
        {
            DateTimeFormat = "yyyy-MM-ddTH:mm:ss.fffK"
        };

        /// <summary>
        /// Salva la lista delle violazioni in formato JSON
        /// </summary>
        /// <param name="BreachesList">Lista delle violazioni</param>
        /// <returns>Oggetto JSON della lista delle violazioni</returns>
        public static JObject SaveAsJSON(List<AccountInfo> BreachesList)
        {
            JArray jsonAccounts = new JArray();
            foreach (AccountInfo accountInfo in BreachesList)
            {
                jsonAccounts.Add(accountInfo.AsJSON());
            }

            JObject jsonBreaches = new JObject
            {
                { "create", new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(DateTime.Now, dateTimeConverter)) },
                { "accounts", jsonAccounts }
            };

            return jsonBreaches;
        }

        /// <summary>
        /// Salva la lista delle violazioni su file
        /// </summary>
        /// <param name="BreachesList">Lista delle violazioni</param>
        /// <param name="FilePath">Percorso del file di destinazione</param>
        public static void SaveToFile(List<AccountInfo> BreachesList, string FilePath)
        {
            try
            {
                JObject jsonBreaches = SaveAsJSON(BreachesList);
                File.WriteAllText(FilePath, jsonBreaches.ToString(), Encoding.UTF8);
            }
            catch(Exception Ex)
            {
                Debug.WriteLine("Unable to save the data to file " + FilePath + ". Error: " + Ex.Message);
                throw new Exception(AppResources.errBreachedSaveFileFailed + " " + Ex.Message);
            }
        }

        /// <summary>
        /// Effettua il parsing del JSON e restituisce la lista degli account violati, con
        /// relative informazioni sulla violazione.
        /// </summary>
        /// <param name="JSON">Json con le informazioni sulle violazioni</param>
        /// <returns>Lista degli account violati letti dal JSON.</returns>
        public static List<AccountInfo> LoadFromJSON(JObject JSON)
        {
            List<AccountInfo> accountInfos = new List<AccountInfo>();
            if (JSON == null)
                throw new Exception("Invalid JSON");

            JArray jsonAccounts = JSON["accounts"].ToObject<JArray>();
            if (jsonAccounts != null)
            {
                foreach (JObject jObject in jsonAccounts)
                {
                    AccountInfo account = new AccountInfo();
                    account.ParseJSON(jObject);
                    accountInfos.Add(account);
                }
            }

            return accountInfos;
        }

        /// <summary>
        /// Restituisce la lista degli account violati, letti da file
        /// </summary>
        /// <param name="FilePath">Percorso del file, in formato JSON, con i dati delle violazioni</param>
        /// <param name="CreationDateTime">Data e ora di creazione del file</param>
        /// <returns>Restituisce la lista degli account violati, con relativi dettagli</returns>
        public static List<AccountInfo> LoadFromFile(string FilePath, out DateTime CreationDateTime)
        {
            CreationDateTime = default(DateTime);

            try
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                    throw new Exception("The file is empty");

                JObject jObject = JObject.Parse(json);
                if (jObject != null)
                {
                    CreationDateTime = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                        jObject["create"].ToString(),
                        dateTimeConverter);
                }
                return LoadFromJSON(jObject);
            }
            catch(Exception Ex)
            {
                Debug.WriteLine("Unable to read the JSON from file " + FilePath + ". Error: " + Ex.Message);
                throw new Exception(AppResources.errBreachedLoadFileFailed + " " + Ex.Message);
            }
        }
    }
}

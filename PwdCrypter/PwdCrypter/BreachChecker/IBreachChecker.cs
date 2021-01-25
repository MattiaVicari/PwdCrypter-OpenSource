using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PwdCrypter.BreachChecker
{
    /// <summary>
    /// Struttura con i risultati della ricerca
    /// Ho usato come riferimento il breach model della documentazione di Have I Been Pwned https://haveibeenpwned.com/API/v2
    /// </summary>
    public class BreachInfo
    {
        static readonly Newtonsoft.Json.Converters.IsoDateTimeConverter dateTimeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter
        {
            DateTimeFormat = "yyyy-MM-ddTH:mm:ss.fffK"
        };

        /// <summary>
        /// A Pascal-cased name representing the breach which is unique across all other breaches. 
        /// This value never changes and may be used to name dependent assets (such as images) 
        /// but should not be shown directly to end users (see the "Title" attribute instead).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A descriptive title for the breach suitable for displaying to end users. 
        /// It's unique across all breaches but individual values may change in the future (i.e. if another breach occurs against an organisation already in the system). 
        /// If a stable value is required to reference the breach, refer to the "Name" attribute instead.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The domain of the primary website the breach occurred on. 
        /// This may be used for identifying other assets external systems may have for the site.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// The date (with no time) the breach originally occurred on in ISO 8601 format. 
        /// This is not always accurate — frequently breaches are discovered and reported long after the original incident. 
        /// Use this attribute as a guide only.
        /// </summary>
        public DateTime BreachDate { get; set; }

        /// <summary>
        /// The date and time (precision to the minute) the breach was added to the system in ISO 8601 format.
        /// </summary>
        public DateTime AddedDate { get; set; }

        /// <summary>
        /// The date and time (precision to the minute) the breach was modified in ISO 8601 format. 
        /// This will only differ from the AddedDate attribute if other attributes represented here are changed or data in the breach itself is changed (i.e. additional data is identified and loaded). 
        /// It is always either equal to or greater then the AddedDate attribute, never less than.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// The total number of accounts loaded into the system. 
        /// This is usually less than the total number reported by the media due to duplication or other data integrity issues in the source data.
        /// </summary>
        public int PwnCount { get; set; }

        /// <summary>
        /// Contains an overview of the breach represented in HTML markup. 
        /// The description may include markup such as emphasis and strong tags as well as hyperlinks.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// This attribute describes the nature of the data compromised in the breach and contains an alphabetically ordered string array of impacted data classes.
        /// </summary>
        public List<string> DataClasses { get; }

        /// <summary>
        /// Indicates that the breach is considered unverified. 
        /// An unverified breach may not have been hacked from the indicated website. 
        /// An unverified breach is still loaded into HIBP when there's sufficient confidence that a significant portion of the data is legitimate.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Indicates that the breach is considered fabricated. 
        /// A fabricated breach is unlikely to have been hacked from the indicated website and usually contains a large amount of manufactured data. 
        /// However, it still contains legitimate email addresses and asserts that the account owners were compromised in the alleged breach.
        /// </summary>
        public bool IsFabricated { get; set; }

        /// <summary>
        /// Indicates if the breach is considered sensitive. 
        /// The public API will not return any accounts for a breach flagged as sensitive.
        /// </summary>
        public bool IsSensitive { get; set; }

        /// <summary>
        /// Indicates if the breach has been retired. 
        /// This data has been permanently removed and will not be returned by the API.
        /// </summary>
        public bool IsRetired { get; set; }

        /// <summary>
        /// Indicates if the breach is considered a spam list. 
        /// This flag has no impact on any other attributes but it means that the data has not come as a result of a security compromise.
        /// </summary>
        public bool IsSpamList { get; set; }

        /// <summary>
        /// A URI that specifies where a logo for the breached service can be found. Logos are always in PNG format.
        /// </summary>
        public string LogoPath { get; set; }

        public BreachInfo()
        {
            DataClasses = new List<string>();
        }

        private void ResetData()
        {
            Name = "";
            Title = "";
            Domain = "";
            BreachDate = DateTime.Now;
            AddedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            PwnCount = 0;
            Description = "";
            DataClasses.Clear();
            IsVerified = false;
            IsFabricated = false;
            IsSensitive = false;
            IsRetired = false;
            IsSpamList = false;
            LogoPath = "";
        }

        /// <summary>
        /// Restituisce il contenuto dell'oggetto in formato JSON
        /// </summary>
        /// <returns>Contenuto dell'oggetto in formato JSON</returns>
        public JObject AsJSON()
        {
            JObject json = new JObject
            {
                { nameof(Name), new JValue(Name) },
                { nameof(Title), new JValue(Title) },
                { nameof(Domain), new JValue(Domain) },
                { nameof(BreachDate), new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(BreachDate, dateTimeConverter)) },
                { nameof(AddedDate), new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(AddedDate, dateTimeConverter)) },
                { nameof(ModifiedDate), new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(ModifiedDate, dateTimeConverter)) },
                { nameof(PwnCount), new JValue(PwnCount) },
                { nameof(Description), new JValue(Description) },
                { nameof(DataClasses), new JArray(DataClasses.ToArray()) },
                { nameof(IsVerified), new JValue(IsVerified) },
                { nameof(IsFabricated), new JValue(IsFabricated) },
                { nameof(IsSensitive), new JValue(IsSensitive) },
                { nameof(IsRetired), new JValue(IsRetired) },
                { nameof(IsSpamList), new JValue(IsSpamList) },
                { nameof(LogoPath), new JValue(LogoPath) }
            };
            return json;
        }

        /// <summary>
        /// Effettua il parsing del json e popola la struttura
        /// </summary>
        /// <param name="json">Oggetto JSON di cui fare il parsing</param>
        public void ParseJSON(JObject json)
        {
            ResetData();

            if (Utility.SafeTryGetJSONString(json, nameof(Name), out string value))
                Name = value;
            if (Utility.SafeTryGetJSONString(json, nameof(Title), out value))
                Title = value;
            if (Utility.SafeTryGetJSONString(json, nameof(Domain), out value))
                Domain = value;
            if (Utility.SafeTryGetJSONDeserialize<DateTime>(json, nameof(BreachDate), dateTimeConverter, out DateTime dtValue))
                BreachDate = dtValue;
            if (Utility.SafeTryGetJSONDeserialize<DateTime>(json, nameof(AddedDate), dateTimeConverter, out dtValue))
                AddedDate = dtValue;
            if (Utility.SafeTryGetJSONDeserialize<DateTime>(json, nameof(ModifiedDate), dateTimeConverter, out dtValue))
                ModifiedDate = dtValue;
            if (Utility.SafeTryGetJSONNumber(json, nameof(PwnCount), out double numberValue))
                PwnCount = (int)Math.Truncate(numberValue);
            if (Utility.SafeTryGetJSONString(json, nameof(Description), out value))
                Description = value;
            if (Utility.SafeTryGetJSON(json, nameof(DataClasses), out JToken jsonValue))
            {
                JArray jArray = jsonValue.ToObject<JArray>();
                foreach(JToken token in jArray)
                {
                    DataClasses.Add(token.ToString());
                }
            }
            if (Utility.SafeTryGetJSONBool(json, nameof(IsVerified), out bool bValue))
                IsVerified = bValue;
            if (Utility.SafeTryGetJSONBool(json, nameof(IsFabricated), out bValue))
                IsFabricated = bValue;
            if (Utility.SafeTryGetJSONBool(json, nameof(IsSensitive), out bValue))
                IsSensitive = bValue;
            if (Utility.SafeTryGetJSONBool(json, nameof(IsRetired), out bValue))
                IsRetired = bValue;
            if (Utility.SafeTryGetJSONBool(json, nameof(IsSpamList), out bValue))
                IsSpamList = bValue;
            if (Utility.SafeTryGetJSONString(json, nameof(LogoPath), out value))
                LogoPath = value;
        }
    }


    /// <summary>
    /// Classe che racchiude tutte le violazioni di un account
    /// </summary>
    public class AccountInfo
    {
        /// <summary>
        /// Account
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Lista delle violazioni
        /// </summary>
        public List<BreachInfo> Breaches { get; }

        /// <summary>
        /// Restituisce true se l'account è stato violato, almeno una volta
        /// </summary>
        public bool IsBreached
        {
            get
            {
                return Breaches.Count > 0;
            }
        }

        /// <summary>
        /// Restituisce il numero di violazioni dell'account
        /// </summary>
        public int BreachesCount
        {
            get
            {
                return Breaches.Count;
            }
        }

        public AccountInfo()
        {
            Breaches = new List<BreachInfo>();
        }

        /// <summary>
        /// Restituisce il contenuto dell'oggetto in formato JSON
        /// </summary>
        /// <returns>Contenuto della classe in formato JSON</returns>
        public JObject AsJSON()
        {
            JArray jsonBreaches = new JArray();
            foreach (BreachInfo breachInfo in Breaches)
            {
                jsonBreaches.Add(breachInfo.AsJSON());
            }

            JObject json = new JObject
            {
                { nameof(Account), new JValue(Account) },
                { nameof(Breaches), jsonBreaches }
            };
            return json;
        }

        /// <summary>
        /// Effettua il parsing del JSON e popola la struttura
        /// </summary>
        /// <param name="json">Oggetto JSON di cui fare il parsing</param>
        public void ParseJSON(JObject json)
        {
            Account = "";
            Breaches.Clear();

            if (Utility.SafeTryGetJSONString(json, nameof(Account), out string accountValue))
                Account = accountValue;
            if (Utility.SafeTryGetJSON(json, nameof(Breaches), out JToken breachesValue))
            {
                JArray breachesArray = breachesValue.ToObject<JArray>();
                foreach (JObject breachJson in breachesArray)
                {
                    BreachInfo info = new BreachInfo();
                    info.ParseJSON(breachJson);
                    Breaches.Add(info);
                }
            }
        }
    }

    /// <summary>
    /// Eccezione che si solleva durante l'interrogazione nel database degli account violati
    /// </summary>
    public class WSBreachedAccountException : Exception
    {
        public string Account { get; private set; }

        public WSBreachedAccountException(string account, string message) : base(message)
        {
            Account = account;
        }

        public WSBreachedAccountException(string account, string message, Exception inner)
            : base(message, inner)
        {
            Account = account;
        }

    }


    /// <summary>
    /// Interfaccia per implementare la classe di interrogazione del database degli account
    /// compromessi.
    /// </summary>
    public interface IBreachChecker
    {
        /// <summary>
        /// Verifica se nella lista ci sono account compromessi.
        /// </summary>
        /// <param name="accounts">lista degli account da verificare</param>
        /// <returns>Restituisce true se c'è almeno un account compromesso, false altrimenti.</returns>
        Task<bool> FindBreaches(List<string> accounts);

        /// <summary>
        /// Restituisce i risultati dell'ultima elaborazione.
        /// </summary>
        /// <returns>Lista di oggetti AccountInfo con i risultati del'ultima operazione di ricerca</returns>
        List<AccountInfo> GetResults();

        /// <summary>
        /// Restituisce il percorso del file di cache
        /// </summary>
        /// <returns>Percorso assoluto del file di cache</returns>
        string GetCacheFilePath();
    }
}

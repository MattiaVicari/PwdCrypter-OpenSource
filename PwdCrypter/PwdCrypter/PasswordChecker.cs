using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PwdCrypter
{
    /// <summary>
    /// Tipologie di problemi di sicurezza per le password
    /// </summary>
    public enum PwdIssue
    {
        TooOld,
        TooWeak
    }

    /// <summary>
    /// Class per la verifica delle password
    /// </summary>
    public class PasswordChecker
    {       
        const string CacheFile = "pwdchecker.cache";
        
        public const int MaxDaysAlert = 6 * 30;    // 6 mesi
        public const int MinScoreAlert = 2;        // Punteggio minimo

        private readonly List<PasswordCheckData> PasswordCheckDataList = null;
        private readonly Zxcvbn.Zxcvbn Checker;

        public string CacheFilePath => GetCacheFilePath();
        public DateTime LastCheck { get; private set; }
        public int NumberOfPassword { get; private set; }
        public int NumberOfSkippedPassword { get; private set; }

        public delegate void VerifyEventHandler(int itemId, int itemCount, PwdListItem itemInfo);
        public VerifyEventHandler OnVerify = null;

        public PasswordChecker()
        {
            PasswordCheckDataList = new List<PasswordCheckData>();            
            Checker = new Zxcvbn.Zxcvbn();
        }

        private string GetCacheFilePath()
        {
            string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(appFolder, CacheFile);
            return filePath;
        }

        public void LoadDataFromCache()
        {
            PasswordCheckDataList.Clear();
            try
            {
                string jsonContent = File.ReadAllText(CacheFilePath, Encoding.UTF8);
                JObject json = JObject.Parse(jsonContent);

                LastCheck = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                        json[nameof(LastCheck)].ToString(),
                        PasswordCheckData.dateTimeConverter);
                NumberOfPassword = json[nameof(NumberOfPassword)].ToObject<int>();
                NumberOfSkippedPassword = json[nameof(NumberOfSkippedPassword)].ToObject<int>();

                JArray jArray = json["data"].ToObject<JArray>();
                foreach (JObject jsonData in jArray)
                {
                    PasswordCheckData item = new PasswordCheckData();
                    item.LoadFromJSON(jsonData);
                    PasswordCheckDataList.Add(item);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Unable to load data from the file " + CacheFilePath + ". Error: " + Ex.Message);
                throw new Exception(string.Format(AppResources.errPwdCheckerLoadFileFailed, Ex.Message));
            }
        }

        private bool IsPasswordToOld(PwdListItem pwd)
        {
            return (DateTime.Now - pwd.LastPwdChangeDateTime).TotalDays > MaxDaysAlert;
        }

        private bool IsPasswordTooWeak(Zxcvbn.Result result)
        {            
            return result.Score < MinScoreAlert;
        }

        public async Task VerifyPasswords()
        {
            var accounts = await App.PwdManager.GetPasswordList();
            int itemId = 0, numItems = accounts.Count, numSkip = 0;

            PasswordCheckDataList.Clear();
            foreach (PwdListItem item in accounts)
            {
                try
                {
                    if (item.SkipCheck)
                    {
                        numSkip++;
                        continue;
                    }

                    Zxcvbn.Result result = Checker.EvaluatePassword(item.Password);

                    bool bOld = IsPasswordToOld(item);
                    bool bWeak = IsPasswordTooWeak(result);

                    if (bOld || bWeak)
                    {
                        PasswordCheckData checkData = new PasswordCheckData
                        {
                            PwdId = item.Id,
                            LastChange = item.LastPwdChangeDateTime
                        };

                        if (bOld)
                            checkData.Issues.Add(PwdIssue.TooOld);
                        if (bWeak)
                            checkData.Issues.Add(PwdIssue.TooWeak);

                        checkData.StrengthData.CalcTime = result.CalcTime;
                        checkData.StrengthData.CrackTime = result.CrackTime;
                        checkData.StrengthData.Entropy = Math.Round(result.Entropy) - 1;
                        checkData.StrengthData.Score = result.Score;

                        PasswordCheckDataList.Add(checkData);
                    }
                }
                finally
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        OnVerify?.Invoke(++itemId, numItems, item);
                    });
                }
            }

            LastCheck = DateTime.Now;
            NumberOfPassword = numItems;
            NumberOfSkippedPassword = numSkip;

            SaveCache();
        }

        private void SaveCache()
        {
            try
            {
                JObject json = new JObject
                {
                    { nameof(LastCheck), new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(LastCheck, PasswordCheckData.dateTimeConverter)) },
                    { nameof(NumberOfPassword), new JValue(NumberOfPassword) },
                    { nameof(NumberOfSkippedPassword), new JValue(NumberOfSkippedPassword) }
                };

                JArray jArray = new JArray();
                foreach (PasswordCheckData item in PasswordCheckDataList)
                {
                    jArray.Add(item.SaveToJSON());
                }
                json.Add("data", jArray);
                File.WriteAllText(CacheFilePath, json.ToString(), Encoding.UTF8);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Unable to save to the file " + CacheFilePath + ". Error: " + Ex.Message);
                throw new Exception(string.Format(AppResources.errPwdCheckerSaveFileFailed, Ex.Message));
            }
        }

        public List<PasswordCheckData> GetResult()
        {
            return PasswordCheckDataList;
        }
    }

    /// <summary>
    /// Classe che raccoglie le informazioni sulla verifica di una password
    /// </summary>
    public class PasswordCheckData
    {
        static public readonly Newtonsoft.Json.Converters.IsoDateTimeConverter dateTimeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter
        {
            DateTimeFormat = "yyyy-MM-ddTH:mm:ss.fffK"
        };

        public string PwdId { get; set; }
        public DateTime LastChange { get; set; }
        public string LastChangeDesc { get => LastChange.ToString("dd-MM-yyyy HH:mm:ss"); }
        public PasswordStrengthData StrengthData { get; private set; }

        public List<PwdIssue> Issues { get; private set; }

        public PasswordCheckData()
        {
            StrengthData = new PasswordStrengthData();
            Issues = new List<PwdIssue>();
        }

        public void LoadFromJSON(JObject json)
        {
            PwdId = json[nameof(PwdId)].ToString();
            LastChange = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(
                        json[nameof(LastChange)].ToString(),
                        dateTimeConverter);
            StrengthData.LoadFromJSON(json["strength"].ToObject<JObject>());

            Issues.Clear();
            if (json.ContainsKey(nameof(Issues)))
            {
                JArray jsonIssues = json[nameof(Issues)].ToObject<JArray>();
                if (jsonIssues != null && jsonIssues.Count > 0)
                {                    
                    foreach (JToken token in jsonIssues)
                        Issues.Add(token.ToObject<PwdIssue>());
                }
            }
        }

        public JObject SaveToJSON()
        {
            JObject jObject = new JObject
            {
                { nameof(PwdId), new JValue(PwdId) },
                { nameof(LastChange), new JValue(Newtonsoft.Json.JsonConvert.SerializeObject(LastChange, dateTimeConverter)) },
                { "strength", StrengthData.SaveToJSON() }
            };

            JArray jsonIssues = new JArray();
            foreach (PwdIssue issue in Issues)
            {
                jsonIssues.Add(new JValue(issue));
            }
            jObject.Add(nameof(Issues), jsonIssues);

            return jObject;
        }
    }

    /// <summary>
    /// Classe che raccoglie le informazioni sulla forza della password
    /// </summary>
    public class PasswordStrengthData
    {
        public long CalcTime { get; set; }
        public double CrackTime { get; set; }
        public string CrackTimeDesc { get => GetCrackTimeDesc(); }
        public double Entropy { get; set; }
        public int Score { get; set; } = 0;
        public string ScoreDesc { get => GetScoreDesc(); }

        private string GetScoreDesc()
        {
            string [] PwdStrengthLabel = new []
            {
                AppResources.txtWeak,
                AppResources.txtVeryGuessable,
                AppResources.txtSomewhatGuessable,
                AppResources.txtSafelyUnguessable,
                AppResources.txtVeryUnguessable
            };

            return Score + " (" + PwdStrengthLabel[Math.Min(PwdStrengthLabel.GetUpperBound(0), Score)] + ")";
        }

        public string GetCrackTimeDesc()
        {
            const long minute = 60;
            const long hour = minute * 60;
            const long day = hour * 24;
            const long month = day * 31;
            const long year = month * 12;
            const long century = year * 100;

            if (CrackTime < minute)
                return AppResources.txtInstant;
            if (CrackTime < hour) 
                return string.Format("{0} " + AppResources.txtMinutes, (1 + Math.Ceiling(CrackTime / minute)));
            if (CrackTime < day) 
                return string.Format("{0} " + AppResources.txtHours, (1 + Math.Ceiling(CrackTime / hour)));
            if (CrackTime < month) 
                return string.Format("{0} " + AppResources.txtDays, (1 + Math.Ceiling(CrackTime / day)));
            if (CrackTime < year) 
                return string.Format("{0} " + AppResources.txtMonths, (1 + Math.Ceiling(CrackTime / month)));
            if (CrackTime < century) 
                return string.Format("{0} " + AppResources.txtYears, (1 + Math.Ceiling(CrackTime / year)));
            return AppResources.txtCenturies;
        }

        public void LoadFromJSON(JObject json)
        {
            CalcTime = json[nameof(CalcTime)].ToObject<long>();
            CrackTime = json[nameof(CrackTime)].ToObject<double>();
            Entropy = json[nameof(Entropy)].ToObject<double>();
            Score = json[nameof(Score)].ToObject<int>();
        }

        public JObject SaveToJSON()
        {
            JObject jObject = new JObject
            {
                { nameof(CalcTime), new JValue(CalcTime) },
                { nameof(CrackTime), new JValue(CrackTime) },
                { nameof(Entropy), new JValue(Entropy) },
                { nameof(Score), new JValue(Score) }
            };            

            return jObject;
        }
    }
}

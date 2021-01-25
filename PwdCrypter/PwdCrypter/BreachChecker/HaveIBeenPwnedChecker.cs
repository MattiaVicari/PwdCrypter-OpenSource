using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PwdCrypter.BreachChecker
{
    /// <summary>
    /// Implementazione della classe per interrogare le API di Have I Been Pwned.
    /// Vedi documentazione in https://haveibeenpwned.com/API/v2
    /// </summary>
    public class HaveIBeenPwnedChecker : IBreachChecker
    {
        const string BaseUrl = "https://haveibeenpwned.com/api/v2/breachedaccount/";
        const string CacheFile = "hibp.cache";

        public delegate void BreachCheckerEvent(int itemId, int itemCount, AccountInfo itemInfo);

        /// <summary>
        /// Evento chiamato per ogni account analizzato
        /// </summary>
        public event BreachCheckerEvent OnBreachedAccount;

        private List<AccountInfo> AccountInfos = new List<AccountInfo>();

        public async Task<bool> FindBreaches(List<string> accounts)
        {
            const int MaxRepetition = 10;

            List<string> accountsFailed = new List<string>();
            int itemId = 0, numItems = accounts.Count;
            bool flagBreached = false;
            System.Net.HttpStatusCode lastStatusCode = System.Net.HttpStatusCode.OK;

            AccountInfos.Clear();

            HttpClient AppClient = new HttpClient();
            try
            {
                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Add("User-Agent", string.Format("PwdCrypter/{0}", App.Version));
                AppClient.DefaultRequestHeaders.Add("Accept", "application/vnd.haveibeenpwned.v2+json");

                foreach (string account in accounts)
                {
                    AccountInfo info = new AccountInfo
                    {
                        Account = account
                    };

                    try
                    {
                        int loopCount = 0;
                        bool repeat = false;
                        do
                        {
                            repeat = false;
                            HttpResponseMessage Response = await AppClient.GetAsync(new Uri(BaseUrl + account));
                            lastStatusCode = Response.StatusCode;
                            if (Response.IsSuccessStatusCode)
                            {
                                flagBreached = true;
                                JArray jsonBreachedArray = JArray.Parse(await Response.Content.ReadAsStringAsync());
                                foreach (JObject jsonBreach in jsonBreachedArray)
                                {
                                    info.Breaches.Add(CreateBreachInfo(jsonBreach));
                                }
                                AccountInfos.Add(info);
                            }
                            else if (Response.StatusCode == (System.Net.HttpStatusCode)429)
                            {
                                // Devo attendere un poco di più e ripeto la richiesta
                                int retryAfter = int.Parse(Response.Headers.RetryAfter.ToString());
                                await Task.Delay(100 + (retryAfter * 1000));
                                repeat = true;
                            }
                            else if (Response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                            {
                                throw new Exception("Unable to communicate with the Have I been pwned service. Status: " + Response.StatusCode);
                            }

                            loopCount++;
                        }
                        while (repeat && loopCount < MaxRepetition);
                    }
                    catch (Exception Ex)
                    {
                        accountsFailed.Add(account);
                        Debug.WriteLine("Unable to search breaches for the account " + account + ". Error: " + Ex.Message);
                        if (lastStatusCode == System.Net.HttpStatusCode.Forbidden)
                            throw;
                    }
                    finally
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            OnBreachedAccount?.Invoke(++itemId, numItems, info);
                        });
                    }

                    // Per specifica, attende 2000 ms
                    await Task.Delay(2000);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("FindBreaches: error occurred. Error: " + Ex.Message);
                throw;
            }
            finally
            {
                AppClient.Dispose();
            }

            if (accountsFailed.Count > 0)
                throw new WSBreachedAccountException(string.Join(", ", accountsFailed.ToArray()), AppResources.errAccountsBreachedFailed);

            // Salva il risultato in cache
            try
            {
                SerializeBreaches();
            }
            catch(Exception Ex)
            {
                // Non voglio che sia bloccante
                Debug.WriteLine(Ex.Message);
            }

            return flagBreached;
        }

        private void SerializeBreaches()
        {
            string fileCache = GetCacheFilePath();
            if (File.Exists(fileCache))
                File.Delete(fileCache);
            BreachesSerializer.SaveToFile(AccountInfos, GetCacheFilePath());
        }

        private BreachInfo CreateBreachInfo(JObject jsonData)
        {
            BreachInfo info = new BreachInfo();

            if (Utility.SafeTryGetJSONString(jsonData, "Name", out string dataString))
                info.Name = dataString;
            if (Utility.SafeTryGetJSONString(jsonData, "Title", out dataString))
                info.Title = dataString;
            if (Utility.SafeTryGetJSONString(jsonData, "Domain", out dataString))
                info.Domain = dataString;
            if (Utility.SafeTryGetJSONString(jsonData, "BreachDate", out dataString))
                info.BreachDate = DateTime.Parse(dataString);
            if (Utility.SafeTryGetJSONString(jsonData, "AddedDate", out dataString))
                info.AddedDate = DateTime.Parse(dataString);
            if (Utility.SafeTryGetJSONString(jsonData, "ModifiedDate", out dataString))
                info.ModifiedDate = DateTime.Parse(dataString);
            if (Utility.SafeTryGetJSONNumber(jsonData, "PwnCount", out double dataNumber))
                info.PwnCount = (int)dataNumber;
            if (Utility.SafeTryGetJSONString(jsonData, "Description", out dataString))
                info.Description = dataString;
            if (Utility.SafeTryGetJSONBool(jsonData, "IsVerified", out bool dataBoolean))
                info.IsVerified = dataBoolean;
            if (Utility.SafeTryGetJSONBool(jsonData, "IsFabricated", out dataBoolean))
                info.IsFabricated = dataBoolean;
            if (Utility.SafeTryGetJSONBool(jsonData, "IsSensitive", out dataBoolean))
                info.IsSensitive = dataBoolean;
            if (Utility.SafeTryGetJSONBool(jsonData, "IsRetired", out dataBoolean))
                info.IsRetired = dataBoolean;
            if (Utility.SafeTryGetJSONBool(jsonData, "IsSpamList", out dataBoolean))
                info.IsSpamList = dataBoolean;
            if (Utility.SafeTryGetJSONString(jsonData, "LogoPath", out dataString))
                info.LogoPath = dataString;

            if (jsonData.ContainsKey("DataClasses"))
            {
                JArray jsonClasses = jsonData["DataClasses"].ToObject<JArray>();
                foreach (JToken token in jsonClasses)
                {
                    info.DataClasses.Add(token.ToString());
                }
            }

            return info;
        }

        public List<AccountInfo> GetResults()
        {
            return AccountInfos;
        }

        public string GetCacheFilePath()
        {
            string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(appFolder, CacheFile);
            return filePath;
        }
    }
}

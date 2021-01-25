using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PwdCrypter
{
    /// <summary>
    /// Classe per la gestione della cache dell'App
    /// </summary>
    public class CacheManager
    {
        private const string CurrentVersionSettings = "1.1.0";
        private const string CachePassword1 = @"YOUR_KEY1";
        private const string CachePassword2 = @"YOUR_KEY2";
        private const string CachePassword3 = @"YOUR_KEY3";
        private const int CacheId1 = 0;
        private const int CacheId2 = 0;
        private const int CacheId3 = 0;

        /// <summary>
        /// Percorso del file della cache dell'App
        /// </summary>
        public string AppCacheFileName { get; private set; }

        private readonly string AppCacheFolder;
        private readonly string Password;

        /// <summary>
        /// Lista delle informazioni in cache sui prodotti
        /// </summary>
        public List<ProductCache> ProductsList = new List<ProductCache>();


        public CacheManager()
        {
            AppCacheFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppCacheFileName = Path.Combine(AppCacheFolder, "app.cache");
            Password = GetPassword();
        }

        private string TransformString(string key, int i)
        {
            char[] chars = key.ToCharArray();
            for (int j = 0; j < chars.Length; j++)
                chars[j] = (char)(chars[j] ^ i);
            return string.Join("", chars);    
        }

        private string GetPassword()
        {
            var key1Transform = TransformString(CachePassword1, CacheId1);
            var key2Transform = TransformString(CachePassword2, CacheId2);
            var key3Transform = TransformString(CachePassword3, CacheId3);

            return key1Transform + key2Transform + key3Transform;
        }

        /// <summary>
        /// Confronta due codici di versione
        /// </summary>
        /// <param name="version1">Primo codice di versione</param>
        /// <param name="version2">Secondo codice di versione</param>
        /// <returns>Restituisce 0 se i due codici sono uguali, -1 se il primo codice è precedente al secondo oppure 1 se successivo.</returns>
        private int CompareVersions(string version1, string version2)
        {
            string[] v1 = version1.Split(new char[] { '.' });
            string[] v2 = version2.Split(new char[] { '.' });
            int len = Math.Min(v1.Length, v2.Length);
            for (int i=0; i < len; i++)
            {
                int code1 = int.Parse(v1[i]);
                int code2 = int.Parse(v2[i]);
                if (code1 > code2)
                    return 1;
                else if (code1 < code2)
                    return -1;
            }
            return 0;
        }

        /// <summary>
        /// Legge i dati presenti in cache
        /// </summary>
        /// <returns></returns>
        public async Task ReadCache()
        {
            ProductsList.Clear();

            try
            {
                StreamReader streamReader = new StreamReader(AppCacheFileName);
                string content = await streamReader.ReadToEndAsync();
                streamReader.Close();
                if (string.IsNullOrEmpty(content))
                {
                    await WriteCache();
                    return;
                }

                EncDecHelper enc = new EncDecHelper
                {
                    PasswordString = Password
                };

                string json = enc.AESDecrypt(content, out bool warning);
                if (warning)
                {
                    Debug.WriteLine("CacheManager read: warning in file reading");
                }

                JObject jObject = JObject.Parse(json);
                if (Utility.SafeTryGetJSONString(jObject, "version", out string version))
                {
                    Debug.WriteLine("CacheManager file version " + version);
                }
                if (Utility.SafeTryGetJSON(jObject, "products", out JToken productsToken))
                {
                    JArray products = productsToken.ToObject<JArray>();
                    foreach (JObject item in products)
                    {
                        ProductCache product = new ProductCache
                        {
                            Id = item[nameof(ProductCache.Id)].ToString(),
                            Purchased = item[nameof(ProductCache.Purchased)].ToObject<bool>()
                        };

                        // Dalla versione 1.1.0 ho il codice di sblocco della versioen PLUS
                        if (CompareVersions(version, "1.1.0") >= 0)
                        {
                            product.Unlocked = item[nameof(ProductCache.Unlocked)].ToObject<bool>();
                            product.UnlockCode = item[nameof(ProductCache.UnlockCode)].ToObject<string>();
                            product.UnlockCodeExpire = item[nameof(ProductCache.UnlockCodeExpire)].ToObject<bool>();
                        }

                        ProductsList.Add(product);
                    }
                }
            }
            catch(FileNotFoundException)
            {
                // Ok. Genera il file
                await WriteCache();
            }
            catch(Exception ex)
            {
                Debug.WriteLine("CacheManager error on read: " + ex.Message);
            }
        }

        /// <summary>
        /// Crea o aggiorna il file della cache
        /// </summary>
        public async Task WriteCache()
        {
            try
            {
                JObject json = new JObject
                {
                    { "version", CurrentVersionSettings }
                };

                JArray productsJson = new JArray();
                foreach(ProductCache item in ProductsList)
                {
                    JObject jsonProd = new JObject
                    {
                        { nameof(item.Id), item.Id },
                        { nameof(item.Purchased), item.Purchased },
                        { nameof(item.Unlocked), item.Unlocked },
                        { nameof(item.UnlockCode), item.UnlockCode },
                        { nameof(item.UnlockCodeExpire), item.UnlockCodeExpire }
                    };
                    productsJson.Add(jsonProd);
                }
                json.Add("products", productsJson);

                string content = json.ToString();

                EncDecHelper enc = new EncDecHelper
                {
                    PasswordString = Password
                };

                StreamWriter streamWriter = new StreamWriter(AppCacheFileName, false);
                await streamWriter.WriteAsync(enc.AESEncrypt(content));
                streamWriter.Close();
            }
            catch(Exception ex)
            {
                Debug.WriteLine("CacheManager error on write: " + ex.Message);
            }
        }

        /// <summary>
        /// Aggiorna o aggiunge le informazioni sull'acquisto di un prodotto
        /// </summary>
        /// <param name="productID">Id del prodotto</param>
        /// <param name="purchased">Passare true se il prodotto è stato acquistato, false altrimenti</param>
        /// <param name="saveToFile">Passare true per aggiornare il file della cache appena dopo l'aggiornamento dei dati.</param>
        /// <returns></returns>
        public async Task UpdateProduct(string productID, bool purchased, bool saveToFile=true)
        {
            int index = ProductsList.FindIndex((item) => item.Id == productID);
            if (index >= 0 && index < ProductsList.Count)
                ProductsList[index].Purchased = purchased;
            else
            {
                ProductsList.Add(new ProductCache
                {
                    Id = productID,
                    Purchased = purchased
                });
            }

            if (saveToFile)
                await WriteCache();
        }

        /// <summary>
        /// Verifica se ho l'informazione sul prodotto in cache
        /// </summary>
        /// <param name="productID">Id del prodotto da ricercare</param>
        /// <return>Restituisce l'oggetto con le informazioni sul prodotto oppure null se non viene trovato</return>
        public ProductCache GetProduct(string productID)
        {
            int index = ProductsList.FindIndex((item) => item.Id == productID);
            if (index >= 0 && index < ProductsList.Count)
                return ProductsList[index];
            return null;
        }
    }

    /// <summary>
    /// Classe che rappresenta i dati della cache per un prodotto dell'App
    /// </summary>
    public class ProductCache
    {
        public string Id { get; set; }
        public bool Purchased { get; set; }
        public bool Unlocked { get; set; }
        public string UnlockCode { get; set; }
        public bool UnlockCodeExpire { get; set; }

        public ProductCache()
        {
            Id = "0";
            Purchased = false;
            Unlocked = false;
            UnlockCode = "";
            UnlockCodeExpire = true;
        }
    }
}

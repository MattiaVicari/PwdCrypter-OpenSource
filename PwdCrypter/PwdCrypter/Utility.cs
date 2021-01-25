using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PwdCrypter
{
    public class Utility
    {        
        /// <summary>
        /// Calcola una stringa bound
        /// </summary>
        /// <returns>Bound formato stringa</returns>
        public static string ComputeBound()
        {
            const string Characters = "abcdefghijklmnopqrstuvwxyz_-?òàùèé@#ç°§*.!$£&%ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            string bound = "";
            int maxValue = Characters.Length;
            Random genRandom = new Random();
            for (int i = 0; i < 20; i++)
            {
                int pos = genRandom.Next(1, maxValue);
                bound += Characters.Substring(pos - 1, 1);
            }
            return "--" + bound + "--";
        }

        /// <summary>
        /// Estrae i dati racchiusi da bound e tag
        /// </summary>
        /// <param name="tag">Tag che identifica il tipo di informazione</param>
        /// <param name="bound">Bound che separa le informazioni</param>
        /// <param name="content">Contenuto da cui estrarre i dati</param>
        /// <param name="pos">Restituisce la posizione dell'ultimo carattere estratto da content</param>
        /// <returns>Dati estratti da content</returns>
        public static string ExtractDataWithBound(string tag, string bound, string content, out int pos)
        {
            if (!content.StartsWith(tag))
                throw new Exception("Data is corrupted!");
            
            int startData = -1;
            pos = tag.Length + 2; /* line break "\r\n" */

            string data = content.Substring(pos);
            if (data.StartsWith(bound))
                startData = bound.Length;
            int endData = data.IndexOf(bound, startData + 1);
            if (startData < 0 || endData < 0)
                throw new Exception("Data is corrupted! Bounds are wrong.");

            pos += endData + bound.Length;
            return data.Substring(startData, endData - startData);
        }

        #region EnumUtility
        /// <summary>
        /// Effettua il parsing di una stringa per ottenere il corrispettivo valore Enum
        /// </summary>
        /// <typeparam name="TEnum">Classe Enum</typeparam>
        /// <param name="value">Valore formato stringa</param>
        /// <param name="defaultValue">Valore di default in caso di valore non valido</param>
        /// <param name="resultValue">Valore TEnum derivato dal parsing</param>
        /// <returns>Restituisce true se il parsing ha avuto successo, false altrimenti.
        /// In caso di fallimento, viene restituito il valore di default indicato in defaultValue.</returns>
        static public bool TryParseEnum<TEnum>(string value, TEnum defaultValue, out TEnum resultValue) where TEnum : struct
        {
            // where TEnum: struct indica che TEnum è una struct e non una classe
            resultValue = defaultValue;
            try
            {
                if (value.Length > 0 && Enum.TryParse(value, true, out resultValue))
                    return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Parse enum error: " + ex.Message);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Classe che fornisce funzioni di utility per i tipi enumerati
        /// </summary>
        public class EnumHelper
        {
            /// <summary>
            /// Restituisce la descrizione in lingua per il tipo enumerato
            /// </summary>
            /// <param name="enumValue">Valore del tipo enumerato</param>
            /// <returns>Descrizione in lingua del valore del tipo enumerato passato come parametro</returns>
            public static string GetDescription<TEnum>(TEnum enumValue) where TEnum : struct
            {
                return AppResources.ResourceManager.GetString("txt" + typeof(TEnum).Name + Enum.GetName(typeof(TEnum), enumValue),
                                                                AppResources.Culture);
            }

            /// <summary>
            /// Restituisce la lista di tutti i valori del tipo enumerato
            /// </summary>
            /// <typeparam name="T">Classe Enum del tipo enumerato</typeparam>
            /// <returns>Interfaccia IEnumerable per iterare sulla lista dei valori del tipo enumerato</returns>
            public static IEnumerable<T> GetValues<T>()
            {
                return Enum.GetValues(typeof(T)).Cast<T>();
            }
        }
        #endregion

        #region JSONUtility
        /// <summary>
        /// Restituisce in modo sicuro il valore richiesto nell'oggetto JSON.
        /// </summary>
        /// <param name="jObject">Oggetto JSON in cui cercare</param>
        /// <param name="jsonValueName">Nome della proprietà di cui restituire il valore</param>
        /// <param name="Result">Valore della proprietà</param>
        /// <returns>Restituisce true se l'operazione ha successo, false altrimenti</returns>
        static public bool SafeTryGetJSON(JObject jObject, string jsonValueName, out JToken Result)
        {
            Result = null;
            if (jObject == null)
                return false;
            if (!jObject.ContainsKey(jsonValueName))
                return false;
            return jObject.TryGetValue(jsonValueName, out Result);
        }

        /// <summary>
        /// Restituisce in modo sicuro il valore double da un oggetto JSON
        /// </summary>
        /// <param name="jObject">Oggetto JSON</param>
        /// <param name="jsonValueName">Nome della proprietà di cui restituire il valore</param>
        /// <param name="Result">Valore della proprietà</param>
        /// <returns>Restituisce true se l'operazione ha successo, false altrimenti</returns>
        static public bool SafeTryGetJSONNumber(JObject jObject, string jsonValueName, out double Result)
        {
            Result = 0;
            if (SafeTryGetJSON(jObject, jsonValueName, out JToken jToken))
            {
                if (double.TryParse(jToken.ToString(), out Result))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Restituisce in modo sicuro il valore bool da un oggetto JSON
        /// </summary>
        /// <param name="jObject">Oggetto JSON</param>
        /// <param name="jsonValueName">Nome della proprietà di cui restituire il valore</param>
        /// <param name="Result">Valore della proprietà</param>
        /// <returns>Restituisce true se l'operazione ha successo, false altrimenti</returns>
        static public bool SafeTryGetJSONBool(JObject jObject, string jsonValueName, out bool Result)
        {
            Result = false;
            if (SafeTryGetJSON(jObject, jsonValueName, out JToken jToken))
            {
                if (bool.TryParse(jToken.ToString(), out Result))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Restituisce in modo sicuro il valore stringa da un oggetto JSON
        /// </summary>
        /// <param name="jObject">Oggetto JSON</param>
        /// <param name="jsonValueName">Nome della proprietà di cui restituire il valore</param>
        /// <param name="Result">Valore della proprietà</param>
        /// <returns>Restituisce true se l'operazione ha successo, false altrimenti</returns>
        static public bool SafeTryGetJSONString(JObject jObject, string jsonValueName, out string Result)
        {
            Result = "";
            if (SafeTryGetJSON(jObject, jsonValueName, out JToken jToken))
            {
                Result = jToken.ToString();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Deserializzazione sicura di un oggetto JSON
        /// </summary>
        /// <param name="jObject">Oggetto JSON</param>
        /// <param name="jsonValueName">Nome della proprietà di cui restituisce il valore</param>
        /// <param name="settings">Impostazioni per la deserializzazione</param>
        /// <param name="Result">Valore della proprietà</param>
        /// <returns>Restituisce true se l'operazione ha successo, false altrimenti</returns>
        static public bool SafeTryGetJSONDeserialize(JObject jObject, string jsonValueName, Newtonsoft.Json.JsonSerializerSettings settings, out object Result)
        {
            Result = default;
            if (jObject == null)
                return false;
            if (!jObject.ContainsKey(jsonValueName))
                return false;
           
            try
            {
                Result = Newtonsoft.Json.JsonConvert.DeserializeObject(jObject[jsonValueName].ToString(), settings);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Deserializzazione sicura di un oggetto JSON
        /// </summary>
        /// <param name="jObject">Oggetto JSON</param>
        /// <param name="jsonValueName">Nome della proprietà di cui restituisce il valore</param>
        /// <param name="settings">Impostazioni per la deserializzazione</param>
        /// <param name="Result">Valore della proprietà</param>
        /// <returns>Restituisce true se l'operazione ha successo, false altrimenti</returns>
        static public bool SafeTryGetJSONDeserialize<T>(JObject jObject, string jsonValueName, Newtonsoft.Json.JsonConverter settings, out T Result)
        {
            Result = default;
            if (jObject == null)
                return false;
            if (!jObject.ContainsKey(jsonValueName))
                return false;

            try
            {
                Result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jObject[jsonValueName].ToString(), settings);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica se il token è null o vuoto
        /// </summary>
        /// <param name="token">Token da verificare</param>
        /// <returns>True se il token è null o vuoto, false altrimenti</returns>
        public static bool IsJTokenNullOrEmpty(JToken token)
        {
            return (token == null) ||
               (token.Type == JTokenType.Array && !token.HasValues) ||
               (token.Type == JTokenType.Object && !token.HasValues) ||
               (token.Type == JTokenType.String && token.ToString() == string.Empty) ||
               (token.Type == JTokenType.Null) ||
               (token.ToString() == "null");
        }
        #endregion

        /// <summary>
        /// Visualizza la pagina modale di attesa per operazione in corso
        /// </summary>
        /// <param name="ParentPage">Pagina che necessita dell'attesa</param>
        /// <returns>Oggetto della pagina di attesa</returns>
        static public async Task<WaitPage> StartWait(Page ParentPage)
        {
            WaitPage waitPage = new WaitPage
            {
                BindingContext = new { Message = AppResources.txtPleaseWait }
            };

            await ParentPage.Navigation.PushModalAsync(waitPage, true);
            return waitPage;
        }

        /// <summary>
        /// Termina l'attesa e chiude la pagina di attesa.
        /// </summary>
        /// <param name="waitPage">Oggetto della pagina di attesa restituito da StartWait</param>
        /// <returns></returns>
        static public async Task StopWait(WaitPage waitPage)
        {
            await waitPage.Close();
        }


        /// <summary>
        /// Oggetto con le informazioni sulla pagina di ritorno
        /// </summary>
        public class RedirectData
        {
            public Page RedirectTo { get; set; }
            public bool Modal { get; set; }
            public string MessageText { get; set; }

            public RedirectData()
            {
                RedirectTo = null;
                Modal = true;
            }
        }

        /// <summary>
        /// Sposta uno specifico file in una nuova posizione.
        /// </summary>
        /// <param name="sourceFileName">Percorso di origine</param>
        /// <param name="destFileName">Percorso di destinazione</param>
        /// <param name="overwrite">True per sovrascrivere il file di destinazione se esiste già. Se false e il file
        /// di destinazione è già esistente, verrà sollevata un'eccezione.</param>
        public static void MoveFile(string sourceFileName, string destFileName, bool overwrite)
        {
            if (overwrite && File.Exists(destFileName))
                File.Delete(destFileName);
            File.Move(sourceFileName, destFileName);
        }

        /// <summary>
        /// Converte una stringa (UTF8) nel rispettivo array di byte.
        /// </summary>
        /// <param name="dataString">Stringa da convertire</param>
        /// <returns>Array di byte corrispondente alla stringa passata in input</returns>
        public static byte[] GetBytesFromString(string dataString)
        {            
            byte[] data;
            if (Device.RuntimePlatform == Device.UWP)
                data = System.Text.Encoding.UTF8.GetBytes(dataString);
            else
            {
                System.Text.Encoder enc = System.Text.Encoding.UTF8.GetEncoder();
                char[] chars = dataString.ToCharArray();
                data = new byte[enc.GetByteCount(chars, chars.GetLowerBound(0), chars.Length, false)];
                enc.GetBytes(chars, chars.GetLowerBound(0), chars.Length, data, 0, false);
            }
            return data;
        }

        private static bool HasBOM(byte[] data, out int BOMLen)
        {
            // Scarta i caratteri del BOM
            /*
             * UTF-32, big-endian	    00 00 FE FF
             * UTF-32, little-endian	FF FE 00 00
             * UTF-16, big-endian	    FE FF
             * UTF-16, little-endian	FF FE
             * UTF-8	                EF BB BF
            */
            BOMLen = 0;
            if (data.Length >= 4 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0xFE && data[3] == 0xFF)
            {
                BOMLen = 4;
                return true;
            }
            if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xFE && data[2] == 0x00 && data[3] == 0x00)
            {
                BOMLen = 4;
                return true;
            }
            if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
            {
                BOMLen = 2;
                return true;
            }
            if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
            {
                BOMLen = 2;
                return true;
            }
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                BOMLen = 3;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sterilizza una array di byte considerando solo numeri e lettere dell'alfabeto.
        /// </summary>
        /// <param name="data">Array di byte da sterilizzare</param>
        /// <returns>Array sterilizzato</returns>
        public static byte[] SanitizeData(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            bool isBOM = HasBOM(data, out int BOMLen);
            int count = data.Length;
            List<byte> buffer = new List<byte>();
            for (int i = 0; i < count; i++)
            {
                if (data[i] >= 32 && ((isBOM && data[i] != 0x00
                    && data[i] != 0xFF && data[i] != 0xFE
                    && data[i] != 0xEF && data[i] != 0xBB && i <= BOMLen
                    && data[i] != 0xBF) || i > BOMLen || !isBOM))
                {
                    buffer.Add(data[i]);
                }
            }
            return buffer.ToArray();
        }

        /// <summary>
        /// Restituisce la chiave partendo dalle coppie di chiavi e id.
        /// </summary>
        /// <param name="key">Chiavi</param>
        /// <param name="id">ID delle chiavi</param>
        /// <returns>Chiave risultante</returns>
        public static string GetSecretKey(string []key, int []id)
        {
            if (key.Length != id.Length)
                throw new Exception("Key array doesn't have the same lenght of the id array.");

            string resultKey = "";
            for (int i=0; i < key.Length; i++)
            {
                char[] chars = key[i].ToCharArray();
                for (int j = 0; j < chars.Length; j++)
                    chars[j] = (char)(chars[j] ^ (id[i]));
                resultKey += string.Join("", chars);
            }
            return resultKey;
        }
    }
}

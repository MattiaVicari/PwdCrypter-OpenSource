using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xamarin.Forms;

namespace PwdCrypter
{
    public class EncDecHelper
    {
        public byte[] Password { get; set; }
        public string PasswordString { get => Encoding.UTF8.GetString(Password); set => Password = Encoding.UTF8.GetBytes(value); }

        public EncDecHelper()
        {
            Password = new byte[] { };
        }

        private byte[] AdaptPassword(byte[] password, int keyLength)
        {
            if (password.Length > keyLength)
                throw new Exception($"The key length is upper than {keyLength}");

            byte[] bufferKey = new byte[keyLength];
            for (int i = 0; i < bufferKey.Length; i++)
                bufferKey[i] = (i < password.Length) ? password[i] : byte.Parse("0");

            return bufferKey;
        }

        /// <summary>
        /// Codifica con triplo DES, PKCS7.
        /// ATTENZIONE: non è consigliabile utilizzare l'algoritmo DES in quanto presenta delle falle.
        /// https://it.wikipedia.org/wiki/Data_Encryption_Standard
        /// </summary>
        /// <param name="content">Testo da codificare</param>
        /// <returns>Testo codificato</returns>
        public string DESEncrypt(string content)
        {
            ITripleDESHelper des = DependencyService.Get<ITripleDESHelper>();
            if (des == null)
                throw new Exception("Interface non found");
            
            des.Password = Encoding.UTF8.GetString(Password);
            return des.Encrypt(content);
        }

        /// <summary>
        /// Decodifica con triplo DES, PKCS7
        /// </summary>
        /// <param name="content">Testo da decodificare</param>
        /// <returns>Testo decodificato</returns>
        public string DESDecrypt(string content)
        {
            ITripleDESHelper des = DependencyService.Get<ITripleDESHelper>();
            if (des == null)
                throw new Exception("Interface non found");

            des.Password = Encoding.UTF8.GetString(Password);
            return des.Decrypt(content);
        }

        /// <summary>
        /// Codifica con AES
        /// </summary>
        /// <param name="content">Testo da codificare</param>
        /// <returns>Testo codificato</returns>
        public string AESEncrypt(string content)
        {
            Aes aes = Aes.Create();

            byte[] bufferKey = AdaptPassword(Password, aes.Key.Length);

            // Codifica
            MemoryStream contentStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(contentStream, aes.CreateEncryptor(bufferKey, aes.IV), CryptoStreamMode.Write);
            StreamWriter streamWriter = new StreamWriter(cryptoStream, Encoding.UTF8);
            streamWriter.Write(content);
            streamWriter.Close();

            string encrypted = Convert.ToBase64String(contentStream.ToArray(), Base64FormattingOptions.None);
            encrypted += ";" + Convert.ToBase64String(aes.IV);  // Metto l'IV in fondo

            return encrypted;
        }

        /// <summary>
        /// Decodifica con AES
        /// </summary>
        /// <param name="content">Testo da decodificare</param>
        /// <param name="warning">Vale true se il contenuto è stato decifrato ma con warning</param>
        /// <returns>Testo decodificato</returns>
        public string AESDecrypt(string content, out bool warning)
        {
            warning = false;
            Aes aes = Aes.Create();

            byte[] bufferKey = AdaptPassword(Password, aes.Key.Length);

            /* 
             * ## Corretta SaveFile di CrossPlatformSpecialFolder per UWP che non sovrascriveva il file completamente.
             * Lascio i workaround per permettere di importare i file list esportati prima della correzione
             */
            // WA.1: Verifica se il file è integro
            string fileContent = content;
            int ivCount = 0, pos = -1;
            while ((pos=content.IndexOf(";", pos + 1)) != -1) { ivCount++; }
            if (ivCount == 0)
                throw new Exception("Content is invalid");
            if (ivCount > 1)
            {
                // Prova a bonificare
                pos = content.IndexOf(";");
                int endIv = content.IndexOf("==", pos + 1);
                if (endIv < 0)
                    throw new Exception("Content is corrupted");

                fileContent = content.Substring(0, endIv + 2);
                warning = true;
                Debug.WriteLine("Decrypted with warning");
            }
            // --

            // Legge l'initialization vector se è nel testo da decodificare
            if (fileContent.IndexOf(';') <= 0)
                throw new Exception("Initialization vector IV not found");

            string iv = fileContent.Substring(fileContent.IndexOf(';') + 1, fileContent.Length - fileContent.IndexOf(';') - 1);
            // WA.2: Verifica doppio IV in coda al file. Prendo il primo.
            ivCount = 0; 
            pos = -1;
            while ((pos = iv.IndexOf("==", pos + 1)) != -1) { ivCount++; }
            if (ivCount > 1)
            {
                int startIV = iv.IndexOf("==");
                iv = iv.Substring(0, startIV + 2);
            }
            // --

            byte[] bufferIV = Convert.FromBase64String(iv);
            string contentToDecrypt = fileContent.Substring(0, fileContent.IndexOf(';'));    

            // Trasforma il testo da decodificare in buffer
            byte[] bufferContent = Convert.FromBase64String(contentToDecrypt);

            // Decodifica
            MemoryStream contentStream = new MemoryStream(bufferContent);
            try
            {
                CryptoStream cryptoStream = new CryptoStream(contentStream, aes.CreateDecryptor(bufferKey, bufferIV), CryptoStreamMode.Read);
                try
                {
                    StreamReader streamReader = new StreamReader(cryptoStream, Encoding.UTF8);
                    try
                    {
                        string decrypted = streamReader.ReadToEnd();
                        return decrypted;
                    }
                    finally
                    {
                        streamReader.Close();
                    }
                }
                finally
                {
                    cryptoStream.Close();
                }
            }
            finally
            {
                contentStream.Close();
            }
        }

        /// <summary>
        /// Restituisce l'hash della stringa in input, calcolato con SHA256
        /// </summary>
        /// <param name="value">Stringa di cui calcolare l'hash</param>
        /// <returns>Hash calcolato in SHA256</returns>
        public static string SHA256(string value)
        {
            SHA256Managed sha256 = new SHA256Managed();
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            byte[] hash = sha256.ComputeHash(buffer);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Restituisce l'hash della stringa in input, calcolato con SHA256
        /// </summary>
        /// <param name="value">Stringa di cui calcolare l'hash</param>
        /// <returns>Hash calcolato in SHA256</returns>
        public static byte[] SHA256Bytes(string value)
        {
            SHA256Managed sha256 = new SHA256Managed();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(value);
            return sha256.ComputeHash(buffer);
        }

        /// <summary>
        /// Restituisce l'hash MD5 dello stream passato in input.
        /// </summary>
        /// <param name="inputStream">Stream di input con i byte di cui calcolare l'hash</param>
        /// <returns>Hash calcolato in MD5</returns>
        public static string MD5(Stream inputStream)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            long oldPosition = inputStream.Position;

            inputStream.Position = 0;
            byte[] hash = md5.ComputeHash(inputStream);
            inputStream.Position = oldPosition;

            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Restituisce l'hash MD5 dello stream passato in input.
        /// </summary>
        /// <param name="inputData">byte di cui calcolare l'hash</param>
        /// <returns>Hash calcolato in MD5</returns>
        public static string MD5(byte[] inputData)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(inputData);
            return Convert.ToBase64String(hash);
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Droid.TripleDESHelper))]
namespace PwdCrypter.Droid
{
    class TripleDESHelper : ITripleDESHelper
    {
        public string Password { get; set; }

        private byte[] AdaptPassword(string password, int keyLength)
        {
            byte[] bufferPassword = Encoding.UTF8.GetBytes(Password);
            if (bufferPassword.Length > keyLength)
            {
                throw new Exception($"The key length is upper than {keyLength}");
            }

            byte[] bufferKey = new byte[keyLength];
            for (int i = 0; i < bufferKey.Length; i++)
                bufferKey[i] = (i < bufferPassword.Length) ? bufferPassword[i] : byte.Parse("0");

            return bufferKey;
        }

        /// <summary>
        /// Decodifica con triplo DES
        /// </summary>
        /// <param name="content">Testo da decodificare</param>
        /// <returns>Testo decodificato</returns>
        public string Decrypt(string content)
        {
            string decrypted = "";
            TripleDES des = TripleDES.Create();
            des.Padding = PaddingMode.PKCS7;
            des.Mode = CipherMode.ECB;

            // La chiave per il triple DES
            byte[] bufferKey = AdaptPassword(Password, des.Key.Length);

            // Legge l'initialization vector se è nel testo da decodificare
            string contentToDecrypt = content;
            byte[] bufferIV = null;
            if (content.IndexOf(';') > 0)
            {
                string iv = content.Substring(content.IndexOf(';') + 1, content.Length - content.IndexOf(';') - 1);
                bufferIV = Convert.FromBase64String(iv);
                contentToDecrypt = content.Substring(0, content.IndexOf(';'));
            }

            // Trasforma il testo da decodificare in buffer
            byte[] bufferContent = Convert.FromBase64String(contentToDecrypt);

            // Decodifica
            MemoryStream contentStream = new MemoryStream(bufferContent);
            try
            {
                CryptoStream cryptoStream = new CryptoStream(contentStream, des.CreateDecryptor(bufferKey, bufferIV), CryptoStreamMode.Read);
                try
                {
                    StreamReader streamReader = new StreamReader(cryptoStream, Encoding.UTF8);
                    try
                    {
                        decrypted = streamReader.ReadToEnd();
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
        /// Codifica con triplo DES
        /// </summary>
        /// <param name="content">Testo da codificare</param>
        /// <returns>Testo codificato</returns>
        public string Encrypt(string content)
        {
            string encrypted = "";
            TripleDES des = TripleDES.Create();
            des.Padding = PaddingMode.PKCS7;
            des.Mode = CipherMode.ECB;

            // La chiave per il triple DES
            byte[] bufferKey = AdaptPassword(Password, des.Key.Length);

            // Codifica
            MemoryStream contentStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(contentStream, des.CreateEncryptor(bufferKey, null), CryptoStreamMode.Write);
            StreamWriter streamWriter = new StreamWriter(cryptoStream, Encoding.UTF8);
            streamWriter.Write(content);
            streamWriter.Close();

            encrypted = Convert.ToBase64String(contentStream.ToArray(), Base64FormattingOptions.None);
            encrypted += ";" + Convert.ToBase64String(des.IV);  // Metto l'IV in fondo

            return encrypted;
        }
    }
}
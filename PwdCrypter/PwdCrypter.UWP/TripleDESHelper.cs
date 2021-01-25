using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.UWP.TripleDESHelper))]
namespace PwdCrypter.UWP
{
    class TripleDESHelper : ITripleDESHelper
    {
        public string Password { get; set; }

        private string ApplyPadding(string password)
        {
            string pwd = password;

            // Caratteri di riepimento per arrivare a 56 bit (triplo DES)
            byte[] bufferKey = Encoding.UTF8.GetBytes(pwd);
            if (bufferKey.Length % 56 > 0)
            {
                long extra = 56 - (bufferKey.Length % 56);
                if (extra > 0)
                {
                    byte[] array = new byte[bufferKey.Length + extra];
                    for (short i = 0; i < bufferKey.Length; i++)
                        array[i] = bufferKey[i];
                    for (short i = 0; i < extra; i++)
                        array[bufferKey.Length + i] = 48;
                    pwd = Encoding.UTF8.GetString(array);
                }
            }

            return pwd;
        }

        public string Decrypt(string content)
        {
            // Nome dell'algoritmo da utilizzare per ottenere il provider
            // Triplo DES con pacchetti da 7
            string algName = SymmetricAlgorithmNames.TripleDesEcbPkcs7;

            // Ottiene il provider
            SymmetricKeyAlgorithmProvider keyProv = SymmetricKeyAlgorithmProvider.OpenAlgorithm(algName);

            // Ottiene la password in formato buffer
            IBuffer bufferKey = CryptographicBuffer.ConvertStringToBinary(ApplyPadding(Password), BinaryStringEncoding.Utf8);
            // La chiave per il triple DES deve esseere da 56 bit (56*3 = 168 bit).
            if (bufferKey.Length % 56 != 0)
            {
                throw new Exception("The key lenght is not a multiple of 56 bit.");
            }

            // Ottiene la chiave
            CryptographicKey key = keyProv.CreateSymmetricKey(bufferKey);

            // Ottiene dalla stringa il buffer del testo da decriptare
            IBuffer dataText = CryptographicBuffer.DecodeFromHexString(content);

            string text = "";
            if (dataText != null && dataText.Length > 0)
            {
                // Decripta
                IBuffer decriptText = CryptographicEngine.Decrypt(key, dataText, null);

                // Trasforma il testo da IBuffer a esadecimale.
                text = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decriptText);
            }
            return text;
        }

        public string Encrypt(string content)
        {
            // Nome dell'algoritmo da utilizzare per ottenere il provider
            // Triplo DES con pacchetti da 7!
            string algName = SymmetricAlgorithmNames.TripleDesEcbPkcs7;

            // Ottiene il provider
            SymmetricKeyAlgorithmProvider keyProv = SymmetricKeyAlgorithmProvider.OpenAlgorithm(algName);

            // Ottiene la password in formato buffer
            IBuffer bufferKey = CryptographicBuffer.ConvertStringToBinary(ApplyPadding(Password), BinaryStringEncoding.Utf8);
            // La chiave per il triple DES deve esseere da 56 bit (56*3 = 168).
            if (bufferKey.Length % 56 != 0)
            {
                throw new Exception("The key lenght is not a multiple of 56 bit.");
            }

            // Ottiene la chiave
            CryptographicKey key = keyProv.CreateSymmetricKey(bufferKey);

            // Testo da criptare in formato buffer
            IBuffer dataText = CryptographicBuffer.ConvertStringToBinary(content, BinaryStringEncoding.Utf8);

            // Determina se la lunghezza del messagio è multipla della lunghezza dei blocchi.
            // Non è necessario per gli algoritmi con PKCS da 7 perché aggiungo in automatico dei caratteri
            // di riempimento nel messaggio per ottenere la lunghezza corretta.
            if (!algName.Contains("PKCS7") && dataText != null)
            {
                if ((dataText.Length % keyProv.BlockLength) != 0)
                {
                    throw new Exception("The message length is not a multiple of the block size.");
                }
            }

            // Vettore iv in supporto di alcuni algoritmi
            IBuffer iv = null;

            // Gli algoritmi CBC richiedonon un vettore di inizializazione. Utilizzo
            // un numero random per il vettore.
            if (algName.Contains("CBC"))
            {
                iv = CryptographicBuffer.GenerateRandom(keyProv.BlockLength);
            }

            string hexText = "";
            if (dataText != null && dataText.Length > 0)
            {
                IBuffer encryptText = CryptographicEngine.Encrypt(key, dataText, iv);

                // Riporta il testo criptato in esadecimale
                hexText = CryptographicBuffer.EncodeToHexString(encryptText);
            }

            return hexText;
        }
    }
}

using System;
using System.Security.Cryptography;
using Xamarin.Forms;
using Wiry.Base32;

[assembly: Dependency(typeof(PwdCrypter.UWP.TwoFactorAuth))]
namespace PwdCrypter.UWP
{
    class TwoFactorAuth : ITwoFactorAuth
    {
        /// <summary>
        /// Restituisce il codice di backup da utilizzare in caso non sia possibile
        /// generare il token temporale.
        /// </summary>
        /// <returns>Codice di backup per l'accesso</returns>
        public string GetBackupCode()
        {
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
            byte[] data = new byte[20];
            rngCsp.GetBytes(data);

            string hash = EncDecHelper.MD5(data);
            string code = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(hash)).ToUpper();
            for (int i = 1; i < 4; i++)
            {
                code = code.Insert(i * 8 + (i - 1), "-");
            }
            return code;
        }

        /// <summary>
        /// Calcola l'URL del QR code nel formato utilizzato per l'App Google Authenticator
        /// </summary>
        /// <param name="secret">codice secreto</param>
        /// <param name="account">account da visualizzare nell'applicazione (es: e-mail, username)</param>
        /// <param name="issuer">nome dell'azienda (es: Microsoft)</param>
        /// <returns>URL da utilizzare per il QR code per la configurazione dell'App di autenticazione</returns>
        public string GetQRCodeURL(string secret, string account, string issuer)
        {
            return "otpauth://totp/"
                    + Uri.EscapeUriString(issuer + ":" + account).Replace("+", "%20")
                    + "?secret=" + Uri.EscapeUriString(secret).Replace("+", "%20")
                    + "&issuer=" + Uri.EscapeUriString(issuer).Replace("+", "%20");
        }

        /// <summary>
        /// Calcola il codice segreto
        /// </summary>
        /// <returns>Codice secreto</returns>
        public string GetSecret()
        {
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
            byte[] data = new byte[20];
            rngCsp.GetBytes(data);
            return Base32Encoding.Standard.GetString(data);
        }

        /// <summary>
        /// Restituisce il TOTP in base al secret.
        /// </summary>
        /// <param name="secret">Codice segreto</param>
        /// <returns>TOTP</returns>
        public string GetTOTP(string secret)
        {
            byte[] data = Base32Encoding.Standard.ToBytes(secret);
            OtpNet.Totp totp = new OtpNet.Totp(data);
            return totp.ComputeTotp(DateTime.UtcNow);
        }
    }
}

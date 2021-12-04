using System;
using System.Threading.Tasks;
using Xamarin.Auth;

namespace PwdCrypter
{
    public delegate void EventOnOAuth2Completed(object sender, AuthenticatorCompletedEventArgs e);
    public delegate void EventOnOAuth2Error(object sender, AuthenticatorErrorEventArgs e);

    /// <summary>
    /// Interfaccia per l'autenticazione tramite OAuth2
    /// </summary>
    public interface IOAuth2Authenticator
    {
        void StartAuthentication(string clientId,
                                 string scope,
                                 string appSecret,
                                 Uri authorizeUrl,
                                 Uri redirectUrl,
                                 Uri tokenUrl,
                                 bool codeChallengeMethod = false,
                                 bool useNativeUI = false);

        Task<string> RefreshToken(OAuthAccountWrapper account, 
                                  string clientId, 
                                  Uri redirectUrl, 
                                  string appSecret, 
                                  Uri refreshTokenUrl);

        event EventOnOAuth2Completed Completed;
        event EventOnOAuth2Error Error;
    }


    /// <summary>
    /// Classe wrapper sugli oggetti di tipo Xamarin.Auth.Account
    /// </summary>
    public class OAuthAccountWrapper
    {
        private Account AuthAccount = null;     // Account sorgente

        public string AccessToken { get; set; } = "";               // Token
        public string RefreshToken { get; set; } = "";              // Token per aggiornare l'access token
        public string Scope { get; set; } = "";                     // Scope
        public DateTime DateToken { get; set; } = DateTime.Now;     // Quando è stato ottenuto il token
        public long ExpiresIn { get; set; } = 0;                    // Duranta del token

        // Account sorgente
        public Account OAuthAccount
        {
            get => AuthAccount;
            set => SetAccount(value);
        }

        private void SetAccount(Account account)
        {
            Reset();

            AuthAccount = account;
            ReadPropertyValues();
        }

        private void ReadPropertyValues()
        {
            if (AuthAccount == null)
                return;

            if (AuthAccount.Properties.ContainsKey("access_token"))
                AccessToken = AuthAccount.Properties["access_token"];
            if (AuthAccount.Properties.ContainsKey("refresh_token"))
                RefreshToken = AuthAccount.Properties["refresh_token"];
            if (AuthAccount.Properties.ContainsKey("scope"))
                Scope = AuthAccount.Properties["scope"];
            if (AuthAccount.Properties.ContainsKey("expires_in"))
                ExpiresIn = long.Parse(AuthAccount.Properties["expires_in"]);
            if (AuthAccount.Properties.ContainsKey("date_token"))
                DateToken = DateTime.ParseExact(AuthAccount.Properties["date_token"], "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        private void Reset()
        {
            AuthAccount = null;

            AccessToken = "";
            RefreshToken = "";
            Scope = "";
            ExpiresIn = 0;
            DateToken = DateTime.Now;
        }

        /// <summary>
        /// Costruttore
        /// </summary>
        /// <param name="account">Xamarin Account sorgente</param>
        public OAuthAccountWrapper(Account account)
        {
            OAuthAccount = account;
        }

        /// <summary>
        /// Rilegge i parametri dall'oggetto Account
        /// </summary>
        public void Refresh()
        {
            ReadPropertyValues();
        }
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Auth;
using Xamarin.Forms;


[assembly: Dependency(typeof(PwdCrypter.Droid.OAuth2Authenticator))]
namespace PwdCrypter.Droid
{
    /// <summary>
    /// Classe che implementa l'autenticazione con OAuth2
    /// </summary>
    public class OAuth2Authenticator : IOAuth2Authenticator
    {
        public event EventOnOAuth2Completed Completed;
        public event EventOnOAuth2Error Error;

        public void StartAuthentication(string clientId, string scope, string appSecret, Uri authorizeUrl, Uri redirectUrl, Uri tokenUrl, bool codeChallengeMethod)
        {
            Xamarin.Auth.OAuth2Authenticator oAuth2 = new Xamarin.Auth.OAuth2Authenticator(clientId,
                                                                                           appSecret,
                                                                                           scope,
                                                                                           authorizeUrl,
                                                                                           redirectUrl,
                                                                                           tokenUrl);
            oAuth2.Completed += OnOAuth2Completed;
            oAuth2.Error += OnOAuth2Error;

            Android.Content.Context context = Android.App.Application.Context;
            Android.Content.Intent loginUI = oAuth2.GetUI(context);
            loginUI.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(loginUI);
        }

        private void OnOAuth2Error(object sender, AuthenticatorErrorEventArgs e)
        {
            Error?.Invoke(sender, e);
        }

        private void OnOAuth2Completed(object sender, AuthenticatorCompletedEventArgs e)
        {
            // Workaround: su Android, se si effettua l'autenticazione passando anche l'url del token
            // (per avere il refresh token), il login viene completato con successo... ma viene sollevato
            // anche l'errore "invalid_grant".
            // Vedi https://github.com/xamarin/Xamarin.Auth/issues/248
            if (e.IsAuthenticated && Device.RuntimePlatform == Device.Android
                && sender is Xamarin.Auth.OAuth2Authenticator oAuth2)
            {
                // Stacco l'evento per la gestione degli errori, ormai l'autenticazione è fatta
                oAuth2.Error -= OnOAuth2Error;
            }
            Completed?.Invoke(sender, e);
        }

        public async Task<string> RefreshToken(OAuthAccountWrapper account, string clientId, Uri redirectUrl, string appSecret, Uri refreshTokenUrl)
        {
            if (account.AccessToken.Length == 0)
            {
                Debug.WriteLine("Token refresh failed!\nUser is not logged in.");
                return "";
            }

            HttpClient appClient = new HttpClient(new Xamarin.Android.Net.AndroidClientHandler());
            try
            {
                List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("redirect_uri", redirectUrl.AbsoluteUri),
                    new KeyValuePair<string, string>("client_secret", appSecret),
                    new KeyValuePair<string, string>("refresh_token", account.RefreshToken),
                    new KeyValuePair<string, string>("scope", account.Scope),
                    new KeyValuePair<string, string>("grant_type", "refresh_token")
                };

                FormUrlEncodedContent ContentToken = new FormUrlEncodedContent(Param);
                HttpResponseMessage ResponseToken = await appClient.PostAsync(refreshTokenUrl, ContentToken);
                if (!ResponseToken.IsSuccessStatusCode)
                {
                    appClient.Dispose();
                    Debug.WriteLine("Token refresh failed: " + ResponseToken.StatusCode);
                    throw new Exception("Token refresh failed: " + ResponseToken.StatusCode);
                }

                string content = await ResponseToken.Content.ReadAsStringAsync();
                JObject JsonObjToken = JObject.Parse(content);
                if (JsonObjToken != null && JsonObjToken.ContainsKey("access_token"))
                {
                    // Aggiorna o inserisce le nuove proprietà
                    foreach (KeyValuePair<string, JToken> token in JsonObjToken)
                    {
                        if (account.OAuthAccount.Properties.ContainsKey(token.Key))
                            account.OAuthAccount.Properties[token.Key] = token.Value.ToString();
                        else
                            account.OAuthAccount.Properties.Add(token.Key, token.Value.ToString());
                    }
                    if (account.OAuthAccount.Properties.ContainsKey("date_token"))
                        account.OAuthAccount.Properties["date_token"] = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                    else
                        account.OAuthAccount.Properties.Add("date_token", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

                    // Ricarica i parametri nel wrapper
                    account.Refresh();
                }
                else
                {
                    appClient.Dispose();
                    Debug.WriteLine("Token refresh failed: malformed response");
                    throw new Exception("Token refresh failed: malformed response");
                }
            }
            catch (Exception Ex)
            {
                appClient.Dispose();
                Debug.WriteLine("Token refresh failed: " + Ex.Message);
                throw new Exception("Token refresh failed: " + Ex.Message);
            }

            appClient.Dispose();
            Debug.WriteLine("Token refreshed!");

            return account.AccessToken;
        }
    }
}
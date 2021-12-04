using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Xamarin.Auth;
using Xamarin.Forms;


[assembly: Dependency(typeof(PwdCrypter.UWP.OAuth2Authenticator))]
namespace PwdCrypter.UWP
{
    /// <summary>
    /// Classe che implementa l'autenticazione con OAuth2
    /// </summary>
    public class OAuth2Authenticator : IOAuth2Authenticator
    {
        public event EventOnOAuth2Completed Completed;
        public event EventOnOAuth2Error Error;

        public async Task<string> RefreshToken(OAuthAccountWrapper account, string clientId, Uri redirectUrl, string appSecret, Uri refreshTokenUrl)
        {
            if (account.AccessToken.Length == 0)
            {
                Debug.WriteLine("Token refresh failed!\nUser is not logged in.");
                return "";
            }

            HttpClient appClient = new HttpClient();
            try
            {
                List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("redirect_uri", redirectUrl.AbsoluteUri),
                    new KeyValuePair<string, string>("refresh_token", account.RefreshToken),
                    new KeyValuePair<string, string>("scope", account.Scope),
                    new KeyValuePair<string, string>("grant_type", "refresh_token")
                };

                // Non è obbligatorio in certi casi(es: Google Drive API per iOS, Android e UWP)
                if (appSecret != "")
                    Param.Add(new KeyValuePair<string, string>("client_secret", appSecret));

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

        private string Base64URLEncode(byte[] value)
        {
            string base64 = Convert.ToBase64String(value);
            return base64.TrimEnd('=').Replace("+", "-").Replace("/", "_");
        }

        private string GetCodeVerifier()
        {
            Random genRandom = new Random(Environment.TickCount);
            string code = Guid.NewGuid().ToString("D") + ".app.pwdcrypter." + genRandom.Next(100, 1000);
            return code;
        }

        public async void StartAuthentication(string clientId, string scope, string appSecret, 
            Uri authorizeUrl, Uri redirectUrl, Uri tokenUrl, 
            bool codeChallengeMethod,
            bool useNativeUI)
        {
            string codeVerifier = "";
            UriBuilder uriBuilder = new UriBuilder(authorizeUrl);
            uriBuilder.Query += "?client_id=" + clientId + "&scope=" + Uri.EscapeUriString(scope) + "&response_type=code&redirect_uri=" + Uri.EscapeUriString(redirectUrl.AbsoluteUri);

            // Codice opzionale di sicurezza
            if (codeChallengeMethod)
            {
                codeVerifier = GetCodeVerifier();
                string codeChallenge = Base64URLEncode(EncDecHelper.SHA256Bytes(codeVerifier));
                uriBuilder.Query += "&code_challenge_method=S256&code_challenge=" + codeChallenge;
            }

            try
            {
                // Step 1: richiede il codice di autorizzazione
                WebAuthenticationResult Result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None,
                                                                                                 uriBuilder.Uri,
                                                                                                 redirectUrl);

                if (Result.ResponseStatus == WebAuthenticationStatus.Success)
                {
                    Account account = new Account();

                    // Estraggo il codice
                    string Code = "";
                    string QueryString = new Uri(Result.ResponseData).Query;
                    QueryString = QueryString.Substring(1); // Salta "?"
                    string[] QueryParams = QueryString.Split('&');
                    for (short i = 0; i < QueryParams.Length; i++)
                    {
                        if (QueryParams[i].Contains("code="))
                        {
                            Code = QueryParams[i].Substring(5);
                            break;
                        }
                    }

                    // Step 2: scambio il codice con un access token
                    HttpClient appClient = new HttpClient();
                    try
                    {
                        List<KeyValuePair<string, string>> Param = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("client_id", clientId),
                            new KeyValuePair<string, string>("redirect_uri", Uri.EscapeUriString(redirectUrl.AbsoluteUri)),
                            new KeyValuePair<string, string>("code", Uri.EscapeUriString(Code)),
                            new KeyValuePair<string, string>("scope", scope),
                            new KeyValuePair<string, string>("grant_type", "authorization_code")
                        };

                        // Non è obbligatorio in certi casi (es: Google Drive API per iOS, Android e UWP)
                        if (appSecret != "")
                            Param.Add(new KeyValuePair<string, string>("client_secret", appSecret));

                        // Codice opzionale di verifica
                        if (codeChallengeMethod && codeVerifier != "")
                            Param.Add(new KeyValuePair<string, string>("code_verifier", codeVerifier));

                        FormUrlEncodedContent Content = new FormUrlEncodedContent(Param);
                        HttpResponseMessage Response = await appClient.PostAsync(tokenUrl, Content);
                        if (Response.IsSuccessStatusCode)
                        {
                            // Ottengo un JSON con l'access token, il refesh token e la scadenza.
                            string content = await Response.Content.ReadAsStringAsync();
                            JObject JsonObj = JObject.Parse(content);
                            if (JsonObj != null && JsonObj.ContainsKey("access_token"))
                            {
                                foreach (KeyValuePair<string, JToken> token in JsonObj)
                                    account.Properties.Add(token.Key, token.Value.ToString());
                                account.Properties.Add("date_token", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));

                                // Step 3: richiedo un nuovo access token e un refresh token per aggiornare il
                                // token (in quando voglio accedere offline alla cartella di OneDrive)
                                OAuthAccountWrapper oAuthAccount = new OAuthAccountWrapper(account);
                                await RefreshToken(oAuthAccount, clientId, redirectUrl, appSecret, tokenUrl);
                                if (oAuthAccount.AccessToken.Length <= 0)
                                {
                                    appClient.Dispose();

                                    Debug.WriteLine("Authetication failed: access token not found in response");
                                    throw new Exception("Access token not found in response");
                                }

                                appClient.Dispose();
                                Debug.WriteLine("Authetication succeeded!");
                            }
                            else
                            {
                                appClient.Dispose();

                                Debug.WriteLine("Authetication failed: malformed response");
                                throw new Exception("Malformed response");
                            }
                        }
                        else
                        {
                            appClient.Dispose();

                            Debug.WriteLine("Authetication failed: " + Response.StatusCode.ToString());
                            throw new Exception("HTTP Error: " + Response.StatusCode + "\nUnable to redeem a token");
                        }

                        AuthenticatorCompletedEventArgs AuthCompletedEventArgs = new AuthenticatorCompletedEventArgs(account);
                        Completed?.Invoke(this, AuthCompletedEventArgs);
                    }
                    catch (Exception Ex)
                    {
                        appClient.Dispose();

                        Debug.WriteLine("Authetication failed: " + Ex.Message);
                        throw;
                    }
                }
                else if (Result.ResponseStatus != WebAuthenticationStatus.UserCancel)
                {
                    Debug.WriteLine("Authetication failed: " + Result.ResponseStatus.ToString());
                    throw new Exception("HTTP Error code: " + Result.ResponseErrorDetail + "\nError: " + Result.ResponseData);
                }
                else
                {
                    Debug.WriteLine("Authetication cancelled by user");

                    // Passando null si avrà IsAuthenticated a false.
                    // Vedi https://github.com/xamarin/Xamarin.Auth/blob/master/source/Core/Xamarin.Auth.Common.LinkSource/AuthenticatorCompletedEventArgs.cs
                    AuthenticatorCompletedEventArgs AuthCompletedEventArgs = new AuthenticatorCompletedEventArgs(null);
                    Completed?.Invoke(this, AuthCompletedEventArgs);
                }
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Authetication failed: " + Ex.Message);

                AuthenticatorErrorEventArgs AuthCompletedEventArgs = new AuthenticatorErrorEventArgs(Ex);
                Error?.Invoke(this, AuthCompletedEventArgs);
            }
        }
    }
}

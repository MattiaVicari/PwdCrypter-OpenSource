using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using PwdCrypter.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PwdCrypter.Cloud
{
    /// <summary>
    /// Classe che rappresenta il collegamento con il Cloud di OneDrive
    /// </summary>
    public class OneDriveConnector : ICloudConnector
    {
        private const string IDApplication = "YOUR_APPLICATION_ID";
        private const string AppSecret = "YOUR_APP_SECRET";
        private const string URLAuthorize = "https://login.live.com/oauth20_authorize.srf";
        private const string URLToken = "https://login.live.com/oauth20_token.srf";
        private const string URLLogout = "https://login.live.com/oauth20_logout.srf";
        private const string URLCallaback = "https://login.live.com/oauth20_desktop.srf";
        private const string URLBase = "https://graph.microsoft.com/v1.0";
        private const string Scopes = "Files.ReadWrite.AppFolder offline_access";

        private readonly string SerializationFileName;
        private const string SerializationPassword = "YOUR_SERIALIZATION_PASSWORD";

        static public readonly Newtonsoft.Json.JsonSerializer dateTimeSerializer = new Newtonsoft.Json.JsonSerializer
        {            
            DateFormatString = "yyyy-MM-ddTH:mm:ss.ffK",
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local
        };

        private OAuthAccountWrapper Account = null;

        // Messaggio dell'ultimo errore avvenuto
        private string LastError { get; set; }

        private IOAuth2Authenticator _OAuth2 = null;
        private IOAuth2Authenticator OAuth2
        {
            get {
                if (_OAuth2 == null)
                {
                    _OAuth2 = DependencyService.Get<IOAuth2Authenticator>();
                    if (_OAuth2 == null)
                        throw new Exception("OAth2 authentication not supported!");

                    _OAuth2.Completed += OnOAuth2Completed;
                    _OAuth2.Error += OnOAuth2Error;
                }
                return _OAuth2;
            }
        }

        // Evento scatenato alla fine del login
        public event EventHandler LoggedIn;
        // Evento scatenato alla fine del log out
        public event EventHandler LoggedOut;

        /// <summary>
        /// Impostare a true per ricordare i dati di accesso
        /// </summary>
        public bool RememberMe { get; private set; }


        public OneDriveConnector()
        {
            RememberMe = false;
            SerializationFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                                                "cloudremember.me");
            DeSerialize();
        }

        /// <summary>
        /// Scarica un file da OneDrive
        /// </summary>
        /// <param name="Url">URL del file da scaricare</param>
        /// <param name="DestFileName">Nome del file di destinazione</param>
        /// <returns>Restituisce true se l'operazione va a buon fine, false altrimenti</returns>
        private async Task<bool> DownloadFile(string Url, string DestFileName)
        {
            bool Result = false;

            HttpClient AppClient = new HttpClient();
            try
            {
                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                HttpResponseMessage Response = await AppClient.GetAsync(new Uri(Url + ":/content"));
                if (!Response.IsSuccessStatusCode)
                    throw new Exception("Unable to read the file. Status code: " + Response.StatusCode);
                
                Stream streamContent = await Response.Content.ReadAsStreamAsync();
                if (streamContent.Length > 0)
                {
                    streamContent.Position = 0;
                    byte[] data = new byte[streamContent.Length];
                    await streamContent.ReadAsync(data, 0, data.Length);

                    File.WriteAllBytes(DestFileName, data);
                    Result = File.Exists(DestFileName);
                }
            }
            catch (Exception Ex)
            {                
                string MessageEx = "Unable to download the file with URL " + Url + ". Error: " + Ex.Message;
                Debug.WriteLine(MessageEx);
                App.Logger.Debug(MessageEx);
                throw new Exception(AppResources.errCloudDownload + " " + MessageEx);
            }
            finally
            {
                AppClient.Dispose();
            }

            return Result;
        }

        /// <summary>
        /// Verifica se un determinato file esiste su OneDrive.
        /// </summary>
        /// <param name="FileName">Nome del file</param>
        /// <returns>Restituisce FileNotFound se il file non viene trovato, FileFound se esiste
        /// o FileError in caso di errore. Chiamare la funzione GetLastError per informazioni sull'eventuale
        /// errore.</returns>
        public async Task<CloudFileStatus> FileExists(string FileName)
        {
            LastError = "";
            const string UrlRoot = URLBase + "/me/drive/special/approot/";

            CloudFileStatus Result = CloudFileStatus.FileNotFound;
            if (!await CheckToken())
            {
                LastError = "User is not logged in!";
                Debug.WriteLine(LastError);
                return CloudFileStatus.FileError;
            }

            // Ottiene la lista dei file nella cartella dedicata all'App
            HttpClient appClient = new HttpClient();
            try
            {
                string Url = UrlRoot + "children";
                if (string.Compare(Path.GetFileName(FileName), FileName) != 0)
                {
                    string[] tokens = FileName.Split( new []{ Path.DirectorySeparatorChar });
                    if (tokens.Length > 1)
                    {
                        Url = URLBase + "/me/drive/special/approot:";
                        for (int i=0; i < tokens.Length - 1; i++)
                        {
                            if (tokens[i].Length > 0)
                                Url += "/" + tokens[i];
                        }
                        Url += ":/children/";
                        FileName = tokens[tokens.Length - 1];
                    }
                }

                appClient.DefaultRequestHeaders.Clear();
                appClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                HttpResponseMessage Response = await appClient.GetAsync(new Uri(Url));
                if (!Response.IsSuccessStatusCode)
                {
                    appClient.Dispose();
                    LastError = "Unable to find the file " + FileName + ". Status code: " + Response.StatusCode;
                    Debug.WriteLine(LastError);
                    return CloudFileStatus.FileError;
                }

                // Cerca nel JSON il file
                JObject JsonObj = JObject.Parse(await Response.Content.ReadAsStringAsync());
                if (JsonObj != null)
                {
                    JArray DriveItems = JArray.Parse(JsonObj["value"].ToString());
                    if (DriveItems != null)
                    {
                        Result = CloudFileStatus.FileNotFound;

                        foreach(JObject JsonDriveItem in DriveItems)
                        {
                            if (JsonDriveItem != null)
                            {
                                if (JsonDriveItem["name"].ToString().CompareTo(FileName) == 0)
                                {
                                    // Trovato!
                                    Result = CloudFileStatus.FileFound;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    appClient.Dispose();
                    LastError = "Unable to find the file " + FileName + ". Malformed response";
                    Debug.WriteLine(LastError);
                    return CloudFileStatus.FileError;
                }
            }
            catch (Exception Ex)
            {
                appClient.Dispose();
                LastError = "Unable to find the file " + FileName + ": " + Ex.Message;
                Debug.WriteLine(LastError);
                return CloudFileStatus.FileError;
            }

            appClient.Dispose();
            return Result;
        }

        /// <summary>
        /// Restituisce l'access token della connessione
        /// </summary>
        /// <returns></returns>
        public string GetAccessToken()
        {
            return Account?.AccessToken;
        }


        /// <summary>
        /// Scarica un file dal Cloud.
        /// </summary>
        /// <param name="FileName">Nome del file da scaricare</param>
        /// <param name="DestFileName">Nome del file di destinazione</param>
        /// <returns>Restituisce FileDownloaded se l'operazione ha successo, altrimenti restituisce
        /// FileDownloadError in caso di errore o FileNotFound se il file non esiste</returns>
        public async Task<CloudFileStatus> GetFile(string FileName, string DestFileName)
        {
            const string UrlRoot = URLBase + "/me/drive/special/approot:/";
            
            if (!await CheckToken())
            {
                Debug.WriteLine("User is not logged in!");
                return CloudFileStatus.FileDownloadError;
            }

            try
            {
                CloudFileStatus Result = await FileExists(FileName);
                if (Result == CloudFileStatus.FileError || Result == CloudFileStatus.FileNotFound)                    
                    throw new Exception(string.Format(string.Format("{0}: Unable to download the file {1}. Error: {2}", AppResources.errCloudDownload, FileName, GetLastError())));

                // Posso scaricare il file            
                if (!await DownloadFile(UrlRoot + FileName, DestFileName))
                    Result = CloudFileStatus.FileDownloadError;
                return CloudFileStatus.FileDownloaded;
            }
            catch(Exception e)
            {
                Debug.WriteLine(string.Format("GetFile error: {0}", e.Message));
                throw;
            }
        }


        /// <summary>
        /// Restituisce le informazioni su un file su OneDrive.
        /// </summary>
        /// <param name="FileName">Nome del file</param>
        /// <returns>Restituisce le informazioni sul file o null se l'operazione fallisce 
        /// o il file non viene trovato</returns>
        private async Task<CloudFileInfo> GetFileInfo(string FileName)
        {
            CloudFileInfo FileInfo = null;

            LastError = "";
            const string UrlRoot = URLBase + "/me/drive/special/approot/";
            if (!await CheckToken())
            {
                LastError = "User is not logged in!";
                Debug.WriteLine(LastError);
                return null;
            }

            // Ottiene la lista dei file nella cartella dedicata all'App
            HttpClient appClient = new HttpClient();
            try
            {
                string Url = UrlRoot + "children";
                if (string.Compare(Path.GetFileName(FileName), FileName) != 0)
                {
                    string[] tokens = FileName.Split(new[] { Path.DirectorySeparatorChar });
                    if (tokens.Length > 1)
                    {
                        Url = URLBase + "/me/drive/special/approot:";
                        for (int i = 0; i < tokens.Length - 1; i++)
                        {
                            if (tokens[i].Length > 0)
                                Url += "/" + tokens[i];
                        }
                        Url += ":/children/";
                        FileName = tokens[tokens.Length - 1];
                    }
                }

                appClient.DefaultRequestHeaders.Clear();
                appClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                HttpResponseMessage Response = await appClient.GetAsync(new Uri(Url));
                if (!Response.IsSuccessStatusCode)
                {
                    appClient.Dispose();
                    LastError = "Unable to find the file " + FileName + ". Status code: " + Response.StatusCode;
                    Debug.WriteLine(LastError);
                    return null;
                }

                // Cerca nel JSON di risposta il file
                JObject JsonObj = JObject.Parse(await Response.Content.ReadAsStringAsync());
                if (JsonObj != null)
                {
                    JArray DriveItems = JArray.Parse(JsonObj["value"].ToString());
                    if (DriveItems != null)
                    {
                        foreach(JObject JsonDriveItem in DriveItems)
                        {
                            if (JsonDriveItem != null)
                            {
                                if (JsonDriveItem["name"].ToString().CompareTo(FileName) == 0)
                                {
                                    // Trovato!
                                    FileInfo = new CloudFileInfo
                                    {
                                        DownloadUrl = JsonDriveItem["@microsoft.graph.downloadUrl"].ToString(),
                                        CreatedDateTime = JsonDriveItem["createdDateTime"].ToObject<DateTime>(dateTimeSerializer),
                                        ID = JsonDriveItem["id"].ToString(),
                                        LastModifiedDateTime = JsonDriveItem["lastModifiedDateTime"].ToObject<DateTime>(dateTimeSerializer),
                                        Name = JsonDriveItem["name"].ToString(),
                                        Size = long.Parse(JsonDriveItem["size"].ToString()),
                                        WebUrl = JsonDriveItem["webUrl"].ToString()
                                    };
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    appClient.Dispose();
                    LastError = "Unable to find the file " + FileName + ". Malformed response";
                    Debug.WriteLine(LastError);
                    return null;
                }
            }
            catch (Exception Ex)
            {
                appClient.Dispose();
                LastError = "Unable to find the file " + FileName + ": " + Ex.Message;
                Debug.WriteLine(LastError);
                return null;
            }

            appClient.Dispose();
            return FileInfo;
        }


        /// <summary>
        /// Verifica se l'untente è loggato.
        /// Se l'utente è loggato, prova ad aggiornare il token.
        /// Se l'utente non è loggato, restituisce false e visualizza un messaggio di errore.
        /// </summary>
        /// <returns>Restituisce true se l'utente è loggato, false altrimenti</returns>
        private async Task<bool> CheckToken()
        {
            if (!IsLoggedIn())
            {
                Debug.Assert(false, AppResources.errCloudUserNotLoggedIn);
                Debug.WriteLine(AppResources.errCloudUserNotLoggedIn);
                return false;
            }
            else
            {
                // Non effettuo il refresh se è trascorso meno di 15 minuti dalla creazione/rinnovo
                // del token
                if (!IsTokenExpired() && (DateTime.Now - Account.DateToken).TotalSeconds < 15 * 60.0)
                {
                    // Controlla però se il token è valido
                    bool ret = await ValidateToken();
                    if (!ret)
                    {
                        ForgetAccessData();
                        Account.OAuthAccount = null;
                    }
                    return ret;
                }

                if (IsTokenExpired())
                    ForgetAccessData();

                // Aggiorna il token
                if (string.IsNullOrEmpty(await Refresh()))
                {
                    Debug.WriteLine(AppResources.errCloudUserNotLoggedIn);                    
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifica se il token è valido
        /// </summary>
        /// <returns>Restituisce true se il token è valido, false altrimenti</returns>
        public async Task<bool> ValidateToken()
        {
            const string UrlMe = URLBase + "/me/drive/special/approot/";
            if (!IsLoggedIn())
            {
                Debug.WriteLine("Token validation failed!\nUser is not logged in.");
                return false;
            }

            // Ottiene la lista dei file nella cartella dedicata all'App
            HttpClient appClient = new HttpClient();
            try
            {
                appClient.DefaultRequestHeaders.Clear();
                appClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                HttpResponseMessage Response = await appClient.GetAsync(new Uri(UrlMe));
                if (!Response.IsSuccessStatusCode)
                    return false;

                JObject jObject = JObject.Parse(await Response.Content.ReadAsStringAsync());
                if (jObject.ContainsKey("error"))
                {
                    string code = jObject["code"].ToString();
                    string message = jObject["message"].ToString();
                    Debug.WriteLine("Invalid token.\nCode: " + code + "\nMessage: " + message);

                    return false;
                }

                return true;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Validation token failed. Error: " + Ex.Message);
                return false;
            }
            finally
            {
                appClient.Dispose();
            }
        }

        /// <summary>
        /// Restituisce il messaggio di errore dell'ultimo errore avvenuto.
        /// </summary>
        /// <returns>Messaggio di errore</returns>
        public string GetLastError()
        {
            return LastError;
        }

        /// <summary>
        /// Verifica se è attiva una connessione con il Cloud (login)
        /// </summary>
        /// <returns>True se c'è una connessione attiva con il Cloud, false altrimenti</returns>
        public bool IsLoggedIn()
        {
            return Account?.AccessToken.Length > 0;
        }

        /// <summary>
        /// Verifica se il token di accesso è valido.
        /// </summary>
        /// <returns>True se il token è scaduto, false altrimenti.</returns>
        public bool IsTokenExpired()
        {
            return DateTime.Now > Account?.DateToken.AddSeconds((double)(Account?.ExpiresIn));
        }

        /// <summary>
        /// Effettua il login su OneDrive.
        /// </summary>
        /// <returns>True se l'operazione è andata a buon fine, false altrimenti</returns>
        public async Task<bool> Login()
        {
            LastError = "";

            try
            {
                OAuth2.StartAuthentication(Uri.EscapeUriString(IDApplication),
                                           Scopes,
                                           AppSecret,
                                           new Uri(URLAuthorize),
                                           new Uri(URLCallaback),
                                           new Uri(URLToken));
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                string msg = AppResources.errCloudAuth + " " + ex.Message;
                Debug.WriteLine(msg);
                throw new Exception(msg);
            }
        }

        private void OnOAuth2Error(object sender, Xamarin.Auth.AuthenticatorErrorEventArgs e)
        {
            LastError = e.Message;
            Debug.WriteLine(AppResources.errCloudAuth + " " + e.Message);
            LoggedIn?.Invoke(sender, new EventArgs());
        }

        private void OnOAuth2Completed(object sender, Xamarin.Auth.AuthenticatorCompletedEventArgs e)
        {
            if (!e.IsAuthenticated)
            {
                // Non autenticato
                Debug.WriteLine("Not authenticated");
                LoggedIn?.Invoke(sender, new EventArgs());
                return;
            }

            Account = new OAuthAccountWrapper(e.Account);
            Debug.WriteLine("Authenticated");
            RememberDataAccess();

            LoggedIn?.Invoke(sender, new EventArgs());
        }

        private void RememberDataAccess()
        {
            if (RememberMe)
                Serialize();
        }

        /// <summary>
        /// Effettua il logout.
        /// </summary>
        /// <returns>True se l'operazione è andata a buon fine, false altrimenti</returns>
        public async Task<bool> Logout()
        {
            if (!IsLoggedIn())
            {
                Debug.WriteLine("Logout: user is not logged in!");
                LoggedOut?.Invoke(this, new EventArgs());
                return true;    // L'utente non è loggato
            }

            HttpClient appClient = new HttpClient();
            try
            {
                string Url = URLLogout + "?post_logout_redirect_uri=" + Uri.EscapeUriString(URLCallaback);
                HttpResponseMessage Response = await appClient.GetAsync(new Uri(URLLogout));
                if (!Response.IsSuccessStatusCode)                                                            
                    throw new Exception("Logout request failed, status code:" + Response.StatusCode);                

                Account.OAuthAccount = null;
                ForgetAccessData();

                Debug.WriteLine("User logged out");
                LoggedOut?.Invoke(this, new EventArgs());
                return true;
            }
            catch (Exception ex)
            {
                string msg = AppResources.errCloudLogout + " " + ex.Message;
                Debug.WriteLine(msg);                
                throw new Exception(msg);
            }
            finally
            {
                appClient.Dispose();
            }            
        }

        private void ForgetAccessData()
        {
            try
            {
                if (File.Exists(SerializationFileName))
                    File.Delete(SerializationFileName);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("ForgetAccessData error: " + Ex.Message);
            }
        }

        /// <summary>
        /// Chiamare per prolungare la validità del token di accesso.
        /// </summary>
        /// <returns>Restituisce l'eventuale token di accesso aggiornato.
        /// In caso di errore, viene restituita stringa vuota.</returns>
        public async Task<string> Refresh()
        {
            if (!IsLoggedIn())
            {
                Debug.WriteLine("Token refresh failed!\nUser is not logged in.");
                return string.Empty;
            }

            try
            {
                await OAuth2.RefreshToken(Account, 
                                          Uri.EscapeUriString(IDApplication), 
                                          new Uri(URLCallaback), 
                                          AppSecret, 
                                          new Uri(URLToken));
                if (Account.AccessToken.Length <= 0)                    
                    throw new Exception("Token refresh failed! The new token is empty");                

                // Aggiorna anche i dati accesso precedente serializzati in file
                RefreshAccessData();

                Debug.WriteLine("Token refreshed!");
                return Account.AccessToken;
            }
            catch (Exception e)
            {
                LastError = e.Message;                
                Debug.WriteLine(AppResources.errCloudRefreshToken2 + " " + e.Message);
                return string.Empty;
            }            
        }

        private void RefreshAccessData()
        {
            if (File.Exists(SerializationFileName))
                Serialize();
        }


        /// <summary>
        /// Carica un file su OneDrive
        /// </summary>
        /// <param name="FileName">Nome del file da caricare</param>
        /// <param name="DestFileName">Nome del file su OneDrive</param>
        /// <returns>Restituisce true se l'operazione va a buon fine, false altrimenti.</returns>
        public async Task<bool> UploadFile(string FileName, string DestFileName)
        {
            if (!await CheckToken())
            {                
                Debug.WriteLine("User is not logged in");
                return false;
            }

            HttpClient AppClient = new HttpClient();
            try
            {                
                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                // Controlla se il file esiste già
                CloudFileInfo FileInfo = null;
                bool UpdateFile = false;
                if (await FileExists(DestFileName) == CloudFileStatus.FileFound)
                {
                    UpdateFile = true;
                    FileInfo = await GetFileInfo(DestFileName);
                    if (FileInfo == null)
                        throw new Exception(string.Format("Unable to find info about the file {0}. Uploaded failed", DestFileName));                    
                }

                // Due modalità diverse a seconda delle dimensioni del file
                FileInfo fileInfo = new FileInfo(FileName);
                if (fileInfo.Length > 4 * 1024 * 1240)
                {
                    // Oltre i 4MB devo creare una sessione di caricamento
                    // Stesso url anche per l'aggiornamento.
                    string Url = URLBase + "/me/drive/special/approot:/" + DestFileName + ":/createUploadSession";

                    // Gestione dei conflitti
                    JObject JsonObj = new JObject();
                    JObject JsonConflictBehavior = new JObject
                    {
                        { "@microsoft.graph.conflictBehavior", new JValue("rename") }
                    };
                    JsonObj["item"] = JsonConflictBehavior;

                    StringContent Content = new StringContent(JsonObj.ToString(), System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage Response = await AppClient.PostAsync(new Uri(Url), Content);
                    if (!Response.IsSuccessStatusCode)                                     
                        throw new Exception(string.Format("File {0} not uploaded. Status code: {1}", FileName, Response.StatusCode));

                    // Leggo l'url per l'upload del file
                    string UpdUrl = "";
                    JObject JsonObjResponse = JObject.Parse(await Response.Content.ReadAsStringAsync());
                    if (JsonObjResponse != null)
                        UpdUrl = JsonObjResponse["uploadUrl"].ToString();
                    if (UpdUrl.Length == 0)                        
                        throw new Exception(string.Format("File {0} not uploaded. Error: uploade session not created", FileName));

                    // Upload
                    FileStream Stream = new FileStream(FileName, FileMode.Open);
                    StreamContent ContentUpload = new StreamContent(Stream);
                    try
                    {
                        ContentUpload.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(0, Stream.Length - 1, Stream.Length);
                        HttpResponseMessage ResponseUpload = await AppClient.PutAsync(new Uri(UpdUrl), ContentUpload);
                        if (!ResponseUpload.IsSuccessStatusCode)                        
                            throw new Exception(string.Format("File {0} not uploaded. Status code: {1}", FileName, Response.StatusCode));                        

                        // Leggo il nome del file e, se diverso da pwdcloud.list, lo rinomino
                        JObject JsonRename = JObject.Parse(await ResponseUpload.Content.ReadAsStringAsync());
                        if (JsonRename != null)
                        {
                            string NewName = JsonRename["name"].ToString();
                            string pwdfileNameInCloud = App.PwdManager.PwdFileNameInCloud;
                            if (NewName != "" && NewName.CompareTo(pwdfileNameInCloud) != 0)
                            {
                                // Copia di backup
                                string CopyName = "pwdcloud_" + DateTime.Now.ToString("ddMMyyyy_Hmmssfff") + ".list.bck";
                                try
                                {
                                    await CopyFile(pwdfileNameInCloud, CopyName);
                                    await DeleteFile(pwdfileNameInCloud);
                                    try
                                    {
                                        await RenameFile(NewName, pwdfileNameInCloud);
                                    }
                                    catch (Exception)
                                    {
                                        await RenameFile(CopyName, pwdfileNameInCloud);
                                        throw;
                                    }
                                }
                                catch (Exception e)
                                {
                                    throw new Exception(string.Format("File {0} not uploaded. Error: unable to deal with the name conflict. {1}", FileName, e.Message));
                                }
                                finally
                                {
                                    // Elimina la copia di backup
                                    await DeleteFile(CopyName);
                                }
                            }
                        }
                    }
                    finally
                    {
                        ContentUpload.Dispose();
                        Stream.Dispose();
                    }
                }
                else
                {
                    // Caricamento diretto
                    string Url = URLBase + "/me/drive/special/approot:/" + DestFileName + ":/content";
                    if (UpdateFile)
                        Url = URLBase + "/me/drive/items/" + FileInfo.ID + "/content";

                    FileStream Stream = new FileStream(FileName, FileMode.Open);
                    StreamContent Content = new StreamContent(Stream);
                    try
                    {
                        HttpResponseMessage Response = await AppClient.PutAsync(new Uri(Url), Content);
                        if (!Response.IsSuccessStatusCode)
                            throw new Exception(string.Format("File {0} not uploaded. Status code: {1}", FileName, Response.StatusCode));                        
                    }
                    finally
                    {
                        Content.Dispose();
                        Stream.Dispose();
                    }
                }

                return true;
            }
            catch (FileNotFoundException e)
            {
                // File sorgente non trovato                
                Debug.WriteLine(AppResources.errCloudUpload + " file not found. Error: " + e.Message);
                return false;
            }
            catch (Exception e)
            {                
                Debug.WriteLine(AppResources.errCloudUpload + " " + e.Message);
                return false;
            }
            finally
            {
                AppClient.Dispose();
            }
        }

        /// <summary>
        /// Rinomina un file su Cloud
        /// </summary>
        /// <param name="FileName">Nome del file da rinominare</param>
        /// <param name="NewFileName">Nuovo nome del file</param>
        /// <returns></returns>
        private async Task RenameFile(string FileName, string NewFileName)
        {
            string Url = URLBase + "/me/drive/special/approot:/" + FileName;
            HttpClient AppClient = new HttpClient();
            try
            {
                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                JObject JsonRename = new JObject
                {
                    ["name"] = new JValue(NewFileName)
                };

                HttpRequestMessage Request = new HttpRequestMessage(new HttpMethod("PATCH"), new Uri(Url))
                {
                    Content = new StringContent(JsonRename.ToString(), System.Text.Encoding.UTF8, "application/json")
                };
                HttpResponseMessage Response = await AppClient.SendAsync(Request);
                if (!Response.IsSuccessStatusCode)
                {
                    AppClient.Dispose();
                    Debug.WriteLine("Renaming file " + FileName + " failed. Status code: " + Response.StatusCode);
                    throw new Exception("Renaming file " + FileName + " failed. Status code: " + Response.StatusCode);
                }
            }
            catch (Exception Ex)
            {
                AppClient.Dispose();
                Debug.WriteLine("Renaming file " + FileName + " failed. Error: " + Ex.Message);
                throw new Exception("Unable to rename the file " + FileName + ". Error: " + Ex.Message);
            }

            Debug.WriteLine("File " + FileName + " renamed in " + NewFileName);
            AppClient.Dispose();
        }

        /// <summary>
        /// Cancella un file dal Cloud
        /// </summary>
        /// <param name="FileName">Nome del file da cancellare</param>
        /// <returns></returns>
        public async Task DeleteFile(string FileName)
        {
            if (!await CheckToken())
            {                
                Debug.WriteLine("User is not logged in");
                throw new Exception("Unable to delete the file " + FileName + ". Error: User is not logged in");
            }

            string Url = URLBase + "/me/drive/special/approot:/" + FileName;
            HttpClient AppClient = new HttpClient();
            try
            {
                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                HttpResponseMessage Response = await AppClient.DeleteAsync(new Uri(Url));
                if (!Response.IsSuccessStatusCode)
                {
                    AppClient.Dispose();
                    Debug.WriteLine("Delete file " + FileName + " failed. Status code: " + Response.StatusCode);
                    throw new Exception("Delete file " + FileName + " failed. Status code: " + Response.StatusCode);
                }
            }
            catch (Exception Ex)
            {
                AppClient.Dispose();
                Debug.WriteLine("Unable to delete the file " + FileName + ". Error: " + Ex.Message);
                throw new Exception("Unable to delete the file " + FileName + ". Error: " + Ex.Message);
            }

            Debug.WriteLine("File " + FileName + " deleted");
            AppClient.Dispose();
        }

        /// <summary>
        /// Crea una copia di un file
        /// </summary>
        /// <param name="FileName">Nome del file da copiare</param>
        /// <param name="CopyFileName">Nome della copia del file</param>
        /// <returns></returns>
        private async Task CopyFile(string FileName, string CopyFileName)
        {
            string Url = URLBase + "/me/drive/special/approot:/" + FileName + ":/copy/";
            HttpClient AppClient = new HttpClient();
            try
            {
                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                JObject JsonObj = new JObject
                {
                    ["name"] = JValue.CreateString(CopyFileName)
                };
                StringContent Content = new StringContent(JsonObj.ToString(), System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage Response = await AppClient.PostAsync(new Uri(Url), Content);
                if (!Response.IsSuccessStatusCode)
                {
                    AppClient.Dispose();
                    Debug.WriteLine("Copy of file " + FileName + " in " + CopyFileName + " failed. Status code: " + Response.StatusCode);
                    throw new Exception("Copy of file " + FileName + " in " + CopyFileName + " failed. Status code: " + Response.StatusCode);
                }

                // Leggo il monitor della copia per conoscerne lo stato
                Uri MonitorLocation = Response.Headers.Location;
                if (MonitorLocation != null)
                {
                    // Controllo ogni 500 ms lo stato della copia
                    do
                    {
                        AppClient.DefaultRequestHeaders.Clear();    // Senza autenticazione nell'header

                        HttpResponseMessage ResponseMonitor = await AppClient.GetAsync(MonitorLocation);
                        if (ResponseMonitor.IsSuccessStatusCode)
                        {
                            JObject JsonPercentage = JObject.Parse(await ResponseMonitor.Content.ReadAsStringAsync());
                            if (JsonPercentage == null)
                            {
                                AppClient.Dispose();
                                Debug.WriteLine("Copy of file " + FileName + " in " + CopyFileName + " failed. Status code: " + ResponseMonitor.StatusCode);
                                throw new Exception("Copy of file " + FileName + " in " + CopyFileName + " failed. Status code: " + ResponseMonitor.StatusCode);
                            }
                            double Percentage = double.Parse(JsonPercentage["percentageComplete"].ToString());
                            Debug.WriteLine("Copy percentage: " + Percentage + "%%");
                            if (Percentage >= 100.0)
                                break;
                        }
                        else
                        {
                            AppClient.Dispose();
                            Debug.WriteLine("Copy of file " + FileName + " in " + CopyFileName + " failed. Status code: " + ResponseMonitor.StatusCode);
                            throw new Exception("Copy of file " + FileName + " in " + CopyFileName + " failed. Status code: " + ResponseMonitor.StatusCode);
                        }
                        await Task.Delay(500);
                    } while (true);
                }
            }
            catch (Exception Ex)
            {
                AppClient.Dispose();
                Debug.WriteLine(Ex.Message);
                throw;
            }

            Debug.WriteLine("Copy of file " + FileName + " in " + CopyFileName + " completed");
            AppClient.Dispose();
        }

        /// <summary>
        /// Serializzazione delle informazioni di accesso al Cloud su file
        /// </summary>
        private void Serialize()
        {
            if (!IsLoggedIn())
                return;

            try
            {
                string data = Account.OAuthAccount.Serialize();
                EncDecHelper encDecHelper = new EncDecHelper
                {
                    PasswordString = SerializationPassword
                };

                File.WriteAllText(SerializationFileName, encDecHelper.AESEncrypt(data), System.Text.Encoding.UTF8);
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Serialize error: " + Ex.Message);
            }
        }

        /// <summary>
        /// Ricostruisce le informazioni di accesso al Cloud da una precedente
        /// serializzazione
        /// </summary>
        private void DeSerialize()
        {
            try
            {
                Account = null;
                string data = File.ReadAllText(SerializationFileName, System.Text.Encoding.UTF8);
                EncDecHelper encDecHelper = new EncDecHelper
                {
                    PasswordString = SerializationPassword
                };

                Xamarin.Auth.Account account = Xamarin.Auth.Account.Deserialize(encDecHelper.AESDecrypt(data, out bool warning));
                if (warning)
                {
                    Debug.WriteLine(string.Format("OneDrive deserialize file {0}: file is corrupted. I have tried to recover it",
                        SerializationFileName));
                }
                if (account != null)
                {
                    Account = new OAuthAccountWrapper(account);
                    RememberMe = true;
                }
            }
            catch (FileNotFoundException)
            {
                // Accettabile
                Debug.WriteLine(string.Format("Deserialize: file {0} not found", SerializationFileName));
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Deserialize error: " + Ex.Message);
            }
        }

        /// <summary>
        /// Impostare a true per permettere di ricordare i dati di accesso
        /// al Cloud.
        /// </summary>
        /// <param name="remember">Valore booleano della scelta</param>
        public void SetRememberMe(bool remember)
        {
            RememberMe = remember;
        }

        /// <summary>
        /// Crea una cartella su Cloud
        /// </summary>
        /// <param name="folderName">Nome della cartella da creare su Cloud</param>
        /// <returns></returns>
        public async Task CreateFolder(string folderName)
        {
            if (!await CheckToken())
            {                
                Debug.WriteLine("User is not logged in");
                throw new Exception(string.Format("Unable to create the folder {0}. User is not logged in", folderName));
            }

            HttpClient AppClient = new HttpClient();
            try
            {
                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                string Url = URLBase + "/me/drive/special/approot/children";

                JObject JsonObj = new JObject
                {
                    { "name", new JValue(folderName) },
                    { "@microsoft.graph.conflictBehavior", new JValue("rename") },
                    { "folder", new JObject() } // Necessario per dire a OneDrive di creare una cartella
                };

                StringContent Content = new StringContent(JsonObj.ToString(), System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = await AppClient.PostAsync(new Uri(Url), Content);
                if (!httpResponse.IsSuccessStatusCode)
                    throw new Exception(string.Format("Folder {0} not created. Status code: {1}", folderName, httpResponse.StatusCode));
            }
            catch (Exception e)
            {
                string msg = string.Format("Error occurred during the creation of the folder {0}. Error: {1}", folderName, e.Message);
                Debug.WriteLine(msg);
                throw new Exception(AppResources.errCloudCreateFolder + " " + msg);                
            }
            finally
            {
                AppClient.Dispose();
            }
        }

        /// <summary>
        /// Verifica se la cartella esiste nella root dell'applicazione.
        /// </summary>
        /// <param name="FolderName">Nome della cartella</param>
        /// <returns>Restituisce FileNotFound se la cartella non viene trovata, FileFound se esiste
        /// o FileError in caso di errore. Chiamare la funzione GetLastError per informazioni sull'eventuale
        /// errore.</returns>
        public async Task<CloudFileStatus> FolderExists(string FolderName)
        {
            // Per OneDrive è come se fosse un file
            return await FileExists(FolderName);
        }

        public bool GetRememberMe()
        {
            return RememberMe;
        }

        /// <summary>
        /// Restituisce le informazioni sui file contenuti nella cartella specificata.
        /// </summary>
        /// <param name="FolderName">Nome della cartella</param>
        /// <returns>Lista degli oggetti di tipo CloudFileInfo con le informazioni sui file nella cartella</returns>
        public async Task<IList<CloudFileInfo>> GetFilesInfo(string FolderName)
        {
            LastError = "";
            const string UrlRoot = URLBase + "/me/drive/special/approot:/";
            if (!await CheckToken())
            {
                LastError = "User is not logged in!";
                Debug.WriteLine(LastError);
                return null;
            }

            // Ottiene la lista dei file nella cartella indicata
            HttpClient appClient = new HttpClient();
            try
            {
                string Url = UrlRoot + FolderName + ":/children/";                

                appClient.DefaultRequestHeaders.Clear();
                appClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                HttpResponseMessage Response = await appClient.GetAsync(new Uri(Url));
                if (!Response.IsSuccessStatusCode)
                {
                    appClient.Dispose();
                    LastError = "Unable to find the files in folder " + FolderName + ". Status code: " + Response.StatusCode;
                    Debug.WriteLine(LastError);
                    return null;
                }

                // Analizza il JSON di risposta                
                JObject JsonObj = JObject.Parse(await Response.Content.ReadAsStringAsync());
                if (JsonObj == null)
                    throw new Exception("Malformed response");

                List<CloudFileInfo> files = new List<CloudFileInfo>();
                JArray DriveItems = JArray.Parse(JsonObj["value"].ToString());
                if (DriveItems != null)
                {
                    foreach (JObject JsonDriveItem in DriveItems)
                    {
                        if (JsonDriveItem != null)
                        {
                            files.Add(new CloudFileInfo
                            {
                                DownloadUrl = JsonDriveItem["@microsoft.graph.downloadUrl"].ToString(),                                
                                CreatedDateTime = JsonDriveItem["createdDateTime"].ToObject<DateTime>(dateTimeSerializer),
                                ID = JsonDriveItem["id"].ToString(),                                
                                LastModifiedDateTime = JsonDriveItem["lastModifiedDateTime"].ToObject<DateTime>(dateTimeSerializer),
                                Name = JsonDriveItem["name"].ToString(),
                                Size = long.Parse(JsonDriveItem["size"].ToString()),
                                WebUrl = JsonDriveItem["webUrl"].ToString()
                            });                            
                        }                        
                    }
                }                

                return files;
            }
            catch (Exception Ex)
            {
                LastError = "Unable to find the files in folder " + FolderName + ": " + Ex.Message;
                Debug.WriteLine(LastError);
                return null;
            }
            finally
            {
                appClient.Dispose();
            }
        }
    }
}

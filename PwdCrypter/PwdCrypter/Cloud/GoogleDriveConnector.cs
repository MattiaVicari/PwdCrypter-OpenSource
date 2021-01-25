using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PwdCrypter.Cloud
{
    public class GoogleDriveConnector : ICloudConnector
    {
        /// <summary>
        /// Classe che raccoglie le informazioni su un file 
        /// </summary>
        private class DriveFileInfo
        {
            /// <summary>
            /// Status sull'operazione sul file
            /// </summary>
            public CloudFileStatus Status { get; set; }

            /// <summary>
            /// ID del file
            /// </summary>
            public string FileID { get; set; }

            /// <summary>
            /// Nome del file
            /// </summary>
            public string FileName { get; set; }
        }

        public event EventHandler LoggedIn;
        public event EventHandler LoggedOut;

        private const string AppDataFolder = "PwdCrypter";

        private const string IDApplication = "YOUR_APPLICATION_ID.apps.googleusercontent.com";
        private const string AppSecret = "";    // Non necessario per Google Drive API
        private const string URLAuthorize = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string URLToken = "https://www.googleapis.com/oauth2/v4/token";
        private const string URLCallaback = "com.googleusercontent.apps.YOUR_CALLBACK_URL:/oauth2redirect";
        private const string URLBase = "https://www.googleapis.com/drive/v3";
        private const string URLBaseUpload = "https://www.googleapis.com/upload/drive/v3";
        private const string Scopes = "https://www.googleapis.com/auth/drive.appdata https://www.googleapis.com/auth/drive.file";

        private readonly string SerializationFileName;
        private const string SerializationPassword = "YOUR_SERIALIZATION_PASSWORD";
        private const string MimeTypeFolder = "application/vnd.google-apps.folder";

        static public readonly Newtonsoft.Json.JsonSerializer dateTimeSerializer = new Newtonsoft.Json.JsonSerializer
        {
            DateFormatString = "yyyy-MM-ddTH:mm:ss.ffK",
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local
        };

        private OAuthAccountWrapper Account = null;

        private string AppDataFolderID = "";

        private readonly Dictionary<string, DriveFileInfo> CacheDriveFileInfo = null;

        /// <summary>
        /// Impostare a true per ricordare i dati di accesso
        /// </summary>
        public bool RememberMe { get; private set; }

        // Messaggio dell'ultimo errore avvenuto
        private string LastError { get; set; }

        private IOAuth2Authenticator _OAuth2 = null;
        private IOAuth2Authenticator OAuth2
        {
            get
            {
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

        public GoogleDriveConnector()
        {
            RememberMe = false;
            SerializationFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                "cloudremember.me");
            CacheDriveFileInfo = new Dictionary<string, DriveFileInfo>();
            DeSerialize();
        }

        private async Task<string> GetAppDataFolderID()
        {
            if (AppDataFolderID != "")
                return AppDataFolderID;

            DriveFileInfo driveFileInfo = await GetFileInfo(AppDataFolder, true);
            if (driveFileInfo.Status == CloudFileStatus.FileError)
                throw new Exception("Unable to access to the App data folder");

            if (driveFileInfo.Status == CloudFileStatus.FileNotFound)
            {
                // Crea la cartella
                HttpClient AppClient = new HttpClient();
                try
                {
                    AppClient.DefaultRequestHeaders.Clear();
                    AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                    string Url = URLBase + "/files";

                    JObject jsonMetadata = new JObject
                    {
                        { "name", JValue.CreateString(AppDataFolder) },
                        { "mimeType", JValue.CreateString(MimeTypeFolder) }
                    };

                    StringContent Content = new StringContent(jsonMetadata.ToString(), Encoding.UTF8, "application/json");
                    HttpResponseMessage Response = await AppClient.PostAsync(new Uri(Url), Content);
                    if (!Response.IsSuccessStatusCode)
                        throw new Exception("Folder " + AppDataFolder + " not created. Status code:" + Response.StatusCode);

                    JObject jsonCreate = JObject.Parse(await Response.Content.ReadAsStringAsync());
                    if (jsonCreate != null)
                    {
                        driveFileInfo.Status = CloudFileStatus.FileFound;
                        driveFileInfo.FileID = jsonCreate["id"].ToString();
                    }
                    else
                        throw new Exception("Error occurred on parsing responde from Cloud");
                }
                catch (Exception Ex)
                {
                    Debug.WriteLine("GetAppDataFolderInfo: unable to create the App data folder");
                    throw new Exception("Unable to create the App data folder. Error: " + Ex.Message);
                }
                finally
                {
                    AppClient.Dispose();
                }
            }

            AppDataFolderID = driveFileInfo.FileID;
            return AppDataFolderID;
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
                string appDataFolderID = await GetAppDataFolderID();

                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                string Url = URLBase + "/files";

                JObject jsonMetadata = new JObject
                {
                    { "name", JValue.CreateString(folderName) },
                    { "mimeType", JValue.CreateString(MimeTypeFolder) },
                    { "parents", new JArray
                        {
                            appDataFolderID
                        }
                    }
                };

                StringContent Content = new StringContent(jsonMetadata.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage Response = await AppClient.PostAsync(new Uri(Url), Content);
                if (!Response.IsSuccessStatusCode)
                    throw new Exception(string.Format("Folder {0} not created. Status code: {1}", folderName, Response.StatusCode));                
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

            HttpClient AppClient = new HttpClient();
            try
            {
                DriveFileInfo fileInfo = await GetFileInfo(FileName);
                if (fileInfo.Status != CloudFileStatus.FileFound)
                {
                    Debug.WriteLine("Delete file " + FileName + " failed. Status code: " + fileInfo.Status);
                    throw new Exception("Delete file " + FileName + " failed. Status code: " + fileInfo.Status);
                }

                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                string Url = URLBase + "/files/" + fileInfo.FileID;
                HttpResponseMessage Response = await AppClient.DeleteAsync(new Uri(Url));
                if (!Response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("Delete file " + FileName + " failed. Status code: " + Response.StatusCode);
                    throw new Exception("Delete file " + FileName + " failed. Status code: " + Response.StatusCode);
                }

                RemoveCache(FileName);
                Debug.WriteLine("File " + FileName + " deleted");
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Unable to delete the file " + FileName + ". Error: " + Ex.Message);
                throw new Exception("Unable to delete the file " + FileName + ". Error: " + Ex.Message);
            }
            finally
            {
                AppClient.Dispose();
            }            
        }

        /// <summary>
        /// Verifica se un determinato file esiste su GoogleDrive.
        /// </summary>
        /// <param name="FileName">Nome del file</param>
        /// <returns>Restituisce FileNotFound se il file non viene trovato, FileFound se esiste
        /// o FileError in caso di errore. Chiamare la funzione GetLastError per informazioni sull'eventuale
        /// errore.</returns>
        public async Task<CloudFileStatus> FileExists(string FileName)
        {
            DriveFileInfo fileInfo = await GetFileInfo(FileName);
            return fileInfo.Status;
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
                        Account.OAuthAccount = null;
                        ForgetAccessData();
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
        /// Verifica se la cartella esiste nella root dell'applicazione.
        /// </summary>
        /// <param name="FolderName">Nome della cartella</param>
        /// <returns>Restituisce FileNotFound se la cartella non viene trovata, FileFound se esiste
        /// o FileError in caso di errore. Chiamare la funzione GetLastError per informazioni sull'eventuale
        /// errore.</returns>
        public async Task<CloudFileStatus> FolderExists(string FolderName)
        {
            // Su GoogleDrive le cartelle sono trattate come file
            return await FileExists(FolderName);
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
            if (!await CheckToken())
            {
                Debug.WriteLine("User is not logged in!");
                return CloudFileStatus.FileDownloadError;
            }

            try
            {
                DriveFileInfo driveFileInfo = await GetFileInfo(FileName);
                CloudFileStatus Result = driveFileInfo.Status;
                if (Result == CloudFileStatus.FileError || Result == CloudFileStatus.FileNotFound)                
                    throw new Exception(string.Format(string.Format("{0}: Unable to download the file {1}. Error: {2}", AppResources.errCloudDownload, FileName, GetLastError())));                

                // Posso scaricare il file                
                if (!await DownloadFile(driveFileInfo.FileID, DestFileName))
                    Result = CloudFileStatus.FileDownloadError;
                return CloudFileStatus.FileDownloaded;
            }
            catch(Exception e)
            {
                Debug.WriteLine(string.Format("GetFile error: {0}", e.Message));
                throw;
            }
        }

        private bool ReadCache(string path, out DriveFileInfo fileInfo)
        {
            return CacheDriveFileInfo.TryGetValue(path, out fileInfo);
        }

        private void SaveCache(string path, DriveFileInfo fileInfo)
        {
            CacheDriveFileInfo.Add(path, fileInfo);
        }

        private void RemoveCache(string path)
        {
            CacheDriveFileInfo.Remove(path);
        }

        /// <summary>
        /// Verifica se il file esiste e restituisce le informazioni al riguardo.
        /// </summary>
        /// <param name="path">Nome del file di cui ottenere informazioni</param>
        /// <param name="fromRoot">Passare true per ricercare il file o cartella nella root.
        /// Passare false (default) per cercare il file o cartella nella app data folder.</param>
        /// <returns>Oggetto con le informazioni sul file e sullo stato della ricerca</returns>
        private async Task<DriveFileInfo> GetFileInfo(string path, bool fromRoot=false)
        {
            const string UrlRoot = URLBase + "/files";

            LastError = "";

            DriveFileInfo Result = new DriveFileInfo
            {
                Status = CloudFileStatus.FileNotFound,
                FileName = path
            };

            if (!await CheckToken())
            {
                LastError = "User is not logged in!";
                Debug.WriteLine(LastError);

                Result.Status = CloudFileStatus.FileError;
                return Result;
            }

            // Legge prima in cache
            if (ReadCache(path, out DriveFileInfo fileInfo))
                return fileInfo;

            // Ottiene la lista dei file nella cartella dedicata all'App
            HttpClient appClient = new HttpClient();
            try
            {
                appClient.DefaultRequestHeaders.Clear();
                appClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                string parentID = (fromRoot ? "root" : await GetAppDataFolderID());
                string[] tokens = new[] { path };
                if (string.Compare(Path.GetFileName(path), path) != 0)
                    tokens = path.Split(new[] { Path.DirectorySeparatorChar });
                for (int i = 0; i < tokens.Length; i++)
                {
                    string filter = "'" + parentID + "' in parents";
                    string Url = UrlRoot + "?q=" + Uri.EscapeUriString("name='" + tokens[i] + "' and " + filter);
                    Url += "&fields=files(id, name)";

                    HttpResponseMessage Response = await appClient.GetAsync(new Uri(Url));
                    if (!Response.IsSuccessStatusCode)
                    {
                        LastError = "Unable to find the file " + path + ". Status code: " + Response.StatusCode;
                        Debug.WriteLine(LastError);

                        Result.Status = CloudFileStatus.FileError;
                        return Result;
                    }

                    JObject JsonObj = JObject.Parse(await Response.Content.ReadAsStringAsync());
                    if (JsonObj != null)
                    {
                        if (JsonObj.ContainsKey("files"))
                        {
                            JArray jsonFiles = JsonObj["files"].ToObject<JArray>();
                            if (jsonFiles.Count == 0)
                            {
                                Result.Status = CloudFileStatus.FileNotFound;
                                return Result;
                            }

                            if (jsonFiles.Count > 1)
                            {
                                LastError = "Unable to find the file " + path + ". Too many result for token " + tokens[i];
                                Debug.WriteLine(LastError);

                                Result.Status = CloudFileStatus.FileError;
                                return Result;
                            }

                            parentID = (jsonFiles[0].ToObject<JObject>())["id"].ToString();
                        }
                        else
                        {
                            LastError = "Parse error for file " + path + ". Property 'files' not found for token " + tokens[i];
                            Debug.WriteLine(LastError);

                            Result.Status = CloudFileStatus.FileError;
                            return Result;
                        }
                    }
                    else
                    {
                        LastError = "Parse error for file " + path + ". Invalid response for token " + tokens[i];
                        Debug.WriteLine(LastError);

                        Result.Status = CloudFileStatus.FileError;
                        return Result;
                    }
                }

                // Il file esiste
                Result.FileID = parentID;
                Result.Status = CloudFileStatus.FileFound;

                SaveCache(path, Result);
            }
            catch (Exception Ex)
            {
                LastError = "Unable to find the file " + path + ": " + Ex.Message;
                Debug.WriteLine(LastError);

                Result.Status = CloudFileStatus.FileError;
                return Result;
            }
            finally
            {
                appClient.Dispose();
            }

            return Result;
        }

        /// <summary>
        /// Scarica un file da GoogleDrive
        /// </summary>
        /// <param name="fileID">ID del file da scaricare</param>
        /// <param name="DestFileName">Nome del file di destinazione</param>
        /// <returns>Restituisce true se l'operazione va a buon fine, false altrimenti</returns>
        private async Task<bool> DownloadFile(string fileID, string destFileName)
        {
            bool Result = false;

            HttpClient AppClient = new HttpClient();
            try
            {
                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                string Url = URLBase + "/files/" + fileID + "?alt=media";
                HttpResponseMessage Response = await AppClient.GetAsync(new Uri(Url));
                if (!Response.IsSuccessStatusCode)
                    throw new Exception("Unable to read the file. Status code: " + Response.StatusCode);

                // Scarico il contenuto del file
                Stream streamContent = await Response.Content.ReadAsStreamAsync();
                if (streamContent.Length > 0)
                {
                    streamContent.Position = 0;
                    byte[] data = new byte[streamContent.Length];
                    await streamContent.ReadAsync(data, 0, data.Length);

                    File.WriteAllBytes(destFileName, data);
                    Result = File.Exists(destFileName);
                }
            }
            catch (Exception Ex)
            {
                string MessageEx = "Unable to download the file with ID " + fileID + ". Error: " + Ex.Message;
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
        /// Restituisce il messaggio di errore dell'ultimo errore avvenuto.
        /// </summary>
        /// <returns>Messaggio di errore</returns>
        public string GetLastError()
        {
            return LastError;
        }

        public bool GetRememberMe()
        {
            return RememberMe;
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
                                           new Uri(URLToken),
                                           true);
                return await Task.FromResult(true);
            }
            catch (Exception Ex)
            {
                LastError = Ex.Message;
                string msg = AppResources.errCloudAuth + " " + Ex.Message;
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
                string data = File.ReadAllText(SerializationFileName, Encoding.UTF8);
                EncDecHelper encDecHelper = new EncDecHelper
                {
                    PasswordString = SerializationPassword
                };

                Xamarin.Auth.Account account = Xamarin.Auth.Account.Deserialize(encDecHelper.AESDecrypt(data, out bool warning));
                if (warning)
                {
                    Debug.Write(string.Format("Google Drive serialize file {0}. File seems to be corrupted. I have tried to recover the data.",
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

        private bool DoLogout()
        {
            Account.OAuthAccount = null;
            ForgetAccessData();

            Debug.WriteLine("User logged out");
            LoggedOut?.Invoke(this, new EventArgs());
            return true;
        }

        public async Task<bool> Logout()
        {
            if (!IsLoggedIn())
            {
                Debug.WriteLine("Logout: user is not logged in!");
                LoggedOut?.Invoke(this, new EventArgs());
                return true;    // L'utente non è loggato
            }

            // Non c'è un logout dall'App... l'utente dovrebbe fare il logout da Google.
            // Per questo motivo, semplicemente mi dimentico dell'access token.
            return await Task.FromResult(DoLogout());
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
        /// Impostare a true per permettere di ricordare i dati di accesso
        /// al Cloud.
        /// </summary>
        /// <param name="remember">Valore booleano della scelta</param>
        public void SetRememberMe(bool remember)
        {
            RememberMe = remember;
        }

        /// <summary>
        /// Carica un file su GoogleDrive
        /// </summary>
        /// <param name="FileName">Nome del file da caricare</param>
        /// <param name="DestFileName">Nome del file su GoogleDrive</param>
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
                string appDataFolderID = await GetAppDataFolderID();

                AppClient.DefaultRequestHeaders.Clear();
                AppClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                // Verifica se il file esiste già su Cloud
                DriveFileInfo driveFileInfo = await GetFileInfo(DestFileName);
                bool update = (driveFileInfo.Status == CloudFileStatus.FileFound);

                // Cartella di destinazione del file
                JArray parentsID = new JArray(new[] { appDataFolderID });
                if (string.Compare(Path.GetFileName(DestFileName), DestFileName) != 0)
                {
                    string[] tokens = DestFileName.Split(new[] { Path.DirectorySeparatorChar });
                    if (tokens.Length > 1)
                    {
                        DriveFileInfo folderInfo = await GetFileInfo(tokens[tokens.Length - 2]);
                        if (folderInfo.Status != CloudFileStatus.FileFound)
                            throw new Exception("Unable to get the ID of the folder for file " + DestFileName);
                        parentsID[0] = new JValue(folderInfo.FileID);
                    }
                }

                // Metadati per l'upload
                HttpResponseMessage Response = null;
                JObject jsonMetadata = new JObject
                {
                    { "name", new JValue(Path.GetFileName(DestFileName)) },
                    { "parents", parentsID }
                };
                if (update)
                    jsonMetadata.Add("id", new JValue(driveFileInfo.FileID));

                // Due modalità diverse a seconda delle dimensioni del file
                FileInfo fileInfo = new FileInfo(FileName);
                if (fileInfo.Length > 5 * 1024 * 1024)
                {
                    string url = URLBaseUpload + "/files?uploadType=resumable";
                    if (update)
                        url = URLBaseUpload + "/files/" + driveFileInfo.FileID + "?uploadType=resumable";

                    AppClient.DefaultRequestHeaders.Add("X-Upload-Content-Length", fileInfo.Length.ToString());
                    if (update)
                    {
                        HttpRequestMessage PatchRequest = new HttpRequestMessage(new HttpMethod("PATCH"), new Uri(url));
                        Response = await AppClient.SendAsync(PatchRequest);
                    }
                    else
                    {
                        StringContent metatdataContent = new StringContent(jsonMetadata.ToString(), Encoding.UTF8, "application/json");
                        Response = await AppClient.PostAsync(new Uri(url), metatdataContent);
                    }

                    if (!Response.IsSuccessStatusCode)                    
                        throw new Exception(string.Format("File {0} not uploaded. Status code: {1}", FileName, Response.StatusCode));                    

                    long read = 0, start = 0;
                    FileStream fileStream = new FileStream(FileName, FileMode.Open);
                    Uri Location = Response.Headers.Location;
                    try
                    {
                        AppClient.DefaultRequestHeaders.Clear();
                        do
                        {
                            read = Math.Min(256 * 1024, fileStream.Length - start);
                            if (read > 0)
                            {
                                int attempt = 0;
                                do
                                {
                                    byte[] buffer = new byte[read];
                                    fileStream.Position = start;
                                    await fileStream.ReadAsync(buffer, 0, (int)read);

                                    ByteArrayContent chunkData = new ByteArrayContent(buffer);
                                    chunkData.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(start, start + read - 1, fileStream.Length);
                                    chunkData.Headers.ContentLength = read;

                                    Response = await AppClient.PutAsync(Location, chunkData);
                                    if (Response.IsSuccessStatusCode)
                                        break;
                                    if (Response.StatusCode != (System.Net.HttpStatusCode)308)
                                    {
                                        await Task.Delay(1000);
                                        attempt++;
                                    }
                                }
                                while (attempt < 10 && Response.StatusCode != (System.Net.HttpStatusCode)308);
                                if (attempt >= 10 || (!Response.IsSuccessStatusCode && Response.StatusCode != (System.Net.HttpStatusCode)308))                                
                                    throw new Exception(string.Format("File {0} not uploaded. Status code: {1}", FileName, Response.StatusCode));                                
                                start += read;
                            }
                        }
                        while (read > 0);
                    }
                    catch (HttpRequestException Ex)
                    {
                        Debug.WriteLine("UploadFile error: " + Ex.Message);
                        throw;
                    }
                    finally
                    {
                        fileStream.Dispose();
                    }
                }
                else
                {
                    // Caricamento per intero. Il file è piccolo
                    string url = URLBaseUpload + "/files?uploadType=multipart";
                    if (update)
                        url = URLBaseUpload + "/files/" + driveFileInfo.FileID + "?uploadType=multipart";
                    bool fail = false;
                    int attempt = 0;
                    do
                    {
                        FileStream fileStream = new FileStream(FileName, FileMode.Open);
                        try
                        {
                            if (update)
                            {
                                HttpRequestMessage PatchRequest = new HttpRequestMessage(new HttpMethod("PATCH"), new Uri(url))
                                {
                                    Content = new StreamContent(fileStream)
                                };
                                Response = await AppClient.SendAsync(PatchRequest);
                            }
                            else
                            {
                                MultipartContent multipart = new MultipartContent("related")
                                {
                                    new StringContent(jsonMetadata.ToString(), Encoding.UTF8, "application/json"),
                                    new StreamContent(fileStream)
                                };
                                Response = await AppClient.PostAsync(new Uri(url), multipart);
                            }
                        }
                        finally
                        {
                            fileStream.Close();
                        }

                        if ((fail=!Response.IsSuccessStatusCode))
                        {
                            attempt++;
                        }                        
                    }
                    while (attempt < 5 && fail);    // Prova al massimo 5 volte

                    if (fail)                    
                        throw new Exception(string.Format("File {0} not uploaded. Status code: {1}", FileName, Response.StatusCode));                                            
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
        /// Verifica se il token è valido
        /// </summary>
        /// <returns>Restituisce true se il token è valido, false altrimenti</returns>
        public async Task<bool> ValidateToken()
        {
            if (!IsLoggedIn())
            {
                Debug.WriteLine("Token validation failed!\nUser is not logged in.");
                return false;
            }

            HttpClient appClient = new HttpClient();
            try
            {
                string Url = "https://www.googleapis.com/oauth2/v3/tokeninfo?access_token=" + Account.AccessToken;

                HttpResponseMessage Response = await appClient.GetAsync(new Uri(Url));
                if (!Response.IsSuccessStatusCode)
                    return false;

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

        // <summary>
        /// Restituisce le informazioni sui file contenuti nella cartella specificata.
        /// </summary>
        /// <param name="FolderName">Nome della cartella</param>
        /// <returns>Lista degli oggetti di tipo CloudFileInfo con le informazioni sui file nella cartella</returns>
        public async Task<IList<CloudFileInfo>> GetFilesInfo(string FolderName)
        {
            const string UrlRoot = URLBase + "/files";

            LastError = "";

            if (!await CheckToken())
            {
                LastError = "User is not logged in!";
                Debug.WriteLine(LastError);
                return null;
            }

            // Ottiene la lista dei file nella cartella
            HttpClient appClient = new HttpClient();
            try
            {
                appClient.DefaultRequestHeaders.Clear();
                appClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Account.AccessToken);

                string parentID = await GetAppDataFolderID();
                string[] tokens = new[] { FolderName };
                if (string.Compare(Path.GetFileName(FolderName), FolderName) != 0)
                    tokens = FolderName.Split(new[] { Path.DirectorySeparatorChar });
                for (int i = 0; i < tokens.Length + 1; i++)
                {
                    string token = "";
                    string filter = "'" + parentID + "' in parents";
                    string Url = UrlRoot;
                    if (i <= tokens.Length - 1)
                    {
                        token = tokens[i];
                        Url += "?q=" + Uri.EscapeUriString("name='" + token + "' and " + filter);
                    }
                    else
                    {
                        token = parentID;
                        Url += "?q=" + Uri.EscapeUriString(filter); // Tutti i file nella cartella identificata da parentID
                    }
                    Url += "&fields=files(id, name, kind, createdTime, modifiedTime, size, webViewLink)";

                    HttpResponseMessage Response = await appClient.GetAsync(new Uri(Url));
                    if (!Response.IsSuccessStatusCode)
                    {
                        LastError = "Unable to find the files in folder " + FolderName + ". Status code: " + Response.StatusCode;
                        Debug.WriteLine(LastError);
                        return null;
                    }

                    JObject JsonObj = JObject.Parse(await Response.Content.ReadAsStringAsync());
                    if (JsonObj == null)
                        throw new Exception("Parse error. Invalid response for token " + token);
                    
                    if (!JsonObj.ContainsKey("files"))
                        throw new Exception("Parse error for folder " + FolderName + ". Property 'files' not found for token " + token);
                    
                    JArray jsonFiles = JsonObj["files"].ToObject<JArray>();
                    if (jsonFiles.Count == 0)
                        return null;

                    if (jsonFiles.Count > 1 && i <= tokens.Length - 1)
                    {
                        LastError = "Unable to find the files in folder " + FolderName + ". Too many result for token " + token;
                        Debug.WriteLine(LastError);
                        return null;
                    }

                    if (i == tokens.Length)
                    {
                        List<CloudFileInfo> files = new List<CloudFileInfo>();
                        foreach (JObject file in jsonFiles)
                        {
                            string kind = file["kind"].ToString();
                            if (kind == "drive#file")
                            {
                                files.Add(new CloudFileInfo
                                {
                                    ID = file["id"].ToString(),
                                    Name = file["name"].ToString(),
                                    CreatedDateTime = file["createdTime"].ToObject<DateTime>(dateTimeSerializer),
                                    LastModifiedDateTime = file["modifiedTime"].ToObject<DateTime>(dateTimeSerializer),
                                    Size = file["size"] != null ? file["size"].ToObject<long>() : 0,
                                    DownloadUrl = file["webViewLink"].ToString(),
                                    WebUrl = file["webViewLink"].ToString()
                                });
                            }
                        }
                        return files;
                    }
                    else
                        parentID = (jsonFiles[0].ToObject<JObject>())["id"].ToString();                  
                }

                // Nessun risultato
                return null;
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

using System;
using Xamarin.Forms;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V4.Hardware.Fingerprint;
using Android.Content.Res;
using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;

[assembly: Dependency(typeof(PwdCrypter.Droid.FingerprintAuth))]
namespace PwdCrypter.Droid
{
    class FingerprintAuth : IFingerprintAuth
    {
        /* 
         * Creata copia della costante USE_FINGERPRINT, deprecata dalle API 28 in poi.
         * Bisognerebbe usare USE_BIOMETRIC (che include anche il riconoscimento facciale) ma voglio
         * gestire SOLO la lettura dell'impronta digitale.
         */
        const string USE_FINFERPRINT_PERMISSION = "android.permission.USE_FINGERPRINT";

        private FingerprintManagerCompat FingerprintManager = null;
        private Android.Support.V4.OS.CancellationSignal FingerprintCancel = null;

        public event FingerprintAuthenticationSucceeded OnAuthenticationSucceeded;
        public event FingerprintAuthenticationFailed OnAuthenticationFailed;
        public event FingerprintAuthenticationError OnAuthenticationError;
        public event FingerprintAuthenticationCanceled OnAuthenticationCanceled;
        public event FingerprintAuthenticationHelp OnAuthenticationHelp;

        private Context CurrentContext => Android.App.Application.Context;
        private Resources Resources => CurrentContext?.Resources;

        private FingerprintSignOp CurrentOperation = FingerprintSignOp.Crypt;

        public void Authenticate(byte[] secret, FingerprintSignOp operation)
        {
            const int flags = 0; /* Sempre zero */

            if (!IsReady())
                throw new Exception(Resources.GetString(Resource.String.errFpNotRead));

            // Estrae l'initialization vector
            byte[] iv = new byte[] { };
            if (operation == FingerprintSignOp.Decrypt)
            {
                string data = System.Text.Encoding.UTF8.GetString(secret);
                string[] base64Data = data.Split(new char[] { ';' });
                if (base64Data.Length < 2)
                    throw new Exception("Invalid data");
                secret = Convert.FromBase64String(base64Data[0]);
                iv = Convert.FromBase64String(base64Data[1]);
            }

            // Oggetto per la verifica dell'integrità dell'impronta digitale
            CurrentOperation = operation;
            CryptoObjectHelper cryptoHelper = new CryptoObjectHelper(operation == FingerprintSignOp.Crypt ? CipherMode.EncryptMode : CipherMode.DecryptMode, iv);

            // Segnale per la cancellazione manuale della lettura dell'impronta 
            FingerprintCancel = new Android.Support.V4.OS.CancellationSignal();

            // Oggetto per la gestione della callback di autenticazione
            FingerprintManagerCompat.AuthenticationCallback authenticationCallback = new FingerprintAuthCallback(this, secret);

            // Avvia lo scanner
            FingerprintManager.Authenticate(cryptoHelper.BuildCryptoObject(), flags, FingerprintCancel, authenticationCallback, null);
        }

        /// <summary>
        /// Cancella la richiesta di lettura dell'impronta digitale
        /// </summary>
        public void Cancel()
        {
            FingerprintCancel?.Cancel();
        }

        /// <summary>
        /// Inizializza lo scanner per l'impronta digitale
        /// </summary>
        public void Init()
        {
            try
            {
                FingerprintManagerCompat fingerprintManagerCmp = FingerprintManagerCompat.From(CurrentContext);
                if (!fingerprintManagerCmp.IsHardwareDetected)
                    throw new Exception(Resources.GetString(Resource.String.errFpHWNotDetected));

                // Il dispositivo deve essere protetto da blocca schermo, altrimenti non si può usare
                // l'impronta digitale
                KeyguardManager keyguardManager = (KeyguardManager)CurrentContext.GetSystemService(Context.KeyguardService);
                if (!keyguardManager.IsKeyguardSecure)
                    throw new Exception(Resources.GetString(Resource.String.errFpLockScreen));

                // Verifica la presenza della registrazione di almeno una impronta digitale
                if (!HasEnrolledFingerprints(fingerprintManagerCmp))
                    throw new Exception(Resources.GetString(Resource.String.errFpNotFound));

                // Verifica delle autorizzazioni da Android 6 in poi
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    Android.Content.PM.Permission permissionResult = ContextCompat.CheckSelfPermission(CurrentContext, USE_FINFERPRINT_PERMISSION);
                    if (permissionResult == Android.Content.PM.Permission.Granted)
                    {
                        FingerprintManager = fingerprintManagerCmp;
                    }
                    else
                    {
                        // No permission. Go and ask for permissions and don't start the scanner. See
                        // https://developer.android.com/training/permissions/requesting.html
                        MainActivity.Instance.RequestPermissions(new[] { USE_FINFERPRINT_PERMISSION }, 0);
                        // Verifico di nuovo i permessi
                        permissionResult = ContextCompat.CheckSelfPermission(CurrentContext, USE_FINFERPRINT_PERMISSION);
                        if (permissionResult != Android.Content.PM.Permission.Granted)
                            throw new Exception(Resources.GetString(Resource.String.errFpPermissionDenied));
                    }
                }
            }
            catch (Exception)
            {
                FingerprintManager = null;
                throw;
            }
        }

        private bool HasEnrolledFingerprints(FingerprintManagerCompat fingerprintManagerCmp = null)
        {
            try
            {
                FingerprintManagerCompat fpManager = fingerprintManagerCmp;
                if (fpManager == null)
                    fpManager = FingerprintManagerCompat.From(CurrentContext);
                if (!fpManager.IsHardwareDetected)
                    throw new Exception(Resources.GetString(Resource.String.errFpHWNotDetected));
                if (!fpManager.HasEnrolledFingerprints)
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error(App.Title, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Restituisce true se lo scanner dell'impronta digitale è stato inizializzato
        /// con successo.
        /// </summary>
        /// <returns>True se lo scanner è utilizzabile, false altrimenti.</returns>
        public bool IsReady()
        {
            return FingerprintManager != null;
        }

        /// <summary>
        /// Verifica se il lettore delle impronte digitali è disponibile, con almeno
        /// la registrazione di una impronta.
        /// </summary>
        /// <returns>True se lo scanner è disponibile e c'è almeno una impronta registrata, false altrimenti.</returns>
        public bool IsAvailable()
        {
            return HasEnrolledFingerprints();
        }

        /// <summary>
        /// Classe per la verifica dell'integrità dell'impronta digitale
        /// https://docs.microsoft.com/it-it/xamarin/android/platform/fingerprint-authentication/creating-a-cryptoobject
        /// </summary>
        private class CryptoObjectHelper
        {
            // This can be key name you want. Should be unique for the app.
            static readonly string KEY_NAME = "com.xamarin.android.pwdcrypter.fingerprint_authentication_key";

            // We always use this keystore on Android.
            static readonly string KEYSTORE_NAME = "AndroidKeyStore";

            // Should be no need to change these values.
            static readonly string KEY_ALGORITHM = KeyProperties.KeyAlgorithmAes;
            static readonly string BLOCK_MODE = KeyProperties.BlockModeCbc;
            static readonly string ENCRYPTION_PADDING = KeyProperties.EncryptionPaddingPkcs7;
            static readonly string TRANSFORMATION = KEY_ALGORITHM + "/" +
                                                    BLOCK_MODE + "/" +
                                                    ENCRYPTION_PADDING;
            readonly KeyStore _keystore;
            readonly CipherMode _mode;
            readonly byte[] _iv;

            public CryptoObjectHelper(CipherMode cipherMode, byte[] iv)
            {
                _mode = cipherMode;
                _iv = iv;
                _keystore = KeyStore.GetInstance(KEYSTORE_NAME);
                _keystore.Load(null);
            }

            public FingerprintManagerCompat.CryptoObject BuildCryptoObject()
            {
                Cipher cipher = CreateCipher();
                return new FingerprintManagerCompat.CryptoObject(cipher);
            }

            Cipher CreateCipher(bool retry = true)
            {
                IKey key = GetKey();
                Cipher cipher = Cipher.GetInstance(TRANSFORMATION);
                try
                {
                    if (_iv.Length > 0)
                        cipher.Init(_mode, key, new IvParameterSpec(_iv));
                    else
                        cipher.Init(_mode, key);
                }
                catch (KeyPermanentlyInvalidatedException e)
                {
                    _keystore.DeleteEntry(KEY_NAME);
                    if (retry)
                    {
                        CreateCipher(false);
                    }
                    else
                    {
                        throw new System.Exception("Could not create the cipher for fingerprint authentication.", e);
                    }
                }
                return cipher;
            }

            IKey GetKey()
            {
                IKey secretKey;
                if (!_keystore.IsKeyEntry(KEY_NAME))
                {
                    CreateKey();
                }

                secretKey = _keystore.GetKey(KEY_NAME, null);
                return secretKey;
            }

            void CreateKey()
            {
                KeyGenerator keyGen = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, KEYSTORE_NAME);
                KeyGenParameterSpec keyGenSpec =
                    new KeyGenParameterSpec.Builder(KEY_NAME, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                        .SetBlockModes(BLOCK_MODE)
                        .SetEncryptionPaddings(ENCRYPTION_PADDING)
                        .SetUserAuthenticationRequired(true)
                        .Build();
                keyGen.Init(keyGenSpec);
                keyGen.GenerateKey();
            }
        }

        /// <summary>
        /// Classe per la gestione delle callback durante la fase di autenticazione con l'impronta digitale
        /// https://docs.microsoft.com/it-it/xamarin/android/platform/fingerprint-authentication/fingerprint-authentication-callbacks
        /// </summary>
        private class FingerprintAuthCallback : FingerprintManagerCompat.AuthenticationCallback
        {
            private readonly byte[] SECRET_BYTES;

            // The TAG can be any string, this one is for demonstration.
            static readonly string TAG = "X:" + App.Title + "/FpCallback";

            private readonly FingerprintAuth AuthManager = null;

            public FingerprintAuthCallback(FingerprintAuth fingerprintAuth, byte[] secret)
            {
                AuthManager = fingerprintAuth;
                SECRET_BYTES = secret;
            }

            public override void OnAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
            {
                if (result.CryptoObject.Cipher != null)
                {
                    try
                    {
                        byte[] secret = SECRET_BYTES;
                        
                        // Calling DoFinal on the Cipher ensures that the encryption worked.
                        byte[] doFinalResult = result.CryptoObject.Cipher.DoFinal(secret);
                        if (AuthManager.CurrentOperation == FingerprintSignOp.Crypt)
                        {
                            byte[] iv = result.CryptoObject.Cipher.GetIV();
                            string data = Convert.ToBase64String(doFinalResult, Base64FormattingOptions.None) + ";" +
                                Convert.ToBase64String(iv, Base64FormattingOptions.None);
                            doFinalResult = System.Text.Encoding.UTF8.GetBytes(data);
                        }

                        // No errors occurred, trust the results.
                        Android.Util.Log.Info(TAG, "Authenticated with success");
                        AuthManager.OnAuthenticationSucceeded?.Invoke(doFinalResult);
                    }
                    catch (BadPaddingException bpe)
                    {
                        // Can't really trust the results.
                        Android.Util.Log.Error(TAG, "Failed to encrypt the data with the generated key." + bpe);
                    }
                    catch (IllegalBlockSizeException ibse)
                    {
                        // Can't really trust the results.
                        Android.Util.Log.Error(TAG, "Failed to encrypt the data with the generated key." + ibse);
                    }
                }
                else
                {
                    // No cipher used, assume that everything went well and trust the results.
                    Android.Util.Log.Info(TAG, "Authenticated with success. No cipher used");
                    AuthManager.OnAuthenticationSucceeded?.Invoke(new byte[] { });
                }
            }

            public override void OnAuthenticationError(int errMsgId, Java.Lang.ICharSequence errString)
            {
                // Report the error to the user. Note that if the user canceled the scan,
                // this method will be called and the errMsgId will be FingerprintState.ErrorCanceled.
                if ((Android.Hardware.Fingerprints.FingerprintState)errMsgId == Android.Hardware.Fingerprints.FingerprintState.ErrorCanceled)
                    AuthManager.OnAuthenticationCanceled?.Invoke();
                else
                    AuthManager.OnAuthenticationError?.Invoke(errMsgId, errString.ToString());
            }

            public override void OnAuthenticationFailed()
            {
                // Tell the user that the fingerprint was not recognized.
                AuthManager.OnAuthenticationFailed?.Invoke();
            }

            public override void OnAuthenticationHelp(int helpMsgId, Java.Lang.ICharSequence helpString)
            {
                // Notify the user that the scan failed and display the provided hint.
                AuthManager.OnAuthenticationHelp?.Invoke(helpMsgId, helpString.ToString());
            }
        }
    }
}
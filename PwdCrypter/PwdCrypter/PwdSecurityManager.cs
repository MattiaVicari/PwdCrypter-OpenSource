using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using Zxcvbn;

namespace PwdCrypter
{
    public class PwdSecurityManager
    {
        public enum PwdOperationStatus { Started, Finished, Aborted, Pending, Timeout };

        private Alert PwdAlert = null;
        private Zxcvbn.Zxcvbn StrengthCalculator = null;

        /// <summary>
        /// Vale true se è necessario rinnovare il login per proseguire con l'operazione
        /// sulla password
        /// </summary>
        public bool RenewLogin { get; private set; }

        /// <summary>
        /// Stato delle operazioni
        /// </summary>
        public PwdOperationStatus OperationStatus { get; private set; }


        public PwdSecurityManager()
        {
            RenewLogin = false;
            OperationStatus = PwdOperationStatus.Pending;
            StrengthCalculator = new Zxcvbn.Zxcvbn();
        }

        /// <summary>
        /// Indica l'avvio di una operazione sulle password
        /// </summary>
        public void BeginOperation()
        {
            EndOperation();

            RenewLogin = false;
            OperationStatus = PwdOperationStatus.Started;

            PwdAlert = new Alert
            {
                Timeout = App.Settings.SessionTimeout
            };
            PwdAlert.OnTimeout += PwdAlert_OnTimeout;
            PwdAlert.StartTimer();
            Debug.WriteLine("Start operation in session");
        }

        /// <summary>
        /// Rinnova il timer della sessione
        /// </summary>
        public void RenewOperation()
        {
            if (PwdAlert == null)
                return;

            Debug.WriteLine("Renew session timer...");

            PwdAlert.OnTimeout -= PwdAlert_OnTimeout;
            PwdAlert = null;
            BeginOperation();
        }

        private void PwdAlert_OnTimeout(object sender, EventArgs e)
        {
            if (OperationStatus == PwdOperationStatus.Finished ||
                OperationStatus == PwdOperationStatus.Aborted)
            {
                return;
            }

            RenewLogin = true;
            OperationStatus = PwdOperationStatus.Timeout;
            Debug.WriteLine("Session timeout");
        }

        /// <summary>
        /// Indica che le operazioni sulle password sono terminate
        /// </summary>
        public void EndOperation()
        {
            RenewLogin = false;
            OperationStatus = PwdOperationStatus.Finished;
            if (PwdAlert != null)
                PwdAlert.OnTimeout -= PwdAlert_OnTimeout;
            PwdAlert = null;
            Debug.WriteLine("End operation in session");
        }

        /// <summary>
        /// Indica che le operazioni sulle password sono state cancellate
        /// </summary>
        public void AbortOperation()
        {
            RenewLogin = false;
            OperationStatus = PwdOperationStatus.Aborted;
            if (PwdAlert != null)
                PwdAlert.OnTimeout -= PwdAlert_OnTimeout;
            PwdAlert = null;
            Debug.WriteLine("Abort operation in session");
        }

        /// <summary>
        /// Verifica se è possibile effettuare l'operazione.
        /// Se la sessione è terminata, richiama la pagina per reinserire la password di accesso.
        /// </summary>
        /// <param name="page">pagina a cui ritornare dopo l'inserimento della password di accesso</param>
        /// <param name="operation">nome identificativo dell'operazione che si sta svolgendo</param>
        /// <returns>Restituisce true se si può proseguire con l'operazione, false altrimenti.</returns>
        public async Task<bool> CheckOperation(Page page, string operation)
        {
            Debug.WriteLine(string.Format("Checking for operation {0}...", operation));
            if (RenewLogin)
            {
                Debug.WriteLine("Renew login needed");
                await page.Navigation.PushModalAsync(new LoginPage
                {
                    BindingContext = new Utility.RedirectData
                    {
                        RedirectTo = page,
                        Modal = true,
                        MessageText = AppResources.msgSessionTimeout
                    }
                }, true);
                return false;
            }
            else
            {
                Debug.WriteLine("Session is active");
            }
            return true;
        }

        /// <summary>
        /// Restituisce il livello di sicurezza della password (tra 0 = debole e 4 = molto forte)
        /// </summary>
        /// <param name="password">Password da verificare</param>
        /// <returns>Livello di sicurezza della password</returns>
        public int ComputePasswordStrength(string password)
        {
            Result strenght = StrengthCalculator.EvaluatePassword(password);
            return strenght.Score;
        }
    }
}

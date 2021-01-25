using Plugin.InAppBilling;
using Plugin.InAppBilling.Abstractions;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PwdCrypter
{
    public class BillingManager
    {
        private const string sourceApp = "app";    // sorgente di sblocco App
        private const string sourceWeb = "web";    // sorgente di sblocco Web

        public const string PLUSProductID_Win = "PLUS";
        public const string PLUSProductID_Android = "plus";
        private const string DevPayload = "PUT_SOMETHING_UNIQUE";

        public static string PLUSProductID
        {
            get
            {
                if (Device.RuntimePlatform == Device.Android)
                    return PLUSProductID_Android;
                return PLUSProductID_Win;
            }
        }

        public BillingManager()
        {
#if DEBUG
            CrossInAppBilling.Current.InTestingMode = true;
#endif
        }

        private string GetPurchaseErrorMessage(PurchaseError error)
        {
            switch (error)
            {
                case PurchaseError.BillingUnavailable:
                    return AppResources.errBillingUnavailable;
                case PurchaseError.DeveloperError:
                    return AppResources.errDeveloperError;
                case PurchaseError.ItemUnavailable:
                    return AppResources.errItemUnavailable;
                default:
                case PurchaseError.GeneralError:
                    return AppResources.errGeneralError;
                case PurchaseError.UserCancelled:
                    return AppResources.errUserCancelled;
                case PurchaseError.AppStoreUnavailable:
                    return AppResources.errAppStoreUnavailable;
                case PurchaseError.PaymentNotAllowed:
                    return AppResources.errPaymentNotAllowed;
                case PurchaseError.PaymentInvalid:
                    return AppResources.errPaymentInvalid;
                case PurchaseError.InvalidProduct:
                    return AppResources.errInvalidProduct;
                case PurchaseError.ProductRequestFailed:
                    return AppResources.errProductRequestFailed;
                case PurchaseError.RestoreFailed:
                    return AppResources.errRestoreFailed;
                case PurchaseError.ServiceUnavailable:
                    return AppResources.errServiceUnavailable;
                case PurchaseError.AlreadyOwned:
                    return AppResources.errAlreadyOwned;
                case PurchaseError.NotOwned:
                    return AppResources.errNotOwned;
            }
        }

        /// <summary>
        /// Effettua l'acquista di un prodotto
        /// </summary>
        /// <param name="productId">Identificatibo del prodotto</param>
        /// <param name="subscription">Passare true se il prodotto è un abbonamento</param>
        /// <returns>Restituisce il token dell'acquisto. Utilizzarlo eventualmente per consumare il prodotto.</returns>
        public async Task<string> Purchase(string productId, bool subscription=false)
        {
            if (!CrossInAppBilling.IsSupported)
                throw new Exception(AppResources.errInAppBillingNotSupported);

            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync(ItemType.InAppPurchase);
                if (!connected)
                    throw new Exception(AppResources.errInAppBillingNotConnected);

                var verify = DependencyService.Get<IInAppBillingVerifyPurchase>();
                var purchase = await billing.PurchaseAsync(productId, subscription ? ItemType.Subscription : ItemType.InAppPurchase, DevPayload, verify);
#if DEBUG
                if (billing.InTestingMode)
                    return "test-purchasing-token";
#endif
                if (purchase == null)
                    throw new Exception(AppResources.errUnableToPurchase);
                if (purchase.State != PurchaseState.Purchased)
                    throw new Exception(AppResources.errUnableToPurchase + "\nState: " + purchase.State);

                return purchase.PurchaseToken;
            }
            catch (InAppBillingPurchaseException Ex)
            {
                Debug.WriteLine("Purchase error: " + Ex.Message);
                throw new Exception(GetPurchaseErrorMessage(Ex.PurchaseError));
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        /// <summary>
        /// Consuma un prodotto acquistato in precedenza
        /// </summary>
        /// <param name="productId">Identificativo del prodotto acquistato</param>
        /// <param name="purchaseToken">Token identificativo dell'acquisto</param>
        /// <returns></returns>
        public async Task ConsumeProduct(string productId, string purchaseToken)
        {
            if (!CrossInAppBilling.IsSupported)
                throw new Exception(AppResources.errInAppBillingNotSupported);

            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync(ItemType.InAppPurchase);
                if (!connected)
                    throw new Exception(AppResources.errInAppBillingNotConnected);

                // Per iOS non devo fare altro. Il prodotto è già consumato al momento dell'acquisto
                if (Device.RuntimePlatform == Device.iOS)
                    return;

                var consumedItem = await CrossInAppBilling.Current.ConsumePurchaseAsync(productId, purchaseToken);
                if (consumedItem == null)
                    throw new Exception(AppResources.errConsumeProduct);
            }
            catch (InAppBillingPurchaseException Ex)
            {
                Debug.WriteLine("ConsumeProduct error: " + Ex.Message);
                throw new Exception(GetPurchaseErrorMessage(Ex.PurchaseError));
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        /// <summary>
        /// Restituisce true se il prodotto è già stato acquistato
        /// </summary>
        /// <param name="productId">Identificativo del prodotto</param>
        /// <returns>True se il prodotto è stato acquistato, false altrimenti</returns>
        public async Task<bool> IsPurchase(string productId)
        {
            if (!CrossInAppBilling.IsSupported)
                throw new Exception(AppResources.errInAppBillingNotSupported);

            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync(ItemType.InAppPurchase);
                if (!connected)
                    throw new Exception(AppResources.errInAppBillingNotConnected);

                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
                if (purchases?.Any(p => p.ProductId == productId) ?? false)
                    return true;
                return false;
            }
            catch (InAppBillingPurchaseException Ex)
            {
                Debug.WriteLine("IsPurchase error: " + Ex.Message);
                throw new Exception(GetPurchaseErrorMessage(Ex.PurchaseError));
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("IsPurchase error: " + Ex.Message);
                throw;
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        /// <summary>
        /// Restituisce la lista delle informazioni sui prodotti
        /// </summary>
        /// <param name="productsId">Array degli identificativi dei prodotti</param>
        /// <returns>Tipo enumerable per scorrere la lista dei prodotti</returns>
        public async Task<IEnumerable<InAppBillingProduct>> GetProductInfo(string[] productsId)
        {
            if (!CrossInAppBilling.IsSupported)
                throw new Exception(AppResources.errInAppBillingNotSupported);

            var billing = CrossInAppBilling.Current;
            try
            {
                var connected = await billing.ConnectAsync(ItemType.InAppPurchase);
                if (!connected)
                    throw new Exception(AppResources.errInAppBillingNotConnected);

                var productsInfo = await billing.GetProductInfoAsync(ItemType.InAppPurchase, productsId);
                if (productsInfo == null)
                    throw new Exception(AppResources.errUnableToFindProducts);
                return productsInfo;
            }
            catch (InAppBillingPurchaseException Ex)
            {
                Debug.WriteLine("GetProductInfo error: " + Ex.Message);
                throw new Exception(GetPurchaseErrorMessage(Ex.PurchaseError));
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        /// <summary>
        /// Verifica se il prodotto è sbloccato da codice di sblocco.
        /// </summary>
        /// <param name="productInfo">Informazioni sul prodotto</param>
        /// <returns>Restituire true se il prodotto è sbloccato, false altrimenti</returns>
        public bool IsProductUnlocked(ProductCache productInfo)
        {
            try
            {
                if (productInfo == null)
                    throw new Exception("Product object is null");

                if (productInfo.Purchased)
                {
                    Debug.WriteLine("Do not invoke IsProductUnlocked for purchased product [{0}]", new[] { productInfo.Id });
                    return true;
                }
                if (productInfo.Unlocked && !productInfo.UnlockCodeExpire)
                {
                    Debug.WriteLine("Product {0} is unlocked without expiration terms", new [] { productInfo.Id });
                    return true;
                }

                if (string.IsNullOrEmpty(App.PwdManager.Password))
                    return productInfo.Unlocked;

                productInfo.Unlocked = CheckUnlockCode(productInfo.Id, productInfo.UnlockCode, App.PwdManager.Password, out bool expire);
                productInfo.UnlockCodeExpire = expire;
                return productInfo.Unlocked;
            }
            catch(Exception ex)
            {
                Debug.WriteLine("IsProductUnlocked error: " + ex.Message);
                return false;
            }
        }

        private string BuildUnlockCode(byte[] codeByte)
        {
            string hash = EncDecHelper.MD5(codeByte);
            string code = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(hash)).ToUpper();
            for (int i = 1; i < 4; i++)
            {
                code = code.Insert(i * 8 + (i - 1), "-");
            }
            return code;
        }

        /// <summary>
        /// Crea il codice di sblocco per un prodotto
        /// </summary>
        /// <param name="productId">Id del prodotto</param>
        /// <param name="password">Master password</param>
        /// <returns>Codice di sblocco</returns>
        public string CreateUnlockCode(string productId, string password)
        {
            string so = Device.RuntimePlatform == Device.UWP ? Device.Android : Device.UWP;

            byte[] codeByte = ComputeCode(productId, password, sourceApp, so);
            return BuildUnlockCode(codeByte);
        }

        /// <summary>
        /// Crea il codice di sblocco persistente per un prodotto
        /// </summary>
        /// <param name="productId">Id del prodotto</param>
        /// <param name="password">Master password</param>
        /// <returns>Codice di sblocco</returns>
        public string CreatePersistentUnlockCode(string productId, string password)
        {
            byte[] codeByte = ComputeCode(productId, password, sourceWeb, Device.RuntimePlatform);
            return BuildUnlockCode(codeByte);
        }

        /// <summary>
        /// Calcola il codice di sblocco per un prodotto
        /// </summary>
        /// <param name="productId">id del prodotto</param>
        /// <param name="password">master password</param>
        /// <param name="source">tipo di sorgente del codice di sblocco</param>
        /// <param name="so">sistema operativo dell'App in cui sbloccare il prodotto</param>
        /// <returns></returns>
        public byte[] ComputeCode(string productId, string password, string source, string so)
        {
            string code = productId + password + so + "_" + source;
            byte[] codeByte = System.Text.Encoding.UTF8.GetBytes(code);
            return codeByte;
        }

        /// <summary>
        /// Verifica se il codice di sblocco è valido
        /// </summary>
        /// <param name="productId">Id del prodotto</param>
        /// <param name="unlockCode">Codice di sblocco</param>
        /// <param name="password">Master password</param>
        /// <param name="expire">Restituire true se il codice scade al cambio della master password, false altrimenti</param>
        /// <returns>Restituisce true se il codice di sblocco è valido, false altrimenti</returns>
        public bool CheckUnlockCode(string productId, string unlockCode, string password, out bool expire)
        {
            expire = true;

            // Prima prova con la sorgente App
            byte[] hashByte = ComputeCode(productId, password, sourceApp, Device.RuntimePlatform);
            if (BuildUnlockCode(hashByte) == unlockCode.ToUpper())
                return true;

            // Prova ora con la sorgente web
            hashByte = ComputeCode(productId, password, sourceWeb, Device.RuntimePlatform);
            if (BuildUnlockCode(hashByte).ToUpper() == unlockCode.ToUpper())
            {
                expire = false; // Non scade quando si cambia la password di accesso
                return true;
            }

            return false;
        }
    }
}

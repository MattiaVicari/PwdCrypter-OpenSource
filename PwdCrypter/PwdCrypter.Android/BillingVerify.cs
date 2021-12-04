using Plugin.InAppBilling;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(PwdCrypter.Droid.BillingVerify))]
namespace PwdCrypter.Droid
{
    public class BillingVerify : IInAppBillingVerifyPurchase
    {
        const string key1 = @"YOUR_KEY1";
        const string key2 = @"YOUR_KEY2";
        const string key3 = @"YOUR_KEY3";
        const int id1 = 0;
        const int id2 = 0;
        const int id3 = 0;

        public Task<bool> VerifyPurchase(string signedData, string signature, string productId = null, string transactionId = null)
        {
            var key1Transform = Plugin.InAppBilling.InAppBillingImplementation.InAppBillingSecurity.TransformString(key1, id1);
            var key2Transform = Plugin.InAppBilling.InAppBillingImplementation.InAppBillingSecurity.TransformString(key2, id2);
            var key3Transform = Plugin.InAppBilling.InAppBillingImplementation.InAppBillingSecurity.TransformString(key3, id3);

            return Task.FromResult(Plugin.InAppBilling.InAppBillingImplementation.InAppBillingSecurity.VerifyPurchase(key1Transform + key2Transform + key3Transform, signedData, signature));
        }
    }
}
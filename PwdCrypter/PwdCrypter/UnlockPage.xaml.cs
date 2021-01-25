using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class UnlockPage : ContentPage
	{
        static public Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public UnlockPage ()
		{
			InitializeComponent ();
		}

        private async void BtnUnlock_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(entryPassword.Text))
                    throw new Exception(AppResources.errInvalidPassword);
                if (string.IsNullOrEmpty(entryUnlockCode.Text))
                    throw new Exception(AppResources.errInvalidUnlockCode);

                if (App.Billing.CheckUnlockCode(BillingManager.PLUSProductID, entryUnlockCode.Text, entryPassword.Text, out bool expire))
                {
                    // Codice corretto
                    ProductCache product = App.Cache.GetProduct(BillingManager.PLUSProductID);
                    if (product == null)
                        throw new Exception(AppResources.errUnableToFindProducts);

                    product.UnlockCode = entryUnlockCode.Text;
                    product.Unlocked = true;
                    product.UnlockCodeExpire = expire;

                    await App.Cache.WriteCache();
                    await App.CurrentApp.CheckPurchasedItem();
                }
                else
                {
                    // Codice non valido
                    throw new Exception(AppResources.errUnlockCodeNoMatch);
                }

                await DisplayAlert(App.Title, AppResources.msgFeatureUnlocked, "Ok");
                await Navigation.PopModalAsync(true);
            }
            catch(Exception ex)
            {
                await DisplayAlert(App.Title, AppResources.errUnlock + " " + ex.Message, "Ok");
            }
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;

                stackEntry.HorizontalOptions = LayoutOptions.CenterAndExpand;
                stackEntry.WidthRequest = fixedWidth;

                stackButtons.HorizontalOptions = LayoutOptions.CenterAndExpand;
                stackButtons.WidthRequest = fixedWidth;

                btnUnlock.HorizontalOptions = LayoutOptions.CenterAndExpand;
                btnUnlock.WidthRequest = fixedWidth;
            }
            else
            {
                stackEntry.HorizontalOptions = LayoutOptions.FillAndExpand;
                stackEntry.WidthRequest = AutoWidth;

                stackButtons.HorizontalOptions = LayoutOptions.FillAndExpand;
                stackButtons.WidthRequest = AutoWidth;

                btnUnlock.HorizontalOptions = LayoutOptions.FillAndExpand;
                btnUnlock.WidthRequest = AutoWidth;
            }
        }

        private async void BtnCancel_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(true);
        }

        private string AdjustUnlockCode(string code)
        {
            if (code.Length > 8 && code.IndexOf("-", 0) != 8)
                code = code.Insert(8, "-");
            if (code.Length > 17 && code.IndexOf("-", 9) != 17)
                code = code.Insert(17, "-");
            if (code.Length > 26 && code.IndexOf("-", 18) != 26)
                code = code.Insert(26, "-");
            if (code.Length > 35 && code.IndexOf("-", 27) != 35)
                code = code.Insert(35, "-");
            
            return code.ToUpper();
        }

        private string EntryUnlockCode_OnSetText(object sender, string oldText)
        {
            return AdjustUnlockCode(oldText);
        }
    }
}
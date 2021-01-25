using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PurchasePLUSPage : ContentPage
	{
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }
        public static Dictionary<string, string> FeatureImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }
        public static bool ShowUnlockCode => !App.IsPLUSActive;

        public bool IsModal { get; set; } = false;

        public PurchasePLUSPage ()
		{
			InitializeComponent ();
		}

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            App.PwdSecurity.BeginOperation();
            btnCancel.IsVisible = IsModal;

            if (App.IsCloudAvailable || App.IsAttachmentFeatureAvailable)
            {
                if (IsModal)
                    await Navigation.PopModalAsync(true);
                else
                    MessagingCenter.Send<Page, Type>(this, "NavigateTo", typeof(WelcomePage));
            }
        }

        private void BtnCancel_Clicked(object sender, EventArgs e)
        {
            if (IsModal)
                Navigation.PopModalAsync(true);
            else
                Navigation.PopAsync(true);
        }

        private async void BtnPurchase_Clicked(object sender, EventArgs e)
        {
            btnPurchase.IsEnabled = false;
            try
            {
                await App.Billing.Purchase(BillingManager.PLUSProductID);
                // Verifica se l'acquisto è andata a buon fine
                await App.CurrentApp.CheckPurchasedItem();
                
                // Ricarica il menu
                MessagingCenter.Send<Page>(this, "ReloadMenu");

                // Torna indietro
                if (IsModal)
                    await Navigation.PopModalAsync(true);
                else
                    MessagingCenter.Send<Page, Type>(this, "NavigateTo", typeof(WelcomePage));
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Purchase error: " + Ex.Message);
                await DisplayAlert(App.Title, Ex.Message, "Ok");
            }
            finally
            {
                btnPurchase.IsEnabled = true;
            }
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;
            if (Width > App.ViewStepMaxWidth)
                gridMain.WidthRequest = App.ViewStepMaxWidth - 140;
            else
                gridMain.WidthRequest = AutoWidth;
        }

        private async void BtnUnlock_Clicked(object sender, EventArgs e)
        {
            // Apre la pagina per inserire il codice di sblocco
            await Navigation.PushModalAsync(new UnlockPage(), true);
        }
    }
}
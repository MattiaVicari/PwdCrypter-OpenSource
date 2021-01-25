using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.Generic;
using System.Diagnostics;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AboutPage : ContentPage
	{
        public static Dictionary<string, string> SupportIconColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#000000" }, { "currentOpacity", "1.0" } };
        }
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public static bool ShowUnlockCode => App.IsPLUSActive;
        public static bool ShowUsedUnlockCode => !string.IsNullOrEmpty(GetUsedUnlockCode());

        public AboutPage ()
		{
			InitializeComponent ();

            LoadAppInfoAndSupport();
		}

        private static string GetUsedUnlockCode()
        {
            try
            {
                ProductCache product = App.Cache.GetProduct(BillingManager.PLUSProductID);
                if (product != null && product.Unlocked)
                {
                    return product.UnlockCode;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Checking for used unlock code error: " + ex.Message);
            }
            return "";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.PwdSecurity.BeginOperation();

            // Codice di sblocco per l'altra piattaforma
            /* Tutte le funzioni PLUS sono ora disponibili gratuitamente
            string productId = "";
            if (ShowUnlockCode)
            {
                productId = Device.RuntimePlatform == Device.UWP ? BillingManager.PLUSProductID_Android : BillingManager.PLUSProductID_Win;
            }

            labelUnlockCode.Text = App.Billing.CreateUnlockCode(productId, App.PwdManager.Password);
            labelUsedUnlockCode.Text = GetUsedUnlockCode();*/
        }

        private void LoadAppInfoAndSupport()
        {
            labelAppTitle.Text = App.Title;
            labelVersion.Text = string.Format(AppResources.txtVersion, App.Version);

            List<SupportItem> menu = new List<SupportItem>
            {
                new SupportItem
                {
                    SupportTitle = AppResources.txtManualAndSupport,
                    SupportUrl = AppResources.urlManualAndSupport,
                    SupportIcon ="Assets/SVG/angle_right_solid.svg"
                },
                new SupportItem
                {
                    SupportPageType = typeof(PrivacyPolicy),
                    SupportTitle = AppResources.titlePrivacyPolicy,
                    BindingContext = new PrivacyPolicyData
                    {
                        IsDetailPage = true
                    },
                    SupportIcon = "Assets/SVG/angle_right_solid.svg"
                },
                new SupportItem
                {
                    SupportTitle = AppResources.txtWebSite,
                    SupportUrl = AppResources.urlMyWebSiteLang,
                    SupportIcon ="Assets/SVG/angle_right_solid.svg"
                },
                new SupportItem
                {
                    SupportPageType = typeof(LicensesPage),
                    SupportTitle = AppResources.title3rdPartyLicenses,
                    SupportIcon = "Assets/SVG/angle_right_solid.svg"
                }
            };
            listViewSupport.ItemsSource = menu;
        }

        private void BtnManualAndSupport_Clicked(object sender, EventArgs e)
        {
            Xamarin.Essentials.Launcher.OpenAsync(new Uri(AppResources.urlManualAndSupport));
        }

        private void BtnPrivacyPolicy_Clicked(object sender, EventArgs e)
        {
            PrivacyPolicyData data = new PrivacyPolicyData
            {
                IsDetailPage = true
            };
            Page page = new PrivacyPolicy
            {
                BindingContext = data
            };
            Navigation.PushAsync(page, true);
        }

        private void ListViewSupport_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is SupportItem support && support != null)
            {
                if (support.SupportUrl != null)
                {
                    Xamarin.Essentials.Launcher.OpenAsync(new Uri(support.SupportUrl));
                }
                else
                {
                    Page page = (Page)Activator.CreateInstance(support.SupportPageType);
                    if (page != null)
                    {
                        if (support.BindingContext != null)
                            page.BindingContext = support.BindingContext;
                        Navigation.PushAsync(page, true);
                    }
                }
                listViewSupport.SelectedItem = null;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }

    /// <summary>
    /// Classe che rappresenta un elemento della lista della
    /// sezione del supporto
    /// </summary>
    public class SupportItem
    {
        public string SupportTitle { get; set; }
        public ImageSource SupportIcon { get; set; }
        public string SupportUrl { get; set; }
        public Type SupportPageType { get; set; }
        public object BindingContext { get; set; }
    }
}
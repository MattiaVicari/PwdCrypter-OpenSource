using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PrivacyPolicy : ContentPage
	{
        private bool CanGoBack = true;

        public PrivacyPolicy ()
		{
			InitializeComponent ();
            btnAgree.Focus();
		}

        /// <summary>
        /// Carica il testo della privacy policy
        /// </summary>
        async private Task LoadPrivacyPolicy()
        {
            try
            {
                string htmlSource = DependencyService.Get<IAssetReader>().ReadText("PrivacyPolicy/privacypolicy_" + AppResources.langID + ".html");
                // Workaround per problema su UWP: non sempre veniva renderizzato il contenuto
                webViewPrivacyPolicy.HtmlToLoad = htmlSource;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadPrivacyPolicy error: " + ex.Message);
                await DisplayAlert(App.Title, AppResources.errPrivacyPolicy + " " + ex.Message, "Ok");
            }
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            if (Height < App.ViewStepMaxHeight)
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Medium, lblTitle.GetType());
            }
            else
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Large, lblTitle.GetType());
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            App.PwdSecurity.BeginOperation();

            PrivacyPolicyData data = BindingContext as PrivacyPolicyData;

            bool needAgree = (data != null) && data.ToBeAgree && !data.IsDetailPage;
            btnDeny.IsVisible = needAgree;
            btnAgree.IsVisible = (data != null) && !data.IsDetailPage;
            btnAgree.Text = needAgree ? AppResources.btnAgree : AppResources.btnClose;
            CanGoBack = (data == null) || (data != null && data.CanGoBack);

            if (Device.RuntimePlatform == Device.UWP && (data == null || (data != null && !data.IsDetailPage)))
            {
                CanGoBack = false;
                NavigationPage.SetHasNavigationBar(this, false);
            }

            await LoadPrivacyPolicy();
        }

        async private void BtnDeny_Clicked(object sender, EventArgs e)
        {
            // Avvisa l'utente che l'applicazione verrà chiusa
            bool answer = await DisplayAlert(App.Title, 
                                                AppResources.msgPrivacyPolicyDeny, 
                                                AppResources.btnYes, 
                                                AppResources.btnNo);
            if (answer)
            {
                // ATTENZIONE: per iOS non è consigliabile chiudere l'applicazione
                var closer = DependencyService.Get<IApplicationCloser>();
                closer.Close();
            }
        }

        async private void BtnAgree_Clicked(object sender, EventArgs e)
        {
            App.AgreePrivacyPolicy = true;
            await Navigation.PopModalAsync(true);
        }

        protected override bool OnBackButtonPressed()
        {
            if (!CanGoBack)
                return true;
            return base.OnBackButtonPressed();
        }

        private void WebViewPrivacyPolicy_Navigating(object sender, WebNavigatingEventArgs e)
        {
            // Apre i link esternamente
            if (e.Url.StartsWith("http"))
            {
                try
                {
                    var uri = new Uri(e.Url);
                    Xamarin.Essentials.Launcher.OpenAsync(uri);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Navigating error: " + ex.Message);
                }

                e.Cancel = true;
            }
        }
    }

    /// <summary>
    /// Classe per lo scambio di informazioni con la pagina della privacy policy
    /// </summary>
    public class PrivacyPolicyData
    {
        /// <summary>
        /// Vale true nel caso sia richiesta l'accettazione della privacy policy, false altrimenti
        /// </summary>
        public bool ToBeAgree { get; set; }

        /// <summary>
        /// Impostare a true per visualizzare la barra di navigazione, false altrimenti
        /// </summary>
        public bool ShowNavigationBar { get; set; }

        /// <summary>
        /// Impostare a true per indicare che la pagina andrà visualizzata come Detail
        /// di una MasterDetailPage. Non sarà visualizzato alcun bottone di chiusura/accettazione.
        /// </summary>
        public bool IsDetailPage { get; set; }

        /// <summary>
        /// Impostare a true per permettere di utilizzare il pulsante fisico o virtuale
        /// dello smartphone per tornare indietro.
        /// </summary>
        public bool CanGoBack { get; set; }

        public PrivacyPolicyData()
        {
            ToBeAgree = false;
            ShowNavigationBar = true;
            IsDetailPage = false;
            CanGoBack = true;
        }
    }
}
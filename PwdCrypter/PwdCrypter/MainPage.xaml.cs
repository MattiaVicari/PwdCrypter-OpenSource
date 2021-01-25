using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace PwdCrypter
{
    public partial class MainPage : ContentPage
    {
        static public Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public MainPage()
        {
            InitializeComponent();

            if (Device.RuntimePlatform == Device.UWP)
                NavigationPage.SetHasNavigationBar(this, false);
        }

        private void SetLoading(bool loading)
        {
            spinnerLoading.IsEnabled = loading;
            spinnerLoading.IsRunning = loading;
            spinnerLoading.IsVisible = loading;

            switchPrivacy.IsEnabled = !loading;

            btnPrivacyPolicy.IsEnabled = !loading;
            btnPrivacyPolicy.BackgroundColor = loading ? AppStyle.ButtonDisabledBackgroundColor : Color.LightBlue;
            btnPrivacyPolicy.TextColor = loading ? AppStyle.ButtonDisabledTextColor : AppStyle.ButtonTextColor;

            btnConfirm.IsEnabled = !loading;
            btnConfirm.BackgroundColor = loading ? AppStyle.ButtonDisabledBackgroundColor : AppStyle.ButtonBackgroundColor;
            btnConfirm.TextColor = loading ? AppStyle.ButtonDisabledTextColor : AppStyle.ButtonTextColor;
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;

                entryPassword.HorizontalOptions = LayoutOptions.CenterAndExpand;
                entryPassword.WidthRequest = fixedWidth;
                entryPasswordConfirm.HorizontalOptions = LayoutOptions.CenterAndExpand;
                entryPasswordConfirm.WidthRequest = fixedWidth;

                gridPrivacy.HorizontalOptions = LayoutOptions.CenterAndExpand;
                gridPrivacy.WidthRequest = fixedWidth;

                if (Height < App.ViewStepMaxHeight)
                {
                    btnPrivacyPolicy.HorizontalOptions = LayoutOptions.FillAndExpand;
                    btnPrivacyPolicy.WidthRequest = AutoWidth;
                    btnConfirm.HorizontalOptions = LayoutOptions.FillAndExpand;
                    btnConfirm.WidthRequest = AutoWidth;
                }
                else
                {
                    btnPrivacyPolicy.HorizontalOptions = LayoutOptions.CenterAndExpand;
                    btnPrivacyPolicy.WidthRequest = fixedWidth;
                    btnConfirm.HorizontalOptions = LayoutOptions.CenterAndExpand;
                    btnConfirm.WidthRequest = fixedWidth;
                }
            }
            else
            {
                gridPrivacy.HorizontalOptions = LayoutOptions.FillAndExpand;
                gridPrivacy.WidthRequest = AutoWidth;

                entryPassword.HorizontalOptions = LayoutOptions.FillAndExpand;
                entryPassword.WidthRequest = AutoWidth;
                entryPasswordConfirm.HorizontalOptions = LayoutOptions.FillAndExpand;
                entryPasswordConfirm.WidthRequest = AutoWidth;

                btnPrivacyPolicy.HorizontalOptions = LayoutOptions.FillAndExpand;
                btnPrivacyPolicy.WidthRequest = AutoWidth;
                btnConfirm.HorizontalOptions = LayoutOptions.FillAndExpand;
                btnConfirm.WidthRequest = AutoWidth;
            }

            if (Height < App.ViewStepMaxHeight)
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Medium, lblTitle.GetType());
                if (Width <= App.ViewStepMaxWidth && Height > App.ViewStepSmallMaxHeight)
                {
                    entryPassword.Orientation = StackOrientation.Vertical;
                    entryPasswordConfirm.Orientation = StackOrientation.Vertical;
                    stackButtons.Orientation = StackOrientation.Vertical;
                }
                else
                {
                    entryPassword.Orientation = StackOrientation.Horizontal;
                    entryPasswordConfirm.Orientation = StackOrientation.Horizontal;
                    stackButtons.Orientation = StackOrientation.Horizontal;
                }

                gridMain.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Auto);

                if (Width <= App.ViewStepMaxWidth && Height > App.ViewStepSmallMaxHeight)
                {
                    gridPassword.RowDefinitions[0].Height = new GridLength(50, GridUnitType.Star);
                    gridPassword.RowDefinitions[1].Height = new GridLength(50, GridUnitType.Star);
                }
                else
                {
                    gridPassword.RowDefinitions[0].Height = new GridLength(35, GridUnitType.Absolute);
                    gridPassword.RowDefinitions[1].Height = new GridLength(35, GridUnitType.Absolute);
                }

                if (Device.RuntimePlatform == Device.Android)
                {
                    entryPassword.FontSize = 14;
                    entryPasswordConfirm.FontSize = 14;
                }
            }
            else
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Large, lblTitle.GetType());
                entryPassword.Orientation = StackOrientation.Vertical;
                entryPasswordConfirm.Orientation = StackOrientation.Vertical;
                stackButtons.Orientation = StackOrientation.Vertical;

                gridMain.RowDefinitions[1].Height = new GridLength(30, GridUnitType.Star);

                gridPassword.RowDefinitions[0].Height = new GridLength(50, GridUnitType.Star);
                gridPassword.RowDefinitions[1].Height = new GridLength(50, GridUnitType.Star);

                if (Device.RuntimePlatform == Device.Android)
                {
                    entryPassword.FontSize = Device.GetNamedSize(NamedSize.Default, typeof(Entry));
                    entryPasswordConfirm.FontSize = Device.GetNamedSize(NamedSize.Default, typeof(Entry));
                }
            }
        }

        async private void BtnPrivacyPolicy_Clicked(object sender, EventArgs e)
        {
            await ShowPrivacyPolicy(false);
        }

        /// <summary>
        /// Mostra la pagina della privacy policy
        /// </summary>
        /// <param name="toBeAgree">Passare true per indicare che è richiesta l'accettazione
        /// della privacy policy</param>
        /// <returns></returns>
        async private Task ShowPrivacyPolicy(bool toBeAgree)
        {
            var data = new PrivacyPolicyData
            {
                ToBeAgree = toBeAgree,
                ShowNavigationBar = !toBeAgree
            };

            await Navigation.PushModalAsync(new PrivacyPolicy
            {
                BindingContext = data
            }, true);
        }

        /// <summary>
        /// Verifica le condizioni di installazione per sapere se è necessario effettuare
        /// il redirect su un altra pagina
        /// </summary>
        /// <returns></returns>
        async private Task CheckRedirect()
        {
            SetLoading(true);
            try
            {
                if (App.IsNotificationOpen() && App.PushNotificationData is NewsInfo newsData)
                    await App.CurrentApp.ShowNews(newsData);
                else if ((!App.PwdManager.IsInitialized() && !App.AgreePrivacyPolicy) || App.IsUpdated())
                    await ShowPrivacyPolicy(true);
                else if (App.IsLoggedIn)
                    App.CurrentApp.ChangeMainPage(new HamburgerMenu());
            }
            finally
            {
                SetLoading(false);
            }
        }

        async protected override void OnAppearing()
        {
            base.OnAppearing();

            if (App.IsAppearingTwice())
            {
                // Workaround
                // Se si aspetta anche solo un secondo, la OnAppearing non verrà chiamata
                // una seconda volta.
                await Task.Delay(1000);
                await CheckRedirect();
            }
            else
                await CheckRedirect();
        }

        async private void BtnConfirm_Clicked(object sender, EventArgs e)
        {
            // Verifica i dati inseriti
            if (!await ValidateData())
                return;

            try
            {
                // Crea il nuovo file delle password
                App.PwdManager.Password = entryPassword.Text.Trim();
                App.PwdManager.CreatePasswordFile();                
            }
            catch(Exception ex)
            {
                await DisplayAlert(App.Title, AppResources.errPasswordFileCreation + " " + ex.Message, "Ok");
                return;
            }

            // Va alla pagina di riepilogo
            await App.Statistic.RegisterLogin();
            App.CurrentApp.ChangeMainPage(new HamburgerMenu());
        }

        /// <summary>
        /// Verifica se sono stati inseriti tutti i dati per creare la password
        /// di accesso.
        /// </summary>
        /// <returns>True se tutti i dati sono stati inseriti e sono validi, false altrimenti</returns>
        async private Task<bool> ValidateData()
        {
            if (entryPassword.Text == null || entryPassword.Text.Trim().Length == 0)
            {
                await DisplayAlert(App.Title, AppResources.errInvalidPassword, "Ok");
                return false;
            }
            if (entryPassword.Text.Trim().Length > 32)
            {
                await DisplayAlert(App.Title, string.Format(AppResources.errPasswordToLong, 32), "Ok");
                return false;
            }
            if (entryPasswordConfirm.Text == null || entryPasswordConfirm.Text.Trim().Length == 0)
            {
                await DisplayAlert(App.Title, AppResources.errPasswordConfirmMissing, "Ok");
                return false;
            }
            if (entryPassword.Text.CompareTo(entryPasswordConfirm.Text) != 0)
            {
                await DisplayAlert(App.Title, AppResources.errPasswordConfirm, "Ok");
                return false;
            }
            if (!switchPrivacy.IsToggled)
            {
                await DisplayAlert(App.Title, AppResources.errPrivacyPolicyAgree, "Ok");
                return false;
            }
            if (entryPassword.PasswordStrength < 2)
            {
                if (!await DisplayAlert(App.Title, AppResources.warnWeakPassword, AppResources.btnYes, AppResources.btnNo))
                    return false;
            }
            return true;
        }
    }
}

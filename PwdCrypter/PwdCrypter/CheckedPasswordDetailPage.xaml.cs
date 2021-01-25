using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CheckedPasswordDetailPage : ContentPage
    {
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public CheckedPasswordDetailPage()
        {
            InitializeComponent();
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;
            LayoutOptions layoutOptions;
            double widthRequest;

            if (Width > App.ViewStepMaxWidth)
            {
                widthRequest = App.ViewStepMaxWidth - 140;
                layoutOptions = LayoutOptions.Center;
            }
            else
            {
                widthRequest = AutoWidth;
                layoutOptions = LayoutOptions.FillAndExpand;
            }

            btnOpenPassword.HorizontalOptions = layoutOptions;
            btnOpenPassword.WidthRequest = widthRequest;
            scrollMain.HorizontalOptions = layoutOptions;
            scrollMain.WidthRequest = widthRequest;
        }

        private async void BtnOpenPassword_Clicked(object sender, EventArgs e)
        {
            CheckedPasswordItem data = BindingContext as CheckedPasswordItem;
            PwdListItem pwdInfo = App.PwdManager.GetPasswordById(data.Item.PwdId);
            if (pwdInfo == null)
            {
                await DisplayAlert(App.Title, AppResources.errPasswordNoyFound, "Ok");
                return;
            }

            // Apre la pagina con le informazioni sulla password
            Page page = new PasswordPage
            {
                BindingContext = new PasswordPageData
                {
                    DataMode = PwdDataMode.ReadOnlyMode,                    
                    PwdInfo = pwdInfo
                }
            };
            await Navigation.PushAsync(page, true);
        }
    }
}
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class WelcomePage : ContentPage
	{
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }
        public static Dictionary<string, string> StatisticsImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#cccccc" }, { "currentOpacity", "1.0" } };
        }

        public static double StatisticsImageSize
        {
            get
            {
                if (Device.RuntimePlatform == Device.UWP)
                    return 100.0;
                return 80.0;
            }
        }
        public static string PasswordCount
        {
            get => $"{App.Statistic.PasswordsCount} password";
        }
        public static string AccountTypeCount
        {
            get
            {
                int count = App.Statistic.PasswordByAccountType.Count;
                if (count == 1)
                    return AppResources.txtStatisticsAccTypeSingle;
                return string.Format(AppResources.txtStatisticsAccType, count);
            }
        }
        public static string LastLoginDate
        {
            get => App.Statistic.LastLogin.ToString("dd/MM/yyyy");
        }
        public static string LastLoginTime
        {
            get => App.Statistic.LastLogin.ToString("HH:mm:ss");
        }
        public static string LastPassword
        {
            get
            {
                if (App.Statistic.LastPasswordId?.Length > 0)
                {
                    PwdListItem item = App.PwdManager.GetPasswordById(App.Statistic.LastPasswordId);
                    if (item != null)
                        return $"{item.Name}\n" + string.Format(AppResources.txtWhen, item.CreationDateTime.ToString("dd/MM/yyyy HH:mm:ss"));
                }
                return AppResources.txtNothing;
            }
        }
        public static string DataSource
        {
            get
            {
                string sourceName = AppResources.txtLocalSource;
                if (App.Statistic.Cloud)
                    sourceName = AppResources.txtCloudSource;
                return string.Format(AppResources.txtDataSource, sourceName);
            }
        }
        public static bool IsCloudAvailable => App.IsCloudAvailable;

        public WelcomePage ()
		{
			InitializeComponent ();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.PwdSecurity.BeginOperation();
            NavigationPage.SetHasBackButton(this, false);
        }

        protected override bool OnBackButtonPressed()
        {
            // Non si potrà mai tornare indietro
            return true;
        }
    }
}
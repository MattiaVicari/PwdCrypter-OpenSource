using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HamburgerMenu : MasterDetailPage
    {
        public static Dictionary<string, string> MenuIconColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#ffffff" }, { "currentOpacity", "1.0" } };
        }

        private Type CurrentDetailPageType;
        private bool Subscribed = false;

        private bool pageIsBusy = false;
        public bool MenuIsBusy
        {
            get { return pageIsBusy; }
            set => SetMenuIsBusy(value);
        }

        private void SetMenuIsBusy(bool busy)
        {
            if (pageIsBusy == busy)
                return;

            pageIsBusy = busy;

            spinnerMenu.IsRunning = busy;
            spinnerMenu.IsEnabled = busy;
            spinnerMenu.IsVisible = busy;

            listViewMenu.IsVisible= !busy;
        }

        public HamburgerMenu()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);

            CurrentDetailPageType = typeof(WelcomePage);
            Detail = new NavigationPage(new WelcomePage());
            if (Device.RuntimePlatform == Device.UWP)
                MasterBehavior = MasterBehavior.Popover;

            IsPresentedChanged += HamburgerMenu_IsPresentedChanged;

            LoadMenu();
        }

        private async void HamburgerMenu_IsPresentedChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Menu refresh");

            MenuIsBusy = true;
            await App.CurrentApp.CheckPurchasedItem(true, () =>
            {
                LoadMenu();
                MenuIsBusy = false;
                return true;
            });
            
        }

        private void LoadMenu()
        {
            listViewMenu.ItemsSource = null;

            // Voci sulla intestazione del menu
            labelAppTitle.Text = App.Title;
            labelVersion.Text = string.Format(AppResources.txtVersion,  App.Version);
            
            // Il Cloud è una funzione disponibile con la versione PLUS
            MenuItem itemCloud = new MenuItem { MenuPageType = typeof(CloudPage), MenuTitle = AppResources.titleCloud, Separator = true, MenuIcon = "Assets/SVG/cloud_solid.svg" };
            if (!App.IsCloudAvailable)
                itemCloud = new MenuItem { MenuPageType = typeof(PurchasePLUSPage), MenuTitle = AppResources.titleCloud, Separator = true, MenuIcon = "Assets/SVG/cloud_solid.svg", MenuRightIcon = "Assets/plus_label.png" };

            // Voci del menu
            List<MenuItem> menu = new List<MenuItem>
            {
                new MenuItem { MenuPageType = typeof(WelcomePage), MenuTitle = AppResources.titleSummary, MenuIcon="Assets/SVG/chart_pie_solid.svg" },
                new MenuItem { MenuPageType = typeof(PasswordListPage), MenuTitle = AppResources.titlePasswordList, MenuIcon="Assets/SVG/key_solid.svg" },
                new MenuItem { MenuPageType = typeof(ChangeLoginPage), MenuTitle = AppResources.titleChangeLoginPwd, MenuIcon="Assets/SVG/lock.svg" },
                new MenuItem { MenuPageType = typeof(ImportExportPage), MenuTitle = AppResources.titleTransferPwd, Separator = true, MenuIcon="Assets/SVG/exchange_alt_solid.svg" },
                itemCloud,
                // Rimosso in seguito alla variazione delle condizioni di utilizzo.
                // Vedi il blog di Troy Hunt:
                // https://www.troyhunt.com/authentication-and-the-have-i-been-pwned-api/
                // Dal 18 agosto 2019 il servizio non è più gratuito
                // 13 Aprile 2020: ripristinata funzione ma con verifica embedded delle password
                new MenuItem { MenuPageType = typeof(CheckPasswordPage), MenuTitle = AppResources.titleCheckPassword, MenuIcon="Assets/SVG/shield_alt_solid.svg" },
                new MenuItem { MenuPageType = typeof(BackupPage), MenuTitle = AppResources.titleBackup, MenuIcon="Assets/SVG/history.svg" },
                new MenuItem { MenuPageType = typeof(NewsListPage), MenuTitle = AppResources.titleNewsList, MenuIcon="Assets/SVG/newspaper_solid.svg", Separator = true },
                new MenuItem { MenuPageType = typeof(SettingsPage), MenuTitle = AppResources.titleSettings, MenuIcon="Assets/SVG/cog_solid.svg" },
                new MenuItem
                {
                    MenuPageType = typeof(PrivacyPolicy),
                    MenuTitle = AppResources.titlePrivacyPolicy,
                    BindingContext = new PrivacyPolicyData
                    {
                        IsDetailPage = true,
                        CanGoBack = false
                    },
                    MenuIcon = "Assets/SVG/user_shield_solid.svg"
                },
                new MenuItem { MenuPageType = typeof(AboutPage), MenuTitle = AppResources.titleAbout, MenuIcon="Assets/SVG/info_circle_solid.svg" }
            };
            listViewMenu.ItemsSource = menu;
        }

        private async Task NavigateTo(Type pageType, object pageBindingContext)
        {
            if (Navigation.ModalStack.Count > 0)
            {
                Debug.WriteLine("Skip navigation because a modal page is currently showing");
                return;
            }

            // Verifica se è scaduta la sessione
            await App.PwdSecurity.CheckOperation(Detail, "Menu");

            // Avvisa la pagina corrente che si sta cambiando pagina
            MessagingCenter.Send(this, "DetailPageChanging", CurrentDetailPageType);

            CurrentDetailPageType = pageType;
            Page page = (Page)Activator.CreateInstance(pageType);
            if (pageBindingContext != null)
                page.BindingContext = pageBindingContext;

            Detail = new NavigationPage(page);
        }

        private async void ListViewMenu_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is MenuItem menu && menu != null && CurrentDetailPageType != menu.MenuPageType)
            {
                await NavigateTo(menu.MenuPageType, menu.BindingContext);
                listViewMenu.SelectedItem = null;
                IsPresented = false;
            }
        }

        private void MasterDetailPage_SizeChanged(object sender, EventArgs e)
        {
            // Workaround per un bug su UWP sull'aggiornamento dell'altezza della pagina
            // master, quando si cambiano le dimensioni dell'App.
            // L'altezza della pagina master rimaneva indietro di un giro di aggiornamento.
            // https://github.com/xamarin/Xamarin.Forms/issues/1332
            if (Device.RuntimePlatform == Device.UWP)
            {
                IsPresented = true;
                IsPresented = false;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!Subscribed)
            {
                MessagingCenter.Subscribe<Page, Type>(this, "NavigateTo", async (sender, pageType) => await NavigateTo(pageType, null));
                MessagingCenter.Subscribe<Page>(this, "ReloadMenu", (sender) => ReloadMenu());
                Subscribed = true;
            }

            if (App.IsAppearingTwice())
            {
                // Workaround
                // Se si aspetta anche solo un secondo, la OnAppearing non verrà chiamata
                // una seconda volta.
                await Task.Delay(1000);
                CheckAction();
            }
            else
                CheckAction();
        }

        private void CheckAction()
        {
            if (App.IsNotificationOpen())
            {
                if (App.PushNotificationData is string action)
                    MessagingCenter.Send(action, "DoAction");
            }
        }

        private void ReloadMenu()
        {
            listViewMenu.ItemsSource = null;
            LoadMenu();
        }
    }

    /// <summary>
    /// Classe che rappresenta una voce del menu
    /// </summary>
    public class MenuItem
    {
        public string MenuTitle { get; set; }
        public ImageSource MenuIcon { get; set; }
        public ImageSource MenuRightIcon { get; set; }
        public Type MenuPageType { get; set; }
        public object BindingContext { get; set; }
        public bool Separator { get; set; }
    }
}
using PwdCrypter.Controls;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PasswordListPage : ContentPage
	{
        private static FilterOption Filter = new FilterOption();                

        private ToolbarItem ToolbarItemClearFilter = null;
        private ToolbarItem ToolbarItemFilter = null;
        private ToolbarItem ToolbarItemAddPassword = null;

        private bool NeedUpdatedPasswordList;
        private bool DisableSearch = false;
        private object PreviousSelectedItem = null;

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }
        public static Dictionary<string, string> AccountTypeIconColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#FF9052" } };
        }

        public PasswordListPage ()
		{
			InitializeComponent ();

            Filter.InCloud = App.PwdManager.Cloud;
            NeedUpdatedPasswordList = true;

            MessagingCenter.Subscribe<Page>(this, "UpdatePasswordList", (p) => NeedUpdatedPasswordList = true);

            PrepareToolbar();
        }

        private async void AddPassword_Tapped(object sender, EventArgs e)
        {
            await AddPassword();
        }

        private void PrepareToolbar()
        {
            ToolbarItemClearFilter = new ToolbarItem(AppResources.txtClearFilter, "Assets/clear_filter.png", async () => await ClearFilter());
            ToolbarItemFilter = new ToolbarItem(AppResources.txtSearch, "Assets/filter.png", async () => await OpenFilterPanel());
            ToolbarItemAddPassword = new ToolbarItem(AppResources.txtAddPassword, "Assets/add.png", async () => await AddPassword());
        }

        private void UpdateSearchBar()
        {
            DisableSearch = true;
            searchBar.Text = Filter.Criteria;
            DisableSearch = false;
        }

        private async Task ClearFilter()
        {
            Filter.Clear();
            UpdateSearchBar();
            SetupToolbar();
            await LoadPasswordList();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            SetupToolbar();
            if (NeedUpdatedPasswordList)
                await LoadPasswordList();
            App.PwdSecurity.BeginOperation();
        }

        private void SetupToolbar()
        {
            ToolbarItems.Clear();
            if (Filter.ApplyFilter)
                ToolbarItems.Add(ToolbarItemClearFilter);
            ToolbarItems.Add(ToolbarItemFilter);
            if (Device.RuntimePlatform == Device.UWP)
                ToolbarItems.Add(ToolbarItemAddPassword);
        }

        private async Task AddPassword()
        {
            if (!await App.PwdSecurity.CheckOperation(this, "AddPassword"))
                return;

            Page page = new PasswordPage
            {
                BindingContext = new PasswordPageData
                {
                    DataMode = PwdDataMode.InsertMode,
                    PwdInfo = new PwdListItem()
                }
            };
            await Navigation.PushAsync(page, true);
        }

        private async Task OpenFilterPanel()
        {
            if (!await App.PwdSecurity.CheckOperation(this, "FilterPassword"))
                return;

            // Naviga verso la pagina del filtro di ricerca
            PasswordFilterPage page = new PasswordFilterPage
            {
                BindingContext = Filter
            };
            page.ConfirmFilter += FilterPassword_ConfirmFilter;
            await Navigation.PushAsync(page, true);
        }

        private void FilterPassword_ConfirmFilter(object sender, EventArgs e, FilterOption filter)
        {
            Filter = filter;
            UpdateSearchBar();
            SetupToolbar();
            NeedUpdatedPasswordList = true;
        }

        /// <summary>
        /// Carica le password nella lista.
        /// </summary>
        private async Task LoadPasswordList()
        {
            int i = 0;
            listViewPassword.ItemsSource = null;

            List<ListRowItem> listRowItems = new List<ListRowItem>();
            List<PwdListItem> pwdLists = await App.PwdManager.GetPasswordList(Filter);
            foreach (PwdListItem item in pwdLists)
                listRowItems.Add(new ListRowItem { Item = item, Index = i++ });
            listViewPassword.ItemsSource = listRowItems;

            listViewPassword.IsVisible = pwdLists.Count > 0;
            lblNoPassword.Text = Filter.ApplyFilter ? AppResources.txtPwdListNoResult : AppResources.txtPwdListEmpty;
            lblNoPassword.IsVisible = !listViewPassword.IsVisible;

            NeedUpdatedPasswordList = false;
            if (PreviousSelectedItem != null)
            {
                // Selezione dell'elemento precedentemente selezionato
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(500);  // Devo attendere che la lista sia del tutto caricata
                    ListRowItem selectedRow = listRowItems.Find((item) => item.Item.Id.CompareTo((PreviousSelectedItem as ListRowItem).Item.Id) == 0);
                    if (selectedRow != null)
                        listViewPassword.ScrollTo(selectedRow, ScrollToPosition.MakeVisible, true);
                    PreviousSelectedItem = null;
                });
            }
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        private async void ListViewPassword_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            // Va alla pagina della password
            if (e.SelectedItem is ListRowItem item)
            {
                if (!await App.PwdSecurity.CheckOperation(this, "OpenPassword"))
                {
                    listViewPassword.SelectedItem = -1;
                    return;
                }

                // Apre la pagina con le informazioni sulla password selezionata
                Page page = new PasswordPage
                {
                    BindingContext = new PasswordPageData
                    {
                        DataMode = PwdDataMode.ViewMode,
                        PwdInfo = item.Item
                    }
                };
                await Navigation.PushAsync(page, true);

                listViewPassword.SelectedItem = -1;
                PreviousSelectedItem = e.SelectedItem;
            }
        }

        private async void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DisableSearch)
                return;

            string criteria = (sender as SearchBar).Text;
            if (string.IsNullOrEmpty(criteria))
                Filter.Criteria = "";
            else
            {
                Filter.Criteria = criteria;
                Filter.AllFields = true;
            }

            PreviousSelectedItem = null;
            await LoadPasswordList();
        }
    }

    // Classe che rappresenta un elemento della lista delle password
    class ListRowItem
    {
        public PwdListItem Item { get; set; }
        public int Index { get; set; }
        public string Name { get => Item?.Name; }
        public string AccountType { get => Item?.GetAccountOptionName(); }
        public string Icon
        {
            get
            {
                switch (Item.AccountOption)
                {
                    default:
                    case PwdCrypter.AccountType.Other:
                        return "Assets/SVG/cat_other.svg";
                    case PwdCrypter.AccountType.Email:
                        return "Assets/SVG/cat_email.svg";
                    case PwdCrypter.AccountType.SocialNetwork:
                        return "Assets/SVG/cat_socials.svg";
                    case PwdCrypter.AccountType.Bank:
                        return "Assets/SVG/cat_bank.svg";
                    case PwdCrypter.AccountType.CreditDebitCard:
                        return "Assets/SVG/cat_creditcard.svg";
                    case PwdCrypter.AccountType.ECommerce:
                        return "Assets/SVG/cat_ecommerce.svg";
                    case PwdCrypter.AccountType.DeviceWork:
                    case PwdCrypter.AccountType.DeviceHome:
                    case PwdCrypter.AccountType.Device:
                        return "Assets/SVG/cat_device.svg";
                    case PwdCrypter.AccountType.App:
                        return "Assets/SVG/cat_app.svg";
                    case PwdCrypter.AccountType.Software:
                        return "Assets/SVG/cat_software.svg";
                    case PwdCrypter.AccountType.Institutional:
                        return "Assets/SVG/cat_institutional.svg";
                }
            }
        }
        public string IconSkip
        {
            get
            {
                if (Item.SkipCheck)
                    return Controls.Icon.FAForwardSolid;
                return null;
            }
        }
        public Color RowColor
        {
            get => Index % 2 == 0 ? Color.Transparent : AppStyle.ListRowColorEven;
        }
    }
}
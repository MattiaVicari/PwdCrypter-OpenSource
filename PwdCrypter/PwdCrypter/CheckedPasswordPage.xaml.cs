using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CheckedPasswordPage : ContentPage
    {
        private FilterOption Filter = new FilterOption();
        private bool DisableSearch = false;
        private object PreviousSelectedItem = null;
        private bool NeedUpdatedPasswordList;
        private ToolbarItem ToolbarItemClearFilter = null;
        private ToolbarItem ToolbarItemFilter = null;

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        private List<CheckedPasswordItem> CheckedPasswordsList = null;

        public List<PasswordCheckData> Data { get; set; }

        public CheckedPasswordPage()
        {
            InitializeComponent();

            Filter.ScopePwdIssues = true;
            NeedUpdatedPasswordList = true;
            PrepareToolbar();           
        }

        private void UpdateSearchBar()
        {
            DisableSearch = true;
            searchBar.Text = Filter.Criteria;
            DisableSearch = false;
        }

        private void PrepareToolbar()
        {
            ToolbarItemClearFilter = new ToolbarItem(AppResources.txtClearFilter, "Assets/clear_filter.png", async () => await ClearFilter());
            ToolbarItemFilter = new ToolbarItem(AppResources.txtSearch, "Assets/filter.png", async () => await OpenFilterPanel());            
        }

        private async Task OpenFilterPanel()
        {
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

        private async Task ClearFilter()
        {
            Filter.Clear();
            UpdateSearchBar();
            SetupToolbar();
            await LoadCheckedPasswords();
        }

        private void SetupToolbar()
        {
            ToolbarItems.Clear();
            if (Filter.ApplyFilter)
                ToolbarItems.Add(ToolbarItemClearFilter);
            ToolbarItems.Add(ToolbarItemFilter);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SetupToolbar();
            if (NeedUpdatedPasswordList)
                await LoadCheckedPasswords();
            App.PwdSecurity.BeginOperation();
        }

        private async Task LoadCheckedPasswords()
        {
            int count = 0;
            listViewPasswords.ItemsSource = null;

            List<PwdListItem> pwds = await App.PwdManager.GetPasswordList(Filter);
            CheckedPasswordsList = new List<CheckedPasswordItem>();
            foreach (PasswordCheckData item in Data)
            {
                string desc = "";
                string issues = "";

                PwdListItem pwdItem = pwds.Find((pwd) => pwd.Id == item.PwdId);
                if (pwdItem == null)
                    continue;
                if (Filter.Issues.Count > 0)
                {
                    IEnumerable<PwdIssue> union = item.Issues.Intersect(Filter.Issues);
                    if (union == null || union.Count() == 0)
                        continue;
                }
                
                desc = pwdItem.Name;
                foreach (PwdIssue issue in item.Issues)
                {
                    if (issues != "")
                        issues += "\n";
                    string issueDesc = Utility.EnumHelper.GetDescription(issue);
                    if (issue == PwdIssue.TooOld)
                        issueDesc = string.Format(issueDesc, PasswordChecker.MaxDaysAlert);
                    issues += issueDesc;
                }

                CheckedPasswordItem listItem = new CheckedPasswordItem
                {
                    Index = count++,
                    Item = item,
                    PasswordDesc = desc,
                    PasswordIssues = issues
                };
                CheckedPasswordsList.Add(listItem);
            }
            listViewPasswords.ItemsSource = CheckedPasswordsList;

            listViewPasswords.IsVisible = CheckedPasswordsList.Count > 0;
            lblNoPassword.Text = Filter.ApplyFilter ? AppResources.txtPwdListNoResult : AppResources.txtPwdListEmpty;
            lblNoPassword.IsVisible = !listViewPasswords.IsVisible;

            NeedUpdatedPasswordList = false;
            if (PreviousSelectedItem != null)
            {
                // Selezione dell'elemento precedentemente selezionato
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(500);  // Devo attendere che la lista sia del tutto caricata
                    CheckedPasswordItem selectedRow = CheckedPasswordsList.Find((item) => item.Index == (PreviousSelectedItem as CheckedPasswordItem).Index);
                    if (selectedRow != null)
                        listViewPasswords.ScrollTo(selectedRow, ScrollToPosition.MakeVisible, true);
                    PreviousSelectedItem = null;
                });
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
            await LoadCheckedPasswords();
        }

        private async void ListViewPasswords_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is CheckedPasswordItem item)
            {
                await ShowPasswordDetail(item);
                
                listViewPasswords.SelectedItem = -1;
                PreviousSelectedItem = e.SelectedItem;
            }
        }

        private async Task ShowPasswordDetail(CheckedPasswordItem item)
        {
            if (!await App.PwdSecurity.CheckOperation(this, "ShowPassword"))
                return;

            Debug.WriteLine("Password: " + item.PasswordDesc);
            Debug.WriteLine("CrackTime: " + item.Item.StrengthData.CrackTime);

            await Navigation.PushAsync(new CheckedPasswordDetailPage
            {
                BindingContext = item
            }, true);
        }
    }

    /// <summary>
    /// Classe che rappresenta un elemento della lista delle password verificate
    /// </summary>
    public class CheckedPasswordItem
    {
        public int Index { get; set; }
        public PasswordCheckData Item { get; set; }
        public Color RowColor
        {
            get => Index % 2 == 0 ? Color.Transparent : AppStyle.ListRowColorEven;
        }
        public string PasswordDesc { get; set; }
        public string PasswordIssues { get; set; }
    }
}
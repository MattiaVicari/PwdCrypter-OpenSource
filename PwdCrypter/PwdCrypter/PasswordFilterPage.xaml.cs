using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PasswordFilterPage : ContentPage
    {
        private FilterOption Filter = new FilterOption();
        private Dictionary<int, AccountType?> AccountTypeOptions = null;
        private Dictionary<int, PwdOrder?> PasswordOrderOptions = null;

        public delegate void EventForFilterOption(object sender, EventArgs e, FilterOption item);

        // Eventi sulla pagina del filtro di ricerca
        public event EventForFilterOption ConfirmFilter;
        public event EventHandler CancelFilter;

        public PasswordFilterPage()
        {
            InitializeComponent();

            LoadOptions();
        }

        private void LoadOptions()
        {
            AccountTypeOptions = new Dictionary<int, AccountType?>
            {
                { 0, null }
            };
            pickerAccountType.Items.Add(AppResources.txtAny);
            foreach (AccountType accountType in Utility.EnumHelper.GetValues<AccountType>())
            {
                string desc = Utility.EnumHelper.GetDescription(accountType);
                AccountTypeOptions.Add((int)(accountType) + 1, accountType);
                pickerAccountType.Items.Add(desc);
            }
            pickerAccountType.SelectedIndex = 0;

            PasswordOrderOptions = new Dictionary<int, PwdOrder?>();
            foreach (PwdOrder order in Utility.EnumHelper.GetValues<PwdOrder>())
            {
                string desc = Utility.EnumHelper.GetDescription(order);
                PasswordOrderOptions.Add((int)(order), order);
                pickerPwdOrder.Items.Add(desc);
            }
            pickerPwdOrder.SelectedIndex = 0;
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;

                stackContent.HorizontalOptions = LayoutOptions.CenterAndExpand;
                stackContent.WidthRequest = fixedWidth;
            }
            else
            {
                stackContent.HorizontalOptions = LayoutOptions.FillAndExpand;
                stackContent.WidthRequest = AutoWidth;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext != null && (BindingContext is FilterOption filterOption))
            {
                Filter = filterOption;
                entryFilter.Text = Filter.Criteria;
                switchAllFields.IsToggled = Filter.AllFields;
                if (!Filter.AccountType.HasValue)
                    pickerAccountType.SelectedIndex = 0;
                else
                    pickerAccountType.SelectedIndex = (int)(Filter.AccountType.Value) + 1;
                if (!Filter.PassowrdOrder.HasValue)
                    pickerPwdOrder.SelectedIndex = 0;
                else
                    pickerPwdOrder.SelectedIndex = (int)(Filter.PassowrdOrder.Value);
                switchFilterCloud.IsToggled = Filter.InCloud;

                switchIssueTooOld.IsToggled = Filter.Issues.Contains(PwdIssue.TooOld);
                switchIssueTooWeak.IsToggled = Filter.Issues.Contains(PwdIssue.TooWeak);
            }

            switchFilterCloud.IsEnabled = App.IsCloudEnabled();
            labelCloudRequirement.IsVisible = !switchFilterCloud.IsEnabled && !Filter.ScopePwdIssues;
        }

        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            if (CancelFilter != null)
                CancelFilter.Invoke(this, e);
            await Navigation.PopAsync(true);
        }

        private async void ConfirmButton_Clicked(object sender, EventArgs e)
        {
            Filter.Criteria = entryFilter.Text;
            Filter.AllFields = switchAllFields.IsToggled;
            if (pickerAccountType.SelectedIndex <= 0)
                Filter.AccountType = null;
            else
            {
                if (AccountTypeOptions.TryGetValue(pickerAccountType.SelectedIndex, out AccountType? account))
                    Filter.AccountType = account;
                else
                    Filter.AccountType = null;
            }
            if (pickerPwdOrder.SelectedIndex <= 0)
                Filter.PassowrdOrder = null;
            else
            {
                if (PasswordOrderOptions.TryGetValue(pickerPwdOrder.SelectedIndex, out PwdOrder? order))
                    Filter.PassowrdOrder = order;
                else
                    Filter.PassowrdOrder = null;
            }
            Filter.InCloud = switchFilterCloud.IsToggled;

            Filter.Issues.Clear();
            if (switchIssueTooOld.IsToggled)
                Filter.Issues.Add(PwdIssue.TooOld);
            if (switchIssueTooWeak.IsToggled)
                Filter.Issues.Add(PwdIssue.TooWeak);

            if (ConfirmFilter != null)
                ConfirmFilter.Invoke(this, e, Filter);
            await Navigation.PopAsync(true);
        }
    }


    /// <summary>
    /// Classe che raggruppa le opzioni di ricerca
    /// </summary>
    public class FilterOption
    {
        private string _Criteria;
        private bool _AllFields;
        private AccountType? _AccountType;
        private bool _InCloud;
        private PwdOrder? _PwdOrder;

        public string Criteria
        {
            get => _Criteria;
            set
            {
                _Criteria = value;
                CheckApplyFilter();
            }
        }
        public bool AllFields
        {
            get => _AllFields;
            set
            {
                _AllFields = value;
                CheckApplyFilter();
            }
        }
        public AccountType? AccountType
        {
            get => _AccountType;
            set
            {
                _AccountType = value;
                CheckApplyFilter();
            }
        }
        public PwdOrder? PassowrdOrder
        {
            get => _PwdOrder;
            set
            {
                _PwdOrder = value;
                CheckApplyFilter();
            }
        }
        public bool InCloud
        {
            get => _InCloud;
            set
            {
                _InCloud = value;
                CheckApplyFilter();
            }
        }

        public ObservableCollection<PwdIssue> Issues { get; private set; }

        public bool ScopePwdIssues { get; set; }

        /// <summary>
        /// Vale true se ci sono criteri di filtro da applicare,
        /// false se non ci sono filtri da applicare.
        /// </summary>
        public bool ApplyFilter { get; private set; }

        public FilterOption()
        {
            Issues = new ObservableCollection<PwdIssue>();
            Issues.CollectionChanged += Issues_CollectionChanged;
            ScopePwdIssues = false;
            Clear();
        }

        private void Issues_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CheckApplyFilter();
        }

        /// <summary>
        /// Reset dei parametri del filtro
        /// </summary>
        public void Clear()
        {
            ApplyFilter = false;

            Criteria = "";
            AllFields = false;
            AccountType = null;
            PassowrdOrder = null;
            InCloud = false;
            Issues.Clear();
        }

        private void CheckApplyFilter()
        {
            ApplyFilter = false;
            ApplyFilter |= (_Criteria != "");
            ApplyFilter |= _AllFields;
            ApplyFilter |= (_AccountType != null);
            ApplyFilter |= (_PwdOrder != null);
            ApplyFilter |= _InCloud;
            ApplyFilter |= (Issues.Count > 0);
        }
    }
}
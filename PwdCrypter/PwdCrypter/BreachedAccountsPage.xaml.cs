using PwdCrypter.BreachChecker;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BreachedAccountsPage : ContentPage
	{
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public BreachedAccountsPage ()
		{
			InitializeComponent ();
		}

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext != null && BindingContext is List<AccountInfo> accounts)
            {                
                int index = 0;
                List<ListBreachedAccountRowItem> items = new List<ListBreachedAccountRowItem>();
                foreach (AccountInfo accountInfo in accounts)
                {
                    items.Add(new ListBreachedAccountRowItem
                    {
                        Index = index++,
                        Item = accountInfo
                    });
                }
                listViewAccounts.ItemsSource = items;
            }
        }

        private void ListViewAccounts_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            // Mostra i dettagli sulle violazioni
            if (e.SelectedItem is ListBreachedAccountRowItem item)
            {
                Navigation.PushAsync(new BreachedAccountDetailsPage
                {
                    BindingContext = item.Item
                }, true);
            }
        }
    }

    /// <summary>
    /// Classe che rappresente un elemento della lista degli account violati
    /// </summary>
    class ListBreachedAccountRowItem
    {
        public int Index { get; set; }
        public AccountInfo Item { get; set; }
        public string Account { get => Item?.Account; }
        public int BreachesCount
        {
            get
            {
                if (Item != null)
                    return Item.BreachesCount;
                return 0;
            }
        }
        public Color RowColor
        {
            get => Index % 2 == 0 ? Color.Transparent : AppStyle.ListRowColorEven;
        }
    }
}
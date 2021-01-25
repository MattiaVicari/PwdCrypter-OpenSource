using PwdCrypter.BreachChecker;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class BreachedAccountDetailsPage : ContentPage
	{
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public BreachedAccountDetailsPage ()
		{
			InitializeComponent ();
		}

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext != null && BindingContext is AccountInfo account)
            {
                Title = AppResources.titleBreachedAccountDetails + ": " + account.Account;

                int index = 0;
                List<ListBreachesRowItem> listBreaches = new List<ListBreachesRowItem>();
                foreach (BreachInfo breachInfo in account.Breaches)
                {
                    listBreaches.Add(new ListBreachesRowItem
                    {
                        Index = index++,
                        Item = breachInfo
                    });
                }
                listViewDetails.ItemsSource = listBreaches;
            }
        }

        private void ShowDescription_Clicked(object sender, EventArgs e)
        {
            if ((sender as Button).CommandParameter is string description)
            {
                Navigation.PushModalAsync(new WebViewPage
                {
                    HtmlData = description,
                    Title = AppResources.txtDescription,
                    ModalMode = true
                }, true);
            }
        }
    }

    /// <summary>
    /// Classe che rappresenta i dettagli sulle violazioni di un account
    /// </summary>
    class ListBreachesRowItem
    {
        public int Index { get; set; }
        public BreachInfo Item { get; set; }

        public Color RowColor
        {
            get => Index % 2 == 0 ? AppStyle.ListRowColorOdd : AppStyle.ListRowColorEven;
        }

        public string Name { get => Item?.Name; }
        public string Title { get => Item?.Title; }
        public string Domain { get => Item?.Domain; }
        public string BreachDate { get => Item?.BreachDate.ToString("dd-MM-yyyy"); }
        public string AddedDate { get => Item?.AddedDate.ToString("dd-MM-yyyy HH:mm:ss"); }
        public string ModifiedDate { get => Item?.ModifiedDate.ToString("dd-MM-yyyy HH:mm:ss"); }
        public string Description { get => Item?.Description; }
        public string DataClasses { get => string.Join("\n", Item?.DataClasses.ToArray()); }
        public string IsSpamList
        {
            get
            {
                if (Item != null && Item.IsSpamList)
                    return AppResources.txtYes;
                return AppResources.txtNo;
            }
        }
        public ImageSource LogoPath
        {
            get
            {
                if (!string.IsNullOrEmpty(Item?.LogoPath))
                    return ImageSource.FromUri(new Uri(Item.LogoPath));
                return null;
            }
        }
    }
}
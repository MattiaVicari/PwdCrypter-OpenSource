using Newtonsoft.Json.Linq;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NewsListPage : ContentPage
    {
        const string URL_WEBSITE_API_NEWS = "https://www.mydomain.com/api/news/";

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        private void SetLoading(bool value)
        {
            spinnerLoading.IsRunning = value;
            spinnerLoading.IsEnabled = value;
            spinnerLoading.IsVisible = value;
            listViewNews.IsVisible = !value;
        }

        public NewsListPage()
        {
            InitializeComponent();
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            if (listViewNews.ItemsSource == null)
                await LoadNews();
        }

        private async Task LoadNews()
        {
            const string NewsUrl = URL_WEBSITE_API_NEWS;

            listViewNews.ItemsSource = null;
            List<NewsListRowItem> newsList = new List<NewsListRowItem>();

            SetLoading(true);
            HttpClient httpClient = new HttpClient();
            try
            {
                int offset = 0, newsCount = 0, totalNewsCount = 0, index = 1;

                do
                {
                    offset = newsCount;
                    string url = string.Format(NewsUrl, offset);
                    HttpResponseMessage responseMessage = await httpClient.GetAsync(url);
                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        Debug.WriteLine("News list error. Status: " + responseMessage.StatusCode);
                        string response = await responseMessage.Content.ReadAsStringAsync();
                        JObject jsonError = JObject.Parse(response);
                        if (jsonError.ContainsKey("message"))
                            throw new Exception(jsonError.Value<string>("message"));
                        else
                            throw new Exception(responseMessage.StatusCode.ToString());
                    }

                    string data = await responseMessage.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(data);

                    if (totalNewsCount == 0)
                        totalNewsCount = json.Value<int>("total_count");

                    JArray notifications = json.GetValue("notifications").ToObject<JArray>();
                    foreach(JObject jsonNot in notifications)
                    {
                        if (!jsonNot.ContainsKey("news"))
                            continue;
                        
                        string propertyByLang = AppResources.langIDSuper;
                        JObject news = jsonNot.GetValue("news").ToObject<JObject>();

                        if (news == null || (news != null && !news.ContainsKey(propertyByLang)))
                            continue;

                        DateTime sentDate = DateTime.Now;
                        try
                        {
                            if (jsonNot.ContainsKey("sent_date") && !string.IsNullOrEmpty(jsonNot.Value<string>("sent_date")))
                                sentDate = jsonNot.Value<DateTime>("sent_date");
                        }
                        catch(Exception ex)
                        {
                            Debug.WriteLine("Failed to parse date. Error: " + ex.Message);
                            sentDate = DateTime.Now;
                        }
                    
                        JObject newsData = news.GetValue(propertyByLang).ToObject<JObject>();
                        newsList.Add(new NewsListRowItem
                        {
                            Index = index++,
                            Item = new NewsInfo
                            {
                                Title = newsData.Value<string>("title"),
                                Content = newsData.Value<string>("content"),
                                Date = sentDate,
                                IsModal = false
                            }
                        });
                    }

                    newsCount = json.Value<int>("in_page_count");
                }
                while (offset + newsCount < totalNewsCount);

                listViewNews.ItemsSource = newsList;
            }
            catch(HttpRequestException ex)
            {
                Debug.WriteLine("Error occurred during the loading of the news list. Error: " + ex.Message + "\nException: " + ex.InnerException);
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Error occurred during the loading of the news list. Error: " + ex.Message);
            }
            finally
            {
                httpClient.Dispose();
                SetLoading(false);
                lblNoNews.IsVisible = (newsList.Count == 0);
                listViewNews.IsVisible = (listViewNews.ItemsSource != null && newsList.Count > 0);
            }
        }

        private void ListViewNews_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is NewsListRowItem itemNews)
            {
                Navigation.PushAsync(new NewsPage
                {
                    BindingContext = itemNews.Item
                }, true);

                listViewNews.SelectedItem = null;
            }
        }
    }

    /// <summary>
    /// Rappresenta un elemento della lista delle news
    /// </summary>
    class NewsListRowItem
    {
        public int Index { get; set; }
        public NewsInfo Item { get; set; }
        public Color RowColor
        {
            get => Index % 2 == 0 ? Color.Transparent : AppStyle.ListRowColorEven;
        }
        public string NewsTitle { get => Item?.Title; }
        public string NewsDate 
        {  
            get
            {
                if (Item != null)
                {
                    try
                    {
                        System.Globalization.CultureInfo cultureInfo = new System.Globalization.CultureInfo(AppResources.langID, false);
                        return string.Format(AppResources.txtNewsOf, Item.Date.ToString(cultureInfo));
                    }
                    catch(System.Globalization.CultureNotFoundException)
                    {
                        return string.Format(AppResources.txtNewsOf, Item.Date.ToString("dd/MM/yyyy HH:mm"));
                    }
                }
                return "";
            }
        }
    }
}
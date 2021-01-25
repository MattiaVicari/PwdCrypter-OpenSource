using System;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NewsPage : ContentPage
    {
        public NewsPage()
        {
            InitializeComponent();
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            if (Height < App.ViewStepMaxHeight)
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Medium, lblTitle.GetType());
            }
            else
            {
                lblTitle.FontSize = Device.GetNamedSize(NamedSize.Large, lblTitle.GetType());
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext != null && BindingContext is NewsInfo Data)
                btnClose.IsVisible = Data.IsModal;
        }

        private void Close_Clicked(object sender, EventArgs e)
        {
            // Pulisce i dati della news se proveniente da push notification
            App.ResetPushNotificationData();
            // Torna indietro
            Navigation.PopModalAsync(true);
        }

        private void CustomWebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            // Apre i link esternamente
            if (e.Url.StartsWith("http"))
            {
                try
                {
                    var uri = new Uri(e.Url);
                    Xamarin.Essentials.Launcher.OpenAsync(uri);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Navigating error: " + ex.Message);
                }

                e.Cancel = true;
            }
        }
    }

    /// <summary>
    /// Oggetto che raccoglie le informazioni sulla news da visualizzare
    /// </summary>
    public class NewsInfo
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public bool IsModal { get; set; }

        public NewsInfo()
        {
            Title = "Title";
            Content = "...";
            Date = DateTime.Now;
            IsModal = true;
        }
    }
}
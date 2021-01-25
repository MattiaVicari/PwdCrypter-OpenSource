using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FileSelectionPage : ContentPage
    {
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public delegate Task FileSelected(object sender, FileData selectedFile);

        public event FileSelected OnFileSelected = null;

        public FileSelectionPage()
        {
            InitializeComponent();
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;

                gridMain.HorizontalOptions = LayoutOptions.CenterAndExpand;
                gridMain.WidthRequest = fixedWidth;
            }
            else
            {
                gridMain.HorizontalOptions = LayoutOptions.FillAndExpand;
                gridMain.WidthRequest = AutoWidth;
            }
        }

        private void BtnConfirm_Clicked(object sender, EventArgs e)
        {
            if (listViewFiles.SelectedItem == null)
            {
                Debug.WriteLine("FileSelection: no file selected");
                return;
            }

            var data = listViewFiles.SelectedItem as FileData;
            Navigation.PopAsync(true);
            OnFileSelected?.Invoke(sender, data);
        }

        /// <summary>
        /// Classe che raccoglie le informazioni sul file da mostrare nella lista
        /// </summary>
        public class FileData
        {
            public int Order { get; set; }
            public string Name { get; set; }
            public DateTime CreationDateTime { get; set; }
            public Color RowColor
            {
                get => Order % 2 == 0 ? Color.Transparent : AppStyle.ListRowColorEven;
            }
        }
    }    
}
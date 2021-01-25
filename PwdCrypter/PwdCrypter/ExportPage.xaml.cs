using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ExportPage : ContentPage
    {
        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }
        public static Dictionary<string, string> SupportIconColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#000000" }, { "currentOpacity", "1.0" } };
        }

        private ToolbarItem ToolbarItemFilter = null;
        private ToolbarItem ToolbarItemClearFilter = null;
        private FilterOption Filter = new FilterOption();

        private bool CurrentCloud = false;
        private WaitPage _currentWaitPage = null;

        private async Task StartWait() 
        {
            _currentWaitPage = await Utility.StartWait(this);
        }
        private async Task StopWait()
        {
            await Utility.StopWait(_currentWaitPage);
            _currentWaitPage = null;
        }

        public ExportPage ()
		{
			InitializeComponent ();

            LoadOptions();
            PrepareToolbar();
            SetupToolbar();
        }

        private void PrepareToolbar()
        {
            ToolbarItemClearFilter = new ToolbarItem(AppResources.txtClearFilter, "Assets/clear_filter.png", () => ClearFilter());
            ToolbarItemFilter = new ToolbarItem(AppResources.txtSearch, "Assets/search.png", () => OpenFilterPanel());
        }

        private void ClearFilter()
        {
            Filter.Clear();
            SetupToolbar();
        }

        private void OpenFilterPanel()
        {
            ExportFilterPage page = new ExportFilterPage
            {
                BindingContext = Filter
            };
            page.ConfirmFilter += Page_ConfirmFilter;
            Navigation.PushAsync(page, true);
        }

        private void Page_ConfirmFilter(object sender, EventArgs e, FilterOption item)
        {
            Filter = item;
            SetupToolbar();
        }

        private void SetupToolbar()
        {
            ToolbarItems.Clear();
            if (Filter.ApplyFilter)
                ToolbarItems.Add(ToolbarItemClearFilter);
            ToolbarItems.Add(ToolbarItemFilter);
        }

        private void LoadOptions()
        {
            List<ExportFormatItem> formats = new List<ExportFormatItem>
            {
                new ExportFormatItem { Format = ImportExportFileFormat.JSON, FormatName = AppResources.txtFormatNameJSON, FormatDescription = AppResources.txtFormatDescJSON },
                new ExportFormatItem { Format = ImportExportFileFormat.ZIP, FormatName = AppResources.txtFormatNameZIP, FormatDescription = AppResources.txtFormatDescZIP },
                new ExportFormatItem { Format = ImportExportFileFormat.LIST, FormatName = AppResources.txtFormatNameLIST, FormatDescription = AppResources.txtFormatDescLIST }
            };
            listViewFormat.ItemsSource = formats;
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

        private async void ListViewFormat_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            const string baseFileName = "password_list_";

            if (e.SelectedItem != null && e.SelectedItem is ExportFormatItem item)
            {
                await StartWait();
                CurrentCloud = App.PwdManager.Cloud;
                App.PwdManager.Cloud = Filter.InCloud;
                try
                {
                    string defaultFilePath = baseFileName + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "." + item.FormatExt;
                    ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();

                    string fileTypeDesc = Utility.EnumHelper.GetDescription(item.Format);
                    List<KeyValuePair<string, List<string>>> fileTypeList = new List<KeyValuePair<string, List<string>>>
                    {
                        new KeyValuePair<string, List<string>>(fileTypeDesc, new List<string>() { "." + item.FormatExt })
                    };

                    // Per ottenere la lista delle password locali o su Cloud
                    await App.PwdManager.GetPasswordList();

                    MemoryStream streamData = new MemoryStream();
                    long count = await App.PwdManager.ExportPasswordListToBytes(item.Format, streamData);
                    streamData.Position = 0;
                    if (count == 0)
                        throw new Exception("Export failed!");

                    Device.BeginInvokeOnMainThread(async () =>
                    {                        
                        try
                        {
                            specialFolder.Init();
                            specialFolder.OnSaveFileHandler += Export_OnSaveFileHandler;
                            if (!await specialFolder.SaveFile(defaultFilePath, fileTypeList, streamData))
                            {
                                specialFolder.Finish();
                                specialFolder.OnSaveFileHandler -= Export_OnSaveFileHandler;
                                
                                // Ritorna alla pagina precedente
                                await Navigation.PopAsync(true);

                                App.PwdManager.Cloud = CurrentCloud;
                                await StopWait();
                            }
                        }
                        catch(Exception ex)
                        {
                            await DisplayAlert(App.Title, AppResources.errExportPwdList + " " + ex.Message, "Ok");
                            
                            // Ritorna alla pagina precedente
                            await Navigation.PopAsync(true);

                            specialFolder.Finish();
                            App.PwdManager.Cloud = CurrentCloud;
                            await StopWait();
                        }
                    });
                }
                catch (Exception Ex)
                {
                    await DisplayAlert(App.Title, AppResources.errExportPwdList + " " + Ex.Message, "Ok");

                    App.PwdManager.Cloud = CurrentCloud;
                    await StopWait();
                }
            }
        }

        private async void Export_OnSaveFileHandler(object sender, FolderPickerResult info)
        {
            ICrossPlatformSpecialFolder specialFolder = DependencyService.Get<ICrossPlatformSpecialFolder>();
            specialFolder.Finish();
            specialFolder.OnSaveFileHandler -= Export_OnSaveFileHandler;

            if (!info.Succeeded)
                await DisplayAlert(App.Title, AppResources.errExportPwdList + " " + info.Error, "Ok");
            
            // Ritorna alla pagina precedente
            await Navigation.PopAsync(true);

            App.PwdManager.Cloud = CurrentCloud;
            await StopWait();
        }

        /// <summary>
        /// Classe che rappresenta un elemento della lista dei formati di esportazione
        /// </summary>
        public class ExportFormatItem
        {
            public ImportExportFileFormat Format { get; set; }
            public string FormatName { get; set; }
            public string FormatDescription { get; set; }
            public string SupportIcon { get; set; }

            public string FormatExt
            {
                get
                {
                    switch (Format)
                    {
                        case ImportExportFileFormat.ZIP:
                            return "zip";
                        case ImportExportFileFormat.JSON:
                            return "json";
                        default:
                        case ImportExportFileFormat.LIST:
                            return "list";
                    }
                }
            }

            public ExportFormatItem()
            {
                Format = ImportExportFileFormat.LIST;
                FormatName = "";
                FormatDescription = "";
                SupportIcon = "Assets/SVG/angle_right_solid.svg";
            }
        }
    }
}
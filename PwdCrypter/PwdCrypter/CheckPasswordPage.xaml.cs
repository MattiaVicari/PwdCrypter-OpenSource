using FFImageLoading.Svg.Forms;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class CheckPasswordPage : ContentPage
	{
        private float ProcessPercentage = 0.0f;
        private readonly PasswordChecker AccountChecker = null;

        //private Label labelLastCheckDateTime = null;

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }
        public static Dictionary<string, string> BackgroundInnerImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#f5ce42" }, { "currentOpacity", "1" } };
        }

        public CheckPasswordPage ()
		{
			InitializeComponent ();

            AccountChecker = new PasswordChecker();
            AccountChecker.OnVerify += AccountChecker_OnVerify;
        }

        private void SKCanvasView_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            float width = e.Info.Width;
            float height = e.Info.Height;
            float radius = Math.Min(width, height) * 0.9f / 2.0f;
            float cx = width / 2.0f, cy = height / 2.0f;

            canvas.Clear();

            canvas.DrawCircle(cx, cy, radius, new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.LightGray,
                IsAntialias = true,
                Style = SkiaSharp.SKPaintStyle.Stroke,
                StrokeWidth = 10.0f
            });

            var arcPercentage = new SkiaSharp.SKPath();
            var rect = new SkiaSharp.SKRect(cx - radius, cy - radius, cx + radius, cy + radius);
            arcPercentage.AddArc(rect, 0.0f, ProcessPercentage * 360.0f / 100.0f);
            canvas.DrawPath(arcPercentage, new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.Orange,
                IsAntialias = true,
                Style = SkiaSharp.SKPaintStyle.Stroke,
                StrokeWidth = 10.0f
            });

            canvas.DrawText(ProcessPercentage + "%", cx, cy + radius / 5.0f, new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.DarkGray,
                IsAntialias = true,
                TextSize = radius * 0.5f,
                TextAlign = SkiaSharp.SKTextAlign.Center
            });
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                scrollViewMain.WidthRequest = App.ViewStepMaxWidth - 140;
                scrollViewMain.HorizontalOptions= LayoutOptions.Center;

                gridMain.WidthRequest = App.ViewStepMaxWidth - 140;
                gridMain.HorizontalOptions = LayoutOptions.Center;
            }
            else
            {
                scrollViewMain.WidthRequest = AutoWidth;
                scrollViewMain.HorizontalOptions = LayoutOptions.FillAndExpand;

                gridMain.WidthRequest = AutoWidth;
                gridMain.HorizontalOptions = LayoutOptions.FillAndExpand;
            }

            if (Height > App.ViewStepMaxHeight)
            {
                gridMain.RowDefinitions[0].Height = new GridLength(Height * 0.4, GridUnitType.Absolute);
            }
            else
            {
                gridMain.RowDefinitions[0].Height = new GridLength(150, GridUnitType.Absolute);
            }

            imgHistory.HeightRequest = Height / 4.0;            
        }

        private void ShowLastCheck(string lastCheckDateTime)
        {
            stackAlert.IsVisible = true;
            try
            {
                labelLastCheckDateTime.Text = lastCheckDateTime;
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Unable to update data in the alert stacklayout. Error: " + ex.Message);
            }  
        }

        private void CheckLastCheck()
        {
            string fileCache = AccountChecker.CacheFilePath;            
            if (File.Exists(fileCache))
            {
                try
                {
                    AccountChecker.LoadDataFromCache();
                    viewCircularProgBar.IsVisible = false;

                    ShowLastCheck(AppResources.txtLastCheck + " " + AccountChecker.LastCheck.ToString("dd-MM-yyyy, HH:mm:ss"));
                    AnalyzeResult(AccountChecker.GetResult());
                }
                catch (Exception Ex)
                {
                    DisplayAlert(App.Title, Ex.Message, "Ok");
                }
            }
        }

        private void CheckNextScheduleDate()
        {
            labelNextCheckDateTime.IsVisible = false;
            if (App.Settings.CheckPasswordFrequency > 0)
            {
                labelNextCheckDateTime.IsVisible = true;
                IBackgroundTaskManager taskManager = DependencyService.Get<IBackgroundTaskManager>();
                DateTime nextCheckDateTime = taskManager.GetNextExecutionDate(App.CheckPasswordBackgroundTaskName);
                if (nextCheckDateTime > DateTime.MinValue)
                    labelNextCheckDateTime.Text = string.Format(AppResources.txtNextCheckScheduled, nextCheckDateTime.ToString("dd-MM-yyyy, HH:mm:ss"));
                else
                    labelNextCheckDateTime.IsVisible = false;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            App.ResetPushNotificationData();

            if (!App.IsLoggedIn)
            {
                Debug.WriteLine("You are not logged in");
                await Navigation.PushModalAsync(new LoginPage
                {
                    BindingContext = new Utility.RedirectData
                    {
                        RedirectTo = this,
                        Modal = true
                    }
                }, true);
                return;
            }

            App.PwdSecurity.BeginOperation();

            // Verifica se ho i dati in cache e mostra il pannello con la data e l'ora dell'ultima esecuzione
            CheckLastCheck();
            // Prossima data di esecuzione se la schedulazione è attivva
            CheckNextScheduleDate();
        }

        private async void BtnGo_Clicked(object sender, EventArgs e)
        {
            ProcessPercentage = 0.0f;

            viewCircularProgBar.IsVisible = true;
            stackAlert.IsVisible = false;

            viewCircularProgBar.InvalidateSurface();

            btnGo.IsEnabled = false;
            btnShowDetails.IsVisible = false;
            try
            {
                labelResult.Text = AppResources.txtPleaseWait;                

                await Task.Run(async () =>
                {
                    await AccountChecker.VerifyPasswords();
                    return AccountChecker.GetResult();
                }).ContinueWith((data) =>
                {
                    Device.BeginInvokeOnMainThread(() => 
                    {
                        AnalyzeResult(data.Result);
                        CheckLastCheck();
                    });
                });
            }
            catch(Exception Ex)
            {
                Debug.WriteLine("CheckPassword: error occurred " + Ex.Message);
                await DisplayAlert(App.Title, AppResources.errCheckingPassword + " " + Ex.Message, "Ok");                 
                labelResult.Text = AppResources.errGenericError;
            }
            finally
            {
                btnGo.IsEnabled = true;
            }
        }

        private void AnalyzeResult(List<PasswordCheckData> result)
        {
            btnShowDetails.IsVisible = result?.Count > 0;
            if (result?.Count > 0)
            {
                if (result.Count > 1)
                    labelResult.Text = string.Format(AppResources.txtPasswordsAttention, result.Count);
                else
                    labelResult.Text = AppResources.txtOnePasswordAttention;
                if (AccountChecker.NumberOfSkippedPassword > 0)
                    labelResult.Text += "\n" + string.Format(AppResources.txtPasswordSkipped, AccountChecker.NumberOfSkippedPassword);
            }
            else
                labelResult.Text = AppResources.txtNoPasswordAttention;
        }

        private void AccountChecker_OnVerify(int itemId, int itemCount, PwdListItem itemInfo)
        {
            // Aggiorna la percentuale del processo di verifica
            ProcessPercentage = (float)Math.Round((100.0f * itemId) / (1.0f * itemCount));
            viewCircularProgBar.InvalidateSurface();
        }

        private async void BtnShowDetails_Clicked(object sender, EventArgs e)
        {
            // Apre la pagina con la lista delle password che richiedono attenzione
            await Navigation.PushAsync(new CheckedPasswordPage
            {
                Data = AccountChecker.GetResult()
            }, true);            
        }
    }
}
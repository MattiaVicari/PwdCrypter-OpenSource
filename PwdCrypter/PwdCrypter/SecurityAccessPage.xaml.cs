using PwdCrypter.Extensions.ResxLocalization.Resx;
using PwdCrypter.Logger;
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
    public partial class SecurityAccessPage : ContentPage
    {
        #region BackgroundTask
        private readonly IBackgroundTaskManager BckTaskManager = null;
        #endregion

        private bool IsLoading;
        private Dictionary<string, SecurityAccess> AccessOptions;
        private Dictionary<string, TimeDef> CheckPwdFrequencyOptions;
        private Dictionary<string, TimeDef> BackupFrequencyOptions;
        private Dictionary<string, TimeDef> BackupHistoryOptions;

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public static GridLength BackupHistoryRowHeight { get => App.IsCloudAvailable ? new GridLength(0, GridUnitType.Auto) : 0; }

        public SecurityAccessPage()
        {
            InitializeComponent();

            BckTaskManager = DependencyService.Get<IBackgroundTaskManager>();
            LoadSettings();
        }

        private void LoadSettings()
        {            
            const int DefaultAccessOption = 0;          // Accesso con solo la master password
            const int DefaultPeriodCheckPwdOption = 0;  // Non eseguire il controllo periodico delle password
            const int DefaultBackupFrequencyOption = 0; // Non eseguire il backup periodico
            const int DefaultBackupHistoryOptions = 0;  // Non mantenere lo storico dei backup

            IsLoading = true;
            int index;

            /* Controllo periodico delle password */
            CheckPwdFrequencyOptions = new Dictionary<string, TimeDef>
            {
                { AppResources.txtNever, new TimeDef { Id = 0, Kind = TimeKind.Day, Time = 0 } },
                { "1 " + AppResources.txtWeek, new TimeDef { Id = 1, Kind = TimeKind.Day, Time = 7 } },
                { "1 " + AppResources.txtMonth, new TimeDef { Id = 2, Kind = TimeKind.Month, Time = 1 } },
                { "6 " + AppResources.txtMonths, new TimeDef { Id = 3, Kind = TimeKind.Month, Time = 6 } },
                { "1 " + AppResources.txtYear, new TimeDef { Id = 4, Kind = TimeKind.Year, Time = 1 } }
            };
#if DEBUG
            CheckPwdFrequencyOptions.Add("1 " + AppResources.txtHour + " (DEBUG)", new TimeDef { Id = 5, Kind = TimeKind.Hour, Time = 1 });
            CheckPwdFrequencyOptions.Add("60 " + AppResources.txtSeconds + " (DEBUG)", new TimeDef { Id = 6, Kind = TimeKind.Minute, Time = 1 } );
            CheckPwdFrequencyOptions.Add("15 " + AppResources.txtMinutes + " (DEBUG)", new TimeDef { Id = 7, Kind = TimeKind.Minute, Time = 15 } );
#endif

            index = 0;
            foreach (KeyValuePair<string, TimeDef> item in CheckPwdFrequencyOptions)
            {
                pickerPeriodicallyCheckPwd.Items.Add(item.Key);
                if (App.Settings.CheckPasswordFrequency == item.Value.Id)
                    pickerPeriodicallyCheckPwd.SelectedIndex = index;
                index++;
            }

            if (pickerPeriodicallyCheckPwd.SelectedIndex < 0)
                pickerPeriodicallyCheckPwd.SelectedIndex = DefaultPeriodCheckPwdOption;
            stackDateTimePeriodicallyCheckPwd.IsVisible = (pickerPeriodicallyCheckPwd.SelectedIndex != 0);
            datePickerPeriodicallyCheckPwd.Date = App.Settings.CheckPasswordStartDate.Date;
            timePickerPeriodicallyCheckPwd.Time = App.Settings.CheckPasswordStartDate.TimeOfDay;
            /* -- */

            /* Opzioni di accesso */
            IFingerprintAuth fingerprint = DependencyService.Get<IFingerprintAuth>();
            AccessOptions = new Dictionary<string, SecurityAccess>
            {
                { AppResources.txtMasterPassword, SecurityAccess.MasterPassword },
                { AppResources.txtTwoFactor, SecurityAccess.TwoFactor }
            };
            if (Device.RuntimePlatform == Device.Android && fingerprint != null && fingerprint.IsAvailable())
                AccessOptions.Add(AppResources.txtFingerprint, SecurityAccess.Fingerprint);                            

            index = 0;
            foreach (KeyValuePair<string, SecurityAccess> item in AccessOptions)
            {
                pickerSecurityAccess.Items.Add(item.Key);
                if (App.Settings.Access == item.Value)
                    pickerSecurityAccess.SelectedIndex = index;
                index++;
            }

            if (pickerSecurityAccess.SelectedIndex < 0)
                pickerSecurityAccess.SelectedIndex = DefaultAccessOption;
            /* -- */

            /* Pianificazione backup */
            BackupFrequencyOptions = new Dictionary<string, TimeDef>
            {
                { AppResources.txtNever, new TimeDef { Id = 0, Kind = TimeKind.Day, Time = 0 } },
                { "1 " + AppResources.txtWeek, new TimeDef { Id = 1, Kind = TimeKind.Day, Time = 7 } },
                { "1 " + AppResources.txtMonth, new TimeDef { Id = 2, Kind = TimeKind.Month, Time = 1 } },
                { "6 " + AppResources.txtMonths, new TimeDef { Id = 3, Kind = TimeKind.Month, Time = 6 } },
                { "1 " + AppResources.txtYear, new TimeDef { Id = 4, Kind = TimeKind.Year, Time = 1 } }
            };
#if DEBUG
            BackupFrequencyOptions.Add("60 " + AppResources.txtSeconds + " (DEBUG)", new TimeDef { Id = 5, Kind = TimeKind.Minute, Time = 1 } );
            BackupFrequencyOptions.Add("15 " + AppResources.txtMinutes + " (DEBUG)", new TimeDef { Id = 6,  Kind = TimeKind.Minute, Time = 15 } );
#endif

            index = 0;
            foreach (KeyValuePair<string, TimeDef> item in BackupFrequencyOptions)
            {
                pickerBackupFrequency.Items.Add(item.Key);
                if (App.Settings.BackupFrequency == item.Value.Id)
                    pickerBackupFrequency.SelectedIndex = index;
                index++;
            }

            if (pickerBackupFrequency.SelectedIndex < 0)
                pickerBackupFrequency.SelectedIndex = DefaultBackupFrequencyOption;
            stackDateTimeBackupFrequency.IsVisible = (pickerBackupFrequency.SelectedIndex != 0);
            datePickerBackupFrequency.Date = App.Settings.BackupStartDate.Date;
            timePickerBackupFrequency.Time = App.Settings.BackupStartDate.TimeOfDay;
            /* -- */

            /* Durata dello storico dei backup */
            BackupHistoryOptions = new Dictionary<string, TimeDef>
            {
                { AppResources.txtNever, new TimeDef { Id = 0, Kind = TimeKind.Day, Time = 0 } },
                { "1 " + AppResources.txtWeek, new TimeDef { Id = 1, Kind = TimeKind.Day, Time = 7 } },
                { "1 " + AppResources.txtMonth, new TimeDef { Id = 2, Kind = TimeKind.Month, Time = 1 } },
                { "6 " + AppResources.txtMonths, new TimeDef { Id = 3, Kind = TimeKind.Month, Time = 6 } },
                { "1 " + AppResources.txtYear, new TimeDef { Id = 4, Kind = TimeKind.Year, Time = 1 } }
            };
#if DEBUG
            BackupHistoryOptions.Add("60 " + AppResources.txtSeconds + " (DEBUG)", new TimeDef { Id = 5, Kind = TimeKind.Minute, Time = 1 } );
            BackupHistoryOptions.Add("15 " + AppResources.txtMinutes + " (DEBUG)", new TimeDef { Id = 6,  Kind = TimeKind.Minute, Time = 15 } );
#endif

            index = 0;
            foreach (KeyValuePair<string, TimeDef> item in BackupHistoryOptions)
            {
                pickerBackupHistory.Items.Add(item.Key);
                if (App.Settings.BackupHistory == item.Value.Id)
                    pickerBackupHistory.SelectedIndex = index;
                index++;
            }

            if (pickerBackupHistory.SelectedIndex < 0)
                pickerBackupHistory.SelectedIndex = DefaultBackupHistoryOptions;
            /* -- */

            IsLoading = false;
        }

        private void ShowOptions()
        {
            SecurityAccess security = AccessOptions.ElementAt(pickerSecurityAccess.SelectedIndex).Value;
            btnConfigureFingerprint.IsVisible = (security == SecurityAccess.Fingerprint);
            btnConfigureTwoFactor.IsVisible = (security == SecurityAccess.TwoFactor);

            if (security == SecurityAccess.Fingerprint)
            {
                bool configured = App.PwdManager.IsFingerprintAccessConfigured();
                stackFingerprintConfigured.IsVisible = configured;
                btnConfigureFingerprint.Text = !configured ? AppResources.btnConfigure : AppResources.btnReconfigure;
            }
            else
                stackFingerprintConfigured.IsVisible = false;

            if (security == SecurityAccess.TwoFactor)
            {
                bool configured = App.PwdManager.Is2FAAccessConfigured();
                stack2FAConfigured.IsVisible = configured;
                btnConfigureTwoFactor.Text = !configured ? AppResources.btnConfigure : AppResources.btnReconfigure;
            }
            else
                stack2FAConfigured.IsVisible = false;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.PwdSecurity.BeginOperation();
            ShowOptions();
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;

            if (Width > App.ViewStepMaxWidth)
            {
                double fixedWidth = App.ViewStepMaxWidth - 140;

                scrollMain.HorizontalOptions = LayoutOptions.CenterAndExpand;
                scrollMain.WidthRequest = fixedWidth;
            }
            else
            {
                scrollMain.HorizontalOptions = LayoutOptions.FillAndExpand;
                scrollMain.WidthRequest = AutoWidth;
            }
        }

        private async void PickerSecurityAccess_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsLoading)
                return;

            ShowOptions();

            SecurityAccess oldAccess = App.Settings.Access;
            await SaveSettings();

            // Se ho rimosso l'impostazione dell'autenticazione a due fattori, aggiorna il file
            if (oldAccess == SecurityAccess.TwoFactor && App.Settings.Access != SecurityAccess.TwoFactor
                && App.PwdManager.Is2FAAccessConfigured())
            {
                App.PwdManager.Access2FA.Clear();
                await App.PwdManager.UpdateLocalPasswordFile();                
            }
        }

        private async Task SaveSettings()
        {
            try
            {
                App.Settings.Access = AccessOptions.ElementAt(pickerSecurityAccess.SelectedIndex).Value;
                App.Settings.CheckPasswordFrequency = CheckPwdFrequencyOptions.ElementAt(pickerPeriodicallyCheckPwd.SelectedIndex).Value.Id;
                App.Settings.CheckPasswordStartDate = datePickerPeriodicallyCheckPwd.Date.Add(timePickerPeriodicallyCheckPwd.Time);
                App.Settings.BackupFrequency = BackupFrequencyOptions.ElementAt(pickerBackupFrequency.SelectedIndex).Value.Id;
                App.Settings.BackupStartDate = datePickerBackupFrequency.Date.Add(timePickerBackupFrequency.Time);
                App.Settings.BackupHistory = BackupHistoryOptions.ElementAt(pickerBackupHistory.SelectedIndex).Value.Id;
                App.Settings.WriteSettings();
            }
            catch(Exception e)
            {
                await DisplayAlert(App.Title, AppResources.errSettingsUpdate + " " + e.Message, "OK");
            }
        }

        private async void ConfigureFingerprint_OnClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new FingerprintPage(), true);
        }

        private async void ConfigureTwoFactor_OnClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new TwoFactorAuthPage(), true);
        }

        private async void PickerPeriodicallyCheckPwd_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool registra = true;
            if (IsLoading)
                return;

            uint oldValue = App.Settings.CheckPasswordFrequency;
            await SaveSettings();
            stackDateTimePeriodicallyCheckPwd.IsVisible = (App.Settings.CheckPasswordFrequency > 0);
            if (oldValue == 0 && App.Settings.CheckPasswordFrequency > 0)
            {
                DateTime precStartDate = datePickerPeriodicallyCheckPwd.Date;
                datePickerPeriodicallyCheckPwd.Date = DateTime.Now.Date.AddDays(1.0);   // Domani.

                // Se non è cambiata l'ora non scatta l'evento sul date picker
                registra = precStartDate.Equals(datePickerPeriodicallyCheckPwd.Date);
            }

            if (registra)
            {
                UnregisterCheckPasswordBackgroundTask();
                // Registrazione del task se impostato un intervallo di controllo
                if (App.Settings.CheckPasswordFrequency > 0)
                    await RegisterCheckPasswordBackgroundTask();
            }            
        }

        #region BackgroundTask
        private async Task RegisterBackgroundTask(string taskName, string entryPoint, DateTime startDateTime, TimeDef intervalDef)
        {
            if (BckTaskManager == null)
                return;                        
            await BckTaskManager.Register(taskName, entryPoint, startDateTime, intervalDef.Kind, intervalDef.Time);
        }
        private void UnregisterBackgroundTask(string taskName)
        {
            if (BckTaskManager != null)
                BckTaskManager.Unregister(taskName);
        }

        /// <summary>
        /// Registra l'attività in background per la verifica delle password
        /// </summary>
        /// <returns></returns>
        private async Task RegisterCheckPasswordBackgroundTask()
        {
            TimeDef interval = CheckPwdFrequencyOptions.First((item) => { return item.Value.Id == App.Settings.CheckPasswordFrequency; }).Value;
            await RegisterBackgroundTask(
                App.CheckPasswordBackgroundTaskName,
                App.CheckPasswordBackgroundTaskEntryPoint,
                App.Settings.CheckPasswordStartDate,
                interval);            
        }

        /// <summary>
        /// Registra l'attività in background per il backup
        /// </summary>
        /// <returns></returns>
        private async Task RegisterBackupBackgroundTask()
        {
            TimeDef interval = BackupFrequencyOptions.First((item) => { return item.Value.Id == App.Settings.BackupFrequency; }).Value;
            await RegisterBackgroundTask(
                App.BackupBackgroundTaskName,
                App.BackupBackgroundTaskEntryPoint,
                App.Settings.BackupStartDate,
                interval);            
        }

        /// <summary>
        /// Cancella la registrazione dell'attività in background per il controllo periodico
        /// delle password
        /// </summary>
        private void UnregisterCheckPasswordBackgroundTask()
        {
            UnregisterBackgroundTask(App.CheckPasswordBackgroundTaskName);
        }

        /// <summary>
        /// Cancella la registrazione dell'attività in background per la pianificazione
        /// dei backup
        /// </summary>
        private void UnregisterBackupBackgroundTask()
        {
            UnregisterBackgroundTask(App.BackupBackgroundTaskName);
        }
        #endregion

        private async void PickerBackupFrequency_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool registra = true;

            if (IsLoading)
                return;

            uint oldValue = App.Settings.BackupFrequency;
            await SaveSettings();
            stackDateTimeBackupFrequency.IsVisible = (App.Settings.BackupFrequency > 0);
            if (oldValue == 0 && App.Settings.BackupFrequency > 0)
            {
                DateTime precStartDate = datePickerBackupFrequency.Date;
                datePickerBackupFrequency.Date = DateTime.Now.Date.AddDays(1.0);    // Domani.

                // Se non è cambiata l'ora non scatta l'evento sul date picker
                registra = precStartDate.Equals(datePickerBackupFrequency.Date);
            }
            
            if (registra)
            {
                UnregisterBackupBackgroundTask();
                // Nuova registrazione del task
                if (App.Settings.BackupFrequency > 0)
                    await RegisterBackupBackgroundTask();
            }            
        }

        private async void PickerBackupHistory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IsLoading)
                return;
            await SaveSettings();
        }

        private async void DatePickerPeriodicallyCheckPwd_DateSelected(object sender, DateChangedEventArgs e)
        {
            if (IsLoading)
                return;

            if (datePickerPeriodicallyCheckPwd.Date < DateTime.Now.Date)
            {
                await DisplayAlert(App.Title, AppResources.errDatePriorToday, "Ok");
                return;
            }
            if (datePickerPeriodicallyCheckPwd.Date == DateTime.Now.Date)
            {
                if (timePickerPeriodicallyCheckPwd.Time <= DateTime.Now.TimeOfDay)
                {
                    await DisplayAlert(App.Title, AppResources.errTimePriorNow, "Ok");
                    return;
                }
                else if (Device.RuntimePlatform == Device.UWP && timePickerPeriodicallyCheckPwd.Time <= DateTime.Now.AddMinutes(15).TimeOfDay)
                {
                    await DisplayAlert(App.Title, AppResources.errTimePrior15MinuteFromNow, "Ok");
                    return;
                }
            }
            await SaveSettings();

            // Cancella la registrazione del task
            UnregisterCheckPasswordBackgroundTask();
            // Registrazione del task
            await RegisterCheckPasswordBackgroundTask();
        }

        private async void DatePickerBackupFrequency_DateSelected(object sender, DateChangedEventArgs e)
        {
            if (IsLoading)
                return;

            if (datePickerBackupFrequency.Date < DateTime.Now.Date)
            {
                await DisplayAlert(App.Title, AppResources.errDatePriorToday, "Ok");
                return;
            }
            if (datePickerBackupFrequency.Date == DateTime.Now.Date)
            {
                if (timePickerBackupFrequency.Time <= DateTime.Now.TimeOfDay)
                {
                    await DisplayAlert(App.Title, AppResources.errTimePriorNow, "Ok");
                    return;
                } 
                else if (Device.RuntimePlatform == Device.UWP && timePickerBackupFrequency.Time <= DateTime.Now.AddMinutes(15).TimeOfDay)
                {
                    await DisplayAlert(App.Title, AppResources.errTimePrior15MinuteFromNow, "Ok");
                    return;
                }
            }
            await SaveSettings();

            // Cancella la registrazione del task
            UnregisterBackupBackgroundTask();
            // Registrazione del task
            await RegisterBackupBackgroundTask();
        }

        private async void TimePickerPeriodicallyCheckPwd_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (IsLoading || !e.PropertyName.Equals(TimePicker.TimeProperty.PropertyName))
                return;

            if (datePickerPeriodicallyCheckPwd.Date < DateTime.Now.Date)
            {
                await DisplayAlert(App.Title, AppResources.errDatePriorToday, "Ok");
                return;
            }
            else if (datePickerPeriodicallyCheckPwd.Date == DateTime.Now.Date)
            {
                if (timePickerPeriodicallyCheckPwd.Time <= DateTime.Now.TimeOfDay)
                {
                    await DisplayAlert(App.Title, AppResources.errTimePriorNow, "Ok");
                    return;
                }
                else if (Device.RuntimePlatform == Device.UWP && timePickerPeriodicallyCheckPwd.Time <= DateTime.Now.AddMinutes(15).TimeOfDay)
                {
                    await DisplayAlert(App.Title, AppResources.errTimePrior15MinuteFromNow, "Ok");
                    return;
                }
            }
            
            await SaveSettings();

            // Cancella la registrazione del task
            UnregisterCheckPasswordBackgroundTask();
            // Registrazione del task
            await RegisterCheckPasswordBackgroundTask();
        }

        private async void TimePickerBackupFrequency_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (IsLoading || !e.PropertyName.Equals(TimePicker.TimeProperty.PropertyName))
                return;
            if (datePickerBackupFrequency.Date < DateTime.Now.Date)
            {
                await DisplayAlert(App.Title, AppResources.errDatePriorToday, "Ok");
                return;
            }
            else if (datePickerBackupFrequency.Date == DateTime.Now.Date)
            {
                if (timePickerBackupFrequency.Time <= DateTime.Now.TimeOfDay)
                {
                    await DisplayAlert(App.Title, AppResources.errTimePriorNow, "Ok");
                    return;
                }
                else if (Device.RuntimePlatform == Device.UWP && timePickerBackupFrequency.Time <= DateTime.Now.AddMinutes(15).TimeOfDay)
                {
                    await DisplayAlert(App.Title, AppResources.errTimePrior15MinuteFromNow, "Ok");
                    return;
                }                
            }
            await SaveSettings();

            // Cancella la registrazione del task
            UnregisterBackupBackgroundTask();
            // Registrazione del task
            await RegisterBackupBackgroundTask();
        }

        /// <summary>
        /// Classe che descrive un intervallo di tempo
        /// </summary>
        internal class TimeDef
        {           
            public uint Id { get; set; }
            public TimeKind Kind { get; set; }
            public uint Time { get; set; }            

            public TimeDef()
            {
                Id = 1;
                Kind = TimeKind.Minute;
                Time = 0;
            }

            public uint GetMinutesFrom(DateTime date)
            {
                DateTime ret = date;
                switch(Kind)
                {
                    case TimeKind.Day:
                        ret = date.AddDays(Time);
                        break;
                    case TimeKind.Hour:
                        ret = date.AddHours(Time);
                        break;
                    case TimeKind.Minute:
                        ret = date.AddMinutes(Time);
                        break;
                    case TimeKind.Month:
                        ret = date.AddMonths((int)Time);
                        break;
                    case TimeKind.Year:
                        ret = date.AddYears((int)Time);
                        break;
                    default:
                        Debug.WriteLine(string.Format("TimeDef: tipo di intervallo non contemplato {0}", Kind.ToString()));
                        break;
                }
                return (uint)Math.Round((ret - date).TotalMinutes, 0);
            }
        }
    }
}
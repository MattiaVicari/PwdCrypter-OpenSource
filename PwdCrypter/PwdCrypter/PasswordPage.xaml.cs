using Newtonsoft.Json.Linq;
using PwdCrypter.Controls;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    /// <summary>
    /// Modalità di gestione della password
    /// </summary>
    public enum PwdDataMode { EditMode, InsertMode, ViewMode, ReadOnlyMode }


    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PasswordPage : ContentPage
    {
        // Caratteri utilizzati per la generazione della password
        private static readonly string PwdCharacters = "abcdefghijklmnopqrstuvwxyz_-?òàùèé@#ç°§*.!$£&%ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private Dictionary<int, AccountType> AccountTypeOptions = null;
        private Dictionary<int, UsernameLoginOption> LoginOptions = null;
        private PasswordPageData PageData = null;

        // Salvati da parte per poterli togliere e mettere all'occorrenza
        private ToolbarItem ToolbarItemSend = null;
        private ToolbarItem ToolbarItemSave = null;
        private ToolbarItem ToolbarItemEdit = null;
        private ToolbarItem ToolbarItemCancel = null;
        private ToolbarItem ToolbarItemDelete = null;

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        private PwdDataMode PageDataMode
        {
            get
            {
                return PageData.DataMode;
            }
            set
            {
                PageData.DataMode = value;
                ShowControls(value);
                SetupToolbar(value);
            }
        }

        public PasswordPage()
        {
            InitializeComponent();

            MessagingCenter.Subscribe<Type>(this, "DetailPageChanging", (pageType) => OnDetailPageChanging(pageType));

            PrepareToolbar();
            LoadOptions();
        }

        private void OnDetailPageChanging(Type pageType)
        {
            if (PageData == null || pageType != typeof(PasswordListPage))
                return;

            if (PageDataMode == PwdDataMode.EditMode)
            {
                PageData.PwdInfo.RestoreState();
                PageData.DataMode = PwdDataMode.ViewMode;
            }
        }

        private void PrepareToolbar()
        {
            // Creo i pulsanti ma non li aggiungo subito
            ToolbarItemSend = new ToolbarItem(AppResources.txtSend, "Assets/send.png", () => OnSendPassword());
            ToolbarItemSave = new ToolbarItem(AppResources.txtSave, "Assets/save.png", () => OnSavePassword());
            ToolbarItemEdit = new ToolbarItem(AppResources.txtEdit, "Assets/edit.png", () => OnEditPassword());
            ToolbarItemCancel = new ToolbarItem(AppResources.txtCancel, "Assets/cancel.png", async () => await OnUndo());
            ToolbarItemDelete = new ToolbarItem(AppResources.txtDelete, "Assets/delete.png", async () => await OnDeletePassword());
        }

        private void OnSendPassword()
        {
            if (!App.IsExtensionEnabled)
                return;

            JObject data = new JObject
            {
                { "account_username", PageData.PwdInfo.AccountUsername },
                { "username", PageData.PwdInfo.Username },
                { "password", PageData.PwdInfo.Password },
                { "email", PageData.PwdInfo.Email }
            };

            App.SendDataToPwdService(data);

            // Notifica all'utente
            App.SendLigthNotification(frameNotification,
                                      lblNotification,
                                      AppResources.msgPwdSent);

            if (App.Settings.BrowserExtensionHelp)
                App.SendToastNotification(App.Title, AppResources.msgPwdExtensionHelp);
        }

        private async Task OnDeletePassword()
        {
            if (PageData == null || PageData.DataMode != PwdDataMode.ViewMode)
                return;

            // Chiede conferma
            if (!await DisplayAlert(App.Title, AppResources.msgQuestionPasswordDelete, AppResources.btnYes, AppResources.btnNo))
                return;

            WaitPage waitPage = await Utility.StartWait(this);
            try
            {
                // Rimuove la password con i suoi allegati
                await App.PwdManager.RemovePassword(PageData.PwdInfo);
                // Salva le modifiche
                await App.PwdManager.SavePasswordFile();
            }
            catch(Exception e)
            {
                await DisplayAlert(App.Title, AppResources.errPasswordDelete + " " + e.Message, "OK");                
            }
            finally
            {
                await Utility.StopWait(waitPage);
            }

            // Richiede l'aggiornamento della lista delle password
            MessagingCenter.Send<Page>(this, "UpdatePasswordList");

            // Torna indietro
            if (PageData.ModalMode)
                await Navigation.PopModalAsync(true);
            else
                await Navigation.PopAsync(true);
        }

        private async Task OnUndo()
        {
            if (PageData == null)
                return;

            if (PageData.PwdInfo.CurrentStateSaved)
            {
                if (await DisplayAlert(App.Title, AppResources.msgQuestionUndo, AppResources.btnYes, AppResources.btnNo))
                {
                    Undo();
                    PageDataMode = PwdDataMode.ViewMode;
                }
            }
        }

        private async void OnSavePassword()
        {
            if (PageData == null)
                return;

            WaitPage waitPage = await Utility.StartWait(this);
            try
            {
                // Inserimento nuova password
                if (PageDataMode == PwdDataMode.InsertMode)
                    await SaveNewPassword();
                else
                    await SavePasswordChange();
            }
            catch(Exception e)
            {
                await DisplayAlert(App.Title, AppResources.errWritePassword + " " + e.Message, "Ok");
            }
            finally
            {
                await Utility.StopWait(waitPage);
            }
        }

        private async Task SaveNewPassword()
        {
            // Aggiunge la nuova password nella lista
            App.PwdManager.AddPassword(PageData.PwdInfo);

            // Salva le modifiche al file
            await App.PwdManager.SavePasswordFile();

            // Mostra la notifica per indicare che la password è stata aggiunta
            App.SendLigthNotification(frameNotification,
                                      lblNotification,
                                      AppResources.msgPwdInserted);

            // Abilita la modalità di visualizzazione
            PageDataMode = PwdDataMode.ViewMode;

            // Richiede l'aggiornamento della lista delle password
            MessagingCenter.Send<Page>(this, "UpdatePasswordList");
        }

        private async Task SavePasswordChange()
        {
            if (PageData.PwdInfo != null && PageData.PwdInfo.Password != null)
            {
                if (PageData.CurrentPwdHash.CompareTo(EncDecHelper.SHA256(PageData.PwdInfo.Password)) != 0)
                    PageData.PwdInfo.LastPwdChangeDateTime = DateTime.Now;
            }

            // Salva le modifiche
            await App.PwdManager.SavePasswordFile(false);

            // Mostra la notifica per indicare che le modifiche sono state salvate
            App.SendLigthNotification(frameNotification,
                                      lblNotification,
                                      AppResources.msgChangeSaved);

            // Ritorna alla situazione di visualizzazione
            PageDataMode = PwdDataMode.ViewMode;

            // Elimina i dati immagazzinati per la funzione Undo
            if (PageData.PwdInfo.CurrentStateSaved)
                PageData.PwdInfo.DiscardChanges();

            // Richiede l'aggiornamento della lista delle password
            MessagingCenter.Send<Page>(this, "UpdatePasswordList");
        }

        private void OnEditPassword()
        {
            if (PageData == null)
                return;

            PageDataMode = PwdDataMode.EditMode;
            if (!PageData.PwdInfo.CurrentStateSaved)
                PageData.PwdInfo.SaveCurrentState();
        }

        private void LoadOptions()
        {
            AccountTypeOptions = new Dictionary<int, AccountType>();
            foreach (AccountType accountType in Utility.EnumHelper.GetValues<AccountType>())
            {
                string desc = Utility.EnumHelper.GetDescription(accountType);
                AccountTypeOptions.Add((int)(accountType), accountType);
                pickerAccountType.Items.Add(desc);
            }
            pickerAccountType.SelectedIndex = 0;

            LoginOptions = new Dictionary<int, UsernameLoginOption>();
            foreach (UsernameLoginOption usernameLogin in Utility.EnumHelper.GetValues<UsernameLoginOption>())
            {
                string desc = Utility.EnumHelper.GetDescription(usernameLogin);
                LoginOptions.Add((int)(usernameLogin), usernameLogin);
                pickerLoginOption.Items.Add(desc);
            }
            pickerLoginOption.SelectedIndex = 0;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext != null && BindingContext is PasswordPageData settings)
            {
                LoadData(settings);
                SetupToolbar(settings.DataMode);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (PageData != null && (PageData.DataMode == PwdDataMode.InsertMode || PageData.DataMode == PwdDataMode.EditMode))
            {
                // Chiede conferma se si vogliono abbandonare le modifiche
                Device.BeginInvokeOnMainThread(async () =>
                {
                    string btnIgnore = AppResources.btnIgnoreChange;
                    string btnContinue = AppResources.btnContinueChange;
                    if (Device.RuntimePlatform == Device.Android)
                    {
                        // Testi più brevi per Android
                        btnIgnore = AppResources.btnIgnore;
                        btnContinue = AppResources.btnContinue;
                    }
                    if (await DisplayAlert(App.Title, AppResources.msgQuestionAbandon, btnIgnore, btnContinue))
                    {
                        // Abbandona le modifiche
                        Undo();

                        base.OnBackButtonPressed();
                        if (PageData.ModalMode)
                            await Navigation.PopModalAsync(true);
                        else
                            await Navigation.PopAsync(true);
                    }
                });

                return true;
            }
            
            return base.OnBackButtonPressed();
        }

        private void Undo()
        {
            PageData.PwdInfo.RestoreState();
            pickerAccountType.SelectedIndex = AccountTypeOptions.First(item => item.Value == PageData.PwdInfo.AccountOption).Key;
        }

        private void SetupToolbar(PwdDataMode mode)
        {
            if (PageData.ModalMode)
                return;

            ToolbarItems.Clear();
            if (mode == PwdDataMode.EditMode || mode == PwdDataMode.InsertMode)
            {
                ToolbarItems.Add(ToolbarItemSave);
                if (mode == PwdDataMode.EditMode)
                    ToolbarItems.Add(ToolbarItemCancel);
            }
            else if (mode != PwdDataMode.ReadOnlyMode)
            {
                if (App.IsExtensionEnabled)
                    ToolbarItems.Add(ToolbarItemSend);
                ToolbarItems.Add(ToolbarItemEdit);
                ToolbarItems.Add(ToolbarItemDelete);
            }
        }

        private void OnCopyData(object sender, EventArgs e, Controls.FeatureInfo featureInfo)
        {
            if (sender is Controls.FormEntry view)
            {
                view.OnFeature(featureInfo);
                SendDataToClipboard(view.Text);
            }
        }

        private void SendDataToClipboard(string text)
        {
            Plugin.Clipboard.CrossClipboard.Current.SetText(text);
            App.SendToastNotification(App.Title, AppResources.msgDataCopyToClipboard);
            App.SendLigthNotification(frameNotification,
                                      lblNotification,
                                      AppResources.msgDataCopyToClipboard);
        }

        private void LoadData(PasswordPageData settings)
        {
            PageData = settings;
            if (PageData.PwdInfo != null && PageData.PwdInfo.Password != null)
                PageData.CurrentPwdHash = EncDecHelper.SHA256(PageData.PwdInfo.Password);
            else
                PageData.CurrentPwdHash = string.Empty;

            pickerAccountType.SelectedIndex = AccountTypeOptions.First(item => item.Value == PageData.PwdInfo.AccountOption).Key;
            pickerLoginOption.SelectedIndex = LoginOptions.First(item => item.Value == PageData.PwdInfo.LoginOption).Key;

            ShowControls(PageData.DataMode);
        }

        private void ShowControls(PwdDataMode mode)
        {
            // Aggiunge le funzioni per la copia e blocca le caselle in sola lettura
            FormEntry[] entryCanCopy = {
                entryDescription,
                entryUsername,
                entryPassword,
                entryEmail
            };

            bool edit = (mode == PwdDataMode.EditMode || mode == PwdDataMode.InsertMode);
            foreach (FormEntry entry in entryCanCopy)
            {
                entry.IsEntryEnabled = edit;
                entry.IsVisible = (edit || entry.Text?.Trim().Length > 0);
                if (!entry.FeatureExists("clipboard-check"))
                    entry.AddFeature("clipboard-check", "clipboard-check", OnCopyData);
                entry.SetFeatureVisible("clipboard-check", !edit);
            }
            pickerAccountType.IsEnabled = edit;
            pickerLoginOption.IsEnabled = edit;

            // Aggiunge la funzione per la generazione della password
            if (!entryPassword.FeatureExists("key"))
                entryPassword.AddFeature("key", "key", OnGeneratePassword);
            entryPassword.SetFeatureVisible("key", edit);

            gridLoginOption.IsVisible = App.IsExtensionEnabled;

            switch (mode)
            {
                case PwdDataMode.EditMode:
                    btnShowDetail.Text = AppResources.btnEditDetail;
                    break;
                case PwdDataMode.InsertMode:
                    btnShowDetail.Text = AppResources.btnInsertDetail;
                    break;
                case PwdDataMode.ViewMode:
                default:
                    btnShowDetail.Text = AppResources.btnDetail;
                    break;
            }
        }

        private void OnGeneratePassword(object sender, EventArgs e, FeatureInfo featureInfo)
        {
            if (sender is FormEntry view)
            {
                view.OnFeature(featureInfo);
                Device.BeginInvokeOnMainThread(async () => await GeneratePassword(view));
            }
        }

        private async Task GeneratePassword(FormEntry view)
        {
            if (!await DisplayAlert(App.Title, AppResources.msgQuestionGenPassword, AppResources.btnYes, AppResources.btnNo))
                return;

            int maxValue = PwdCharacters.Length;
            Random genRandom = new Random();
            string pwd = "";
            for (int i = 0; i < 12; i++)
            {
                int pos = genRandom.Next(1, maxValue);
                pwd += PwdCharacters.Substring(pos - 1, 1);
            }
            view.Text = pwd;
        }

        private void BtnShowDetail_Clicked(object sender, EventArgs e)
        {
            if (PageData.ModalMode)
            {
                Navigation.PushModalAsync(new PasswordDetailPage
                {
                    BindingContext = PageData
                }, true);
            }
            else
            {
                Navigation.PushAsync(new PasswordDetailPage
                {
                    BindingContext = PageData
                }, true);
            }
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;
            View[] views = {
                entryDescription,
                gridAccountType,
                gridLoginOption,
                entryUsername,
                entryPassword,
                entryEmail,
                btnShowDetail
            };
            LayoutOptions layoutOptions;
            double widthRequest;
            if (Width > App.ViewStepMaxWidth)
            {
                widthRequest = App.ViewStepMaxWidth - 140;
                layoutOptions = LayoutOptions.Center;
            }
            else
            {
                widthRequest = AutoWidth;
                layoutOptions = LayoutOptions.FillAndExpand;
            }

            foreach (View view in views)
            {
                view.HorizontalOptions = layoutOptions;
                view.WidthRequest = widthRequest;
            }
        }

        private void PickerAccountType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PageData != null && PageData.PwdInfo != null)
                PageData.PwdInfo.AccountOption = AccountTypeOptions[pickerAccountType.SelectedIndex];
        }

        private void PickerLoginOption_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PageData != null && PageData.PwdInfo != null)
                PageData.PwdInfo.LoginOption = LoginOptions[pickerLoginOption.SelectedIndex];
        }
    }

    /// <summary>
    /// Classe che racchiude dati e impostazioni delle pagine di gestione delle
    /// password
    /// </summary>
    public class PasswordPageData
    {
        public PwdListItem PwdInfo { get; set; }
        public PwdDataMode DataMode { get; set; }
        public string CurrentPwdHash { get; set; }
        public bool ModalMode { get; set; }

        public PasswordPageData()
        {
            CurrentPwdHash = "";
            ModalMode = false;
            DataMode = PwdDataMode.ViewMode;
        }
    }
}
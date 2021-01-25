using PwdCrypter.Controls;
using PwdCrypter.Extensions.ResxLocalization.Resx;
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
    public partial class PasswordDetailPage : ContentPage
    {
        // Salvati da parte per poterli togliere e mettere all'occorrenza
        private ToolbarItem ToolbarItemSave = null;
        private ToolbarItem ToolbarItemEdit = null;
        private ToolbarItem ToolbarItemCancel = null;

        // Opzioni di selezione per alcune property dell'account
        private readonly Dictionary<string, PropertyOptions> PropertiesOptions = null;
        // Lista dei file allegati alla password
        private List<AttachmentListItem> AttachmentListItems = null;

        private event EventHandler OnPasswordSaved = null;

        private bool DataLoaded = false;

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        private PasswordPageData PageData = null;

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

        private bool pageIsBusy = false;
        public bool AttachmentsIsBusy
        {
            get { return pageIsBusy; }
            set => SetAttachmentsIsBusy(value);
        }

        private void SetAttachmentsIsBusy(bool busy)
        {
            if (pageIsBusy == busy)
                return;

            pageIsBusy = busy;

            spinnerAttachments.IsRunning = busy;
            spinnerAttachments.IsEnabled = busy;
            spinnerAttachments.IsVisible = busy;
        }

        public PasswordDetailPage()
        {
            InitializeComponent();
            PropertiesOptions = new Dictionary<string, PropertyOptions>();

            PrepareToolbar();

            OnPasswordSaved += PasswordDetailPage_OnPasswordSaved;
        }

        private void PasswordDetailPage_OnPasswordSaved(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(AttachmentPage))
            {
                // Mostra la notifica per indicare che le modifiche sono state salvate
                App.SendLigthNotification(frameNotification,
                                          lblNotification,
                                          PageDataMode == PwdDataMode.InsertMode ? AppResources.msgPwdInserted : AppResources.msgChangeSaved);

                // Modalità visualizzazione
                PageDataMode = PwdDataMode.ViewMode;

                // Ricarica la lista degli allegati
                LoadAttachments();
            }
        }

        private void PrepareToolbar()
        {
            // Creo i pulsanti ma non li aggiungo subito
            ToolbarItemSave = new ToolbarItem(AppResources.txtSave, "Assets/save.png", () => OnSavePassword());
            ToolbarItemEdit = new ToolbarItem(AppResources.txtEdit, "Assets/edit.png", () => OnEditPassword());
            ToolbarItemCancel = new ToolbarItem(AppResources.txtCancel, "Assets/cancel.png", async () => await OnUndo());
        }

        private async Task OnUndo()
        {
            if (PageData == null)
                return;

            if (PageData.PwdInfo.CurrentStateSaved)
            {
                if (await DisplayAlert(App.Title, AppResources.msgQuestionPwdUndo, AppResources.btnYes, AppResources.btnNo))
                {
                    PageData.PwdInfo.RestoreState();
                    PageDataMode = PwdDataMode.ViewMode;
                }
            }
        }

        private void SetupToolbar(PwdDataMode mode)
        {
            ToolbarItems.Clear();
            if (mode == PwdDataMode.EditMode || mode == PwdDataMode.InsertMode)
            {
                ToolbarItems.Add(ToolbarItemSave);
                if (mode == PwdDataMode.EditMode)
                    ToolbarItems.Add(ToolbarItemCancel);
            }
            else if (mode != PwdDataMode.ReadOnlyMode)
                ToolbarItems.Add(ToolbarItemEdit);
        }

        private void LoadOptions()
        {
            PropertiesOptions.Clear();

            if (PageData.PwdInfo.AccountOption == AccountType.CreditDebitCard)
            {
                PropertyOptions creditCardProviders = new PropertyOptions();
                creditCardProviders.AddOption(0, "American Express");
                creditCardProviders.AddOption(1, "Diner's Club");
                creditCardProviders.AddOption(2, "Discover");
                creditCardProviders.AddOption(3, "Mastercard");
                creditCardProviders.AddOption(4, "Visa International");
                creditCardProviders.AddOption(5, AppResources.txtOther, true);

                creditCardProviders.PropertyName = "Provider";
                PropertiesOptions.Add(creditCardProviders.PropertyName, creditCardProviders);
            }

            if (PageData.PwdInfo.AccountOption == AccountType.ECommerce)
            {
                PropertyOptions ecommerceProviders = new PropertyOptions();
                ecommerceProviders.AddOption(0, "Alibaba");
                ecommerceProviders.AddOption(1, "Amazon");
                ecommerceProviders.AddOption(2, "Apple");
                ecommerceProviders.AddOption(3, "Booking.com");
                ecommerceProviders.AddOption(4, "eBay");
                ecommerceProviders.AddOption(5, "eDreams");
                ecommerceProviders.AddOption(6, "Google");
                ecommerceProviders.AddOption(7, "Groupon");
                ecommerceProviders.AddOption(8, "Microsoft");
                ecommerceProviders.AddOption(9, "Netflix");
                ecommerceProviders.AddOption(10, "Steam");
                ecommerceProviders.AddOption(11, "Telepass");
                ecommerceProviders.AddOption(12, "ToysRus");
                ecommerceProviders.AddOption(13, "Trivago");
                ecommerceProviders.AddOption(14, "Vodafone");
                ecommerceProviders.AddOption(15, "Walmart");
                ecommerceProviders.AddOption(16, "Yoox");
                ecommerceProviders.AddOption(17, "Zalando");
                ecommerceProviders.AddOption(18, AppResources.txtOther, true);

                ecommerceProviders.PropertyName = "Provider";
                PropertiesOptions.Add(ecommerceProviders.PropertyName, ecommerceProviders);
            }

            if (PageData.PwdInfo.AccountOption == AccountType.Device ||
                PageData.PwdInfo.AccountOption == AccountType.DeviceHome ||
                PageData.PwdInfo.AccountOption == AccountType.DeviceWork ||
                PageData.PwdInfo.AccountOption == AccountType.App ||
                PageData.PwdInfo.AccountOption == AccountType.Software)
            {
                PropertyOptions os = new PropertyOptions();
                os.AddOption(0, "Android");
                os.AddOption(1, "iOS");
                os.AddOption(2, "Linux");
                os.AddOption(3, "Macintosh");
                os.AddOption(4, "Unix");
                os.AddOption(5, "Windows");
                os.AddOption(6, AppResources.txtOther, true);

                os.PropertyName = "OS";
                PropertiesOptions.Add(os.PropertyName, os);
            }
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            AttachmentsIsBusy = true;
            await App.CurrentApp.CheckPurchasedItem(true, () =>
            {
                if (BindingContext != null && BindingContext is PasswordPageData settings)
                {
                    LoadData(settings);
                    ShowControls(settings.DataMode);
                    SetupToolbar(settings.DataMode);
                }
                AttachmentsIsBusy = false;

                // Non è chiamata in fase di navigazione con Push
                ContentPage_SizeChanged(this, null);
                return true;
            });
        }

        private FormEntry AddItemEntry(string title, string placeHolder, string propertyName)
        {
            gridMain.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            FormEntry entry = new FormEntry
            {
                Title = title,
                Placeholder = placeHolder,
                BindingContext = PageData.PwdInfo
            };
            entry.SetBinding(FormEntry.TextProperty, propertyName, BindingMode.TwoWay);

            gridMain.Children.Add(entry, 0, gridMain.RowDefinitions.Count - 1);
            return entry;
        }

        private Grid AddItemSwitch(string title, string propertyName)
        {
            gridMain.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            Grid gridSwitch = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Star
                    }
                }
            };

            // Label
            Label label = new Label
            {
                Text = title,
                TextColor = Color.Black,
                VerticalOptions = LayoutOptions.Center
            };
            if (Device.RuntimePlatform == Device.Android)
            {
                label.VerticalOptions = LayoutOptions.Center;
                label.Margin = new Thickness(0);
                label.WidthRequest = 150;
                label.LineBreakMode = LineBreakMode.WordWrap;
            }
            else if (Device.RuntimePlatform == Device.UWP)
            {
                label.VerticalOptions = LayoutOptions.End;
                label.Margin = new Thickness(0, 0, 0, 8);
            }
            gridSwitch.Children.Add(label, 0, 0);

            // Switch
            Xamarin.Forms.Switch flag = new Xamarin.Forms.Switch
            {
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };
            flag.BindingContext = PageData.PwdInfo;
            flag.SetBinding(Xamarin.Forms.Switch.IsToggledProperty, propertyName, BindingMode.TwoWay);
            gridSwitch.Children.Add(flag, 1, 0);

            gridMain.Children.Add(gridSwitch, 0, gridMain.RowDefinitions.Count - 1);
            return gridSwitch;
        }

        private Grid AddItemPicker(string title, string propertyName, PropertyOptions propertyOptions)
        {
            gridMain.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });

            Grid gridPicker = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto
                    },
                    new ColumnDefinition
                    {
                        Width = GridLength.Star
                    }
                }
            };

            // Label
            Label label = new Label
            {
                Text = title,
                TextColor = Color.Black,
                VerticalOptions = LayoutOptions.Center
            };
            if (Device.RuntimePlatform == Device.Android)
            {
                label.VerticalOptions = LayoutOptions.Center;
                label.Margin = new Thickness(0);
            }
            else if (Device.RuntimePlatform == Device.UWP)
            {
                label.VerticalOptions = LayoutOptions.End;
                label.Margin = new Thickness(0, 0, 0, 8);
            }
            gridPicker.Children.Add(label, 0, 0);

            // Picker
            Picker picker = new Picker
            {
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Center
            };
            if (Device.RuntimePlatform == Device.Android)
                picker.WidthRequest = 150;
            picker.BindingContext = propertyOptions;
            picker.SelectedIndexChanged += Picker_SelectedIndexChanged;

            if (propertyOptions != null)
            {
                int sel = -1;
                System.Text.RegularExpressions.Regex regProp = new System.Text.RegularExpressions.Regex(@"^SpecialFields\[(.*)\]$");
                System.Text.RegularExpressions.Match propMatch = regProp.Match(propertyName);
                if (propMatch.Success && propMatch.Groups.Count > 1)
                {
                    string propName = propMatch.Groups[1].Value;
                    if (int.TryParse(PageData.PwdInfo.SpecialFields[propName], out int selectedOption))
                        sel = selectedOption;
                    if (sel == -1 || !propertyOptions.OptionValues.ContainsKey(sel))
                        sel = propertyOptions.DefaultOptionID;
                }
                foreach (KeyValuePair<int, string> item in propertyOptions.OptionValues)
                {
                    picker.Items.Add(item.Value);
                    if (item.Key == sel)
                        picker.SelectedIndex = picker.Items.Count - 1;
                }
            }
            gridPicker.Children.Add(picker, 1, 0);

            gridMain.Children.Add(gridPicker, 0, gridMain.RowDefinitions.Count - 1);

            return gridPicker;
        }

        private void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker)
            {
                if (picker.BindingContext is PropertyOptions data)
                {
                    string propertyName = data.PropertyName;
                    PageData.PwdInfo.SpecialFields[propertyName] = picker.SelectedIndex.ToString();
                }
            }
        }

        private void LoadData(PasswordPageData settings)
        {
            if (DataLoaded)
            {
                LoadAttachments();
                return;
            }

            PageData = settings;
            LoadOptions();
            LoadAttachments();

            AddItemEntry(AppResources.txtNote, AppResources.txtNote, nameof(PageData.PwdInfo.Note))
                    .IsMultilineText = true;
            AddItemSwitch(AppResources.txtSkipPasswordCheck, nameof(PageData.PwdInfo.SkipCheck));

            try
            {
                if (PageData.PwdInfo.AccountOption == AccountType.Other ||
                    PageData.PwdInfo.AccountOption == AccountType.Email ||
                    PageData.PwdInfo.AccountOption == AccountType.SocialNetwork ||
                    PageData.PwdInfo.AccountOption == AccountType.ECommerce)
                {
                    AddItemEntry(AppResources.txtSecurityQuestion, AppResources.txtSecurityQuestion, nameof(PageData.PwdInfo.SecurityQuestion))
                        .TitleWidthRequest = 250;
                    AddItemEntry(AppResources.txtSecurityAnswer, AppResources.txtSecurityAnswer, nameof(PageData.PwdInfo.SecurityAnswer))
                        .TitleWidthRequest = 250;
                }

                if (PageData.PwdInfo.AccountOption == AccountType.Email)
                {
                    AddItemEntry(AppResources.txtPOPServer, AppResources.txtPOPServer, string.Format("SpecialFields[{0}]", "POPServer"));
                    AddItemEntry(AppResources.txtPOPServerPort, AppResources.txtPOPServerPort, string.Format("SpecialFields[{0}]", "POPServerPort"));
                    AddItemEntry(AppResources.txtSMTPServer, AppResources.txtSMTPServer, string.Format("SpecialFields[{0}]", "SMTPServer"));
                    AddItemEntry(AppResources.txtSMTPServerPort, AppResources.txtSMTPServerPort, string.Format("SpecialFields[{0}]", "SMTPServerPort"));
                    AddItemEntry(AppResources.txtIMAPServer, AppResources.txtIMAPServer, string.Format("SpecialFields[{0}]", "IMAPServer"));
                    AddItemEntry(AppResources.txtIMAPServerPort, AppResources.txtIMAPServerPort, string.Format("SpecialFields[{0}]", "IMAPServerPort"));
                }

                if (PageData.PwdInfo.AccountOption == AccountType.SocialNetwork ||
                    PageData.PwdInfo.AccountOption == AccountType.ECommerce)
                {
                    AddItemEntry(AppResources.txtNickname, AppResources.txtNickname, string.Format("SpecialFields[{0}]", "Nickname"));
                }

                if (PageData.PwdInfo.AccountOption == AccountType.SocialNetwork)
                {
                    AddItemEntry(AppResources.txtUrlProfile, AppResources.txtUrlProfile, string.Format("SpecialFields[{0}]", "UrlProfile"));
                }

                if (PageData.PwdInfo.AccountOption == AccountType.Bank)
                {
                    AddItemEntry(AppResources.txtUserID, AppResources.txtUserID, string.Format("SpecialFields[{0}]", "UserID"));
                    AddItemEntry(AppResources.txtContractNr, AppResources.txtContractNr, string.Format("SpecialFields[{0}]", "ContractNr"));
                    AddItemEntry(AppResources.txtTelSupport, AppResources.txtTelSupport, string.Format("SpecialFields[{0}]", "TelephoneSupport"));
                    AddItemEntry(AppResources.txtBranch, AppResources.txtBranch, string.Format("SpecialFields[{0}]", "Branch"));
                    AddItemEntry(AppResources.txtIBAN, AppResources.txtIBAN, string.Format("SpecialFields[{0}]", "IBAN"));
                    AddItemEntry(AppResources.txtBICSWIFT, AppResources.txtBICSWIFT, string.Format("SpecialFields[{0}]", "BICSWIFT"));
                    AddItemEntry(AppResources.txtFirstSecurityCode, AppResources.txtFirstSecurityCode, string.Format("SpecialFields[{0}]", "FirstSecurityCode"));
                    AddItemEntry(AppResources.txtSecondSecurityCode, AppResources.txtSecondSecurityCode, string.Format("SpecialFields[{0}]", "SecondSecurityCode"));
                }

                if (PageData.PwdInfo.AccountOption == AccountType.CreditDebitCard)
                {
                    AddItemEntry(AppResources.txtCreditCardNumber, AppResources.txtCreditCardNumber, string.Format("SpecialFields[{0}]", "CreditCardNumber"));
                    AddItemPicker(AppResources.txtProvider, string.Format("SpecialFields[{0}]", "Provider"), PropertiesOptions["Provider"]);
                    AddItemEntry(AppResources.txtPIN, AppResources.txtPIN, string.Format("SpecialFields[{0}]", "PIN"));
                    AddItemEntry(AppResources.txtCardHolder, AppResources.txtCardHolder, string.Format("SpecialFields[{0}]", "CardHolder"));
                    AddItemEntry(AppResources.txtVerificationNumber, AppResources.txtVerificationNumber, string.Format("SpecialFields[{0}]", "CardVerificationNumber"));
                    AddItemEntry(AppResources.txtExpirationDate, AppResources.txtExpirationDate, string.Format("SpecialFields[{0}]", "ExpirationDate"));
                }

                if (PageData.PwdInfo.AccountOption == AccountType.ECommerce)
                {
                    AddItemPicker(AppResources.txtProvider, string.Format("SpecialFields[{0}]", "Provider"), PropertiesOptions["Provider"]);
                }

                if (PageData.PwdInfo.AccountOption == AccountType.App ||
                    PageData.PwdInfo.AccountOption == AccountType.Software)
                {
                    AddItemEntry(AppResources.txtName, AppResources.txtName, string.Format("SpecialFields[{0}]", "Name"));
                }

                if (PageData.PwdInfo.AccountOption == AccountType.DeviceHome ||
                    PageData.PwdInfo.AccountOption == AccountType.DeviceWork ||
                    PageData.PwdInfo.AccountOption == AccountType.Device ||
                    PageData.PwdInfo.AccountOption == AccountType.App ||
                    PageData.PwdInfo.AccountOption == AccountType.Software)
                {
                    AddItemPicker(AppResources.txtOS, string.Format("SpecialFields[{0}]", "OS"), PropertiesOptions["OS"]);
                    AddItemEntry(AppResources.txtVersionDesc, AppResources.txtVersionDesc, string.Format("SpecialFields[{0}]", "Version"));
                }

                if (PageData.PwdInfo.AccountOption == AccountType.App ||
                    PageData.PwdInfo.AccountOption == AccountType.Software)
                {
                    AddItemEntry(AppResources.txtSupport, AppResources.txtSupport, string.Format("SpecialFields[{0}]", "Support"));
                    AddItemEntry(AppResources.txtManual, AppResources.txtManual, string.Format("SpecialFields[{0}]", "Manual"));
                    AddItemEntry(AppResources.txtWebsiteDesc, AppResources.txtWebsiteDesc, string.Format("SpecialFields[{0}]", "Website"));
                }

                if (PageData.PwdInfo.AccountOption == AccountType.Institutional)
                {
                    AddItemEntry(AppResources.txtName, AppResources.txtName, string.Format("SpecialFields[{0}]", "Name"));
                    AddItemEntry(AppResources.txtUserID, AppResources.txtUserID, string.Format("SpecialFields[{0}]", "UserID"));
                    AddItemEntry(AppResources.txtSecurityCode, AppResources.txtSecurityCode, string.Format("SpecialFields[{0}]", "SecurityCode"));
                    AddItemEntry(AppResources.txtWebsiteDesc, AppResources.txtWebsiteDesc, string.Format("SpecialFields[{0}]", "Website"));
                    AddItemEntry(AppResources.txtSupport, AppResources.txtSupport, string.Format("SpecialFields[{0}]", "Support"));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error occurred during loading option " + ex.Message);
            }
            finally
            {
                DataLoaded = true;
            }
        }

        private void LoadAttachments()
        {
            listViewAttachments.ItemsSource = null;
            AttachmentListItems = new List<AttachmentListItem>();
            foreach (PwdAttachment attachment in PageData.PwdInfo.Attachments)
            {
                AttachmentListItems.Add(new AttachmentListItem
                {
                    Attachment = attachment,
                    Name = attachment.Name
                });
            }
            listViewAttachments.ItemsSource = AttachmentListItems;
            listViewAttachments.HeightRequest = listViewAttachments.RowHeight * AttachmentListItems.Count;
        }

        private void ShowControls(PwdDataMode mode)
        {
            int countCtrlsVisible = 0;

            bool edit = (mode == PwdDataMode.EditMode || mode == PwdDataMode.InsertMode);
            foreach (View view in gridMain.Children)
            {
                if (view is FormEntry entry)
                {
                    entry.IsEntryEnabled = edit;
                    entry.IsVisible = (edit || entry.Text?.Trim().Length > 0);
                    countCtrlsVisible += entry.IsVisible ? 1 : 0;
                    if (!entry.FeatureExists("clipboard-check"))
                        entry.AddFeature("clipboard-check", "clipboard-check", OnCopyData);
                    entry.SetFeatureVisible("clipboard-check", !edit);
                }
                else if (view is Grid grid)
                {
                    foreach (View child in grid.Children)
                    {
                        if (child is Picker || child is Xamarin.Forms.Switch)
                        {
                            child.IsEnabled = edit;
                            countCtrlsVisible++;
                        }
                    }
                }
            }

            listViewAttachments.ItemsSource = null;

            // Elimina l'elemento fittizio per caricare un allegato
            if (!edit && AttachmentListItems.Count > 0)
            {
                try
                {
                    AttachmentListItem fakeAttachment = AttachmentListItems.First(item => item.Attachment == null);
                    AttachmentListItems.Remove(fakeAttachment);
                }
                catch (Exception Ex)
                {
                    Debug.WriteLine("Element for \"Add attachment\" not found. Exception: " + Ex.Message);
                }
                
            }

            if (App.IsAttachmentFeatureAvailable)
            {
                // Visualizza i controlli per gli allegati in base alla modalità
                foreach (AttachmentListItem attachment in AttachmentListItems)
                {
                    attachment.Download = !edit && (attachment.Attachment != null);
                    attachment.Upload = edit;
                    attachment.Remove = edit && (attachment.Attachment != null);
                    countCtrlsVisible++;
                }
                // Elemento fittizio per caricare un file allegato
                if (edit)
                {
                    AttachmentListItem fakeAttachment = AttachmentListItems.Find(item => item.Attachment == null);
                    if (fakeAttachment == null)
                    {
                        AttachmentListItems.Add(new AttachmentListItem
                        {
                            Name = AppResources.txtAddAttachment,
                            Remove = false,
                            Download = false,
                            Upload = true
                        });
                        countCtrlsVisible++;
                    }
                }

                listViewAttachments.ItemsSource = AttachmentListItems;
                listViewAttachments.IsVisible = (AttachmentListItems.Count > 0);
                listViewAttachments.HeightRequest = listViewAttachments.RowHeight * AttachmentListItems.Count;
                lblTitleAttachment.IsVisible = listViewAttachments.IsVisible;
            }
            else
            {
                // Funzione non disponibile
                listViewAttachments.IsVisible = false;
                lblTitleAttachment.IsVisible = false;
                btnPLUSFeature.IsVisible = true;
            }

            lblNoDetails.IsVisible = (countCtrlsVisible == 0);
        }

        private void OnCopyData(object sender, EventArgs e, FeatureInfo featureInfo)
        {
            if (sender is FormEntry view)
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

        private void OnEditPassword()
        {
            if (PageData == null)
                return;

            PageDataMode = PwdDataMode.EditMode;
            if (!PageData.PwdInfo.CurrentStateSaved)
                PageData.PwdInfo.SaveCurrentState();
        }

        private void ContentPage_SizeChanged(object sender, EventArgs e)
        {
            const double AutoWidth = -1;
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

            foreach (View view in gridMain.Children)
            {
                if (view is FormEntry || view is Grid)
                {
                    view.HorizontalOptions = layoutOptions;
                    view.WidthRequest = widthRequest;
                }
            }

            listViewAttachments.HorizontalOptions = layoutOptions;
            listViewAttachments.WidthRequest = widthRequest;
        }

        private async void ListViewAttachments_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is AttachmentListItem item)
            {
                if (PageDataMode == PwdDataMode.EditMode || PageDataMode == PwdDataMode.InsertMode)
                {
                    // Apre la pagina per la gestione dell'allegato
                    Page page = new AttachmentPage
                    {
                        BindingContext = new AttachmentPageData
                        {
                            DataMode = PageDataMode,
                            PwdInfo = PageData.PwdInfo,
                            AttachmentId = item.Attachment != null ? item.Attachment.Id : -1,
                            CurrentPwdHash = PageData.CurrentPwdHash,
                            OnPasswordSaved = OnPasswordSaved
                        }
                    };
                    if (PageData.ModalMode)
                        await Navigation.PushModalAsync(page, true);
                    else
                        await Navigation.PushAsync(page, true);
                }
                else if (PageDataMode == PwdDataMode.ViewMode || PageDataMode == PwdDataMode.ReadOnlyMode)
                {
                    try
                    {
                        // Apre l'allegato
                        await App.PwdManager.GetAttachment(item.Attachment);
                    }
                    catch(Exception ex)
                    {
                        await DisplayAlert(App.Title, AppResources.errGetAttachmentFile + " " + ex.Message, "OK");
                    }
                }

                listViewAttachments.SelectedItem = -1;
            }
        }

        private void BtnPLUSFeature_Clicked(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new PurchasePLUSPage { IsModal = true }, true);
        }
    }

    /// <summary>
    /// Classe che rappresenta le opzioni disponibili per una determinata property.
    /// </summary>
    public class PropertyOptions
    {
        public string PropertyName { get; set; }
        public Dictionary<int, string> OptionValues { get; private set; }
        public int DefaultOptionID { get; set; }

        public PropertyOptions()
        {
            PropertyName = "";
            DefaultOptionID = -1;
            OptionValues = new Dictionary<int, string>();
        }

        /// <summary>
        /// Aggiunge una opzione per la proprietà
        /// </summary>
        /// <param name="id">id identificativo dell'opzione</param>
        /// <param name="description">descrizione da visualizzare</param>
        /// <param name="isDefault">passare true per indicare che l'opzione è quella di default</param>
        public void AddOption(int id, string description, bool isDefault = false)
        {
            OptionValues.Add(id, description);
            if (isDefault)
                DefaultOptionID = id;
        }
    }

    /// <summary>
    /// Classe che rappresenta un elemento della lista degli allegati
    /// </summary>
    public class AttachmentListItem
    {
        public PwdAttachment Attachment { get; set; }
        public string Name { get; set; }
        public bool Remove { get; set; }
        public bool Download { get; set; }
        public bool Upload { get; set; }

        public AttachmentListItem()
        {
            Attachment = null;
            Name = "";
            Remove = false;
            Download = false;
            Upload = false;
        }
    }
}
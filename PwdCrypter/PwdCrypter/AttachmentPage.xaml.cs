using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using PwdCrypter.Controls;
using PwdCrypter.Extensions.ResxLocalization.Resx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PwdCrypter
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AttachmentPage : ContentPage
    {
        private AttachmentPageData PageData = null;
        private FileData FileData = null;
        private bool DataLoaded = false;

        private ToolbarItem SaveToolbarItem = null;
        private ToolbarItem CancelToolbarItem = null;
        private ToolbarItem DeleteToolbarItem = null;

        private event PasswordManager.UpdateProgressEventHandler OnUpdateProgress = null;

        public static Dictionary<string, string> BackgroundImageColor
        {
            get => new Dictionary<string, string> { { "currentColor", "#0000ff" }, { "currentOpacity", "0.15" } };
        }

        public AttachmentPage()
        {
            InitializeComponent();

            PrepareToolbar();
            SetupControls();

            OnUpdateProgress += AttachmentPage_OnUpdateProgress;
        }

        private void AttachmentPage_OnUpdateProgress(object sender, EventArgs e, double step)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                await progressbar.ProgressTo(step, 250, Easing.Linear);
            });
        }

        private void SetupControls()
        {
            entryAttachmentFile.AddFeature("paperclip", "paperclip", OnSelectFile);
        }

        private async Task SelectAttachmentFile()
        {
            try
            {
                FileData = await CrossFilePicker.Current.PickFile();
                if (FileData == null)
                {
                    Debug.WriteLine("Attachment file picker cancelled by user");
                    return;
                }

                entryAttachmentFile.Text = FileData.FileName;
            }
            catch (Exception Ex)
            {
                Debug.WriteLine("Attachment file picker error: " + Ex.Message);
            }
        }

        private async void AttachmentFile_OnClick(object sender, EventArgs e)
        {
            await SelectAttachmentFile();
        }

        private async void OnSelectFile(object sender, EventArgs e, FeatureInfo featureInfo)
        {
            await SelectAttachmentFile();
        }

        private void PrepareToolbar()
        {
            SaveToolbarItem = new ToolbarItem(AppResources.txtSave, "Assets/save.png", () => OnSaveAttachment());
            CancelToolbarItem = new ToolbarItem(AppResources.txtCancel, "Assets/cancel.png", async () => await OnUndo());
            DeleteToolbarItem = new ToolbarItem(AppResources.txtDelete, "Assets/delete.png", async () => await OnDeleteAttachment());
        }

        private async Task OnUndo()
        {
            if (PageData == null)
                return;

            if (await DisplayAlert(App.Title, AppResources.msgQuestionAttachmentUndo, AppResources.btnYes, AppResources.btnNo))
            {
                // Ricarica i dati nei controlli della form
                PresentData();
                // Cancella la selezione del file
                FileData = null;
            }
        }

        private async void OnSaveAttachment()
        {
            if (PageData == null)
                return;

            progressbar.Progress = 0;
            progressbar.IsVisible = true;

            WaitPage waitPage = null;
            if (App.PwdManager.Cloud)
                waitPage = await Utility.StartWait(this);

            try
            {
                string name = entryAttachmentName.Text;
                if (string.IsNullOrEmpty(name))
                    throw new Exception(AppResources.errAttachmentName);
                if (FileData == null && string.IsNullOrEmpty(entryAttachmentFile.Text))
                    throw new Exception(AppResources.errAttachmentFile);

                if (PageData.Attachment != null)
                    PageData.Attachment.Name = name;

                if (FileData != null)
                {
                    if (PageData.AttachmentId > -1)
                    {
                        // Elimina il file dal Cloud e dal locale
                        await App.PwdManager.RemoveAttachment(PageData.Attachment.Filename);
                        // Elimina l'elemento della struttura
                        PageData.PwdInfo.RemoveAttachment(PageData.Attachment);
                    }

                    // Crea un nuovo allegato
                    PageData.AttachmentId = PageData.PwdInfo.AddAttachment().Id;
                    PageData.Attachment.Name = name;
                    try
                    {
                        // Nuovo file
                        long count = FileData.GetStream().Length;
                        byte[] data = new byte[count];
                        FileData.GetStream().Position = 0;
                        await FileData.GetStream().ReadAsync(data, 0, (int)count);
                        PageData.Attachment.AttachmentFile = new PwdAttachmentFile
                        {
                            OriginalFileName = FileData.FileName,
                            MD5 = EncDecHelper.MD5(data)
                        };

                        // Salva l'allegato in locale e su Cloud
                        PageData.Attachment.Filename = await App.PwdManager.SaveAttachment(FileData.GetStream(), OnUpdateProgress);
                        if (string.IsNullOrEmpty(PageData.Attachment.Filename))
                            throw new Exception("Something's gone wrong during the save of the attachment");
                    }
                    catch (Exception)
                    {
                        PageData.AttachmentId = -1;
                        PageData.PwdInfo.RemoveAttachment(PageData.Attachment);
                        throw;
                    }                    
                }

                // Il salvataggio dell'allegato è stato effettuato
                FileData = null;

                // Inserimento nuova password
                if (PageData.DataMode == PwdDataMode.InsertMode)
                    App.PwdManager.AddPassword(PageData.PwdInfo);

                if (PageData.PwdInfo != null && PageData.PwdInfo.Password != null)
                {
                    if (PageData.CurrentPwdHash.CompareTo(EncDecHelper.SHA256(PageData.PwdInfo.Password)) != 0)
                        PageData.PwdInfo.LastPwdChangeDateTime = DateTime.Now;
                }

                // Salva le modifiche
                await App.PwdManager.SavePasswordFile(false);

                // Avvisa la pagina precedente che le modifiche sono state salvate
                PageData.OnPasswordSaved?.Invoke(this, new EventArgs());

                // Richiede l'aggiornamento della lista delle password
                MessagingCenter.Send<Page>(this, "UpdatePasswordList");

                // Torna indietro
                await Navigation.PopAsync(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unable to save the attachment. Error: " + e.Message);
                await DisplayAlert(App.Title, AppResources.errSaveAttachment + " " + e.Message, "OK");
            }
            finally
            {
                progressbar.IsVisible = false;
                progressbar.Progress = 0;

                if (waitPage != null)
                    await Utility.StopWait(waitPage);
            }
        }

        private async Task OnDeleteAttachment()
        {
            if (PageData == null)
                return;
            if (PageData.AttachmentId == -1 || PageData.DataMode != PwdDataMode.EditMode)
            {
                Debug.WriteLine("Delete of attachment failed! Attachment isn't been saved or the operation is not allowed by the current edit modality");
                return;
            }

            // Chiede conferma
            if (!await DisplayAlert(App.Title, AppResources.msgQuestionAttachmentDelete, AppResources.btnYes, AppResources.btnNo))
                return;

            try
            {
                // Elimina il file dal Cloud e dal locale
                await App.PwdManager.RemoveAttachment(PageData.Attachment.Filename);
                // Elimina l'allegato dalla struttura
                PageData.PwdInfo.RemoveAttachment(PageData.Attachment);

                // Salva le modifiche
                await App.PwdManager.SavePasswordFile();

                // Mostra la notifica per indicare che le modifiche sono state salvate
                App.SendLigthNotification(frameNotification,
                                          lblNotification,
                                          AppResources.msgChangeSaved);

                // Richiede l'aggiornamento della lista delle password
                MessagingCenter.Send<Page>(this, "UpdatePasswordList");

                // Torna indietro
                await Navigation.PopAsync(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unable to delete the attachment. Errore: " + e.Message);
                await DisplayAlert(App.Title, AppResources.errDeleteAttachment + e.Message, "OK");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext != null && BindingContext is AttachmentPageData attachmentData)
                LoadData(attachmentData);
        }

        private void LoadData(AttachmentPageData attachmentData)
        {
            if (DataLoaded)
                return;

            PageData = attachmentData;
            SetupToolbar();
            PresentData();
            DataLoaded = true;
        }

        private void SetupToolbar()
        {
            ToolbarItems.Clear();
            ToolbarItems.Add(SaveToolbarItem);

            if (PageData.DataMode == PwdDataMode.EditMode)
            {
                // Il pulsante di cancellazione ha senso se c'è un allegato associato alla password
                if (PageData.AttachmentId > -1)
                    ToolbarItems.Add(DeleteToolbarItem);
                ToolbarItems.Add(CancelToolbarItem);
            }
        }

        private void PresentData()
        {
            entryAttachmentName.Text = PageData.Name;
            entryAttachmentFile.Text = string.IsNullOrEmpty(PageData.OriginalFilename) ? AppResources.txtAttachmentPlaceholder : PageData.OriginalFilename;
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

            gridMain.HorizontalOptions = layoutOptions;
            gridMain.WidthRequest = widthRequest;
        }
    }


    /// <summary>
    /// Classe che rappresenta l'oggetto da passare alla pagina di gestione dell'allegato
    /// </summary>
    public class AttachmentPageData
    {
        public PwdListItem PwdInfo { get; set; }
        public int AttachmentId { get; set; }
        public PwdDataMode DataMode { get; set; }
        public string CurrentPwdHash { get; set; }

        /// <summary>
        /// Allegato della password
        /// </summary>
        public PwdAttachment Attachment
        {
            get
            {
                foreach (PwdAttachment pwdAttachment in PwdInfo.Attachments)
                {
                    if (pwdAttachment.Id == AttachmentId)
                        return pwdAttachment;
                }

                return null;
            }
        }

        public string Name { get => Attachment?.Name; }
        public string OriginalFilename { get => Attachment?.OriginalFilename; }

        public EventHandler OnPasswordSaved { get; set; }

        public AttachmentPageData()
        {
            PwdInfo = null;
            AttachmentId = -1;  // Nessun allegato
            DataMode = PwdDataMode.EditMode;
            CurrentPwdHash = "";
            OnPasswordSaved = null;
        }
    }
}
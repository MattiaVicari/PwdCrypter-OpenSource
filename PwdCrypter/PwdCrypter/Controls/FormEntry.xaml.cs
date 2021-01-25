using PwdCrypter.Extensions.ResxLocalization.Resx;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Zxcvbn;

namespace PwdCrypter.Controls
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class FormEntry : StackLayout
    {
        public delegate void FeatureEventHandler(object sender, EventArgs e, FeatureInfo featureInfo);
        public delegate string SetTextEventHandler(object sender, string oldText);

        private event FeatureEventHandler FeatureIsPassword = null;
        public event EventHandler OnClick = null;
        public event SetTextEventHandler OnSetText = null;
        private readonly Dictionary<FeatureEventHandler, FeatureInfo> DictionaryFeatureEvent = null;

        private readonly Zxcvbn.Zxcvbn StrengthCalculator = null;

        // Livello di sicurezza della passworda, da 0 a 4
        public int PasswordStrength { get; private set; }

        private static string[] PwdStrengthLabel = null;

        public static BindableProperty IsPasswordProperty =
            BindableProperty.Create(nameof(IsPassword), typeof(bool), typeof(FormEntry), default(bool), BindingMode.OneWay);
        public static BindableProperty ShowPasswordStrengthProperty =
            BindableProperty.Create(nameof(ShowPasswordStrength), typeof(bool), typeof(FormEntry), false, BindingMode.OneWay);
        public static BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(FormEntry), default(string), BindingMode.OneWay);
        public static BindableProperty TextProperty =
            BindableProperty.Create(nameof(Text), typeof(string), typeof(FormEntry), default(string), BindingMode.OneWay);
        public static BindableProperty PlaceholderProperty =
            BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(FormEntry), default(string), BindingMode.OneWay);
        public static BindableProperty FontSizeProperty =
            BindableProperty.Create(nameof(FontSize), typeof(double), typeof(FormEntry), default(double), BindingMode.OneWay);
        public static BindableProperty IsEntryEnabledProperty =
            BindableProperty.Create(nameof(IsEntryEnabled), typeof(bool), typeof(FormEntry), true, BindingMode.OneWay);
        public static BindableProperty IsMultilineTextProperty =
            BindableProperty.Create(nameof(IsMultilineText), typeof(bool), typeof(FormEntry), false, BindingMode.OneWay);
        public static BindableProperty TitleWidthRequestProperty =
            BindableProperty.Create(nameof(TitleWidthRequest), typeof(double), typeof(FormEntry), 200.0, BindingMode.OneWay);
        public static BindableProperty MaxTextLengthProperty =
            BindableProperty.Create(nameof(MaxTextLength), typeof(int), typeof(FormEntry), -1, BindingMode.OneWay);
        public static BindableProperty HorizontalTextAlignmentProperty =
            BindableProperty.Create(nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(FormEntry), TextAlignment.Start, BindingMode.OneWay);

        // Valore precedente della view
        public string UndoValue { get; private set; }

        public bool IsPassword
        {
            get => (bool)GetValue(IsPasswordProperty);
            set => SetValue(IsPasswordProperty, value);
        }
        public bool ShowPasswordStrength
        {
            get => (bool)GetValue(ShowPasswordStrengthProperty);
            set => SetValue(ShowPasswordStrengthProperty, value);
        }
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public string Text
        {
            get
            {
                string textValue = (string)GetValue(TextProperty);
                if (MaxTextLength > 0)
                    textValue = textValue.Substring(0, Math.Min(textValue.Length, MaxTextLength));
                return textValue;
            }
            set
            {
                string textValue = value;
                if (OnSetText != null)
                    textValue = OnSetText?.Invoke(this, textValue);
                SetValue(TextProperty, textValue);
            }
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }
        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }
        public bool IsEntryEnabled
        {
            get => (bool)GetValue(IsEntryEnabledProperty);
            set => SetValue(IsEntryEnabledProperty, value);
        }
        public bool IsMultilineText
        {
            get => (bool)GetValue(IsMultilineTextProperty);
            set => SetValue(IsMultilineTextProperty, value);
        }
        public double TitleWidthRequest
        {
            get => (double)GetValue(TitleWidthRequestProperty);
            set => SetValue(TitleWidthRequestProperty, value);
        }
        public int MaxTextLength
        {
            get => (int)GetValue(MaxTextLengthProperty);
            set => SetValue(MaxTextLengthProperty, value);
        }

        public TextAlignment HorizontalTextAlignment
        {
            get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
            set => SetValue(HorizontalTextAlignmentProperty, value);
        }

        public FormEntry ()
		{
			InitializeComponent ();

            UndoValue = null;

            entryValue.IsPassword = IsPassword;
            entryValue.Text = Text;
            editorValue.Text = Text;
            labelDisabledValue.Text = GetLabelText(Text, IsPassword);
            entryValue.Placeholder = Placeholder;
            editorValue.Placeholder = Placeholder;
            if (FontSize > 0)
            {
                entryValue.FontSize = FontSize;
                editorValue.FontSize = FontSize;
                labelDisabledValue.FontSize = FontSize;
            }
            labelTitle.Text = Title;
            labelTitle.WidthRequest = TitleWidthRequest;

            EnableControl();
            ShowControl();

            FeatureIsPassword += OnFeatureIsPassword;

            DictionaryFeatureEvent = new Dictionary<FeatureEventHandler, FeatureInfo>();

            TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += FormEntry_ClickEvent;
            labelDisabledValue.GestureRecognizers.Add(tapGestureRecognizer);

            StrengthCalculator = new Zxcvbn.Zxcvbn();
            PwdStrengthLabel = new string[]
            {
                AppResources.txtWeak,
                AppResources.txtVeryGuessable,
                AppResources.txtSomewhatGuessable,
                AppResources.txtSafelyUnguessable,
                AppResources.txtVeryUnguessable
            };
        }

        private void FormEntry_ClickEvent(object sender, EventArgs e)
        {
            OnClick?.Invoke(sender, e);
        }

        private void EnableControl()
        {
            entryValue.IsEnabled = IsEntryEnabled && IsEnabled;
        }

        private void ShowControl()
        {
            entryValue.IsVisible = entryValue.IsEnabled && !IsMultilineText;
            editorValue.IsVisible = IsMultilineText;
            labelDisabledValue.IsVisible = !entryValue.IsVisible && !IsMultilineText;
            editorValue.IsReadOnly = !entryValue.IsEnabled;
            stackPwdStrength.IsVisible = IsPassword && ShowPasswordStrength && entryValue.IsEnabled;
        }

        private string GetLabelText(string text, bool isPassword)
        {
            if (!isPassword || text == null)
                return text;

            string maskText = "";
            for (int i = 0; i < text.Length; i++)
                maskText += "*";
            return maskText;
        }

        private void EntryValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsMultilineText)
                return;

            Text = e.NewTextValue;

            Result strength = StrengthCalculator.EvaluatePassword(Text);
            PasswordStrength = strength.Score;
            viewPwdStrengthBar.InvalidateSurface();
            labelPwdStrength.Text = PwdStrengthLabel[Math.Min(PwdStrengthLabel.GetUpperBound(0), PasswordStrength)];
        }

        /// <summary>
        /// Aggiunge una funzionalità alla casella di input.
        /// </summary>
        /// <param name="icon">Icona che esegue la funzionalità</param>
        /// <param name="iconTapped">Icon da visualizzare dopo il click</param>
        /// <param name="eventHandler">Evento da richiamare al click</param>
        public void AddFeature(string icon, string iconTapped, FeatureEventHandler eventHandler)
        {
            if (DictionaryFeatureEvent.ContainsKey(eventHandler))
                return;

            gridEntry.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });

            FontAwesomeLabel fontAwesomeLabel = new FontAwesomeLabel(icon)
            {
                TextColor = AppStyle.ButtonBackgroundColor,
                FontSize = GetFontIconSize(),
                VerticalOptions = LayoutOptions.Center
            };
            gridEntry.Children.Add(fontAwesomeLabel, gridEntry.ColumnDefinitions.Count - 1, 0);

            FeatureInfo featureInfo = new FeatureInfo
            {
                FeatureIcon = icon,
                FeatureIconTapped = iconTapped,
                FeatureView = fontAwesomeLabel
            };

            TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += CreateEvent(this, eventHandler, featureInfo);
            fontAwesomeLabel.GestureRecognizers.Add(tapGestureRecognizer);

            DictionaryFeatureEvent.Add(eventHandler, featureInfo);
        }

        /// <summary>
        /// Restituisce true se esiste la feature con icona icon
        /// </summary>
        /// <param name="icon">Icona della feature</param>
        /// <returns>True se la feature è stata attaccata al controllo, false altrimenti</returns>
        public bool FeatureExists(string icon)
        {
            foreach (FeatureInfo info in DictionaryFeatureEvent.Values)
            {
                if (info.FeatureIcon.CompareTo(icon) == 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Permette di nascondere o visualizzare la feature associata all'icona icon.
        /// </summary>
        /// <param name="icon">Icona della feature</param>
        /// <param name="show">True per visualizzare la feature, false per nasconderla</param>
        public void SetFeatureVisible(string icon, bool show)
        {
            foreach (FeatureInfo info in DictionaryFeatureEvent.Values)
            {
                if (info.FeatureIcon.CompareTo(icon) == 0)
                {
                    info.FeatureView.IsVisible = show;
                }
            }
        }

        private EventHandler CreateEvent(FormEntry formEntry, FeatureEventHandler featureEventHandler, FeatureInfo featureInfo)
        {
            return new EventHandler((sender, e) => { featureEventHandler?.Invoke(formEntry, e, featureInfo); });
        }

        private void OnFeatureIsPassword(object sender, EventArgs e, FeatureInfo featureInfo)
        {
            OnFeature(featureInfo);
            entryValue.IsPassword = !entryValue.IsPassword;
            labelDisabledValue.Text = GetLabelText(Text, entryValue.IsPassword);
        }

        /// <summary>
        /// Funzione da richiamare all'interno del gestore della funzione per eseguire
        /// alcune operazioni di default all'evento di click.
        /// </summary>
        /// <param name="featureInfo">Oggetto con le informazioni sulla funzionalità</param>
        public void OnFeature(FeatureInfo featureInfo)
        {
            if (featureInfo.FeatureView is FontAwesomeLabel iconView)
            {
                iconView.Text = iconView.Text.CompareTo(featureInfo.FeatureIcon) == 0 ? 
                                featureInfo.FeatureIconTapped : 
                                featureInfo.FeatureIcon;
            }
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == IsPasswordProperty.PropertyName)
            {
                entryValue.IsPassword = IsPassword;
                labelDisabledValue.Text = GetLabelText(Text, IsPassword);
                if (IsPassword)
                    AddFeature("eye", "eye-slash", FeatureIsPassword);
                ShowControl();
            }
            if (propertyName == IsEntryEnabledProperty.PropertyName)
            {
                EnableControl();
                ShowControl();
            }
            if (propertyName == IsMultilineTextProperty.PropertyName)
            {
                ShowControl();
            }
            if (propertyName == TextProperty.PropertyName)
            {
                entryValue.Text = Text;
                editorValue.Text = Text;
                labelDisabledValue.Text = GetLabelText(Text, IsPassword);

                if (UndoValue == null)
                    UndoValue = Text;
            }
            if (propertyName == PlaceholderProperty.PropertyName)
            {
                entryValue.Placeholder = Placeholder;
                editorValue.Placeholder = Placeholder;
            }
            if (propertyName == FontSizeProperty.PropertyName && FontSize > 0)
            {
                entryValue.FontSize = FontSize;
                editorValue.FontSize = FontSize;
                labelDisabledValue.FontSize = FontSize;
            }
            if (propertyName == TitleProperty.PropertyName)
                labelTitle.Text = Title;
            if (propertyName == TitleWidthRequestProperty.PropertyName)
                labelTitle.WidthRequest = TitleWidthRequest;
            if (propertyName == ShowPasswordStrengthProperty.PropertyName)
                ShowControl();
            if (propertyName == HorizontalTextAlignmentProperty.PropertyName)
            {
                entryValue.HorizontalTextAlignment = HorizontalTextAlignment;                
                labelDisabledValue.HorizontalTextAlignment = HorizontalTextAlignment;
            }
        }

        private double GetFontIconSize()
        {
            if (Device.RuntimePlatform == Device.Android)
                return 40;
            return 20;
        }

        /// <summary>
        /// Permette di tornare al valore iniziale.
        /// Funzione utile se si effettua il binding dei dati.
        /// </summary>
        public void BindingUndo()
        {
            Text = UndoValue;
        }

        private void SKCanvasView_PaintSurface(object sender, SkiaSharp.Views.Forms.SKPaintSurfaceEventArgs e)
        {
            var surface = e.Surface;
            var canvas = surface.Canvas;
            canvas.Clear();

            SKColor[] colors = { SKColors.Red, SKColors.OrangeRed, SKColors.Yellow, SKColors.YellowGreen, SKColors.Green };
            float start = 0, offset = 2.0f;
            float width = (1.0f * e.Info.Width) / 5.0f;
            int score = string.IsNullOrEmpty(entryValue.Text) ? -1 : PasswordStrength;
            for (int i = 0; i < 5; i++)
            {
                if (i <= score)
                {
                    canvas.DrawRect(new SKRect
                    {
                        Left = start,
                        Right = start + width - offset,
                        Bottom = 0,
                        Top = 3
                    }, new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        Color = colors[i]
                    });
                }
                else
                {
                    canvas.DrawRect(new SKRect
                    {
                        Left = start,
                        Right = start + width - offset,
                        Bottom = 0,
                        Top = 3
                    }, new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        Color = SKColors.LightGray
                    });

                    // Ombra
                    canvas.DrawLine(start, 0, start + width - offset, 0,  new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke,
                        Color = SKColors.Gray
                    });
                    canvas.DrawLine(start, 3, start, 0, new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke,
                        Color = SKColors.Gray
                    });
                    // Luce
                    canvas.DrawLine(start, 3, start + width - offset, 3, new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke,
                        Color = SKColor.Parse("#DDDDDD")
                    });
                    canvas.DrawLine(start + width - offset, 3, start + width - offset, 0, new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke,
                        Color = SKColor.Parse("#DDDDDD")
                    });
                }
                start += width;
            }
        }

        private void EditorValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsMultilineText)
                return;
            Text = e.NewTextValue;
        }
    }

    /// <summary>
    /// Classe che raccoglie le informazioni su una feature aggiunto a un controllo
    /// </summary>
    public class FeatureInfo
    {
        public View FeatureView { get; set; }
        public string FeatureIcon { get; set; }
        public string FeatureIconTapped { get; set; }
    }
}
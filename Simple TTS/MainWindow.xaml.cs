﻿#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Simple_TTS.Properties;

#endregion

namespace Simple_TTS
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly SpeechSynthesizer _synthesizer;
        private int _currentCharacterIndex;
        private Prompt _currentPrompt;
        private string _currentWord;
        private int _maxCharacters;
        private SynthesizerState _synthesizerState;
        private int _wordOffset;

        public MainWindow()
        {
            InitializeComponent();

            SettingsHelper.UpgradeSettings();

            _synthesizer = new SpeechSynthesizer();
            SetupSynthesizer();

            if (!Settings.Default.HasSeenWelcomeMessage)
            {
                Settings.Default.HasSeenWelcomeMessage = true;
                ShowWelcomeMessage();
            }

            txtDocument.SelectionStart = Settings.Default.SelectionStart;

            DataContext = this;
            txtDocument.Focus();
        }

        public SynthesizerState SynthesizerState
        {
            get { return _synthesizerState; }
            private set { Set(ref _synthesizerState, value); }
        }

        public int CurrentCharacterIndex
        {
            get { return _currentCharacterIndex; }
            private set { Set(ref _currentCharacterIndex, value); }
        }

        public string CurrentWord
        {
            get { return _currentWord; }
            private set { Set(ref _currentWord, value); }
        }

        public int MaxCharacters
        {
            get { return _maxCharacters; }
            private set { Set(ref _maxCharacters, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool Set<T>(ref T field, T newValue = default(T), [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }
            field = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }

        private void RaisePropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.SelectionStart = txtDocument.SelectionStart;
            SettingsHelper.SaveSettings();
            StopSpeech();
            _synthesizer.Dispose();
        }

        private void Synthesizer_OnSpeakProgress(object sender, SpeakProgressEventArgs e)
        {
            CurrentWord = e.Text;
            CurrentCharacterIndex = e.CharacterPosition + e.Text.Length;
            txtDocument.Select(e.CharacterPosition + _wordOffset, e.Text.Length);
        }

        private void ToggleState()
        {
            switch (_synthesizer.State)
            {
                case SynthesizerState.Paused:
                    ResumeSpeech();
                    break;
                case SynthesizerState.Speaking:
                    PauseSpeech();
                    break;
                default:
                    StartSpeech();
                    break;
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            ToggleState();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopSpeech();
            ResetDocumentSelection();
        }

        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuItemSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog {Filter = "Text Documents (*.txt)|*.txt|All Files (*.*)|*.*"};
            if (dialog.ShowDialog() ?? false)
            {
                File.WriteAllText(dialog.FileName, Settings.Default.Document);
            }
        }

        private void txtDocument_OnLostFocus(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void txtDocument_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SynthesizerState == SynthesizerState.Speaking)
            {
                e.Handled = true;
            }
        }

        private void txtDocument_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (SynthesizerState == SynthesizerState.Speaking)
            {
                e.Handled = true;
            }
        }

        private void MenuItemAbout_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new About();
            dialog.ShowDialog();
        }

        private void MenuItemOptions_OnClick(object sender, RoutedEventArgs e)
        {
            var optionsDialog = new Options(_synthesizer.GetInstalledVoices());
            optionsDialog.ShowDialog();
        }

        private void StartSpeech()
        {
            var selection = txtDocument.SelectionStart;
            if (selection >= Settings.Default.Document.Length || selection == -1)
            {
                selection = 0;
            }
            var text = Settings.Default.Document.Substring(selection, Settings.Default.Document.Length - selection);
            _wordOffset = selection;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            _currentPrompt = new Prompt(text);
            MaxCharacters = text.Length;
            _synthesizer.Rate = Settings.Default.Speed;
            _synthesizer.Volume = Settings.Default.Volume;
            try
            {
                _synthesizer.SelectVoice(Settings.Default.Voice);
            }
            catch (ArgumentException)
            {
            }
            _synthesizer.SetOutputToDefaultAudioDevice();
            _synthesizer.SpeakAsync(_currentPrompt);
        }

        private void StopSpeech()
        {
            if (_currentPrompt == null)
            {
                return;
            }
            _synthesizer.SpeakAsyncCancel(_currentPrompt);
            _currentPrompt = null;
            _synthesizer.Resume();
            CurrentWord = string.Empty;
            CurrentCharacterIndex = 0;

            btnStart.Focus();
        }

        private void PauseSpeech()
        {
            _synthesizer.Pause();
        }

        private void ResumeSpeech()
        {
            _synthesizer.Resume();
        }

        private void ResetDocumentSelection()
        {
            txtDocument.SelectionStart = 0;
            txtDocument.Select(0, 0);
        }

        private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.MediaPlayPause:
                    ToggleState();
                    break;
                case Key.MediaStop:
                    StopSpeech();
                    break;
            }
        }

        private void ShowWelcomeMessage()
        {
            Settings.Default.SelectionStart = 0;
            Settings.Default.Document = string.Format(Properties.Resources.WelcomeMessage, Environment.UserName,
                AssemblyInfo.Version, Properties.Resources.GitHubIssues);

            Settings.Default.Document +=
                $"{Environment.NewLine}{Environment.NewLine}{AssemblyInfo.Copyright}";
            Settings.Default.Document +=
                $"{Environment.NewLine}{Environment.NewLine}(This message will only appear once.)";
        }

        private void SetupSynthesizer()
        {
            _synthesizer.SpeakCompleted += (sender, args) => ResetDocumentSelection();
            _synthesizer.StateChanged += (sender, args) => SynthesizerState = args.State;
            _synthesizer.SpeakProgress += Synthesizer_OnSpeakProgress;
        }

        private void Savebutton_OnClick(object sender, RoutedEventArgs e)
        {
            var filename = $"{filenamebox.Text}_{_synthesizer.Voice.Name}.wav";
            try
            {
                _synthesizer.Resume();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            _synthesizer.Speak("");
            _synthesizer.SetOutputToWaveFile(filename);
            
            _synthesizer.Speak(txtDocument.Text);
            _synthesizer.SetOutputToDefaultAudioDevice();
            MessageBox.Show($"Saved file {filename}");
        }
    }
}
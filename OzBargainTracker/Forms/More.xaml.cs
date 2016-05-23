#region

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

#endregion

namespace OzBargainTracker.Forms
{
    public partial class MoreWindow : MetroWindow
    {
        private readonly TrackerMain _mainForm;
        private bool _reloaded;

        public MoreWindow(TrackerMain mainForm)
        {
            _mainForm = mainForm;
            InitializeComponent();
            Icon = Properties.Resources.IconPng.ToBitmapImage();
        }

        public void Reload()
        {
            VersionLbl.Content = TrackerMain.AppVersion;
            WebsiteTbx.Text = _mainForm.Settings.Website;
            EmailTbx.Text = _mainForm.Settings.Email;
            PasswordTbx.Text = _mainForm.Settings.Password;
            SMTPHostTbx.Text = _mainForm.Settings.SmtpHost;
            EmailSubjectTbx.Text = _mainForm.Settings.EmailSubject;
            PortNUD.Value = _mainForm.Settings.Port;
            LogTimeChk.IsChecked = _mainForm.Settings.LogShowTime;
            _reloaded = true;
        }

        public new virtual void Show()
        {
            Reload();
            base.Show();
        }

        public void Save()
        {
            if (!IsInitialized || !_reloaded) return;
            _mainForm.Settings.Website = WebsiteTbx.Text;
            _mainForm.Settings.Email = EmailTbx.Text;
            _mainForm.Settings.Password = PasswordTbx.Text;
            _mainForm.Settings.SmtpHost = SMTPHostTbx.Text;
            _mainForm.Settings.EmailSubject = EmailSubjectTbx.Text;
            _mainForm.Settings.Port = (int) (PortNUD.Value);
            _mainForm.Settings.LogShowTime = (bool) (LogTimeChk.IsChecked);
            _mainForm.Settings.Save();
            _reloaded = false;
            Reload();
        }

        private void DonateBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=HKWZZE6933B48");
        }

        private void GithubBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/AntonSidorov/OzBargainTracker");
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void LogTimeChk_Checked(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void PortNUD_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            Save();
        }

        private void EmailSubjectTbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            Save();
        }

        private void SMTPHostTbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            Save();
        }

        private void PasswordTbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            Save();
        }

        private void EmailTbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            Save();
        }

        private void WebsiteTbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            Save();
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            Icon = Properties.Resources.IconPng.ToBitmapImage();
        }

        private void MetroWindow_Deactivated(object sender, EventArgs e)
        {
            Icon = Properties.Resources.IconGreyPng.ToBitmapImage();
        }
    }
}
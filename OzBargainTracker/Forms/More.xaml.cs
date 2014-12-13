using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OzBargainTracker
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class MoreWindow : MetroWindow
    {
        bool Reloaded = false;
        TrackerMain MainForm;
        /// <summary>
        /// Default constructor for this form
        /// </summary>
        /// <param name="MainForm">Reference to the Main Form, used for Logs and Settings</param>
        public MoreWindow(TrackerMain MainForm)
        {
            this.MainForm = MainForm;
            InitializeComponent();
            Icon = BitmapFrame.Create(new Uri(Utils.StartupLocation + @"\Icon.png"));
        }
        /// <summary>
        /// Method that reloads the settings into this form
        /// </summary>
        public void Reload()
        {
            this.VersionLbl.Content = TrackerMain.AppVersion;
            this.WebsiteTbx.Text = MainForm.Settings.Website;
            this.EmailTbx.Text = MainForm.Settings.Email;
            this.PasswordTbx.Text = MainForm.Settings.Password;
            this.SMTPHostTbx.Text = MainForm.Settings.SMTPHost;
            this.EmailSubjectTbx.Text = MainForm.Settings.EmailSubject;
            this.PortNUD.Value = MainForm.Settings.Port;
            this.LogTimeChk.IsChecked = MainForm.Settings.LogShowTime;
            Reloaded = true;
        }
        /// <summary>
        /// An overide for the Show method, which should show the form but now also Reloads the settings into the form before showing it
        /// </summary>
        public virtual void Show()
        {
            Reload();
            base.Show();
        }
        /// <summary>
        /// Saves the Settings from this form into the Settings Class and calls the Settings Class's method of saving those settings
        /// </summary>
        public void Save()
        {
            if (this.IsInitialized && Reloaded)
            {
                MainForm.Settings.Website = this.WebsiteTbx.Text;
                MainForm.Settings.Email = this.EmailTbx.Text;
                MainForm.Settings.Password = this.PasswordTbx.Text;
                MainForm.Settings.SMTPHost = this.SMTPHostTbx.Text;
                MainForm.Settings.EmailSubject = this.EmailSubjectTbx.Text;
                MainForm.Settings.Port = (int)(this.PortNUD.Value);
                MainForm.Settings.LogShowTime = (bool)(this.LogTimeChk.IsChecked);
                MainForm.Settings.Save();
                Reloaded = false;
                Reload();
            }
        }
        //Donate
        private void DonateBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=HKWZZE6933B48");
        }

        //Github
        private void GithubBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/TheMrNobody/OzBargainTracker");
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = System.Windows.Visibility.Hidden;
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
            Icon = BitmapFrame.Create(new Uri(Utils.StartupLocation + @"\Icon.png"));
        }

        private void MetroWindow_Deactivated(object sender, EventArgs e)
        {
            Icon = BitmapFrame.Create(new Uri(Utils.StartupLocation + @"\Icon2.png"));
        }

    }
}

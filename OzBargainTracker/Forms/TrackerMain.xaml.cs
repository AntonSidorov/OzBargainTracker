#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using LogWriter;
using MahApps.Metro.Controls;
using OzBargainTracker.Classes;
using OzBargainTracker.Forms;
using Brushes = System.Windows.Media.Brushes;

#endregion

namespace OzBargainTracker
{
    public enum LogMessageType
    {
        DebugLog = 0,
        Info = 1,
        Warning = 2,
        HandledError = 3,
        UnHandledError = 4
    }

    public partial class TrackerMain : MetroWindow
    {
        public const string AppVersion = "2.1.1.0";
        private readonly Logger _logWriter;
        private readonly NotifyIcon _trayIcon = new NotifyIcon();
        private DispatcherTimer _oneSecTimer;
        private bool _running, _loaded;
        public AccountsSetup AccountsWindow;
        public EmailManager MailManager;
        public AppSettings Settings;
        public MoreWindow SettingsWindow;

        public TrackerMain()
        {
            InitializeComponent();
            if (Process.GetProcessesByName("ozbargaintracker").Length > 1)
                Environment.Exit(-1);
            _logWriter = new Logger(Log_box, Dispatcher, logFileName: "debug.log", prependLogType: true);

            _logWriter.Log("Booting up...", LogType.DebugLog);

            _trayIcon.Text = @"OzBargainTracker";
            _trayIcon.Icon = Properties.Resources.Icon;
            _trayIcon.Click += _trayIcon_Click;

            _logWriter.Log("GUI Set up Done", LogType.DebugLog);

            Icon = Properties.Resources.IconPng.ToBitmapImage();
            Settings = new AppSettings(this);
            if (Settings.Debug)
                _logWriter.WriteDebugToLog = true;
            AccountsWindow = new AccountsSetup(this);
            SettingsWindow = new MoreWindow(this);
            new Task(() => { MailManager = new EmailManager(this); }).Start();
            _logWriter.Log("Initializing...", LogType.DebugLog);
            Init();
            _logWriter.Log("Finished Setup.", LogType.DebugLog);
        }

        private void _trayIcon_Click(object sender, EventArgs e)
        {
            Visibility = Visibility.Visible;
            Focus();
            _trayIcon.Visible = false;
        }

        private void Init()
        {
            Settings = new AppSettings(this, Utils.StartupLocation + @"\Settings.xml");
            AccountsWindow.Reload(Utils.StartupLocation + @"\Accounts.xml");
            _oneSecTimer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            _oneSecTimer.Tick += OneSecTimer_Tick;
            _oneSecTimer.Start();
            Refresh();
            _loaded = true;
            StartStopBtn_Click(null, new RoutedEventArgs());
        }

        private async Task<string> FetchOzBargain()
        {
            try
            {
                _logWriter.Log("Fetching OzBargain");
                var client = new HttpClient();
                var websiteContent = await client.GetStringAsync(Settings.Website);
                _logWriter.Log("OzBargain Fetched");
                return websiteContent;
            }
            catch (Exception ex)
            {
                _logWriter.Log("Failed To Fetch OzBargain", LogType.DebugLog);
                LogException(ex);
                return null;
            }
        }

        private void CheckOzBargain(string ozBargain)
        {
            _logWriter.Log("Analysing OzBargain");
            AccountsWindow.Reload(Utils.StartupLocation + "\\accounts.xml");
            var repeated = false;
            string lastDeal = null;
            try
            {
                using (var reader = new StreamReader(Utils.StartupLocation + @"\LastDeal.txt"))
                    lastDeal = reader.ReadLine();
            }
            catch (Exception ex)
            {
                _logWriter.Log("Couldn't read the LastDeal.txt, continuing anyways.", LogType.Warning);
                LogException(ex);
            }

            if (lastDeal == null)
                lastDeal = "NoLastDeal";

            var deals = new List<Deal>();

            try
            {
                _logWriter.Log("Looking for deals", LogType.DebugLog);
                var aMatches = new Match[10];
                Regex.Matches(ozBargain, @"<item>[\s]*?<title>([\s\S]*?)<\/title>[\s\S]*?<link>([\s\S]*?)<\/link>",
                    RegexOptions.Multiline).CopyTo(aMatches, 0);
                var matches = aMatches.ToList();
                _logWriter.Log($"Found {matches.Count} Deals after regex", LogType.DebugLog);
                var lastDealIndex = matches.FindIndex(m => m.Groups[2].Value == lastDeal);
                if (lastDealIndex > -1)
                    matches.RemoveRange(lastDealIndex, 10 - lastDealIndex);
                _logWriter.Log($"Found {matches.Count} Deals after lastdeal remove", LogType.DebugLog);
                deals.AddRange(matches.Select(m => new Deal(m.Groups[2].Value, m.Groups[1].Value)));
            }
            catch (Exception ex)
            {
                _logWriter.Log($"Failed to find the deals. Will try again in {Settings.Interval} seconds.",
                    LogType.Warning);
                LogException(ex);
            }

            if (deals.Count == 0) return;

            lastDeal = deals[0].Url;
            try
            {
                var writer = new StreamWriter(Utils.StartupLocation + @"\LastDeal.txt");
                writer.WriteLine(lastDeal);
                writer.Close();
            }
            catch (Exception ex)
            {
                _logWriter.Log("Couldn't write the lastdeal", LogType.Warning);
                LogException(ex);
            }

            foreach (var deal in deals)
                foreach (var user in AccountsWindow.Users)
                    foreach (var tag in user.TagsSplit.Where(tag => deal.Title.ToUpper().Contains(tag.ToUpper())))
                    {
                        _logWriter.Log($"Found a deal for user {user.Email}, with tag {tag}", LogType.DebugLog);
                        if (!user.DealsForUser.Contains(deal))
                            user.DealsForUser.Add(deal);

                        if (!user.TriggerTags.Contains(tag))
                            user.TriggerTags.Add(tag);
                    }

            foreach (var user in AccountsWindow.Users.Where(user => user.DealsForUser.Count != 0))
                MailManager.Requests.Enqueue(user);
        }

        public void Refresh()
        {
            TimeLeftPBar.Value = 0;
            TimeLeftPBar.Maximum = Settings.Interval;
            RefreshRateNUD.Value = Settings.Interval;
        }

        private async void OneSecTimer_Tick(object sender, EventArgs e)
        {
            if (!_running) return;
            TimeLeftPBar.Value++;
            if (TimeLeftPBar.Value != TimeLeftPBar.Maximum) return;
            TimeLeftPBar.Value = 0;
            var fetch = FetchOzBargain();
            await fetch;
            if (fetch.Result != null)
                CheckOzBargain(fetch.Result);
        }

        public void LogException(Exception ex)
        {
            _logWriter.Log(
                $"An exception occured, but was (most likely) handled.\r\n Message: {ex.Message}\r\n InnerException: {ex.InnerException}\r\n Stack Trace: {ex.StackTrace}",
                LogType.DebugLog);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AccountsWindow.Visibility = Visibility.Visible;
            AccountsWindow.Show();
            AccountsWindow.Focus();
        }

        private void StartStopBtn_Click(object sender, RoutedEventArgs e)
        {
            _running = !_running;
            switch (_running)
            {
                case true:
                    Status_Label.Text = "Running";
                    Status_Label.Foreground = Brushes.Green;
                    StartStopBtn.Content = "Stop";
                    break;
                case false:
                    Status_Label.Text = "Stopped";
                    Status_Label.Foreground = Brushes.Red;
                    StartStopBtn.Content = "Start";
                    TimeLeftPBar.Value = 0;
                    break;
            }
        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log_box.FontSize = (Width - MinWidth)/(MaxWidth - MinWidth)*(13 - 9.333) + 9.333;
            //13 -- max font size; 9.333 --  min
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SettingsWindow.Visibility = Visibility.Visible;
            SettingsWindow.Show();
            SettingsWindow.Focus();
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
        }

        private void RefreshRateNUD_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            Settings.Interval = (int) RefreshRateNUD.Value;
            Settings.Save();
            Refresh();
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            Icon = Properties.Resources.IconPng.ToBitmapImage();
        }

        private void MetroWindow_Deactivated(object sender, EventArgs e)
        {
            Icon = Properties.Resources.IconGreyPng.ToBitmapImage();
            if (WindowState != WindowState.Minimized)
                return;
            WindowState = WindowState.Normal;
            AccountsWindow.Visibility = Visibility.Hidden;
            SettingsWindow.Visibility = Visibility.Hidden;
            Visibility = Visibility.Hidden;
            _trayIcon.Visible = true;
        }

        private void Log_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            Log_box.ScrollToEnd();
        }
    }

    public static class Utils
    {
        public static string StartupLocation => Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            using (var memoryStream1 = new MemoryStream())
            {
                bitmap.Save(memoryStream1, ImageFormat.Png);
                memoryStream1.Position = 0L;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                var memoryStream2 = memoryStream1;
                bitmapImage.StreamSource = memoryStream2;
                var num = 1;
                bitmapImage.CacheOption = (BitmapCacheOption) num;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        public static XmlDocument ToXmlDoc(this XmlReader reader)
        {
            var doc = new XmlDocument();
            doc.Load(reader);
            return doc;
        }

        public static XmlReader ToXmlReader(this XmlDocument document)
        {
            var reader = XmlReader.Create(new StringReader(document.OuterXml));
            reader.Read();
            return reader;
        }
    }
}
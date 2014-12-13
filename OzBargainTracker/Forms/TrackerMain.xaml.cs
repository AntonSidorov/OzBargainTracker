using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
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
using System.Windows.Threading;
using System.Xml;

namespace OzBargainTracker
{
    /// <summary>
    /// Interaction logic for TrackerMain.xaml
    /// </summary>



    /// <summary>
    /// Types of Messages used in Log
    /// </summary>
    public enum LogMessageType
    {
        DebugLog = 0,
        Info = 1,
        Warning = 2,
        HandledError = 3,
        UnHandledError = 4,
    }
    /// <summary>
    /// Main Tracker Form
    /// </summary>
    public partial class TrackerMain : MetroWindow
    {
        public const string AppVersion = "1.0.0.0";

        DispatcherTimer OneSecTimer;
        bool Running = false, Loaded = false;
        public AccountsSetup AccountsWindow;
        public MoreWindow _Settings;
        public AppSettings Settings;
        public EmailManager MailManager;

        /// <summary>
        /// The constructor of the Main Tracker Form
        /// </summary>
        public TrackerMain()
        {
            InitializeComponent();
            Icon = BitmapFrame.Create(new Uri(Utils.StartupLocation + @"\Icon.png"));
            //Custom Classes Setup
            Settings = new AppSettings(this);
            Log("Setting up the Custom Classes", LogMessageType.DebugLog);
            AccountsWindow = new AccountsSetup(this);
            _Settings = new OzBargainTracker.MoreWindow(this);
            //Start the MailManager on a separate thread
            new Task(() => { MailManager = new EmailManager(this); }).Start();
            Log("Setup part 1 finished", LogMessageType.DebugLog);
            //XML Loading and other stuff
            Init();
        }

        /// <summary>
        /// Initializes the tracker and sets up the AccountsSetup Window  + Settings for the tracker
        /// </summary>
        void Init()
        {
            Log("Loading Accounts and Settings", LogMessageType.DebugLog);
            Settings = new AppSettings(this, Utils.StartupLocation + @"\Settings.xml");
            AccountsWindow.Reload(Utils.StartupLocation + @"\Accounts.xml");
            Log("Accounts and Settings Loaded successfully");
            //Timer
            OneSecTimer = new DispatcherTimer();
            OneSecTimer.Interval = TimeSpan.FromSeconds(1);
            OneSecTimer.Tick += OneSecTimer_Tick;
            OneSecTimer.Start();
            //Load all the stuff from the already loaded settings into the forms
            Refresh();
            //Start the tracker
            Log("Tracker Setup finished...Starting up", LogMessageType.DebugLog);
            Loaded = true;
            Log("Start/Stop Button Triggered, causing the tracker to start", LogMessageType.DebugLog);
            StartStopBtn_Click(null, new RoutedEventArgs());
        }

        /// <summary>
        /// Task for fetching OzBargain, needs to be executed asyncrhoniously for best experience
        /// </summary>
        /// <returns>returns ozbargain.com.au/feed in a string</returns>
        async Task<string> FetchOzBargain()
        {
            //Add Comments and logging
            try
            {//Possible Problems: No internet, tampering with settings, causing the tracker to not fetch the right website
                //[PAL] If there are problems with the settings, reload the default settings (After asking the user for permission)
                Log("Trying to fetch OzBargain...", LogMessageType.DebugLog);
                HttpClient client = new HttpClient();
                string WebsiteContent = await client.GetStringAsync(Settings.Website);
                Log("OzBargain Fetch Successful");
                return WebsiteContent;
            }
            //in case of an exception return nothing
            catch (Exception ex)
            {
                Log("Unable to fetch OzBargain, retrying in {0}s", Settings.Interval, LogMessageType.Warning);
                Log("Exception occured and was handled, info about the exception: \r\nStackTrace: {0}\r\nMessage: {1}\r\nSetup: \r\nWebsite: {2}", LogMessageType.DebugLog,
                               ex.StackTrace, ex.Message, Settings.Website);
                return null;
            }

        }

        /// <summary>
        /// Does the main work (Reloads the Accounts and checks the fetched string with OzBargain in it for new Deals)
        /// </summary>
        /// <param name="OzBargain">The XmlReader with OzBargain RSS Feed loaded in it</param>
        //Removing this in next release
        [Obsolete("This class is Obsolete and will be replaced by CheckOzBargain in next release")]
        void DoWork(XmlReader OzBargain)
        {
            //Reload the accounts, they may have been changed
            AccountsWindow.Reload(Utils.StartupLocation + @"\accounts.xml");
            CheckOzBargain(OzBargain);
        }

        /// <summary>
        /// The Actual method that checks if the fetched RSS Feed contains any interesting Deals for the users.
        /// </summary>
        /// <param name="OzBargain">The XmlReader with OzBargain RSS Feed loaded in it</param>
        void CheckOzBargain(XmlReader OzBargain)
        {
            Log("Starting To Check OzBargain");
            //Changed to true when reached the last deal from previous time
            bool Repeated = false;
            string LastDeal = null;
            try
            {//Possible problems: File not found/in use
                Log("Attempting to Read LastDeal.txt", LogMessageType.DebugLog);
                StreamReader Reader = new StreamReader(Utils.StartupLocation + @"\LastDeal.txt");
                LastDeal = Reader.ReadLine();
                Reader.Close();
            }
            catch (Exception ex)
            {//[PAL]add better handling
                Log("Could not get the last deal from the LastDeal.txt, continuing without, this may cause duplicate emails", LogMessageType.HandledError);
                LogException(ex);
            }

            //No possible matches, meaning all of the front page deals from OzBargain will be loaded in
            if (LastDeal == null)
                LastDeal = "RandomString here M9";

            List<Deal> Deals = new List<Deal>();

            try
            {//Possible problems: Unknown exception not allowing the Syndication Feed to load in
                Log("Attempting to load the RSS Feed into a Syndication Feed", LogMessageType.DebugLog);
                SyndicationFeed Feed = SyndicationFeed.Load(OzBargain);
                foreach (SyndicationItem i in Feed.Items)
                {
                    if (i.Links[0].Uri.ToString().ToUpper() == LastDeal.ToUpper())
                        Repeated = true;
                    if (i.Links[0].Uri.ToString().ToUpper() != LastDeal.ToUpper() && !Repeated)
                    {
                        Deal _Deal = new Deal(i.Links[0].Uri.ToString(), i.Title.Text);
                        Deals.Add(_Deal);
                    }
                }
            }
            catch (Exception ex)
            {//Analyse posible errors and add better handling
                Log("Unable to load OzBargain RSS feed into a syndication feed, retrying in {0}s", Settings.Interval, LogMessageType.HandledError);
                LogException(ex);
            }

            if (Deals.Count != 0)
            {
                Log("Found Deals.", LogMessageType.DebugLog);
                LastDeal = Deals[0].Url;
                try
                {//Possible Errors: File in use
                    Log("Attempting to save the last deal to LastDeal.txt", LogMessageType.DebugLog);
                    StreamWriter Writer = new StreamWriter(Utils.StartupLocation + @"\LastDeal.txt");
                    Writer.WriteLine(LastDeal);
                    Writer.Close();
                }
                catch (Exception ex)
                {// Add better handling?
                    Log("Unable to save Last Deal to LastDeal.txt, attempting to continue, possible email duplicates may appear", LogMessageType.HandledError);
                    LogException(ex);
                }

                foreach (Deal DealItem in Deals)
                {
                    string Title = DealItem.Title.ToUpper();
                    foreach (User user in AccountsWindow.Users)
                        foreach (string tag in user.TagsSplit)
                        {
                            string Tag = tag.ToUpper();
                            if (Title.Contains(Tag))
                            {//Contains a Tag
                                if (!user.DealsForUser.Contains(DealItem))
                                    user.DealsForUser.Add(DealItem);
                                if(!user.TriggerTags.Contains(tag))
                                    user.TriggerTags.Add(tag);
                                //Break if found one tag(doesn't matter if multiple tho)
                            }
                        }
                }
                foreach (User user in AccountsWindow.Users)
                    if (user.DealsForUser.Count != 0)
                        MailManager.Requests.Enqueue(user);
            }
        }
        /// <summary>
        /// Method called to load in the settings that may have been changed into the form
        /// </summary>
        public void Refresh()
        {
            TimeLeftPBar.Value = 0;
            TimeLeftPBar.Maximum = Settings.Interval;
            RefreshRateNUD.Value = Settings.Interval;
        }

        async void OneSecTimer_Tick(object sender, EventArgs e)
        {
            if (Running)
            {
                TimeLeftPBar.Value++;
                if (TimeLeftPBar.Value == TimeLeftPBar.Maximum)
                {
                    Log("Interval Elapsed, initiating Fetching logic");
                    TimeLeftPBar.Value = 0;
                    //Fetch OzBargain here
                    Task<string> Fetch = FetchOzBargain();
                    await Fetch;
                    Log("OzBargain Fetched", LogMessageType.DebugLog);
                    if (Fetch.Result != null)
                        DoWork(XmlReader.Create(new StringReader(Fetch.Result)));
                    else
                        Log("OzBargain fetch fail.", LogMessageType.Warning);
                }
            }
        }

        /// <summary>
        /// Method that Logs a message into the LogBox in TrackerMainForm
        /// </summary>
        /// <param name="LogMessage">Message to Log with a special format</param>
        /// <param name="arg0">An object to format in the LogMessage</param>
        /// <param name="MsgType">Type of the Message(defaulted to LogMessageType.Info)</param>
        public void Log(string LogMessage, object arg0, LogMessageType MsgType = LogMessageType.Info)
        { Log(string.Format(LogMessage, arg0), MsgType); }
        /// <summary>
        /// Method that Logs a message into the LogBox in TrackerMainForm
        /// </summary>
        /// <param name="LogMessage">Message to Log with a special format</param>
        /// <param name="arg0">First object to format in the LogMessage</param>
        /// <param name="arg1">Second object to format in the LogMessage</param>
        /// <param name="MsgType">Type of the Message(defaulted to LogMessageType.Info)</param>
        public void Log(string LogMessage, object arg0, object arg1, LogMessageType MsgType = LogMessageType.Info)
        { Log(string.Format(LogMessage, arg0, arg1), MsgType); }
        /// <summary>
        /// Method that Logs a message into the LogBox in TrackerMainForm
        /// </summary>
        /// <param name="LogMessage">Message to Log with a special format</param>
        /// <param name="arg0">First object to format in the LogMessage</param>
        /// <param name="arg1">Second object to format in the LogMessage</param>
        /// <param name="arg2">Third object to format in the LogMessage</param>
        /// <param name="MsgType">Type of the Message(defaulted to LogMessageType.Info)</param>
        public void Log(string LogMessage, object arg0, object arg1, object arg2, LogMessageType MsgType = LogMessageType.Info)
        { Log(string.Format(LogMessage, arg0, arg1, arg2), MsgType); }
        /// <summary>
        /// Method that Logs a message into the LogBox in TrackerMainForm
        /// </summary>
        /// <param name="LogMessage">Message to Log with a special format</param>
        /// <param name="MsgType">Type of the Message(defaulted to LogMessageType.Info)</param>
        /// <param name="args">An array of objects to format in the LogMessage</param>
        public void Log(string LogMessage, LogMessageType MsgType = LogMessageType.Info, params object[] args)
        { Log(string.Format(LogMessage, args), MsgType); }
        /// <summary>
        /// Method that Logs a message into the LogBox in TrackerMainForm
        /// </summary>
        /// <param name="LogMessage">Message to Log</param>
        /// <param name="MsgType">Type of the Message(defaulted to LogMessageType.Info)</param>
        public void Log(string LogMessage, LogMessageType MsgType = LogMessageType.Info)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    Paragraph Msg = new Paragraph();
                    Msg.LineHeight = 0.5;
                    string Message = LogMessage;
                    switch (MsgType)
                    {
                        case LogMessageType.DebugLog:
                            Message = "[Debug] " + Message;
                            Msg.Foreground = Brushes.MediumBlue;
                            break;
                        case LogMessageType.Info:
                            Message = "[Info] " + Message;
                            Msg.Foreground = Brushes.Blue;
                            break;
                        case LogMessageType.Warning:
                            Message = "[Warning] " + Message;
                            Msg.Foreground = Brushes.Goldenrod;
                            break;
                        case LogMessageType.HandledError:
                            Message = "[Error] " + Message;
                            Msg.Foreground = Brushes.Goldenrod;
                            break;
                        case LogMessageType.UnHandledError:
                            Message = "[Critical Error] " + Message;
                            Msg.Foreground = Brushes.DarkRed;
                            break;
                        default:
                            break;
                    }
                    if (Settings.LogShowTime || MsgType == LogMessageType.DebugLog)
                        Message = string.Format("[{0}] {1}", DateTime.Now, Message);
                    Msg.Inlines.Add(Message);
                    if (!(MsgType == LogMessageType.DebugLog && !Settings.Debug))
                        Log_box.Document.Blocks.Add(Msg);
                    //Write the debug log
                    if (MsgType == LogMessageType.DebugLog)
                    {
                        string DebugLog = "";
                        try
                        {
                            StreamReader SR = new StreamReader(Utils.StartupLocation + @"\Debug.log");
                            DebugLog = SR.ReadToEnd();
                            SR.Close();
                        }
                        catch
                        {//[DAL] Add handling for if the tracker cannot access the file
                        }
                        StreamWriter SW = new StreamWriter(Utils.StartupLocation + @"\Debug.log");
                        if (DebugLog.Length != 0)
                            SW.WriteLine(DebugLog);
                        SW.WriteLine(Message);
                        SW.Close();
                    }
                    try
                    {
                        //Write the Global Log (Resets every application launch)
                        StreamWriter Writer = new StreamWriter(Utils.StartupLocation + @"\Log.txt");
                        TextRange Text = new TextRange(Log_box.Document.ContentStart, Log_box.Document.ContentEnd);
                        Writer.Write(Text.Text);
                        Writer.Close();
                    }
                    catch
                    {//[DAL] Add handling for if the tracker cannot access the file
                    }
                }));
        }
        /// <summary>
        /// Produces a log out of an exception 
        /// </summary>
        /// <param name="Ex">The exception to log</param>
        public void LogException(Exception Ex)
        {
            Log("Exception occured and was handled, info about the exception: \r\nStackTrace: {0}\r\nMessage: {1}\r\n", LogMessageType.DebugLog,
                           Ex.StackTrace, Ex.Message);
        }
        //Event Handlers
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AccountsWindow.Show();
        }
        private void StartStopBtn_Click(object sender, RoutedEventArgs e)
        {
            Running = !Running;
            switch (Running)
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
            Log_box.FontSize = (this.Width - this.MinWidth) / (MaxWidth - MinWidth) * (13 - 9.333) + 9.333;//13 -- max font size; 9.333 --  min
        }

        //Show Settings
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _Settings.Show();
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //add maybe minimize to tray here
        }

        private void RefreshRateNUD_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            Settings.Interval = (int)RefreshRateNUD.Value;
            Settings.Save();
            Refresh();
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            Icon = BitmapFrame.Create(new Uri(Utils.StartupLocation + @"\Icon.png"));
        }

        private void MetroWindow_Deactivated(object sender, EventArgs e)
        {
            Icon = BitmapFrame.Create(new Uri(Utils.StartupLocation + @"\Icon2.png"));
        }

        private void Log_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            Log_box.ScrollToEnd();
        }


    }
    //[PAL] Add error handling for the XML Classes conversion (just in case something does happen)
    public static class Utils
    {
        /// <summary>
        /// Gets the startup location of the app
        /// </summary>
        public static string StartupLocation
        {
            get
            {
                return System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            }
        }
        /// <summary>
        /// Converts a XmlReader to a XmlDocument
        /// </summary>
        /// <param name="Reader">XmlReader to convert</param>
        /// <returns>XmlDocument made from the XmlReader</returns>
        public static XmlDocument ToXMLDoc(this XmlReader Reader)
        {
            XmlDocument Doc = new XmlDocument();
            Doc.Load(Reader);
            return Doc;
        }
        /// <summary>
        /// Converts a XmlDocument to a XmlReader
        /// </summary>
        /// <param name="Document">XmlDocument to convert</param>
        /// <returns>XmlReader made from the XmlDocument</returns>
        public static XmlReader ToXMLReader(this XmlDocument Document)
        {
            XmlReader Reader = XmlReader.Create(new StringReader(Document.OuterXml));
            Reader.Read();
            return Reader;
        }
    }
}

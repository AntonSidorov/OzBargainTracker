using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;

namespace OzBargainTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        BackgroundWorker WebsiteWorker = new BackgroundWorker();
        XmlReader Settings, OzBargain;
        DispatcherTimer UpdateTimer, OneSecTimer;
        bool isRunning = false, Advanced = false, OzBargainFetched = false, ToCheck = true, isInitialized = false;
        string Email = "", Password = "", Website = "";
        DataSet SettingsDS = new DataSet();
        List<User> Users = new List<User>();
        int Interval = 0;
        enum LogMessageType
        {
            DebugLog = 0,
            Message = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
        }
        public MainWindow()
        {

            //Initialization of variables and UI
            InitializeComponent();
            Log("Init Begin", LogMessageType.DebugLog);

            OneSecTimer = new DispatcherTimer();    //A timer for steps in the time left progressbar
            OneSecTimer.Tick += OneSecTimer_Tick;   //with an interval of 1 second
            OneSecTimer.Interval = TimeSpan.FromSeconds(1);
            OneSecTimer.Start();

            WebsiteWorker.DoWork += WebsiteWorker_DoWork;
            WebsiteWorker.RunWorkerCompleted += WebsiteWorker_RunWorkerCompleted;

            UpdateTimer = new DispatcherTimer();
            UpdateTimer.Tick += UpdateTimer_Tick;   //the timer for fetching the deals

            TimeLeftPBar.Value = 0;

            this.Height = 350;
            //End init
            Log("Init End", LogMessageType.DebugLog);
            //Initialization of data
            GetSettings();

            Log("Started up ", LogMessageType.Info);
            WebsiteWorker.RunWorkerAsync();
            isInitialized = true;
        }

        void WebsiteWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Log("OzBargain fetched", LogMessageType.DebugLog);
            OzBargainFetched = true;
            if (ToCheck == true)
            {
                Main();
                ToCheck = false;
            }
        }

        void WebsiteWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Log("")
            WebClient client = new WebClient();
            OzBargain = XmlReader.Create(Website);
        }

        void Main()
        {
            SettingsDS.Reset();
            GetAccounts();
            Checker();
        }

        async void Checker()
        {
            OzBargainFetched = false;
            StreamReader Reader = new StreamReader(Stuff.MyStartupLocation() + @"\LastDeal.txt");
            string LastDeal = Reader.ReadLine();

            if (LastDeal == null)                   // So that it wont send random emails
                LastDeal = " BlahBlahBlah";

            bool Repeated = false; // becomes true when the lastdeal item is reached, so that there are not duplicates

            var reader = OzBargain;
            var feed = SyndicationFeed.Load(reader);
            List<OzBargainSaleItem> Items = new List<OzBargainSaleItem>();
            foreach (SyndicationItem i in feed.Items)
            {
                if (i.Links[0].Uri.ToString().ToUpper() == LastDeal.ToUpper())
                    Repeated = true;    //trigger
                else
                {
                    OzBargainSaleItem Item = new OzBargainSaleItem(i.Title.Text, i.Links[0].Uri.ToString());
                    Items.Add(Item);
                }
            }
            Reader.Close();

            if (Items.Count != 0)
            {
                LastDeal = Items[0].Url;
                StreamWriter Writer = new StreamWriter(Stuff.MyStartupLocation() + @"\LastDeal.txt");
                Writer.WriteLine(LastDeal);
                Writer.Close();

                foreach (OzBargainSaleItem Item in Items)
                {
                    string Title = Item.Title.ToUpper();
                    foreach (User user in Users)
                        foreach (string tag in user.Tags)
                        {
                            string Tag = tag.ToUpper();
                            if (Title.Contains(Tag))
                            {                       //if contains a tag
                                user.DealsForUser.Add(Item);
                                break;              //break if found one tag(same item can have two tags, so break here)
                            }
                        }

                    foreach (User user in Users)
                    {
                        if (user.DealsForUser.Count != 0)
                            SendEmail(user);
                    }
                }
            }
        }

        void Log(string LogMessage, LogMessageType MsgType)
        {
            bool ShowTime = false;
            Paragraph Msg = new Paragraph();
            Msg.LineHeight = 0.5;
            string Message = LogMessage;
            switch (MsgType)
            {
                // make sure to make it show color only if the Debug setting is on 2 
                case LogMessageType.DebugLog:
                    Message = "[Debug] " + Message;
                    Msg.Foreground = Brushes.MediumBlue;
                    break;
                case LogMessageType.Message:
                    break;
                case LogMessageType.Info:
                    Message = "[Info] " + Message;
                    Msg.Foreground = Brushes.Blue;
                    break;
                case LogMessageType.Warning:
                    Message = "[Warning] " + Message;
                    Msg.Foreground = Brushes.Goldenrod;
                    break;
                case LogMessageType.Error:
                    Message = "[Error] " + Message;
                    Msg.Foreground = Brushes.DarkRed;
                    break;
                default:
                    break;
            }
            if (ShowTime)
                Message = string.Format("[{0}] {1}", DateTime.Now, Message);
            Msg.Inlines.Add(Message);
            Log_box.Document.Blocks.Add(Msg);

            StreamWriter Writer = new StreamWriter(Stuff.MyStartupLocation() + @"\Log.txt");
            TextRange Text = new TextRange(Log_box.Document.ContentStart, Log_box.Document.ContentEnd);
            Writer.Write(Text.Text);
            Writer.Close();
        }

        void GetSettings()
        {       //2Add: Make the settings file an xml
            Log("Get Settings begin", LogMessageType.DebugLog);
            StreamReader Reader = new StreamReader(Stuff.MyStartupLocation() + @"\Settings.txt");
            Email = Reader.ReadLine();
            Password = Reader.ReadLine();
            Interval = int.Parse(Reader.ReadLine());
            Website = Reader.ReadLine();
            TimeLeftPBar.Maximum = Interval;
            UpdateTimer.Interval = TimeSpan.FromSeconds(Interval);
            RefreshRateNUD.Value = Interval;
            Log("Get Settings end", LogMessageType.DebugLog);
        }

        void SaveSettings()
        {
            if (isInitialized)
            {
                StreamWriter writer = new StreamWriter(Stuff.MyStartupLocation() + @"\Settings.txt");
                writer.WriteLine(Email);
                writer.WriteLine(Password);
                writer.WriteLine(Interval);
                writer.WriteLine(Website);
                writer.Close();
            }
        }

        void SendEmail(User ToUser)
        {
            try
            {
                Log("Trying to send an email to " + ToUser.Email, LogMessageType.DebugLog);
                string Subject = "", Body = "";
                foreach (OzBargainSaleItem Item in ToUser.DealsForUser)
                    Body += "\r\n" + Item.Title + "\r\n" + Item.Url + "\r\n";

                Subject = "Deals at OzBargain "; //2Add: Add the tags that trigged this email

                MailMessage Msg = new MailMessage();
                Msg.To.Add(ToUser.Email);
                MailAddress Address = new MailAddress(Email);
                Msg.From = Address;
                Msg.Subject = Subject;

                var smtp = new SmtpClient   //2Add : Load most of the details from the settings file
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(Email, Password)
                };

                smtp.Send(Msg);
                Log(string.Format("Sent Email to: {0}, email topic: {1}, email text: {2}", ToUser.Email, Subject, Body), LogMessageType.Info);

                ToUser.DealsForUser.Clear();

            }
            catch (Exception ex)
            {
                Log("Unable to send an email", LogMessageType.Error);
            }
        }
        void GetAccounts()
        {
            Log("Loading accounts...", LogMessageType.DebugLog);
            XmlDocument Doc = new XmlDocument();
            Doc.Load(Stuff.MyStartupLocation() + @"\Accounts.xml");
            Settings = XmlReader.Create(Stuff.MyStartupLocation() + @"\Accounts.xml", new XmlReaderSettings());
            Settings.Read();
            SettingsDS.ReadXml(Settings);
            SettingsDataGrid.ItemsSource = SettingsDS.Tables[0].DefaultView;
            foreach (XmlNode Node in Doc.DocumentElement)
            {
                if (Node.Name == "Account")
                {
                    string Email = "";
                    List<string> Tags = new List<string>();
                    foreach (XmlNode Child in Node.ChildNodes)
                    {
                        switch (Child.Name)
                        {
                            case "Email":
                                Email = Child.InnerText;
                                break;
                            default:
                                Log("Something Weird happened", LogMessageType.Warning);
                                break;
                            case "Tags":
                                foreach (XmlNode SuperChild in Child.ChildNodes)
                                {
                                    Tags.Add(SuperChild.InnerText);
                                }
                                break;
                        }
                    }
                    string TagsLoaded = "";
                    foreach (string Tag in Tags)
                        TagsLoaded += " [" + Tag + "] ";
                    Log("Loaded account: " + Email + "  Tags: " + TagsLoaded, LogMessageType.DebugLog);
                    User user = new User(Email, Tags.ToArray());
                    Users.Add(user);
                }
            }
            Log("Accounts loaded", LogMessageType.DebugLog);
        }

        void OneSecTimer_Tick(object sender, EventArgs e)
        {
            if (isRunning)
            {
                TimeLeftPBar.Value++;
                if (TimeLeftPBar.Value == TimeLeftPBar.Maximum - 8)
                {
                    WebsiteWorker.RunWorkerAsync();
                }
            }
        }

        void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (OzBargainFetched)
            {
                TimeLeftPBar.Value = 0;
                Main();
            }
            else
            {
                ToCheck = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) //Update Account Info
        {
            Settings.Close();
            try
            {
                SettingsDS.WriteXml(Stuff.MyStartupLocation() + @"\Accounts.xml");
                Log("Updated the account info", LogMessageType.DebugLog);
            }
            catch (Exception ex)
            {
                Log("Couldn't update the account info", LogMessageType.Error);
            }
            SettingsDS.Reset();
            GetAccounts();
        }

        private void StartStop_Click(object sender, RoutedEventArgs e) //Start/Stop
        {
            isRunning = !isRunning;
            if (isRunning)
            {
                Status_Label.Text = "Running";
                Status_Label.Foreground = Brushes.Green;
                StartStopBtn.Content = "Stop";
                TimeLeftPBar.Value = 0;
            }
            else
            {
                Status_Label.Text = "Stopped";
                Status_Label.Foreground = Brushes.Red;
                StartStopBtn.Content = "Start";
            }
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (RefreshRateNUD.Value >= 6)
            {
                UpdateTimer.Stop();
                UpdateTimer.Interval = TimeSpan.FromSeconds((double)RefreshRateNUD.Value);
                Interval = (int)RefreshRateNUD.Value;
                UpdateTimer.Start();
                TimeLeftPBar.Maximum = (double)RefreshRateNUD.Value;
                TimeLeftPBar.Value = 0;
                SaveSettings();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Advanced = !Advanced;
            if (Advanced)
                this.Height = this.MaxHeight;
            else
                this.Height -= 200;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
        }



    }
}

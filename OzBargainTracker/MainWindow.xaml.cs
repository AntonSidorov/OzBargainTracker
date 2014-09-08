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
        XmlReader Accounts, OzBargain;
        XmlDocument Settings;
        DispatcherTimer UpdateTimer, OneSecTimer;
        bool isRunning = false, Advanced = false, isInitialized = false, ShowTime = false;
        string Email = "", Password = "", Website = "", SMTPHost = "smtp.gmail.com", Mode = "", EmailSubject = "Deals at OzBargain";
        DataSet AccountsDS = new DataSet();
        List<User> Users = new List<User>();
        int Interval = 0, Port = 587;
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

            Settings = new XmlDocument();

            OneSecTimer = new DispatcherTimer();    //A timer for steps in the time left progressbar
            OneSecTimer.Tick += OneSecTimer_Tick;   //with an interval of 1 second
            OneSecTimer.Interval = TimeSpan.FromSeconds(1);
            OneSecTimer.Start();


            UpdateTimer = new DispatcherTimer();
            UpdateTimer.Tick += UpdateTimer_Tick;   //the timer for fetching the deals

            TimeLeftPBar.Value = 0;

            this.Height = 350;
            //End init
            Log("Init End", LogMessageType.DebugLog);
            //Initialization of data
            LoadSettings();

            Task t = Task.Run(() =>
                {
                    Fetch();
                }
                );
            Log("Started up ", LogMessageType.Info);
        }

        async void Fetch()
        {
            try
            {
                string OzBarg = await FetchOzBargain();
                OzBargain = XmlReader.Create(new StringReader(OzBarg));
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    Log("Failed to fetch Ozbargain " + ex.Message, LogMessageType.Error);
                }));
            }
        }

        async Task<string> FetchOzBargain()
        {

            this.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    Log("Started Fetching OzBargain...", LogMessageType.DebugLog);
                }));
            try
            {
                HttpClient client = new HttpClient();
                string WebsiteContent = await client.GetStringAsync(Website);
                this.Dispatcher.Invoke(DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        Log("OzBargain Fetched", LogMessageType.DebugLog);
                    }));
                return WebsiteContent;
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(DispatcherPriority.Normal,
                                new Action(() =>
                                {
                                    Log("Unable to fetch OzBargain", LogMessageType.Error);
                                }));
                return "";
            }

        }

        void Main()
        {
            AccountsDS.Reset();
            LoadAccounts();
            Checker();
        }

        void Checker()
        {
            StreamReader Reader = new StreamReader(Stuff.MyStartupLocation() + @"\LastDeal.txt");
            string LastDeal = Reader.ReadLine();

            if (LastDeal == null)                   // So that it wont send random emails
                LastDeal = " BlahBlahBlah";

            bool Repeated = false; // becomes true when the lastdeal item is reached, so that there are no duplicates

            var reader = OzBargain;
            var feed = SyndicationFeed.Load(reader);
            List<OzBargainSaleItem> Items = new List<OzBargainSaleItem>();
            foreach (SyndicationItem i in feed.Items)
            {
                if (i.Links[0].Uri.ToString().ToUpper() == LastDeal.ToUpper())
                    Repeated = true;    //trigger
                if (i.Links[0].Uri.ToString().ToUpper() != LastDeal.ToUpper() && !Repeated)
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
                }
                foreach (User user in Users)
                {
                    if (user.DealsForUser.Count != 0)
                        SendEmail(user);
                }
            }
        }

        void Log(string LogMessage, LogMessageType MsgType)
        {
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
            if (MsgType == LogMessageType.DebugLog && Mode != "Debug")
            { }
            else
                Log_box.Document.Blocks.Add(Msg);

            StreamWriter Writer = new StreamWriter(Stuff.MyStartupLocation() + @"\Log.txt");
            TextRange Text = new TextRange(Log_box.Document.ContentStart, Log_box.Document.ContentEnd);
            Writer.Write(Text.Text);
            Writer.Close();
        }

        void LoadSettings()
        {
            Log("Loading Settings...", LogMessageType.DebugLog);

            Settings.Load(Stuff.MyStartupLocation() + @"\Settings.xml");

            foreach (XmlNode Node in Settings.DocumentElement.ChildNodes)
            {
                switch (Node.Name)
                {
                    case "Email":
                        Email = Node.InnerText;
                        break;
                    case "Password":
                        Password = Node.InnerText;
                        break;
                    case "Port":
                        Port = int.Parse(Node.InnerText);
                        break;
                    case "SMTPHost":
                        SMTPHost = Node.InnerText;
                        break;
                    case "Website":
                        Website = Node.InnerText;
                        break;
                    case "Interval":
                        Interval = int.Parse(Node.InnerText);
                        break;
                    case "Mode":
                        Mode = Node.InnerText;
                        break;
                    case "ShowTimeInLog":
                        ShowTime = bool.Parse(Node.InnerText);
                        break;
                    case "Subject":
                        EmailSubject = Node.InnerText;
                        break;
                    default:
                        Log("Unknown Node detected in Settings.xml: " + Node.Name, LogMessageType.DebugLog);
                        break;
                }
            }
            TimeLeftPBar.Maximum = Interval;
            UpdateTimer.Interval = TimeSpan.FromSeconds(Interval);
            RefreshRateNUD.Value = Interval;
            Log("Settings Loaded", LogMessageType.DebugLog);
        }

        void SaveSettings()
        {
            if (isInitialized)
            {
                foreach (XmlNode Child in Settings.DocumentElement.ChildNodes)
                    if (Child.Name == "Interval")
                        Child.InnerText = Interval.ToString();
                Settings.Save(Stuff.MyStartupLocation() + @"\Settings.xml");

            }
        }

        void SendEmail(User ToUser)
        {
            LoadSettings();
            try
            {
                Log("Trying to send an email to " + ToUser.Email, LogMessageType.DebugLog);
                string Body = "", Subject = EmailSubject; //2Add: Add the tags that trigged this email
                foreach (OzBargainSaleItem Item in ToUser.DealsForUser)
                {
                    Body += "\r\n" + Item.Title + "\r\n" + Item.Url + "\r\n";
                    foreach (string Tag in ToUser.Tags)
                        if (Item.Title.ToUpper().Contains(Tag.ToUpper()))
                            Subject += "[" + Tag + ']';
                }
                MailMessage Msg = new MailMessage();
                Msg.To.Add(ToUser.Email);
                MailAddress Address = new MailAddress(Email);
                Msg.From = Address;
                Msg.Subject = Subject;
                Msg.Body = Body;

                var smtp = new SmtpClient   //2Add : Load most of the details from the settings file
                {
                    Host = SMTPHost,
                    Port = Port,
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
                Log("Unable to send an email  " + ex.Message, LogMessageType.Error);
            }
        }
        void LoadAccounts()
        {
            Users.Clear();
            Log("Loading accounts...", LogMessageType.DebugLog);
            XmlDocument Doc = new XmlDocument();
            Doc.Load(Stuff.MyStartupLocation() + @"\Accounts.xml");
            Accounts = XmlReader.Create(Stuff.MyStartupLocation() + @"\Accounts.xml", new XmlReaderSettings());
            Accounts.Read();
            AccountsDS.ReadXml(Accounts);
            AccountsDataGrid.ItemsSource = AccountsDS.Tables[0].DefaultView;
            foreach (XmlNode Node in Doc.DocumentElement)
            {
                if (Node.Name == "Account")
                {
                    string Email = "";
                    string TagsLoaded = "";
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
                                TagsLoaded = Child.InnerText;
                                break;
                        }
                    }
                    if (Email != "")
                    {
                        string[] Splitter = { " " };
                        string[] Tags = TagsLoaded.Split(Splitter, StringSplitOptions.None);
                        Log("Loaded account: " + Email + "  Tags: " + TagsLoaded, LogMessageType.DebugLog);
                        User user = new User(Email, Tags);
                        Users.Add(user);
                    }
                }
            }
            Log("Accounts loaded", LogMessageType.DebugLog);
        }

        async void OneSecTimer_Tick(object sender, EventArgs e)
        {
            if (!isInitialized)
                if (OzBargain != null)
                {
                    isInitialized = true;
                    StartStop_Click(null, new RoutedEventArgs());
                    await Task.Delay(15000);
                    UpdateTimer_Tick(null, null);
                }
            if (isRunning)
                TimeLeftPBar.Value++;
        }

        async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            TimeLeftPBar.Value = 0;
            Task<string> Fetch = FetchOzBargain();
            await Fetch;
            OzBargain = XmlReader.Create(new StringReader(Fetch.Result));
            Main();
        }

        private void Button_Click(object sender, RoutedEventArgs e) //Update Account Info
        {
            Accounts.Close();
            try
            {
                AccountsDS.WriteXml(Stuff.MyStartupLocation() + @"\Accounts.xml");
                Log("Updated the account info", LogMessageType.DebugLog);
            }
            catch (Exception ex)
            {
                Log("Couldn't update the account info", LogMessageType.Error);
            }
            AccountsDS.Reset();
            LoadAccounts();
        }

        private void StartStop_Click(object sender, RoutedEventArgs e) //Start/Stop
        {
            isRunning = !isRunning;
            if (isRunning)
            {
                Status_Label.Text = "Running";
                Status_Label.Foreground = Brushes.Green;
                StartStopBtn.Content = "Stop";
                UpdateTimer.Start();
            }
            else
            {
                Status_Label.Text = "Stopped";
                Status_Label.Foreground = Brushes.Red;
                StartStopBtn.Content = "Start";
                TimeLeftPBar.Value = 0;
                UpdateTimer.Stop();
            }
        }

        private void NumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (RefreshRateNUD.Value >= 6)
            {
                if (isRunning)
                    UpdateTimer.Stop();
                UpdateTimer.Interval = TimeSpan.FromSeconds((double)RefreshRateNUD.Value);
                Interval = (int)RefreshRateNUD.Value;
                if (isRunning)
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

        private void Log_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            Log_box.ScrollToEnd();
        }




    }
}

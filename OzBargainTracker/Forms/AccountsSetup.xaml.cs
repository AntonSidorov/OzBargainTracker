using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace OzBargainTracker
{
    /// <summary>
    /// Interaction logic for AccountsSetup.xaml
    /// </summary>
    public partial class AccountsSetup : MetroWindow
    {
        TrackerMain MainForm;
        /// <summary>
        /// List of users in Accounts.xml
        /// </summary>
        public List<User> Users;
        XmlDocument AccountsDoc;
        DataSet AccountsDS;
        /// <summary>
        /// Default Constructor for this Class
        /// </summary>
        /// <param name="MainForm">Reference to the main form(used for Logs, Settings)</param>
        public AccountsSetup(TrackerMain MainForm)
        {
            this.InitializeComponent();
            this.MainForm = MainForm;
            Users = new List<User>();
            AccountsDoc = new XmlDocument();
            AccountsDS = new DataSet();
            InitializeComponent();
            Icon = BitmapFrame.Create(new Uri(Utils.StartupLocation + @"\Icon.png"));
        }

        /// <summary>
        /// Reloads the Accounts of the OzBargainTracker
        /// </summary>
        /// <param name="AccountsXMLDoc">Location of the Accounts.xml</param>
        public void Reload(string AccountsXMLDoc)
        {
            //Clear the users so there are no duplicates
            Users.Clear();
            MainForm.Log("Loading Accounts...", LogMessageType.DebugLog);
            //Add handling
            try
            {//Possible Problems: Issues with the XmlDocument (in use/corrupted/not found)
                AccountsDoc.Load(Utils.StartupLocation + @"\Accounts.xml");
                AccountsDS.Reset();
                AccountsDS.ReadXml(AccountsDoc.ToXMLReader());
                AccountsDataGrid.ItemsSource = AccountsDS.Tables[0].DefaultView;

                //Load in the accounts into Users
                foreach (XmlNode Node in AccountsDoc.DocumentElement)
                {
                    if (Node.Name == "Account")
                    {
                        string Email = "", TagsLoaded = "";
                        foreach (XmlNode Child in Node.ChildNodes)
                        {
                            switch (Child.Name)
                            {
                                case "Email":
                                    Email = Child.InnerText;
                                    break;
                                case "Tags":
                                    TagsLoaded = Child.InnerText;
                                    break;
                                default:
                                    MainForm.Log("Unexpected node: {0}", Child.Name, LogMessageType.DebugLog);
                                    break;
                            }
                        }
                        if (Email != "")
                        {
                            string[] Tags = TagsLoaded.Split(' ');
                            MainForm.Log("Loaded Account: {0}, Tags: {1}.", Email, TagsLoaded, LogMessageType.DebugLog);
                            User user = new User(Email, TagsLoaded);
                            Users.Add(user);
                        }
                    }
                }
                MainForm.Log("Accounts Loaded", LogMessageType.DebugLog);
            }
            catch (Exception ex)
            {
                MainForm.Log("Unable to load accounts, default accounts loaded and saved to Accconts.xml, please update the info.", LogMessageType.HandledError);
                //Resets the DataSet, then loads an Xml from a string, then reading it into the dataset and reloading the datagrid
                AccountsDS.Reset();
                AccountsDoc.LoadXml("<Accounts>\r\n  <Account>\r\n    <Email>Someone@example.com</Email>\r\n    <Tags>Please insert your tags here with a space in between each</Tags>\r\n  </Account>\r\n</Accounts>");
                AccountsDS.ReadXml(AccountsDoc.ToXMLReader());
                AccountsDataGrid.ItemsSource = AccountsDS.Tables[0].DefaultView;
                Save();
                MainForm.LogException(ex);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {//Hides the window instead of actually closing it (lowers the loading time of the window each time its reopened)
            e.Cancel = true;
            this.Visibility = System.Windows.Visibility.Hidden;
        }

        //Update the Accounts.xml and reload
        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }
        /// <summary>
        /// The Method that saves the Accounts from the datagrid of this form to Accounts.xml
        /// </summary>
        public void Save()
        {
            try
            {//Possible Problems: Issues with the file, such as: File being used 
                AccountsDS.WriteXml(Utils.StartupLocation + @"\Accounts.xml");
                MainForm.Log("Account info Updated", LogMessageType.DebugLog);
            }
            catch (Exception ex)
            {
                MainForm.Log("Unable to save the default accounts.xml, please close all windows with it and retry.", LogMessageType.HandledError);
                MainForm.LogException(ex);
                //Add error handling 
            }
            Reload(Utils.StartupLocation + @"\Accounts.xml");
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
#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Xml;
using OzBargainTracker.Classes;

#endregion

namespace OzBargainTracker.Forms
{
    public partial class AccountsSetup
    {
        private readonly XmlDocument _accountsDoc;
        private readonly DataSet _accountsDs;
        private readonly TrackerMain _mainForm;
        public List<User> Users;

        public AccountsSetup(TrackerMain mainForm)
        {
            InitializeComponent();
            _mainForm = mainForm;
            Users = new List<User>();
            _accountsDoc = new XmlDocument();
            _accountsDs = new DataSet();
            InitializeComponent();
            Icon = Properties.Resources.IconPng.ToBitmapImage();
        }

        public void Reload(string accountsXmlDoc)
        {
            Users.Clear();
            try
            {
                _accountsDoc.Load(Utils.StartupLocation + @"\Accounts.xml");
                _accountsDs.Reset();
                _accountsDs.ReadXml(_accountsDoc.ToXmlReader());
                AccountsDataGrid.ItemsSource = _accountsDs.Tables[0].DefaultView;
                foreach (XmlNode node in _accountsDoc.DocumentElement)
                    if (node.Name == "Account")
                    {
                        string email = "", tagsLoaded = "";
                        foreach (XmlNode child in node.ChildNodes)
                            switch (child.Name)
                            {
                                case "Email":
                                    email = child.InnerText;
                                    break;
                                case "Tags":
                                    tagsLoaded = child.InnerText;
                                    break;
                                default:
                                    break;
                            }
                        if (email != "")
                        {
                            var tags = tagsLoaded.Split(' ');
                            var user = new User(email, tagsLoaded);
                            Users.Add(user);
                        }
                    }
            }
            catch (Exception ex)
            {
                _accountsDs.Reset();
                _accountsDoc.LoadXml(
                    "<Accounts>\r\n  <Account>\r\n    <Email>Someone@example.com</Email>\r\n    <Tags>Please insert your tags here with a space in between each</Tags>\r\n  </Account>\r\n</Accounts>");
                _accountsDs.ReadXml(_accountsDoc.ToXmlReader());
                AccountsDataGrid.ItemsSource = _accountsDs.Tables[0].DefaultView;
                Save();
                _mainForm.LogException(ex);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        public void Save()
        {
            try
            {
                _accountsDs.WriteXml(Utils.StartupLocation + @"\Accounts.xml");
            }
            catch (Exception ex)
            {
                _mainForm.LogException(ex);
            }
            Reload(Utils.StartupLocation + @"\Accounts.xml");
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
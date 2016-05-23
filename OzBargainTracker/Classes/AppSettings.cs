#region

using System;
using System.IO;
using System.Xml;

#endregion

namespace OzBargainTracker
{
    public class AppSettings
    {
        private readonly TrackerMain _mainForm;
        private XmlDocument _settingsDocument = new XmlDocument();
        public string Email, Password, Website, SmtpHost, EmailSubject;
        public int Interval, Port;
        public bool LogShowTime, Debug;

        public AppSettings(TrackerMain mainForm)
        {
            _mainForm = mainForm;
            Debug = false;
            Interval = 60;
            LogShowTime = false;
            Website = "http://www.ozbargain.com.au/feed";
            Email = "OzBargainTracker@gmail.com";
            EmailSubject = "Deals at OzBargain";
            Password = "TrackOzBargain";
            Port = 587;
            SmtpHost = "smtp.gmail.com";
        }

        public AppSettings(TrackerMain mainForm, string settingsLocation)
        {
            _mainForm = mainForm;
            Load(settingsLocation);
        }

        public void Load(string settingsLocation)
        {
            int loadedElements = 0, totalElements = 8;
            var exceptionCaught = false;
            try
            {
                _settingsDocument.Load(settingsLocation);
                foreach (XmlNode node in _settingsDocument.DocumentElement.ChildNodes)
                    switch (node.Name.ToLower())
                    {
                        case "email":
                            Email = node.InnerText;
                            loadedElements++;
                            break;
                        case "website":
                            Website = node.InnerText;
                            loadedElements++;
                            break;
                        case "password":
                            Password = node.InnerText;
                            loadedElements++;
                            break;
                        case "smtphost":
                            SmtpHost = node.InnerText;
                            loadedElements++;
                            break;
                        case "emailsubject":
                            EmailSubject = node.InnerText;
                            loadedElements++;
                            break;
                        case "port":
                            Port = int.Parse(node.InnerText);
                            loadedElements++;
                            break;
                        case "logshowtime":
                            LogShowTime = bool.Parse(node.InnerText);
                            loadedElements++;
                            break;
                        case "interval":
                            Interval = int.Parse(node.InnerText);
                            loadedElements++;
                            break;
                        case "debug":
                            Debug = true;
                            break;
                        default:
                            break;
                    }
            }
            catch (FileNotFoundException ex)
            {
                exceptionCaught = true;
            }
            catch (Exception ex)
            {
//Look out for these
                exceptionCaught = true;
            }
            if (exceptionCaught)
            {
                Debug = false;
                Interval = 60;
                LogShowTime = false;
                Website = "http://www.ozbargain.com.au/feed";
                Email = "OzBargainTracker@gmail.com";
                EmailSubject = "Deals at OzBargain";
                Password = "TrackOzBargain";
                Port = 587;
                SmtpHost = "smtp.gmail.com";
                Save();
            }
        }

        public void Save()
        {
            _settingsDocument = new XmlDocument();
            XmlNode rootNode = _settingsDocument.CreateElement("Settings");
            _settingsDocument.AppendChild(rootNode);

            XmlNode node2Add;
            if (Debug)
            {
                node2Add = _settingsDocument.CreateElement("Debug");
                node2Add.InnerText = "true";
                rootNode.AppendChild(node2Add);
            }
            node2Add = _settingsDocument.CreateElement("Interval");
            node2Add.InnerText = Interval.ToString();
            rootNode.AppendChild(node2Add);

            node2Add = _settingsDocument.CreateElement("LogShowTime");
            node2Add.InnerText = LogShowTime.ToString();
            rootNode.AppendChild(node2Add);

            node2Add = _settingsDocument.CreateElement("SMTPHost");
            node2Add.InnerText = SmtpHost;
            rootNode.AppendChild(node2Add);

            node2Add = _settingsDocument.CreateElement("Website");
            node2Add.InnerText = Website;
            rootNode.AppendChild(node2Add);

            node2Add = _settingsDocument.CreateElement("Email");
            node2Add.InnerText = Email;
            rootNode.AppendChild(node2Add);

            node2Add = _settingsDocument.CreateElement("EmailSubject");
            node2Add.InnerText = EmailSubject;
            rootNode.AppendChild(node2Add);

            node2Add = _settingsDocument.CreateElement("Password");
            node2Add.InnerText = Password;
            rootNode.AppendChild(node2Add);

            node2Add = _settingsDocument.CreateElement("Port");
            node2Add.InnerText = Port.ToString();
            rootNode.AppendChild(node2Add);

            try
            {
                _settingsDocument.Save(Utils.StartupLocation + @"\Settings.xml");
            }
            catch (Exception ex)
            {
                _mainForm.LogException(ex);
            }
        }
    }
}
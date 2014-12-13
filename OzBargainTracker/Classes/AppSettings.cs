using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;

namespace OzBargainTracker
{
    /// <summary>
    /// Class for the settings of the tracker
    /// </summary>
    public class AppSettings
    {
        public bool LogShowTime, Debug;
        public string Email, Password, Website, SMTPHost, EmailSubject;
        public int Interval, Port;
        XmlDocument SettingsDocument = new XmlDocument();
        TrackerMain MainForm;
        
        /// <summary>
        /// Default Constructor for TrackerSettings Class
        /// </summary>
        public AppSettings(TrackerMain MainForm)
        {
            this.MainForm = MainForm;
            Debug = true;
            Interval = 60;         
            LogShowTime = false;
            Website = "http://www.ozbargain.com.au/feed";
            Email = "OzBargainTracker@gmail.com";
            EmailSubject = "Deals at OzBargain";
            Password = "TrackOzBargain";
            Port = 587;
            SMTPHost = "smtp.gmail.com";
        }
        /// <summary>
        /// Constructor which loads the settings from a special file
        /// </summary>
        /// <param name="SettingsLocation">The location of the settings</param>
        public AppSettings(TrackerMain MainForm, string SettingsLocation)
        {
            this.MainForm = MainForm;
            Load(SettingsLocation);
        }
        /// <summary>
        /// Loads the Settings from the path given
        /// </summary>
        /// <param name="SettingsLocation">Path used to load the settings</param>
        public void Load (string SettingsLocation)
        {
            int LoadedElements = 0, TotalElements = 8;
            bool ExceptionCaught = false;
            try
            {//Possible problems: File not found/in use/file not formatted properly(not xml)
                SettingsDocument.Load(SettingsLocation);
                // Get the settings from the XML here
                foreach (XmlNode Node in SettingsDocument.DocumentElement.ChildNodes)
                {
                    switch (Node.Name.ToLower())
                    {
                        case "email":
                            Email = Node.InnerText;
                            LoadedElements++;                            
                            break;
                        case "website":
                            Website = Node.InnerText;
                            LoadedElements++;
                            break;
                        case "password":
                            Password = Node.InnerText;
                            LoadedElements++;
                            break;
                        case "smtphost":
                            SMTPHost = Node.InnerText;
                            LoadedElements++;
                            break;
                        case "emailsubject":
                            EmailSubject = Node.InnerText;
                            LoadedElements++;
                            break;
                        case "port":
                            Port = int.Parse(Node.InnerText);
                            LoadedElements++;
                            break;
                        case "logshowtime":
                            LogShowTime = bool.Parse(Node.InnerText);
                            LoadedElements++;
                            break;
                        case "interval":
                            Interval = int.Parse(Node.InnerText);
                            LoadedElements++;
                            break;
                        case "debug":
                            Debug = true;
                            break;
                        default:
                            break;
                    }
                }
            }
            catch(FileNotFoundException ex)
            {
                MainForm.Log("Settings.xml not found, loading default settings.", LogMessageType.HandledError);
                MainForm.Log("Default Settings have been saved to Settings.xml");
                MainForm.LogException(ex);
                ExceptionCaught = true;
            }
            catch (Exception ex)
            {//Look out for these
                MainForm.Log("Unknown Exception has occured, loading default settings.", LogMessageType.HandledError);
                MainForm.Log("Default Settings have been saved to Settings.xml");
                MainForm.LogException(ex);
                ExceptionCaught = true;
            }
            if (ExceptionCaught)
            {
                Debug = false;
                Interval = 60;
                LogShowTime = false;
                Website = "http://www.ozbargain.com.au/feed";
                Email = "OzBargainTracker@gmail.com";
                EmailSubject = "Deals at OzBargain";
                Password = "TrackOzBargain";
                Port = 587;
                SMTPHost = "smtp.gmail.com";
                Save();
                MainForm.Log("Unable to load settings, default settings loaded and saved to Settings.xml");
            }
            else
            {
                //Check if all of the settings have been loaded
                if(LoadedElements == TotalElements)
                    MainForm.Log("All 8 Elements from the xml have been loaded", LogMessageType.DebugLog);
                if (LoadedElements < TotalElements)
                    MainForm.Log("Some of the settings in the XML were not set, attempting to continue...", LogMessageType.Warning);
                if (LoadedElements > TotalElements)
                    MainForm.Log("Some of the settings in the XML have been set twice, attempting to continue...", LogMessageType.Warning);
            }
        }
        /// <summary>
        /// Saves all the settings to Settings.Xml
        /// </summary>
        public void Save ()
        {
            //Setup Root
            SettingsDocument = new XmlDocument();
            XmlNode RootNode = SettingsDocument.CreateElement("Settings");
            SettingsDocument.AppendChild(RootNode);

            XmlNode Node2Add;

            //Debug
            if (Debug)
            {
                Node2Add = SettingsDocument.CreateElement("Debug");
                Node2Add.InnerText = "true";
                RootNode.AppendChild(Node2Add);
            }

            //Interval 
            Node2Add = SettingsDocument.CreateElement("Interval");
            Node2Add.InnerText = Interval.ToString();
            RootNode.AppendChild(Node2Add);

            //LogShowTime 
            Node2Add = SettingsDocument.CreateElement("LogShowTime");
            Node2Add.InnerText = LogShowTime.ToString();
            RootNode.AppendChild(Node2Add);

            //SMTPHost 
            Node2Add = SettingsDocument.CreateElement("SMTPHost");
            Node2Add.InnerText = SMTPHost;
            RootNode.AppendChild(Node2Add);

            //Website 
            Node2Add = SettingsDocument.CreateElement("Website");
            Node2Add.InnerText = Website;
            RootNode.AppendChild(Node2Add);

            //Email 
            Node2Add = SettingsDocument.CreateElement("Email");
            Node2Add.InnerText = Email;
            RootNode.AppendChild(Node2Add);

            //EmailSubject
            Node2Add = SettingsDocument.CreateElement("EmailSubject");
            Node2Add.InnerText = EmailSubject;
            RootNode.AppendChild(Node2Add);

            //Password 
            Node2Add = SettingsDocument.CreateElement("Password");
            Node2Add.InnerText = Password;
            RootNode.AppendChild(Node2Add);

            //Port 
            Node2Add = SettingsDocument.CreateElement("Port");
            Node2Add.InnerText = Port.ToString();
            RootNode.AppendChild(Node2Add);

            try
            {//Possible problems: File in use
                SettingsDocument.Save(Utils.StartupLocation + @"\Settings.xml");
            }
            catch(Exception ex)
            {
                MainForm.Log("Unable to save the settings, retrying next time settings are changed.", LogMessageType.UnHandledError);
                MainForm.LogException(ex);
            }
        }
    }
}

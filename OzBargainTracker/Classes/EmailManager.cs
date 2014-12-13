using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;
using System.Windows.Threading;
using System.Timers;

namespace OzBargainTracker
{
    /// <summary>
    /// Class representing the separate threaded worker that sends emails when they are added to the Queue
    /// </summary>
    public class EmailManager
    {
        /// <summary>
        /// Queue used for the emails
        /// </summary>
        public ConcurrentQueue<User> Requests;
        /// <summary>
        /// MainForm
        /// </summary>
        TrackerMain MainForm;
        /// <summary>
        /// Timer that checks if there's anything in the Queue and if there is activates the process request method 
        /// </summary>
        Timer _Timer = new Timer();
        /// <summary>
        /// Default constructor for the class
        /// </summary>
        /// <param name="MainForm">Reference to the MainForm(for Logs and settings)</param>
        public EmailManager(TrackerMain MainForm)
        {
            this.MainForm = MainForm;
            Requests = new ConcurrentQueue<User>();
            _Timer.Interval = 15000;
            _Timer.Elapsed += Timer_Tick;
            _Timer.Start();
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            if (Requests.Count > 0)
            {
                new Task(() =>
                {
                    User Req;
                    bool Success = Requests.TryDequeue(out Req);
                    if (Success)
                        ProcessRequest(Req);
                }).Start();
            }
        }
        /// <summary>
        /// Method that processes the send email request
        /// </summary>
        /// <param name="_User">The user the email will be sent to</param>
        void ProcessRequest(User _User)
        {
            //Build the message
            MainForm.Log("Trying to send an email to " + _User.Email, LogMessageType.DebugLog);
            MailMessage Message = new MailMessage();
            MailAddress Address = new MailAddress(MainForm.Settings.Email);
            Message.From = Address;
            Message.To.Add(_User.Email);

            string Body = "", Subject = MainForm.Settings.EmailSubject;
            //Add tags to the email
            foreach (string Tag in _User.TriggerTags)
                Subject += string.Format("[{0}]", Tag);
            //Add the Deals to the email body
            foreach (Deal _Deal in _User.DealsForUser)
                Body += string.Format("\r\n{0}\r\n{1}\r\n", _Deal.Title, _Deal.Url);

            //Assign stuff
            Message.Body = Body;
            Message.Subject = Subject;

            //Create an SMTP Client 
            //?? May this cause any exceptions? as in does the SMTP client creation do something before i ask the client to send a message?
            SmtpClient Client = new SmtpClient
            {
                Host = MainForm.Settings.SMTPHost,
                Port = MainForm.Settings.Port,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(MainForm.Settings.Email, MainForm.Settings.Password)
            };

            try
            {//Possible Problems: No internet connection / Broken Credentials / SMTP Host down 
                Client.Send(Message);
                MainForm.Log("Sent Email to: {0}, Email Subject: {1}", _User.Email, Subject);
            }
            catch (Exception ex)
            {
                MainForm.Log("Unable to send an email", LogMessageType.Warning);
                MainForm.Log("StackTrace: {0}\r\nMessage: {1}\r\nSetup: \r\nEmail: {2}\r\nPassword: {3}\r\nSMTPHost: {4}\r\nSMTPPort: {5}.", LogMessageType.DebugLog,
                                ex.StackTrace, ex.Message, MainForm.Settings.Email, MainForm.Settings.Password, MainForm.Settings.SMTPHost, MainForm.Settings.Port);
            }
            //Cleanup
            _User.TriggerTags.Clear();
            _User.DealsForUser.Clear();

        }
    }
}

#region

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Timers;
using LogWriter;

#endregion

namespace OzBargainTracker.Classes
{
    public class EmailManager
    {
        private readonly TrackerMain _mainForm;
        private readonly Timer _timer = new Timer();
        public ConcurrentQueue<User> Requests;

        public EmailManager(TrackerMain mainForm)
        {
            _mainForm = mainForm;
            Requests = new ConcurrentQueue<User>();
            _timer.Interval = 15000;
            _timer.Elapsed += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Requests.Count > 0)
                new Task(() =>
                {
                    User req;
                    var success = Requests.TryDequeue(out req);
                    if (success)
                        ProcessRequest(req);
                }).Start();
        }

        private void ProcessRequest(User user)
        {
            Logger.CurrentLogger.Log($"Emailing {user.Email} with {user.DealsForUser.Count} Deals.", LogType.DebugLog);
            var message = new MailMessage();
            var address = new MailAddress(_mainForm.Settings.Email);
            message.From = address;
            message.To.Add(user.Email);

            string body = "", subject = _mainForm.Settings.EmailSubject;
            subject = user.TriggerTags.Aggregate(subject, (current, tag) => current + $"[{tag}]");
            body = user.DealsForUser.Aggregate(body, (current, deal) => current + $"\r\n{deal.Title}\r\n{deal.Url}\r\n");

            message.Body = body;
            message.Subject = subject;

            var client = new SmtpClient
            {
                Host = _mainForm.Settings.SmtpHost,
                Port = _mainForm.Settings.Port,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_mainForm.Settings.Email, _mainForm.Settings.Password)
            };

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                Logger.CurrentLogger.Log($"Failed to send a message to {user.Email}", LogType.Error);
                _mainForm.LogException(ex);
            }
            user.TriggerTags.Clear();
            user.DealsForUser.Clear();
        }
    }
}
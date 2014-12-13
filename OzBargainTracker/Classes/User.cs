using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OzBargainTracker
{
    /// <summary>
    /// Class for a user listed in Accounts.xml
    /// </summary>
    public class User
    {
        /// <summary>
        /// User's Email
        /// </summary>
        public string Email;
        /// <summary>
        /// Tags that user has setup as one string, all split by a space
        /// </summary>
        public string Tags;
        /// <summary>
        /// Tags that user has setup in a list
        /// </summary>
        public List<string> TagsSplit = new List<string>();
        /// <summary>
        /// Tags that have been triggered(not processed yet)
        /// </summary>
        public List<string> TriggerTags = new List<string>();
        /// <summary>
        /// Deals that have been found for this user (not processed yet)
        /// </summary>
        public List<Deal> DealsForUser = new List<Deal>();
        /// <summary>
        /// Default constructor for this class
        /// </summary>
        /// <param name="email">User's email</param>
        /// <param name="tags">Tags that user has setup as one string, all split by a space</param>
        public User(string email, string tags)
        {
            Email = email;
            Tags = tags;
            TagsSplit = tags.Split(' ').ToList();
        }
        /// <summary>
        /// Method called when a deal has been found for this user, which adds the tag that has been triggered to TriggerTags and also adds the Deal to the DealsForUser
        /// </summary>
        /// <param name="_Deal"></param>
        public void DealFound(Deal _Deal)
        {
            foreach(string Tag in TagsSplit)
                if (_Deal.Title.ToUpper().Contains(Tag))
                    TriggerTags.Add(Tag);
            DealsForUser.Add(_Deal);
        }
    }
}

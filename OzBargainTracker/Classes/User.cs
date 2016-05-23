#region

using System.Collections.Generic;
using System.Linq;

#endregion

namespace OzBargainTracker.Classes
{
    public class User
    {
        public List<Deal> DealsForUser = new List<Deal>();
        public string Email;
        public string Tags;
        public List<string> TagsSplit;
        public List<string> TriggerTags = new List<string>();

        public User(string email, string tags)
        {
            Email = email;
            Tags = tags;
            TagsSplit = tags.Split(' ').ToList();
        }

        public void DealFound(Deal deal)
        {
            foreach (var tag in TagsSplit)
                if (deal.Title.ToUpper().Contains(tag))
                    TriggerTags.Add(tag);
            DealsForUser.Add(deal);
        }
    }
}
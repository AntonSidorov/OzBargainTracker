using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OzBargainTracker
{
    class Stuff
    {
        public static string MyStartupLocation()
        {
            return System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        }
    }
    class OzBargainSaleItem
    {
        public string Url = "", Title = "";

        public OzBargainSaleItem(string title, string url)
        {
            Url = url; Title = title;
            
        }
    }

    class User
    {
        public string Email = "";
        public string[] Tags = { "" };
        public List<OzBargainSaleItem> DealsForUser = new List<OzBargainSaleItem>();

        public User(string email, string[] tags)
        {
            Email = email; Tags = tags;
        }
    }

}

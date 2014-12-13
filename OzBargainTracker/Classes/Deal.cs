using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OzBargainTracker
{
    /// <summary>
    /// Class representing an OzBargain Deal
    /// </summary>
    public class Deal
    {
        /// <summary>
        /// Url that represents the deal
        /// </summary>
        public string Url;
        /// <summary>
        /// Deal's Title
        /// </summary>
        public string Title;
        /// <summary>
        /// Default constructor for this class
        /// </summary>
        /// <param name="url">Url that represents the deal</param>
        /// <param name="title">Deal's Title</param>
        public Deal(string url, string title)
        {
            Url = url; Title = title;
        }
    }
}

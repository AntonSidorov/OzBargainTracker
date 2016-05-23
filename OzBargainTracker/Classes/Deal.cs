namespace OzBargainTracker.Classes
{
    public class Deal
    {
        public string Title;
        public string Url;

        public Deal(string url, string title)
        {
            Url = url;
            Title = title;
        }
    }
}
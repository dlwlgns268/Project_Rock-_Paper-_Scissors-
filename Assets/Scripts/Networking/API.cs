namespace Networking
{
    public static class API
    {
        public static Networking.Get<Void> Log(string message)
        {
            return new Networking.Get<Void>($"/debug/log?message={message}");
        }
    }
}
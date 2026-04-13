namespace Networking
{
    public static class API
    {
        public static Networking.Get<Void> Log(string message)
        {
            return new Networking.Get<Void>($"/debug/log?message={message}");
        }

        public static Networking.Post<JwtResponse> Login(string username, string password)
        {
            return new Networking.Post<JwtResponse>("/api/auth/login", new { username, password });
        }

        public static Networking.Post<JwtResponse> Signup(string username, string password)
        {
            return new Networking.Post<JwtResponse>("/api/auth/signup", new { username, password });
        }
    }
}
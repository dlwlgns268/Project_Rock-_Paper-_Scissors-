using System;
using System.Collections.Generic;

namespace Networking
{
    [Serializable]
    public class ErrorBody
    {
        public int errorId;
        public string message;
    }

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;

        public LoginRequest(string username, string password)
        {
            this.username = username;
            this.password = password;
        }
    }

    [Serializable]
    public class JwtResponse
    {
        public string token;
    }
    
    public class Void { }
}
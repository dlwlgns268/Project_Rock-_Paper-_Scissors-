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
    }

    [Serializable]
    public class JwtResponse
    {
        public string token;
    }
    
    public class Void { }
}
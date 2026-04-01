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

    public class Void { }
}
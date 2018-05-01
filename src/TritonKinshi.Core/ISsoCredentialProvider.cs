using System;
using System.Net;

namespace TritonKinshi.Core
{
    public interface ISsoCredentialProvider : IDisposable
    {
        CookieCollection GetCredentials();
    }
}

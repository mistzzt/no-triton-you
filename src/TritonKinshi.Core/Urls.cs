using System;

namespace TritonKinshi.Core
{
    public static class Urls
    {
        public const string MyTritonLinkUrl = "http://mytritonlink.ucsd.edu";
        public const string ActUrl = "https://act.ucsd.edu";

        public static readonly Uri MyTritonLink = new Uri(MyTritonLinkUrl);
        public static readonly Uri Act = new Uri(ActUrl);
    }
}

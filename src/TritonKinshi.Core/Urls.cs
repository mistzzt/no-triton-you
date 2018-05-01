using System;

namespace TritonKinshi.Core
{
    public static class Urls
    {
        public const string TritonLinkUrl = "http://mytritonlink.ucsd.edu";
        public const string ActUrl = "https://act.ucsd.edu";

        public static readonly Uri TritonLink = new Uri(TritonLinkUrl);
        public static readonly Uri Act = new Uri(ActUrl);
    }
}

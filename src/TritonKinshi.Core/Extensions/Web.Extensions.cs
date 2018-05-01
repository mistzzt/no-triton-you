using System.Collections.Specialized;
using System.Net;
using System.Web;

using static TritonKinshi.Core.Urls;

namespace TritonKinshi.Core.Extensions
{
    public static class WebExtensions
    {
        public static CookieCollection GetAllRelatedCookies(this CookieContainer container)
        {
            var collection = new CookieCollection();

            foreach (Cookie cookie in container.GetCookies(Act))
            {
                collection.Add(cookie);
            }

            foreach (Cookie cookie in container.GetCookies(MyTritonLink))
            {
                collection.Add(cookie);
            }

            return collection;
        }

        public static string BuildQueryString(this NameValueCollection collection)
        {
            if (collection == null || collection.Count == 0)
            {
                return string.Empty;
            }

            var htmlCollection = HttpUtility.ParseQueryString(string.Empty);
            htmlCollection.Add(collection);

            return "?" + htmlCollection;
        }
    }
}

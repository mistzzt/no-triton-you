using System;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using TritonKinshi.Core.Extensions;
using TritonKinshi.Core.Parser;

using Value = System.Collections.Generic.KeyValuePair<string, string>;

namespace TritonKinshi.Core
{
    public sealed class UserIdSsoProvider : ISsoCredentialProvider
    {
        private readonly string _user;
        private readonly SecureString _password;
        private bool _disposed;

        private readonly HttpClient _client;
        private readonly CookieContainer _container;

        private bool _pending; 

        public UserIdSsoProvider(string user, SecureString password)
        {
            _user = user;
            _password = password.Copy();

            _container = new CookieContainer();

            var clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = _container
            };
            _client = new HttpClient(clientHandler);
        }

        public async Task LoginAsync()
        {
            if (_pending)
            {
                throw new InvalidOperationException();
            }

            _pending = true;

            // load webpage and redirect first
            var postUri = (await _client.GetAsync(Urls.MyTritonLink)).RequestMessage.RequestUri;
            var content = new FormUrlEncodedContent(new[]
            {
                new Value("initAuthMethod", "urn:mace:ucsd.edu:sso:studentsso"),
                new Value("urn:mace:ucsd.edu:sso:username", _user),
                new Value("urn:mace:ucsd.edu:sso:password", _password.ToUnsecureString()),
                new Value("urn:mace:ucsd.edu:sso:authmethod", "urn:mace:ucsd.edu:sso:studentsso"),
                new Value("submit", "submit")
            });

            // parse the saml form
            var samlResponse = await _client.PostAsync(postUri, content);
            var parser = new SamlParser(await samlResponse.Content.ReadAsStreamAsync());
            var (target, values) = await parser.ParseAsync();

            if (!target.IsAbsoluteUri)
            {
                target = new Uri(new Uri(postUri.GetLeftPart(UriPartial.Authority)), target);
            }

            // post saml values and then we should be logged in
            content = new FormUrlEncodedContent(values);
            await _client.PostAsync(target, content);

            // todo validate whether any error occurred

            _pending = false;
        }

        public CookieCollection GetCredentials()
        {
            return _container.GetAllRelatedCookies();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _password?.Dispose();
            _client?.Dispose();
            _disposed = true;
        }
    }
}

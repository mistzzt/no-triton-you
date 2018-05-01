using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TritonKinshi.Core.Parser;
using static TritonKinshi.Core.Urls;

namespace TritonKinshi.Core
{
    public sealed class TritonLink : ITritonLink
    {
        private readonly HttpClient _client;
        private readonly CookieContainer _container;

        private IWebRegImpl _webReg;

        private string _major;
        private string _level;
        private string _college;
        private string _name;
        private string _balance;
        private string _logout;

        private bool _loaded;

        public TritonLink(ISsoCredentialProvider sso)
        {
            _container = new CookieContainer();
            _container.Add(sso.GetCredentials());

            var clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = _container
            };

            _client = new HttpClient(clientHandler) { BaseAddress = new Uri(ActUrl) };
        }

        public async Task InitializeAsync()
        {
            if (_loaded)
            {
                throw new InvalidOperationException();
            }

            var response = _client.GetAsync(new Uri(MyTritonLinkUrl));
            var parser = new TritonLinkParser(await response.Result.Content.ReadAsStreamAsync());
            (_name, _college, _major, _level, _balance, _logout) = await parser.ParseAsync();
            _loaded = true;
        }

        public string Major => _loaded ? _major : throw new InvalidOperationException();
        public string Level => _loaded ? _level : throw new InvalidOperationException();
        public string College => _loaded ? _college : throw new InvalidOperationException();
        public string Name => _loaded ? _name : throw new InvalidOperationException();
        public string Balance => _loaded ? _balance : throw new InvalidOperationException();

        public IWebRegImpl CreateWebRegInstance()
        {
            return _webReg ?? (_webReg = new PhantomJsWebReg(_client, _container));
        }

        public async Task LogoutAsync()
        {
            await _client.GetAsync(_logout);
        }
    }
}

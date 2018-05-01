using System.Threading.Tasks;

namespace TritonKinshi.Core
{
    public interface ITritonLink
    {
        string Major { get; }

        string Level { get; }

        string College { get; }

        string Name { get; }

        string Balance { get; }

        IWebRegImpl CreateWebRegInstance();

        Task LogoutAsync();

        Task InitializeAsync();
    }
}

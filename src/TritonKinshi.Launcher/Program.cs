using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using TritonKinshi.Core;
using TritonKinshi.Launcher.Examples;

namespace TritonKinshi.Launcher
{
    internal static class Program
    {
        private static void Main()
        {
            Test().Wait();

            Console.Write("All operations ended; press any key to exit..");
            Console.Read();
        }

        private static async Task Test()
        {
            var pwd = new SecureString();
            foreach (var chr in TestUser.Pwd)
            {
                pwd.AppendChar(chr);
            }

            var sso = new UserIdSsoProvider(TestUser.UserName, pwd);
            await sso.LoginAsync();

            var monitor = new WaitListMonitor(TestUser.UserName, pwd);
            monitor.Start();

            while (true)
            {
                await Task.Delay(1000);
            }
        }
    }
}

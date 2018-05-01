using System;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using TritonKinshi.Core;

namespace TritonKinshi.Launcher
{
    internal static class Program
    {
        private static void Main()
        {
#if DUMP
            DumpCredentials().Wait();
#else
            Test().Wait();
#endif

            Console.Write("All operations ended; press any key to exit..");
            Console.Read();
        }

        private static async Task DumpCredentials()
        {
            var pwd = new SecureString();
            foreach (var chr in TestUser.Pwd)
            {
                pwd.AppendChar(chr);
            }

            var sso = new UserIdSsoProvider(TestUser.UserName, pwd);
            await sso.LoginAsync();

            var tLink = new TritonLink(sso);
            sso.Dispose();

            await tLink.InitializeAsync();

            Console.WriteLine(tLink.Name);
            Console.WriteLine(tLink.College);
            Console.WriteLine(tLink.Major);
            Console.WriteLine(tLink.Level);
            Console.WriteLine(tLink.Balance);

            var webReg = (PhantomJsWebReg)tLink.CreateWebRegInstance();
            await webReg.PrepareWebReg();
        }

        private static async Task Test()
        {
            var webReg = PhantomJsWebReg.FromPersistedCredentials();

            var terms = await webReg.GetTermsAsync();
            var sp18 = terms.Single(x => x.Code == "S118");
            Console.WriteLine(webReg.CheckEligibilityAsync(sp18).Result);
            foreach (var term in terms)
            {
                Console.WriteLine($"{term.Code,5}\t{term.SequenceId,6}\t{term.Description,10}");
            }

            var subjects = await webReg.SearchSubjectListAsync(sp18);
            foreach (var subject in subjects)
            {
                Console.WriteLine($"{subject.Code,5}\t{subject.Description,10}");
            }

            var cse = subjects.Single(x => x.Code == "CSE");
            var courses = await webReg.SearchCourseListAsync(sp18, cse);
            foreach (var id in courses)
            {
                Console.WriteLine($"{id.Subject,4}\t{id.Code,5}\t{id.Section,7}");
            }
        }
    }
}

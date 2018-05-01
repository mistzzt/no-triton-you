using System;
using System.IO;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;

namespace TritonKinshi.Core.Parser
{
    public sealed class TritonLinkParser : ParserBase
    {
        public TritonLinkParser(Stream stream) : base(stream) { }

        public async Task
            <(string name, string college, string major, string level, string balance, string logout)>
            ParseAsync()
        {
            var document = await LoadDocument();

            var hint = ParseContent(document, UnavailableSelector);
            if (!string.IsNullOrEmpty(hint))
            {
                throw new NotSupportedException(hint);
            }

            return (
                ParseContent(document, NameSelector),
                ParseContent(document, CollegeSelector),
                ParseContent(document, MajorSelector),
                ParseContent(document, LevelSelector),
                ParseContent(document, BalanceSelector),
                ParseLogout(document)
            );
        }

        private static string ParseLogout(IParentNode document)
        {
            return ((IHtmlAnchorElement)document.QuerySelector(LogoutSelector)).PathName;
        }

        private static string ParseContent(IParentNode document, string selector)
        {
            return document?.QuerySelector(selector)?.TextContent?.Trim();
        }

        private const string NameSelector = "#my_tritonlink_sidebar > h2";
        private const string CollegeSelector = "#my_tritonlink_sidebar > p:nth-child(3) > a";
        private const string MajorSelector = "#my_tritonlink_sidebar > p:nth-child(4) > a";
        private const string LevelSelector = "#my_tritonlink_sidebar > p:nth-child(5) > b";
        private const string BalanceSelector = "#account_balance > div.cs_box_amount > a > strong";

        private const string UnavailableSelector = "#class_schedule > div > h1 > b > font";

        private const string LogoutSelector = "#tdr_login_content > a";
    }
}
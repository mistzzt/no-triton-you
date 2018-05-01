using System.IO;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace TritonKinshi.Core.Parser
{
    public abstract class ParserBase
    {
        protected readonly Stream Stream;
        protected readonly HtmlParser Parser;

        protected ParserBase(Stream stream, HtmlParserOptions options = default(HtmlParserOptions))
        {
            Stream = stream;
            Parser = new HtmlParser(options);
        }

        protected async Task<IHtmlDocument> LoadDocument()
        {
            return await Parser.ParseAsync(Stream);
        }
    }
}
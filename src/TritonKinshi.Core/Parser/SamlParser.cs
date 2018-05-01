using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;

namespace TritonKinshi.Core.Parser
{
	public sealed class SamlParser : ParserBase
	{
		public SamlParser(Stream stream) : base(stream) { }

		public async Task<(Uri, KeyValuePair<string, string>[])> ParseAsync()
		{
			var document = await LoadDocument();
			var form = document.Forms.Single();

			return (new Uri(form.Action, UriKind.RelativeOrAbsolute), form.Children
				.SelectMany(x => x.Children)
				.Where(x => x is IHtmlInputElement)
				.Select(x => { var input = (IHtmlInputElement)x; return new KeyValuePair<string, string>(input.Name, input.DefaultValue); })
				.ToArray());
		}
	}
}
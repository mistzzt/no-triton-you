using Newtonsoft.Json;

namespace TritonKinshi.Core
{
    public struct Subject
    {
        [JsonProperty("LONG_DESC")]
        public string Description { get; internal set; }

        [JsonProperty("SUBJECT_CODE")]
        public string Code { get; internal set; }
    }
}
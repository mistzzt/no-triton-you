using Newtonsoft.Json;

namespace TritonKinshi.Core
{
    public struct Term
    {
        [JsonProperty("termDesc")]
        public string Description { get; internal set; }

        [JsonProperty("termCode")]
        public string Code { get; internal set; }

        [JsonProperty("seqId")]
        public int SequenceId { get; internal set; }
    }
}
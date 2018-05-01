namespace TritonKinshi.Core
{
    public struct CourseId
    {
        public string Subject { get; internal set; }

        public string Code { get; internal set; }

        public int Section { get; internal set; }

        public override string ToString()
        {
            var id = $"{Subject} {Code}";
            if (Section > 0) id += $", {Section}";

            return id;
        }
    }
}

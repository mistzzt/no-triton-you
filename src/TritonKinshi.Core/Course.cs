namespace TritonKinshi.Core
{
    public struct Course
    {
        public CourseId Id { get; internal set; }

        public GradingOption GradingOption { get; internal set; }

        public double Unit { get; internal set; }

        public string TermCode { get; internal set; }
    }
}
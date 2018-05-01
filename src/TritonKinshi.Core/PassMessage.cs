using System;

namespace TritonKinshi.Core
{
    public struct PassMessage
    {
        public DateTime FirstPassBegin { get; internal set; }

        public DateTime FirstPassEnd { get; internal set; }

        public DateTime SecondPassBegin { get; internal set; }

        public DateTime SecondPassEnd { get; internal set; }

        public string AppointmentTimer { get; internal set; }

        public string CollegeCode { get; internal set; }

        public string Display { get; internal set; }
    }
}

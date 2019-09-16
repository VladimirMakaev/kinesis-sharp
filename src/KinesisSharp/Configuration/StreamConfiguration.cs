using System;
using KinesisSharp.Common;

namespace KinesisSharp.Configuration
{
    public class StreamConfiguration
    {
        public string StreamArn { get; set; }

        public InitialPositionType Position { get; set; }

        public DateTime? TimeStamp { get; set; }
    }
}
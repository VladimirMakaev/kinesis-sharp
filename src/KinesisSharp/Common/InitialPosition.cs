using System;
using KinesisSharp.Configuration;

namespace KinesisSharp.Common
{
    public class InitialPosition
    {
        private readonly DateTime? timeStamp;
        private readonly InitialPositionType type;

        public InitialPosition(InitialPositionType type, DateTime? timeStamp = null)
        {
            this.type = type;
            this.timeStamp = timeStamp;
        }

        public DateTime? TimeStamp => timeStamp;

        public InitialPositionType Type => type;
    }
}
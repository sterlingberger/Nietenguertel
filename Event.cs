using System;
using System.Collections.Generic;
using System.Text;

namespace EventCrawler
{
    internal class Event
    {
        public string Venue { get; set; } = "unknown";

        public DateOnly Date { get; set; }

        public string Artist { get; set; } = "unknown";

        public string Info { get; set; } = "unknown";
    }
}

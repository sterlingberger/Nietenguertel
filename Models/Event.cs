using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace EventCrawler.Models
{
    internal class Event
    {
        public string Venue { get; set; } = "unknown";

        public DateOnly Date { get; set; }

        public string Artist { get; set; } = "unknown";

        [JsonIgnore]
        public string Info { get; set; } = "unknown";

        public string Link { get; set; } = "unknown";

        //nur erste 50 zeichen nehmen
        public string InfoShort => Info.Length > 128 ? Info[..128] + "..." : Info;


        public override bool Equals(object obj) =>
            obj is Event other &&
            Artist == other.Artist &&
            Date == other.Date &&
            Link == other.Link &&
            InfoShort == other.InfoShort &&
            Venue == other.Venue;

        public override int GetHashCode() =>
            HashCode.Combine(Artist, Date, Link, InfoShort, Venue);
    }
}

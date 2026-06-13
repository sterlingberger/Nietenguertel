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

        public string Info { get; set; } = "unknown";

        public string Link { get; set; } = "unknown";

        [JsonIgnore] //nicht notwendig, da wenn kein Feld eh ignoriert
        private DateTime? _start;
        public DateTime Start
        {
            get => _start ?? Date.ToDateTime(new TimeOnly(18, 0));
            set
            {
                _start = value;
                foundstartdate = true;
            }
        }

        private DateTime? _end;
        public DateTime End
        {
            get => _end ?? Date.ToDateTime(new TimeOnly(23, 59));
            set
            {
                _end = value;
                foundenddate = true;
            }
        }

        //debugging
        [JsonIgnore]
        public bool foundstartdate { get; set; } = false;
        [JsonIgnore]
        public bool foundenddate { get; set; } = false;

        public string InfoShort => Info.Length > 128 ? Info[..128] + "..." : Info;

        public string IcsFileName
        {
            get
            {
                static string Safe(string s) =>
                    string.Concat(s.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_'))
                          .Trim('_');
                return $"{Date:yyyy-MM-dd}_{Safe(Venue)}_{Safe(Artist)}.ics";
            }
        }


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

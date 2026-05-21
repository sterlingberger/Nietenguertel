using EventCrawler.Crawler;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventCrawler.Models
{
    internal class Venue
    {
        public string Name { get; private set; }
        public string[] LocationFilter { get; private set; }
        public ICrawler Crawler { get; private set; }

        public Venue(string name, string[] locationfilter, ICrawler crawler)
        {
            Name = name;
            LocationFilter = locationfilter;
            
            Crawler = crawler;
            Crawler.SetVenueName(Name);
        }
    }
}

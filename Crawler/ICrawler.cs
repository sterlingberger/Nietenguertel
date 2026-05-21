using EventCrawler.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventCrawler.Crawler
{
    internal interface ICrawler
    {
        public Task<IEnumerable<Event>> FetchAsync();
        public void SetVenueName(string name);
    }
}

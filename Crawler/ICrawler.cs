using System;
using System.Collections.Generic;
using System.Text;

namespace EventCrawler.Crawler
{
    internal interface ICrawler
    {
        Task<IEnumerable<Event>> FetchAsync();
    }
}

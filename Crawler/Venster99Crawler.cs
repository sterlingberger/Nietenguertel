using EventCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EventCrawler.Crawler
{
    internal class Venster99Crawler : ICrawler
    {
        private string url = "https://www.venster99.at/";
        private IPage _page;

        public Venster99Crawler(IPage page)
        {
            _page = page;
        }

        public string VenueName { get; private set; }

        public void SetVenueName(string name)
        {
            VenueName = name;
        }
        public async Task<IEnumerable<Event>> FetchAsync()
        {
            List<Event> result = new List<Event>();

            await _page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            string eventxpath = "xpath=//div[@id='events-container']/div[@class='event']";
            var eventDivs = await _page.Locator(eventxpath).AllAsync();

            foreach (var div in eventDivs)
            {
                try
                {
                    var date = await div.Locator("p").First.InnerTextAsync();

                    var artist = await div.Locator("strong").First.InnerTextAsync();

                    var link = await div.Locator("a[href]").First.GetAttributeAsync("href");

                    //eingrenzen auf die kommendne 2 Monate, der findet sonst zu viel
                    if (ParseDate(date).Month > DateTime.Now.Month + 1)
                        continue;

                    var ev = new Event
                    {
                        Date = ParseDate(date),
                        Artist = artist,
                        Venue = VenueName,
                        Info = "keine Info", //haben keine info
                        Link = link
                    };
                    result.Add(ev);
                }
                catch (Exception ex)
                {
                    var ev = new Event
                    {
                        Venue = VenueName,
                        Info = $"{ex.Message}"
                    };
                    result.Add(ev);
                }

            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            // "Wed Jun 10 2026"
            return DateOnly.ParseExact(raw, "ddd MMM d yyyy", CultureInfo.InvariantCulture);
        }
    }
}

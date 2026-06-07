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

            if (eventDivs.Count == 0)
            {
                throw new InvalidDataException($"Scheint, als würde kein Eventcontainer für {VenueName} gefunden > 0 Events auffindbar, überspringe Crawl");
            }

            foreach (var div in eventDivs)
            {
                try
                {
                    var dateRaw = await div.Locator("p").First.InnerTextAsync();

                    var artist = await div.Locator("strong").First.InnerTextAsync();

                    var link = await div.Locator("a[href]").First.GetAttributeAsync("href");

                    var date = ParseDate(dateRaw);
                    if (date.Month > DateTime.Now.Month + 1)
                        continue;

                    result.Add(new Event
                    {
                        Date = date,
                        Artist = artist,
                        Venue = VenueName,
                        Info = "",
                        Link = link
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Venster99Crawler: item übersprungen - {ex.Message}");
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

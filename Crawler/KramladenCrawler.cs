using EventCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EventCrawler.Crawler
{
    internal class KramladenCrawler : ICrawler
    {
        private string url = "https://www.kramladenvienna.at/";
        private IPage _page;

        public KramladenCrawler(IPage page)
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

            // Button klicken
            await _page.ClickAsync("//button[contains(text(), 'Load more events...')]");

            // Warten bis alle Elemente geladen sind
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var eventDivs = await _page.Locator("xpath=//div[@id='block-yui_3_17_2_1_1743151507799_1398']//div[@class='sqs-block-content']//div[@class='sqs-code-container']//div[@class='sk-fb-event']//div[@class='sk-events-body']//div[@class='sk-events-wrapper --sk-columns-3']//div[@class='sk-events-masonry']//div[@class='sk-event-item --vertical --sk-event-image-loaded']").AllAsync();

            if (eventDivs.Count == 0)
            {
                throw new InvalidDataException($"Scheint, als würde kein Eventcontainer für {VenueName} gefunden > 0 Events auffindbar, überspringe Crawl");
            }

            foreach (var div in eventDivs)
            {
                try
                {
                    var datespan = div.Locator(".sk-event-item-date");

                    string dateRaw = await datespan.Locator(".icon_text").InnerTextAsync();
                    string link = await div.Locator(".sk-event-item-thumbnail img").GetAttributeAsync("src") ?? "";

                    string artist = await div.Locator(".sk-event-item-title").InnerTextAsync();

                    string info = await div.Locator(".sk-event-item-desc--less.js-event-item-desc--less > div").InnerTextAsync();

                    var date = ParseDate(dateRaw);
                    if (date.Month > DateTime.Now.Month + 1)
                        continue;

                    result.Add(new Event
                    {
                        Date = date,
                        Artist = artist,
                        Venue = VenueName,
                        Info = info,
                        Link = link
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"KramladenCrawler: item übersprungen - {ex.Message}");
                }
            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            var date = DateTime.ParseExact(raw, "MMMM d, yyyy h:mm tt", CultureInfo.InvariantCulture);
            return DateOnly.FromDateTime(date);
        }
    }
}

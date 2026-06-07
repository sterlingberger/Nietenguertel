using EventCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EventCrawler.Crawler
{
    internal class SzeneCrawler : ICrawler
    {
        private string url = "https://szene.wien/";
        private IPage _page;

        public SzeneCrawler(IPage page)
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
            await _page.ClickAsync("button.wpgb-button.wpgb-load-more");

            // Warten bis alle Elemente geladen sind
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Task.Delay(2000);

            string eventxpath = "xpath=//article[starts-with(@class, 'wpgb-card wpgb-card-2 wpgb')]";
            var eventDivs = await _page.Locator(eventxpath).AllAsync();

            if (eventDivs.Count == 0)
            {
                throw new InvalidDataException($"Scheint, als würde kein Eventcontainer für {VenueName} gefunden > 0 Events auffindbar, überspringe Crawl");
            }

            foreach (var div in eventDivs)
            {
                try
                {
                    var artist = await div.Locator("h3.wpgb-block-1").InnerTextAsync();

                    var dateRaw = await div.Locator("div.wpgb-block-2").InnerTextAsync();

                    var link = await div.Locator("a.wpgb-card-layer-link").GetAttributeAsync("href");

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
                    Console.WriteLine($"SzeneCrawler: item übersprungen - {ex.Message}");
                }

            }

            //info nachträglich setzen
            foreach (Event ev in result)
            {
                try
                {
                    await _page.GotoAsync(ev.Link, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle
                    });

                    var container = _page.Locator("xpath=//div[@id='em-event-6']/div[@class='text']");

                    ev.Info = await container.InnerTextAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SzeneCrawler: info für '{ev.Artist}' nicht abrufbar - {ex.Message}");
                }
            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            // "((fr., 22. mai 2026))"
            raw = raw.Trim('(', ')', ' ').ToLower();
            raw = raw.Replace(".", "");
            // de-AT lowercase MonthNames registrieren geht nicht nativ,
            // daher ToTitleCase
            raw = new CultureInfo("de-AT").TextInfo.ToTitleCase(raw);
            return DateOnly.ParseExact(raw, "ddd, d MMMM yyyy", new CultureInfo("de-AT"));
        }
    }
}

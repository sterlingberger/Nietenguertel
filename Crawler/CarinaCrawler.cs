using EventCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EventCrawler.Crawler
{
    internal class CarinaCrawler : ICrawler
    {
        private string url = "https://www.cafe-carina.at/2020/program/";
        private IPage _page;

        public CarinaCrawler(IPage page)
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

            int clickcounter = 0;

            //12 elemente pro ladevorgang, 60 tage = 5
            while (clickcounter < 6)
            {
                if (!await _page.Locator("div.mec-load-more-button").IsVisibleAsync())
                    break;

                var button = _page.Locator("div.mec-load-more-button");
                var count = await button.CountAsync();
                if (count == 0)
                    break;

                await button.ClickAsync();
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(3000);
                clickcounter++;
            }

            // Warten bis alle Elemente geladen sind
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            //await Task.Delay(2000);

            string eventxpath = "xpath=//div[@id='mec_skin_events_599']/div[@class='mec-wrap colorskin-custom']/div[@class='mec-event-grid-clean']/div[@class='row']/div[@class='col-md-3 col-sm-3']";
            var eventDivs = await _page.Locator(eventxpath).AllAsync();

            if (eventDivs.Count == 0)
            {
                throw new InvalidDataException($"Scheint, als würde kein Eventcontainer für {VenueName} gefunden > 0 Events auffindbar, überspringe Crawl");
            }

            foreach (var div in eventDivs)
            {
                try
                {
                    var artist = await div.Locator("h4.mec-event-title a").InnerTextAsync();

                    if (artist.Contains("Sonntag Ruhetag"))
                        continue;

                    var day = await div.Locator("div.mec-event-date").InnerTextAsync();
                    var month = await div.Locator("div.mec-event-month").InnerTextAsync();

                    var date = ParseDate(day + " " + month);

                    var link = await div.Locator("h4.mec-event-title a").GetAttributeAsync("href");

                    if (date.Month > DateTime.Now.Month + 1)
                        continue;

                    var ev = new Event
                    {
                        Date = date,
                        Artist = artist,
                        Venue = VenueName,
                        Info = "",
                        Link = link
                    };

                    if (!result.Contains(ev))
                        result.Add(ev);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CarinaCrawler: item übersprungen - {ex.Message}");
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

                    var container = _page.Locator("//div[@class='col-md-8']/div[@class='mec-event-content']");

                    ev.Info = await container.InnerTextAsync();

                    //var timecontainer = _page.Locator("xpath=//*[@class='mec-events-abbr']");
                    var timecontainer = _page.Locator("xpath=//*[@class='mec-events-abbr'][not(ancestor::*[@class='mec-next-event'])]");
                    var start = await timecontainer.AllInnerTextsAsync();
                    string alls = String.Empty;

                    foreach (string s in start)
                    {
                        alls += s + " ";
                    }
                    //zeitformat hh:mm
                    var match = Regex.Match(alls, @"\b(\d{1,2}:\d{2})\b");
                    if (match.Success)
                        ev.Start = ev.Date.ToDateTime(TimeOnly.Parse(match.Value));

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CarinaCrawler: info für '{ev.Artist}' nicht abrufbar - {ex.Message}");
                }
            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            // "20 Juni"
            int year = DateTime.Now.Month >= 10
                ? DateTime.Now.Year + 1
                : DateTime.Now.Year;

            raw = $"{raw} {year}"; // → "20 Juni 2026"
            raw = new CultureInfo("de-AT").TextInfo.ToTitleCase(raw.ToLower());

            return DateOnly.ParseExact(raw, "d MMMM yyyy", new CultureInfo("de-AT"));
        }
    }
}

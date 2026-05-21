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
                await Task.Delay(1000);
                clickcounter++;
            }

            // Warten bis alle Elemente geladen sind
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            //await Task.Delay(2000);

            string eventxpath = "xpath=//div[@id='mec_skin_events_599']/div[@class='mec-wrap colorskin-custom']/div[@class='mec-event-grid-clean']/div[@class='row']/div[@class='col-md-3 col-sm-3']";
            var eventDivs = await _page.Locator(eventxpath).AllAsync();

            foreach (var div in eventDivs)
            {
                try
                {
                    var artist = await div.Locator("h4.mec-event-title a").InnerTextAsync();

                    var day = await div.Locator("div.mec-event-date").InnerTextAsync();
                    var month = await div.Locator("div.mec-event-month").InnerTextAsync();

                    string date = day + " " + month;

                    var link = await div.Locator("h4.mec-event-title a").GetAttributeAsync("href");

                    //eingrenzen auf die kommendne 2 Monate, der findet sonst zu viel
                    if (ParseDate(date).Month > DateTime.Now.Month + 1)
                        continue;

                    var ev = new Event
                    {
                        Date = ParseDate(date),
                        Artist = artist,
                        Venue = VenueName,
                        Info = "", //haben keine info
                        Link = link
                    };

                    if (!result.Contains(ev))
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

            //info nachträglich setzen
            foreach (Event ev in result)
            {
                try
                {
                    //ins event reingehen und info beziehen
                    await _page.GotoAsync(ev.Link, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle
                    });

                    var container = _page.Locator("//div[@class='col-md-8']/div[@class='mec-event-content']");

                    ev.Info = await container.InnerTextAsync();
                }
                catch (Exception ex)
                {
                    ev.Info = $"{ex.Message}";
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

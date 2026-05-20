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

            string venue = "Szene";

            foreach (var div in eventDivs)
            {
                try
                {
                    var artist = await div.Locator("h3.wpgb-block-1").InnerTextAsync();

                    var date = await div.Locator("div.wpgb-block-2").InnerTextAsync();

                    var link = await div.Locator("a.wpgb-card-layer-link").GetAttributeAsync("href");

                    //eingrenzen auf die kommendne 2 Monate, der findet sonst zu viel
                    if (ParseDate(date).Month > DateTime.Now.Month + 1)
                        continue;

                    var ev = new Event
                    {
                        Date = ParseDate(date),
                        Artist = artist,
                        Venue = venue,
                        Info = "", //haben keine info
                        Link = link
                    };
                    result.Add(ev);
                }
                catch (Exception ex)
                {
                    var ev = new Event
                    {
                        Venue = venue,
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

                    var container = _page.Locator("xpath=//div[@id='em-event-6']/div[@class='text']");

                    //bisher nur bei gürtelconnection als mehrere strongs gesehen
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

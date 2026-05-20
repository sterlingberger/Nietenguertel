using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EventCrawler.Crawler
{
    internal class B72Crawler : ICrawler
    {
        private const string urlbase = "https://www.b72.at/";
        private string url = $"{urlbase}program";
        private IPage _page;

        public B72Crawler(IPage page)
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

            //string eventxpath = "xpath=//*[@id=\"copilot-render-69fd92f9b55ed\"]//div//div[3]//div//div//div//div//div";
            //var eventDivs = await _page.Locator(eventxpath).AllAsync();
            var eventDivs = await _page.Locator("xpath=//div[@class='section']//*[contains(@class,'row mtb0')]").AllAsync();

            string venue = "B72";

            foreach (var div in eventDivs)
            {
                try
                {
                    var anchor = div.Locator("a").First;
                    string link = urlbase + await anchor.GetAttributeAsync("href") ?? "";
                    string artist = await anchor.InnerTextAsync();

                    string date = await div.Locator("h4").First.InnerTextAsync();

                    //eingrenzen auf die kommendne 2 Monate, der findet sonst zu viel
                    if (ParseDate(date).Month > DateTime.Now.Month + 1)
                        continue;

                    var ev = new Event
                    {
                        Date = ParseDate(date),
                        Artist = artist,
                        Venue = venue,
                        Info = "",
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
                //ins event reingehen und info beziehen
                await _page.GotoAsync(ev.Link, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                var container = _page.Locator("xpath=//html//body//div[@class='container']//div[@class='section']//div[@class='row']//div[@class='col s12']");

                //bisher nur bei gürtelconnection als mehrere strongs gesehen
                var elements = await container.Locator("p, strong").AllAsync();

                foreach (var el in elements)
                {
                    string text = await el.InnerTextAsync();
                    if (!string.IsNullOrWhiteSpace(text))
                        ev.Info += $"\n{text}";
                }
            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            //raw format "08.05"
            return DateOnly.ParseExact($"{raw}.{DateTime.Now.Year}", "dd.MM.yyyy");
        }
    }
}

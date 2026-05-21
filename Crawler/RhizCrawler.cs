using EventCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EventCrawler.Crawler
{
    internal class RhizCrawler : ICrawler
    {
        private string url = "https://rhiz.wien/";
        private IPage _page;

        public RhizCrawler(IPage page)
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

            //string eventxpath = "xpath=//*[@id=\"copilot-render-69fd92f9b55ed\"]//div//div[3]//div//div//div//div//div";
            //var eventDivs = await _page.Locator(eventxpath).AllAsync();
            var eventDivs = await _page.Locator("css=.card-body").AllAsync(); //css selector

            foreach (var div in eventDivs)
            {
                try
                {
                    var anchor = div.Locator("a").First;
                    string link = url + await anchor.GetAttributeAsync("href") ?? "";

                    string artist = await div.Locator("h2.card-title").InnerTextAsync();

                    string info = await div.Locator("h3").First.InnerTextAsync();
                    //für mehr info könnte man jeden Link öffnen und dann von dort auslesen, für jetzt mal ok

                    var datelocator = div.Locator("p").Filter(new LocatorFilterOptions
                    {
                        HasText = "datum"
                    }).First;

                    string date = await datelocator.InnerTextAsync();

                    //eingrenzen auf die kommendne 2 Monate, der findet sonst zu viel
                    if (ParseDate(date).Month > DateTime.Now.Month + 1)
                        continue;

                    var ev = new Event
                    {
                        Date = ParseDate(date),
                        Artist = artist,
                        Venue = VenueName,
                        Info = info,
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

            //info nachträglich setzen
            foreach (Event ev in result)
            {
                //ins event reingehen und info beziehen
                await _page.GotoAsync(ev.Link, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                var infos = await _page.Locator("div.singleEvent p").AllAsync();

                string info = ev.Info;

                foreach (var p in infos)
                {
                    string ptext = await p.InnerTextAsync();

                    if (!string.IsNullOrWhiteSpace(ptext))
                        info += $"\n{ptext}";
                }

                ev.Info = info;
            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            //raw format "Datum: 08.05.2026 | 23:00 Uhr"
            string datePart = raw.Split(' ')[1]; // "08.05.2026"
            return DateOnly.ParseExact(datePart, "dd.MM.yyyy");
        }
    }
}

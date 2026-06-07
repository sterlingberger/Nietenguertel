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
    internal class B72Crawler : ICrawler
    {
        private const string urlbase = "https://www.b72.at/";
        private string url = $"{urlbase}program";
        private IPage _page;

        public B72Crawler(IPage page)
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
            var eventDivs = await _page.Locator("xpath=//div[@class='section']//*[contains(@class,'row mtb0')]").AllAsync();

            if (eventDivs.Count == 0)
            {
                throw new InvalidDataException($"Scheint, als würde kein Eventcontainer für {VenueName} gefunden > 0 Events auffindbar, überspringe Crawl");
            }

            foreach (var div in eventDivs)
            {
                try
                {
                    var anchor = div.Locator("a").First;
                    string link = urlbase + await anchor.GetAttributeAsync("href") ?? "";
                    string artist = await anchor.InnerTextAsync();

                    string dateRaw = await div.Locator("h4").First.InnerTextAsync();
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
                    Console.WriteLine($"B72Crawler: item übersprungen - {ex.Message}");
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
                catch (Exception ex)
                {
                    Console.WriteLine($"B72Crawler: info für '{ev.Artist}' nicht abrufbar - {ex.Message}");
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

using EventCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EventCrawler.Crawler
{
    internal class WUKCrawler : ICrawler
    {
        private const string url = "https://www.wuk.at/en/events/";
        private const string urlbase = "https://wuk.at";
        private IPage _page;

        public WUKCrawler(IPage page)
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
            List<Event> tmpresult = new List<Event>();
            List<Event> result = new List<Event>();

            await _page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            int clickcounter = 0;

            //3 klicks sollten reichen
            while (clickcounter < 3)
            {
                if (!await _page.Locator("xpath=//div[@id='event-pagination-drop-area-6484']/a[@class='ajax ajax-loader btn btn-outline-default mb-1 extbase-ajaxified']").IsVisibleAsync())
                    break;

                var button = _page.Locator("xpath=//div[@id='event-pagination-drop-area-6484']/a[@class='ajax ajax-loader btn btn-outline-default mb-1 extbase-ajaxified']");
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

            var eventDivs = await _page.Locator("xpath=//*[@class='event-list-item-text-wrapper']").AllAsync();

            if (eventDivs.Count == 0)
            {
                throw new InvalidDataException($"Scheint, als würde kein Eventcontainer für {VenueName} gefunden > 0 Events auffindbar, überspringe Crawl");
            }

            foreach (var div in eventDivs)
            {
                try
                {
                    string theme = await div.Locator("xpath=//*[@class='theme-list']").InnerTextAsync();

                    //alle events auf theme "Musik" prüfen, dann mal alle mit link speichern. später alle links besuchen und informationen sammeln
                    if (!theme.Contains("Musik", StringComparison.OrdinalIgnoreCase))
                        continue;

                    else
                    {
                        var link = urlbase + await div.Locator("a").Filter(new() { HasTextRegex = new Regex("more", RegexOptions.IgnoreCase) }).First.GetAttributeAsync("href");

                        tmpresult.Add(new Event
                        {
                            Venue = VenueName,
                            Link = link
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WUKCrawler: item übersprungen - {ex.Message}");
                }
            }

            foreach (Event ev in tmpresult)
            {
                try
                {
                    await _page.GotoAsync(ev.Link, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle
                    });

                    //artist
                    ev.Artist = await _page.Locator("h1").First.InnerTextAsync();

                    //info
                    var info1 = await _page.Locator("xpath=//*[@class='page-complex-layout-heading']/div[@class='container']").AllInnerTextsAsync();
                    var info2 = await _page.Locator("xpath=//*[@class='fce-header-page-notes']").AllInnerTextsAsync();
                    var info3 = await _page.Locator("xpath=//*[@class='fce-text']/div[@class='container']").AllInnerTextsAsync();

                    string info = String.Empty;
                    foreach (var i in info1)
                        info += i + " | ";
                    foreach (var i in info2)
                        info += i + " | ";
                    foreach (var i in info3)
                        info += i + " | ";

                    ev.Info = info;

                    //date
                    var datestring = await _page.Locator("xpath=//*[@class='px-md-0 px-half'][1]/div/p").InnerTextAsync();
                    ev.Date = ParseDate(datestring);

                    // Datum
                    var dateMatch = Regex.Match(datestring, @"\d{1,2}\.\d{1,2}\.\d{4}");
                    var date = DateOnly.ParseExact(dateMatch.Value, "d.M.yyyy");

                    ev.Date = date;
                    
                    //überspringen wenn nicht im Daterahmen
                    if (date.Month > DateTime.Now.Month + 1)
                        continue;

                    // Uhrzeit
                    var timeMatch = Regex.Match(datestring, @"(\d{1,2})[\.\:](\d{2})\s*(AM|PM)", RegexOptions.IgnoreCase);
                    if (timeMatch.Success)
                    {
                        int hour = int.Parse(timeMatch.Groups[1].Value);
                        int minute = int.Parse(timeMatch.Groups[2].Value);
                        if (timeMatch.Groups[3].Value.ToUpper() == "PM" && hour != 12) hour += 12;
                        if (timeMatch.Groups[3].Value.ToUpper() == "AM" && hour == 12) hour = 0;
                        ev.Start = date.ToDateTime(new TimeOnly(hour, minute));
                    }

                    result.Add(ev);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WUKCrawler: info für '{ev.Artist}' nicht abrufbar - {ex.Message}");
                }



            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            // "Fr 26.6.2026\n7.30"
            var match = Regex.Match(raw, @"\d{1,2}\.\d{1,2}\.\d{4}");
            return DateOnly.ParseExact(match.Value, "d.M.yyyy");
        }
    }
}

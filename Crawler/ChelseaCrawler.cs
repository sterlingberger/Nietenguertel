using EventCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EventCrawler.Crawler
{
    internal class ChelseaCrawler : ICrawler
    {
        private string url = "https://www.chelsea.co.at/concerts.php";
        private IPage _page;

        public ChelseaCrawler(IPage page)
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

            string eventxpath = "xpath=//html//body//div[@class='main']//table[@class='termindetails']";
            var eventDivs = await _page.Locator(eventxpath).AllAsync();

            if (eventDivs.Count == 0)
            {
                throw new InvalidDataException($"Scheint, als würde kein Eventcontainer für {VenueName} gefunden > 0 Events auffindbar, überspringe Crawl");
            }

            foreach (var div in eventDivs)
            {
                try
                {
                    string dateRaw = await div.Locator(".date").InnerTextAsync();
                    string artist = await div.Locator(".band").InnerTextAsync();
                    string info = await div.Locator(".text").InnerTextAsync();

                    var anchor = div.Locator("xpath=preceding-sibling::a[starts-with(@name,'concert_')]");
                    string concertid = await anchor.Last.GetAttributeAsync("name") ?? "";

                    string link = url + $"#{concertid}";

                    var date = ParseDate(dateRaw);
                    if (date.Month > DateTime.Now.Month + 1)
                        continue;

                    Event ev = new Event
                    {
                        Date = date,
                        Artist = artist,
                        Venue = VenueName,
                        Info = info,
                        Link = link
                    };

                    result.Add(ev);

                    //uhrzeiten lesen per regex
                    #region regex start
                    var match = Regex.Match(info, @"(\d{1,2}):(\d{2})\s*-\s*(\d{1,2}):(\d{2})|(\d{1,2}):(\d{2})|(\d{1,2})h");

                    string start = null, end = null;

                    if (match.Success)
                    {
                        if (match.Groups[1].Success) // Range "14:00 - 19:00"
                        {
                            start = $"{match.Groups[1].Value.PadLeft(2, '0')}:{match.Groups[2].Value}";
                            end = $"{match.Groups[3].Value.PadLeft(2, '0')}:{match.Groups[4].Value}";
                        }
                        else if (match.Groups[5].Success) // einzelne "hh:mm"
                        {
                            start = $"{match.Groups[5].Value.PadLeft(2, '0')}:{match.Groups[6].Value}";
                        }
                        else if (match.Groups[7].Success) // "19h"
                        {
                            start = $"{match.Groups[7].Value.PadLeft(2, '0')}:00";
                        }

                        if (start != null)
                            ev.Start = ev.Date.ToDateTime(TimeOnly.Parse(start));
                        if (end != null)
                            ev.End = ev.Date.ToDateTime(TimeOnly.Parse(end));
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ChelseaCrawler: item übersprungen - {ex.Message}");
                }

            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            // Wochentag entfernen
            var cleaned = Regex.Replace(raw, @"^[^,]+,\s*", "").Trim().TrimEnd('.');

            if (cleaned.Count(c => c == '.') == 1)
            {
                // "21.11" → kein Jahr
                return DateOnly.ParseExact(cleaned + "." + DateTime.Now.Year, "d.M.yyyy", CultureInfo.InvariantCulture);
            }
            else
            {
                // "17.04.2027" → Jahr vorhanden
                return DateOnly.ParseExact(cleaned, "d.M.yyyy", CultureInfo.InvariantCulture);
            }
        }
    }
}

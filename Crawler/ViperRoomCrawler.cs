using EventCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EventCrawler.Crawler
{
    internal class ViperRoomCrawler : ICrawler
    {
        private string url = "https://www.viper-room.at/veranstaltungen";
        private IPage _page;

        public ViperRoomCrawler(IPage page)
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

            string eventxpath = "xpath=//div[@id='em-events-list-grouped-1']//ul[@class='events_list']//li";
            var eventDivs = await _page.Locator(eventxpath).AllAsync();

            if (eventDivs.Count == 0)
            {
                throw new InvalidDataException($"Scheint, als würde kein Eventcontainer für {VenueName} gefunden > 0 Events auffindbar, überspringe Crawl");
            }

            foreach (var div in eventDivs)
            {
                try
                {
                    string dateRaw = await div.Locator(".event_date_monthyear").InnerTextAsync();
                    var anchor = div.Locator("h2.event_title a");

                    string link = await anchor.GetAttributeAsync("href") ?? "";
                    string artist = await anchor.InnerTextAsync();

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
                    Console.WriteLine($"ViperRoomCrawler: item übersprungen - {ex.Message}");
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

                    var div = _page.Locator("//div[@id='em-event-6']");

                    var allText = await div.Locator("> *:not(.event_time):not(.event_actions)").AllInnerTextsAsync();
                    ev.Info = string.Join("\n", allText);

                    //uhrzeit auslesen

                    var time = await _page.Locator("xpath=//*[@class='event_doors']").InnerTextAsync();

                    var match = Regex.Match(time.Trim(), @"(\d{1,2}):(\d{2})");

                    string? start = null;

                    if (match.Success)
                    {
                        start = $"{match.Groups[1].Value.PadLeft(2, '0')}:{match.Groups[2].Value}";
                        ev.Start = ev.Date.ToDateTime(TimeOnly.Parse(start));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ViperRoomCrawler: info für '{ev.Artist}' nicht abrufbar - {ex.Message}");
                }
            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            return DateOnly.ParseExact(raw, "dd.MM.yy");
        }
    }
}

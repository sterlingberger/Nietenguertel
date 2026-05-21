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

            foreach (var div in eventDivs)
            {
                try
                {
                    string date = await div.Locator(".event_date_monthyear").InnerTextAsync();
                    var anchor = div.Locator("h2.event_title a");
                    
                    string link = await anchor.GetAttributeAsync("href") ?? "";
                    string artist = await anchor.InnerTextAsync();

                    //string info = await div.Locator(".event_teaser").InnerTextAsync();

                    //eingrenzen auf die kommendne 2 Monate, der findet sonst zu viel
                    if (ParseDate(date).Month > DateTime.Now.Month + 1)
                        continue;

                    var ev = new Event
                    {
                        Date = ParseDate(date),
                        Artist = artist,
                        Venue = VenueName,
                        Info = "",
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
                try
                {
                    //ins event reingehen und info beziehen
                    await _page.GotoAsync(ev.Link, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle
                    });

                    var div = _page.Locator("//div[@id='em-event-6']");

                    var allText = await div.Locator("> *:not(.event_time):not(.event_actions)").AllInnerTextsAsync();
                    ev.Info = string.Join("\n", allText);
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
            return DateOnly.ParseExact(raw, "dd.MM.yy");
        }
    }
}

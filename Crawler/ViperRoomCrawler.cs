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

        public async Task<IEnumerable<Event>> FetchAsync()
        {
            List<Event> result = new List<Event>();

            await _page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            string eventxpath = "xpath=//div[@id='em-events-list-grouped-1']//ul[@class='events_list']//li";
            var eventDivs = await _page.Locator(eventxpath).AllAsync();

            string venue = "Viper Room";

            foreach (var div in eventDivs)
            {
                try
                {
                    string date = await div.Locator(".event_date_monthyear").InnerTextAsync();
                    var anchor = div.Locator("h2.event_title a");
                    
                    string link = await anchor.GetAttributeAsync("href") ?? "";
                    string artist = await anchor.InnerTextAsync();

                    string info = await div.Locator(".event_teaser").InnerTextAsync();

                    var ev = new Event
                    {
                        Date = ParseDate(date),
                        Artist = artist,
                        Venue = venue,
                        Info = info,
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

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            return DateOnly.ParseExact(raw, "dd.MM.yy");
        }
    }
}

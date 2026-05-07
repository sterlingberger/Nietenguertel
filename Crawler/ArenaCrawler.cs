using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EventCrawler.Crawler
{
    internal class ArenaCrawler : ICrawler
    {
        private string url = "https://arena.wien/Home/Programm#data_abonnement=-1&data_month=5&data_year=2026&data_event_category=-1&searchTerm=&data_mode=DATE&data_pagenumber=0&page_header=Mai+2026";
        private IPage _page;
        public ArenaCrawler(IPage page)
        {
            _page = page;
        }

        public string GetName() { return "Arena"; }

        public async Task<IEnumerable<Event>> FetchAsync()
        {
            List<Event> result = new List<Event>();

            await _page.GotoAsync(url);

            var eventDivs = await _page.Locator("xpath=//*[@id=\"dnn_ctr1076_ViewEventListDirectTicketing_ctl01\"]/div[2]/div/div/div").AllAsync();

            foreach (var div in eventDivs)
            {
                try
                {
                    var datespan = div.Locator(".suite_datePlate");

                    var weekday = await datespan.Locator(".suite_day").InnerTextAsync();
                    var numweekday = await datespan.Locator(".suite_day-number").InnerTextAsync();
                    var monthyear = await datespan.Locator(".suite_year").InnerTextAsync();

                    string date = $"{weekday} {numweekday} {monthyear}";
                    string artist = await div.Locator(".Event_H1").InnerTextAsync();
                    string info = await div.Locator(".Event_H2").InnerTextAsync();
                    string venue = "Arena";

                    var span = div.Locator("xpath=.//span[@class='col-md-5  suite_Eventitle']/span[3]");
                    string? halle = await span.TextContentAsync();

                    //if (halle != null && (halle.Contains("Dreiraum") || halle.Contains("Kleine Halle")))
                    //    venue = "Arena Dreiraum";

                    var anchor = div.Locator("a").First;
                    string link = await anchor.GetAttributeAsync("href") ?? "";

                    var ev = new Event
                    {
                        Date = await ParseDate(date),
                        Artist = artist,
                        Venue = venue,
                        Info = info + " | " + halle,
                        Link = link
                    };
                    result.Add(ev);
                }
                catch (Exception ex)
                {
                    var ev = new Event
                    {
                        Venue = "Arena",
                        Info = $"{ex.Message}"
                    };
                    result.Add(ev);
                }
            }

            return result;
        }

        private async Task<DateOnly> ParseDate(string raw)
        {
            // "SA 09 MAI | 2026"
            var cleaned = raw.Replace("|", "").Trim();
            return DateOnly.ParseExact(cleaned, "ddd dd MMM  yyyy", new CultureInfo("de-AT"));
        }
    }
}

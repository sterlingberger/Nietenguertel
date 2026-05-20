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
        private int crawlscount = 0;
        private string url = "https://arena.wien/Home/Programm";
        private IPage _page;
        public ArenaCrawler(IPage page)
        {
            _page = page;
        }

        public async Task<IEnumerable<Event>> FetchAsync()
        {
            List<Event> result = new List<Event>();

            while (crawlscount < 2)
            {
                await _page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                await Task.Delay(2000);

                var eventDivs = await _page.Locator("xpath=//*[@id=\"dnn_ctr1076_ViewEventListDirectTicketing_ctl01\"]/div[2]/div/div/div").AllAsync();

                //Console.WriteLine($"ArenaCrawler fand {eventDivs?.Count} eventDivs");

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

                        //eingrenzen auf die kommendne 2 Monate, der findet sonst zu viel
                        if (ParseDate(date).Month > DateTime.Now.Month + 1)
                            continue;

                        var ev = new Event
                        {
                            Date = ParseDate(date),
                            Artist = artist,
                            Venue = venue,
                            Info = info + " | " + halle,
                            Link = link
                        };
                        result.Add(ev);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Fehler in ArenaCrawler FetchAsync():");
                        Console.WriteLine($"ExceptionMessage: {ex.Message}");
                        Console.WriteLine($"InnerException{ex.InnerException}");

                        var ev = new Event
                        {
                            Venue = "Arena",
                            Info = $"{ex.Message}"
                        };
                        result.Add(ev);
                    }
                }

                // Button klicken um auf nächsten Monat zu wechseln, wir machen vorerst den aktuellen und kommenden
                await _page.ClickAsync("button[type='button'][data-role='next']");

                // Warten bis alle Elemente geladen sind
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Task.Delay(2000);

                url = _page.Url;

                crawlscount++;
            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            // "SA 09 MAI | 2026" oder "MI 03 JUN. | 2026"
            var cleaned = raw.Replace("|", "").Replace(".", "").Trim();
            return DateOnly.ParseExact(cleaned, "ddd dd MMM  yyyy", new CultureInfo("de-AT"));
        }
    }
}

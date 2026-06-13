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
    internal class ArenaCrawler : ICrawler
    {
        private int crawlscount = 0;
        private string url = "https://arena.wien/Home/Programm";
        private IPage _page;
        public ArenaCrawler(IPage page)
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

            while (crawlscount < 2)
            {
                await _page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                await Task.Delay(2000);

                var eventDivs = await _page.Locator("xpath=//*[@id=\"dnn_ctr1076_ViewEventListDirectTicketing_ctl01\"]/div[2]/div/div/div").AllAsync();

                if (eventDivs.Count == 0)
                {
                    throw new InvalidDataException($"Scheint, als würde kein Eventcontainer für {VenueName} gefunden > 0 Events auffindbar, überspringe Crawl");
                }

                //Console.WriteLine($"ArenaCrawler fand {eventDivs?.Count} eventDivs");

                foreach (var div in eventDivs)
                {
                    try
                    {
                        var datespan = div.Locator(".suite_datePlate");

                        var weekday = await datespan.Locator(".suite_day").InnerTextAsync();
                        var numweekday = await datespan.Locator(".suite_day-number").InnerTextAsync();
                        var monthyear = await datespan.Locator(".suite_year").InnerTextAsync();

                        string dateRaw = $"{weekday} {numweekday} {monthyear}";
                        string artist = await div.Locator(".Event_H1").InnerTextAsync();
                        string info = await div.Locator(".Event_H2").InnerTextAsync();

                        var span = div.Locator("xpath=.//span[@class='col-md-5  suite_Eventitle']/span[3]");
                        string? halle = await span.TextContentAsync();

                        var anchor = div.Locator("a").First;
                        string link = await anchor.GetAttributeAsync("href") ?? "";

                        var date = ParseDate(dateRaw);
                        if (date.Month > DateTime.Now.Month + 1)
                            continue;

                        result.Add(new Event
                        {
                            Date = date,
                            Artist = artist,
                            Venue = VenueName,
                            Info = info + " | " + halle,
                            Link = link
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ArenaCrawler: item übersprungen - {ex.Message}");
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

            //info nachträglich setzen
            foreach (Event ev in result)
            {
                try
                {
                    await _page.GotoAsync(ev.Link, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle
                    });

                    var extinfocontainer = _page.Locator("xpath=//div[@id='dnn_ctr577_ViewEventDetail_hgc_RowContainer']/div[@class='suite_calRowContainer teaser']/div[@class='col-md-12']/div[@class='suite_VAdescr']");

                    //bisher nur bei gürtelconnection als mehrere strongs gesehen
                    var elements = await extinfocontainer.Locator("p").AllAsync();

                    foreach (var el in elements)
                    {
                        string text = await el.InnerTextAsync();
                        if (!string.IsNullOrWhiteSpace(text))
                            ev.Info += $" | {text}";
                    }

                    var doorselement = _page.Locator("xpath=//div[@id='dnn_ctr577_ViewEventDetail_hgc_RowContainer']/div[@class='suite_calRowContainer teaser']/div[@class='col-md-12']/div[@class='col-md-3 suite_EvenTime']");
                    var doors = await doorselement.Locator("p").InnerTextAsync();

                    //zeitformat hh:mm
                    var match = Regex.Match(doors, @"\b(\d{1,2}:\d{2})\b");
                    if (match.Success)
                        ev.Start = ev.Date.ToDateTime(TimeOnly.Parse(match.Value));


                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ArenaCrawler: extendedinfo für '{ev.Artist}' nicht abrufbar - {ex.Message}");
                }
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

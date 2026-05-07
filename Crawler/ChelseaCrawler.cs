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

        public string GetName() { return "Chelsea"; }

        public async Task<IEnumerable<Event>> FetchAsync()
        {
            List<Event> result = new List<Event>();

            await _page.GotoAsync(url);

            string eventxpath = "xpath=//html//body//div[@class='main']//table[@class='termindetails']";
            var eventDivs = await _page.Locator(eventxpath).AllAsync();

            string venue = "Chelsea";

            foreach (var div in eventDivs)
            {
                try
                {
                    string date = await div.Locator(".date").InnerTextAsync();
                    string artist = await div.Locator(".band").InnerTextAsync();
                    string info = await div.Locator(".text").InnerTextAsync();

                    var anchor = div.Locator("xpath=preceding-sibling::a[starts-with(@name,'concert_')]");
                    string concertid = await anchor.Last.GetAttributeAsync("name") ?? "";

                    string link = url + $"#{concertid}";

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

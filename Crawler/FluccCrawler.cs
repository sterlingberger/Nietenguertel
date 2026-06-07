using EventCrawler.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EventCrawler.Crawler
{
    internal class FluccCrawler : ICrawler
    {
        private int crawlscount = 0;
        private string urlbase = "https://flucc.at";
        private string url = "https://flucc.at/?filter=Live%20Concert";
        private IPage _page;
        public FluccCrawler(IPage page)
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
            List<Event> tmpresult = new List<Event>();


            await _page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            await Task.Delay(2000);

            //zuerst alle urls für die events selbst sammeln, und dann aus den urls die restliche info bauen
            var eventLinks = await _page.Locator("xpath=//section[@id='events-block']/div[@class='container no-padding']/div[@class='himmel event-list']/ul/li[@class='himmel-card card']/a").AllAsync();

            foreach (var el in eventLinks)
            {
                string? url = urlbase + await el.GetAttributeAsync("href");
                if (url != null)
                    tmpresult.Add(new Event() { Link = url });
            }


            foreach (Event show in tmpresult)
            {
                try
                {
                    await _page.GotoAsync(show.Link, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle
                    });

                    var artistdiv = await _page.Locator("xpath=//section[@id='main-block']/div[@class='container no-padding']/div[@class='grid block-builder']/div[@class='heading-col col col-m-12 larger-font horizontal-center']/div[@class='container']/div[@class='text-content']/h2").AllAsync();
                    string artist = await artistdiv.First().InnerTextAsync();

                    var div = await _page.Locator("xpath=//section[@id='main-block']/div[@class='container no-padding']/div[@class='grid block-builder']/div[@class='event-main-col col col-m-6 ']/div[@class='container']").AllAsync();

                    string dateRaw = await div.First().Locator(".date.uppercase").InnerTextAsync();

                    var infos = await div.First().Locator(".location.uppercase.notranslate").AllInnerTextsAsync();
                    string info = string.Empty;

                    //@WANNE usw
                    foreach (var i in infos)
                        info += i;

                    info += " | ";

                    infos = await _page.Locator("div p[data-block-key]").AllInnerTextsAsync();

                    //Echter text
                    foreach (var i in infos)
                        info += i + "\n";

                    var date = ParseDate(dateRaw);
                    if (date.Month > DateTime.Now.Month + 1)
                        continue;

                    show.Date = date;
                    show.Artist = artist;
                    show.Venue = VenueName;
                    show.Info = info;

                    result.Add(show);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FluccCrawler: item übersprungen ({show.Link}) - {ex.Message}");
                }
            }

            return result;
        }

        private DateOnly ParseDate(string raw)
        {
            // "DI, 26. MAI 2026"
            var cleaned = raw.Replace(",", "").Replace(".", "").Trim();
            // -> "DI 26 MAI 2026"
            return DateOnly.ParseExact(cleaned, "ddd dd MMM yyyy", new CultureInfo("de-AT"));
        }
    }
}

using EventCrawler.Crawler;
using Microsoft.Playwright;
using System.Text.Json;

namespace EventCrawler
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //the following lines into Main(). These will initialize Playwright, launch a Chromium window, and open a new page:
            // initialize a Playwright instance to
            // perform browser automation
            using var playwright = await Playwright.CreateAsync();

            // initialize a Chromium instance
            await using var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = true, // set to "false" while developing
            });

            // open a new page within the current browser context
            var page = await browser.NewPageAsync();

            //Crwaler initialisieren
            ICrawler[] crawlers = [new ArenaCrawler(page), new ChelseaCrawler(page), new KramladenCrawler(page)];

            //alle events sammeln
            var allEvents = new List<Event>();

            foreach (var crawler in crawlers)
            {
                var events = await crawler.FetchAsync();
                allEvents.AddRange(events);
            }

            var json = JsonSerializer.Serialize(allEvents, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync("events.json", json);
        }
    }
}

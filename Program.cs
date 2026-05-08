using EventCrawler.Crawler;
using Microsoft.Playwright;
using System.Diagnostics;
using System.Text.Json;

namespace EventCrawler
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();

            //the following lines into Main(). These will initialize Playwright, launch a Chromium window, and open a new page:
            // initialize a Playwright instance to
            // perform browser automation
            using var playwright = await Playwright.CreateAsync();

            // initialize a Chromium instance
            await using var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = true, // set to "false" while developing
            });

            Console.WriteLine($"Init dauerte {sw.ElapsedMilliseconds} ms");


            // open a new page within the current browser context
            var page = await browser.NewPageAsync();

            //Crwaler initialisieren
            ICrawler[] crawlers = [
                new ChelseaCrawler(page),
                new ArenaCrawler(page),
                new KramladenCrawler(page),
                new ViperRoomCrawler(page),
                new RhizCrawler(page),
                new B72Crawler(page)
                ];

            //alle events sammeln
            var allEvents = new List<Event>();

            sw.Restart();

            foreach (var crawler in crawlers)
            {
                var events = await crawler.FetchAsync();
                allEvents.AddRange(events);

                Console.WriteLine($"{crawler.GetType().Name} lieferte {events.Count()} events in {sw.ElapsedMilliseconds} ms");
                sw.Restart();
            }

            sw.Restart();

            var json = JsonSerializer.Serialize(allEvents, new JsonSerializerOptions { WriteIndented = true });
            var dataPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data");
            Directory.CreateDirectory(dataPath);
            await File.WriteAllTextAsync(Path.Combine(dataPath, "events.json"), json);

            Console.WriteLine($"events.json geschrieben in {sw.ElapsedMilliseconds} ms");
            sw.Stop();
        }
    }
}

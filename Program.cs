using EventCrawler.Crawler;
using EventCrawler.Models;
using Microsoft.Playwright;
using System.Diagnostics;
using System.Text;
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

            // veraltete events aus der bestehenden events.json entfernen
            var dataPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data");
            var eventsFilePath = Path.Combine(dataPath, "events.json");
            List<Event> existingEvents = [];
            if (File.Exists(eventsFilePath))
            {
                var content = await File.ReadAllTextAsync(eventsFilePath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    existingEvents = JsonSerializer.Deserialize<List<Event>>(content) ?? [];
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    existingEvents = existingEvents.Where(e => e.Date >= today).ToList();
                    Console.WriteLine($"events.json bereinigt: {existingEvents.Count} aktuelle events behalten");
                    await File.WriteAllTextAsync(eventsFilePath, JsonSerializer.Serialize(existingEvents, new JsonSerializerOptions { WriteIndented = true }));
                }
            }

            // open a new page within the current browser context
            var page = await browser.NewPageAsync();

            //Venues initialisieren
            Venue[] venues = [
                new Venue("Arena", new string[]{"U3"}, new ArenaCrawler(page)),
                new Venue("Chelsea", new string[]{"U6"}, new ChelseaCrawler(page)),
                new Venue("Kramladen", new string[]{"U6"}, new KramladenCrawler(page)),
                new Venue("Viper Room", new string[]{"U3"}, new ViperRoomCrawler(page)),
                new Venue("Rhiz", new string[]{"U6"}, new RhizCrawler(page)),
                new Venue("B72", new string[]{"U6"}, new B72Crawler(page)),
                new Venue("Venster99", new string[]{"U6"}, new Venster99Crawler(page)),
                new Venue("Szene", new string[]{"U3"}, new SzeneCrawler(page)),
                new Venue("Cafe Carina", new string[]{"U6"}, new CarinaCrawler(page)),
                new Venue("Flucc", new string[]{"U1","U2"}, new FluccCrawler(page)),
                ];

            //alle events sammeln
            var allEvents = new List<Event>();

            sw.Restart();

            foreach (var venue in venues)
            {
                var crawler = venue.Crawler;
                Console.WriteLine($"{crawler.GetType().Name} ...");

                IEnumerable<Event> events;
                try
                {
                    events = await crawler.FetchAsync();
                    Console.WriteLine($"lieferte {events.Count()} events in {sw.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FEHLER - behalte bestehende events aus events.json");
                    Console.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
                    events = existingEvents.Where(e => e.Venue == venue.Name);
                }
                allEvents.AddRange(events);
                sw.Restart();
            }

            //nur diesen und nächsten monat berücksichtigen
            //könnte man im crawler machen, aber vielleicht will ich das wieder entfernen
            allEvents.RemoveAll(e => e.Date.Month > DateTime.Now.Month + 1);
            allEvents.RemoveAll(e => e.Date.Year > DateTime.Now.Year);
            allEvents.RemoveAll(e => e.Date < DateOnly.FromDateTime(DateTime.Today));

            sw.Restart();

            var json = JsonSerializer.Serialize(allEvents, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(dataPath);
            await File.WriteAllTextAsync(Path.Combine(dataPath, "events.json"), json);

            Console.WriteLine($"events.json geschrieben in {sw.ElapsedMilliseconds} ms");

            sw.Restart();

            // ICS-Dateien generieren
            var calendarPath = Path.Combine(dataPath, "calendar");
            Directory.CreateDirectory(calendarPath);

            // Veraltete ICS löschen bevor neu geschrieben wird
            foreach (var f in Directory.GetFiles(calendarPath, "*.ics"))
                File.Delete(f);

            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            foreach (var ev in allEvents)
                await File.WriteAllTextAsync(Path.Combine(calendarPath, ev.IcsFileName), BuildIcs(ev), utf8NoBom);

            Console.WriteLine($"{allEvents.Count} ICS-Dateien geschrieben in {sw.ElapsedMilliseconds} ms");

            sw.Stop();
        }

        static string BuildIcs(Event ev)
        {
            // Sonderzeichen gemäß RFC 5545 escapen
            static string Esc(string s) =>
                s.Replace("\\", "\\\\")
                 .Replace(",",  "\\,")
                 .Replace(";",  "\\;")
                 .Replace("\r\n", " ")
                 .Replace("\n",   " ");

            var dtStamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
            var dtStart = ev.Start.ToString("yyyyMMdd'T'HHmmss");
            var dtEnd   = ev.End.ToString("yyyyMMdd'T'HHmmss");
            var uid     = $"{ev.Date:yyyyMMdd}-{ev.Venue}-{ev.Artist}@eventcrawler"
                            .Replace(" ", "-");

            var sb = new StringBuilder();
            sb.Append("BEGIN:VCALENDAR\r\n");
            sb.Append("VERSION:2.0\r\n");
            sb.Append("PRODID:-//EventCrawler//EventCrawler//DE\r\n");
            sb.Append("BEGIN:VEVENT\r\n");
            sb.Append($"UID:{uid}\r\n");
            sb.Append($"DTSTAMP:{dtStamp}\r\n");
            sb.Append($"DTSTART:{dtStart}\r\n");
            sb.Append($"DTEND:{dtEnd}\r\n");
            sb.Append($"SUMMARY:{Esc(ev.Artist) + " (Uhrzeit prüfen)"}\r\n");
            sb.Append($"LOCATION:{Esc(ev.Venue)}\r\n");
            sb.Append($"DESCRIPTION:{Esc(ev.Info)}\r\n");
            sb.Append($"URL:{ev.Link}\r\n");
            sb.Append("END:VEVENT\r\n");
            sb.Append("END:VCALENDAR\r\n");
            return sb.ToString();
        }
    }
}

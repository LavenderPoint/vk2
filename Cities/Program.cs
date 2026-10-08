using System.Text.Json;

namespace WeatherApp
{
    public class WeatherData
    {
        public string City { get; set; }
        public int TempC { get; set; }
        public string Country { get; set; }

        public override string ToString()
        {
            return $"{City}, {Country} {TempC.ToString("+0;-0;0")}°C";
        }
    }

    class Program
    {
        static async Task Main()
        {
            string file = "vk-task-14.txt";

            if (!File.Exists(file))
            {
                Console.WriteLine($"Файл '{file}' не найден.");
                return;
            }

            List<string> cities = new List<string>();
            foreach (string line in File.ReadLines(file))
            {
                string trim = line.Trim();
                if (!string.IsNullOrEmpty(trim))
                {
                    cities.Add(trim);
                }
            }

            List<WeatherData> weatherRecords = new List<WeatherData>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "MyWeatherClient");

                foreach (string city in GetUniqueCities(cities))
                {
                    WeatherData record = await FetchWeatherAsync(client, city);
                    if (record != null)
                    {
                        weatherRecords.Add(record);
                    }
                }
            }

            Console.WriteLine("Погода по городам:");
            foreach (WeatherData record in weatherRecords)
            {
                Console.WriteLine(record);
            }

            Console.WriteLine();
            Console.WriteLine("Погода по странам:");

            var grouped = weatherRecords.GroupBy(x => x.Country).OrderBy(y => y.Key);

            foreach (var group in grouped)
            {
                int count = group.Count();
                double average = group.Average(x => x.TempC);
                int min = group.Min(x => x.TempC);
                int max = group.Max(x => x.TempC);

                Console.WriteLine(
                    $"{group.Key} — {count} cities, " + $"avg: {average.ToString("+0.0;-0.0;0.0")}°C, " + $"min: {min.ToString("+0;-0;0")}°C, " +
                    $"max: {max.ToString("+0;-0;0")}°C");
            }
        }

        static IEnumerable<string> GetUniqueCities(List<string> cities)
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (string city in cities)
            {
                if (seen.Add(city))
                {
                    yield return city;
                }
            }
        }

        static async Task<WeatherData?> FetchWeatherAsync(HttpClient client, string city)
        {
            try
            {
                string encodedCity = Uri.EscapeDataString(city);
                string url = $"https://wttr.in/{encodedCity}?format=j1";

                string response = await client.GetStringAsync(url);

                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    JsonElement root = doc.RootElement;
                    int tempC = int.Parse(root.GetProperty("current_condition")[0].GetProperty("temp_C").GetString());
                    string country = root.GetProperty("nearest_area")[0].GetProperty("country")[0].GetProperty("value").GetString();

                    return new WeatherData
                    {
                        City = city,
                        TempC = tempC,
                        Country = country
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось получить данные для {city}: {ex.Message}");
                return null;
            }
        }
    }
}
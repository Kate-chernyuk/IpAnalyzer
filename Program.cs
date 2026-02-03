using Newtonsoft.Json;


class IpData
{
    public string ip { get; set; }
    public string hostname { get; set; }
    public string city { get; set; }
    public string region { get; set; }
    public string country { get; set; }
    public string loc {  get; set; }
    public string org { get; set; }
    public string postal { get; set; }
    public string timezone { get; set; }
    public string readme { get; set; }

}

class IpAnalyzer
{
    public static async Task Main()
    {
        string path = @"IPs.txt";

        if (!File.Exists(path))
        {
            Console.WriteLine("Отсутствует искомый файл");
            return;
        }


        string[] IPs = File.ReadAllLines(path).Where(ip => !string.IsNullOrWhiteSpace(ip)).ToArray();
        List<IpData> ipDatas = new List<IpData>();

        Console.WriteLine("Список анализируемых IP-адресов:");
        foreach (string ip in IPs)
        {
            Console.WriteLine(ip);
        }


        using (HttpClient httpClient = new HttpClient()) {
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var tasks = IPs.Select(ip => SendGET(ip, httpClient));
            IpData[] results = await Task.WhenAll(tasks);
            ipDatas = results.Where(r => r != null).ToList();
        }

        if (ipDatas.Count > 0)
        {
            Console.WriteLine("\nСтатистика по странам:");
            string MostPopularCountry = AnalyzeIPDatasByCountries(ipDatas);
            Console.WriteLine($"\nСамая часто встречающаяся страна: {MostPopularCountry}");

            Console.WriteLine("Города этой страны, жителям которых принадлежат анализируемые ip-адреса:");
            List<string> KnownCitiesOfMostPopularCountry = GetAllKnownCitiesInCountry(ipDatas, MostPopularCountry);
            foreach (string city in KnownCitiesOfMostPopularCountry)
            {
                Console.WriteLine(city);
            }
        }
        else
        {
            Console.WriteLine("Нет данных для анализа");
        }

    }

    private static async Task<IpData> SendGET(string ip, HttpClient httpClient)
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync("https://ipinfo.io/" + ip + "/json");
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();

            IpData ipData = JsonConvert.DeserializeObject<IpData>(jsonResponse);

            return ipData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке IP {ip}: {ex.Message}");
            return null;
        }
    }

    private static string AnalyzeIPDatasByCountries(List<IpData> ipDatas)
    {
        Dictionary<string, int> dict = new Dictionary<string, int>();
        foreach (IpData ipData in ipDatas)
        {
            if (!string.IsNullOrEmpty(ipData.country))
            {
                if (!dict.ContainsKey(ipData.country))
                {
                    dict[ipData.country] = 1;
                }
                else
                {
                    dict[ipData.country]++;
                }
            }
                
        }

        string MostPopularCountry = "";
        int MostPopularCountryValue = -1;

        foreach (string country in dict.Keys)
        {
            Console.WriteLine($"{country}: {dict[country]}" );
            if (dict[country] > MostPopularCountryValue)
            {
                MostPopularCountry = country;
                MostPopularCountryValue = dict[country];
            }
        }

        return MostPopularCountry;
        
    }

    private static List<String> GetAllKnownCitiesInCountry(List<IpData> ipDatas, string country)
    {
        List<string> KnownCitiesOfMostPopularCountry = ipDatas
            .Where(ip => ip.country == country && !string.IsNullOrEmpty(ip.country))
            .Select(ip => ip.city)
            .Distinct()
            .ToList();

        return KnownCitiesOfMostPopularCountry;
    }
}
using System;
using System.Net;

namespace SkyKick.NinjectWorkshop.WordCounting.Prototype
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("Enter Url: ");

                var url = Console.ReadLine();

                Console.WriteLine($"Number of words on [{url}]: {CountWordsOnUrl(url)}");
                Console.WriteLine();
            }
        }

        static int CountWordsOnUrl(string url)
        {
            string html = string.Empty;
            using (var webClient = new WebClient())
                html = webClient.DownloadString(url);
            
            var text = new CsQuery.CQ(html).Text();

            return text.Split(' ').Length;
        }
    }
}

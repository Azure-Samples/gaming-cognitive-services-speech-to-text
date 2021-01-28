using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Web;
using System.Globalization;
using System.Text;

namespace STTClient
{
    class Program
    {
        static void Main(string[] args)
        {
            SendData();
            Console.ReadLine();
        }

        static async void SendData()
        {
            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {

                    Console.WriteLine("Starting client");

                    // Set the HTTP method
                    request.Method = HttpMethod.Post;

                    // Construct the URI
                    request.RequestUri = new Uri("https://" + Constants.STTFunction + ".azurewebsites.net/api/speechtotext?code=" + Constants.STTKey);

                    // Set the content to be the WAV file at 256 kbps, 16 kHz, mono
                    request.Content = new ByteArrayContent(File.ReadAllBytes("sample.wav"));

                    // Set language
                    request.Headers.Add("Language", "en-US");

                    Console.WriteLine("Sending audio sample");

                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        Console.WriteLine("Response received");
                        
                        response.EnsureSuccessStatusCode();

                        // Asynchronously read the response
                        using (Stream dataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            using (var sr = new StreamReader(dataStream))
                            {
                                Console.WriteLine("Answer: " + sr.ReadToEnd());
                            }
                        }
                    }
                }
            }
        }
    }
}

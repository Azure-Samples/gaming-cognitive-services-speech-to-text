using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SpeechToText
{
    public static class SpeechToText
    {
        [FunctionName("SpeechToText")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var exceptions = new List<Exception>();

            try
            {
                log.LogInformation("Starting function");

                if (req.Body.Length == 0)
                {
                    log.LogInformation("Invalid request");
                    return new NoContentResult();
                }

                string CSSubscriptionKey = Environment.GetEnvironmentVariable("SPEECH_KEY");
                string accessToken;

                // Add your subscription key here
                // If your resource isn't in WEST US, change the endpoints
                Authentication auth = new Authentication("https://westus.api.cognitive.microsoft.com/sts/v1.0/issueToken", CSSubscriptionKey);
                string host = "https://westus.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1";

                try
                {
                    accessToken = await auth.FetchTokenAsync().ConfigureAwait(false);
                    log.LogInformation("Successfully obtained an access token");
                }
                catch (Exception)
                {
                    log.LogInformation("Failed to obtain an access token");
                    return new UnauthorizedResult();
                }

                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage())
                    {
                        StringValues lang;

                        if (req.Headers.TryGetValue("Language", out lang))
                        {
                            lang = "en-US";
                        }

                        // Set the HTTP method
                        request.Method = HttpMethod.Post;

                        // Construct the URI
                        request.RequestUri = new Uri(host + "?language=" + lang);

                        // Set the content to be the WAV file at 256 kbps, 16 kHz, mono
                        byte[] AudioFile = new byte[req.Body.Length];
                        if (req.Body.Read(AudioFile, 0, (int)req.Body.Length) != req.Body.Length)
                        {
                            log.LogInformation("Invalid audio file");
                            return new NoContentResult();
                        }
                        request.Content = new ByteArrayContent(AudioFile);

                        // Set additional header, such as Authorization and Content-type
                        request.Headers.Add("Authorization", "Bearer " + accessToken);
                        request.Content.Headers.TryAddWithoutValidation("Content-Type", "audio/wav; codecs=audio/pcm; samplerate=16000");

                        // Create a request
                        log.LogInformation("Calling the STT service. Please wait...");

                        using (var response = await client.SendAsync(request).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();

                            // Asynchronously read the response
                            string reponseJSON = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            JObject jsonObject = JObject.Parse(reponseJSON);
                            string textResponse = jsonObject.Value<string>("DisplayText");

                            log.LogInformation($"Translation: {textResponse}");

                            return new OkObjectResult(textResponse);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // We need to keep processing the rest of the batch - capture this exception and continue.
                // Also, consider capturing details of the message that failed processing so it can be processed again later.
                exceptions.Add(e);
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.
            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();

            return null;
        }

        public class Authentication
        {
            private string subscriptionKey;
            private string tokenFetchUri;

            public Authentication(string tokenFetchUri, string subscriptionKey)
            {
                if (string.IsNullOrWhiteSpace(tokenFetchUri))
                {
                    throw new ArgumentNullException(nameof(tokenFetchUri));
                }
                if (string.IsNullOrWhiteSpace(subscriptionKey))
                {
                    throw new ArgumentNullException(nameof(subscriptionKey));
                }

                this.tokenFetchUri = tokenFetchUri;
                this.subscriptionKey = subscriptionKey;
            }

            public async Task<string> FetchTokenAsync()
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this.subscriptionKey);
                    UriBuilder uriBuilder = new UriBuilder(this.tokenFetchUri);

                    var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null).ConfigureAwait(false);
                    return await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
        }
    }
}

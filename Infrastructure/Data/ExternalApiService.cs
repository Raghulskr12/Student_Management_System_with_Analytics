using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class ExternalApiService
    {
        private static readonly HttpClient _httpClient = new();

        public async Task<string> FetchExternalDataAsync(int studentId, CancellationToken cancellationToken)
        {
            // Using JSONPlaceholder API to fetch placeholder post titles as external data strings
            string url = $"https://jsonplaceholder.typicode.com/posts/{((studentId - 1) % 100) + 1}";
            int maxRetries = 3;
            int delayMilliseconds = 1000;

            for (int retry = 1; retry <= maxRetries; retry++)
            {
                try
                {
                    // Throws an exception if the cancellation token timeout has triggered
                    cancellationToken.ThrowIfCancellationRequested();

                    var startTime = DateTime.Now;
                    HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    string jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                    using JsonDocument doc = JsonDocument.Parse(jsonString);
                    string title = doc.RootElement.GetProperty("title").GetString() ?? "No Data";

                    var duration = DateTime.Now - startTime;
                    Console.WriteLine($"[BATCH] Student ID {studentId}: Fetch finished in {duration.TotalMilliseconds:F0}ms (Try #{retry})");
                    
                    return title;
                }
                catch (OperationCanceledException)
                {
                    throw; // Bubble up timeout cancellations directly
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[WARN] Student ID {studentId}: Try #{retry} failed ({ex.Message}).");
                    Console.ResetColor();

                    if (retry == maxRetries)
                    {
                        return $"Failed to load external insights after {maxRetries} attempts.";
                    }

                    // Linear backoff delay before trying again
                    await Task.Delay(delayMilliseconds * retry, cancellationToken);
                }
            }

            return "No Data Available";
        }
    }
}
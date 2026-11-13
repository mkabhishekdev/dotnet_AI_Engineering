using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

// Read the saved API key.
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string? apiKey = configuration["Gemini:ApiKey"];

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Gemini API key is missing.");
    return;
}

// Choose the model and its API endpoint.
const string model = "gemini-3.1-flash-lite";

string endpoint =
    $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

// Create the HTTP client and attach authentication.
using var client = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(60)
};

client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey.Trim());

// Build the message we want to send.
var requestBody = new
{
    contents = new[]
    {
        new
        {
            role = "user",
            parts = new[]
            {
                new
                {
                    text = "Explain dependency injection in two simple sentences."
                }
            }
        }
    },
    generationConfig = new
    {
        maxOutputTokens = 512
    }
};

// Send the request and read the response.
Console.WriteLine("Sending your question to Gemini...");

using var response = await client.PostAsJsonAsync(endpoint, requestBody);

string responseBody = await response.Content.ReadAsStringAsync();

if (!response.IsSuccessStatusCode)
{
    Console.WriteLine($"HTTP error: {(int)response.StatusCode}");
    Console.WriteLine(responseBody);
    return;
}

using var document = JsonDocument.Parse(responseBody);
var root = document.RootElement;

if (!root.TryGetProperty("candidates", out var candidates)
    || candidates.GetArrayLength() == 0
    || !candidates[0].TryGetProperty("content", out var content)
    || !content.TryGetProperty("parts", out var parts))
{
    Console.WriteLine("Gemini returned no answer text.");
    return;
}

var answerParts = new List<string>();

foreach (var part in parts.EnumerateArray())
{
    // Skip any parts marked as internal thought.
    if (part.TryGetProperty("thought", out var thought)
        && thought.ValueKind == JsonValueKind.True)
    {
        continue;
    }

    if (part.TryGetProperty("text", out var text))
    {
        answerParts.Add(text.GetString() ?? "");
    }
}

string answer = string.Join("", answerParts);

Console.WriteLine("Gemini:");
Console.WriteLine(string.IsNullOrWhiteSpace(answer) ? "No answer text was returned." : answer);
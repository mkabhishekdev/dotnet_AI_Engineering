using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string? apiKey = configuration["Gemini:ApiKey"];

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Gemini API key is missing.");
    return;
}

Console.WriteLine("Gemini API key loaded.");

Console.Write("Enter a phrase: ");
string phrase = Console.ReadLine() ?? "";

string[] words = phrase.Split(
    (char[]?)null,
    StringSplitOptions.RemoveEmptyEntries);

Console.WriteLine($"Words ({words.Length}): {string.Join(" | ", words)}");

if (string.IsNullOrWhiteSpace(phrase))
{
    Console.WriteLine("Enter a phrase to measure.");
    return;
}

const string model = "gemini-3.1-flash-lite";
string endpoint =
    $"https://generativelanguage.googleapis.com/v1beta/models/{model}:countTokens";

using var client = new HttpClient();
client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey.Trim());

var requestBody = new
{
    contents = new[]
    {
        new { parts = new[] { new { text = phrase } } }
    }
};

using var response = await client.PostAsJsonAsync(endpoint, requestBody);

if (!response.IsSuccessStatusCode)
{
    Console.WriteLine($"Token count failed: HTTP {(int)response.StatusCode}");
    Console.WriteLine(await response.Content.ReadAsStringAsync());
    return;
}

var result = await response.Content.ReadFromJsonAsync<JsonElement>();
int tokenCount = result.GetProperty("totalTokens").GetInt32();

Console.WriteLine($"Gemini input tokens: {tokenCount}");
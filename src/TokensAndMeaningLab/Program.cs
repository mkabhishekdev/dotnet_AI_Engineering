using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

if (args.Contains("--attention"))
{
    RunAttentionDemo();
    return;
}

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

//asking user input
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

// the above same idea applies to counting tokens using the Gemini API
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

// parse the response to get the total token count
var result = await response.Content.ReadFromJsonAsync<JsonElement>();
int tokenCount = result.GetProperty("totalTokens").GetInt32();

Console.WriteLine($"Gemini input tokens: {tokenCount}");

/* Keep this example
 A dog is mans best friend
 there is a 4 legged animal in this planet which is a great friend to a human
 masala dosa is a delicious south Indian breakfast
*/

// request an embedding for the input phrase using the Gemini embedding model
float[]? referenceVector = await GetEmbeddingAsync(client, phrase);
if (referenceVector is null)
{
    return;
}

Console.WriteLine($"Embedding dimensions: {referenceVector.Length}");
Console.WriteLine($"First 5 values: {string.Join(", ", referenceVector.Take(5))}");

string[] candidates =
[
    "there is a 4 legged animal in this planet which is dearer to a human",
    "a hen lays eggs which are eaten by humans and help them in their diet",
    "masala dosa is a delicious south Indian breakfast"
];

var matches = new List<(string Text, double Score)>();

foreach (string candidate in candidates)
{
    float[]? candidateVector = await GetEmbeddingAsync(client, candidate);
    if (candidateVector is null)
    {
        continue;
    }

    double score = CosineSimilarity(referenceVector, candidateVector);
    matches.Add((candidate, score));
}

Console.WriteLine("\nMatches ranked by similarity");

foreach (var match in matches.OrderByDescending(match => match.Score))
{
    Console.WriteLine($"{match.Score:F4}  {match.Text}");
}

static async Task<float[]?> GetEmbeddingAsync(HttpClient client, string text)
{
    const string endpoint =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent";

    var request = new
    {
        taskType = "SEMANTIC_SIMILARITY",
        content = new
        {
            parts = new[] { new { text } }
        }
    };

    using var response = await client.PostAsJsonAsync(endpoint, request);
    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Embedding request failed: HTTP {(int)response.StatusCode}");
        return null;
    }

    var result = await response.Content.ReadFromJsonAsync<JsonElement>();

    return result.GetProperty("embedding")
        .GetProperty("values")
        .EnumerateArray()
        .Select(value => value.GetSingle())
        .ToArray();
}

static double CosineSimilarity(float[] left, float[] right)
{
    if (left.Length != right.Length)
    {
        throw new ArgumentException("The vectors must have the same number of dimensions.");
    }

    double dotProduct = 0;
    double leftLengthSquared = 0;
    double rightLengthSquared = 0;

    for (int i = 0; i < left.Length; i++)
    {
        double x = left[i];
        double y = right[i];

        dotProduct += x * y;
        leftLengthSquared += x * x;
        rightLengthSquared += y * y;
    }

    if (leftLengthSquared == 0 || rightLengthSquared == 0)
    {
        throw new ArgumentException("Cosine similarity requires nonzero vectors.");
    }

    return dotProduct /
        (Math.Sqrt(leftLengthSquared) * Math.Sqrt(rightLengthSquared));
}

static void RunAttentionDemo()
{
    double riverScore = 4;
    double loanScore = 1;

    // Softmax turns the scores into weights that sum to 1.
    double riverExp = Math.Exp(riverScore);
    double loanExp = Math.Exp(loanScore);
    double total = riverExp + loanExp;

    double riverWeight = riverExp / total;
    double loanWeight = loanExp / total;

    // Made-up two-dimensional value vectors.
    var river = (X: 1.0, Y: 0.0);
    var loan = (X: 0.0, Y: 1.0);

    double outputX = riverWeight * river.X + loanWeight * loan.X;
    double outputY = riverWeight * river.Y + loanWeight * loan.Y;

    Console.WriteLine($"Weight on river: {riverWeight:F4}");
    Console.WriteLine($"Weight on loan:  {loanWeight:F4}");
    Console.WriteLine($"Attention output for bank: [{outputX:F4}, {outputY:F4}]");
}


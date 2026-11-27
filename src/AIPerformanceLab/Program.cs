using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using TokensAndMeaningLab;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(GeminiEmbeddingClient).Assembly)
    .Build();

string? apiKey = configuration["Gemini:ApiKey"];

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Gemini API key is missing.");
    return;
}

using var client = new HttpClient();
client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey.Trim());

string[] queries =
[
    "Dogs are loyal companions.",
    "Why do people keep dogs as pets?"
];

string[] candidates =
[
    "The database backup runs every night.",
    "The word dog has three letters.",
    "A dog is man's best friend."
];

if (args.Contains("--batch"))
{
    var timer = Stopwatch.StartNew();
    var vectors = await GeminiEmbeddingClient.GetEmbeddingsAsync(client, candidates);
    timer.Stop();

    if (vectors is null)
    {
        return;
    }

    Console.WriteLine($"Batched: {timer.ElapsedMilliseconds} ms");
    Console.WriteLine("HTTP requests: 1");
    Console.WriteLine($"Vectors received: {vectors.Length}");
    return;
}

if (args.Contains("--precision"))
{
    const string query = "Dogs are loyal companions.";
    string[] texts = [query, .. candidates];

    var vectors = await GeminiEmbeddingClient.GetEmbeddingsAsync(client, texts);

    if (vectors is null || vectors.Length != texts.Length)
    {
        Console.WriteLine("Could not get all embeddings.");
        return;
    }

    var queryVector = vectors[0];

    Console.WriteLine($"Query: {query}");
    Console.WriteLine($"Coordinates per vector: {queryVector.Length:N0}");
    Console.WriteLine($"Bytes per vector as float: {queryVector.Length * 4:N0}");
    Console.WriteLine($"Bytes per vector as Half: {queryVector.Length * 2:N0}");

    for (int i = 0; i < candidates.Length; i++)
    {
        var original = vectors[i + 1];
        Half[] stored = Array.ConvertAll(original, value => (Half)value);
        float[] restored = Array.ConvertAll(stored, value => (float)value);

        double originalScore = VectorMath.CosineSimilarity(queryVector, original);
        double reducedScore = VectorMath.CosineSimilarity(queryVector, restored);

        Console.WriteLine($"{originalScore:F6} → {reducedScore:F6} | {candidates[i]}");
    }

    return;
}

if (args.Contains("--compare-requests"))
{
    var timer = Stopwatch.StartNew();

    foreach (string candidate in candidates)
    {
        var vector = await GeminiEmbeddingClient.GetEmbeddingAsync(client, candidate);
        if (vector is null)
        {
            return;
        }
    }

    timer.Stop();
    long sequentialMs = timer.ElapsedMilliseconds;

    timer.Restart();

    var requests = candidates
        .Select(candidate => GeminiEmbeddingClient.GetEmbeddingAsync(client, candidate))
        .ToArray();

    var vectors = await Task.WhenAll(requests);
    timer.Stop();

    if (vectors.Any(vector => vector is null))
    {
        return;
    }

    Console.WriteLine($"Sequential: {sequentialMs} ms");
    Console.WriteLine($"Concurrent: {timer.ElapsedMilliseconds} ms");
    Console.WriteLine($"Embedding calls per approach: {candidates.Length}");
    return;
}


var candidateCache = new Dictionary<string, float[]>(StringComparer.Ordinal);

foreach (string query in queries)
{
    Console.WriteLine($"\nSearch: {query}");
    var timer = Stopwatch.StartNew();

    int geminiCalls = 1; // One call for this query.
    float[]? queryVector =
        await GeminiEmbeddingClient.GetEmbeddingAsync(client, query);

    if (queryVector is null)
    {
        return;
    }

    var matches = new List<(string Text, double Score)>();

    foreach (string candidate in candidates)
    {
        if (!candidateCache.TryGetValue(candidate, out var candidateVector))
        {
            geminiCalls++;
            candidateVector =
                await GeminiEmbeddingClient.GetEmbeddingAsync(client, candidate);

            if (candidateVector is null)
            {
                return;
            }

            candidateCache.Add(candidate, candidateVector);
        }

        double score = VectorMath.CosineSimilarity(queryVector, candidateVector);
        matches.Add((candidate, score));
    }

    timer.Stop();

    foreach (var match in matches.OrderByDescending(match => match.Score))
    {
        Console.WriteLine($"{match.Score:F4}  {match.Text}");
    }

    Console.WriteLine($"Gemini embedding calls: {geminiCalls}");
    Console.WriteLine($"Time to score candidates: {timer.ElapsedMilliseconds} ms");
}
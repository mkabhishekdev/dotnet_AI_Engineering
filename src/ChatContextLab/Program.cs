using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;

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


Console.Clear();
Console.Write("\u001b[3J");
Console.SetCursorPosition(0, 0);

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("DotNet AI Chat App");
Console.ResetColor();

Console.WriteLine("Your .NET AI learning assistant");
Console.WriteLine();

Console.WriteLine("Type a message and press Enter.");
Console.WriteLine("Commands:");
Console.WriteLine("  clear — Start a fresh conversation");
Console.WriteLine("  exit  — Close the chat session");
Console.WriteLine();

var history = new List<JsonElement>();

while (true)
{
    Console.Write("You: ");
    string? userPrompt = Console.ReadLine();

    if (userPrompt is null)
    {
        break;
    }

    if (userPrompt.Trim().Equals(
        "exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Chat ended.");
        break;
    }

    if (userPrompt.Trim().Equals(
    "clear", StringComparison.OrdinalIgnoreCase))
    {
        history.Clear();
        Console.WriteLine("Conversation cleared. Starting fresh.");
        continue;
    }

    if (string.IsNullOrWhiteSpace(userPrompt))
    {
        Console.WriteLine("Please enter a question.");
        continue;
    }

    // Build the message we want to send.
    var userMessage = JsonSerializer.SerializeToElement(new
    {
        role = "user",
        parts = new[]
        {
            new { text = userPrompt }
        }
    });

    // Send previous messages plus the current question.
    var messagesForRequest = new List<JsonElement>(history)
    {
        userMessage
    };
    
    //NOTE: systemInstructions -> parts -> text: is an example of Prompting, where we are guiding what LLM has to 
    // consider while giving answers
    var requestBody = new
    {
        systemInstruction = new
        {
            parts = new[]
        {
            new
            {
                text = """
                    Explain in simple English.
                    Use plain text without Markdown formatting.
                    If some information needs usage of flow diagrams, include them appropriately.
                    """
            }
        }
        },
        contents = messagesForRequest,
        generationConfig = new
        {
            maxOutputTokens = 512
        }
    };

    // Send the request and read the response.
    Console.WriteLine("Sending your question to Gemini...");

    var requestTimer = Stopwatch.StartNew();

    using var response = await client.PostAsJsonAsync(endpoint, requestBody);

    string responseBody = await response.Content.ReadAsStringAsync();
    
    requestTimer.Stop();

    Console.WriteLine(
    $"Response time: {requestTimer.Elapsed.TotalSeconds:F2} seconds");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"HTTP error: {(int)response.StatusCode}");
        Console.WriteLine(responseBody);
        continue;
    }

    using var document = JsonDocument.Parse(responseBody);
    var root = document.RootElement;

    if (root.TryGetProperty("usageMetadata", out var usage))
    {
        if (usage.TryGetProperty("promptTokenCount", out var inputTokens))
        {
            Console.WriteLine($"Input tokens: {inputTokens.GetInt32()}");
        }

        if (usage.TryGetProperty("candidatesTokenCount", out var outputTokens))
        {
            Console.WriteLine($"Answer tokens: {outputTokens.GetInt32()}");
        }

        if (usage.TryGetProperty("thoughtsTokenCount", out var thinkingTokens))
        {
            Console.WriteLine($"Thinking tokens: {thinkingTokens.GetInt32()}");
        }

        if (usage.TryGetProperty("totalTokenCount", out var totalTokens))
        {
            Console.WriteLine($"Total tokens: {totalTokens.GetInt32()}");
        }
    }

    if (!root.TryGetProperty("candidates", out var candidates)
        || candidates.GetArrayLength() == 0
        || !candidates[0].TryGetProperty("content", out var content)
        || !content.TryGetProperty("parts", out var parts))
    {
        Console.WriteLine("Gemini returned no answer text.");
        continue;
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
    answer = answer.Replace("**", "");
    if (!string.IsNullOrWhiteSpace(answer))
    {
        history.Add(userMessage);
        history.Add(content.Clone());
    }

    Console.WriteLine("Gemini:");
    Console.WriteLine(string.IsNullOrWhiteSpace(answer) ? "No answer text was returned." : answer);
    Console.WriteLine(); // Add an empty line for better readability between interactions.
}
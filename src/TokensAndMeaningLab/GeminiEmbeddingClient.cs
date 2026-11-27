using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace TokensAndMeaningLab
{
    public static class GeminiEmbeddingClient
    {
        public static async Task<float[]?> GetEmbeddingAsync(HttpClient client, string text)
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

        public static async Task<float[][]?> GetEmbeddingsAsync(HttpClient client, string[] texts)
        {
            const string model = "models/gemini-embedding-001";
            string endpoint =
                $"https://generativelanguage.googleapis.com/v1beta/{model}:batchEmbedContents";

            var request = new
            {
                requests = texts.Select(text => new
                {
                    model,
                    taskType = "SEMANTIC_SIMILARITY",
                    content = new { parts = new[] { new { text } } }
                }).ToArray()
            };

            using var response = await client.PostAsJsonAsync(endpoint, request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Batch embedding failed: HTTP {(int)response.StatusCode}");
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            return result.GetProperty("embeddings")
                .EnumerateArray()
                .Select(embedding => embedding.GetProperty("values")
                    .EnumerateArray()
                    .Select(value => value.GetSingle())
                    .ToArray())
                .ToArray();
        }
    }
}



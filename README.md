# DOTNET_AI_Engineering
.NET AI Based Projects

## Platform and Technology Choices

- **Gemini API** — I chose Gemini because its free tier let me experiment with chat, token counting, and embeddings without paying for model access.
- **Console apps** — I use small console apps so I can focus on how the AI calls work and inspect results such as token counts, similarity scores, and response times directly in the terminal.
- **REST with `HttpClient`** — I call Gemini directly from C# so I can see what my app sends and how it handles the response. I keep the API key in .NET User Secrets for local development.
- **.NET project references** — I keep each lab in its own project and reuse code where it makes sense. For example, the performance lab uses the Gemini embedding client and vector math from the Tokens and Meaning lab.



## PROJECT 1 - Chat and Context: DotNet AI Chat App
[GitHub Project](https://github.com/mkabhishekdev/dotnet_AI_Engineering/tree/main/src/ChatContextLab)

A .NET 8 console app that calls the Gemini API over HTTP. It lets me chat across multiple turns and see how conversation history affects the model’s answers.

**What’s Built**

- **Chat loop.** The app reads my message, sends it to Gemini using `HttpClient`, and displays the answer from the JSON response.
- **Conversation history.** The app keeps my messages and Gemini’s replies in memory and includes them in each follow-up request. The `clear` command starts a fresh conversation, and `exit` ends the session.
- **Token usage and response timing.** The app shows the token counts reported by Gemini and measures how long it takes to receive the full response.
- **Local setup.** The Gemini API key is stored using .NET User Secrets. VS Code can launch the app in a separate macOS Terminal window.

**What's Learnt**

The app creates the chat experience by deciding what to send with each request. If I say “My project is called DotNet Project” and later ask “What’s my project called?”, Gemini can answer because my app sends the earlier message again. After I type `clear`, the next request starts with fresh history. Keeping more messages also means sending more input tokens.

**Engineering Question Answered**

What information does the model receive, and why does it seem to remember earlier messages?

For each turn, my app sends the system instructions, the conversation history it has kept, and my latest message. Gemini uses that context to generate the next reply.



## PROJECT 2 - Tokens and Meaning: DotNet Tokens & Meaning Lab
[GitHub Project](https://github.com/mkabhishekdev/dotnet_AI_Engineering/tree/main/src/TokensAndMeaningLab)

A .NET 8 console app that shows how AI models count tokens, represent text as vectors, and compare sentences by meaning.

**What’s Built**

- **Word and token counter.** C# counts words by splitting on whitespace. Gemini’s `countTokens` API reports the model’s token count. In my test, `explain IHttpClientFactory` had 2 words and 4 tokens.
- **Text embeddings.** Gemini turned each of my two dog sentences into a `float[]` with 3,072 numbers. The app prints the first five as a preview.
- **Semantic search.** The app uses all 3,072 numbers to calculate cosine similarity in C#, then ranks the candidate sentences from highest to lowest score.
- **Attention and masking demo.** A local C# exercise uses made-up scores to show how attention gives words different levels of influence. Turning on the causal mask gives a later word zero influence.

**What's Learnt**

A word can be split into multiple tokens. An embedding represents a piece of text as a vector, and cosine similarity lets my app compare vectors from the same model. Attention and masking explain how a model uses context internally. Attention controls how much each allowed token contributes, and a causal mask limits that calculation to the current token and earlier tokens.

**Engineering Question Answered**

How can a .NET app find related text when people use different words?

Generate embeddings for the query and candidate texts using the same model, calculate cosine similarity, and rank the candidates by score. This is the part which is reused in the RAG project to find relevant document passages.



## PROJECT 3 - AI Performance: DotNet AI Performance Lab
[GitHub Project](https://github.com/mkabhishekdev/dotnet_AI_Engineering/tree/main/src/AIPerformanceLab)

A .NET 8 console app that explores how to make embedding-based search faster, send fewer requests to Gemini, and store vectors using less space.

**What’s Built**

- **Embedding cache.** The app saves candidate vectors in an in-memory dictionary. When a new question uses the same candidates, the app reuses their vectors and calculates fresh similarity scores for the new question.
- **Sequential and concurrent requests.** I compared sending three embedding requests one at a time with starting all three and waiting for them using `Task.WhenAll`. In my test, the sequential run took 1,688 ms and the concurrent run took 528 ms. Both sent three HTTP requests.
- **Batch embedding.** The app sends three sentences to Gemini in one HTTP request and gets three vectors back. My batch run took 1,014 ms.
- **Smaller vectors.** I stored each candidate vector as a `Half[]` instead of a `float[]`. The 3,072 numbers took 6,144 bytes instead of 12,288. The scores changed slightly, and all three candidates ranked in the same order for my test query.
- **Shared code.** I used a .NET project reference to reuse `GeminiEmbeddingClient` and `VectorMath` from `TokensAndMeaningLab`.

**What’s Learnt**

Each technique solves a different problem. Caching skips repeat work. Concurrency lets requests spend less time waiting in line. Batching puts several inputs into one request. Using `Half` stores the same number of coordinates in less space, with a small loss of precision.

In my run, concurrency finished fastest, while batching sent the fewest HTTP requests. That showed me why elapsed time, request count, and storage size are worth measuring separately.

**Engineering Question Answered**

How can I make embedding-based search more efficient, and how do I check whether the results change?

Reuse embeddings for text I’ve already processed, compare ways to send new requests, and measure how much space the vectors take. When I change how I store vectors, I can compare the similarity scores and check whether the same passages still come out on top. This carries into the RAG project, where those rankings decide which document passages Gemini sees.

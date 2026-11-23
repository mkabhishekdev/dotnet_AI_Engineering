# DOTNET_AI_Engineering
.NET AI Based Projects

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

using Xedap.Models;
using Xedap.Services.Embedding;
using Xedap.Services.Llm;
using Xedap.Services.Vector;
using NuGet.Packaging.Core;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Xedap.Services
{
    public class RagPipeline // ✅ Đặt tên class trùng với constructor
    {
        private readonly ILlmChatProvider _llmChatProvider;
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IQdrantClient _vectorClient;
        private readonly AppConfig _appConfig;
        public RagPipeline(IEmbeddingProvider embeddingProvider, ILlmChatProvider llmProvider, IQdrantClient vectorClient, AppConfig appConfig)
        {
            _llmChatProvider = llmProvider;
            _vectorClient = vectorClient;
            _embeddingProvider = embeddingProvider;
            _appConfig = appConfig;
        }
        public async Task IngestAsync(IEnumerable<(string text, string? source)> chunks, CancellationToken ct = default)
        {
            int dim = await _embeddingProvider.GetDimAsync(ct);
            await _vectorClient.EnsureCollectionAsync(dim, ct);

            var list = new List<VecPoint>();
            foreach (var (text, source) in chunks)
            {
                var vec = await _embeddingProvider.EmbedAsync(text, ct);
                list.Add(new VecPoint(Guid.NewGuid().ToString("N"), vec, text, source));
            }
            await _vectorClient.UpsertAsync(list, ct);
        }
        public async Task<QueryResponse> AskAsync(string question, int? topK, CancellationToken cancellationToken = default)
        {
           var dim = await _embeddingProvider.GetDimAsync(cancellationToken);
            await _vectorClient.EnsureCollectionAsync(dim, cancellationToken);
            var questionVector = await _embeddingProvider.EmbedAsync(question, cancellationToken);
            var hits = await _vectorClient.SearchAsync(questionVector, topK ?? _appConfig.Rag.TopK, cancellationToken);
            //Filter by minimum score
            var filteredHits = hits.Where(h=>h.Score >= _appConfig.Rag.MinScore).ToList();
            if(filteredHits.Count == 0)
            {
                return new QueryResponse("Cannot find any match context data in your knowledge base.", new List<QueryHit>());
            }
            //deduplicate by text fingerprint
            var seenHits = new HashSet<string>();
            var dedupedHits = new List<VecHit>();
            foreach (var h in filteredHits)
            {
                var fp = Fingerprint(h.Text);
                if (seenHits.Contains(fp)) continue;
                seenHits.Add(fp);
                dedupedHits.Add(h);
            }
            var numbered = dedupedHits.Select((h, i) => (Idx: i + 1, Hit: h)).ToList();

            var citations = string.Join("\n", numbered.Select(x => $"[#${x.Idx}] {x.Hit.Source ?? x.Hit.Id} (score={x.Hit.Score:0.000})"));

            var context = string.Join("\n---\n", numbered.Select(x => $"ID:{x.Idx}\n{x.Hit.Text}"));
            //Step 1 : Retrieve relevant documents from the vector store
            string relevantDocs = string.Empty;
            //Step 2 : Construct the prompt for LLM

            var systemPrompt = "You must answer ONLY using the CONTEXT. If not found, reply: 'I don't know'. " +
                                "Cite context IDs in square brackets like [#1], [#2]. Be concise.";
            var userPrompt = $"QUESTION:\n{question}\n\nCONTEXT (each block has ID):\n{context}";

            // Step 3: Get the answer from the LLM
            var answer = await _llmChatProvider.AskAsync(systemPrompt, userPrompt, cancellationToken);
           // answer += "\n\nSources:\n" + citations;

            var respHits = dedupedHits.Select(h => new QueryHit(h.Id, h.Score, h.Text, h.Source)).ToList();

            return new QueryResponse(answer, respHits);
        }
        static string Fingerprint(string s)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(s.Trim().ToLowerInvariant());
            return Convert.ToHexString(sha.ComputeHash(bytes)).Substring(0, 16);
        }
    }
}

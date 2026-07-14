using Xedap.Models;
using Xedap.Services.Embedding;
using Xedap.Services.Vector;
using System.Text;
using System.Text.Json;

namespace Xedap.Services.Tools
{
    public class SearchDocsTool
    {
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IQdrantClient _vectorClient;
        private readonly AppConfig _appConfig;

        public SearchDocsTool(IEmbeddingProvider embeddingProvider, IQdrantClient vectorClient, AppConfig appConfig)
        {
            _embeddingProvider = embeddingProvider;
            _vectorClient = vectorClient;
            _appConfig = appConfig;
        }

        public async Task<string> InvokeAsync(string query, int? topK, float? minScore, CancellationToken cancellationToken = default)
        {
            int dim = await _embeddingProvider.GetDimAsync(cancellationToken);
            await _vectorClient.EnsureCollectionAsync(dim, cancellationToken);

            int k = topK ?? _appConfig.Rag.TopK;
            int overfetch = Math.Max(2, k * 3);

            var questionVector = await _embeddingProvider.EmbedAsync(query, cancellationToken);
            var raw = await _vectorClient.SearchAsync(questionVector, overfetch, cancellationToken);

            float gate = minScore ?? (_appConfig.Rag is { } ? _appConfig.Rag is { } ? _appConfig.Rag.MinScore : 0.0f : 0.0f);
            var passed = raw.Where(h => h.Score >= gate).ToList();

            // Dedupe theo hash nội dung chuẩn hóa
            string Fingerprint(string s)
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(s.Trim().ToLowerInvariant());
                return Convert.ToHexString(sha.ComputeHash(bytes)).Substring(0, 16);
            }

            var kept = new List<VecHit>();
            var seen = new HashSet<string>();
            foreach (var h in passed.OrderByDescending(x => x.Score))
            {
                if (seen.Add(Fingerprint(h.Text))) kept.Add(h);
                if (kept.Count >= k) break;
            }

            // Trả JSON gọn cho model (snippet rút gọn)
            static string Snip(string s, int max = 500)
            {
                var t = s.Replace("\r", " ").Replace("\n", " ").Trim();
                return t.Length <= max ? t : t[..max] + "…";
            }

            var payload = new
            {
                hits = kept.Select((h, i) => new {
                    id = h.Id,
                    ordinal = i + 1,     // để model trích dẫn [#ordinal]
                    score = Math.Round(h.Score, 4),
                    source = h.Source ?? "(unknown)",
                    snippet = Snip(h.Text, 500)
                }).ToArray()
            };

            return JsonSerializer.Serialize(payload);
        }
    }
}

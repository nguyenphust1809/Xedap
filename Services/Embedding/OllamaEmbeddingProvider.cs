using System.Net.Http.Json;
using Xedap.Models;

namespace Xedap.Services.Embedding
{
    public class OllamaEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _http;
        private readonly AppConfig _cfg;
        private int? _dim;
        public OllamaEmbeddingProvider(IHttpClientFactory f, AppConfig cfg)
        {
            _cfg = cfg;
            _http = f.CreateClient(nameof(OllamaEmbeddingProvider));
            _http.BaseAddress = new Uri(cfg.Ollama.BaseUrl);
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            var body = new { model = _cfg.Ollama.EmbedModel, input = text };
            var resp = await _http.PostAsJsonAsync("api/embeddings", body, ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<dynamic>(cancellationToken: ct);
            var vec = ((IEnumerable<object>)json.embedding).Select(o => Convert.ToSingle(o)).ToArray();
            _dim ??= vec.Length;
            return vec;
        }

        public async Task<int> GetDimAsync(CancellationToken ct = default)
        {
            if (_dim.HasValue) return _dim.Value;
            _ = await EmbedAsync("dimension probe", ct);
            return _dim!.Value;
        }
    }
}

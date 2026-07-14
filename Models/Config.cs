namespace Xedap.Models
{
    public class AppConfig
    {
        public string Provider { get; set; } = "OpenAI";
        public OpenAIConfig OpenAI { get; set; } = new();
        public OllamaConfig Ollama { get; set; } = new();
        public QdrantConfig Qdrant { get; set; } = new();
        public RagConfig Rag { get; set; } = new();
    }

    public class OpenAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ChatModel { get; set; } = "gpt-4o-mini";
        public string EmbedModel { get; set; } = "text-embedding-3-small";
    }

    public class OllamaConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string EmbedModel { get; set; } = "nomic-embed-text";
        public string ChatModel { get; set; } = "llama3";
    }

    public class QdrantConfig
    {
        public string Endpoint { get; set; } = "http://localhost:6333";
        public string Collection { get; set; } = "docs_basic";
       

    }

    public class RagConfig
    {
        public int ChunkSize { get; set; } = 800;
        public int ChunkOverlap { get; set; } = 120;
        public int TopK { get; set; } = 5;
        public float MinScore { get; set; } = 0.25f;

    }

}

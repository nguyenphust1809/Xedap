using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xedap.Services.Vector
{
    // Đại diện một điểm dữ liệu trong Qdrant (vector + metadata)
    public record VecPoint(string Id, float[] Vector, string Text, string? Source);

    // Kết quả tìm kiếm (hit) trả về từ Qdrant
    public record VecHit(string Id, float Score, string Text, string? Source);

    // Giao diện client kết nối tới Qdrant
    public interface IQdrantClient
    {
        Task EnsureCollectionAsync(int vectorSize, CancellationToken ct = default);
        Task UpsertAsync(IEnumerable<VecPoint> points, CancellationToken ct = default);
        Task<List<VecHit>> SearchAsync(float[] query, int topK, CancellationToken ct = default);
    }

    // Response chung khi gọi API /search
    public class QdrantSearchResponse
    {
        public List<QdrantHit>? Result { get; set; } = new();
    }

    // Mỗi item kết quả trong response
    public class QdrantHit
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public QdrantPayload? Payload { get; set; }
    }

    // Payload: metadata lưu cùng vector
    public class QdrantPayload
    {
        public string Text { get; set; } = string.Empty;
        public string? Source { get; set; }
    }
}

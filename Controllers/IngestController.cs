using Xedap.Models;
using Xedap.Services;
using Xedap.Services.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Xedap.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngestController : ControllerBase
    {
        private readonly RagPipeline _rag;
        private readonly AppConfig _cfg;

        public IngestController(RagPipeline rag, AppConfig cfg)
        {
            _rag = rag;
            _cfg = cfg;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 1024L * 1024L * 200L)] // 200MB
        public async Task<IActionResult> Upload([FromForm] List<IFormFile> files, CancellationToken ct)
        {
            if (files == null || files.Count == 0) return BadRequest("No files.");

            var chunks = new List<(string text, string? source)>();
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is not (".pdf" or ".txt" or ".md")) continue;

                var tmp = Path.GetTempFileName();
                await using (var fs = System.IO.File.Create(tmp))
                    await file.CopyToAsync(fs, ct);

                string raw = ext switch
                {
                    ".pdf" => PdfTextExtractor.Extract(tmp),
                    ".txt" or ".md" => await System.IO.File.ReadAllTextAsync(tmp, ct),
                    _ => ""
                };
                System.IO.File.Delete(tmp);

                if (string.IsNullOrWhiteSpace(raw)) continue;

                foreach (var ch in TextChunker.Chunk(raw, _cfg.Rag.ChunkSize, _cfg.Rag.ChunkOverlap))
                    chunks.Add((ch, file.FileName));
            }

            if (chunks.Count == 0) return BadRequest("No extractable text. Support: .pdf, .txt, .md");
            await _rag.IngestAsync(chunks, ct);
            return Ok(new { Ingested = chunks.Count, Files = files.Count });
        }
    }
}

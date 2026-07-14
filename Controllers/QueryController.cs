using Xedap.Models;
using Xedap.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Xedap.Controllers
{
    [Route("query")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        private readonly RagPipeline _rag;

        public QueryController(RagPipeline rag)
        {
            _rag = rag;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] QueryRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest("Question cannot be empty.");
            }

            var answer = await _rag.AskAsync(request.Question, request.TopK, cancellationToken);
            return Ok(new { Answer = answer }); 
        }
    }
}

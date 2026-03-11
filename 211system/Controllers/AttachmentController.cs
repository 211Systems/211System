using _211system.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentController : Controller
    {
        private readonly IAttachmentService _attachmentService;

        public AttachmentController(IAttachmentService attachmentService)
        {
            _attachmentService = attachmentService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAttachment(IFormFile file, [FromQuery] Guid incidentId)
        {
            try
            {
                var result = await _attachmentService.UploadFileAsync(file, incidentId);
                return Ok(new { Message = "Plik zapisany poprawnie", AttachmentId = result.Id });
            } 
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

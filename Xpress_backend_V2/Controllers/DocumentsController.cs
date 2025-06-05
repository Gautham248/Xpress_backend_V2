using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _repository;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(IDocumentService repository, ILogger<DocumentsController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // GET: api/documents/User/5
        [HttpGet("User/{UserId}")]
        public async Task<ActionResult<List<DocumentDTO>>> GetAllDocuments(int UserId)
        {
            try
            {
                var documents = await _repository.GetAllDocumentsByUserAsync(UserId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for User {UserId}", UserId);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/documents/User/5/type/1
        [HttpGet("User/{UserId}/type/{IDType}")]
        public async Task<ActionResult<List<DocumentDTO>>> GetDocumentsByType(int UserId, string IDType)
        {
            try
            {
                if (IDType != "Passport" && IDType != "Visa" && IDType != "Aadhar")
                    return BadRequest("Invalid document type ID. Valid values: Passport, Visa, Aadhar");

                var documents = await _repository.GetDocumentsByTypeAsync(UserId, IDType);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents of type {IDType} for User {UserId}", IDType, UserId);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/documents
        [HttpPost]
        public async Task<IActionResult> CreateDocument([FromBody] DocumentDTO DocumentDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var allErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new { Message = "Validation failed", Errors = allErrors });
                }

                if (DocumentDTO == null)
                {
                    return BadRequest("Request body is empty. Please provide valid document data.");
                }

                if (string.IsNullOrWhiteSpace(DocumentDTO.IDType))
                {
                    return BadRequest("Document type (IDType) is required and cannot be empty.");
                }

                var validTypes = new[] { "Passport", "Visa", "Aadhar" };

                if (!validTypes.Contains(DocumentDTO.IDType))
                {
                    return BadRequest($"Invalid document type '{DocumentDTO.IDType}'. Allowed values: {string.Join(", ", validTypes)}.");
                }

                var createdDocument = await _repository.AddDocumentAsync(DocumentDTO);

                if (createdDocument == null)
                {
                    return StatusCode(500, "Document creation failed. Please try again or contact support.");
                }

                return CreatedAtAction(
                    nameof(GetDocumentsByType),
                    new { UserId = createdDocument.UserId, IDType = createdDocument.IDType },
                    createdDocument
                );
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null
                    ? ex.InnerException.Message
                    : ex.Message;

                Console.WriteLine("Error occurred while saving document:");
                Console.WriteLine(errorMessage);

                // Temporarily return the detailed error in response for debugging
                return StatusCode(500, $"An error occurred while saving the document: {errorMessage}");
            }

        }


        // PUT: api/documents/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<DocumentDTO>> UpdateDocument(int id, [FromBody] DocumentDTO documentDTO)
        {
            try
            {
                var validTypes = new[] { "Passport", "Visa", "Aadhar" };

                if (!validTypes.Contains(documentDTO.IDType))
                    return BadRequest("Invalid document type ID. Valid values: Passport, Visa, Aadhar");

                // Assign the route ID to the DTO
                documentDTO.Id = id;

                var updatedDocument = await _repository.UpdateDocumentAsync(documentDTO);
                return Ok(updatedDocument);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document");
                return StatusCode(500, "Internal server error");
            }
        }


        // DELETE: api/documents/5/type/1
        [HttpDelete("{documentId}/type/{IDType}")]
        public async Task<IActionResult> DeleteDocument(int documentId, string IDType)
        {
            try
            {
                // Add logging to verify the endpoint is hit
                _logger.LogInformation($"Delete request received for ID: {documentId}, Type: {IDType}");

                var success = await _repository.DeleteDocumentAsync(documentId, IDType);

                if (!success)
                {
                    _logger.LogWarning($"Document not found - ID: {documentId}, Type: {IDType}");
                    return NotFound();
                }

                _logger.LogInformation($"Successfully deleted document - ID: {documentId}, Type: {IDType}");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting document - ID: {documentId}, Type: {IDType}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

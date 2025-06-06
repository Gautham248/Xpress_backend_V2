using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Interface
{
    public interface IDocumentService
    {
        // Document operations
        Task<DocumentDTO> AddDocumentAsync(DocumentDTO DocumentDTO);
        Task<DocumentDTO> UpdateDocumentAsync(DocumentDTO DocumentDTO);

        Task<bool> DeleteDocumentAsync(int documentId, string idType);

        // Get operations
        Task<List<DocumentDTO>> GetAllDocumentsByUserAsync(int employeeId);
        Task<List<DocumentDTO>> GetDocumentsByTypeAsync(int employeeId, string idType);
        Task<DocumentDTO> GetDocumentByIdAsync(int documentId, string idType);
    }
}

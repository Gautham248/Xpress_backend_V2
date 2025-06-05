using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Xpress_backend_V2.Data;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Repository
{
    public class DocumentRepository : IDocumentService
    {
        private readonly ApiDbContext _context;
        private readonly IMapper _mapper;

        public DocumentRepository(ApiDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<DocumentDTO> AddDocumentAsync(DocumentDTO DocumentDTO)
        {
            switch (DocumentDTO.IDType)
            {
                case "Passport": // Passport
                    var passport = _mapper.Map<PassportDoc>(DocumentDTO);
                    passport.IsActive = true;
                    _context.PassportDocs.Add(passport);
                    await _context.SaveChangesAsync();
                    return _mapper.Map<DocumentDTO>(passport);

                case "Visa": // Visa
                    var visa = _mapper.Map<VisaDoc>(DocumentDTO);
                    visa.IsActive = true;
                    _context.VisaDocs.Add(visa);
                    await _context.SaveChangesAsync();
                    return _mapper.Map<DocumentDTO>(visa);

                case "Aadhar": // Aadhar
                    var aadhar = _mapper.Map<AadharDoc>(DocumentDTO);
                    _context.AadharDocs.Add(aadhar);
                    await _context.SaveChangesAsync();
                    return _mapper.Map<DocumentDTO>(aadhar);

                default:
                    throw new ArgumentException("Invalid document type ID");
            }
        }

        public async Task<DocumentDTO> UpdateDocumentAsync(DocumentDTO documentDto)
        {
            switch (documentDto.IDType)
            {
                case "Passport": // Passport
                    var passport = await _context.PassportDocs.FindAsync(documentDto.Id);
                    if (passport == null) throw new KeyNotFoundException("Passport not found");
                    _mapper.Map(documentDto, passport);
                    passport.UploadedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return _mapper.Map<DocumentDTO>(passport);

                case "Visa": // Visa
                    var visa = await _context.VisaDocs.FindAsync(documentDto.Id);
                    if (visa == null) throw new KeyNotFoundException("Visa not found");
                    _mapper.Map(documentDto, visa);
                    visa.UploadedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return _mapper.Map<DocumentDTO>(visa);

                case "Aadhar": // Aadhar
                    var aadhar = await _context.AadharDocs.FindAsync(documentDto.Id);
                    if (aadhar == null) throw new KeyNotFoundException("Aadhar not found");
                    _mapper.Map(documentDto, aadhar);
                    aadhar.UploadedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return _mapper.Map<DocumentDTO>(aadhar);

                default:
                    throw new ArgumentException("Invalid document type ID");
            }
        }
        public async Task<bool> DeleteDocumentAsync(int documentId, string IDType)
        {
            switch (IDType)
            {
                case "Passport": // Passport
                    var passport = await _context.PassportDocs.FindAsync(documentId);
                    if (passport == null) return false;
                    _context.PassportDocs.Remove(passport);
                    break;

                case "Visa": // Visa
                    var visa = await _context.VisaDocs.FindAsync(documentId);
                    if (visa == null) return false;
                    _context.VisaDocs.Remove(visa);
                    break;

                case "Aadhar": // Aadhar
                    var aadhar = await _context.AadharDocs.FindAsync(documentId);
                    if (aadhar == null) return false;
                    _context.AadharDocs.Remove(aadhar);
                    break;

                default:
                    throw new ArgumentException("Invalid document type ID");
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<DocumentDTO>> GetAllDocumentsByUserAsync(int UserId)
        {
            var PassportDocs = await _context.PassportDocs
                .Where(p => p.UserId == UserId && p.IsActive)
                .ProjectTo<DocumentDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var VisaDocs = await _context.VisaDocs
                .Where(v => v.UserId == UserId && v.IsActive)
                .ProjectTo<DocumentDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var AadharDocs = await _context.AadharDocs
                .Where(a => a.UserId == UserId )
                .ProjectTo<DocumentDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return PassportDocs.Concat(VisaDocs).Concat(AadharDocs).ToList();
        }

        public async Task<List<DocumentDTO>> GetDocumentsByTypeAsync(int UserId, string IDType)
        {
            return IDType switch
            {
                "Passport" => await _context.PassportDocs
                    .Where(p => p.UserId == UserId && p.IsActive)
                    .ProjectTo<DocumentDTO>(_mapper.ConfigurationProvider)
                    .ToListAsync(),

                "Visa" => await _context.VisaDocs
                    .Where(v => v.UserId == UserId && v.IsActive)
                    .ProjectTo<DocumentDTO>(_mapper.ConfigurationProvider)
                    .ToListAsync(),

                "Aadhar" => await _context.AadharDocs
                    .Where(a => a.UserId == UserId)
                    .ProjectTo<DocumentDTO>(_mapper.ConfigurationProvider)
                    .ToListAsync(),

                _ => throw new ArgumentException("Invalid document type ID")
            };
        }

        public async Task<DocumentDTO> GetDocumentByIdAsync(int documentId, string IDType)
        {
            return IDType switch
            {
                "Passport" => await _context.PassportDocs
                    .Where(p => p.PassportDocId == documentId && p.IsActive)
                    .ProjectTo<DocumentDTO>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(),

                "Visa" => await _context.VisaDocs
                    .Where(v => v.VisaDocId == documentId && v.IsActive)
                    .ProjectTo<DocumentDTO>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(),

                "Aadhar" => await _context.AadharDocs
                    .Where(a => a.AadharId == documentId)
                    .ProjectTo<DocumentDTO>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(),

                _ => throw new ArgumentException("Invalid document type ID")
            };
        }
    }
}

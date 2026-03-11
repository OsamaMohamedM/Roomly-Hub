using Domain.Enums;

namespace Application.DTOs
{
    public class SubmitKycRequestDto
    {
        public KycDocumentType DocumentType { get; set; }
        public string FrontImageUrl { get; set; } = string.Empty;
        public string? BackImageUrl { get; set; }
        public string SelfieUrl { get; set; } = string.Empty;
    }
}
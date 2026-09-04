using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Infrastructure.DTOs;
using QL_HocVien.Infrastructure.Exceptions;
using QL_HocVien.Infrastructure.Factory;
using QL_HocVien.Infrastructure.Security;

namespace QL_HocVien.Infrastructure.Validation
{
    /// <summary>
    /// Quy tắc thẩm định an ninh tập tin Excel trong Factory Pattern.
    /// Tự động được ValidationFactory kích hoạt khi nhận ExcelFileValidationRequest.
    /// </summary>
    public class ExcelFileSecurityRule : IValidationRule<ExcelFileValidationRequest>
    {
        private readonly IExcelSecurityValidator _excelValidator;

        public ExcelFileSecurityRule(IExcelSecurityValidator excelValidator)
        {
            _excelValidator = excelValidator;
        }

        public async Task ValidateAsync(ExcelFileValidationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ValidationException("Yêu cầu kiểm duyệt tập tin Excel không được để trống.");
            }

            FileValidationResult result;

            if (request.FileStream != null)
            {
                result = await _excelValidator.ValidateExcelFileAsync(
                    request.FileStream, 
                    request.OriginalFileName, 
                    request.DisallowMacros, 
                    cancellationToken);
            }
            else
            {
                result = await _excelValidator.ValidateExcelFileAsync(
                    request.FilePath, 
                    request.DisallowMacros, 
                    cancellationToken);
            }

            if (!result.IsValid)
            {
                throw new SecurityThreatException(result.Message, threatType: "MaliciousExcelUploadThreat");
            }
        }
    }
}

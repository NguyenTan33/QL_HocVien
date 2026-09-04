using System;
using System.Text.RegularExpressions;
using QL_HocVien.Infrastructure.Exceptions;

namespace QL_HocVien.Infrastructure.Security
{
    /// <summary>
    /// Triển khai dịch vụ làm sạch và ngăn chặn các dạng tấn công Injection.
    /// Tuân thủ nguyên lý Single Responsibility (chuyên trách an ninh chuỗi đầu vào).
    /// </summary>
    public class SecuritySanitizer : ISecuritySanitizer
    {
        // 1. Mẫu nhận diện SQL Injection
        private static readonly Regex SqlInjectionPattern = new(
            @"(?i)(\b(UNION(\s+ALL)?\s+SELECT)\b|" +
            @"\b(OR|AND)\s+['""]?(?<cmp>\d+|[a-zA-Z_]+)['""]?\s*=\s*['""]?\k<cmp>['""]?|" +
            @"\b(DROP\s+TABLE|DROP\s+DATABASE|TRUNCATE\s+TABLE|ALTER\s+TABLE)\b|" +
            @"\b(EXEC(\s+XP_|\s+SP_)?|XP_CMDSHELL)\b|" +
            @"\b(WAITFOR\s+DELAY|BENCHMARK\s*\(|SLEEP\s*\()|" +
            @";\s*(SELECT|INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|EXEC)\b|" +
            @"'(\s*;\s*|\s*--\s*|\s*#\s*|\s*\/\*)|" +
            @"\b(OR|AND)\s+['""]?1['""]?\s*=\s*['""]?1['""]?)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // 2. Mẫu nhận diện Script / XSS Injection
        private static readonly Regex ScriptInjectionPattern = new(
            @"(?i)(<\s*script[^>]*>|" +
            @"<\s*\/\s*script\s*>|" +
            @"javascript\s*:|" +
            @"vbscript\s*:|" +
            @"data\s*:\s*text\/html|" +
            @"\bon(load|error|click|mouseover|mouseenter|focus|blur|submit|keydown|keyup)\s*=|" +
            @"<\s*(iframe|object|embed|applet|meta)[^>]*>)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // 3. Mẫu nhận diện OS Command Injection & Path Traversal
        private static readonly Regex CommandInjectionPattern = new(
            @"(?i)(\x00|%00|" +
            @"\.\.[\/\\]|" +
            @"[|&;]\s*(cmd(\.exe)?|powershell(\.exe)?|bash|sh|rm|del|copy|dir|cat|echo|net|whoami|curl|wget|certutil|rundll32|regsvr32)\b|" +
            @"\$\([^\)]+\)|`[^`]+`)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // 4. Mẫu nhận diện Spreadsheet / Excel Formula Injection (DDE)
        private static readonly Regex FormulaInjectionPattern = new(
            @"^(?i)[=@\+\-](.*[|!].*|\b(CMD|POWERSHELL|SHELL|DDE|EXEC|SYSTEM)\b.*)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public bool ContainsSqlInjection(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return SqlInjectionPattern.IsMatch(input);
        }

        public bool ContainsScriptInjection(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return ScriptInjectionPattern.IsMatch(input);
        }

        public bool ContainsCommandInjection(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            return CommandInjectionPattern.IsMatch(input);
        }

        public bool ContainsFormulaInjection(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            var trimmed = input.Trim();
            return FormulaInjectionPattern.IsMatch(trimmed);
        }

        public bool ContainsDangerousPatterns(string? input, out string detectedThreat)
        {
            detectedThreat = string.Empty;
            if (string.IsNullOrWhiteSpace(input)) return false;

            if (ContainsSqlInjection(input))
            {
                detectedThreat = "Phát hiện mã SQL độc hại (SQL Injection nguy cơ cao).";
                return true;
            }

            if (ContainsScriptInjection(input))
            {
                detectedThreat = "Phát hiện mã kịch bản thực thi nhúng (XSS / Script Injection).";
                return true;
            }

            if (ContainsCommandInjection(input))
            {
                detectedThreat = "Phát hiện ký tự thực thi lệnh hệ điều hành hoặc duyệt thư mục (Command Injection / Path Traversal).";
                return true;
            }

            if (ContainsFormulaInjection(input))
            {
                detectedThreat = "Phát hiện công thức bảng tính chứa lệnh thực thi độc hại (Excel Formula DDE Injection).";
                return true;
            }

            return false;
        }

        public string SanitizeInput(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var sanitized = input.Trim();

            // Loại bỏ các ký tự điều khiển ẩn hoặc null bytes
            sanitized = sanitized.Replace("\0", string.Empty);

            // Khử công thức DDE đầu dòng nếu xuất hiện ký tự kích hoạt công thức
            if (sanitized.StartsWith("=") || sanitized.StartsWith("+") || sanitized.StartsWith("-") || sanitized.StartsWith("@"))
            {
                // Thêm dấu nháy đơn đằng trước để Excel hiểu là văn bản thuần, không thực thi công thức
                sanitized = "'" + sanitized;
            }

            return sanitized;
        }

        public void EnsureSafeInput(string? input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            if (ContainsDangerousPatterns(input, out var threat))
            {
                throw new SecurityThreatException(
                    $"Dữ liệu tại trường '{fieldName}' không an toàn: {threat}", 
                    threatType: "InputSanitizationThreat");
            }
        }
    }
}

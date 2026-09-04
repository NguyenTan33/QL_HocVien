namespace QL_HocVien.Infrastructure.Security
{
    /// <summary>
    /// Giao diện dịch vụ phân tích, phát hiện và phòng chống các dạng tấn công Injection:
    /// - SQL Injection (mã SQL độc hại: ' OR '1'='1, UNION SELECT, DROP TABLE...)
    /// - Script / XSS Injection (<script>, javascript:, iframe, onerror...)
    /// - OS Command Injection & Path Traversal (cmd, powershell, sh, ../, null bytes...)
    /// - Formula Injection trong bảng tính (=cmd|', @SUM, DDE...)
    /// </summary>
    public interface ISecuritySanitizer
    {
        bool ContainsSqlInjection(string? input);
        bool ContainsScriptInjection(string? input);
        bool ContainsCommandInjection(string? input);
        bool ContainsFormulaInjection(string? input);
        bool ContainsDangerousPatterns(string? input, out string detectedThreat);
        string SanitizeInput(string? input);
        void EnsureSafeInput(string? input, string fieldName);
    }
}

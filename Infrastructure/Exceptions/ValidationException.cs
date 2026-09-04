using System;

namespace QL_HocVien.Infrastructure.Exceptions
{
    /// <summary>
    /// Ngoại lệ ném ra khi dữ liệu đầu vào vi phạm các quy tắc nghiệp vụ hoặc an ninh hệ thống.
    /// Tuân thủ chuẩn OOP & SOLID (Single Responsibility Principle).
    /// </summary>
    public class ValidationException : Exception
    {
        public string? PropertyName { get; }

        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, string? propertyName) : base(message)
        {
            PropertyName = propertyName;
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Ngoại lệ chuyên biệt cho các vi phạm bảo mật nghiêm trọng (SQL Injection, XSS, File thực thi độc hại).
    /// </summary>
    public class SecurityThreatException : ValidationException
    {
        public string ThreatType { get; }

        public SecurityThreatException(string message, string threatType = "GeneralSecurity") 
            : base(message)
        {
            ThreatType = threatType;
        }
    }
}

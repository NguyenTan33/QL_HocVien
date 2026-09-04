using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.Infrastructure.Security;

namespace QL_HocVien.Infrastructure.Factory
{
    /// <summary>
    /// Extension method đăng ký toàn bộ kiến trúc Validation & Security vào DI Container.
    /// Tự động quét Assembly để tìm nạp mọi IValidationRule<T> theo chuẩn Open/Closed Principle.
    /// </summary>
    public static class ValidationExtensions
    {
        public static IServiceCollection AddAppInfrastructureValidation(this IServiceCollection services)
        {
            // 1. Đăng ký Security Sanitizer và Excel Security Validator
            services.AddSingleton<ISecuritySanitizer, SecuritySanitizer>();
            services.AddSingleton<IExcelSecurityValidator, ExcelSecurityValidator>();

            // 2. Đăng ký Validation Factory
            services.AddScoped<IValidationFactory, ValidationFactory>();

            // 3. Tự động quét và đăng ký tất cả các class thực thi IValidationRule<T> trong Assembly hiện tại
            var assembly = Assembly.GetExecutingAssembly();

            var ruleTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(
                    t => t.GetInterfaces(),
                    (implementationType, interfaceType) => new { implementationType, interfaceType }
                )
                .Where(x => x.interfaceType.IsGenericType &&
                            x.interfaceType.GetGenericTypeDefinition() == typeof(IValidationRule<>));

            foreach (var rule in ruleTypes)
            {
                services.AddScoped(rule.interfaceType, rule.implementationType);
            }

            return services;
        }
    }
}

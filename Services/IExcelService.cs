using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface IExcelService
    {
        // 1. Học viên
        Task<(bool Success, string Message)> ExportCadetsToExcelAsync(IEnumerable<Cadet> cadets, string filePath);
        Task<(bool Success, string Message, List<Cadet> Cadets)> ImportCadetsFromExcelAsync(string filePath);

        // 2. Môn học
        Task<(bool Success, string Message)> ExportSubjectsToExcelAsync(IEnumerable<Subject> subjects, string filePath);
        Task<(bool Success, string Message, List<Subject> Subjects)> ImportSubjectsFromExcelAsync(string filePath);

        // 3. Kiểm tra thể lực
        Task<(bool Success, string Message)> ExportExamRecordsToExcelAsync(IEnumerable<PhysicalExamRecord> records, string filePath);
        Task<(bool Success, string Message, List<PhysicalExamRecord> Records)> ImportExamRecordsFromExcelAsync(string filePath);

        // 4. Toàn bộ hệ thống (Multi-sheet Full Backup & Restore)
        Task<(bool Success, string Message)> ExportAllDataToExcelAsync(string filePath);
        Task<(bool Success, string Message, int CadetsCount, int SubjectsCount, int ExamsCount)> ImportAllDataFromExcelAsync(string filePath);
    }
}

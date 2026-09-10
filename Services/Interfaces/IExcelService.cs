using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Interfaces
{
    public interface IExcelService
    {
        // 1. Há»c viÃªn
        Task<(bool Success, string Message)> ExportCadetsToExcelAsync(IEnumerable<Cadet> cadets, string filePath);
        Task<(bool Success, string Message, List<Cadet> Cadets)> ImportCadetsFromExcelAsync(string filePath);

        // 2. MÃ´n há»c
        Task<(bool Success, string Message)> ExportSubjectsToExcelAsync(IEnumerable<Subject> subjects, string filePath);
        Task<(bool Success, string Message, List<Subject> Subjects)> ImportSubjectsFromExcelAsync(string filePath);

        // 3. Kiá»ƒm tra thá»ƒ lá»±c
        Task<(bool Success, string Message)> ExportExamRecordsToExcelAsync(IEnumerable<PhysicalExamRecord> records, string filePath);
        Task<(bool Success, string Message, List<PhysicalExamRecord> Records)> ImportExamRecordsFromExcelAsync(string filePath);

        // 4. Lá»›p há»c
        Task<(bool Success, string Message)> ExportClassesToExcelAsync(IEnumerable<MilitaryClass> classes, string filePath);
        Task<(bool Success, string Message, List<MilitaryClass> Classes)> ImportClassesFromExcelAsync(string filePath);

        // 5. CÃ¡n bá»™
        Task<(bool Success, string Message)> ExportOfficersToExcelAsync(IEnumerable<Officer> officers, string filePath);
        Task<(bool Success, string Message, List<Officer> Officers)> ImportOfficersFromExcelAsync(string filePath);

        // 6. Danh má»¥c tá»• chá»©c (Cáº¥p báº­c, Chá»©c vá»¥, ÄÆ¡n vá»‹, ChuyÃªn ngÃ nh)
        Task<(bool Success, string Message)> ExportCatalogsToExcelAsync(string filePath);
        Task<(bool Success, string Message, int RanksCount, int PositionsCount, int UnitsCount, int MajorsCount)> ImportCatalogsFromExcelAsync(string filePath);

        // 7. ToÃ n bá»™ há»‡ thá»‘ng (Multi-sheet Full Backup & Restore)
        Task<(bool Success, string Message)> ExportAllDataToExcelAsync(string filePath);
        Task<(bool Success, string Message, int ClassesCount, int CadetsCount, int SubjectsCount, int ExamsCount, int OfficersCount, int CreditSubjectsCount, int CreditScoresCount)> ImportAllDataFromExcelAsync(string filePath);

        // 8. BÃ¡o cÃ¡o phÃ¢n tÃ­ch so sÃ¡nh Ä‘á»£t thi
        Task<(bool Success, string Message)> ExportComparisonToExcelAsync(QL_HocVien.Models.DTOs.ExamComparisonResultDto comparison, string filePath);

        // 9. BÃ¡o cÃ¡o Tá»•ng quan & Äá» xuáº¥t Huáº¥n luyá»‡n AI
        Task<(bool Success, string Message)> ExportDashboardExecutiveReportAsync(
            string filePath,
            QL_HocVien.Models.DTOs.DashboardSummaryDto summary,
            IEnumerable<QL_HocVien.Models.DTOs.UnitLeaderboardDto> units,
            IEnumerable<QL_HocVien.Models.DTOs.SubjectPerformanceDto> subjects,
            QL_HocVien.Models.DTOs.TrainingRecommendationSummaryDto aiRecommendations,
            IEnumerable<PhysicalExamRecord> failedRecords,
            IEnumerable<QL_HocVien.Models.DTOs.CadetHonorDto> honoredCadets);
    }
}



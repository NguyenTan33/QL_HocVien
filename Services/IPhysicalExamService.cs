using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface IPhysicalExamService
    {
        Task<IEnumerable<PhysicalExamRecord>> GetAllRecordsAsync();
        Task<IEnumerable<PhysicalExamRecord>> GetRecordsByCadetIdAsync(int cadetId);
        Task<IEnumerable<PhysicalExamRecord>> GetFailedRecordsAsync();
        Task<IEnumerable<PhysicalExamRecord>> SearchRecordsAsync(string? cadetKeyword, int? subjectId, string? grade, string? session);
        Task<(bool Success, string Message, PhysicalExamRecord? Record)> AddExamRecordAsync(PhysicalExamRecord record);
        Task<(bool Success, string Message)> UpdateExamRecordAsync(PhysicalExamRecord record);
        Task<(bool Success, string Message)> DeleteExamRecordAsync(int id);
    }
}

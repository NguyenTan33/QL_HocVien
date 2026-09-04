using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Data.Repositories
{
    public interface IPhysicalExamRepository : IRepository<PhysicalExamRecord>
    {
        Task<IEnumerable<PhysicalExamRecord>> GetRecordsByCadetIdAsync(int cadetId);
        Task<IEnumerable<PhysicalExamRecord>> GetAllWithDetailsAsync();
        Task<IEnumerable<PhysicalExamRecord>> GetFailedRecordsAsync();
        Task<IEnumerable<PhysicalExamRecord>> SearchRecordsAsync(string? cadetKeyword, int? subjectId, string? grade, string? session);
    }
}

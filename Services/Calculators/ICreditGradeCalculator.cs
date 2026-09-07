using System.Collections.Generic;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;

namespace QL_HocVien.Services.Calculators
{
    /// <summary>
    /// Contract cho bộ tính toán điểm học phần tín chỉ và TBM toàn khóa (SOLID - SRP, ISP)
    /// </summary>
    public interface ICreditGradeCalculator
    {
        /// <summary>
        /// Tính tỷ trọng của thành phần trong môn lớn (Credits thành phần / Tổng Credits môn lớn)
        /// </summary>
        double CalculateComponentWeight(double componentCredits, double totalMajorCredits);

        /// <summary>
        /// Tính điểm thành phần đóng góp vào môn lớn = (Điểm ghi nhận * TC thành phần) / Tổng TC môn lớn
        /// </summary>
        double? CalculateComponentContribution(double? recordedScore, double componentCredits, double totalMajorCredits);

        /// <summary>
        /// Tính điểm tổng kết của một môn lớn từ các thành phần con
        /// </summary>
        double? CalculateMajorSubjectScore(IEnumerable<(double? recordedScore, double componentCredits)> components, double totalMajorCredits);

        /// <summary>
        /// Tính TBM tích lũy toàn khóa theo chuẩn Excel ROUNDDOWN(SUMPRODUCT / curriculumCredits, 2)
        /// </summary>
        double CalculateCurriculumTbm(IEnumerable<(double score, double credits)> scoredComponents, double totalCurriculumCredits);

        /// <summary>
        /// Nhận diện danh sách các môn/thành phần học viên còn thiếu điểm
        /// </summary>
        (bool HasMissing, List<string> MissingList, int MissingCount) IdentifyMissingSubjects(
            IEnumerable<CreditSubject> curriculumSubjects,
            IDictionary<int, double?> cadetScores);

        /// <summary>
        /// Xây dựng cấu trúc phân rã chi tiết điểm thành phần cho từng môn lớn (hỗ trợ 1, 2, 3, 4 hay N thành phần)
        /// </summary>
        List<MajorSubjectBreakdownDto> BuildMajorSubjectBreakdowns(
            IEnumerable<CreditSubject> subjects,
            IDictionary<int, double?> cadetScores);

        /// <summary>
        /// Tính điểm môn học từ danh sách các đợt kiểm tra trực thuộc
        /// </summary>
        double? CalculateSubjectScoreFromComponents(
            IEnumerable<(double? score, double credits)> components,
            double totalSubjectCredits);

        /// <summary>
        /// Kiểm tra học viên có thiếu điểm ở các đợt kiểm tra đã diễn ra (có trên 10 học viên có điểm) hay không
        /// </summary>
        (bool HasMissingInActive, List<string> MissingActiveComponentNames) CheckMissingInActiveComponents(
            IEnumerable<SubjectAssessmentComponent> components,
            IDictionary<int, double?> cadetComponentScores,
            ISet<int> activeComponentIds);
    }
}

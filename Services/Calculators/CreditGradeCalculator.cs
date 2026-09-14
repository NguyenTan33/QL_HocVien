using System;
using System.Collections.Generic;
using System.Linq;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;

namespace QL_HocVien.Services.Calculators
{
    /// <summary>
    /// Triển khai nghiệp vụ tính toán điểm học phần tín chỉ và TBM (OOP & SOLID: SRP, OCP, LSP)
    /// Hỗ trợ động số lượng môn thành phần bất kỳ (1, 2, 3, 4 hoặc N thành phần)
    /// </summary>
    public class CreditGradeCalculator : ICreditGradeCalculator
    {
        public double CalculateComponentWeight(double componentCredits, double totalMajorCredits)
        {
            if (totalMajorCredits <= 0) return 0.0;
            return Math.Round(componentCredits / totalMajorCredits, 4);
        }

        public double? CalculateComponentContribution(double? recordedScore, double componentCredits, double totalMajorCredits)
        {
            if (!recordedScore.HasValue || totalMajorCredits <= 0) return null;
            // Công thức: (Điểm ghi nhận * TC thành phần) / Tổng TC môn lớn
            return Math.Round((recordedScore.Value * componentCredits) / totalMajorCredits, 2);
        }

        public double? CalculateMajorSubjectScore(IEnumerable<(double? recordedScore, double componentCredits)> components, double totalMajorCredits)
        {
            if (totalMajorCredits <= 0) return null;
            double weightedSum = 0;
            bool isAllRecorded = true;

            foreach (var (score, credits) in components)
            {
                if (score.HasValue && score.Value >= 0)
                {
                    weightedSum += score.Value * credits;
                }
                else
                {
                    isAllRecorded = false;
                }
            }

            if (!isAllRecorded) return null;
            return Math.Round(weightedSum / totalMajorCredits, 2);
        }

        public double CalculateCurriculumTbm(IEnumerable<(double score, double credits)> scoredComponents, double totalCurriculumCredits)
        {
            if (totalCurriculumCredits <= 0) return 0.0;
            double weightedSum = 0.0;

            foreach (var (score, credits) in scoredComponents)
            {
                weightedSum += score * credits;
            }

            // Chuẩn Excel ROUNDDOWN 2 chữ số thập phân: Math.Floor(x * 100) / 100
            // Khử sai số làm tròn dấu chấm động của vi xử lý trước khi Floor
            double raw = Math.Round(weightedSum / totalCurriculumCredits, 8);
            return Math.Floor(raw * 100.0) / 100.0;
        }

        public (bool HasMissing, List<string> MissingList, int MissingCount) IdentifyMissingSubjects(
            IEnumerable<CreditSubject> curriculumSubjects,
            IDictionary<int, double?> cadetScores)
        {
            var missing = new List<string>();

            foreach (var subj in curriculumSubjects)
            {
                if (!cadetScores.TryGetValue(subj.Id, out var score) || !score.HasValue || score.Value < 0)
                {
                    missing.Add(subj.SubjectName);
                }
            }

            return (missing.Count > 0, missing, missing.Count);
        }

        public List<MajorSubjectBreakdownDto> BuildMajorSubjectBreakdowns(
            IEnumerable<CreditSubject> subjects,
            IDictionary<int, double?> cadetScores)
        {
            var result = new List<MajorSubjectBreakdownDto>();
            var subjList = subjects.ToList();

            // Kiểm tra xem danh sách subjects có chứa các đợt thi thành phần trực thuộc Components hay không
            bool hasStructuredComponents = subjList.Any(s => s.Components != null && s.Components.Count > 0);

            if (hasStructuredComponents)
            {
                // Xử lý theo mô hình môn lớn và các đợt kiểm tra / thi thành phần trực thuộc (SubjectAssessmentComponent)
                var majorSubjects = subjList.Where(s => !s.IsComponent).ToList();
                foreach (var major in majorSubjects)
                {
                    var compList = new List<ComponentScoreDto>();
                    var comps = (major.Components ?? Enumerable.Empty<SubjectAssessmentComponent>())
                        .OrderBy(c => c.OrderIndex)
                        .ToList();

                    if (comps.Count > 0)
                    {
                        double totalCredits = Math.Round(comps.Sum(c => c.Credits), 2);
                        if (totalCredits <= 0) totalCredits = major.Credits > 0 ? major.Credits : 1.0;

                        double weightedSum = 0;
                        bool allComplete = true;

                        foreach (var c in comps)
                        {
                            cadetScores.TryGetValue(c.Id, out var cScore);
                            double weightRatio = totalCredits > 0 ? c.Credits / totalCredits : 1.0;
                            double? contrib = cScore.HasValue && cScore.Value >= 0
                                ? Math.Round((cScore.Value * c.Credits) / totalCredits, 2)
                                : null;

                            if (cScore.HasValue && cScore.Value >= 0)
                            {
                                weightedSum += cScore.Value * c.Credits;
                            }
                            else
                            {
                                allComplete = false;
                            }

                            compList.Add(new ComponentScoreDto
                            {
                                ComponentName = c.ComponentName,
                                Credits = c.Credits,
                                RecordedScore = cScore,
                                WeightRatio = Math.Round(weightRatio, 4),
                                ContributionScore = contrib
                            });
                        }

                        double? finalScore = allComplete && totalCredits > 0
                            ? Math.Round(weightedSum / totalCredits, 2)
                            : null;

                        result.Add(new MajorSubjectBreakdownDto
                        {
                            MajorSubjectName = major.SubjectName,
                            TotalCredits = totalCredits,
                            FinalScore = finalScore,
                            IsComplete = allComplete,
                            Components = compList
                        });
                    }
                    else
                    {
                        // Môn độc lập không có thành phần con
                        cadetScores.TryGetValue(major.Id, out var sScore);
                        compList.Add(new ComponentScoreDto
                        {
                            ComponentName = major.SubjectName,
                            Credits = major.Credits,
                            RecordedScore = sScore,
                            WeightRatio = 1.0,
                            ContributionScore = sScore
                        });

                        result.Add(new MajorSubjectBreakdownDto
                        {
                            MajorSubjectName = major.SubjectName,
                            TotalCredits = major.Credits,
                            FinalScore = sScore,
                            IsComplete = sScore.HasValue && sScore.Value >= 0,
                            Components = compList
                        });
                    }
                }

                return result;
            }

            // Fallback: Chế độ gom nhóm theo DisplayGroupName (tương thích ngược)
            var grouped = subjList.GroupBy(s => s.DisplayGroupName);

            foreach (var grp in grouped)
            {
                var groupSubjs = grp.ToList();
                double groupTotalCredits = Math.Round(groupSubjs.Sum(s => s.Credits), 2);
                double groupWeightedSum = 0;
                bool groupComplete = true;
                var compList = new List<ComponentScoreDto>();

                foreach (var comp in groupSubjs)
                {
                    cadetScores.TryGetValue(comp.Id, out var compScore);
                    double weightRatio = groupTotalCredits > 0 ? comp.Credits / groupTotalCredits : 1.0;
                    double? contrib = compScore.HasValue && compScore.Value >= 0
                        ? Math.Round((compScore.Value * comp.Credits) / (groupTotalCredits > 0 ? groupTotalCredits : 1.0), 2)
                        : null;

                    if (compScore.HasValue && compScore.Value >= 0)
                    {
                        groupWeightedSum += compScore.Value * comp.Credits;
                    }
                    else
                    {
                        groupComplete = false;
                    }

                    compList.Add(new ComponentScoreDto
                    {
                        ComponentName = comp.SubjectName,
                        Credits = comp.Credits,
                        RecordedScore = compScore,
                        WeightRatio = Math.Round(weightRatio, 4),
                        ContributionScore = contrib
                    });
                }

                double? groupFinalScore = groupComplete && groupTotalCredits > 0
                    ? Math.Round(groupWeightedSum / groupTotalCredits, 2)
                    : null;

                result.Add(new MajorSubjectBreakdownDto
                {
                    MajorSubjectName = grp.Key,
                    TotalCredits = groupTotalCredits,
                    FinalScore = groupFinalScore,
                    IsComplete = groupComplete,
                    Components = compList
                });
            }

            return result;
        }

        public double? CalculateSubjectScoreFromComponents(
            IEnumerable<(double? score, double credits)> components,
            double totalSubjectCredits)
        {
            var compList = components.ToList();
            if (!compList.Any() || totalSubjectCredits <= 0) return null;

            double weightedSum = 0;

            foreach (var (score, credits) in compList)
            {
                // Điểm trung bình môn chỉ có khi TẤT CẢ các cột của môn chính được nhập
                if (score.HasValue && score.Value >= 0)
                {
                    weightedSum += score.Value * credits;
                }
                else
                {
                    // Còn ít nhất 1 đợt thi chưa nhập -> Chưa tính điểm trung bình môn
                    return null;
                }
            }

            return Math.Round(weightedSum / totalSubjectCredits, 2);
        }

        public (bool HasMissingInActive, List<string> MissingActiveComponentNames) CheckMissingInActiveComponents(
            IEnumerable<SubjectAssessmentComponent> components,
            IDictionary<int, double?> cadetComponentScores,
            ISet<int> activeComponentIds)
        {
            var missingList = new List<string>();

            foreach (var comp in components)
            {
                // Chỉ xét những đợt kiểm tra đã active (có từ 20 học viên trở lên có điểm)
                if (activeComponentIds.Contains(comp.Id))
                {
                    if (!cadetComponentScores.TryGetValue(comp.Id, out var score) || !score.HasValue || score.Value < 0)
                    {
                        missingList.Add(comp.ComponentName);
                    }
                }
            }

            return (missingList.Count > 0, missingList);
        }
    }
}

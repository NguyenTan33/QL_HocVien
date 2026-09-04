using System.Collections.Generic;

namespace QL_HocVien.Models.DTOs
{
    public class StrategicDirectiveDto
    {
        public string Title { get; set; } = string.Empty;
        public string ExecutiveSummary { get; set; } = string.Empty;
        public List<string> KeyActionItems { get; set; } = new();
        public string TimeAllocationDirective { get; set; } = string.Empty;
        public string RecoveryAndNutritionAdvice { get; set; } = string.Empty;
    }

    public class FitnessComponentPrescriptionDto
    {
        public string ComponentName { get; set; } = string.Empty;
        public string TargetSubjects { get; set; } = string.Empty;
        public double FailRate { get; set; }
        public int AffectedCadetsCount { get; set; }
        public string UrgencyLevel { get; set; } = "Tiêu chuẩn"; // "🔴 KHẨN CẤP", "🟡 CẦN CHÚ Ý", "🟢 DUY TRÌ"
        public string UrgencyColor { get; set; } = "#16A34A";
        public string UrgencyBackground { get; set; } = "#DCFCE7";
        public string CoreWeaknessAnalysis { get; set; } = string.Empty;
        public string ScientificTrainingProtocol { get; set; } = string.Empty;
        public string WeeklyScheduleRecommendation { get; set; } = string.Empty;
        public string MeasurableTarget { get; set; } = string.Empty;
    }

    public class PersonalizedCadetPrescriptionDto
    {
        public int CadetId { get; set; }
        public string CadetCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string WeakSubject { get; set; } = string.Empty;
        public string CurrentPerformance { get; set; } = string.Empty;
        public string StandardThreshold { get; set; } = string.Empty;
        public string TailoredExercisePlan { get; set; } = string.Empty;
        public string RemedialTimeline { get; set; } = "30 ngày (Kiểm tra lại)";
        public string AssignedCoach { get; set; } = "Cán bộ Tiểu đội trưởng phụ trách";
    }

    public class TrainingRecommendationSummaryDto
    {
        public StrategicDirectiveDto StrategicDirective { get; set; } = new();
        public List<FitnessComponentPrescriptionDto> ComponentPrescriptions { get; set; } = new();
        public List<PersonalizedCadetPrescriptionDto> PersonalizedCadetPrescriptions { get; set; } = new();
    }
}

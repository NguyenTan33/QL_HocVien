using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;

namespace QL_HocVien.Services
{
    public class TrainingRecommendationService : ITrainingRecommendationService
    {
        public Task<TrainingRecommendationSummaryDto> GenerateRecommendationsAsync(
            IEnumerable<PhysicalExamRecord> filteredRecords,
            IEnumerable<Cadet> allCadets,
            string? unit = null)
        {
            var recordsList = filteredRecords.ToList();
            var result = new TrainingRecommendationSummaryDto();

            int totalTests = recordsList.Count;
            int failedTests = recordsList.Count(r => r.Grade == "Không đạt");
            double overallFailRate = totalTests > 0 ? (double)failedTests / totalTests * 100 : 0;

            // 1. CHỈ ĐẠO CHIẾN LƯỢC TỔNG THỂ (Strategic Directive)
            var directive = new StrategicDirectiveDto();
            string scopeText = string.IsNullOrWhiteSpace(unit) || unit == "Tất cả" ? "Toàn viện / Toàn đơn vị" : $"Đơn vị {unit}";

            if (totalTests == 0)
            {
                directive.Title = $"KẾ HOẠCH RÈN LUYỆN THỂ LỰC - {scopeText.ToUpper()}";
                directive.ExecutiveSummary = "Chưa ghi nhận đủ dữ liệu kiểm tra trong phạm vi lọc đã chọn. Cần đẩy nhanh tiến độ tổ chức kiểm tra rèn luyện thể lực ban đầu để AI có cơ sở phân tích.";
                directive.TimeAllocationDirective = "Duy trì lịch thể dục buổi sáng (05:15 - 05:45) và rèn luyện thể thao buổi chiều (16:30 - 17:30) các ngày trong tuần.";
                directive.RecoveryAndNutritionAdvice = "Đảm bảo định lượng khẩu phần ăn quân trang quân dụng, bổ sung nước điện giải và vitamin nhóm B trong giai đoạn chuyển mùa.";
            }
            else
            {
                directive.Title = $"CHỈ THỊ RÈN LUYỆN & NÂNG CAO THỂ LỰC QUÂN SỰ - {scopeText.ToUpper()}";

                if (overallFailRate > 15)
                {
                    directive.ExecutiveSummary = $"Đánh giá tình hình thể lực tại {scopeText}: Tỷ lệ chưa đạt chuẩn Thông tư 32 chiếm {overallFailRate:F1}% (ở mức CẢNH BÁO). Cần mở đợt cao điểm huấn luyện thể lực phụ đạo trong 4 tuần tới, tập trung phân loại và kèm cặp sát sao nhóm học viên có nguy cơ trượt chuẩn.";
                }
                else if (overallFailRate > 5)
                {
                    directive.ExecutiveSummary = $"Đánh giá tình hình thể lực tại {scopeText}: Toàn đơn vị duy trì nền nếp rèn luyện khá tốt (Tỷ lệ đạt chuẩn {100 - overallFailRate:F1}%). Tuy nhiên vẫn còn một bộ phận nhỏ ({failedTests} lượt) chưa đồng đều giữa các nội dung sức bền và sức mạnh.";
                }
                else
                {
                    directive.ExecutiveSummary = $"Đánh giá tình hình thể lực tại {scopeText}: Phong trào rèn luyện thể lực đạt kết quả XUẤT SẮC (Tỷ lệ đạt chuẩn {100 - overallFailRate:F1}%). Tiếp tục bồi dưỡng các nhân tố nòng cốt tham gia hội thao quân sự cấp Học viện và toàn quân.";
                }

                directive.KeyActionItems.Add("Phân nhóm học viên theo thể lực: Thành lập 'Tổ rèn luyện nâng cao' cho các đồng chí chưa đạt chuẩn dưới sự kèm cặp của cán bộ Trung đội.");
                directive.KeyActionItems.Add("Tối ưu hóa giờ thể thao buổi chiều: 45 phút đầu tập trung nội dung yếu (Xà đơn / Chạy bền), 15 phút sau thả lỏng hồi tĩnh.");
                directive.KeyActionItems.Add("Thực hiện kiểm tra định kỳ 2 tuần/lần vào sáng Thứ Bảy để đánh giá tiến bộ của từng cá nhân.");

                directive.TimeAllocationDirective = overallFailRate > 15
                    ? "Tăng cường thêm 3 buổi phụ đạo/tuần (Thứ 2, 4, 6 từ 16:15 - 17:15). Thứ 7 tổ chức chạy việt dã cự ly trung bình."
                    : "Duy trì đều đặn 4 buổi/tuần theo tiến trình biểu; chú trọng chất lượng từng động tác kỹ thuật.";

                directive.RecoveryAndNutritionAdvice = "Chấn chỉnh công tác bảo đảm nước uống có muối khoáng tại bãi tập; sau các buổi chạy 3000m phải dành tối thiểu 10 phút thả lỏng cơ bắp tránh căng cơ chuột rút.";
            }

            result.StrategicDirective = directive;

            // 2. PHÁC ĐỒ CHUYÊN SÂU THEO TỪNG NHÓM TỐ CHẤT THỂ LỰC (Component Prescriptions)
            // Nhóm A: Sức mạnh (Xà đơn, xà kép)
            var strengthRecords = recordsList.Where(r => 
                (r.Subject?.SubjectName?.Contains("xà", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("chống đẩy", StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            int strengthTotal = strengthRecords.Count;
            int strengthFail = strengthRecords.Count(r => r.Grade == "Không đạt");
            double strengthFailRate = strengthTotal > 0 ? (double)strengthFail / strengthTotal * 100 : 0;

            result.ComponentPrescriptions.Add(new FitnessComponentPrescriptionDto
            {
                ComponentName = "Tố Chất Sức Mạnh Cơ Bắp & Chi Trên",
                TargetSubjects = "Co tay xà đơn, Xà kép, Chống đẩy",
                FailRate = Math.Round(strengthFailRate, 1),
                AffectedCadetsCount = strengthFail,
                UrgencyLevel = strengthFailRate >= 18 ? "🔴 KHẨN CẤP" : (strengthFailRate >= 8 ? "🟡 CẦN CHÚ Ý" : "🟢 DUY TRÌ"),
                UrgencyColor = strengthFailRate >= 18 ? "#DC2626" : (strengthFailRate >= 8 ? "#D97706" : "#16A34A"),
                UrgencyBackground = strengthFailRate >= 18 ? "#FEE2E2" : (strengthFailRate >= 8 ? "#FEF3C7" : "#DCFCE7"),
                CoreWeaknessAnalysis = strengthFailRate >= 15 
                    ? "Lực bám cẳng tay và cơ lưng rộng (Latissimus dorsi) còn yếu; nhiều đồng chí bị quán tính lắc người không đúng kỹ thuật chuẩn quân sự."
                    : "Học viên cơ bản nắm được kỹ thuật; cần gia tăng sức bền cơ bắp khi đạt mốc 10 - 12 cái.",
                ScientificTrainingProtocol = "• Tuần 1-2: Tập treo xà tĩnh tính thời gian (Dead hang) 3 hiệp x 45 giây; kéo xà có dây kháng lực (Rubber band) hỗ trợ 4 hiệp x 8 lần.\n• Tuần 3-4: Co tay xà đơn có ngắt nhịp (2 giây giữ đỉnh xà - 3 giây hạ xuống); bổ trợ hít đất kim cương (Diamond push-ups) 3 hiệp x 15 lần.",
                WeeklyScheduleRecommendation = "3 buổi/tuần (Thứ 2, 4, 6 lúc 16:30 - 17:15). Lắp đặt xà phụ tại hồi nhà nội vụ để tranh thủ rèn luyện.",
                MeasurableTarget = "100% học viên kéo đạt tối thiểu 10 cái (Đạt chuẩn TT32); trên 40% đạt mốc 14 cái trở lên (Khá - Giỏi)."
            });

            // Nhóm B: Sức bền (Chạy 3000m vũ trang)
            var enduranceRecords = recordsList.Where(r => 
                (r.Subject?.SubjectName?.Contains("3000", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("việt dã", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("bền", StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            int enduranceTotal = enduranceRecords.Count;
            int enduranceFail = enduranceRecords.Count(r => r.Grade == "Không đạt");
            double enduranceFailRate = enduranceTotal > 0 ? (double)enduranceFail / enduranceTotal * 100 : 0;

            result.ComponentPrescriptions.Add(new FitnessComponentPrescriptionDto
            {
                ComponentName = "Tố Chất Sức Bền Tim Mạch & Hô Hấp",
                TargetSubjects = "Chạy vũ trang 3000m, Hành quân rèn luyện",
                FailRate = Math.Round(enduranceFailRate, 1),
                AffectedCadetsCount = enduranceFail,
                UrgencyLevel = enduranceFailRate >= 18 ? "🔴 KHẨN CẤP" : (enduranceFailRate >= 8 ? "🟡 CẦN CHÚ Ý" : "🟢 DUY TRÌ"),
                UrgencyColor = enduranceFailRate >= 18 ? "#DC2626" : (enduranceFailRate >= 8 ? "#D97706" : "#16A34A"),
                UrgencyBackground = enduranceFailRate >= 18 ? "#FEE2E2" : (enduranceFailRate >= 8 ? "#FEF3C7" : "#DCFCE7"),
                CoreWeaknessAnalysis = enduranceFailRate >= 15
                    ? "Học viên chưa làm chủ kỹ thuật phân phối sức; thường xuất phát quá nhanh ở 800m đầu dẫn đến tụt dốc ở nửa cuối đường chạy. Kỹ thuật nhịp thở chưa đồng bộ bước chạy."
                    : "Học viên duy trì ngưỡng hô hấp ổn định; cần tối ưu hóa guồng chân rút đích ở 400m cuối cùng.",
                ScientificTrainingProtocol = "• Phương pháp Fartlek (Biến tốc): 400m chạy nhanh vừa - 200m chạy chậm thả lỏng liên tục 5 vòng sân.\n• Luyện tập nhịp thở 2-2 (2 bước hít vào, 2 bước thở ra dứt khoát bằng mũi và miệng).\n• Tăng cự ly lũy tiến: Tuần 1 chạy 1.8km, Tuần 2 chạy 2.4km, Tuần 3-4 hoàn thiện chuẩn 3.0km có trang bị súng tiểu liên AK.",
                WeeklyScheduleRecommendation = "2 buổi rèn cự ly dài (Thứ 3, Thứ 6) + 1 buổi hành quân mang vác 15kg vào sáng Thứ 7.",
                MeasurableTarget = "Thời gian chạy 3000m toàn đơn vị dưới 13 phút 30 giây; không có học viên bỏ cuộc giữa chặng."
            });

            // Nhóm C: Sức nhanh & Bộc phát (Chạy 100m, Nhảy xa)
            var speedRecords = recordsList.Where(r => 
                (r.Subject?.SubjectName?.Contains("100", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("nhảy", StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            int speedTotal = speedRecords.Count;
            int speedFail = speedRecords.Count(r => r.Grade == "Không đạt");
            double speedFailRate = speedTotal > 0 ? (double)speedFail / speedTotal * 100 : 0;

            result.ComponentPrescriptions.Add(new FitnessComponentPrescriptionDto
            {
                ComponentName = "Tố Chất Tốc Độ & Bộc Phát Thần Kinh Cơ",
                TargetSubjects = "Chạy 100m, Nhảy xa có đà",
                FailRate = Math.Round(speedFailRate, 1),
                AffectedCadetsCount = speedFail,
                UrgencyLevel = speedFailRate >= 15 ? "🔴 KHẨN CẤP" : (speedFailRate >= 6 ? "🟡 CẦN CHÚ Ý" : "🟢 DUY TRÌ"),
                UrgencyColor = speedFailRate >= 15 ? "#DC2626" : (speedFailRate >= 6 ? "#D97706" : "#16A34A"),
                UrgencyBackground = speedFailRate >= 15 ? "#FEE2E2" : (speedFailRate >= 6 ? "#FEF3C7" : "#DCFCE7"),
                CoreWeaknessAnalysis = "Góc độ xuất phát thấp chưa tối ưu, sức bộc phát của cơ bắp chân và khớp cổ chân còn hạn chế ở 30m gia tốc đầu.",
                ScientificTrainingProtocol = "• Bài tập Plyometrics: Bật cóc (Frog jumps) 3 hiệp x 20m; nhảy lò cò đổi chân tăng độ đàn hồi gân gót Achilles.\n• Luyện kỹ thuật xuất phát thấp với bàn đạp: Chạy tăng tốc 30m - 50m lặp lại 6 lần.",
                WeeklyScheduleRecommendation = "2 buổi/tuần lồng ghép vào đầu giờ thể dục chiều (Thứ 3, Thứ 5).",
                MeasurableTarget = "Thời gian chạy 100m dưới 14.5 giây (Chuẩn đạt); trên 50% đạt mốc dưới 13.8 giây (Chuẩn Khá - Giỏi)."
            });

            // Nhóm D: Bơi vũ trang & Vượt vật cản
            var waterRecords = recordsList.Where(r => 
                (r.Subject?.SubjectName?.Contains("bơi", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("vật cản", StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            int waterTotal = waterRecords.Count;
            int waterFail = waterRecords.Count(r => r.Grade == "Không đạt");
            double waterFailRate = waterTotal > 0 ? (double)waterFail / waterTotal * 100 : 0;

            result.ComponentPrescriptions.Add(new FitnessComponentPrescriptionDto
            {
                ComponentName = "Kỹ Năng Quân Sự: Bơi Vũ Trang & Vượt Vật Cản",
                TargetSubjects = "Bơi 100m mang súng, Vượt vật cản K91",
                FailRate = Math.Round(waterFailRate, 1),
                AffectedCadetsCount = waterFail,
                UrgencyLevel = waterFailRate >= 15 ? "🔴 KHẨN CẤP" : (waterFailRate >= 6 ? "🟡 CẦN CHÚ Ý" : "🟢 DUY TRÌ"),
                UrgencyColor = waterFailRate >= 15 ? "#DC2626" : (waterFailRate >= 6 ? "#D97706" : "#16A34A"),
                UrgencyBackground = waterFailRate >= 15 ? "#FEE2E2" : (waterFailRate >= 6 ? "#FEF3C7" : "#DCFCE7"),
                CoreWeaknessAnalysis = "Tâm lý sợ nước ở một số đồng chí mới; kỹ thuật đạp chân ếch mang giày và ba lô chưa thuần thục làm tiêu hao thể lực nhanh.",
                ScientificTrainingProtocol = "• Rèn luyện kỹ năng nổi ngửa giữ súng khô trên mặt nước 5 phút liên tục.\n• Đạp chân ếch ôm phao bơi 4 x 50m; chuyển tiếp sang bơi vũ trang mang súng tiểu liên AK và phao lưng trang bị.",
                WeeklyScheduleRecommendation = "2 buổi/tuần tại hồ bơi quân sự (Thứ 4, Thứ 7).",
                MeasurableTarget = "100% bơi được cự ly 100m vũ trang an toàn tuyệt đối; bơi đạt chuẩn dưới 2 phút 30 giây."
            });

            // 3. PHÁC ĐỒ BỒI DƯỠNG CÁ NHÂN HÓA (Personalized Cadet Prescriptions)
            var failedCadetRecords = recordsList.Where(r => r.Grade == "Không đạt").Take(20).ToList();
            foreach (var fr in failedCadetRecords)
            {
                string subjName = fr.Subject?.SubjectName ?? "Rèn luyện thể lực";
                string tailoredPlan;
                string standard;

                if (subjName.Contains("xà", StringComparison.OrdinalIgnoreCase))
                {
                    tailoredPlan = "Treo xà tĩnh 45s x 3 hiệp + Kéo xà có dây hỗ trợ 8 lần/hiệp. Tập mỗi chiều trước giờ ăn cơm.";
                    standard = "Tối thiểu 10 cái (Đạt chuẩn TT32)";
                }
                else if (subjName.Contains("3000", StringComparison.OrdinalIgnoreCase))
                {
                    tailoredPlan = "Chạy cự ly tăng dần (1.5km -> 2km -> 3km), luyện nhịp thở 2-2 kết hợp đi bộ thả lỏng.";
                    standard = "Thời gian dưới 13 phút 30 giây";
                }
                else if (subjName.Contains("100", StringComparison.OrdinalIgnoreCase))
                {
                    tailoredPlan = "Luyện bật cóc 3 hiệp 20m + Chạy biến tốc 30m - 50m nâng cao đùi bộc phát tốc độ.";
                    standard = "Thời gian dưới 14.5 giây";
                }
                else
                {
                    tailoredPlan = "Tập bổ trợ thể lực chuyên biệt theo hướng dẫn của Cán bộ huấn luyện; kiểm tra lại sau 3 tuần.";
                    standard = "Đạt tiêu chuẩn môn theo TT 32";
                }

                result.PersonalizedCadetPrescriptions.Add(new PersonalizedCadetPrescriptionDto
                {
                    CadetId = fr.CadetId,
                    CadetCode = fr.Cadet?.CadetCode ?? $"HV-{fr.CadetId}",
                    FullName = fr.Cadet?.FullName ?? "Học viên",
                    Unit = fr.Cadet?.Unit ?? "Đơn vị",
                    ClassName = fr.Cadet?.ClassName ?? (fr.Cadet?.MilitaryClass?.ClassName ?? "Lớp"),
                    WeakSubject = subjName,
                    CurrentPerformance = fr.ScoreValue.ToString("0.##"),
                    StandardThreshold = standard,
                    TailoredExercisePlan = tailoredPlan,
                    RemedialTimeline = "30 ngày (Kiểm tra sát hạch lại)",
                    AssignedCoach = $"Cán bộ {fr.Cadet?.Unit ?? "Đại đội"} trực tiếp đôn đốc"
                });
            }

            return Task.FromResult(result);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QL_HocVien.Models;
using QL_HocVien.Models.DTOs;

namespace QL_HocVien.Services.Implementations
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
            int failedTests = recordsList.Count(r => r.Grade == "KhÃ´ng Ä‘áº¡t");
            double overallFailRate = totalTests > 0 ? (double)failedTests / totalTests * 100 : 0;

            // 1. CHá»ˆ Äáº O CHIáº¾N LÆ¯á»¢C Tá»”NG THá»‚ (Strategic Directive)
            var directive = new StrategicDirectiveDto();
            string scopeText = string.IsNullOrWhiteSpace(unit) || unit == "Táº¥t cáº£" ? "ToÃ n viá»‡n / ToÃ n Ä‘Æ¡n vá»‹" : $"ÄÆ¡n vá»‹ {unit}";

            if (totalTests == 0)
            {
                directive.Title = $"Káº¾ HOáº CH RÃˆN LUYá»†N THá»‚ Lá»°C - {scopeText.ToUpper()}";
                directive.ExecutiveSummary = "ChÆ°a ghi nháº­n Ä‘á»§ dá»¯ liá»‡u kiá»ƒm tra trong pháº¡m vi lá»c Ä‘Ã£ chá»n. Cáº§n Ä‘áº©y nhanh tiáº¿n Ä‘á»™ tá»• chá»©c kiá»ƒm tra rÃ¨n luyá»‡n thá»ƒ lá»±c ban Ä‘áº§u Ä‘á»ƒ AI cÃ³ cÆ¡ sá»Ÿ phÃ¢n tÃ­ch.";
                directive.TimeAllocationDirective = "Duy trÃ¬ lá»‹ch thá»ƒ dá»¥c buá»•i sÃ¡ng (05:15 - 05:45) vÃ  rÃ¨n luyá»‡n thá»ƒ thao buá»•i chiá»u (16:30 - 17:30) cÃ¡c ngÃ y trong tuáº§n.";
                directive.RecoveryAndNutritionAdvice = "Äáº£m báº£o Ä‘á»‹nh lÆ°á»£ng kháº©u pháº§n Äƒn quÃ¢n trang quÃ¢n dá»¥ng, bá»• sung nÆ°á»›c Ä‘iá»‡n giáº£i vÃ  vitamin nhÃ³m B trong giai Ä‘oáº¡n chuyá»ƒn mÃ¹a.";
            }
            else
            {
                directive.Title = $"CHá»ˆ THá»Š RÃˆN LUYá»†N & NÃ‚NG CAO THá»‚ Lá»°C QUÃ‚N Sá»° - {scopeText.ToUpper()}";

                if (overallFailRate > 15)
                {
                    directive.ExecutiveSummary = $"ÄÃ¡nh giÃ¡ tÃ¬nh hÃ¬nh thá»ƒ lá»±c táº¡i {scopeText}: Tá»· lá»‡ chÆ°a Ä‘áº¡t chuáº©n ThÃ´ng tÆ° 32 chiáº¿m {overallFailRate:F1}% (á»Ÿ má»©c Cáº¢NH BÃO). Cáº§n má»Ÿ Ä‘á»£t cao Ä‘iá»ƒm huáº¥n luyá»‡n thá»ƒ lá»±c phá»¥ Ä‘áº¡o trong 4 tuáº§n tá»›i, táº­p trung phÃ¢n loáº¡i vÃ  kÃ¨m cáº·p sÃ¡t sao nhÃ³m há»c viÃªn cÃ³ nguy cÆ¡ trÆ°á»£t chuáº©n.";
                }
                else if (overallFailRate > 5)
                {
                    directive.ExecutiveSummary = $"ÄÃ¡nh giÃ¡ tÃ¬nh hÃ¬nh thá»ƒ lá»±c táº¡i {scopeText}: ToÃ n Ä‘Æ¡n vá»‹ duy trÃ¬ ná»n náº¿p rÃ¨n luyá»‡n khÃ¡ tá»‘t (Tá»· lá»‡ Ä‘áº¡t chuáº©n {100 - overallFailRate:F1}%). Tuy nhiÃªn váº«n cÃ²n má»™t bá»™ pháº­n nhá» ({failedTests} lÆ°á»£t) chÆ°a Ä‘á»“ng Ä‘á»u giá»¯a cÃ¡c ná»™i dung sá»©c bá»n vÃ  sá»©c máº¡nh.";
                }
                else
                {
                    directive.ExecutiveSummary = $"ÄÃ¡nh giÃ¡ tÃ¬nh hÃ¬nh thá»ƒ lá»±c táº¡i {scopeText}: Phong trÃ o rÃ¨n luyá»‡n thá»ƒ lá»±c Ä‘áº¡t káº¿t quáº£ XUáº¤T Sáº®C (Tá»· lá»‡ Ä‘áº¡t chuáº©n {100 - overallFailRate:F1}%). Tiáº¿p tá»¥c bá»“i dÆ°á»¡ng cÃ¡c nhÃ¢n tá»‘ nÃ²ng cá»‘t tham gia há»™i thao quÃ¢n sá»± cáº¥p Há»c viá»‡n vÃ  toÃ n quÃ¢n.";
                }

                directive.KeyActionItems.Add("PhÃ¢n nhÃ³m há»c viÃªn theo thá»ƒ lá»±c: ThÃ nh láº­p 'Tá»• rÃ¨n luyá»‡n nÃ¢ng cao' cho cÃ¡c Ä‘á»“ng chÃ­ chÆ°a Ä‘áº¡t chuáº©n dÆ°á»›i sá»± kÃ¨m cáº·p cá»§a cÃ¡n bá»™ Trung Ä‘á»™i.");
                directive.KeyActionItems.Add("Tá»‘i Æ°u hÃ³a giá» thá»ƒ thao buá»•i chiá»u: 45 phÃºt Ä‘áº§u táº­p trung ná»™i dung yáº¿u (XÃ  Ä‘Æ¡n / Cháº¡y bá»n), 15 phÃºt sau tháº£ lá»ng há»“i tÄ©nh.");
                directive.KeyActionItems.Add("Thá»±c hiá»‡n kiá»ƒm tra Ä‘á»‹nh ká»³ 2 tuáº§n/láº§n vÃ o sÃ¡ng Thá»© Báº£y Ä‘á»ƒ Ä‘Ã¡nh giÃ¡ tiáº¿n bá»™ cá»§a tá»«ng cÃ¡ nhÃ¢n.");

                directive.TimeAllocationDirective = overallFailRate > 15
                    ? "TÄƒng cÆ°á»ng thÃªm 3 buá»•i phá»¥ Ä‘áº¡o/tuáº§n (Thá»© 2, 4, 6 tá»« 16:15 - 17:15). Thá»© 7 tá»• chá»©c cháº¡y viá»‡t dÃ£ cá»± ly trung bÃ¬nh."
                    : "Duy trÃ¬ Ä‘á»u Ä‘áº·n 4 buá»•i/tuáº§n theo tiáº¿n trÃ¬nh biá»ƒu; chÃº trá»ng cháº¥t lÆ°á»£ng tá»«ng Ä‘á»™ng tÃ¡c ká»¹ thuáº­t.";

                directive.RecoveryAndNutritionAdvice = "Cháº¥n chá»‰nh cÃ´ng tÃ¡c báº£o Ä‘áº£m nÆ°á»›c uá»‘ng cÃ³ muá»‘i khoÃ¡ng táº¡i bÃ£i táº­p; sau cÃ¡c buá»•i cháº¡y 3000m pháº£i dÃ nh tá»‘i thiá»ƒu 10 phÃºt tháº£ lá»ng cÆ¡ báº¯p trÃ¡nh cÄƒng cÆ¡ chuá»™t rÃºt.";
            }

            result.StrategicDirective = directive;

            // 2. PHÃC Äá»’ CHUYÃŠN SÃ‚U THEO Tá»ªNG NHÃ“M Tá» CHáº¤T THá»‚ Lá»°C (Component Prescriptions)
            // NhÃ³m A: Sá»©c máº¡nh (XÃ  Ä‘Æ¡n, xÃ  kÃ©p)
            var strengthRecords = recordsList.Where(r => 
                (r.Subject?.SubjectName?.Contains("xÃ ", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("chá»‘ng Ä‘áº©y", StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            int strengthTotal = strengthRecords.Count;
            int strengthFail = strengthRecords.Count(r => r.Grade == "KhÃ´ng Ä‘áº¡t");
            double strengthFailRate = strengthTotal > 0 ? (double)strengthFail / strengthTotal * 100 : 0;

            result.ComponentPrescriptions.Add(new FitnessComponentPrescriptionDto
            {
                ComponentName = "Tá»‘ Cháº¥t Sá»©c Máº¡nh CÆ¡ Báº¯p & Chi TrÃªn",
                TargetSubjects = "Co tay xÃ  Ä‘Æ¡n, XÃ  kÃ©p, Chá»‘ng Ä‘áº©y",
                FailRate = Math.Round(strengthFailRate, 1),
                AffectedCadetsCount = strengthFail,
                UrgencyLevel = strengthFailRate >= 18 ? "ðŸ”´ KHáº¨N Cáº¤P" : (strengthFailRate >= 8 ? "ðŸŸ¡ Cáº¦N CHÃš Ã" : "ðŸŸ¢ DUY TRÃŒ"),
                UrgencyColor = strengthFailRate >= 18 ? "#DC2626" : (strengthFailRate >= 8 ? "#D97706" : "#16A34A"),
                UrgencyBackground = strengthFailRate >= 18 ? "#FEE2E2" : (strengthFailRate >= 8 ? "#FEF3C7" : "#DCFCE7"),
                CoreWeaknessAnalysis = strengthFailRate >= 15 
                    ? "Lá»±c bÃ¡m cáº³ng tay vÃ  cÆ¡ lÆ°ng rá»™ng (Latissimus dorsi) cÃ²n yáº¿u; nhiá»u Ä‘á»“ng chÃ­ bá»‹ quÃ¡n tÃ­nh láº¯c ngÆ°á»i khÃ´ng Ä‘Ãºng ká»¹ thuáº­t chuáº©n quÃ¢n sá»±."
                    : "Há»c viÃªn cÆ¡ báº£n náº¯m Ä‘Æ°á»£c ká»¹ thuáº­t; cáº§n gia tÄƒng sá»©c bá»n cÆ¡ báº¯p khi Ä‘áº¡t má»‘c 10 - 12 cÃ¡i.",
                ScientificTrainingProtocol = "â€¢ Tuáº§n 1-2: Táº­p treo xÃ  tÄ©nh tÃ­nh thá»i gian (Dead hang) 3 hiá»‡p x 45 giÃ¢y; kÃ©o xÃ  cÃ³ dÃ¢y khÃ¡ng lá»±c (Rubber band) há»— trá»£ 4 hiá»‡p x 8 láº§n.\nâ€¢ Tuáº§n 3-4: Co tay xÃ  Ä‘Æ¡n cÃ³ ngáº¯t nhá»‹p (2 giÃ¢y giá»¯ Ä‘á»‰nh xÃ  - 3 giÃ¢y háº¡ xuá»‘ng); bá»• trá»£ hÃ­t Ä‘áº¥t kim cÆ°Æ¡ng (Diamond push-ups) 3 hiá»‡p x 15 láº§n.",
                WeeklyScheduleRecommendation = "3 buá»•i/tuáº§n (Thá»© 2, 4, 6 lÃºc 16:30 - 17:15). Láº¯p Ä‘áº·t xÃ  phá»¥ táº¡i há»“i nhÃ  ná»™i vá»¥ Ä‘á»ƒ tranh thá»§ rÃ¨n luyá»‡n.",
                MeasurableTarget = "100% há»c viÃªn kÃ©o Ä‘áº¡t tá»‘i thiá»ƒu 10 cÃ¡i (Äáº¡t chuáº©n TT32); trÃªn 40% Ä‘áº¡t má»‘c 14 cÃ¡i trá»Ÿ lÃªn (KhÃ¡ - Giá»i)."
            });

            // NhÃ³m B: Sá»©c bá»n (Cháº¡y 3000m vÅ© trang)
            var enduranceRecords = recordsList.Where(r => 
                (r.Subject?.SubjectName?.Contains("3000", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("viá»‡t dÃ£", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("bá»n", StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            int enduranceTotal = enduranceRecords.Count;
            int enduranceFail = enduranceRecords.Count(r => r.Grade == "KhÃ´ng Ä‘áº¡t");
            double enduranceFailRate = enduranceTotal > 0 ? (double)enduranceFail / enduranceTotal * 100 : 0;

            result.ComponentPrescriptions.Add(new FitnessComponentPrescriptionDto
            {
                ComponentName = "Tá»‘ Cháº¥t Sá»©c Bá»n Tim Máº¡ch & HÃ´ Háº¥p",
                TargetSubjects = "Cháº¡y vÅ© trang 3000m, HÃ nh quÃ¢n rÃ¨n luyá»‡n",
                FailRate = Math.Round(enduranceFailRate, 1),
                AffectedCadetsCount = enduranceFail,
                UrgencyLevel = enduranceFailRate >= 18 ? "ðŸ”´ KHáº¨N Cáº¤P" : (enduranceFailRate >= 8 ? "ðŸŸ¡ Cáº¦N CHÃš Ã" : "ðŸŸ¢ DUY TRÃŒ"),
                UrgencyColor = enduranceFailRate >= 18 ? "#DC2626" : (enduranceFailRate >= 8 ? "#D97706" : "#16A34A"),
                UrgencyBackground = enduranceFailRate >= 18 ? "#FEE2E2" : (enduranceFailRate >= 8 ? "#FEF3C7" : "#DCFCE7"),
                CoreWeaknessAnalysis = enduranceFailRate >= 15
                    ? "Há»c viÃªn chÆ°a lÃ m chá»§ ká»¹ thuáº­t phÃ¢n phá»‘i sá»©c; thÆ°á»ng xuáº¥t phÃ¡t quÃ¡ nhanh á»Ÿ 800m Ä‘áº§u dáº«n Ä‘áº¿n tá»¥t dá»‘c á»Ÿ ná»­a cuá»‘i Ä‘Æ°á»ng cháº¡y. Ká»¹ thuáº­t nhá»‹p thá»Ÿ chÆ°a Ä‘á»“ng bá»™ bÆ°á»›c cháº¡y."
                    : "Há»c viÃªn duy trÃ¬ ngÆ°á»¡ng hÃ´ háº¥p á»•n Ä‘á»‹nh; cáº§n tá»‘i Æ°u hÃ³a guá»“ng chÃ¢n rÃºt Ä‘Ã­ch á»Ÿ 400m cuá»‘i cÃ¹ng.",
                ScientificTrainingProtocol = "â€¢ PhÆ°Æ¡ng phÃ¡p Fartlek (Biáº¿n tá»‘c): 400m cháº¡y nhanh vá»«a - 200m cháº¡y cháº­m tháº£ lá»ng liÃªn tá»¥c 5 vÃ²ng sÃ¢n.\nâ€¢ Luyá»‡n táº­p nhá»‹p thá»Ÿ 2-2 (2 bÆ°á»›c hÃ­t vÃ o, 2 bÆ°á»›c thá»Ÿ ra dá»©t khoÃ¡t báº±ng mÅ©i vÃ  miá»‡ng).\nâ€¢ TÄƒng cá»± ly lÅ©y tiáº¿n: Tuáº§n 1 cháº¡y 1.8km, Tuáº§n 2 cháº¡y 2.4km, Tuáº§n 3-4 hoÃ n thiá»‡n chuáº©n 3.0km cÃ³ trang bá»‹ sÃºng tiá»ƒu liÃªn AK.",
                WeeklyScheduleRecommendation = "2 buá»•i rÃ¨n cá»± ly dÃ i (Thá»© 3, Thá»© 6) + 1 buá»•i hÃ nh quÃ¢n mang vÃ¡c 15kg vÃ o sÃ¡ng Thá»© 7.",
                MeasurableTarget = "Thá»i gian cháº¡y 3000m toÃ n Ä‘Æ¡n vá»‹ dÆ°á»›i 13 phÃºt 30 giÃ¢y; khÃ´ng cÃ³ há»c viÃªn bá» cuá»™c giá»¯a cháº·ng."
            });

            // NhÃ³m C: Sá»©c nhanh & Bá»™c phÃ¡t (Cháº¡y 100m, Nháº£y xa)
            var speedRecords = recordsList.Where(r => 
                (r.Subject?.SubjectName?.Contains("100", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("nháº£y", StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            int speedTotal = speedRecords.Count;
            int speedFail = speedRecords.Count(r => r.Grade == "KhÃ´ng Ä‘áº¡t");
            double speedFailRate = speedTotal > 0 ? (double)speedFail / speedTotal * 100 : 0;

            result.ComponentPrescriptions.Add(new FitnessComponentPrescriptionDto
            {
                ComponentName = "Tá»‘ Cháº¥t Tá»‘c Äá»™ & Bá»™c PhÃ¡t Tháº§n Kinh CÆ¡",
                TargetSubjects = "Cháº¡y 100m, Nháº£y xa cÃ³ Ä‘Ã ",
                FailRate = Math.Round(speedFailRate, 1),
                AffectedCadetsCount = speedFail,
                UrgencyLevel = speedFailRate >= 15 ? "ðŸ”´ KHáº¨N Cáº¤P" : (speedFailRate >= 6 ? "ðŸŸ¡ Cáº¦N CHÃš Ã" : "ðŸŸ¢ DUY TRÃŒ"),
                UrgencyColor = speedFailRate >= 15 ? "#DC2626" : (speedFailRate >= 6 ? "#D97706" : "#16A34A"),
                UrgencyBackground = speedFailRate >= 15 ? "#FEE2E2" : (speedFailRate >= 6 ? "#FEF3C7" : "#DCFCE7"),
                CoreWeaknessAnalysis = "GÃ³c Ä‘á»™ xuáº¥t phÃ¡t tháº¥p chÆ°a tá»‘i Æ°u, sá»©c bá»™c phÃ¡t cá»§a cÆ¡ báº¯p chÃ¢n vÃ  khá»›p cá»• chÃ¢n cÃ²n háº¡n cháº¿ á»Ÿ 30m gia tá»‘c Ä‘áº§u.",
                ScientificTrainingProtocol = "â€¢ BÃ i táº­p Plyometrics: Báº­t cÃ³c (Frog jumps) 3 hiá»‡p x 20m; nháº£y lÃ² cÃ² Ä‘á»•i chÃ¢n tÄƒng Ä‘á»™ Ä‘Ã n há»“i gÃ¢n gÃ³t Achilles.\nâ€¢ Luyá»‡n ká»¹ thuáº­t xuáº¥t phÃ¡t tháº¥p vá»›i bÃ n Ä‘áº¡p: Cháº¡y tÄƒng tá»‘c 30m - 50m láº·p láº¡i 6 láº§n.",
                WeeklyScheduleRecommendation = "2 buá»•i/tuáº§n lá»“ng ghÃ©p vÃ o Ä‘áº§u giá» thá»ƒ dá»¥c chiá»u (Thá»© 3, Thá»© 5).",
                MeasurableTarget = "Thá»i gian cháº¡y 100m dÆ°á»›i 14.5 giÃ¢y (Chuáº©n Ä‘áº¡t); trÃªn 50% Ä‘áº¡t má»‘c dÆ°á»›i 13.8 giÃ¢y (Chuáº©n KhÃ¡ - Giá»i)."
            });

            // NhÃ³m D: BÆ¡i vÅ© trang & VÆ°á»£t váº­t cáº£n
            var waterRecords = recordsList.Where(r => 
                (r.Subject?.SubjectName?.Contains("bÆ¡i", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (r.Subject?.SubjectName?.Contains("váº­t cáº£n", StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            int waterTotal = waterRecords.Count;
            int waterFail = waterRecords.Count(r => r.Grade == "KhÃ´ng Ä‘áº¡t");
            double waterFailRate = waterTotal > 0 ? (double)waterFail / waterTotal * 100 : 0;

            result.ComponentPrescriptions.Add(new FitnessComponentPrescriptionDto
            {
                ComponentName = "Ká»¹ NÄƒng QuÃ¢n Sá»±: BÆ¡i VÅ© Trang & VÆ°á»£t Váº­t Cáº£n",
                TargetSubjects = "BÆ¡i 100m mang sÃºng, VÆ°á»£t váº­t cáº£n K91",
                FailRate = Math.Round(waterFailRate, 1),
                AffectedCadetsCount = waterFail,
                UrgencyLevel = waterFailRate >= 15 ? "ðŸ”´ KHáº¨N Cáº¤P" : (waterFailRate >= 6 ? "ðŸŸ¡ Cáº¦N CHÃš Ã" : "ðŸŸ¢ DUY TRÃŒ"),
                UrgencyColor = waterFailRate >= 15 ? "#DC2626" : (waterFailRate >= 6 ? "#D97706" : "#16A34A"),
                UrgencyBackground = waterFailRate >= 15 ? "#FEE2E2" : (waterFailRate >= 6 ? "#FEF3C7" : "#DCFCE7"),
                CoreWeaknessAnalysis = "TÃ¢m lÃ½ sá»£ nÆ°á»›c á»Ÿ má»™t sá»‘ Ä‘á»“ng chÃ­ má»›i; ká»¹ thuáº­t Ä‘áº¡p chÃ¢n áº¿ch mang giÃ y vÃ  ba lÃ´ chÆ°a thuáº§n thá»¥c lÃ m tiÃªu hao thá»ƒ lá»±c nhanh.",
                ScientificTrainingProtocol = "â€¢ RÃ¨n luyá»‡n ká»¹ nÄƒng ná»•i ngá»­a giá»¯ sÃºng khÃ´ trÃªn máº·t nÆ°á»›c 5 phÃºt liÃªn tá»¥c.\nâ€¢ Äáº¡p chÃ¢n áº¿ch Ã´m phao bÆ¡i 4 x 50m; chuyá»ƒn tiáº¿p sang bÆ¡i vÅ© trang mang sÃºng tiá»ƒu liÃªn AK vÃ  phao lÆ°ng trang bá»‹.",
                WeeklyScheduleRecommendation = "2 buá»•i/tuáº§n táº¡i há»“ bÆ¡i quÃ¢n sá»± (Thá»© 4, Thá»© 7).",
                MeasurableTarget = "100% bÆ¡i Ä‘Æ°á»£c cá»± ly 100m vÅ© trang an toÃ n tuyá»‡t Ä‘á»‘i; bÆ¡i Ä‘áº¡t chuáº©n dÆ°á»›i 2 phÃºt 30 giÃ¢y."
            });

            // 3. PHÃC Äá»’ Bá»’I DÆ¯á» NG CÃ NHÃ‚N HÃ“A (Personalized Cadet Prescriptions)
            var failedCadetRecords = recordsList.Where(r => r.Grade == "KhÃ´ng Ä‘áº¡t").Take(20).ToList();
            foreach (var fr in failedCadetRecords)
            {
                string subjName = fr.Subject?.SubjectName ?? "RÃ¨n luyá»‡n thá»ƒ lá»±c";
                string tailoredPlan;
                string standard;

                if (subjName.Contains("xÃ ", StringComparison.OrdinalIgnoreCase))
                {
                    tailoredPlan = "Treo xÃ  tÄ©nh 45s x 3 hiá»‡p + KÃ©o xÃ  cÃ³ dÃ¢y há»— trá»£ 8 láº§n/hiá»‡p. Táº­p má»—i chiá»u trÆ°á»›c giá» Äƒn cÆ¡m.";
                    standard = "Tá»‘i thiá»ƒu 10 cÃ¡i (Äáº¡t chuáº©n TT32)";
                }
                else if (subjName.Contains("3000", StringComparison.OrdinalIgnoreCase))
                {
                    tailoredPlan = "Cháº¡y cá»± ly tÄƒng dáº§n (1.5km -> 2km -> 3km), luyá»‡n nhá»‹p thá»Ÿ 2-2 káº¿t há»£p Ä‘i bá»™ tháº£ lá»ng.";
                    standard = "Thá»i gian dÆ°á»›i 13 phÃºt 30 giÃ¢y";
                }
                else if (subjName.Contains("100", StringComparison.OrdinalIgnoreCase))
                {
                    tailoredPlan = "Luyá»‡n báº­t cÃ³c 3 hiá»‡p 20m + Cháº¡y biáº¿n tá»‘c 30m - 50m nÃ¢ng cao Ä‘Ã¹i bá»™c phÃ¡t tá»‘c Ä‘á»™.";
                    standard = "Thá»i gian dÆ°á»›i 14.5 giÃ¢y";
                }
                else
                {
                    tailoredPlan = "Táº­p bá»• trá»£ thá»ƒ lá»±c chuyÃªn biá»‡t theo hÆ°á»›ng dáº«n cá»§a CÃ¡n bá»™ huáº¥n luyá»‡n; kiá»ƒm tra láº¡i sau 3 tuáº§n.";
                    standard = "Äáº¡t tiÃªu chuáº©n mÃ´n theo TT 32";
                }

                result.PersonalizedCadetPrescriptions.Add(new PersonalizedCadetPrescriptionDto
                {
                    CadetId = fr.CadetId,
                    CadetCode = fr.Cadet?.CadetCode ?? $"HV-{fr.CadetId}",
                    FullName = fr.Cadet?.FullName ?? "Há»c viÃªn",
                    Unit = fr.Cadet?.Unit ?? "ÄÆ¡n vá»‹",
                    ClassName = fr.Cadet?.ClassName ?? (fr.Cadet?.MilitaryClass?.ClassName ?? "Lá»›p"),
                    WeakSubject = subjName,
                    CurrentPerformance = fr.ScoreValue.ToString("0.##"),
                    StandardThreshold = standard,
                    TailoredExercisePlan = tailoredPlan,
                    RemedialTimeline = "30 ngÃ y (Kiá»ƒm tra sÃ¡t háº¡ch láº¡i)",
                    AssignedCoach = $"CÃ¡n bá»™ {fr.Cadet?.Unit ?? "Äáº¡i Ä‘á»™i"} trá»±c tiáº¿p Ä‘Ã´n Ä‘á»‘c"
                });
            }

            return Task.FromResult(result);
        }
    }
}


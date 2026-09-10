using System;

namespace QL_HocVien.Models.Entity
{
    public class TrainingEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // TiÃªu Ä‘á» sá»± kiá»‡n
        public string Category { get; set; } = "Kiá»ƒm tra thá»ƒ lá»±c"; // "Kiá»ƒm tra thá»ƒ lá»±c", "Thi cá»­ quÃ¢n sá»±", "Táº­p luyá»‡n / RÃ¨n luyá»‡n", "Há»™i thao / Sá»± kiá»‡n"
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
        public string TargetUnit { get; set; } = "ToÃ n Ä‘Æ¡n vá»‹"; // ÄÆ¡n vá»‹/Lá»›p Ã¡p dá»¥ng (Äáº¡i Ä‘á»™i 1, ToÃ n Ä‘Æ¡n vá»‹, K26A...)
        public string Location { get; set; } = string.Empty; // Thao trÆ°á»ng, BÃ£i táº­p xÃ , SÃ¢n váº­n Ä‘á»™ng, Bá»ƒ bÆ¡i...
        public string Priority { get; set; } = "BÃ¬nh thÆ°á»ng"; // "Kháº©n cáº¥p", "Cao", "BÃ¬nh thÆ°á»ng"
        public string Status { get; set; } = "Äang chuáº©n bá»‹"; // "Äang chuáº©n bá»‹", "Äang diá»…n ra", "ÄÃ£ hoÃ n thÃ nh", "Táº¡m hoÃ£n"
        public string Description { get; set; } = string.Empty; // Ná»™i dung chá»‰ thá»‹, ghi chÃº chi tiáº¿t
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Thuá»™c tÃ­nh hiá»ƒn thá»‹ UI theo chuáº©n Celandar.png (KhÃ´ng lÆ°u CSDL)
        public string DayOfWeekVietnamese => StartDate.DayOfWeek switch
        {
            DayOfWeek.Monday => "Thá»© Hai",
            DayOfWeek.Tuesday => "Thá»© Ba",
            DayOfWeek.Wednesday => "Thá»© TÆ°",
            DayOfWeek.Thursday => "Thá»© NÄƒm",
            DayOfWeek.Friday => "Thá»© SÃ¡u",
            DayOfWeek.Saturday => "Thá»© Báº£y",
            DayOfWeek.Sunday => "Chá»§ Nháº­t",
            _ => string.Empty
        };

        public string WatermarkArtPath
        {
            get
            {
                if (Category == "Kiá»ƒm tra thá»ƒ lá»±c" || Title.Contains("thá»ƒ lá»±c", StringComparison.OrdinalIgnoreCase))
                    return "/Assets/Images/timeline_art_watchtower.png";
                if (Category == "Thi cá»­ quÃ¢n sá»±" || Title.Contains("báº¯n sÃºng", StringComparison.OrdinalIgnoreCase))
                    return "/Assets/Images/timeline_art_ak_shooting.png";
                if (Category == "Táº­p luyá»‡n / RÃ¨n luyá»‡n" || Title.Contains("hÃ nh quÃ¢n", StringComparison.OrdinalIgnoreCase))
                    return "/Assets/Images/timeline_art_marching.png";
                return "/Assets/Images/timeline_art_watchtower.png";
            }
        }

        public string CategoryBg => Category switch
        {
            "Kiá»ƒm tra thá»ƒ lá»±c" => "#0C683B",
            "Thi cá»­ quÃ¢n sá»±" => "#0A6A45",
            "Táº­p luyá»‡n / RÃ¨n luyá»‡n" => "#0C683B",
            "Há»™i thao / Sá»± kiá»‡n" => "#1D4ED8",
            _ => "#334155"
        };

        public string PriorityBg => Priority switch
        {
            "Kháº©n cáº¥p" => "#FEE2E2",
            "Cao" => "#FEE2E2",
            _ => "#E2E8F0"
        };

        public string PriorityFg => Priority switch
        {
            "Kháº©n cáº¥p" => "#DC2626",
            "Cao" => "#DC2626",
            _ => "#475569"
        };

        public string StatusBg => Status switch
        {
            "ÄÃ£ hoÃ n thÃ nh" => "#DCFCE7",
            "Äang diá»…n ra" => "#DBEAFE",
            "Äang chuáº©n bá»‹" => "#DCFCE7",
            _ => "#F1F5F9"
        };

        public string StatusFg => Status switch
        {
            "ÄÃ£ hoÃ n thÃ nh" => "#15803D",
            "Äang diá»…n ra" => "#1D4ED8",
            "Äang chuáº©n bá»‹" => "#15803D",
            _ => "#475569"
        };
    }
}


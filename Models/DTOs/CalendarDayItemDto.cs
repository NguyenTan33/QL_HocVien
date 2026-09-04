using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QL_HocVien.Models.DTOs
{
    public partial class CalendarDayItemDto : ObservableObject
    {
        public DateTime Date { get; set; }
        public int DayNumber => Date.Day;
        public bool IsCurrentMonth { get; set; }
        public bool IsToday => Date.Date == DateTime.Today;

        [ObservableProperty]
        private bool _isSelected;

        public List<TrainingEvent> Events { get; set; } = new();

        public bool HasEvents => Events.Count > 0;
        public int EventCount => Events.Count;

        public bool HasExamEvent => Events.Any(e => e.Category == "Thi cử quân sự");
        public bool HasFitnessEvent => Events.Any(e => e.Category == "Kiểm tra thể lực");
        public bool HasPracticeEvent => Events.Any(e => e.Category == "Tập luyện / Rèn luyện");
        public bool HasSportsEvent => Events.Any(e => e.Category == "Hội thao / Sự kiện");

        public string PrimaryCategoryColor
        {
            get
            {
                if (HasExamEvent) return "#DC2626";     // Đỏ cờ
                if (HasFitnessEvent) return "#0F766E";  // Xanh teal
                if (HasPracticeEvent) return "#16A34A"; // Xanh lục
                if (HasSportsEvent) return "#2563EB";   // Xanh dương
                return "#64748B";
            }
        }

        /// <summary>
        /// Badge hiển thị ngắn gọn, đẹp mắt, vừa vặn trên ô lịch mà không bao giờ bị cắt chữ
        /// </summary>
        public string CategoryBadgeText
        {
            get
            {
                if (!HasEvents) return string.Empty;
                if (EventCount > 1) return $"● {EventCount} sự kiện";

                var cat = Events[0].Category;
                return cat switch
                {
                    "Thi cử quân sự" => "🎯 Thi cử",
                    "Kiểm tra thể lực" => "⏱️ Thể lực",
                    "Tập luyện / Rèn luyện" => "🏋️ Rèn luyện",
                    "Hội thao / Sự kiện" => "🏆 Hội thao",
                    _ => Events[0].Title.Length > 10 ? Events[0].Title[..9] + "…" : Events[0].Title
                };
            }
        }

        public string PrimaryBadgeBackground
        {
            get
            {
                if (EventCount > 1) return "#FEF3C7";  // Amber nhạt
                if (HasExamEvent) return "#FEE2E2";    // Đỏ nhạt
                if (HasFitnessEvent) return "#CCFBF1";  // Teal nhạt
                if (HasPracticeEvent) return "#DCFCE7"; // Lục nhạt
                if (HasSportsEvent) return "#DBEAFE";   // Lam nhạt
                return "#F1F5F9";
            }
        }

        public string PrimaryBadgeBorder
        {
            get
            {
                if (EventCount > 1) return "#FDE68A";
                if (HasExamEvent) return "#FECACA";
                if (HasFitnessEvent) return "#99F6E4";
                if (HasPracticeEvent) return "#BBF7D0";
                if (HasSportsEvent) return "#BFDBFE";
                return "#CBD5E1";
            }
        }

        public string PrimaryBadgeForeground
        {
            get
            {
                if (EventCount > 1) return "#B45309";  // Cam đậm
                if (HasExamEvent) return "#DC2626";    // Đỏ
                if (HasFitnessEvent) return "#0F766E";  // Teal đậm
                if (HasPracticeEvent) return "#16A34A"; // Lục đậm
                if (HasSportsEvent) return "#2563EB";   // Lam đậm
                return "#334155";
            }
        }

        public string EventSummaryText
        {
            get
            {
                if (!HasEvents) return string.Empty;
                if (EventCount == 1) return Events[0].Title;
                return $"{Events[0].Title} (+{EventCount - 1})";
            }
        }

        public string FullTooltip
        {
            get
            {
                if (!HasEvents) return $"{Date:dd/MM/yyyy}: Không có sự kiện";
                var lines = Events.Select(e => $"• [{e.Category}] {e.Title}\n  ⏱ Thời gian: {e.StartDate:dd/MM/yyyy} ➔ {e.EndDate:dd/MM/yyyy}\n  📍 Địa điểm: {e.Location} | Đơn vị: {e.TargetUnit}");
                return $"📅 Ngày {Date:dd/MM/yyyy} ({Events.Count} sự kiện):\n\n" + string.Join("\n\n", lines);
            }
        }
    }
}

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
                if (EventCount > 1) return "#3D3014";  // Hổ phách sẫm
                if (HasExamEvent) return "#3D1616";    // Đỏ sẫm
                if (HasFitnessEvent) return "#1A2F2B";  // Teal sẫm
                if (HasPracticeEvent) return "#16331C"; // Lục sẫm
                if (HasSportsEvent) return "#172A45";   // Lam sẫm
                return "#253628";
            }
        }

        public string PrimaryBadgeBorder
        {
            get
            {
                if (EventCount > 1) return "#8C7D46";
                if (HasExamEvent) return "#DC2626";
                if (HasFitnessEvent) return "#14B8A6";
                if (HasPracticeEvent) return "#22C55E";
                if (HasSportsEvent) return "#3B82F6";
                return "#625C34";
            }
        }

        public string PrimaryBadgeForeground
        {
            get
            {
                if (EventCount > 1) return "#FFE17A";  // Vàng sáng
                if (HasExamEvent) return "#FCA5A5";    // Đỏ sáng
                if (HasFitnessEvent) return "#99F6E4";  // Teal sáng
                if (HasPracticeEvent) return "#86EFAC"; // Lục sáng
                if (HasSportsEvent) return "#93C5FD";   // Lam sáng
                return "#F7F1D4";
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

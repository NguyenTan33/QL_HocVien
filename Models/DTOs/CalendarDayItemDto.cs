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
                var lines = Events.Select(e => $"• [{e.Category}] {e.Title} ({e.StartDate:dd/MM} - {e.EndDate:dd/MM}) - Đơn vị: {e.TargetUnit}");
                return $"{Date:dd/MM/yyyy}:\n" + string.Join("\n", lines);
            }
        }
    }
}

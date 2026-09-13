using System;
using System.Windows;
using System.Windows.Media;

namespace QL_HocVien.Services.Implementations
{
    public class ThemeService : IThemeService
    {
        public bool IsCombatMode { get; private set; } = true;
        public static bool CurrentIsCombatMode { get; private set; } = true;
        public event Action<bool>? ThemeChanged;

        public void ToggleTheme()
        {
            ApplyTheme(!IsCombatMode);
        }

        public void ApplyTheme(bool isCombatMode)
        {
            IsCombatMode = isCombatMode;
            CurrentIsCombatMode = isCombatMode;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                var res = Application.Current.Resources;

                if (isCombatMode)
                {
                    // ==========================================
                    // 1. CHẾ ĐỘ TÁC CHIẾN (COMBAT COMMAND CENTER)
                    // ==========================================
                    SetBrush(res, "PageBackgroundBrush", "#101B14");
                    SetBrush(res, "CardBackgroundBrush", "#1B2A1E");
                    SetBrush(res, "CardBorderBrush", "#625C34");
                    SetBrush(res, "KpiCardBackgroundBrush", "#18251A");

                    SetBrush(res, "TextPrimaryBrush", "#FFFFFF");
                    SetBrush(res, "TextSecondaryBrush", "#E5E7EB");
                    SetBrush(res, "TextDarkBrush", "#20241D");

                    SetBrush(res, "AccentBrush", "#F2C94C");
                    SetBrush(res, "AccentLightBrush", "#FFE17A");

                    SetBrush(res, "InputBackgroundBrush", "#EAE0BA");
                    SetBrush(res, "InputForegroundBrush", "#20241D");
                    SetBrush(res, "InputBorderBrush", "#8C7D46");

                    SetBrush(res, "PrimaryBrush", "#8F1515");
                    SetBrush(res, "PrimaryLightBrush", "#B32020");
                    SetBrush(res, "PrimaryDarkBrush", "#741010");
                    SetBrush(res, "PrimaryBorderBrush", "#D4B547");

                    SetBrush(res, "SecondaryButtonBackgroundBrush", "#253628");
                    SetBrush(res, "SecondaryButtonBorderBrush", "#8B7A3D");
                    SetBrush(res, "SecondaryButtonForegroundBrush", "#FFFFFF");

                    SetBrush(res, "WarningBrush", "#D97706");
                    SetBrush(res, "DangerBrush", "#DC2626");
                    SetBrush(res, "SuccessBrush", "#16A34A");

                    // Semantic Status & KPI Brushes (Dã chiến)
                    SetBrush(res, "WarningCardBackgroundBrush", "#2D240E");
                    SetBrush(res, "WarningCardBorderBrush", "#F59E0B");
                    SetBrush(res, "WarningCardForegroundBrush", "#FDE047");

                    SetBrush(res, "DangerCardBackgroundBrush", "#2A1515");
                    SetBrush(res, "DangerCardBorderBrush", "#DC2626");
                    SetBrush(res, "DangerCardForegroundBrush", "#F87171");

                    SetBrush(res, "SuccessCardBackgroundBrush", "#12281C");
                    SetBrush(res, "SuccessCardBorderBrush", "#356E49");
                    SetBrush(res, "SuccessCardForegroundBrush", "#86EFAC");

                    SetBrush(res, "InfoCardBackgroundBrush", "#11232E");
                    SetBrush(res, "InfoCardBorderBrush", "#3A5E70");
                    SetBrush(res, "InfoCardForegroundBrush", "#7DD3FC");

                    // DataGrid
                    SetBrush(res, "DataGridHeaderBackgroundBrush", "#35452C");
                    SetBrush(res, "DataGridHeaderForegroundBrush", "#FFE17A");
                    SetBrush(res, "DataGridHeaderBorderBrush", "#625C34");
                    SetBrush(res, "DataGridRowBackgroundBrush", "#1B2A1E");
                    SetBrush(res, "DataGridRowAltBackgroundBrush", "#243424");
                    SetBrush(res, "DataGridRowForegroundBrush", "#F7F1D4");
                    SetBrush(res, "DataGridLineBrush", "#4F573E");
                    SetBrush(res, "DataGridSelectedBackgroundBrush", "#4E1215");
                    SetBrush(res, "DataGridSelectedForegroundBrush", "#FFE17A");
                    SetBrush(res, "DataGridMissingRowBrush", "#382914");
                    SetBrush(res, "DataGridMissingRowTextBrush", "#F7F1D4");
                    SetBrush(res, "DataGridHoverBrush", "#2A3C2B");

                    // TabHeader
                    SetBrush(res, "TabHeaderBackgroundBrush", "#253628");
                    SetBrush(res, "TabHeaderForegroundBrush", "#B9B99E");
                    SetBrush(res, "TabHeaderActiveBackgroundBrush", "#8F1515");
                    SetBrush(res, "TabHeaderActiveForegroundBrush", "#FFE17A");
                    SetBrush(res, "TabHeaderBorderBrush", "#625C34");

                    // Training Timeline Tác Chiến
                    SetBrush(res, "TimelineBackgroundBrush", "#EDE5D0");
                    SetBrush(res, "TimelineBorderBrush", "#8A784A");
                    SetBrush(res, "TimelineCardBackgroundBrush", "#FAF6EC");
                    SetBrush(res, "TimelineCardBorderBrush", "#D5CAAE");
                    SetBrush(res, "TimelineFilterBackgroundBrush", "#162417");
                    SetBrush(res, "TimelineFilterBorderBrush", "#72683A");
                    SetBrush(res, "TimelineKpi1BackgroundBrush", "#15261A");
                    SetBrush(res, "TimelineKpi1BorderBrush", "#7B6F3E");
                    SetBrush(res, "TimelineKpi2BackgroundBrush", "#11232E");
                    SetBrush(res, "TimelineKpi2BorderBrush", "#3A5E70");
                    SetBrush(res, "TimelineKpi3BackgroundBrush", "#2D1215");
                    SetBrush(res, "TimelineKpi3BorderBrush", "#782E32");
                    SetBrush(res, "TimelineKpi4BackgroundBrush", "#12281C");
                    SetBrush(res, "TimelineKpi4BorderBrush", "#356E49");

                    // Sidebar & Active Menu (Tác chiến)
                    if (res.Contains("MilActiveMenuGradient"))
                        res["ActiveMenuBackgroundBrush"] = res["MilActiveMenuGradient"];
                    else
                        SetBrush(res, "ActiveMenuBackgroundBrush", "#8F1515");
                    SetBrush(res, "ActiveMenuBorderBrush", "#ECC867");
                    SetBrush(res, "ActiveMenuForegroundBrush", "#FFFFFF");
                    SetBrush(res, "SidebarNavHoverBackgroundBrush", "#1E2E20");
                    SetBrush(res, "SidebarNavHoverBorderBrush", "#35452C");
                    SetBrush(res, "SidebarIconBrush", "#F2C94C");

                    // Đồng bộ lại các khóa kế thừa
                    SetBrush(res, "BgDarkBrush", "#101B14");
                    SetBrush(res, "BgLightBrush", "#18251A");
                    SetBrush(res, "BgCardBrush", "#1B2A1E");
                    SetBrush(res, "BorderBrush", "#625C34");
                }
                else
                {
                    // ==========================================
                    // 2. CHẾ ĐỘ HÀNH CHÍNH (ADMINISTRATIVE BASIC LIGHT)
                    // ==========================================
                    SetBrush(res, "PageBackgroundBrush", "#F8FAFC");
                    SetBrush(res, "CardBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "CardBorderBrush", "#CBD5E1");
                    SetBrush(res, "KpiCardBackgroundBrush", "#FFFFFF");

                    SetBrush(res, "TextPrimaryBrush", "#0F172A");
                    SetBrush(res, "TextSecondaryBrush", "#64748B");
                    SetBrush(res, "TextDarkBrush", "#0F172A");

                    SetBrush(res, "AccentBrush", "#2563EB");
                    SetBrush(res, "AccentLightBrush", "#1E3A8A");

                    SetBrush(res, "InputBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "InputForegroundBrush", "#0F172A");
                    SetBrush(res, "InputBorderBrush", "#CBD5E1");

                    SetBrush(res, "PrimaryBrush", "#1E3A8A");
                    SetBrush(res, "PrimaryLightBrush", "#2563EB");
                    SetBrush(res, "PrimaryDarkBrush", "#172554");
                    SetBrush(res, "PrimaryBorderBrush", "#3B82F6");

                    SetBrush(res, "SecondaryButtonBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "SecondaryButtonBorderBrush", "#CBD5E1");
                    SetBrush(res, "SecondaryButtonForegroundBrush", "#0F172A");

                    SetBrush(res, "WarningBrush", "#D97706");
                    SetBrush(res, "DangerBrush", "#DC2626");
                    SetBrush(res, "SuccessBrush", "#16A34A");

                    // Semantic Status & KPI Brushes (Công vụ / Hành chính mềm mại)
                    SetBrush(res, "WarningCardBackgroundBrush", "#FEF3C7");
                    SetBrush(res, "WarningCardBorderBrush", "#F59E0B");
                    SetBrush(res, "WarningCardForegroundBrush", "#92400E");

                    SetBrush(res, "DangerCardBackgroundBrush", "#FEE2E2");
                    SetBrush(res, "DangerCardBorderBrush", "#FCA5A5");
                    SetBrush(res, "DangerCardForegroundBrush", "#991B1B");

                    SetBrush(res, "SuccessCardBackgroundBrush", "#DCFCE7");
                    SetBrush(res, "SuccessCardBorderBrush", "#86EFAC");
                    SetBrush(res, "SuccessCardForegroundBrush", "#166534");

                    SetBrush(res, "InfoCardBackgroundBrush", "#EFF6FF");
                    SetBrush(res, "InfoCardBorderBrush", "#BFDBFE");
                    SetBrush(res, "InfoCardForegroundBrush", "#1E40AF");

                    // DataGrid
                    SetBrush(res, "DataGridHeaderBackgroundBrush", "#E2E8F0");
                    SetBrush(res, "DataGridHeaderForegroundBrush", "#0F172A");
                    SetBrush(res, "DataGridHeaderBorderBrush", "#CBD5E1");
                    SetBrush(res, "DataGridRowBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "DataGridRowAltBackgroundBrush", "#F8FAFC");
                    SetBrush(res, "DataGridRowForegroundBrush", "#0F172A");
                    SetBrush(res, "DataGridLineBrush", "#E2E8F0");
                    SetBrush(res, "DataGridSelectedBackgroundBrush", "#DBEAFE");
                    SetBrush(res, "DataGridSelectedForegroundBrush", "#1E3A8A");
                    SetBrush(res, "DataGridMissingRowBrush", "#FEF3C7");
                    SetBrush(res, "DataGridMissingRowTextBrush", "#92400E");
                    SetBrush(res, "DataGridHoverBrush", "#F1F5F9");

                    // TabHeader
                    SetBrush(res, "TabHeaderBackgroundBrush", "#F1F5F9");
                    SetBrush(res, "TabHeaderForegroundBrush", "#64748B");
                    SetBrush(res, "TabHeaderActiveBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TabHeaderActiveForegroundBrush", "#1E3A8A");
                    SetBrush(res, "TabHeaderBorderBrush", "#CBD5E1");

                    // Training Timeline Hành Chính (Trắng sạch, viền xám, thanh lịch)
                    SetBrush(res, "TimelineBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineBorderBrush", "#CBD5E1");
                    SetBrush(res, "TimelineCardBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineCardBorderBrush", "#E2E8F0");
                    SetBrush(res, "TimelineFilterBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineFilterBorderBrush", "#CBD5E1");
                    SetBrush(res, "TimelineKpi1BackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineKpi1BorderBrush", "#E2E8F0");
                    SetBrush(res, "TimelineKpi2BackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineKpi2BorderBrush", "#E2E8F0");
                    SetBrush(res, "TimelineKpi3BackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineKpi3BorderBrush", "#E2E8F0");
                    SetBrush(res, "TimelineKpi4BackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineKpi4BorderBrush", "#E2E8F0");

                    // Sidebar & Active Menu (Hành chính công vụ basic)
                    SetBrush(res, "ActiveMenuBackgroundBrush", "#2563EB");
                    SetBrush(res, "ActiveMenuBorderBrush", "#60A5FA");
                    SetBrush(res, "ActiveMenuForegroundBrush", "#FFFFFF");
                    SetBrush(res, "SidebarNavHoverBackgroundBrush", "#1E293B");
                    SetBrush(res, "SidebarNavHoverBorderBrush", "#334155");
                    SetBrush(res, "SidebarIconBrush", "#94A3B8");

                    // Đồng bộ lại các khóa kế thừa
                    SetBrush(res, "BgDarkBrush", "#0F172A");
                    SetBrush(res, "BgLightBrush", "#F8FAFC");
                    SetBrush(res, "BgCardBrush", "#FFFFFF");
                    SetBrush(res, "BorderBrush", "#CBD5E1");
                }

                ThemeChanged?.Invoke(isCombatMode);
            });
        }

        private static void SetBrush(ResourceDictionary res, string key, string hexColor)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hexColor)!;
            if (brush.CanFreeze) brush.Freeze();
            res[key] = brush;
        }
    }
}

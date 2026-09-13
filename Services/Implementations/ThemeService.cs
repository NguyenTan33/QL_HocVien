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

                    res["CardWatermarkVisibility"] = Visibility.Visible;

                    SetBrush(res, "SecondaryButtonBackgroundBrush", "#253628");
                    SetBrush(res, "SecondaryButtonBorderBrush", "#8B7A3D");
                    SetBrush(res, "SecondaryButtonForegroundBrush", "#FFFFFF");
                    SetBrush(res, "SecondaryButtonHoverBackgroundBrush", "#354B38");
                    SetBrush(res, "SecondaryButtonHoverBorderBrush", "#A28D42");
                    SetBrush(res, "SecondaryButtonHoverForegroundBrush", "#FFE17A");
                    SetBrush(res, "SecondaryButtonPressedBackgroundBrush", "#1E2E20");

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
                    SetBrush(res, "TimelineKpi1BackgroundBrush", "#1B3322");
                    SetBrush(res, "TimelineKpi1BorderBrush", "#C4A035");
                    SetBrush(res, "TimelineKpi1CircleBackgroundBrush", "#122417");
                    SetBrush(res, "TimelineKpi1CircleBorderBrush", "#D4B547");
                    SetBrush(res, "TimelineKpi1ForegroundBrush", "#FFE17A");

                    SetBrush(res, "TimelineKpi2BackgroundBrush", "#132F42");
                    SetBrush(res, "TimelineKpi2BorderBrush", "#38BDF8");
                    SetBrush(res, "TimelineKpi2CircleBackgroundBrush", "#0E2333");
                    SetBrush(res, "TimelineKpi2CircleBorderBrush", "#38BDF8");
                    SetBrush(res, "TimelineKpi2ForegroundBrush", "#7DD3FC");

                    SetBrush(res, "TimelineKpi3BackgroundBrush", "#3D161A");
                    SetBrush(res, "TimelineKpi3BorderBrush", "#EF4444");
                    SetBrush(res, "TimelineKpi3CircleBackgroundBrush", "#2B0E11");
                    SetBrush(res, "TimelineKpi3CircleBorderBrush", "#EF4444");
                    SetBrush(res, "TimelineKpi3ForegroundBrush", "#FCA5A5");

                    SetBrush(res, "TimelineKpi4BackgroundBrush", "#133824");
                    SetBrush(res, "TimelineKpi4BorderBrush", "#22C55E");
                    SetBrush(res, "TimelineKpi4CircleBackgroundBrush", "#0D2618");
                    SetBrush(res, "TimelineKpi4CircleBorderBrush", "#22C55E");
                    SetBrush(res, "TimelineKpi4ForegroundBrush", "#86EFAC");

                    SetBrush(res, "TimelineKpiSubtextBrush", "#D5CEBD");

                    // Unit Tree Cơ Cấu Đơn Vị (Tác Chiến)
                    SetBrush(res, "UnitTreeContainerBackgroundBrush", "#0F1A12");
                    SetBrush(res, "UnitTreeContainerBorderBrush", "#596645");
                    SetBrush(res, "UnitCardBackgroundBrush", "#1B3122");
                    SetBrush(res, "UnitCardBorderBrush", "#7C6E3E");
                    SetBrush(res, "UnitCardHoverBorderBrush", "#F2C94C");
                    SetBrush(res, "UnitCardSelectedBackgroundBrush", "#28442E");
                    SetBrush(res, "UnitCardSelectedBorderBrush", "#FFE17A");
                    SetBrush(res, "UnitCardSubtextBrush", "#D5CEBD");
                    SetBrush(res, "UnitBranchLineBrush", "#D4B547");
                    SetBrush(res, "UnitAddButtonBackgroundBrush", "#163A24");
                    SetBrush(res, "UnitAddButtonBorderBrush", "#22C55E");
                    SetBrush(res, "UnitAddButtonForegroundBrush", "#86EFAC");
                    SetBrush(res, "UnitEditButtonBackgroundBrush", "#132B3E");
                    SetBrush(res, "UnitEditButtonBorderBrush", "#38BDF8");
                    SetBrush(res, "UnitEditButtonForegroundBrush", "#7DD3FC");

                    // Training Timeline Tác Chiến Buttons
                    SetBrush(res, "TimelineHeaderButtonBackgroundBrush", "#182619");
                    SetBrush(res, "TimelineHeaderButtonBorderBrush", "#756A3C");
                    SetBrush(res, "TimelineHeaderButtonForegroundBrush", "#E8CE7E");
                    SetBrush(res, "TimelineHeaderButtonHoverBackgroundBrush", "#233825");
                    SetBrush(res, "TimelineHeaderButtonHoverForegroundBrush", "#FFF0A3");

                    SetBrush(res, "TimelinePrimaryButtonBackgroundBrush", "#A31818");
                    SetBrush(res, "TimelinePrimaryButtonBorderBrush", "#E55353");
                    SetBrush(res, "TimelinePrimaryButtonForegroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelinePrimaryButtonHoverBackgroundBrush", "#BA2020");

                    SetBrush(res, "TimelineFilterButtonBackgroundBrush", "#8A1414");
                    SetBrush(res, "TimelineFilterButtonBorderBrush", "#DFC57B");
                    SetBrush(res, "TimelineFilterButtonForegroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineFilterButtonHoverBackgroundBrush", "#BA1E1E");

                    SetBrush(res, "TimelineFilterResetBackgroundBrush", "#1A2619");
                    SetBrush(res, "TimelineFilterResetBorderBrush", "#6D673B");
                    SetBrush(res, "TimelineFilterResetForegroundBrush", "#E5DFCA");
                    SetBrush(res, "TimelineFilterResetHoverBackgroundBrush", "#263B26");

                    SetBrush(res, "TimelineItemCompleteBackgroundBrush", "#FBF7EE");
                    SetBrush(res, "TimelineItemCompleteBorderBrush", "#C2B59B");
                    SetBrush(res, "TimelineItemCompleteForegroundBrush", "#1A2216");
                    SetBrush(res, "TimelineItemCompleteHoverBackgroundBrush", "#FFFDF8");
                    SetBrush(res, "TimelineItemCompleteHoverForegroundBrush", "#1A2216");

                    SetBrush(res, "TimelineItemEditBackgroundBrush", "#FBF7EE");
                    SetBrush(res, "TimelineItemEditBorderBrush", "#C2B59B");
                    SetBrush(res, "TimelineItemEditForegroundBrush", "#1A2216");
                    SetBrush(res, "TimelineItemEditHoverBackgroundBrush", "#FFFDF8");
                    SetBrush(res, "TimelineItemEditHoverForegroundBrush", "#1A2216");

                    SetBrush(res, "TimelineItemDeleteBackgroundBrush", "#C51F1F");
                    SetBrush(res, "TimelineItemDeleteBorderBrush", "#831010");
                    SetBrush(res, "TimelineItemDeleteForegroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineItemDeleteHoverBackgroundBrush", "#D92626");

                    SetBrush(res, "TimelineActionButtonBackgroundBrush", "#9E1616");
                    SetBrush(res, "TimelineActionButtonBorderBrush", "#ECC867");
                    SetBrush(res, "TimelineActionButtonForegroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineActionButtonHoverBackgroundBrush", "#B71C1C");

                    // Sidebar & Active Menu (Tác chiến)
                    if (res.Contains("MilActiveMenuGradient"))
                        res["ActiveMenuBackgroundBrush"] = res["MilActiveMenuGradient"];
                    else
                        SetLinearGradientBrush(res, "ActiveMenuBackgroundBrush", "#8F1515", "#5C0A0A");
                    SetBrush(res, "ActiveMenuBorderBrush", "#ECC867");
                    SetBrush(res, "ActiveMenuForegroundBrush", "#FFFFFF");
                    SetBrush(res, "SidebarBackgroundBrush", "#131C14");
                    SetBrush(res, "SidebarBorderBrush", "#18291D");
                    SetBrush(res, "SidebarNavForegroundBrush", "#CBD5E1");
                    SetBrush(res, "SidebarNavHoverBackgroundBrush", "#1E2E20");
                    SetBrush(res, "SidebarNavHoverBorderBrush", "#35452C");
                    SetBrush(res, "SidebarIconBrush", "#F2C94C");

                    // TopBar & BottomBar (Tác chiến)
                    if (res.Contains("MilTopBarGradient"))
                        res["TopBarBackgroundBrush"] = res["MilTopBarGradient"];
                    else
                        SetLinearGradientBrush(res, "TopBarBackgroundBrush", "#6B0F0F", "#3B0707");
                    SetBrush(res, "TopBarBorderBrush", "#625C34");
                    SetBrush(res, "TopBarTitleForegroundBrush", "#FFE17A");
                    SetBrush(res, "TopBarSubForegroundBrush", "#F7F1D4");
                    SetBrush(res, "BottomBarBackgroundBrush", "#101B14");
                    SetBrush(res, "BottomBarBorderBrush", "#625C34");

                    // 6 Thẻ KPI Dashboard (Tác chiến dã chiến sắc nét, tương phản cao)
                    SetBrush(res, "Kpi1BackgroundBrush", "#1C3022");
                    SetBrush(res, "Kpi1BorderBrush", "#8C7D46");
                    SetBrush(res, "Kpi1ForegroundBrush", "#FFE17A");
                    SetBrush(res, "Kpi1SubtextBrush", "#E5E7EB");

                    SetBrush(res, "Kpi2BackgroundBrush", "#142C38");
                    SetBrush(res, "Kpi2BorderBrush", "#38BDF8");
                    SetBrush(res, "Kpi2ForegroundBrush", "#7DD3FC");
                    SetBrush(res, "Kpi2SubtextBrush", "#BAE6FD");

                    SetBrush(res, "Kpi3BackgroundBrush", "#1B2B3E");
                    SetBrush(res, "Kpi3BorderBrush", "#60A5FA");
                    SetBrush(res, "Kpi3ForegroundBrush", "#93C5FD");
                    SetBrush(res, "Kpi3SubtextBrush", "#BFDBFE");

                    SetBrush(res, "Kpi4BackgroundBrush", "#153323");
                    SetBrush(res, "Kpi4BorderBrush", "#22C55E");
                    SetBrush(res, "Kpi4ForegroundBrush", "#86EFAC");
                    SetBrush(res, "Kpi4SubtextBrush", "#BBF7D0");

                    SetBrush(res, "Kpi5BackgroundBrush", "#33280F");
                    SetBrush(res, "Kpi5BorderBrush", "#F59E0B");
                    SetBrush(res, "Kpi5ForegroundBrush", "#FDE047");
                    SetBrush(res, "Kpi5SubtextBrush", "#FEF08A");

                    SetBrush(res, "Kpi6BackgroundBrush", "#361517");
                    SetBrush(res, "Kpi6BorderBrush", "#EF4444");
                    SetBrush(res, "Kpi6ForegroundBrush", "#FCA5A5");
                    SetBrush(res, "Kpi6SubtextBrush", "#FECACA");

                    // Đồng bộ lại các khóa kế thừa
                    SetBrush(res, "BgDarkBrush", "#101B14");
                    SetBrush(res, "BgLightBrush", "#18251A");
                    SetBrush(res, "BgCardBrush", "#1B2A1E");
                    SetBrush(res, "BorderBrush", "#625C34");
                }
                else
                {
                    // ==========================================
                    // 2. CHẾ ĐỘ HÀNH CHÍNH (ADMINISTRATIVE MODERN LIGHT AI)
                    // ==========================================
                    SetBrush(res, "PageBackgroundBrush", "#F8FAFC");
                    SetBrush(res, "CardBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "CardBorderBrush", "#E2E8F0");
                    SetBrush(res, "KpiCardBackgroundBrush", "#FFFFFF");

                    SetBrush(res, "TextPrimaryBrush", "#0F172A");
                    SetBrush(res, "TextSecondaryBrush", "#475569");
                    SetBrush(res, "TextDarkBrush", "#0F172A");

                    SetBrush(res, "AccentBrush", "#2563EB");
                    SetBrush(res, "AccentLightBrush", "#3B82F6");

                    SetBrush(res, "InputBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "InputForegroundBrush", "#0F172A");
                    SetBrush(res, "InputBorderBrush", "#CBD5E1");

                    SetBrush(res, "PrimaryBrush", "#2563EB");
                    SetBrush(res, "PrimaryLightBrush", "#3B82F6");
                    SetBrush(res, "PrimaryDarkBrush", "#1D4ED8");
                    SetBrush(res, "PrimaryBorderBrush", "#2563EB");

                    res["CardWatermarkVisibility"] = Visibility.Collapsed;

                    SetBrush(res, "SecondaryButtonBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "SecondaryButtonBorderBrush", "#CBD5E1");
                    SetBrush(res, "SecondaryButtonForegroundBrush", "#1E293B");
                    // Vibrant modern hover: Rực rỡ và sắc nét khi chạm vào nút thao tác
                    SetBrush(res, "SecondaryButtonHoverBackgroundBrush", "#2563EB");
                    SetBrush(res, "SecondaryButtonHoverBorderBrush", "#1D4ED8");
                    SetBrush(res, "SecondaryButtonHoverForegroundBrush", "#FFFFFF");
                    SetBrush(res, "SecondaryButtonPressedBackgroundBrush", "#1E40AF");

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
                    SetBrush(res, "DataGridHeaderBackgroundBrush", "#F1F5F9");
                    SetBrush(res, "DataGridHeaderForegroundBrush", "#1E293B");
                    SetBrush(res, "DataGridHeaderBorderBrush", "#CBD5E1");
                    SetBrush(res, "DataGridRowBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "DataGridRowAltBackgroundBrush", "#F8FAFC");
                    SetBrush(res, "DataGridRowForegroundBrush", "#0F172A");
                    SetBrush(res, "DataGridLineBrush", "#E2E8F0");
                    SetBrush(res, "DataGridSelectedBackgroundBrush", "#DBEAFE");
                    SetBrush(res, "DataGridSelectedForegroundBrush", "#1E40AF");
                    SetBrush(res, "DataGridMissingRowBrush", "#FEF3C7");
                    SetBrush(res, "DataGridMissingRowTextBrush", "#92400E");
                    SetBrush(res, "DataGridHoverBrush", "#EFF6FF");

                    // TabHeader
                    SetBrush(res, "TabHeaderBackgroundBrush", "#F1F5F9");
                    SetBrush(res, "TabHeaderForegroundBrush", "#64748B");
                    SetBrush(res, "TabHeaderActiveBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TabHeaderActiveForegroundBrush", "#2563EB");
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
                    SetBrush(res, "TimelineKpi1CircleBackgroundBrush", "#EFF6FF");
                    SetBrush(res, "TimelineKpi1CircleBorderBrush", "#BFDBFE");
                    SetBrush(res, "TimelineKpi1ForegroundBrush", "#2563EB");

                    SetBrush(res, "TimelineKpi2BackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineKpi2BorderBrush", "#E2E8F0");
                    SetBrush(res, "TimelineKpi2CircleBackgroundBrush", "#F0FDF4");
                    SetBrush(res, "TimelineKpi2CircleBorderBrush", "#BBF7D0");
                    SetBrush(res, "TimelineKpi2ForegroundBrush", "#16A34A");

                    SetBrush(res, "TimelineKpi3BackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineKpi3BorderBrush", "#E2E8F0");
                    SetBrush(res, "TimelineKpi3CircleBackgroundBrush", "#FEF2F2");
                    SetBrush(res, "TimelineKpi3CircleBorderBrush", "#FECACA");
                    SetBrush(res, "TimelineKpi3ForegroundBrush", "#DC2626");

                    SetBrush(res, "TimelineKpi4BackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineKpi4BorderBrush", "#E2E8F0");
                    SetBrush(res, "TimelineKpi4CircleBackgroundBrush", "#ECFDF5");
                    SetBrush(res, "TimelineKpi4CircleBorderBrush", "#A7F3D0");
                    SetBrush(res, "TimelineKpi4ForegroundBrush", "#059669");

                    SetBrush(res, "TimelineKpiSubtextBrush", "#64748B");

                    // Unit Tree Cơ Cấu Đơn Vị (Hành Chính)
                    SetBrush(res, "UnitTreeContainerBackgroundBrush", "#F8FAFC");
                    SetBrush(res, "UnitTreeContainerBorderBrush", "#E2E8F0");
                    SetBrush(res, "UnitCardBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "UnitCardBorderBrush", "#E2E8F0");
                    SetBrush(res, "UnitCardHoverBorderBrush", "#2563EB");
                    SetBrush(res, "UnitCardSelectedBackgroundBrush", "#EFF6FF");
                    SetBrush(res, "UnitCardSelectedBorderBrush", "#2563EB");
                    SetBrush(res, "UnitCardSubtextBrush", "#64748B");
                    SetBrush(res, "UnitBranchLineBrush", "#3B82F6");
                    SetBrush(res, "UnitAddButtonBackgroundBrush", "#ECFDF5");
                    SetBrush(res, "UnitAddButtonBorderBrush", "#10B981");
                    SetBrush(res, "UnitAddButtonForegroundBrush", "#059669");
                    SetBrush(res, "UnitEditButtonBackgroundBrush", "#EFF6FF");
                    SetBrush(res, "UnitEditButtonBorderBrush", "#3B82F6");
                    SetBrush(res, "UnitEditButtonForegroundBrush", "#2563EB");

                    // Training Timeline Hành Chính Buttons (Đồng bộ, hiện đại, màu sắc bắt mắt)
                    SetBrush(res, "TimelineHeaderButtonBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineHeaderButtonBorderBrush", "#CBD5E1");
                    SetBrush(res, "TimelineHeaderButtonForegroundBrush", "#1E293B");
                    SetBrush(res, "TimelineHeaderButtonHoverBackgroundBrush", "#2563EB");
                    SetBrush(res, "TimelineHeaderButtonHoverForegroundBrush", "#FFFFFF");

                    SetBrush(res, "TimelinePrimaryButtonBackgroundBrush", "#2563EB");
                    SetBrush(res, "TimelinePrimaryButtonBorderBrush", "#1D4ED8");
                    SetBrush(res, "TimelinePrimaryButtonForegroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelinePrimaryButtonHoverBackgroundBrush", "#1D4ED8");

                    SetBrush(res, "TimelineFilterButtonBackgroundBrush", "#2563EB");
                    SetBrush(res, "TimelineFilterButtonBorderBrush", "#1D4ED8");
                    SetBrush(res, "TimelineFilterButtonForegroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineFilterButtonHoverBackgroundBrush", "#1D4ED8");

                    SetBrush(res, "TimelineFilterResetBackgroundBrush", "#F1F5F9");
                    SetBrush(res, "TimelineFilterResetBorderBrush", "#CBD5E1");
                    SetBrush(res, "TimelineFilterResetForegroundBrush", "#475569");
                    SetBrush(res, "TimelineFilterResetHoverBackgroundBrush", "#E2E8F0");

                    SetBrush(res, "TimelineItemCompleteBackgroundBrush", "#ECFDF5");
                    SetBrush(res, "TimelineItemCompleteBorderBrush", "#A7F3D0");
                    SetBrush(res, "TimelineItemCompleteForegroundBrush", "#059669");
                    SetBrush(res, "TimelineItemCompleteHoverBackgroundBrush", "#10B981");
                    SetBrush(res, "TimelineItemCompleteHoverForegroundBrush", "#FFFFFF");

                    SetBrush(res, "TimelineItemEditBackgroundBrush", "#EFF6FF");
                    SetBrush(res, "TimelineItemEditBorderBrush", "#BFDBFE");
                    SetBrush(res, "TimelineItemEditForegroundBrush", "#2563EB");
                    SetBrush(res, "TimelineItemEditHoverBackgroundBrush", "#2563EB");
                    SetBrush(res, "TimelineItemEditHoverForegroundBrush", "#FFFFFF");

                    SetBrush(res, "TimelineItemDeleteBackgroundBrush", "#FEF2F2");
                    SetBrush(res, "TimelineItemDeleteBorderBrush", "#FECACA");
                    SetBrush(res, "TimelineItemDeleteForegroundBrush", "#DC2626");
                    SetBrush(res, "TimelineItemDeleteHoverBackgroundBrush", "#EF4444");

                    SetBrush(res, "TimelineActionButtonBackgroundBrush", "#2563EB");
                    SetBrush(res, "TimelineActionButtonBorderBrush", "#1D4ED8");
                    SetBrush(res, "TimelineActionButtonForegroundBrush", "#FFFFFF");
                    SetBrush(res, "TimelineActionButtonHoverBackgroundBrush", "#1D4ED8");

                    // TopBar & BottomBar (Hành chính sáng hiện đại)
                    SetBrush(res, "TopBarBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TopBarBorderBrush", "#E2E8F0");
                    SetBrush(res, "TopBarTitleForegroundBrush", "#DC2626");
                    SetBrush(res, "TopBarSubForegroundBrush", "#0F172A");
                    SetBrush(res, "BottomBarBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "BottomBarBorderBrush", "#E2E8F0");

                    // Sidebar & Active Menu (Hành chính sáng - Nền trắng, active xanh gradient, hover nhẹ)
                    SetBrush(res, "SidebarBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "SidebarBorderBrush", "#E2E8F0");
                    SetBrush(res, "SidebarNavForegroundBrush", "#334155");
                    SetBrush(res, "SidebarNavHoverBackgroundBrush", "#F1F5F9");
                    SetBrush(res, "SidebarNavHoverBorderBrush", "#E2E8F0");
                    SetBrush(res, "SidebarIconBrush", "#2563EB");
                    SetLinearGradientBrush(res, "ActiveMenuBackgroundBrush", "#2563EB", "#1D4ED8");
                    SetBrush(res, "ActiveMenuBorderBrush", "#3B82F6");
                    SetBrush(res, "ActiveMenuForegroundBrush", "#FFFFFF");

                    // 6 THẺ KPI DASHBOARD ĐA SẮC MÀU RỰC RỠ CHUẨN AI (SaaS Modern Analytics)
                    // Card 1: Xanh ngọc Emerald / Teal (Quân số)
                    SetLinearGradientBrush(res, "Kpi1BackgroundBrush", "#059669", "#10B981");
                    SetBrush(res, "Kpi1BorderBrush", "#059669");
                    SetBrush(res, "Kpi1ForegroundBrush", "#FFFFFF");
                    SetBrush(res, "Kpi1SubtextBrush", "#D1FAE5");

                    // Card 2: Xanh dương Hoàng gia Royal Blue (Học phần tín chỉ)
                    SetLinearGradientBrush(res, "Kpi2BackgroundBrush", "#1D4ED8", "#3B82F6");
                    SetBrush(res, "Kpi2BorderBrush", "#1D4ED8");
                    SetBrush(res, "Kpi2ForegroundBrush", "#FFFFFF");
                    SetBrush(res, "Kpi2SubtextBrush", "#DBEAFE");

                    // Card 3: Xanh lá rừng Mint Forest Green (Điểm trung bình GPA)
                    SetLinearGradientBrush(res, "Kpi3BackgroundBrush", "#15803D", "#22C55E");
                    SetBrush(res, "Kpi3BorderBrush", "#15803D");
                    SetBrush(res, "Kpi3ForegroundBrush", "#FFFFFF");
                    SetBrush(res, "Kpi3SubtextBrush", "#DCFCE7");

                    // Card 4: Tím Indigo AI Modern (Đạt chuẩn tích lũy)
                    SetLinearGradientBrush(res, "Kpi4BackgroundBrush", "#4F46E5", "#7C3AED");
                    SetBrush(res, "Kpi4BorderBrush", "#4F46E5");
                    SetBrush(res, "Kpi4ForegroundBrush", "#FFFFFF");
                    SetBrush(res, "Kpi4SubtextBrush", "#EDE9FE");

                    // Card 5: Vàng hổ phách Amber Orange (Học lực Khá & Giỏi)
                    SetLinearGradientBrush(res, "Kpi5BackgroundBrush", "#D97706", "#F59E0B");
                    SetBrush(res, "Kpi5BorderBrush", "#D97706");
                    SetBrush(res, "Kpi5ForegroundBrush", "#FFFFFF");
                    SetBrush(res, "Kpi5SubtextBrush", "#FEF3C7");

                    // Card 6: Đỏ San hô Coral Crimson (Cảnh báo nợ môn / Chưa kiểm tra)
                    SetLinearGradientBrush(res, "Kpi6BackgroundBrush", "#DC2626", "#EF4444");
                    SetBrush(res, "Kpi6BorderBrush", "#DC2626");
                    SetBrush(res, "Kpi6ForegroundBrush", "#FFFFFF");
                    SetBrush(res, "Kpi6SubtextBrush", "#FEE2E2");

                    // Đồng bộ lại các khóa kế thừa
                    SetBrush(res, "BgDarkBrush", "#0F172A");
                    SetBrush(res, "BgLightBrush", "#F8FAFC");
                    SetBrush(res, "BgCardBrush", "#FFFFFF");
                    SetBrush(res, "BorderBrush", "#E2E8F0");
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

        private static void SetLinearGradientBrush(ResourceDictionary res, string key, string color1, string color2, Point? start = null, Point? end = null)
        {
            var c1 = (Color)ColorConverter.ConvertFromString(color1);
            var c2 = (Color)ColorConverter.ConvertFromString(color2);
            var brush = new LinearGradientBrush(c1, c2, start ?? new Point(0, 0), end ?? new Point(1, 1));
            if (brush.CanFreeze) brush.Freeze();
            res[key] = brush;
        }
    }
}

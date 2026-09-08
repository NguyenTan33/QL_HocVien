using System;
using System.Windows;
using System.Windows.Media;

namespace QL_HocVien.Services
{
    public class ThemeService : IThemeService
    {
        public bool IsCombatMode { get; private set; } = true;

        public void ToggleTheme()
        {
            ApplyTheme(!IsCombatMode);
        }

        public void ApplyTheme(bool isCombatMode)
        {
            IsCombatMode = isCombatMode;

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

                    SetBrush(res, "TextPrimaryBrush", "#F7F1D4");
                    SetBrush(res, "TextSecondaryBrush", "#B9B99E");
                    SetBrush(res, "TextDarkBrush", "#20241D");

                    SetBrush(res, "InputBackgroundBrush", "#EAE0BA");
                    SetBrush(res, "InputForegroundBrush", "#20241D");
                    SetBrush(res, "InputBorderBrush", "#8C7D46");

                    SetBrush(res, "PrimaryBrush", "#8F1515");
                    SetBrush(res, "PrimaryLightBrush", "#B32020");
                    SetBrush(res, "PrimaryDarkBrush", "#741010");
                    SetBrush(res, "PrimaryBorderBrush", "#D4B547");

                    SetBrush(res, "SecondaryButtonBackgroundBrush", "#253628");
                    SetBrush(res, "SecondaryButtonBorderBrush", "#8B7A3D");
                    SetBrush(res, "SecondaryButtonForegroundBrush", "#F7F1D4");

                    SetBrush(res, "DataGridHeaderBackgroundBrush", "#35452C");
                    SetBrush(res, "DataGridHeaderForegroundBrush", "#FFE17A");
                    SetBrush(res, "DataGridHeaderBorderBrush", "#625C34");
                    SetBrush(res, "DataGridRowBackgroundBrush", "#1B2A1E");
                    SetBrush(res, "DataGridRowAltBackgroundBrush", "#243424");
                    SetBrush(res, "DataGridRowForegroundBrush", "#F7F1D4");
                    SetBrush(res, "DataGridLineBrush", "#4F573E");
                    SetBrush(res, "DataGridSelectedBackgroundBrush", "#4E1215");
                    SetBrush(res, "DataGridSelectedForegroundBrush", "#FFE17A");

                    SetBrush(res, "TabHeaderBackgroundBrush", "#253628");
                    SetBrush(res, "TabHeaderForegroundBrush", "#B9B99E");
                    SetBrush(res, "TabHeaderActiveBackgroundBrush", "#8F1515");
                    SetBrush(res, "TabHeaderActiveForegroundBrush", "#FFE17A");
                    SetBrush(res, "TabHeaderBorderBrush", "#625C34");

                    // Đồng bộ lại các khóa kế thừa
                    SetBrush(res, "BgDarkBrush", "#101B14");
                    SetBrush(res, "BgLightBrush", "#18251A");
                    SetBrush(res, "BgCardBrush", "#1B2A1E");
                    SetBrush(res, "BorderBrush", "#625C34");
                }
                else
                {
                    // ==========================================
                    // 2. CHẾ ĐỘ HÀNH CHÍNH (ADMINISTRATIVE LIGHT)
                    // ==========================================
                    SetBrush(res, "PageBackgroundBrush", "#F8FAFC");
                    SetBrush(res, "CardBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "CardBorderBrush", "#CBD5E1");
                    SetBrush(res, "KpiCardBackgroundBrush", "#EFF6FF");

                    SetBrush(res, "TextPrimaryBrush", "#0F172A");
                    SetBrush(res, "TextSecondaryBrush", "#64748B");
                    SetBrush(res, "TextDarkBrush", "#0F172A");

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

                    SetBrush(res, "DataGridHeaderBackgroundBrush", "#E2E8F0");
                    SetBrush(res, "DataGridHeaderForegroundBrush", "#0F172A");
                    SetBrush(res, "DataGridHeaderBorderBrush", "#CBD5E1");
                    SetBrush(res, "DataGridRowBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "DataGridRowAltBackgroundBrush", "#F1F5F9");
                    SetBrush(res, "DataGridRowForegroundBrush", "#0F172A");
                    SetBrush(res, "DataGridLineBrush", "#E2E8F0");
                    SetBrush(res, "DataGridSelectedBackgroundBrush", "#DBEAFE");
                    SetBrush(res, "DataGridSelectedForegroundBrush", "#1E3A8A");

                    SetBrush(res, "TabHeaderBackgroundBrush", "#F1F5F9");
                    SetBrush(res, "TabHeaderForegroundBrush", "#64748B");
                    SetBrush(res, "TabHeaderActiveBackgroundBrush", "#FFFFFF");
                    SetBrush(res, "TabHeaderActiveForegroundBrush", "#1E3A8A");
                    SetBrush(res, "TabHeaderBorderBrush", "#CBD5E1");

                    // Đồng bộ lại các khóa kế thừa
                    SetBrush(res, "BgDarkBrush", "#0F172A");
                    SetBrush(res, "BgLightBrush", "#F8FAFC");
                    SetBrush(res, "BgCardBrush", "#FFFFFF");
                    SetBrush(res, "BorderBrush", "#CBD5E1");
                }
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

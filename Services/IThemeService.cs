namespace QL_HocVien.Services
{
    public interface IThemeService
    {
        bool IsCombatMode { get; }
        event Action<bool>? ThemeChanged;
        void ApplyTheme(bool isCombatMode);
        void ToggleTheme();
    }
}

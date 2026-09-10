namespace QL_HocVien.Services.Interfaces
{
    public interface IThemeService
    {
        bool IsCombatMode { get; }
        event Action<bool>? ThemeChanged;
        void ApplyTheme(bool isCombatMode);
        void ToggleTheme();
    }
}


namespace QL_HocVien.Services
{
    public interface IThemeService
    {
        bool IsCombatMode { get; }
        void ApplyTheme(bool isCombatMode);
        void ToggleTheme();
    }
}

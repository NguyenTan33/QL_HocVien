using System.Threading.Tasks;

namespace QL_HocVien.Services.Interfaces
{
    /// <summary>
    /// Service hiá»ƒn thá»‹ há»™p thoáº¡i nháº­p máº­t kháº©u báº£o máº­t cáº¥p 2 (SOLID - DIP, SRP).
    /// GiÃºp tÃ¡ch biá»‡t logic giao diá»‡n (UI) ra khá»i Service nghiá»‡p vá»¥ (SecurityGateService).
    /// </summary>
    public interface ISecurityDialogService
    {
        /// <summary>
        /// Hiá»ƒn thá»‹ há»™p thoáº¡i yÃªu cáº§u ngÆ°á»i dÃ¹ng nháº­p máº­t kháº©u báº£o máº­t cáº¥p 2.
        /// </summary>
        /// <param name="actionDescription">MÃ´ táº£ hÃ nh Ä‘á»™ng cáº§n báº£o vá»‡</param>
        /// <param name="verifier">HÃ m kiá»ƒm tra máº­t kháº©u do SecurityGateService cung cáº¥p</param>
        /// <returns>True náº¿u ngÆ°á»i dÃ¹ng xÃ¡c thá»±c thÃ nh cÃ´ng, False náº¿u há»§y hoáº·c tháº¥t báº¡i</returns>
        Task<bool> ShowPasswordVerificationDialogAsync(string actionDescription, System.Func<string, bool> verifier);
    }
}


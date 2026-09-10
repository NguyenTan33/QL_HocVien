using System;
using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Interfaces
{
    public interface IUpdateService
    {
        /// <summary>
        /// Láº¥y phiÃªn báº£n hiá»‡n táº¡i cá»§a á»©ng dá»¥ng Ä‘ang cháº¡y.
        /// </summary>
        Version GetCurrentVersion();

        /// <summary>
        /// Kiá»ƒm tra cáº­p nháº­t tá»« mÃ¡y chá»§/GitHub chá»©a tá»‡p version.json.
        /// </summary>
        Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    }
}


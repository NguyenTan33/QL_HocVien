using System;
using System.Threading;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services.Interfaces
{
    public interface IDownloadUpdateService
    {
        /// <summary>
        /// Táº£i tá»‡p cÃ i Ä‘áº·t cáº­p nháº­t tá»« URL vÃ  bÃ¡o cÃ¡o tiáº¿n Ä‘á»™ theo thá»i gian thá»±c.
        /// </summary>
        Task<string> DownloadInstallerAsync(
            string downloadUrl, 
            IProgress<DownloadProgressReport>? progress = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Khá»Ÿi cháº¡y bá»™ cÃ i Ä‘áº·t Ä‘Ã£ táº£i vá» vÃ  Ä‘Ã³ng á»©ng dá»¥ng hiá»‡n táº¡i.
        /// </summary>
        void LaunchInstallerAndExit(string installerPath);
    }
}


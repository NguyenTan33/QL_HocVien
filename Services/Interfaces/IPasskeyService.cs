using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QL_HocVien.Models;

namespace QL_HocVien.Services
{
    public interface IPasskeyService
    {
        DateTime TrialExpirationDate { get; }
        bool IsTrialActive { get; }
        Task<(bool Success, string Message)> ActivatePasskeyAsync(string username, string passkey);
        Task<bool> HasUserActivatedPasskeyAsync(string username);
        Task<List<AccountPasskey>> GetAllPasskeysAsync();
    }
}

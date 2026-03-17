using Redact1.Models;

namespace Redact1.Services
{
    public interface IStorageService
    {
        string? GetAuthToken();
        void SetAuthToken(string? token);
        void ClearAuthToken();

        User? GetUser();
        void SetUser(User? user);
        void ClearUser();

        AgencyConfig? GetAgencyConfig();
        void SetAgencyConfig(AgencyConfig? config);
        void ClearAgencyConfig();

        void ClearAll();
    }
}

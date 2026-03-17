using Moq;
using Redact1.Models;
using Redact1.Services;

namespace Redact1.Tests.Mocks;

public static class MockStorageService
{
    public static Mock<IStorageService> Create()
    {
        var mock = new Mock<IStorageService>();

        string? authToken = null;
        User? user = null;
        AgencyConfig? agencyConfig = null;

        mock.Setup(x => x.GetAuthToken()).Returns(() => authToken);
        mock.Setup(x => x.SetAuthToken(It.IsAny<string?>()))
            .Callback<string?>(t => authToken = t);
        mock.Setup(x => x.ClearAuthToken())
            .Callback(() => authToken = null);

        mock.Setup(x => x.GetUser()).Returns(() => user);
        mock.Setup(x => x.SetUser(It.IsAny<User?>()))
            .Callback<User?>(u => user = u);
        mock.Setup(x => x.ClearUser())
            .Callback(() => user = null);

        mock.Setup(x => x.GetAgencyConfig()).Returns(() => agencyConfig);
        mock.Setup(x => x.SetAgencyConfig(It.IsAny<AgencyConfig?>()))
            .Callback<AgencyConfig?>(c => agencyConfig = c);
        mock.Setup(x => x.ClearAgencyConfig())
            .Callback(() => agencyConfig = null);

        mock.Setup(x => x.ClearAll())
            .Callback(() =>
            {
                authToken = null;
                user = null;
                agencyConfig = null;
            });

        return mock;
    }
}

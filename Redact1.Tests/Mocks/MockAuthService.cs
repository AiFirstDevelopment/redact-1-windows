using Moq;
using Redact1.Models;
using Redact1.Services;

namespace Redact1.Tests.Mocks;

public static class MockAuthService
{
    public static Mock<IAuthService> Create(bool isAuthenticated = false, bool isEnrolled = true, bool isSupervisor = false)
    {
        var mock = new Mock<IAuthService>();
        var user = isAuthenticated ? MockApiService.CreateTestUser(isSupervisor) : null;

        mock.SetupGet(x => x.CurrentUser).Returns(user);
        mock.SetupGet(x => x.IsAuthenticated).Returns(isAuthenticated);
        mock.SetupGet(x => x.IsEnrolled).Returns(isEnrolled);
        mock.SetupGet(x => x.CurrentAgency).Returns(isEnrolled ? new AgencyConfig
        {
            Code = "DEMO",
            Name = "Demo Police Department",
            ApiBaseUrl = "https://test.api.com"
        } : null);

        mock.Setup(x => x.TryRestoreSessionAsync())
            .ReturnsAsync(isAuthenticated);

        mock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(MockApiService.CreateTestUser(isSupervisor));

        mock.Setup(x => x.LogoutAsync())
            .Returns(Task.CompletedTask)
            .Callback(() =>
            {
                mock.SetupGet(x => x.CurrentUser).Returns((User?)null);
                mock.SetupGet(x => x.IsAuthenticated).Returns(false);
                mock.Raise(x => x.AuthStateChanged += null, mock.Object, null);
            });

        mock.Setup(x => x.SetDepartmentCode(It.IsAny<string>()))
            .Callback(() =>
            {
                mock.SetupGet(x => x.IsEnrolled).Returns(true);
            });

        mock.Setup(x => x.ClearEnrollment())
            .Callback(() =>
            {
                mock.SetupGet(x => x.IsEnrolled).Returns(false);
                mock.SetupGet(x => x.CurrentAgency).Returns((AgencyConfig?)null);
            });

        return mock;
    }
}

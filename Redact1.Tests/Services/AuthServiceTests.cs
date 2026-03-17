using FluentAssertions;
using Moq;
using Redact1.Models;
using Redact1.Services;
using Redact1.Tests.Mocks;

namespace Redact1.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IApiService> _mockApiService;
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockApiService = MockApiService.Create();
        _mockStorageService = MockStorageService.Create();
        _authService = new AuthService(_mockApiService.Object, _mockStorageService.Object);
    }

    [Fact]
    public void Constructor_InitializesWithNullUser()
    {
        _authService.CurrentUser.Should().BeNull();
        _authService.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void IsEnrolled_WhenNoConfig_ReturnsFalse()
    {
        _authService.IsEnrolled.Should().BeFalse();
    }

    [Fact]
    public void IsEnrolled_WhenConfigExists_ReturnsTrue()
    {
        var config = new AgencyConfig { Code = "TEST" };
        _mockStorageService.Object.SetAgencyConfig(config);

        _authService.IsEnrolled.Should().BeTrue();
    }

    [Fact]
    public async Task TryRestoreSessionAsync_WithNoToken_ReturnsFalse()
    {
        var result = await _authService.TryRestoreSessionAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryRestoreSessionAsync_WithValidToken_ReturnsTrue()
    {
        var user = MockApiService.CreateTestUser();
        _mockStorageService.Object.SetAuthToken("valid-token");
        _mockStorageService.Object.SetUser(user);
        _mockApiService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

        var result = await _authService.TryRestoreSessionAsync();

        result.Should().BeTrue();
        _authService.CurrentUser.Should().NotBeNull();
        _authService.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task TryRestoreSessionAsync_WithInvalidToken_ClearsAndReturnsFalse()
    {
        _mockStorageService.Object.SetAuthToken("invalid-token");
        _mockStorageService.Object.SetUser(MockApiService.CreateTestUser());
        _mockApiService.Setup(x => x.GetCurrentUserAsync()).ThrowsAsync(new Exception("Unauthorized"));

        var result = await _authService.TryRestoreSessionAsync();

        result.Should().BeFalse();
        _mockStorageService.Object.GetAuthToken().Should().BeNull();
        _mockStorageService.Object.GetUser().Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_Success_SetsUserAndToken()
    {
        var user = MockApiService.CreateTestUser();
        User? eventUser = null;
        _authService.AuthStateChanged += (s, u) => eventUser = u;

        var result = await _authService.LoginAsync("test@pd.local", "password");

        result.Should().NotBeNull();
        _authService.CurrentUser.Should().NotBeNull();
        _authService.IsAuthenticated.Should().BeTrue();
        _mockStorageService.Object.GetAuthToken().Should().Be("test-token");
        eventUser.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithEmployeeId_SendsCorrectRequest()
    {
        await _authService.LoginAsync("12345", "password", useEmployeeId: true);

        _mockApiService.Verify(x => x.LoginAsync(It.Is<LoginRequest>(r =>
            r.EmployeeId == "12345" && r.Email == null
        )), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ClearsUserAndToken()
    {
        // First login
        await _authService.LoginAsync("test@pd.local", "password");
        _authService.IsAuthenticated.Should().BeTrue();

        User? eventUser = new User();
        _authService.AuthStateChanged += (s, u) => eventUser = u;

        // Then logout
        await _authService.LogoutAsync();

        _authService.CurrentUser.Should().BeNull();
        _authService.IsAuthenticated.Should().BeFalse();
        _mockStorageService.Object.GetAuthToken().Should().BeNull();
        _mockStorageService.Object.GetUser().Should().BeNull();
        eventUser.Should().BeNull();
    }

    [Fact]
    public async Task LogoutAsync_HandlesApiError()
    {
        await _authService.LoginAsync("test@pd.local", "password");
        _mockApiService.Setup(x => x.LogoutAsync()).ThrowsAsync(new Exception("Network error"));

        // Should not throw
        await _authService.LogoutAsync();

        _authService.CurrentUser.Should().BeNull();
        _authService.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void SetDepartmentCode_SavesConfig()
    {
        _authService.SetDepartmentCode("SPRINGFIELD-PD");

        var config = _mockStorageService.Object.GetAgencyConfig();
        config.Should().NotBeNull();
        config!.Code.Should().Be("SPRINGFIELD-PD");
        config.Name.Should().Contain("SPRINGFIELD-PD");
    }

    [Fact]
    public void SetDepartmentCode_Demo_CreatesDemoConfig()
    {
        _authService.SetDepartmentCode("DEMO");

        var config = _mockStorageService.Object.GetAgencyConfig();
        config.Should().NotBeNull();
        config!.Code.Should().Be("DEMO");
        config.Name.Should().Be("Demo Police Department");
    }

    [Fact]
    public async Task ClearEnrollment_ClearsAllData()
    {
        // Setup some data
        await _authService.LoginAsync("test@pd.local", "password");
        _authService.SetDepartmentCode("TEST");

        User? eventUser = new User();
        _authService.AuthStateChanged += (s, u) => eventUser = u;

        // Clear
        _authService.ClearEnrollment();

        _mockStorageService.Object.GetAgencyConfig().Should().BeNull();
        _mockStorageService.Object.GetAuthToken().Should().BeNull();
        _mockStorageService.Object.GetUser().Should().BeNull();
        _authService.CurrentUser.Should().BeNull();
        eventUser.Should().BeNull();
    }
}

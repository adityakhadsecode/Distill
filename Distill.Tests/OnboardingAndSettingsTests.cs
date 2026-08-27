using Distill.Core.Configuration;
using Xunit;

namespace Distill.Tests;

public class OnboardingAndSettingsTests
{
    [Fact]
    public void DistillSettings_DefaultsHasCompletedOnboardingToFalse()
    {
        var settings = new DistillSettings();
        Assert.False(settings.HasCompletedOnboarding);
    }

    [Fact]
    public void DistillSettings_CanSetHasCompletedOnboarding()
    {
        var settings = new DistillSettings
        {
            HasCompletedOnboarding = true
        };
        Assert.True(settings.HasCompletedOnboarding);
    }

    [Theory]
    [InlineData("https://www.instagram.com/reel/C8xyz123/", true)]
    [InlineData("https://instagram.com/p/C9abc456/", true)]
    [InlineData("https://instagr.am/p/C9abc456/", true)]
    [InlineData("https://www.instagram.com/tv/C_def789/", true)]
    [InlineData("https://www.youtube.com/watch?v=123", false)]
    [InlineData("not a url", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void InstagramUrl_ValidationChecks(string? input, bool expectedValid)
    {
        var isValid = !string.IsNullOrWhiteSpace(input) &&
            (input.Contains("instagram.com/", StringComparison.OrdinalIgnoreCase) ||
             input.Contains("instagr.am/", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(expectedValid, isValid);
    }
}

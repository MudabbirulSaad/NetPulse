using NetPulse.Core.Models;
using NetPulse.Core.Monitoring;
using NetPulse.Core.Validation;

namespace NetPulse.Core.Tests;

public sealed class TargetDefaultsTests
{
    [Fact]
    public void CreateReturnsThreeValidRemovableTargets()
    {
        var defaults = TargetDefaults.Create();

        Assert.Collection(
            defaults,
            openReels =>
            {
                Assert.Equal("OpenReels", openReels.Name);
                Assert.Equal(TargetType.Http, openReels.Type);
            },
            google =>
            {
                Assert.Equal("Google DNS", google.Name);
                Assert.Equal("8.8.8.8", google.DnsResolver);
            },
            cloudflare =>
            {
                Assert.Equal("Cloudflare DNS", cloudflare.Name);
                Assert.Equal("1.1.1.1", cloudflare.DnsResolver);
            });

        Assert.All(defaults, target =>
            Assert.True(TargetValidator.ValidateAndNormalize(target, 0).IsValid));
    }
}

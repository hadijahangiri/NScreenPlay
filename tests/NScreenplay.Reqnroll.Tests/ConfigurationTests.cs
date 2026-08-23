using NScreenplay.Reqnroll;

namespace NScreenplay.Reqnroll.Tests;

/// <summary>Tests for the configuration mechanism.</summary>
public class ConfigurationTests
{
    [Fact]
    public void DefaultOptions_AreSensible()
    {
        var options = new NScreenplayOptions();
        Assert.Equal("chromium", options.Browser);
        Assert.True(options.Headless);
        Assert.Equal(30_000, options.TimeoutMilliseconds);
    }

    [Fact]
    public void Configure_SetsGlobalOptions()
    {
        try
        {
            NScreenplayConfiguration.Configure(new NScreenplayOptions { Headless = false });
            Assert.False(NScreenplayConfiguration.Options.Headless);
        }
        finally
        {
            // restore defaults so other tests are unaffected
            NScreenplayConfiguration.Configure(new NScreenplayOptions());
        }
    }

    [Fact]
    public void Configure_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => NScreenplayConfiguration.Configure(null!));
    }
}

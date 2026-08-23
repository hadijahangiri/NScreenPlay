using NScreenplay.Core;

namespace NScreenplay.Core.Tests;

public class TargetTests
{
    [Fact]
    public void The_SetsName()
    {
        var target = Target.The("login button");
        Assert.Equal("login button", target.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void The_ThrowsForBlankName(string name)
    {
        Assert.Throws<ArgumentException>(() => Target.The(name));
    }

    [Fact]
    public void NewTarget_HasNoStrategies()
    {
        var target = Target.The("something");
        Assert.Empty(target.Strategies);
    }

    [Fact]
    public void ByCss_AddsStrategy()
    {
        var target = Target.The("btn").ByCss("#login-btn");
        Assert.Single(target.Strategies);
        Assert.Equal(LocatorStrategyKind.Css, target.Strategies[0].Kind);
        Assert.Equal("#login-btn", target.Strategies[0].Value);
    }

    [Fact]
    public void ByLabel_AddsLabelStrategy()
    {
        var target = Target.The("username").ByLabel("Username");
        Assert.Equal(LocatorStrategyKind.Label, target.Strategies[0].Kind);
        Assert.Equal("Username", target.Strategies[0].Value);
    }

    [Fact]
    public void ByRole_AddsRoleStrategyWithOptionalName()
    {
        var target = Target.The("btn").ByRole("button", "Sign in");
        var strategy = target.Strategies[0];
        Assert.Equal(LocatorStrategyKind.Role, strategy.Kind);
        Assert.Equal("button", strategy.Value);
        Assert.Equal("Sign in", strategy.Qualifier);
    }

    [Fact]
    public void ByRole_WithoutAccessibleName_HasNullQualifier()
    {
        var target = Target.The("nav").ByRole("navigation");
        Assert.Null(target.Strategies[0].Qualifier);
    }

    [Fact]
    public void ById_AddsIdStrategy()
    {
        var target = Target.The("field").ById("email");
        Assert.Equal(LocatorStrategyKind.Id, target.Strategies[0].Kind);
    }

    [Fact]
    public void ByTestId_AddsTestIdStrategy()
    {
        var target = Target.The("submit").ByTestId("submit-btn");
        Assert.Equal(LocatorStrategyKind.TestId, target.Strategies[0].Kind);
    }

    [Fact]
    public void ByText_AddsTextStrategy()
    {
        var target = Target.The("link").ByText("Click here");
        Assert.Equal(LocatorStrategyKind.Text, target.Strategies[0].Kind);
    }

    [Fact]
    public void MultipleStrategies_AreAllStored()
    {
        var target = Target.The("field")
            .ByLabel("Email")
            .ById("email-input")
            .ByCss(".email-field");

        Assert.Equal(3, target.Strategies.Count);
    }

    [Fact]
    public void FluentBuilder_IsImmutable_OriginalUnchanged()
    {
        var original = Target.The("something");
        var withCss = original.ByCss(".foo");
        Assert.Empty(original.Strategies);
        Assert.Single(withCss.Strategies);
    }

    [Fact]
    public void ToString_ReturnsName()
    {
        var target = Target.The("login button");
        Assert.Equal("login button", target.ToString());
    }
}

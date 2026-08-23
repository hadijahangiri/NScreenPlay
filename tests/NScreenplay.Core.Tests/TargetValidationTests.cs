using NScreenplay.Core;

namespace NScreenplay.Core.Tests;

public class TargetValidationTests
{
    private static readonly Target BaseTarget = Target.The("field");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ByCss_ThrowsForBlank(string selector) =>
        Assert.Throws<ArgumentException>(() => BaseTarget.ByCss(selector));

    [Fact]
    public void ByCss_ThrowsForNull() =>
        Assert.Throws<ArgumentNullException>(() => BaseTarget.ByCss(null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ByLabel_ThrowsForBlank(string label) =>
        Assert.Throws<ArgumentException>(() => BaseTarget.ByLabel(label));

    [Fact]
    public void ByLabel_ThrowsForNull() =>
        Assert.Throws<ArgumentNullException>(() => BaseTarget.ByLabel(null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ByRole_ThrowsForBlank(string role) =>
        Assert.Throws<ArgumentException>(() => BaseTarget.ByRole(role));

    [Fact]
    public void ByRole_ThrowsForNull() =>
        Assert.Throws<ArgumentNullException>(() => BaseTarget.ByRole(null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ById_ThrowsForBlank(string id) =>
        Assert.Throws<ArgumentException>(() => BaseTarget.ById(id));

    [Fact]
    public void ById_ThrowsForNull() =>
        Assert.Throws<ArgumentNullException>(() => BaseTarget.ById(null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ByTestId_ThrowsForBlank(string testId) =>
        Assert.Throws<ArgumentException>(() => BaseTarget.ByTestId(testId));

    [Fact]
    public void ByTestId_ThrowsForNull() =>
        Assert.Throws<ArgumentNullException>(() => BaseTarget.ByTestId(null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ByXPath_ThrowsForBlank(string xpath) =>
        Assert.Throws<ArgumentException>(() => BaseTarget.ByXPath(xpath));

    [Fact]
    public void ByXPath_ThrowsForNull() =>
        Assert.Throws<ArgumentNullException>(() => BaseTarget.ByXPath(null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ByText_ThrowsForBlank(string text) =>
        Assert.Throws<ArgumentException>(() => BaseTarget.ByText(text));

    [Fact]
    public void ByText_ThrowsForNull() =>
        Assert.Throws<ArgumentNullException>(() => BaseTarget.ByText(null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ByPlaceholder_ThrowsForBlank(string placeholder) =>
        Assert.Throws<ArgumentException>(() => BaseTarget.ByPlaceholder(placeholder));

    [Fact]
    public void ByPlaceholder_ThrowsForNull() =>
        Assert.Throws<ArgumentNullException>(() => BaseTarget.ByPlaceholder(null!));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ByAltText_ThrowsForBlank(string altText) =>
        Assert.Throws<ArgumentException>(() => BaseTarget.ByAltText(altText));

    [Fact]
    public void ByAltText_ThrowsForNull() =>
        Assert.Throws<ArgumentNullException>(() => BaseTarget.ByAltText(null!));
}

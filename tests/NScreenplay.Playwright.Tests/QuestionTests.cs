using Microsoft.Playwright;
using NScreenplay.Core;
using NSubstitute;

namespace NScreenplay.Playwright.Tests;

/// <summary>Tests for Playwright questions (Text, Visibility, CurrentUrl, PageTitle, InputValue).</summary>
public class QuestionTests
{
    private static (IPage page, ILocator locator, Actor actor) Setup()
    {
        var locator = Substitute.For<ILocator>();
        var page = Substitute.For<IPage>();
        page.Locator(Arg.Any<string>()).Returns(locator);
        page.GetByLabel(Arg.Any<string>(), Arg.Any<PageGetByLabelOptions?>()).Returns(locator);
        var actor = Actor.Named("Alice");
        actor.Can(BrowseTheWeb.Using(page));
        return (page, locator, actor);
    }

    // ── Text ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Text_Of_ThrowsForNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() => Text.Of(null!));
    }

    [Fact]
    public async Task Text_Of_ReturnsInnerText()
    {
        var target = Target.The("heading").ByCss("h1");
        var (_, locator, actor) = Setup();
        locator.InnerTextAsync(Arg.Any<LocatorInnerTextOptions?>()).Returns("Welcome");

        var result = await actor.AsksFor(Text.Of(target));

        Assert.Equal("Welcome", result);
    }

    // ── Visibility ────────────────────────────────────────────────────────────

    [Fact]
    public void Visibility_Of_ThrowsForNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() => Visibility.Of(null!));
    }

    [Fact]
    public async Task Visibility_Of_ReturnsTrueWhenVisible()
    {
        var target = Target.The("header").ByCss("header");
        var (_, locator, actor) = Setup();
        locator.IsVisibleAsync(Arg.Any<LocatorIsVisibleOptions?>()).Returns(true);

        var result = await actor.AsksFor(Visibility.Of(target));

        Assert.True(result);
    }

    [Fact]
    public async Task Visibility_Of_ReturnsFalseWhenNotVisible()
    {
        var target = Target.The("spinner").ByCss(".spinner");
        var (_, locator, actor) = Setup();
        locator.IsVisibleAsync(Arg.Any<LocatorIsVisibleOptions?>()).Returns(false);

        var result = await actor.AsksFor(Visibility.Of(target));

        Assert.False(result);
    }

    // ── CurrentUrl ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CurrentUrl_Value_ReturnsPageUrl()
    {
        var (page, _, actor) = Setup();
        page.Url.Returns("https://example.com/dashboard");

        var result = await actor.AsksFor(CurrentUrl.Value());

        Assert.Equal("https://example.com/dashboard", result);
    }

    [Fact]
    public void CurrentUrl_Value_ReturnsSameInstance()
    {
        Assert.Same(CurrentUrl.Value(), CurrentUrl.Value());
    }

    // ── PageTitle ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task PageTitle_Value_ReturnsTitle()
    {
        var (page, _, actor) = Setup();
        page.TitleAsync().Returns("Dashboard — MyApp");

        var result = await actor.AsksFor(PageTitle.Value());

        Assert.Equal("Dashboard — MyApp", result);
    }

    // ── InputValue ────────────────────────────────────────────────────────────

    [Fact]
    public void InputValue_Of_ThrowsForNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() => InputValue.Of(null!));
    }

    [Fact]
    public async Task InputValue_Of_ReturnsFieldValue()
    {
        var target = Target.The("email field").ByLabel("Email");
        var (_, locator, actor) = Setup();
        locator.InputValueAsync(Arg.Any<LocatorInputValueOptions?>()).Returns("alice@example.com");

        var result = await actor.AsksFor(InputValue.Of(target));

        Assert.Equal("alice@example.com", result);
    }
}

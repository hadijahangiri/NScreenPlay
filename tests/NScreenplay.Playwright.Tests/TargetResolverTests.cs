using Microsoft.Playwright;
using NScreenplay.Core;
using NSubstitute;

namespace NScreenplay.Playwright.Tests;

/// <summary>Tests for TargetResolver — validates strategy-to-Playwright-locator mapping.</summary>
public class TargetResolverTests
{
    private static (IPage page, ILocator locator, Actor actor) Setup()
    {
        var locator = Substitute.For<ILocator>();
        var page = Substitute.For<IPage>();
        var actor = Actor.Named("Alice");
        actor.Can(BrowseTheWeb.Using(page));
        return (page, locator, actor);
    }

    [Fact]
    public void Resolve_ThrowsForNullPage()
    {
        var target = Target.The("btn").ByCss("#btn");
        Assert.Throws<ArgumentNullException>(() => TargetResolver.Resolve(null!, target));
    }

    [Fact]
    public void Resolve_ThrowsForNullTarget()
    {
        var page = Substitute.For<IPage>();
        Assert.Throws<ArgumentNullException>(() => TargetResolver.Resolve(page, null!));
    }

    [Fact]
    public void Resolve_ThrowsWhenTargetHasNoStrategies()
    {
        var (page, _, _) = Setup();
        var target = Target.The("empty");
        Assert.Throws<ArgumentException>(() => TargetResolver.Resolve(page, target));
    }

    [Fact]
    public void Resolve_CSS_CallsPageLocatorWithSelector()
    {
        var (page, locator, _) = Setup();
        page.Locator(".btn").Returns(locator);
        var target = Target.The("button").ByCss(".btn");
        var result = TargetResolver.Resolve(page, target);
        Assert.Same(locator, result);
        page.Received(1).Locator(".btn");
    }

    [Fact]
    public void Resolve_Id_CallsPageLocatorWithHashId()
    {
        var (page, locator, _) = Setup();
        page.Locator("#submit").Returns(locator);
        var target = Target.The("submit").ById("submit");
        var result = TargetResolver.Resolve(page, target);
        Assert.Same(locator, result);
        page.Received(1).Locator("#submit");
    }

    [Fact]
    public void Resolve_Label_CallsGetByLabel()
    {
        var (page, locator, _) = Setup();
        page.GetByLabel("Email", Arg.Any<PageGetByLabelOptions?>()).Returns(locator);
        var target = Target.The("email").ByLabel("Email");
        var result = TargetResolver.Resolve(page, target);
        Assert.Same(locator, result);
    }

    [Fact]
    public void Resolve_TestId_CallsGetByTestId()
    {
        var (page, locator, _) = Setup();
        page.GetByTestId(Arg.Is<string>(s => s == "submit-btn")).Returns(locator);
        var target = Target.The("submit").ByTestId("submit-btn");
        var result = TargetResolver.Resolve(page, target);
        Assert.Same(locator, result);
    }

    [Fact]
    public void Resolve_Text_CallsGetByText()
    {
        var (page, locator, _) = Setup();
        page.GetByText("Sign in", Arg.Any<PageGetByTextOptions?>()).Returns(locator);
        var target = Target.The("link").ByText("Sign in");
        var result = TargetResolver.Resolve(page, target);
        Assert.Same(locator, result);
    }

    [Fact]
    public void Resolve_Role_WithAccessibleName_CallsGetByRole()
    {
        var (page, locator, _) = Setup();
        page.GetByRole(AriaRole.Button, Arg.Any<PageGetByRoleOptions?>()).Returns(locator);
        var target = Target.The("login btn").ByRole("button", "Sign in");
        var result = TargetResolver.Resolve(page, target);
        Assert.Same(locator, result);
    }

    [Fact]
    public void Resolve_Role_WithInvalidRole_ThrowsArgumentException()
    {
        var (page, _, _) = Setup();
        var target = Target.The("btn").ByRole("notavalidrole");
        Assert.Throws<ArgumentException>(() => TargetResolver.Resolve(page, target));
    }

    [Fact]
    public void Resolve_UsesFirstStrategy_WhenMultipleExist()
    {
        var (page, cssLocator, _) = Setup();
        page.Locator(".btn").Returns(cssLocator);
        // CSS is first, Label is second — CSS must win
        var target = Target.The("button").ByCss(".btn").ByLabel("Submit");
        var result = TargetResolver.Resolve(page, target);
        Assert.Same(cssLocator, result);
    }
}

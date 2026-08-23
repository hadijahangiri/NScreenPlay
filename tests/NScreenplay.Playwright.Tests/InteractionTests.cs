using Microsoft.Playwright;
using NScreenplay.Core;
using NSubstitute;

namespace NScreenplay.Playwright.Tests;

/// <summary>Tests for Playwright interactions (Click, Enter, Navigate, Select, Check).</summary>
public class InteractionTests
{
    private static (IPage page, ILocator locator, Actor actor) Setup(Target? target = null)
    {
        var locator = Substitute.For<ILocator>();
        var page = Substitute.For<IPage>();

        // Setup all common locator resolutions
        page.Locator(Arg.Any<string>()).Returns(locator);
        page.GetByLabel(Arg.Any<string>(), Arg.Any<PageGetByLabelOptions?>()).Returns(locator);
        page.GetByRole(Arg.Any<AriaRole>(), Arg.Any<PageGetByRoleOptions?>()).Returns(locator);
        page.GetByTestId(Arg.Any<string>()).Returns(locator);
        page.GetByText(Arg.Any<string>(), Arg.Any<PageGetByTextOptions?>()).Returns(locator);

        var actor = Actor.Named("Alice");
        actor.Can(BrowseTheWeb.Using(page));
        return (page, locator, actor);
    }

    // ── Click ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Click_On_ThrowsForNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() => Click.On(null!));
    }

    [Fact]
    public async Task Click_On_CallsLocatorClickAsync()
    {
        var target = Target.The("btn").ByCss(".btn");
        var (_, locator, actor) = Setup(target);
        locator.ClickAsync(Arg.Any<LocatorClickOptions?>()).Returns(Task.CompletedTask);

        await actor.AttemptsTo(Click.On(target));

        await locator.Received(1).ClickAsync(Arg.Any<LocatorClickOptions?>());
    }

    [Fact]
    public void Click_ToString_ContainsTargetName()
    {
        var target = Target.The("login button").ByCss(".login");
        Assert.Contains("login button", Click.On(target).ToString());
    }

    // ── Enter ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Enter_TheValue_ThrowsForNullValue()
    {
        Assert.Throws<ArgumentNullException>(() => Enter.TheValue(null!));
    }

    [Fact]
    public async Task Enter_TheValue_Into_FillsLocator()
    {
        var target = Target.The("email").ByLabel("Email");
        var (_, locator, actor) = Setup(target);
        locator.FillAsync(Arg.Any<string>(), Arg.Any<LocatorFillOptions?>()).Returns(Task.CompletedTask);

        await actor.AttemptsTo(Enter.TheValue("alice@example.com").Into(target));

        await locator.Received(1).FillAsync("alice@example.com", Arg.Any<LocatorFillOptions?>());
    }

    [Fact]
    public async Task Enter_WithoutInto_ThrowsInvalidOperationException()
    {
        var (_, _, actor) = Setup();
        var interaction = Enter.TheValue("test"); // no .Into()
        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.AttemptsTo(interaction));
    }

    [Fact]
    public void Enter_ToString_ContainsValueAndTarget()
    {
        var target = Target.The("email field").ByLabel("Email");
        var interaction = Enter.TheValue("test@example.com").Into(target);
        var str = interaction.ToString();
        Assert.Contains("test@example.com", str);
        Assert.Contains("email field", str);
    }

    // ── Navigate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Navigate_To_ThrowsForBlankUrl()
    {
        Assert.Throws<ArgumentException>(() => Navigate.To(""));
    }

    [Fact]
    public async Task Navigate_To_CallsPageGotoAsync()
    {
        var (page, _, actor) = Setup();
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions?>())
            .Returns(Task.FromResult<IResponse?>(null));

        await actor.AttemptsTo(Navigate.To("https://example.com"));

        await page.Received(1).GotoAsync("https://example.com", Arg.Any<PageGotoOptions?>());
    }

    [Fact]
    public void Navigate_ToString_ContainsUrl()
    {
        Assert.Contains("https://example.com", Navigate.To("https://example.com").ToString());
    }

    // ── Select ────────────────────────────────────────────────────────────────

    [Fact]
    public void Select_TheOption_ThrowsForBlank()
    {
        Assert.Throws<ArgumentException>(() => Select.TheOption(""));
    }

    [Fact]
    public async Task Select_TheOption_From_SelectsOption()
    {
        var target = Target.The("country").ByCss("select");
        var (_, locator, actor) = Setup(target);
        locator.SelectOptionAsync(Arg.Any<string>(), Arg.Any<LocatorSelectOptionOptions?>())
               .Returns(new[] { "Canada" });

        await actor.AttemptsTo(Select.TheOption("Canada").From(target));

        await locator.Received(1).SelectOptionAsync("Canada", Arg.Any<LocatorSelectOptionOptions?>());
    }

    [Fact]
    public async Task Select_WithoutFrom_ThrowsInvalidOperationException()
    {
        var (_, _, actor) = Setup();
        var interaction = Select.TheOption("Canada"); // no .From()
        await Assert.ThrowsAsync<InvalidOperationException>(() => actor.AttemptsTo(interaction));
    }

    // ── Check ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Check_The_ThrowsForNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() => Check.The(null!));
    }

    [Fact]
    public async Task Check_The_CallsCheckAsync()
    {
        var target = Target.The("terms").ByCss("#terms");
        var (_, locator, actor) = Setup(target);
        locator.CheckAsync(Arg.Any<LocatorCheckOptions?>()).Returns(Task.CompletedTask);

        await actor.AttemptsTo(Check.The(target));

        await locator.Received(1).CheckAsync(Arg.Any<LocatorCheckOptions?>());
    }

    [Fact]
    public async Task Check_Not_CallsUncheckAsync()
    {
        var target = Target.The("newsletter").ByCss("#newsletter");
        var (_, locator, actor) = Setup(target);
        locator.UncheckAsync(Arg.Any<LocatorUncheckOptions?>()).Returns(Task.CompletedTask);

        await actor.AttemptsTo(Check.Not(target));

        await locator.Received(1).UncheckAsync(Arg.Any<LocatorUncheckOptions?>());
    }
}

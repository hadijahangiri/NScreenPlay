# Playwright Integration

`NScreenplay.Playwright` connects Screenplay concepts to Playwright.

## Verified Types

- `BrowseTheWeb.Using(IPage)`
- `Navigate.To(string)`
- `Click.On(Target)`
- `Enter.TheValue(string).Into(Target)`
- `Select.TheOption(string).From(Target)`
- `Check.The(Target)`
- `Check.Not(Target)`
- `Text.Of(Target)`
- `Visibility.Of(Target)`
- `CurrentUrl.Value()`
- `PageTitle.Value()`
- `InputValue.Of(Target)`

## Example

```csharp
using Microsoft.Playwright;
using NScreenplay.Core;
using NScreenplay.Playwright;

await using IPlaywright playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
await using var page = await browser.NewPageAsync();

var actor = Actor.Named("Alice");
actor.Can(BrowseTheWeb.Using(page));

await actor.AttemptsTo(Navigate.To("https://example.com/login"));
await actor.AttemptsTo(Click.On(Target.The("login button").ByTestId("login-button")));
```

## Notes

`BrowseTheWeb` owns the page and is disposed when the actor is disposed. The browser and browser context are managed outside the ability.

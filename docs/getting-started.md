# Getting Started

NScreenplay v0.1.0 is an AI-native Screenplay Test Automation Framework for .NET.

## Install

```bash
dotnet add package NScreenplay.Core --version 0.1.0
dotnet add package NScreenplay.Playwright --version 0.1.0
dotnet add package NScreenplay.Reqnroll --version 0.1.0
```

## Minimal Screenplay Flow

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
```

## Next Steps

- Read [screenplay-pattern.md](screenplay-pattern.md)
- Read [playwright.md](playwright.md)
- Read [reqnroll.md](reqnroll.md)
- Read [mcp.md](mcp.md)

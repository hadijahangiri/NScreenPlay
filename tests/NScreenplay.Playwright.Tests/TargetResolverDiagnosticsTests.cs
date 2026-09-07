using Microsoft.Playwright;
using NScreenplay.Core;
using NSubstitute;
using System.Reflection;

namespace NScreenplay.Playwright.Tests;

/// <summary>
/// Verifies that when the resolver encounters an unsupported first strategy it
/// emits a deterministic diagnostic containing the target name and declared kinds.
///
/// Note: constructing a truly unsupported strategy requires creating a LocatorStrategy
/// with an out-of-range enum value. The production API does not expose a public
/// constructor for LocatorStrategy, so this test uses reflection to mutate the
/// target's private strategies list. This keeps the test focused on diagnostics
/// while remaining as close as possible to the public surface (we still call
/// TargetResolver.Resolve which is the public behavior under test).
/// </summary>
public class TargetResolverDiagnosticsTests
{
    [Fact]
    public void Resolve_UnsupportedStrategy_IncludesTargetNameAndDeclaredKinds()
    {
        var page = Substitute.For<IPage>();

        // Create a target via public API and then inject a synthetic unsupported strategy
        // as the first strategy. We add a CSS strategy afterwards so the declared kinds
        // list contains both entries and we can assert determinism/order.
        var target = Target.The("mimik");

        var strategiesField = typeof(Target).GetField("_strategies", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var list = (System.Collections.IList?)strategiesField.GetValue(target) ?? throw new InvalidOperationException("Unable to access target strategies");

        // Construct internal LocatorStrategy instances via non-public constructor.
        var ctor = typeof(LocatorStrategy).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();

        var unknownKind = (LocatorStrategyKind)999; // sentinel out-of-range kind
        var fake = ctor.Invoke(new object?[] { unknownKind, "ignored", (string?)null });
        list.Add(fake);

        var css = ctor.Invoke(new object?[] { LocatorStrategyKind.Css, ".btn", (string?)null });
        list.Add(css);

        var ex = Assert.Throws<NotSupportedException>(() => TargetResolver.Resolve(page, target));

        // Diagnostics must include target name and the declared kinds (in order).
        Assert.Contains("mimik", ex.Message);
        Assert.Contains("Declared kinds:", ex.Message);
        Assert.Contains(LocatorStrategyKind.Css.ToString(), ex.Message);
        Assert.Contains(((LocatorStrategyKind)999).ToString(), ex.Message);
    }
}

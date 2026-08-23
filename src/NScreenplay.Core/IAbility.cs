namespace NScreenplay.Core;

/// <summary>
/// Marker interface for capabilities that can be granted to an <see cref="Actor"/>.
/// </summary>
/// <remarks>
/// Concrete abilities (e.g., BrowseTheWeb, CallAnApi) live in integration packages,
/// never in Core. Core only defines the contract.
/// </remarks>
public interface IAbility
{
}

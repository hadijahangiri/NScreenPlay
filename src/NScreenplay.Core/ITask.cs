namespace NScreenplay.Core;

/// <summary>
/// Marks a performable as a business-level operation composed of one or more interactions.
/// </summary>
/// <remarks>
/// This marker interface distinguishes tasks from interactions at a conceptual level.
/// Examples: Login.WithCredentials(...), Checkout.Product(...)
/// </remarks>
public interface ITask : IPerformable
{
}

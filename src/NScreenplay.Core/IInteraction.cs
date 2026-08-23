namespace NScreenplay.Core;

/// <summary>
/// Marks a performable as an atomic, single-purpose action.
/// </summary>
/// <remarks>
/// Interactions must not contain business logic. They perform one action.
/// Examples: Click.On(target), Enter.Text(value).Into(target), Navigate.To(url)
/// </remarks>
public interface IInteraction : IPerformable
{
}

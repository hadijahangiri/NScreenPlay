namespace NScreenplay.Core;

/// <summary>
/// Thrown when an <see cref="Actor"/> attempts to use an <see cref="IAbility"/> it does not have.
/// </summary>
public sealed class MissingAbilityException : ScreenplayException
{
    /// <summary>The actor that was missing the ability.</summary>
    public string ActorName { get; }

    /// <summary>The ability type that was not found.</summary>
    public Type AbilityType { get; }

    internal MissingAbilityException(string actorName, Type abilityType)
        : base($"Actor '{actorName}' does not have the ability '{abilityType.Name}'. " +
               $"Grant it with: actor.Can(/* your {abilityType.Name} instance */).")
    {
        ActorName = actorName;
        AbilityType = abilityType;
    }
}

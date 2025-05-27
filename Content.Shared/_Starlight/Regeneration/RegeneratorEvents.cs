

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Regeneration;

[ByRefEvent]
public readonly record struct TargetResurrectedEvent(EntityUid rezTarget);

public abstract class BeforeResurrectionEvent : CancellableEntityEventArgs
{
        public EntityUid ResurrectionTarget;

    public BeforeResurrectionEvent(EntityUid rezTarget)
    {
        ResurrectionTarget = rezTarget;
    }
}

/// <summary>
///     This event is raised on the regenerator before it resurrects.
///     The event is triggered on the target itself and all its clothing.
/// </summary>
public sealed class TargetBeforeResurrectionEvent : BeforeResurrectionEvent
{
    public TargetBeforeResurrectionEvent(EntityUid rezTarget) : base(rezTarget) { }
}


[Serializable, NetSerializable]
public sealed partial class ResurrectionDoAfterEvent : SimpleDoAfterEvent
{
}

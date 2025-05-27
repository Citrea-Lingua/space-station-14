using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Starlight.Regeneration;

/// <summary>
/// Handles entities that regenerate health passively.
/// </summary>
[RegisterComponent, Access(typeof(RegeneratorSystem))]
public sealed partial class RegeneratorComponent : Component
{
    /// <summary>
    /// How much the Regenerator heals each tick
    /// </summary>
    [DataField]
    public DamageSpecifier? DamageRecovery = null;

    /// <summary>
    /// The next time that this body will regenerate.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The interval between updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// If the regenerator can resurrect itself
    /// </summary>
    [DataField]
    public bool SelfResurrect = false;

    /// <summary>
    /// How long the regenerator will be interruptible while resurrecting.
    /// </summary>
    [DataField]
    public int ResurrectionDelayCycles = 10;

        /// <summary>
        ///     How many cycles in a row has the mob been dead?
        /// </summary>
        [ViewVariables]
        public int ResurrectionCycles = 0;

    /// <summary>
    /// How long the regenerator will be interruptible while resurrecting.
    /// </summary>
    [DataField]
    public TimeSpan ResurrectionDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long the regenerator will be electrocuted after resurrecting.
    /// </summary>
    [DataField]
    public TimeSpan WritheDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// The sound when the regenerator resurrects.
    /// </summary>
    [DataField]
    public SoundSpecifier? ResurrectionSound = null;

    [DataField]
    public SoundSpecifier? ChargeSound = null;

    [DataField]
    public SoundSpecifier? FailureSound = null;

    [DataField]
    public SoundSpecifier? SuccessSound = null;
    /// <summary>
    /// How much damage is healed from getting zapped.
    /// </summary>
    [DataField]
    public DamageSpecifier ResurrectionHeal = default!;


}
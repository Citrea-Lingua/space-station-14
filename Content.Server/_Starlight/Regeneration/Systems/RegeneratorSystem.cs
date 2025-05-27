using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;
using Content.Server.Body.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared.DoAfter;
using Content.Server.DoAfter;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;
using Content.Server.Electrocution;
using Content.Shared.Mobs;
using Content.Server.Chat.Systems;
using Content.Shared.Mind;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared._Starlight.Regeneration;
using Content.Shared.Traits.Assorted;
using Content.Server.Atmos.Rotting;

namespace Content.Server._Starlight.Regeneration;

/// <summary>
/// This handles interactions and logic relating to <see cref="RegeneratorComponent"/>
/// </summary>
public sealed class RegeneratorSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly ChatSystem _chatManager = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    private ISawmill _sawmill = default!;

	public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RegeneratorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RegeneratorComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<RegeneratorComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<RegeneratorComponent, ResurrectionDoAfterEvent>(OnDoAfter);
    }

    private void OnMapInit(Entity<RegeneratorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _gameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnUnpaused(Entity<RegeneratorComponent> ent, ref EntityUnpausedEvent args)
    {
        ent.Comp.NextUpdate += args.PausedTime;
    }

    private void OnDoAfter(EntityUid uid, RegeneratorComponent component, ResurrectionDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        args.Handled = true;
        Resurrect(target, component);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RegeneratorComponent>();
        while (query.MoveNext(out var uid, out var regenerator))
        {
            if (_gameTiming.CurTime < regenerator.NextUpdate)
                continue;

            regenerator.NextUpdate += regenerator.UpdateInterval;

            if (_mobState.IsDead(uid))
            {
                if(!regenerator.SelfResurrect)
                    continue;
                if(regenerator.ResurrectionCycles < regenerator.ResurrectionDelayCycles)
                {
                    regenerator.ResurrectionCycles++;
                }
                else
                {
                    if(_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, regenerator.ResurrectionDelay, new ResurrectionDoAfterEvent(),
                        uid, uid, uid)
                        {
                            NeedHand = false,
                            BreakOnMove = false,
                            RequireCanInteract = false,
                            BreakOnDamage = true,
                            CancelDuplicate = false
                        }))
                    {   
                        if(regenerator.ChargeSound != null)
                            _audio.PlayPvs(regenerator.ChargeSound, uid);
                        regenerator.ResurrectionCycles = 0;
                    }
                }
            }

            if(regenerator.DamageRecovery != null)
                _damageable.TryChangeDamage(uid, regenerator.DamageRecovery);
        }
    }

    private void OnApplyMetabolicMultiplier(
        Entity<RegeneratorComponent> ent,
        ref ApplyMetabolicMultiplierEvent args)
    {
        // Taken directly from RespiratorComponent, ideally refactor with it
        // TODO REFACTOR THIS
        // This will slowly drift over time due to floating point errors.
        // Instead, raise an event with the base rates and allow modifiers to get applied to it.
        if (args.Apply)
        {
            ent.Comp.UpdateInterval *= args.Multiplier;
            return;
        }

        // This way we don't have to worry about it breaking if the stasis bed component is destroyed
        ent.Comp.UpdateInterval /= args.Multiplier;
    }
    

    /// <summary>
    ///     Tries to defibrillate the target with the given defibrillator.
    /// </summary>
    private void Resurrect(EntityUid uid, RegeneratorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var targetEvent = new TargetBeforeResurrectionEvent(uid);
        RaiseLocalEvent(uid, targetEvent);

        uid = targetEvent.ResurrectionTarget;

        if (targetEvent.Cancelled || !_mobState.IsDead(uid))
            return;

        if (!TryComp<MobStateComponent>(uid, out var mob))
            return;
        if(component.ResurrectionSound != null)
            _audio.PlayPvs(component.ResurrectionSound, uid);
        _electrocution.TryDoElectrocution(uid, null, 0, component.WritheDuration, true, ignoreInsulation: true);

        ICommonSession? session = null;
        
        var dead = true;
        if(!(_rotting.IsRotten(uid)||TryComp<UnrevivableComponent>(uid, out var unrevivable)))
        {  
            if (_mobState.IsDead(uid, mob))
                _damageable.TryChangeDamage(uid, component.ResurrectionHeal, true);
            if (_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold) &&
                TryComp<DamageableComponent>(uid, out var damageableComponent) &&
                damageableComponent.TotalDamage < threshold)
            {
                _mobState.ChangeMobState(uid, MobState.Critical, mob);
                dead = false;
            }

            if (_mind.TryGetMind(uid, out _, out var mind) &&
                mind.Session is { } playerSession)
            {
                session = playerSession;
                // notify them they're being revived.
                if (mind.CurrentEntity != uid)
                {
                    _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind), session);
                }
            }
            else
            {
                _chatManager.TrySendInGameICMessage(uid, Loc.GetString("regenerator-no-mind"),
                    InGameICChatType.Speak, true);
            }
        }

        bool failure = dead || session == null;
        if(failure && component.FailureSound != null)
            _audio.PlayPvs(component.FailureSound, uid);
        else if(component.SuccessSound != null)
            _audio.PlayPvs(component.SuccessSound, uid);

        var ev = new TargetResurrectedEvent(uid);
        RaiseLocalEvent(uid, ref ev);
    }
}
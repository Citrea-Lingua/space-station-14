using Content.Shared._Starlight.Xeno;
using Content.Shared._Starlight.Xeno.Components;
using Content.Shared.Actions;

namespace Content.Shared._Starlight.Xemo;

public abstract partial class SharedXenoDemocracySystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoDemocracyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<XenoDemocracyComponent, XenoDemocracyVoteActionEvent>(OnVote);
    }

    private void OnShutdown(EntityUid uid, XenoDemocracyComponent component, ComponentShutdown args)
    {
        if (Deleted(uid) || component.ActionEntity is null)
            return;

        _action.RemoveAction((uid, null), component.ActionEntity);
    }

    private void OnVote(EntityUid uid, XenoDemocracyComponent component, XenoDemocracyVoteActionEvent args)
    {
        //No voting for Ian please, much as he deserves it <3
        if(!HasComp<XenoDemocracyComponent>(args.Target))
            return;

        component.CurrentVoteHolder = args.Target;
    }
}

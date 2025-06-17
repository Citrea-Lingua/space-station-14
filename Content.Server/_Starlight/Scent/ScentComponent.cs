namespace Content.Server._Starlight.Scent
{
    [RegisterComponent]
    public sealed partial class ScentComponent : Component
    {
        [DataField("scents")]
        public HashSet<string> Scents = new();
	}
}
public partial class AmbiNode
{
    protected override int MaximumCapacity => 2000;

    [AmbiRegistrationHook]
    partial void Register(AmbiNode instance);

    [AmbiDeregistrationHook]
    partial void Deregister(AmbiNode instance);
}

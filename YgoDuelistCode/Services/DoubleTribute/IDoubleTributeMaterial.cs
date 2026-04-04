namespace YgoDuelist.YgoDuelistCode.Services;

/// <summary>Field monster that can count as two tributes for a matching normal summon.</summary>
public interface IDoubleTributeMaterial
{
    DoubleTributeSummonTargetSpec DoubleTributeTargetSpec { get; }
}

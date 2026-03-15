using BaseLib.Abstracts;
using Godot;

namespace YgoDuelist.YgoDuelistCode.Character;

public class YgoDuelistPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => "ironclad";
    public override Color LabOutlineColor => YgoDuelist.Color;
}
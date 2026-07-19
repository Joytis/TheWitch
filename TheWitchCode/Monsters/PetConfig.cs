using Godot;

namespace TheWitch.TheWitchCode.Monsters;

/// <summary>
/// Data resource for a familiar pet's visual configuration, authored as a .tres under
/// TheWitch/ and loaded at runtime via GD.Load&lt;PetConfig&gt;(). Stub — properties TBD.
/// </summary>
[GlobalClass]
public partial class PetConfig : Resource
{
	[Export] public Texture2D? Texture { get; set; }
	[Export] public Vector2 Offset { get; set; }

	/// <summary>Spacing between multiple pets of this species (the pyramid pattern in WitchPetClusterPatch).</summary>
	[Export] public float SpeciesDistance { get; set; } = 55f;

	/// <summary>Uniform scale applied to the scene's Visuals sprite (NOT VisualsRoot, which animations drive).</summary>
	[Export] public float VisualsScale { get; set; } = 1f;

	/// <summary>Whether the ground shadow is shown (off for flyers like Owl/Crow).</summary>
	[Export] public bool HasShadow { get; set; } = true;
}

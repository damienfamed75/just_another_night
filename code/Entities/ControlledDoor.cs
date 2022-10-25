using Sandbox;
using SandboxEditor;

[HammerEntity]
[Library("ent_controlled_door")]
public partial class ControlledDoor : DoorEntity
{
	public ControlledDoor() : base()
	{
	}

	public override bool OnUse( Entity user )
	{
		if (Locked) {

		} else {
			string soundPath = "quick-door-open";
			// Freezer door has a specific sound to play.
			if (Tags.Has("freezer_door")) {
				soundPath = "slow-door-open";
			}

			Sound.FromEntity( soundPath, user );
		}

		return base.OnUse( user );
	}
}
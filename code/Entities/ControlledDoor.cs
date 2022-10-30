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
			Sound.FromEntity( "door-locked", user );
		} else {
			string soundPath = "quick-door-open";
			// Freezer door has a specific sound to play.
			if (Tags.Has("freezer_door")) {
				soundPath = "slow-door-open";
			}

			// var dir = (Position - user.Position).Normal;
			// Sound.FromWorld( soundPath,
			// 	user.EyePosition + dir * 450f
			// );
			Sound.FromEntity( soundPath, user );
		}

		return base.OnUse( user );
	}
}
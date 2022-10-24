using System.Linq;
using Sandbox;
using SandboxEditor;

[HammerEntity]
[Model(Model = "models/dumpster.vmdl")]
public partial class Dumpster : ModelEntity, IUse
{
	public ActionButton Button;

	public RealTimeSince TimeSinceDump { get; set; }

	public Dumpster()
	{

		EnableAllCollisions = true;

		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Tags.Add( "dumpster" );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		Button = new ActionButton();
		Button.Position = Position + Rotation.Up * 55f + Rotation.Forward * 46f;
		Button.Prop = this;
	}

	public bool IsUsable( Entity user )
	{
		var player = user as JustAnotherPlayer;
		if (player.ActiveChild == null)
			return false;

		return player.ActiveChild is TrashBag;
	}

	public bool OnUse( Entity user )
	{
		var player = user as JustAnotherPlayer;
		// Double check the player has something in their hands.
		if (player.ActiveChild == null)
			return false;

		// Ensure that the player is holding a trash bag.
		if (player.ActiveChild is not TrashBag)
			return false;

		// If this gets caught then the OnUse method is being called too many times.
		if (TimeSinceDump < 5)
			return false;

		TimeSinceDump = 0;

		// Check to see if this is the last trash bag left on the map.
		var allTrashBags = All.OfType<TrashBag>().ToArray();
		if (allTrashBags.Length == 1) {
			// Move the player onto their next task.
			ConsoleSystem.Run( "increment_task" );
		}

		// Unequip the player's current held item.
		ConsoleSystem.Run( "unequip_ent" );

		return false;
	}
}
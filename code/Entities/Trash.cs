using System.Linq;
using Sandbox;
using Sandbox.Component;
using SandboxEditor;

[HammerEntity]
[Model(Model = "models/rust_props/small_junk/ground_junk_a.vmdl")]
// [EditorModel("models/rust_props/small_junk/ground_junk_a.vmdl")]
public partial class Trash : ModelEntity, IUse
{
	public ActionButton Button;

	public Trash()
	{
		SetupPhysicsFromAABB(
			PhysicsMotionType.Keyframed, CollisionBounds.Mins, CollisionBounds.Maxs
		);

		Tags.Add( "trash" );
	}

	public bool IsUsable( Entity user ) {
		var player = user as JustAnotherPlayer;
		return player.State == JustAnotherPlayer.PlayerStates.PickupTrash;
	}

	public bool OnUse( Entity user )
	{
		Sound.FromEntity( "grab-item", user );

		var allTrash = All.OfType<Trash>().ToArray();
		// If this is true then this is the last piece of trash left on the map.
		if (allTrash.Length == 1) {
			// Move the player onto their next task.
			ConsoleSystem.Run( "increment_task" );
		}

		Button.Delete();
		ConsoleSystem.Run( "delete_ent", Name );

		return false;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		var glow = Components.GetOrCreate<Glow>();
		glow.ObscuredColor = Color.Transparent;
		glow.Color = Color.White;
		glow.Width = 0f;

		Button = new ActionButton();
		Button.Position = Position + Rotation.Up * 15f;
		Button.Prop = this;
	}
}
using System.Linq;
using Sandbox;
using SandboxEditor;

[HammerEntity]
[OrthoBoundsHelper("range", "width", "height")]
[EditorSprite("materials/editor/color_correction.vmdl")]
public partial class Spill : ModelEntity, IUse
{
	[Property]
	[Net]
	public bool IsOnWall { get; set; } = false;

	Particles SpillParticle;

	public float SpillAmount { get; set; } = 20f;

	public ActionButton Button;

	[Property]
	public float Range { get; set; } = 25f;
	[Property]
	public float Width { get; set; } = 25f;
	[Property]
	public float Height { get; set; } = 25f;

	public Spill()
	{
		SetupPhysicsFromCylinder( PhysicsMotionType.Keyframed, new Capsule{
			CenterA = Position + Vector3.Up * 1.5f,
			CenterB = Position + Vector3.Down * 1.5f,
			Radius = SpillAmount,
		} );

		Tags.Add( "spill" );
	}

	private string GetPath()
	{
		var path = "particles/spill.vpcf";
		if (IsOnWall) {
			path = "particles/wall_spill.vpcf";
		}

		return path;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		var path = GetPath();

		SpillParticle = Particles.Create( path, Position );
		SpillParticle.SetPosition( 1, new Vector3( SpillAmount, 0, 0 ) );
		// SpillParticle.SetOrientation( 0, Rotation );
		SpillParticle.SetOrientation( 2, Rotation );

		Button = new ActionButton();
		Button.Position = Position + Rotation.Backward * 15f;
		Button.Prop = this;
	}

	public bool OnUse( Entity user )
	{
		SpillAmount = (SpillAmount - 2 * Time.Delta).Clamp( 0.0f, 100f );

		SpillParticle.Destroy( true );
		SpillParticle = Particles.Create( GetPath(), Position );
		SpillParticle.SetPosition( 1, new Vector3( SpillAmount, 0, 0 ) );
		SpillParticle.SetOrientation( 2, Rotation );

		if (SpillAmount < 1f) {
			SpillParticle.Destroy( true );
			Button.Delete();
			// Tell the server to delete this stain.
			ConsoleSystem.Run( "delete_ent", Name );

			var allSpills = All.OfType<Spill>().ToArray();

			// If this is true then this is the last spill left on the map.
			if (allSpills.Length == 1) {
				// Move the player onto their next task.
				ConsoleSystem.Run( "increment_task" );
			}
		}

		return true;
	}

	public bool IsUsable( Entity user ) {
		var player = user as JustAnotherPlayer;
		return player.State == JustAnotherPlayer.PlayerStates.MopFloors;
	}
}
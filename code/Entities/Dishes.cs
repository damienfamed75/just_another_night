using Sandbox;
using Editor;
using JustAnotherNight.Player;
using JustAnotherNight.UI;

namespace JustAnotherNight.Entities;

[HammerEntity]
public partial class Dishes : ModelEntity, IUse, IEnablerDisabler
{
	Particles UnwashedDishesParticle;

	public float Progress { get; set; } = 0f;

	public ActionButton Button;

	public bool Using { get; set; } = false;

	Sound WashingSound { get; set; }

	TimeSince TimeSinceUsed { get; set; }

	public Dishes()
	{
		Tags.Add( "dishes" );

		// SetupPhysicsFromAABB( PhysicsMotionType.Keyframed,
		// 	CollisionBounds.Mins, CollisionBounds.Maxs );
		BBox box = BBox.FromPositionAndSize( Position, 20f );

		// SetupPhysicsFromAABB( PhysicsMotionType.Keyframed,
		// 	CollisionBounds.Mins, CollisionBounds.Maxs );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed,
			box.Mins, box.Maxs );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		UnwashedDishesParticle = Particles.Create( "particles/dishes.vpcf", Position );

		Button = new ActionButton();
		Button.Position = Position + Rotation.Up * 10f;
		Button.Prop = this;
	}

	public bool OnUse( Entity user )
	{
		if (Game.IsServer)
			return false;

		if ((GameManager.Current as JustAnotherGame).WaitingCustomer) {
			SelfSpeak.Current.SayCustomerWaiting();
			return false;
		}

		// Add to the progress
		Progress += (12 * Time.Delta).Clamp( 0.0f, 100f );
		Particles.Create( "particles/soap.vpcf", Position );

		if (Progress > 99f) {
			UnwashedDishesParticle.Destroy();
			WashingSound.Stop();
			Button.Delete();
			// Tell the server to delete these dishes.
			ConsoleSystem.Run( "delete_ent", Name );

			ConsoleSystem.Run( "increment_task" );
		}

		return false;
	}

	public bool IsUsable( Entity user )
	{
		var player = user as JustAnotherPlayer;
		return player.State == JustAnotherPlayer.PlayerStates.WashDishes;
	}

	public void Enable()
	{
		if (!Using && TimeSinceUsed > 0.5f) {
			WashingSound = Sound.FromEntity( "sink", this );
			TimeSinceUsed = 0;
			Using = true;
		}
	}

	public void Disable()
	{
		if (Using && TimeSinceUsed > 0.5) {
			WashingSound.Stop();
			Using = false;
		}
	}
}

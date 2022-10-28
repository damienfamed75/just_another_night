using System;
using Sandbox;
using Sandbox.UI;
using SandboxEditor;

[HammerEntity]
[Model(Model = "models/soda_machine.vmdl")]
public partial class IceCreamMachine : ModelEntity, IUse, IEnablerDisabler
{
	public ActionButton Button;

	public float Progress { get; set; } = 0f;

	public bool Finished { get; set; } = false;

	public bool Using { get; set; } = false;

	public Sound WrenchingSound { get; set; }

	public TimeSince TimeSinceUsed { get; set; }

	public IceCreamMachine()
	{
		EnableAllCollisions = true;

		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Tags.Add( "icecream_machine" );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		Button = new ActionButton();
		Button.Position = Position + Rotation.Up * 55f + Rotation.Left * 18f;
		Button.Prop = this;
	}

	public bool OnUse( Entity user )
	{
		if (IsServer)
			return false;

		if ((Game.Current as JustAnotherGame).WaitingCustomer) {
			SelfSpeak.Current.SayCustomerWaiting();
			return false;
		}

		Progress += (10 * Time.Delta).Clamp( 0.0f, 100.0f );

		if (Progress > 99f && !Finished) {
			Finished = true;
			Button.Delete();
			WrenchingSound.Stop();
			ConsoleSystem.Run( "increment_task" );
		}

		return false;
	}

	public bool IsUsable( Entity user )
	{
		var player = user as JustAnotherPlayer;
		return player.State == JustAnotherPlayer.PlayerStates.FixIceCreamMachine;
	}

	public void Enable()
	{
		if (!Using && TimeSinceUsed > 0.5f) {
			WrenchingSound = Sound.FromEntity( "wrench", this );
			Using = true;
		}

		if (Using) {
			TimeSinceUsed = 0;
		}
	}

	public void Disable()
	{
		if (Using && TimeSinceUsed > 0.5) {
			WrenchingSound.Stop();
			Using = false;
		}
	}
}
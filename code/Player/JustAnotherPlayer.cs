using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Sandbox;
using Sandbox.Component;
using Sandbox.UI;

public partial class JustAnotherPlayer : Player
{
	[Net, Change]
	public PlayerStates State { get; set; } = PlayerStates.PickupTrash;

	public void OnStateChanged(PlayerStates oldValue, PlayerStates newValue)
	{
		SelfSpeak.Current.SayTaskComplete();
		ConsoleSystem.Run( "increment_chore" );
	}

	[Net]
	public RealTimeSince EventTimer { get; set; }

	[Net]
	public bool Incapacitated { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceIncapacitated { get; set; }

	public bool PrevIncapacitated { get; set; }

	[Net]
	public RealTimeUntil TimeUntilDeath { get; set; }

	[Net]
	public bool PlayedDeathSound { get; set; } = false;

	[Net]
	public Vector3 DeathCameraPos { get; set; }

	WorldInput WorldInput = new();

	const float WalkSpeed = 150f;

	public enum PlayerStates
	{
		PickupTrash,
		TakeOutTrashBags,
		WashDishes,
		MopFloors,
		FixIceCreamMachine,
		OrganizeFreezer
	}

	public bool HasRoomForItem()
	{
		return ActiveChild == null;
	}

	public override void Respawn()
	{
		base.Respawn();

		SetModel( "models/citizen/citizen.vmdl" );

		// BBox box = BBox.FromHeightAndRadius( 70.0f, 18.0f );
		// BBox box = BBox.FromHeightAndRadius( 10.0f, 12.0f );

		// SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, box.Mins + EyePosition + Vector3.Up * 5f, box.Maxs + EyePosition + Vector3.Up * 5f );
		Capsule cap = Capsule.FromHeightAndRadius( 10, 10f );
		SetupPhysicsFromCylinder( PhysicsMotionType.Keyframed, cap );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		// Don't show how the citizen looks idk.
		EnableShadowInFirstPerson = false;

		// Controller = new WalkController(){
		// 	SprintSpeed = WalkSpeed - 50,
		// 	WalkSpeed = WalkSpeed,
		// 	DefaultSpeed = WalkSpeed,
		// 	AirAcceleration = 0,
		// 	StopSpeed = 50,
		// 	Gravity = 1000,
		// };

		Controller = new WalkController();
		// Animator = new StandardPlayerAnimator();
		// CameraMode = new FirstPersonCamera{
		// 	ZNear = 2.5f // to compensate for the zooming in effect of chromatic abberation.
		// };

		State = PlayerStates.PickupTrash;
		EventTimer = 0;

		Tags.Add( "player" );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();
		// Play the intro sound when client spawns
		Sound.FromEntity( "intro", this );
	}

	public override void Simulate( IClient cl )
	{
		if (Incapacitated && !PrevIncapacitated) {
			EnableDrawing = false;
			Controller = null;
			// Animator = null;
			// CameraMode = new DeathCamera();
			var creep = All.OfType<Creep>().First();

			DeathCameraPos = (creep.Position + creep.Rotation.Forward * 25f).WithZ( EyePosition.z );
			TimeUntilDeath = 8;

			TimeSinceIncapacitated = 0;

			Sound.FromWorld( "death", EyePosition );

			PrevIncapacitated = Incapacitated;
		}
		if (Incapacitated) {
			// var creep = All.OfType<Creep>().First();
			Camera.ZNear = 1f;

			if (TimeUntilDeath < 3.5f && !PlayedDeathSound) {
				if (Game.IsServer) {
					Sound.FromEntity( "end_game", this );
					PlayedDeathSound = true;
				}
			}
			// if (Sound.Listener == null) {
			// 	Sound.Listener = Transform;
			// }

			// Sound.Listener = Transform.WithPosition(
			// 	Sound.Listener.Value.Position - Vector3.Down * .5f
			// );
		}

		base.Simulate( cl );

		// Get the current active controller.
		if ( GetActiveController() is not WalkController ctrl )
			return;

		var controller = GetActiveController();
		if (controller != null) {
			SimulateAnimator( controller );
		}

		TickPlayerUse();

		//! TODO idk yet
		// if (Input.ActiveChild != null) {
		// 	ActiveChild = Input.ActiveChild;
		// }

		// CameraMode.ZNear = 2.5f;
		Camera.ZNear = 2.5f;

		// Set some default speeds. (I don't like how I'm setting it every tick)
		ctrl.DefaultSpeed = WalkSpeed;
		ctrl.WalkSpeed = WalkSpeed;
		ctrl.SprintSpeed = WalkSpeed - 50;

		SimulateActiveChild( cl, ActiveChild );

		// If we have something equipped then adjust our movement speeds.
		if (ActiveChild != null) {
			ctrl.DefaultSpeed = 100;
			ctrl.WalkSpeed = 100;
			ctrl.SprintSpeed = 50;
		}
	}

	protected override void TickPlayerUse()
	{
		// This is serverside only
		// if ( !Host.IsServer ) return;
		// if (IsServer)
		// 	return;

		// Turn prediction off
		using ( Prediction.Off() )
		{
			if ( Input.Pressed( InputButton.PrimaryAttack ) )
			{
				Using = FindUsable();

				if ( Using == null )
				{
					// UseFail();
					return;
				}

				if (Using is not ControlledDoor && Game.IsServer) {
					return;
				} else if ((Using is ControlledDoor || Using is DoorEntity) && Game.IsClient) {
					return;
				}
			}

			if ( !Input.Down( InputButton.PrimaryAttack ) )
			{
				StopUsing();
				return;
			}

			if ( !Using.IsValid() )
				return;

			if (Using is not ControlledDoor && Game.IsServer) {
				return;
			} else if ((Using is ControlledDoor || Using is DoorEntity) && Game.IsClient) {
				return;
			}

			// If we move too far away or something we should probably ClearUse()?

			//
			// If use returns true then we can keep using it
			//
			if ( Using is IUse use && use.OnUse( this ) )
				return;

			StopUsing();
		}
	}

	public override void BuildInput()
	{
		base.BuildInput();

		WorldInput.Ray = new Ray( EyePosition, EyeRotation.Forward );
		WorldInput.MouseLeftPressed = Input.Down( InputButton.PrimaryAttack );

		// Disallow jumping. This is a no fun zone.
		if (Input.Down(InputButton.Jump)) {
			Input.ClearButton( InputButton.Jump );
		}
	}

	public override float FootstepVolume()
	{
		if (Incapacitated) {
			return 0f;
		}

		return Velocity.WithZ( 0 ).Length.LerpInverse( 0, 200f ) * 2.5f;
	}
}

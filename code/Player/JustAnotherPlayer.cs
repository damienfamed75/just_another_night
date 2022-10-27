using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Sandbox;
using Sandbox.Component;
using Sandbox.UI;

public partial class JustAnotherPlayer : Player
{
	[Net]
	public PlayerStates State { get; set; } = PlayerStates.PickupTrash;

	[Net]
	public RealTimeSince EventTimer { get; set; }

	[Net]
	public bool Incapacitated { get; set; }

	public bool PrevIncapacitated { get; set; }

	[Net]
	public RealTimeUntil TimeUntilDeath { get; set; }

	[Net]
	public bool PlayedDeathSound { get; set; } = false;

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

	public override void Spawn()
	{
		base.Spawn();

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

		Controller = new WalkController(){
			SprintSpeed = WalkSpeed - 50,
			WalkSpeed = WalkSpeed,
			DefaultSpeed = WalkSpeed,
			AirAcceleration = 0,
			StopSpeed = 50,
			Gravity = 1000,
		};
		Animator = new StandardPlayerAnimator();
		CameraMode = new FirstPersonCamera{
			ZNear = 2.5f // to compensate for the zooming in effect of chromatic abberation.
		};

		State = PlayerStates.PickupTrash;
		EventTimer = 0;

		Tags.Add( "player" );
	}

	public override void Simulate( Client cl )
	{
		if (Incapacitated && !PrevIncapacitated) {
			EnableDrawing = false;
			Controller = null;
			Animator = null;
			CameraMode = new DeathCamera();
			TimeUntilDeath = 8;
			// CameraMode.Position = EyePosition;
			// CameraMode.Rotation = EyeRotation;
			// Sound.FromScreen( To.Single(cl), "death" );
			Sound.FromWorld( "death", EyePosition );

			PrevIncapacitated = Incapacitated;
		}
		if (Incapacitated) {
			// var creep = All.OfType<Creep>().First();
			CameraMode.ZNear = 1f;

			if (TimeUntilDeath < 3.5f && !PlayedDeathSound) {
				if (IsServer) {
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

		TickPlayerUse();

		if (Input.ActiveChild != null) {
			ActiveChild = Input.ActiveChild;
		}

		CameraMode.ZNear = 2.5f;

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

	public override void BuildInput( InputBuilder input )
	{
		// base.BuildInput( input );
		WorldInput.Ray = new Ray( EyePosition, EyeRotation.Forward );
		WorldInput.MouseLeftPressed = input.Down( InputButton.PrimaryAttack );

		// Disallow jumping. This is a no fun zone.
		if (input.Down(InputButton.Jump)) {
			input.ClearButton( InputButton.Jump );
		}
	}

	public override float FootstepVolume()
	{
		return Velocity.WithZ( 0 ).Length.LerpInverse( 0, 200f ) * 10f;
	}
}
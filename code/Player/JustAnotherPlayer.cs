using System;
using System.ComponentModel;
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

	WorldInput WorldInput = new();

	public enum PlayerStates
	{
		PickupTrash,
		TakeOutTrashBags,
		WashDishes,
		MopFloors,
		FixIceCreamMachine,
		OrganizeFreezer
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/citizen/citizen.vmdl" );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		// Don't show how the citizen looks idk.
		EnableShadowInFirstPerson = false;

		Controller = new WalkController(){
			SprintSpeed = 100,
			WalkSpeed = 150,
			DefaultSpeed = 150,
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
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );
		TickPlayerUse();
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
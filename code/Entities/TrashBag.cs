using Sandbox;
using Editor;
using Sandbox.Component;
using System.Linq;
using JustAnotherNight.Player;
using JustAnotherNight.UI;

namespace JustAnotherNight.Entities;

[HammerEntity]
[EditorModel( "models/trash_bag.vmdl" )]
public partial class TrashBag : ModelEntity, IUse
{
	public ActionButton Button;

	public TrashBag()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		Tags.Add( "trashbag" );
	}

	public bool IsUsable( Entity user )
	{
		var player = user as JustAnotherPlayer;

		return player.State == JustAnotherPlayer.PlayerStates.TakeOutTrashBags && player.HasRoomForItem();
	}

	public bool OnUse( Entity user )
	{
		if ( Game.IsServer )
			return false;

		if ( (GameManager.Current as JustAnotherGame).WaitingCustomer )
		{
			SelfSpeak.Current.SayCustomerWaiting();
			return false;
		}

		var player = user as JustAnotherPlayer;
		// Double check that the player has space for the item.
		if ( !player.HasRoomForItem() )
			return false;

		// Delete the button since we've picked it up.
		Button.Delete();
		// Remove any glow components on this object so it doesn't glow
		// in the player's arms.
		Components.RemoveAny<Glow>();

		ConsoleSystem.Run( "equip_ent", Name );

		return false;
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/trash_bag.vmdl" );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		var glow = Components.GetOrCreate<Glow>();
		glow.ObscuredColor = Color.Transparent;
		glow.Color = Color.White;
		glow.Width = 0f;

		Button = new ActionButton();
		Button.Position = Position + Rotation.Up * 42f;
		Button.Prop = this;
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		var pl = Owner as JustAnotherPlayer;

		Position = pl.EyePosition + pl.EyeRotation.Forward * 32f + pl.EyeRotation.Down * 40f;
		Rotation = pl.EyeRotation;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}
}

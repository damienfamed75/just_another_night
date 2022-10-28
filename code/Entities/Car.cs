using System.Linq;
using Sandbox;
using SandboxEditor;

[HammerEntity]
[Model(Model = "models/car.vmdl")]
public partial class Car : ModelEntity, IUse
{
	[Net]
	public bool Finished { get; set; } = false;

	[Net]
	public bool Driving { get; set; } = false;

	[Net]
	public int MaterialGroup { get; set; } = 0;

	public bool HasDialogued { get; set; } = false;

	public ActionButton Button;
	public Sound RevSound;

	[ConCmd.Server("car_finish")]
	public static void Finish(string name)
	{
		var car = All.OfType<Car>().Where( x => x.Name == name ).First();
		car.Finished = true;

		(Game.Current as JustAnotherGame).WaitingCustomer = false;
	}

	public Car()
	{
		Tags.Add( "car" );
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/car.vmdl" );

		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );

		if (MaterialGroup != 0)
			SetMaterialGroup( MaterialGroup );
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();

		if (Driving) {
			RevSound = Sound.FromEntity( "car-rev", this );
		}


		// Button = new ActionButton();
		// Button.Position = Position + Vector3.Up * 45f + Rotation.Left * 36f;
		// Button.Prop = this;
	}

	[ClientRpc]
	public void CreateButton()
	{
		if (Button == null) {
			Button = new ActionButton();
			Button.Position = Position + Vector3.Up * 40f + Rotation.Left * 36f;
			Button.Prop = this;
		}

		var creep = Parent as Creep;
		if (creep.HasBeenLookedAt && !HasDialogued && creep.CarSinceLookedAt > 1) {
			if (IsClient) {
				string message = MaterialGroup switch {
					0 => "...",
					1 => "hey what's up",
					2 => "can i just get my order please.",
					_ => "i am error"
				};

				var dialogue = new Speech(message);
				dialogue.Position = Position + Vector3.Up * 58f + Rotation.Left * 35f;

				Log.Info( "created speech bubble" );
			}

			HasDialogued = true;

		}
	}

	public Transform GetSeatPosition()
	{
		var t = Model.GetAttachment( "seat" );

		if (t.HasValue)
			return t.Value;

		return default;
	}

	public bool OnUse( Entity user )
	{
		if (IsServer)
			return false;

		if (!Finished) {
			Button.Delete();
			ConsoleSystem.Run( "car_finish", Name );
		}

		return false;
	}

	public bool IsUsable( Entity user )
	{
		return true;
	}
}
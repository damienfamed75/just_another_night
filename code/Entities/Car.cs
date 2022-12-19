using System.Linq;
using Sandbox;
using Editor;

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

	[Net]
	public RealTimeSince TimeSinceFinished { get; set; }

	public ActionButton Button;
	public Sound RevSound;

	[Net, Predicted]
	public ModelEntity FoodBag { get; set; }

	[Net]
	public Vector3 FoodBeginPosition { get; set; }

	[Net]
	public bool SaidSecondText { get; set; } = false;

	[ConCmd.Server("car_finish")]
	public static void Finish(string name)
	{
		var car = All.OfType<Car>().Where( x => x.Name == name ).First();
		car.Finished = true;
		car.TimeSinceFinished = 0;
		car.FoodBag.EnableDrawing = true;

		car.FoodBeginPosition = car.Position
			+ Vector3.Up * 40f
			+ car.Rotation.Left * 42f
			+ car.Rotation.Forward * 5f;

		(GameManager.Current as JustAnotherGame).WaitingCustomer = false;
	}

	public Car()
	{
		Tags.Add( "car" );
	}

	[ClientRpc]
	public void StopSounds()
	{
		RevSound.Stop();
	}

	public override void Spawn()
	{
		base.Spawn();

		FoodBag = new ModelEntity( "models/paper_bag.vmdl" );
		FoodBag.SetParent( this, null, Transform.Zero );
		FoodBag.EnableDrawing = false;

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
			Button.Position = Position + Vector3.Up * 40f + Rotation.Left * 36f + Rotation.Forward * 5f;
			Button.Prop = this;
		}

		var creep = Parent as Creep;
		if (creep.HasBeenLookedAt && !HasDialogued && creep.CarSinceLookedAt > 1) {
			if (Game.IsClient) {
				string message = MaterialGroup switch {
					0 => ". . .", // the spaces are needed because my font is bad. :)
					1 => "hey what's up",
					2 => "i have places to be.",
					_ => "i am error"
				};

				var dialogue = new Speech(message);
				dialogue.Position = Position + Vector3.Up * 58f + Rotation.Left * 35f + Rotation.Forward * 5f;

				if (MaterialGroup == 1) {
					Sound.FromEntity( "customer_1_speak", Game.LocalPawn );
				} else if (MaterialGroup == 2) {
					Sound.FromEntity( "customer_2_speak_first", Game.LocalPawn );
				}

				// Log.Info( "created speech bubble" );
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
		if (Game.IsServer)
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

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		// If the button was clicked then transfer some food here.
		if (!Finished)
			return;

		if (FoodBag == null || !FoodBag.IsValid())
			return;

		var delta = TimeSinceFinished.Relative.LerpInverse( 0.0f, 3.0f );

		FoodBag.EnableDrawing = true;

		// if (IsClient)
		// 	return;

		if (Game.IsClient) {
			return;
		}

		FoodBag.Position = FoodBeginPosition.LerpTo(
			Position + Vector3.Up * 40f + Rotation.Forward * 5f,
			delta
		);

		if (delta == 1.0f) {
			FoodBag.Delete();

			// If this is the second normal customer then say something else
			// to get the player to continue looking at them.
			if (MaterialGroup == 2 && !SaidSecondText) {
				SecondText();
				SaidSecondText = true;
			}
		}
	}

	[ClientRpc]
	public void SecondText()
	{
		Sound.FromEntity( "customer_2_speak_second", Game.LocalPawn );
		var dialogue = new Speech("be careful kid.");
		dialogue.Position = Position + Vector3.Up * 58f + Rotation.Left * 35f + Rotation.Forward * 5f;
	}
}

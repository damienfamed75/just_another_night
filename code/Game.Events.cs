using System.Linq;
using System.Threading.Tasks;

public partial class JustAnotherGame
{
	public void DeleteCreep()
	{
		var creep = All.OfType<Creep>().FirstOrDefault();
		// If the creep is not null, then delete them.
		creep?.Delete();
	}

	public void TrashStaringCreep()
	{
		var creep = SpawnCreep("creep_trash");
		creep.State = Creep.CreepStates.TakeOutTrashBags;
	}

	public void StaringCreep()
	{
		var creep = SpawnCreep("creep_stare");
		creep.State = Creep.CreepStates.WatchOutsideBuilding;
	}

	public void FreezerCreep()
	{
		var creep = SpawnCreep( "creep_freezer" );
		creep.State = Creep.CreepStates.Kill;
	}

	private async Task WaitUntilCustomerIsGone()
	{
		while (WaitingCustomer) {
			StateTimer = 1;
			await WaitStateTimer();
		}
	}

	public async Task FirstNormalCustomer()
	{
		await WaitUntilCustomerIsGone();
		// Put the creep in the seat of the car.
		SpawnCustomer( 1 ); // Material Group 1
	}

	public async Task CreepCustomer()
	{
		await WaitUntilCustomerIsGone();
		// The creep's material group skin is default so it's zero.
		SpawnCustomer( 0 ); // Material Group 0
	}

	public async Task SecondNormalCustomer()
	{
		await WaitUntilCustomerIsGone();
		// Put the creep in the seat of the car.
		SpawnCustomer( 2 ); // Material Group 2
	}

	private void SpawnCustomer(int group = 0)
	{
		var creep = SpawnCreep( "car_spawn", group );
		creep.State = Creep.CreepStates.Car;
		creep.Position += Rotation.Forward * 200f;

		// Create a new car and move the car to the creep location.
		var car = new Car {
			Parent = creep,
			Driving = true,
			MaterialGroup = group,
			Transform = creep.Transform
		};

		car.Spawn();

		WaitingCustomer = true;
		SayCustomerArrived();
	}

	/// <summary>
	/// Do not use this.
	/// </summary>
	private Creep SpawnCreep(string locationTag, int matGroup = 0)
	{
		var creep = new Creep {
			Parent = this,
			MaterialGroup = matGroup
		};

		var spawnpoint = All.OfType<NPCSpawn>()
			.Where( x => x.Tags.Has( locationTag ) )
			.FirstOrDefault();

		if (spawnpoint != null)
			creep.Transform = spawnpoint.Transform;

		creep.Spawn();
		return creep;
	}
}
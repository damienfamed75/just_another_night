using System.Linq;
using System.Threading.Tasks;

public partial class JustAnotherGame
{
	public void DeleteCreep()
	{
		var creep = All.OfType<Creep>().FirstOrDefault();
		if (creep != null) {
			creep.Delete();
		}
	}

	public void TrashStaringCreep()
	{
		var creep = SpawnCreep("creep_trash");
		creep.State = Creep.CreepStates.TakeOutTrashBags;

		// var spawnpoint = All.OfType<NPCSpawn>()
		// 	.Where( x => x.Tags.Has( "creep_trash" ) )
		// 	.FirstOrDefault();

		// if (spawnpoint != null)
		// 	creep.Transform = spawnpoint.Transform;

		Log.Info( "spawned trash creep" );
	}

	public void StaringCreep()
	{
		var creep = SpawnCreep("creep_stare");
		creep.State = Creep.CreepStates.WatchOutsideBuilding;

		// var spawnpoint = All.OfType<NPCSpawn>()
		// 	.Where( x => x.Tags.Has( "creep_stare" ) )
		// 	.FirstOrDefault();

		// if (spawnpoint != null)
		// 	creep.Transform = spawnpoint.Transform;

		Log.Info( "spawned creep" );
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
		// creep.Transform = car.GetSeatPosition();
		SpawnCustomer( 1 );
	}

	public async Task CreepCustomer()
	{
		await WaitUntilCustomerIsGone();
		// The creep's material group skin is default so it's zero.
		SpawnCustomer( 0 );
	}

	public async Task SecondNormalCustomer()
	{
		await WaitUntilCustomerIsGone();
		SpawnCustomer( 2 );
	}

	private void SpawnCustomer(int group = 0)
	{
		var creep = SpawnCreep( "car_spawn", group );
		creep.State = Creep.CreepStates.Car;
		creep.Position += Rotation.Forward * 200f;

		var car = new Car {
			Parent = creep,
			Driving = true,
			MaterialGroup = group
		};
		// Move the car to the creep.
		car.Transform = creep.Transform;

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
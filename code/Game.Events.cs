using System.Linq;

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

	/// <summary>
	/// Do not use this.
	/// </summary>
	private Creep SpawnCreep(string locationTag)
	{
		var creep = new Creep {
			Parent = this
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
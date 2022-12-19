using System.Linq;
using Sandbox;
using Editor;

[HammerEntity]
[Model(Model = "models/chore_list.vmdl")]
[Title( "Chore List" ), Category( "Gameplay" ), Icon( "door_front" )]
public partial class ChoreList : ModelEntity
{
	[Net]
	public int MaterialGroup { get; set; } = 0;

	public ChoreList()
	{

	}

	public void Increment()
	{
		MaterialGroup += 1;
		SetMaterialGroup( MaterialGroup );
	}

	[ConCmd.Server("increment_chore")]
	public static void IncrementChore()
	{
		var choreList = All.OfType<ChoreList>().FirstOrDefault();
		choreList.Increment();
	}
}

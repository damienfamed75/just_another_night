using Sandbox;
using SandboxEditor;

[HammerEntity]
[EditorModel("models/trash_bag.vmdl")]
public partial class TrashBag : ModelEntity, IUse
{
	public TrashBag()
	{

	}

	public bool IsUsable( Entity user )
	{
		throw new System.NotImplementedException();
	}

	public bool OnUse( Entity user )
	{
		throw new System.NotImplementedException();
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/trash_bag.vmdl" );
	}
}
using System;
using System.Linq;
using Sandbox;

public partial class Creep
{
	public Entity target;
	public NavSteer Steer;

	public void FindTarget()
    {
		target = All
			.OfType<Player>()
			.OrderBy( x => Guid.NewGuid() ) // Order randomly
			.FirstOrDefault();

        if (target == null)
			Log.Warning( $"couldn't find target for {this}" );
    }

}
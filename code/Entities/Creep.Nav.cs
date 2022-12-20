using System;
using System.Linq;
using Sandbox;
using JustAnotherNight.Player;

namespace JustAnotherNight.Entities;

public partial class Creep
{
	public Entity target;
	public NavSteer Steer;

	public void FindTarget()
    {
		target = Entity.All
			.OfType<Basic.Player>()
			.OrderBy( x => Guid.NewGuid() ) // Order randomly
			.FirstOrDefault();

        if (target == null)
			Log.Warning( $"couldn't find target for {this}" );
    }

}

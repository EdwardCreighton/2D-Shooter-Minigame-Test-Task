﻿using System.Numerics;
using Leopotam.Ecs;

namespace ShootingRangeMiniGame.Engine.Components
{
	public struct OnCollision
	{
		public EcsEntity OtherEntity;
		public Vector2 Point;
	}
}
using System;

using UnityEngine;

namespace ET
{
	[ComponentOf(typeof(Scene))]
	public class OperaComponent: Entity, IAwake<Scene>, IUpdate
    {
        public Vector3 ClickPoint;

	    public int mapMask;

	    public Scene Scene;
    }
}

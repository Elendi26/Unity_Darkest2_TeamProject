using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CRouteEffect
{
	public ERouteType RouteType = ERouteType.None;
	[SerializeReference] public List<IRunEffect> Effects = new List<IRunEffect>();
}

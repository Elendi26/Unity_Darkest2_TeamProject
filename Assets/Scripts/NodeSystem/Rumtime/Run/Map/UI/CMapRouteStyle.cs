using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CMapRouteStyle
{
    public ERouteType RouteType = ERouteType.Safe;
    public Color Color = Color.white;
    public Sprite Icon;
}
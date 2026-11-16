using System;
using System.Collections.Generic;
using UnityEngine;

public enum ERoadEventCategory
{
    None = 0,
    Obstacle,
    Pickup,
    Ambush,
    Banter,
}

[Serializable]
public class CRoadEventOption
{
    public string LabelKey = "road_option_ram";
    [SerializeReference] public List<IRunEffect> Effects = new List<IRunEffect>();
}

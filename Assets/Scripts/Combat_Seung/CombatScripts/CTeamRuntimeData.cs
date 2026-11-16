using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTeamRuntimeData : MonoBehaviour
{
    public static CTeamRuntimeData Instance { get; private set; }

    [SerializeField] private CTeamData<CEntityRuntimeData> _team;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InputTeamData(CTeamData<CEntityRuntimeData> teamData)
    {
        _team = teamData;
    }

    public CTeamData<CEntityRuntimeData> GetTeamData()
    {
        return _team;
    }
}
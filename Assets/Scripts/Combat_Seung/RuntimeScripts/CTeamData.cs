using System.Collections.Generic;
using UnityEngine;

public interface IRankEntity
{
    public abstract void InitializeRank(ERank rank);
}

public class CTeamData<T>
{
    private Dictionary<ERank, IRankEntity> _team;
    private int _maxCapacity;

    public CTeamData(int maxCapacity = 4)
    {
        _maxCapacity = maxCapacity;
        _team = new Dictionary<ERank, IRankEntity>(_maxCapacity);
    }

    public bool Add(ERank rank, IRankEntity value)
    {
        if (rank == ERank.None)
        {
            Debug.LogWarning("유효하지 않은 랭크(None)입니다.");
            return false;
        }

        if (_team.ContainsKey(rank))
        {
            Debug.LogWarning($"이미 {rank} 랭크에 캐릭터가 존재합니다!");
            return false;
        }

        if (_team.Count >= _maxCapacity)
        {
            Debug.LogWarning("팀 슬롯이 가득 참!");
            return false;
        }

        value.InitializeRank(rank);

        _team.Add(rank, value);
        return true;
    }

    public bool Add(IRankEntity value)
    {
        if (_team.Count >= _maxCapacity)
        {
            Debug.LogWarning("팀 슬롯이 가득 참!");
            return false;
        }

        ERank[] ranks = { ERank.One, ERank.Two, ERank.Three, ERank.Four };

        foreach (var rank in ranks)
        {
            if (!_team.ContainsKey(rank))
            {
                value.InitializeRank(rank);

                _team.Add(rank, value);

                return true;
            }
        }
        return false;
    }

    public bool Remove(ERank rank)
    {
        if (!_team.ContainsKey(rank))
        {
            Debug.LogWarning($"해당 랭크({rank})에 삭제할 데이터가 없습니다.");
            return false;
        }

        _team.Remove(rank);
        return true;
    }

    public IRankEntity Get(ERank rank)
    {
        if (_team.TryGetValue(rank, out IRankEntity value))
        {
            return value;
        }

        return default;
    }

    public void Clear()
    {
        _team.Clear();
    }

    public void ChangeRank(ERank rank, ERank moveTo)
    {

    }

    /// <summary>
    /// 일정 수치만큼 밀거나/당기기
    /// 밀기 -> One -> Four
    /// 당기기 -> Four -> One
    /// </summary>
    /// <param name="rank"></param>
    /// <param name="amount"></param>
    /// <param name="push"></param>
    public void ChangeRank(ERank rank, int amount, bool push = false)
    {

    }
}
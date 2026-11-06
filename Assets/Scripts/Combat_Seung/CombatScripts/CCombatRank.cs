using UnityEngine;

public enum ECombatRank
{
    One,
    Two,
    Three,
    Four
}

/// <summary>
/// 각 칸에 CCombatRank를 붙이기
/// </summary>
public class CCombatRank : MonoBehaviour
{
    [SerializeField] private ECombatRank _combatRank;
    [SerializeField] private CEntityRuntimeData _entityRuntimeData;

    // 여기에 활성화/비활성화 시키고 싶은 것을 넣고

    private ERank _rank;

    private void Awake()
    {
        switch (_combatRank)
        {
            case ECombatRank.One:

                _rank |= ERank.One;

                break;
            case ECombatRank.Two:

                _rank |= ERank.Two;

                break;
            case ECombatRank.Three:

                _rank |= ERank.Three;

                break;

            case ECombatRank.Four:

                _rank |= ERank.Four;

                break;
            default:
                break;
        }
    }

    private void CheckRank(ERank rank)
    {
        if ((rank & _rank) != 0)
        {
            // 활성화
        }

        else
        {
            // 비활성화
        }
    }

    private void CheckTarget(ERank target)
    {
        if ((target & _rank) != 0)
        {
            // 활성화
        }

        else
        {
            // 비활성화
        }
    }
}

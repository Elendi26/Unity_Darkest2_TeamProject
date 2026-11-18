using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#region Enum

public enum ESkillType
{
    Melee,
    Ranged,
    Buff,
    Heal
}

public enum EInteractCombo
{
    None,
    True,
    False
}

[Flags]
public enum ERank
{
    None = 0,
    One = 1 << 0,
    Two = 1 << 1,
    Three = 1 << 2,
    Four = 1 << 3
}

#endregion


[System.Serializable]
[CreateAssetMenu (menuName = "2D3D/SO/SkillBasicDataSO", fileName = "SkillBasicData_")]
public class CSkillDataSO : ScriptableObject, ICombatData
{
    #region 인스펙터

    [Header("기본 데이터")]
    [SerializeField] private string _heroName;
    [Header("스킬 고유 ID")]
    [SerializeField] private string _skillID;
    [Header("스킬 이름")]
    [SerializeField] private string _skillName;
    [Header("스킬 설명")]
    [SerializeField] private string _skillExplain;
    [Header("스킬 원거리 or 근거리 타입")]
    [SerializeField] private string _skillType;
    [Header("스킬 세부 타입(피해 주는 스킬, 회복 스킬 등")]
    [SerializeField] private string _skillDetail;

    [SerializeField] private int _index;
	[SerializeField] private ERank _rank; // 스킬 고를 때 스킬 아이콘 활성화/비활성화 여부 확인
	[SerializeField] private ERank _target; // 스킬 고른 후 대상을 선택할 때 대상 활성화/비활성화 여부 확인
	[SerializeField] private ESkillType _type;
	[SerializeField] private bool _areaOfEffect = false; // 대상 선택 시, true라면 대상 전부 가져오기
	[SerializeField, Range(0, 100)] private float _critical = -1; // -1 = None
	[SerializeField] private int _cooldown = -1; // -1 = None
	[SerializeField] private int _charges = -1; // -1 = None

	[Header("스킬 효과")]
	[SerializeReference]
    [PolymorphicDropdown]
	private List<ISkillEffect> _skillEffect;
	[SerializeField] private bool _skillSelfEffect = false;
	[SerializeReference]
    [PolymorphicDropdown]
    private List<ISkillEffect> _skillEffectForSelf;

    [Header("스킬 아이콘 이미지")]
    [SerializeField] private Sprite _skillIcon;
    [Header("스킬 근거리 or 원거리 이미지")]
    [SerializeField] private Sprite _skillTypeIcon;
    [Header("스킬 사용 가능 자리 및 공격 가능 자리 이미지")]
    [SerializeField] private Sprite _skillRangeImage;
    [SerializeField] private Animation _testAnimation;
    [SerializeField] private Sprite _activeImage;
    [SerializeField] private Sprite _inactiveImage;

    #endregion


    #region 프로퍼티

    public string HeroName => _heroName;
    public string SkillID => _skillIcon.name;
    public string SkillName => _skillName;
    public string SkillExplain => _skillExplain;
    public string SkillType => _skillType;
    public string SkillDetail => _skillDetail;
    public int Index => _index;
    public ERank Rank => _rank;
    public ERank Target => _target;
    public ESkillType Type => _type;
    public bool AOE => _areaOfEffect;
    public float Critical => _critical;
    public int Cooldown => _cooldown;
    public int Charges => _charges;

    public List<ISkillEffect> SkillEffect => _skillEffect;
    public bool SkillSelfEffect => _skillSelfEffect;
    public List<ISkillEffect> SkillEffectForSelf => _skillEffectForSelf;



    public Sprite SkillIcon => _skillIcon;
    public Sprite SkillTypeIcon => _skillTypeIcon;
    public Sprite SkillRangeImage => _skillRangeImage;
    public Animation TestAnimation => _testAnimation;
    public Sprite ActiveImage => _activeImage;
    public Sprite InactiveImage => _inactiveImage;


    #endregion

    public int GetRankBitmask()
	{
		return (int) _rank;
	}

	public int GetTargetBitmask()
	{
		return (int) _target;
	}

	public (string, int) GetSkillIndex()
	{
		return (_heroName, _index);
	}

    public ICombatData ReturnCombatData()
    {
        return this;
    }


    // 이걸 스킬 작동에서 체크해야 할 지도??
    public bool CheckCritical(float extraCritical)
	{
		bool isCritical = false;

        if (!(_critical <= 0))
        {
            isCritical = UnityEngine.Random.Range(0f, 100f) <= _critical + extraCritical;
        }

		return isCritical;
    }

    public void UseSKill(CEntityRuntimeData entity, CEntityRuntimeData target)
    {
        Debug.Log($"스킬 사용 : {_heroName}, {_index}");

        switch (_type)
        {
            case ESkillType.Melee:

                if (_skillEffect == null)
                {
                    return;
                }

                else
                {
                    foreach (var effect in _skillEffect)
                    {
                        effect.SkillEffect(target, CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                    }
                }

                if (_skillSelfEffect)
                {
                    foreach (var effect in _skillEffectForSelf)
                    {
                        effect.SkillEffect(entity, CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                    }
                }

                break;

            case ESkillType.Ranged:

                if (_skillEffect == null)
                {
                    return;
                }

                else
                {
                    foreach (var effect in _skillEffect)
                    {
                        effect.SkillEffect(target, CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                    }
                }

                if (_skillSelfEffect)
                {
                    foreach (var effect in _skillEffectForSelf)
                    {
                        effect.SkillEffect(entity, CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                    }
                }

                break;

            case ESkillType.Buff:

                if (_skillEffect == null)
                {
                    return;
                }

                else
                {
                    foreach (var effect in _skillEffect)
                    {
                        effect.SkillEffect(target);
                    }
                }

                if (_skillSelfEffect)
                {
                    foreach (var effect in _skillEffectForSelf)
                    {
                        effect.SkillEffect(entity);
                    }
                }

                break;

            case ESkillType.Heal:

                if (_skillEffect == null)
                {
                    return;
                }

                else
                {
                    foreach (var effect in _skillEffect)
                    {
                        effect.SkillEffect(target, CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                    }
                }

                if (_skillSelfEffect)
                {
                    foreach (var effect in _skillEffectForSelf)
                    {
                        effect.SkillEffect(entity, CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                    }
                }

                break;

            default:

                break;
        }
    }

    public void UseSKill(CEntityRuntimeData entity, List<CEntityRuntimeData> targets)
	{
        Debug.Log($"다수 대상 스킬 사용 : {_heroName}, {_index}");

        switch (_type)
		{
			case ESkillType.Melee:

                if (_skillEffect == null)
                {
                    return;
                }

                else
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        foreach (var effect in _skillEffect)
                        {
                            effect.SkillEffect(targets[i], CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                        }
                    }
                }

                if (_skillSelfEffect)
                {
                    foreach (var effect in _skillEffectForSelf)
                    {
                        effect.SkillEffect(entity, CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                    }
                }

                break;

			case ESkillType.Ranged:

                if (_skillEffect == null)
                {
                    return;
                }

                else
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        foreach (var effect in _skillEffect)
                        {
                            effect.SkillEffect(targets[i], CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                        }
                    }
                }

                if (_skillSelfEffect)
                {
                    foreach (var effect in _skillEffectForSelf)
                    {
                        effect.SkillEffect(entity, CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                    }
                }

                break;

			case ESkillType.Buff:

                if (_skillEffect == null)
                {
                    return;
                }

                else
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        foreach (var effect in _skillEffect)
                        {
                            effect.SkillEffect(targets[i]);
                        }
                    }
                }

                if (_skillSelfEffect)
                {
                    foreach (var effect in _skillEffectForSelf)
                    {
                        effect.SkillEffect(entity);
                    }
                }

                break;

			case ESkillType.Heal:

                if (_skillEffect == null)
                {
                    return;
                }

                else
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        foreach (var effect in _skillEffect)
                        {
                            effect.SkillEffect(targets[i], CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                        }
                    }
                }

                if (_skillSelfEffect)
                {
                    foreach (var effect in _skillEffectForSelf)
                    {
                        effect.SkillEffect(entity, CheckCritical(entity.GetStat(EEntityStat.ExtraCritical)));
                    }
                }

                break;

			default:
				break;
		}
	}
}



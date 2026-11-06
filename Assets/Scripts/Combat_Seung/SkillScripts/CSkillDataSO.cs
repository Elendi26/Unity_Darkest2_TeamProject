using System;
using System.Collections.Generic;
using UnityEngine;

#region Enum

public enum ESkillType
{
    Melee,
    Ranged,
    Buff,
    Heal
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
public class CSkillDataSO : ScriptableObject
{
	#region 인스펙터

	[Header("기본 데이터")]
    [SerializeField] private string _heroName;
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
	[SerializeReference] private List<ISkillEffect> _skillEffectForSelf;

	[Header("UI등")]
	[SerializeField] private object _UI; // UI 관련 자유롭게 변경 가능
	[SerializeField] private Sprite _testSprite;
	[SerializeField] private Animation _testAnimation;

	#endregion


	#region 프로퍼티

	public string HeroName => _heroName;
	public int Index => _index;
	public ERank Rank => _rank;
	public ERank Target => _target;
	public ESkillType Type => _type;
	public bool AOE => _areaOfEffect;
	public int Cooldown => _cooldown;
	public List<ISkillEffect> SkillEffect => _skillEffect;
	public List<ISkillEffect> SkillEffectForSelf => _skillEffectForSelf;

	public object UI => _UI;

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


	public void UseSKill(CEntityRuntimeData Entity, params CEntityRuntimeData[] target)
	{
		bool isCritical = false;

		if (!(_critical <= 0))
		{
            isCritical = UnityEngine.Random.Range(0f, 100f) <= _critical;
        }

		if (_skillEffect == null)
		{
			return;
        }

		else
		{
            for (int i = 0; i < target.Length; i++)
            {
                foreach (var effect in _skillEffect)
                {
                    effect.SkillEffect(target[i], isCritical);
                }
            }
        }

		if (_skillSelfEffect)
		{
			foreach (var effect in _skillEffectForSelf)
			{
				effect.SkillEffect(Entity);
			}
		}
	}
}



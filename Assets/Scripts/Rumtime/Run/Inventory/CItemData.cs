using System.Collections.Generic;
using UnityEngine;

// 마차 인벤토리에 담는 소모품 하나
[CreateAssetMenu(menuName = "Inventory/Item_")]
public class CItemData : ScriptableObject
{
	#region 인스펙터
	[SerializeField] private string _displayName = "횃불 기름";
	[SerializeField, TextArea] private string _description = "";
	[SerializeField] private Sprite _icon;
	// 짐칸 분류 탭 (전투·횃불·장신구·마차)
	[SerializeField] private EItemCategory _category = EItemCategory.Supply;
	// 한 칸에 겹칠 수 있는 개수
	[SerializeField] private int _maxStack = 4;
	// 이 값이 가득 차 있으면 못 쓴다 (기름·수리 도구 등). None 이면 검사 안 함
	[SerializeField] private ERunValue _blockWhenFull = ERunValue.None;
	// 쓰면 적용하는 효과 (비우면 보관만 가능)
	[SerializeReference] private List<IRunEffect> _useEffects = new List<IRunEffect>();
	#endregion

	// 인벤토리에 보이는 이름
	public string DisplayName => _displayName;
	// 인벤토리에 보이는 설명
	public string Description => _description;
	// 칸 아이콘
	public Sprite Icon => _icon;
	// 짐칸 분류
	public EItemCategory Category => _category;
	// 한 칸에 겹칠 수 있는 개수
	public int MaxStack => Mathf.Max(1, _maxStack);
	// 가득 차면 못 쓰게 막을 값
	public ERunValue BlockWhenFull => _blockWhenFull;
	// 쓰면 적용하는 효과
	public IReadOnlyList<IRunEffect> UseEffects => _useEffects;
	// 쓸 수 있는 아이템인지 (효과가 하나라도 있다)
	public bool IsUsable => _useEffects != null && _useEffects.Count > 0;
}

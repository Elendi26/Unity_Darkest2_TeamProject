using System;
using UnityEngine;
using UnityEngine.UI;

// 인벤토리 칸 하나 → 아이콘 + 겹친 개수, 누르면 선택
public class CInventorySlotView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Image _frame;
	[SerializeField] private Image _icon;
	// 고른 칸에 씌우는 테두리 (비우면 칸 색으로 표시)
	[SerializeField] private Image _highlight;
	[SerializeField] private Text _countText;
	[SerializeField] private Button _button;
	[SerializeField] private Color _normalColor = new Color(0.22f, 0.18f, 0.14f, 1f);
	[SerializeField] private Color _selectedColor = new Color(0.62f, 0.45f, 0.2f, 1f);
	#endregion

	#region 내부 변수
	private Action _onClick;
	private bool _hasItem;
	private CItemDragSource _drag;
	#endregion

	private void Awake()
	{
		if (_button != null)
		{
			_button.onClick.AddListener(Click);
		}
	}

	public void Bind(CItemStack stack, bool isSelected, Action onClick)
	{
		_onClick = onClick;

		bool hasItem = stack != null && stack.Item != null;

		_hasItem = hasItem;

		if (_icon != null)
		{
			_icon.sprite = hasItem ? stack.Item.GetIcon(stack.Count) : null;
			_icon.enabled = _icon.sprite != null;
		}

		if (_countText != null)
		{
			_countText.text = hasItem ? stack.Count.ToString() : string.Empty;
		}

		if (_frame != null)
		{
			_frame.color = isSelected && _highlight == null ? _selectedColor : _normalColor;
		}

		if (_highlight != null)
		{
			_highlight.enabled = isSelected;
		}

		// 빈칸도 버튼을 켜 둔다 → 꺼 두면 비활성 색으로 흐려져 칸 테두리가 안 보인다 (누르는 건 Click 에서 막는다)
		if (_button != null)
		{
			_button.interactable = true;
		}
	}

	// 끌어서 다른 칸(정비공·영웅 장신구)에 놓을 때 들 아이템 → null 이면 이 칸은 끌지 않는다
	public void SetDrag(Func<CItemDragPayload> getPayload)
	{
		if (_drag == null)
		{
			_drag = gameObject.AddComponent<CItemDragSource>();
		}

		_drag.Setup(getPayload);
	}

	private void Click()
	{
		if (!_hasItem)
		{
			return;
		}

		_onClick?.Invoke();
	}
}

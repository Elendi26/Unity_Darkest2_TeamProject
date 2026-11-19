using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 고물상 상품 칸 하나 → 바탕·테두리·아이콘·개수·가격·상태를 그리고, 올리면 알리고 누르면 산다
public class CShopSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	#region 내부 변수
	private Image _frame;
	private Image _icon;
	private Text _count;
	private Text _price;
	private Image _priceIcon;
	private Text _stateText;
	private CanvasGroup _group;
	private Action _onClick;
	private Action<bool> _onHover;
	private bool _canBuy;
	#endregion

	public void Setup(Image frame, Image icon, Text count, Text price, Image priceIcon, Text stateText, CanvasGroup group)
	{
		_frame = frame;
		_icon = icon;
		_count = count;
		_price = price;
		_priceIcon = priceIcon;
		_stateText = stateText;
		_group = group;
	}

	public void Bind(CShopCardInfo info, Sprite frame, Sprite priceIcon, Action onClick, Action<bool> onHover)
	{
		_onClick = onClick;
		_onHover = onHover;
		_canBuy = info.State == EShopCardState.Available;

		_frame.sprite = frame;
		_frame.enabled = frame != null;
		_icon.sprite = info.Icon;
		_icon.enabled = info.Icon != null;
		_count.text = info.Left > 0 ? info.Left.ToString() : string.Empty;
		_price.text = info.Cost > 0 ? info.Cost.ToString() : "무료";
		// 돈이 모자라면 가격을 붉게
		_price.color = info.State == EShopCardState.TooExpensive ? new Color(0.85f, 0.3f, 0.25f, 1f) : new Color(0.9f, 0.82f, 0.62f, 1f);
		_priceIcon.sprite = priceIcon;
		_priceIcon.enabled = priceIcon != null && info.Cost > 0;
		// 돈 부족은 붉은 가격으로 이미 보이니 글자는 안 띄운다
		_stateText.text = info.State == EShopCardState.TooExpensive ? string.Empty : CShopView.GetStateName(info.State);

		// 품절·상한은 흐리게, 가득 참은 살짝 흐리게
		bool isDim = info.State == EShopCardState.SoldOut || info.State == EShopCardState.Maxed;

		_group.alpha = isDim ? 0.35f : (info.State == EShopCardState.Full ? 0.7f : 1f);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_onHover?.Invoke(true);
		transform.localScale = Vector3.one * 1.05f;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_onHover?.Invoke(false);
		transform.localScale = Vector3.one;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && _canBuy)
		{
			_onClick?.Invoke();
		}
	}

	private void OnDisable()
	{
		transform.localScale = Vector3.one;
	}
}

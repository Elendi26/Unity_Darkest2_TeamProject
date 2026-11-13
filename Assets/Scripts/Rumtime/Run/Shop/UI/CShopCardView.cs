using System;
using UnityEngine;
using UnityEngine.UI;

// 상점 카드 하나 → 아이콘·이름·설명·가격·재고·구매 버튼
public class CShopCardView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Image _icon;
	[SerializeField] private Text _nameText;
	[SerializeField] private Text _descText;
	[SerializeField] private Text _priceText;
	[SerializeField] private Text _stockText;
	[SerializeField] private Button _buyButton;
	[SerializeField] private Text _buyLabel;
	[SerializeField] private CanvasGroup _group;
	[SerializeField] private float _dimAlpha = 0.45f;	// 못 사는 카드 투명도
	#endregion

	#region 내부 변수
	private Action _onBuy;
	#endregion

	private void Awake()
	{
		if (_buyButton != null)
		{
			_buyButton.onClick.AddListener(Buy);
		}
	}

	public void Bind(CShopCardInfo info, Action onBuy)
	{
		_onBuy = onBuy;

		if (_icon != null)
		{
			_icon.sprite = info.Icon;
			_icon.enabled = info.Icon != null;
		}

		SetText(_nameText, info.Name);
		SetText(_descText, info.Description);
		SetText(_priceText, info.Price);
		SetText(_stockText, info.Left > 0 ? $"남은 수량 {info.Left}" : string.Empty);
		SetText(_buyLabel, GetButtonLabel(info.State));

		bool canBuy = info.State == EShopCardState.Available;

		if (_buyButton != null)
		{
			_buyButton.interactable = canBuy;
		}

		// 품절·상한은 흐리게, 돈 부족·가득 참은 버튼만 끈다
		if (_group != null)
		{
			bool isDim = info.State == EShopCardState.SoldOut || info.State == EShopCardState.Maxed;

			_group.alpha = isDim ? _dimAlpha : 1f;
		}
	}

	private void Buy()
	{
		_onBuy?.Invoke();
	}

	private static void SetText(Text text, string value)
	{
		if (text != null)
		{
			text.text = value;
		}
	}

	private static string GetButtonLabel(EShopCardState state)
	{
		switch (state)
		{
			case EShopCardState.TooExpensive:
				return "돈 부족";
			case EShopCardState.SoldOut:
				return "품절";
			case EShopCardState.Full:
				return "가득 참";
			case EShopCardState.Maxed:
				return "최대";
		}

		return "구매";
	}
}

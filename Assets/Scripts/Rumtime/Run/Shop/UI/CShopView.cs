using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 상점 화면 → 상단 지갑, 가운데 카드 그리드, 하단 떠나기 버튼
public class CShopView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CanvasGroup _group;
	[SerializeField] private Text _titleText;
	[SerializeField] private Text _bodyText;
	[SerializeField] private Text _walletText;
	[SerializeField] private RectTransform _cardRoot;
	[SerializeField] private CShopCardView _cardTemplate;
	[SerializeField] private Button _leaveButton;
	#endregion

	#region 내부 변수
	private readonly List<CShopCardView> _cards = new List<CShopCardView>();
	private Action _onLeave;
	private Action<int> _onBuy;
	#endregion

	// 상점이 열려 있는지
	public bool IsOpen => _onLeave != null;

	private void Awake()
	{
		if (_cardTemplate != null)
		{
			_cardTemplate.gameObject.SetActive(false);
		}

		if (_leaveButton != null)
		{
			_leaveButton.onClick.AddListener(Leave);
		}

		SetVisible(false);
	}

	public void Open(string title, string body, Action onLeave)
	{
		_onLeave = onLeave;

		if (_titleText != null)
		{
			_titleText.text = title;
		}

		if (_bodyText != null)
		{
			_bodyText.text = body;
		}

		SetVisible(true);
	}

	public void SetWallet(string text)
	{
		if (_walletText != null)
		{
			_walletText.text = text;
		}
	}

	public void SetCards(IReadOnlyList<CShopCardInfo> cards, Action<int> onBuy)
	{
		_onBuy = onBuy;

		if (_cardRoot == null || _cardTemplate == null)
		{
			CPrint.Error("CShopView : Card Root / Card Template 이 비어 있습니다.");
			return;
		}

		int count = cards != null ? cards.Count : 0;

		for (int i = 0; i < count; i++)
		{
			while (_cards.Count <= i)
			{
				CShopCardView created = Instantiate(_cardTemplate, _cardRoot);

				created.name = $"Card_{_cards.Count}";
				_cards.Add(created);
			}

			int slot = i;

			_cards[i].gameObject.SetActive(true);
			_cards[i].Bind(cards[i], () => Buy(slot));
		}

		for (int i = count; i < _cards.Count; i++)
		{
			_cards[i].gameObject.SetActive(false);
		}
	}

	public void Close()
	{
		_onLeave = null;
		_onBuy = null;
		SetVisible(false);
	}

	private void Buy(int index)
	{
		if (!IsOpen)
		{
			return;
		}

		_onBuy?.Invoke(index);
	}

	private void Leave()
	{
		if (!IsOpen)
		{
			return;
		}

		_onLeave();
	}

	private void SetVisible(bool isVisible)
	{
		if (_group == null)
		{
			gameObject.SetActive(isVisible);
			return;
		}

		_group.alpha = isVisible ? 1f : 0f;
		_group.interactable = isVisible;
		_group.blocksRaycasts = isVisible;
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 도로 이벤트를 팝업으로 띄우고 플레이어가 고른 선택지의 효과를 적용한다
public class CRoadEventPopup : MonoBehaviour, IRoadEventResolver
{
	#region 인스펙터
	[SerializeField] private CanvasGroup _group;
	[SerializeField] private Text _titleText;
	[SerializeField] private Text _bodyText;
	[SerializeField] private RectTransform _optionRoot;
	[SerializeField] private Button _optionTemplate;
	[SerializeField] private string _continueLabel = "계속";
	[SerializeField] private bool _logInput = true;
	#endregion

	#region 내부 변수
	private readonly List<Button> _buttons = new List<Button>();
	private CRoadEventData _event;
	private CRunContext _ctx;
	private Action _onDone;
	#endregion

	// 팝업이 열려 있는지
	public bool IsOpen => _event != null;

	private void Awake()
	{
		SetVisible(false);
	}

	public void Resolve(CRoadEventData roadEvent, CRunContext ctx, Action onDone)
	{
		if (roadEvent == null)
		{
			onDone?.Invoke();
			return;
		}

		if (IsOpen)
		{
			CPrint.Warn($"CRoadEventPopup : {_event.Id} 가 아직 열려 있어 {roadEvent.Id} 로 교체합니다.");
		}

		_event = roadEvent;
		_ctx = ctx;
		_onDone = onDone;

		if (_titleText != null)
		{
			_titleText.text = roadEvent.Id;
		}

		if (_bodyText != null)
		{
			_bodyText.text = GetCategoryLabel(roadEvent.Category);
		}

		BuildOptions(roadEvent.Options);
		SetVisible(true);
	}

	private void BuildOptions(IReadOnlyList<CRoadEventOption> options)
	{
		int count = options != null ? options.Count : 0;

		// 선택지가 없으면 "계속" 버튼 하나만
		if (count == 0)
		{
			SetupButton(0, _continueLabel, -1);
			HideButtonsFrom(1);
			return;
		}

		for (int i = 0; i < count; i++)
		{
			string label = options[i] != null ? options[i].LabelKey : "-";

			SetupButton(i, label, i);
		}

		HideButtonsFrom(count);
	}

	private void SetupButton(int slot, string label, int optionIndex)
	{
		if (_optionRoot == null || _optionTemplate == null)
		{
			CPrint.Error("CRoadEventPopup : Option Root / Option Template 이 비어 있습니다.");
			return;
		}

		while (_buttons.Count <= slot)
		{
			Button created = Instantiate(_optionTemplate, _optionRoot);

			created.name = $"Option_{_buttons.Count}";
			_buttons.Add(created);
		}

		Button button = _buttons[slot];
		Text text = button.GetComponentInChildren<Text>(true);

		if (text != null)
		{
			text.text = label;
		}

		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(() => Choose(optionIndex));
		button.gameObject.SetActive(true);
	}

	private void HideButtonsFrom(int start)
	{
		for (int i = start; i < _buttons.Count; i++)
		{
			_buttons[i].gameObject.SetActive(false);
		}
	}

	private void Choose(int optionIndex)
	{
		if (!IsOpen)
		{
			return;
		}

		if (optionIndex >= 0 && optionIndex < _event.Options.Count)
		{
			CRoadEventOption option = _event.Options[optionIndex];

			if (_logInput)
			{
				CPrint.Log($"도로 이벤트 {_event.Id} → {option.LabelKey}");
			}

			ApplyEffects(option.Effects);
		}

		// 콜백 전에 비운다
		Action onDone = _onDone;

		_event = null;
		_ctx = null;
		_onDone = null;

		SetVisible(false);
		onDone?.Invoke();
	}

	private void ApplyEffects(List<IRunEffect> effects)
	{
		if (effects == null)
		{
			return;
		}

		for (int i = 0; i < effects.Count; i++)
		{
			if (effects[i] == null)
			{
				continue;
			}

			effects[i].Apply(_ctx);
		}
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

	private string GetCategoryLabel(ERoadEventCategory category)
	{
		switch (category)
		{
			case ERoadEventCategory.Obstacle:
				return "길이 막혀 있다.";
			case ERoadEventCategory.Pickup:
				return "길가에 무언가 떨어져 있다.";
			case ERoadEventCategory.Ambush:
				return "매복이다!";
			case ERoadEventCategory.Banter:
				return "일행이 이야기를 나눈다.";
		}

		return string.Empty;
	}
}

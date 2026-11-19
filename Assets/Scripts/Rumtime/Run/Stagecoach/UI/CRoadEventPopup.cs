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
	private readonly List<string> _labels = new List<string>();
	private Action<int> _onChoose;
	#endregion

	// 팝업이 열려 있는지
	public bool IsOpen => _onChoose != null;

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

		IReadOnlyList<CRoadEventOption> options = roadEvent.Options;
		int count = options != null ? options.Count : 0;

		_labels.Clear();

		for (int i = 0; i < count; i++)
		{
			_labels.Add(options[i] != null ? options[i].LabelKey : "-");
		}

		// 선택지가 없으면 "계속" 버튼 하나만
		if (count == 0)
		{
			_labels.Add(_continueLabel);
		}

		string title = string.IsNullOrEmpty(roadEvent.Title) ? roadEvent.Id : roadEvent.Title;
		string body = string.IsNullOrEmpty(roadEvent.Body) ? GetCategoryLabel(roadEvent.Category) : roadEvent.Body;

		Show(title, body, _labels, null, index =>
		{
			if (index < count && options[index] != null)
			{
				if (_logInput)
				{
					CPrint.Log($"도로 이벤트 {roadEvent.Id} → {options[index].LabelKey}");
				}

				CRunEffectUtil.ApplyAll(options[index].Effects, ctx);
			}

			Close();
			onDone?.Invoke();
		});
	}

	public void Show(string title, string body, IReadOnlyList<string> labels, IReadOnlyList<bool> interactable, Action<int> onChoose)
	{
		if (IsOpen && _onChoose != onChoose)
		{
			CPrint.Warn($"CRoadEventPopup : 열려 있는 팝업을 {title} 로 교체합니다.");
		}

		_onChoose = onChoose;

		if (_titleText != null)
		{
			_titleText.text = title;
		}

		if (_bodyText != null)
		{
			_bodyText.text = body;
		}

		int count = labels != null ? labels.Count : 0;

		for (int i = 0; i < count; i++)
		{
			bool canChoose = interactable == null || i >= interactable.Count || interactable[i];

			SetupButton(i, labels[i], canChoose);
		}

		HideButtonsFrom(count);
		SetVisible(true);
	}

	public void Close()
	{
		_onChoose = null;
		SetVisible(false);
	}

	private void SetupButton(int slot, string label, bool interactable)
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
		button.onClick.AddListener(() => Choose(slot));
		button.interactable = interactable;
		button.gameObject.SetActive(true);
	}

	private void HideButtonsFrom(int start)
	{
		for (int i = start; i < _buttons.Count; i++)
		{
			_buttons[i].gameObject.SetActive(false);
		}
	}

	private void Choose(int index)
	{
		if (!IsOpen)
		{
			return;
		}

		_onChoose(index);
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

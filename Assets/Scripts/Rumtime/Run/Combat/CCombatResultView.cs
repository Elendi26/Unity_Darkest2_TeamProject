using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 전투를 닫고 주행 화면이 밝아진 뒤 띄우는 결과창 → 결과·전투 종류·영웅별 체력/스트레스 변화, '계속' 으로 닫는다
public class CCombatResultView : MonoBehaviour
{
	#region 상수
	// 일시정지(900)보다 아래, 주행 UI 보다 위
	private const int SORTING_ORDER = 850;
	private const float PANEL_WIDTH = 720f;
	private const float HEADER_HEIGHT = 150f;
	private const float FOOTER_HEIGHT = 110f;
	private const float SHOW_TIME = 0.25f;
	private const float REWARD_TITLE_HEIGHT = 46f;
	private const float REWARD_ROW_HEIGHT = 30f;
	#endregion

	#region 내부 변수
	private static readonly Color DIM_COLOR = new Color(0f, 0f, 0f, 0.55f);
	private static readonly Color PANEL_COLOR = new Color(0.06f, 0.05f, 0.045f, 0.96f);
	private static readonly Color LINE_COLOR = new Color(0.45f, 0.38f, 0.26f, 1f);
	private static readonly Color TEXT_COLOR = new Color(0.9f, 0.84f, 0.7f, 1f);
	private static readonly Color DIM_TEXT_COLOR = new Color(0.62f, 0.58f, 0.5f, 1f);
	private static readonly Color BUTTON_COLOR = new Color(0.16f, 0.13f, 0.1f, 1f);
	private static readonly Color GOLD_COLOR = new Color(0.92f, 0.78f, 0.42f, 1f);
	private const string GOOD_HEX = "#8fbf6a";

	private CanvasGroup _group;
	private Action _onClosed;
	private float _time;
	private bool _isClosed;
	#endregion

	// 결과창을 띄운다 → 닫히면 onClosed
	public static CCombatResultView Show(Transform parent, CCombatResultData data, Font font, Action onClosed)
	{
		RectTransform canvas = CUIBuilder.CreateOverlayCanvas("CombatResultCanvas", parent, SORTING_ORDER, true);
		CCombatResultView view = canvas.gameObject.AddComponent<CCombatResultView>();

		view._onClosed = onClosed;
		view.Build(canvas, data, font != null ? font : CUIBuilder.GetDefaultFont());
		return view;
	}

	private void Update()
	{
		if (_group != null && _time < SHOW_TIME)
		{
			_time += Time.unscaledDeltaTime;
			_group.alpha = Mathf.Clamp01(_time / SHOW_TIME);
		}

		Keyboard keyboard = Keyboard.current;

		// 엔터·스페이스로도 닫는다
		if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
		{
			Close();
		}
	}

	private void Close()
	{
		if (_isClosed)
		{
			return;
		}

		_isClosed = true;

		Action onClosed = _onClosed;

		_onClosed = null;
		Destroy(gameObject);
		onClosed?.Invoke();
	}

	#region 만들기
	private void Build(RectTransform canvas, CCombatResultData data, Font font)
	{
		_group = canvas.gameObject.AddComponent<CanvasGroup>();
		_group.alpha = 0f;

		// 뒤를 어둡게 + 클릭 막기
		Image dim = CUIBuilder.CreateImage("Dim", canvas, null, DIM_COLOR);

		dim.raycastTarget = true;
		Stretch(dim.rectTransform);

		// 파티 정보는 띄우지 않고 전리품만 → 받은 게 없어도 "없음" 한 줄은 둔다
		int rewards = data != null ? data.Rewards.Count : 0;
		float rewardHeight = REWARD_TITLE_HEIGHT + Mathf.Max(1, rewards) * REWARD_ROW_HEIGHT + 10f;
		float height = HEADER_HEIGHT + rewardHeight + FOOTER_HEIGHT;

		Image panel = CUIBuilder.CreateImage("Panel", canvas, null, PANEL_COLOR);

		panel.raycastTarget = true;
		Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PANEL_WIDTH, height));

		RectTransform root = panel.rectTransform;
		ECombatResult result = data != null ? data.Result : ECombatResult.None;

		// 제목 (결과)
		Text title = CreateText("Title", root, font, GetTitle(result), 40, TextAnchor.MiddleCenter, GetTitleColor(result));

		title.fontStyle = FontStyle.Bold;
		Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(PANEL_WIDTH, 56f));

		// 부제 (전투 종류 · 장소)
		Text sub = CreateText("Subtitle", root, font, GetSubtitle(data), 22, TextAnchor.MiddleCenter, DIM_TEXT_COLOR);

		Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(PANEL_WIDTH, 30f));
		CreateLine(root, -HEADER_HEIGHT + 16f);

		CreateRewards(root, font, data != null ? data.Rewards : null, -HEADER_HEIGHT + 16f);

		CreateLine(root, -height + FOOTER_HEIGHT - 4f);
		CreateButton(root, font, "계속", -height + FOOTER_HEIGHT * 0.45f);

		EnsureEventSystem();
	}

	// 전리품 → 제목 한 줄 + 보상 한 줄씩 (top 은 이 칸의 위쪽 y)
	private static void CreateRewards(RectTransform root, Font font, List<string> rewards, float top)
	{
		Text title = CreateText("RewardTitle", root, font, "전리품", 24, TextAnchor.MiddleCenter, GOLD_COLOR);

		title.fontStyle = FontStyle.Bold;
		Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, top - REWARD_TITLE_HEIGHT * 0.55f), new Vector2(PANEL_WIDTH, 30f));

		if (rewards == null || rewards.Count == 0)
		{
			Text none = CreateText("RewardNone", root, font, "없음", 22, TextAnchor.MiddleCenter, DIM_TEXT_COLOR);

			Place(none.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, top - REWARD_TITLE_HEIGHT - REWARD_ROW_HEIGHT * 0.5f), new Vector2(PANEL_WIDTH, REWARD_ROW_HEIGHT));
			return;
		}

		for (int i = 0; i < rewards.Count; i++)
		{
			float y = top - REWARD_TITLE_HEIGHT - REWARD_ROW_HEIGHT * (i + 0.5f);
			Text line = CreateText($"Reward_{i}", root, font, $"<color={GOOD_HEX}>{rewards[i]}</color>", 22, TextAnchor.MiddleCenter, TEXT_COLOR);

			Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(PANEL_WIDTH, REWARD_ROW_HEIGHT));
		}
	}

	private void CreateButton(RectTransform root, Font font, string label, float y)
	{
		Image image = CUIBuilder.CreateImage("ContinueButton", root, null, BUTTON_COLOR);

		image.raycastTarget = true;
		Place(image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(220f, 56f));

		Button button = image.gameObject.AddComponent<Button>();

		button.onClick.AddListener(Close);

		Text text = CreateText("Label", image.rectTransform, font, label, 26, TextAnchor.MiddleCenter, TEXT_COLOR);

		Stretch(text.rectTransform);
	}

	private static void CreateLine(RectTransform root, float y)
	{
		Image line = CUIBuilder.CreateImage("Line", root, null, LINE_COLOR);

		Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(PANEL_WIDTH - 60f, 2f));
	}

	private static Text CreateText(string name, RectTransform parent, Font font, string value, int size, TextAnchor alignment, Color color)
	{
		RectTransform rect = CUIBuilder.CreateRect(name, parent);
		Text text = rect.gameObject.AddComponent<Text>();

		text.font = font;
		text.text = value;
		text.fontSize = size;
		text.alignment = alignment;
		text.color = color;
		text.supportRichText = true;
		text.horizontalOverflow = HorizontalWrapMode.Overflow;
		text.verticalOverflow = VerticalWrapMode.Overflow;
		text.raycastTarget = false;
		return text;
	}

	private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
	{
		rect.anchorMin = anchor;
		rect.anchorMax = anchor;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	// 버튼을 누르려면 EventSystem 이 있어야 한다 (주행 씬에 이미 있으면 그대로)
	private static void EnsureEventSystem()
	{
		if (EventSystem.current != null)
		{
			return;
		}

		CPrint.Warn("CCombatResultView : EventSystem 이 없어 버튼 대신 엔터·스페이스로만 닫을 수 있습니다.");
	}
	#endregion

	#region 문구
	private static string GetTitle(ECombatResult result)
	{
		switch (result)
		{
			case ECombatResult.Victory:
				return "전투 승리";
			case ECombatResult.Defeat:
				return "파티 전멸";
			case ECombatResult.Skipped:
				return "전투 건너뜀";
			case ECombatResult.Abandoned:
				return "전투 포기";
		}

		return "전투 종료";
	}

	private static Color GetTitleColor(ECombatResult result)
	{
		switch (result)
		{
			case ECombatResult.Victory:
				return new Color(0.92f, 0.78f, 0.42f, 1f);
			case ECombatResult.Defeat:
			case ECombatResult.Abandoned:
				return new Color(0.85f, 0.38f, 0.29f, 1f);
		}

		return TEXT_COLOR;
	}

	private static string GetSubtitle(CCombatResultData data)
	{
		if (data == null)
		{
			return string.Empty;
		}

		string kind = CCombatKindText.GetName(data.Kind);

		return string.IsNullOrEmpty(data.PlaceName) ? kind : $"{kind} · {data.PlaceName}";
	}
	#endregion
}

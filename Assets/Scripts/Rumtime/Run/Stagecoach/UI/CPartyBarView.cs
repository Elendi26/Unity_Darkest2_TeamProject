using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 주행 화면 아래쪽 파티 줄 → 교차로에서 고른 영웅을 초상화·이름·체력·스트레스로 보여 준다

public class CPartyBarView : MonoBehaviour
{
	#region 상수
	private const int SLOT_COUNT = 4;
	private const int STRESS_PIPS = 10;
	private const int SHADE_HEIGHT = 64;
	#endregion

	#region 인스펙터
	// 영웅 데이터(CEntityBasicDataSO)를 찾을 Resources 경로
	[SerializeField] private string _heroDataPath = "EntityBasicDataSO";
	// 다른 주행 UI(갈림길 표지판 5, 이벤트 띠 6)보다 아래
	[SerializeField] private int _sortingOrder = 4;
	// 화면 아래에서 띄우는 거리
	[SerializeField] private float _bottomMargin = 14f;
	[SerializeField] private Vector2 _slotSize = new Vector2(250f, 72f);
	[SerializeField] private float _slotSpacing = 14f;
	[SerializeField] private float _portraitSize = 70f;
	// 이름·막대가 초상화 오른쪽 끝에 살짝 겹쳐 시작한다
	[SerializeField] private float _infoOverlap = 10f;
	[SerializeField] private Vector2 _hpBarSize = new Vector2(150f, 7f);
	[SerializeField] private Vector2 _pipSize = new Vector2(9f, 9f);
	[SerializeField] private float _pipGap = 3f;
	[SerializeField] private int _nameFontSize = 24;
	// 화면 아래쪽을 아래로 갈수록 어둡게 → 글자가 읽히게 (상자 대신)
	[SerializeField] private float _shadeHeight = 150f;
	[SerializeField] private Color _shadeColor = new Color(0f, 0f, 0f, 0.75f);
	[SerializeField] private Color _nameColor = new Color(0.94f, 0.9f, 0.8f, 1f);
	[SerializeField] private Color _outlineColor = new Color(0f, 0f, 0f, 0.95f);
	[SerializeField] private Color _hpBackColor = new Color(0.12f, 0.03f, 0.02f, 0.9f);
	[SerializeField] private Color _hpFillColor = new Color(0.72f, 0.09f, 0.07f, 1f);
	[SerializeField] private Color _stressOnColor = new Color(0.96f, 0.95f, 0.92f, 1f);
	[SerializeField] private Color _stressOffColor = new Color(0.22f, 0.2f, 0.19f, 0.9f);
	// 마우스를 올렸을 때 초상화 밝기
	[SerializeField] private Color _portraitColor = new Color(0.82f, 0.82f, 0.82f, 1f);
	[SerializeField] private Color _portraitHoverColor = Color.white;
	// 교차로·여관처럼 위에 덧붙은 씬이 열려 있으면 숨긴다
	[SerializeField] private bool _hideUnderOverlayScene = true;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	// 칸 하나 (화면 왼쪽부터)
	private class CSlot
	{
		public GameObject Root;
		public Image Portrait;
		public Text Name;
		public RectTransform HpFill;
		public Image[] Pips;
		public CEntityBasicDataSO Data;
	}

	private readonly CSlot[] _slots = new CSlot[SLOT_COUNT];
	private readonly Dictionary<string, CEntityBasicDataSO> _heroes = new Dictionary<string, CEntityBasicDataSO>();
	private static Sprite _shadeSprite;
	private RectTransform _canvas;
	private RectTransform _bar;
	private Font _font;
	private int _partyCount;
	#endregion

	private void Awake()
	{
		_font = CUIBuilder.GetDefaultFont();
		Build();
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += HandleSceneLoaded;
		SceneManager.sceneUnloaded += HandleSceneUnloaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		SceneManager.sceneUnloaded -= HandleSceneUnloaded;
	}

	private void Start()
	{
		Refresh();
	}

	private void Update()
	{
		CHeroPopupLink.Tick();
	}

	private void LateUpdate()
	{
		if (_canvas == null)
		{
			return;
		}

		bool visible = _partyCount > 0 && (!_hideUnderOverlayScene || IsTopScene());

		if (_canvas.gameObject.activeSelf != visible)
		{
			_canvas.gameObject.SetActive(visible);
		}
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Refresh();
	}

	// 교차로에서 출정하면 파티가 저장되고 교차로 씬이 닫힌다 → 이때 다시 읽는다
	private void HandleSceneUnloaded(Scene scene)
	{
		Refresh();
	}

	// 저장된 파티를 다시 읽어 칸을 채운다 (체력·스트레스가 바뀐 뒤 불러도 된다)
	public void Refresh()
	{
		if (_bar == null)
		{
			return;
		}

		List<UserProfileData.HeroSaveData> party = LoadParty();

		if (party.Count > 0 && _heroes.Count == 0)
		{
			LoadHeroData();
		}

		for (int i = 0; i < SLOT_COUNT; i++)
		{
			_slots[i].Root.SetActive(false);
			_slots[i].Data = null;
		}

		_partyCount = 0;

		for (int i = 0; i < party.Count; i++)
		{
			UserProfileData.HeroSaveData save = party[i];

			if (save.PartySlotIndex < 0 || save.PartySlotIndex >= SLOT_COUNT)
			{
				continue;
			}

			if (!_heroes.TryGetValue(save.HeroID, out CEntityBasicDataSO data))
			{
				CPrint.Warn($"CPartyBarView : 영웅 데이터를 찾지 못했습니다. ({save.HeroID})");
				continue;
			}

			// 맨 앞(슬롯 0)이 오른쪽 끝
			Fill(_slots[SLOT_COUNT - 1 - save.PartySlotIndex], data, save);
			_partyCount++;
		}
	}

	private void Fill(CSlot slot, CEntityBasicDataSO data, UserProfileData.HeroSaveData save)
	{
		slot.Root.SetActive(true);
		slot.Data = data;
		slot.Portrait.sprite = data.HeroPortrait;
		slot.Portrait.enabled = data.HeroPortrait != null;
		slot.Portrait.color = _portraitColor;
		slot.Name.text = data.Name;

		int maxHp = Mathf.Max(1, data.MaxHP);
		float hp = Mathf.Clamp01((float)save.CurrentHP / maxHp);

		slot.HpFill.anchorMax = new Vector2(hp, 1f);

		int maxStress = data.MaxStress;
		int filled = maxStress > 0 ? Mathf.RoundToInt((float)Mathf.Clamp(save.CurrentStress, 0, maxStress) / maxStress * STRESS_PIPS) : 0;

		for (int i = 0; i < slot.Pips.Length; i++)
		{
			slot.Pips[i].color = i < filled ? _stressOnColor : _stressOffColor;
		}
	}

	// 지금 프로필의 파티 (파티에 들었고 살아 있는 영웅만)
	private static List<UserProfileData.HeroSaveData> LoadParty()
	{
		List<UserProfileData.HeroSaveData> party = new List<UserProfileData.HeroSaveData>();

		// 프로필에는 지난 런의 파티 자리가 남아 있다 → 이번 런에서 교차로 출정을 했을 때만 보인다
		if (!CRunCarry.PartyChosen)
		{
			return party;
		}

		DataSaveManager saveManager = DataSaveManager.Instance;

		if (saveManager == null || saveManager.CurrentSlotIndex < 0)
		{
			return party;
		}

		UserProfileData profile = saveManager.LoadProfile(saveManager.CurrentSlotIndex);

		if (profile == null || profile.PartyHeroes == null)
		{
			return party;
		}

		for (int i = 0; i < profile.PartyHeroes.Count; i++)
		{
			UserProfileData.HeroSaveData hero = profile.PartyHeroes[i];

			if (hero == null || hero.PartySlotIndex < 0 || hero.CurrentHP <= 0)
			{
				continue;
			}

			party.Add(hero);
		}

		return party;
	}

	private void LoadHeroData()
	{
		CEntityBasicDataSO[] all = Resources.LoadAll<CEntityBasicDataSO>(_heroDataPath);

		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] == null || string.IsNullOrEmpty(all[i].HeroID) || _heroes.ContainsKey(all[i].HeroID))
			{
				continue;
			}

			_heroes.Add(all[i].HeroID, all[i]);
		}
	}

	// 맨 위(마지막에 연) 씬이 이 씬일 때만 보인다
	private bool IsTopScene()
	{
		for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
		{
			Scene scene = SceneManager.GetSceneAt(i);

			if (!scene.isLoaded)
			{
				continue;
			}

			return scene == gameObject.scene;
		}

		return true;
	}

	#region 영웅 정보 창
	private void HandleSlotClicked(int index)
	{
		CSlot slot = _slots[index];

		if (slot == null || slot.Data == null)
		{
			return;
		}

		OpenPopup(slot.Data);
	}

	// 줄에 보이는 순서(왼쪽 → 오른쪽)대로 넘겨 → 창 안의 이전·다음 버튼도 같은 순서로 돈다
	private void OpenPopup(CEntityBasicDataSO data)
	{
		List<CEntityBasicDataSO> heroes = new List<CEntityBasicDataSO>();

		for (int i = 0; i < SLOT_COUNT; i++)
		{
			if (_slots[i].Data != null)
			{
				heroes.Add(_slots[i].Data);
			}
		}

		int index = heroes.IndexOf(data);

		if (CHeroPopupLink.Open(heroes, index) && _logState)
		{
			CPrint.Log($"CPartyBarView : 영웅 정보 창 열기 ({data.HeroID})");
		}
	}
	#endregion

	#region 만들기
	private void Build()
	{
		_canvas = CUIBuilder.CreateOverlayCanvas("PartyBarCanvas", transform, _sortingOrder, true);

		// 화면 아래 어둠 (가로 전체)
		Image shade = CUIBuilder.CreateImage("Shade", _canvas, GetShadeSprite(), _shadeColor);

		shade.rectTransform.anchorMin = Vector2.zero;
		shade.rectTransform.anchorMax = new Vector2(1f, 0f);
		shade.rectTransform.pivot = new Vector2(0.5f, 0f);
		shade.rectTransform.anchoredPosition = Vector2.zero;
		shade.rectTransform.sizeDelta = new Vector2(0f, _shadeHeight);

		_bar = CUIBuilder.CreateRect("PartyBar", _canvas);

		float width = SLOT_COUNT * _slotSize.x + (SLOT_COUNT - 1) * _slotSpacing;

		_bar.anchorMin = new Vector2(0.5f, 0f);
		_bar.anchorMax = new Vector2(0.5f, 0f);
		_bar.pivot = new Vector2(0.5f, 0f);
		_bar.anchoredPosition = new Vector2(0f, _bottomMargin);
		_bar.sizeDelta = new Vector2(width, _slotSize.y);

		for (int i = 0; i < SLOT_COUNT; i++)
		{
			_slots[i] = BuildSlot(i, -width * 0.5f + i * (_slotSize.x + _slotSpacing));
		}

		_canvas.gameObject.SetActive(false);
	}

	private CSlot BuildSlot(int index, float left)
	{
		CSlot slot = new CSlot();

		// 칸 전체가 누르는 자리 (투명)
		Image hit = CUIBuilder.CreateImage($"Slot_{index}", _bar, null, new Color(0f, 0f, 0f, 0f));
		RectTransform root = hit.rectTransform;

		hit.raycastTarget = true;
		root.anchorMin = new Vector2(0.5f, 0f);
		root.anchorMax = new Vector2(0.5f, 0f);
		root.pivot = new Vector2(0f, 0f);
		root.anchoredPosition = new Vector2(left, 0f);
		root.sizeDelta = _slotSize;
		slot.Root = root.gameObject;

		// 초상화 (왼쪽, 테두리 없이 그림자만)
		slot.Portrait = CUIBuilder.CreateImage("Portrait", root, null, _portraitColor);
		slot.Portrait.preserveAspect = true;
		SetLeftRect(slot.Portrait.rectTransform, 0f, (_slotSize.y - _portraitSize) * 0.5f, new Vector2(_portraitSize, _portraitSize));

		Shadow portraitShadow = slot.Portrait.gameObject.AddComponent<Shadow>();

		portraitShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
		portraitShadow.effectDistance = new Vector2(3f, -3f);

		// 이름 → 체력 막대 → 스트레스 칸 (초상화 오른쪽 끝에 겹쳐서, 위에서 아래로)
		float x = _portraitSize - _infoOverlap;
		float nameHeight = _nameFontSize + 8f;
		float nameY = _slotSize.y * 0.5f + 2f;

		slot.Name = CreateText("Name", root, _nameFontSize, _nameColor);
		SetLeftRect(slot.Name.rectTransform, x, nameY, new Vector2(_slotSize.x - x, nameHeight));

		float hpY = nameY - 4f - _hpBarSize.y;
		Image hpBack = CUIBuilder.CreateImage("HpBack", root, null, _hpBackColor);

		SetLeftRect(hpBack.rectTransform, x, hpY, _hpBarSize);

		Image hpFill = CUIBuilder.CreateImage("HpFill", hpBack.rectTransform, null, _hpFillColor);

		slot.HpFill = hpFill.rectTransform;
		slot.HpFill.anchorMin = Vector2.zero;
		slot.HpFill.anchorMax = Vector2.one;
		slot.HpFill.offsetMin = Vector2.zero;
		slot.HpFill.offsetMax = Vector2.zero;

		float pipY = hpY - 8f - _pipSize.y;

		slot.Pips = new Image[STRESS_PIPS];

		for (int i = 0; i < STRESS_PIPS; i++)
		{
			slot.Pips[i] = CUIBuilder.CreateImage($"Stress_{i}", root, null, _stressOffColor);
			SetLeftRect(slot.Pips[i].rectTransform, x + i * (_pipSize.x + _pipGap), pipY, _pipSize);
		}

		// 누르면 영웅 정보 창, 올리면 초상화를 밝게
		Button button = root.gameObject.AddComponent<Button>();
		int captured = index;

		button.transition = Selectable.Transition.None;
		button.targetGraphic = hit;
		button.onClick.AddListener(() => HandleSlotClicked(captured));
		AddHover(root.gameObject, slot);

		slot.Root.SetActive(false);

		return slot;
	}

	private void AddHover(GameObject target, CSlot slot)
	{
		EventTrigger trigger = target.AddComponent<EventTrigger>();

		AddTrigger(trigger, EventTriggerType.PointerEnter, () => slot.Portrait.color = _portraitHoverColor);
		AddTrigger(trigger, EventTriggerType.PointerExit, () => slot.Portrait.color = _portraitColor);
	}

	private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityAction action)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };

		entry.callback.AddListener(_ => action());
		trigger.triggers.Add(entry);
	}

	// 굵은 글자 + 검은 외곽선 (주행 화면 위에서 읽히게)
	private Text CreateText(string name, RectTransform parent, int size, Color color)
	{
		RectTransform rect = CUIBuilder.CreateRect(name, parent);
		Text text = rect.gameObject.AddComponent<Text>();

		text.font = _font;
		text.fontSize = size;
		text.fontStyle = FontStyle.Bold;
		text.color = color;
		text.alignment = TextAnchor.LowerLeft;
		text.horizontalOverflow = HorizontalWrapMode.Overflow;
		text.raycastTarget = false;

		Outline outline = rect.gameObject.AddComponent<Outline>();

		outline.effectColor = _outlineColor;
		outline.effectDistance = new Vector2(2f, -2f);

		return text;
	}

	// 칸 왼쪽 아래 기준으로 놓는다
	private static void SetLeftRect(RectTransform rect, float x, float y, Vector2 size)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.zero;
		rect.pivot = Vector2.zero;
		rect.anchoredPosition = new Vector2(x, y);
		rect.sizeDelta = size;
	}

	// 아래는 진하고 위로 갈수록 투명한 세로 그라데이션 (흰색, 이미지 색으로 물들인다)
	private static Sprite GetShadeSprite()
	{
		if (_shadeSprite != null)
		{
			return _shadeSprite;
		}

		Texture2D texture = new Texture2D(1, SHADE_HEIGHT, TextureFormat.RGBA32, false);

		texture.name = "PartyBarShade";
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Bilinear;

		for (int y = 0; y < SHADE_HEIGHT; y++)
		{
			float t = 1f - (y + 0.5f) / SHADE_HEIGHT;

			texture.SetPixel(0, y, new Color(1f, 1f, 1f, t * t));
		}

		texture.Apply();
		_shadeSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, SHADE_HEIGHT), new Vector2(0.5f, 0.5f), 100f);
		_shadeSprite.name = "PartyBarShade";

		return _shadeSprite;
	}
	#endregion
}

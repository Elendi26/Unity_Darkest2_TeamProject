using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 교차로 밖(주행 파티 줄·여관)에서 영웅 정보 창(HeroPopup_UI)을 열고 닫는다

public static class CHeroPopupLink
{
	#region 상수
	private const string CROSSROAD_CLOSE_METHOD = "ExitPopup";
	// 정보 창(300) 위로 짐칸을 올린다
	private const int INVENTORY_SORTING_ORDER = 310;
	#endregion

	#region 내부 변수
	private static HeroPopup_UI _opened;
	private static readonly List<Button> _hookedButtons = new List<Button>();
	private static CInventoryView _inventory;
	// 창과 함께 짐칸을 열었는지 (원래 열려 있었으면 닫을 때도 그대로) / 올리기 전 캔버스 상태
	private static bool _openedInventory;
	private static bool _addedCanvas;
	private static bool _prevOverrideSorting;
	private static int _prevSortingOrder;
	// 전투 밖에서 창에 넘기려고 세이브 값으로 만든 영웅들 (꺼진 오브젝트 아래에 둔다)
	private static readonly List<CEntityRuntimeData> _runtimeHeroes = new List<CEntityRuntimeData>();
	private static UnityEngine.GameObject _runtimeRoot;
	// 지난번에 만든 영웅들의 세이브 값·장신구 (같으면 다시 쓴다)
	private static string _runtimeKey;
	// 마지막으로 연 영웅·세이브 목록 → 장신구가 바뀌면 같은 목록으로 다시 채운다
	private static List<CEntityBasicDataSO> _lastHeroes;
	private static List<UserProfileData.HeroSaveData> _lastSaves;
	// 창의 탭 캔버스 (관계·전투 기술·상태) → 다시 채워도 보던 탭을 유지하려고 찾는다
	private static readonly string[] TAB_CANVAS_FIELDS = { "_rlsCanvas", "_skillCanvas", "_stsCanvas" };
	#endregion

	// 에디터에서 도메인 리로드를 꺼 둬도 지난 재생의 창을 붙잡고 있지 않게
	[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetOnPlay()
	{
		_opened = null;
		_hookedButtons.Clear();
		_inventory = null;
		_openedInventory = false;
		_runtimeHeroes.Clear();
		_runtimeRoot = null;
		_runtimeKey = null;
		_lastHeroes = null;
		_lastSaves = null;
	}

	// 여기서 연 창이 지금 열려 있는지
	public static bool IsOpen => _opened != null && IsVisible(_opened);

	// 전투 밖에는 전투용 영웅(CEntityRuntimeData)이 없다 → 세이브 값으로 창에 넘길 영웅을 만든다
	//  ㄴ heroes 와 saves 는 같은 순서, 세이브 값·장신구가 그대로면 지난번 것을 다시 쓴다
	//  ㄴ 왜? → 만들 때마다 전투 쪽 초기화 로그·토큰 로드 오류가 영웅 수만큼 찍힌다
	public static bool Open(List<CEntityBasicDataSO> heroes, List<UserProfileData.HeroSaveData> saves, int index)
	{
		if (heroes == null || saves == null || heroes.Count != saves.Count || index < 0 || index >= heroes.Count)
		{
			return false;
		}

		_lastHeroes = new List<CEntityBasicDataSO>(heroes);
		_lastSaves = new List<UserProfileData.HeroSaveData>(saves);

		string key = MakeRuntimeKey(heroes, saves);

		if (key != _runtimeKey || !IsRuntimeAlive())
		{
			ClearRuntimeHeroes();

			for (int i = 0; i < heroes.Count; i++)
			{
				CEntityRuntimeData hero = CreateRuntimeHero(heroes[i], saves[i]);

				if (hero != null)
				{
					_runtimeHeroes.Add(hero);
				}
			}

			_runtimeKey = key;
		}

		int targetIndex = -1;

		for (int i = 0; i < _runtimeHeroes.Count; i++)
		{
			if (_runtimeHeroes[i].HeroBasicData == heroes[index])
			{
				targetIndex = i;
				break;
			}
		}

		if (targetIndex < 0)
		{
			return false;
		}

		// 창은 넘긴 목록을 붙잡고 있다 → 다시 만들 때 창 쪽 목록까지 비지 않게 복사해서 넘긴다
		return Open(new List<CEntityRuntimeData>(_runtimeHeroes), targetIndex);
	}

	// 넘긴 순서대로 창의 이전·다음 버튼이 돈다
	public static bool Open(List<CEntityRuntimeData> heroes, int index)
	{
		HeroPopup_UI popup = HeroPopup_UI.instance;

		if (popup == null)
		{
			CPrint.Warn("CHeroPopupLink : 영웅 정보 창이 없습니다. (교차로를 거쳐야 만들어집니다)");
			return false;
		}

		if (heroes == null || index < 0 || index >= heroes.Count || heroes[index] == null)
		{
			return false;
		}

		popup.OpenPopupExceptTheCrossRoad(heroes, index);
		SetVisible(popup, true);
		HookCloseButtons(popup);

		// 장신구 칸에 끌어다 놓기를 붙이고, 옆에 짐칸을 연다
		if (popup.GetComponent<CHeroPopupTrinkets>() == null)
		{
			popup.gameObject.AddComponent<CHeroPopupTrinkets>();
		}

		if (_opened == null)
		{
			ShowInventory();
			CHeroTrinkets.OnChanged -= HandleTrinketsChanged;
			CHeroTrinkets.OnChanged += HandleTrinketsChanged;
			CRunSound.PlayOpenUI();
		}

		_opened = popup;

		return true;
	}

	public static void Close()
	{
		if (_opened == null)
		{
			return;
		}

		SetVisible(_opened, false);
		Release();
		CRunSound.PlayCloseUI();
	}

	// 여는 쪽이 매 프레임 부른다 → ESC 로 닫고, 다른 쪽에서 닫혔으면 정리한다
	public static void Tick()
	{
		if (_opened == null)
		{
			return;
		}

		if (!IsVisible(_opened))
		{
			Release();
			return;
		}

		if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
		{
			Close();
		}
	}

	private static void Release()
	{
		for (int i = 0; i < _hookedButtons.Count; i++)
		{
			if (_hookedButtons[i] != null)
			{
				_hookedButtons[i].onClick.RemoveListener(Close);
			}
		}

		_hookedButtons.Clear();
		_opened = null;
		CHeroTrinkets.OnChanged -= HandleTrinketsChanged;
		HideInventory();
	}

	#region 전투 밖 영웅
	// 장신구를 끼우거나 뗐다 → 능력치를 다시 계산해 보던 영웅·탭 그대로 창을 다시 채운다
	private static void HandleTrinketsChanged(string heroId)
	{
		if (_opened == null || _lastHeroes == null || _lastSaves == null)
		{
			return;
		}

		int index = _lastHeroes.IndexOf(_opened.CurrentHeroDataSO);

		if (index < 0)
		{
			return;
		}

		int tab = GetVisibleTab(_opened);

		Open(_lastHeroes, _lastSaves, index);

		switch (tab)
		{
			case 0:
				_opened.OnClickRelationTab();
				break;
			case 2:
				_opened.OnClickStatusTab();
				break;
		}
	}

	// 창은 다시 채울 때 전투 기술 탭으로 돌아간다 → 지금 보이는 탭 번호를 찾아 둔다 (못 찾으면 -1)
	private static int GetVisibleTab(HeroPopup_UI popup)
	{
		const System.Reflection.BindingFlags FLAGS = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

		for (int i = 0; i < TAB_CANVAS_FIELDS.Length; i++)
		{
			System.Reflection.FieldInfo field = typeof(HeroPopup_UI).GetField(TAB_CANVAS_FIELDS[i], FLAGS);
			UnityEngine.CanvasGroup group = field != null ? field.GetValue(popup) as UnityEngine.CanvasGroup : null;

			if (group != null && group.alpha > 0f)
			{
				return i;
			}
		}

		return -1;
	}

	// 전투 스포너와 같은 순서로 채운다 (세이브 체력·스트레스, 자리 → 랭크) + 끼운 장신구 능력치
	private static CEntityRuntimeData CreateRuntimeHero(CEntityBasicDataSO data, UserProfileData.HeroSaveData save)
	{
		if (data == null || save == null || save.CurrentHP <= 0)
		{
			return null;
		}

		if (_runtimeRoot == null)
		{
			// 꺼 둔 채로 두면 아래 영웅들의 Awake·Update 가 돌지 않는다
			_runtimeRoot = new UnityEngine.GameObject("HeroPopupRuntimeHeroes");
			_runtimeRoot.SetActive(false);
			UnityEngine.Object.DontDestroyOnLoad(_runtimeRoot);
		}

		UnityEngine.GameObject go = new UnityEngine.GameObject(data.HeroID);

		go.transform.SetParent(_runtimeRoot.transform, false);

		CEntityRuntimeData hero = go.AddComponent<CEntityRuntimeData>();

		hero.Init(data, ToRank(save.PartySlotIndex), save.CurrentHP, save.CurrentStress);
		CHeroTrinketEffects.Apply(hero, false);

		return hero;
	}

	private static ERank ToRank(int partySlotIndex)
	{
		switch (partySlotIndex)
		{
			case 0:
				return ERank.One;
			case 1:
				return ERank.Two;
			case 2:
				return ERank.Three;
			case 3:
				return ERank.Four;
		}

		return ERank.None;
	}

	// 영웅·자리·체력·스트레스·끼운 장신구를 한 줄로 이어 붙인다
	private static string MakeRuntimeKey(List<CEntityBasicDataSO> heroes, List<UserProfileData.HeroSaveData> saves)
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();

		for (int i = 0; i < heroes.Count; i++)
		{
			CEntityBasicDataSO data = heroes[i];
			UserProfileData.HeroSaveData save = saves[i];

			if (data == null || save == null)
			{
				sb.Append("-|");
				continue;
			}

			sb.Append(data.HeroID).Append(',').Append(save.PartySlotIndex).Append(',').Append(save.CurrentHP).Append(',').Append(save.CurrentStress);

			List<CTrinketItemData> trinkets = CHeroTrinkets.GetEquipped(data.HeroID);

			for (int j = 0; j < trinkets.Count; j++)
			{
				sb.Append(',').Append(trinkets[j] != null ? trinkets[j].Id : "-");
			}

			sb.Append('|');
		}

		return sb.ToString();
	}

	// 씬을 오가며 누가 지웠는지
	private static bool IsRuntimeAlive()
	{
		if (_runtimeRoot == null)
		{
			return false;
		}

		for (int i = 0; i < _runtimeHeroes.Count; i++)
		{
			if (_runtimeHeroes[i] == null)
			{
				return false;
			}
		}

		return true;
	}

	private static void ClearRuntimeHeroes()
	{
		for (int i = 0; i < _runtimeHeroes.Count; i++)
		{
			if (_runtimeHeroes[i] != null)
			{
				UnityEngine.Object.Destroy(_runtimeHeroes[i].gameObject);
			}
		}

		_runtimeHeroes.Clear();
		_runtimeKey = null;
	}
	#endregion

	#region 짐칸
	// 정보 창 옆(오른쪽)에 짐칸을 열고 창보다 위로 올린다
	private static void ShowInventory()
	{
		_inventory = UnityEngine.Object.FindFirstObjectByType<CInventoryView>(UnityEngine.FindObjectsInactive.Include);

		if (_inventory == null)
		{
			return;
		}

		UnityEngine.Canvas canvas = _inventory.GetComponent<UnityEngine.Canvas>();

		_addedCanvas = canvas == null;

		if (_addedCanvas)
		{
			canvas = _inventory.gameObject.AddComponent<UnityEngine.Canvas>();
			_inventory.gameObject.AddComponent<GraphicRaycaster>();
		}

		_prevOverrideSorting = canvas.overrideSorting;
		_prevSortingOrder = canvas.sortingOrder;
		canvas.overrideSorting = true;
		canvas.sortingOrder = INVENTORY_SORTING_ORDER;

		_openedInventory = !_inventory.IsOpen;

		if (_openedInventory)
		{
			_inventory.Open();
		}
	}

	private static void HideInventory()
	{
		if (_inventory == null)
		{
			return;
		}

		if (_openedInventory)
		{
			_inventory.Close();
		}

		UnityEngine.Canvas canvas = _inventory.GetComponent<UnityEngine.Canvas>();

		if (canvas != null)
		{
			// 정비공 화면처럼 다른 쪽이 붙여 둔 캔버스면 원래 값으로, 여기서 붙였으면 정렬만 끈다
			canvas.overrideSorting = !_addedCanvas && _prevOverrideSorting;
			canvas.sortingOrder = _addedCanvas ? 0 : _prevSortingOrder;
		}

		_inventory = null;
		_openedInventory = false;
	}
	#endregion

	// 교차로 매니저가 숨기던 방식 그대로
	private static void SetVisible(HeroPopup_UI popup, bool visible)
	{
		UnityEngine.CanvasGroup group = popup.GetComponent<UnityEngine.CanvasGroup>();

		if (group == null)
		{
			popup.gameObject.SetActive(visible);
			return;
		}

		group.alpha = visible ? 1f : 0f;
		group.blocksRaycasts = visible;
		group.interactable = visible;
	}

	private static bool IsVisible(HeroPopup_UI popup)
	{
		UnityEngine.CanvasGroup group = popup.GetComponent<UnityEngine.CanvasGroup>();

		return group != null ? group.alpha > 0f && group.blocksRaycasts : popup.gameObject.activeInHierarchy;
	}

	private static void HookCloseButtons(HeroPopup_UI popup)
	{
		Button[] buttons = popup.GetComponentsInChildren<Button>(true);

		for (int i = 0; i < buttons.Length; i++)
		{
			Button button = buttons[i];

			if (button == null || _hookedButtons.Contains(button) || !CallsMethod(button.onClick, CROSSROAD_CLOSE_METHOD))
			{
				continue;
			}

			button.onClick.AddListener(Close);
			_hookedButtons.Add(button);
		}
	}

	private static bool CallsMethod(UnityEvent unityEvent, string method)
	{
		for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
		{
			if (unityEvent.GetPersistentMethodName(i) == method)
			{
				return true;
			}
		}

		return false;
	}
}

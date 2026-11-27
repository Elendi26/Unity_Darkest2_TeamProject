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
	#endregion

	// 에디터에서 도메인 리로드를 꺼 둬도 지난 재생의 창을 붙잡고 있지 않게
	[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetOnPlay()
	{
		_opened = null;
		_hookedButtons.Clear();
		_inventory = null;
		_openedInventory = false;
	}

	// 여기서 연 창이 지금 열려 있는지
	public static bool IsOpen => _opened != null && IsVisible(_opened);

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
		HideInventory();
	}

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

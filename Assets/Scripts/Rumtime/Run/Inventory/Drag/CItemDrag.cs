using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 아이템을 어디서 끌어왔는지
public enum EItemDragSource
{
	None = 0,
	Inventory,	// 짐칸 칸
	Coach,		// 마차 정비공 칸 (단 업그레이드)
	HeroTrinket,	// 영웅 장신구 칸
}

// 끌고 있는 아이템 하나 → 놓는 곳(CItemDropZone)이 보고 받을지 정한다
public class CItemDragPayload
{
	public EItemDragSource Source;
	public CItemDataBase Item;
	// 짐칸에서 끌었을 때
	public CInventory Inventory;
	public int InventoryIndex = -1;
	// 영웅 장신구 칸에서 끌었을 때
	public string HeroId;
	public int HeroSlot = -1;
}

// 아이템 끌어다 놓기 → 지금 끄는 아이템 하나 + 마우스를 따라다니는 아이콘
public static class CItemDrag
{
	#region 상수
	// 영웅 정보 창(300)·짐칸보다 위
	private const int GHOST_SORTING_ORDER = 1000;
	private const float GHOST_SIZE = 96f;
	private const float GHOST_ALPHA = 0.85f;
	#endregion

	#region 내부 변수
	private static CItemDragPayload _current;
	private static RectTransform _ghostCanvas;
	private static Image _ghost;
	#endregion

	// 끄는 중인 아이템 (없으면 null)
	public static CItemDragPayload Current => _current;
	public static bool IsDragging => _current != null;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetOnPlay()
	{
		_current = null;
		_ghostCanvas = null;
		_ghost = null;
	}

	public static void Begin(CItemDragPayload payload, Sprite icon, PointerEventData eventData)
	{
		_current = payload;
		EnsureGhost();

		_ghost.sprite = icon;
		_ghost.enabled = icon != null;
		_ghostCanvas.gameObject.SetActive(true);
		Move(eventData);
	}

	public static void Move(PointerEventData eventData)
	{
		if (_ghostCanvas == null || eventData == null)
		{
			return;
		}

		if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_ghostCanvas, eventData.position, null, out Vector2 local))
		{
			_ghost.rectTransform.anchoredPosition = local;
		}
	}

	// 놓기 처리(OnDrop)가 끝난 뒤 불린다 → 놓은 곳이 없으면 아무 일 없이 제자리
	public static void End()
	{
		_current = null;

		if (_ghostCanvas != null)
		{
			_ghostCanvas.gameObject.SetActive(false);
		}
	}

	private static void EnsureGhost()
	{
		if (_ghostCanvas != null)
		{
			return;
		}

		_ghostCanvas = CUIBuilder.CreateOverlayCanvas("ItemDragCanvas", null, GHOST_SORTING_ORDER, false);
		Object.DontDestroyOnLoad(_ghostCanvas.gameObject);

		// 아이콘은 마우스 클릭을 받지 않는다 → 아래 놓는 곳이 잡히게
		_ghost = CUIBuilder.CreateImage("Ghost", _ghostCanvas, null, new Color(1f, 1f, 1f, GHOST_ALPHA));
		_ghost.preserveAspect = true;
		_ghost.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		_ghost.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		_ghost.rectTransform.sizeDelta = new Vector2(GHOST_SIZE, GHOST_SIZE);

		CanvasGroup group = _ghostCanvas.gameObject.AddComponent<CanvasGroup>();

		group.blocksRaycasts = false;
		group.interactable = false;
	}
}

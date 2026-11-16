using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CInventoryView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CStagecoachController _controller;
	[SerializeField] private CanvasGroup _group;
	// 지금 고른 탭 이름 (모든 아이템 / 전투 ...)
	[SerializeField] private Text _titleText;
	[SerializeField] private Text _slotCountText;
	// 분류 탭 → 첫 칸이 기본 탭
	[SerializeField] private CInventoryTab[] _tabs;
	[SerializeField] private Color _tabOnColor = new Color(0.95f, 0.85f, 0.6f, 1f);
	[SerializeField] private Color _tabOffColor = new Color(0.45f, 0.42f, 0.38f, 1f);
	// 분류 탭에서 빈 칸을 채울 때 한 줄 칸 수
	[SerializeField] private int _columns = 3;
	// 패널 아래 자원 표시
	[SerializeField] private CRunValueView _goldView;
	[SerializeField] private CRunValueView _factionView;
	[SerializeField] private RectTransform _slotRoot;
	[SerializeField] private CInventorySlotView _slotTemplate;
	[SerializeField] private Text _detailNameText;
	[SerializeField] private Text _detailDescText;
	[SerializeField] private Button _useButton;
	[SerializeField] private Button _dropButton;
	[SerializeField] private Button _closeButton;
	[SerializeField] private Button _openButton;
	[SerializeField] private InputAction _toggleAction = new InputAction("ToggleInventory", InputActionType.Button, "<Keyboard>/i");
	[SerializeField] private float _slideDuration = 0.25f;
	[SerializeField] private Vector2 _hiddenOffset = new Vector2(0f, -1100f);
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private readonly List<CInventorySlotView> _slots = new List<CInventorySlotView>();
	// 칸 번호 → 아이템 번호 (-1 이면 빈 칸)
	private readonly List<int> _visible = new List<int>();
	private int _tab;
	private RectTransform _rect;
	private Vector2 _shownPosition;
	private Coroutine _slideRoutine;
	private float _progress;
	private CInventory _inventory;
	private CRunContext _ctx;
	private int _selected = -1;
	private bool _isOpen;
	#endregion

	public bool IsOpen => _isOpen;

	private void Awake()
	{
		_rect = transform as RectTransform;

		// 씬에 배치한 위치를 열린 위치로 기억해 둔다
		if (_rect != null)
		{
			_shownPosition = _rect.anchoredPosition;
		}

		// 숨김은 CanvasGroup 으로 → 오브젝트를 끄면 I 키 입력도 꺼지니까
		if (_group == null)
		{
			_group = GetComponent<CanvasGroup>();
		}

		if (_group == null)
		{
			_group = gameObject.AddComponent<CanvasGroup>();
		}

		if (_slotTemplate != null)
		{
			_slotTemplate.gameObject.SetActive(false);
		}

		AddListener(_useButton, Use);
		AddListener(_dropButton, Drop);
		AddListener(_closeButton, Close);
		AddListener(_openButton, Toggle);
		BindTabs();

		SetVisible(false);
	}

	private void OnEnable()
	{
		if (_toggleAction == null)
		{
			return;
		}

		_toggleAction.performed += HandleTogglePerformed;
		_toggleAction.Enable();
	}

	private void OnDisable()
	{
		if (_toggleAction != null)
		{
			_toggleAction.performed -= HandleTogglePerformed;
			_toggleAction.Disable();
		}

		if (_slideRoutine != null)
		{
			StopSlide();
			ApplyProgress(_isOpen ? 1f : 0f);
			SetInteractable(_isOpen);
		}
	}

	private void Start()
	{
		if (_controller == null)
		{
			_controller = FindFirstObjectByType<CStagecoachController>();
		}

		if (_controller == null || _controller.Stagecoach == null)
		{
			CPrint.Warn("CInventoryView : 역마차가 없습니다. 컨트롤러와 Config 를 확인하세요.");
			return;
		}

		_inventory = _controller.Stagecoach.Inventory;
		_ctx = _controller.Context;
		_inventory.OnChanged += Refresh;

		if (_controller.RunData != null)
		{
			BindValue(_goldView, _controller.RunData.Get(ERunValue.Gold));
			BindValue(_factionView, _controller.RunData.Get(ERunValue.Faction));
		}
	}

	private static void BindValue(CRunValueView view, CRunValue value)
	{
		if (view != null)
		{
			view.Bind(value);
		}
	}

	#region 분류 탭
	private void BindTabs()
	{
		if (_tabs == null)
		{
			return;
		}

		for (int i = 0; i < _tabs.Length; i++)
		{
			if (_tabs[i] == null || _tabs[i].Button == null)
			{
				continue;
			}

			int tab = i;

			_tabs[i].Button.onClick.AddListener(() => SelectTab(tab));
		}

		RefreshTabs();
	}

	private void SelectTab(int tab)
	{
		if (_tab == tab)
		{
			return;
		}

		_tab = tab;
		_selected = -1;
		RefreshTabs();
		Refresh();
	}

	private void RefreshTabs()
	{
		if (_tabs == null)
		{
			return;
		}

		for (int i = 0; i < _tabs.Length; i++)
		{
			if (_tabs[i] != null && _tabs[i].Icon != null)
			{
				_tabs[i].Icon.color = i == _tab ? _tabOnColor : _tabOffColor;
			}
		}

		if (_titleText != null)
		{
			CInventoryTab current = GetTab();

			_titleText.text = current != null ? current.Title : "모든 아이템";
		}
	}

	private CInventoryTab GetTab()
	{
		return _tabs != null && _tab >= 0 && _tab < _tabs.Length ? _tabs[_tab] : null;
	}

	// 지금 탭에 보일 칸 목록을 만든다
	//  ㄴ 전체 탭 → 짐칸 수만큼 (빈 칸 포함), 분류 탭 → 맞는 아이템 + 줄을 채울 빈 칸
	private void BuildVisible(IReadOnlyList<CItemStack> stacks)
	{
		_visible.Clear();

		CInventoryTab tab = GetTab();
		bool showAll = tab == null || tab.IsAll;

		for (int i = 0; i < stacks.Count; i++)
		{
			CItemData item = stacks[i] != null ? stacks[i].Item : null;

			if (showAll || (item != null && item.Category == tab.Category))
			{
				_visible.Add(i);
			}
		}

		int columns = Mathf.Max(1, _columns);
		int target = showAll ? Mathf.Max(_inventory.SlotCount, _visible.Count) : Mathf.Max(columns, Mathf.CeilToInt(_visible.Count / (float)columns) * columns);

		while (_visible.Count < target)
		{
			_visible.Add(-1);
		}
	}
	#endregion

	private void OnDestroy()
	{
		if (_inventory != null)
		{
			_inventory.OnChanged -= Refresh;
		}
	}

	private void HandleTogglePerformed(InputAction.CallbackContext context)
	{
		Toggle();
	}

	public void Toggle()
	{
		SetOpen(!_isOpen);
	}

	public void Open()
	{
		SetOpen(true);
	}

	public void Close()
	{
		SetOpen(false);
	}

	private void SetOpen(bool isOpen)
	{
		if (_isOpen == isOpen)
		{
			return;
		}

		if (isOpen && _inventory == null)
		{
			CPrint.Warn("CInventoryView : 인벤토리가 준비되지 않았습니다.");
			return;
		}

		_isOpen = isOpen;

		if (_logState)
		{
			CPrint.Log($"CInventoryView : {(isOpen ? "열기" : "닫기")}");
		}

		if (isOpen)
		{
			_selected = -1;
			Refresh();
		}

		SetVisible(isOpen);
	}

	private void Refresh()
	{
		if (!_isOpen || _inventory == null)
		{
			return;
		}

		if (_slotRoot == null || _slotTemplate == null)
		{
			CPrint.Error("CInventoryView : Slot Root / Slot Template 이 비어 있습니다.");
			return;
		}

		IReadOnlyList<CItemStack> stacks = _inventory.Stacks;

		if (_selected >= stacks.Count)
		{
			_selected = -1;
		}

		BuildVisible(stacks);

		int count = _visible.Count;

		for (int i = 0; i < count; i++)
		{
			while (_slots.Count <= i)
			{
				CInventorySlotView created = Instantiate(_slotTemplate, _slotRoot);

				created.name = $"Slot_{_slots.Count}";
				_slots.Add(created);
			}

			int stackIndex = _visible[i];
			CItemStack stack = stackIndex >= 0 ? stacks[stackIndex] : null;

			_slots[i].gameObject.SetActive(true);
			_slots[i].Bind(stack, stackIndex >= 0 && stackIndex == _selected, () => Select(stackIndex));
		}

		for (int i = count; i < _slots.Count; i++)
		{
			_slots[i].gameObject.SetActive(false);
		}

		if (_slotCountText != null)
		{
			_slotCountText.text = $"{_inventory.UsedSlots} / {_inventory.SlotCount}";
		}

		RefreshDetail();
	}

	private void RefreshDetail()
	{
		CItemStack stack = _selected >= 0 ? _inventory.Stacks[_selected] : null;
		CItemData item = stack != null ? stack.Item : null;

		if (_detailNameText != null)
		{
			_detailNameText.text = item != null ? $"{item.DisplayName}  x{stack.Count}" : string.Empty;
		}

		if (_detailDescText != null)
		{
			_detailDescText.text = GetDetailText(item);
		}

		if (_useButton != null)
		{
			_useButton.interactable = _ctx != null && _inventory.CanUse(_selected, _ctx.RunData);
		}

		if (_dropButton != null)
		{
			_dropButton.interactable = item != null;
		}
	}

	private string GetDetailText(CItemData item)
	{
		if (item == null)
		{
			return "아이템을 고르세요.";
		}

		if (!item.IsUsable)
		{
			return $"{item.Description}\n\n(보관만 하는 아이템)";
		}

		if (_ctx != null && CInventory.IsBlockedByFull(item, _ctx.RunData))
		{
			return $"{item.Description}\n\n(가득 차 있어 지금은 쓸 수 없다)";
		}

		return item.Description;
	}

	private void Select(int index)
	{
		_selected = index;
		Refresh();
	}

	private void Use()
	{
		if (!_isOpen || _inventory == null)
		{
			return;
		}

		_inventory.TryUse(_selected, _ctx);
	}

	private void Drop()
	{
		if (!_isOpen || _inventory == null || _selected < 0)
		{
			return;
		}

		CItemData item = _inventory.Stacks[_selected].Item;

		if (_inventory.Remove(_selected, 1) && _logState)
		{
			CPrint.Log($"CInventoryView : {(item != null ? item.DisplayName : "?")} 1개 버림");
		}
	}

	private void SetVisible(bool isVisible)
	{
		if (!isVisible)
		{
			SetInteractable(false);
		}

		StopSlide();

		if (_slideDuration <= 0f || !isActiveAndEnabled)
		{
			ApplyProgress(isVisible ? 1f : 0f);
			SetInteractable(isVisible);
			return;
		}

		_slideRoutine = StartCoroutine(CoSlide(isVisible ? 1f : 0f));
	}

	private IEnumerator CoSlide(float target)
	{
		float start = _progress;

		float duration = _slideDuration * Mathf.Abs(target - start);
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;

			float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

			ApplyProgress(Mathf.Lerp(start, target, t));
			yield return null;
		}

		ApplyProgress(target);

		if (_isOpen)
		{
			SetInteractable(true);
		}

		_slideRoutine = null;
	}

	private void StopSlide()
	{
		if (_slideRoutine == null)
		{
			return;
		}

		StopCoroutine(_slideRoutine);
		_slideRoutine = null;
	}

	private void ApplyProgress(float progress)
	{
		_progress = progress;

		if (_rect != null)
		{
			_rect.anchoredPosition = Vector2.Lerp(_shownPosition + _hiddenOffset, _shownPosition, progress);
		}

		_group.alpha = progress;
	}

	private void SetInteractable(bool isInteractable)
	{
		_group.interactable = isInteractable;
		_group.blocksRaycasts = isInteractable;
	}

	private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
	{
		if (button != null)
		{
			button.onClick.AddListener(action);
		}
	}
}

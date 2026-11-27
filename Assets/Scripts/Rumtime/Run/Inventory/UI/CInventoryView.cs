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
	[SerializeField] private Text _titleText;
	[SerializeField] private Text _slotCountText;
	[SerializeField] private CInventoryTab[] _tabs;
	[SerializeField] private Color _tabOnColor = new Color(0.95f, 0.85f, 0.6f, 1f);
	[SerializeField] private Color _tabOffColor = new Color(0.45f, 0.42f, 0.38f, 1f);
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
	private IInventoryUseHandler _useHandler;
	private Text _useLabel;
	private string _defaultUseLabel;
	#endregion

	public bool IsOpen => _isOpen;

	// 다른 화면이 '사용' 버튼을 맡는다 (null 이면 원래대로)
	public void SetUseHandler(IInventoryUseHandler handler)
	{
		_useHandler = handler;
		Refresh();
	}

	// 고른 아이템을 다른 화면이 맡고 있는지
	private bool IsHandled(CItemDataBase item)
	{
		return _useHandler != null && item != null && _useHandler.Handles(item);
	}

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

		if (_useButton != null)
		{
			_useLabel = _useButton.GetComponentInChildren<Text>(true);
			_defaultUseLabel = _useLabel != null ? _useLabel.text : null;
		}
		AddListener(_dropButton, Drop);
		SetupDropZone();
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
	private void BuildVisible(IReadOnlyList<CItemStack> stacks)
	{
		_visible.Clear();

		CInventoryTab tab = GetTab();
		bool showAll = tab == null || tab.IsAll;

		for (int i = 0; i < stacks.Count; i++)
		{
			CItemDataBase item = stacks[i] != null ? stacks[i].Item : null;

			if (showAll || (item != null && item.Category == tab.Category))
			{
				_visible.Add(i);
			}
		}

		// 남은 빈칸은 탭과 상관없이 모두 보여 준다 → 칸 수로 더 넣을 수 있는 자리를 바로 안다
		int freeSlots = Mathf.Max(0, _inventory.SlotCount - _inventory.UsedSlots);
		int target = showAll ? Mathf.Max(_inventory.SlotCount, _visible.Count) : _visible.Count + freeSlots;

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

		if (isOpen)
		{
			_selected = -1;
			Refresh();
			CRunSound.PlayOpenUI();
		}
		else
		{
			CRunSound.PlayCloseUI();
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
			_slots[i].SetDrag(stackIndex >= 0 ? () => MakeDragPayload(stackIndex) : (System.Func<CItemDragPayload>)null);
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
		CItemDataBase item = stack != null ? stack.Item : null;

		if (_detailNameText != null)
		{
			_detailNameText.text = item != null ? $"{item.DisplayName}  x{stack.Count}" : string.Empty;
		}

		if (_detailDescText != null)
		{
			_detailDescText.text = GetDetailText(item);
		}

		bool isHandled = IsHandled(item);

		if (_useButton != null)
		{
			_useButton.interactable = isHandled ? _useHandler.CanUse(_selected) : _ctx != null && _inventory.CanUse(_selected, _ctx.RunData);
		}

		if (_useLabel != null)
		{
			_useLabel.text = isHandled ? _useHandler.UseLabel : _defaultUseLabel;
		}

		if (_dropButton != null)
		{
			_dropButton.interactable = item != null;
		}
	}

	private string GetDetailText(CItemDataBase item)
	{
		if (item == null)
		{
			return "아이템을 고르세요.";
		}

		// 종류별 안내 한 줄 (보관만 / 가득 차 못 씀 / 장신구 등) 은 아이템이 정한다
		string note = item.GetDetailNote(_ctx != null ? _ctx.RunData : null);

		return string.IsNullOrEmpty(note) ? item.FullDescription : $"{item.FullDescription}\n\n{note}";
	}

	#region 끌어다 놓기
	// 짐칸 칸에서 끌 때 드는 것
	private CItemDragPayload MakeDragPayload(int stackIndex)
	{
		if (!_isOpen || _inventory == null || stackIndex < 0 || stackIndex >= _inventory.Stacks.Count)
		{
			return null;
		}

		return new CItemDragPayload
		{
			Source = EItemDragSource.Inventory,
			Item = _inventory.Stacks[stackIndex].Item,
			Inventory = _inventory,
			InventoryIndex = stackIndex,
		};
	}

	// 짐칸 패널 전체가 놓는 곳 → 마차 칸·영웅 장신구 칸에서 끌어와 놓으면 떼어 짐칸으로
	private void SetupDropZone()
	{
		CItemDropZone zone = gameObject.AddComponent<CItemDropZone>();

		zone.Setup(CanAcceptDrop, AcceptDrop);
	}

	private bool CanAcceptDrop(CItemDragPayload payload)
	{
		if (!_isOpen || _inventory == null)
		{
			return false;
		}

		switch (payload.Source)
		{
			case EItemDragSource.Coach:
				return _controller != null && _controller.Stagecoach != null && _controller.Stagecoach.CanUnequipToInventory(payload.Item as CStagecoachUpgradeData);
			case EItemDragSource.HeroTrinket:
				return CHeroTrinkets.CanUnequipToInventory(payload.HeroId, payload.HeroSlot, _inventory);
		}

		return false;
	}

	private void AcceptDrop(CItemDragPayload payload)
	{
		bool done = false;

		switch (payload.Source)
		{
			case EItemDragSource.Coach:
				done = _controller.Stagecoach.TryUnequipToInventory(payload.Item as CStagecoachUpgradeData);
				break;
			case EItemDragSource.HeroTrinket:
				done = CHeroTrinkets.TryUnequipToInventory(payload.HeroId, payload.HeroSlot, _inventory);
				break;
		}

		if (done && _logState)
		{
			CPrint.Log($"CInventoryView : {payload.Item.DisplayName} 떼어 짐칸으로");
		}
	}
	#endregion

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

		CItemDataBase item = _selected >= 0 && _selected < _inventory.Stacks.Count ? _inventory.Stacks[_selected].Item : null;

		if (IsHandled(item))
		{
			if (_useHandler.CanUse(_selected))
			{
				_useHandler.Use(_selected);
			}

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

		CItemDataBase item = _inventory.Stacks[_selected].Item;

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

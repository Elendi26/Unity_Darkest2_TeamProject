using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class CMapPanelToggle : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private InputAction _toggleAction = new InputAction("ToggleMinimap", InputActionType.Button, "<Keyboard>/m");
	[SerializeField] private bool _startOpen = false;
	[SerializeField] private float _duration = 0.25f;
	[SerializeField] private Vector2 _hiddenOffset = new Vector2(0f, -1100f);
	[SerializeField] private bool _logState = false;
	#endregion

	#region 이벤트
	public event Action<bool> OnToggled;
	#endregion

	#region 내부 변수
	private RectTransform _rect;
	private CanvasGroup _canvasGroup;
	private Vector2 _shownPosition;
	private Coroutine _moveRoutine;
	private float _progress;
	private bool _isOpen;
	#endregion

	public bool IsOpen => _isOpen;

	private void Awake()
	{
		_rect = (RectTransform)transform;
		_canvasGroup = GetComponent<CanvasGroup>();

		// 씬에 배치한 위치를 열린 위치로 기억해 둔다
		_shownPosition = _rect.anchoredPosition;

		_isOpen = _startOpen;
		ApplyProgress(_startOpen ? 1f : 0f);
		SetInteractable(_startOpen);
	}

	// 입력 구독·활성화는 OnEnable, 해제는 OnDisable 에서 짝으로
	private void OnEnable()
	{
		if (_toggleAction == null)
		{
			CPrint.Warn("CMapPanelToggle : Toggle Action 이 없습니다.");
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

		// 움직이는 도중에 꺼지면 목표 위치로 바로 맞춰 둔다
		StopMove();
		ApplyProgress(_isOpen ? 1f : 0f);
		SetInteractable(_isOpen);
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

		_isOpen = isOpen;

		if (_logState)
		{
			CPrint.Log($"CMapPanelToggle : {(isOpen ? "열기" : "닫기")}");
		}

		// 닫기 시작하면 바로 클릭을 막는다
		if (!isOpen)
		{
			SetInteractable(false);
		}

		StopMove();

		if (_duration <= 0f || !isActiveAndEnabled)
		{
			ApplyProgress(isOpen ? 1f : 0f);
			SetInteractable(isOpen);
		}
		else
		{
			_moveRoutine = StartCoroutine(CoMove(isOpen ? 1f : 0f));
		}

		OnToggled?.Invoke(_isOpen);
	}

	private IEnumerator CoMove(float target)
	{
		float start = _progress;

		// 반쯤 내려가다 다시 열면 남은 거리만큼만 시간을 쓴다
		float duration = _duration * Mathf.Abs(target - start);
		float elapsed = 0f;

		while (elapsed < duration)
		{
			// 일시정지(timeScale 0) 중에도 열고 닫을 수 있게
			elapsed += Time.unscaledDeltaTime;

			float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

			ApplyProgress(Mathf.Lerp(start, target, t));
			yield return null;
		}

		ApplyProgress(target);

		// 다 올라온 뒤에 클릭을 허용한다
		if (_isOpen)
		{
			SetInteractable(true);
		}

		_moveRoutine = null;
	}

	private void StopMove()
	{
		if (_moveRoutine == null)
		{
			return;
		}

		StopCoroutine(_moveRoutine);
		_moveRoutine = null;
	}

	// 진행도에 맞춰 위치와 투명도를 정한다
	private void ApplyProgress(float progress)
	{
		_progress = progress;
		_rect.anchoredPosition = Vector2.Lerp(_shownPosition + _hiddenOffset, _shownPosition, progress);
		_canvasGroup.alpha = progress;
	}

	private void SetInteractable(bool isInteractable)
	{
		_canvasGroup.interactable = isInteractable;
		_canvasGroup.blocksRaycasts = isInteractable;
	}
}

using UnityEngine;
using UnityEngine.UI;

// 주행 씬 시작 연출 → 검은 화면으로 시작해, 프레임이 안정되면 밝아지고, 다 밝아진 뒤에 출발할 수 있다
public class CDriveIntroFade : MonoBehaviour
{
	#region 인스펙터
	// 최소로 가리고 있는 시간 (초, 실제 시간)
	[SerializeField] private float _holdMinTime = 0.3f;
	// 프레임이 안정되지 않아도 이 시간이 지나면 밝힌다
	[SerializeField] private float _holdMaxTime = 2f;
	// 이보다 짧은 프레임이 연속으로 이어지면 안정된 것으로 본다
	[SerializeField] private float _stableFrameTime = 0.05f;
	[SerializeField] private int _stableFrameCount = 5;
	// 밝아지는 시간
	[SerializeField] private float _fadeTime = 0.6f;
	[SerializeField] private Color _color = Color.black;
	// 주행 UI 위, 일시정지 메뉴(100) 아래
	[SerializeField] private int _sortingOrder = 90;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private enum EIntroState
	{
		Done = 0,
		Holding,
		Fading,
	}

	private EIntroState _state = EIntroState.Done;
	private Image _cover;
	private float _elapsed;
	private int _stableFrames;
	#endregion

	// 연출 중인지 → 끝나기 전에는 마차를 출발시키지 않는다
	public bool IsPlaying => _state != EIntroState.Done;

	private void Awake()
	{
		RectTransform canvas = CUIBuilder.CreateOverlayCanvas("DriveIntroCanvas", transform, _sortingOrder, true);

		_cover = CUIBuilder.CreateImage("Cover", canvas, null, _color);
		_cover.raycastTarget = true;
		_cover.rectTransform.anchorMin = Vector2.zero;
		_cover.rectTransform.anchorMax = Vector2.one;
		_cover.rectTransform.offsetMin = Vector2.zero;
		_cover.rectTransform.offsetMax = Vector2.zero;

		SetState(EIntroState.Holding);

		// 여관 등에서 검게 덮은 채로 넘어왔으면 → 이 연출이 같은 검은 화면으로 이어받는다
		CRunScreenFade.Clear();
	}

	private void Update()
	{
		switch (_state)
		{
			case EIntroState.Holding:
				TickHold();
				break;
			case EIntroState.Fading:
				TickFade();
				break;
		}
	}

	private void TickHold()
	{
		float delta = Time.unscaledDeltaTime;

		_elapsed += delta;
		_stableFrames = delta <= _stableFrameTime ? _stableFrames + 1 : 0;

		bool stable = _elapsed >= _holdMinTime && _stableFrames >= _stableFrameCount;

		if (stable || _elapsed >= _holdMaxTime)
		{
			SetState(EIntroState.Fading);
		}
	}

	private void TickFade()
	{
		_elapsed += Time.unscaledDeltaTime;

		float t = _fadeTime > 0f ? Mathf.Clamp01(_elapsed / _fadeTime) : 1f;
		Color color = _color;

		color.a = _color.a * (1f - t);
		_cover.color = color;

		if (t >= 1f)
		{
			SetState(EIntroState.Done);
		}
	}

	private void SetState(EIntroState next)
	{
		if (_state == next)
		{
			return;
		}

		EIntroState prev = _state;
		float waited = _elapsed;

		_state = next;
		_elapsed = 0f;

		if (_logState)
		{
			CPrint.Log($"CDriveIntroFade : {prev} → {_state} ({waited:0.00}초)");
		}

		switch (_state)
		{
			case EIntroState.Holding:
				_stableFrames = 0;
				_cover.color = _color;
				break;
			case EIntroState.Done:
				// 다 밝아졌으면 화면을 덮던 캔버스는 지운다 → 클릭을 막지 않게
				Destroy(_cover.canvas.gameObject);
				_cover = null;
				break;
		}
	}
}

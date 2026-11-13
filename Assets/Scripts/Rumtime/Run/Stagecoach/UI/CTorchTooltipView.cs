using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 마차 횃불에 마우스를 올리면 화면 위에서 작은 박스가 내려와 횃불 수치를 알려준다
//  ㄴ 횃불에는 충돌체가 없다 → 횃불 위치를 화면 좌표로 바꿔 마우스와의 거리로 판단
public class CTorchTooltipView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private CStagecoachController _controller;
	[SerializeField] private CCoachTorchView _torch;
	[SerializeField] private RectTransform _panel;
	[SerializeField] private CanvasGroup _group;
	[SerializeField] private Text _valueText;
	[SerializeField] private Text _stateText;
	// 수치 막대 (오른쪽 끝을 비율만큼 당겨 채운다)
	[SerializeField] private RectTransform _fill;
	// 횃불 화면 위치에서 이 픽셀 안이면 올린 것으로 본다
	[SerializeField] private float _hoverRadius = 70f;
	// 보일 때·숨을 때 패널 y 위치 (위쪽 기준)
	[SerializeField] private float _shownY = -16f;
	[SerializeField] private float _hiddenY = 140f;
	[SerializeField] private float _slideSpeed = 10f;
	[SerializeField] private bool _logState = false;
	#endregion

	#region 내부 변수
	private CRunValue _value;
	private float _show;
	private bool _isHovered;
	#endregion

	private void Start()
	{
		if (_controller == null)
		{
			_controller = FindFirstObjectByType<CStagecoachController>();
		}

		if (_group == null && _panel != null)
		{
			_group = _panel.GetComponent<CanvasGroup>();
		}

		if (_controller == null || _controller.RunData == null)
		{
			CPrint.Warn("CTorchTooltipView : 런 데이터가 없어 횃불 수치를 보여줄 수 없습니다.");
			enabled = false;
			return;
		}

		_value = _controller.RunData.Get(ERunValue.Torch);

		if (_value != null)
		{
			_value.OnChanged += HandleChanged;
		}

		Refresh();
		Apply(0f);
	}

	private void OnDestroy()
	{
		if (_value != null)
		{
			_value.OnChanged -= HandleChanged;
		}
	}

	private void HandleChanged(ERunValue type, int prev, int current)
	{
		Refresh();
	}

	private void Update()
	{
		bool isHovered = IsHoveringTorch();

		if (isHovered != _isHovered)
		{
			_isHovered = isHovered;

			if (_logState)
			{
				CPrint.Log($"CTorchTooltipView : 횃불 {(isHovered ? "올림" : "벗어남")}");
			}
		}

		_show = Mathf.MoveTowards(_show, _isHovered ? 1f : 0f, _slideSpeed * Time.unscaledDeltaTime);
		Apply(Mathf.SmoothStep(0f, 1f, _show));
	}

	private bool IsHoveringTorch()
	{
		// 마차 횃불은 CDriving3DScene 이 실행 중에 붙일 수 있다 → 없으면 계속 찾는다
		if (_torch == null)
		{
			_torch = FindFirstObjectByType<CCoachTorchView>();
		}

		Camera camera = CChaseCamera.CurrentCamera;
		Mouse mouse = Mouse.current;

		if (_torch == null || camera == null || mouse == null)
		{
			return false;
		}

		Vector3 screen = camera.WorldToScreenPoint(_torch.transform.position);

		if (screen.z <= 0f)
		{
			return false;
		}

		Vector2 delta = mouse.position.ReadValue() - new Vector2(screen.x, screen.y);

		return delta.sqrMagnitude <= _hoverRadius * _hoverRadius;
	}

	private void Apply(float t)
	{
		if (_panel != null)
		{
			_panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, Mathf.Lerp(_hiddenY, _shownY, t));
		}

		if (_group != null)
		{
			_group.alpha = t;
			_group.blocksRaycasts = false;
		}
	}

	private void Refresh()
	{
		if (_value == null)
		{
			return;
		}

		float ratio = _value.Ratio;

		if (_valueText != null)
		{
			_valueText.text = $"{_value.Current} / {_value.Max}";
		}

		if (_stateText != null)
		{
			_stateText.text = GetStateLabel(ratio);
		}

		if (_fill != null)
		{
			_fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), _fill.anchorMax.y);
			_fill.offsetMax = new Vector2(0f, _fill.offsetMax.y);
		}
	}

	// 횃불 밝기 단계 이름
	private static string GetStateLabel(float ratio)
	{
		if (ratio >= 0.75f)
		{
			return "밝음";
		}

		if (ratio >= 0.5f)
		{
			return "보통";
		}

		if (ratio >= 0.25f)
		{
			return "어둑함";
		}

		return "어둠";
	}
}

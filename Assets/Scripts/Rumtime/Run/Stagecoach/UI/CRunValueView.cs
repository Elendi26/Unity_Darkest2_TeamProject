using UnityEngine;
using UnityEngine.UI;

// 횃불·골드처럼 숫자(+막대)로 보여주는 값
public class CRunValueView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Image _fill;
	[SerializeField] private Text _text;
	[SerializeField] private bool _showMax = false;
	[SerializeField] private float _fillSpeed = 2f;
	#endregion

	#region 내부 변수
	private CRunValue _value;
	private float _targetFill;
	#endregion

	public void Bind(CRunValue value)
	{
		UnBind();

		_value = value;

		if (_value == null)
		{
			return;
		}

		_value.OnChanged += HandleChanged;
		Refresh(true);
	}

	public void UnBind()
	{
		if (_value != null)
		{
			_value.OnChanged -= HandleChanged;
		}

		_value = null;
	}

	private void OnDestroy()
	{
		UnBind();
	}

	private void Update()
	{
		if (_fill == null || Mathf.Approximately(_fill.fillAmount, _targetFill))
		{
			return;
		}

		_fill.fillAmount = Mathf.MoveTowards(_fill.fillAmount, _targetFill, _fillSpeed * Time.unscaledDeltaTime);
	}

	private void HandleChanged(ERunValue type, int prev, int current)
	{
		Refresh(false);
	}

	private void Refresh(bool instant)
	{
		if (_value == null)
		{
			return;
		}

		_targetFill = _value.Ratio;

		if (_fill != null && (instant || _fillSpeed <= 0f))
		{
			_fill.fillAmount = _targetFill;
		}

		if (_text != null)
		{
			_text.text = _showMax ? $"{_value.Current}/{_value.Max}" : _value.Current.ToString();
		}
	}
}

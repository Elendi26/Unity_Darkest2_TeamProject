using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 바퀴·장갑처럼 최대치가 작은 값을 칸(pip)으로 보여준다
public class CRunValuePipView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private RectTransform _pipRoot;
	[SerializeField] private Image _pipTemplate;
	// 칸마다 쓸 아이콘 (바퀴·방패 등, 비우면 템플릿 그림 그대로)
	[SerializeField] private Sprite _pipSprite;
	[SerializeField] private Text _countText;
	[SerializeField] private Color _fullColor = new Color(0.95f, 0.8f, 0.45f);
	[SerializeField] private Color _emptyColor = new Color(0.25f, 0.22f, 0.2f, 0.8f);
	[SerializeField] private Color _flashColor = new Color(1f, 0.3f, 0.25f);
	[SerializeField] private float _flashDuration = 0.35f;
	#endregion

	#region 내부 변수
	private readonly List<Image> _pips = new List<Image>();
	private CRunValue _value;
	private Coroutine _flashRoutine;
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
		Refresh();
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

	private void HandleChanged(ERunValue type, int prev, int current)
	{
		Refresh();

		// 줄어든 칸만 잠깐 빨갛게
		if (current < prev)
		{
			StartFlash(current, prev);
		}
	}

	private void Refresh()
	{
		if (_value == null)
		{
			return;
		}

		EnsurePips(_value.Max);

		for (int i = 0; i < _pips.Count; i++)
		{
			_pips[i].color = i < _value.Current ? _fullColor : _emptyColor;
		}

		if (_countText != null)
		{
			_countText.text = $"{_value.Current}/{_value.Max}";
		}
	}

	// 최대치만큼 칸을 만들고 넘치는 칸은 숨긴다
	private void EnsurePips(int count)
	{
		if (_pipRoot == null || _pipTemplate == null)
		{
			CPrint.Once("pip_view_missing", "CRunValuePipView : Pip Root / Pip Template 이 비어 있습니다.");
			return;
		}

		while (_pips.Count < count)
		{
			Image pip = Instantiate(_pipTemplate, _pipRoot);

			pip.name = $"Pip_{_pips.Count}";

			if (_pipSprite != null)
			{
				pip.sprite = _pipSprite;
				pip.preserveAspect = true;
			}

			_pips.Add(pip);
		}

		for (int i = 0; i < _pips.Count; i++)
		{
			_pips[i].gameObject.SetActive(i < count);
		}
	}

	private void StartFlash(int from, int to)
	{
		if (!isActiveAndEnabled)
		{
			return;
		}

		if (_flashRoutine != null)
		{
			StopCoroutine(_flashRoutine);
			Refresh();
		}

		_flashRoutine = StartCoroutine(CoFlash(from, to));
	}

	private IEnumerator CoFlash(int from, int to)
	{
		float elapsed = 0f;

		while (elapsed < _flashDuration)
		{
			elapsed += Time.unscaledDeltaTime;

			Color color = Color.Lerp(_flashColor, _emptyColor, elapsed / _flashDuration);

			for (int i = from; i < to && i < _pips.Count; i++)
			{
				_pips[i].color = color;
			}

			yield return null;
		}

		_flashRoutine = null;
		Refresh();
	}
}

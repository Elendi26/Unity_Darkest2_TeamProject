using System.Collections;
using UnityEngine;

// 길 위에 놓이는 수풀 하나 → 마차가 들이받으면 부풀었다가 사라진다
public class CRoadLootBush : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private float _radius = 1f;
	[SerializeField] private float _burstDuration = 0.3f;
	[SerializeField] private float _burstScale = 1.35f;
	#endregion

	#region 내부 변수
	private bool _isCollected;
	#endregion

	// 길 위 위치 (m) → 뒤로 지나갔는지 판단할 때 쓴다
	public float Arc { get; set; }
	// 부딪힘 판정 반경
	public float Radius => _radius;
	// 이미 터졌는지
	public bool IsCollected => _isCollected;

	public void Collect()
	{
		if (_isCollected)
		{
			return;
		}

		_isCollected = true;
		StartCoroutine(CoBurst());
	}

	private IEnumerator CoBurst()
	{
		Vector3 start = transform.localScale;
		float elapsed = 0f;
		float duration = Mathf.Max(0.01f, _burstDuration);

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;

			float t = Mathf.Clamp01(elapsed / duration);

			// 앞 30% 는 부풀고 → 나머지는 줄어들며 사라진다
			float scale = t < 0.3f
				? Mathf.Lerp(1f, _burstScale, t / 0.3f)
				: Mathf.Lerp(_burstScale, 0f, (t - 0.3f) / 0.7f);

			transform.localScale = start * scale;
			yield return null;
		}

		Destroy(gameObject);
	}
}

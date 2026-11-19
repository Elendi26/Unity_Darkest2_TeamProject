using System.Collections;
using UnityEngine;

// 길을 가로막는 방책 (왼쪽·오른쪽 두 덩어리)
public class CRoadBarricade : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Transform _leftSide;
	[SerializeField] private Transform _rightSide;
	[SerializeField] private ESide _openSide = ESide.None;
	[SerializeField] private float _sinkDepth = 2f;

	[Header("부서질 때 조각이 튕겨 나가는 정도 (x ~ y 사이 무작위)")]
	[SerializeField] private Vector2 _tossSide = new Vector2(3.5f, 6.5f);
	[SerializeField] private Vector2 _tossUp = new Vector2(3f, 5.5f);
	[SerializeField] private Vector2 _tossForward = new Vector2(0.5f, 2.5f);
	[SerializeField] private Vector2 _spinSpeed = new Vector2(180f, 420f);
	[SerializeField] private float _gravity = 18f;
	[SerializeField] private float _restTime = 1.2f;
	[SerializeField] private float _sinkTime = 0.6f;

	[Header("방책을 비추는 불빛 (열리면 꺼진다)")]
	[SerializeField] private Light _light;
	[SerializeField] private float _lightFadeTime = 0.8f;
	[SerializeField] private bool _logState = true;
	#endregion

	public enum ESide
	{
		None = 0,
		Left,
		Right,
	}

	#region 내부 변수
	private bool _isOpen;
	#endregion

	// 한쪽이 치워져 길이 열렸는지
	public bool IsOpen => _isOpen;

	public void Open()
	{
		if (_isOpen)
		{
			return;
		}

		_isOpen = true;

		ESide side = _openSide != ESide.None ? _openSide : (Random.value < 0.5f ? ESide.Left : ESide.Right);
		Transform target = side == ESide.Left ? _leftSide : _rightSide;

		if (target == null)
		{
			CPrint.Warn($"CRoadBarricade : {side} 덩어리가 비어 있어 길을 열 수 없습니다.");
			return;
		}

		// 치운 쪽은 바로 지나갈 수 있게 충돌체부터 끈다
		Collider[] colliders = target.GetComponentsInChildren<Collider>();

		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i] == null)
			{
				continue;
			}

			colliders[i].enabled = false;
		}

		if (_logState)
		{
			CPrint.Log($"CRoadBarricade : 방책 {(side == ESide.Left ? "왼쪽" : "오른쪽")}을 부숴 길을 엶");
		}

		// 왼쪽 덩어리는 길 왼쪽(-right)으로, 오른쪽 덩어리는 오른쪽으로 날아간다
		Vector3 outward = transform.right * (side == ESide.Left ? -1f : 1f);

		StartCoroutine(CoBreak(target, outward));

		if (_light != null)
		{
			StartCoroutine(CoFadeLight());
		}
	}

	// 조각마다 바깥·위·앞으로 튕겨 돌며 떨어지고, 땅에 닿으면 멈췄다가 가라앉는다
	private IEnumerator CoBreak(Transform target, Vector3 outward)
	{
		int count = target.childCount;
		Transform[] pieces = new Transform[count];
		Vector3[] velocities = new Vector3[count];
		Vector3[] spinAxes = new Vector3[count];
		float[] spinSpeeds = new float[count];
		float[] groundY = new float[count];
		bool[] landed = new bool[count];

		for (int i = 0; i < count; i++)
		{
			pieces[i] = target.GetChild(i);
			groundY[i] = pieces[i].position.y;
			velocities[i] = outward * Random.Range(_tossSide.x, _tossSide.y)
				+ Vector3.up * Random.Range(_tossUp.x, _tossUp.y)
				+ transform.forward * Random.Range(_tossForward.x, _tossForward.y);

			// 바깥으로 넘어가는 쪽으로 주로 돌고, 약간 비틀린다
			spinAxes[i] = (Vector3.Cross(Vector3.up, outward) + Random.insideUnitSphere * 0.4f).normalized;
			spinSpeeds[i] = Random.Range(_spinSpeed.x, _spinSpeed.y);
		}

		float time = 0f;

		while (time < _restTime)
		{
			float deltaTime = Time.deltaTime;

			time += deltaTime;

			for (int i = 0; i < count; i++)
			{
				if (pieces[i] == null || landed[i])
				{
					continue;
				}

				velocities[i] += Vector3.down * (_gravity * deltaTime);
				pieces[i].position += velocities[i] * deltaTime;
				pieces[i].rotation = Quaternion.AngleAxis(spinSpeeds[i] * deltaTime, spinAxes[i]) * pieces[i].rotation;

				// 처음 자리 높이까지 떨어지면 그 자리에 눕는다
				if (velocities[i].y < 0f && pieces[i].position.y <= groundY[i])
				{
					Vector3 position = pieces[i].position;

					pieces[i].position = new Vector3(position.x, groundY[i], position.z);
					landed[i] = true;
				}
			}

			yield return null;
		}

		Vector3 sinkStart = target.position;

		time = 0f;

		while (time < _sinkTime)
		{
			time += Time.deltaTime;

			float t = Mathf.Clamp01(time / Mathf.Max(0.01f, _sinkTime));

			target.position = sinkStart + Vector3.down * (_sinkDepth * t * t);

			yield return null;
		}

		target.gameObject.SetActive(false);
	}

	private IEnumerator CoFadeLight()
	{
		float startIntensity = _light.intensity;
		float time = 0f;

		while (time < _lightFadeTime)
		{
			time += Time.deltaTime;
			_light.intensity = Mathf.Lerp(startIntensity, 0f, time / Mathf.Max(0.01f, _lightFadeTime));

			yield return null;
		}

		_light.enabled = false;
	}
}

using System.Collections;
using UnityEngine;

// 도로 위험 연출
public class CRoadHazardView : MonoBehaviour
{
	#region 인스펙터
	// 화살 모델 (비우면 기본 도형으로 만든다, 앞(+Z)이 촉 방향)
	[SerializeField] private GameObject _arrowPrefab;
	[SerializeField] private int _arrowCount = 9;
	// 화살 사이 간격 (초)
	[SerializeField] private float _arrowInterval = 0.08f;
	// 날아가는 시간 (초)
	[SerializeField] private float _flightTime = 0.45f;
	// 길 가운데에서 쏘는 곳까지 옆 거리 (m, 최소·최대)
	[SerializeField] private Vector2 _launchSide = new Vector2(13f, 19f);
	// 쏘는 높이 (m, 최소·최대)
	[SerializeField] private Vector2 _launchHeight = new Vector2(2.5f, 5f);
	// 날아가며 위로 휘는 높이 (m)
	[SerializeField] private float _arcHeight = 1.2f;
	// 마차 몸통에서 꽂히는 높이 (m, 최소·최대)
	[SerializeField] private Vector2 _hitHeight = new Vector2(1.1f, 2.3f);
	// 마차에 꽂힌 채 남아 있는 시간 (초)
	[SerializeField] private float _stickTime = 4f;
	// 첫 화살이 맞을 때 마차가 들썩이는 세기
	[SerializeField] private float _hitJolt = 1.6f;
	[SerializeField] private bool _logState = true;
	#endregion

	#region 내부 변수
	private Material _shaftMaterial;
	private Material _headMaterial;
	private Material _featherMaterial;
	#endregion

	public void Play(ERoadHazard hazard, CCoach3DView coach, CCoachPhysics physics)
	{
		if (hazard == ERoadHazard.None || coach == null)
		{
			return;
		}

		switch (hazard)
		{
			case ERoadHazard.ArrowVolley:
				StartCoroutine(CoArrowVolley(coach, physics));
				break;
		}
	}

	private IEnumerator CoArrowVolley(CCoach3DView coach, CCoachPhysics physics)
	{
		// 한쪽 숲에서만 쏜다 → 어느 쪽에서 공격이 왔는지 보이게
		float side = Random.value < 0.5f ? -1f : 1f;
		bool isFirst = true;

		if (_logState)
		{
			CPrint.Log($"CRoadHazardView : {(side < 0f ? "왼쪽" : "오른쪽")} 숲에서 화살이 날아옴");
		}

		for (int i = 0; i < _arrowCount; i++)
		{
			if (coach == null)
			{
				yield break;
			}

			StartCoroutine(CoArrow(coach, physics, side, isFirst));
			isFirst = false;

			yield return new WaitForSeconds(_arrowInterval * Random.Range(0.6f, 1.4f));
		}
	}

	// 화살 하나 → 쏜 곳에서 마차 몸통(날아가는 동안 움직일 자리)으로 휘어 날아가 꽂힌다
	private IEnumerator CoArrow(CCoach3DView coach, CCoachPhysics physics, float side, bool isFirst)
	{
		Transform body = coach.transform;
		Vector3 forward = Vector3.ProjectOnPlane(body.forward, Vector3.up).normalized;
		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		float speed = physics != null ? physics.ForwardSpeed : 0f;

		Vector3 start = body.position
			+ right * (side * Random.Range(_launchSide.x, _launchSide.y))
			+ forward * Random.Range(-3f, 10f)
			+ Vector3.up * Random.Range(_launchHeight.x, _launchHeight.y);

		// 맞을 곳은 마차 기준 자리로 잡는다 → 날아가는 동안 마차가 움직여도 따라간다
		Vector3 localHit = new Vector3
			(
			side * Random.Range(0.2f, 0.7f),
			Random.Range(_hitHeight.x, _hitHeight.y),
			Random.Range(-1.4f, 1.4f)
			);

		GameObject arrow = CreateArrow();
		Transform t = arrow.transform;
		float time = 0f;
		float duration = Mathf.Max(0.05f, _flightTime * Random.Range(0.85f, 1.15f));
		Vector3 previous = start;

		t.position = start;

		while (time < duration)
		{
			if (body == null)
			{
				Destroy(arrow);
				yield break;
			}

			time += Time.deltaTime;

			float k = Mathf.Clamp01(time / duration);
			Vector3 end = body.TransformPoint(localHit);
			Vector3 position = Vector3.Lerp(start, end, k) + Vector3.up * (_arcHeight * 4f * k * (1f - k));
			Vector3 direction = position - previous;

			t.position = position;

			if (direction.sqrMagnitude > 0.0001f)
			{
				t.rotation = Quaternion.LookRotation(direction, Vector3.up);
			}

			previous = position;

			yield return null;
		}

		if (body == null)
		{
			Destroy(arrow);
			yield break;
		}

		// 촉이 살짝 박히게 앞으로 밀고 마차에 붙인다 → 마차와 같이 움직인다
		t.position = body.TransformPoint(localHit) + t.forward * 0.25f;
		t.SetParent(body, true);

		if (isFirst)
		{
			coach.Bump();

			if (physics != null)
			{
				physics.Jolt(_hitJolt, side < 0f);
			}
		}

		Destroy(arrow, _stickTime);
	}

	private GameObject CreateArrow()
	{
		if (_arrowPrefab != null)
		{
			return Instantiate(_arrowPrefab, transform);
		}

		EnsureMaterials();

		GameObject root = new GameObject("HazardArrow");

		root.transform.SetParent(transform, false);

		// 화살대 (원기둥은 Y 축 → 앞(Z) 으로 눕힌다), 촉이 앞 끝
		CreatePart(PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0f, -0.45f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.035f, 0.45f, 0.035f), _shaftMaterial);
		CreatePart(PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0.02f), Quaternion.Euler(0f, 0f, 45f) * Quaternion.Euler(45f, 0f, 0f), new Vector3(0.07f, 0.07f, 0.07f), _headMaterial);
		// 깃 (뒤 끝에 십자로 두 장)
		CreatePart(PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, -0.82f), Quaternion.identity, new Vector3(0.14f, 0.01f, 0.16f), _featherMaterial);
		CreatePart(PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, -0.82f), Quaternion.identity, new Vector3(0.01f, 0.14f, 0.16f), _featherMaterial);

		return root;
	}

	private static void CreatePart(PrimitiveType type, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
	{
		GameObject part = GameObject.CreatePrimitive(type);
		Collider collider = part.GetComponent<Collider>();

		// 마차 물리를 건드리지 않게 충돌체는 지운다
		if (collider != null)
		{
			Destroy(collider);
		}

		part.transform.SetParent(parent, false);
		part.transform.localPosition = position;
		part.transform.localRotation = rotation;
		part.transform.localScale = scale;

		MeshRenderer renderer = part.GetComponent<MeshRenderer>();

		renderer.sharedMaterial = material;
		renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
	}

	private void EnsureMaterials()
	{
		if (_shaftMaterial != null)
		{
			return;
		}

		Shader shader = Shader.Find("Standard");

		_shaftMaterial = new Material(shader) { name = "HazardArrowShaft", color = new Color(0.32f, 0.22f, 0.13f) };
		_headMaterial = new Material(shader) { name = "HazardArrowHead", color = new Color(0.2f, 0.2f, 0.22f) };
		_featherMaterial = new Material(shader) { name = "HazardArrowFeather", color = new Color(0.55f, 0.12f, 0.1f) };
	}

	private void OnDestroy()
	{
		DestroyCreated(_shaftMaterial);
		DestroyCreated(_headMaterial);
		DestroyCreated(_featherMaterial);
	}

	private static void DestroyCreated(Object created)
	{
		if (created != null)
		{
			Destroy(created);
		}
	}
}

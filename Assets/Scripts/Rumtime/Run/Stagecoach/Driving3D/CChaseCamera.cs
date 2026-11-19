using UnityEngine;

public class CChaseCamera : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Transform _target;
	[SerializeField] private float _distance = 13f;
	[SerializeField] private float _height = 6f;
	[SerializeField] private float _sideOffset = 0f;
	[SerializeField] private float _lookAhead = 6f;
	[SerializeField] private float _lookHeight = 1.2f;
	[SerializeField] private float _positionSmooth = 3f;
	[SerializeField] private float _rotationSmooth = 3.5f;
	[SerializeField] private float _focusDistanceScale = 1.8f;
	[SerializeField] private float _focusMinDistance = 12f;
	[SerializeField] private float _focusMaxDistance = 32f;
	[SerializeField] private float _focusHeightScale = 0.5f;
	[SerializeField] private float _focusBackAngle = 0.6f;
	[SerializeField] private float _focusSmooth = 2f;

	[Header("노드 장면 건물 비추기 (카메라는 길 위에서만)")]
	[SerializeField] private float _buildingAlongOffset = 0f;
	[SerializeField] private float _buildingMaxAlong = 25f;
	[SerializeField] private float _buildingHeight = 4.5f;
	[SerializeField] private float _buildingSideShift = 2f;
	[SerializeField] private float _roadHalfWidth = 4f;
	[SerializeField, Range(0f, 1f)] private float _buildingLookHeight = 0.45f;
	#endregion

	#region 내부 변수
	private bool _snap = true;
	private Transform _focus;
	private Transform _fixedCameraPoint;
	private Bounds _focusBounds;
	private bool _buildingFocus;
	private float _buildingDistanceScale = 1f;
	private Camera _camera;
	private static CChaseCamera _current;
	#endregion

	// 건물 등을 비추는 중인지
	public bool IsFocusing => _focus != null;
	// 지금 마차를 따라가는 카메라 (없으면 Camera.main)
	public static Camera CurrentCamera => _current != null && _current._camera != null ? _current._camera : Camera.main;

	private void Awake()
	{
		_camera = GetComponent<Camera>();
	}

	private void OnEnable()
	{
		_current = this;
	}

	private void OnDisable()
	{
		if (_current == this)
		{
			_current = null;
		}
	}

	/// <summary>
	/// 따라갈 마차를 바꾸고 다음 프레임에 바로 그 자리로 붙는다 / 경태
	/// </summary>
	public void SetTarget(Transform target)
	{
		_target = target;
		_snap = true;
	}

	/// <summary>
	/// 정해진 카메라 자리(cameraPoint)로 옮겨 가서 lookPoint 를 본다 → ReleaseFocus 까지 유지 / 경태
	/// </summary>
	public void FocusFrom(Transform cameraPoint, Transform lookPoint)
	{
		if (cameraPoint == null || lookPoint == null)
		{
			return;
		}

		BeginFocus(lookPoint);
		_fixedCameraPoint = cameraPoint;
	}

	/// <summary>
	/// 마차 대신 이 오브젝트를 비스듬히 비춘다 → ReleaseFocus 까지 유지 / 경태
	/// </summary>
	public void Focus(Transform focus)
	{
		BeginFocus(focus);

		if (_focus == null)
		{
			return;
		}

		_focusBounds = new Bounds(_focus.position, Vector3.one);

		if (TryGetRendererBounds(_focus, out Bounds bounds))
		{
			_focusBounds.Encapsulate(bounds);
		}
	}

	/// <summary>
	/// 길 위에서 건물을 길과 직각으로 비춘다 → ReleaseFocus 까지 유지 / 경태
	/// </summary>
	public void FocusBuilding(Transform building, float distanceScale = 1f)
	{
		BeginFocus(building);

		if (_focus == null)
		{
			return;
		}

		// 그릴 게 없으면 중심점만 보는 일반 비추기로
		if (!TryGetRendererBounds(building, out Bounds bounds))
		{
			_focusBounds = new Bounds(building.position, Vector3.one);
			return;
		}

		_focusBounds = bounds;
		_buildingFocus = true;
		_buildingDistanceScale = Mathf.Max(0.1f, distanceScale);
	}

	/// <summary>
	/// 비추기를 끝내고 다시 마차를 따라간다 / 경태
	/// </summary>
	public void ReleaseFocus()
	{
		BeginFocus(null);
	}

	// 비추기 상태를 한 번에 초기화한다 → 세 가지 비추기가 서로 섞이지 않게
	private void BeginFocus(Transform focus)
	{
		_focus = focus;
		_fixedCameraPoint = null;
		_buildingFocus = false;
	}

	private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
	{
		Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
		bool hasBounds = false;

		bounds = default;

		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i] == null)
			{
				continue;
			}

			if (!hasBounds)
			{
				bounds = renderers[i].bounds;
				hasBounds = true;
			}
			else
			{
				bounds.Encapsulate(renderers[i].bounds);
			}
		}

		return hasBounds;
	}

	private void LateUpdate()
	{
		if (_target == null)
		{
			return;
		}

		Vector3 forward = Vector3.ProjectOnPlane(_target.forward, Vector3.up);

		if (forward.sqrMagnitude < 0.001f)
		{
			forward = Vector3.forward;
		}

		forward.Normalize();

		if (_focus != null)
		{
			TickFocus(forward);
			return;
		}

		Vector3 right = Vector3.Cross(Vector3.up, forward);
		Vector3 desiredPosition = _target.position - forward * _distance + Vector3.up * _height + right * _sideOffset;
		Vector3 lookPoint = _target.position + forward * _lookAhead + Vector3.up * _lookHeight;
		Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - desiredPosition, Vector3.up);

		if (_snap)
		{
			transform.SetPositionAndRotation(desiredPosition, desiredRotation);
			_snap = false;
			return;
		}

		float deltaTime = Time.unscaledDeltaTime;

		transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-_positionSmooth * deltaTime));
		transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-_rotationSmooth * deltaTime));
	}

	// 정해진 자리로 부드럽게 옮겨 가며 정해진 곳을 본다
	private void TickFixedFocus()
	{
		Vector3 desiredPosition = _fixedCameraPoint.position;
		Vector3 look = _focus.position - desiredPosition;
		Quaternion desiredRotation = look.sqrMagnitude > 0.001f ? Quaternion.LookRotation(look, Vector3.up) : transform.rotation;

		MoveFocusCamera(desiredPosition, desiredRotation);
	}

	// 카메라는 길 위에서만 움직인다 → 건물 정면 맞은편 길 위에 서서 건물을 90도(길과 직각)로 본다
	private void TickBuildingFocus(Vector3 roadForward)
	{
		Vector3 center = _focusBounds.center;
		Vector3 right = Vector3.Cross(Vector3.up, roadForward).normalized;
		float buildingSide = Vector3.Dot(center - _target.position, right) >= 0f ? 1f : -1f;
		float side = Mathf.Clamp(_buildingSideShift, 0f, _roadHalfWidth);
		float along = Vector3.Dot(center - _target.position, roadForward) + _buildingAlongOffset;

		along = Mathf.Clamp(along, -_buildingMaxAlong, _buildingMaxAlong);

		Vector3 desiredPosition = _target.position
			+ roadForward * along
			- right * (side * buildingSide)
			+ Vector3.up * (_buildingHeight * _buildingDistanceScale);

		// 길과 직각 방향으로만 본다 → 위아래만 건물 높이에 맞춰 기울인다
		Vector3 lookDirection = right * buildingSide;
		float distance = Mathf.Max(1f, Vector3.Dot(center - desiredPosition, lookDirection));
		float lookY = _focusBounds.min.y + _focusBounds.size.y * _buildingLookHeight;
		Vector3 look = lookDirection * distance + Vector3.up * (lookY - desiredPosition.y);
		Quaternion desiredRotation = Quaternion.LookRotation(look, Vector3.up);

		MoveFocusCamera(desiredPosition, desiredRotation);
	}

	// 비추는 동안 카메라는 같은 빠르기로 부드럽게 옮겨 간다
	private void MoveFocusCamera(Vector3 desiredPosition, Quaternion desiredRotation)
	{
		float t = 1f - Mathf.Exp(-_focusSmooth * Time.unscaledDeltaTime);

		transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
		transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
	}

	private void TickFocus(Vector3 roadForward)
	{
		if (_fixedCameraPoint != null)
		{
			TickFixedFocus();
			return;
		}

		if (_buildingFocus)
		{
			TickBuildingFocus(roadForward);
			return;
		}

		Vector3 center = _focusBounds.center;
		Vector3 toRoad = Vector3.ProjectOnPlane(_target.position - center, Vector3.up);

		if (toRoad.sqrMagnitude < 0.001f)
		{
			toRoad = -roadForward;
		}

		toRoad.Normalize();

		float size = _focusBounds.extents.magnitude;
		float distance = Mathf.Clamp(size * _focusDistanceScale, _focusMinDistance, Mathf.Max(_focusMinDistance, _focusMaxDistance));
		// 높이도 거리에 맞춰 줄인다 → 멀리 못 물러난 만큼 덜 올라간다
		float height = size * _focusHeightScale * Mathf.Min(1f, distance / Mathf.Max(0.01f, size * _focusDistanceScale));
		Vector3 direction = (toRoad - roadForward * _focusBackAngle).normalized;
		Vector3 desiredPosition = center + direction * distance + Vector3.up * height;
		Quaternion desiredRotation = Quaternion.LookRotation(center - desiredPosition, Vector3.up);

		MoveFocusCamera(desiredPosition, desiredRotation);
	}
}

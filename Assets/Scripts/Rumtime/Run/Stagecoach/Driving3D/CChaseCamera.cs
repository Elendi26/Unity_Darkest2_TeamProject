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
	// 건물 비추기 → 건물 크기에 곱해 떨어질 거리
	[SerializeField] private float _focusDistanceScale = 1.8f;
	[SerializeField] private float _focusMinDistance = 12f;
	// 건물 크기에 곱해 올릴 높이
	[SerializeField] private float _focusHeightScale = 0.5f;
	// 길 뒤쪽으로 비껴 서는 정도 (0 이면 길에서 정면, 클수록 비스듬히)
	[SerializeField] private float _focusBackAngle = 0.6f;
	[SerializeField] private float _focusSmooth = 2f;
	#endregion

	#region 내부 변수
	private bool _snap = true;
	private Transform _focus;
	private Bounds _focusBounds;
	private Camera _camera;
	private static CChaseCamera _current;
	#endregion

	// 건물 등을 비추는 중인지
	public bool IsFocusing => _focus != null;
	// 지금 마차를 따라가는 카메라 (없으면 Camera.main)
	//  ㄴ 씬을 여러 개 띄우면 Camera.main 이 다른 씬 카메라일 수 있어서 따로 둔다
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

	public void SetTarget(Transform target)
	{
		_target = target;
		_snap = true;
	}

	// 마차 대신 이 오브젝트를 비춘다 → ReleaseFocus 까지 유지
	public void Focus(Transform focus)
	{
		_focus = focus;

		if (_focus == null)
		{
			return;
		}

		Renderer[] renderers = _focus.GetComponentsInChildren<Renderer>();

		_focusBounds = new Bounds(_focus.position, Vector3.one);

		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i] == null)
			{
				continue;
			}

			_focusBounds.Encapsulate(renderers[i].bounds);
		}
	}

	// 다시 마차를 따라간다 (부드럽게 돌아온다)
	public void ReleaseFocus()
	{
		_focus = null;
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

	// 길 쪽에서 비스듬히 건물을 바라본다
	private void TickFocus(Vector3 roadForward)
	{
		Vector3 center = _focusBounds.center;
		Vector3 toRoad = Vector3.ProjectOnPlane(_target.position - center, Vector3.up);

		if (toRoad.sqrMagnitude < 0.001f)
		{
			toRoad = -roadForward;
		}

		toRoad.Normalize();

		float size = _focusBounds.extents.magnitude;
		float distance = Mathf.Max(_focusMinDistance, size * _focusDistanceScale);
		Vector3 direction = (toRoad - roadForward * _focusBackAngle).normalized;
		Vector3 desiredPosition = center + direction * distance + Vector3.up * (size * _focusHeightScale);
		Quaternion desiredRotation = Quaternion.LookRotation(center - desiredPosition, Vector3.up);
		float t = 1f - Mathf.Exp(-_focusSmooth * Time.unscaledDeltaTime);

		transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
		transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
	}
}

using UnityEngine;

public class CChaseCamera : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Transform _target;
	[SerializeField] private float _distance = 10f;
	[SerializeField] private float _height = 5f;
	[SerializeField] private float _sideOffset = 0f;
	[SerializeField] private float _lookAhead = 6f;
	[SerializeField] private float _lookHeight = 1.2f;
	[SerializeField] private float _positionSmooth = 3f;
	[SerializeField] private float _rotationSmooth = 3.5f;
	#endregion

	#region 내부 변수
	private bool _snap = true;
	#endregion

	public void SetTarget(Transform target)
	{
		_target = target;
		_snap = true;
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
}

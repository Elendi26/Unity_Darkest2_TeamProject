using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CCoach3DView))]
public class CCoachPhysics : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private float _mass = 1500f;
	[SerializeField] private float _suspensionDistance = 0.4f;
	[SerializeField] private float _spring = 28000f;
	[SerializeField] private float _damper = 4000f;
	[SerializeField] private float _wheelMass = 40f;
	[SerializeField] private LayerMask _groundMask = ~0;
	[SerializeField] private float _spawnProbeHeight = 50f;
	[SerializeField] private float _uprightTorque = 8000f;
	[SerializeField] private float _wobbleDamping = 3f;
	[SerializeField] private float _brakeResponse = 4f;
	[SerializeField] private float _brakeDeceleration = 5f;
	[SerializeField] private float _rollingDeceleration = 0.2f;
	[SerializeField] private float _wallSoftZone = 0.8f;
	[SerializeField] private float _wallStiffness = 8f;
	[SerializeField] private float _wallDamping = 12f;
	[SerializeField] private float _wallReturnSpeed = 0.3f;
	[SerializeField] private float _maxTeamAngle = 35f;
	[SerializeField] private float _centerOfMassOffset = 0f;
	[SerializeField] private float _rearGrip = 2f;
	[SerializeField] private float _teamAlign = 16f;
	[SerializeField, Range(0.3f, 1f)] private float _teamAlignStart = 0.5f;
	[SerializeField] private float _trailerFollow = 4f;
	[SerializeField] private float _edgeYawFollow = 15f;
	[SerializeField] private float _edgeYawAlign = 4f;
	[SerializeField] private float _leanPerAcceleration = 0.8f;
	[SerializeField] private float _maxLean = 5f;
	[SerializeField] private float _leanResponse = 3f;
	[SerializeField] private float _liftedWheelDownForce = 1f;
	[SerializeField] private float _pitchDamping = 4f;
	[SerializeField] private float _groundAlign = 25f;
	[SerializeField] private bool _logState = true;
	[SerializeField] private bool _logWheelLift = true;
	[SerializeField] private bool _logEdge = false;
	#endregion

	#region 내부 변수
	private const float FREE_ROLL_TORQUE = 0.0001f;
	private Rigidbody _rigidbody;
	private CCoach3DView _view;
	private readonly List<WheelCollider> _allWheels = new List<WheelCollider>();
	private readonly List<WheelCollider> _frontWheels = new List<WheelCollider>();
	private readonly List<WheelCollider> _rearWheels = new List<WheelCollider>();
	private bool _brake = true;
	private float _smoothBrake = 1f;
	private CHorseTeam _team;
	private ConfigurableJoint _joint;
	private Vector3 _teamLocalPosition;
	private Vector3 _hitchLocal;
	private float _wheelBottomY = float.MaxValue;
	private float _rearLiftTime;
	private float _liftLogTimer;
	private float _lean;
	private float _bendLogTimer;
	private bool _isAtEdge;
	private float _edgeLogTimer;
	private float _edgeRoadAngle;
	#endregion

	public float ForwardSpeed => _rigidbody != null ? Vector3.Dot(_rigidbody.velocity, transform.forward) : 0f;
	public float MaxSpeed => _team != null ? _team.MaxSpeed : 0f;
	public CHorseTeam Team => _team;

	public void Drive(float throttle, float steer, bool brake)
	{
		_brake = brake;

		if (_team != null)
		{
			_team.Drive(throttle, steer, brake);
		}
	}

	public void KeepInside(Vector3 right, float offset, float limit)
	{
		if (_rigidbody == null)
		{
			return;
		}

		float softZone = Mathf.Max(0.1f, _wallSoftZone);
		float depth = Mathf.Abs(offset) - (limit - softZone);

		if (depth <= 0f)
		{
			return;
		}

		float strength = Mathf.Clamp01(depth / softZone);
		Vector3 outwardDir = right * Mathf.Sign(offset);

		_isAtEdge = true;
		_edgeLogTimer -= Time.fixedDeltaTime;

		Vector3 roadForward = Vector3.Cross(Flat(right), Vector3.up);

		if (Vector3.Dot(transform.forward, roadForward) < 0f)
		{
			roadForward = -roadForward;
		}

		float roadAngle = Vector3.SignedAngle(roadForward, Flat(transform.forward), Vector3.up);

		// 바깥을 향하고 있을 때만 길 쪽으로 돌린다
		_edgeRoadAngle = Vector3.Dot(Flat(transform.forward), outwardDir) > 0f ? roadAngle : 0f;

		if (_logEdge && _edgeLogTimer <= 0f)
		{
			_edgeLogTimer = 0.5f;

			float teamAngle = _team != null ? Vector3.SignedAngle(Flat(transform.forward), Flat(_team.transform.forward), Vector3.up) : 0f;

			CPrint.Warn
				(
				$"CCoachPhysics : 마차가 길 가장자리에 들어감 {depth:F2}m ({(offset > 0f ? "오른쪽" : "왼쪽")}), 길과 각도 {roadAngle:F0}도, " +
				$"꺾임 {teamAngle:F0}도, 회전 {_rigidbody.angularVelocity.y * Mathf.Rad2Deg:F0}도/s, 속도 {ForwardSpeed:F1}m/s"
				);
		}

		float outward = Vector3.Dot(_rigidbody.velocity, outwardDir);

		if (-outward < _wallReturnSpeed)
		{
			_rigidbody.AddForce(-outwardDir * (depth * depth / softZone * _wallStiffness), ForceMode.Acceleration);
		}

		if (outward > 0f)
		{
			_rigidbody.AddForce(-outwardDir * (outward * Mathf.Clamp01(_wallDamping * strength * Time.fixedDeltaTime)), ForceMode.VelocityChange);
		}
	}

	public void Teleport(Vector3 position, Quaternion rotation)
	{
		transform.SetPositionAndRotation(position, rotation);

		if (_rigidbody != null)
		{
			_rigidbody.position = position;
			_rigidbody.rotation = rotation;
			_rigidbody.velocity = Vector3.zero;
			_rigidbody.angularVelocity = Vector3.zero;
		}

		Physics.SyncTransforms();
		PlaceOnGround();

		if (_team != null)
		{
			_team.Teleport(transform.TransformPoint(_teamLocalPosition), Quaternion.LookRotation(Flat(transform.forward), Vector3.up));
			Physics.SyncTransforms();
		}
	}

	private void Awake()
	{
		_view = GetComponent<CCoach3DView>();
		_rigidbody = GetComponent<Rigidbody>();

		if (_rigidbody == null)
		{
			_rigidbody = gameObject.AddComponent<Rigidbody>();
		}

		_rigidbody.mass = _mass;
		_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		_rigidbody.constraints = RigidbodyConstraints.None;

		if (!CreateWheelColliders())
		{
			enabled = false;
			return;
		}

		_rigidbody.centerOfMass = new Vector3(0f, _hitchLocal.y + _centerOfMassOffset, GetWheelCenterZ());
		_rigidbody.inertiaTensor = new Vector3(_mass * 1.2f, _mass * 1.5f, _mass * 0.6f);
		_rigidbody.inertiaTensorRotation = Quaternion.identity;
	}

	private void Start()
	{
		PlaceOnGround();

		if (!AttachTeam())
		{
			enabled = false;
		}
	}

	private bool CreateWheelColliders()
	{
		Renderer[] wheels = _view.Wheels;

		if (wheels == null || wheels.Length == 0)
		{
			CPrint.Warn("CCoachPhysics : 바퀴가 없어 물리를 쓸 수 없습니다.");
			return false;
		}

		for (int i = 0; i < wheels.Length; i++)
		{
			if (wheels[i] == null)
			{
				continue;
			}

			Bounds bounds = wheels[i].bounds;
			GameObject go = new GameObject($"WheelCollider_{i}");

			go.transform.SetParent(transform, false);

			// 스프링이 반쯤 눌린 상태가 그림의 바퀴 위치와 맞도록 위로 올려 둔다
			go.transform.position = bounds.center + Vector3.up * (_suspensionDistance * 0.5f);

			WheelCollider wheel = go.AddComponent<WheelCollider>();

			wheel.radius = Mathf.Max(0.1f, bounds.extents.y);
			wheel.mass = _wheelMass;
			wheel.suspensionDistance = _suspensionDistance;
			wheel.suspensionSpring = new JointSpring { spring = _spring, damper = _damper, targetPosition = 0.5f };

			wheel.motorTorque = FREE_ROLL_TORQUE;
			wheel.brakeTorque = 0f;

			_wheelBottomY = Mathf.Min(_wheelBottomY, transform.InverseTransformPoint(bounds.center).y - wheel.radius);
			_allWheels.Add(wheel);
		}

		float centerZ = GetWheelCenterZ();

		for (int i = 0; i < _allWheels.Count; i++)
		{
			bool isFront = transform.InverseTransformPoint(_allWheels[i].transform.position).z > centerZ;

			(isFront ? _frontWheels : _rearWheels).Add(_allWheels[i]);
		}

		if (_frontWheels.Count == 0 || _rearWheels.Count == 0)
		{
			CPrint.Warn("CCoachPhysics : 앞바퀴·뒷바퀴를 나누지 못해 물리를 쓸 수 없습니다.");
			return false;
		}

		_hitchLocal = transform.InverseTransformPoint(GetAverage(_frontWheels));
		_hitchLocal.x = 0f;

		_allWheels[0].ConfigureVehicleSubsteps(5f, 12, 15);
		return true;
	}

	private bool AttachTeam()
	{
		Transform horseRoot = _view.HorseRoot;

		if (horseRoot == null)
		{
			CPrint.Warn("CCoachPhysics : 말을 찾지 못해 마차를 움직일 수 없습니다.");
			return false;
		}

		_teamLocalPosition = transform.InverseTransformPoint(horseRoot.position);

		float footHeight = _teamLocalPosition.y - _wheelBottomY;

		_team = CHorseTeam.Create(horseRoot, transform, _maxTeamAngle, footHeight);

		_joint = _team.gameObject.AddComponent<ConfigurableJoint>();
		_joint.connectedBody = _rigidbody;
		_joint.autoConfigureConnectedAnchor = false;
		_joint.anchor = _team.transform.InverseTransformPoint(transform.TransformPoint(_hitchLocal));
		_joint.connectedAnchor = _hitchLocal;
		_joint.axis = Vector3.right;
		_joint.secondaryAxis = Vector3.up;
		_joint.xMotion = ConfigurableJointMotion.Locked;
		_joint.yMotion = ConfigurableJointMotion.Free;
		_joint.zMotion = ConfigurableJointMotion.Locked;
		_joint.angularXMotion = ConfigurableJointMotion.Free;
		_joint.angularYMotion = ConfigurableJointMotion.Limited;
		_joint.angularZMotion = ConfigurableJointMotion.Limited;
		_joint.angularYLimit = new SoftJointLimit { limit = _maxTeamAngle };
		_joint.angularZLimit = new SoftJointLimit { limit = 30f };
		_joint.enableCollision = false;
		_joint.enablePreprocessing = false;
		_joint.projectionMode = JointProjectionMode.PositionAndRotation;
		_joint.projectionDistance = 0.1f;
		_joint.projectionAngle = 5f;

		_team.SetHitch(_hitchLocal);

		if (_logState)
		{
			CPrint.Log($"CCoachPhysics : 말 분리, 연결점 {_hitchLocal}, 무게중심 {_rigidbody.centerOfMass}");
		}

		return true;
	}

	private void FixedUpdate()
	{
		float deltaTime = Time.fixedDeltaTime;

		TickBrake(deltaTime);
		TickTrailer(deltaTime);
		TickUpright();
		TickWheelLift(deltaTime);
	}

	private void TickBrake(float deltaTime)
	{
		_smoothBrake = _brake ? Mathf.MoveTowards(_smoothBrake, 1f, _brakeResponse * deltaTime) : 0f;

		Vector3 forward = Flat(transform.forward);
		float speed = Vector3.Dot(_rigidbody.velocity, forward);
		float slow = Mathf.Min(Mathf.Abs(speed), (_smoothBrake * _brakeDeceleration + _rollingDeceleration) * deltaTime);

		if (slow > 0f)
		{
			_rigidbody.AddForce(-forward * (Mathf.Sign(speed) * slow), ForceMode.VelocityChange);
		}
	}

	private void TickTrailer(float deltaTime)
	{
		// 앞바퀴는 말이 향한 쪽으로 꺾는다
		float teamAngle = Mathf.Clamp(Vector3.SignedAngle(Flat(transform.forward), Flat(_team.transform.forward), Vector3.up), -_maxTeamAngle, _maxTeamAngle);

		for (int i = 0; i < _frontWheels.Count; i++)
		{
			_frontWheels[i].steerAngle = teamAngle;
		}

		_view.SetSteerAngle(teamAngle);

		// 뒷바퀴 축이 옆으로 가는 속도를 없애는 힘
		Vector3 right = Flat(transform.right);
		Vector3 rearAxle = GetAverage(_rearWheels);
		float lateral = Vector3.Dot(_rigidbody.GetPointVelocity(rearAxle), right);

		_rigidbody.AddForceAtPosition(-right * (lateral * _rigidbody.mass * _rearGrip), rearAxle, ForceMode.Force);

		// 말보다 많이 꺾였으면 말 쪽으로 돌린다
		float excess = Mathf.Abs(teamAngle) - _maxTeamAngle * _teamAlignStart;
		float yawTorque = excess > 0f ? Mathf.Sign(teamAngle) * excess * Mathf.Deg2Rad * _teamAlign : 0f;

		// 끌려가는 마차는 앞으로 갈수록 말 쪽으로 펴진다 → 꺾인 각도와 속도로 정한 회전 속도를 따라간다
		//  ㄴ 말 회전 속도를 따라가면 말이 곧게 갈 때 꺾인 채로 굳는다
		float wheelBase = Mathf.Max(1f, _hitchLocal.z - transform.InverseTransformPoint(rearAxle).z);
		float followYawRate = ForwardSpeed / wheelBase * Mathf.Sin(teamAngle * Mathf.Deg2Rad);

		// 가장자리에서 바깥을 보고 있으면 말을 기다리지 않고 길 방향으로 돌린다
		float edgeYawRate = -_edgeRoadAngle * _edgeYawAlign * Mathf.Deg2Rad;

		if (edgeYawRate != 0f && (edgeYawRate * followYawRate <= 0f || Mathf.Abs(edgeYawRate) > Mathf.Abs(followYawRate)))
		{
			followYawRate = edgeYawRate;
		}

		// 가장자리에 있는 동안은 더 세게 따라가게 한다
		float follow = _isAtEdge ? _edgeYawFollow : _trailerFollow;

		_isAtEdge = false;
		_edgeRoadAngle = 0f;
		yawTorque += (followYawRate - _rigidbody.angularVelocity.y) * follow;
		_rigidbody.AddTorque(Vector3.up * yawTorque, ForceMode.Acceleration);

		_bendLogTimer -= deltaTime;

		if (_logState && Mathf.Abs(teamAngle) >= _maxTeamAngle - 1f && _bendLogTimer <= 0f)
		{
			_bendLogTimer = 0.5f;
			CPrint.Warn($"CCoachPhysics : 말과 마차가 한계까지 꺾임 {teamAngle:F0}도, 속도 {ForwardSpeed:F1}m/s, 기울기 {Vector3.Angle(transform.up, Vector3.up):F0}도");
		}

		float lateralAcceleration = _rigidbody.angularVelocity.y * ForwardSpeed;
		float targetLean = Mathf.Clamp(lateralAcceleration * _leanPerAcceleration, -_maxLean, _maxLean);

		_lean = Mathf.Lerp(_lean, targetLean, 1f - Mathf.Exp(-_leanResponse * deltaTime));
		_view.SetLean(_lean);
	}

	private void TickUpright()
	{
		Vector3 tilt = Vector3.Cross(transform.up, Vector3.up);
		Vector3 wobble = _rigidbody.angularVelocity - Vector3.Project(_rigidbody.angularVelocity, Vector3.up);

		_rigidbody.AddTorque(tilt * _uprightTorque, ForceMode.Force);
		_rigidbody.AddTorque(-wobble * _wobbleDamping, ForceMode.Acceleration);
	}

	private void TickWheelLift(float deltaTime)
	{
		float cornerWeight = _rigidbody.mass * Physics.gravity.magnitude / _allWheels.Count;
		bool rearLifted = false;

		for (int i = 0; i < _allWheels.Count; i++)
		{
			WheelCollider wheel = _allWheels[i];

			if (wheel.isGrounded)
			{
				continue;
			}

			_rigidbody.AddForceAtPosition(-transform.up * (cornerWeight * _liftedWheelDownForce), wheel.transform.position, ForceMode.Force);
			rearLifted |= _rearWheels.Contains(wheel);
		}

		Vector3 pitchRate = Vector3.Project(_rigidbody.angularVelocity, transform.right);

		_rigidbody.AddTorque(-pitchRate * _pitchDamping, ForceMode.Acceleration);
		AlignPitchToGround();

		_rearLiftTime = rearLifted ? _rearLiftTime + deltaTime : 0f;
		_liftLogTimer -= deltaTime;

		if (_logWheelLift && _rearLiftTime >= 0.15f && _liftLogTimer <= 0f)
		{
			_liftLogTimer = 0.5f;
			LogWheelLift();
		}
	}

	// 앞바퀴 축·뒷바퀴 축 아래 땅을 재서, 마차 앞뒤 기울기를 땅 기울기에 맞춘다
	private void AlignPitchToGround()
	{
		Vector3 up = Vector3.up * _spawnProbeHeight;

		if (_groundAlign <= 0f
			|| !TryFindGround(GetAverage(_frontWheels) + up, out RaycastHit frontGround)
			|| !TryFindGround(GetAverage(_rearWheels) + up, out RaycastHit rearGround))
		{
			return;
		}

		Vector3 right = transform.right;
		Vector3 groundForward = Vector3.ProjectOnPlane(frontGround.point - rearGround.point, right);
		float error = Vector3.SignedAngle(transform.forward, groundForward, right) * Mathf.Deg2Rad;

		_rigidbody.AddTorque(right * (error * _groundAlign), ForceMode.Acceleration);
	}

	private void LogWheelLift()
	{
		System.Text.StringBuilder wheels = new System.Text.StringBuilder();

		for (int i = 0; i < _allWheels.Count; i++)
		{
			string side = _frontWheels.Contains(_allWheels[i]) ? "앞" : "뒤";

			if (_allWheels[i].GetGroundHit(out WheelHit hit))
			{
				wheels.Append($"[{side} {hit.collider.name} 힘 {hit.force:F0} 미끄럼 {hit.forwardSlip:F2}/{hit.sidewaysSlip:F2}] ");
			}
			else
			{
				wheels.Append($"[{side} 공중] ");
			}
		}

		float pitch = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
		Vector3 hitchForce = transform.InverseTransformDirection(-_joint.currentForce);

		CPrint.Warn
			(
			$"CCoachPhysics : 뒷바퀴 뜸 {_rearLiftTime:F2}초, 앞뒤 기울기 {pitch:F1}°, 속도 {ForwardSpeed:F1}m/s, " +
			$"말 속도 {_team.ForwardSpeed:F1}m/s, 연결점 힘(마차 기준) {hitchForce}, 브레이크 {_smoothBrake:F2}, 바퀴 {wheels}"
			);
	}

	private bool TryFindGround(Vector3 origin, out RaycastHit ground)
	{
		RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, _spawnProbeHeight * 2f, _groundMask, QueryTriggerInteraction.Ignore);
		float nearest = float.MaxValue;

		ground = default;

		for (int i = 0; i < hits.Length; i++)
		{
			bool isTeam = _team != null && hits[i].rigidbody == _team.Body;

			if (hits[i].collider is WheelCollider || hits[i].collider.transform.IsChildOf(transform) || isTeam || hits[i].distance >= nearest)
			{
				continue;
			}

			nearest = hits[i].distance;
			ground = hits[i];
		}

		return nearest < float.MaxValue;
	}

	// 바퀴마다 땅을 재서 가장 높은 곳 위에 올린다
	private void PlaceOnGround()
	{
		float lift = float.MinValue;

		for (int i = 0; i < _allWheels.Count; i++)
		{
			Vector3 center = _allWheels[i].transform.position;

			if (!TryFindGround(center + Vector3.up * _spawnProbeHeight, out RaycastHit hit))
			{
				continue;
			}

			float bottom = center.y - _allWheels[i].radius - _allWheels[i].suspensionDistance;

			lift = Mathf.Max(lift, hit.point.y - bottom);
		}

		if (lift == float.MinValue)
		{
			CPrint.Warn($"CCoachPhysics : 마차 아래에서 땅을 찾지 못했습니다. 위치 {transform.position}");
			return;
		}

		Vector3 position = transform.position + Vector3.up * lift;

		transform.position = position;
		_rigidbody.position = position;
		_rigidbody.velocity = Vector3.zero;

		if (_logState)
		{
			CPrint.Log($"CCoachPhysics : 땅 위로 {lift:F2}m 올림");
		}
	}

	private float GetWheelCenterZ()
	{
		return transform.InverseTransformPoint(GetAverage(_allWheels)).z;
	}

	private static Vector3 GetAverage(List<WheelCollider> wheels)
	{
		Vector3 sum = Vector3.zero;

		for (int i = 0; i < wheels.Count; i++)
		{
			sum += wheels[i].transform.position;
		}

		return wheels.Count > 0 ? sum / wheels.Count : Vector3.zero;
	}

	private static Vector3 Flat(Vector3 direction)
	{
		direction.y = 0f;

		return direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
	}
}

using UnityEngine;

public class CHorseTeam : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private float _mass = 2000f;
	[SerializeField] private float _maxSpeed = 12f;
	[SerializeField] private float _acceleration = 4.5f;
	[SerializeField] private float _braking = 10f;
	[SerializeField] private float _coastDeceleration = 1.2f;
	[SerializeField] private float _turnRate = 95f;
	[SerializeField] private float _highSpeedTurn = 0.95f;
	[SerializeField] private float _throttleResponse = 2.5f;
	[SerializeField] private float _throttleRelease = 2.5f;
	[SerializeField] private float _steerResponse = 3f;
	[SerializeField] private float _steerReturn = 2.2f;
	[SerializeField] private float _lateralGrip = 8f;
	[SerializeField] private float _blockedSlack = 5f;
	[SerializeField] private float _groundProbe = 3f;
	[SerializeField] private float _groundSpring = 12f;
	[SerializeField] private float _footSink = 0f;
	[SerializeField] private float _bodyLift = 0.5f;
	[SerializeField] private float _wallSoftZone = 1.5f;
	[SerializeField] private float _wallPush = 6f;
	[SerializeField] private float _wallSteerAngle = 25f;
	[SerializeField] private float _wallInwardAngle = 15f;
	[SerializeField] private float _wallBounceSpeed = 0.1f;
	[SerializeField] private float _wallTurnTime = 0.8f;
	[SerializeField] private float _edgeMaxRoadAngle = 25f;
	[SerializeField] private float _edgeAlignRate = 8f;
	[SerializeField] private float _tetherStrength = 0.4f;
	[SerializeField] private float _tetherSlack = 0.15f;
	[SerializeField] private bool _logState = true;
	[SerializeField] private bool _logEdgeSteer = false;
	#endregion

	#region 내부 변수
	private const float GRAVITY = 9.81f;
	private Rigidbody _rigidbody;
	private Transform _wagon;
	private float _maxTeamAngle = 40f;
	private float _groundOffset;
	private float _throttle;
	private float _steer;
	private bool _brake = true;
	private float _smoothThrottle;
	private float _smoothSteer;
	private float _speed;
	private bool _hasEdge;
	private Vector3 _edgeRight;
	private float _edgeOffset;
	private float _edgeLimit;
	private bool _touchWall;
	private Vector3 _wallNormal;
	private float _wallTurnTimer;
	private Vector3 _roadForward;
	private bool _hasHitch;
	private Vector3 _wagonHitchLocal;
	private Vector3 _teamHitchLocal;
	private Rigidbody _wagonBody;
	private float _nextTetherLog;
	private float _nextEdgeLog;
	#endregion

	public float ForwardSpeed => _rigidbody != null ? Vector3.Dot(_rigidbody.velocity, Flat(transform.forward)) : 0f;
	public float MaxSpeed => _maxSpeed;
	public Rigidbody Body => _rigidbody;

	public static CHorseTeam Create(Transform horseRoot, Transform wagon, float maxTeamAngle, float footHeight)
	{
		GameObject go = new GameObject("HorseTeam");

		go.transform.SetPositionAndRotation(horseRoot.position, Quaternion.LookRotation(Flat(wagon.forward), Vector3.up));
		go.transform.SetParent(wagon.parent, true);
		horseRoot.SetParent(go.transform, true);

		CHorseTeam team = go.AddComponent<CHorseTeam>();

		team.Setup(wagon, maxTeamAngle, footHeight);

		return team;
	}

	public void Drive(float throttle, float steer, bool brake)
	{
		_throttle = Mathf.Clamp(throttle, -1f, 1f);
		_steer = Mathf.Clamp(steer, -1f, 1f);
		_brake = brake;
	}

	public void KeepInside(Vector3 right, float offset, float limit)
	{
		_hasEdge = true;
		_edgeRight = right;
		_edgeOffset = offset;
		_edgeLimit = limit;
		_roadForward = Vector3.Cross(right, Vector3.up).normalized;
	}

	public void SetHitch(Vector3 wagonHitchLocal)
	{
		_wagonHitchLocal = wagonHitchLocal;
		_teamHitchLocal = transform.InverseTransformPoint(_wagon.TransformPoint(wagonHitchLocal));
		_wagonBody = _wagon.GetComponent<Rigidbody>();
		_hasHitch = true;
	}

	public void Teleport(Vector3 position, Quaternion rotation)
	{
		transform.SetPositionAndRotation(position, rotation);
		_rigidbody.position = position;
		_rigidbody.rotation = rotation;
		_rigidbody.velocity = Vector3.zero;
		_rigidbody.angularVelocity = Vector3.zero;
		_speed = 0f;

		if (TryFindGround(out float groundY))
		{
			Vector3 grounded = new Vector3(position.x, groundY + _groundOffset, position.z);

			transform.position = grounded;
			_rigidbody.position = grounded;
		}
	}

	private void Setup(Transform wagon, float maxTeamAngle, float footHeight)
	{
		_wagon = wagon;
		_maxTeamAngle = maxTeamAngle;
		_groundOffset = footHeight + _footSink;

		_rigidbody = gameObject.AddComponent<Rigidbody>();
		_rigidbody.mass = _mass;
		_rigidbody.useGravity = false;
		_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		_rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
		_rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
		_rigidbody.maxDepenetrationVelocity = 1.5f;

		CreateBodyCollider();

		_rigidbody.centerOfMass = Vector3.up;
		_rigidbody.inertiaTensor = new Vector3(_mass, _mass, _mass);
		_rigidbody.inertiaTensorRotation = Quaternion.identity;

		Teleport(transform.position, transform.rotation);

		if (_logState)
		{
			CPrint.Log($"CHorseTeam : 말 분리 완료, 땅과의 높이 차 {_groundOffset:F2}");
		}
	}

	private void CreateBodyCollider()
	{
		Renderer[] renderers = GetComponentsInChildren<Renderer>();
		bool hasBounds = false;
		Bounds local = default;

		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
			{
				continue;
			}

			Bounds b = renderers[i].localBounds;

			Transform t = renderers[i].transform;

			if (renderers[i] is SkinnedMeshRenderer skinned && skinned.rootBone != null)
			{
				t = skinned.rootBone;
			}

			for (int c = 0; c < 8; c++)
			{
				Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((c & 1) == 0 ? -1f : 1f, (c & 2) == 0 ? -1f : 1f, (c & 4) == 0 ? -1f : 1f));
				Vector3 point = transform.InverseTransformPoint(t.TransformPoint(corner));

				if (!hasBounds)
				{
					local = new Bounds(point, Vector3.zero);
					hasBounds = true;
				}
				else
				{
					local.Encapsulate(point);
				}
			}
		}

		if (!hasBounds)
		{
			CPrint.Warn("CHorseTeam : 말 그림이 없어 충돌체를 만들지 못했습니다.");
			return;
		}

		float bottom = _bodyLift;
		float top = Mathf.Max(bottom + 0.5f, local.max.y);
		BoxCollider box = gameObject.AddComponent<BoxCollider>();

		box.center = new Vector3(local.center.x, (bottom + top) * 0.5f, local.center.z);
		box.size = new Vector3(local.size.x, top - bottom, local.size.z);
		box.sharedMaterial = new PhysicMaterial("HorseBody")
		{
			bounciness = 0f,
			dynamicFriction = 0f,
			staticFriction = 0f,
			bounceCombine = PhysicMaterialCombine.Minimum,
			frictionCombine = PhysicMaterialCombine.Minimum
		};
	}

	private void FixedUpdate()
	{
		float deltaTime = Time.fixedDeltaTime;

		Vector3 forward = Flat(transform.forward);

		SmoothInput(deltaTime, GetSteerInput(forward));
		TickSpeed(deltaTime);

		float yawRate = GetSteerYawRate();
		Vector3 velocity = GetHorizontalVelocity(forward, deltaTime);

		LimitEdgeHeading(forward, ref yawRate);
		ApplyEdge(deltaTime, ref velocity);
		ApplyWall(deltaTime, ref velocity);
		ClampTeamAngle(forward, ref yawRate);
		ApplyTether(deltaTime, ref velocity);

		velocity.y = GetVerticalVelocity(deltaTime);

		_rigidbody.velocity = velocity;
		_rigidbody.angularVelocity = Vector3.up * (yawRate * Mathf.Deg2Rad);
	}

	private void SmoothInput(float deltaTime, float steer)
	{
		float throttleSpeed = Mathf.Abs(_throttle) > Mathf.Abs(_smoothThrottle) ? _throttleResponse : _throttleRelease;

		_smoothThrottle = Mathf.MoveTowards(_smoothThrottle, _brake ? 0f : _throttle, throttleSpeed * deltaTime);

		bool returning = Mathf.Abs(steer) < 0.01f || Mathf.Sign(steer) != Mathf.Sign(_smoothSteer);
		float steerSpeed = returning ? _steerReturn : _steerResponse;

		_smoothSteer = Mathf.MoveTowards(_smoothSteer, steer, steerSpeed * deltaTime);
	}

	private void TickSpeed(float deltaTime)
	{
		Vector3 flatVelocity = _rigidbody.velocity;

		flatVelocity.y = 0f;

		float actual = flatVelocity.magnitude * Mathf.Sign(ForwardSpeed == 0f ? 1f : ForwardSpeed);

		_speed = Mathf.Clamp(_speed, actual - _blockedSlack, actual + 1f);
		// 벽에 밀려 실제로 뒤로 가고 있어도 말이 뒤로 걷지는 않는다
		_speed = Mathf.Max(0f, _speed);

		if (_brake)
		{
			_speed = Mathf.MoveTowards(_speed, 0f, _braking * deltaTime);
			return;
		}

		if (Mathf.Abs(_smoothThrottle) < 0.05f)
		{
			_speed = Mathf.MoveTowards(_speed, 0f, _coastDeceleration * deltaTime);
			return;
		}

		// 후진은 없다 → 뒤로 누르면 브레이크처럼 빨리 줄여 멈추기만 한다
		bool slowing = _smoothThrottle < 0f;
		float target = slowing ? 0f : _smoothThrottle * _maxSpeed;
		float rate = slowing ? _braking : _acceleration;

		_speed = Mathf.MoveTowards(_speed, target, rate * deltaTime);
	}

	private float GetSteerYawRate()
	{
		float actual = ForwardSpeed;
		float moving = Mathf.Clamp01(Mathf.Abs(actual) / 2f);
		float speedRatio = Mathf.Clamp01(Mathf.Abs(actual) / Mathf.Max(0.1f, _maxSpeed));
		float turn = _turnRate * Mathf.Lerp(1f, _highSpeedTurn, speedRatio);

		return _smoothSteer * turn * moving * Mathf.Sign(actual == 0f ? 1f : actual);
	}

	private Vector3 GetHorizontalVelocity(Vector3 forward, float deltaTime)
	{
		Vector3 current = _rigidbody.velocity;

		current.y = 0f;

		Vector3 side = current - forward * Vector3.Dot(current, forward);

		side *= 1f - Mathf.Clamp01(_lateralGrip * deltaTime);

		return forward * _speed + side;
	}

	private float GetSteerInput(Vector3 forward)
	{
		float amount = 0f;
		Vector3 road = Vector3.zero;
		Vector3 inward = Vector3.zero;

		if (_wallTurnTimer > 0f)
		{
			amount = 0.2f;
			road = _roadForward.sqrMagnitude > 0.5f ? _roadForward : Vector3.ProjectOnPlane(forward, _wallNormal);
			inward = _wallNormal;
		}
		else if (_hasEdge)
		{
			amount = Mathf.Clamp01(GetEdgeDepth(out float softZone) / softZone);
			road = _roadForward;
			inward = -_edgeRight * Mathf.Sign(_edgeOffset);
		}

		if (amount <= 0f || road.sqrMagnitude < 0.0001f)
		{
			return _steer;
		}

		Vector3 target = GetInwardTarget(forward, road, inward, amount);

		if (Vector3.Dot(forward - target, inward) > 0f)
		{
			return _steer;
		}

		float wallSteer = Mathf.Clamp(Vector3.SignedAngle(forward, target, Vector3.up) / Mathf.Max(1f, _wallSteerAngle), -1f, 1f);

		if (ForwardSpeed < 0f)
		{
			wallSteer = -wallSteer;
		}

		if (_logEdgeSteer && Time.time >= _nextEdgeLog)
		{
			_nextEdgeLog = Time.time + 0.5f;
			CPrint.Log($"CHorseTeam : 가장자리 조향 {wallSteer:F2} (가까운 정도 {amount:F2}, 플레이어 {_steer:F2})");
		}

		return wallSteer > 0f ? Mathf.Max(_steer, wallSteer) : Mathf.Min(_steer, wallSteer);
	}

	private Vector3 GetInwardTarget(Vector3 forward, Vector3 roadForward, Vector3 inward, float amount)
	{
		roadForward = Flat(roadForward);

		if (Vector3.Dot(forward, roadForward) < 0f)
		{
			roadForward = -roadForward;
		}

		if (ForwardSpeed <= 0f)
		{
			return roadForward;
		}

		float tilt = Mathf.Tan(_wallInwardAngle * Mathf.Clamp01(amount) * Mathf.Deg2Rad);

		return Flat(roadForward + Flat(inward) * tilt);
	}

	private float GetEdgeDepth(out float softZone)
	{
		softZone = Mathf.Max(0.1f, _wallSoftZone);

		return Mathf.Abs(_edgeOffset) - (_edgeLimit - softZone);
	}

	// 가장자리에 가까울수록 바깥을 향할 수 있는 각도를 줄인다 → 벽에 비스듬히 박힌 채 끌려가지 않는다
	private void LimitEdgeHeading(Vector3 forward, ref float yawRate)
	{
		if (!_hasEdge || ForwardSpeed <= 0f)
		{
			return;
		}

		float amount = Mathf.Clamp01(GetEdgeDepth(out float softZone) / softZone);
		Vector3 outwardDir = Flat(_edgeRight * Mathf.Sign(_edgeOffset));

		if (amount <= 0f || Vector3.Dot(forward, outwardDir) <= 0f)
		{
			return;
		}

		Vector3 road = Flat(_roadForward);

		if (Vector3.Dot(forward, road) < 0f)
		{
			road = -road;
		}

		float angle = Vector3.SignedAngle(road, forward, Vector3.up);
		float excess = Mathf.Abs(angle) - _edgeMaxRoadAngle * (1f - amount);

		if (excess <= 0f)
		{
			return;
		}

		float inward = -Mathf.Sign(angle);
		float alignRate = Mathf.Min(_turnRate, excess * _edgeAlignRate);

		yawRate = inward * Mathf.Max(yawRate * inward, alignRate);
	}

	// 길 가장자리 근처면 바깥으로 가는 속도를 줄인다
	private void ApplyEdge(float deltaTime, ref Vector3 velocity)
	{
		if (!_hasEdge)
		{
			return;
		}

		_hasEdge = false;

		float depth = GetEdgeDepth(out float softZone);

		if (depth <= 0f)
		{
			return;
		}

		float strength = Mathf.Clamp01(depth / softZone);
		Vector3 outwardDir = _edgeRight * Mathf.Sign(_edgeOffset);
		float outward = Vector3.Dot(velocity, outwardDir);

		if (outward > 0f)
		{
			velocity = RemoveKeepSpeed(velocity, outwardDir, strength);
		}

		// 가장자리를 넘어선 만큼만 옆으로 민다
		float overshoot = depth - softZone;

		if (overshoot > 0f)
		{
			velocity -= outwardDir * (overshoot * _wallPush * deltaTime);
		}
	}

	private void ApplyWall(float deltaTime, ref Vector3 velocity)
	{
		if (_touchWall)
		{
			_touchWall = false;

			if (_logState && _wallTurnTimer <= 0f)
			{
				CPrint.Log("CHorseTeam : 말이 벽에 닿음 → 길 안쪽으로 조향");
			}

			_wallTurnTimer = _wallTurnTime;

			float into = Vector3.Dot(velocity, _wallNormal);

			if (into < 0f)
			{
				velocity = RemoveKeepSpeed(velocity, Flat(_wallNormal), 1f);
			}
			else if (into > _wallBounceSpeed)
			{
				velocity -= _wallNormal * (into - _wallBounceSpeed);
			}
		}

		_wallTurnTimer = Mathf.Max(0f, _wallTurnTimer - deltaTime);
	}

	private void ApplyTether(float deltaTime, ref Vector3 velocity)
	{
		if (!_hasHitch || _wagon == null)
		{
			return;
		}

		Vector3 error = _wagon.TransformPoint(_wagonHitchLocal) - transform.TransformPoint(_teamHitchLocal);

		error.y = 0f;

		float distance = error.magnitude;

		if (distance <= _tetherSlack)
		{
			return;
		}

		if (_logState && distance > 1f && Time.time >= _nextTetherLog)
		{
			_nextTetherLog = Time.time + 0.5f;
			CPrint.Warn($"CHorseTeam : 말과 마차 연결점이 {distance:F2}m 벌어짐, 마차 위치 {_wagon.position}");
		}

		Vector3 toHitch = error / distance;
		Vector3 hitchVelocity = _wagonBody != null ? _wagonBody.GetPointVelocity(_wagon.TransformPoint(_wagonHitchLocal)) : Vector3.zero;

		hitchVelocity.y = 0f;

		float away = Vector3.Dot(velocity - hitchVelocity, -toHitch);

		if (away > 0f)
		{
			velocity += toHitch * away;
		}

		velocity += toHitch * ((distance - _tetherSlack) * _tetherStrength / deltaTime);
	}

	// 마차와 꺾인 각도가 한계면 더 꺾지 않는다
	private void ClampTeamAngle(Vector3 forward, ref float yawRate)
	{
		if (_wagon == null)
		{
			return;
		}

		float relative = Vector3.SignedAngle(Flat(_wagon.forward), forward, Vector3.up);

		if (Mathf.Abs(relative) >= _maxTeamAngle - 2f && Mathf.Sign(yawRate) == Mathf.Sign(relative))
		{
			yawRate = 0f;
		}
	}

	private float GetVerticalVelocity(float deltaTime)
	{
		if (!TryFindGround(out float groundY))
		{
			return _rigidbody.velocity.y - GRAVITY * deltaTime;
		}

		float error = groundY + _groundOffset - _rigidbody.position.y;

		return Mathf.Clamp(error * _groundSpring, -GRAVITY, GRAVITY);
	}

	private bool TryFindGround(out float groundY)
	{
		Vector3 origin = transform.position + Vector3.up * _groundProbe;
		RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, _groundProbe * 3f, ~0, QueryTriggerInteraction.Ignore);
		float nearest = float.MaxValue;

		groundY = 0f;

		for (int i = 0; i < hits.Length; i++)
		{
			Transform hit = hits[i].collider.transform;

			if (hits[i].collider is WheelCollider || hit.IsChildOf(transform) || (_wagon != null && hit.IsChildOf(_wagon)))
			{
				continue;
			}

			if (hits[i].normal.y < 0.5f || hits[i].distance >= nearest)
			{
				continue;
			}

			nearest = hits[i].distance;
			groundY = hits[i].point.y;
		}

		return nearest < float.MaxValue;
	}

	private void OnCollisionStay(Collision collision)
	{
		Vector3 normal = Vector3.zero;

		for (int i = 0; i < collision.contactCount; i++)
		{
			Vector3 contactNormal = collision.GetContact(i).normal;

			if (Mathf.Abs(contactNormal.y) > 0.5f)
			{
				continue;
			}

			normal += contactNormal;
		}

		normal.y = 0f;

		if (normal.sqrMagnitude < 0.0001f)
		{
			return;
		}

		_touchWall = true;
		_wallNormal = normal.normalized;
	}

	private void OnCollisionEnter(Collision collision)
	{
		OnCollisionStay(collision);
	}

	// 수평 속도에서 direction 쪽 성분을 amount 만큼 빼고, 빠르기는 그대로 둔다 → 벽·가장자리를 따라 미끄러진다
	private static Vector3 RemoveKeepSpeed(Vector3 velocity, Vector3 direction, float amount)
	{
		Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
		float flatSpeed = flat.magnitude;

		flat -= direction * (Vector3.Dot(flat, direction) * amount);

		if (flat.sqrMagnitude > 0.0001f)
		{
			flat = flat.normalized * flatSpeed;
		}

		return new Vector3(flat.x, velocity.y, flat.z);
	}

	private static Vector3 Flat(Vector3 direction)
	{
		direction.y = 0f;

		return direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
	}
}

using UnityEngine;

public class CCoach3DView : MonoBehaviour
{
	#region 인스펙터
	[SerializeField] private Transform _body;
	[SerializeField] private Renderer[] _wheels;
	[SerializeField] private Animator[] _horses;
	[SerializeField] private string _horseSpeedParam = "Speed";
	[SerializeField] private float _horseAnimSpeed = 1.2f;
	[SerializeField] private float _bobHeight = 0.05f;
	[SerializeField] private float _bobFrequency = 9f;
	[SerializeField] private float _rollAngle = 1.2f;
	[SerializeField] private float _rollFrequency = 2.1f;
	[SerializeField] private float _bumpHeight = 0.25f;
	[SerializeField] private float _bumpDuration = 0.45f;
	[SerializeField] private Transform _horseRoot;
	[SerializeField] private Transform[] _steerParts;
	[SerializeField] private string[] _steerPartNames = { "Front Axle", "Stear" };
	#endregion

	#region 내부 변수
	private Vector3 _bodyPosition;
	private Quaternion _bodyRotation;
	private float _speed;
	private float _time;
	private float _bumpTimer;
	private Quaternion[] _steerRest;
	private bool[] _wheelSteers;
	private float _steerAngle;
	private float _lean;
	#endregion

	public Renderer[] Wheels => _wheels;

	// 말 두 마리를 묶은 부모
	public Transform HorseRoot
	{
		get
		{
			if (_horseRoot == null)
			{
				_horseRoot = FindHorseRoot();
			}

			return _horseRoot;
		}
	}

	public void SetSteerAngle(float angle)
	{
		_steerAngle = angle;
	}

	public void SetLean(float angle)
	{
		_lean = angle;
	}

	private void Awake()
	{
		if (_body != null)
		{
			_bodyPosition = _body.localPosition;
			_bodyRotation = _body.localRotation;
		}

		SetupSteerParts();
		AddHoofEvents();
	}

	private void AddHoofEvents()
	{
		if (_horses == null)
		{
			return;
		}

		for (int i = 0; i < _horses.Length; i++)
		{
			if (_horses[i] == null || _horses[i].GetComponent<CHorseHoofEvents>() != null)
			{
				continue;
			}

			_horses[i].gameObject.AddComponent<CHorseHoofEvents>();
		}
	}

	// _horses 의 Animator 에서 위로 올라가며 마차 바로 아래 자식을 찾는다
	private Transform FindHorseRoot()
	{
		if (_horses == null)
		{
			return null;
		}

		for (int i = 0; i < _horses.Length; i++)
		{
			if (_horses[i] == null)
			{
				continue;
			}

			Transform current = _horses[i].transform;

			while (current.parent != null && current.parent != transform)
			{
				current = current.parent;
			}

			if (current.parent == transform)
			{
				return current;
			}
		}

		return null;
	}

	// 꺾일 부품의 처음 회전을 기억하고, 그 아래 있는 바퀴를 표시해 둔다
	private void SetupSteerParts()
	{
		if (_steerParts == null || _steerParts.Length == 0)
		{
			System.Collections.Generic.List<Transform> found = new System.Collections.Generic.List<Transform>();
			Transform[] children = GetComponentsInChildren<Transform>(true);

			for (int i = 0; i < children.Length; i++)
			{
				if (System.Array.IndexOf(_steerPartNames, children[i].name) >= 0)
				{
					found.Add(children[i]);
				}
			}

			_steerParts = found.ToArray();
		}

		_steerRest = new Quaternion[_steerParts.Length];

		for (int i = 0; i < _steerParts.Length; i++)
		{
			if (_steerParts[i] != null)
			{
				_steerRest[i] = _steerParts[i].localRotation;
			}
		}

		_wheelSteers = new bool[_wheels != null ? _wheels.Length : 0];

		for (int i = 0; i < _wheelSteers.Length; i++)
		{
			if (_wheels[i] == null)
			{
				continue;
			}

			for (int p = 0; p < _steerParts.Length; p++)
			{
				if (_steerParts[p] != null && _wheels[i].transform.IsChildOf(_steerParts[p]))
				{
					_wheelSteers[i] = true;
				}
			}
		}
	}

	// 마차 위쪽 축을 기준으로 처음 회전에서 _steerAngle 만큼 돌린다
	private void ApplySteer()
	{
		if (_steerParts == null || _steerRest == null)
		{
			return;
		}

		for (int i = 0; i < _steerParts.Length; i++)
		{
			Transform part = _steerParts[i];

			if (part == null || part.parent == null)
			{
				continue;
			}

			Vector3 up = part.parent.InverseTransformDirection(transform.up);

			part.localRotation = Quaternion.AngleAxis(_steerAngle, up) * _steerRest[i];
		}
	}

	public void Drive(float speed, float distance)
	{
		_speed = Mathf.Clamp01(speed);

		SpinWheels(distance);
		TickHorses();
	}

	public void Bump()
	{
		_bumpTimer = _bumpDuration;
	}

	private void Update()
	{
		if (_body == null)
		{
			return;
		}

		float deltaTime = Time.unscaledDeltaTime;

		_time += deltaTime * _speed;

		float bob = Mathf.Abs(Mathf.Sin(_time * _bobFrequency)) * _bobHeight * _speed;
		float roll = Mathf.Sin(_time * _rollFrequency) * _rollAngle * _speed;
		float bump = 0f;

		if (_bumpTimer > 0f)
		{
			_bumpTimer -= deltaTime;
			bump = Mathf.Sin((1f - Mathf.Clamp01(_bumpTimer / _bumpDuration)) * Mathf.PI) * _bumpHeight;
		}

		_body.localPosition = _bodyPosition + Vector3.up * (bob + bump);
		// 기울기는 마차 앞 방향 축으로 돌린다
		Quaternion lean = Quaternion.identity;

		if (_body.parent != null)
		{
			lean = Quaternion.AngleAxis(_lean, _body.parent.InverseTransformDirection(transform.forward));
		}

		_body.localRotation = lean * _bodyRotation * Quaternion.Euler(0f, 0f, roll);

		ApplySteer();
	}

	private void SpinWheels(float distance)
	{
		if (_wheels == null || distance == 0f)
		{
			return;
		}

		for (int i = 0; i < _wheels.Length; i++)
		{
			if (_wheels[i] == null)
			{
				continue;
			}

			Bounds bounds = _wheels[i].bounds;
			float radius = Mathf.Max(0.05f, bounds.extents.y);
			float angle = distance / radius * Mathf.Rad2Deg;
			bool steers = _wheelSteers != null && i < _wheelSteers.Length && _wheelSteers[i];

			// 꺾인 앞바퀴는 꺾인 축으로 굴린다
			Vector3 axis = steers ? Quaternion.AngleAxis(_steerAngle, transform.up) * transform.right : transform.right;

			_wheels[i].transform.RotateAround(bounds.center, axis, angle);
		}
	}

	private void TickHorses()
	{
		if (_horses == null)
		{
			return;
		}

		for (int i = 0; i < _horses.Length; i++)
		{
			if (_horses[i] == null)
			{
				continue;
			}

			_horses[i].SetFloat(_horseSpeedParam, _speed);
			_horses[i].speed = Mathf.Lerp(1f, _horseAnimSpeed, _speed);
		}
	}
}

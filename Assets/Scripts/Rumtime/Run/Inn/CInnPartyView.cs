using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

// 영웅별 여관 앉기 애니메이션
[Serializable]
public class CInnHeroPose
{
	public string HeroId;
	public AnimationClip Clip;
}

// 여관 파티 → 지금 파티 영웅을 의자에 앉히고 의자 아래에 이름·체력·스트레스를 띄운다
public class CInnPartyView : MonoBehaviour
{
	#region 상수
	private const int SEAT_COUNT = 4;
	private const int STRESS_PIPS = 10;
	#endregion

	#region 인스펙터
	// 파티 슬롯 순서의 의자 자리 (0 = 맨 앞 영웅, 화면 오른쪽)
	[SerializeField] private Transform[] _seats = new Transform[SEAT_COUNT];
	[SerializeField] private CInnHeroPose[] _poses;
	// 영웅 데이터(CEntityBasicDataSO)를 찾을 Resources 경로
	[SerializeField] private string _heroDataPath = "EntityBasicDataSO";
	// 여관 세트(의자 0.8 배율) 기준 영웅 크기
	[SerializeField] private float _modelScale = 0.85f;
	// 의자 자리에서 영웅을 옮기는 양 (자리 기준 → y 위로, z 의자 앞쪽으로) → 발은 바닥에, 등은 등받이에 박히지 않게
	[SerializeField] private Vector3 _seatOffset = new Vector3(0f, 0f, 0.12f);
	// 영웅 재질을 여관 방에 맞춘다 → 영웅은 광택 0.5(회색으로 번들거림), 방은 광택 0 + 따뜻한 불빛
	[SerializeField] private bool _blendWithRoom = true;
	[SerializeField, Range(0f, 1f)] private float _heroGlossiness = 0f;
	// 영웅 색에 곱하는 색 → 어둡고 따뜻하게 (흰색이면 원래 색)
	[SerializeField] private Color _heroTint = new Color(0.8f, 0.68f, 0.56f, 1f);
	// 저장된 파티가 없을 때(여관 씬만 재생 등) 포즈 목록 순서로 앉혀 본다
	[SerializeField] private bool _fillWhenNoParty = true;
	// 비우면 여관 씬의 카메라
	[SerializeField] private Camera _camera;
	[SerializeField] private CInnScene _inn;
	// 의자 바닥에서 이름표까지 (화면 기준)
	[SerializeField] private Vector2 _labelOffset = new Vector2(0f, -16f);
	// 이름표 아래끝이 내려갈 수 있는 최저 높이 (화면 아래에서) → 아래 메뉴 줄·대사와 겹치지 않게
	[SerializeField] private float _labelMinBottom = 170f;
	[SerializeField] private Vector2 _barSize = new Vector2(150f, 18f);
	[SerializeField] private Color _nameColor = new Color(0.93f, 0.83f, 0.6f, 1f);
	[SerializeField] private Color _hpBackColor = new Color(0.18f, 0.04f, 0.03f, 0.9f);
	[SerializeField] private Color _hpFillColor = new Color(0.72f, 0.1f, 0.08f, 1f);
	[SerializeField] private Color _stressOnColor = new Color(0.92f, 0.9f, 0.86f, 1f);
	[SerializeField] private Color _stressOffColor = new Color(0.3f, 0.28f, 0.26f, 0.8f);
	// 앉힌 영웅의 위치·크기·화면 좌표 로그 (자리 맞출 때만 켠다)
	[SerializeField] private bool _logDetail = false;
	// 영웅에 마우스를 올리면 두르는 테두리 (비우면 이름으로 찾는다)
	[SerializeField] private Shader _outlineShader;
	[SerializeField] private Color _outlineColor = new Color(1f, 0.85f, 0.55f, 1f);
	[SerializeField] private float _outlineWidth = 3f;
	#endregion

	#region 내부 변수
	// 의자 하나에 앉은 영웅
	private class CSeated
	{
		public Transform Seat;
		public GameObject Model;
		public PlayableGraph Graph;
		public RectTransform Label;
		public CEntityBasicDataSO Data;
		public UserProfileData.HeroSaveData Save;
		public int Slot;
		// 테두리를 켜고 끌 때 바꿔 끼우는 재질 목록 (렌더러마다)
		public Renderer[] Renderers;
		public Material[][] BaseMaterials;
		public Material[][] OutlinedMaterials;
	}

	private readonly List<CSeated> _seated = new List<CSeated>();
	private Material _outlineMaterial;
	private CSeated _hovered;
	private RectTransform _labelRoot;
	private Font _font;
	#endregion

	private void Start()
	{
		if (_inn == null)
		{
			_inn = GetComponent<CInnScene>();
		}

		if (_camera == null)
		{
			_camera = FindSceneCamera();
		}

		_font = CUIBuilder.GetDefaultFont();
		_labelRoot = _inn != null && _inn.MenuRoot != null ? _inn.MenuRoot : CUIBuilder.CreateOverlayCanvas("InnPartyCanvas", transform, 39, false);

		SeatParty();
	}

	private void Update()
	{
		CHeroPopupLink.Tick();
		TickHover();
	}

	private void LateUpdate()
	{
		// 해상도가 바뀌어도 의자 아래를 따라가게 매 프레임 맞춘다
		for (int i = 0; i < _seated.Count; i++)
		{
			PlaceLabel(_seated[i]);
		}
	}

	private void OnDestroy()
	{
		for (int i = 0; i < _seated.Count; i++)
		{
			if (_seated[i].Graph.IsValid())
			{
				_seated[i].Graph.Destroy();
			}
		}

		_seated.Clear();

		if (_outlineMaterial != null)
		{
			Destroy(_outlineMaterial);
		}
	}

	#region 파티 앉히기
	private void SeatParty()
	{
		Dictionary<string, CEntityBasicDataSO> heroes = LoadHeroData();
		List<UserProfileData.HeroSaveData> party = LoadParty();

		if (party.Count == 0 && _fillWhenNoParty)
		{
			party = MakePreviewParty(heroes);
		}

		for (int i = 0; i < party.Count; i++)
		{
			UserProfileData.HeroSaveData save = party[i];
			int slot = save.PartySlotIndex;

			if (slot < 0 || slot >= _seats.Length || _seats[slot] == null)
			{
				continue;
			}

			if (!heroes.TryGetValue(save.HeroID, out CEntityBasicDataSO data))
			{
				CPrint.Warn($"CInnPartyView : 영웅 데이터를 찾지 못했습니다. ({save.HeroID})");
				continue;
			}

			Seat(_seats[slot], data, save, slot);
		}

		if (_logDetail)
		{
			StartCoroutine(LogSeatedRoutine());
		}
	}

	// 지금 프로필의 파티 (파티에 들었고 살아 있는 영웅만)
	private static List<UserProfileData.HeroSaveData> LoadParty()
	{
		List<UserProfileData.HeroSaveData> party = new List<UserProfileData.HeroSaveData>();
		DataSaveManager saveManager = DataSaveManager.Instance;

		if (saveManager == null || saveManager.CurrentSlotIndex < 0)
		{
			return party;
		}

		UserProfileData profile = saveManager.LoadProfile(saveManager.CurrentSlotIndex);

		if (profile == null || profile.PartyHeroes == null)
		{
			return party;
		}

		for (int i = 0; i < profile.PartyHeroes.Count; i++)
		{
			UserProfileData.HeroSaveData hero = profile.PartyHeroes[i];

			if (hero == null || hero.PartySlotIndex < 0 || hero.CurrentHP <= 0)
			{
				continue;
			}

			party.Add(hero);
		}

		return party;
	}

	private List<UserProfileData.HeroSaveData> MakePreviewParty(Dictionary<string, CEntityBasicDataSO> heroes)
	{
		List<UserProfileData.HeroSaveData> party = new List<UserProfileData.HeroSaveData>();

		if (_poses == null)
		{
			return party;
		}

		for (int i = 0; i < _poses.Length && party.Count < SEAT_COUNT; i++)
		{
			if (_poses[i] == null || !heroes.TryGetValue(_poses[i].HeroId, out CEntityBasicDataSO data))
			{
				continue;
			}

			party.Add(new UserProfileData.HeroSaveData
			{
				HeroID = data.HeroID,
				CurrentHP = data.MaxHP,
				CurrentStress = 0,
				PartySlotIndex = party.Count,
			});
		}

		return party;
	}

	private Dictionary<string, CEntityBasicDataSO> LoadHeroData()
	{
		Dictionary<string, CEntityBasicDataSO> heroes = new Dictionary<string, CEntityBasicDataSO>();
		CEntityBasicDataSO[] all = Resources.LoadAll<CEntityBasicDataSO>(_heroDataPath);

		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] == null || string.IsNullOrEmpty(all[i].HeroID) || heroes.ContainsKey(all[i].HeroID))
			{
				continue;
			}

			heroes.Add(all[i].HeroID, all[i]);
		}

		return heroes;
	}

	private void Seat(Transform seat, CEntityBasicDataSO data, UserProfileData.HeroSaveData save, int slot)
	{
		CSeated seated = new CSeated();

		seated.Seat = seat;
		seated.Data = data;
		seated.Save = save;
		seated.Slot = slot;

		if (data.HeroModel != null)
		{
			GameObject model = Instantiate(data.HeroModel, seat, false);

			model.name = data.HeroID;
			model.transform.localPosition = _seatOffset;
			model.transform.localRotation = Quaternion.identity;
			model.transform.localScale = Vector3.one * _modelScale;
			SetLayerRecursive(model.transform, seat.gameObject.layer);
			KeepRendered(model);
			BlendWithRoom(model);
			seated.Model = model;
			PrepareOutline(seated);
			seated.Graph = PlayPose(model, FindPose(data.HeroID));
		}
		else
		{
			CPrint.Warn($"CInnPartyView : {data.HeroID} 에 영웅 모델(HeroModel)이 없어 이름표만 띄웁니다.");
		}

		seated.Label = CreateLabel(data, save);
		_seated.Add(seated);
		PlaceLabel(seated);
	}

	private static void KeepRendered(GameObject model)
	{
		SkinnedMeshRenderer[] skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);

		for (int i = 0; i < skins.Length; i++)
		{
			skins[i].updateWhenOffscreen = true;
		}

		Animator[] animators = model.GetComponentsInChildren<Animator>(true);

		for (int i = 0; i < animators.Length; i++)
		{
			animators[i].cullingMode = AnimatorCullingMode.AlwaysAnimate;
		}
	}

	// 재질마다 광택을 낮추고 색을 곱한다
	private void BlendWithRoom(GameObject model)
	{
		if (!_blendWithRoom)
		{
			return;
		}

		Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
		MaterialPropertyBlock block = new MaterialPropertyBlock();

		for (int i = 0; i < renderers.Length; i++)
		{
			Renderer renderer = renderers[i];

			// 파티클 등은 그대로 둔다
			if (renderer == null || renderer is ParticleSystemRenderer)
			{
				continue;
			}

			Material[] materials = renderer.sharedMaterials;

			for (int m = 0; m < materials.Length; m++)
			{
				Material material = materials[m];

				if (material == null)
				{
					continue;
				}

				renderer.GetPropertyBlock(block, m);

				if (material.HasProperty("_Glossiness"))
				{
					block.SetFloat("_Glossiness", Mathf.Min(material.GetFloat("_Glossiness"), _heroGlossiness));
				}

				if (material.HasProperty("_Color"))
				{
					block.SetColor("_Color", material.GetColor("_Color") * _heroTint);
				}

				renderer.SetPropertyBlock(block, m);
			}
		}
	}

	private static void SetLayerRecursive(Transform root, int layer)
	{
		root.gameObject.layer = layer;

		for (int i = 0; i < root.childCount; i++)
		{
			SetLayerRecursive(root.GetChild(i), layer);
		}
	}

	// 실행 확인용 → 앉힌 뒤 실제 위치·크기·화면 좌표를 남긴다
	private System.Collections.IEnumerator LogSeatedRoutine()
	{
		yield return null;
		yield return null;

		for (int i = 0; i < _seated.Count; i++)
		{
			CSeated seated = _seated[i];

			if (seated.Model == null)
			{
				CPrint.Warn($"CInnPartyView : {seated.Seat.name} 모델 없음");
				continue;
			}

			Renderer[] renderers = seated.Model.GetComponentsInChildren<Renderer>(true);
			int visibleCount = 0;
			Bounds bounds = new Bounds(seated.Model.transform.position, Vector3.zero);

			for (int j = 0; j < renderers.Length; j++)
			{
				if (renderers[j].enabled && renderers[j].gameObject.activeInHierarchy)
				{
					visibleCount++;
					bounds.Encapsulate(renderers[j].bounds);
				}
			}

			Vector3 screen = _camera != null ? _camera.WorldToScreenPoint(bounds.center) : Vector3.zero;

			CPrint.Log
				(
				$"CInnPartyView : {seated.Model.name} @ {seated.Seat.name} 활성 {seated.Model.activeInHierarchy}, 렌더러 {visibleCount}/{renderers.Length}, " +
				$"크기 {bounds.size}, 중심 {bounds.center}, 화면 {screen}, 애니 {(seated.Graph.IsValid() ? "재생" : "없음")}"
				);
		}
	}

	private AnimationClip FindPose(string heroId)
	{
		if (_poses == null)
		{
			return null;
		}

		for (int i = 0; i < _poses.Length; i++)
		{
			if (_poses[i] != null && _poses[i].HeroId == heroId)
			{
				return _poses[i].Clip;
			}
		}

		return null;
	}

	// 컨트롤러 없이 클립 하나만 돌린다 → 영웅마다 시작 시점을 흩어 동작이 맞물리지 않게
	private PlayableGraph PlayPose(GameObject model, AnimationClip clip)
	{
		Animator animator = model.GetComponentInChildren<Animator>();

		if (animator == null || clip == null)
		{
			CPrint.Warn($"CInnPartyView : {model.name} 의 Animator 또는 여관 애니메이션이 없어 서 있는 자세로 둡니다.");
			return default;
		}

		// 앉은 자리에서 미끄러지지 않게
		animator.applyRootMotion = false;

		PlayableGraph graph = PlayableGraph.Create($"Inn_{model.name}");
		AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "InnPose", animator);
		AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);

		playable.SetTime(UnityEngine.Random.value * clip.length);
		output.SetSourcePlayable(playable);
		graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
		graph.Play();

		return graph;
	}
	#endregion

	#region 마우스 올리기·누르기
	// 테두리용 재질을 끼운 목록을 미리 만들어 둔다 → 올릴 때마다 배열을 만들지 않게
	private void PrepareOutline(CSeated seated)
	{
		if (_outlineMaterial == null)
		{
			Shader shader = _outlineShader != null ? _outlineShader : Shader.Find("Hidden/Run/InnHeroOutline");

			if (shader == null)
			{
				CPrint.Warn("CInnPartyView : 테두리 셰이더가 없어 테두리 없이 누르기만 됩니다.");
				return;
			}

			_outlineMaterial = new Material(shader);
			_outlineMaterial.name = "InnHeroOutline";
			_outlineMaterial.SetColor("_OutlineColor", _outlineColor);
			_outlineMaterial.SetFloat("_OutlineWidth", _outlineWidth);
		}

		List<Renderer> renderers = new List<Renderer>();
		Renderer[] all = seated.Model.GetComponentsInChildren<Renderer>(true);

		for (int i = 0; i < all.Length; i++)
		{
			if (all[i] is SkinnedMeshRenderer || all[i] is MeshRenderer)
			{
				renderers.Add(all[i]);
			}
		}

		seated.Renderers = renderers.ToArray();
		seated.BaseMaterials = new Material[seated.Renderers.Length][];
		seated.OutlinedMaterials = new Material[seated.Renderers.Length][];

		for (int i = 0; i < seated.Renderers.Length; i++)
		{
			Material[] baseMaterials = seated.Renderers[i].sharedMaterials;
			Material[] outlined = new Material[baseMaterials.Length + 1];

			baseMaterials.CopyTo(outlined, 0);
			outlined[baseMaterials.Length] = _outlineMaterial;
			seated.BaseMaterials[i] = baseMaterials;
			seated.OutlinedMaterials[i] = outlined;
		}
	}

	private void TickHover()
	{
		CSeated next = FindHovered();

		SetHovered(next);

		if (next != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
		{
			OpenPopup(next);
		}
	}

	// 마우스 아래 영웅 → 여러 명이 겹치면 카메라에 가까운 쪽
	private CSeated FindHovered()
	{
		if (_camera == null || Mouse.current == null)
		{
			return null;
		}

		// 정보 창이 열려 있거나 마우스가 메뉴·상점 같은 UI 위에 있으면 영웅을 고르지 않는다
		if (CHeroPopupLink.IsOpen || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
		{
			return null;
		}

		Vector2 mouse = Mouse.current.position.ReadValue();
		CSeated best = null;
		float bestDistance = float.MaxValue;

		for (int i = 0; i < _seated.Count; i++)
		{
			CSeated seated = _seated[i];

			if (seated.Model == null || !TryGetScreenRect(seated, out Rect rect, out float distance))
			{
				continue;
			}

			if (rect.Contains(mouse) && distance < bestDistance)
			{
				best = seated;
				bestDistance = distance;
			}
		}

		return best;
	}

	// 모델 전체를 감싸는 상자를 화면에 옮긴 사각형
	private bool TryGetScreenRect(CSeated seated, out Rect rect, out float distance)
	{
		rect = default;
		distance = 0f;

		if (seated.Renderers == null || seated.Renderers.Length == 0)
		{
			return false;
		}

		Bounds bounds = seated.Renderers[0].bounds;

		for (int i = 1; i < seated.Renderers.Length; i++)
		{
			bounds.Encapsulate(seated.Renderers[i].bounds);
		}

		Vector3 min = bounds.min;
		Vector3 max = bounds.max;
		Vector2 screenMin = new Vector2(float.MaxValue, float.MaxValue);
		Vector2 screenMax = new Vector2(float.MinValue, float.MinValue);

		for (int i = 0; i < 8; i++)
		{
			Vector3 corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
			Vector3 screen = _camera.WorldToScreenPoint(corner);

			if (screen.z <= 0f)
			{
				return false;
			}

			screenMin = Vector2.Min(screenMin, screen);
			screenMax = Vector2.Max(screenMax, screen);
		}

		rect = Rect.MinMaxRect(screenMin.x, screenMin.y, screenMax.x, screenMax.y);
		distance = Vector3.Distance(_camera.transform.position, bounds.center);

		return true;
	}

	private void SetHovered(CSeated next)
	{
		if (_hovered == next)
		{
			return;
		}

		SetOutline(_hovered, false);
		_hovered = next;
		SetOutline(_hovered, true);
	}

	private static void SetOutline(CSeated seated, bool on)
	{
		if (seated == null || seated.Renderers == null)
		{
			return;
		}

		for (int i = 0; i < seated.Renderers.Length; i++)
		{
			if (seated.Renderers[i] != null)
			{
				seated.Renderers[i].sharedMaterials = on ? seated.OutlinedMaterials[i] : seated.BaseMaterials[i];
			}
		}
	}

	// 화면 왼쪽부터(뒤 → 앞) 넘긴다 → 창 안의 이전·다음 버튼도 자리 순서로 돈다
	private void OpenPopup(CSeated target)
	{
		List<CSeated> ordered = new List<CSeated>(_seated);

		ordered.Sort((a, b) => b.Slot.CompareTo(a.Slot));

		List<CEntityBasicDataSO> heroes = new List<CEntityBasicDataSO>();
		List<UserProfileData.HeroSaveData> saves = new List<UserProfileData.HeroSaveData>();

		for (int i = 0; i < ordered.Count; i++)
		{
			heroes.Add(ordered[i].Data);
			saves.Add(ordered[i].Save);
		}

		SetHovered(null);

		CHeroPopupLink.Open(heroes, saves, heroes.IndexOf(target.Data));
	}
	#endregion

	#region 이름표
	// 이름 / 체력 막대(현재/최대) / 스트레스 칸
	private RectTransform CreateLabel(CEntityBasicDataSO data, UserProfileData.HeroSaveData save)
	{
		RectTransform root = CUIBuilder.CreateRect($"{data.HeroID}_Label", _labelRoot);

		root.anchorMin = new Vector2(0.5f, 0.5f);
		root.anchorMax = new Vector2(0.5f, 0.5f);
		root.pivot = new Vector2(0.5f, 1f);
		root.sizeDelta = new Vector2(_barSize.x + 60f, 90f);

		Text name = CreateText("Name", root, data.Name, 30, _nameColor);

		SetRect(name.rectTransform, new Vector2(0f, 0f), new Vector2(root.sizeDelta.x, 36f));

		int maxHp = Mathf.Max(1, data.MaxHP);
		int hp = Mathf.Clamp(save.CurrentHP, 0, maxHp);
		Image back = CUIBuilder.CreateImage("HpBack", root, null, _hpBackColor);

		SetRect(back.rectTransform, new Vector2(0f, -40f), _barSize);

		Image fill = CUIBuilder.CreateImage("HpFill", back.rectTransform, null, _hpFillColor);

		fill.rectTransform.anchorMin = Vector2.zero;
		fill.rectTransform.anchorMax = new Vector2((float)hp / maxHp, 1f);
		fill.rectTransform.offsetMin = Vector2.zero;
		fill.rectTransform.offsetMax = Vector2.zero;

		Text hpText = CreateText("HpText", back.rectTransform, $"{hp}/{maxHp}", 18, Color.white);

		hpText.rectTransform.anchorMin = Vector2.zero;
		hpText.rectTransform.anchorMax = Vector2.one;
		hpText.rectTransform.offsetMin = Vector2.zero;
		hpText.rectTransform.offsetMax = Vector2.zero;

		CreateStressPips(root, save.CurrentStress, data.MaxStress);

		return root;
	}

	private void CreateStressPips(RectTransform root, int stress, int maxStress)
	{
		const float PIP_SIZE = 10f;
		const float PIP_GAP = 4f;
		int filled = maxStress > 0 ? Mathf.RoundToInt((float)Mathf.Clamp(stress, 0, maxStress) / maxStress * STRESS_PIPS) : 0;
		float width = STRESS_PIPS * PIP_SIZE + (STRESS_PIPS - 1) * PIP_GAP;

		for (int i = 0; i < STRESS_PIPS; i++)
		{
			Image pip = CUIBuilder.CreateImage($"Stress_{i}", root, null, i < filled ? _stressOnColor : _stressOffColor);
			float x = -width * 0.5f + PIP_SIZE * 0.5f + i * (PIP_SIZE + PIP_GAP);

			SetRect(pip.rectTransform, new Vector2(x, -40f - _barSize.y - 10f), new Vector2(PIP_SIZE, PIP_SIZE));
		}
	}

	// 의자 바닥 위치를 화면 좌표로 옮겨 그 아래에 둔다
	private void PlaceLabel(CSeated seated)
	{
		if (seated.Label == null || seated.Seat == null || _camera == null)
		{
			return;
		}

		Vector3 screen = _camera.WorldToScreenPoint(seated.Seat.position);
		bool visible = screen.z > 0f;

		if (seated.Label.gameObject.activeSelf != visible)
		{
			seated.Label.gameObject.SetActive(visible);
		}

		if (!visible)
		{
			return;
		}

		RectTransform parent = seated.Label.parent as RectTransform;

		if (parent == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local))
		{
			return;
		}

		Vector2 position = local + _labelOffset;

		// 이름표는 위쪽 가운데 기준 → 아래끝 = y - 높이
		float minY = parent.rect.yMin + _labelMinBottom + seated.Label.sizeDelta.y;

		position.y = Mathf.Max(position.y, minY);
		seated.Label.anchoredPosition = position;
	}

	private Text CreateText(string name, RectTransform parent, string value, int size, Color color)
	{
		RectTransform rect = CUIBuilder.CreateRect(name, parent);
		Text text = rect.gameObject.AddComponent<Text>();

		text.font = _font;
		text.fontSize = size;
		text.color = color;
		text.alignment = TextAnchor.MiddleCenter;
		text.raycastTarget = false;
		text.text = value;

		Shadow shadow = rect.gameObject.AddComponent<Shadow>();

		shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
		shadow.effectDistance = new Vector2(2f, -2f);

		return text;
	}

	// 위쪽 가운데 기준으로 놓는다
	private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
	{
		rect.anchorMin = new Vector2(0.5f, 1f);
		rect.anchorMax = new Vector2(0.5f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = position;
		rect.sizeDelta = size;
	}
	#endregion

	// 여관 씬에 놓인 카메라 → 주행 씬 카메라와 헷갈리지 않게 같은 씬에서만 찾는다
	private Camera FindSceneCamera()
	{
		Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

		for (int i = 0; i < cameras.Length; i++)
		{
			if (cameras[i] != null && cameras[i].gameObject.scene == gameObject.scene)
			{
				return cameras[i];
			}
		}

		return Camera.main;
	}
}

using UnityEngine;

public static class CInnMenuArt
{
	#region 상수
	// 화살표 판 해상도 (UI 에서 늘려 쓴다)
	private const int PLATE_WIDTH = 256;
	private const int PLATE_HEIGHT = 96;
	// 오른쪽 뾰족한 끝 / 왼쪽 파인 홈의 깊이
	private const float PLATE_POINT = 30f;
	private const float PLATE_BORDER = 3f;
	private const int ICON_SIZE = 64;
	#endregion

	#region 내부 변수
	private static Sprite _plate;
	private static Sprite _door;
	#endregion

	// 화살표 판 → 안쪽은 흰색(이미지 색으로 물들인다), 테두리는 밝은 회색
	public static Sprite GetPlate(Color fill, Color border)
	{
		if (_plate != null)
		{
			return _plate;
		}

		Vector2[] shape =
		{
			new Vector2(0f, 0f),
			new Vector2(PLATE_WIDTH - PLATE_POINT, 0f),
			new Vector2(PLATE_WIDTH, PLATE_HEIGHT * 0.5f),
			new Vector2(PLATE_WIDTH - PLATE_POINT, PLATE_HEIGHT),
			new Vector2(0f, PLATE_HEIGHT),
			new Vector2(PLATE_POINT, PLATE_HEIGHT * 0.5f),
		};

		Texture2D texture = CreateTexture(PLATE_WIDTH, PLATE_HEIGHT, "InnMenuPlate");
		Color[] pixels = new Color[PLATE_WIDTH * PLATE_HEIGHT];

		for (int y = 0; y < PLATE_HEIGHT; y++)
		{
			for (int x = 0; x < PLATE_WIDTH; x++)
			{
				Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
				float distance = SignedDistance(p, shape);
				// 바깥 → 0, 테두리 → border, 안쪽 → fill (경계는 1px 부드럽게)
				float alpha = Mathf.Clamp01(0.5f - distance);
				float borderMix = Mathf.Clamp01(distance + PLATE_BORDER + 0.5f);
				Color color = Color.Lerp(fill, border, borderMix);

				color.a *= alpha;
				pixels[y * PLATE_WIDTH + x] = color;
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
		_plate = Sprite.Create(texture, new Rect(0f, 0f, PLATE_WIDTH, PLATE_HEIGHT), new Vector2(0.5f, 0.5f), 100f);
		_plate.name = "InnMenuPlate";

		return _plate;
	}

	// 문 + 밖으로 나가는 화살표
	public static Sprite GetDoorIcon()
	{
		if (_door != null)
		{
			return _door;
		}

		const float THICK = 2.6f;
		Vector2[,] lines =
		{
			// 문틀 (오른쪽 위아래 일부만 → 화살표가 지나갈 틈)
			{ new Vector2(12f, 8f), new Vector2(12f, 56f) },
			{ new Vector2(12f, 56f), new Vector2(38f, 56f) },
			{ new Vector2(12f, 8f), new Vector2(38f, 8f) },
			{ new Vector2(38f, 56f), new Vector2(38f, 42f) },
			{ new Vector2(38f, 8f), new Vector2(38f, 22f) },
			// 화살표
			{ new Vector2(24f, 32f), new Vector2(56f, 32f) },
			{ new Vector2(56f, 32f), new Vector2(46f, 42f) },
			{ new Vector2(56f, 32f), new Vector2(46f, 22f) },
		};

		Texture2D texture = CreateTexture(ICON_SIZE, ICON_SIZE, "InnMenuDoor");
		Color[] pixels = new Color[ICON_SIZE * ICON_SIZE];

		for (int y = 0; y < ICON_SIZE; y++)
		{
			for (int x = 0; x < ICON_SIZE; x++)
			{
				Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
				float distance = float.MaxValue;

				for (int i = 0; i < lines.GetLength(0); i++)
				{
					distance = Mathf.Min(distance, DistanceToSegment(p, lines[i, 0], lines[i, 1]));
				}

				pixels[y * ICON_SIZE + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(THICK + 0.5f - distance));
			}
		}

		texture.SetPixels(pixels);
		texture.Apply();
		_door = Sprite.Create(texture, new Rect(0f, 0f, ICON_SIZE, ICON_SIZE), new Vector2(0.5f, 0.5f), 100f);
		_door.name = "InnMenuDoor";

		return _door;
	}

	private static Texture2D CreateTexture(int width, int height, string name)
	{
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

		texture.name = name;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Bilinear;

		return texture;
	}

	// 다각형까지의 거리 → 안쪽은 음수
	private static float SignedDistance(Vector2 p, Vector2[] shape)
	{
		float distance = float.MaxValue;
		bool inside = false;

		for (int i = 0, j = shape.Length - 1; i < shape.Length; j = i++)
		{
			Vector2 a = shape[j];
			Vector2 b = shape[i];

			distance = Mathf.Min(distance, DistanceToSegment(p, a, b));

			if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
			{
				inside = !inside;
			}
		}

		return inside ? -distance : distance;
	}

	private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));

		return Vector2.Distance(p, a + ab * t);
	}
}

// 여관 영웅에 마우스를 올렸을 때 두르는 테두리
//  ㄴ 모델을 한 번 더, 뒷면만, 화면 기준으로 몇 픽셀 부풀려 그린다 → 앞면에 가려 바깥쪽만 선으로 보인다
//  ㄴ 화면 픽셀 기준이라 영웅 크기·카메라 거리와 상관없이 굵기가 같다
Shader "Hidden/Run/InnHeroOutline"
{
	Properties
	{
		_OutlineColor ("Outline Color", Color) = (1, 0.85, 0.55, 1)
		_OutlineWidth ("Outline Width (px)", Float) = 3
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" }

		Pass
		{
			Name "OUTLINE"
			Cull Front
			ZWrite On
			ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _OutlineColor;
			float _OutlineWidth;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);

				// 법선을 화면 방향으로 옮겨 그쪽으로 밀어낸다 (w 를 곱해 원근과 상관없이 같은 픽셀 수)
				float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
				float2 screenNormal = TransformViewToProjection(viewNormal.xy);
				float len = max(length(screenNormal), 0.0001);

				o.pos.xy += (screenNormal / len) * _OutlineWidth * 2.0 * o.pos.w / _ScreenParams.xy;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return _OutlineColor;
			}
			ENDCG
		}
	}

	Fallback Off
}

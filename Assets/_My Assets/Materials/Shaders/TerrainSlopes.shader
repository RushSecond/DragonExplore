Shader "Custom/Terrain" {
	Properties{
		_Color1("Flat/Grass Color", Color) = (1, 1, 1, 1)
		_Color2("Steep/Dirt Color", Color) = (1, 1, 1, 1)
		_Color3("Snow Color", Color) = (1, 1, 1, 1)
		_NormLow("Lower Normal Threshold", Range(0,1)) = 0.5
		_NormHigh("Higher Normal Threshold", Range(0,1)) = 0.5
		_GrassMax("Max-Grass Height", Float) = 0
		_GrassMin("Min-Grass Height", Float) = 200
		_SnowNormLow("Lower Normal Snow Threshold", Range(0,1)) = 0.5
		_SnowNormHigh("Higher Normal Snow Threshold", Range(0,1)) = 0.5
		_SnowMinFlat("Min-FlatSnow Height", Float) = 300
		_SnowMaxFlat("Max-FlatSnow Height", Float) = 500
		_SnowMinSlope("Min-SlopeSnow Height", Float) = 500
		_SnowMaxSlope("Max-SlopeSnow Height", Float) = 600
		_DissolveTexture("Dissolve Texture", 2D) = "white" {}
		_MaxDistance("Max Fade Distance", Float) = 10
		_MinDistance("Min Fade Distance", Float) = 0
	}
		SubShader{
		AlphaToMask On
		Tags{ "RenderType" = "Opaque" }
		Cull Off
		CGPROGRAM
#pragma surface surf Lambert
	struct Input {
		float2 uv_MainTex;
		float3 worldPos;
	};
	
	fixed4 _Color1;
	fixed4 _Color2;
	fixed4 _Color3;
	float _NormLow;
	float _NormHigh;
	float _GrassMax;
	float _GrassMin;

	float _SnowNormLow;
	float _SnowNormHigh;
	float _SnowMinFlat;
	float _SnowMaxFlat;
	float _SnowMinSlope;
	float _SnowMaxSlope;
	sampler2D _DissolveTexture;
	float _MaxDistance;
	float _MinDistance;

	void surf(Input IN, inout SurfaceOutput o) {
		half dissolve_value = tex2D(_DissolveTexture, IN.uv_MainTex).r;
		float yValue = IN.worldPos.y;

		fixed grassyness = clamp((_GrassMin - yValue) / (_GrassMin - _GrassMax), 0.0, 1.0);
		fixed flatness = clamp((o.Normal.y - _NormLow)/ (_NormHigh - _NormLow), 0.0, 1.0);
		fixed4 grassDirtColor = lerp(_Color2, _Color1, flatness * grassyness);

		// Snow mixing
		fixed snowFlatRange = _SnowMaxFlat - _SnowMinFlat;
		fixed flatSnowy = clamp((yValue - _SnowMinFlat) / snowFlatRange, 0.0, 1.0);
	
		fixed snowSlopeRange = _SnowMaxSlope - _SnowMinSlope;
		fixed slopeSnowiness = (yValue - _SnowMinSlope) / snowSlopeRange;
		fixed snowFlatness = clamp((o.Normal.y - _SnowNormLow) / (_SnowNormHigh - _SnowNormLow) + slopeSnowiness, 0.0, 1.0);

		//fixed snowRange = lerp(snowSlopeRange, snowFlatRange, snowFlatness);
		//fixed snowMin = lerp(_SnowMinSlope, _SnowMinFlat, snowFlatness);
		//fixed snowiness = clamp((yValue - snowMin) / snowRange, 0.0, 1.0);

		o.Albedo = lerp(grassDirtColor, _Color3, flatSnowy * snowFlatness);

		// distance fading
		float camDist = distance(IN.worldPos, _WorldSpaceCameraPos);
		float ratio = clamp((camDist - _MinDistance) / (_MaxDistance - _MinDistance), 0.0, 1.0);
		//float camDist = 30.0;

		 //Get how much we have to dissolve based on our dissolve texture
		clip(dissolve_value - ratio); //Dissolve!

	}
	ENDCG
	}
		Fallback "Diffuse"
}
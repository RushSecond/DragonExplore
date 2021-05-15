Shader "Custom/TwoColorEye" {
	Properties{
		//_Color ("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_TintColorA("Tint Color A", Color) = (1, 1, 1, 1)
		_TintColorB("Tint Color B", Color) = (1, 1, 1, 1)
		_TintMask("Tint Mask", 2D) = "black" {}
		_TintEyeLid("Tint Eye Lid", Color) = (1, 1, 1, 1)
		_TopEyeLid("Top Lid", Range(0,1)) = 0.0
		_BotEyeLid("Bottom Lid", Range(0,1)) = 0.0
		_EyeLidThickness("Lid Thickness", Range(0,0.03)) = 0.01
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _MainTex;
	sampler2D _TintMask;

	struct Input {
		float2 uv_MainTex;
		float2 uv_TintMask;
	};

	half _Glossiness;
	half _Metallic;
	fixed4 _Color;
	fixed4 _TintColorA;
	fixed4 _TintColorB;
	fixed4 _TintEyeLid;
	float _TopEyeLid;
	float _BotEyeLid;
	float _EyeLidThickness;

	void surf(Input IN, inout SurfaceOutputStandard o) {
		// Albedo comes from a texture tinted by color
		//fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

		fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex);
		fixed4 mask = tex2D(_TintMask, IN.uv_TintMask);
		fixed maskA = mask.r; // red channel is the first mask
		fixed maskB = mask.g; // green channel is the second mask
		fixed3 tint = fixed3(1.0, 1.0, 1.0);

		tint = lerp(tint, _TintColorB, 1 - maskA);
		tint = lerp(tint, _TintColorA, maskA);
		albedo.rgb *= tint;

		fixed3 black = fixed3(0.0, 0.0, 0.0);


		fixed eyeLidLoc = IN.uv_MainTex.g;
		
		//Bot Eyelid
		fixed vSign = max(0, sign(_BotEyeLid - eyeLidLoc));
		albedo.rgb = lerp(albedo.rgb, _TintEyeLid, vSign);

		fixed d = max(0, 1 - abs(eyeLidLoc - _BotEyeLid) / _EyeLidThickness);
		albedo.rgb = lerp(albedo.rgb, black, d * vSign);

		//Top Eyelid
		vSign = max(0, sign(eyeLidLoc - _TopEyeLid));
		albedo.rgb = lerp(albedo.rgb, _TintEyeLid, vSign);

		
		d = max(0, 1 - abs(eyeLidLoc - _TopEyeLid) / _EyeLidThickness);
		albedo.rgb = lerp(albedo.rgb, black, d * vSign);


		o.Albedo = albedo.rgb;
		// Metallic and smoothness come from slider variables
		o.Metallic = _Metallic;
		o.Smoothness = _Glossiness;
		o.Alpha = albedo.a;
	}
	ENDCG
	}
		//FallBack "Diffuse"
}

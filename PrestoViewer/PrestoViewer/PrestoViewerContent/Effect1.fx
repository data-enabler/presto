sampler s0;
texture hMap;
sampler hMapSampler = sampler_state {
	Texture = hMap;
	Filter = Point;
};
texture palette;
sampler paletteSampler = sampler_state { Texture = palette; };
float3 lightDir;
float texWidth;
float texHeight;

float4 PixelShaderFunction(float2 coords: TEXCOORD0) : COLOR0
{
	float dx = 1 / texWidth;
	float dy = 1 / texHeight;
	float3 average = { 0.333f, 0.333f, 0.334f };
	
	float4 upColor    = tex2D(hMapSampler, coords + float2(0.0f, -dy));
	float4 downColor  = tex2D(hMapSampler, coords + float2(0.0f, dy));
	float4 leftColor  = tex2D(hMapSampler, coords + float2(-dx, 0.0f));
	float4 rightColor = tex2D(hMapSampler, coords + float2(dx, 0.0f));

	float upValue    = dot(upColor.rgb, average)    * upColor.a;
	float downValue  = dot(downColor.rgb, average)  * downColor.a;
	float leftValue  = dot(leftColor.rgb, average)  * leftColor.a;
	float rightValue = dot(rightColor.rgb, average) * rightColor.a;

	float3 normal = { leftValue - rightValue, upValue - downValue, 1.0f };
    normal = normalize(normal);
	float shading = max(dot(normal, lightDir), 0.0f);

	float4 color = tex2D(s0, coords);
	float2 paletteCoords = { 0.1875f + 0.750f * (1.0f - shading), 0.5f };
	float4 paletteColor = tex2D(paletteSampler, paletteCoords);
	color.rgb = paletteColor.rgb * color.a;
	return color;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

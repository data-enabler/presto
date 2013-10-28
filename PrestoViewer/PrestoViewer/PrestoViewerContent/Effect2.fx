sampler s0;
texture nMap;
sampler nMapSampler = sampler_state {
	Texture = nMap;
	Filter = Point;
};
texture palette;
sampler paletteSampler = sampler_state {
	Texture = palette;
	Filter = Point;
};
float3 lightDir;

float4 PixelShaderFunction(float2 coords: TEXCOORD0) : COLOR0
{
	float3 normal = tex2D(nMapSampler, coords).xyz * 2.0f - 1.0f;
	float shading = max(dot(normal, lightDir), 0.0f);
	float4 color = tex2D(s0, coords);
	float2 paletteCoords = { color.g * (1.0f - shading), color.r };
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

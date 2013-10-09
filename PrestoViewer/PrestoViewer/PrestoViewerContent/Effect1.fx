sampler s0;
texture hMap;
sampler hMapSampler = sampler_state{Texture = hMap;};
float2 lightDir;

float4 PixelShaderFunction(float2 coords: TEXCOORD0) : COLOR0
{
	float4 color = tex2D(s0, coords);
	float4 hMapColor = tex2D(hMapSampler, coords + 0.1 * lightDir);
	color = hMapColor;
    return color;
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

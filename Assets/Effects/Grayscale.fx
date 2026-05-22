sampler image0 : register(s0);

float Intensity = 1.0f;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 col = tex2D(image0, coords);
    float gray = dot(col.rgb, float3(0.299, 0.587, 0.114));
    float3 g3 = float3(gray, gray, gray);
    
    col.rgb = lerp(col.rgb, g3, saturate(Intensity));
    
    return col;
}

technique Grayscale
{
    pass P0
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

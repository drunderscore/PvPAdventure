sampler image0 : register(s0);

float globalTime;
float3 borderColor;
float opacity;
float2 borderSize;
float outerEdgeFade;
float innerEdgeFade;
float pulseStrength;
float shimmerStrength;
float shimmerScale;
float shimmerSpeed;

float SmootherStep(float edge0, float edge1, float value)
{
    float t = saturate((value - edge0) / (edge1 - edge0));
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 tex = tex2D(image0, coords);

    float2 safeBorderSize = max(borderSize, float2(0.0001, 0.0001));
    float leftDepth = coords.x / safeBorderSize.x;
    float rightDepth = (1.0 - coords.x) / safeBorderSize.x;
    float topDepth = coords.y / safeBorderSize.y;
    float bottomDepth = (1.0 - coords.y) / safeBorderSize.y;
    float depth = min(min(leftDepth, rightDepth), min(topDepth, bottomDepth));
    float border = 1.0 - step(1.0, depth);
    float edgeAlpha = SmootherStep(0.0, outerEdgeFade, depth) * SmootherStep(0.0, innerEdgeFade, 1.0 - depth);
    float coreGlow = 1.0 - smoothstep(0.0, 0.36, abs(depth - 0.5));

    float pulse = 0.5 + 0.5 * sin(globalTime * 0.075);
    float shimmer = 0.5 + 0.5 * sin((coords.x + coords.y) * shimmerScale + globalTime * shimmerSpeed);
    float glow = 1.0 + pulse * pulseStrength + shimmer * shimmerStrength + coreGlow * 0.18;

    float3 color = borderColor * glow;
    float alpha = tex.a * sampleColor.a * opacity * border * edgeAlpha * lerp(0.82, 1.0, coreGlow);
    return float4(color * alpha, alpha);
}

technique t0
{
    pass p0
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}

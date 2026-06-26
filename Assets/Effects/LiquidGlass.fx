sampler uImage0 : register(s0);

texture uBackdropTexture;
sampler uBackdrop = sampler_state
{
    Texture = <uBackdropTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float3 uColor;
float3 uSecondaryColor;
float3 uBorderColor;
float uOpacity;
float uSaturation;
float uTime;
float2 uScreenSize;
float4 uPanelRect;
float2 uBackdropOffset;
float4 uShaderSpecificData; // x blur, y refraction, z gloss, w rim

float Hash21(float2 p)
{
    p = frac(p * float2(127.1, 311.7));
    p += dot(p, p + 19.19);
    return frac(p.x * p.y);
}

float Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(Hash21(i), Hash21(i + float2(1, 0)), u.x),
        lerp(Hash21(i + float2(0, 1)), Hash21(i + 1), u.x),
        u.y);
}

float3 SaturateColor(float3 c)
{
    float gray = dot(c, float3(0.299, 0.587, 0.114));
    return lerp(gray.xxx, c, uSaturation);
}

float3 BlurBackdrop(float2 uv, float radius)
{
    float2 px = radius * 1.65 / uScreenSize;
    float3 c = tex2D(uBackdrop, uv).rgb * 0.28;

    c += tex2D(uBackdrop, uv + float2( px.x, 0)).rgb * 0.12;
    c += tex2D(uBackdrop, uv + float2(-px.x, 0)).rgb * 0.12;
    c += tex2D(uBackdrop, uv + float2(0,  px.y)).rgb * 0.12;
    c += tex2D(uBackdrop, uv + float2(0, -px.y)).rgb * 0.12;

    c += tex2D(uBackdrop, uv + float2( px.x,  px.y)).rgb * 0.06;
    c += tex2D(uBackdrop, uv + float2(-px.x,  px.y)).rgb * 0.06;
    c += tex2D(uBackdrop, uv + float2( px.x, -px.y)).rgb * 0.06;
    c += tex2D(uBackdrop, uv + float2(-px.x, -px.y)).rgb * 0.06;

    return c;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0, float2 screenPos : VPOS) : COLOR0
{
    float4 mask = tex2D(uImage0, coords);
    if (mask.a <= 0.001)
        return 0;

    float2 panelUv = saturate((screenPos - uPanelRect.xy) / uPanelRect.zw);
    float2 screenUv = (screenPos + uBackdropOffset) / uScreenSize;

    float blur = uShaderSpecificData.x;
    float refraction = uShaderSpecificData.y;
    float glossPower = uShaderSpecificData.z;
    float rimPower = uShaderSpecificData.w;

    float n1 = Noise(panelUv * 8.0 + float2(uTime * 0.045, -uTime * 0.035));
    float n2 = Noise(panelUv * 18.0 + float2(-uTime * 0.055, uTime * 0.04));
    float2 wave = float2(
        sin(panelUv.y * 34.0 + panelUv.x * 10.0 + uTime * 0.7),
        sin(panelUv.x * 27.0 - panelUv.y * 7.0 - uTime * 0.55));
    float2 liquid = (float2(n1, n2) - 0.5) * 0.88 + wave * 0.10;

    float3 backdrop = SaturateColor(BlurBackdrop(screenUv + liquid * (refraction / uScreenSize), blur)) * 0.94 + 0.004;
    float3 tint = lerp(uColor, uSecondaryColor, saturate(panelUv.y + (n1 - 0.5) * 0.22));
    float3 glass = lerp(backdrop, tint, 0.18);

    float edgeDist = min(min(panelUv.x, panelUv.y), min(1.0 - panelUv.x, 1.0 - panelUv.y));
    float rim = 1.0 - smoothstep(0.0, 0.045, edgeDist);
    float inner = 1.0 - smoothstep(0.025, 0.13, edgeDist);
    float topShine = smoothstep(0.24, 0.0, panelUv.y);
    float diagonal = dot(panelUv - 0.5, normalize(float2(0.82, -0.58)));
    float gloss = exp(-pow(diagonal + 0.16, 2.0) / 0.010) + exp(-pow(panelUv.y - 0.10, 2.0) / 0.012);
    float caustic = smoothstep(0.68, 1.0, sin(panelUv.x * 30.0 + panelUv.y * 19.0 + uTime * 0.45 + n2 * 5.5) * 0.5 + 0.5);

    glass += tint * (1.0 - saturate(dot(panelUv * 2.0 - 1.0, panelUv * 2.0 - 1.0) * 0.58)) * 0.04;
    glass += tint * caustic * 0.014;
    glass += uBorderColor * rim * 0.22 * rimPower;
    glass += 1.0.xxx * (inner * 0.028 + topShine * 0.07 + gloss * 0.07 * glossPower);
    glass *= 1.0 - smoothstep(0.54, 1.0, panelUv.y) * 0.07;

    float alpha = mask.a * sampleColor.a * uOpacity;
    return float4(saturate(glass) * sampleColor.rgb * alpha, alpha);
}

technique Technique1
{
    pass Pass0
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}

sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);

float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;

float4 IRSightPixelShader(float2 coords : TEXCOORD0) : COLOR0
{
    float4 source = tex2D(uImage0, coords);
    float gray = source.r * 0.299 + source.g * 0.587 + source.b * 0.114;

    // 压低暗部、抬高亮部：被白色照明的NPC趋近亮白，环境暗部则
    // 更接近黑色，同时保留物块轮廓所需的中间灰阶。
    gray = saturate((gray - 0.035) * 1.32);
    gray = pow(gray, 0.55);
    source.rgb = float3(gray, gray, gray);
    return source;
}

technique Technique1
{
    pass IRSightPass
    {
        PixelShader = compile ps_2_0 IRSightPixelShader();
    }
}

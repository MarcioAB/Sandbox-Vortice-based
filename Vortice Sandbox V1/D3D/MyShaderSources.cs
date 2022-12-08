
#nullable disable

namespace D3D_Mama;



public class MyShaderSources
{
    public const int vertexSlot = 3; // 

    // MyModel_L0 & MyModel_L0A
    // Receive 2D vertices (x,y) from IA vertex buffer
    // Shader add z=0. Color white, hardcoded.
    // pos.w = 1 meaning NO scale
    public const string float2_NoMVS1 = @"
float4 VSMain(float2 pos : entryA) : SV_POSITION
{ return float4(pos,0,1); }

float3 PSMain() : SV_TARGET
{ return float3(1,1,1); }
";
    // Idem previous ( float2_NoMVS1 ) + strucs
    public const string float2_NoMVS2 = @"
struct Input 
{ float2 pos : entryA; };

struct Output 
{ float4 pos : SV_POSITION; };

Output VSMain(Input v1) {
  Output v2;
  v2.pos = float4(v1.pos,0,1);
  return v2; }

float3 PSMain() : SV_Target
{ return float3(1,1,1); }
";
    // Idem previous ( float2_NoMVS2 ) + COLOR
    public const string float2_NoMVS3 = @"
// slot1 per vertex: float2 XY

struct Input { 
  float2 pos : entryA; };

struct Output { 
  float4 pos : SV_POSITION; 
  float4 cor : COLOR; };

Output VSMain(Input v1) {
  Output v2;
  v2.pos = float4(v1.pos,0,1);
  v2.cor = float4(1,1,1,0);
  return v2; }

float4 PSMain(Output v1) : SV_Target
{ return v1.cor; }
";

    // MyModel_L1, MyModel_L2, MyModel_L4, MyModel_L4A
    // idem previous (float2_NoMVS3) + MVP
    // Receive MVP (float4x4) via constant buffer on register 0 and multiply by position
    public const string float2 = @"
// slot1 per vertex: float2 XY

cbuffer _ : register(b0) { float4x4 MVP;} // aligned with D3DMama.Slots.MVP_registerB

struct Input {
  float2 posXY : entryA; //See this InputElementDesc semantics for more information
};

struct Output {
  float4 posXYZW : SV_POSITION;
  float4 cor : COLOR; };

Output VSMain(Input v1) { 
  Output v2;
  v2.posXYZW = mul(MVP, float4(v1.posXY,0,1));
  v2.cor = float4(1,1,1,0);
  return v2; }

float4 PSMain(Output v1) : SV_Target
{ return v1.cor; }
";



    public const string float3B = @"
// TEXTURE

cbuffer _ : register(b0) { float4x4 MVP; }; // aligned with D3DMama.Slots.MVP_registerB

struct Input { 
  float3 posXYZ : entryA; // xyz coordinates. See this InputElementDesc semantics for more information
  float2 texUV : entryB; // uv coordinates. See this InputElementDesc semantics for more information
};

struct Output {
    float4 posXYZW : SV_POSITION;
    float2 texUV : TEXCOORD;
};

Texture2D MamaTexture: register(t0); // aligned with D3DMama.Slots.Texture_registerT
SamplerState Texture9Sampler: register(s0); // aligned with D3DMama.Slots.Sampler_registerS

Output VSMain(Input v1)
{
    Output v2;
    v2.posXYZW = mul(MVP, float4(v1.posXYZ,1));
    v2.texUV = v1.texUV * 1.0f;
    return v2;
}

float4 PSMain(Output v1) : SV_TARGET
{ return MamaTexture.Sample(Texture9Sampler, v1.texUV); }
";



    public const string float3C = @"
// slot1 (per vertex): float3 XYZ

cbuffer _ : register(b0) { float4x4 MVP;} // aligned with D3DMama.Slots.MVP_registerB

struct Input {
  float3 posXYZ : entryA;
  float2 texUV : entryB;
};

struct Output {
  float4 posXYZW : SV_POSITION;
  float2 texUV : TEXCOORD;
};

Texture2D MamaTexture: register(t0); // aligned with D3DMama.Slots.Texture_registerT
SamplerState Texture9Sampler: register(s0); // aligned with D3DMama.Slots.Sampler_registerS

Output VSMain(Input v1) {
  Output v2;
  v2.posXYZW = mul(MVP, float4(v1.posXYZ,1));
  v2.texUV = v1.texUV * 1.0f;
  return v2; }

float3 PSMain() : SV_Target { return float3(0,1,1); }
";


    // idem previous ( float2 ) + SV_InstanceID
    // coord z vem da instancia
    public const string float3_Instance = @"
// slot1 per vertex: float2 XY
// slot2 per instance: float Z

cbuffer _ : register(b0) { float4x4 MVP;} // aligned with D3DMama.Slots.MVP_registerB

struct Input { 
  float3 posXYZ : entryA; // xyz coordinate: See this InputElementDesc semantics for more information
  float2 texUV : entryB; // uv coordinates. See this InputElementDesc semantics for more information
  float posY : entryInstance; // Y coordinate: See this InputElementDesc semantics for more information
};

struct Output { 
  float4 posXYZW : SV_POSITION;
  float2 texUV : TEXCOORD; };

Texture2D MamaTexture: register(t0); // aligned with D3DMama.Slots.Texture_registerT
SamplerState Texture9Sampler: register(s0); // aligned with D3DMama.Slots.Sampler_registerS

Output VSMain(Input v1) { 
  Output v2;
  v1.posXYZ.y = v1.posXYZ.y + v1.posY;
  v2.posXYZW = mul(MVP, float4(v1.posXYZ,1));
  v2.texUV = v1.texUV * 1.0f;
  return v2; }

float4 PSMain(Output v1) : SV_Target
{ return MamaTexture.Sample(Texture9Sampler, v1.texUV); }
";

    // idem previous ( float2 ) + SV_InstanceID
    // coord z vem da instancia
    public const string float2_InstanceC = @"
// slot1 per vertex: float2 XY
// slot2 per instance: float Z

cbuffer _ : register(b0) { float4x4 MVP;} // aligned with D3DMama.Slots.MVP_registerB

struct Input { 
  float2 posXY : entryA; // xy coordinate: See this InputElementDesc semantics for more information
  float posZ : entryB_instance; // z coordinate: See this InputElementDesc semantics for more information
  uint mama1 : SV_InstanceID; // number of current instance
};

struct Output { 
  float4 posXYZW : SV_POSITION;
  float3 cor : COLOR; };

Output VSMain(Input v1) { 
  Output v2;
  v2.posXYZW = mul(MVP, float4(v1.posXY,v1.posZ,1));
  v2.cor = float3(1,1,1);
  if(v1.mama1 == 0) v2.cor = float3(1,0,0);
  return v2; }

float3 PSMain(Output v1) : SV_Target
{ return v1.cor; }
";


    // Idem previous (float2_Instance)
    // Receive 2D vertices and MVP, etc ... (idem anterior)
    // Receive 3D position from instances and add with position
    // pos1.w = 0 because pos.w = 1. The sum must be 1 or NO scale
    public const string float2A_Instance = @"
// slot1 (per vertex): float2 XY
// slot2 (per instance): float3 XYZ

cbuffer _ : register(b0) { float4x4 MVP;} // aligned with D3DMama.Slots.MVP_registerB

struct Input { 
  float2 pos : entryA; // xy coordinate. See this InputElementDesc semantics for more information
  float3 pos1 : entryInstance; // xyz coordinate. See this InputElementDesc semantics for more information
  uint mama1 : SV_InstanceID; };

struct Output { 
  float4 pos : SV_POSITION;
  float3 cor : COLOR; };

Output VSMain(Input v1) { 
  Output v2;
  v2.pos = mul(MVP, (float4(v1.pos,0,1) + float4(v1.pos1,0)));
  v2.cor = float3(1,1,1);
  if(v1.mama1 == 0) v2.cor = float3(1,0,0);
  return v2; }

float3 PSMain(Output v1) : SV_Target
{ return v1.cor; }
";



    // Receive 2D vertices (x,y), etc ... (idem anterior)
    // Receive MVP (float4x4) via constant buffer on register 0 and multiply by position
    public const string float3 = @"
// slot1 (per vertex): float3 XYZ

cbuffer _ : register(b0) { float4x4 MVP;} // aligned with D3DMama.Slots.MVP_registerB

struct Input {
  float3 posXYZ : entryA;
};

struct Output {
  float4 posXYZ : SV_POSITION;
};

Output VSMain(Input v1) {
  Output v2;
  v2.posXYZ = mul(MVP, float4(v1.posXYZ,1));
  return v2; }

float3 PSMain() : SV_Target { return float3(1,1,1); }
";


    // Receive 2D vertices (x,y), etc ... (idem anterior)
    // Receive MVP (float4x4) via constant buffer on register 0 and multiply by position
    public const string float2D = @"
cbuffer _ : register(b0) { float4x4 MVP;} // aligned with D3DMama.Slots.MVP_registerB

struct Input {
    float2 pos : entryA;
    float3 cor : entryB; };

struct Output {
    float4 pos : SV_POSITION;
    float3 cor; };

Output VSMain(Input v1) {
  Output v2;
  v2.cor = v1.cor;
  v2.pos = mul(MVP, float4(v1.pos,0,1));
  return v2; }

float3 PSMain(Output v1) : SV_Target
{ return float3(v1.cor); }
";

    public const string float3D = @"
//
cbuffer _ : register(b0) { float4x4 MVP;} // aligned with D3DMama.Slots.MVP_registerB

struct Input {
    float3 pos : entryA;
    float3 cor : entryB; };

struct Output {
    float4 pos : SV_POSITION;
    float3 cor : COLOR; };

Output VSMain(Input v1)
{
  Output v2;
  v2.cor = v1.cor;
  v2.pos = mul(MVP, float4(v1.pos,1));
  return v2;
}

float3 PSMain(Output v1) : SV_Target
{ return v1.cor; }
";

    // end
}

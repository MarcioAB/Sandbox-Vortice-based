using System.Numerics;
using System.Runtime.CompilerServices;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI; // Necessary for DXGI.Format on IASetIndexBuffer

using D3D_Mama;
using D3D11_Mama;
using static D3D11_Mama.D3D11_Base;

#nullable disable

namespace D3D11_MyDrawSets;

/* Por ora o DrawSetBase vai ser o level 3 (MVP + Indexed + MultiData) ou seja, sem Instanced
 * O DrawSetBaseInstanced, level 4, fica para o caso de uso de instancias.
 * 
 * level 0: nothing.
 * level 1: MVP introduction
 * level 2: level 1 + Indexing introduction
 * level 3: level 2 + Multidata introduction
 * level 4: level 3 + Instancing introduction
 */

// L0 SandBox introduction: Non-INDEXED
// direct vertices coods with format <T> { float2,3,4, vector2,3,4, others }
/// <summary>non-indexed</summary>
public class DrawSetBase_NoMulti_NoInst_NoTex_NoMVP_NoIndex : D3D11_ShaderCompiler
{
    public int qtd_vertices;
    public PrimitiveTopology tipo;
    public ID3D11Buffer vertexBuffer;
    public int rasterizerNDX;
    public ID3D11InputLayout inputLayout;
    public int stride;

    public DrawSetBase_NoMulti_NoInst_NoTex_NoMVP_NoIndex(string sh) : base(sh)
    {
        rasterizerNDX = (int)RenderMode.Solid; // default. Use SetRenderMode to change
    }
    public void SetVertexData<T>((PrimitiveTopology tipo, T[] vertex) data) where T : unmanaged
    {
        qtd_vertices = data.vertex.Length;
        tipo = data.tipo;
        stride = Unsafe.SizeOf<T>(); // Obs: managed size. (float,float,float) = 16 bytes

        vertexBuffer = DEV11.CreateBuffer(data.vertex, BindFlags.VertexBuffer);
        vertexBuffer.DebugName = "Vbuffer1";

        var vertex1Format = InputDesc.GetFormatFromType<T>();
        var inputElements = D3D11_InputDesc.GetInputA1(vertex1Format);
        inputLayout = DEV11.CreateInputLayout(inputElements, blob: VS11_ByteCode);
        inputLayout.DebugName = "InputLayout1";
    }
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);

        // IA stage
        DC11.IASetInputLayout(inputLayout);
        DC11.IASetPrimitiveTopology(tipo);
        DC11.IASetVertexBuffer(slot: MyShaderSources.vertexSlot, vertexBuffer, stride); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.

        // Rasterizer stage
        DC11.RSSetState(rasterizers[rasterizerNDX]);

        // Draw ( DrawInstanced to be equal D3D12 )
        DC11.DrawInstanced(vertexCountPerInstance: qtd_vertices, instanceCount: 1, startVertexLocation: 0, startInstanceLocation: 0);
    }

    public void Dispose()
    {
        vertexBuffer.Dispose();
        inputLayout.Dispose();
        Dispose_Shader();
    }
}

// L1 SandBox introduction: INDEXED
// indexed vertices
/// <summary>indexed</summary>
public class DrawSetBase_NoMulti_NoInst_NoTex_NoMVP : D3D11_ShaderCompiler
{
    public int qtd_indices;
    public PrimitiveTopology tipo;
    public ID3D11Buffer vertexBuffer;
    public int rasterizerNDX;
    public ID3D11InputLayout inputLayout;
    public int stride;
    public ID3D11Buffer indexBuffer;

    public DrawSetBase_NoMulti_NoInst_NoTex_NoMVP(string sh) : base(sh)
    {
        rasterizerNDX = (int)RenderMode.Solid;
    }
    public void SetVertexData<T>((PrimitiveTopology tipo, T[] vertex, ushort[] index) data) where T : unmanaged
    {
        qtd_indices = data.index.Length;
        tipo = data.tipo;

        indexBuffer = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);

        vertexBuffer = DEV11.CreateBuffer(data.vertex, BindFlags.VertexBuffer);
        stride = Unsafe.SizeOf<T>();
        var vertex1Format = InputDesc.GetFormatFromType<T>();
        var inputElements = D3D11_InputDesc.GetInputA1(vertex1Format);
        inputLayout = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);
    }
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);

        // IA stage
        DC11.IASetInputLayout(inputLayout);
        DC11.IASetPrimitiveTopology(tipo);
        DC11.IASetVertexBuffer(slot: MyShaderSources.vertexSlot, vertexBuffer, stride, offset: 0);
        DC11.IASetIndexBuffer(indexBuffer, Format.R16_UInt, offset: 0); // R16_Uint = ushort index, capable of 65K indices. If necessary more, use R32_Uint = uint index =  4 billion indeces.

        // Rasterizer stage
        DC11.RSSetState(rasterizers[rasterizerNDX]);

        // Draw ( DrawInstanced to be equal D3D12 )
        DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices, instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
    }

    public void Dispose()
    {
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        inputLayout.Dispose();
        Dispose_Shader();
    }
}

// L2 SandBox introduction: MVP
// Model * View * Projection transformation
/// <summary>mvp + indexed</summary>
public class DrawSetBase_NoMulti_NoInst_NoTex : D3D11_ShaderCompiler
{
    public int qtd_indices;
    public Matrix4x4 model;
    public PrimitiveTopology tipo;
    public int rasterizerNDX;
    public ID3D11InputLayout inputLayout;
    public ID3D11Buffer vertexBuffer;
    public int stride;
    public ID3D11Buffer indexBuffer;
    public D3D11_MVP v1;

    public DrawSetBase_NoMulti_NoInst_NoTex(string sh) : base(sh)
    {
        rasterizerNDX = (int)RenderMode.Solid; // default. Use SetRenderMode to change
        model = Matrix4x4.Identity;
        v1 = new D3D11_MVP();
    }
    public void SetVertexData<T>((PrimitiveTopology tipo, T[] vertex, ushort[] index) data) where T : unmanaged
    {
        qtd_indices = data.index.Length;
        tipo = data.tipo;

        indexBuffer = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);

        vertexBuffer = DEV11.CreateBuffer(data.vertex, BindFlags.VertexBuffer);
        stride = Unsafe.SizeOf<T>();
        var inputElements = D3D11_InputDesc.GetInputA1(InputDesc.GetFormatFromType<T>());
        inputLayout = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);
    }
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);

        // MVP constant buffer
        DC11.VSSetConstantBuffer(Slots.MVP_registerB, v1.UploadMVP(model * Camera1.ViewProjection)); // MVP deve estar no slot 0 do VS: [HLSL CODE] cbuffer params : register(b0) {float4x4 worldViewProjection;};

        // IA stage (Input-Assemble)
        DC11.IASetInputLayout(inputLayout);
        DC11.IASetPrimitiveTopology(tipo);
        DC11.IASetVertexBuffer(slot: MyShaderSources.vertexSlot, vertexBuffer, stride, offset: 0); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.
        DC11.IASetIndexBuffer(indexBuffer, Format.R16_UInt, offset: 0); // R16_Uint = ushort index, capable of 65K indices. If necessary more, use R32_Uint = uint index =  4 billion indeces.

        // Rasterizer stage
        DC11.RSSetState(rasterizers[rasterizerNDX]);

        // Draw ( DrawInstanced to be equal D3D12 )
        DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices, instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
    }

    public void Dispose()
    {
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        inputLayout.Dispose();
        v1.Dispose_MVP();
        Dispose_Shader();
    }
}

// L3 SandBox introduction: TEXTURE
// (A) 1 mixed dataset: float3 vertex coords + float2 UV coords
// (B) 2 datasets: one for vertex coods <T1> and other for UV coords <T2>
/// <summary>texture + indexed + mvp, 1 mixed coords dataset</summary>
public class DrawSetBase_NoMulti_NoInst1 : D3D11_ShaderCompiler
{
    public int qtd_indices;
    public Matrix4x4 model;
    public PrimitiveTopology tipo;
    public int rasterizerNDX;
    public ID3D11InputLayout inputLayout;
    public ID3D11Buffer vertexBuffer;
    public int stride;
    public ID3D11Buffer indexBuffer;
    public D3D11_MVP v1;
    public ID3D11ShaderResourceView textureSRV1;
    public ID3D11SamplerState textureSampler;
    //public ID3D11Texture2D texture; // class field em caso de ser necessario usar no Device.WriteTexture(..)

    public DrawSetBase_NoMulti_NoInst1(string sh) : base(sh)
    {
        rasterizerNDX = (int)RenderMode.Solid; // default. Use SetRenderMode to change
        model = Matrix4x4.Identity;
        v1 = new D3D11_MVP();

        // Texture sampler state para ser usado no Draw() ... DeviceContext.PSSetSampler(..)
        textureSampler = DEV11.CreateSamplerState(samplerDesc: SamplerDescription.PointWrap);
    }

    public void Texture99((int width, int height, byte[] byteArray) input1)
    {
        var t1 = DEV11.CreateTexture2D(format: rtvFormat, width: input1.width, height: input1.height, initialData: input1.byteArray);
        textureSRV1 = DEV11.CreateShaderResourceView(resource: t1);
        t1.Dispose();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }
    // Este SetVertexData espera que vertex tenha interleaved data (float3 vertex coords + float2 UV coords)
    // Dessa forma, esses Formats para o inputElements ja estão hardcoded aqui.
    public void SetVertexData<T>((PrimitiveTopology tipo, T[] vertex, ushort[] index) data) where T : unmanaged
    {
        qtd_indices = data.index.Length;
        tipo = data.tipo;
        indexBuffer = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);
        vertexBuffer = DEV11.CreateBuffer(data.vertex, BindFlags.VertexBuffer);
        stride = Unsafe.SizeOf<T>();
        // Como os dados (vertex coords + UV coords) estao interleaved é necessario informar esse format aqui (hardcoded).
        var vertex1Format = Format.R32G32B32_Float; 
        var vertex2Format = Format.R32G32_Float;
        var inputElements = D3D11_InputDesc.GetInputA1B1(vertex1Format, vertex2Format);
        inputLayout = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);
    }
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);

        DC11.VSSetConstantBuffer(Slots.MVP_registerB, v1.UploadMVP(model * Camera1.ViewProjection)); // MVP deve estar no slot 0 do VS: [HLSL CODE] cbuffer params : register(b0) {float4x4 worldViewProjection;};

        //DC11_4.WriteTexture(resource: _texture, arraySlice: 0, mipLevel: 0, data: pixels);
        DC11.PSSetShaderResource(slot: Slots.Texture_registerT, textureSRV1); // ou textureSRV2
        DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler);

        // IA stage
        DC11.IASetInputLayout(inputLayout);
        DC11.IASetPrimitiveTopology(tipo);
        DC11.IASetVertexBuffer(slot: MyShaderSources.vertexSlot, vertexBuffer, stride, offset: 0); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.
        DC11.IASetIndexBuffer(indexBuffer, Format.R16_UInt, offset: 0); // R16_Uint = ushort index, capable of 65K indices. If necessary more, use R32_Uint = uint index =  4 billion indeces.

        // Rasterizer stage
        DC11.RSSetState(rasterizers[rasterizerNDX]);

        // Draw ( DrawInstanced to be equal D3D12 )
        DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices, instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
    }
    public void Dispose()
    {
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        inputLayout.Dispose();
        v1.Dispose_MVP();
        textureSRV1.Dispose();
        textureSampler.Dispose();
        Dispose_Shader();
    }
}
/// <summary>texture + indexed + mvp, 2 coords dataset</summary>
public class DrawSetBase_NoMulti_NoInst2 : D3D11_ShaderCompiler
{
    public int qtd_indices; // precisa ser public porque é usado para calcular o numero total de vertices no Models.
    public Matrix4x4 model; // precisa ser public porque esse input é feito no Models.
    public PrimitiveTopology tipo;
    public ID3D11Buffer[] vertexBuffers;
    public ID3D11Buffer indexBuffer;
    public int rasterizerNDX;
    public ID3D11InputLayout inputLayout;
    public int[] strides;
    public int[] offsets;
    public D3D11_MVP v1;
    public ID3D11ShaderResourceView textureSRV1;
    public ID3D11SamplerState textureSampler;
    //public ID3D11Texture2D texture; // class field em caso de ser necessario usar no Device.WriteTexture(..)

    public DrawSetBase_NoMulti_NoInst2(string sh) : base(sh)
    {
        rasterizerNDX = (int)RenderMode.Solid; // default. Use SetRenderMode to change
        model = Matrix4x4.Identity;
        v1 = new D3D11_MVP();

        // Texture sampler state para ser usado no DeviceContext.PSSetSampler(..)
        textureSampler = DEV11.CreateSamplerState(samplerDesc: SamplerDescription.PointWrap);

        // Se não vai mais mudar a textura, então não é necessario manter o resource ID3D11Texture2D 
        // Obs: use Device.WriteTexture(..) para mudar a textura, tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }
    public void Texture99((int width, int height, byte[] byteArray) input1)
    {
        var t1 = DEV11.CreateTexture2D(format: rtvFormat, width: input1.width, height: input1.height, initialData: input1.byteArray);
        textureSRV1 = DEV11.CreateShaderResourceView(resource: t1);
        t1.Dispose();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }
    public void SetVertexData<T1, T2>((PrimitiveTopology tipo, T1[] vertexCoords, ushort[] index, T2[] textureCoords) data) where T1 : unmanaged where T2 : unmanaged
    {
        if (data.vertexCoords.Length != data.textureCoords.Length) throw new Exception();
        qtd_indices = data.index.Length;
        tipo = data.tipo;
        indexBuffer = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);
        vertexBuffers = new ID3D11Buffer[]
        {
            DEV11.CreateBuffer(data.vertexCoords, BindFlags.VertexBuffer), // only vertex coordinates in this vertex buffer
            DEV11.CreateBuffer(data.textureCoords, BindFlags.VertexBuffer) // only vertex uv texture coordinates in this vertex buffer
        };
        strides = new int[] { Unsafe.SizeOf<T1>(), Unsafe.SizeOf<T2>() };
        offsets = new int[] { 0, 0 };
        var vertex1Format = InputDesc.GetFormatFromType<T1>();
        var vertex2Format = InputDesc.GetFormatFromType<T2>();
        var inputElements = D3D11_InputDesc.GetInputA1B2(vertex1Format, vertex2Format);
        inputLayout = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);
    }
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        //DC11_4.WriteTexture(resource: _texture, arraySlice: 0, mipLevel: 0, data: pixels);
        DC11.PSSetShaderResource(slot: Slots.Texture_registerT, textureSRV1);
        DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler);

        DC11.VSSetShader(vertexShader: VS11);
        DC11.PSSetShader(pixelShader: PS11);

        DC11.VSSetConstantBuffer(slot: Slots.MVP_registerB, constantBuffer: v1.UploadMVP(model * Camera1.ViewProjection)); // MVP deve estar no slot 0 do VS: [HLSL CODE] cbuffer params : register(b0) {float4x4 worldViewProjection;};

        // IA stage
        DC11.IASetInputLayout(inputLayout: inputLayout);
        DC11.IASetPrimitiveTopology(topology: tipo);
        DC11.IASetVertexBuffers(firstSlot: MyShaderSources.vertexSlot, vertexBuffers: vertexBuffers, strides: strides, offsets: offsets); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.
        DC11.IASetIndexBuffer(indexBuffer, Format.R16_UInt, offset: 0);

        // Rasterizer stage
        DC11.RSSetState(rasterizerState: rasterizers[rasterizerNDX]);

        // Draw ( DrawInstanced to be equal D3D12 )
        DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices, instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
    }
    public void Dispose()
    {
        vertexBuffers[0].Dispose();
        vertexBuffers[1].Dispose();
        indexBuffer.Dispose();
        inputLayout.Dispose();
        v1.Dispose_MVP();
        textureSRV1.Dispose();
        textureSampler.Dispose();
        Dispose_Shader();
    }
}

// L4 SandBox introduction: INSTANCE
// (A) 1 mixed dataset: float3 vertex coords + float2 UV coords
// (B) 2 dataset: one for vertex coods <T1>, other for UV coords <T2>
/// <summary>instanced + texture + indexed + mvp, 1 mixed coods dataset</summary>
public class DrawSetBase_NoMulti1 : D3D11_ShaderCompiler
{
    public D3D11_MVP mvp1;
    public ID3D11SamplerState textureSampler;

    public int qtd_indices; // precisa ser public porque é usado para calcular o numero total de vertices no Models.
    public int qtd_instances;
    public Matrix4x4 model; // precisa ser public porque esse input é feito no Models.
    public PrimitiveTopology tipo;
    public int rasterizerNDX;
    public ID3D11InputLayout inputLayout;
    public ID3D11Buffer[] vertexBuffer;
    public int[] strides;
    public int[] offsets;
    public ID3D11Buffer indexBuffer;
    public ID3D11ShaderResourceView textureSRV1;

    //public ID3D11Texture2D texture; // class field em caso de ser necessario usar no Device.WriteTexture(..)

    public DrawSetBase_NoMulti1(string shader) : base(shader)
    {
        model = Matrix4x4.Identity;
        rasterizerNDX = (int)RenderMode.Solid; // Use SetRenderMode to change
        mvp1 = new D3D11_MVP();

        // Texture sampler state para ser usado no DeviceContext.PSSetSampler(..)
        textureSampler = DEV11.CreateSamplerState(samplerDesc: SamplerDescription.PointWrap);
        textureSampler.DebugName = "SamplerState (unico)";
        //textureSampler = Enumerable.Repeat(y, qtd).ToArray();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }
    public void Texture99((int width, int height, byte[] byteArray) input1)
    {
        var t1 = DEV11.CreateTexture2D(format: rtvFormat, width: input1.width, height: input1.height, initialData: input1.byteArray);
        textureSRV1 = DEV11.CreateShaderResourceView(resource: t1);
        t1.Dispose();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }
    // Este SetVertexData espera que vertex tenha interleaved data (float3 vertex coords + float2 UV coords)
    // Dessa forma, os Format para o inputElements ja estão hardcoded aqui.
    public void SetVertexData<T1, T2>((PrimitiveTopology tipo, T1[] vertex, ushort[] index) data, T2[] instances) where T1 : unmanaged where T2 : unmanaged
    {
        qtd_indices = data.index.Length;
        qtd_instances = instances.Length;
        tipo = data.tipo;

        indexBuffer = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);
        indexBuffer.DebugName = "index buffer";

        vertexBuffer = new ID3D11Buffer[]
        {
            DEV11.CreateBuffer(data.vertex, BindFlags.VertexBuffer),
            DEV11.CreateBuffer(instances, BindFlags.VertexBuffer)
        };
        vertexBuffer[0].DebugName = "vertex buffer";
        vertexBuffer[1].DebugName = "instance buffer";


        strides = new int[] { Unsafe.SizeOf<T1>(), Unsafe.SizeOf<T2>() };
        offsets = new int[] { 0, 0 };

        // Como os dados (vertex coords + UV coords) estao interleaved é necessario colocar o format hard coded.
        var vertex1Format = Format.R32G32B32_Float; // float3
        var vertex2Format = Format.R32G32_Float; // float2
        var instanceFormat = InputDesc.GetFormatFromType<T2>();
        var inputElements = D3D11_InputDesc.GetInputA1B1I2(slot1Format1: vertex1Format, slot1Format2: vertex2Format, slot2InstanceFormat1: instanceFormat);
        inputLayout = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);
        inputLayout.DebugName = "input layout";
    }
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);
        DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler);

        // MVP deve estar no slot 0 do VS: [HLSL CODE] cbuffer params : register(b0) {float4x4 worldViewProjection;}
        DC11.VSSetConstantBuffer(Slots.MVP_registerB, mvp1.UploadMVP(model * Camera1.ViewProjection));

        //DC11_4.WriteTexture(resource: _texture, arraySlice: 0, mipLevel: 0, data: pixels);
        DC11.PSSetShaderResource(slot: Slots.Texture_registerT, textureSRV1); // ou textureSRV2[n2]
                                                                              //DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler[n1]);
        // IA stage
        DC11.IASetInputLayout(inputLayout);
        DC11.IASetPrimitiveTopology(topology: tipo);
        DC11.IASetVertexBuffers(firstSlot: MyShaderSources.vertexSlot, vertexBuffer, strides, offsets); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.
        DC11.IASetIndexBuffer(indexBuffer, Format.R16_UInt, offset: 0); // R16_Uint = ushort index, capable of 65K indices. If necessary more, use R32_Uint = uint index =  4 billion indeces.

        // Rasterizer stage
        //rasterizer.Description = CullMode.None;
        DC11.RSSetState(rasterizers[rasterizerNDX]);

        // Draw ( DrawIndexedInstanced to be equal D3D12 )
        DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices, instanceCount: qtd_instances, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);

    }
    public void Dispose()
    {
        vertexBuffer[0].Dispose();
        vertexBuffer[1].Dispose();
        indexBuffer.Dispose();
        inputLayout.Dispose();
        textureSRV1.Dispose();
        textureSampler.Dispose();
        mvp1.Dispose_MVP();
        Dispose_Shader();
    }
}
///<summary>instanced + texture + indexed + mvp, 2 coords dataset</summary>
public class DrawSetBase_NoMulti2 : D3D11_ShaderCompiler
{
    public D3D11_MVP mvp1;
    public int qtd_indices; // precisa ser public porque é usado para calcular o numero total de vertices no Models.
    public int qtd_instances;
    public Matrix4x4 model; // precisa ser public porque esse input é feito no Models.
    public PrimitiveTopology tipo;
    public int rasterizerNDX;
    public ID3D11InputLayout inputLayout;
    public ID3D11Buffer[] vertexBuffer;
    public int[] strides;
    public int[] offsets;
    public ID3D11Buffer indexBuffer;

    public ID3D11ShaderResourceView textureSRV1;
    public ID3D11SamplerState textureSampler;
    //public ID3D11Texture2D texture; // class field em caso de ser necessario usar no Device.WriteTexture(..)

    public DrawSetBase_NoMulti2(string shader) : base(shader)
    {
        model = Matrix4x4.Identity;
        rasterizerNDX = (int)RenderMode.Solid; // Use SetRenderMode to change
        mvp1 = new D3D11_MVP();

        // Texture sampler state para ser usado no DeviceContext.PSSetSampler(..)
        textureSampler = DEV11.CreateSamplerState(samplerDesc: SamplerDescription.PointWrap);
        textureSampler.DebugName = "SamplerState (unico)";
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }

    public void Texture99((int width, int height, byte[] byteArray) input1)
    {
        var t1 = DEV11.CreateTexture2D(format: rtvFormat, width: input1.width, height: input1.height, initialData: input1.byteArray);
        textureSRV1 = DEV11.CreateShaderResourceView(resource: t1);
        t1.Dispose();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }

    public void SetVertexData<T1, T2, T3>((PrimitiveTopology tipo, T1[] vertexCoords, ushort[] index, T2[] uvCoords) data, T3[] instances)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        qtd_indices = data.index.Length;
        qtd_instances = instances.Length;
        tipo = data.tipo;

        indexBuffer = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);

        vertexBuffer = new ID3D11Buffer[]
        {
            DEV11.CreateBuffer(data.vertexCoords, BindFlags.VertexBuffer),
            DEV11.CreateBuffer(data.uvCoords, BindFlags.VertexBuffer),
            DEV11.CreateBuffer(instances, BindFlags.VertexBuffer)
        };
        strides = new int[] { Unsafe.SizeOf<T1>(), Unsafe.SizeOf<T2>(), Unsafe.SizeOf<T3>() };
        offsets = new int[] { 0, 0, 0 };

        // Como os dados (vertex coords + UV coords) estao interleaved é necessario colocar o format hard coded.
        var vertex1Format = InputDesc.GetFormatFromType<T1>();
        var vertex2Format = InputDesc.GetFormatFromType<T2>();
        var instanceFormat = InputDesc.GetFormatFromType<T3>();
        var inputElements = D3D11_InputDesc.GetInputA1B2I3(slot1Format1: vertex1Format, slot2Format2: vertex2Format, slot3InstanceFormat1: instanceFormat);
        inputLayout = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);

        //DebugLayer1("DrawSetBaseInstanced2 SetVertexData");
    }
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);

        // MVP deve estar no slot 0 do VS: [HLSL CODE] cbuffer params : register(b0) {float4x4 worldViewProjection;}
        DC11.VSSetConstantBuffer(Slots.MVP_registerB, mvp1.UploadMVP(model * Camera1.ViewProjection));

        //DC11_4.WriteTexture(resource: _texture, arraySlice: 0, mipLevel: 0, data: pixels);
        DC11.PSSetShaderResource(slot: Slots.Texture_registerT, textureSRV1); // ou textureSRV2[n2]
        DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler);

        // IA stage
        DC11.IASetInputLayout(inputLayout);
        DC11.IASetPrimitiveTopology(topology: tipo);
        DC11.IASetVertexBuffers(firstSlot: MyShaderSources.vertexSlot, vertexBuffer, strides, offsets); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.
        DC11.IASetIndexBuffer(indexBuffer, Format.R16_UInt, offset: 0); // R16_Uint = ushort index, capable of 65K indices. If necessary more, use R32_Uint = uint index =  4 billion indeces.

        // Rasterizer stage
        DC11.RSSetState(rasterizers[rasterizerNDX]);

        // Draw ( DrawIndexedInstanced to be equal D3D12 )
        DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices, instanceCount: qtd_instances, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);

    }
    public void Dispose()
    {
        vertexBuffer[0].Dispose();
        vertexBuffer[1].Dispose();
        vertexBuffer[2].Dispose();
        indexBuffer.Dispose();
        inputLayout.Dispose();
        textureSRV1.Dispose();
        textureSampler.Dispose();
        mvp1.Dispose_MVP();
        Dispose_Shader();
    }
}

// L5 SandBox indroduction: MULTIDATA
// (A) No instanced: 1 vertex buffer mixed: float3 vertex coords + float2 UV coords
// (B) No instanced: 2 vertex buffers: one for vertex coods <T1>, other for UV coords <T2>
// (C) Instances: 1 vertex buffer mixed: float3 vertex coords + float2 UV coords
// (D) Instanced: 2 vertex buffers: one for vertex coods <T1>, other for UV coords <T2>
/// <summary>multidata + texture + indexing + mvp, 1 mixed coords dataset</summary>
public class DrawSetBase1 : D3D11_ShaderCompiler
{
    public int qtd; // precisa por causa do Draw()
    public int[] qtd_indices; // precisa ser public porque é usado para calcular o numero total de vertices no Models.
    public Matrix4x4[] model; // precisa ser public porque esse input é feito no Models.
    public PrimitiveTopology[] tipo;
    public int[] rasterizerNDX;
    public ID3D11InputLayout[] inputLayout;
    public ID3D11Buffer[] vertexBuffer;
    public int[] stride;
    public ID3D11Buffer[] indexBuffer;
    public D3D11_MVP v1;
    public ID3D11ShaderResourceView[] textureSRV1;
    public ID3D11SamplerState[] textureSampler;
    //public ID3D11Texture2D texture; // class field em caso de ser necessario usar no Device.WriteTexture(..)

    public DrawSetBase1(int qtd, string sh) : base(sh)
    {
        this.qtd = qtd;
        rasterizerNDX = Enumerable.Repeat((int)RenderMode.Solid, qtd).ToArray(); // Use SetRenderMode to change
        model = Enumerable.Repeat(Matrix4x4.Identity, qtd).ToArray();
        vertexBuffer = new ID3D11Buffer[qtd];
        stride = new int[qtd];
        indexBuffer = new ID3D11Buffer[qtd];
        tipo = new PrimitiveTopology[qtd];
        qtd_indices = new int[qtd];
        inputLayout = new ID3D11InputLayout[qtd];
        textureSRV1 = new ID3D11ShaderResourceView[qtd];
        textureSampler = new ID3D11SamplerState[qtd];
        v1 = new D3D11_MVP();
        // Texture sampler state para ser usado no DeviceContext.PSSetSampler(..)
        var y = DEV11.CreateSamplerState(samplerDesc: SamplerDescription.PointWrap);
        textureSampler = Enumerable.Repeat(y, qtd).ToArray();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }

    public void Texture99(int index, (int width, int height, byte[] byteArray) input1)
    {
        var t1 = DEV11.CreateTexture2D(format: rtvFormat, width: input1.width, height: input1.height, initialData: input1.byteArray);
        textureSRV1[index] = DEV11.CreateShaderResourceView(resource: t1);
        t1.Dispose();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }


    // Este SetVertexData espera que vertex tenha interleaved data (float3 vertex coords + float2 UV coords)
    // Dessa forma, os Format para o inputElements ja estão hardcoded aqui.
    public void SetVertexData<T>(int index, (PrimitiveTopology tipo, T[] vertex, ushort[] index) data) where T : unmanaged
    {
        qtd_indices[index] = data.index.Length;
        tipo[index] = data.tipo;

        indexBuffer[index] = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);

        vertexBuffer[index] = DEV11.CreateBuffer(data.vertex, BindFlags.VertexBuffer);
        stride[index] = Unsafe.SizeOf<T>();
        // Como os dados (vertex coords + UV coords) estao interleaved é necessario colocar o format hard coded.
        var vertex1Format = Format.R32G32B32_Float;
        var vertex2Format = Format.R32G32_Float;
        var inputElements = D3D11_InputDesc.GetInputA1B1(vertex1Format, vertex2Format);
        inputLayout[index] = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);
    }


    public void SetRenderMode(int index, RenderMode r) => rasterizerNDX[index] = (int)r;
    public void SetTranslation(int index, Vector3 v) => model[index].Translation = v;
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);

        for (var n1 = 0; n1 < qtd; n1++)
        {
            // MVP deve estar no slot 0 do VS: [HLSL CODE] cbuffer params : register(b0) {float4x4 worldViewProjection;}
            DC11.VSSetConstantBuffer(Slots.MVP_registerB, v1.UploadMVP(model[n1] * Camera1.ViewProjection));

            //DC11_4.WriteTexture(resource: _texture, arraySlice: 0, mipLevel: 0, data: pixels);
            DC11.PSSetShaderResource(slot: Slots.Texture_registerT, textureSRV1[n1]); // ou textureSRV2
            DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler[n1]);

            // IA stage
            DC11.IASetInputLayout(inputLayout[n1]);
            DC11.IASetPrimitiveTopology(topology: tipo[n1]);
            DC11.IASetVertexBuffer(slot: MyShaderSources.vertexSlot, vertexBuffer[n1], stride[n1], offset: 0); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.
            DC11.IASetIndexBuffer(indexBuffer[n1], Format.R16_UInt, offset: 0); // R16_Uint = ushort index, capable of 65K indices. If necessary more, use R32_Uint = uint index =  4 billion indeces.

            // Rasterizer stage
            DC11.RSSetState(rasterizers[rasterizerNDX[n1]]);

            // Draw ( DrawIndexedInstanced to be equal D3D12 )
            DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices[n1], instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
        }
    }
    public void Dispose()
    {
        for (var n1 = 0; n1 < qtd; n1++)
        {
            vertexBuffer[n1].Dispose();
            indexBuffer[n1].Dispose();
            inputLayout[n1].Dispose();
            textureSRV1[n1].Dispose();
            textureSampler[n1].Dispose();
        }
        v1.Dispose_MVP();
        Dispose_Shader();
    }
}
/// <summary>multidata + texture + indexing + mvp, 2 coords dataset</summary>
public class DrawSetBase2 : D3D11_ShaderCompiler
{
    public int qtd; // precisa por causa do Draw()
    public int[] qtd_indices; // precisa ser public porque é usado para calcular o numero total de vertices no Models.
    public Matrix4x4[] model; // precisa ser public porque esse input é feito no Models.
    public PrimitiveTopology[] tipo;
    public int[] rasterizerNDX;
    public ID3D11InputLayout[] inputLayout;
    public ID3D11Buffer[][] vertexBuffer;
    public int[][] strides;
    public int[][] offsets;
    public ID3D11Buffer[] indexBuffer;
    public D3D11_MVP v1;
    public ID3D11ShaderResourceView[] textureSRV1;
    public ID3D11ShaderResourceView[] textureSRV2;
    public ID3D11SamplerState[] textureSampler;
    //public ID3D11Texture2D texture; // class field em caso de ser necessario usar no Device.WriteTexture(..)

    public DrawSetBase2(int qtd, string sh) : base(sh)
    {
        this.qtd = qtd;
        rasterizerNDX = Enumerable.Repeat((int)RenderMode.Solid, qtd).ToArray(); // Use SetRenderMode to change
        model = Enumerable.Repeat(Matrix4x4.Identity, qtd).ToArray();
        vertexBuffer = new ID3D11Buffer[qtd][];
        strides = new int[qtd][];
        offsets = new int[qtd][];
        indexBuffer = new ID3D11Buffer[qtd];
        tipo = new PrimitiveTopology[qtd];
        qtd_indices = new int[qtd];
        inputLayout = new ID3D11InputLayout[qtd];
        textureSRV1 = new ID3D11ShaderResourceView[qtd];
        textureSampler = new ID3D11SamplerState[qtd];
        v1 = new D3D11_MVP();
        // Texture sampler state para ser usado no DeviceContext.PSSetSampler(..)
        var y = DEV11.CreateSamplerState(samplerDesc: SamplerDescription.PointWrap);
        textureSampler = Enumerable.Repeat(y, qtd).ToArray();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }
    public void Texture99(int index, (int width, int height, byte[] byteArray) input1)
    {
        var t1 = DEV11.CreateTexture2D(format: rtvFormat, width: input1.width, height: input1.height, initialData: input1.byteArray);
        textureSRV1[index] = DEV11.CreateShaderResourceView(resource: t1);
        t1.Dispose();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }
    public void SetVertexData<T1,T2>(int index, (PrimitiveTopology tipo, T1[] vertexCoords, ushort[] index, T2[] textureCoords) data) where T1 : unmanaged where T2 : unmanaged
    {
        if (data.vertexCoords.Length != data.textureCoords.Length) throw new Exception();
        qtd_indices[index] = data.index.Length;
        tipo[index] = data.tipo;

        indexBuffer[index] = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);

        vertexBuffer[index] = new ID3D11Buffer[]
        {
            DEV11.CreateBuffer(data.vertexCoords, BindFlags.VertexBuffer), // only vertex coordinates in this vertex buffer
            DEV11.CreateBuffer(data.textureCoords, BindFlags.VertexBuffer) // only vertex uv texture coordinates in this vertex buffer
        };
        strides[index] = new int[] { Unsafe.SizeOf<T1>(), Unsafe.SizeOf<T2>() };
        offsets[index] = new int[] { 0, 0 };
        var vertex1Format = InputDesc.GetFormatFromType<T1>();
        var vertex2Format = InputDesc.GetFormatFromType<T2>();
        var inputElements = D3D11_InputDesc.GetInputA1B2(vertex1Format, vertex2Format);
        inputLayout[index] = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);
    }
    public void SetRenderMode(int index, RenderMode r) => rasterizerNDX[index] = (int)r;
    public void SetTranslation(int index, Vector3 v) => model[index].Translation = v;
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);

        for (var n1 = 0; n1 < qtd; n1++)
        {
            // MVP deve estar no slot 0 do VS: [HLSL CODE] cbuffer params : register(b0) {float4x4 worldViewProjection;}
            DC11.VSSetConstantBuffer(Slots.MVP_registerB, v1.UploadMVP(model[n1] * Camera1.ViewProjection));

            //DC11_4.WriteTexture(resource: _texture, arraySlice: 0, mipLevel: 0, data: pixels);
            DC11.PSSetShaderResource(slot: Slots.Texture_registerT, textureSRV1[n1]); // ou textureSRV2[n2]
            DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler[n1]);

            // IA stage
            DC11.IASetInputLayout(inputLayout[n1]);
            DC11.IASetPrimitiveTopology(topology: tipo[n1]);
            DC11.IASetVertexBuffers(firstSlot: MyShaderSources.vertexSlot, vertexBuffer[n1], strides[n1], offsets: offsets[n1]); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.
            DC11.IASetIndexBuffer(indexBuffer[n1], Format.R16_UInt, offset: 0); // R16_Uint = ushort index, capable of 65K indices. If necessary more, use R32_Uint = uint index =  4 billion indeces.

            // Rasterizer stage
            DC11.RSSetState(rasterizers[rasterizerNDX[n1]]);

            // Draw ( DrawIndexedInstanced to be equal D3D12 )
            DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices[n1], instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
        }
    }
    public void Dispose()
    {
        for (var n1 = 0; n1 < qtd; n1++)
        {
            vertexBuffer[n1][0].Dispose();
            vertexBuffer[n1][1].Dispose();
            indexBuffer[n1].Dispose();
            inputLayout[n1].Dispose();
            textureSRV1[n1].Dispose();
            textureSampler[n1].Dispose();
        }
        v1.Dispose_MVP();
        Dispose_Shader();
    }
}
///<summary>multidata + instanced + texture + indexed + mvp, 1 mixed coords dataset</summary>
public class DrawSetBaseInstanced1 : D3D11_ShaderCompiler
{
    public int qtd; // precisa por causa do Draw()

    public D3D11_MVP mvp1;
    public ID3D11SamplerState textureSampler;

    public int[] qtd_indices; // precisa ser public porque é usado para calcular o numero total de vertices no Models.
    public int[] qtd_instances;
    public Matrix4x4[] model; // precisa ser public porque esse input é feito no Models.
    public PrimitiveTopology[] tipo;
    public int[] rasterizerNDX;
    public ID3D11InputLayout[] inputLayout;
    public ID3D11Buffer[][] vertexBuffer;
    public int[][] strides;
    public int[][] offsets;
    public ID3D11Buffer[] indexBuffer;
    public ID3D11ShaderResourceView[] textureSRV1;

    //public ID3D11Texture2D texture; // class field em caso de ser necessario usar no Device.WriteTexture(..)

    public DrawSetBaseInstanced1(int qtd, string sh) : base(sh)
    {
        this.qtd = qtd;

        model = Enumerable.Repeat(Matrix4x4.Identity, qtd).ToArray();
        rasterizerNDX = Enumerable.Repeat((int)RenderMode.Solid, qtd).ToArray(); // Use SetRenderMode to change
        inputLayout = new ID3D11InputLayout[qtd];
        textureSRV1 = new ID3D11ShaderResourceView[qtd];
        //textureSampler = new ID3D11SamplerState[qtd];
        vertexBuffer = new ID3D11Buffer[qtd][];
        strides = new int[qtd][];
        offsets = new int[qtd][];
        indexBuffer = new ID3D11Buffer[qtd];
        tipo = new PrimitiveTopology[qtd];
        qtd_indices = new int[qtd];
        qtd_instances = new int[qtd];

        mvp1 = new D3D11_MVP();

        // Texture sampler state para ser usado no DeviceContext.PSSetSampler(..)
        textureSampler = DEV11.CreateSamplerState(samplerDesc: SamplerDescription.PointWrap);
        textureSampler.DebugName = "SamplerState (unico)";
        //textureSampler = Enumerable.Repeat(y, qtd).ToArray();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);

        //DebugLayer1("DrawSetBaseInstanced1");
        /*
         *  1 ID3D11RasterizerState
         *  1 ID3D11Buffer
         *  1 ID3D11Sampler
         */
    }

    public void Texture99(int index, (int width, int height, byte[] byteArray) input1)
    {
        var t1 = DEV11.CreateTexture2D(format: rtvFormat, width: input1.width, height: input1.height, initialData: input1.byteArray);
        textureSRV1[index] = DEV11.CreateShaderResourceView(resource: t1);
        t1.Dispose();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }

    // Este SetVertexData espera que vertex tenha interleaved data (float3 vertex coords + float2 UV coords)
    // Dessa forma, os Format para o inputElements ja estão hardcoded aqui.
    public void SetVertexData<T1,T2>(int index, (PrimitiveTopology tipo, T1[] vertex, ushort[] index) data, T2[] instances) where T1 : unmanaged where T2 : unmanaged
    {
        qtd_indices[index] = data.index.Length;
        qtd_instances[index] = instances.Length;
        tipo[index] = data.tipo;

        indexBuffer[index] = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);
        indexBuffer[index].DebugName = $"index buffer NUM-{index}";

        vertexBuffer[index] = new ID3D11Buffer[]
        { 
            DEV11.CreateBuffer(data.vertex, BindFlags.VertexBuffer),
            DEV11.CreateBuffer(instances, BindFlags.VertexBuffer)
        };
        vertexBuffer[index][0].DebugName = $"vertex buffer NUM-{index}";
        vertexBuffer[index][1].DebugName = $"instance buffer NUM-{index}";


        strides[index] = new int[] { Unsafe.SizeOf<T1>(), Unsafe.SizeOf<T2>() };
        offsets[index] = new int[] { 0, 0 };

        // Como os dados (vertex coords + UV coords) estao interleaved é necessario colocar o format hard coded.
        var vertex1Format = Format.R32G32B32_Float; // float3
        var vertex2Format = Format.R32G32_Float; // float2
        var instanceFormat = InputDesc.GetFormatFromType<T2>();
        var inputElements = D3D11_InputDesc.GetInputA1B1I2(slot1Format1: vertex1Format, slot1Format2: vertex2Format, slot2InstanceFormat1: instanceFormat);
        inputLayout[index] = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);
        inputLayout[index].DebugName = $"input layout NUM-{index}";

        //DebugLayer1("DrawSetBaseInstanced1 SetVertexData");
    }

    public void SetRenderMode(int index, RenderMode r) => rasterizerNDX[index] = (int)r;

    public void SetTranslation(int index, Vector3 v) => model[index].Translation = v;
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);
        DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler);

        for (var n1 = 0; n1 < qtd; n1++)
        {
            // MVP deve estar no slot 0 do VS: [HLSL CODE] cbuffer params : register(b0) {float4x4 worldViewProjection;}
            DC11.VSSetConstantBuffer(Slots.MVP_registerB, mvp1.UploadMVP(model[n1] * Camera1.ViewProjection));

            //DC11_4.WriteTexture(resource: _texture, arraySlice: 0, mipLevel: 0, data: pixels);
            DC11.PSSetShaderResource(slot: Slots.Texture_registerT, textureSRV1[n1]); // ou textureSRV2[n2]
            //DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler[n1]);

            // IA stage
            DC11.IASetInputLayout(inputLayout[n1]);
            DC11.IASetPrimitiveTopology(topology: tipo[n1]);
            DC11.IASetVertexBuffers(firstSlot: MyShaderSources.vertexSlot, vertexBuffer[n1], strides[n1], offsets[n1]); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.
            DC11.IASetIndexBuffer(indexBuffer[n1], Format.R16_UInt, offset: 0); // R16_Uint = ushort index, capable of 65K indices. If necessary more, use R32_Uint = uint index =  4 billion indeces.

            // Rasterizer stage
            //rasterizer.Description = CullMode.None;
            DC11.RSSetState(rasterizers[rasterizerNDX[n1]]);

            // Draw ( DrawIndexedInstanced to be equal D3D12 )
            DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices[n1], instanceCount: qtd_instances[n1], startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
        }
    }
    public void Dispose()
    {
        for (var n1 = 0; n1 < qtd; n1++)
        {
            vertexBuffer[n1][0].Dispose();
            vertexBuffer[n1][1].Dispose();
            indexBuffer[n1].Dispose();
            inputLayout[n1].Dispose();
            textureSRV1[n1].Dispose();
            //textureSampler[n1].Dispose();
        }
        textureSampler.Dispose();
        mvp1.Dispose_MVP();
        Dispose_Shader();
    }
}
///<summary>multidata + instanced + texture + indexed + mvp, 2 coords dataset</summary>
public class DrawSetBaseInstanced2 : D3D11_ShaderCompiler
{
    public int qtd; // precisa por causa do Draw()
    public int[] qtd_indices; // precisa ser public porque é usado para calcular o numero total de vertices no Models.
    public int[] qtd_instances;
    public Matrix4x4[] model; // precisa ser public porque esse input é feito no Models.
    public PrimitiveTopology[] tipo;
    public int[] rasterizerNDX;
    public ID3D11InputLayout[] inputLayout;
    public ID3D11Buffer[][] vertexBuffer;
    public int[][] strides;
    public int[][] offsets;
    public ID3D11Buffer[] indexBuffer;
    public D3D11_MVP v1;
    public ID3D11ShaderResourceView[] textureSRV1;
    public ID3D11SamplerState[] textureSampler;
    //public ID3D11Texture2D texture; // class field em caso de ser necessario usar no Device.WriteTexture(..)

    public DrawSetBaseInstanced2(int qtd, string sh) : base(sh)
    {
        this.qtd = qtd;
        rasterizerNDX = Enumerable.Repeat((int)RenderMode.Solid, qtd).ToArray(); // Use SetRenderMode to change
        model = Enumerable.Repeat(Matrix4x4.Identity, qtd).ToArray();

        inputLayout = new ID3D11InputLayout[qtd];
        textureSRV1 = new ID3D11ShaderResourceView[qtd];
        textureSampler = new ID3D11SamplerState[qtd];

        vertexBuffer = new ID3D11Buffer[qtd][];

        strides = new int[qtd][];
        offsets = new int[qtd][];
        indexBuffer = new ID3D11Buffer[qtd];

        tipo = new PrimitiveTopology[qtd];
        qtd_indices = new int[qtd];
        qtd_instances = new int[qtd];
        inputLayout = new ID3D11InputLayout[qtd];
        v1 = new D3D11_MVP();
        // Texture sampler state para ser usado no DeviceContext.PSSetSampler(..)
        var y = DEV11.CreateSamplerState(samplerDesc: SamplerDescription.PointWrap);
        textureSampler = Enumerable.Repeat(y, qtd).ToArray();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);

        //DebugLayer1("DrawSetBaseInstanced2");
        /*
         * 
         */
    }

    public void Texture99(int index, (int width, int height, byte[] byteArray) input1)
    {
        var t1 = DEV11.CreateTexture2D(format: rtvFormat, width: input1.width, height: input1.height, initialData: input1.byteArray);
        textureSRV1[index] = DEV11.CreateShaderResourceView(resource: t1);
        t1.Dispose();
        // Se não vai mais mudar a textura, então não é necessario manter
        // o resource ID3D11Texture2D usado no CreateShaderResourceView.
        // Se a textura vai mudar, guarde o resource e use Device.WriteTexture(..)  tipo:
        // DC11_4.WriteTexture(resource: texture2D, arraySlice: 0, mipLevel: 0, data: pixels);
    }

    // Este SetVertexData espera que vertex tenha interleaved data (float3 vertex coords + float2 UV coords)
    // Dessa forma, os Format para o inputElements ja estão hardcoded aqui.
    public void SetVertexData<T1, T2, T3>(int index, (PrimitiveTopology tipo, T1[] vertexCoords, ushort[] index, T2[] uvCoords) data, T3[] instances)
        where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        qtd_indices[index] = data.index.Length;
        qtd_instances[index] = instances.Length;
        tipo[index] = data.tipo;

        indexBuffer[index] = DEV11.CreateBuffer(data.index, BindFlags.IndexBuffer);

        vertexBuffer[index] = new ID3D11Buffer[]
        {
            DEV11.CreateBuffer(data.vertexCoords, BindFlags.VertexBuffer),
            DEV11.CreateBuffer(data.uvCoords, BindFlags.VertexBuffer),
            DEV11.CreateBuffer(instances, BindFlags.VertexBuffer)
        };
        strides[index] = new int[] { Unsafe.SizeOf<T1>(), Unsafe.SizeOf<T2>(), Unsafe.SizeOf<T3>() };
        offsets[index] = new int[] { 0, 0, 0 };

        // Como os dados (vertex coords + UV coords) estao interleaved é necessario colocar o format hard coded.
        var vertex1Format = InputDesc.GetFormatFromType<T1>();
        var vertex2Format = InputDesc.GetFormatFromType<T2>();
        var instanceFormat = InputDesc.GetFormatFromType<T3>();
        var inputElements = D3D11_InputDesc.GetInputA1B2I3(slot1Format1: vertex1Format, slot2Format2: vertex2Format, slot3InstanceFormat1: instanceFormat);
        inputLayout[index] = DEV11.CreateInputLayout(inputElements, VS11_ByteCode);

        //DebugLayer1("DrawSetBaseInstanced2 SetVertexData");
    }

    public void SetRenderMode(int index, RenderMode r) => rasterizerNDX[index] = (int)r;

    public void SetTranslation(int index, Vector3 v) => model[index].Translation = v;
    public void Draw() // executed at EACH frame, for EACH DrawSet
    {
        DC11.VSSetShader(VS11);
        DC11.PSSetShader(PS11);

        for (var n1 = 0; n1 < qtd; n1++)
        {
            // MVP deve estar no slot 0 do VS: [HLSL CODE] cbuffer params : register(b0) {float4x4 worldViewProjection;}
            DC11.VSSetConstantBuffer(Slots.MVP_registerB, v1.UploadMVP(model[n1] * Camera1.ViewProjection));

            //DC11_4.WriteTexture(resource: _texture, arraySlice: 0, mipLevel: 0, data: pixels);
            DC11.PSSetShaderResource(slot: Slots.Texture_registerT, textureSRV1[n1]); // ou textureSRV2[n2]
            DC11.PSSetSampler(slot: Slots.Sampler_registerS, textureSampler[n1]);

            // IA stage
            DC11.IASetInputLayout(inputLayout[n1]);
            DC11.IASetPrimitiveTopology(topology: tipo[n1]);
            DC11.IASetVertexBuffers(firstSlot: MyShaderSources.vertexSlot, vertexBuffer[n1], strides[n1], offsets[n1]); // slot used here MUST BE the same as defined in "InputElementDescription" for this element.
            DC11.IASetIndexBuffer(indexBuffer[n1], Format.R16_UInt, offset: 0); // R16_Uint = ushort index, capable of 65K indices. If necessary more, use R32_Uint = uint index =  4 billion indeces.

            // Rasterizer stage
            DC11.RSSetState(rasterizers[rasterizerNDX[n1]]);

            // Draw ( DrawIndexedInstanced to be equal D3D12 )
            DC11.DrawIndexedInstanced(indexCountPerInstance: qtd_indices[n1], instanceCount: qtd_instances[n1], startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
        }
    }
    public void Dispose()
    {
        for (var n1 = 0; n1 < qtd; n1++)
        {
            vertexBuffer[n1][0].Dispose();
            vertexBuffer[n1][1].Dispose();
            vertexBuffer[n1][2].Dispose();
            indexBuffer[n1].Dispose();
            inputLayout[n1].Dispose();
            textureSRV1[n1].Dispose();
            textureSampler[n1].Dispose();
        }
        v1.Dispose_MVP();
        Dispose_Shader();
    }
}

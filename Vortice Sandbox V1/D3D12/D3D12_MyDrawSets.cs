using System.Numerics;
using System.Runtime.CompilerServices;

using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI; // Necessary for DXGI.Format on IASetIndexBuffer

using D3D_Mama;
using D3D12_Mama;
using static D3D12_Mama.D3D12_Base;

#nullable disable

namespace D3D12_MyDrawSets;

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
public class DrawSetBase_NoMulti_NoInst_NoTex_NoMVP_NoIndex : D3D12_ShaderCompiler
{
    public int qtd_vertices; // Draw part
    public ID3D12Resource vertexCoords; // keep resource to proper disposal
    public VertexBufferView VBV; // Draw part
    //
    public ID3D12RootSignature rootSignature; // keep resource to proper disposal
    public PrimitiveTopology tipo; // Draw part
    public ID3D12PipelineState PSO; // Draw part
    public GraphicsPipelineStateDescription PSOdesc;

    public DrawSetBase_NoMulti_NoInst_NoTex_NoMVP_NoIndex(string sh) : base(sh)
    {
        rootSignature = D3D12_Signatures.GetRootSignature(0); // no parameters
        PSOdesc = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            RenderTargetFormats = new[] { rtvFormat },
            DepthStencilFormat = dsvFormat,
            SampleDescription = SampleDescription.Default,
        };
    }
    public void SetVertexData<T>((PrimitiveTopology tipo, T[] vertexCoords) data)
    {
        var vertex1Format = InputDesc.GetFormatFromType<T>();
        var inputElements = D3D12_InputDesc.GetInputA1(vertex1Format);
        PSOdesc.InputLayout = inputElements; // complete pipeline description

        // IMPORTANT: PrimitiveTopologyType @ PipelineState  versus IASetPrimitiveTopology @ GraphicsCommandList 
        //
        // The IA primitive topology type (point, line, triangle, patch)
        // is set within the PSO using the D3D12_PRIMITIVE_TOPOLOGY_TYPE enumeration. 
        // The primitive adjacency and ordering (line list, line strip, line strip with adjacency data, etc.) 
        // is set from within a command list using the ID3D12GraphicsCommandList::IASetPrimitiveTopology method.
        PSOdesc.PrimitiveTopologyType = (PrimitiveTopologyType)data.tipo;
        PSO = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc);
        PSO.Name = "Pipeline";

        qtd_vertices = data.vertexCoords.Length;
        tipo = data.tipo;
        vertexCoords = CreateBufferResourceAndUploadData(data.vertexCoords, nameID: 0);
        vertexCoords.Name = "MAMA vertex buffer";

        VBV = new VertexBufferView(
            bufferLocation: vertexCoords.GPUVirtualAddress,
            sizeInBytes: Unsafe.SizeOf<T>() * qtd_vertices,
            strideInBytes: Unsafe.SizeOf<T>());
    }
    public void Draw()
    {
        // Pipeline state
        CL12_4.SetGraphicsRootSignature(rootSignature);
        CL12_4.SetPipelineState(PSO);

        // Input Assembler stage
        CL12_4.IASetPrimitiveTopology(primitiveTopology: tipo);
        CL12_4.IASetVertexBuffers(slot: MyShaderSources.vertexSlot, vertexBufferView: VBV);

        // Draw
        CL12_4.DrawInstanced(vertexCountPerInstance: qtd_vertices, instanceCount: 1, startVertexLocation: 0, startInstanceLocation: 0);
    }
    public void Dispose()
    {
        vertexCoords.Dispose();
        rootSignature.Dispose();
        PSO.Dispose();
    }
}

// L1 SandBox introduction: INDEXED
// indexed vertices
/// <summary>indexed</summary>
public class DrawSetBase_NoMulti_NoInst_NoTex_NoMVP : D3D12_ShaderCompiler
{
    public int qtd_vertices; // Draw part
    public ID3D12Resource vertexCoords; // // keep resource to proper disposal
    public VertexBufferView VBV; // Draw part
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public ID3D12PipelineState PSO; // Draw part
    public PrimitiveTopology tipo; // Draw part
    public GraphicsPipelineStateDescription PSOdesc;

    public DrawSetBase_NoMulti_NoInst_NoTex_NoMVP(string sh) : base(sh)
    {
        rootSignature = D3D12_Signatures.GetRootSignature(0); // no parameters
        PSOdesc = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            RenderTargetFormats = new[] { rtvFormat },
            DepthStencilFormat = dsvFormat,
            SampleDescription = SampleDescription.Default,
        };
    }
    public void SetVertexData<T>((PrimitiveTopology tipo, T[] vertex, ushort[] index) data)
    {
        var vertex1Format = InputDesc.GetFormatFromType<T>();
        var inputElements = D3D12_InputDesc.GetInputA1(vertex1Format);
        PSOdesc.InputLayout = inputElements; // complete pipeline description
        PSOdesc.PrimitiveTopologyType = (PrimitiveTopologyType)data.tipo;
        PSO = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc);
        PSO.Name = "Pipeline";

        qtd_vertices = data.vertex.Length;
        tipo = data.tipo;

        vertexCoords = CreateBufferResourceAndUploadData(data.vertex, nameID: 0);
        vertexCoords.Name = "MAMA vertex buffer";

        VBV = new VertexBufferView(
            bufferLocation: vertexCoords.GPUVirtualAddress,
            sizeInBytes: Unsafe.SizeOf<T>() * qtd_vertices,
            strideInBytes: Unsafe.SizeOf<T>());
    }
    public void Draw()
    {
        // Pipeline state
        CL12_4.SetGraphicsRootSignature(rootSignature);
        CL12_4.SetPipelineState(PSO);

        // Input Assembler stage
        CL12_4.IASetPrimitiveTopology(tipo);
        CL12_4.IASetVertexBuffers(slot: MyShaderSources.vertexSlot, VBV);

        // Draw
        CL12_4.DrawInstanced(vertexCountPerInstance: qtd_vertices, instanceCount: 1, startVertexLocation: 0, startInstanceLocation: 0);
    }
    public void Dispose()
    {
        vertexCoords.Dispose();
        rootSignature.Dispose();
        PSO.Dispose();
    }
}

// L2 SandBox introduction: MVP
// Model * View * Projection transformation
/// <summary>mvp + indexed</summary>
public class DrawSetBase_NoMulti_NoInst_NoTex : D3D12_ShaderCompiler
{
    public Matrix4x4 model; // Draw part
    //
    public ID3D12Resource vertexCoords; // keep resource to proper disposal
    public VertexBufferView VBV; // Draw part
    //
    public int qtd_indices; // Draw part
    public ID3D12Resource vertexIndices; // keep resource to proper disposal
    public IndexBufferView IBV; // Draw part
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public PrimitiveTopology tipo; // Draw part
    public ID3D12PipelineState PSO; // Draw part
    public GraphicsPipelineStateDescription PSOdesc;

    // Nesse level, a rootSignature(1) tem um unico parametro para o buffer MVP (index 0)
    // Esse parameter esta definido com o tipo RootConstant de 16 floats no register 0.
    // D3D12_Signatures.GetRootSignature(1) faz tudo isso.
    // Depois, no draw, basta usar SetGraphicsRoot32BitConstants(..) para colocar 16 floats no index 0.
    public DrawSetBase_NoMulti_NoInst_NoTex(string sh) : base(sh)
    {
        rootSignature = D3D12_Signatures.GetRootSignature(1); // 1 parameter: constant MVP
        PSOdesc = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            RenderTargetFormats = new[] { rtvFormat },
            DepthStencilFormat = dsvFormat,
            SampleDescription = SampleDescription.Default,
        };
        model = Matrix4x4.Identity;
    }
    public void SetVertexData<T>((PrimitiveTopology tipo, T[] vertex, ushort[] index) data)
    {
        var inputElements = D3D12_InputDesc.GetInputA1(InputDesc.GetFormatFromType<T>());
        PSOdesc.InputLayout = inputElements; // complete pipeline description
        PSOdesc.PrimitiveTopologyType = (PrimitiveTopologyType)data.tipo;
        PSO = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc);
        PSO.Name = "Pipeline";

        tipo = data.tipo;
        var qtd_vertices = data.vertex.Length; // local because it is necessary only here
        qtd_indices = data.index.Length;

        vertexCoords = CreateBufferResourceAndUploadData(data.vertex, nameID: 0);

        VBV = new VertexBufferView(
            bufferLocation: vertexCoords.GPUVirtualAddress,
            sizeInBytes: Unsafe.SizeOf<T>() * qtd_vertices,
            strideInBytes: Unsafe.SizeOf<T>());

        vertexIndices = CreateBufferResourceAndUploadData(data.index, nameID: 0);

        IBV = new IndexBufferView(
            bufferLocation: vertexIndices.GPUVirtualAddress,
            sizeInBytes: qtd_indices * sizeof(ushort),
            is32Bit: false); // 16-bit or 65K indices. Use "true" for a lot more indices.
    }
    public void Draw()
    {
        // Pipeline state
        CL12_4.SetGraphicsRootSignature(rootSignature);
        CL12_4.SetPipelineState(PSO);

        // Input Assembler stage
        CL12_4.IASetPrimitiveTopology(tipo);
        CL12_4.IASetVertexBuffers(MyShaderSources.vertexSlot, VBV);
        CL12_4.IASetIndexBuffer(IBV);

        // MVP constant buffer
        CL12_4.SetGraphicsRoot32BitConstants(rootParameterIndex: 0, model * Camera1.ViewProjection, 0);

        // Draw
        CL12_4.DrawIndexedInstanced(indexCountPerInstance: qtd_indices, instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
    }
    public void Dispose()
    {
        vertexCoords.Dispose();
        vertexIndices.Dispose();
        rootSignature.Dispose();
        PSO.Dispose();
    }
}

// L3 SandBox introduction: TEXTURE
// (A) 1 mixed dataset: float3 vertex coords + float2 UV coords
// (B) 2 datasets: one for vertex coods <T1> and other for UV coords <T2>
/// <summary>texture + indexed + mvp, 1 mixed coords dataset</summary>
public class DrawSetBase_NoMulti_NoInst1 : D3D12_ShaderCompiler
{
    public Matrix4x4 model; // Draw part
    //
    public ID3D12Resource vertexMixedCoords; // (include textureCoords) keep to allow Disposal 
    public VertexBufferView VBV; // Draw part
    //
    public ID3D12Resource TEXTURE; // keep resource to proper disposal
    public ID3D12DescriptorHeap TEXTURE_HEAP; // Draw part
    //
    public int qtd_indices; // Draw part
    public ID3D12Resource vertexIndices; // keep resource to proper disposal
    public IndexBufferView IBV; // Draw part
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public PrimitiveTopology tipo; // Draw part
    public ID3D12PipelineState PSO; // Draw part
    public GraphicsPipelineStateDescription PSOdesc;

    public DrawSetBase_NoMulti_NoInst1(string sh) : base(sh)
    {
        rootSignature = D3D12_Signatures.GetRootSignature(2); // 2 parameters: constant MVP and Table with SRV for texture
        PSOdesc = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,       
            DepthStencilFormat = dsvFormat,
            RenderTargetFormats = new[] { rtvFormat },
            SampleDescription = SampleDescription.Default,
            SampleMask = uint.MaxValue
        };
        model = Matrix4x4.Identity;
    }
    public void Texture99((int width, int height, byte[] byteArray) input1)
    {
        // Description of a texture ( Texture2D ).
        var textureDesc = new ResourceDescription()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (ulong)input1.width,
            Height = input1.height,
            Format = rtvFormat,
            SampleDescription = SampleDescription.Default,
            DepthOrArraySize = 1,
            MipLevels = 1,
            //Layout = TextureLayout.Unknown, // default
            //Alignment = 0, // default
            //Flags = ResourceFlags.None, // default
        };

        TEXTURE = CreateTextureResourceAndUploadData(textureDesc, input1.byteArray);

        // Describe and create a shader resource view (SRV) heap for the texture.
        var descriptorHeapDesc = new DescriptorHeapDescription(
            type: DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            descriptorCount: 1,
            DescriptorHeapFlags.ShaderVisible);

        TEXTURE_HEAP = DEV12_8.CreateDescriptorHeap(description: descriptorHeapDesc);
        TEXTURE_HEAP.Name = "MAMA Heap da Textura 20";

        // SRV (shader resource view) for accessing data in a resource: Describe & create.
        var srvDesc = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = TEXTURE.Description.Format,
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
        };
        srvDesc.Texture2D.MipLevels = 1;
        DEV12_8.CreateShaderResourceView(resource: TEXTURE, desc: srvDesc, destDescriptor: TEXTURE_HEAP.GetCPUDescriptorHandleForHeapStart());
    }
    public void SetVertexData<T>((PrimitiveTopology tipo, T[] vertexMixedCoords, ushort[] index) data)
    {
        // Como os dados (vertex coords + UV coords) estao interleaved é necessario informar esse format aqui (hardcoded).
        var vertex1Format = Format.R32G32B32_Float; // hardcoded: data must be in this format
        var vertex2Format = Format.R32G32_Float; // hardcoded: data must be in this format
        var inputElements = D3D12_InputDesc.GetInputA1B1(vertex1Format, vertex2Format);
        PSOdesc.InputLayout= inputElements;
        PSOdesc.PrimitiveTopologyType = D3D12_AuxFunctions.PrimitiveTopologyD3D12(data.tipo);
        PSO = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc); // var a1 = DEV12_5.DeviceRemovedReason;
        PSO.Name = "Pipeline";                                                              // 
        tipo = data.tipo;
        var qtd_vertices = data.vertexMixedCoords.Length; // local because it is necessary only here
        qtd_indices = data.index.Length;

        vertexMixedCoords = CreateBufferResourceAndUploadData(data.vertexMixedCoords, nameID: 0);

        VBV = new VertexBufferView(
            bufferLocation: vertexMixedCoords.GPUVirtualAddress,
            sizeInBytes: Unsafe.SizeOf<T>() * qtd_vertices,
            strideInBytes: Unsafe.SizeOf<T>());

        vertexIndices = CreateBufferResourceAndUploadData(data.index, nameID: 0);

        IBV = new IndexBufferView(
            bufferLocation: vertexIndices.GPUVirtualAddress,
            sizeInBytes: qtd_indices * sizeof(ushort),
            is32Bit: false); // 16-bit or 65K indices. Use "true" for a lot more indices.
    }
    public void Draw()
    {
        // Pipeline state
        CL12_4.SetGraphicsRootSignature(rootSignature);
        CL12_4.SetPipelineState(PSO);

        CL12_4.SetDescriptorHeaps(heap: TEXTURE_HEAP);
        CL12_4.SetGraphicsRootDescriptorTable(rootParameterIndex: 1, TEXTURE_HEAP.GetGPUDescriptorHandleForHeapStart());

        // IA stage (Input-Assembler)
        CL12_4.IASetPrimitiveTopology(primitiveTopology: tipo);
        CL12_4.IASetVertexBuffers(slot: MyShaderSources.vertexSlot, VBV);
        CL12_4.IASetIndexBuffer(view: IBV);

        // MVP constant buffer
        CL12_4.SetGraphicsRoot32BitConstants(rootParameterIndex: 0, srcData: model * Camera1.ViewProjection, destOffsetIn32BitValues: 0);

        // Draw
        CL12_4.DrawIndexedInstanced(indexCountPerInstance: qtd_indices, instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
    }
    public void Dispose()
    {
        vertexMixedCoords.Dispose();
        vertexIndices.Dispose();
        TEXTURE.Dispose();
        TEXTURE_HEAP.Dispose();
        rootSignature.Dispose();
        PSO.Dispose();
    }
}
/// <summary>texture + indexed + mvp, 2 coords dataset</summary>
public class DrawSetBase_NoMulti_NoInst2 : D3D12_ShaderCompiler
{
    public Matrix4x4 model; // Draw part
    //
    public ID3D12Resource vertexCoords; // keep resource to proper disposal
    public VertexBufferView[] VBV; // Draw part
    //
    public ID3D12Resource textureCoords; // keep resource to proper disposal
    public ID3D12Resource TEXTURE; // keep resource to proper disposal
    public ID3D12DescriptorHeap TEXTURE_HEAP; // Draw part
    //
    public int qtd_indices; // Draw part
    public ID3D12Resource vertexIndices; // keep resource to proper disposal
    public IndexBufferView IBV; // Draw part
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public PrimitiveTopology tipo; // Draw part
    public ID3D12PipelineState PSO; // Draw part
    public GraphicsPipelineStateDescription PSOdesc;

    public DrawSetBase_NoMulti_NoInst2(string sh) : base(sh)
    {
        model = Matrix4x4.Identity;
        rootSignature = D3D12_Signatures.GetRootSignature(2); // 2 parameters: constant MVP and Table with SRV for texture
        PSOdesc = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = dsvFormat,
            RenderTargetFormats = new[] { rtvFormat },
            SampleDescription = SampleDescription.Default,
            SampleMask = uint.MaxValue
        };
    }
    public void Texture99((int width, int height, byte[] byteArray) input1)
    {
        // Description of a texture ( Texture2D ).
        var textureDesc = new ResourceDescription()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (ulong)input1.width,
            Height = input1.height,
            Format = rtvFormat,
            SampleDescription = SampleDescription.Default,
            DepthOrArraySize = 1,
            MipLevels = 1,
            //Layout = TextureLayout.Unknown, // default
            //Alignment = 0, // default
            //Flags = ResourceFlags.None, // default
        };

        TEXTURE = CreateTextureResourceAndUploadData(textureDesc, input1.byteArray);

        // Describe and create a shader resource view (SRV) heap for the texture.
        var descriptorHeapDesc = new DescriptorHeapDescription(
            type: DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            descriptorCount: 1,
            DescriptorHeapFlags.ShaderVisible);

        TEXTURE_HEAP = DEV12_8.CreateDescriptorHeap(description: descriptorHeapDesc);
        TEXTURE_HEAP.Name = "MAMA Heap da Textura 20";

        // SRV (shader resource view) for accessing data in a resource: Describe & create.
        var srvDesc = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = TEXTURE.Description.Format,
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
        };
        srvDesc.Texture2D.MipLevels = 1;
        DEV12_8.CreateShaderResourceView(resource: TEXTURE, desc: srvDesc, destDescriptor: TEXTURE_HEAP.GetCPUDescriptorHandleForHeapStart());
    }
    public void SetVertexData<T1, T2>((PrimitiveTopology tipo, T1[] vertexCoords, ushort[] index, T2[] textureCoords) data)
    {
        // Como os dados (vertex coords + UV coords) estao interleaved é necessario informar esse format aqui (hardcoded).
        var vertex1Format = InputDesc.GetFormatFromType<T1>();
        var vertex2Format = InputDesc.GetFormatFromType<T2>();
        var inputElements = D3D12_InputDesc.GetInputA1B2(vertex1Format, vertex2Format);
        PSOdesc.InputLayout = inputElements;
        PSOdesc.PrimitiveTopologyType = D3D12_AuxFunctions.PrimitiveTopologyD3D12(data.tipo);
        PSO = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc); // var a1 = DEV12_5.DeviceRemovedReason;
        PSO.Name = "Pipeline";

        tipo = data.tipo;
        var qtd_vertices = data.vertexCoords.Length; // local because it is necessary only here
        if (data.textureCoords.Length != qtd_vertices) throw new Exception();
        qtd_indices = data.index.Length;

        vertexCoords = CreateBufferResourceAndUploadData(data.vertexCoords, nameID: 0);
        vertexCoords.Name = "vertexCoords";
        textureCoords = CreateBufferResourceAndUploadData(data.textureCoords, nameID: 0);
        textureCoords.Name = "textureCoods";
        VBV = new VertexBufferView[]
        {
         new VertexBufferView(
            bufferLocation: vertexCoords.GPUVirtualAddress,
            sizeInBytes: Unsafe.SizeOf<T1>() * qtd_vertices,
            strideInBytes: Unsafe.SizeOf<T1>()),

         new VertexBufferView(
            bufferLocation: textureCoords.GPUVirtualAddress,
            sizeInBytes: Unsafe.SizeOf<T2>() * qtd_vertices,
            strideInBytes: Unsafe.SizeOf<T2>())
        };

        vertexIndices = CreateBufferResourceAndUploadData(data.index, nameID: 0);
        vertexIndices.Name = "vertexIndices";
        IBV = new IndexBufferView(
            bufferLocation: vertexIndices.GPUVirtualAddress,
            sizeInBytes: qtd_indices * sizeof(ushort),
            is32Bit: false); // 16-bit or 65K indices. Use "true" for a lot more indices.
    }
    public void Draw()
    {
        // Pipeline state
        CL12_4.SetGraphicsRootSignature(rootSignature);
        CL12_4.SetPipelineState(PSO);

        CL12_4.SetDescriptorHeaps(heap: TEXTURE_HEAP);
        CL12_4.SetGraphicsRootDescriptorTable(rootParameterIndex: 1, TEXTURE_HEAP.GetGPUDescriptorHandleForHeapStart());

        // IA stage (Input-Assembler)
        CL12_4.IASetPrimitiveTopology(primitiveTopology: tipo);
        CL12_4.IASetVertexBuffers(startSlot: MyShaderSources.vertexSlot, viewsCount: 2, VBV);
        CL12_4.IASetIndexBuffer(view: IBV);

        // MVP constant buffer
        CL12_4.SetGraphicsRoot32BitConstants(rootParameterIndex: 0, srcData: model * Camera1.ViewProjection, destOffsetIn32BitValues: 0);

        // Draw
        CL12_4.DrawIndexedInstanced(indexCountPerInstance: qtd_indices, instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
    }
    public void Dispose()
    {
        vertexCoords.Dispose();
        textureCoords.Dispose();
        vertexIndices.Dispose();
        TEXTURE.Dispose();
        TEXTURE_HEAP.Dispose();
        rootSignature.Dispose();
        PSO.Dispose();
    }
}

// L4 SandBox introduction: INSTANCE
// (A) 1 mixed dataset: float3 vertex coords + float2 UV coords
// (B) 2 dataset: one for vertex coods <T1>, other for UV coords <T2>
/// <summary>instanced + texture + indexed + mvp, 1 mixed coods dataset</summary>
public class DrawSetBase_NoMulti1 : D3D12_ShaderCompiler
{
    public int qtd; // Draw part
    public Matrix4x4 model; // Draw part
    //
    public ID3D12Resource vertexMixedCoords; // (contem textureCoords) keep resource to proper disposal
    public VertexBufferView[] VBV; // Draw part
    //
    public ID3D12Resource TEXTURE; // keep resource to proper disposal
    public ID3D12DescriptorHeap TEXTURE_HEAP; // Draw part
    //
    public int qtd_indices; // Draw part
    public ID3D12Resource vertexIndices; // keep resource to proper disposal
    public IndexBufferView IBV; // Draw part
    //
    public int qtd_instances; // Draw part
    public ID3D12Resource instancesB; // keep resource to proper disposal
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public PrimitiveTopology tipo; // Draw part
    public ID3D12PipelineState PSO; // Draw part
    public GraphicsPipelineStateDescription PSOdesc;

    public DrawSetBase_NoMulti1(string shader) : base(shader)
    {
        model = Matrix4x4.Identity;
        rootSignature = D3D12_Signatures.GetRootSignature(2); // 2 parameters: constant MVP and Table with SRV for texture
        PSOdesc = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = dsvFormat,
            RenderTargetFormats = new[] { rtvFormat },
            SampleDescription = SampleDescription.Default,
            SampleMask = uint.MaxValue
        };

    }
    public void Texture99((int width, int height, byte[] byteArray) input1)
    {
        // Description of a texture ( Texture2D ).
        var textureDesc = new ResourceDescription()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (ulong)input1.width,
            Height = input1.height,
            Format = rtvFormat,
            SampleDescription = SampleDescription.Default,
            DepthOrArraySize = 1,
            MipLevels = 1,
        };

        TEXTURE = CreateTextureResourceAndUploadData(textureDesc, input1.byteArray);

        // Describe and create a shader resource view (SRV) heap for the texture.
        var descriptorHeapDesc = new DescriptorHeapDescription(
            type: DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            descriptorCount: 1,
            DescriptorHeapFlags.ShaderVisible);

        TEXTURE_HEAP = DEV12_8.CreateDescriptorHeap(description: descriptorHeapDesc);
        TEXTURE_HEAP.Name = "MAMA Heap da Textura";

        // SRV (shader resource view) for accessing data in a resource: Describe & create.
        var srvDesc = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = TEXTURE.Description.Format,
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
        };
        srvDesc.Texture2D.MipLevels = 1;
        DEV12_8.CreateShaderResourceView(resource: TEXTURE, desc: srvDesc, destDescriptor: TEXTURE_HEAP.GetCPUDescriptorHandleForHeapStart());
    }
    public void SetVertexData<T1, T2>((PrimitiveTopology tipo, T1[] vertexMixedCoords, ushort[] vertexIndices) data, T2[] instances)
    {
        // Como os dados estao mixed, interleaved (vertex coords + UV coords) é necessario informar esse format aqui (HARD CODED).
        var vertex1Format = Format.R32G32B32_Float; // hardcoded: data must be in this format
        var vertex2Format = Format.R32G32_Float; // hardcoded: data must be in this format
        var instanceFormat = InputDesc.GetFormatFromType<T2>();
        var inputElements = D3D12_InputDesc.GetInputA1B1I2(vertex1Format, vertex2Format, instanceFormat);
        PSOdesc.InputLayout = inputElements; // complete pipeline description
        PSOdesc.PrimitiveTopologyType = D3D12_AuxFunctions.PrimitiveTopologyD3D12(data.tipo);
        PSO = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc); // var a1 = DEV12_5.DeviceRemovedReason; 
        PSO.Name = "Pipeline";

        tipo = data.tipo;

        var qtd_vertices = data.vertexMixedCoords.Length; // local because it is necessary only here
        qtd_instances = instances.Length;

        vertexMixedCoords = CreateBufferResourceAndUploadData(data.vertexMixedCoords, nameID: 0);
        vertexMixedCoords.Name = "vertexMixedCoords";
        instancesB = CreateBufferResourceAndUploadData(instances, nameID: 0);
        instancesB.Name = "instancesB";

        VBV = new VertexBufferView[]
        {
            new VertexBufferView(
                bufferLocation: vertexMixedCoords.GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T1>() * qtd_vertices,
                strideInBytes: Unsafe.SizeOf<T1>()),
            new VertexBufferView(
                bufferLocation: instancesB.GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T2>() * qtd_instances,
                strideInBytes: Unsafe.SizeOf<T2>())
        };

        qtd_indices = data.vertexIndices.Length;
        vertexIndices = CreateBufferResourceAndUploadData(data.vertexIndices, nameID: 0);
        vertexIndices.Name = "vertexIndices";
        IBV = new IndexBufferView(
            bufferLocation: vertexIndices.GPUVirtualAddress,
            sizeInBytes: qtd_indices * sizeof(ushort),
            is32Bit: false); // 16-bit or 65K indices. Use "true" for a lot more indices.    
    }
    public void Draw()
    {
        // Pipeline state
        CL12_4.SetGraphicsRootSignature(rootSignature);

        // Pipeline state
        CL12_4.SetPipelineState(PSO);

        CL12_4.SetDescriptorHeaps(heap: TEXTURE_HEAP);
        CL12_4.SetGraphicsRootDescriptorTable(rootParameterIndex: 1, TEXTURE_HEAP.GetGPUDescriptorHandleForHeapStart());

        // Input Assembler stage
        CL12_4.IASetPrimitiveTopology(tipo);
        CL12_4.IASetVertexBuffers(MyShaderSources.vertexSlot, VBV);
        CL12_4.IASetIndexBuffer(IBV);

        // MVP constant buffer
        CL12_4.SetGraphicsRoot32BitConstants(0, model * Camera1.ViewProjection, 0);

        // Draw
        CL12_4.DrawIndexedInstanced(
            indexCountPerInstance: qtd_indices,
            instanceCount: qtd_instances,
            startIndexLocation: 0,
            baseVertexLocation: 0,
            startInstanceLocation: 0);
    }
    public void Dispose()
    {
        rootSignature.Dispose();
        vertexMixedCoords.Dispose();
        vertexIndices.Dispose();
        instancesB.Dispose();
        TEXTURE.Dispose();
        TEXTURE_HEAP.Dispose();
        PSO.Dispose();
    }
}
///<summary>instanced + texture + indexed + mvp, 2 coords dataset</summary>
public class DrawSetBase_NoMulti2 : D3D12_ShaderCompiler
{
    public Matrix4x4 model; // Draw part
    //
    public ID3D12Resource vertexCoords; // (contem textureCoords) keep resource to proper disposal
    public VertexBufferView[] VBV; // Draw part
    //
    public ID3D12Resource textureCoords; // keep resource to proper disposal
    public ID3D12Resource TEXTURE; // keep resource to proper disposal
    public ID3D12DescriptorHeap TEXTURE_HEAP; // Draw part
    //
    public int qtd_indices; // Draw part
    public ID3D12Resource vertexIndices; // keep resource to proper disposal
    public IndexBufferView IBV; // Draw part
    //
    public int qtd_instances; // Draw part
    public ID3D12Resource instancesB; // keep resource to proper disposal
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public PrimitiveTopology tipo; // Draw part
    public ID3D12PipelineState PSO; // Draw part
    public GraphicsPipelineStateDescription PSOdesc;

    public DrawSetBase_NoMulti2(string shader) : base(shader)
    {
        model = Matrix4x4.Identity;

        rootSignature = D3D12_Signatures.GetRootSignature(2); // 2 parameters: constant MVP and Table with SRV for texture
        PSOdesc = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = dsvFormat,
            RenderTargetFormats = new[] { rtvFormat },
            SampleDescription = SampleDescription.Default,
            SampleMask = uint.MaxValue
        };
    }
    public void Texture99((int width, int height, byte[] byteArray) input1)
    {
        // Description of a texture ( Texture2D ).
        var textureDesc = new ResourceDescription()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (ulong)input1.width,
            Height = input1.height,
            Format = rtvFormat,
            SampleDescription = SampleDescription.Default,
            DepthOrArraySize = 1,
            MipLevels = 1,
        };

        TEXTURE = CreateTextureResourceAndUploadData(textureDesc, input1.byteArray);

        // Describe and create a shader resource view (SRV) heap for the texture.
        var descriptorHeapDesc = new DescriptorHeapDescription(
            type: DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            descriptorCount: 1,
            DescriptorHeapFlags.ShaderVisible);

        TEXTURE_HEAP = DEV12_8.CreateDescriptorHeap(description: descriptorHeapDesc);
        TEXTURE_HEAP.Name = "MAMA Heap da Textura";

        // SRV (shader resource view) for accessing data in a resource: Describe & create.
        var srvDesc = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = TEXTURE.Description.Format,
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
        };
        srvDesc.Texture2D.MipLevels = 1;
        DEV12_8.CreateShaderResourceView(resource: TEXTURE, desc: srvDesc, destDescriptor: TEXTURE_HEAP.GetCPUDescriptorHandleForHeapStart());
    }
    public void SetVertexData<T1, T2, T3>((PrimitiveTopology tipo, T1[] vertexCoords, ushort[] vertexIndices, T2[] textureCoords) data, T3[] instances)
    {
        // Como os dados estao mixed, interleaved (vertex coords + UV coords) é necessario informar esse format aqui (HARD CODED).
        var vertex1Format = InputDesc.GetFormatFromType<T1>();
        var vertex2Format = InputDesc.GetFormatFromType<T2>();
        var instanceFormat = InputDesc.GetFormatFromType<T3>();
        var inputElements = D3D12_InputDesc.GetInputA1B2I3(vertex1Format, vertex2Format, instanceFormat);
        PSOdesc.InputLayout = inputElements; // complete pipeline description
        PSOdesc.PrimitiveTopologyType = D3D12_AuxFunctions.PrimitiveTopologyD3D12(data.tipo);
        PSO = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc); // var a1 = DEV12_5.DeviceRemovedReason; 
        PSO.Name = "Pipeline";

        tipo = data.tipo;

        var qtd_vertices = data.vertexCoords.Length; // local because it is necessary only here
        qtd_instances = instances.Length;
        vertexCoords = CreateBufferResourceAndUploadData(data.vertexCoords, nameID: 0);
        vertexCoords.Name = "vertexMixedCoords";
        textureCoords = CreateBufferResourceAndUploadData(data.textureCoords, nameID: 0);
        textureCoords.Name = "textureCoords";
        instancesB = CreateBufferResourceAndUploadData(instances, nameID: 0);
        instancesB.Name = "instancesB";
        VBV = new VertexBufferView[]
        {
            new VertexBufferView(
                bufferLocation: vertexCoords.GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T1>() * qtd_vertices,
                strideInBytes: Unsafe.SizeOf<T1>()),

            new VertexBufferView(
                bufferLocation: textureCoords.GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T2>() * qtd_vertices,
                strideInBytes: Unsafe.SizeOf<T2>()),

            new VertexBufferView(
                bufferLocation: instancesB.GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T3>() * qtd_instances,
                strideInBytes: Unsafe.SizeOf<T3>())
        };

        qtd_indices = data.vertexIndices.Length;
        vertexIndices = CreateBufferResourceAndUploadData(data.vertexIndices, nameID: 0);
        vertexIndices.Name = "vertexIndices";
        IBV = new IndexBufferView(
            bufferLocation: vertexIndices.GPUVirtualAddress,
            sizeInBytes: qtd_indices * sizeof(ushort),
            is32Bit: false); // 16-bit or 65K indices. Use "true" for a lot more indices.
    }
    public void Draw()
    {
        CL12_4.SetGraphicsRootSignature(rootSignature);

        // Pipeline state
        CL12_4.SetPipelineState(PSO);

        CL12_4.SetDescriptorHeaps(heap: TEXTURE_HEAP);
        CL12_4.SetGraphicsRootDescriptorTable(rootParameterIndex: 1, TEXTURE_HEAP.GetGPUDescriptorHandleForHeapStart());

        // IA stage (Input-Assembler)
        CL12_4.IASetPrimitiveTopology(primitiveTopology: tipo);
        CL12_4.IASetVertexBuffers(startSlot: MyShaderSources.vertexSlot, VBV);
        CL12_4.IASetIndexBuffer(view: IBV);

        // MVP constant buffer
        CL12_4.SetGraphicsRoot32BitConstants(rootParameterIndex: 0, srcData: model * Camera1.ViewProjection, destOffsetIn32BitValues: 0);

        // Draw
        CL12_4.DrawIndexedInstanced(
            indexCountPerInstance: qtd_indices,
            instanceCount: qtd_instances,
            startIndexLocation: 0,
            baseVertexLocation: 0,
            startInstanceLocation: 0);

    }
    public void Dispose()
    {
        rootSignature.Dispose();
        vertexCoords.Dispose();
        textureCoords.Dispose();
        vertexIndices.Dispose();
        instancesB.Dispose();
        TEXTURE.Dispose();
        TEXTURE_HEAP.Dispose();
        PSO.Dispose();
    }
}

// L5 SandBox indroduction: MULTIDATA
// (A) No instanced: 1 vertex buffer mixed: float3 vertex coords + float2 UV coords
// (B) No instanced: 2 vertex buffers: one for vertex coods <T1>, other for UV coords <T2>
// (C) Instances: 1 vertex buffer mixed: float3 vertex coords + float2 UV coords
// (D) Instanced: 2 vertex buffers: one for vertex coods <T1>, other for UV coords <T2>
/// <summary>multidata + texture + indexing + mvp, 1 mixed coords dataset</summary>
public class DrawSetBase1 : D3D12_ShaderCompiler
{
    public int qtd; // Draw part
    public Matrix4x4[] model; // Draw part
    //
    public ID3D12Resource[] vertexMixedCoords; // (contem textureCoords) keep resource to proper disposal
    public VertexBufferView[] VBV; // Draw part
    //
    public ID3D12Resource[] TEXTURE; // keep resource to proper disposal
    public ID3D12DescriptorHeap[] TEXTURE_HEAP; // Draw part
    //
    public int[] qtd_indices;
    public ID3D12Resource[] vertexIndices; // keep resource to proper disposal
    public IndexBufferView[] IBV; // Draw part
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public PrimitiveTopology[] tipo; // Draw part
    public ID3D12PipelineState[] PSO; // Draw part
    public GraphicsPipelineStateDescription PSOdesc; // basta 1 para todos (só muda InputLayout e PrimitiveTopologyType no SetVertexData(..))

    public DrawSetBase1(int qtd, string sh) : base(sh)
    {
        rootSignature = D3D12_Signatures.GetRootSignature(2); // 2 parameters: constant MVP and Table with SRV for texture
        PSOdesc = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = dsvFormat,
            RenderTargetFormats = new[] { rtvFormat },
            SampleDescription = SampleDescription.Default,
            SampleMask = uint.MaxValue
        };
        this.qtd = qtd;
        qtd_indices = new int[qtd];
        model = Enumerable.Repeat(Matrix4x4.Identity, qtd).ToArray();
        tipo = new PrimitiveTopology[qtd];
        IBV = new IndexBufferView[qtd];
        PSO = new ID3D12PipelineState[qtd];
        VBV = new VertexBufferView[qtd];
        vertexMixedCoords = new ID3D12Resource[qtd];
        vertexIndices = new ID3D12Resource[qtd];
        TEXTURE = new ID3D12Resource[qtd];
        TEXTURE_HEAP = new ID3D12DescriptorHeap[qtd];
    }
    public void Texture99(int index, (int width, int height, byte[] byteArray) input1)
    {
        // Description of a texture ( Texture2D ).
        var textureDesc = new ResourceDescription()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (ulong)input1.width,
            Height = input1.height,
            Format = rtvFormat,
            SampleDescription = SampleDescription.Default,
            DepthOrArraySize = 1,
            MipLevels = 1,
        };

        TEXTURE[index] = CreateTextureResourceAndUploadData(textureDesc, input1.byteArray);

        // Describe and create a shader resource view (SRV) heap for the texture.
        var descriptorHeapDesc = new DescriptorHeapDescription(
            type: DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            descriptorCount: 1,
            DescriptorHeapFlags.ShaderVisible);

        TEXTURE_HEAP[index] = DEV12_8.CreateDescriptorHeap(description: descriptorHeapDesc);
        TEXTURE_HEAP[index].Name = $"MAMA Heap da Textura 20 NDX-{index}";

        // SRV (shader resource view) for accessing data in a resource: Describe & create.
        var srvDesc = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = TEXTURE[index].Description.Format,
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
        };
        srvDesc.Texture2D.MipLevels = 1;
        DEV12_8.CreateShaderResourceView(resource: TEXTURE[index], desc: srvDesc, destDescriptor: TEXTURE_HEAP[index].GetCPUDescriptorHandleForHeapStart());
    }
    public void SetVertexData<T>(int index, (PrimitiveTopology tipo, T[] vertexMixedCoords, ushort[] vertexIndices) data)
    {
        // Como os dados estao mixed, interleaved (vertex coords + UV coords) é necessario informar esse format aqui (HARD CODED).
        var vertex1Format = Format.R32G32B32_Float; // hardcoded: data must be in this format
        var vertex2Format = Format.R32G32_Float; // hardcoded: data must be in this format
        var inputElements = D3D12_InputDesc.GetInputA1B1(vertex1Format, vertex2Format);
        PSOdesc.InputLayout = inputElements; // complete pipeline description
        PSOdesc.PrimitiveTopologyType = D3D12_AuxFunctions.PrimitiveTopologyD3D12(data.tipo);
        PSO[index] = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc); // var a1 = DEV12_5.DeviceRemovedReason;
        PSO[index].Name = $"Pipeline NDX-{index}";

        tipo[index] = data.tipo;

        var qtd_vertices = data.vertexMixedCoords.Length; // local because it is necessary only here
        vertexMixedCoords[index] = CreateBufferResourceAndUploadData(data.vertexMixedCoords, nameID: index);
        vertexMixedCoords[index].Name = $"vertexMixedCoords NDX-{index}";
        VBV[index] = new VertexBufferView(
            bufferLocation: vertexMixedCoords[index].GPUVirtualAddress,
            sizeInBytes: Unsafe.SizeOf<T>() * qtd_vertices,
            strideInBytes: Unsafe.SizeOf<T>());

        qtd_indices[index] = data.vertexIndices.Length;
        vertexIndices[index] = CreateBufferResourceAndUploadData(data.vertexIndices, nameID: index);
        vertexIndices[index].Name = $"vertexIndices NDX-{index}";
        IBV[index] = new IndexBufferView(
            bufferLocation: vertexIndices[index].GPUVirtualAddress,
            sizeInBytes: qtd_indices[index] * sizeof(ushort),
            is32Bit: false); // 16-bit or 65K indices. Use "true" for a lot more indices.
    }
    public void SetRenderMode(int index, RenderMode r)
    {
        PSOdesc.RasterizerState = GetRasterizerDesc(r);
        PSO[index] = DEV12_8.CreateGraphicsPipelineState<ID3D12PipelineState>(PSOdesc);
    }
    public void SetTranslation(int index, Vector3 v) => model[index].Translation = v;
    public void Draw()
    {
        // Pipeline state
        CL12_4.SetGraphicsRootSignature(rootSignature);

        for (var n1 = 0; n1 < qtd; n1++)
        {
            // Pipeline state
            CL12_4.SetPipelineState(PSO[n1]);

            CL12_4.SetDescriptorHeaps(heap: TEXTURE_HEAP[n1]);
            CL12_4.SetGraphicsRootDescriptorTable(rootParameterIndex: 1, TEXTURE_HEAP[n1].GetGPUDescriptorHandleForHeapStart());

            // IA stage (Input-Assembler)
            CL12_4.IASetPrimitiveTopology(primitiveTopology: tipo[n1]);
            CL12_4.IASetVertexBuffers(slot: MyShaderSources.vertexSlot, VBV[n1]);
            CL12_4.IASetIndexBuffer(view: IBV[n1]);

            // MVP constant buffer
            CL12_4.SetGraphicsRoot32BitConstants(rootParameterIndex: 0, srcData: model[n1] * Camera1.ViewProjection, destOffsetIn32BitValues: 0);

            // Draw
            CL12_4.DrawIndexedInstanced(indexCountPerInstance: qtd_indices[n1], instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
        }
    }
    public void Dispose()
    {
        rootSignature.Dispose();
        for (var n1 = 0; n1 < qtd; n1++)
        {
            vertexMixedCoords[n1].Dispose();
            vertexIndices[n1].Dispose();
            TEXTURE[n1].Dispose();
            TEXTURE_HEAP[n1].Dispose();
            PSO[n1].Dispose();
        }
    }
}
/// <summary>multidata + texture + indexing + mvp, 2 coords dataset</summary>
public class DrawSetBase2 : D3D12_ShaderCompiler
{
    public int qtd; // Draw part
    public Matrix4x4[] model; // Draw part
    //
    public ID3D12Resource[] vertexCoords; // keep resource to proper disposal
    public VertexBufferView[][] VBV; // Draw part
    //
    public ID3D12Resource[] textureCoords; // keep resource to proper disposal
    public ID3D12Resource[] TEXTURE; // keep resource to proper disposal
    public ID3D12DescriptorHeap[] TEXTURE_HEAP; // Draw part
    //
    public int[] qtd_indices; // Draw part
    public ID3D12Resource[] vertexIndices; // keep resource to proper disposal
    public IndexBufferView[] IBV; // Draw part
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public PrimitiveTopology[] tipo; // Draw part
    public ID3D12PipelineState[] PSO; // Draw part
    public GraphicsPipelineStateDescription PSOdesc; // basta 1 para todos (só muda InputLayout e PrimitiveTopologyType no SetVertexData(..))

    public DrawSetBase2(int qtd, string sh) : base(sh)
    {
        rootSignature = D3D12_Signatures.GetRootSignature(2); // 2 parameters: constant MVP and Table with SRV for texture
        PSOdesc = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = dsvFormat,
            RenderTargetFormats = new[] { rtvFormat },
            SampleDescription = SampleDescription.Default,
            SampleMask = uint.MaxValue
        };
        this.qtd = qtd;
        qtd_indices = new int[qtd];
        model = Enumerable.Repeat(Matrix4x4.Identity, qtd).ToArray();
        tipo = new PrimitiveTopology[qtd];
        IBV = new IndexBufferView[qtd];
        PSO = new ID3D12PipelineState[qtd];
        VBV = new VertexBufferView[qtd][];
        vertexCoords = new ID3D12Resource[qtd];
        textureCoords = new ID3D12Resource[qtd];
        vertexIndices = new ID3D12Resource[qtd];
        TEXTURE = new ID3D12Resource[qtd];
        TEXTURE_HEAP = new ID3D12DescriptorHeap[qtd];
    }
    public void Texture99(int index, (int width, int height, byte[] byteArray) input1)
    {
        // Description of a texture ( Texture2D ).
        var textureDesc = new ResourceDescription()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (ulong)input1.width,
            Height = input1.height,
            Format = rtvFormat,
            SampleDescription = SampleDescription.Default,
            DepthOrArraySize = 1,
            MipLevels = 1,
        };

        TEXTURE[index] = CreateTextureResourceAndUploadData(textureDesc, input1.byteArray);

        // Describe and create a shader resource view (SRV) heap for the texture.
        var descriptorHeapDesc = new DescriptorHeapDescription(
            type: DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            descriptorCount: 1,
            DescriptorHeapFlags.ShaderVisible);

        TEXTURE_HEAP[index] = DEV12_8.CreateDescriptorHeap(description: descriptorHeapDesc);
        TEXTURE_HEAP[index].Name = $"MAMA Heap da Textura 20 NUM-{index}";

        // SRV (shader resource view) for accessing data in a resource: Describe & create.
        var srvDesc = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = TEXTURE[index].Description.Format,
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
        };
        srvDesc.Texture2D.MipLevels = 1;
        DEV12_8.CreateShaderResourceView(resource: TEXTURE[index], desc: srvDesc, destDescriptor: TEXTURE_HEAP[index].GetCPUDescriptorHandleForHeapStart());
    }
    public void SetVertexData<T1, T2>(int index, (PrimitiveTopology tipo, T1[] vertexCoords, ushort[] index, T2[] textureCoords) data)
    {
        // Como os dados (vertex coords + UV coords) estao interleaved é necessario informar esse format aqui (hardcoded).
        var vertex1Format = InputDesc.GetFormatFromType<T1>();
        var vertex2Format = InputDesc.GetFormatFromType<T2>();
        var inputElements = D3D12_InputDesc.GetInputA1B2(vertex1Format, vertex2Format);
        PSOdesc.InputLayout = inputElements;
        PSOdesc.PrimitiveTopologyType = D3D12_AuxFunctions.PrimitiveTopologyD3D12(data.tipo);
        PSO[index] = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc); // var a1 = DEV12_5.DeviceRemovedReason;
        PSO[index].Name = $"Pipeline NDX-{index}";

        tipo[index] = data.tipo;
        var qtd_vertices = data.vertexCoords.Length; // local because it is necessary only here
        if (data.textureCoords.Length != qtd_vertices) throw new Exception();
        qtd_indices[index] = data.index.Length;

        vertexCoords[index] = CreateBufferResourceAndUploadData(data.vertexCoords, nameID: index);
        vertexCoords[index].Name = $"vertexCoords NDX-{index}";
        textureCoords[index] = CreateBufferResourceAndUploadData(data.textureCoords, nameID: index);
        textureCoords[index].Name = $"textureCoords NDX-{index}";
        VBV[index] = new VertexBufferView[]
        {
            new VertexBufferView(
                bufferLocation: vertexCoords[index].GPUVirtualAddress, 
                sizeInBytes: Unsafe.SizeOf<T1>() * qtd_vertices, 
                strideInBytes: Unsafe.SizeOf<T1>()),
            new VertexBufferView(
                bufferLocation: textureCoords[index].GPUVirtualAddress, 
                sizeInBytes: Unsafe.SizeOf<T2>() * qtd_vertices, 
                strideInBytes: Unsafe.SizeOf<T2>())
        };

        vertexIndices[index] = CreateBufferResourceAndUploadData(data.index, nameID: index);
        vertexIndices[index].Name = $"vertexIndices NDX-{index}";
        IBV[index] = new IndexBufferView(
            bufferLocation: vertexIndices[index].GPUVirtualAddress,
            sizeInBytes: qtd_indices[index] * sizeof(ushort),
            is32Bit: false); // 16-bit or 65K indices. Use "true" for a lot more indices.
    }
    public void Draw()
    {
        CL12_4.SetGraphicsRootSignature(rootSignature);

        for (var n1 = 0; n1 < qtd; n1++)
        {
            // Pipeline state
            CL12_4.SetPipelineState(PSO[n1]);

            CL12_4.SetDescriptorHeaps(heap: TEXTURE_HEAP[n1]);
            CL12_4.SetGraphicsRootDescriptorTable(rootParameterIndex: 1, TEXTURE_HEAP[n1].GetGPUDescriptorHandleForHeapStart());

            // IA stage (Input-Assembler)
            CL12_4.IASetPrimitiveTopology(primitiveTopology: tipo[n1]);
            CL12_4.IASetVertexBuffers(startSlot: MyShaderSources.vertexSlot, VBV[n1]);
            CL12_4.IASetIndexBuffer(view: IBV[n1]);

            // MVP constant buffer
            CL12_4.SetGraphicsRoot32BitConstants(rootParameterIndex: 0, srcData: model[n1] * Camera1.ViewProjection, destOffsetIn32BitValues: 0);

            // Draw
            CL12_4.DrawIndexedInstanced(indexCountPerInstance: qtd_indices[n1], instanceCount: 1, startIndexLocation: 0, baseVertexLocation: 0, startInstanceLocation: 0);
        }
    }
    public void Dispose()
    {
        rootSignature.Dispose();
        for (var n1 = 0; n1 < qtd; n1++)
        {
            vertexCoords[n1].Dispose();
            textureCoords[n1].Dispose();
            vertexIndices[n1].Dispose();
            TEXTURE[n1].Dispose();
            TEXTURE_HEAP[n1].Dispose();
            PSO[n1].Dispose();
        }
    }
}
///<summary>multidata + instanced + texture + indexed + mvp, 1 mixed coords dataset</summary>
public class DrawSetBaseInstanced1 : D3D12_ShaderCompiler
{
    public int qtd; // Draw part
    public Matrix4x4[] model; // Draw part
    //
    public ID3D12Resource[] vertexMixedCoords; // (contem textureCoords) keep resource to proper disposal
    public VertexBufferView[][] VBV; // Draw part
    //
    public ID3D12Resource[] TEXTURE; // keep resource to proper disposal
    public ID3D12DescriptorHeap[] TEXTURE_HEAP; // Draw part
    //
    public int[] qtd_indices; // Draw part
    public ID3D12Resource[] vertexIndices; // keep resource to proper disposal
    public IndexBufferView[] IBV; // Draw part
    //
    public int[] qtd_instances; // Draw part
    public ID3D12Resource[] instancesB; // keep resource to proper disposal
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public PrimitiveTopology[] tipo; // Draw part
    public ID3D12PipelineState[] PSO; // Draw part
    public GraphicsPipelineStateDescription[] PSOdesc;
    public GraphicsPipelineStateDescription PSOdescAux; // basta 1 para todos (só muda InputLayout e PrimitiveTopologyType no SetVertexData(..))

    public DrawSetBaseInstanced1(int qtd, string sh) : base(sh)
    {
        rootSignature = D3D12_Signatures.GetRootSignature(2); // 2 parameters: constant MVP and Table with SRV for texture
        PSOdescAux = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = dsvFormat,
            RenderTargetFormats = new[] { rtvFormat },
            SampleDescription = SampleDescription.Default,
            SampleMask = uint.MaxValue
        };
        this.qtd = qtd;
        qtd_indices = new int[qtd];
        qtd_instances = new int[qtd];
        instancesB = new ID3D12Resource[qtd];
        model = Enumerable.Repeat(Matrix4x4.Identity, qtd).ToArray();
        tipo = new PrimitiveTopology[qtd];
        IBV = new IndexBufferView[qtd];
        PSO = new ID3D12PipelineState[qtd];
        PSOdesc = new GraphicsPipelineStateDescription[qtd];
        VBV = new VertexBufferView[qtd][];
        vertexMixedCoords = new ID3D12Resource[qtd];
        vertexIndices = new ID3D12Resource[qtd];
        TEXTURE = new ID3D12Resource[qtd];
        TEXTURE_HEAP = new ID3D12DescriptorHeap[qtd];
    }
    public void Texture99(int index, (int width, int height, byte[] byteArray) input1)
    {
        // Description of a texture ( Texture2D ).
        var textureDesc = new ResourceDescription()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (ulong)input1.width,
            Height = input1.height,
            Format = rtvFormat,
            SampleDescription = SampleDescription.Default,
            DepthOrArraySize = 1,
            MipLevels = 1,
        };

        TEXTURE[index] = CreateTextureResourceAndUploadData(textureDesc, input1.byteArray);

        // Describe and create a shader resource view (SRV) heap for the texture.
        var descriptorHeapDesc = new DescriptorHeapDescription(
            type: DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            descriptorCount: 1,
            DescriptorHeapFlags.ShaderVisible);

        TEXTURE_HEAP[index] = DEV12_8.CreateDescriptorHeap(description: descriptorHeapDesc);
        TEXTURE_HEAP[index].Name = $"MAMA Heap da Textura 20 NDX-{index}";

        // SRV (shader resource view) for accessing data in a resource: Describe & create.
        var srvDesc = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = TEXTURE[index].Description.Format,
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
        };
        srvDesc.Texture2D.MipLevels = 1;
        DEV12_8.CreateShaderResourceView(resource: TEXTURE[index], desc: srvDesc, destDescriptor: TEXTURE_HEAP[index].GetCPUDescriptorHandleForHeapStart());
    }
    public void SetVertexData<T1,T2>(int index, (PrimitiveTopology tipo, T1[] vertexMixedCoords, ushort[] vertexIndices) data, T2[] instances)
    {
        // Como os dados estao mixed, interleaved (vertex coords + UV coords) é necessario informar esse format aqui (HARD CODED).
        var vertex1Format = Format.R32G32B32_Float; // hardcoded: data must be in this format
        var vertex2Format = Format.R32G32_Float; // hardcoded: data must be in this format
        var instanceFormat = InputDesc.GetFormatFromType<T2>();
        var inputElements = D3D12_InputDesc.GetInputA1B1I2(vertex1Format, vertex2Format, instanceFormat);
        PSOdescAux.InputLayout = inputElements; // complete pipeline description
        PSOdescAux.PrimitiveTopologyType = D3D12_AuxFunctions.PrimitiveTopologyD3D12(data.tipo);
        PSOdesc[index] = PSOdescAux;
        PSO[index] = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc[index]); // var a1 = DEV12_5.DeviceRemovedReason; 
        PSO[index].Name = $"Pipeline NDX-{index}";

        tipo[index] = data.tipo;

        var qtd_vertices = data.vertexMixedCoords.Length; // local because it is necessary only here
        qtd_instances[index] = instances.Length;

        vertexMixedCoords[index] = CreateBufferResourceAndUploadData(data.vertexMixedCoords, nameID: index);
        vertexMixedCoords[index].Name = $"vertexMixedCoords NDX-{index}";
        instancesB[index] = CreateBufferResourceAndUploadData(instances, nameID: index);
        instancesB[index].Name = $"instancesB NDX-{index}";

        VBV[index] = new VertexBufferView[]
        {
            new VertexBufferView(
                bufferLocation: vertexMixedCoords[index].GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T1>() * qtd_vertices,
                strideInBytes: Unsafe.SizeOf<T1>()),
            new VertexBufferView(
                bufferLocation: instancesB[index].GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T2>() * qtd_instances[index],
                strideInBytes: Unsafe.SizeOf<T2>())
        };

        qtd_indices[index] = data.vertexIndices.Length;
        vertexIndices[index] = CreateBufferResourceAndUploadData(data.vertexIndices, nameID: index);
        vertexIndices[index].Name = $"vertexIndices NDX-{index}";
        IBV[index] = new IndexBufferView(
            bufferLocation: vertexIndices[index].GPUVirtualAddress,
            sizeInBytes: qtd_indices[index] * sizeof(ushort),
            is32Bit: false); // 16-bit or 65K indices. Use "true" for a lot more indices.    
    }
    public void SetRenderMode(int index, RenderMode r)
    {
        WaitForGpu();
        PSO[index].Dispose();
        PSOdesc[index].RasterizerState = GetRasterizerDesc(r);
        PSO[index] = DEV12_8.CreateGraphicsPipelineState<ID3D12PipelineState>(PSOdesc[index]);
    }
    public void SetTranslation(int index, Vector3 v) => model[index].Translation = v;
    public void Draw()
    {
        // Pipeline state
        CL12_4.SetGraphicsRootSignature(rootSignature);

        for (var n1 = 0; n1 < qtd; n1++)
        {
            // Pipeline state
            CL12_4.SetPipelineState(PSO[n1]);

            CL12_4.SetDescriptorHeaps(heap: TEXTURE_HEAP[n1]);
            CL12_4.SetGraphicsRootDescriptorTable(rootParameterIndex: 1, TEXTURE_HEAP[n1].GetGPUDescriptorHandleForHeapStart());

            // Input Assembler stage
            CL12_4.IASetPrimitiveTopology(tipo[n1]);
            CL12_4.IASetVertexBuffers(MyShaderSources.vertexSlot, VBV[n1]);
            CL12_4.IASetIndexBuffer(IBV[n1]);

            // MVP constant buffer
            CL12_4.SetGraphicsRoot32BitConstants(0, model[n1] * Camera1.ViewProjection, 0);

            // Draw
            CL12_4.DrawIndexedInstanced(
                indexCountPerInstance: qtd_indices[n1], 
                instanceCount: qtd_instances[n1], 
                startIndexLocation: 0, 
                baseVertexLocation: 0,
                startInstanceLocation: 0);
        }
    }
    public void Dispose()
    {
        rootSignature.Dispose();
        for (var n1 = 0; n1 < qtd; n1++)
        {
            vertexMixedCoords[n1].Dispose();
            vertexIndices[n1].Dispose();
            instancesB[n1].Dispose();
            TEXTURE[n1].Dispose();
            TEXTURE_HEAP[n1].Dispose();
            PSO[n1].Dispose();
        }
    }
}
///<summary>multidata + instanced + texture + indexed + mvp, 2 coords dataset</summary>
public class DrawSetBaseInstanced2 : D3D12_ShaderCompiler
{
    public int qtd; // Draw part
    public Matrix4x4[] model; // Draw part
    //
    public ID3D12Resource[] vertexCoords; // (contem textureCoords) keep resource to proper disposal
    public VertexBufferView[][] VBV; // Draw part
    //
    public ID3D12Resource[] textureCoords; // keep resource to proper disposal
    public ID3D12Resource[] TEXTURE; // keep resource to proper disposal
    public ID3D12DescriptorHeap[] TEXTURE_HEAP; // Draw part
    //
    public int[] qtd_indices; // Draw part
    public ID3D12Resource[] vertexIndices; // keep resource to proper disposal
    public IndexBufferView[] IBV; // Draw part
    //
    public int[] qtd_instances; // Draw part
    public ID3D12Resource[] instancesB; // keep resource to proper disposal
    //
    public ID3D12RootSignature rootSignature; // Draw part
    public PrimitiveTopology[] tipo; // Draw part
    public ID3D12PipelineState[] PSO; // Draw part
    public GraphicsPipelineStateDescription[] PSOdesc;
    public GraphicsPipelineStateDescription PSOdescAux; // basta 1 para todos (só muda InputLayout e PrimitiveTopologyType no SetVertexData(..))

    public DrawSetBaseInstanced2(int qtd, string sh) : base(sh)
    {
        this.qtd = qtd;
        model = Enumerable.Repeat(Matrix4x4.Identity, qtd).ToArray();

        vertexCoords = new ID3D12Resource[qtd];
        VBV = new VertexBufferView[qtd][];
        
        textureCoords = new ID3D12Resource[qtd];
        TEXTURE = new ID3D12Resource[qtd];
        TEXTURE_HEAP = new ID3D12DescriptorHeap[qtd];

        qtd_indices = new int[qtd];
        vertexIndices = new ID3D12Resource[qtd];
        IBV = new IndexBufferView[qtd];

        qtd_instances = new int[qtd];
        instancesB = new ID3D12Resource[qtd];

        tipo = new PrimitiveTopology[qtd];
        rootSignature = D3D12_Signatures.GetRootSignature(2); // 2 parameters: constant MVP and Table with SRV for texture
        PSO = new ID3D12PipelineState[qtd];
        PSOdesc = new GraphicsPipelineStateDescription[qtd];
        PSOdescAux = new GraphicsPipelineStateDescription()
        {
            RootSignature = rootSignature,
            VertexShader = VS12,
            PixelShader = PS12,
            BlendState = BlendDescription.Opaque,
            RasterizerState = GetRasterizerDesc(RenderMode.Solid), // Render both sides versus CullCounterClockwise rende single side
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = dsvFormat,
            RenderTargetFormats = new[] { rtvFormat },
            SampleDescription = SampleDescription.Default,
            SampleMask = uint.MaxValue
        };
    }
    public void Texture99(int index, (int width, int height, byte[] byteArray) input1)
    {
        // Description of a texture ( Texture2D ).
        var textureDesc = new ResourceDescription()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (ulong)input1.width,
            Height = input1.height,
            Format = rtvFormat,
            SampleDescription = SampleDescription.Default,
            DepthOrArraySize = 1,
            MipLevels = 1,
        };

        TEXTURE[index] = CreateTextureResourceAndUploadData(textureDesc, input1.byteArray);

        // Describe and create a shader resource view (SRV) heap for the texture.
        var descriptorHeapDesc = new DescriptorHeapDescription(
            type: DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            descriptorCount: 1,
            DescriptorHeapFlags.ShaderVisible);

        TEXTURE_HEAP[index] = DEV12_8.CreateDescriptorHeap(description: descriptorHeapDesc);
        TEXTURE_HEAP[index].Name = $"MAMA Heap da Textura 20 NDX-{index}";

        // SRV (shader resource view) for accessing data in a resource: Describe & create.
        var srvDesc = new ShaderResourceViewDescription()
        {
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
            Format = TEXTURE[index].Description.Format,
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
        };
        srvDesc.Texture2D.MipLevels = 1;
        DEV12_8.CreateShaderResourceView(resource: TEXTURE[index], desc: srvDesc, destDescriptor: TEXTURE_HEAP[index].GetCPUDescriptorHandleForHeapStart());
    }
    public void SetVertexData<T1, T2, T3>(int index, (PrimitiveTopology tipo, T1[] vertexCoords, ushort[] vertexIndices, T2[] textureCoords) data, T3[] instances)
    {
        // Como os dados estao mixed, interleaved (vertex coords + UV coords) é necessario informar esse format aqui (HARD CODED).
        var vertex1Format = InputDesc.GetFormatFromType<T1>();
        var vertex2Format = InputDesc.GetFormatFromType<T2>();
        var instanceFormat = InputDesc.GetFormatFromType<T3>();
        var inputElements = D3D12_InputDesc.GetInputA1B2I3(vertex1Format, vertex2Format, instanceFormat);
        PSOdescAux.InputLayout = inputElements; // complete pipeline description
        PSOdescAux.PrimitiveTopologyType = D3D12_AuxFunctions.PrimitiveTopologyD3D12(data.tipo);
        PSOdesc[index] = PSOdescAux;
        PSO[index] = DEV12_8.CreateGraphicsPipelineState(description: PSOdesc[index]); // var a1 = DEV12_5.DeviceRemovedReason; 
        PSO[index].Name = $"Pipeline NDX-{index}";

        tipo[index] = data.tipo;

        var qtd_vertices = data.vertexCoords.Length; // local because it is necessary only here
        qtd_instances[index] = instances.Length;
        vertexCoords[index] = CreateBufferResourceAndUploadData(data.vertexCoords, nameID: index);
        vertexCoords[index].Name = $"vertexMixedCoords NDX-{index}";
        textureCoords[index] = CreateBufferResourceAndUploadData(data.textureCoords, nameID: index);
        textureCoords[index].Name = $"textureCoords NDX-{index}";
        instancesB[index] = CreateBufferResourceAndUploadData(instances, nameID: index);
        instancesB[index].Name = $"instancesB NDX-{index}";
        VBV[index] = new VertexBufferView[]
        {
            new VertexBufferView(
                bufferLocation: vertexCoords[index].GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T1>() * qtd_vertices,
                strideInBytes: Unsafe.SizeOf<T1>()),

            new VertexBufferView(
                bufferLocation: textureCoords[index].GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T2>() * qtd_vertices,
                strideInBytes: Unsafe.SizeOf<T2>()),

            new VertexBufferView(
                bufferLocation: instancesB[index].GPUVirtualAddress,
                sizeInBytes: Unsafe.SizeOf<T3>() * qtd_instances[index],
                strideInBytes: Unsafe.SizeOf<T3>())
        };

        qtd_indices[index] = data.vertexIndices.Length;
        vertexIndices[index] = CreateBufferResourceAndUploadData(data.vertexIndices, nameID: index);
        vertexIndices[index].Name = $"vertexIndices NDX-{index}";
        IBV[index] = new IndexBufferView(
            bufferLocation: vertexIndices[index].GPUVirtualAddress,
            sizeInBytes: qtd_indices[index] * sizeof(ushort),
            is32Bit: false); // 16-bit or 65K indices. Use "true" for a lot more indices.
    }
    public void SetRenderMode(int index, RenderMode r)
    {
        WaitForGpu();
        PSO[index].Dispose();
        PSOdesc[index].RasterizerState = GetRasterizerDesc(r);
        PSO[index] = DEV12_8.CreateGraphicsPipelineState<ID3D12PipelineState>(PSOdesc[index]);
    }
    public void SetTranslation(int index, Vector3 v) => model[index].Translation = v;
    public void Draw()
    {
        CL12_4.SetGraphicsRootSignature(rootSignature);

        for (var n1 = 0; n1 < qtd; n1++)
        {
            // Pipeline state
            CL12_4.SetPipelineState(PSO[n1]);

            CL12_4.SetDescriptorHeaps(heap: TEXTURE_HEAP[n1]);
            CL12_4.SetGraphicsRootDescriptorTable(rootParameterIndex: 1, TEXTURE_HEAP[n1].GetGPUDescriptorHandleForHeapStart());

            // IA stage (Input-Assembler)
            CL12_4.IASetPrimitiveTopology(primitiveTopology: tipo[n1]);
            CL12_4.IASetVertexBuffers(startSlot: MyShaderSources.vertexSlot, VBV[n1]);
            CL12_4.IASetIndexBuffer(view: IBV[n1]);

            // MVP constant buffer
            CL12_4.SetGraphicsRoot32BitConstants(rootParameterIndex: 0, srcData: model[n1] * Camera1.ViewProjection, destOffsetIn32BitValues: 0);

            // Draw
            CL12_4.DrawIndexedInstanced(
                indexCountPerInstance: qtd_indices[n1], 
                instanceCount: qtd_instances[n1], 
                startIndexLocation: 0, 
                baseVertexLocation: 0, 
                startInstanceLocation: 0);
        }
    }
    public void Dispose()
    {
        rootSignature.Dispose();
        for (var n1 = 0; n1 < qtd; n1++)
        {
            vertexCoords[n1].Dispose();
            textureCoords[n1].Dispose();
            vertexIndices[n1].Dispose();
            instancesB[n1].Dispose();
            TEXTURE[n1].Dispose();
            TEXTURE_HEAP[n1].Dispose();
            PSO[n1].Dispose();
        }
    }
}

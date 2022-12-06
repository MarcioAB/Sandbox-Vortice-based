using System.Numerics;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Mathematics;
using Vortice.Direct3D11;
using Vortice.D3DCompiler;
using Vortice.Direct3D11.Debug;

using D3D_Mama;
using static D3D11_Mama.D3D11_Base;

#nullable disable

namespace D3D11_Mama;

public static class D3D11_Base
{
    public static event Action UserRender;

    public static ID3D11Device1 DEV11; // interface 0 parece ser suficiente
    public static IDXGISwapChain1 SWAP11_1; // Device.CreateSwapChainForHwnd cria versão 1 
    public static ID3D11DeviceContext DC11; // D3D11 Device context. Interface 4 is the highest to D3D11

    // Deep Stencil stuff ...
    public const Format dsvFormat = AuxFunctions.dsvFormat; //Format.D32_Float;
    public static ID3D11DepthStencilView DSV11; // D3D11 Deep-Stencil view

    // Render Target stuff ...
    public const Format rtvFormat = AuxFunctions.rtvFormat; // by the way, that is the format of my bitmaps
    public static ID3D11RenderTargetView RTV11; // D3D11 Render-Target view
    public static Color4 backgroudColor = new(0.0f, 0.2f, 0.4f, 1.0f);

    public static ID3D11RasterizerState1[] rasterizers;

    public static void D3DSetup(IntPtr hWnd, FeatureLevel level = FeatureLevel.Level_12_0, int qtdBackBuffers = 4)
    {
        DeviceAndContextCreator(level);
        CreateRasterizers();
        CreateSWAP11(hWnd, qtdBackBuffers); // mandatory for D3D
        CreateRTV11(); // render target views
        CreateDSV11(); // depende do Viewport
        DC11.RSSetViewport(Camera1.viewport); // Bind new viewport to Rasterizer-stage of the pipeline.
        //DebugLayer(DebugLayerTypes.Messages,"D3DSetup: end. Must be empty");
    }

    public static void CreateRasterizers()
    {
        // 4 rasterizers basicos para comodidade alinhados com D3D_Mama.RenderMode 
        rasterizers = new ID3D11RasterizerState1[]
        {
            DEV11.CreateRasterizerState1(RasterizerDescription1.CullNone), // solid
            DEV11.CreateRasterizerState1(RasterizerDescription1.Wireframe), // wireframe
            DEV11.CreateRasterizerState1(RasterizerDescription1.CullFront), // solid RenderBackOnly
            DEV11.CreateRasterizerState1(RasterizerDescription1.CullBack), // solid RenderFrontOnly ... same as DEV11 internal
        };
        rasterizers[0].DebugName = "Rasterizer CullNone"; // solid
        rasterizers[1].DebugName = "Rasterizer Wireframe"; // wireframe
        rasterizers[2].DebugName = "Rasterizer CullFront"; // solid RenderBackOnly
        rasterizers[3].DebugName = "Rasterizer CullBack"; // solid RenderFrontOnly ... same as DEV11 internal

        //DebugLayer1("CreateRasterizers");
    }

    public static void DeviceAndContextCreator(FeatureLevel level)
    {
        using (var adapter = GetHardwareAdapter1())
        {

            DeviceCreationFlags creationFlags = DeviceCreationFlags.BgraSupport;
#if DEBUG
            creationFlags |= DeviceCreationFlags.Debug; // NECESSARY to get ID3D11Debug (via query in this device)
#endif
            // D3D11CreateDevice aceita somente DriverType.Unknown considerando o adapter selecionado em GetHardwareAdapter1
            if (D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, creationFlags, new FeatureLevel[] { level }, out ID3D11Device tempDev).Failure)
                throw new Exception("can not create device");
            DEV11 = tempDev.QueryInterface<ID3D11Device1>();
            DEV11.DebugName = "DEV11";
            DEV11.ImmediateContext.DebugName = "DC11";
            DC11 = DEV11.ImmediateContext;
        }

        //DebugLayer1("CreateDevice");
        /* Device Creation é o 1º ponto a partir do qual é possivel usar o Debug Layer.
         * 
         * Device tem 4 references que não consegui identificar.
         * (obs1: Context NÃO é uma delas)
         * (obs2: candidates: Blend-state, DeptStencil-state, Rasterizer-state, Sampler, Query) 
         * 
         * Alem do create do Device (que não aparece nas mensagens),
         * essa API faz create & destroy de uma ID3D11Fence e de varios outros objetos:
         *
         * Resource objects:
         *  ID3D11Context           17C EEEC 0A30 <-- ImmediateContext
         *  ID3D11Sampler           17C EEEE 9A60
         *  ID3D11Query             17C EEEF BCA0
         * 
         * State objects:
         *  ID3D11RasterizerState   17C EABD 8F80 <-- effect state
         *  ID3D11DepthStencilState 17C EEEB C2D0 <-- effect state
         *  ID3D11BlendState        17C EEED 9C40 <-- effect state
         *  ID3DDeviceContextState  17C EEED C6F0
         */
    }
    public static IDXGIAdapter1 GetHardwareAdapter1()
    {
        // CreateFactory1 is OK to debug layer: use d3dconfig.exe command line tool to activate it
        // IDXGIFactory6 is minimum to Description1.Flags.
        using (var dxgiFactory6 = DXGI.CreateDXGIFactory1<IDXGIFactory6>()) 
        {
            if (dxgiFactory6.EnumAdapterByGpuPreference(0, GpuPreference.HighPerformance, out IDXGIAdapter1 adapter1).Failure) throw new Exception(fodeu("Cannot detect requested adapter"));
            if (adapter1 == null) throw new Exception(fodeu("adapter creation NULL"));
            if ((adapter1.Description1.Flags & AdapterFlags.Software) != AdapterFlags.None) throw new Exception(fodeu("Cannot detect hardware accelerated adapter"));
            return adapter1;
        }
    }
    static string fodeu(string msg1) => new string('*', 30) + "\r\n" + msg1 + "\r\n" + new string('*', 30);

    static void CreateSWAP11(IntPtr hWnd, int qtdBackBuffers)
    {
        var dxgiFactory2 = DXGI.CreateDXGIFactory1<IDXGIFactory2>();
        SWAP11_1 = dxgiFactory2.CreateSwapChainForHwnd(DEV11, hWnd, CreateSwapDesc(qtdBackBuffers));
        SWAP11_1.DebugName = "SWAP11";
        dxgiFactory2.Dispose();
        Camera1.viewport = new Viewport(SWAP11_1.Description1.Width, SWAP11_1.Description1.Height);
        // Curiosidade: Os back buffers (swap chain) podem ser menores ou maiores que o front buffer (client area).
        // Se for menor, a area que sobra recebe a "BackgroundColor".
        // Durante um resize da window, quando a client area ficar maior que os back buffers,
        // essa area maior recebe a "BackgroundColor". OBS: SwapChain com Scalling.None,
        SWAP11_1.BackgroundColor = backgroudColor;
        //DebugLayer1("Swap Chain");
        /* 
         * SwapChain: 
         *    n ID3D11Texture2D resources sendo uma para cada back buffer
         *    1 Fence
         */
    }

    public static SwapChainDescription1 CreateSwapDesc(int qtdBackBuffers)
    {
        return new SwapChainDescription1()
        {
            //Height = 500, // default = 0 ... full client area
            //Width = 1200, // default = 0 ... full client area
            //Stereo = false, // default = false
            Format = rtvFormat, // mandatory entry
            BufferCount = qtdBackBuffers, // mandatory, minimum = 2
            BufferUsage = Usage.RenderTargetOutput, // default = 0 (não consta)
            SampleDescription = SampleDescription.Default, // mandatory entry
            Scaling = Scaling.None, // default = Stretch
            SwapEffect = SwapEffect.FlipDiscard, // mandatory entry
            AlphaMode = AlphaMode.Ignore, // default = Unspecified
            Flags = SwapChainFlags.AllowTearing,
        };
    }
    static void CreateRTV11()
    {
        // TBD: It seems to be necessary the resource for RTV must be a texture extracted from swapchain(0).
        // It seems OK to create RTV without description.
        //var desc1 = new RenderTargetViewDescription(RenderTargetViewDimension.Texture2D, SWAP11_1.Description1.Format);
        //RTV11 =  DEV11.CreateRenderTargetView(texture2D, desc1);

        var texture2D = SWAP11_1.GetBuffer<ID3D11Texture2D>(index: 0); // Texture to be bound as a render target for the output-merger stage.
        texture2D.DebugName = "Texture for RTV";
        RTV11 = DEV11.CreateRenderTargetView(texture2D); 
        RTV11.DebugName = "RTV11";
        texture2D.Dispose(); // otherwise it will show up as live object in debug layer
        //DebugLayer1("Render-Target View");
        /* 
         * Render-Target view:
         *   1 ID3D11RenderTargetView
         */
    }

    static void CreateDSV11()
    {
        // It seems OK to create DSV without description.
        //var desc1 = new DepthStencilViewDescription(texture2D, DepthStencilViewDimension.Texture2D);
        //DSV11 = DEV11.CreateDepthStencilView(texture2D, desc1);

        var texture2D = DEV11.CreateTexture2D(dsvFormat, (int)Camera1.viewport.Width, (int)Camera1.viewport.Height, bindFlags: BindFlags.DepthStencil);
        texture2D.DebugName = "Texture for DSV";
        DSV11 = DEV11.CreateDepthStencilView(texture2D);
        DSV11.DebugName = "DSV11";
        texture2D.Dispose();
        //DebugLayer1("Depth-Stencil View");
        /*
         * Depth-Stencil view
         *   1 ID3D11DepthStencilView
         *   1 ID3D11Texture2D
         */
    }

    public static void D3D_Resize()
    {
        RTV11.Dispose();
        DSV11.Dispose();
        ResizeSWAP11();
        CreateRTV11(); // the render target views
        CreateDSV11(); // depende do Viewport
        DC11.RSSetViewport(Camera1.viewport); // Bind new viewport to Rasterizer-stage of the pipeline.
    }

    public static void ResizeSWAP11()
    {
        // Obs: aparentemente o dispose do RTV e DSV só ocorrem aqui no resize buffers do swapchain
        if (SWAP11_1.ResizeBuffers(0, 0, 0, Format.Unknown, SWAP11_1.Description1.Flags).Failure) throw new Exception();
        //Camera1.viewport = new Viewport(SWAP11_2.SourceSize.Width, SWAP11_2.SourceSize.Height);
        Camera1.viewport = new Viewport(SWAP11_1.Description1.Width, SWAP11_1.Description1.Height);

        //DebugLayer1("SWAP resize");
        /*
         * Destroy ID3D11RenderTargetView
         * Destroy ID3D11DepthStencilView
         * Destroy ID3D11Texture2D
         * 
         * Destroy ID3D11Fence 
         * Destroy ID3D11Texture2D (back buffers)
         * Create  ID3D11Texture2D (back buffers)
         * Create  ID3D11Fence
         */
    }

    /*
    public static void DebugLayer1(string name)
    {
        //DebugLayer(DebugLayerTypes.Messages, name);
        DebugLayer(DebugLayerTypes.Detail, name);
        //DebugLayer(DebugLayerTypes.Summary, name);
        Debug.WriteLine("");
    }
    */

    /*
    public static void DebugLayer2(string name)
    {
        DebugLayer(DebugLayerTypes.Messages, name);
        DebugLayer(DebugLayerTypes.Detail, name);
        DebugLayer(DebugLayerTypes.DetailInternal, name);
        DebugLayer(DebugLayerTypes.Summary, name);
        DebugLayer(DebugLayerTypes.SummaryInternal, name);
        Debug.WriteLine("");
    }
    */

    /* 
 * To get messages on the Output (Debug) window enable mixed-code debugging (nativeDebugging).
 * By default only the managed code debugging messages goes to Output window.
 * To get both (managed and unmanaged) use "Enable native code debugging".
 * This option is in the Debug Properties of the startup Project.
 */
    /*
    public static void DebugLayer(DebugLayerTypes tipo, string toto)
    {
        var d3dDebug = DEV11.QueryInterface<ID3D11Debug>();
        var d3dInfoQueue = d3dDebug.QueryInterface<ID3D11InfoQueue>();
        var toto2 = "Messages";
        var n1 = d3dInfoQueue.NumStoredMessages;
        if (tipo != DebugLayerTypes.Messages)
        {
            if (n1 != 0) throw new Exception("Queue não esta vazia");
            switch (tipo)
            {
                case DebugLayerTypes.Summary:
                    toto2 = "SUMMARY";
                    d3dDebug.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Summary | ReportLiveDeviceObjectFlags.IgnoreInternal);
                    break;
                case DebugLayerTypes.Detail:
                    toto2 = "DETAIL";
                    d3dDebug.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail | ReportLiveDeviceObjectFlags.IgnoreInternal);
                    break;
                case DebugLayerTypes.SummaryInternal:
                    toto2 = "SUMMARY INTERNAL";
                    d3dDebug.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Summary);
                    break;
                case DebugLayerTypes.DetailInternal:
                    toto2 = "DETAIL INTERNAL";
                    d3dDebug.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail);
                    break;
                case DebugLayerTypes.ValidateContext:
                    toto2 = "VALIDATE CONTEXT";
                    try { d3dDebug.ValidateContext(DC11); } catch { } // ignore
                    break;
            }
            n1 = d3dInfoQueue.NumStoredMessages;
        }
        d3dInfoQueue.AddApplicationMessage(MessageSeverity.Info, $"*** D3D11: {n1} {toto2} @ {toto}");
        Debug.WriteLine(d3dInfoQueue.GetMessage(n1).Description);
        Debug.WriteLine("");
        for (var n0 = 0u; n0 < n1; ++n0)
        {
            var v1 = d3dInfoQueue.GetMessage(n0);
            if (v1.Severity == MessageSeverity.Error || v1.Severity == MessageSeverity.Corruption) throw new Exception();
            Debug.WriteLine($"{n0,4} {v1.Severity,-10} {v1.Category,-20} {v1.Id,-50}{v1.Description}");
            Debug.WriteLine("");
        }
        d3dInfoQueue.ClearStoredMessages();
        d3dInfoQueue.Release();
        d3dDebug.Release();
    }

    public static void DebugLayerDETAIL(string toto)
    {
        var d3dDebug = DEV11.QueryInterface<ID3D11Debug>();
        var d3dInfoQueue = d3dDebug.QueryInterface<ID3D11InfoQueue>();
        d3dInfoQueue.ClearStoredMessages();
        d3dDebug.ReportLiveDeviceObjects(ReportLiveDeviceObjectFlags.Detail | ReportLiveDeviceObjectFlags.IgnoreInternal);
        var n1 = d3dInfoQueue.NumStoredMessages;
        d3dInfoQueue.AddApplicationMessage(MessageSeverity.Info, $"*** D3D11: {n1} DETAIL @ {toto}");
        Debug.WriteLine(d3dInfoQueue.GetMessage(n1).Description);
        Debug.WriteLine("");
        for (var n0 = 0u; n0 < n1; ++n0)
        {
            var v1 = d3dInfoQueue.GetMessage(n0);
            if (v1.Severity == MessageSeverity.Error || v1.Severity == MessageSeverity.Corruption) throw new Exception();
            Debug.WriteLine($"{n0,4} {v1.Severity,-10} {v1.Category,-20} {v1.Id,-50}{v1.Description}");
            Debug.WriteLine("");
        }
        d3dInfoQueue.Release();
        d3dDebug.Release();
    }
    */

    public static void D3D_Dispose()
    {
        DebugLayer.DebugLayer_DXGIbased("D3D_Dispose", true);

        DSV11.Dispose();
        SWAP11_1.Dispose();
        DC11.Dispose();
        RTV11.Dispose();
        rasterizers[0].Dispose();
        rasterizers[1].Dispose();
        rasterizers[2].Dispose();
        rasterizers[3].Dispose();
        DebugLayer.DXGI_DebugLayer0("D3D_Dispose", true); // Last chance to use DebugLayer
    }

    public static void D3D_MainDeviceDispose()
    {
        DEV11.Dispose(); // O ID3D11Device gera um exception no Dispose()
    }

    public static void Render()
    {
        SetupNewFrameRender();
        UserRender?.Invoke(); // Event definido na UI. Invoke DrawAll() no MyModel o qual corresponde a execução dos trocentos DrawSets definidos no MyModel
        PresentFrameRender();
    }
    public static void SetupNewFrameRender()
    {
        DC11.OMSetRenderTargets(RTV11, DSV11); // Bind RTV on Output-Merger stage: mandatory for each new render (really!!)
        DC11.ClearRenderTargetView(RTV11, backgroudColor); // "clear" background for new render
        DC11.ClearDepthStencilView(DSV11, DepthStencilClearFlags.Depth, 1.0f, 0); // clear deep-stencil for new render

        //drawTargetWithEixoXYZ.Draw();
    }
    public static void PresentFrameRender()
    {
        // if the line below is commented, debug layer will issue an INFO stating the OM Render Target slot 0 is forced to NULL.
        // debug layer says the reason is calling Present for DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL SwapChains unbinds backbuffer 0 from all GPU writeable bind points.
        // so ... I will unset it instead let the runtime do it.
        DC11.UnsetRenderTargets();

        if (SWAP11_1.Present(0, PresentFlags.None).Failure) throw new Exception("Error presenting frame"); // For stress test: GPU renders at max speed: Here GPU @ 75% & CPU @ 55%
        //if (SwapChain!.Present(1, PresentFlags.None).Failure) throw new Exception(); // GPU renders @ 75 fps: low GPU & CPU usage
    }
    // Especifico para D3D11


}

// Constant buffer explanation
/*******************************************************

CONSTANT BUFFER

The idea is to send data to Vertex Shader register type B using a buffer (a constant buffer). 

Despite the data itself be highly dynamic, the structure that holds the data must be constant (Matrix4x4 or any struct).
The structure to hold the data must be large enough and must be 16-bytes aligned.

A buffer object with the size of the structure must be created to carry on the data from CPU to GPU.
A mapped resource must be extracted from the buffer and the data must be copied to this resource.
Trick part: Pointers must be used to make this copy, such as:

      Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref myDATA)

After the copy the mapped resource must be unmapped and the buffer can be sent to Vertex Shader register type B

	DeviceContext.VSSetConstantBuffer(register_B, buffer)

Example:

     Source: Matrix4x4 CPUdata on register_B
Destination: float4x4  GPUdata on register(b0)

Simplified instructions to send data:

	BufferDescription.ByteWidth = SizeOf<Matrix4x4>()
	Buffer = Device.CreateBuffer(BufferDescription)
	Copy( DeviceContext.Map(Buffer), CPUdata )
	DeviceContext.VSSetConstantBuffer(slot0, Buffer)

Instruction for receive data:

	cbuffer params : register(b0) {float4x4 GPUdata;};

********************************************************/

public class D3D11_MVP
{
    ID3D11Buffer MVP_cBuffer;

    // Create a 64-bytes constant buffer for MVP ( Matrix4x4 )
    public D3D11_MVP()
    {
        var cBufferDesc = new BufferDescription { BindFlags = BindFlags.ConstantBuffer, Usage = ResourceUsage.Dynamic, CPUAccessFlags = CpuAccessFlags.Write };
        cBufferDesc.ByteWidth = 16 * (int)Math.Ceiling(Unsafe.SizeOf<Matrix4x4>() / 16d); // 16 bytes aligned size
        MVP_cBuffer = DEV11.CreateBuffer(cBufferDesc);
        MVP_cBuffer.DebugName = "MVP";
    }
    // Copy MVP data to MVP buffer
    public ID3D11Buffer UploadMVP(Matrix4x4 MVP)
    {
        unsafe { Unsafe.Copy(DC11.Map(MVP_cBuffer, MapMode.WriteDiscard).DataPointer.ToPointer(), ref MVP); }
        DC11.Unmap(MVP_cBuffer, 0);
        return MVP_cBuffer;
    }
    public void Dispose_MVP() => MVP_cBuffer.Dispose();
}

public class D3D11_ShaderCompiler
{
    public ID3D11VertexShader VS11;
    public ID3D11PixelShader PS11;
    public Blob VS11_ByteCode;

    (string vertexShader, string pixelShader) entrypoints = ("VSMain", "PSMain"); // default
    public virtual (string vertexShader, string pixelShader) Entrypoints { get => entrypoints; } // use default is upstream do not implemented
    public D3D11_ShaderCompiler(string sh)
    {
        // Obs: DirectX 11 does not support Shader Model 6: https://github.com/microsoft/DirectXTK12/wiki/Shader-Model-6
        if(Compiler.Compile(sh, Entrypoints.vertexShader, "", "vs_5_0", out VS11_ByteCode, out Blob toto1).Failure) throw new Exception($"vs compile: {toto1.AsString()}");
        if(Compiler.Compile(sh, Entrypoints.pixelShader, "", "ps_5_0", out Blob PS11_ByteCode, out Blob toto2).Failure) throw new Exception($"ps compile: {toto2.AsString()}");
        VS11 = DEV11.CreateVertexShader(VS11_ByteCode);
        VS11.DebugName = "VS11";
        PS11 = DEV11.CreatePixelShader(PS11_ByteCode);
        PS11.DebugName = "PS11";
        PS11_ByteCode.Dispose();
        //DebugLayer1("D3D11_ShaderCompiler");
        /*
         * Shaders 
         *    1 ID3D11VertexShader
         *    1 ID3D11PixelShader
         */
    }

    public void Dispose_Shader()
    {
        VS11.Dispose();
        PS11.Dispose();
        VS11_ByteCode.Dispose();
    }
}

public static class D3D11_InputDesc
{
    /// <summary>(entryA, slot1)</summary>
    public static InputElementDescription[] GetInputA1(Format slot1Format1)
    {
        var v1 = new InputElementDescription(
            semanticName: "entryA",
            semanticIndex: 0,
            format: slot1Format1,
            // offset default = AppendAligned
            slot: MyShaderSources.vertexSlot);

        return new[] { v1 };
    }

    /// <summary>(entryA, slot1) (entryB, slot1)</summary>
    public static InputElementDescription[] GetInputA1B1(Format slot1Format1, Format slot1Format2)
    {
        var v1 = new InputElementDescription(
            semanticName: "entryA",
            semanticIndex: 0,
            format: slot1Format1,
            slot: MyShaderSources.vertexSlot);

        var v2 = new InputElementDescription(
            semanticName: "entryB",
            semanticIndex: 0,
            format: slot1Format2,
            slot: MyShaderSources.vertexSlot);

        return new[] { v1, v2 };
    }

    /// <summary>(entryA, slot1) (entryB, slot2)</summary>
    public static InputElementDescription[] GetInputA1B2(Format slot1Format1, Format slot2Format1)
    {
        var v1 = new InputElementDescription(
            semanticName: "entryA",
            semanticIndex: 0,
            format: slot1Format1,
            slot: MyShaderSources.vertexSlot);

        var v2 = new InputElementDescription(
            semanticName: "entryB",
            semanticIndex: 0,
            format: slot2Format1,
            slot: MyShaderSources.vertexSlot + 1);

        return new[] { v1, v2 };
    }

    /// <summary>(entryA, slot1) (entryInstance, slot2)</summary>
    public static InputElementDescription[] GetInputA1I2(Format slot1Format1, Format slot2InstanceFormat1)
    {
        // For InputClassification = PerVertexData, for convenience 
        // For convenience do not inform offset if it is "AppendAligned".
        // offset = 0 also can be omited as it is a kind of "AppendAligned".
        // "AppendAligned" = InputElementDescription.AppendAligned

        var v1 = new InputElementDescription(
            semanticName: "entryA",
            semanticIndex: 0,
            format: slot1Format1,
            slot: MyShaderSources.vertexSlot);
        // slotClass = InputClassification.PerVertexData ... default
        // offset = InputElementDescription.AppendAligned ... default
        // stepRate = 0 ... default. Obs: must be 0 for PerVertexData

        var v2 = new InputElementDescription(
            semanticName: "entryInstance",
            semanticIndex: 0,
            format: slot2InstanceFormat1,
            offset: InputElementDescription.AppendAligned,
            slot: MyShaderSources.vertexSlot + 1, // for PerInstanceData, slots must be sequential, one per instance data buffer
            slotClass: InputClassification.PerInstanceData,
            stepRate: 1);
        // for PerInstanceData all parameters must be set.

        return new[] { v1, v2 };
    }

    /// <summary>(entryA, slot1) (entryB, slot1) (entryInstance, slot2)</summary>
    public static InputElementDescription[] GetInputA1B1I2(Format slot1Format1, Format slot1Format2, Format slot2InstanceFormat1)
    {
        var v1 = new InputElementDescription(
            semanticName: "entryA",
            semanticIndex: 0,
            format: slot1Format1,
            slot: MyShaderSources.vertexSlot);

        var v2 = new InputElementDescription(
            semanticName: "entryB",
            semanticIndex: 0,
            format: slot1Format2,
            slot: MyShaderSources.vertexSlot);

        var v3 = new InputElementDescription(
            semanticName: "entryInstance",
            semanticIndex: 0,
            format: slot2InstanceFormat1,
            offset: InputElementDescription.AppendAligned,
            slot: MyShaderSources.vertexSlot + 1, // for PerInstanceData, slots must be sequential, one per instance data buffer
            slotClass: InputClassification.PerInstanceData,
            stepRate: 1);
        // for PerInstanceData all parameters must be set.

        return new[] { v1, v2, v3 };
    }

    /// <summary>(entryA, slot1) (entryB, slot2) (entryInstance, slot3)</summary>
    public static InputElementDescription[] GetInputA1B2I3(Format slot1Format1, Format slot2Format2, Format slot3InstanceFormat1)
    {
        var v1 = new InputElementDescription(
            semanticName: "entryA",
            semanticIndex: 0,
            format: slot1Format1,
            slot: MyShaderSources.vertexSlot);

        var v2 = new InputElementDescription(
            semanticName: "entryB",
            semanticIndex: 0,
            format: slot2Format2,
            slot: MyShaderSources.vertexSlot + 1);

        var v3 = new InputElementDescription(
            semanticName: "entryInstance",
            semanticIndex: 0,
            format: slot3InstanceFormat1,
            offset: InputElementDescription.AppendAligned,
            slot: MyShaderSources.vertexSlot + 2, // for PerInstanceData, slots must be sequential, one per instance data buffer
            slotClass: InputClassification.PerInstanceData,
            stepRate: 1);
        // for PerInstanceData all parameters must be set.

        return new[] { v1, v2, v3 };
    }

}

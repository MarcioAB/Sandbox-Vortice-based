using System.Numerics;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Mathematics;
using Vortice.Direct3D12;
using Vortice.Dxc;
using Vortice.Direct3D12.Debug;

using D3D_Mama;
using static D3D12_Mama.D3D12_Base;

#nullable disable

namespace D3D12_Mama;

public static class D3D12_Base
{
    public const int D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING = 5768;

    public static event Action UserRender;

    public static ID3D12Device8 DEV12_8;

    // SwapChain
    public static IDXGISwapChain4 SWAP12_4; // to resuse everywhere
    public static ID3D12GraphicsCommandList4 CL12_4; // to reuse on SetupNewFrameRender()
    public static ID3D12CommandAllocator[] commandAllocators; // to reuse on SetupNewFrameRender()
    public static ID3D12CommandQueue Q12; // to reuse on PresentFrameRender

    // Render Target:
    public static ID3D12Resource[] RTV12; // to reuse on SetupNewFrameRender()
    public static ID3D12DescriptorHeap rtvDescriptorHeap; // to keep track to proper Disposal
    public static CpuDescriptorHandle[] rtvHandles; // to reuse on SetupNewFrameRender()
    public const Format rtvFormat = AuxFunctions.rtvFormat; // Render Target ... fechado nesse format por enquanto // by the way, that is the format of my bitmaps


    // Deep Stencil:
    public static ID3D12Resource DSV12; // to keep track to proper Disposal
    public static ID3D12DescriptorHeap dsvDescriptorHeap; // to keep track to proper Disposal
    public static CpuDescriptorHandle dsv12Handle; // to reuse on SetupNewFrameRender()
    public const Format dsvFormat = AuxFunctions.dsvFormat; // Deep Stencil


    public static Color4 backgroudColor = new(0f, 0.2f, 0.4f, 1f); // to reuse on SetupNewFrameRender()
    public static bool dxgiDebug;

    //public static ID3D12Fence fence1;
    //public static AutoResetEvent event1;

    public static ulong[] CPUcompleted;
    public static ID3D12Fence GPU;
    public static AutoResetEvent CPUsynchronizer;
    public static int _backBufferIndex;



    //public static ID3D12DescriptorHeap rtvHeap_MAMA;
    //public static ID3D12DescriptorHeap srvHeap_MAMA;

    // pipeline stuff ...


    public static void D3DSetup(IntPtr hWnd, FeatureLevel level = FeatureLevel.Level_12_0, int qtdBackBuffers = 4)
    {

#if DEBUG
        EnableD3D12DebugLayer();  // NECESSARY to enable DebugLayer. TBD: What if 
#endif

        DeviceCreator(level);
        CreateSWAP12(hWnd, qtdBackBuffers);
        CreateCommandList();
        CreateRTV12(); // render-target view
        CreateDSV12(); // deep-stencil view
        CreateFence();
    }
    public static void D3D_Resize()
    {
        DisposeRTV12();
        ResizeSWAP12(); // resize swapchain buffers requires all outstanding buffer references have been released ( prefer RTV dispose )
        ReCreateRTV12(); // recreate
        ReCreateDSV12(); // recreate
    }
    public static void D3D_Dispose()
    {

        DebugLayer.DebugLayer_DXGIbased("D3D_Dispose", true);

        WaitForGpu(); // garante que a GPU esta livre antes dos Disposals.
        GPU.Dispose();
        DSV12.Dispose();
        dsvDescriptorHeap.Dispose();
        for (int i = 0; i < commandAllocators.Length; i++)
        {
            commandAllocators[i].Dispose();
            RTV12[i].Dispose();
        }
        CL12_4.Dispose();
        rtvDescriptorHeap.Dispose();
        SWAP12_4.Release(); // Precisa desse Release aqui caso contrario o Dispose não funciona.
        SWAP12_4.Dispose();
        Q12.Dispose();

#if DEBUG
        DebugLayer.DXGI_DebugLayer0("D3D_Dispose", true); // Last chance to use DebugLayer
#endif

        DEV12_8.Dispose(); // No more DebugLayer here.
    }
    public static void Render()
    {
        SetupNewFrameRender();
        UserRender?.Invoke();
        PresentFrameRender();
    }
    public static RasterizerDescription GetRasterizerDesc(RenderMode tipo)
    {
        var x = tipo switch
        {
            RenderMode.Solid => RasterizerDescription.CullNone, // new RasterizerDescription(CullMode.None, FillMode.Solid),
            RenderMode.Wireframe => new RasterizerDescription(CullMode.None, FillMode.Wireframe), // new RasterizerDescription(CullMode.None, FillMode.Wireframe),
            RenderMode.Solid_RenderFrontOnly => RasterizerDescription.CullCounterClockwise, //new RasterizerDescription(CullMode.Back, FillMode.Solid),
            RenderMode.Solid_RenderBackOnly => RasterizerDescription.CullClockwise, //new RasterizerDescription(CullMode.Front, FillMode.Solid),
            _ => throw new Exception(),
        };
        return x;
    }
    public static ID3D12Resource CreateBufferResourceAndUploadData<T>(T[] buffer, int nameID)
    {
        // GetCopyableFootprints de Dimension.Buffer é o proprio sizeInBytes (bufferSize).

        var qtd_vertices = buffer.Length;
        var bufferSize = Unsafe.SizeOf<T>() * qtd_vertices;

        // local resource to create the Vertex Buffer View
        var dstResource = DEV12_8.CreateCommittedResource(
            heapType: HeapType.Default,
            description: ResourceDescription.Buffer(sizeInBytes: bufferSize),
            initialResourceState: ResourceStates.CopyDest);
        dstResource.Name = $"vertex buffer NDX-{nameID}";
        //Aux2.DXGI_DebugLayer2("Vetex buffer");
        /* ID3D12Resource (live)
         * ID3D12Heap (internal)
         */

        // intermediary local resource to (1) upload vertex data and (2) copy it into the local vertex buffer resource
        var srcResource = DEV12_8.CreateCommittedResource(
            heapType: HeapType.Upload,
            description: ResourceDescription.Buffer(sizeInBytes: bufferSize),
            initialResourceState: ResourceStates.GenericRead);
        srcResource.Name = $"intermediario NDX-{nameID}";
        //Aux2.DXGI_DebugLayer2("Intermediary");
        /*
         * ID3D12Resource (live)
         * ID3D12Heap (internal)
         */

        // copy vertex data to subresource 0 of vertex buffer resource 
        // (1) obter destination pointer via MAP do resource (o resource é um vertex buffer resource)
        // (2) obter o Span da source Array<T>
        // (3) criar um novo destination Span<T>, do mesmo tamanho da source Array<T> e a partir do destination pointer, 
        // (4) copiar tudo de um span para o outro
        unsafe
        {
            void* destinationPtr;
            srcResource.Map(subresource: 0, data: &destinationPtr).CheckError();
            buffer.AsSpan().CopyTo(destination: new Span<T>(pointer: destinationPtr, length: qtd_vertices)); // internal buffer.memmove
        }
        srcResource.Unmap(0);

        //Aux2.DXGI_DebugLayer3("p198", false);

        WaitForGpu(); // garante que a GPU esta pronta para liberar o recurso "source". Sem isso temos um Exception no Dispose() 

        commandAllocators[0].Reset(); // re-use command allocator
        CL12_4.Reset(commandAllocators[0]); // re-use command list + set command allocator
        CL12_4.CopyResource(dstResource, srcResource);
        CL12_4.Close();
        Q12.ExecuteCommandList(CL12_4);

        //Aux2.DXGI_DebugLayer3("p199", false);

        WaitForGpu(); // garante que a GPU esta pronta para liberar o recurso "source". Sem isso temos um Exception no Dispose()

        srcResource.Dispose();
        return dstResource;
    }
    public static ID3D12Resource CreateTextureResourceAndUploadData(ResourceDescription textureDesc, ReadOnlySpan<byte> textureData)
    {

        // 3 steps to create auxiliar buffer to upload the texture: (1) get required size, (2) describe it & (3) create it

        var layouts = new PlacedSubresourceFootPrint[1];
        var numRows = new int[1];
        var rowSizeInBytes = new ulong[1];

        DEV12_8.GetCopyableFootprints(resourceDesc: textureDesc, firstSubresource: 0, numSubresources: 1, baseOffset: 0, layouts, numRows, rowSizeInBytes, out ulong totalBytes);

        var srcResource = DEV12_8.CreateCommittedResource(
            heapType: HeapType.Upload,
            description: ResourceDescription.Buffer(totalBytes),
            initialResourceState: ResourceStates.GenericRead);
        srcResource.Name = "Intermediario";

        // Create destination resource ( Dimension: Texture2D )
        var dstResource = DEV12_8.CreateCommittedResource(
            heapType: HeapType.Default,
            description: textureDesc,
            initialResourceState: ResourceStates.CopyDest);
        dstResource.Name = "Texture";

        // copy byte[] data to subresource 0 of source resource 
        // (1) obter destination pointer via MAP do resource (o resource é um vertex buffer resource)
        // (2) obter o Span da source Array<T>
        // (3) criar um novo destination Span<T>, do mesmo tamanho da source Array<T> e a partir do destination pointer, 
        // (4) copiar tudo de um span para o outro
        unsafe
        {
            void* destinationPtr;
            srcResource.Map(subresource: 0, data: &destinationPtr).CheckError();
            textureData.CopyTo(destination: new Span<byte>(pointer: destinationPtr, length: (int)totalBytes)); // internal buffer.memmove
        }
        srcResource.Unmap(0);

        PlacedSubresourceFootPrint footPrint_0 = new PlacedSubresourceFootPrint() 
        { 
            Footprint = new SubresourceFootPrint() 
            { 
                Width = layouts[0].Footprint.Width, 
                Height = layouts[0].Footprint.Height, 
                RowPitch = layouts[0].Footprint.RowPitch, 
                Format = layouts[0].Footprint.Format,
                Depth = layouts[0].Footprint.Depth,
            }
        };

        var transicionador = new ResourceBarrier(new ResourceTransitionBarrier(dstResource, ResourceStates.CopyDest, ResourceStates.PixelShaderResource));

        commandAllocators[0].Reset(); // re-use command allocator
        CL12_4.Reset(commandAllocators[0]); // re-use command list + set command allocator
        CL12_4.CopyTextureRegion(new TextureCopyLocation(dstResource), 0, 0, 0, new TextureCopyLocation(srcResource, footPrint_0));
        CL12_4.ResourceBarrier(transicionador);
        CL12_4.Close();
        Q12.ExecuteCommandList(CL12_4);

        WaitForGpu(); // garante que a GPU esta livre antes dos Disposals.
        srcResource.Dispose();

        return dstResource;
    }

    public static ID3D12Resource CreateTextureResourceAndUploadData_ORIGINAL(ResourceDescription textureDesc, byte[] textureData)
    {

        // 3 steps to create auxiliar buffer to upload the texture: (1) get required size, (2) describe it & (3) create it

        var layouts = new PlacedSubresourceFootPrint[1];
        var numRows = new int[1];
        var rowSizeInBytes = new ulong[1];

        DEV12_8.GetCopyableFootprints(resourceDesc: textureDesc, firstSubresource: 0, numSubresources: 1, baseOffset: 0, layouts, numRows, rowSizeInBytes, out ulong totalBytes);

        var srcResource = DEV12_8.CreateCommittedResource(
            heapType: HeapType.Upload,
            description: ResourceDescription.Buffer(totalBytes),
            initialResourceState: ResourceStates.GenericRead);
        srcResource.Name = "Intermediario";

        // Create destination resource ( Dimension: Texture2D )
        var dstResource = DEV12_8.CreateCommittedResource(
            heapType: HeapType.Default,
            description: textureDesc,
            initialResourceState: ResourceStates.CopyDest);
        dstResource.Name = "Texture";

        // copy byte[] data to subresource 0 of source resource 
        // (1) obter destination pointer via MAP do resource (o resource é um vertex buffer resource)
        // (2) obter o Span da source Array<T>
        // (3) criar um novo destination Span<T>, do mesmo tamanho da source Array<T> e a partir do destination pointer, 
        // (4) copiar tudo de um span para o outro
        unsafe
        {
            void* destinationPtr;
            srcResource.Map(subresource: 0, data: &destinationPtr).CheckError();
            textureData.AsSpan().CopyTo(destination: new Span<byte>(pointer: destinationPtr, length: (int)totalBytes)); // internal buffer.memmove
        }
        srcResource.Unmap(0);

        PlacedSubresourceFootPrint toto = new PlacedSubresourceFootPrint()
        { Footprint = new SubresourceFootPrint() { Width = 8, Height = 1, RowPitch = 256, Format = Format.R8G8B8A8_UNorm, Depth = 1 } };

        var v1 = new ResourceBarrier(new ResourceTransitionBarrier(dstResource, ResourceStates.CopyDest, ResourceStates.PixelShaderResource));

        commandAllocators[0].Reset(); // re-use command allocator
        CL12_4.Reset(commandAllocators[0]); // re-use command list + set command allocator
        CL12_4.CopyTextureRegion(new TextureCopyLocation(dstResource), 0, 0, 0, new TextureCopyLocation(srcResource, toto));
        CL12_4.ResourceBarrier(v1);
        CL12_4.Close();
        Q12.ExecuteCommandList(CL12_4);

        WaitForGpu(); // garante que a GPU esta livre antes dos Disposals.
        srcResource.Dispose();

        return dstResource;
    }

    static void DeviceCreator(FeatureLevel level)
    {
        var adapter = GetHardwareAdapter4();
        if (D3D12.D3D12CreateDevice(adapter, level, out DEV12_8).Failure) throw new Exception(fodeu("device creation failure"));
        adapter.Dispose();
        if (DEV12_8 == null) throw new Exception(fodeu("device creation NULL"));
        DEV12_8.Name = "DEV12_8";

        // Apenas curiosidades ...
        //var v1 = DEV12_8.CheckFeatureSupport<FeatureDataD3D12Options1>();
        //var v2 = DEV12_8.CheckMaxSupportedFeatureLevel();

        //Aux2.DXGI_DebugLayer2("CreateDevice");
        //Aux2.DXGI_ClearMessages("CreateDevice");
        /*
         *  1 ID3D12Device (live)
         * 
         * INTERNALS
         * 
         *  1 ID3D12RootSignature (internal) referenciada por 1 dos PipelineStates (??)
         *  2 ID3D12PipelineState (internal)
         *  1 ID3D12Resource      (internal)
         *  1 ID3D12Heap          (internal)
         *  2 ID3D12Fence         (internal)
         *  1 ID3D12CommandQueue  (internal)
         */
    }
    static IDXGIAdapter4 GetHardwareAdapter4()
    {
        using var dxgiFactory7 = DXGI.CreateDXGIFactory2<IDXGIFactory7>(false);
        if (dxgiFactory7.EnumAdapterByGpuPreference(0, GpuPreference.HighPerformance, out IDXGIAdapter4 adapter4).Failure) throw new Exception(fodeu("Cannot detect any adapter"));
        if (adapter4 == null) throw new Exception(fodeu("adapter creation NULL"));
        if ((adapter4.Description1.Flags & AdapterFlags.Software) != AdapterFlags.None) throw new Exception(fodeu("Cannot detect hardware accelerated adapter"));
        return adapter4;
    }
    static string fodeu(string msg1) => new string('*', 30) + "\r\n" + msg1 + "\r\n" + new string('*', 30);
    static void CreateFence()
    {
        // Create synchronization objects.
        //fence1 = DEV12_8.CreateFence();
        //fence1.Name = "MAMA Fence";
        //event1 = new AutoResetEvent(false);
        //if (fence1.SetEventOnCompletion(0, event1).Failure) throw new Exception();


        // Create synchronization objects.
        CPUsynchronizer = new AutoResetEvent(false);
        CPUcompleted = new ulong[SWAP12_4.Description.BufferCount];

        GPU = DEV12_8.CreateFence();
        CPUcompleted[0] = 1;

        //GPU = Device.CreateFence(CPUcompleted[0]);
        GPU.Name = "Frame Fence";

    }
    static void CreateSWAP12(IntPtr hWnd, int qtdBackBuffers)
    {
        //Aux2.DXGI_DebugLayer2("Swapchain: antes");

        Q12 = DEV12_8.CreateCommandQueue(CommandListType.Direct); // Create Command queue (direct with no flag)
        Q12.Name = "Q12";

        var q3 = DEV12_8.CreateCommandQueue(CommandListType.Compute);

        //Aux2.DXGI_DebugLayer2("Command Queue");
        /*
         * 1 ID3D12CommandQueue (live)
         * 
         * INTERNALS
         * 
         * 1 ID3D12Fence (internal)
         */

        var dxgiFactory7 = DXGI.CreateDXGIFactory2<IDXGIFactory7>(false);
        var tempSwapChain = dxgiFactory7.CreateSwapChainForHwnd(Q12, hWnd, CreateSwapDesc(qtdBackBuffers));
        tempSwapChain.DebugName = "temporary swapchain";
        dxgiFactory7.Dispose();
        SWAP12_4 = tempSwapChain.QueryInterface<IDXGISwapChain4>();
        SWAP12_4.DebugName = "SWAP12_4";
        SWAP12_4.BackgroundColor = backgroudColor;
        // Curiosidade: Os back buffers (swap chain) podem ser menores ou maiores que o front buffer (client area).
        // Se for menor, a area que sobra recebe a "BackgroundColor".
        // Durante um resize da window, quando a client area ficar maior que os back buffers,
        // essa area maior recebe a "BackgroundColor". OBS: SwapChain com Scaling.None,

        Camera1.viewport = new Viewport(SWAP12_4.SourceSize.Width, SWAP12_4.SourceSize.Height);

        //Aux2.DXGI_DebugLayer2("Swapchain");
        //Aux2.DXGI_ClearMessages("Swapchain");
        /*
         * LIVES
         * 
         * 1x ID3D12LifetimeTracker     (live) 
         * 2x ID3D12Fence               (live)
         * 2x ID3D12CommandAllocator    (live)
         * 1x ID3D12GraphicsCommandList (live)
         * 
         * INTERNALS
         * 
         * 1x ID3D12GraphicsCommandList (internal)
         * 
         * 4 back buffers
         * 
         * ID3D12Resource (internal)          
         * ID3D12Heap (internal)              
         * ID3D12Resource (internal)          
         * ID3D12Heap (internal)              
         * ID3D12Resource (internal)          
         * ID3D12Heap (internal)             
         * ID3D12Resource (internal)          
         * ID3D12Heap (internal)                    
         */
    }
    static SwapChainDescription1 CreateSwapDesc(int qtdBackBuffers) => new()
     {
         //Height = 500, // default = 0 ... full client area
        //Width = 1200, // default = 0 ... full client area
        //Stereo = false, // default = false
        Format = rtvFormat, // default = Unknown
        BufferCount = qtdBackBuffers, // mandatory, minimum = 2
        BufferUsage = Usage.RenderTargetOutput, // default = 0
        SampleDescription = SampleDescription.Default, // default = {0,0}
        Scaling = Scaling.None, // AspectRatioStretch // default = Stretch
        SwapEffect = SwapEffect.FlipDiscard, // default = Discard
        AlphaMode = AlphaMode.Ignore, // default = Unspecified
        Flags = SwapChainFlags.AllowTearing // default = none
    };
    static void CreateCommandList()
    {
        var qtdBuffers2 = SWAP12_4.Description1.BufferCount;

        commandAllocators = new ID3D12CommandAllocator[qtdBuffers2];
        for (int i = 0; i < qtdBuffers2; i++) 
        {
            commandAllocators[i] = DEV12_8.CreateCommandAllocator(CommandListType.Direct);
            commandAllocators[i].Name = $"Command Allocator NUM-{i}";
        }
        //Aux2.DXGI_DebugLayer2("Command Allocators");
        /*
         * 4 back buffers
         * ID3D12CommandAllocator (live)
         * ID3D12CommandAllocator (live)
         * ID3D12CommandAllocator (live)
         * ID3D12CommandAllocator (live)
         */

        // Create a command list for recording graphics commands.

        CL12_4 = DEV12_8.CreateCommandList<ID3D12GraphicsCommandList4>(CommandListType.Direct, commandAllocators[0]);
        CL12_4.Name = "Command List";
        CL12_4.Close();
        //Aux2.DXGI_DebugLayer2("Command List");
        //Aux2.DXGI_ClearMessages("Command List");
        /*
         * ID3D12GraphicsCommandList (live)
         * ID3D12GraphicsCommandList (internal)
         */
    }
    static void CreateDSV12()
    {
        var desc1 = ResourceDescription.Texture2D(dsvFormat, (uint)SWAP12_4.SourceSize.Width, (uint)SWAP12_4.SourceSize.Height, 1, 1);
        desc1.Flags |= ResourceFlags.AllowDepthStencil;
        DSV12 = DEV12_8.CreateCommittedResource(new HeapProperties(HeapType.Default), HeapFlags.None, desc1, ResourceStates.DepthWrite, new(dsvFormat, 1f, 0));
        DSV12.Name = "DSV12";
        //Aux2.DXGI_DebugLayer2("DSV resource");
        /*
         * ID3D12Resource (live)
         * ID3D12Heap (internal)
         */

        var dsv12Desc = new DepthStencilViewDescription()
        {
            Format = dsvFormat,
            ViewDimension = DepthStencilViewDimension.Texture2D
        };
        dsvDescriptorHeap = DEV12_8.CreateDescriptorHeap(new DescriptorHeapDescription(DescriptorHeapType.DepthStencilView, 1));
        dsvDescriptorHeap.Name = "Descriptor Heap for DSV";
        dsv12Handle = dsvDescriptorHeap.GetCPUDescriptorHandleForHeapStart();
        DEV12_8.CreateDepthStencilView(DSV12, dsv12Desc, dsv12Handle);
        //Aux2.DXGI_DebugLayer2("Depth-Stencil View");
        //Aux2.DXGI_ClearMessages("Depth-Stencil View");
        /*
         * ID3D12DescriptorHeap (live)
         */
    }
    static void CreateRTV12()
    {
        var qtdBuffers = SWAP12_4.Description1.BufferCount;
        RTV12 = new ID3D12Resource[qtdBuffers];
        rtvHandles = new CpuDescriptorHandle[qtdBuffers];

        var desc1 = new DescriptorHeapDescription(type: DescriptorHeapType.RenderTargetView, descriptorCount: qtdBuffers);
        rtvDescriptorHeap = DEV12_8.CreateDescriptorHeap(desc1);
        rtvDescriptorHeap.Name = "Descriptor Heap for RTV";

        var rtvDescriptorSize = DEV12_8.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        for (int i = 0; i < qtdBuffers; i++) // Create a RTV for each back buffer.
        {
            RTV12[i] = SWAP12_4.GetBuffer<ID3D12Resource>(i);
            RTV12[i].Name = $"RTV NUM-{i}";
            rtvHandles[i] = new CpuDescriptorHandle(rtvDescriptorHeap.GetCPUDescriptorHandleForHeapStart(), i, rtvDescriptorSize);
            DEV12_8.CreateRenderTargetView(RTV12[i], null, rtvHandles[i]);
        }
        //Aux2.DXGI_DebugLayer2("Render Target View");
        //Aux2.DXGI_ClearMessages("Render Target View");
        /*
         * ID3D12DescriptorHeap (live)
         */
    }
    static void ReCreateRTV12()
    {
        var qtdBuffers = SWAP12_4.Description1.BufferCount;
        var rtvDescriptorSize = DEV12_8.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        for (int i = 0; i < qtdBuffers; i++)
        {
            RTV12[i] = SWAP12_4.GetBuffer<ID3D12Resource>(i);
            RTV12[i].Name = $"MAMA RTV buffer NUM-{i}";
            rtvHandles[i] = new CpuDescriptorHandle(rtvDescriptorHeap.GetCPUDescriptorHandleForHeapStart(), i, rtvDescriptorSize);
            DEV12_8.CreateRenderTargetView(RTV12[i], null, rtvHandles[i]);
        }
    }
    static void ReCreateDSV12()
    {
        var desc1 = DSV12.Description;  // Get current DSV description before dispose it
        DSV12.Dispose();

        desc1.Width = (ulong)SWAP12_4.SourceSize.Width;
        desc1.Height = SWAP12_4.SourceSize.Height;
        DSV12 = DEV12_8.CreateCommittedResource(new HeapProperties(HeapType.Default), HeapFlags.None, desc1, ResourceStates.DepthWrite, new(dsvFormat, 1.0f, 0));
        DSV12.Name = "DSV12";

        DepthStencilViewDescription dsViewDesc = new()
        {
            Format = dsvFormat,
            ViewDimension = DepthStencilViewDimension.Texture2D
        };

        DEV12_8.CreateDepthStencilView(DSV12, dsViewDesc, dsvDescriptorHeap.GetCPUDescriptorHandleForHeapStart());
    }
    static void DisposeRTV12()
    {
        // IDXGISwapChain::ResizeBuffers: Swapchain cannot be resized unless all outstanding buffer references have been released.
        var qtdBuffers = SWAP12_4.Description1.BufferCount;
        for (var i = 0; i < qtdBuffers; i++)
            RTV12[i].Dispose();

        //Aux2.DXGI_DebugLayer3("DisposeRTV12", false);
    }
    static void ResizeSWAP12()
    {
        // IDXGISwapChain::ResizeBuffers: Swapchain cannot be resized unless all outstanding buffer references have been released.
        if (SWAP12_4.ResizeBuffers(0, 0, 0, Format.Unknown, SWAP12_4.Description1.Flags).Failure) throw new Exception();
        //Aux2.DXGI_DebugLayer3("aqui", false);
        Camera1.viewport = new Viewport(SWAP12_4.SourceSize.Width, SWAP12_4.SourceSize.Height);

        // Reset the synchronism elements
        Array.Clear(CPUcompleted);
        CPUcompleted[0] = 1;
        _backBufferIndex = 0;
    }

    static void SetupNewFrameRender()
    {
        commandAllocators[_backBufferIndex].Reset();
        CL12_4.Reset(commandAllocators[_backBufferIndex]);
        CL12_4.ResourceBarrierTransition(RTV12[_backBufferIndex], ResourceStates.Present, ResourceStates.RenderTarget);
        CL12_4.OMSetRenderTargets(rtvHandles[_backBufferIndex], dsv12Handle);
        CL12_4.RSSetViewport(Camera1.viewport);
        CL12_4.RSSetScissorRect((int)Camera1.viewport.Width, (int)Camera1.viewport.Height);
        CL12_4.ClearRenderTargetView(rtvHandles[_backBufferIndex], backgroudColor);
        CL12_4.ClearDepthStencilView(dsv12Handle, ClearFlags.Depth, 1f, 0);
        // GOTO current model DrawAll ( user render part )
    }
    static void PresentFrameRender()
    {
        // Indicate that the back buffer will now be used to present.
        CL12_4.ResourceBarrierTransition(RTV12[_backBufferIndex], ResourceStates.RenderTarget, ResourceStates.Present);
        CL12_4.Close();

        //Aux2.DXGI_DebugLayer3("p11", true);
        Q12.ExecuteCommandList(CL12_4); // start GPU execution
        //Aux2.DXGI_DebugLayer3("p12", true);

        if(SWAP12_4.Present(0, PresentFlags.AllowTearing).Failure) throw new Exception(); // CurrentBackBufferIndex é atualizado AQUI

        MoveToNextFrame();
    }
    static void EnableD3D12DebugLayer()
    {
        // A interface ID3D12Debug serve para usar o metodo EnableDebugLayer()
        // A interface ID3D12Debug3 serve para usar o metodo SetEnableGPUBasedValidation() ... que eu ainda não sei usar.
        // Obs1: A interface IDXGIDebug NÃO serve para usar o metodo EnableDebugLayer().
        // Obs2: É necessario ativar o "Native Code Debug" para acionar o "mixed-code debugging"
        // se houver necessidade das mensagens do DebugLayer aparecem "automaticamente" na Output Debug window.
        //
        // A interface IDXGIDebug serve para usar o metodo ReportLiveObjects(..).
        // A interface IDXGIInfoQueue serve para usar os metodos para obter as Mensagens.

        /* 
        * To get messages on the Output (Debug) window enable mixed-code debugging (nativeDebugging).
        * By default only the managed code debugging messages goes to Output window.
        * To get both (managed and unmanaged) use "Enable native code debugging".
        * This option is in the Debug Properties of the startup Project.
        */

        if (D3D12.D3D12GetDebugInterface(out ID3D12Debug3 debug3).Failure) throw new Exception();
        debug3.EnableDebugLayer();
        debug3.SetEnableGPUBasedValidation(false); // No render when turned ON.
        debug3.Dispose();
    }

    public static void WaitForGpu()
    {
        var f1 = DEV12_8.CreateFence(); // Melhorar um pouco aqui REUTILIZANDO uma unica fence ...
        f1.Name = "TMP fence";
        Q12.Signal(f1, 1234); // Q12 só vai conseguir colocar esse 0 na Fence quando terminar essa tarefa
        if (f1.CompletedValue != 1234)
        {
            Debug.WriteLine("hold ...");
            f1.SetEventOnCompletion(1234, CPUsynchronizer);
            CPUsynchronizer.WaitOne();
        }
        f1.Dispose();
    }

    static void MoveToNextFrame()
    {
        // MAMA: If CPU is ready for next frame but GPU did not finished previous one, hold CPU until GPU finish.
        // If the next frame is not ready to be rendered yet, wait until it is ready.

        // Schedule a Signal command in the queue.
        ulong currentFenceValue = CPUcompleted[_backBufferIndex];
        Q12.Signal(GPU, currentFenceValue);

        // Update the back buffer index.
        _backBufferIndex = SWAP12_4.CurrentBackBufferIndex;

        if (GPU.CompletedValue < CPUcompleted[_backBufferIndex])
        {
            AuxFunctions.waitCount++;

            // inform GPU where CPU is and ask GPU to signal when it is there.
            GPU.SetEventOnCompletion(CPUcompleted[_backBufferIndex], CPUsynchronizer).CheckError();
            // hold CPU until GPU did not signal it is there.
            CPUsynchronizer.WaitOne();
        }
        // Set the fence value for the next frame.
        CPUcompleted[_backBufferIndex] = currentFenceValue + 1;
    }
}

// A rootSignature é definida no <base.Shader> mas os dados são carregados no

// Constant buffer explanation
/*******************************************************

CONSTANT BUFFER

The idea is to send data to Vertex Shader register type B using a buffer (a constant buffer). 

Despite the data itself be highly dynamic, the structure that holds the data must be constant (Matrix4x4 or any struct).
The structure to hold the data must be large enough and must be 256-bytes aligned.

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

public class D3D12_Signatures
{
    public static RootSignatureFlags rootSignatureFlags = RootSignatureFlags.AllowInputAssemblerInputLayout;

    public static ID3D12RootSignature GetRootSignature(int i)
    {
        switch (i)
        {
            case 0:
                var r0 = DEV12_8.CreateRootSignature(new RootSignatureDescription1(flags: rootSignatureFlags));
                r0.Name = "mama_root 0";
                return r0;

            case 1:
                // registerSpace aparece no HLSL como: register(b3, space4) ... se for 0 pode ser simplesmente register(b3)
                // o size desse tipo de constante é informado em multiplos de 4 bytes (32bits) para garantir o alinhamento que esse tipo de constante pede.
                var rootConstant_A1 = new RootConstants(shaderRegister: Slots.MVP_registerB, registerSpace: 0, num32BitValues: Unsafe.SizeOf<Matrix4x4>() / 4);
                var rootParameter_A1 = new RootParameter1(rootConstants: rootConstant_A1, visibility: ShaderVisibility.Vertex);

                var rootParameters1 = new RootParameter1[] { rootParameter_A1 };
                var r1 = DEV12_8.CreateRootSignature(new RootSignatureDescription1(flags: rootSignatureFlags, parameters: rootParameters1));
                return r1;

            case 2:
                var rootConstant2_A2 = new RootConstants(shaderRegister: Slots.MVP_registerB, registerSpace: 0, num32BitValues: Unsafe.SizeOf<Matrix4x4>() / 4);
                var rootParameter_A2 = new RootParameter1(rootConstants: rootConstant2_A2, visibility: ShaderVisibility.Vertex);

                // Obs: 2D texture SRV cannot be used as a root descriptor.
                var range1 = new DescriptorRange1(rangeType: DescriptorRangeType.ShaderResourceView, numDescriptors: 1, baseShaderRegister: Slots.Texture_registerT);
                var table1 = new RootDescriptorTable1(new DescriptorRange1[] { range1 });
                var rootParameter_B2 = new RootParameter1(descriptorTable: table1, visibility: ShaderVisibility.Pixel);

                var rootParameters2 = new RootParameter1[] { rootParameter_A2, rootParameter_B2 };

                // (1) Samplers are not allowed in the same descriptor table as CBVs, UAVs and SRVs.
                // (2) Static samplers: bound without requiring root signature slots.
                var sampler1 = new StaticSamplerDescription()
                {
                    AddressU = TextureAddressMode.Border,
                    AddressV = TextureAddressMode.Border,
                    AddressW = TextureAddressMode.Border,
                    ComparisonFunction = ComparisonFunction.Never,
                    MaxLOD = D3D12.Float32Max,
                    ShaderRegister = Slots.Sampler_registerS
                };
                var samplers = new StaticSamplerDescription[] { sampler1 };
                var r2 = DEV12_8.CreateRootSignature(new RootSignatureDescription1(flags: rootSignatureFlags, parameters: rootParameters2, samplers: samplers));
                r2.Name = "mama_root 2";
                return r2;

            default : throw new Exception();
        }
    }
}

// Based on D3D12_BaseShader_NoMVP
// RootSignature contem 1 parameter tipo RootConstant ocupando 16 slots dos 64 slots disponiveis.
// Essa constant é type Matrix4x4 e contem o MVP.
public class D3D12_ShaderCompiler // with MVP via simple root constant
{
    public ReadOnlyMemory<byte> VS12;
    public ReadOnlyMemory<byte> PS12;

    (string vertexShader, string pixelShader) entrypoints = ("VSMain", "PSMain"); // default
    public virtual (string vertexShader, string pixelShader) Entrypoints { get => entrypoints; } // use default is upstream do not implemented

    public D3D12_ShaderCompiler(string sh)
    {
        VS12 = CompileBytecode(sh, DxcShaderStage.Vertex, Entrypoints.vertexShader);
        PS12 = CompileBytecode(sh, DxcShaderStage.Pixel, Entrypoints.pixelShader);
    }

    ReadOnlyMemory<byte> CompileBytecode(string shaderSource, DxcShaderStage stage, string entryPoint)
    {
        var options = new DxcCompilerOptions() { ShaderModel = DxcShaderModel.Model6_4 };
        using IDxcResult results = DxcCompiler.Compile(stage, shaderSource, entryPoint, options);
        if (results.GetStatus().Failure) throw new Exception(results.GetErrors());
        return results.GetObjectBytecodeMemory();
    }
}

public static class D3D12_InputDesc
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
            slot: MyShaderSources.vertexSlot); // same slot as "entryA"

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
            slot: MyShaderSources.vertexSlot + 1); // next slot after "entryA"

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

public static class D3D12_AuxFunctions
{
    public static PrimitiveTopologyType PrimitiveTopologyD3D12(PrimitiveTopology primitiveTopology)
    {
        return primitiveTopology switch
        {
            PrimitiveTopology.TriangleList => PrimitiveTopologyType.Triangle,
            PrimitiveTopology.LineList => PrimitiveTopologyType.Line,
            _ => throw new Exception("not implement yet"),
        };
    }
}
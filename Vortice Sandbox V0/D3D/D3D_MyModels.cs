using System.Numerics;

#if D3D12
using D3D12_MyDrawSets;
#else
using D3D11_MyDrawSets;
#endif

using D3D_Mama;
using static D3D_Mama.MyDataSets;

#nullable disable

namespace D3D_MyModels;

// Os modelos abaixo, introduzem alguns conceitos basicos desse sandbox

// Esse sandbox contem um method Render() que corresponde a parte central da renderização. Call Render() renderiza 1 frame.
// Render() é impleentado na class D3D11_Mama ou D3D12_Mama e contem 3 calls:
// SetupNewFrameRender(..) + event UserRender + PresentFrameRender(..)
// O event UserRender() é implementado aqui, nas classes MyModels, por DrawAll().
// A ligação é feita no Form1: UserRender += model.DrawAll;
//
// Model 100% vazio. Neste caso, com DrawAll() vazio, o Render() é feito apenas pelos 2 calls internos implementados no Render() do sandbox.
// Tirando os setups do D3D11 ou D3D12, um render vazio basicamente é colocar uma cor no back buffer e fazer o swap para presentation.
// Na minha GPU (Radeon 550) esse tempo é da ordem de 150 µs (microseconds) per frame, full screen size.

// SandBox introduction: level 00 - no data, just clean screen and present.
public class MyModel_Nothing
{ 
    public void DrawAll() { /* nothing */ }

    public void Dispose() { /* nothing */ }

}

// L0 SandBox introduction: NON-INDEXED (1 drawset)
public class MyModel_L0A
{
    DrawSetBase_NoMulti_NoInst_NoTex_NoMVP_NoIndex drawSet1;

    public MyModel_L0A()
    {
        //drawSet1 = new(ShaderSources.float2_NoMVS1); // MyModel_L0A
        //drawSet1 = new(ShaderSources.float2_NoMVS2); // MyModel_L0A, idem previous + structs introduction
        drawSet1 = new(MyShaderSources.float2_NoMVS3); // MyModel_L0A, idem previous + color introduction
        drawSet1.SetVertexData(Line1_direct(-0.95f)); // MyModel_L0A
    }
    public void DrawAll() // MyModel_L0A
    {
        // Comming from SetupNewFrameRender() on D3D12_Base.Render()
        drawSet1.Draw(); // MyModel_L0A
        // Go to PresentFrameRender() on D3D12_Base.Render()
    }

    public void Dispose()
    {
        drawSet1.Dispose();

        //Aux2.DXGI_DebugLayer1("Model Disposal");
    }
}

// L0 SandBox introduction: NON-INDEXED (2 drawsets)
public class MyModel_L0B
{
    DrawSetBase_NoMulti_NoInst_NoTex_NoMVP_NoIndex drawSet1; // MyModel_L0B
    DrawSetBase_NoMulti_NoInst_NoTex_NoMVP_NoIndex drawSet2; // MyModel_L0B

    public MyModel_L0B()
    {
        drawSet1 = new(MyShaderSources.float2_NoMVS3); // MyModel_L0B
        drawSet1.SetVertexData(Line1_direct(-0.95f)); // MyModel_L0B

        drawSet2 = new(MyShaderSources.float2_NoMVS3); // MyModel_L0B
        drawSet2.SetVertexData(Line1_direct(-0.75f)); // MyModel_L0B

    }
    public void DrawAll() // MyModel_L0B
    {
        drawSet1.Draw(); // MyModel_L0B
        drawSet2.Draw(); // MyModel_L0B
    }

    public void Dispose()
    {
        drawSet1.Dispose();
        drawSet2.Dispose();
    }

}

// L1 SandBox introduction: INDEXED
public class MyModel_L1
{
    DrawSetBase_NoMulti_NoInst_NoTex_NoMVP drawSet1; // MyModel_L1

    public MyModel_L1()
    {
        drawSet1 = new(MyShaderSources.float2_NoMVS3); // MyModel_L1
        drawSet1.SetVertexData(Line1(-0.95f)); // MyModel_L1
    }
    public void DrawAll() // MyModel_L1
    {
        drawSet1.Draw(); // MyModel_L1
    }
    public void Dispose()
    {
        drawSet1.Dispose();
    }

}

// L2 SandBox introduction: MVP
public class MyModel_L2
{
    DrawSetBase_NoMulti_NoInst_NoTex drawSet1; // MyModel_L2

    public MyModel_L2()
    {
        Camera1.SetupInitial(new() { cam_position = 2.5f * Vector3.UnitZ }); // MyModel_L2 : MVP needs Camera SetupInitial
        drawSet1 = new(MyShaderSources.float2); // MyModel_L2
        drawSet1.SetVertexData(Line1(-0.95f)); // MyModel_L2
    }
    public void DrawAll() // MyModel_L2
    {
        drawSet1.Draw(); // MyModel_L2
    }
    public void Dispose()
    {
        drawSet1.Dispose();
    }
}

// L3 SandBox introduction: TEXTURE
public class MyModel_L3
{
    DrawSetBase_NoMulti_NoInst1 d_1buffer; // MyModel_L3 : vertex + UV coords mixed in a single buffer
    DrawSetBase_NoMulti_NoInst2 d_2buffers; // MyModel_L3 : vertex coords in 1 buffer, UV coords in another buffer

    public MyModel_L3()
    {
        Camera1.SetupInitial(new() { cam_position = new Vector3(1, 0.5f, 3), cam_target = new Vector3(1, 0.5f, 0) }); // MyModel_L3

        d_1buffer = new(MyShaderSources.float3B); // MyModel_L3
        d_1buffer.SetVertexData(Triang1(0.5f)); // MyModel_L3     
        d_1buffer.Texture99(AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
        //d_1buffer.Texture99(AuxFunctions.BitmapFileToByteArray("Textures/10points.png"));


        d_2buffers = new(MyShaderSources.float3B); // MyModel_L3
        d_2buffers.SetVertexData(Triang2(0)); // MyModel_L3
        d_2buffers.Texture99(AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
    }
    public void DrawAll() // MyModel_L3
    {
        d_1buffer.Draw(); // MyModel_L3
        d_2buffers.Draw(); // MyModel_L3
    }

    public void Dispose()
    {
        d_1buffer.Dispose();
        d_2buffers.Dispose();
    }
}

// L4 SandBox introduction: INSTANCES
public class MyModel_L4
{
    DrawSetBase_NoMulti1 d_2buffers; // MyModel_L5
    DrawSetBase_NoMulti2 d_3buffers; // MyModel_L5

    public MyModel_L4()
    {
        Camera1.SetupInitial(new() { cam_position = new Vector3(1, 2, 8), cam_target = new Vector3(1, 2, 1.05f) }); // MyModel_L5

        d_2buffers = new(MyShaderSources.float3_Instance); // MyModel_L5
        d_2buffers.SetVertexData(Triang1(0.3f), Instance1()); // MyModel_L5
        d_2buffers.Texture99(AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
      
        d_3buffers = new(MyShaderSources.float3_Instance); // MyModel_L5
        d_3buffers.SetVertexData(Triang2(1.2f), Instance1()); // MyModel_L5
        d_3buffers.Texture99(AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));        
    }
    public void DrawAll() // MyModel_L5
    {
        d_2buffers.Draw(); // MyModel_L5
        d_3buffers.Draw(); // MyModel_L5
    }
    public void Dispose()
    {
        d_2buffers.Dispose();
        d_3buffers.Dispose();
    }
}

// L5 SandBox introduction: MULTIDATA (no instanced)
public class MyModel_L5A
{
    DrawSetBase1 d_1buffer; // MyModel_L4
    DrawSetBase2 d_2buffer; // MyModel_L4
    public MyModel_L5A()
    {
        Camera1.SetupInitial(new() { cam_position = new Vector3(1, 0.75f, 4), cam_target = new Vector3(1, 0.75f, 1.05f) }); // MyModel_L4

        d_1buffer = new(3, MyShaderSources.float3B); // MyModel_L4
        d_1buffer.SetVertexData(0, Triang1(0.3f)); // MyModel_L4
        d_1buffer.Texture99(0, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
        d_1buffer.SetVertexData(1, Triang1(0.6f)); // MyModel_L4
        d_1buffer.Texture99(1, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
        d_1buffer.SetVertexData(2, Triang1(0.9f)); // MyModel_L4
        d_1buffer.Texture99(2, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));

        d_2buffer = new(3, MyShaderSources.float3B); // MyModel_L4
        d_2buffer.SetVertexData(0, Triang2(1.2f)); // MyModel_L4
        d_2buffer.Texture99(0, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
        d_2buffer.SetVertexData(1, Triang2(1.5f)); // MyModel_L4
        d_2buffer.Texture99(1, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
        d_2buffer.SetVertexData(2, Triang2(1.8f)); // MyModel_L4
        d_2buffer.Texture99(2, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
    }
    public void DrawAll() // MyModel_L4
    {
        d_1buffer.Draw(); // MyModel_L4
        d_2buffer.Draw(); // MyModel_L4
    }
    public void Dispose()
    {
        d_1buffer.Dispose();
        d_2buffer.Dispose();
    }
}

// L5 SandBox introduction: MULTIDATA (instanced)
public class MyModel_L5B
{
    DrawSetBaseInstanced1 d_2buffers; // MyModel_L5
    DrawSetBaseInstanced2 d_3buffers; // MyModel_L5

    public MyModel_L5B()
    {
        Camera1.SetupInitial(new() { cam_position = new Vector3(1, 2, 8), cam_target = new Vector3(1, 2, 1.05f) }); // MyModel_L5
  
        d_2buffers = new(3, MyShaderSources.float3_Instance); // MyModel_L5
        d_2buffers.SetVertexData(0, Triang1(0.3f), Instance1()); // MyModel_L5
        d_2buffers.Texture99(0, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
        d_2buffers.SetVertexData(1, Triang1(0.6f), Instance1()); // MyModel_L5
        d_2buffers.Texture99(1, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
        d_2buffers.SetVertexData(2, Triang1(0.9f), Instance1()); // MyModel_L5
        d_2buffers.Texture99(2, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));

        d_2buffers.SetRenderMode(0, RenderMode.Solid);
        d_2buffers.SetRenderMode(1, RenderMode.Solid);
        d_2buffers.SetRenderMode(2, RenderMode.Solid);
        
        d_3buffers = new(3, MyShaderSources.float3_Instance); // MyModel_L5
        d_3buffers.SetVertexData(0, Triang2(1.2f), Instance1()); // MyModel_L5
        d_3buffers.Texture99(0, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
        d_3buffers.SetVertexData(1, Triang2(1.5f), Instance1()); // MyModel_L5
        d_3buffers.Texture99(1, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));
        d_3buffers.SetVertexData(2, Triang2(1.8f), Instance1()); // MyModel_L5
        d_3buffers.Texture99(2, AuxFunctions.BitmapHardCodedToByteArray(tipo: 2));

        d_3buffers.SetRenderMode(0, RenderMode.Wireframe);
        d_3buffers.SetRenderMode(1, RenderMode.Solid);
        d_3buffers.SetRenderMode(2, RenderMode.Solid);
    }
    public void DrawAll() // MyModel_L5
    {
        d_2buffers.Draw(); // MyModel_L5
        d_3buffers.Draw(); // MyModel_L5
    }
    public void Dispose()
    {
        d_2buffers.Dispose();
        d_3buffers.Dispose();
    }
}

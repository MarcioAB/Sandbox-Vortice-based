using System.Diagnostics;
using System.Numerics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using Color = System.Drawing.Color;

using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.DXGI.Debug;
using static Vortice.DXGI.DXGI;

#if D3D12
using Vortice.Direct3D12;
#else
using Vortice.Direct3D11;
#endif

#nullable disable

namespace D3D_Mama;

public static class Slots // aligned with HLSL
{
    public const int MVP_registerB = 0; // register(b0)
    public const int Texture_registerT = 0; // register(t1)
    public const int Sampler_registerS = 0; // register(s2)
    public const int CB_1 = 4;
    public const int CB_2 = 5;
}

public struct MovingPoint
{
    // esta struct serve apenas para armazenar algumas informações e é usada pelo target da Camera e pelo light do DSP (drawset moving point)

    private Vector3 position;
    private Color cor;
    public float movePointStep;
    public float pointSize;

    public event Action PositionChanged;
    public event Action ColorChanged;

    public Vector3 Position
    {
        get => position;
        set
        {
            position = value;
            PositionChanged?.Invoke();
        }
    }

    public System.Drawing.Color Cor
    {
        get => cor;
        set
        {
            cor = value;
            ColorChanged?.Invoke();
        }
    }

    /// <summary>
    /// cor = White<br></br>
    /// posição = 0,0,0<br></br>
    /// move step = 1<br></br>
    /// point size = 5<br></br>
    /// </summary>
    public MovingPoint()
    {
        Position = Vector3.Zero; // default
        Cor = System.Drawing.Color.White; // default
        movePointStep = 1f; // default
        pointSize = 5f; // default
    }
}

public enum RenderMode { Solid, Wireframe, Solid_RenderBackOnly, Solid_RenderFrontOnly }

/* 
 * To get messages on the Output (Debug) window enable mixed-code debugging (nativeDebugging).
 * By default only the managed code debugging messages goes to Output window.
 * To get both (managed and unmanaged) use "Enable native code debugging".
 * This option is in the Debug Properties of the startup Project.
 */

public enum DebugLayerTypes { Messages, SummaryInternal, DetailInternal, Summary, Detail, All, ValidateContext }

public class Camera1
{
    public static Viewport viewport;

    public static event Action PositionChanged;

    public static Matrix4x4 _projectionMatrix; // = Matrix4x4.Identity;
    public static Matrix4x4 _viewMatrix; // = Matrix4x4.Identity;
    public static Matrix4x4 ViewProjection;
    public static Matrix4x4 MVP;

    public static Vector3 _up = Vector3.UnitY; // camera começa na vertical; 
    public static Vector3 _right;
    public static Vector3 position;
    public static MovingPoint target1;

    public static float _fov = 45 * OneRadianINV; // The field of view of the camera (radians)

    public static float near = 0.01f;
    public static float far = 100000f;

    public static bool localRotation;
    public static float positionStep = 10; // user adjstable
    public static float rotationStep = OneRadianINV; // 1 grau decimal ... convertido para radianos ;

    public const float OneRadian = (float)(180d / Math.PI);

    public const float OneRadianINV = (float)(Math.PI / 180d);

    /// <summary>
    /// top
    /// </summary>
    public struct InitialParms
    {
        public Vector3 cam_target;
        public Vector3 cam_position;
        public float targetStep;
        public float positionStep;
        public float rotationStep;

        /// <summary>
        /// Camera target: origem<br></br>
        /// Camera position: Z = 1
        /// </summary>
        public InitialParms()
        {
            cam_target = Vector3.Zero;
            cam_position = Vector3.UnitZ;
        }
    }

    public static InitialParms inicial;

    public static void SetupInitial(InitialParms p)
    {
        target1 = new() { Position = p.cam_target };
        Position = p.cam_position;
        CheckCameraAxis(); // set dos eixos da camera: Longitudinal, Right, Up
        _projectionMatrix = GetProjectionMatrix(); // obs1: projMatrix ainda esta zerada aqui. obs2: viewport foi updated pelo Setup() do D3D_Viewer ( D3D11 ou D3D12 )
        UpdateView();
    }

    public static Viewport CreateViewport(IDXGISwapChain4 SWAP4) => new Viewport(SWAP4.SourceSize.Width, SWAP4.SourceSize.Height); // vide comment about Viewport Z direction

    // Apenas se mudar a posição da camera
    public static void UpdateView()
    {
        _viewMatrix = GetViewMatrix();
        ViewProjection = _viewMatrix * _projectionMatrix;
    }

    // Apenas se a camera mudar de posição
    public static Matrix4x4 GetViewMatrix() => Matrix4x4.CreateLookAt(Position, target1.Position, _up);

    /// <summary>
    /// ProjectionMatrix muda apenas se aspect ou fovy mudar<br></br>
    /// obs: near and far estão fixos.<br></br>
    /// </summary>
    //private Matrix4x4 GetProjectionMatrix() => Matrix4x4.CreatePerspectiveFieldOfView(_fov, _aspect, 0.01f, 100000f);
    public static Matrix4x4 GetProjectionMatrix() => Matrix4x4.CreatePerspectiveFieldOfView(_fov, viewport.AspectRatio, near, far);

    public static void SetProjectionMatrix()
    {
        _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(_fov, viewport.AspectRatio, near, far);
    }

    public static Vector3 Position
    {
        get => position;
        set
        {
            position = value;
            PositionChanged?.Invoke();
        }
    }

    // Apenas se mudar o tamanho da window.
    public static void UpdateProjection()
    {
        _projectionMatrix = GetProjectionMatrix();
        ViewProjection = _viewMatrix * _projectionMatrix;
    }

    /// <summary>
    /// Verifica se o eixo longitudinal da camera não esta muito proximo do eixo Y.<br></br>
    /// Se estiver usar o Eixo X para o plano<br></br>
    /// Rigth é o vetor normal ao plano que passa pelo plano { eixo longitudinal, eixo Y } (ou X se for o caso).<br></br>
    /// Up é o vetor normal ao plano que passa pelo plano { eixo longitudinal, Right }<br></br>
    /// </summary>
    public static void CheckCameraAxis() // Muda: eixoRight, eixoUp. Não muda: camera_position, target_position
    {
        //System.Diagnostics.Debug.WriteLine($"1: up {_up}    right {_right}");

        if (Position == target1.Position)
        {
            Position = target1.Position + Vector3.UnitX; // NA LOUCA !!!
        }

        var eixoLongitudinal = Vector3.Normalize(target1.Position - Position);

        // Just in case o eixoLongitudinal estiver muito perto de Y, usar X.
        var a2 = Math.Abs(Vector3.Dot(eixoLongitudinal, _up));
        if (a2 > 0.95f) _up = Vector3.UnitX; // eixo Y não serve para Up então vai ser o X mesmo.

        _right = Vector3.Normalize(Vector3.Cross(eixoLongitudinal, _up));

        // Isso não deve acontecer nunca, mas ... just in case.
        if (float.IsNaN(_right.X) || float.IsNaN(_right.Y) || float.IsNaN(_right.Z)) throw new Exception();

        _up = Vector3.Normalize(Vector3.Cross(_right, eixoLongitudinal));
    }

    // Dados: ponto, direção e rotação, a formula abaixo gera a nova posição do ponto rotacionado.
    public static void MoveRotation(Vector3 v)
    {
        var q1 = Quaternion.CreateFromAxisAngle(v, rotationStep);
        Position = Vector3.Transform(Position - target1.Position, q1) + target1.Position; // point rotation
        _up = Vector3.Transform(_up, q1); // vector rotation. Obs: Se o vector já é unitario, resultante tambem é (salvo pequeno erro residual)
        _right = Vector3.Normalize(Vector3.Cross(target1.Position - Position, _up));
    }

    // Dados: ponto, direção e distancia, a formula abaixo gera a nova posição do ponto deslocado.
    public static void MovePosition(int multi)
    {
        var v1 = target1.Position - Position;
        var newPosition = Position + Vector3.Normalize(v1) * (v1.Length() / positionStep) * multi;

        // Update Position only if the distance does not block view
        if ((target1.Position - newPosition).Length() >= 0.5f + near) Position = newPosition;
    }

}

public static class InputDesc
{
    enum VertexFormat { float1, float2, float3, float4, half2, half4, none }

    static Format Toto2(VertexFormat x)
    {
        return x switch
        {
            VertexFormat.float1 => Format.R32_Float,
            VertexFormat.float2 => Format.R32G32_Float,
            VertexFormat.float3 => Format.R32G32B32_Float,
            VertexFormat.float4 => Format.R32G32B32A32_Float,
            VertexFormat.half2 => Format.R16G16_Float,
            VertexFormat.half4 => Format.R16G16B16A16_Float,
            _ => throw new Exception(),
        };
    }

    static VertexFormat GetFormat<T>()
    {
        var x = typeof(T);
        var x2 = typeof(T).Name;
        if (x2.StartsWith("ValueTuple"))
        {
            var y = x.GenericTypeArguments;
            var y1 = y.Length;
            var y2 = y[0].Name;

            switch (y2)
            {
                case "Single":
                    switch (y1)
                    {
                        case 2: return VertexFormat.float2;
                        case 3: return VertexFormat.float3;
                        case 4: return VertexFormat.float4;
                        default: throw new Exception();
                    }
                default: throw new Exception();
            }
        }
        else if (x2.StartsWith("Vector"))
        {
            switch (x2)
            {
                case "Vector2": return VertexFormat.float2;
                case "Vector3": return VertexFormat.float3;
                case "Vector4": return VertexFormat.float4;
                default: throw new Exception();
            }
        }
        else if (x2 == "Single")
        {
            return VertexFormat.float1;
        }
        else
        {
            throw new Exception();
        }
    }


    public static Format GetFormatFromType<T>() => Toto2(GetFormat<T>());

    /*
    public static InputElementDescription[] GetSimplexInputElementsDesc(VertexFormat format, int offset) =>
    new[] { new InputElementDescription("I", 0, Toto2(format), offset, Slots.vertex) };

    public static InputElementDescription[] GetInputElementDesc3(VertexFormat format, int offset) => new[]
{
            new InputElementDescription("I", semanticIndex: 0, Toto2(format), offset: offset, Slots.vertex),
            new InputElementDescription("I", semanticIndex: 1, Toto2(format), offset: offset, Slots.vertex),
        };

    */

    // D3D12 based ... OR ... D3D11 based.
    public static InputElementDescription[] GetComplexInputElements(string name)
    {
        return name switch
        {
            "toto2" => new[] {
                new InputElementDescription("float3_MyCPU", semanticIndex: 0, Format.R32G32B32_Float, offset: 0, slot: MyShaderSources.vertexSlot),
                new InputElementDescription("float4_MyCPU", semanticIndex: 0, Format.R32G32B32A32_Float, offset: 0, slot: MyShaderSources.vertexSlot)},

            // struct { float3, float3, float2 }
            "VSinput" => new[] {
                new InputElementDescription("POSITION", semanticIndex: 0, Format.R32G32B32_Float, offset: 0, slot: MyShaderSources.vertexSlot),
                new InputElementDescription("NORMAL", semanticIndex: 0, Format.R32G32B32_Float, offset: 12, slot: MyShaderSources.vertexSlot),
                new InputElementDescription("TEXCOORD", semanticIndex: 0, Format.R32G32_Float, offset : 24, slot: MyShaderSources.vertexSlot)},

            _ => throw new Exception(),
        };
    }
}

public static class AuxFunctions
{
    public static int waitCount;

    /// <summary>A single-component, 32-bit floating-point format that supports 32 bits for depth</summary>
    public const Format dsvFormat = Format.D32_Float;

    /// <summary>32-bit BGRA pixel format for all basic cases here in the sandbox</summary>
    public const Format rtvFormat = Format.B8G8R8A8_UNorm; // by the way, that is the format of my bitmaps

    // SUPER IMPORTANTE: ReadOnlySpan é um "glorified pointer" ... e MemoryMarshal.Cast é importante nesse contexto.
    /// <summary>T must be a value type such as int, short, double, etc ...</summary>
    public static ReadOnlySpan<byte> FromValueTypesToByte<T>(T[] array1) where T : struct => MemoryMarshal.Cast<T, byte>(new ReadOnlySpan<T>(array1));

    /// <summary>Return bitmap as byte array</summary>
    public static (int width, int height, byte[] byteArray) BitmapFileToByteArray(string bmpFile)
    {
        if (!File.Exists(bmpFile)) throw new Exception();

        var bmp = new Bitmap(bmpFile);

        if(bmp.PixelFormat != PixelFormat.Format32bppArgb) throw new Exception("not implemented for this bitmap format");

        //bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
        //bmp.RotateFlip(RotateFlipType.Rotate90FlipY);
        //bmp.RotateFlip(RotateFlipType.RotateNoneFlipX); // funciona

        var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat); // PixelFormat.Format32bppArgb
        int qtdBytes = Math.Abs(bmpData.Stride) * bmp.Height;
        byte[] rgbValues = new byte[qtdBytes];
        Marshal.Copy(source: bmpData.Scan0, destination: rgbValues, startIndex: 0, length: qtdBytes);

        return (bmp.Width, bmp.Height, rgbValues);
    }
  
    /// <summary>Return bitmap as byte array</summary>
    public static (int width, int heigth, byte[] byteArray) BitmapHardCodedToByteArray(int tipo)
    {
        int[] pixels = null;
        var width = 0;

        switch (tipo)
        {
            case 1:
                pixels = new int[]
{
            Color.Red.ToArgb(),
            Color.Gray.ToArgb(),
            Color.LightGray.ToArgb(),
            Color.Black.ToArgb(),
            Color.White.ToArgb(),
            Color.Purple.ToArgb(),
            Color.Brown.ToArgb(),
            Color.CadetBlue.ToArgb()
};
                width = 8; // Uma vez o que bitmap é definido HARD-CODED aqui é uma linguiça, a largura do bitmap precisa ser informada. 
                break;

            case 2:
                pixels = new int[]
{
            Color.Red.ToArgb(),
            Color.Lime.ToArgb(),
            Color.Blue.ToArgb(),
            0x00FF0000, // red
            0x0000FF00, // green
            0x000000FF, // blue
            Color.Brown.ToArgb(),
            Color.CadetBlue.ToArgb(),
};
                width = 8;
                break;

            default:
                throw new Exception();
        }

        var (Quotient, Remainder) = Math.DivRem(pixels.Length, width);
        if (Remainder != 0) throw new Exception("size not multiple of width");

        return (width, Quotient, FromValueTypesToByte(pixels).ToArray()); // 
    }
}

public static class DebugLayer
{
    /// <summary>Clear Messages + All</summary> 
    public static void DXGI_DebugLayer0(string name, bool stopIfError)
    {
        DXGI_ClearMessages(name, stopIfError);
        //DebugLayer_DXGIbased(DebugLayerTypes.DetailInternal, name, stopIfError);
        DebugLayer_DXGIbased(DebugLayerTypes.All, name, stopIfError);
    }

    /// <summary>
    /// Messages
    /// </summary>
    public static void DXGI_DebugLayer1(string name, bool stop)
    {
        DebugLayer_DXGIbased(DebugLayerTypes.Messages, name, stop);
    }

    /// <summary>
    /// Messages + All
    /// </summary>
    public static void DXGI_DebugLayer2(string name, bool stop)
    {
        DebugLayer_DXGIbased(DebugLayerTypes.Messages, name, stop);
        DebugLayer_DXGIbased(DebugLayerTypes.All, name, stop);
    }

    /// <summary>
    /// Messages + DetailInternal + All
    /// </summary>
    public static void DXGI_DebugLayer3(string name, bool stop)
    {
        DebugLayer_DXGIbased(DebugLayerTypes.Messages, name, stop);
        DebugLayer_DXGIbased(DebugLayerTypes.DetailInternal, name, stop);
        DebugLayer_DXGIbased(DebugLayerTypes.All, name, stop);
    }

    public static void DXGI_ClearMessages(string onde, bool stop)
    {
        if (DXGIGetDebugInterface1(out IDXGIInfoQueue dxgiInfoQueue).Failure) throw new Exception();
        var n1 = dxgiInfoQueue.GetNumStoredMessages(DebugAll);
        for (var n0 = 0u; n0 < n1; ++n0)
        {
            var v1 = dxgiInfoQueue.GetMessage(DebugAll, n0);
            if (stop && (v1.Severity == InfoQueueMessageSeverity.Error || v1.Severity == InfoQueueMessageSeverity.Corruption))
                throw new Exception($"Location: {onde} {v1.Description}");
        }
        dxgiInfoQueue.ClearStoredMessages(DebugAll);
    }


    static void DebugLayer_DXGIbased(DebugLayerTypes tipo, string onde, bool stop)
    {
        if (DXGIGetDebugInterface1(out IDXGIInfoQueue dxgiInfoQueue).Failure) throw new Exception();

        var toto2 = "Messages";
        if (tipo != DebugLayerTypes.Messages)
        {
            if (dxgiInfoQueue.GetNumStoredMessages(DebugAll) != 0) throw new Exception("Queue não esta vazia");
            if (DXGIGetDebugInterface1(out IDXGIDebug1 dxgiDebug).Failure) throw new Exception("Não tem a interface de Debug");

            switch (tipo)
            {
                case DebugLayerTypes.Summary:
                    toto2 = "SUMMARY";
                    dxgiDebug.ReportLiveObjects(DebugAll, ReportLiveObjectFlags.Summary | ReportLiveObjectFlags.IgnoreInternal);
                    break;
                case DebugLayerTypes.Detail:
                    toto2 = "DETAIL";
                    dxgiDebug.ReportLiveObjects(DebugAll, ReportLiveObjectFlags.Detail | ReportLiveObjectFlags.IgnoreInternal);
                    break;
                case DebugLayerTypes.SummaryInternal:
                    toto2 = "SUMMARY INTERNAL";
                    dxgiDebug.ReportLiveObjects(DebugAll, ReportLiveObjectFlags.Summary);
                    break;
                case DebugLayerTypes.DetailInternal:
                    toto2 = "DETAIL INTERNAL";
                    dxgiDebug.ReportLiveObjects(DebugAll, ReportLiveObjectFlags.Detail);
                    break;
                case DebugLayerTypes.All:
                    toto2 = "ALL";
                    dxgiDebug.ReportLiveObjects(DebugAll, ReportLiveObjectFlags.All);
                    break;
                case DebugLayerTypes.ValidateContext:
                    throw new Exception("not implemented");
            }
            dxgiDebug.Dispose();
        }
        var n1 = dxgiInfoQueue.GetNumStoredMessages(DebugAll);
        dxgiInfoQueue.AddApplicationMessage(InfoQueueMessageSeverity.Info, $"*** DXGI: {n1} {toto2} @ {onde}");
        Debug.WriteLine(dxgiInfoQueue.GetMessage(DebugAll, n1).Description);
        for (var n0 = 0u; n0 < n1; ++n0)
        {
            var v1 = dxgiInfoQueue.GetMessage(DebugAll, n0);
            Debug.WriteLine($"{n0,4} {v1.Severity,-10} {v1.Category,-20} {v1.Description}");
            if (stop && (v1.Severity == InfoQueueMessageSeverity.Error || v1.Severity == InfoQueueMessageSeverity.Corruption))
                throw new Exception($"Location: {onde} {v1.Description}");
        }
        dxgiInfoQueue.ClearStoredMessages(DebugAll);
        dxgiInfoQueue.Dispose();
    }

    /// <summary>
    /// Warning messages + Stop (or not) on Error, Corruption 
    /// </summary>
    public static void DebugLayer_DXGIbased(string onde, bool stop)
    {
        if (DXGIGetDebugInterface1(out IDXGIInfoQueue dxgiInfoQueue).Failure) throw new Exception();
        var n1 = dxgiInfoQueue.GetNumStoredMessages(DebugAll);
        for (var n0 = 0u; n0 < n1; ++n0)
        {
            var v1 = dxgiInfoQueue.GetMessage(DebugAll, n0);
            if((int)v1.Severity < 3)
            {
                Debug.WriteLine($"{n0,4} {v1.Severity,-10} {v1.Category,-20} {v1.Description}");
                if (stop && v1.Severity != InfoQueueMessageSeverity.Warning) throw new Exception($"Location: {onde} {v1.Description}");
            }
        }
        dxgiInfoQueue.ClearStoredMessages(DebugAll);
        dxgiInfoQueue.Dispose();
    }

    public static void DXGI_Mama()
    {
        if (DXGIGetDebugInterface1(out IDXGIInfoQueue dxgiInfoQueue).Failure) throw new Exception();

        var n1 = dxgiInfoQueue.GetNumStoredMessages(DebugAll);

        for (var n0 = 0u; n0 < n1; ++n0)
        {
            var v1 = dxgiInfoQueue.GetMessage(DebugAll, n0);

            var t1 = v1.Description;

            if (t1.StartsWith("Create "))
            {
                var t2 = t1[7..];

                if (t2.StartsWith("ID3D12RootSignature: "))
                {
                    var t3 = t2[21..];
                    if (t3.StartsWith("Addr=0x"))
                    {
                        var t4 = t3[5..23];
                        var intValue = Convert.ToInt64(t4, 16);
                        var p1 = new IntPtr(intValue);
                    }
                }
            }
            Debug.WriteLine($"{n0,4} {v1.Severity,-10} {v1.Category,-20} {v1.Description}");
        }
        dxgiInfoQueue.ClearStoredMessages(DebugAll);
        dxgiInfoQueue.Dispose();
    }

    public static void DXGI_DebugLayerFinalSummary(string onde, bool stop) // To be used just before Device.Dispose(). After Device.Dispose there is no DebugLayer anymore.
    {
        if (DXGIGetDebugInterface1(out IDXGIInfoQueue dxgiInfoQueue).Failure) throw new Exception();
        dxgiInfoQueue.ClearStoredMessages(DebugAll);

        if (DXGIGetDebugInterface1(out IDXGIDebug1 dxgiDebug).Failure) throw new Exception("Não tem a interface de Debug");
        dxgiDebug.ReportLiveObjects(DebugAll, ReportLiveObjectFlags.Detail);
        dxgiDebug.Dispose();

        var n1 = dxgiInfoQueue.GetNumStoredMessages(DebugAll);

        dxgiInfoQueue.AddApplicationMessage(InfoQueueMessageSeverity.Info, $"*** DXGI DETAIL - {onde}");
        Debug.WriteLine(dxgiInfoQueue.GetMessage(DebugAll, n1).Description);
        for (var n0 = 0u; n0 < n1; ++n0)
        {
            var v1 = dxgiInfoQueue.GetMessage(DebugAll, n0);
            Debug.WriteLine($"{n0,4} {v1.Severity,-10} {v1.Category,-20} {v1.Description}");
            if (v1.Severity == InfoQueueMessageSeverity.Error || v1.Severity == InfoQueueMessageSeverity.Corruption) throw new Exception($"Location: {onde} {v1.Description}");
        }
        dxgiInfoQueue.Dispose();
    }

    public static void DXGI_DebugLayerQuiet(string onde, bool stop)
    {
        if (DXGIGetDebugInterface1(out IDXGIInfoQueue dxgiInfoQueue).Failure) throw new Exception();
        var n1 = dxgiInfoQueue.GetNumStoredMessages(DebugAll);
        if (n1 != 0)
        {
            Debug.WriteLine($"*** DXGI - {onde}");
            for (var n0 = 0u; n0 < n1; ++n0)
            {
                var v1 = dxgiInfoQueue.GetMessage(DebugAll, n0);
                Debug.WriteLine($"{n0,4} {v1.Severity,-10} {v1.Category,-20} {v1.Description}");
                if (stop && (v1.Severity == InfoQueueMessageSeverity.Error || v1.Severity == InfoQueueMessageSeverity.Corruption))
                    throw new Exception($"Location: {onde} {v1.Description}");
            }
            dxgiInfoQueue.ClearStoredMessages(DebugAll);
            Debug.WriteLine("---");
        }
        dxgiInfoQueue.Dispose();
    }
    public static void DXGI_DebugLayerQuiet2(string onde)
    {
        if (DXGIGetDebugInterface1(out IDXGIInfoQueue dxgiInfoQueue).Failure) throw new Exception();
        var n1 = dxgiInfoQueue.GetNumStoredMessages(DebugAll);
        if (n1 != 0)
        {
            for (var n0 = 0u; n0 < n1; ++n0)
            {
                var v1 = dxgiInfoQueue.GetMessage(DebugAll, n0);
                if (v1.Severity == InfoQueueMessageSeverity.Error || v1.Severity == InfoQueueMessageSeverity.Corruption)
                {
                    Debug.WriteLine($"{v1.Severity,-10} msg {n0,4} de {n1,4}: {onde} {v1.Description}");
                }
            }
            dxgiInfoQueue.ClearStoredMessages(DebugAll);
        }
        dxgiInfoQueue.Dispose();
    }
    public static void DXGI_DebugLayerQuiet3(string onde)
    {
        if (DXGIGetDebugInterface1(out IDXGIInfoQueue dxgiInfoQueue).Failure) throw new Exception();
        var n1 = dxgiInfoQueue.GetNumStoredMessages(DebugAll);
        if (n1 != 0)
        {
            for (var n0 = 0u; n0 < n1; ++n0)
            {
                var v1 = dxgiInfoQueue.GetMessage(DebugAll, n0);
                if (v1.Severity != InfoQueueMessageSeverity.Info)
                {
                    Debug.WriteLine($"{v1.Severity,-10} msg {n0,4} de {n1,4}: {onde} {v1.Description}");
                }
            }
            dxgiInfoQueue.ClearStoredMessages(DebugAll);
        }
        dxgiInfoQueue.Dispose();
    }

}

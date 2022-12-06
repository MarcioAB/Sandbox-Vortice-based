using System.Numerics;

using Vortice.Direct3D;
using Vortice.Mathematics;

namespace D3D_Mama;

/*  Como o format do IA (semantic) afeta a passagem dos dados para o shader:

(1) Se shader e dados são float2 porem o IA esta como float3,
    o IA quase não atrapalha, a não ser no ultimo vertice.
    No ultimo vertice, como falta dados para o IA, o IA zera esse ultimo vertice.

(2) Se shader e dados são float3 porem o IA esta como float2,
    o IA trunca a 3° coordenada, entrega um float2 para o shader e o shader completa a 3° coordenada com 0.

(3) Se o shader espera float3 mas os dados são float2:
    Se o IA esta como float2, o IA entrega float2 e o shader completa a 3° coordenada com 0.
    Se o IA esta como float3, o IA "empresta" um float do proximo vertice e entrega um float3 que não faz muito sentido.
 */

/*
SandBox BASIC introduction

L00: nothing at all. No geometry, just the basic clean before each frame.
L0: NON-INDEXED introduction
L0: NON-INDEXED introduction: 2 drawsets
L1: INDEXED introduction
L2: MVP introduction
L3: TEXTURE introduction
L4: MULTIDATA introduction
L5: INSTANCES introduction
*/

public static class MyDataSets
{
    // Sandbox introduction: models 1, 2, 3, 4 e 5
    #region INTRODUCTION
    // L0A, L0B : line direct
    public static (PrimitiveTopology tipo, (float, float)[] vertex) Line1_direct(float v) => (PrimitiveTopology.LineList,
new (float, float)[] { (-0.95f, 0.95f), (0.95f, v) });

    // L1 & L2 :line indexed
    public static (PrimitiveTopology tipo, (float, float)[] vertex, ushort[] indices) Line1(float v) => (PrimitiveTopology.LineList,
new (float, float)[] { (-0.95f, 0.95f), (0.95f, v) },
new ushort[] { 0, 1 });

    // L3, L4 & L5 ( 1 mixed buffer: vertex coords + texture UV coords )
    public static (PrimitiveTopology tipo, (float, float, float, float, float)[] vertex, ushort[] indices) Triang1(float z) => (PrimitiveTopology.TriangleList,
new (float, float, float, float, float)[] { (0, 0, z, 0, 1), (0, 1, z, 0, 0), (2, 0, z, 1, 1) }, // vertex coords + texture coords
new ushort[] { 0, 1, 2 }); // vertex index

    // L3, L4 & L5 ( 2 buffers : 1 for vertex coords, 1 for texture UV coords)
    public static (PrimitiveTopology tipo, (float, float, float)[] vertex, ushort[] indices, (float, float)[] texture) Triang2(float z) => (PrimitiveTopology.TriangleList,
new (float, float, float)[] { (0, 0.5f, z), (0, 1.5f, z), (2, 0.5f, z) }, // vertex coords
new ushort[] { 0, 1, 2 }, // vertex index
new (float, float)[] { (0, 1), (0, 0), (1, 1) }); // texture coords

    // L5 (instances)
    public static float[] Instance1() =>
new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    #endregion

    // L3A
    public static (PrimitiveTopology tipo, (float, float, float)[] vertex, ushort[] indices) Triang_L3A(float z) => (PrimitiveTopology.TriangleList,
new (float, float, float)[] { (0, 0, z), (0, 1, z), (2, 0, z) }, // vertex coords + texture coords
new ushort[] { 0, 1, 2 }); // vertex index




    // L5A & L5B
    public static (PrimitiveTopology tipo, Vector2[] vertex, ushort[] indices) Line9(float v) => (PrimitiveTopology.LineList,
new Vector2[] { new Vector2(-0.95f, 0.95f), new Vector2(0.95f, v) },
new ushort[] { 0, 1 });
    public static (float, float, float)[] Instance2() => 
new (float, float, float)[] { (0, -0.1f, 0), (0, -0.2f, 0.1f), (0, -0.3f, 0.2f), (0, -0.4f, 0.3f) };

    // L3B
    public static (PrimitiveTopology tipo, (float, float)[] vertex, ushort[] indices, (float, float)[] texture) Triang4A() => (PrimitiveTopology.TriangleList,
new (float, float)[] { (1, 0), (2, 0), (0, 1) }, // vertex coords
new ushort[] { 0, 1, 2 }, // vertex index
new (float, float)[] { (0.5f, 1), (1, 1), (0, 0) }); // texture coords

    // ( L4A & L5B ) - Triangle on XY plane, indexing
    public static (PrimitiveTopology tipo, (float, float)[] vertex, ushort[] indices) Triang1() => (PrimitiveTopology.TriangleList,
new (float, float)[] { (0, 0), (1, 0), (1, 1) },
new ushort[] { 0, 1, 2 }); // Counter-clockwise vertices. FrontCounterClockwise = false --> this tringle is Back-faced








    // L1A: Eixo XYZ: buffer1: vertex coords: buffer2: vertex color
    public static (PrimitiveTopology tipo, (float, float, float)[] vertex) EixoXYZ() => (PrimitiveTopology.LineList,
    new (float, float, float)[] { (0, 0, 0), (1, 0, 0), (0, 0, 0), (0, 1, 0), (0, 0, 0), (0, 0, 1) });
    public static (float, float, float)[] EixoXYZ_Colors()
    {
        var c1 = new Color4[] { Colors.Red, Colors.Lime, Colors.Blue };
        return new (float, float, float)[] { f1(c1[0]), f1(c1[0]), f1(c1[1]), f1(c1[1]), f1(c1[2]), f1(c1[2]) };
        static (float, float, float) f1(Color4 c) => (c.R, c.G, c.B);
    }



    public static (PrimitiveTopology tipo, (float, float)[] vertex) Line1_direct97() => (PrimitiveTopology.LineList,
    new (float, float)[]
    {
            (-0.95f, 0.95f), (0.65f, 0.95f),
            (-0.95f, 0.85f), (0.95f, 0.85f),
            (-0.95f, 0.75f), (0.95f, 0.75f)
    });


    // Instanced data: Usado nos modelos 3, 4 e 5 onde os elementos são instanciados.
    public static (float, float)[] Instance5() => new (float, float)[] { (0, -0.1f), (0, -0.2f), (0, -0.3f), (0, -0.4f) };

    public static (float, float)[] Instance3() => new (float, float)[] { (0,0) };

    public static (PrimitiveTopology tipo, (float, float)[] vertex) Triang2() => (PrimitiveTopology.TriangleList,
        new (float, float)[] { (0, 0), (1, 0), (1, 1) }); // Counter-clockwise vertices. FrontCounterClockwise = false --> this tringle is Back-faced

    public static (PrimitiveTopology tipo, (float, float)[] vertex, (float, float)[] texture) Triang3() => (PrimitiveTopology.TriangleList,
    new (float, float)[] { (0.01f, 1.01f), (2.01f, 0.01f), (2.01f, 1.01f) }, // vertex coords
    new (float, float)[] { (0, 2), (0, 0), (0, 0) }); // texture coords

    public static (PrimitiveTopology tipo, (float, float)[] vertex, (float, float)[] texture) Triang3B() => (PrimitiveTopology.TriangleList,
new (float, float)[] { (0.01f, 1.01f), (2.01f, 0.01f), (2.01f, 1.01f) }, // vertex coords
new (float, float)[] { (0, 0), (2, 1), (2, 0) }); // texture coords

    public static (PrimitiveTopology tipo, (float, float)[] vertex, (float, float)[] texture) Triang3C() => (PrimitiveTopology.TriangleList,
new (float, float)[] { (0, 1), (0, 2), (2, 1) }, // vertex coords
new (float, float)[] { (0, 2), (0, 0), (2, 2) }); // texture coords


    public static (PrimitiveTopology tipo, (float, float)[] vertex, (float, float)[] texture) Triang4() => (PrimitiveTopology.TriangleList,
    new (float, float)[] { (1, 0), (2, 0), (0, 1) }, // vertex coords
    new (float, float)[] { (0.5f, 1), (1, 1), (0, 0) }); // texture coords


    public static (PrimitiveTopology tipo, (float, float, float, float)[] vertex) Triang5() => (PrimitiveTopology.TriangleList,
        new (float, float, float, float)[] { (0, 0, 0, 1), (0, 1, 0, 0), (2, 0, 1, 1) }); // vertex coords + texture coords


    public static (PrimitiveTopology tipo, (float, float)[] vertex, (float, float)[] texture) Triang6() => (PrimitiveTopology.TriangleList,
new (float, float)[] { (0, 1), (0, 2), (2, 1) }, // vertex coords
new (float, float)[] { (0, 1), (0, 0), (1, 1) }); // texture coords


    public static (PrimitiveTopology tipo, (float,float)[] vertexX, (float,float)[] vertexY) Triang7() => (PrimitiveTopology.TriangleList,
new (float,float)[] { (0, -0.5f), (0.5f, -0.5f), (0.5f, 0) }, // coordX
new (float,float)[] { (0, 0), (0.5f, 0), (0.5f, 0.5f) }); // coordY





    public static (PrimitiveTopology tipo, Vector2[] vertex) Line2_direct(float v) => (PrimitiveTopology.LineList,
    new Vector2[] { new Vector2(-0.95f, 0.95f), new Vector2(0.95f, v) });

    public static (PrimitiveTopology tipo, Vector3[] vertex) Line3_direct(float v) => (PrimitiveTopology.LineList,
        new Vector3[] { new Vector3(-0.95f, 0.95f, 0), new Vector3(0.95f, v, 0) });




    public struct VertexPositionColor
    {
        public readonly Vector3 Position;
        public readonly Color4 Color;

        public VertexPositionColor(in Vector3 position, in Color4 color)
        {
            Position = position;
            Color = color;
        }
    }

    public struct VertexPositionNormalTexture
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;

        public VertexPositionNormalTexture(in Vector3 position, in Vector3 normal, in Vector2 textureCoordinate)
        {
            Position = position;
            Normal = normal;
            TextureCoordinate = textureCoordinate;
        }
    }


    // o stride e a qtd_vertices são necessarios para o DRAW.
    //
    // qtd_vertices = qtd_lines / lines_per_vertex
    // stride = vertexBuffers.Description.ByteWidth / qtd_vertices;
    //
    // o numero de lines_per_vertex é necessario para calcular o numero de vertices.

    // ------------------- float4
    #region float4
    // float4 vertice. 1 vertice cada linha.
    public static (PrimitiveTopology tipo, int lines_per_vertex, Vector4[] vertex, ushort[] index) DSA_Triangulo_Vector4(int code)
    {
        var z1 = 0.5f; // 0.5 corresponde ao centro do Z do Viewport
        var z2 = z1 - 0.1f; // Z varia de 0 a 1, sendo 0 o mais proximo e 1 o mais afastado
        Vector4[] x;
        ushort[] y;
        switch (code)
        {
            case 1: // 1 triangulo na parte de cima afastado z2 no eixo Z
                //x = new Vector4[] { new Vector4(0, 0, 1, 1), new Vector4(0.95f, 0, 1, 1), new Vector4(0.95f, 0.95f, 0.9999999701976776123046874f, 1) }; // impressionante essa "precisão" do float
                x = new Vector4[] { new Vector4(0, 0, z2, 1), new Vector4(0.95f, 0, z2, 1), new Vector4(0.95f, 0.95f, z2, 1) };
                y = new ushort[] { 0, 1, 2 };
                break;

            case 2: // 4 triangulos na parte de cima e de baixo, afastados z no eixo Z
                x = new Vector4[] {
                    new Vector4(0, 0, z1, 1), new Vector4(0.5f, 0.05f, z1, 1), new Vector4(0.5f, 0.5f, z1, 1),
                    new Vector4(0, 0, z1, 1), new Vector4(0.95f, -0.05f, z1, 1), new Vector4(0.95f, -0.95f, z1, 1),
                    new Vector4(0, 0, z1, 1), new Vector4(-0.95f, -0.05f, z1, 1), new Vector4(-0.95f, -0.95f, z1, 1),
                    new Vector4(0, 0, z1, 1), new Vector4(-0.95f, 0.05f, z1, 1), new Vector4(-0.95f, 0.95f, z1, 1) };
                y = new ushort[]
                {
                    0,1,2,
                    3,4,5,
                    6,7,8,
                    9,10,11
                };
                break;

            case 3: // 1 triangulo na parte de baixo afastado z2 no eixo Z
                x = new Vector4[] { new Vector4(0, 0, z2, 1), new Vector4(-0.95f, 0, z2, 1), new Vector4(-0.95f, -0.95f, z2, 1) };
                y = new ushort[] { 0, 1, 2 };
                break;
            default: throw new Exception();

        }
        return (PrimitiveTopology.TriangleList, 1, x, y);
    }

    // float4 vertice. 1 vertice cada linha.
    public static (PrimitiveTopology tipo, int lines_per_vertex, (float, float, float, float)[] vertex, ushort[] index) DSA_Triangulo_float4()
        => (PrimitiveTopology.TriangleList, 1, 
        new (float, float, float, float)[] 
        {
            (0, 0, 0, 1), (0.95f, 0.05f, 0, 1), (0.95f, 0.95f, 0, 1),
            (0, 0, 0, 1), (0.95f, -0.05f, 0, 1), (0.95f, -0.95f, 0, 1),
            (0, 0, 0, 1), (-0.95f, -0.05f, 0, 1), (-0.95f, -0.95f, 0, 1),
            (0, 0, 0, 1), (-0.95f, 0.05f, 0, 1), (-0.95f, 0.95f, 0, 1) 
        }, 
        new ushort[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 });

    // float4 vertice. 1 vertice cada 2 linhas.
    public static (PrimitiveTopology tipo, int lines_per_vertex, (float, float)[] vertex) DSA_Triangulo_2float2()
        => (PrimitiveTopology.TriangleList, 2, new (float, float)[] {
            (0, 0), (0, 1), (0.95f, 0.05f), (0, 1), (0.95f, 0.95f), (0, 1),
            (0, 0), (0, 1), (0.95f, -0.05f), (0, 1), (0.95f, -0.95f), (0, 1),
            (0, 0), (0, 1), (-0.95f, -0.05f), (0, 1), (-0.95f, -0.95f), (0, 1),
            (0, 0), (0, 1), (-0.95f, 0.05f), (0, 1), (-0.95f, 0.95f), (0, 1) });

    // float4 vertice. 1 vertice cada 4 linhas
    public static (PrimitiveTopology tipo, int lines_per_vertex, float[] vertex) DSA_Triangulo_4float(int code)
    {
        float[] x;
        switch (code)
        {
            case 1:
                x = new float[] { 0, 0, 0, 1, 0.95f, 0, 0, 1, 0.95f, 0.95f, 0, 1 };
                break;
            case 2:
                x = new float[] {
            0, 0, 0, 1, 0.95f, 0.05f, 0, 1, 0.95f, 0.95f, 0, 1,
            0, 0, 0, 1, 0.95f, -0.05f, 0, 1, 0.95f, -0.95f, 0, 1,
            0, 0, 0, 1, -0.95f, -0.05f, 0, 1, -0.95f, -0.95f, 0, 1,
            0, 0, 0, 1, -0.95f, 0.05f, 0, 1, -0.95f, 0.95f, 0, 1 };
                break;
            default: throw new Exception();
        }
        return (PrimitiveTopology.TriangleList, 4, x);
    }
    #endregion

    // -------------------- Half
    #region Half
    // Half4 vertice. 1 vertice a cada linha
    public static (PrimitiveTopology tipo, int lines_per_vertex, (Half, Half, Half, Half)[] vertex, ushort[] index) DSA_Triangulo_Half4()
        => (PrimitiveTopology.TriangleList, 1,
        new (Half, Half, Half, Half)[] 
        { 
            ((Half)(-0.95f), (Half)(-0.95), (Half)0, (Half)1),
            ((Half)0.95f, (Half)(-0.95f), (Half)0, (Half)1),
            ((Half)0.95f, (Half)0.95f, (Half)0, (Half)1)
        },
        new ushort[] { 0, 1, 2 });

    // Half3 vertice. 1 vertice a cada linha
    public static (PrimitiveTopology tipo, int lines_per_vertex, (Half, Half, Half)[] vertex, ushort[] index) DSA_Triangulo_Half3()
        => (PrimitiveTopology.TriangleList, 1, 
        new (Half, Half, Half)[] 
        {
            ((Half)0, (Half)0, (Half)0), 
            ((Half)0.95f, (Half)0.05f, (Half)0), 
            ((Half)0.95f, (Half)0.95f, (Half)0)
        },
        new ushort[] { 0, 1, 2 });

    // Half2 vertice. 1 vertice a cada linha
    public static (PrimitiveTopology tipo, int lines_per_vertex, (Half, Half)[] vertex, ushort[] index) DSA_Triangulo_Half2()
        => (PrimitiveTopology.TriangleList, 1,
        new (Half, Half)[] 
        { 
            ((Half)0, (Half)0),
            ((Half)0.95f, (Half)0.05f),
            ((Half)0.95f, (Half)0.95f) 
        },
        new ushort[] { 0, 1, 2});
    #endregion

    // ------------------- float3
    #region float3
    /// <summary>
    /// float3[] com vertex float3. 1 vertice por linha.
    /// </summary>
    public static (PrimitiveTopology tipo, int lines_per_vertex, Vector3[] vertex, ushort[] index) DSA_Triangulo_Vector3(int code)
    {
        Vector3[] x;
        ushort[] y;
        switch (code)
        {
            case 1:
                x = new Vector3[] { new Vector3(0, 0, 0), new Vector3(0.8f, 0, 0), new Vector3(0.95f, 0.95f, 0) };
                y = new ushort[] { 0, 1, 2 };
                break;
            case 2:
                x = new Vector3[] {
                    new Vector3(0, 0, 0), new Vector3(0.8f, 0.05f, 0), new Vector3(0.95f, 0.95f, 0),
                    new Vector3(0, 0, 0), new Vector3(0.95f, -0.05f, 0), new Vector3(0.95f, -0.95f, 0) };
                y = new ushort[] { 0, 1, 2, 3, 4, 5};

                break;
            case 3:
                x = new Vector3[] {
                    new Vector3(0, 0, 0), new Vector3(0.95f, 0.05f, 0), new Vector3(0.95f, 0.95f, 0),
                    new Vector3(0, 0, 0), new Vector3(0.95f, -0.05f, 0), new Vector3(0.95f, -0.95f, 0),
                    new Vector3(0, 0, 0), new Vector3(-0.95f, -0.05f, 0), new Vector3(-0.95f, -0.95f, 0) };
                y = new ushort[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
                break;

            case 4:
                x = new Vector3[] {
                    new Vector3(0.05f, 0.05f, 0), new Vector3(0.95f, 0.05f, 0), new Vector3(0.95f, 0.95f, 0),
                    new Vector3(0.05f, -0.05f, 0), new Vector3(0.95f, -0.05f, 0), new Vector3(0.95f, -0.95f, 0),
                    new Vector3(-0.05f, -0.05f, 0), new Vector3(-0.95f, -0.05f, 0), new Vector3(-0.95f, -0.95f, 0),
                    new Vector3(-0.05f, 0.05f, 0.1f), new Vector3(-0.95f, 0.05f, 0.1f), new Vector3(-0.95f, 0.95f, 0.1f) };
                y = new ushort[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11};
                break;
            default: throw new Exception();

        }
        return (PrimitiveTopology.TriangleList, 1, x, y);
    }


    /// <summary>
    /// float3[] com vertex float3. 1 vertice por linha.
    /// </summary>
    public static (PrimitiveTopology tipo, int lines_per_vertex, Vector3[] vertex) DSA_Box1(Vector3 size) // triangulos definidos diretamente na matrix de coordenadas
    {
        var mesh = CreateBox2(size);
        return (PrimitiveTopology.TriangleList, 0, mesh);
    }
    /// <summary>
    /// float3[] com vertex float3. 1 vertice por linha.
    /// </summary>
    public static (PrimitiveTopology tipo, int lines_per_vertex, Vector3[] vertex) Toto3()
    {
        var x = new Vector3[]
        {
            new Vector3(-0.8f, -0.8f, 0f),
            new Vector3(0.8f, 0.8f, 0f),
        };
        return (PrimitiveTopology.LineList, 1, x);
    }

    public static Vector3[] CreateBox2(in Vector3 size)
    {
        var result = new Vector3[36];
        var x = size.X; var y = size.Y; var z = size.Z;

        var vertices = new Vector3[]
        {
            new Vector3(0, 0, 0), new Vector3(x, 0, 0), new Vector3(x, 0, z), new Vector3(0, 0, z), // 0,1,2,3
            new Vector3(0, y, 0), new Vector3(x, y, 0), new Vector3(x, y, z), new Vector3(0, y, z)  // 4,5,6,7
        };

        var faces = new (int, int, int)[]
        {
            (2, 1, 0), (3, 2, 0),
            (2, 1, 0), (3, 2, 0),
            (2, 1, 0), (3, 2, 0),
            (2, 1, 0), (3, 2, 0),
            //(2, 1, 0), (3, 2, 0),

            //(1, 5, 0), (5, 4, 0),
            //(5, 1, 0), (0, 4, 5),

            //(1, 2, 6), (6, 5, 1),
            //(2, 3, 7), (7, 6, 2),
            (0, 3, 7), (7, 4, 0),
            (4, 5, 6), (6, 7, 4)
        };

        var k = 0;
        for (int i = 0; i < 12; i++)
        {
            result[k] = vertices[faces[i].Item1]; k++;
            result[k] = vertices[faces[i].Item2]; k++;
            result[k] = vertices[faces[i].Item3]; k++;
        }
        return result;
    }

    /// <summary>
    /// float3[] com vertex float3. 1 vertice cada linha.
    /// </summary>
    public static (PrimitiveTopology tipo, int lines_per_vertex, (float, float, float)[] vertex) DSA_Triangulo_float3()
        => (PrimitiveTopology.TriangleList, 1, new (float, float, float)[] {
            (0, 0, 0), (0.95f, 0.05f, 0), (0.95f, 0.95f, 0),
            (0, 0, 0), (0.95f, -0.05f, 0), (0.95f, -0.95f, 0),
            (0, 0, 0), (-0.95f, -0.05f, 0), (-0.95f, -0.95f, 0),
            (0, 0, 0), (-0.95f, 0.05f, 0), (-0.95f, 0.95f, 0) });
    #endregion

    // ------------------- float2
    #region float2
    /// <summary>
    /// float2[] com vertex float2. 1 vertice a cada linha.  
    /// </summary>
    public static (PrimitiveTopology tipo, int lines_per_vertex, Vector2[] vertex, ushort[] indices) DSA_Triangulo_Vector2()
    {
        return (PrimitiveTopology.TriangleList, 1, 
            new Vector2[] { new Vector2(0, 0), new Vector2(0.95f, 0), new Vector2(0.95f, 0.95f) },
            new ushort[] { 0,1,2});
    }
    /// <summary>
    /// float2[] com vertex float2. 1 vertice a cada linha.  
    /// </summary>
    public static (PrimitiveTopology tipo, int lines_per_vertex, (float, float)[] vertex) Line0()
    {
        var x = new (float, float)[] { new(-0.8f, -0.8f), new(0.8f, 0.8f) };
        return (PrimitiveTopology.LineList, 1, x);
    }


    // Simplex aqui significa apenas uma array com as coordenadas dos vertices, usando apenas Vector ou Tuplet,
    // cada linha array contendo uma coordenada completa (2, 3 ou 4 valores), sem offset, sem qquer outra coisa.








    public static (PrimitiveTopology tipo, int lines_per_vertex, (float, float)[] vertex, ushort[] index) Line1_Indexed(float v)
    {
        var vertex = new (float, float)[] { new(-0.95f, 0.95f), new(0.95f, v) };
        var index = new ushort[] { 0, 1 };
        return (PrimitiveTopology.LineList, 1, vertex, index);
    }

    public static (PrimitiveTopology tipo, int lines_per_vertex, VertexPositionColor[] vertex) toto99()
    {
        var x = new VertexPositionColor[]
        {
            new VertexPositionColor(new Vector3(0, 0.5f, 0), Colors.Red),
            new VertexPositionColor(new Vector3(0.5f, -0.5f, 0), Colors.Green),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0), Colors.Blue)
        };
        return (PrimitiveTopology.TriangleList, 1, x);
    }


    /// <summary>
    /// float2[] com vertex float2. No extra payload
    /// </summary>
    public static (PrimitiveTopology tipo, int lines_per_vertex, (float, float)[] vertex) Line2(float v)
    {
        var vertex = new (float, float)[] { new(-v, v), new(v, -v) };
        return (PrimitiveTopology.LineList, 1, vertex);
    }

    #endregion

    // vertice cada TBD linhas
    public static (PrimitiveTopology tipo, int lines_per_vertex, VertexPositionNormalTexture[] vertex, ushort[] index) DSE_Box1(Vector3 size) // triangulos definidos diretamente na matrix de coordenadas
    {
        var mesh = CreateBox(size);
        return (PrimitiveTopology.TriangleList, 0, mesh.vertices, mesh.indices);
    }

    public static (PrimitiveTopology tipo, int lines_per_vertex, VertexPositionColor[] vertex) toto()
    {
        var x = new VertexPositionColor[]
        {
            new VertexPositionColor(new Vector3(0, 0.5f, 0), Colors.Red),
            new VertexPositionColor(new Vector3(0.5f, -0.5f, 0), Colors.Green),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0), Colors.Blue)
        };
        return (PrimitiveTopology.TriangleList, 1, x);
    }

    public static (PrimitiveTopology tipo, int lines_per_vertex, VertexPositionColor[] vertex) toto2()
    {
        var x = new VertexPositionColor[]
        {
            new VertexPositionColor(new Vector3(0f, 0f, 0f), Colors.Red),
            new VertexPositionColor(new Vector3(1f, 0f, 0f), Colors.Red),
            new VertexPositionColor(new Vector3(0f, 0f, 0f), Colors.Green),
            new VertexPositionColor(new Vector3(0f, 1f, 0f), Colors.Green),
            new VertexPositionColor(new Vector3(0f, 0f, 0f), Colors.Blue),
            new VertexPositionColor(new Vector3(0f, 0f, 1f), Colors.Blue)
        };
        return (PrimitiveTopology.LineList, 1, x);
    }

    public static (VertexPositionNormalTexture[] vertices, ushort[] indices) CreateBox(in Vector3 size)
    {
        List<VertexPositionNormalTexture> vertices = new();
        List<ushort> indices = new();

        Vector3[] faceNormals = new Vector3[6]
        {
            Vector3.UnitZ, new Vector3(0.0f, 0.0f, -1.0f),
            Vector3.UnitX, new Vector3(-1.0f, 0.0f, 0.0f),
            Vector3.UnitY, new Vector3(0.0f, -1.0f, 0.0f),
        };

        Vector2[] textureCoordinates = new Vector2[4]
        {
            Vector2.UnitX,
            Vector2.One,
            Vector2.UnitY,
            Vector2.Zero,
        };

        Vector3 tsize = size / 2.0f;

        // Create each face in turn.
        int vbase = 0;
        for (int i = 0; i < 6; i++)
        {
            Vector3 normal = faceNormals[i];

            // Get two vectors perpendicular both to the face normal and to each other.
            Vector3 basis = (i >= 4) ? Vector3.UnitZ : Vector3.UnitY;

            Vector3 side1 = Vector3.Cross(normal, basis);
            Vector3 side2 = Vector3.Cross(normal, side1);

            // Six indices (two triangles) per face.
            indices.Add((ushort)(vbase + 0));
            indices.Add((ushort)(vbase + 1));
            indices.Add((ushort)(vbase + 2));

            indices.Add((ushort)(vbase + 0));
            indices.Add((ushort)(vbase + 2));
            indices.Add((ushort)(vbase + 3));

            // Four vertices per face: (normal - side1 - side2) * tsize // normal // t0, t1, t2, t3
            vertices.Add(new VertexPositionNormalTexture(Vector3.Multiply(Vector3.Subtract(Vector3.Subtract(normal, side1), side2), tsize), normal, textureCoordinates[0]));
            vertices.Add(new VertexPositionNormalTexture(Vector3.Multiply(Vector3.Add(Vector3.Subtract(normal, side1), side2), tsize), normal, textureCoordinates[1]));
            vertices.Add(new VertexPositionNormalTexture(Vector3.Multiply(Vector3.Add(normal, Vector3.Add(side1, side2)), tsize), normal, textureCoordinates[2]));
            vertices.Add(new VertexPositionNormalTexture(Vector3.Multiply(Vector3.Subtract(Vector3.Add(normal, side1), side2), tsize), normal, textureCoordinates[3]));

            vbase += 4;
        }

        return (vertices.ToArray(), indices.ToArray());
    }


    /// <summary>
    /// float3[] com vertex float2. Offset de 1 float o qual pode ser usado para alguma outra coisa
    /// </summary>
    public static ( PrimitiveTopology tipo, int lines_per_vertex, (float, float, float)[] vertex) Line3()
    {
        var x = new (float, float, float)[] { new(10, -0.8f, -0.8f), new(10, -0.1f, -0.1f) };
        return (PrimitiveTopology.LineList, 1, x);
    }


}

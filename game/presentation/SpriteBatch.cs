using Godot;

namespace Rebirth.Presentation;

public partial class SpriteBatch : MultiMeshInstance2D
{
    private static ShaderMaterial? animationMaterial;
    private static ArrayMesh? spriteMesh;
    private float[] buffer = [];
    private int capacity;
    private Vector2 boundsMinimum;
    private Vector2 boundsMaximum;
    public int Count { get; private set; }

    public SpriteBatch() { }

    public SpriteBatch(Texture2D texture)
    {
        Texture = texture;
        TextureFilter = TextureFilterEnum.Nearest;
        animationMaterial ??= new ShaderMaterial { Shader = new Shader { Code = "shader_type canvas_item; void vertex() { UV.x = (UV.x + INSTANCE_CUSTOM.x) * INSTANCE_CUSTOM.y; UV.y = 1.0 - UV.y; }" } };
        Material = animationMaterial;
        Multimesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform2D, UseColors = true, UseCustomData = true, Mesh = spriteMesh ??= CreateSpriteMesh() };
    }

    private static ArrayMesh CreateSpriteMesh()
    {
        using var quad = new QuadMesh { Size = Vector2.One };
        using var arrays = quad.GetMeshArrays();
        var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        arrays[(int)Mesh.ArrayType.Color] = Enumerable.Repeat(Colors.White, vertices.Length).ToArray();
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    public void Begin() => Count = 0;

    public void Add(Vector2 position, Vector2 size, Color color, float rotation = 0, int frame = 0, int frameCount = 1)
    {
        if (Count == capacity)
        {
            capacity = Math.Max(32, capacity * 2);
            Array.Resize(ref buffer, capacity * 16);
            Multimesh.InstanceCount = capacity;
        }
        var offset = Count++ * 16;
        var cosine = MathF.Cos(rotation);
        var sine = MathF.Sin(rotation);
        buffer[offset] = cosine * size.X;
        buffer[offset + 1] = -sine * size.Y;
        buffer[offset + 3] = position.X;
        buffer[offset + 4] = sine * size.X;
        buffer[offset + 5] = cosine * size.Y;
        buffer[offset + 7] = position.Y;
        var extent = new Vector2(MathF.Abs(buffer[offset]) + MathF.Abs(buffer[offset + 1]), MathF.Abs(buffer[offset + 4]) + MathF.Abs(buffer[offset + 5])) * 0.5f;
        boundsMinimum = Count == 1 ? position - extent : boundsMinimum.Min(position - extent);
        boundsMaximum = Count == 1 ? position + extent : boundsMaximum.Max(position + extent);
        buffer[offset + 8] = color.R;
        buffer[offset + 9] = color.G;
        buffer[offset + 10] = color.B;
        buffer[offset + 11] = color.A;
        buffer[offset + 12] = frame;
        buffer[offset + 13] = 1f / frameCount;
    }

    public void Submit()
    {
        Multimesh.VisibleInstanceCount = Count;
        if (Count > 0)
        {
            Multimesh.CustomAabb = new(new(boundsMinimum.X, boundsMinimum.Y, -0.5f), new(boundsMaximum.X - boundsMinimum.X, boundsMaximum.Y - boundsMinimum.Y, 1));
            Multimesh.Buffer = buffer;
        }
    }
}

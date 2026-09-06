using Godot;

namespace Rebirth.Presentation;

public partial class SpriteBatch : MultiMeshInstance2D
{
    private static ShaderMaterial? animationMaterial;
    private float[] buffer = [];
    private int capacity;
    public int Count { get; private set; }

    public SpriteBatch() { }

    public SpriteBatch(Texture2D texture)
    {
        Texture = texture;
        TextureFilter = TextureFilterEnum.Nearest;
        animationMaterial ??= new ShaderMaterial { Shader = new Shader { Code = "shader_type canvas_item; void vertex() { UV.x = (UV.x + INSTANCE_CUSTOM.x) * INSTANCE_CUSTOM.y; }" } };
        Material = animationMaterial;
        Multimesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform2D, UseColors = true, UseCustomData = true, Mesh = new QuadMesh { Size = Vector2.One } };
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
        if (Count > 0) Multimesh.Buffer = buffer;
    }
}

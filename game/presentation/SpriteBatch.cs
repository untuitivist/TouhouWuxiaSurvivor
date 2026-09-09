using Godot;

namespace Rebirth.Presentation;

public partial class SpriteBatch : MultiMeshInstance2D
{
    private static ShaderMaterial? animationMaterial;
    private static ArrayMesh? spriteMesh;
    private float[] buffer = [];
    private int capacity;
    private readonly float fixedSize;
    private float minimumX;
    private float minimumY;
    private float maximumX;
    private float maximumY;
    public int Count { get; private set; }

    public SpriteBatch() { }

    public SpriteBatch(Texture2D texture, float fixedSize = 0)
    {
        this.fixedSize = fixedSize;
        Texture = texture;
        TextureFilter = TextureFilterEnum.Nearest;
        animationMaterial ??= new ShaderMaterial { Shader = new Shader { Code = "shader_type canvas_item; void vertex() { UV.y = 1.0 - UV.y; UV = INSTANCE_CUSTOM.xy + UV * INSTANCE_CUSTOM.zw; }" } };
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

    private void EnsureCapacity()
    {
        if (Count < capacity) return;
        var previousCapacity = capacity;
        capacity = Math.Max(32, capacity * 2);
        Array.Resize(ref buffer, capacity * 16);
        if (fixedSize > 0)
            for (var index = previousCapacity; index < capacity; index++)
            {
                var offset = index * 16;
                buffer[offset] = buffer[offset + 5] = fixedSize;
                buffer[offset + 8] = buffer[offset + 9] = buffer[offset + 10] = buffer[offset + 11] = buffer[offset + 14] = buffer[offset + 15] = 1;
            }
        Multimesh.InstanceCount = capacity;
    }

    public void AddPosition(float horizontal, float vertical)
    {
        if (fixedSize <= 0) throw new InvalidOperationException("Position-only instances require a fixed-size batch");
        EnsureCapacity();
        var offset = Count++ * 16;
        buffer[offset + 3] = horizontal;
        buffer[offset + 7] = vertical;
        var extent = fixedSize * 0.5f;
        minimumX = Count == 1 ? horizontal - extent : MathF.Min(minimumX, horizontal - extent);
        minimumY = Count == 1 ? vertical - extent : MathF.Min(minimumY, vertical - extent);
        maximumX = Count == 1 ? horizontal + extent : MathF.Max(maximumX, horizontal + extent);
        maximumY = Count == 1 ? vertical + extent : MathF.Max(maximumY, vertical + extent);
    }

    public void Add(Vector2 position, Vector2 size, Color color, float rotation = 0, int frame = 0, int frameCount = 1)
    {
        var cosine = rotation == 0 ? 1 : MathF.Cos(rotation);
        var sine = rotation == 0 ? 0 : MathF.Sin(rotation);
        AddBasis(position, size, color, cosine, sine, frame, frameCount);
    }

    public void AddDirected(Vector2 position, Vector2 size, Color color, Vector2 velocity, int frame = 0, int frameCount = 1)
    {
        var length = MathF.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y);
        AddBasis(position, size, color, length > 0 ? -velocity.Y / length : 0, length > 0 ? velocity.X / length : 1, frame, frameCount);
    }

    public void AddRegion(Vector2 position, Vector2 size, Color color, Rect2 region, bool mirror = false)
    {
        AddBasis(position, size, color, 1, 0, 0, 1);
        var offset = (Count - 1) * 16;
        buffer[offset + 12] = mirror ? region.End.X : region.Position.X;
        buffer[offset + 13] = region.Position.Y;
        buffer[offset + 14] = mirror ? -region.Size.X : region.Size.X;
        buffer[offset + 15] = region.Size.Y;
    }

    private void AddBasis(Vector2 position, Vector2 size, Color color, float cosine, float sine, int frame, int frameCount)
    {
        if (fixedSize > 0) throw new InvalidOperationException("Fixed-size batches accept position-only instances");
        EnsureCapacity();
        var offset = Count++ * 16;
        buffer[offset] = cosine * size.X;
        buffer[offset + 1] = -sine * size.Y;
        buffer[offset + 3] = position.X;
        buffer[offset + 4] = sine * size.X;
        buffer[offset + 5] = cosine * size.Y;
        buffer[offset + 7] = position.Y;
        var extentX = (MathF.Abs(buffer[offset]) + MathF.Abs(buffer[offset + 1])) * 0.5f;
        var extentY = (MathF.Abs(buffer[offset + 4]) + MathF.Abs(buffer[offset + 5])) * 0.5f;
        minimumX = Count == 1 ? position.X - extentX : MathF.Min(minimumX, position.X - extentX);
        minimumY = Count == 1 ? position.Y - extentY : MathF.Min(minimumY, position.Y - extentY);
        maximumX = Count == 1 ? position.X + extentX : MathF.Max(maximumX, position.X + extentX);
        maximumY = Count == 1 ? position.Y + extentY : MathF.Max(maximumY, position.Y + extentY);
        buffer[offset + 8] = color.R;
        buffer[offset + 9] = color.G;
        buffer[offset + 10] = color.B;
        buffer[offset + 11] = color.A;
        buffer[offset + 12] = (float)frame / frameCount;
        buffer[offset + 13] = 0;
        buffer[offset + 14] = 1f / frameCount;
        buffer[offset + 15] = 1;
    }

    public void Submit()
    {
        Multimesh.VisibleInstanceCount = Count;
        if (Count > 0)
        {
            Multimesh.CustomAabb = new(new(minimumX, minimumY, -0.5f), new(maximumX - minimumX, maximumY - minimumY, 1));
            Multimesh.Buffer = buffer;
        }
    }
}

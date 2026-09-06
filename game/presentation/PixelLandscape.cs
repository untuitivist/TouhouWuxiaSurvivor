using Godot;

namespace Rebirth.Presentation;

public static class PixelLandscape
{
    public static Texture2D Create()
    {
        using var image = Image.CreateEmpty(320, 180, false, Image.Format.Rgba8);
        image.Fill(new("3c485f"));
        Block(image, 0, 52, 320, 40, "5d6175");
        Block(image, 0, 92, 320, 43, "8a7780");
        Block(image, 0, 135, 320, 45, "35493f");
        Disk(image, 255, 48, 23, "697180");
        Disk(image, 255, 48, 20, "eeddb1");
        Disk(image, 263, 42, 5, "e1c99b");
        Disk(image, 248, 53, 3, "e1c99b");
        for (var star = 0; star < 40; star++)
        {
            var horizontal = (star * 67 + 17) % 320;
            var vertical = (star * 23 + 9) % 88;
            Block(image, horizontal, vertical, star % 7 == 0 ? 2 : 1, 1, "d5c59d");
        }
        for (var layer = 0; layer < 3; layer++)
            for (var horizontal = 0; horizontal < 320; horizontal += 4)
            {
                var vertical = 96 + layer * 18 + (int)(Math.Sin(horizontal * 0.027 + layer * 1.6) * 10 + Math.Sin(horizontal * 0.085) * 4);
                Block(image, horizontal, vertical, 4, 180 - vertical, new[] { "606573", "48585b", "304c48" }[layer]);
            }
        for (var step = 0; step < 40; step++)
        {
            var vertical = 126 + step;
            var center = 244 + (int)(Math.Sin(step * 0.08) * 11);
            var width = 12 + step;
            Block(image, center - width / 2, vertical, width, 1, step % 6 == 0 ? "61736a" : "8c9380");
        }
        Tree(image, 310, 145, true);
        Tree(image, 179, 153, true);
        Tree(image, 199, 116, false);
        Tree(image, 295, 117, false);
        Block(image, 218, 83, 7, 52, "572f32");
        Block(image, 272, 83, 7, 52, "572f32");
        Block(image, 219, 83, 3, 49, "b55b49");
        Block(image, 273, 83, 3, 49, "b55b49");
        Block(image, 209, 80, 80, 5, "3d3038");
        Block(image, 214, 85, 70, 5, "b75e4a");
        Block(image, 207, 78, 5, 3, "3d3038");
        Block(image, 286, 78, 5, 3, "3d3038");
        Block(image, 214, 98, 70, 4, "914d40");
        Block(image, 244, 89, 9, 16, "664039");
        Block(image, 246, 91, 5, 11, "cfad64");
        for (var paper = 0; paper < 5; paper++)
        {
            var horizontal = 228 + paper * 9;
            Block(image, horizontal, 110, 3, 4, "f0ddb8");
            Block(image, horizontal + 1, 113, 3, 3, "c9c9ab");
        }
        Lantern(image, 206, 128);
        Lantern(image, 290, 141);
        for (var tuft = 0; tuft < 66; tuft++)
        {
            var horizontal = (tuft * 71) % 320;
            var vertical = 150 + tuft * 17 % 30;
            Block(image, horizontal, vertical, 3, 1, tuft % 3 == 0 ? "658066" : "405d49");
        }
        Block(image, 0, 174, 320, 6, "273c37");
        return ImageTexture.CreateFromImage(image);
    }

    private static void Tree(Image image, int horizontal, int vertical, bool blossom)
    {
        Block(image, horizontal - 2, vertical - 37, 5, 41, "493c3b");
        Block(image, horizontal - 9, vertical - 23, 7, 3, "493c3b");
        for (var crown = 0; crown < 6; crown++)
        {
            var offset = new Vector2I((crown % 3 - 1) * 11, -(crown / 3) * 10);
            var shade = blossom ? "925d71" : "34534b";
            var light = blossom ? "c98991" : "59715b";
            Disk(image, horizontal + offset.X, vertical - 35 + offset.Y, 13, shade);
            Block(image, horizontal + offset.X - 8, vertical - 43 + offset.Y, 12, 3, light);
            Block(image, horizontal + offset.X - 4, vertical - 46 + offset.Y, 7, 3, light);
        }
    }

    private static void Lantern(Image image, int horizontal, int vertical)
    {
        Disk(image, horizontal, vertical - 16, 12, "5e6252");
        Block(image, horizontal - 2, vertical - 21, 4, 25, "433b34");
        Block(image, horizontal - 6, vertical - 26, 12, 3, "3e3537");
        Block(image, horizontal - 5, vertical - 23, 10, 13, "bd7849");
        Block(image, horizontal - 3, vertical - 22, 6, 10, "f7d892");
        Block(image, horizontal - 6, vertical - 10, 12, 3, "3e3537");
        Block(image, horizontal - 8, vertical + 3, 16, 3, "6d7869");
    }

    private static void Disk(Image image, int horizontal, int vertical, int radius, string color)
    {
        for (var row = -radius; row <= radius; row++)
        {
            var half = (int)Math.Sqrt(radius * radius - row * row);
            Block(image, horizontal - half, vertical + row, half * 2 + 1, 1, color);
        }
    }

    private static void Block(Image image, int horizontal, int vertical, int width, int height, string color)
    {
        var rectangle = new Rect2I(horizontal, vertical, width, height).Intersection(new(0, 0, image.GetWidth(), image.GetHeight()));
        if (rectangle.Size.X > 0 && rectangle.Size.Y > 0) image.FillRect(rectangle, new(color));
    }
}

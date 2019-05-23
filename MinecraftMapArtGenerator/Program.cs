using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace MinecraftMapArtGenerator
{
    class Program
    {
        private static readonly Dictionary<string, Color> MapColors = new Dictionary<string, Color>
        {
            { "minecraft:slime_block", Color.FromArgb(127, 178, 56) },
            { "minecraft:sandstone", Color.FromArgb(247, 233, 163) },
            { "minecraft:mushroom_stem", Color.FromArgb(199, 199, 199) },
            { "minecraft:redstone_block", Color.FromArgb(255, 0, 0) },
            { "minecraft:packed_ice", Color.FromArgb(160, 160, 255) },
            { "minecraft:iron_block", Color.FromArgb(167, 167, 167) },
            { "minecraft:oak_leaves", Color.FromArgb(0, 124, 0) },
            { "minecraft:white_wool", Color.FromArgb(255, 255, 255) },
            { "minecraft:clay", Color.FromArgb(164, 168, 184) },
            { "minecraft:jungle_wood", Color.FromArgb(151, 109, 77) },
            { "minecraft:cobblestone", Color.FromArgb(112, 112, 112) },
            //{ "minecraft:water", Color.FromArgb(64, 64, 255) },
            { "minecraft:oak_wood", Color.FromArgb(143, 119, 72) },
            { "minecraft:quartz_block", Color.FromArgb(255, 252, 245) },
            { "minecraft:acacia_planks", Color.FromArgb(216, 127, 51) },
            { "minecraft:purpur_block", Color.FromArgb(178, 76, 216) },
            { "minecraft:light_blue_wool", Color.FromArgb(102, 153, 216) },
            { "minecraft:hay_block", Color.FromArgb(229, 229, 51) },
            { "minecraft:lime_wool", Color.FromArgb(127, 204, 25) },
            { "minecraft:pink_wool", Color.FromArgb(242, 127, 165) },
            { "minecraft:gray_wool", Color.FromArgb(65, 65, 65) },
            { "minecraft:light_gray_wool", Color.FromArgb(153, 153, 153) },
            { "minecraft:cyan_wool", Color.FromArgb(76, 127, 153) },
            { "minecraft:purple_wool", Color.FromArgb(127, 63, 178) },
            { "minecraft:blue_wool", Color.FromArgb(51, 76, 178) },
            { "minecraft:dark_oak_wood", Color.FromArgb(102, 76, 51) },
            { "minecraft:green_wool", Color.FromArgb(102, 127, 51) },
            { "minecraft:bricks", Color.FromArgb(153, 51, 51) },
            { "minecraft:black_wool", Color.FromArgb(21, 21, 21) },
            { "minecraft:gold_block", Color.FromArgb(250, 238, 77) },
            { "minecraft:diamond_block", Color.FromArgb(92, 219, 213) },
            { "minecraft:lapis_block", Color.FromArgb(74, 128, 255) },
            { "minecraft:emerald_block", Color.FromArgb(0, 217, 58) },
            { "minecraft:spruce_wood", Color.FromArgb(129, 86, 49) },
            { "minecraft:netherrack", Color.FromArgb(112, 2, 0) },
            { "minecraft:white_terracotta", Color.FromArgb(209, 177, 161) },
            { "minecraft:orange_terracotta", Color.FromArgb(159, 82, 36) },
            { "minecraft:magenta_terracotta", Color.FromArgb(149, 87, 108) },
            { "minecraft:light_blue_terracotta", Color.FromArgb(112, 108, 138) },
            { "minecraft:yellow_terracotta", Color.FromArgb(186, 133, 36) },
            { "minecraft:lime_terracotta", Color.FromArgb(103, 117, 53) },
            { "minecraft:pink_terracotta", Color.FromArgb(160, 77, 78) },
            { "minecraft:gray_terracotta", Color.FromArgb(57, 41, 35) },
            { "minecraft:light_gray_terracotta", Color.FromArgb(135, 107, 98)},
            { "minecraft:cyan_terracotta", Color.FromArgb(87, 92, 92) },
            { "minecraft:purple_terracotta", Color.FromArgb(122, 73, 88) },
            { "minecraft:blue_terracotta", Color.FromArgb(76, 62, 92) },
            { "minecraft:brown_terracotta", Color.FromArgb(76, 50, 35) },
            { "minecraft:green_terracotta", Color.FromArgb(76, 82, 42) },
            { "minecraft:red_terracotta", Color.FromArgb(142, 60, 46) },
            { "minecraft:black_terracotta", Color.FromArgb(37, 22, 16) },
        };

        // distance in RGB space
        private static int ColorDiff(Color c1, Color c2)
        {
            return (int)Math.Sqrt((c1.R - c2.R) * (c1.R - c2.R)
                                  + (c1.G - c2.G) * (c1.G - c2.G)
                                  + (c1.B - c2.B) * (c1.B - c2.B));
        }

        private static double ColorDistance(Color c1, Color c2)
        {
            int red1 = c1.R;
            int red2 = c2.R;
            int rmean = (red1 + red2) >> 1;
            int r = red1 - red2;
            int g = c1.G - c2.G;
            int b = c1.B - c2.B;
            return Math.Sqrt((((512 + rmean) * r * r) >> 8) + 4 * g * g + (((767 - rmean) * b * b) >> 8));
        }

        private static Color ClosestColor(Dictionary<Color, string> colors, Color target)
        {
            Color? lowest = null;
            int diff = Int32.MaxValue;

            foreach (var color in colors.Keys)
            {
                int d = ColorDiff(color, target);
                if (d < diff)
                {
                    lowest = color;
                    diff = d;
                }
            }

            return lowest.Value;
        }

        private static Color ClosestColor2(Dictionary<Color, string> colors, Color target)
        {
            Color? lowest = null;
            double diff = Int32.MaxValue;

            foreach (var color in colors.Keys)
            {
                double d = ColorDistance(color, target);
                if (d < diff)
                {
                    lowest = color;
                    diff = d;
                }
            }

            return lowest.Value;
        }

        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine($"Expected 1 argument, got {args.Length}.");
                return 1;
            }

            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine($"Input file \"{args[0]}\" does not exist.");
                return 1;
            }

            var actualColors = new Dictionary<Color, string>();

            foreach (var mapColor in MapColors)
            {
                actualColors.Add(mapColor.Value, mapColor.Key);
            }



            var inputBitmap = new Bitmap(args[0]);

            var outputBitmap = new Bitmap(inputBitmap.Width, inputBitmap.Height);

            for (int x = 0; x < inputBitmap.Width; x++)
            {
                for (int y = 0; y < inputBitmap.Height; y++)
                {
                    Color color = ClosestColor(actualColors, inputBitmap.GetPixel(x, y));

                    outputBitmap.SetPixel(x, y, color);
                }
            }

            outputBitmap.Save("First.png", ImageFormat.Png);

            for (int x = 0; x < inputBitmap.Width; x++)
            {
                for (int y = 0; y < inputBitmap.Height; y++)
                {
                    Color color = ClosestColor2(actualColors, inputBitmap.GetPixel(x, y));

                    outputBitmap.SetPixel(x, y, color);
                }
            }

            outputBitmap.Save("Second.png", ImageFormat.Png);

            var mostColorDict = new Dictionary<Color, int>();

            for (int x = 0; x < outputBitmap.Width; x++)
            {
                for (int y = 0; y < outputBitmap.Height; y++)
                {
                    Color color = outputBitmap.GetPixel(x, y);

                    if (!mostColorDict.ContainsKey(color))
                        mostColorDict.Add(color, 0);

                    mostColorDict[color] = mostColorDict[color] + 1;
                }
            }

            Color? color1 = null;
            int max = 0;
            foreach (var color in mostColorDict)
            {
                if (color.Value > max)
                {
                    max = color.Value;
                    color1 = color.Key;
                }
            }

            //Console.WriteLine($"Most used color is: {actualColors[color1.Value]}");
            //Console.WriteLine("Commands:\r\n");

            Console.WriteLine($"/fill ~ ~-1 ~ ~127 ~-1 ~127 {actualColors[color1.Value]}");

            for (int y = 0; y < outputBitmap.Height; y++)
            {
                for (int x = 0; x < outputBitmap.Width; x++)
                {
                    if (outputBitmap.GetPixel(x, y) == color1.Value) continue;

                    Color col = outputBitmap.GetPixel(x, y);

                    if (x < 127 && outputBitmap.GetPixel(x + 1, y) == col)
                    {
                        int originalX = x;
                        x += 2;
                        int amount = 2;
                        while (x < 127 && outputBitmap.GetPixel(x, y) == col)
                        {
                            amount++;
                            x++;
                        }

                        x--;
                        Console.WriteLine($"/fill ~{originalX} ~-1 ~{y} ~{x} ~-1 ~{y} {actualColors[col]}");
                    }
                    else
                    {
                        Console.WriteLine($"/setblock ~{x} ~-1 ~{y} {actualColors[outputBitmap.GetPixel(x, y)]}");
                    }
                }
            }

            //outputBitmap.Save(args[1], ImageFormat.Png);

            return 0;
        }
    }
}

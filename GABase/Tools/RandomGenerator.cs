using System;
using System.Drawing;

namespace GABase
{
    public static class RandomGenerator
    {
        private static readonly Random random = new Random();

        public static Color GetRandomColor()
        {
            int a = random.Next(255);
            int r = random.Next(255);
            int g = random.Next(255);
            int b = random.Next(255);
            return Color.FromArgb(Settings.UseARGB ? a : 255, r, g, b);
        }

        public static Color ChangeColor(Color color)
        {
            int a = color.A;
            int r = color.R;
            int g = color.G;
            int b = color.B;
            int randomInt = GetRandomInt(Settings.UseARGB ? 4 : 3);
            switch (randomInt)
            {
                case 0:
                    r += GetRandomInt(50) - 25;
                    if (r < 0)
                        r = 255 + r;
                    else if (r > 255)
                        r = r - 255;
                    break;
                case 1:
                    g += GetRandomInt(50) - 25;
                    if (g < 0)
                        g = 255 + g;
                    else if (g > 255)
                        g = g - 255;
                    break;
                case 2:
                    b += GetRandomInt(50) - 25;
                    if (b < 0)
                        b = 255 + b;
                    else if (b > 255)
                        b = b - 255;
                    break;
                case 3:
                    a+= GetRandomInt(50) - 25;
                    if (a < 0)
                        a = 255 + a;
                    else if (a > 255)
                        a = a - 255;
                    break;
            }
            return Color.FromArgb(a, r, g, b);
        }

        public static Color ChangeColorWithAlpha(Color color)
        {
            int a = color.A;
            int r = color.R;
            int g = color.G;
            int b = color.B;
            int randomInt = GetRandomInt(4);
            switch (randomInt)
            {
                case 0:
                    r += GetRandomInt(50) - 25;
                    if (r < 0)
                        r = 255 + r;
                    else if (r > 255)
                        r = r - 255;
                    break;
                case 1:
                    g += GetRandomInt(50) - 25;
                    if (g < 0)
                        g = 255 + g;
                    else if (g > 255)
                        g = g - 255;
                    break;
                case 2:
                    b += GetRandomInt(50) - 25;
                    if (b < 0)
                        b = 255 + b;
                    else if (b > 255)
                        b = b - 255;
                    break;
                case 3:
                    a += GetRandomInt(50) - 25;
                    if (a < 0)
                        a = 255 + a;
                    else if (a > 255)
                        a = a - 255;
                    break;
            }
            return Color.FromArgb(a, r, g, b);
        }

        public static int GetRandomInt(int max)
        {
            return random.Next(max);
        }

        public static long GetRandomLong(long max)
        {
            long randomValue = 0;

            while (max > Int32.MaxValue)
            {
                randomValue += GetRandomInt(Int32.MaxValue);
                max -= Int32.MaxValue;
            }
            randomValue += GetRandomInt((int)max);

            return randomValue;
        }
    }
}

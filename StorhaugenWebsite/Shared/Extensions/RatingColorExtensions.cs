using System;
using System.Globalization;

namespace StorhaugenWebsite.Shared.Extensions
{
    public static class RatingColorExtensions
    {
        // Default 0..10 rating scale
        public static string ToRatingColorHex(this double score, double min = 0, double max = 10)
        {
            var x = Clamp(score, min, max);

            var stops = new (double pos, string hex)[]
            {
                (0.0,  "#6A1B9A"),
                (5.0,  "#F44336"),
                (8.2,  "#4CAF50"),
                (9.4,  "#4CAF50"),
                (10.0, "#2196F3"),
            };

            var t = (x - min) / (max - min);
            var v = t * 10.0;

            for (int i = 0; i < stops.Length - 1; i++)
            {
                var (p0, c0) = stops[i];
                var (p1, c1) = stops[i + 1];

                if (v <= p1)
                {
                    var segT = (v - p0) / (p1 - p0);
                    segT = Clamp(segT, 0, 1);
                    return LerpHex(c0, c1, segT);
                }
            }

            return stops[^1].hex;
        }

        // NEW: semi-transparent background color for pills/cards etc.
        public static string ToRatingBgRgba(this double score, double alpha = 0.14, double min = 0, double max = 10)
        {
            alpha = Clamp(alpha, 0, 1);
            var hex = score.ToRatingColorHex(min, max);
            var (r, g, b) = ParseHex(hex);

            // InvariantCulture so you don’t get "0,14" on Norwegian locales
            var a = alpha.ToString(CultureInfo.InvariantCulture);
            return $"rgba({r}, {g}, {b}, {a})";
        }

        // Convenience overloads for decimal scores (your MemberRatings are decimal?)
        public static string ToRatingColorHex(this decimal score, double min = 0, double max = 10)
            => ((double)score).ToRatingColorHex(min, max);

        public static string ToRatingBgRgba(this decimal score, double alpha = 0.14, double min = 0, double max = 10)
            => ((double)score).ToRatingBgRgba(alpha, min, max);

        private static string LerpHex(string a, string b, double t)
        {
            var (ar, ag, ab) = ParseHex(a);
            var (br, bg, bb) = ParseHex(b);

            int r = (int)Math.Round(ar + (br - ar) * t);
            int g = (int)Math.Round(ag + (bg - ag) * t);
            int bl = (int)Math.Round(ab + (bb - ab) * t);

            return $"#{r:X2}{g:X2}{bl:X2}";
        }

        private static (int r, int g, int b) ParseHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) throw new ArgumentException("Hex color is null/empty");
            if (hex[0] == '#') hex = hex[1..];
            if (hex.Length != 6) throw new ArgumentException("Hex color must be 6 digits (RRGGBB)");

            return (
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16)
            );
        }

        private static double Clamp(double v, double min, double max) => v < min ? min : (v > max ? max : v);
    }
}

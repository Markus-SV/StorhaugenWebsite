using System;

namespace StorhaugenWebsite.Shared.Extensions
{
    public static class RatingColorExtensions
    {
        // Default 0..10 rating scale
        public static string ToRatingColorHex(this double score, double min = 0, double max = 10)
        {
            // Clamp to range
            var x = Clamp(score, min, max);

            // Color stops (position, hex)
            // Tune these if you want different "where green starts" etc.
            var stops = new (double pos, string hex)[]
            {
                (0.0,  "#6A1B9A"), // purple (worst)
                (5.0,  "#F44336"), // red (below 5 fades to purple)
                (8.2,  "#4CAF50"), // green (good)
                (9.4,  "#4CAF50"), // keep green until "near 10"
                (10.0, "#2196F3"), // blue (best)
            };

            // Normalize x to 0..10 if min/max differ
            var t = (x - min) / (max - min);
            var v = t * 10.0;

            // Find segment
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace HtmlTransformer.Core.Base.Utils
{
    public static class HtmlParseUtils
    {
        public static Dictionary<string, string> GetStyleAttrs(string styleValue)
        {
            string styleStr = styleValue ?? "";
            styleStr = styleStr.Trim();

            string[] styleLines = styleStr.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var styleAttrs = new Dictionary<string, string>();

            foreach (string styleLine in styleLines)
            {
                string[] styleLineParts = styleLine.Split(new char[] { ':' }, 2);
                string styleKey = styleLineParts.Length >= 1 ? styleLineParts[0].Trim() : "";
                string styleVal = styleLineParts.Length >= 2 ? styleLineParts[1].Trim() : "";

                if (!string.IsNullOrEmpty(styleKey) && !string.IsNullOrEmpty(styleVal))
                {
                    styleAttrs[styleKey] = styleVal;
                }
            }
            return styleAttrs;
        }

        public static string ColorToHex(string colorStr)
        {
            colorStr = colorStr ?? "";
            colorStr = colorStr.Trim();

            // #RGB
            Match match = Regex.Match(colorStr, @"^#([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string r = match.Groups[1].Value;
                string g = match.Groups[2].Value;
                string b = match.Groups[3].Value;
                int red = Convert.ToInt32(r + r, 16);
                int green = Convert.ToInt32(g + g, 16);
                int blue = Convert.ToInt32(b + b, 16);
                return $"#{red:X2}{green:X2}{blue:X2}";
            }

            // #RGBA
            match = Regex.Match(colorStr, @"^#([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])([0-9a-fA-F])$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string r = match.Groups[1].Value;
                string g = match.Groups[2].Value;
                string b = match.Groups[3].Value;
                string a = match.Groups[4].Value;
                int red = Convert.ToInt32(r + r, 16);
                int green = Convert.ToInt32(g + g, 16);
                int blue = Convert.ToInt32(b + b, 16);
                int alpha = Convert.ToInt32(a + a, 16);
                return $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
            }

            // #RRGGBB
            match = Regex.Match(colorStr, @"^#([0-9a-fA-F]{6})$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string hexColor = match.Groups[1].Value;
                int colorInt = Convert.ToInt32(hexColor, 16);
                Color color = Color.FromArgb(colorInt);
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }

            // #RRGGBBAA
            match = Regex.Match(colorStr, @"^#([0-9a-fA-F]{6})([0-9a-fA-F]{2})$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string hexColor = match.Groups[1].Value;
                string alphaStr = match.Groups[2].Value;
                int colorInt = Convert.ToInt32(hexColor, 16);
                Color color = Color.FromArgb(colorInt);
                int alpha = Convert.ToInt32(alphaStr, 16);
                return $"#{color.R:X2}{color.G:X2}{color.B:X2}{alpha:X2}";
            }

            // rgb(R, G, B)
            match = Regex.Match(colorStr, @"^rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                try
                {
                    int red = int.Parse(match.Groups[1].Value);
                    int green = int.Parse(match.Groups[2].Value);
                    int blue = int.Parse(match.Groups[3].Value);
                    return $"#{red:X2}{green:X2}{blue:X2}";
                }
                catch (Exception)
                {
                    return "";
                }
            }

            // rgba(R, G, B, A)
            match = Regex.Match(colorStr, @"^rgba\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*([01]|0?\.\d+)\s*\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                try
                {
                    int red = int.Parse(match.Groups[1].Value);
                    int green = int.Parse(match.Groups[2].Value);
                    int blue = int.Parse(match.Groups[3].Value);
                    double alphaDouble = double.Parse(match.Groups[4].Value);
                    int alpha = (int)(alphaDouble * 255) & 0xFF;
                    return $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
                }
                catch (Exception)
                {
                    return "";
                }
            }

            return "";
        }

        public static string SizeToNumber(string sizeStr)
        {
            if (string.IsNullOrWhiteSpace(sizeStr))
            {
                return "";
            }
            try
            {
                int sizeInt = int.Parse(sizeStr);
                string sizeNum = sizeInt.ToString();
                return sizeNum;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}

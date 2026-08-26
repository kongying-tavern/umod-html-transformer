using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HtmlTransformer.Base.Utils
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

            var patterns = new[]
            {
                // #RGB
                new {
                    Pattern = @"^#([0-9a-f])([0-9a-f])([0-9a-f])$",
                    Handler = new Func<Match, string>(match =>
                    {
                        string r = match.Groups[1].Value;
                        string g = match.Groups[2].Value;
                        string b = match.Groups[3].Value;
                        byte red = Convert.ToByte(r + r, 16);
                        byte green = Convert.ToByte(g + g, 16);
                        byte blue = Convert.ToByte(b + b, 16);
                        return $"#{red:X2}{green:X2}{blue:X2}";
                    })
                },
                // #RGBA
                new {
                    Pattern = @"^#([0-9a-f])([0-9a-f])([0-9a-f])([0-9a-f])$",
                    Handler = new Func<Match, string>(match =>
                    {
                        string r = match.Groups[1].Value;
                        string g = match.Groups[2].Value;
                        string b = match.Groups[3].Value;
                        string a = match.Groups[4].Value;
                        int red = Convert.ToByte(r + r, 16);
                        int green = Convert.ToByte(g + g, 16);
                        int blue = Convert.ToByte(b + b, 16);
                        int alpha = Convert.ToByte(a + a, 16);
                        return $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
                    })
                },
                // #RRGGBB
                new {
                    Pattern = @"^#([0-9a-f]{6})$",
                    Handler = new Func<Match, string>(match =>
                    {
                        string hexColor = match.Groups[1].Value;
                        int colorInt = Convert.ToInt32(hexColor, 16);
                        byte red = (byte)((colorInt >> 16) & 0xFF);
                        byte green = (byte)((colorInt >> 8) & 0xFF);
                        byte blue = (byte)(colorInt & 0xFF);
                        return $"#{red:X2}{green:X2}{blue:X2}";
                    })
                },
                // #RRGGBBAA
                new {
                    Pattern = @"^#([0-9a-f]{6})([0-9a-f]{2})$",
                    Handler = new Func<Match, string>(match =>
                    {
                        string hexColor = match.Groups[1].Value;
                        string alphaStr = match.Groups[2].Value;
                        int colorInt = Convert.ToInt32(hexColor, 16);
                        int alpha = Convert.ToByte(alphaStr, 16);
                        byte red = (byte)((colorInt >> 16) & 0xFF);
                        byte green = (byte)((colorInt >> 8) & 0xFF);
                        byte blue = (byte)(colorInt & 0xFF);
                        return $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
                    })
                },
                // rgb(R, G, B)
                new {
                    Pattern = @"^rgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$",
                    Handler = new Func<Match, string>(match =>
                    {
                        try
                        {
                            byte red = byte.Parse(match.Groups[1].Value);
                            byte green = byte.Parse(match.Groups[2].Value);
                            byte blue = byte.Parse(match.Groups[3].Value);
                            return $"#{red:X2}{green:X2}{blue:X2}";
                        }
                        catch (Exception)
                        {
                            return "";
                        }
                    })
                },
                // rgba(R, G, B, A)
                new {
                    Pattern = @"^rgba\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*([01]|0?\.\d+)\s*\)$",
                    Handler = new Func<Match, string>(match =>
                    {
                        try
                        {
                            byte red = byte.Parse(match.Groups[1].Value);
                            byte green = byte.Parse(match.Groups[2].Value);
                            byte blue = byte.Parse(match.Groups[3].Value);
                            double alphaDouble = double.Parse(match.Groups[4].Value);
                            byte alpha = Convert.ToByte((int)(alphaDouble * 255));
                            return $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
                        }
                        catch (Exception)
                        {
                            return "";
                        }
                    })
                },
                // rgba(R, G, B, A%)
                new {
                    Pattern = @"^rgba\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+|\d+\.\d+|.\d+)%\s*\)$",
                    Handler = new Func<Match, string>(match =>
                    {
                        try
                        {
                            byte red = byte.Parse(match.Groups[1].Value);
                            byte green = byte.Parse(match.Groups[2].Value);
                            byte blue = byte.Parse(match.Groups[3].Value);
                            double alphaDouble = double.Parse(match.Groups[4].Value);
                            byte alpha = Convert.ToByte((int)(alphaDouble * 255 / 100));
                            return $"#{red:X2}{green:X2}{blue:X2}{alpha:X2}";
                        }
                        catch (Exception)
                        {
                            return "";
                        }
                    })
                },
            };

            foreach (var patternConf in patterns)
            {
                if (string.IsNullOrEmpty(patternConf.Pattern) || patternConf.Handler == null)
                {
                    continue;
                }

                var pattern = new Regex(patternConf.Pattern, RegexOptions.IgnoreCase);
                var match = pattern.Match(colorStr);
                if (match.Success)
                {
                    return patternConf.Handler(match);
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

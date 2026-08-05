// Minimale Unity-Stubs, damit die geteilten Engine-Quellen (Rouge.Tcg.Engine)
// ohne Unity kompilieren. Nur was die Engine wirklich benutzt: Inspector-Attribute
// (wirkungslos), ScriptableObject als schlichte Basisklasse (der Host erzeugt
// Karten per `new` aus cards-full.json), Color/Color32 für FrameColor-Properties
// und Debug fürs Log.

using System;

namespace UnityEngine
{
    public class ScriptableObject { }

    public class Sprite { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class TextAreaAttribute : Attribute
    {
        public TextAreaAttribute() { }
        public TextAreaAttribute(int minLines, int maxLines) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
        public int order;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }

    public static class ColorUtility
    {
        public static string ToHtmlStringRGB(Color color) =>
            $"{(int)Math.Round(color.r * 255):X2}{(int)Math.Round(color.g * 255):X2}{(int)Math.Round(color.b * 255):X2}";
    }

    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Max(float a, float b) => Math.Max(a, b);
    }

    public static class Debug
    {
        public static void Log(object message) => Console.WriteLine($"[log] {message}");
        public static void Log(object message, object context) => Log(message);
        public static void LogWarning(object message) => Console.WriteLine($"[warn] {message}");
        public static void LogWarning(object message, object context) => LogWarning(message);
        public static void LogError(object message) => Console.Error.WriteLine($"[error] {message}");
        public static void LogError(object message, object context) => LogError(message);
    }
}

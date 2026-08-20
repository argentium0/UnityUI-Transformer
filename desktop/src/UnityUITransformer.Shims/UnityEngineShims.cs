using System;
using System.Globalization;

namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object? message)
        {
            ShimLogSink.RaiseLog(ShimLogLevel.Info, message?.ToString() ?? string.Empty);
        }

        public static void LogWarning(object? message)
        {
            ShimLogSink.RaiseLog(ShimLogLevel.Warning, message?.ToString() ?? string.Empty);
        }

        public static void LogError(object? message)
        {
            ShimLogSink.RaiseLog(ShimLogLevel.Error, message?.ToString() ?? string.Empty);
        }
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2 zero => new Vector2(0f, 0f);
        public static Vector2 one => new Vector2(1f, 1f);

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F1}, {1:F1})", x, y);
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z = 0f)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one => new Vector3(1f, 1f, 1f);

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F1}, {1:F1}, {2:F1})", x, y, z);
        }
    }

    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static Vector4 zero => new Vector4(0f, 0f, 0f, 0f);
        public static Vector4 one => new Vector4(1f, 1f, 1f, 1f);

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:F1}, {1:F1}, {2:F1}, {3:F1})", x, y, z, w);
        }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color red => new Color(1f, 0f, 0f, 1f);
        public static Color green => new Color(0f, 1f, 0f, 1f);
        public static Color blue => new Color(0f, 0f, 1f, 1f);
        public static Color white => new Color(1f, 1f, 1f, 1f);
        public static Color black => new Color(0f, 0f, 0f, 1f);
        public static Color clear => new Color(0f, 0f, 0f, 0f);

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "RGBA({0:F3}, {1:F3}, {2:F3}, {3:F3})", r, g, b, a);
        }
    }

    public struct Rect
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float xMin => x;
        public float xMax => x + width;
        public float yMin => y;
        public float yMax => y + height;

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "(x:{0:F2}, y:{1:F2}, width:{2:F2}, height:{3:F2})", x, y, width, height);
        }
    }

    public struct Bounds
    {
        public Vector3 center;
        public Vector3 size;

        public Bounds(Vector3 center, Vector3 size)
        {
            this.center = center;
            this.size = size;
        }

        public Vector3 min => new Vector3(center.x - size.x / 2f, center.y - size.y / 2f, center.z - size.z / 2f);
        public Vector3 max => new Vector3(center.x + size.x / 2f, center.y + size.y / 2f, center.z + size.z / 2f);
    }
}

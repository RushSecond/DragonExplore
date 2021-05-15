using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathHelpers {

    public static float FindAverage(float[] list)
    {
        float returnF = 0;

        for (int i = 0; i < list.Length; i++)
        {
            returnF += list[i];
        }

        returnF /= list.Length;
        return returnF;
    }

    /// <summary>
    /// Geometrically interpolates between a and b by t.
    /// </summary>
    public static float Gerp(float a, float b, float t)
    {
        return Mathf.Pow(a, 1- t) * Mathf.Pow(b, t);
    }

    public static float MinusXSquaredCurve(float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        return 1f - (t - 1) * (t - 1);
    }

    /// <summary>
    /// Instert a value between 0 and 1, returns values closer to 0 or 1
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public static float SCurve(float t)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        return (-2f * Mathf.Pow(t, 3) + 3f * t * t); // -2t^3 + 3t^2
    }

    /// <summary>
    /// Adjusts timescale based on time, to better follow objects
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static float RotateTimeAdjust(float input)
    {
        float timeScale = Time.deltaTime * 60f;
        return timeScale / ((1 - input) / input + timeScale);
    }

    /// <summary>
    /// Check if two vectors are aligned in the same direction
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static bool AreVectorsAligned(Vector3 first, Vector3 second)
    {
        return (Vector3.Dot(first, second) > 0f);
    }

    static int[] perm = {151,160,137,91,90,15,
  131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
  190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
  88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
  77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
  102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
  135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
  5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
  223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
  129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
  251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
  49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
  138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
  151,160,137,91,90,15,
  131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
  190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
  88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
  77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
  102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
  135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
  5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
  223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
  129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
  251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
  49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
  138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };


    static float grad1(int hash, float x)
    {
        int h = hash & 15;
        float grad = 1.0f + (h & 7);  // Gradient value 1.0, 2.0, ..., 8.0
        if ((h & 8) > 0) grad = -grad;         // and a random sign for the gradient
        return (grad * x);           // Multiply the gradient with the distance
    }

    static float grad2(int hash, float x, float y)
    {
        int h = hash & 7;      // Convert low 3 bits of hash code
        float u = h < 4 ? x : y;  // into 8 simple gradient directions,
        float v = h < 4 ? y : x;  // and compute the dot product with (x,y).
        return ((h & 1) > 0 ? -u : u) + ((h & 2) > 0 ? -2.0f * v : 2.0f * v);
    }

    static float grad3(int hash, float x, float y, float z)
    {
        int h = hash & 15;     // Convert low 4 bits of hash code into 12 simple
        float u = h < 8 ? x : y; // gradient directions, and compute dot product.
        float v = h < 4 ? y : h == 12 || h == 14 ? x : z; // Fix repeats at h = 12 to 15
        return ((h & 1) > 0 ? -u : u) + ((h & 2) > 0 ? -v : v);
    }

    static float grad4(int hash, float x, float y, float z, float t)
    {
        int h = hash & 31;      // Convert low 5 bits of hash code into 32 simple
        float u = h < 24 ? x : y; // gradient directions, and compute dot product.
        float v = h < 16 ? y : z;
        float w = h < 8 ? z : t;
        return ((h & 1) > 0 ? -u : u) + ((h & 2) > 0 ? -v : v) + ((h & 4) > 0 ? -w : w);
    }

    public static float pnoise2(float x, float y, int px, int py)
    {
        int ix0, iy0, ix1, iy1;
        float fx0, fy0, fx1, fy1;
        float s, t, nx0, nx1, n0, n1;

        ix0 = (int)Mathf.Floor(x); // Integer part of x
        iy0 = (int)Mathf.Floor(y); // Integer part of y
        fx0 = x - ix0;        // Fractional part of x
        fy0 = y - iy0;        // Fractional part of y
        fx1 = fx0 - 1.0f;
        fy1 = fy0 - 1.0f;
        ix1 = ((ix0 + 1) % px) & 0xff;  // Wrap to 0..px-1 and wrap to 0..255
        iy1 = ((iy0 + 1) % py) & 0xff;  // Wrap to 0..py-1 and wrap to 0..255
        ix0 = (ix0 % px) & 0xff;
        iy0 = (iy0 % py) & 0xff;

        t = FADE(fy0);
        s = FADE(fx0);

        nx0 = grad2(perm[ix0 + perm[iy0]], fx0, fy0);
        nx1 = grad2(perm[ix0 + perm[iy1]], fx0, fy1);
        n0 = Mathf.Lerp(t, nx0, nx1);

        nx0 = grad2(perm[ix1 + perm[iy0]], fx1, fy0);
        nx1 = grad2(perm[ix1 + perm[iy1]], fx1, fy1);
        n1 = Mathf.Lerp(t, nx0, nx1);

        return 0.507f * (Mathf.Lerp(s, n0, n1));
    }

    static float FADE(float t)
    { return t * t * t * (t * (t * 6 - 15) + 10); }
}

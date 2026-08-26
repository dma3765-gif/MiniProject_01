using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorCore
{
    public static Color MakeColor(Color baseColor, float hueOffset, float saturationRate, float valueRate)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);

        h = Mathf.Repeat(h + hueOffset / 360f, 1f);
        s = Mathf.Clamp01(s * saturationRate);
        v = Mathf.Clamp01(v * valueRate);

        return Color.HSVToRGB(h, s, v);
    }

    public static (Color, Color, Color) GetBrightSoftDarkColors(Color baseColor)
    {
        Color brightColor = MakeColor(baseColor, -2.1f, 0.789f, 1.449f);
        Color softColor = MakeColor(baseColor, -0.8f, 0.393f, 1.206f);
        Color darkColor = MakeColor(baseColor, -1.3f, 0.933f, 0.782f);

        return (brightColor, softColor, darkColor);
    }

    public static void SetParticleColor(Transform root, string name, Color color)
    {
        Transform target = root.Find(name);

        if (target == null)
        {
            return;
        }

        ParticleSystem particle = target.GetComponent<ParticleSystem>();

        if (particle == null)
        {
            return;
        }

        ParticleSystem.MainModule main = particle.main;
        main.startColor = color;
    }

    public static void SetParticleGradientType01(Transform root, string path,  Color color1,  Color color2, Color color3)
    {
        Transform target = root.Find(path);

        if (target == null)
        {
            return;
        }

        ParticleSystem particle = target.GetComponent<ParticleSystem>();

        if (particle == null)
        {
            return;
        }

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particle.colorOverLifetime;

        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
            new GradientColorKey(color1, 0f),
            new GradientColorKey(color2, 0.35f),
            new GradientColorKey(color3, 0.7f),
            new GradientColorKey(Color.black, 1f)
            },
            new GradientAlphaKey[]
            {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 0.7f),
            new GradientAlphaKey(0f, 1f)
            });

        colorOverLifetime.color = gradient;
    }
}

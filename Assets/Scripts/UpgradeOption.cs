using System;
using UnityEngine;
public enum UpgradeKind { Stat, Element, Behavior, Trigger, Sigil }

public class UpgradeOption
{
    public string title;
    public string description;
    public UpgradeKind kind;
    public Action apply;
    public string glyphKey;
    public UpgradeOption(string title, string description, UpgradeKind kind, Action apply)
    {
        this.title = title;
        this.description = description;
        this.kind = kind;
        this.apply = apply;
    }
}

public static class SygilPalette
{
    public static readonly Color Cream = new Color32(0xFF, 0xF3, 0xDC, 0xFF);
    public static readonly Color Butter = new Color32(0xFF, 0xD2, 0x7A, 0xFF);
    public static readonly Color Coral = new Color32(0xFF, 0x8A, 0x7A, 0xFF);
    public static readonly Color Mint = new Color32(0x8F, 0xE3, 0xC0, 0xFF);
    public static readonly Color Lilac = new Color32(0xB7, 0x9C, 0xFF, 0xFF);
    public static readonly Color Sky = new Color32(0x8E, 0xC9, 0xFF, 0xFF);

    public static Color ForKind(UpgradeKind kind)
    {
        switch (kind)
        {
            case UpgradeKind.Stat: return Butter;
            case UpgradeKind.Element: return Coral;
            case UpgradeKind.Behavior: return Mint;
            case UpgradeKind.Sigil: return Sky;
            default: return Lilac;
        }
    }
}
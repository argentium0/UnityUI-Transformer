using System;
using System.Collections.Generic;
using UnityEngine;

namespace Figma2Unity.Tokens
{
    [Serializable]
    public class ColorTokenEntry
    {
        public string id;
        public string name;
        public Color color;
        public string hex;
        public string description;
    }

    [CreateAssetMenu(fileName = "ColorPalette", menuName = "Figma2Unity/Tokens/ColorPalette")]
    public class ColorPaletteSO : ScriptableObject
    {
        public List<ColorTokenEntry> colors = new List<ColorTokenEntry>();

        private Dictionary<string, ColorTokenEntry> _lookupDict;

        public Dictionary<string, ColorTokenEntry> GetLookupDictionary()
        {
            if (_lookupDict == null)
            {
                _lookupDict = new Dictionary<string, ColorTokenEntry>();
                foreach (var entry in colors)
                {
                    if (!string.IsNullOrEmpty(entry.id) && !_lookupDict.ContainsKey(entry.id))
                    {
                        _lookupDict[entry.id] = entry;
                    }
                }
            }
            return _lookupDict;
        }

        public bool TryGetToken(string tokenId, out ColorTokenEntry entry)
        {
            return GetLookupDictionary().TryGetValue(tokenId, out entry);
        }
    }

    [Serializable]
    public class TypographyTokenEntry
    {
        public string id;
        public string name;
        public string fontFamily;
        public float fontSize;
        public float fontWeight;
        public float? lineHeight;
        public float? letterSpacing;
        public string textCase;
        public string textDecoration;
    }

    [CreateAssetMenu(fileName = "TypeRamp", menuName = "Figma2Unity/Tokens/TypeRamp")]
    public class TypeRampSO : ScriptableObject
    {
        public List<TypographyTokenEntry> typography = new List<TypographyTokenEntry>();

        private Dictionary<string, TypographyTokenEntry> _lookupDict;

        public Dictionary<string, TypographyTokenEntry> GetLookupDictionary()
        {
            if (_lookupDict == null)
            {
                _lookupDict = new Dictionary<string, TypographyTokenEntry>();
                foreach (var entry in typography)
                {
                    if (!string.IsNullOrEmpty(entry.id) && !_lookupDict.ContainsKey(entry.id))
                    {
                        _lookupDict[entry.id] = entry;
                    }
                }
            }
            return _lookupDict;
        }

        public bool TryGetToken(string tokenId, out TypographyTokenEntry entry)
        {
            return GetLookupDictionary().TryGetValue(tokenId, out entry);
        }
    }

    [Serializable]
    public class SpacingTokenEntry
    {
        public string id;
        public string name;
        public float value;
    }

    [CreateAssetMenu(fileName = "SpacingScale", menuName = "Figma2Unity/Tokens/SpacingScale")]
    public class SpacingScaleSO : ScriptableObject
    {
        public List<SpacingTokenEntry> spacing = new List<SpacingTokenEntry>();

        private Dictionary<string, SpacingTokenEntry> _lookupDict;

        public Dictionary<string, SpacingTokenEntry> GetLookupDictionary()
        {
            if (_lookupDict == null)
            {
                _lookupDict = new Dictionary<string, SpacingTokenEntry>();
                foreach (var entry in spacing)
                {
                    if (!string.IsNullOrEmpty(entry.id) && !_lookupDict.ContainsKey(entry.id))
                    {
                        _lookupDict[entry.id] = entry;
                    }
                }
            }
            return _lookupDict;
        }

        public bool TryGetToken(string tokenId, out SpacingTokenEntry entry)
        {
            return GetLookupDictionary().TryGetValue(tokenId, out entry);
        }
    }

    [Serializable]
    public class EffectTokenDataValue
    {
        public string type;
        public Color color;
        public Vector2 offset;
        public float radius;
        public float spread;
    }

    [Serializable]
    public class EffectTokenEntry
    {
        public string id;
        public string name;
        public List<EffectTokenDataValue> effects = new List<EffectTokenDataValue>();
    }

    [CreateAssetMenu(fileName = "EffectStyle", menuName = "Figma2Unity/Tokens/EffectStyle")]
    public class EffectStyleSO : ScriptableObject
    {
        public List<EffectTokenEntry> effects = new List<EffectTokenEntry>();

        private Dictionary<string, EffectTokenEntry> _lookupDict;

        public Dictionary<string, EffectTokenEntry> GetLookupDictionary()
        {
            if (_lookupDict == null)
            {
                _lookupDict = new Dictionary<string, EffectTokenEntry>();
                foreach (var entry in effects)
                {
                    if (!string.IsNullOrEmpty(entry.id) && !_lookupDict.ContainsKey(entry.id))
                    {
                        _lookupDict[entry.id] = entry;
                    }
                }
            }
            return _lookupDict;
        }

        public bool TryGetToken(string tokenId, out EffectTokenEntry entry)
        {
            return GetLookupDictionary().TryGetValue(tokenId, out entry);
        }
    }
}

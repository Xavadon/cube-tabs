using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Baking
{
    public enum AtlasAlphaMode
    {
        Opaque,
        Cutout,
        Transparent
    }

    public class TextureAtlasBuilder
    {
        private const int ColorTileSize = 8;
        private const int MortonBits = 16;

        private readonly List<Tile> _tiles = new();
        private readonly List<Tile> _packOrder = new();
        private int _size;

        public void Clear()
        {
            _tiles.Clear();
            _packOrder.Clear();
            _size = 0;
        }

        public int AddTile(Texture2D texture, Color color, AtlasAlphaMode mode, float cutoff)
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i].Matches(texture, color, mode, cutoff))
                {
                    return i;
                }
            }

            _tiles.Add(new Tile(texture, color, mode, cutoff, GetTileSize(texture)));
            return _tiles.Count - 1;
        }

        public Texture2D Build(string name)
        {
            Pack();

            var pixels = new Color32[_size * _size];

            foreach (Tile tile in _tiles)
            {
                Write(tile, pixels);
            }

            var atlas = new Texture2D(_size, _size, TextureFormat.RGBA32, true)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            atlas.SetPixels32(pixels);
            atlas.Apply(true, true);
            return atlas;
        }

        public Vector2 RemapUv(int tileIndex, Vector2 uv)
        {
            Tile tile = _tiles[tileIndex];

            if (tile.Texture == null)
            {
                float center = tile.Size * 0.5f;
                return new Vector2((tile.X + center) / _size, (tile.Y + center) / _size);
            }

            float u = Mathf.Clamp01(uv.x) * tile.Texture.width;
            float v = Mathf.Clamp01(uv.y) * tile.Texture.height;
            return new Vector2((tile.X + u) / _size, (tile.Y + v) / _size);
        }

        private void Pack()
        {
            _packOrder.Clear();
            _packOrder.AddRange(_tiles);
            _packOrder.Sort(CompareBySizeDescending);

            int area = 0;
            int maxSize = 0;

            foreach (Tile tile in _packOrder)
            {
                area += tile.Size * tile.Size;
                maxSize = Mathf.Max(maxSize, tile.Size);
            }

            _size = Mathf.Max(maxSize, Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(area))));

            int offset = 0;

            foreach (Tile tile in _packOrder)
            {
                tile.X = CompactBits(offset);
                tile.Y = CompactBits(offset >> 1);
                offset += tile.Size * tile.Size;
            }
        }

        private void Write(Tile tile, Color32[] pixels)
        {
            if (tile.Texture == null)
            {
                Color32 color = tile.Apply(new Color32(255, 255, 255, 255));

                for (int y = 0; y < tile.Size; y++)
                {
                    for (int x = 0; x < tile.Size; x++)
                    {
                        pixels[(tile.Y + y) * _size + tile.X + x] = color;
                    }
                }

                return;
            }

            Color32[] source = tile.Texture.GetPixels32();
            int width = tile.Texture.width;
            int height = tile.Texture.height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[(tile.Y + y) * _size + tile.X + x] = tile.Apply(source[y * width + x]);
                }
            }
        }

        private static int GetTileSize(Texture2D texture)
        {
            if (texture == null)
            {
                return ColorTileSize;
            }

            return Mathf.NextPowerOfTwo(Mathf.Max(texture.width, texture.height));
        }

        private static int CompareBySizeDescending(Tile a, Tile b)
        {
            return b.Size.CompareTo(a.Size);
        }

        private static int CompactBits(int value)
        {
            int result = 0;

            for (int bit = 0; bit < MortonBits; bit++)
            {
                result |= ((value >> (2 * bit)) & 1) << bit;
            }

            return result;
        }

        private class Tile
        {
            public readonly Texture2D Texture;
            public readonly int Size;
            public int X;
            public int Y;

            private readonly Color _color;
            private readonly AtlasAlphaMode _mode;
            private readonly float _cutoff;

            public Tile(Texture2D texture, Color color, AtlasAlphaMode mode, float cutoff, int size)
            {
                Texture = texture;
                Size = size;
                _color = color;
                _mode = mode;
                _cutoff = cutoff;
            }

            public bool Matches(Texture2D texture, Color color, AtlasAlphaMode mode, float cutoff)
            {
                return Texture == texture && _color == color && _mode == mode && Mathf.Approximately(_cutoff, cutoff);
            }

            public Color32 Apply(Color32 source)
            {
                var result = new Color32(
                    (byte)(source.r * _color.r),
                    (byte)(source.g * _color.g),
                    (byte)(source.b * _color.b),
                    255);

                float alpha = source.a / 255f * _color.a;

                if (_mode == AtlasAlphaMode.Cutout)
                {
                    if (alpha < _cutoff)
                    {
                        result.a = 0;
                    }
                }
                else if (_mode == AtlasAlphaMode.Transparent)
                {
                    result.a = (byte)(alpha * 255f);
                }

                return result;
            }
        }
    }
}

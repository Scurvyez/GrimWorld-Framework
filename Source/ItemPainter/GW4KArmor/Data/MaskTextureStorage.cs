﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

namespace GW4KArmor.Data
{
    public class MaskTextureStorage : IDisposable
    {
        private static readonly Dictionary<string, MaskTextureStorage> _cache = new();
        private readonly Dictionary<TextureID, Texture2D> _map = new();
        
        public string FormatString { get; }
        
        private MaskTextureStorage(string formatString)
        {
            FormatString = formatString;
        }

        public Texture2D GetTexture(in TextureID id)
        {
            Texture2D texture2D;
            bool flag = _map.TryGetValue(id, out texture2D);
            Texture2D result;
            
            if (flag)
            {
                result = texture2D;
            }
            else
            {
                string itemPath = id.MakeTexturePath(FormatString);
                Texture2D texture2D2 = ContentFinder<Texture2D>.Get(itemPath);
                _map.Add(id, texture2D2);
                result = texture2D2;
            }
            return result;
        }

        public void Dispose()
        {
            foreach (var keyValuePair in _map)
            {
                Object.Destroy(keyValuePair.Value);
            }
            _map.Clear();
        }

        public static MaskTextureStorage GetOrCreate(string formatString)
        {
            MaskTextureStorage maskTextureStorage;
            bool flag = _cache.TryGetValue(formatString, out maskTextureStorage);
            MaskTextureStorage result;
            
            if (flag)
            {
                result = maskTextureStorage;
            }
            else
            {
                maskTextureStorage = new MaskTextureStorage(formatString);
                _cache.Add(formatString, maskTextureStorage);
                result = maskTextureStorage;
            }
            return result;
        }
    }
}
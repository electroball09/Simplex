using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Simplex.Util
{
    public class SimplexDataCache
    {
        protected static JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        public static bool IndentJSON
        {
            get => serializerOptions.WriteIndented;
            set { if (value != IndentJSON) serializerOptions = new JsonSerializerOptions() { WriteIndented = value }; }
        }
        public static bool UseAsyncMethods { get; set; } = true;

        public string cacheDir { get; }
        public string cacheId { get; }
        public string cacheSubdir { get; }

        private Dictionary<string, SimplexDataCache> dataCaches { get; set; } = new Dictionary<string, SimplexDataCache>();

        protected SimplexDataCache(SimplexDataCache old)
        {
            cacheDir = old.cacheDir;
            cacheId = old.cacheId;
            cacheSubdir = old.cacheSubdir;
        }
        public SimplexDataCache(string dir, string id, string subdir)
        {
            cacheDir = dir;
            cacheId = id;
            cacheSubdir = subdir;
        }

        public Task<SimplexDataCache<T>> GetCache<T>(string cacheName = null) where T : new()
        {
            return Task.Run<SimplexDataCache<T>>
                (async () =>
                {
                    if (string.IsNullOrEmpty(cacheName))
                        cacheName = "default";

                    string key = MakeCacheName<T>(cacheId, cacheName);

                    var hasCache = dataCaches.TryGetValue(key, out var cache);
                    if (!hasCache)
                    {
                        var newCache = new SimplexDataCache<T>(this, cacheName);
                        await SimplexDataCache<T>.CacheIO.LoadCache(newCache);
                        dataCaches.Add(key, newCache);
                        return newCache;
                    }

                    return cache as SimplexDataCache<T>;
                });
        }

        public static string MakeCacheName<T>(string cacheId, string name)
        {
            return $"{cacheId}.{typeof(T).Name}.{name}.cache";
        }
    }

    public class SimplexDataCache<T> : SimplexDataCache where T : new()
    {
        public static class CacheIO
        {
            public static async Task SaveCache(SimplexDataCache<T> cache)
            {
                if (!cache.CacheFile.Directory.Exists)
                    Directory.CreateDirectory(cache.CacheFile.DirectoryName);
                using (FileStream fs = File.Open(cache.CacheFile.FullName, FileMode.Create))
                {
                    if (UseAsyncMethods)
                        await JsonSerializer.SerializeAsync<T>(fs, cache.Data, serializerOptions);
                    else
                    {
                        var json = JsonSerializer.Serialize<T>(cache.Data, serializerOptions);
                        using (StreamWriter sw = new StreamWriter(fs, leaveOpen: true))
                        {
                            sw.Write(json);
                        }
                    }
                }
            }

            public static async Task LoadCache(SimplexDataCache<T> cache)
            {
                if (!cache.CacheFile.Directory.Exists)
                    Directory.CreateDirectory(cache.CacheFile.DirectoryName);
                using (FileStream fs = File.Open(cache.CacheFile.FullName, FileMode.OpenOrCreate))
                {
                    try
                    {
                        if (UseAsyncMethods)
                            cache.Data = await JsonSerializer.DeserializeAsync<T>(fs, serializerOptions);
                        else
                        {
                            using (StreamReader sr = new StreamReader(fs, leaveOpen: true))
                            {
                                var json = sr.ReadToEnd();
                                cache.Data = JsonSerializer.Deserialize<T>(json, serializerOptions);
                            }
                        }
                    }
                    catch
                    {
                        cache.Data = new T();
                    }
                }
            }
        }

        public T Data { get; private set; }
        public string cacheName { get; }

        private FileInfo cacheFile;
        [JsonIgnore]
        public FileInfo CacheFile
        {
            get
            {
                if (cacheFile == null)
                    cacheFile = new FileInfo(Path.Combine(cacheDir, cacheSubdir, MakeCacheName<T>(cacheId, cacheName)));
                return cacheFile;
            }
        }

        internal SimplexDataCache(SimplexDataCache old, string name) : base(old)
        {
            cacheName = name;
        }

        public Task Modify(Action<T> action)
        {
            action(Data);
            return Task.Run(() => CacheIO.SaveCache(this));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EVEMakeMoney.Data
{
    public class TypeNameService
    {
        private Dictionary<long, string> _typeNames = new();
        private bool _loaded = false;

        public void Load(string filePath)
        {
            if (_loaded) return;

            if (!File.Exists(filePath))
                return;

            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                try
                {
                    var data = JObject.Parse(line);
                    if (data != null && data["_key"] != null && data["name"] != null)
                    {
                        long typeId = data["_key"]!.Value<long>();
                        var nameObj = data["name"] as JObject;
                        
                        string name = "Unknown";
                        if (nameObj != null)
                        {
                            if (nameObj["zh"] != null)
                                name = nameObj["zh"]!.ToString();
                            else if (nameObj["en"] != null)
                                name = nameObj["en"]!.ToString();
                            else
                            {
                                var firstProp = nameObj.Properties().FirstOrDefault();
                                if (firstProp != null)
                                    name = firstProp.Value!.ToString();
                            }
                        }
                        
                        _typeNames[typeId] = name;
                    }
                }
                catch
                {
                }
            }

            _loaded = true;
        }

        public string GetName(long typeId)
        {
            if (_typeNames.TryGetValue(typeId, out var name))
                return name;
            return $"Type_{typeId}";
        }

        public string GetNameWithId(long typeId)
        {
            var name = GetName(typeId);
            return $"{name} ({typeId})";
        }
    }
}

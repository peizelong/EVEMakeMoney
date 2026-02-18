using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using QuickType;

namespace EVEMakeMoney
{
    public class BlueprintLoader
    {
        public static List<Blueprint> LoadBlueprints(string filePath)
        {
            var blueprints = new List<Blueprint>();
            
            foreach (var line in File.ReadLines(filePath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var blueprint = Blueprint.FromJson(line);
                    blueprints.Add(blueprint);
                }
            }
            
            return blueprints;
        }
    }
}

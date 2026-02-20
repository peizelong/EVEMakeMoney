using System.Collections.Generic;
using System.IO;
using EVEMakeMoney.Api.Models;

namespace EVEMakeMoney.Api.Services
{
    public class BlueprintService
    {
        public List<Blueprint> LoadBlueprints(string filePath)
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

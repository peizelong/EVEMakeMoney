using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuickType;

namespace EVEMakeMoney.Data
{
    public class CostCalculationService
    {
        private readonly EVEMakeMoneyDbContext _db;

        public CostCalculationService(EVEMakeMoneyDbContext db)
        {
            _db = db;
        }

        public Dictionary<long, decimal> CalculateAllCosts(List<Blueprint> blueprints, int me = 0, int te = 0)
        {
            var productToBlueprint = new Dictionary<long, Blueprint>();
            var typeIdToMaterialBlueprints = new Dictionary<long, List<Blueprint>>();
            var t1ToT2Blueprints = new HashSet<long>();
            var manufacturedProductTypeIds = new HashSet<long>();

            foreach (var bp in blueprints)
            {
                if (bp.Activities?.Invention?.Products != null)
                {
                    foreach (var product in bp.Activities.Invention.Products)
                    {
                        t1ToT2Blueprints.Add(product.TypeId);
                    }
                }

                if (bp.Activities?.Manufacturing?.Products != null)
                {
                    foreach (var product in bp.Activities.Manufacturing.Products)
                    {
                        manufacturedProductTypeIds.Add(product.TypeId);
                    }
                }

                if (bp.Activities?.Reaction?.Products != null)
                {
                    foreach (var product in bp.Activities.Reaction.Products)
                    {
                        manufacturedProductTypeIds.Add(product.TypeId);
                    }
                }
            }

            var blueprintSettings = _db.Blueprints.ToDictionary(b => b.BlueprintTypeId, b => new { b.ME, b.TE });

            foreach (var bp in blueprints)
            {
                if (blueprintSettings.TryGetValue(bp.BlueprintTypeId, out var settings))
                {
                    bp.ME = settings.ME;
                    bp.TE = settings.TE;
                }
                else if (t1ToT2Blueprints.Contains(bp.BlueprintTypeId))
                {
                    bp.ME = 2;
                    bp.TE = 2;
                }
                else
                {
                    bp.ME = me;
                    bp.TE = te;
                }

                if (bp.Activities?.Manufacturing?.Products != null)
                {
                    foreach (var product in bp.Activities.Manufacturing.Products)
                    {
                        productToBlueprint[product.TypeId] = bp;

                        if (!typeIdToMaterialBlueprints.ContainsKey(product.TypeId))
                            typeIdToMaterialBlueprints[product.TypeId] = new List<Blueprint>();
                    }
                }

                if (bp.Activities?.Manufacturing?.Materials != null)
                {
                    foreach (var material in bp.Activities.Manufacturing.Materials)
                    {
                        if (!typeIdToMaterialBlueprints.ContainsKey(material.TypeId))
                            typeIdToMaterialBlueprints[material.TypeId] = new List<Blueprint>();
                        typeIdToMaterialBlueprints[material.TypeId].Add(bp);
                    }
                }

                if (bp.Activities?.Reaction?.Products != null)
                {
                    foreach (var product in bp.Activities.Reaction.Products)
                    {
                        productToBlueprint[product.TypeId] = bp;

                        if (!typeIdToMaterialBlueprints.ContainsKey(product.TypeId))
                            typeIdToMaterialBlueprints[product.TypeId] = new List<Blueprint>();
                    }
                }

                if (bp.Activities?.Reaction?.Materials != null)
                {
                    foreach (var material in bp.Activities.Reaction.Materials)
                    {
                        if (!typeIdToMaterialBlueprints.ContainsKey(material.TypeId))
                            typeIdToMaterialBlueprints[material.TypeId] = new List<Blueprint>();
                        typeIdToMaterialBlueprints[material.TypeId].Add(bp);
                    }
                }
            }

            var marketPrices = GetMarketPrices();
            var costs = new Dictionary<long, decimal>();
            var calculated = new HashSet<long>();

            var sortedBlueprints = TopologicalSort(blueprints, productToBlueprint, typeIdToMaterialBlueprints);

            foreach (var bp in sortedBlueprints)
            {
                var cost = CalculateBlueprintCost(bp, costs, marketPrices, productToBlueprint, calculated, t1ToT2Blueprints, manufacturedProductTypeIds);
                costs[bp.BlueprintTypeId] = cost;
                calculated.Add(bp.BlueprintTypeId);
            }

            return costs;
        }

        public Dictionary<long, decimal> CalculateAllTimes(List<Blueprint> blueprints, int me = 0, int te = 0)
        {
            var productToBlueprint = new Dictionary<long, Blueprint>();
            var typeIdToMaterialBlueprints = new Dictionary<long, List<Blueprint>>();
            var t1ToT2Blueprints = new HashSet<long>();
            var manufacturedProductTypeIds = new HashSet<long>();

            foreach (var bp in blueprints)
            {
                if (bp.Activities?.Invention?.Products != null)
                {
                    foreach (var product in bp.Activities.Invention.Products)
                    {
                        t1ToT2Blueprints.Add(product.TypeId);
                    }
                }

                if (bp.Activities?.Manufacturing?.Products != null)
                {
                    foreach (var product in bp.Activities.Manufacturing.Products)
                    {
                        manufacturedProductTypeIds.Add(product.TypeId);
                    }
                }

                if (bp.Activities?.Reaction?.Products != null)
                {
                    foreach (var product in bp.Activities.Reaction.Products)
                    {
                        manufacturedProductTypeIds.Add(product.TypeId);
                    }
                }
            }

            var blueprintSettings = _db.Blueprints.ToDictionary(b => b.BlueprintTypeId, b => new { b.ME, b.TE });

            foreach (var bp in blueprints)
            {
                if (blueprintSettings.TryGetValue(bp.BlueprintTypeId, out var settings))
                {
                    bp.ME = settings.ME;
                    bp.TE = settings.TE;
                }
                else if (t1ToT2Blueprints.Contains(bp.BlueprintTypeId))
                {
                    bp.ME = 2;
                    bp.TE = 2;
                }
                else
                {
                    bp.ME = me;
                    bp.TE = te;
                }

                if (bp.Activities?.Manufacturing?.Products != null)
                {
                    foreach (var product in bp.Activities.Manufacturing.Products)
                    {
                        productToBlueprint[product.TypeId] = bp;
                    }
                }

                if (bp.Activities?.Manufacturing?.Materials != null)
                {
                    foreach (var material in bp.Activities.Manufacturing.Materials)
                    {
                        if (!typeIdToMaterialBlueprints.ContainsKey(material.TypeId))
                            typeIdToMaterialBlueprints[material.TypeId] = new List<Blueprint>();
                        typeIdToMaterialBlueprints[material.TypeId].Add(bp);
                    }
                }

                if (bp.Activities?.Reaction?.Products != null)
                {
                    foreach (var product in bp.Activities.Reaction.Products)
                    {
                        productToBlueprint[product.TypeId] = bp;
                    }
                }

                if (bp.Activities?.Reaction?.Materials != null)
                {
                    foreach (var material in bp.Activities.Reaction.Materials)
                    {
                        if (!typeIdToMaterialBlueprints.ContainsKey(material.TypeId))
                            typeIdToMaterialBlueprints[material.TypeId] = new List<Blueprint>();
                        typeIdToMaterialBlueprints[material.TypeId].Add(bp);
                    }
                }
            }

            var times = new Dictionary<long, decimal>();
            var calculated = new HashSet<long>();

            var sortedBlueprints = TopologicalSort(blueprints, productToBlueprint, typeIdToMaterialBlueprints);

            foreach (var bp in sortedBlueprints)
            {
                var time = CalculateBlueprintTime(bp, times, productToBlueprint, calculated, t1ToT2Blueprints, manufacturedProductTypeIds);
                times[bp.BlueprintTypeId] = time;
                calculated.Add(bp.BlueprintTypeId);
            }

            return times;
        }

        private decimal CalculateBlueprintTime(
            Blueprint bp,
            Dictionary<long, decimal> calculatedTimes,
            Dictionary<long, Blueprint> productToBlueprint,
            HashSet<long> calculated,
            HashSet<long> t2Blueprints,
            HashSet<long> manufacturedProductTypeIds)
        {
            decimal totalTime = 0;

            if (calculated.Contains(bp.BlueprintTypeId))
            {
                return calculatedTimes.GetValueOrDefault(bp.BlueprintTypeId, 0);
            }

            calculated.Add(bp.BlueprintTypeId);

            decimal outputQuantity = 1;
            if (bp.Activities?.Manufacturing?.Products != null && bp.Activities.Manufacturing.Products.Length > 0)
            {
                outputQuantity = bp.Activities.Manufacturing.Products[0].Quantity;
            }
            else if (bp.Activities?.Reaction?.Products != null && bp.Activities.Reaction.Products.Length > 0)
            {
                outputQuantity = bp.Activities.Reaction.Products[0].Quantity;
            }

            decimal teFactor = 1.0m - (bp.TE * 0.01m);
            if (teFactor < 0.80m)
                teFactor = 0.80m;

            bool isT2Blueprint = t2Blueprints.Contains(bp.BlueprintTypeId);

            if (bp.Activities?.Manufacturing?.Materials != null)
            {
                foreach (var material in bp.Activities.Manufacturing.Materials)
                {
                    if (material == null || material.TypeId == 0)
                        continue;

                    decimal materialQuantity;
                    bool isManufacturedProduct = manufacturedProductTypeIds.Contains(material.TypeId);

                    if (isT2Blueprint && !isManufacturedProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity * (1.0m - (bp.ME * 0.01m));
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else if (isT2Blueprint && isManufacturedProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity;
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else
                    {
                        materialQuantity = material.Quantity * (1.0m - (bp.ME * 0.01m));
                        if (materialQuantity < 1 && material.Quantity > 0)
                            materialQuantity = 1;
                    }

                    decimal materialTime;

                    if (productToBlueprint.TryGetValue(material.TypeId, out var materialBp) && materialBp != null)
                    {
                        if (calculatedTimes.TryGetValue(materialBp.BlueprintTypeId, out var cachedTime))
                        {
                            materialTime = cachedTime * materialQuantity;
                        }
                        else
                        {
                            var subTime = CalculateBlueprintTime(materialBp, calculatedTimes, productToBlueprint, calculated, t2Blueprints, manufacturedProductTypeIds);
                            materialTime = subTime * materialQuantity;
                        }
                    }
                    else
                    {
                        materialTime = 0;
                    }

                    totalTime += materialTime;
                }
            }

            if (bp.Activities?.Reaction?.Materials != null)
            {
                foreach (var material in bp.Activities.Reaction.Materials)
                {
                    if (material == null || material.TypeId == 0)
                        continue;

                    decimal materialQuantity;
                    bool isManufacturedProduct = manufacturedProductTypeIds.Contains(material.TypeId);

                    if (isT2Blueprint && !isManufacturedProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity * (1.0m - (bp.ME * 0.01m));
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else if (isT2Blueprint && isManufacturedProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity;
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else
                    {
                        materialQuantity = material.Quantity * (1.0m - (bp.ME * 0.01m));
                        if (materialQuantity < 1 && material.Quantity > 0)
                            materialQuantity = 1;
                    }

                    decimal materialTime;

                    if (productToBlueprint.TryGetValue(material.TypeId, out var materialBp) && materialBp != null)
                    {
                        if (calculatedTimes.TryGetValue(materialBp.BlueprintTypeId, out var cachedTime))
                        {
                            materialTime = cachedTime * materialQuantity;
                        }
                        else
                        {
                            var subTime = CalculateBlueprintTime(materialBp, calculatedTimes, productToBlueprint, calculated, t2Blueprints, manufacturedProductTypeIds);
                            materialTime = subTime * materialQuantity;
                        }
                    }
                    else
                    {
                        materialTime = 0;
                    }

                    totalTime += materialTime;
                }
            }

            var bpTime = bp.Activities?.Manufacturing?.Time ?? bp.Activities?.Reaction?.Time ?? 0;
            var adjustedBpTime = bpTime * teFactor;

            var unitTime = outputQuantity > 0 ? (totalTime + adjustedBpTime) / outputQuantity : totalTime + adjustedBpTime;
            calculatedTimes[bp.BlueprintTypeId] = unitTime;
            return unitTime;
        }

        private List<Blueprint> TopologicalSort(
            List<Blueprint> blueprints,
            Dictionary<long, Blueprint> productToBlueprint,
            Dictionary<long, List<Blueprint>> typeIdToMaterialBlueprints)
        {
            var result = new List<Blueprint>();
            var visited = new HashSet<long>();
            var visiting = new HashSet<long>();

            void Visit(Blueprint bp)
            {
                if (visited.Contains(bp.BlueprintTypeId))
                    return;

                if (visiting.Contains(bp.BlueprintTypeId))
                    return;

                visiting.Add(bp.BlueprintTypeId);

                if (bp.Activities?.Manufacturing?.Materials != null)
                {
                    foreach (var material in bp.Activities.Manufacturing.Materials)
                    {
                        if (productToBlueprint.TryGetValue(material.TypeId, out var dependencyBp))
                        {
                            Visit(dependencyBp);
                        }
                    }
                }

                if (bp.Activities?.Reaction?.Materials != null)
                {
                    foreach (var material in bp.Activities.Reaction.Materials)
                    {
                        if (productToBlueprint.TryGetValue(material.TypeId, out var dependencyBp))
                        {
                            Visit(dependencyBp);
                        }
                    }
                }

                visiting.Remove(bp.BlueprintTypeId);
                visited.Add(bp.BlueprintTypeId);
                result.Add(bp);
            }

            foreach (var bp in blueprints)
            {
                Visit(bp);
            }

            return result;
        }

        private decimal CalculateBlueprintCost(
            Blueprint bp,
            Dictionary<long, decimal> calculatedCosts,
            Dictionary<long, double> marketPrices,
            Dictionary<long, Blueprint> productToBlueprint,
            HashSet<long> calculated,
            HashSet<long> t2Blueprints,
            HashSet<long> manufacturedProductTypeIds)
        {
            decimal totalCost = 0;

            if (calculated.Contains(bp.BlueprintTypeId))
            {
                return calculatedCosts.GetValueOrDefault(bp.BlueprintTypeId, 0);
            }

            calculated.Add(bp.BlueprintTypeId);

            decimal outputQuantity = 1;
            if (bp.Activities?.Manufacturing?.Products != null && bp.Activities.Manufacturing.Products.Length > 0)
            {
                outputQuantity = bp.Activities.Manufacturing.Products[0].Quantity;
            }
            else if (bp.Activities?.Reaction?.Products != null && bp.Activities.Reaction.Products.Length > 0)
            {
                outputQuantity = bp.Activities.Reaction.Products[0].Quantity;
            }

            decimal meFactor = 1.0m - (bp.ME * 0.01m);
            if (meFactor < 0.01m)
                meFactor = 0.01m;

            decimal teFactor = 1.0m - (bp.TE * 0.01m);
            if (teFactor < 0.80m)
                teFactor = 0.80m;

            bool isT2Blueprint = t2Blueprints.Contains(bp.BlueprintTypeId);

            if (bp.Activities?.Manufacturing?.Materials != null)
            {
                foreach (var material in bp.Activities.Manufacturing.Materials)
                {
                    if (material == null || material.TypeId == 0)
                        continue;

                    decimal materialQuantity;
                    bool isManufacturedProduct = manufacturedProductTypeIds.Contains(material.TypeId);

                    if (isT2Blueprint && !isManufacturedProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity * meFactor;
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else if (isT2Blueprint && isManufacturedProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity;
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else
                    {
                        materialQuantity = material.Quantity * meFactor;
                        if (materialQuantity < 1 && material.Quantity > 0)
                            materialQuantity = 1;
                    }

                    decimal materialCost;

                    if (productToBlueprint.TryGetValue(material.TypeId, out var materialBp) && materialBp != null)
                    {
                        if (calculatedCosts.TryGetValue(materialBp.BlueprintTypeId, out var cachedCost))
                        {
                            materialCost = cachedCost * materialQuantity;
                        }
                        else
                        {
                            var subCost = CalculateBlueprintCost(materialBp, calculatedCosts, marketPrices, productToBlueprint, calculated, t2Blueprints, manufacturedProductTypeIds);
                            materialCost = subCost * materialQuantity;
                        }
                    }
                    else
                    {
                        var marketPrice = marketPrices.GetValueOrDefault(material.TypeId, 0);
                        materialCost = (decimal)marketPrice * materialQuantity;
                    }

                    totalCost += materialCost;
                }
            }

            if (bp.Activities?.Reaction?.Materials != null)
            {
                foreach (var material in bp.Activities.Reaction.Materials)
                {
                    if (material == null || material.TypeId == 0)
                        continue;

                    decimal materialQuantity;
                    bool isManufacturedProduct = manufacturedProductTypeIds.Contains(material.TypeId);

                    if (isT2Blueprint && !isManufacturedProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity * meFactor;
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else if (isT2Blueprint && isManufacturedProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity;
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else
                    {
                        materialQuantity = material.Quantity * meFactor;
                        if (materialQuantity < 1 && material.Quantity > 0)
                            materialQuantity = 1;
                    }

                    decimal materialCost;

                    if (productToBlueprint.TryGetValue(material.TypeId, out var materialBp) && materialBp != null)
                    {
                        if (calculatedCosts.TryGetValue(materialBp.BlueprintTypeId, out var cachedCost))
                        {
                            materialCost = cachedCost * materialQuantity;
                        }
                        else
                        {
                            var subCost = CalculateBlueprintCost(materialBp, calculatedCosts, marketPrices, productToBlueprint, calculated, t2Blueprints, manufacturedProductTypeIds);
                            materialCost = subCost * materialQuantity;
                        }
                    }
                    else
                    {
                        var marketPrice = marketPrices.GetValueOrDefault(material.TypeId, 0);
                        materialCost = (decimal)marketPrice * materialQuantity;
                    }

                    totalCost += materialCost;
                }
            }

            var unitCost = outputQuantity > 0 ? totalCost / outputQuantity : totalCost;
            calculatedCosts[bp.BlueprintTypeId] = unitCost;
            return unitCost;
        }

        private Dictionary<long, double> GetMarketPrices()
        {
            var prices = new Dictionary<long, double>();

            var buyOrders = _db.MarketOrders
                .Where(x => x.IsBuyOrder == true)
                .GroupBy(x => x.TypeId)
                .Select(g => new
                {
                    TypeId = g.Key,
                    MaxPrice = g.Max(x => x.Price)
                })
                .ToList();

            foreach (var order in buyOrders)
            {
                prices[order.TypeId] = order.MaxPrice;
            }

            return prices;
        }
    }
}

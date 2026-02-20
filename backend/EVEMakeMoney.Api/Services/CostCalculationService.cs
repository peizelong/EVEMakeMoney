using System;
using System.Collections.Generic;
using System.Linq;
using EVEMakeMoney.Api.Data;
using EVEMakeMoney.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EVEMakeMoney.Api.Services
{
    public class CostCalculationService
    {
        private readonly EVEMakeMoneyDbContext _db;

        public CostCalculationService(EVEMakeMoneyDbContext db)
        {
            _db = db;
        }

        public Dictionary<long, (decimal Cost, decimal Time)> CalculateAllCostsAndTimes(
            List<Blueprint> blueprints, 
            int me = 0, int te = 0,
            decimal structureTimeBonus = 0, decimal rigTimeBonus = 0,
            int industryLevel = 0, int advancedIndustryLevel = 0,
            decimal reactionStructureTimeBonus = 0, decimal reactionRigTimeBonus = 0,
            int reactionLevel = 0)
        {
            var productToBlueprint = new Dictionary<long, Blueprint>();
            var typeIdToMaterialBlueprints = new Dictionary<long, List<Blueprint>>();
            var t2Blueprints = new HashSet<long>();
            var t2BlueprintToT1Product = new Dictionary<long, long>();
            var manufacturedProductTypeIds = new HashSet<long>();

            foreach (var bp in blueprints)
            {
                if (bp.Activities?.Invention?.Products != null)
                {
                    foreach (var product in bp.Activities.Invention.Products)
                    {
                        t2Blueprints.Add(product.TypeId);
                    }
                }

                if (bp.Activities?.Manufacturing?.Products != null)
                {
                    foreach (var product in bp.Activities.Manufacturing.Products)
                    {
                        manufacturedProductTypeIds.Add(product.TypeId);
                        if (bp.Activities?.Invention?.Products != null)
                        {
                            foreach (var t2BpProduct in bp.Activities.Invention.Products)
                            {
                                t2BlueprintToT1Product[t2BpProduct.TypeId] = product.TypeId;
                            }
                        }
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
                else if (t2Blueprints.Contains(bp.BlueprintTypeId))
                {
                    bp.ME = 2;
                    bp.TE = 2;
                }
                else
                {
                    bp.ME = me;
                    bp.TE = te;
                }

                bp.StructureTimeBonus = structureTimeBonus;
                bp.RigTimeBonus = rigTimeBonus;
                bp.IndustryLevel = industryLevel;
                bp.AdvancedIndustryLevel = advancedIndustryLevel;
                bp.ReactionStructureTimeBonus = reactionStructureTimeBonus;
                bp.ReactionRigTimeBonus = reactionRigTimeBonus;
                bp.ReactionLevel = reactionLevel;

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

            var marketPrices = GetMarketPrices();
            var results = new Dictionary<long, (decimal Cost, decimal Time)>();
            var calculatedCosts = new Dictionary<long, decimal>();
            var calculatedTimes = new Dictionary<long, decimal>();
            var calculated = new HashSet<long>();

            var sortedBlueprints = TopologicalSort(blueprints, productToBlueprint, typeIdToMaterialBlueprints);

            foreach (var bp in sortedBlueprints)
            {
                var cost = CalculateBlueprintCost(bp, calculatedCosts, marketPrices, productToBlueprint, calculated, t2Blueprints, t2BlueprintToT1Product, manufacturedProductTypeIds);
                var time = CalculateBlueprintTime(bp, calculatedTimes, productToBlueprint, calculated, t2Blueprints, t2BlueprintToT1Product, manufacturedProductTypeIds);
                
                bp.ManufacturingCost = cost;
                bp.ManufacturingTime = time;
                results[bp.BlueprintTypeId] = (cost, time);
                calculated.Add(bp.BlueprintTypeId);
            }

            return results;
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
            Dictionary<long, long> t2BlueprintToT1Product,
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

            bool isT2Blueprint = t2Blueprints.Contains(bp.BlueprintTypeId);
            long t1ProductTypeId = 0;
            if (isT2Blueprint)
            {
                t2BlueprintToT1Product.TryGetValue(bp.BlueprintTypeId, out t1ProductTypeId);
            }

            if (bp.Activities?.Manufacturing?.Materials != null)
            {
                foreach (var material in bp.Activities.Manufacturing.Materials)
                {
                    if (material == null || material.TypeId == 0)
                        continue;

                    decimal materialQuantity;
                    bool isT1PrototypeProduct = (material.TypeId == t1ProductTypeId);

                    if (isT1PrototypeProduct)
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
                            var subCost = CalculateBlueprintCost(materialBp, calculatedCosts, marketPrices, productToBlueprint, calculated, t2Blueprints, t2BlueprintToT1Product, manufacturedProductTypeIds);
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
                    bool isT1PrototypeProduct = (material.TypeId == t1ProductTypeId);

                    if (isT1PrototypeProduct)
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
                            var subCost = CalculateBlueprintCost(materialBp, calculatedCosts, marketPrices, productToBlueprint, calculated, t2Blueprints, t2BlueprintToT1Product, manufacturedProductTypeIds);
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

        private decimal CalculateBlueprintTime(
            Blueprint bp,
            Dictionary<long, decimal> calculatedTimes,
            Dictionary<long, Blueprint> productToBlueprint,
            HashSet<long> calculated,
            HashSet<long> t2Blueprints,
            Dictionary<long, long> t2BlueprintToT1Product,
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
            if (teFactor < 0.01m)
                teFactor = 0.01m;

            bool isT2Blueprint = t2Blueprints.Contains(bp.BlueprintTypeId);
            long t1ProductTypeId = 0;
            if (isT2Blueprint)
            {
                t2BlueprintToT1Product.TryGetValue(bp.BlueprintTypeId, out t1ProductTypeId);
            }

            if (bp.Activities?.Manufacturing?.Materials != null)
            {
                foreach (var material in bp.Activities.Manufacturing.Materials)
                {
                    if (material == null || material.TypeId == 0)
                        continue;

                    decimal materialQuantity;
                    bool isT1PrototypeProduct = (material.TypeId == t1ProductTypeId);

                    if (isT1PrototypeProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity;
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else
                    {
                        materialQuantity = material.Quantity;
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
                            var subTime = CalculateBlueprintTime(materialBp, calculatedTimes, productToBlueprint, calculated, t2Blueprints, t2BlueprintToT1Product, manufacturedProductTypeIds);
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
                    bool isT1PrototypeProduct = (material.TypeId == t1ProductTypeId);

                    if (isT1PrototypeProduct)
                    {
                        materialQuantity = outputQuantity * material.Quantity;
                        materialQuantity = Math.Ceiling(materialQuantity);
                    }
                    else
                    {
                        materialQuantity = material.Quantity;
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
                            var subTime = CalculateBlueprintTime(materialBp, calculatedTimes, productToBlueprint, calculated, t2Blueprints, t2BlueprintToT1Product, manufacturedProductTypeIds);
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
            bool isManufacturing = bp.Activities?.Manufacturing != null;
            
            decimal adjustedBpTime;
            if (isManufacturing)
            {
                decimal structureFactor = 1.0m - (bp.StructureTimeBonus * 0.01m);
                decimal rigFactor = 1.0m - (bp.RigTimeBonus * 0.01m);
                decimal industryFactor = 1.0m - (bp.IndustryLevel * 0.04m);
                decimal advancedIndustryFactor = 1.0m - (bp.AdvancedIndustryLevel * 0.03m);
                adjustedBpTime = bpTime * teFactor * structureFactor * rigFactor * industryFactor * advancedIndustryFactor;
            }
            else
            {
                decimal reactionStructureFactor = 1.0m - (bp.ReactionStructureTimeBonus * 0.01m);
                decimal reactionRigFactor = 1.0m - (bp.ReactionRigTimeBonus * 0.01m);
                decimal reactionFactor = 1.0m - (bp.ReactionLevel * 0.04m);
                adjustedBpTime = bpTime * reactionStructureFactor * reactionRigFactor * reactionFactor;
            }

            var unitTime = outputQuantity > 0 ? (totalTime + adjustedBpTime) / outputQuantity : totalTime + adjustedBpTime;
            calculatedTimes[bp.BlueprintTypeId] = unitTime;
            return unitTime;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QuickType;

namespace EVEMakeMoney.Data
{
    public class CostBreakdownItem
    {
        public long TypeId { get; set; }
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal UnitTime { get; set; }
        public decimal TotalTime { get; set; }
        public bool IsIntermediate { get; set; }
        public List<CostBreakdownItem> Children { get; set; } = new();
        public decimal? InventionCost { get; set; }
    }

    public class CostBreakdownService
    {
        private readonly EVEMakeMoneyDbContext _db;
        private readonly TypeNameService _typeNameService;
        private Dictionary<long, Blueprint> _productToBlueprint;
        private Dictionary<long, double> _marketPrices;
        private Dictionary<long, long> _t2BlueprintToT1Product;
        private HashSet<long> _t2Blueprints;
        private Dictionary<long, Blueprint> _blueprints;

        public CostBreakdownService(EVEMakeMoneyDbContext db, TypeNameService typeNameService)
        {
            _db = db;
            _typeNameService = typeNameService;
        }

        public CostBreakdownItem GetCostBreakdown(long blueprintTypeId, List<Blueprint> blueprints)
        {
            _productToBlueprint = new Dictionary<long, Blueprint>();
            _t2BlueprintToT1Product = new Dictionary<long, long>();
            _t2Blueprints = new HashSet<long>();
            _marketPrices = GetMarketPrices();
            _blueprints = blueprints.ToDictionary(b => b.BlueprintTypeId, b => b);

            foreach (var bp in blueprints)
            {
                if (bp.Activities?.Manufacturing?.Products != null)
                {
                    foreach (var product in bp.Activities.Manufacturing.Products)
                    {
                        _productToBlueprint[product.TypeId] = bp;
                    }
                }

                if (bp.Activities?.Reaction?.Products != null)
                {
                    foreach (var product in bp.Activities.Reaction.Products)
                    {
                        _productToBlueprint[product.TypeId] = bp;
                    }
                }

                if (bp.Activities?.Invention?.Products != null)
                {
                    foreach (var product in bp.Activities.Invention.Products)
                    {
                        _t2Blueprints.Add(product.TypeId);
                    }
                    
                    if (bp.Activities?.Manufacturing?.Products != null)
                    {
                        foreach (var t1Product in bp.Activities.Manufacturing.Products)
                        {
                            foreach (var t2BpProduct in bp.Activities.Invention.Products)
                            {
                                _t2BlueprintToT1Product[t2BpProduct.TypeId] = t1Product.TypeId;
                            }
                        }
                    }
                }
            }

            CalculateInventionCosts(blueprints);

            var blueprint = blueprints.FirstOrDefault(b => b.BlueprintTypeId == blueprintTypeId);
            if (blueprint == null)
                return null;

            return BuildCostTree(blueprint, 0, new HashSet<long>());
        }

        private void CalculateInventionCosts(List<Blueprint> blueprints)
        {
            foreach (var bp in blueprints)
            {
                if (bp.Activities?.Invention != null && bp.Activities.Invention.Products != null)
                {
                    foreach (var inventionProduct in bp.Activities.Invention.Products)
                    {
                        var t2Blueprint = blueprints.FirstOrDefault(b => b.BlueprintTypeId == inventionProduct.TypeId);
                        if (t2Blueprint == null)
                            continue;

                        var inventionCost = CalculateInventionCostForT2(bp, _marketPrices);
                        t2Blueprint.InventionCost = inventionCost;

                        var existingBp = _productToBlueprint.GetValueOrDefault(inventionProduct.TypeId);
                        if (existingBp != null)
                        {
                            existingBp.InventionCost = inventionCost;
                        }
                    }
                }
            }
        }

        private decimal CalculateInventionCostForT2(Blueprint t1Blueprint, Dictionary<long, double> marketPrices)
        {
            if (t1Blueprint.Activities?.Invention == null)
                return 0;

            var invention = t1Blueprint.Activities.Invention;
            decimal materialCost = 0;

            if (invention.Materials != null)
            {
                foreach (var material in invention.Materials)
                {
                    var price = marketPrices.GetValueOrDefault(material.TypeId, 0);
                    materialCost += (decimal)price * material.Quantity;
                }
            }

            double probability = 1.0;
            long outputQuantity = 1;
            if (invention.Products != null && invention.Products.Length > 0)
            {
                if (invention.Products[0].Probability.HasValue)
                {
                    probability = invention.Products[0].Probability.Value;
                }
                outputQuantity = invention.Products[0].Quantity;
            }

            if (probability <= 0)
                probability = 1.0;
            if (outputQuantity <= 0)
                outputQuantity = 1;

            return materialCost / (decimal)probability / outputQuantity;
        }

        private CostBreakdownItem BuildCostTree(Blueprint bp, int level, HashSet<long> visiting)
        {
            if (visiting.Contains(bp.BlueprintTypeId))
            {
                return new CostBreakdownItem
                {
                    TypeId = bp.BlueprintTypeId,
                    Name = $"[循环] {_typeNameService.GetName(bp.BlueprintTypeId)} ({bp.BlueprintTypeId})",
                    Level = level,
                    IsIntermediate = true
                };
            }

            visiting.Add(bp.BlueprintTypeId);

            decimal outputQuantity = 1;
            if (bp.Activities?.Manufacturing?.Products != null && bp.Activities.Manufacturing.Products.Length > 0)
            {
                outputQuantity = bp.Activities.Manufacturing.Products[0].Quantity;
            }
            else if (bp.Activities?.Reaction?.Products != null && bp.Activities.Reaction.Products.Length > 0)
            {
                outputQuantity = bp.Activities.Reaction.Products[0].Quantity;
            }

            var item = new CostBreakdownItem
            {
                TypeId = bp.BlueprintTypeId,
                Name = $"{_typeNameService.GetName(bp.BlueprintTypeId)} ({bp.BlueprintTypeId}) x{outputQuantity}",
                Level = level,
                Quantity = outputQuantity,
                IsIntermediate = outputQuantity > 1
            };

            if (bp.Activities?.Manufacturing?.Materials == null && bp.Activities?.Reaction?.Materials == null)
            {
                visiting.Remove(bp.BlueprintTypeId);
                return item;
            }

            decimal totalCost = 0;
            decimal totalTime = 0;

            if (bp.Activities?.Manufacturing?.Materials != null)
            {
                foreach (var material in bp.Activities.Manufacturing.Materials)
                {
                    var childItem = ProcessMaterial(material, level, visiting, bp, outputQuantity);
                    item.Children.Add(childItem);
                    totalCost += childItem.TotalCost;
                    totalTime += childItem.TotalTime;
                }
            }

            if (bp.Activities?.Reaction?.Materials != null)
            {
                foreach (var material in bp.Activities.Reaction.Materials)
                {
                    var childItem = ProcessMaterial(material, level, visiting, bp, outputQuantity);
                    item.Children.Add(childItem);
                    totalCost += childItem.TotalCost;
                    totalTime += childItem.TotalTime;
                }
            }

            item.TotalCost = totalCost;
            item.UnitCost = outputQuantity > 0 ? totalCost / outputQuantity : totalCost;

            decimal teFactor = 1.0m - (bp.TE * 0.02m);
            if (teFactor < 0.01m)
                teFactor = 0.01m;

            var bpTime = bp.Activities?.Manufacturing?.Time ?? bp.Activities?.Reaction?.Time ?? 0;
            var adjustedBpTime = bpTime * teFactor;
            item.TotalTime = totalTime + adjustedBpTime;
            item.UnitTime = outputQuantity > 0 ? item.TotalTime / outputQuantity : item.TotalTime;

            bp.ManufacturingCost = item.UnitCost;
            bp.ManufacturingTime = item.UnitTime;

            if (bp.InventionCost > 0)
            {
                item.InventionCost = bp.InventionCost;
                item.TotalCost += bp.InventionCost;
                item.UnitCost = outputQuantity > 0 ? item.TotalCost / outputQuantity : item.TotalCost;
                bp.ManufacturingCost = item.UnitCost;
            }

            visiting.Remove(bp.BlueprintTypeId);
            return item;
        }

        private CostBreakdownItem ProcessMaterial(dynamic material, int level, HashSet<long> visiting, Blueprint parentBp, decimal parentOutputQuantity)
        {
            CostBreakdownItem childItem;
            var typeId = (long)material.TypeId;

            decimal meFactor = 1.0m - (parentBp.ME * 0.01m);
            if (meFactor < 0.01m)
                meFactor = 0.01m;

            bool isT2Blueprint = _t2Blueprints.Contains(parentBp.BlueprintTypeId);
            long t1ProductTypeId = 0;
            if (isT2Blueprint)
            {
                _t2BlueprintToT1Product.TryGetValue(parentBp.BlueprintTypeId, out t1ProductTypeId);
            }
            bool isT1PrototypeProduct = (typeId == t1ProductTypeId);

            decimal materialQuantity;
            if (isT1PrototypeProduct)
            {
                materialQuantity = parentOutputQuantity * (decimal)material.Quantity;
                materialQuantity = Math.Ceiling(materialQuantity);
            }
            else
            {
                materialQuantity = (decimal)material.Quantity * meFactor;
                if (materialQuantity < 1 && material.Quantity > 0)
                    materialQuantity = 1;
            }

            if (_productToBlueprint.TryGetValue(typeId, out var materialBp))
            {
                var childTree = BuildCostTree(materialBp, level + 1, new HashSet<long>(visiting));
                    
                decimal childTotalCost = 0;
                decimal childTotalTime = 0;
                if (childTree.Children != null)
                {
                    foreach (var child in childTree.Children)
                    {
                        childTotalCost += child.TotalCost;
                        childTotalTime += child.TotalTime;
                    }
                }
                    
                childItem = new CostBreakdownItem
                {
                    TypeId = typeId,
                    Name = $"{_typeNameService.GetName(materialBp.BlueprintTypeId)} ({materialBp.BlueprintTypeId}) (制造成本)",
                    Level = level + 1,
                    Quantity = materialQuantity,
                    UnitCost = childTree.UnitCost,
                    TotalCost = childTree.UnitCost * materialQuantity,
                    UnitTime = childTree.UnitTime,
                    TotalTime = childTree.UnitTime * materialQuantity,
                    IsIntermediate = true,
                    Children = childTree.Children
                };
            }
            else
            {
                _marketPrices.TryGetValue(typeId, out var price);
                var priceValue = price;
                var cost = (decimal)priceValue * materialQuantity;

                childItem = new CostBreakdownItem
                {
                    TypeId = typeId,
                    Name = $"{_typeNameService.GetName(typeId)} ({typeId})",
                    Level = level + 1,
                    Quantity = materialQuantity,
                    UnitCost = (decimal)priceValue,
                    TotalCost = cost,
                    UnitTime = 0,
                    TotalTime = 0,
                    IsIntermediate = false
                };
            }

            return childItem;
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

        public string FormatCostTree(CostBreakdownItem item, int indent = 0)
        {
            var sb = new StringBuilder();
            var prefix = new string(' ', indent * 4);
            
            var timeStr = item.TotalTime > 0 ? $" ({FormatTime(item.TotalTime)})" : "";
            
            if (item.IsIntermediate)
            {
                sb.AppendLine($"{prefix}└─ {item.Name}: {item.TotalCost:N0} ISK{timeStr}");
            }
            else
            {
                sb.AppendLine($"{prefix}└─ {item.Name} x{item.Quantity}: {item.TotalCost:N0} ISK (单价: {item.UnitCost:N2}){timeStr}");
            }

            foreach (var child in item.Children)
            {
                sb.Append(FormatCostTree(child, indent + 1));
            }

            return sb.ToString();
        }

        private string FormatTime(decimal seconds)
        {
            if (seconds < 60)
                return $"{seconds:N0}秒";
            if (seconds < 3600)
                return $"{seconds / 60:N1}分钟";
            if (seconds < 86400)
                return $"{seconds / 3600:N1}小时";
            return $"{seconds / 86400:N1}天";
        }
    }
}

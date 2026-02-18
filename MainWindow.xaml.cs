using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EVEStandard;
using EVEStandard.Enumerations;
using EVEMakeMoney.Data;
using QuickType;

namespace EVEMakeMoney;

public partial class MainWindow : Window
{
    private readonly EVEStandardAPI _esi;
    private List<Blueprint> _blueprints = new();
    private List<Blueprint> _allBlueprints = new();
    private TypeNameService _typeNameService = new();

    public MainWindow()
    {
        InitializeComponent();
        _esi = new EVEStandardAPI("EVEMakeMoney/1.0", DataSource.Tranquility, CompatibilityDate.v2025_12_16, TimeSpan.FromSeconds(30));
        
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = FindProjectRoot(baseDir);
        var sdePath = Path.Combine(projectRoot, "sde");
        
        var typesFilePath = Path.Combine(sdePath, "types.jsonl");
        _typeNameService.Load(typesFilePath);
    }
    
    private static string FindProjectRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "EVEMakeMoney.csproj")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return startPath;
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = FindProjectRoot(baseDir);
            var filePath = Path.Combine(projectRoot, "sde", "blueprints.jsonl");

            if (!File.Exists(filePath))
            {
                StatusText.Text = "文件未找到: " + filePath;
                return;
            }

            _blueprints = BlueprintLoader.LoadBlueprints(filePath);
            foreach (var bp in _blueprints)
            {
                bp.Name = _typeNameService.GetName(bp.BlueprintTypeId);
            }
            _allBlueprints = _blueprints;
            BlueprintGrid.ItemsSource = _blueprints;
            StatusText.Text = $"成功加载 {_blueprints.Count} 个蓝图";
        }
        catch (Exception ex)
        {
            StatusText.Text = "加载失败: " + ex.Message;
            MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LoadMarketButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadMarketButton.IsEnabled = false;
            MarketProgress.Visibility = Visibility.Visible;
            ProgressText.Visibility = Visibility.Visible;

            var marketService = new MarketDataService(_esi);

            const long regionId = 10000002;

            StatusText.Text = "正在获取区域订单...";
            
            var typeIds = await marketService.GetTypeIdsFromOrdersAsync(regionId);
            StatusText.Text = $"找到 {typeIds.Count} 个物品ID，正在加载市场数据...";
            
            var progress = new Progress<int>(percent =>
            {
                MarketProgress.Value = percent;
                ProgressText.Text = $"加载进度: {percent}%";
            });

            await marketService.FetchAndSaveAllMarketHistoryAsync(regionId, typeIds, progress);

            StatusText.Text = "正在计算周销量...";
            await marketService.CalculateAllWeeklySalesAsync(regionId, progress);

            StatusText.Text = "市场数据加载完成！";
            MarketProgress.Value = 100;
            ProgressText.Text = "完成！";
        }
        catch (Exception ex)
        {
            StatusText.Text = "加载失败: " + ex.Message;
            MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            LoadMarketButton.IsEnabled = true;
        }
    }

    private void CalculateCostButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_blueprints.Count == 0)
            {
                StatusText.Text = "请先加载蓝图数据";
                return;
            }

            if (!int.TryParse(METextBox.Text, out int me))
            {
                me = 0;
            }
            if (!int.TryParse(TETextBox.Text, out int te))
            {
                te = 0;
            }

            foreach (var bp in _blueprints)
            {
                bp.ME = me;
                bp.TE = te;
            }

            CalculateCostButton.IsEnabled = false;
            StatusText.Text = "正在计算成本和时间...";

            using var db = new EVEMakeMoneyDbContext();
            var costService = new CostCalculationService(db);

            var costs = costService.CalculateAllCosts(_blueprints, me, te);
            var times = costService.CalculateAllTimes(_blueprints, me, te);

            foreach (var bp in _blueprints)
            {
                if (costs.TryGetValue(bp.BlueprintTypeId, out var cost))
                {
                    bp.ManufacturingCost = cost;
                }
                if (times.TryGetValue(bp.BlueprintTypeId, out var time))
                {
                    bp.ManufacturingTime = time;
                }
            }

            BlueprintGrid.Items.Refresh();
            StatusText.Text = $"成本和时间计算完成！共计算 {_blueprints.Count} 个蓝图 (ME={me}, TE={te})";
        }
        catch (Exception ex)
        {
            StatusText.Text = "计算失败: " + ex.Message;
            MessageBox.Show(ex.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CalculateCostButton.IsEnabled = true;
        }
    }

    private void BlueprintGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        try
        {
            if (BlueprintGrid.SelectedItem is Blueprint bp)
            {
                using var db = new EVEMakeMoneyDbContext();
                var breakdownService = new CostBreakdownService(db, _typeNameService);

                var breakdown = breakdownService.GetCostBreakdown(bp.BlueprintTypeId, _blueprints);

                if (breakdown != null)
                {
                    var text = breakdownService.FormatCostTree(breakdown);
                    var bpName = _typeNameService.GetName(bp.BlueprintTypeId);
                    var timeStr = FormatTime(bp.ManufacturingTime);
                    var costInfo = $"总成本: {bp.ManufacturingCost:N0} ISK\n制造时间: {timeStr}";
                    if (bp.InventionCost > 0)
                    {
                        costInfo = $"制造成本: {(bp.ManufacturingCost - bp.InventionCost):N0} ISK\n发明成本: {bp.InventionCost:N0} ISK\n总成本: {bp.ManufacturingCost:N0} ISK\n制造时间: {timeStr}";
                    }
                    CostBreakdownText.Text = $"Blueprint: {bpName} ({bp.BlueprintTypeId})\n{costInfo}\n\n{text}";
                }
                else
                {
                    CostBreakdownText.Text = "未找到该蓝图的成本信息";
                }
            }
        }
        catch (Exception ex)
        {
            CostBreakdownText.Text = "获取成本链条失败: " + ex.Message;
        }
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var searchText = SearchBox.Text.Trim().ToLower();
        if (string.IsNullOrEmpty(searchText))
        {
            _blueprints = _allBlueprints;
        }
        else
        {
            _blueprints = _allBlueprints.Where(bp =>
                bp.Name.ToLower().Contains(searchText) ||
                bp.BlueprintTypeId.ToString().Contains(searchText)
            ).ToList();
        }
        BlueprintGrid.ItemsSource = _blueprints;
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

    private async void MarketAnalysisButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (BlueprintGrid.SelectedItem is not Blueprint selectedBlueprint)
            {
                StatusText.Text = "请先在列表中选择一个蓝图";
                MessageBox.Show("请先选择一个蓝图", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            long? productTypeId = null;

            if (selectedBlueprint.Activities?.Manufacturing?.Products?.Length > 0)
            {
                productTypeId = selectedBlueprint.Activities.Manufacturing.Products[0].TypeId;
            }
            else if (selectedBlueprint.Activities?.Invention?.Products?.Length > 0)
            {
                productTypeId = selectedBlueprint.Activities.Invention.Products[0].TypeId;
            }
            else if (selectedBlueprint.Activities?.Reaction?.Products?.Length > 0)
            {
                productTypeId = selectedBlueprint.Activities.Reaction.Products[0].TypeId;
            }

            if (productTypeId == null)
            {
                StatusText.Text = "无法获取该蓝图的产品类型ID";
                return;
            }

            MarketAnalysisButton.IsEnabled = false;
            AnalysisResultText.Text = "正在分析市场热门指数...";

            await Task.Run(() =>
            {
                using var db = new EVEMakeMoneyDbContext();
                var popularityService = new MarketPopularityService(db, _typeNameService);

                const long regionId = 10000002;

                var result = popularityService.Analyze(
                    typeId: productTypeId.Value,
                    regionId: regionId,
                    recentDaysThreshold: 7,
                    clusterHours: 2
                );

                var report = popularityService.GenerateReport(result);

                Dispatcher.Invoke(() =>
                {
                    AnalysisResultText.Text = report;
                });
            });

            StatusText.Text = "热门指数分析完成！";
        }
        catch (Exception ex)
        {
            StatusText.Text = "分析失败: " + ex.Message;
            AnalysisResultText.Text = "分析失败: " + ex.Message;
        }
        finally
        {
            MarketAnalysisButton.IsEnabled = true;
        }
    }
}
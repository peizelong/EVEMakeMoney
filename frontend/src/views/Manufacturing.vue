<template>
  <div class="manufacturing-page">
    <div class="control-panel">
      <el-card>
        <div class="control-form">
          <el-input
            v-model="searchText"
            placeholder="搜索蓝图..."
            @input="handleSearch"
            clearable
            class="search-input"
          >
            <template #prefix>
              <el-icon><Search /></el-icon>
            </template>
          </el-input>

          <el-tooltip content="Material Efficiency 材料效率，每级减少1%材料需求" placement="top">
            <el-select v-model="calcParams.me" placeholder="ME" class="param-select">
              <el-option v-for="i in 10" :key="i-1" :label="'ME ' + (i-1)" :value="i-1" />
            </el-select>
          </el-tooltip>

          <el-tooltip content="Time Efficiency 时间效率，每级减少1%制造时间" placement="top">
            <el-select v-model="calcParams.te" placeholder="TE" class="param-select">
              <el-option v-for="i in 21" :key="i-1" :label="'TE ' + (i-1)" :value="i-1" />
            </el-select>
          </el-tooltip>

          <el-tooltip content="Industry 工业技能，每级减少4%制造时间" placement="top">
            <el-select v-model="calcParams.industryLevel" placeholder="工业LV" class="param-select">
              <el-option v-for="i in 6" :key="i-1" :label="'工业 ' + (i-1)" :value="i-1" />
            </el-select>
          </el-tooltip>

          <el-tooltip content="建筑时间加成（如Azumul、Sotiyo），每级减少1%制造时间" placement="top">
            <el-select v-model="calcParams.structureBonus" placeholder="建筑时间" class="param-select">
              <el-option v-for="i in 21" :key="i-1" :label="(i-1) + '%'" :value="i-1" />
            </el-select>
          </el-tooltip>

          <el-tooltip content="建筑装备RIG时间加成，每级减少5%制造时间（最大50%）" placement="top">
            <el-select v-model="calcParams.rigBonus" placeholder="RIG时间" class="param-select">
              <el-option v-for="i in 11" :key="i-1" :label="(i-1) * 5 + '%'" :value="(i-1) * 5" />
            </el-select>
          </el-tooltip>

          <el-tooltip content="Advanced Industry 高级工业技能，每级减少3%制造时间" placement="top">
            <el-select v-model="calcParams.advancedIndustryLevel" placeholder="高精工业" class="param-select">
              <el-option v-for="i in 6" :key="i-1" :label="'高精 ' + (i-1)" :value="i-1" />
            </el-select>
          </el-tooltip>

          <el-tooltip content="Reaction 反应技能，每级减少4%反应时间" placement="top">
            <el-select v-model="calcParams.reactionLevel" placeholder="反应LV" class="param-select">
              <el-option v-for="i in 6" :key="i-1" :label="'反应 ' + (i-1)" :value="i-1" />
            </el-select>
          </el-tooltip>

          <el-tooltip content="反应建筑时间加成，每级减少1%反应时间" placement="top">
            <el-select v-model="calcParams.reactionStructureBonus" placeholder="反应建筑" class="param-select">
              <el-option v-for="i in 21" :key="i-1" :label="(i-1) + '%'" :value="i-1" />
            </el-select>
          </el-tooltip>

          <el-tooltip content="反应建筑RIG时间加成，每级减少5%反应时间（最大50%）" placement="top">
            <el-select v-model="calcParams.reactionRigBonus" placeholder="反应RIG" class="param-select">
              <el-option v-for="i in 11" :key="i-1" :label="(i-1) * 5 + '%'" :value="(i-1) * 5" />
            </el-select>
          </el-tooltip>
        </div>

        <div class="button-group">
          <el-button type="primary" @click="loadBlueprints" :loading="loading">加载蓝图数据</el-button>
          <el-button type="success" @click="calculateCosts" :loading="calculating">计算成本</el-button>
          <el-button type="warning" @click="saveAllSettings" :loading="savingSettings" :disabled="!authStore.isAuthenticated || savedSettings.size === 0">
            保存所有ME/TE
          </el-button>
        </div>
      </el-card>
    </div>

    <div class="status-bar">
      {{ statusText }}
    </div>

    <el-row :gutter="20" class="main-content">
      <el-col :span="14">
        <el-card>
          <template #header>
            <div class="card-header">
              <span>蓝图列表</span>
              <el-pagination
                v-model:current-page="currentPage"
                v-model:page-size="pageSize"
                :total="totalBlueprints"
                layout="prev, pager, next, total"
                @current-change="loadBlueprints"
                small
              />
            </div>
          </template>
          <el-table 
            :data="blueprints" 
            v-loading="loading"
            @row-click="handleRowClick"
            highlight-current-row
            :row-class-name="getRowClassName"
            stripe
            size="small"
          >
            <el-table-column prop="name" label="蓝图名称" min-width="180" />
            <el-table-column prop="blueprintTypeId" label="TypeID" width="100" />
            <el-table-column width="80" label="类型" align="center">
              <template #default="{ row }">
                <el-tag v-if="row.activities?.invention" type="warning" size="small">T2</el-tag>
                <el-tag v-else-if="row.activities?.reaction" type="success" size="small">反应</el-tag>
                <el-tag v-else type="info" size="small">T1</el-tag>
              </template>
            </el-table-column>
            <el-table-column label="ME" width="70" align="center">
              <template #default="{ row }">
                <el-select
                  :model-value="getSavedME(row.blueprintTypeId)"
                  @change="(val: number) => { saveBlueprintSettings(row.blueprintTypeId, val, getSavedTE(row.blueprintTypeId)) }"
                  size="small"
                  :disabled="!authStore.isAuthenticated"
                  class="me-select"
                >
                  <el-option v-for="i in 10" :key="i-1" :label="String(i-1)" :value="i-1" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column label="TE" width="70" align="center">
              <template #default="{ row }">
                <el-select
                  :model-value="getSavedTE(row.blueprintTypeId)"
                  @change="(val: number) => { saveBlueprintSettings(row.blueprintTypeId, getSavedME(row.blueprintTypeId), val) }"
                  size="small"
                  :disabled="!authStore.isAuthenticated"
                  class="te-select"
                >
                  <el-option v-for="i in 21" :key="i-1" :label="String(i-1)" :value="i-1" />
                </el-select>
              </template>
            </el-table-column>
            <el-table-column label="成本" width="120" align="right">
              <template #default="{ row }">
                <span v-if="row.manufacturingCost === 0" class="cost-none">无数据</span>
                <span v-else-if="row.manufacturingCost === undefined || row.manufacturingCost === null" class="cost-pending">未计算</span>
                <span v-else :class="{ 'cost-zero': row.manufacturingCost === 0 }">
                  {{ formatNumber(row.manufacturingCost) + ' ISK' }}
                </span>
              </template>
            </el-table-column>
            <el-table-column label="时间" width="100" align="center">
              <template #default="{ row }">
                {{ row.manufacturingTime > 0 ? formatTime(row.manufacturingTime) : '-' }}
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>

      <el-col :span="10">
        <el-card>
          <template #header>
            <span>成本链条</span>
          </template>
          <div class="cost-breakdown" v-loading="loadingBreakdown">
            <div v-if="costBreakdownTree.length > 0" class="tree-container">
              <el-tree
                :data="costBreakdownTree"
                :props="defaultProps"
                default-expand-all
                :expand-on-click-node="false"
              >
                <template #default="{ data }">
                  <span class="tree-node">
                    <span :class="{ 'cost-high': data.cost > 1000000, 'cost-medium': data.cost > 100000 && data.cost <= 1000000 }">
                      {{ data.label }}
                    </span>
                  </span>
                </template>
              </el-tree>
            </div>
            <pre v-else-if="costBreakdownText">{{ costBreakdownText }}</pre>
            <el-empty v-else description="请选择一个蓝图查看成本详情" :image-size="80" />
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue'
import { Search } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { blueprintApi, blueprintSettingsApi, type Blueprint, type CostBreakdownItem, type CalculateCostsRequest, type BlueprintSetting } from '../api'
import { useAuthStore } from '../stores/auth'

const authStore = useAuthStore()

const blueprints = ref<Blueprint[]>([])
const selectedBlueprint = ref<Blueprint | null>(null)
const searchText = ref('')
const statusText = ref('')
const loading = ref(false)
const calculating = ref(false)
const loadingBreakdown = ref(false)
const savingSettings = ref(false)
const costBreakdownText = ref('')
const costBreakdownTree = ref<any[]>([])
const currentPage = ref(1)
const pageSize = ref(50)
const totalBlueprints = ref(0)

const savedSettings = reactive<Map<number, BlueprintSetting>>(new Map())

const defaultProps = {
  children: 'children',
  label: 'label'
}

const calcParams = ref<CalculateCostsRequest>({
  ME: 0,
  TE: 0,
  StructureBonus: 0,
  RigBonus: 0,
  IndustryLevel: 5,
  AdvancedIndustryLevel: 0,
  ReactionStructureBonus: 0,
  ReactionRigBonus: 0,
  ReactionLevel: 0
})

let searchTimeout: number | null = null

async function loadSavedSettings() {
  if (!authStore.isAuthenticated) return
  try {
    const settings = await blueprintSettingsApi.getSettings()
    savedSettings.clear()
    for (const s of settings) {
      savedSettings.set(s.blueprintTypeId, s)
    }
  } catch (e) {
    console.error('Failed to load settings', e)
  }
}

function getSavedME(bpTypeId: number): number {
  return savedSettings.get(bpTypeId)?.me ?? 0
}

function getSavedTE(bpTypeId: number): number {
  return savedSettings.get(bpTypeId)?.te ?? 0
}

async function saveBlueprintSettings(bpTypeId: number, me: number, te: number) {
  if (!authStore.isAuthenticated) {
    ElMessage.warning('请先登录以保存设置')
    return
  }
  savingSettings.value = true
  try {
    const saved = await blueprintSettingsApi.saveSetting(bpTypeId, me, te)
    savedSettings.set(bpTypeId, saved)
    ElMessage.success('设置已保存')
  } catch (e: any) {
    ElMessage.error(e.response?.data?.message || '保存设置失败')
  } finally {
    savingSettings.value = false
  }
}

async function loadAndApplySettings() {
  await loadSavedSettings()
  if (savedSettings.size > 0) {
    calcParams.value.ME = Array.from(savedSettings.values())[0].me
    calcParams.value.TE = Array.from(savedSettings.values())[0].te
  }
}

async function saveAllSettings() {
  if (savedSettings.size === 0) {
    ElMessage.warning('没有需要保存的设置')
    return
  }
  savingSettings.value = true
  try {
    const settingsToSave = Array.from(savedSettings.values()).map(s => ({
      blueprintTypeId: s.blueprintTypeId,
      me: s.me,
      te: s.te
    }))
    await blueprintSettingsApi.saveSettingsBatch(settingsToSave)
    ElMessage.success(`已保存 ${settingsToSave.length} 个蓝图设置`)
  } catch (e: any) {
    ElMessage.error(e.response?.data?.message || '保存设置失败')
  } finally {
    savingSettings.value = false
  }
}

function handleSearch() {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    currentPage.value = 1
    loadBlueprints()
  }, 300)
}

function getRowClassName({ row }: { row: Blueprint }) {
  if (selectedBlueprint.value && row.blueprintTypeId === selectedBlueprint.value.blueprintTypeId) {
    return 'selected-row'
  }
  return ''
}

async function loadBlueprints() {
  loading.value = true
  statusText.value = '正在加载蓝图数据...'
  
  try {
    const result = await blueprintApi.getBlueprints(searchText.value, currentPage.value, pageSize.value)
    blueprints.value = result.data
    totalBlueprints.value = result.total
    statusText.value = `已加载 ${result.data.length} 个蓝图，总计 ${result.total} 个`
  } catch (error: any) {
    statusText.value = '加载失败: ' + (error.message || '未知错误')
    ElMessage.error('加载蓝图数据失败')
    console.error(error)
  } finally {
    loading.value = false
  }
}

async function calculateCosts() {
  if (blueprints.value.length === 0) {
    ElMessage.warning('请先加载蓝图数据')
    return
  }
  
  calculating.value = true
  statusText.value = '正在计算成本和时间...'
  
  try {
    const me = calcParams.value.ME
    const te = calcParams.value.TE
    
    const updatedBlueprints = await blueprintApi.calculateCosts({
      ME: me,
      TE: te,
      StructureBonus: calcParams.value.StructureBonus,
      RigBonus: calcParams.value.RigBonus,
      IndustryLevel: calcParams.value.IndustryLevel,
      AdvancedIndustryLevel: calcParams.value.AdvancedIndustryLevel,
      ReactionStructureBonus: calcParams.value.ReactionStructureBonus,
      ReactionRigBonus: calcParams.value.ReactionRigBonus,
      ReactionLevel: calcParams.value.ReactionLevel
    })
    
    blueprints.value = updatedBlueprints.map(bp => ({
      ...bp,
      customME: me,
      customTE: te
    }))
    
    ElMessage.success('成本计算完成')
  } catch (error: any) {
    statusText.value = '计算失败: ' + (error.message || '未知错误')
    ElMessage.error('计算成本失败')
    console.error(error)
  } finally {
    calculating.value = false
  }
}

async function handleRowClick(row: Blueprint) {
  selectedBlueprint.value = row
  loadingBreakdown.value = true
  costBreakdownText.value = ''
  costBreakdownTree.value = []
  
  try {
    const breakdown = await blueprintApi.getCostBreakdown(row.blueprintTypeId)
    const timeStr = row.manufacturingTime > 0 ? formatTime(row.manufacturingTime) : ''
    
    let costInfo = `总成本: ${formatNumber(row.manufacturingCost)} ISK\n制造时间: ${timeStr}`
    if (row.inventionCost > 0) {
      costInfo = `制造成本: ${formatNumber(row.manufacturingCost - row.inventionCost)} ISK\n发明成本: ${formatNumber(row.inventionCost)} ISK\n总成本: ${formatNumber(row.manufacturingCost)} ISK\n制造时间: ${timeStr}`
    }
    
    costBreakdownText.value = `Blueprint: ${row.name} (${row.blueprintTypeId})\n${costInfo}\n\n${formatBreakdown(breakdown)}`
    
    costBreakdownTree.value = [convertToTreeData(breakdown)]
  } catch (error: any) {
    costBreakdownText.value = '获取成本链条失败: ' + (error.message || '未知错误')
    console.error(error)
  } finally {
    loadingBreakdown.value = false
  }
}

function formatBreakdown(item: CostBreakdownItem, indent: number = 0): string {
  const prefix = ' '.repeat(indent * 4)
  const timeStr = item.totalTime > 0 ? ` (${formatTime(item.totalTime)})` : ''
  
  let result = ''
  if (item.isIntermediate) {
    result = `${prefix}└─ ${item.name}: ${formatNumber(item.totalCost)} ISK${timeStr}\n`
  } else {
    result = `${prefix}└─ ${item.name} x${item.quantity}: ${formatNumber(item.totalCost)} ISK (单价: ${item.unitCost.toFixed(2)})${timeStr}\n`
  }
  
  for (const child of item.children) {
    result += formatBreakdown(child, indent + 1)
  }
  
  return result
}

function convertToTreeData(item: CostBreakdownItem): any {
  const timeStr = item.totalTime > 0 ? ` (${formatTime(item.totalTime)})` : ''
  let label = ''
  
  if (item.isIntermediate) {
    label = `${item.name}: ${formatNumber(item.totalCost)} ISK${timeStr}`
  } else {
    label = `${item.name} x${item.quantity}: ${formatNumber(item.totalCost)} ISK (单价: ${item.unitCost.toFixed(2)})${timeStr}`
  }
  
  const node: any = {
    label,
    cost: item.totalCost,
    isIntermediate: item.isIntermediate
  }
  
  if (item.children && item.children.length > 0) {
    node.children = item.children.map((child: CostBreakdownItem) => convertToTreeData(child))
  }
  
  return node
}

function formatNumber(num: number): string {
  if (num >= 1000000000) return (num / 1000000000).toFixed(2) + 'B'
  if (num >= 1000000) return (num / 1000000).toFixed(2) + 'M'
  if (num >= 1000) return (num / 1000).toFixed(2) + 'K'
  return num.toFixed(2)
}

function formatTime(seconds: number): string {
  if (seconds < 60) return seconds.toFixed(0) + '秒'
  if (seconds < 3600) return (seconds / 60).toFixed(1) + '分钟'
  if (seconds < 86400) return (seconds / 3600).toFixed(1) + '小时'
  return (seconds / 86400).toFixed(1) + '天'
}

onMounted(async () => {
  statusText.value = '点击"加载蓝图数据"按钮开始'
  await loadSavedSettings()
})
</script>

<style scoped>
.me-select, .te-select {
  width: 50px;
}

.me-select .el-select__wrapper, .te-select .el-select__wrapper {
  min-height: 24px;
  padding: 2px 8px;
}

.manufacturing-page {
  max-width: 1600px;
  margin: 0 auto;
}

.control-panel {
  margin-bottom: 24px;
}

.control-panel :deep(.el-card) {
  border-radius: 8px;
}

.control-form {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.search-input {
  width: 240px;
}

.param-select {
  width: 100px;
}

.button-group {
  margin-top: 14px;
  display: flex;
  gap: 8px;
}

.status-bar {
  margin-bottom: 24px;
  padding: 12px 16px;
  background: #f0f9ff;
  border-left: 4px solid #409eff;
  color: #606266;
  font-size: 13px;
  border-radius: 4px;
}

.main-content {
  margin-bottom: 24px;
}

.main-content :deep(.el-card) {
  border-radius: 8px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.cost-zero { color: #b0b0b0; }
.cost-none { color: #888888; font-size: 11px; }
.cost-pending { color: #b0b0b0; font-size: 11px; }

.cost-breakdown {
  height: 500px;
  overflow: auto;
}

.cost-breakdown pre {
  margin: 0;
  font-size: 11px;
  white-space: pre-wrap;
  color: #555555;
}

.tree-node {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
}

.cost-high { color: #000000; font-weight: 700; }
.cost-medium { color: #555555; font-weight: 500; }

@media (max-width: 768px) {
  .control-form {
    flex-direction: column;
    align-items: stretch;
  }
  
  .search-input,
  .param-select {
    width: 100%;
  }
  
  .button-group {
    flex-direction: column;
  }
  
  .button-group .el-button {
    width: 100%;
  }
  
  .main-content :deep(.el-col) {
    margin-bottom: 16px;
  }
}
</style>

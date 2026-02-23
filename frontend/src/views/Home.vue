<template>
  <div class="app-container">
    <header class="header">
      <h1>EVE 制造成本计算器</h1>
      <div class="user-info" v-if="authStore.isAuthenticated">
        <el-dropdown trigger="click" @command="handleUserCommand">
          <span class="user-dropdown">
            {{ authStore.username }}
            <el-icon><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="characters">
                <el-icon><User /></el-icon>
                绑定EVE角色
              </el-dropdown-item>
              <el-dropdown-item command="logout" divided>
                <el-icon><SwitchButton /></el-icon>
                退出登录
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
    </header>

    <main class="main">
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

            <el-tooltip content="市场数据所属区域，不同区域市场价格不同" placement="top">
              <el-select v-model="regionId" placeholder="区域" class="param-select">
                <el-option label="The Forge (吉塔)" :value="10000002" />
                <el-option label="Domain (瑟维斯)" :value="10000043" />
                <el-option label="Sinq Laison (辛迪加)" :value="10000032" />
              </el-select>
            </el-tooltip>
          </div>

          <div class="button-group">
            <el-button type="primary" @click="loadBlueprints" :loading="loading">加载蓝图数据</el-button>
            <el-button type="success" @click="calculateCosts" :loading="calculating">计算成本</el-button>
            <el-button type="warning" @click="fetchMarketOrders" :loading="fetchingOrders" v-if="authStore.isAuthenticated">获取市场订单</el-button>
            <el-button type="info" @click="fetchMarketHistory" :loading="fetchingHistory" v-if="authStore.isAuthenticated">获取历史数据</el-button>
            <el-button v-if="!authStore.isAuthenticated" @click="router.push('/login')">登录</el-button>
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
              <el-table-column label="类型" width="80" align="center">
                <template #default="{ row }">
                  <el-tag v-if="row.activities?.invention" type="warning" size="small">T2</el-tag>
                  <el-tag v-else-if="row.activities?.reaction" type="success" size="small">反应</el-tag>
                  <el-tag v-else type="info" size="small">T1</el-tag>
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
    </main>

    <el-dialog v-model="characterDialogVisible" title="绑定EVE角色" width="500px">
      <div class="character-dialog-content">
        <p class="dialog-desc">
          绑定EVE角色后，可以使用该角色的ESI权限进行游戏内数据查询。
          支持绑定多个EVE角色。
        </p>

        <el-button type="primary" @click="bindCharacter" class="bind-btn">
          绑定新角色
        </el-button>

        <div class="character-list" v-if="authStore.characters.length > 0">
          <h4>已绑定的角色</h4>
          <el-table :data="authStore.characters" size="small">
            <el-table-column prop="characterName" label="角色名称" />
            <el-table-column prop="corporationName" label="公司" />
            <el-table-column label="操作" width="100">
              <template #default="{ row }">
                <el-button type="danger" size="small" link @click="unbindCharacter(row.characterId)">
                  解绑
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </div>

        <el-empty v-else description="暂无绑定的角色" :image-size="60" />
      </div>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Search, ArrowDown, User, SwitchButton } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '../stores/auth'
import { blueprintApi, marketApi, marketDataApi, type Blueprint, type CostBreakdownItem, type CalculateCostsRequest } from '../api'

const router = useRouter()
const authStore = useAuthStore()

const blueprints = ref<Blueprint[]>([])
const selectedBlueprint = ref<Blueprint | null>(null)
const searchText = ref('')
const statusText = ref('')
const loading = ref(false)
const calculating = ref(false)
const loadingBreakdown = ref(false)
const fetchingOrders = ref(false)
const fetchingHistory = ref(false)
const costBreakdownText = ref('')
const costBreakdownTree = ref<any[]>([])
const analysisResult = ref('')
const currentPage = ref(1)
const pageSize = ref(50)
const totalBlueprints = ref(0)
const regionId = ref(10000002)
const marketDataStatus = ref<{
  orderCount: number;
  historyCount: number;
  hasMarketData: boolean;
}>({ orderCount: 0, historyCount: 0, hasMarketData: false })

const characterDialogVisible = ref(false)

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
    const updatedBlueprints = await blueprintApi.calculateCosts(calcParams.value)
    blueprints.value = updatedBlueprints
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

async function handleLogout() {
  await authStore.logout()
  ElMessage.success('已退出登录')
  router.push('/login')
}

async function handleUserCommand(command: string) {
  if (command === 'logout') {
    await handleLogout()
  } else if (command === 'characters') {
    showCharacterDialog()
  }
}

function showCharacterDialog() {
  authStore.fetchCharacters()
  characterDialogVisible.value = true
}

async function bindCharacter() {
  try {
    await authStore.bindCharacter()
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '获取授权链接失败')
  }
}

async function unbindCharacter(characterId: number) {
  try {
    await ElMessageBox.confirm('确定要解绑这个EVE角色吗？', '确认解绑', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    await authStore.unbindCharacter(characterId)
    ElMessage.success('已解绑')
  } catch {
  }
}

async function fetchMarketOrders() {
  fetchingOrders.value = true
  statusText.value = '正在从 ESI 获取市场订单数据...'
  
  try {
    const result = await marketDataApi.fetchMarketOrders(regionId.value)
    statusText.value = result.message
    ElMessage.success('市场订单获取完成')
  } catch (error: any) {
    statusText.value = '获取失败: ' + (error.response?.data?.message || error.message || '未知错误')
    ElMessage.error('获取市场订单失败')
    console.error(error)
  } finally {
    fetchingOrders.value = false
  }
}

async function fetchMarketHistory() {
  fetchingHistory.value = true
  statusText.value = '正在从 ESI 获取市场历史数据（可能需要几分钟）...'
  
  try {
    const result = await marketDataApi.fetchMarketHistory(regionId.value)
    statusText.value = result.message
    ElMessage.success('市场历史数据获取完成')
  } catch (error: any) {
    statusText.value = '获取失败: ' + (error.response?.data?.message || error.message || '未知错误')
    ElMessage.error('获取市场历史数据失败')
    console.error(error)
  } finally {
    fetchingHistory.value = false
  }
}

onMounted(async () => {
  statusText.value = '点击"加载蓝图数据"按钮开始'
  
  try {
    marketDataStatus.value = await marketDataApi.getStatus()
    if (!marketDataStatus.value.hasMarketData) {
      statusText.value = '提示: 没有市场数据，请先点击"获取市场订单"按钮'
    }
  } catch (e) {
    console.error('Failed to get market data status', e)
  }
})
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600&display=swap');

.app-container {
  min-height: 100vh;
  background: #ffffff;
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  color: #111111;
}

.header {
  background: #ffffff;
  border-bottom: 1px solid #f5f5f5;
  color: #000000;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 24px 48px;
  position: sticky;
  top: 0;
  z-index: 100;
}

.header h1 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  letter-spacing: -0.03em;
  text-transform: uppercase;
}

.header h1::before {
  content: '';
  display: none;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 16px;
  font-size: 14px;
}

.main {
  padding: 24px 32px;
  max-width: 1600px;
  margin: 0 auto;
}

.control-panel {
  margin-bottom: 24px;
}

.control-panel :deep(.el-card) {
  border: none;
  border-radius: 0;
  box-shadow: none;
  background: transparent;
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

.search-input :deep(.el-input__wrapper) {
  border-radius: 0;
  background: transparent;
  box-shadow: none;
  border: none;
  border-bottom: 1px solid #cccccc;
}

.search-input :deep(.el-input__wrapper):hover {
  border-bottom-color: #999999;
}

.search-input :deep(.el-input__wrapper.is-focus) {
  border-bottom-color: #111111;
  box-shadow: none;
}

.search-input :deep(.el-input__inner) {
  color: #111111;
}

.search-input :deep(.el-input__inner::placeholder) {
  color: #999999;
}

.param-select {
  width: 100px;
}

.param-select :deep(.el-input__wrapper) {
  border-radius: 0;
  background: transparent;
  box-shadow: none;
  border: none;
  border-bottom: 1px solid #cccccc;
}

.param-select :deep(.el-input__wrapper):hover {
  border-bottom-color: #999999;
}

.param-select :deep(.el-input__wrapper.is-focus) {
  border-bottom-color: #111111;
  box-shadow: none;
}

.param-select :deep(.el-select__placeholder) {
  color: #999999;
}

.param-select :deep(.el-select__selected-item) {
  color: #111111;
}

.button-group {
  margin-top: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.button-group .el-button {
  border-radius: 0;
  font-weight: 500;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  border: 1px solid #e0e0e0;
  background: #ffffff;
  color: #000000;
  transition: all 0.2s ease;
}

.button-group .el-button:hover {
  border-color: #000000;
  background: #000000;
  color: #ffffff;
}

.button-group .el-button--primary {
  background: #000000;
  border-color: #000000;
  color: #ffffff;
}

.button-group .el-button--primary:hover {
  background: #333333;
  border-color: #333333;
  color: #ffffff;
}

.status-bar {
  margin-bottom: 32px;
  padding: 16px 0;
  background: transparent;
  border: none;
  border-left: 3px solid #000000;
  color: #666666;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding-left: 16px;
}

.main-content {
  margin-bottom: 24px;
}

.main-content :deep(.el-card) {
  border: none;
  border-radius: 0;
  box-shadow: none;
  background: transparent;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-weight: 500;
}

.card-header span {
  font-size: 14px;
  color: #111111;
}

:deep(.el-table) {
  font-size: 12px;
  background: transparent !important;
  --el-table-bg-color: transparent;
  --el-table-tr-bg-color: transparent;
  --el-table-row-hover-bg-color: #fafafa;
  --el-table-header-bg-color: transparent;
  --el-table-border-color: #f0f0f0;
  --el-table-text-color: #000000;
  --el-table-header-text-color: #000000;
}

:deep(.el-table th) {
  background-color: transparent !important;
  color: #000000 !important;
  font-weight: 600;
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  border-bottom: 2px solid #000000 !important;
}

:deep(.el-table td) {
  border-bottom: 1px solid #f5f5f5 !important;
}

:deep(.el-table tr) {
  cursor: pointer;
  transition: background-color 0.2s ease;
}

:deep(.el-table tr:hover > td) {
  background-color: #fafafa !important;
}

:deep(.el-table .selected-row) {
  background-color: #f5f5f5 !important;
}

.cost-zero {
  color: #b0b0b0;
}

.cost-none {
  color: #888888;
  font-size: 11px;
}

.cost-pending {
  color: #b0b0b0;
  font-size: 11px;
}

.cost-breakdown {
  height: 500px;
  overflow: auto;
}

.cost-breakdown pre {
  margin: 0;
  font-size: 11px;
  white-space: pre-wrap;
  word-wrap: break-word;
  color: #555555;
}

.tree-node {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: #000000;
}

.cost-high {
  color: #000000;
  font-weight: 700;
}

.cost-medium {
  color: #555555;
  font-weight: 500;
}

:deep(.el-tree) {
  background: transparent;
}

:deep(.el-tree-node__content) {
  height: 28px;
  border-radius: 0;
  color: #333333;
}

:deep(.el-tree-node__content:hover) {
  background-color: #fafafa;
}

:deep(.el-empty__description) {
  color: #cccccc;
}

:deep(.el-pagination) {
  --el-pagination-button-bg-color: transparent;
  --el-pagination-hover-color: #111111;
  --el-pagination-button-color: #666666;
  --el-pagination-text-color: #666666;
}

:deep(.el-tag) {
  font-size: 11px;
  border: none;
}

:deep(.el-tag--info) {
  background: #f5f5f5;
  color: #666666;
}

:deep(.el-tag--warning) {
  background: #f5f5f5;
  color: #666666;
}

:deep(.el-tag--success) {
  background: #111111;
  color: #ffffff;
}

.user-dropdown {
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 4px;
}

.character-dialog-content {
  padding: 10px 0;
}

.dialog-desc {
  color: #666;
  font-size: 14px;
  margin-bottom: 20px;
}

.bind-btn {
  margin-bottom: 20px;
}

.character-list h4 {
  margin: 0 0 12px 0;
  font-size: 14px;
  color: #333;
}

@media (max-width: 768px) {
  .main {
    padding: 12px;
  }
  
  .header {
    padding: 12px 16px;
    flex-direction: column;
    gap: 12px;
  }
  
  .header h1 {
    font-size: 18px;
  }
  
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
    margin-bottom: 12px;
  }
  
  :deep(.el-table) {
    font-size: 11px;
  }
  
  .status-bar {
    font-size: 11px;
    padding: 10px 14px;
  }
}
</style>

<template>
  <div class="manufacturing-assistant">
    <el-row :gutter="20">
      <el-col :span="6">
        <el-card class="sidebar-card">
          <template #header>
            <div class="card-header">
              <span>制造助手</span>
            </div>
          </template>
          
          <el-menu :default-active="activeMenu" @select="handleMenuSelect">
            <el-menu-item index="profit">
              <el-icon><TrendCharts /></el-icon>
              <span>利润分析</span>
            </el-menu-item>
            <el-menu-item index="queue">
              <el-icon><List /></el-icon>
              <span>制造队列</span>
            </el-menu-item>
            <el-menu-item index="materials">
              <el-icon><Box /></el-icon>
              <span>材料计算</span>
            </el-menu-item>
            <el-menu-item index="settings">
              <el-icon><Setting /></el-icon>
              <span>制造设置</span>
            </el-menu-item>
          </el-menu>
        </el-card>
      </el-col>

      <el-col :span="18">
        <el-card class="main-card">
          <template #header>
            <div class="card-header">
              <span>{{ menuTitles[activeMenu] }}</span>
              <div class="header-actions">
                <el-button v-if="activeMenu === 'profit'" type="primary" @click="refreshProfitData" :loading="loading">
                  <el-icon><Refresh /></el-icon>
                  刷新数据
                </el-button>
                <el-button v-if="activeMenu === 'queue'" type="success" @click="showAddQueueDialog">
                  <el-icon><Plus /></el-icon>
                  添加任务
                </el-button>
              </div>
            </div>
          </template>

          <div v-if="activeMenu === 'profit'" class="profit-section">
            <div class="filter-bar">
              <el-input
                v-model="profitFilter.search"
                placeholder="搜索蓝图..."
                clearable
                class="search-input"
                @input="filterProfitList"
              >
                <template #prefix>
                  <el-icon><Search /></el-icon>
                </template>
              </el-input>
              
              <el-select v-model="profitFilter.type" placeholder="蓝图类型" clearable @change="filterProfitList">
                <el-option label="全部" value="" />
                <el-option label="T1" value="t1" />
                <el-option label="T2" value="t2" />
                <el-option label="反应" value="reaction" />
              </el-select>

              <el-select v-model="profitFilter.sortBy" placeholder="排序方式" @change="sortProfitList">
                <el-option label="利润率" value="profitRate" />
                <el-option label="利润额" value="profit" />
                <el-option label="制造时间" value="time" />
                <el-option label="日均利润" value="dailyProfit" />
              </el-select>

              <el-switch
                v-model="profitFilter.hideNegative"
                active-text="隐藏负利润"
                @change="filterProfitList"
              />
            </div>

            <el-table
              :data="filteredProfitList"
              v-loading="loading"
              stripe
              size="small"
              @row-click="handleProfitRowClick"
              highlight-current-row
              max-height="600"
            >
              <el-table-column prop="name" label="蓝图名称" min-width="180" />
              <el-table-column label="类型" width="70" align="center">
                <template #default="{ row }">
                  <el-tag :type="getTypeTagType(row.type)" size="small">{{ row.type }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="制造成本" width="110" align="right">
                <template #default="{ row }">
                  <span class="cost">{{ formatISK(row.manufacturingCost) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="市场售价" width="110" align="right">
                <template #default="{ row }">
                  <span>{{ formatISK(row.marketPrice) }}</span>
                </template>
              </el-table-column>
              <el-table-column label="利润" width="100" align="right">
                <template #default="{ row }">
                  <span :class="row.profit >= 0 ? 'profit-positive' : 'profit-negative'">
                    {{ formatISK(row.profit) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="利润率" width="90" align="center">
                <template #default="{ row }">
                  <el-tag :type="getProfitTagType(row.profitRate)" size="small">
                    {{ (row.profitRate * 100).toFixed(1) }}%
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="制造时间" width="90" align="center">
                <template #default="{ row }">
                  {{ formatTime(row.manufacturingTime) }}
                </template>
              </el-table-column>
              <el-table-column label="日均利润" width="110" align="right">
                <template #default="{ row }">
                  <span :class="row.dailyProfit >= 0 ? 'profit-positive' : 'profit-negative'">
                    {{ formatISK(row.dailyProfit) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="100" align="center" fixed="right">
                <template #default="{ row }">
                  <el-button type="primary" size="small" link @click.stop="addToQueue(row)">
                    <el-icon><Plus /></el-icon>
                    加入队列
                  </el-button>
                </template>
              </el-table-column>
            </el-table>

            <div class="profit-summary" v-if="filteredProfitList.length > 0">
              <el-statistic title="显示项目" :value="filteredProfitList.length" />
              <el-statistic title="平均利润率" :value="(avgProfitRate * 100).toFixed(2)" suffix="%" />
              <el-statistic title="最高日均利润" :value="formatISK(maxDailyProfit)" />
            </div>
          </div>

          <div v-if="activeMenu === 'queue'" class="queue-section">
            <el-table :data="manufacturingQueue" stripe size="small">
              <el-table-column type="index" width="50" label="#" />
              <el-table-column prop="name" label="蓝图名称" min-width="180" />
              <el-table-column label="数量" width="80" align="center">
                <template #default="{ row }">
                  <el-input-number
                    v-model="row.quantity"
                    :min="1"
                    :max="row.maxLimit"
                    size="small"
                    controls-position="right"
                    @change="recalculateQueueItem(row)"
                  />
                </template>
              </el-table-column>
              <el-table-column label="总成本" width="110" align="right">
                <template #default="{ row }">
                  {{ formatISK(row.totalCost) }}
                </template>
              </el-table-column>
              <el-table-column label="预计收入" width="110" align="right">
                <template #default="{ row }">
                  {{ formatISK(row.totalRevenue) }}
                </template>
              </el-table-column>
              <el-table-column label="预计利润" width="110" align="right">
                <template #default="{ row }">
                  <span :class="row.totalProfit >= 0 ? 'profit-positive' : 'profit-negative'">
                    {{ formatISK(row.totalProfit) }}
                  </span>
                </template>
              </el-table-column>
              <el-table-column label="总时间" width="100" align="center">
                <template #default="{ row }">
                  {{ formatTime(row.totalTime) }}
                </template>
              </el-table-column>
              <el-table-column label="状态" width="90" align="center">
                <template #default="{ row }">
                  <el-tag :type="getStatusTagType(row.status)" size="small">
                    {{ statusLabels[row.status] }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="操作" width="120" align="center" fixed="right">
                <template #default="{ row, $index }">
                  <el-button-group>
                    <el-button
                      v-if="row.status === 'pending'"
                      type="success"
                      size="small"
                      @click="startManufacturing(row)"
                    >
                      开始
                    </el-button>
                    <el-button
                      v-if="row.status === 'in_progress'"
                      type="warning"
                      size="small"
                      @click="completeManufacturing(row)"
                    >
                      完成
                    </el-button>
                    <el-button
                      type="danger"
                      size="small"
                      @click="removeFromQueue($index)"
                    >
                      删除
                    </el-button>
                  </el-button-group>
                </template>
              </el-table-column>
            </el-table>

            <div class="queue-summary" v-if="manufacturingQueue.length > 0">
              <el-card shadow="never">
                <div class="summary-grid">
                  <div class="summary-item">
                    <span class="label">队列任务数</span>
                    <span class="value">{{ manufacturingQueue.length }}</span>
                  </div>
                  <div class="summary-item">
                    <span class="label">总投入成本</span>
                    <span class="value cost">{{ formatISK(queueTotalCost) }}</span>
                  </div>
                  <div class="summary-item">
                    <span class="label">预计总收入</span>
                    <span class="value">{{ formatISK(queueTotalRevenue) }}</span>
                  </div>
                  <div class="summary-item">
                    <span class="label">预计总利润</span>
                    <span class="value" :class="queueTotalProfit >= 0 ? 'profit-positive' : 'profit-negative'">
                      {{ formatISK(queueTotalProfit) }}
                    </span>
                  </div>
                </div>
              </el-card>
              
              <el-button type="primary" @click="exportQueueMaterials" class="export-btn">
                <el-icon><Download /></el-icon>
                导出材料清单
              </el-button>
            </div>

            <el-empty v-else description="制造队列为空，请从利润分析添加任务" />
          </div>

          <div v-if="activeMenu === 'materials'" class="materials-section">
            <div class="materials-header">
              <el-input
                v-model="materialSearch"
                placeholder="搜索蓝图添加到计算..."
                @input="searchBlueprintsForMaterials"
                clearable
                class="search-input"
              >
                <template #prefix>
                  <el-icon><Search /></el-icon>
                </template>
              </el-input>
            </div>

            <div v-if="blueprintSearchResults.length > 0 && materialSearch" class="search-results">
              <el-card
                v-for="bp in blueprintSearchResults.slice(0, 5)"
                :key="bp.blueprintTypeId"
                class="result-card"
                shadow="hover"
                @click="addBlueprintToMaterials(bp)"
              >
                <div class="result-info">
                  <span class="name">{{ bp.name }}</span>
                  <el-tag :type="getTypeTagType(getBlueprintType(bp))" size="small">{{ getBlueprintType(bp) }}</el-tag>
                </div>
                <el-icon><Plus /></el-icon>
              </el-card>
            </div>

            <div v-if="selectedBlueprints.length > 0" class="selected-blueprints">
              <h4>已选择的蓝图</h4>
              <el-tag
                v-for="bp in selectedBlueprints"
                :key="bp.blueprintTypeId"
                closable
                @close="removeBlueprintFromMaterials(bp.blueprintTypeId)"
                class="bp-tag"
              >
                {{ bp.name }} x{{ bp.quantity }}
                <el-input-number
                  v-model="bp.quantity"
                  :min="1"
                  size="small"
                  controls-position="right"
                  class="qty-input"
                  @change="recalculateMaterials"
                />
              </el-tag>
            </div>

            <el-divider />

            <div v-if="materialRequirements.length > 0" class="materials-list">
              <h4>所需材料清单</h4>
              <el-table :data="materialRequirements" stripe size="small">
                <el-table-column prop="name" label="材料名称" min-width="180" />
                <el-table-column label="需求数量" width="120" align="right">
                  <template #default="{ row }">
                    {{ formatNumber(row.required) }}
                  </template>
                </el-table-column>
                <el-table-column label="单价" width="100" align="right">
                  <template #default="{ row }">
                    {{ formatISK(row.unitPrice) }}
                  </template>
                </el-table-column>
                <el-table-column label="总价" width="110" align="right">
                  <template #default="{ row }">
                    {{ formatISK(row.totalPrice) }}
                  </template>
                </el-table-column>
              </el-table>

              <div class="materials-total">
                <span>材料总成本: </span>
                <span class="total-cost">{{ formatISK(materialsTotalCost) }}</span>
              </div>
            </div>

            <el-empty v-else description="请搜索并添加蓝图以计算材料需求" />
          </div>

          <div v-if="activeMenu === 'settings'" class="settings-section">
            <el-form label-width="140px" label-position="left">
              <el-divider content-position="left">制造建筑设置</el-divider>
              
              <el-form-item label="材料效率 (ME)">
                <el-select v-model="manufacturingSettings.me" @change="saveSettings">
                  <el-option v-for="i in 10" :key="i-1" :label="'ME ' + (i-1)" :value="i-1" />
                </el-select>
                <span class="hint">每级减少1%材料需求</span>
              </el-form-item>

              <el-form-item label="时间效率 (TE)">
                <el-select v-model="manufacturingSettings.te" @change="saveSettings">
                  <el-option v-for="i in 21" :key="i-1" :label="'TE ' + (i-1)" :value="i-1" />
                </el-select>
                <span class="hint">每级减少1%制造时间</span>
              </el-form-item>

              <el-form-item label="建筑时间加成">
                <el-select v-model="manufacturingSettings.structureBonus" @change="saveSettings">
                  <el-option v-for="i in 21" :key="i-1" :label="(i-1) + '%'" :value="i-1" />
                </el-select>
                <span class="hint">Azumul/Sotiyo等建筑加成</span>
              </el-form-item>

              <el-form-item label="RIG时间加成">
                <el-select v-model="manufacturingSettings.rigBonus" @change="saveSettings">
                  <el-option v-for="i in 11" :key="i-1" :label="(i-1) * 5 + '%'" :value="(i-1) * 5" />
                </el-select>
                <span class="hint">建筑装备RIG加成，最大50%</span>
              </el-form-item>

              <el-divider content-position="left">技能设置</el-divider>

              <el-form-item label="工业技能">
                <el-select v-model="manufacturingSettings.industryLevel" @change="saveSettings">
                  <el-option v-for="i in 6" :key="i-1" :label="'等级 ' + (i-1)" :value="i-1" />
                </el-select>
                <span class="hint">每级减少4%制造时间</span>
              </el-form-item>

              <el-form-item label="高级工业">
                <el-select v-model="manufacturingSettings.advancedIndustryLevel" @change="saveSettings">
                  <el-option v-for="i in 6" :key="i-1" :label="'等级 ' + (i-1)" :value="i-1" />
                </el-select>
                <span class="hint">每级减少3%制造时间</span>
              </el-form-item>

              <el-divider content-position="left">反应设置</el-divider>

              <el-form-item label="反应技能">
                <el-select v-model="manufacturingSettings.reactionLevel" @change="saveSettings">
                  <el-option v-for="i in 6" :key="i-1" :label="'等级 ' + (i-1)" :value="i-1" />
                </el-select>
                <span class="hint">每级减少4%反应时间</span>
              </el-form-item>

              <el-form-item label="反应建筑加成">
                <el-select v-model="manufacturingSettings.reactionStructureBonus" @change="saveSettings">
                  <el-option v-for="i in 21" :key="i-1" :label="(i-1) + '%'" :value="i-1" />
                </el-select>
              </el-form-item>

              <el-form-item label="反应RIG加成">
                <el-select v-model="manufacturingSettings.reactionRigBonus" @change="saveSettings">
                  <el-option v-for="i in 11" :key="i-1" :label="(i-1) * 5 + '%'" :value="(i-1) * 5" />
                </el-select>
              </el-form-item>

              <el-divider content-position="left">市场设置</el-divider>

              <el-form-item label="销售税率">
                <el-input-number
                  v-model="manufacturingSettings.salesTax"
                  :min="0"
                  :max="100"
                  :step="0.5"
                  :precision="1"
                  @change="saveSettings"
                />
                <span class="hint">空间站税率（默认8%）</span>
              </el-form-item>

              <el-form-item label="经纪人费用">
                <el-input-number
                  v-model="manufacturingSettings.brokerFee"
                  :min="0"
                  :max="100"
                  :step="0.5"
                  :precision="1"
                  @change="saveSettings"
                />
                <span class="hint">挂单费用（默认3%）</span>
              </el-form-item>
            </el-form>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-dialog v-model="queueDialogVisible" title="添加制造任务" width="500px">
      <el-form :model="queueForm" label-width="100px">
        <el-form-item label="蓝图">
          <span>{{ queueForm.name }}</span>
        </el-form-item>
        <el-form-item label="制造数量">
          <el-input-number v-model="queueForm.quantity" :min="1" :max="queueForm.maxLimit" />
        </el-form-item>
        <el-form-item label="预计成本">
          <span>{{ formatISK(queueForm.quantity * queueForm.unitCost) }}</span>
        </el-form-item>
        <el-form-item label="预计收入">
          <span>{{ formatISK(queueForm.quantity * queueForm.marketPrice) }}</span>
        </el-form-item>
        <el-form-item label="预计利润">
          <span :class="queueForm.quantity * queueForm.profit >= 0 ? 'profit-positive' : 'profit-negative'">
            {{ formatISK(queueForm.quantity * queueForm.profit) }}
          </span>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="queueDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmAddToQueue">确认添加</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="detailDialogVisible" :title="selectedBlueprintDetail?.name" width="600px">
      <div v-if="selectedBlueprintDetail" class="detail-content">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="蓝图类型">
            <el-tag :type="getTypeTagType(selectedBlueprintDetail.type)">
              {{ selectedBlueprintDetail.type }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="TypeID">
            {{ selectedBlueprintDetail.blueprintTypeId }}
          </el-descriptions-item>
          <el-descriptions-item label="制造成本">
            {{ formatISK(selectedBlueprintDetail.manufacturingCost) }}
          </el-descriptions-item>
          <el-descriptions-item label="市场售价">
            {{ formatISK(selectedBlueprintDetail.marketPrice) }}
          </el-descriptions-item>
          <el-descriptions-item label="单件利润">
            <span :class="selectedBlueprintDetail.profit >= 0 ? 'profit-positive' : 'profit-negative'">
              {{ formatISK(selectedBlueprintDetail.profit) }}
            </span>
          </el-descriptions-item>
          <el-descriptions-item label="利润率">
            <el-tag :type="getProfitTagType(selectedBlueprintDetail.profitRate)">
              {{ (selectedBlueprintDetail.profitRate * 100).toFixed(2) }}%
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="制造时间">
            {{ formatTime(selectedBlueprintDetail.manufacturingTime) }}
          </el-descriptions-item>
          <el-descriptions-item label="日均利润">
            <span :class="selectedBlueprintDetail.dailyProfit >= 0 ? 'profit-positive' : 'profit-negative'">
              {{ formatISK(selectedBlueprintDetail.dailyProfit) }}
            </span>
          </el-descriptions-item>
        </el-descriptions>

        <el-divider content-position="left">成本构成</el-divider>
        <div class="cost-breakdown" v-loading="loadingBreakdown">
          <el-tree
            v-if="costBreakdownTree.length > 0"
            :data="costBreakdownTree"
            :props="{ children: 'children', label: 'label' }"
            default-expand-all
          />
          <el-empty v-else description="加载中..." :image-size="60" />
        </div>
      </div>
      <template #footer>
        <el-button @click="detailDialogVisible = false">关闭</el-button>
        <el-button type="primary" @click="addToQueue(selectedBlueprintDetail!)">
          加入制造队列
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { 
  TrendCharts, List, Box, Setting, Refresh, Plus, Search, Download 
} from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { blueprintApi, type Blueprint, type CostBreakdownItem, type CalculateCostsRequest } from '../api'

interface ProfitItem {
  blueprintTypeId: number
  name: string
  type: string
  manufacturingCost: number
  marketPrice: number
  profit: number
  profitRate: number
  manufacturingTime: number
  dailyProfit: number
  maxLimit: number
}

interface QueueItem {
  blueprintTypeId: number
  name: string
  type: string
  quantity: number
  unitCost: number
  marketPrice: number
  profit: number
  maxLimit: number
  totalCost: number
  totalRevenue: number
  totalProfit: number
  totalTime: number
  status: 'pending' | 'in_progress' | 'completed'
}

interface MaterialItem {
  typeId: number
  name: string
  required: number
  unitPrice: number
  totalPrice: number
}

interface BlueprintForMaterial extends Blueprint {
  quantity: number
}

const activeMenu = ref('profit')
const loading = ref(false)
const loadingBreakdown = ref(false)

const menuTitles: Record<string, string> = {
  profit: '利润分析',
  queue: '制造队列',
  materials: '材料计算',
  settings: '制造设置'
}

const statusLabels: Record<string, string> = {
  pending: '待开始',
  in_progress: '进行中',
  completed: '已完成'
}

const profitList = ref<ProfitItem[]>([])
const filteredProfitList = ref<ProfitItem[]>([])
const manufacturingQueue = ref<QueueItem[]>([])
const selectedBlueprintDetail = ref<ProfitItem | null>(null)
const costBreakdownTree = ref<any[]>([])

const profitFilter = reactive({
  search: '',
  type: '',
  sortBy: 'profitRate',
  hideNegative: true
})

const materialSearch = ref('')
const blueprintSearchResults = ref<Blueprint[]>([])
const selectedBlueprints = ref<BlueprintForMaterial[]>([])
const materialRequirements = ref<MaterialItem[]>([])

const queueDialogVisible = ref(false)
const detailDialogVisible = ref(false)
const queueForm = reactive({
  blueprintTypeId: 0,
  name: '',
  quantity: 1,
  unitCost: 0,
  marketPrice: 0,
  profit: 0,
  maxLimit: 100
})

const manufacturingSettings = reactive({
  me: 0,
  te: 0,
  structureBonus: 0,
  rigBonus: 0,
  industryLevel: 5,
  advancedIndustryLevel: 0,
  reactionLevel: 0,
  reactionStructureBonus: 0,
  reactionRigBonus: 0,
  salesTax: 8,
  brokerFee: 3
})

const avgProfitRate = computed(() => {
  if (filteredProfitList.value.length === 0) return 0
  const sum = filteredProfitList.value.reduce((acc, item) => acc + item.profitRate, 0)
  return sum / filteredProfitList.value.length
})

const maxDailyProfit = computed(() => {
  if (filteredProfitList.value.length === 0) return 0
  return Math.max(...filteredProfitList.value.map(item => item.dailyProfit))
})

const queueTotalCost = computed(() => 
  manufacturingQueue.value.reduce((sum, item) => sum + item.totalCost, 0)
)

const queueTotalRevenue = computed(() => 
  manufacturingQueue.value.reduce((sum, item) => sum + item.totalRevenue, 0)
)

const queueTotalProfit = computed(() => 
  manufacturingQueue.value.reduce((sum, item) => sum + item.totalProfit, 0)
)

const materialsTotalCost = computed(() =>
  materialRequirements.value.reduce((sum, item) => sum + item.totalPrice, 0)
)

function handleMenuSelect(index: string) {
  activeMenu.value = index
}

function formatISK(num: number): string {
  if (num >= 1000000000) return (num / 1000000000).toFixed(2) + 'B'
  if (num >= 1000000) return (num / 1000000).toFixed(2) + 'M'
  if (num >= 1000) return (num / 1000).toFixed(2) + 'K'
  return num.toFixed(2)
}

function formatNumber(num: number): string {
  return num.toLocaleString()
}

function formatTime(seconds: number): string {
  if (seconds < 60) return seconds.toFixed(0) + '秒'
  if (seconds < 3600) return (seconds / 60).toFixed(0) + '分钟'
  if (seconds < 86400) return (seconds / 3600).toFixed(1) + '小时'
  return (seconds / 86400).toFixed(1) + '天'
}

function getTypeTagType(type: string): string {
  switch (type) {
    case 'T1': return 'info'
    case 'T2': return 'warning'
    case '反应': return 'success'
    default: return ''
  }
}

function getBlueprintType(bp: Blueprint): string {
  if (bp.activities?.invention) return 'T2'
  if (bp.activities?.reaction) return '反应'
  return 'T1'
}

function getProfitTagType(rate: number): string {
  if (rate >= 0.3) return 'success'
  if (rate >= 0.1) return ''
  if (rate >= 0) return 'warning'
  return 'danger'
}

function getStatusTagType(status: string): string {
  switch (status) {
    case 'pending': return 'info'
    case 'in_progress': return 'warning'
    case 'completed': return 'success'
    default: return ''
  }
}

async function refreshProfitData() {
  loading.value = true
  try {
    const calcParams: CalculateCostsRequest = {
      ME: manufacturingSettings.me,
      TE: manufacturingSettings.te,
      StructureBonus: manufacturingSettings.structureBonus,
      RigBonus: manufacturingSettings.rigBonus,
      IndustryLevel: manufacturingSettings.industryLevel,
      AdvancedIndustryLevel: manufacturingSettings.advancedIndustryLevel,
      ReactionStructureBonus: manufacturingSettings.reactionStructureBonus,
      ReactionRigBonus: manufacturingSettings.reactionRigBonus,
      ReactionLevel: manufacturingSettings.reactionLevel
    }
    
    const calculatedBlueprints = await blueprintApi.calculateCosts(calcParams)
    
    profitList.value = calculatedBlueprints
      .filter(bp => bp.manufacturingCost > 0)
      .map(bp => {
        const cost = bp.manufacturingCost || 0
        const time = bp.manufacturingTime || 0
        
        const marketPrice = cost * (1 + Math.random() * 0.5)
        const salesTaxRate = manufacturingSettings.salesTax / 100
        const brokerFeeRate = manufacturingSettings.brokerFee / 100
        const netPrice = marketPrice * (1 - salesTaxRate - brokerFeeRate)
        
        const profit = netPrice - cost
        const profitRate = cost > 0 ? profit / cost : 0
        const dailyProfit = time > 0 ? (profit * 86400) / time : 0
        
        let type = 'T1'
        if (bp.activities?.invention) type = 'T2'
        else if (bp.activities?.reaction) type = '反应'
        
        return {
          blueprintTypeId: bp.blueprintTypeId,
          name: bp.name,
          type,
          manufacturingCost: cost,
          marketPrice: netPrice,
          profit,
          profitRate,
          manufacturingTime: time,
          dailyProfit,
          maxLimit: bp.maxProductionLimit || 100
        }
      })
    
    filterProfitList()
    ElMessage.success('数据刷新成功')
  } catch (error: any) {
    ElMessage.error('刷新数据失败: ' + (error.message || '未知错误'))
  } finally {
    loading.value = false
  }
}

function filterProfitList() {
  let result = [...profitList.value]
  
  if (profitFilter.search) {
    const search = profitFilter.search.toLowerCase()
    result = result.filter(item => item.name.toLowerCase().includes(search))
  }
  
  if (profitFilter.type) {
    result = result.filter(item => item.type === profitFilter.type)
  }
  
  if (profitFilter.hideNegative) {
    result = result.filter(item => item.profit > 0)
  }
  
  sortProfitList()
  filteredProfitList.value = result
}

function sortProfitList() {
  const sorted = [...filteredProfitList.value]
  
  switch (profitFilter.sortBy) {
    case 'profitRate':
      sorted.sort((a, b) => b.profitRate - a.profitRate)
      break
    case 'profit':
      sorted.sort((a, b) => b.profit - a.profit)
      break
    case 'time':
      sorted.sort((a, b) => a.manufacturingTime - b.manufacturingTime)
      break
    case 'dailyProfit':
      sorted.sort((a, b) => b.dailyProfit - a.dailyProfit)
      break
  }
  
  filteredProfitList.value = sorted
}

async function handleProfitRowClick(row: ProfitItem) {
  selectedBlueprintDetail.value = row
  detailDialogVisible.value = true
  loadingBreakdown.value = true
  
  try {
    const breakdown = await blueprintApi.getCostBreakdown(row.blueprintTypeId)
    costBreakdownTree.value = [convertToTreeData(breakdown)]
  } catch (error) {
    console.error('Failed to load breakdown', error)
  } finally {
    loadingBreakdown.value = false
  }
}

function convertToTreeData(item: CostBreakdownItem): any {
  const timeStr = item.totalTime > 0 ? ` (${formatTime(item.totalTime)})` : ''
  let label = ''
  
  if (item.isIntermediate) {
    label = `${item.name}: ${formatISK(item.totalCost)} ISK${timeStr}`
  } else {
    label = `${item.name} x${item.quantity}: ${formatISK(item.totalCost)} ISK (单价: ${item.unitCost.toFixed(2)})${timeStr}`
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

function showAddQueueDialog() {
  queueForm.blueprintTypeId = 0
  queueForm.name = ''
  queueForm.quantity = 1
  queueForm.unitCost = 0
  queueForm.marketPrice = 0
  queueForm.profit = 0
  queueForm.maxLimit = 100
  queueDialogVisible.value = true
}

function addToQueue(item: ProfitItem) {
  queueForm.blueprintTypeId = item.blueprintTypeId
  queueForm.name = item.name
  queueForm.quantity = 1
  queueForm.unitCost = item.manufacturingCost
  queueForm.marketPrice = item.marketPrice
  queueForm.profit = item.profit
  queueForm.maxLimit = item.maxLimit
  queueDialogVisible.value = true
  detailDialogVisible.value = false
}

function confirmAddToQueue() {
  const newItem: QueueItem = {
    blueprintTypeId: queueForm.blueprintTypeId,
    name: queueForm.name,
    type: '',
    quantity: queueForm.quantity,
    unitCost: queueForm.unitCost,
    marketPrice: queueForm.marketPrice,
    profit: queueForm.profit,
    maxLimit: queueForm.maxLimit,
    totalCost: queueForm.quantity * queueForm.unitCost,
    totalRevenue: queueForm.quantity * queueForm.marketPrice,
    totalProfit: queueForm.quantity * queueForm.profit,
    totalTime: 0,
    status: 'pending'
  }
  
  manufacturingQueue.value.push(newItem)
  queueDialogVisible.value = false
  ElMessage.success('已添加到制造队列')
}

function recalculateQueueItem(item: QueueItem) {
  item.totalCost = item.quantity * item.unitCost
  item.totalRevenue = item.quantity * item.marketPrice
  item.totalProfit = item.quantity * item.profit
}

function startManufacturing(item: QueueItem) {
  item.status = 'in_progress'
  ElMessage.success(`开始制造: ${item.name}`)
}

function completeManufacturing(item: QueueItem) {
  item.status = 'completed'
  ElMessage.success(`制造完成: ${item.name}`)
}

function removeFromQueue(index: number) {
  manufacturingQueue.value.splice(index, 1)
  ElMessage.success('已从队列移除')
}

function exportQueueMaterials() {
  let text = '制造队列材料清单\n\n'
  text += '任务列表:\n'
  
  manufacturingQueue.value.forEach((item, index) => {
    text += `${index + 1}. ${item.name} x${item.quantity}\n`
    text += `   成本: ${formatISK(item.totalCost)} | 预计利润: ${formatISK(item.totalProfit)}\n`
  })
  
  text += `\n总投入: ${formatISK(queueTotalCost.value)}\n`
  text += `预计总收入: ${formatISK(queueTotalRevenue.value)}\n`
  text += `预计总利润: ${formatISK(queueTotalProfit.value)}\n`
  
  const blob = new Blob([text], { type: 'text/plain' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'manufacturing_queue.txt'
  a.click()
  URL.revokeObjectURL(url)
  
  ElMessage.success('材料清单已导出')
}

let searchTimeout: number | null = null

async function searchBlueprintsForMaterials() {
  if (searchTimeout) clearTimeout(searchTimeout)
  
  if (!materialSearch.value) {
    blueprintSearchResults.value = []
    return
  }
  
  searchTimeout = setTimeout(async () => {
    try {
      const result = await blueprintApi.getBlueprints(materialSearch.value, 1, 10)
      blueprintSearchResults.value = result.data
    } catch (error) {
      console.error('Search failed', error)
    }
  }, 300)
}

function addBlueprintToMaterials(bp: Blueprint) {
  const existing = selectedBlueprints.value.find(b => b.blueprintTypeId === bp.blueprintTypeId)
  if (existing) {
    existing.quantity++
  } else {
    selectedBlueprints.value.push({ ...bp, quantity: 1 })
  }
  materialSearch.value = ''
  blueprintSearchResults.value = []
  recalculateMaterials()
}

function removeBlueprintFromMaterials(typeId: number) {
  const index = selectedBlueprints.value.findIndex(b => b.blueprintTypeId === typeId)
  if (index >= 0) {
    selectedBlueprints.value.splice(index, 1)
    recalculateMaterials()
  }
}

async function recalculateMaterials() {
  if (selectedBlueprints.value.length === 0) {
    materialRequirements.value = []
    return
  }
  
  const materialsMap = new Map<number, MaterialItem>()
  
  for (const bp of selectedBlueprints.value) {
    try {
      const breakdown = await blueprintApi.getCostBreakdown(bp.blueprintTypeId)
      collectMaterials(breakdown, materialsMap, bp.quantity)
    } catch (error) {
      console.error('Failed to get breakdown for', bp.name, error)
    }
  }
  
  materialRequirements.value = Array.from(materialsMap.values())
}

function collectMaterials(item: CostBreakdownItem, map: Map<number, MaterialItem>, multiplier: number) {
  if (!item.isIntermediate && item.children.length === 0) {
    const existing = map.get(item.typeId)
    const required = item.quantity * multiplier
    
    if (existing) {
      existing.required += required
      existing.totalPrice = existing.required * existing.unitPrice
    } else {
      map.set(item.typeId, {
        typeId: item.typeId,
        name: item.name,
        required,
        unitPrice: item.unitCost,
        totalPrice: required * item.unitCost
      })
    }
  }
  
  for (const child of item.children) {
    collectMaterials(child, map, multiplier)
  }
}

function saveSettings() {
  localStorage.setItem('manufacturingSettings', JSON.stringify(manufacturingSettings))
  ElMessage.success('设置已保存')
}

function loadSettings() {
  const saved = localStorage.getItem('manufacturingSettings')
  if (saved) {
    try {
      const settings = JSON.parse(saved)
      Object.assign(manufacturingSettings, settings)
    } catch (e) {
      console.error('Failed to load settings', e)
    }
  }
}

onMounted(() => {
  loadSettings()
  refreshProfitData()
})
</script>

<style scoped>
.manufacturing-assistant {
  max-width: 1600px;
  margin: 0 auto;
}

.sidebar-card :deep(.el-card__body) {
  padding: 0;
}

.sidebar-card :deep(.el-menu) {
  border-right: none;
}

.main-card {
  min-height: 700px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.card-header span {
  font-size: 16px;
  font-weight: 600;
}

.filter-bar {
  display: flex;
  gap: 12px;
  margin-bottom: 16px;
  flex-wrap: wrap;
  align-items: center;
}

.search-input {
  width: 200px;
}

.profit-section,
.queue-section,
.materials-section,
.settings-section {
  padding: 0 4px;
}

.profit-positive {
  color: #67c23a;
  font-weight: 600;
}

.profit-negative {
  color: #f56c6c;
  font-weight: 600;
}

.cost {
  color: #909399;
}

.profit-summary {
  display: flex;
  gap: 48px;
  margin-top: 24px;
  padding: 16px;
  background: #f5f7fa;
  border-radius: 8px;
}

.queue-summary {
  margin-top: 24px;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 16px;
}

.summary-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.summary-item .label {
  font-size: 12px;
  color: #909399;
}

.summary-item .value {
  font-size: 18px;
  font-weight: 600;
}

.export-btn {
  margin-top: 16px;
}

.materials-header {
  margin-bottom: 16px;
}

.search-results {
  margin-bottom: 16px;
}

.result-card {
  margin-bottom: 8px;
  cursor: pointer;
}

.result-card :deep(.el-card__body) {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
}

.result-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.result-info .name {
  font-weight: 500;
}

.selected-blueprints {
  margin-bottom: 16px;
}

.selected-blueprints h4 {
  margin-bottom: 12px;
  color: #606266;
}

.bp-tag {
  margin-right: 8px;
  margin-bottom: 8px;
}

.qty-input {
  width: 80px;
  margin-left: 8px;
}

.materials-list h4 {
  margin-bottom: 12px;
  color: #606266;
}

.materials-total {
  margin-top: 16px;
  padding: 12px;
  background: #f5f7fa;
  border-radius: 6px;
  font-size: 14px;
}

.total-cost {
  font-weight: 600;
  font-size: 16px;
  color: #409eff;
}

.settings-section :deep(.el-form-item) {
  margin-bottom: 18px;
}

.settings-section :deep(.el-select) {
  width: 150px;
}

.settings-section :deep(.el-input-number) {
  width: 150px;
}

.hint {
  margin-left: 12px;
  font-size: 12px;
  color: #909399;
}

.detail-content {
  max-height: 500px;
  overflow-y: auto;
}

.cost-breakdown {
  max-height: 300px;
  overflow-y: auto;
}

@media (max-width: 1200px) {
  .filter-bar {
    flex-direction: column;
    align-items: stretch;
  }
  
  .search-input {
    width: 100%;
  }
  
  .summary-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .manufacturing-assistant :deep(.el-col) {
    margin-bottom: 16px;
  }
  
  .profit-summary {
    flex-direction: column;
    gap: 16px;
  }
  
  .summary-grid {
    grid-template-columns: 1fr;
  }
}
</style>

<template>
  <div class="market-page">
    <div class="status-card">
      <el-card>
        <template #header>
          <span>数据状态</span>
        </template>
        <el-row :gutter="20">
          <el-col :span="6">
            <div class="stat-item">
              <div class="stat-label">市场订单</div>
              <div class="stat-value">{{ formatNumber(marketDataStatus.orderCount) }}</div>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-item">
              <div class="stat-label">历史记录</div>
              <div class="stat-value">{{ formatNumber(marketDataStatus.historyCount) }}</div>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-item">
              <div class="stat-label">订单最新</div>
              <div class="stat-value small">{{ marketDataStatus.lastOrderSync || '-' }}</div>
            </div>
          </el-col>
          <el-col :span="6">
            <div class="stat-item">
              <div class="stat-label">历史最新</div>
              <div class="stat-value small">{{ marketDataStatus.lastHistorySync || '-' }}</div>
            </div>
          </el-col>
        </el-row>
      </el-card>
    </div>

    <div class="control-panel">
      <el-card>
        <template #header>
          <span>数据同步</span>
        </template>
        <div class="sync-form">
          <el-select v-model="regionId" placeholder="选择区域" class="region-select">
            <el-option label="The Forge (吉塔)" :value="10000002" />
            <el-option label="Domain (瑟维斯)" :value="10000043" />
            <el-option label="Sinq Laison (辛迪加)" :value="10000032" />
          </el-select>

          <div class="sync-buttons">
            <el-button 
              type="warning" 
              @click="fetchMarketOrders" 
              :loading="fetchingOrders"
              :disabled="!authStore.isAuthenticated"
            >
              获取市场订单
            </el-button>
            <el-button 
              type="info" 
              @click="fetchMarketHistory" 
              :loading="fetchingHistory"
              :disabled="!authStore.isAuthenticated"
            >
              获取历史数据
            </el-button>
          </div>
        </div>
        
        <div v-if="!authStore.isAuthenticated" class="login-tip">
          <el-icon><Warning /></el-icon>
          登录后可手动同步市场数据
        </div>
      </el-card>
    </div>

    <div class="status-bar">
      {{ statusText }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Warning } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '../stores/auth'
import { marketDataApi } from '../api'

const authStore = useAuthStore()

const regionId = ref(10000002)
const statusText = ref('')
const fetchingOrders = ref(false)
const fetchingHistory = ref(false)
const marketDataStatus = ref<{
  orderCount: number;
  historyCount: number;
  lastOrderSync?: string;
  lastHistorySync?: string;
}>({
  orderCount: 0,
  historyCount: 0
})

function formatNumber(num: number): string {
  if (num >= 1000000000) return (num / 1000000000).toFixed(2) + 'B'
  if (num >= 1000000) return (num / 1000000).toFixed(2) + 'M'
  if (num >= 1000) return (num / 1000).toFixed(2) + 'K'
  return num.toString()
}

async function fetchMarketOrders() {
  fetchingOrders.value = true
  statusText.value = '正在从 ESI 获取市场订单数据...'
  
  try {
    const result = await marketDataApi.fetchMarketOrders(regionId.value)
    statusText.value = result.message
    await loadStatus()
    ElMessage.success('市场订单获取完成')
  } catch (error: any) {
    statusText.value = '获取失败: ' + (error.response?.data?.message || error.message || '未知错误')
    ElMessage.error('获取市场订单失败')
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
    await loadStatus()
    ElMessage.success('市场历史数据获取完成')
  } catch (error: any) {
    statusText.value = '获取失败: ' + (error.response?.data?.message || error.message || '未知错误')
    ElMessage.error('获取市场历史数据失败')
  } finally {
    fetchingHistory.value = false
  }
}

async function loadStatus() {
  try {
    const status = await marketDataApi.getStatus()
    marketDataStatus.value = {
      orderCount: status.orderCount,
      historyCount: status.historyCount,
      lastOrderSync: status.lastOrderSync,
      lastHistorySync: status.lastHistorySync
    }
  } catch (e) {
    console.error('Failed to get market data status', e)
  }
}

onMounted(() => {
  loadStatus()
  statusText.value = '选择区域并点击按钮同步数据'
})
</script>

<style scoped>
.market-page {
  max-width: 1200px;
  margin: 0 auto;
}

.status-card {
  margin-bottom: 24px;
}

.status-card :deep(.el-card) {
  border-radius: 8px;
}

.stat-item {
  text-align: center;
}

.stat-label {
  font-size: 12px;
  color: #666;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin-bottom: 8px;
}

.stat-value {
  font-size: 24px;
  font-weight: 600;
  color: #000;
}

.stat-value.small {
  font-size: 14px;
  font-weight: 400;
}

.control-panel {
  margin-bottom: 24px;
}

.control-panel :deep(.el-card) {
  border-radius: 8px;
}

.sync-form {
  display: flex;
  align-items: center;
  gap: 16px;
  flex-wrap: wrap;
}

.region-select {
  width: 200px;
}

.sync-buttons {
  display: flex;
  gap: 8px;
}

.login-tip {
  margin-top: 16px;
  padding: 12px;
  background: #fff3cd;
  border: 1px solid #ffc107;
  color: #856404;
  font-size: 14px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.status-bar {
  padding: 12px 16px;
  background: #f0f9ff;
  border-left: 4px solid #409eff;
  color: #606266;
  font-size: 13px;
  border-radius: 4px;
}
</style>

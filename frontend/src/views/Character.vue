<template>
  <div class="character-page">
    <el-tabs v-model="activeTab" class="character-tabs">
      <el-tab-pane label="角色管理" name="characters">
        <div class="section-header">
          <el-button type="primary" @click="bindCharacter" :loading="binding">
            绑定新角色
          </el-button>
        </div>
        
        <el-table :data="characters" v-loading="loadingCharacters" stripe>
          <el-table-column prop="characterName" label="角色名称" min-width="150" />
          <el-table-column prop="corporationName" label="公司" min-width="150">
            <template #default="{ row }">
              {{ row.corporationName || '-' }}
            </template>
          </el-table-column>
          <el-table-column prop="allianceName" label="联盟" min-width="120">
            <template #default="{ row }">
              {{ row.allianceName || '-' }}
            </template>
          </el-table-column>
          <el-table-column label="绑定时间" width="120">
            <template #default="{ row }">
              {{ formatDate(row.createdAt) }}
            </template>
          </el-table-column>
          <el-table-column label="操作" width="100" fixed="right">
            <template #default="{ row }">
              <el-button type="danger" size="small" link @click="unbindCharacter(row.characterId)">
                解绑
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <el-empty v-if="!loadingCharacters && characters.length === 0" description="暂无绑定的角色，请绑定EVE角色以使用更多功能" />
      </el-tab-pane>

      <el-tab-pane label="产线管理" name="production">
        <div class="section-header">
          <el-button type="primary" @click="showAddLineDialog">
            新建产线
          </el-button>
        </div>

        <el-table :data="productionLines" v-loading="loadingLines" stripe>
          <el-table-column prop="name" label="产线名称" min-width="150" />
          <el-table-column prop="characterName" label="角色" min-width="120" />
          <el-table-column label="类型" width="100">
            <template #default="{ row }">
              <el-tag v-if="row.type === 'manufacturing'" size="small">制造</el-tag>
              <el-tag v-else-if="row.type === 'reaction'" type="success" size="small">反应</el-tag>
            </template>
          </el-table-column>
          <el-table-column prop="locationName" label="位置" min-width="150" />
          <el-table-column label="状态" width="80">
            <template #default="{ row }">
              <el-tag :type="row.active ? 'success' : 'info'" size="small">
                {{ row.active ? '运行中' : '暂停' }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="操作" width="150" fixed="right">
            <template #default="{ row }">
              <el-button size="small" link @click="toggleLine(row)">
                {{ row.active ? '暂停' : '启动' }}
              </el-button>
              <el-button type="danger" size="small" link @click="deleteLine(row)">
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <el-empty v-if="!loadingLines && productionLines.length === 0" description="暂无产线，请新建产线开始管理制造" />
      </el-tab-pane>

      <el-tab-pane label="资产" name="assets">
        <div class="section-header">
          <el-select v-model="selectedCharacterId" placeholder="选择角色" @change="onCharacterChange" class="character-select">
            <el-option v-for="c in characters" :key="c.characterId" :label="c.characterName" :value="c.characterId" />
          </el-select>
          <el-button @click="loadAssets" :loading="loadingAssets" :disabled="!selectedCharacterId">刷新资产</el-button>
        </div>

        <div v-if="assetSummary && !loadingAssets" class="asset-summary">
          <el-row :gutter="20">
            <el-col :span="6">
              <div class="summary-card">
                <div class="summary-label">物品总数</div>
                <div class="summary-value">{{ assetSummary.totalItems }}</div>
              </div>
            </el-col>
            <el-col :span="6">
              <div class="summary-card">
                <div class="summary-label">资产估值</div>
                <div class="summary-value highlight">{{ formatNumber(assetSummary.totalValue) }} ISK</div>
              </div>
            </el-col>
            <el-col :span="6">
              <div class="summary-card">
                <div class="summary-label">总体积</div>
                <div class="summary-value">{{ formatVolume(assetSummary.totalVolume) }} m³</div>
              </div>
            </el-col>
            <el-col :span="6">
              <div class="summary-card">
                <div class="summary-label">位置数</div>
                <div class="summary-value">{{ assetsByLocation.length }}</div>
              </div>
            </el-col>
          </el-row>
        </div>

        <el-tabs v-if="assetsByLocation.length > 0" v-model="assetSubTab" class="asset-sub-tabs">
          <el-tab-pane label="按位置" name="location">
            <el-collapse v-model="activeLocationCollapse">
              <el-collapse-item 
                v-for="location in assetsByLocation" 
                :key="location.locationId" 
                :name="location.locationId"
              >
                <template #title>
                  <div class="location-header">
                    <span class="location-name">{{ location.locationName }}</span>
                    <span class="location-count">{{ location.itemCount }} 项</span>
                    <span class="location-value">{{ formatNumber(location.totalValue) }} ISK</span>
                  </div>
                </template>
                <el-table :data="location.assets" size="small" stripe>
                  <el-table-column prop="typeName" label="物品名称" min-width="200" />
                  <el-table-column prop="quantity" label="数量" width="80" align="right" />
                  <el-table-column prop="locationFlag" label="位置" width="100" />
                  <el-table-column prop="categoryName" label="分类" width="100" />
                  <el-table-column label="单价" width="100" align="right">
                    <template #default="{ row }">
                      {{ formatNumber(row.unitPrice) }}
                    </template>
                  </el-table-column>
                  <el-table-column label="总价" width="120" align="right">
                    <template #default="{ row }">
                      {{ formatNumber(row.estimatedValue) }}
                    </template>
                  </el-table-column>
                </el-table>
              </el-collapse-item>
            </el-collapse>
          </el-tab-pane>
          
          <el-tab-pane label="按分类" name="category">
            <el-table :data="assetSummary?.byCategory || []" stripe>
              <el-table-column prop="categoryName" label="分类" min-width="150" />
              <el-table-column prop="itemCount" label="物品数量" width="100" align="right" />
              <el-table-column label="总估值" width="150" align="right">
                <template #default="{ row }">
                  {{ formatNumber(row.totalValue) }} ISK
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
        </el-tabs>

        <el-empty v-if="!loadingAssets && assetsByLocation.length === 0 && selectedCharacterId" description="该角色没有资产" />
        <el-empty v-if="!loadingAssets && !selectedCharacterId" description="请选择角色加载资产" />
      </el-tab-pane>

      <el-tab-pane label="技能" name="skills">
        <div class="section-header">
          <el-select v-model="selectedCharacterId" placeholder="选择角色" @change="loadSkills">
            <el-option v-for="c in characters" :key="c.characterId" :label="c.characterName" :value="c.characterId" />
          </el-select>
          <el-button @click="loadSkills" :loading="loadingSkills" :disabled="!selectedCharacterId">刷新技能</el-button>
        </div>

        <el-table :data="skills" v-loading="loadingSkills" stripe height="500">
          <el-table-column prop="skillName" label="技能名称" min-width="200" />
          <el-table-column prop="level" label="等级" width="80" align="center" />
          <el-table-column prop="rank" label="Rank" width="80" align="center" />
          <el-table-column prop="points" label="技能点" width="120" align="right" />
        </el-table>

        <el-empty v-if="!loadingSkills && skills.length === 0 && selectedCharacterId" description="请选择角色加载技能" />
        <el-empty v-if="!loadingSkills && !selectedCharacterId" description="请选择角色" />
      </el-tab-pane>
    </el-tabs>

    <el-dialog v-model="addLineDialogVisible" title="新建产线" width="500px">
      <el-form :model="newLine" label-width="80px">
        <el-form-item label="产线名称">
          <el-input v-model="newLine.name" placeholder="输入产线名称" />
        </el-form-item>
        <el-form-item label="角色">
          <el-select v-model="newLine.characterId" placeholder="选择角色">
            <el-option v-for="c in characters" :key="c.characterId" :label="c.characterName" :value="c.characterId" />
          </el-select>
        </el-form-item>
        <el-form-item label="类型">
          <el-radio-group v-model="newLine.type">
            <el-radio value="manufacturing">制造</el-radio>
            <el-radio value="reaction">反应</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="位置">
          <el-input v-model="newLine.locationName" placeholder="输入位置名称" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="addLineDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="addProductionLine">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '../stores/auth'
import { authApi, assetApi, type CharacterInfo, type AssetGroupedByResolvedLocation, type AssetSummaryResponse, AssetOwnerType } from '../api'

const router = useRouter()
const authStore = useAuthStore()

const activeTab = ref('characters')
const assetSubTab = ref('location')
const activeLocationCollapse = ref<number[]>([])
const binding = ref(false)
const loadingCharacters = ref(false)
const loadingLines = ref(false)
const loadingAssets = ref(false)
const loadingSkills = ref(false)
const characters = ref<CharacterInfo[]>([])
const selectedCharacterId = ref<number | null>(null)
const assetsByLocation = ref<AssetGroupedByResolvedLocation[]>([])
const assetSummary = ref<AssetSummaryResponse | null>(null)
const skills = ref<any[]>([])
const productionLines = ref<any[]>([])

const addLineDialogVisible = ref(false)
const newLine = ref({
  name: '',
  characterId: null as number | null,
  type: 'manufacturing',
  locationName: ''
})

function formatNumber(num: number): string {
  if (!num || isNaN(num)) return '0'
  if (num >= 1000000000) return (num / 1000000000).toFixed(2) + 'B'
  if (num >= 1000000) return (num / 1000000).toFixed(2) + 'M'
  if (num >= 1000) return (num / 1000).toFixed(2) + 'K'
  return num.toFixed(2)
}

function formatVolume(volume: number): string {
  if (!volume || isNaN(volume)) return '0'
  if (volume >= 1000000) return (volume / 1000000).toFixed(2) + 'M'
  if (volume >= 1000) return (volume / 1000).toFixed(2) + 'K'
  return volume.toFixed(2)
}

function formatDate(dateStr: string): string {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return `${date.getMonth() + 1}/${date.getDate()}`
}

async function loadCharacters() {
  loadingCharacters.value = true
  try {
    characters.value = await authApi.getCharacters()
    if (characters.value.length > 0 && !selectedCharacterId.value) {
      selectedCharacterId.value = characters.value[0].characterId
    }
  } catch (e) {
    console.error('Failed to load characters', e)
  } finally {
    loadingCharacters.value = false
  }
}

async function bindCharacter() {
  binding.value = true
  try {
    await authStore.bindCharacter()
  } catch (error: any) {
    ElMessage.error(error.response?.data?.message || '获取授权链接失败')
  } finally {
    binding.value = false
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
    characters.value = characters.value.filter(c => c.characterId !== characterId)
    if (selectedCharacterId.value === characterId) {
      selectedCharacterId.value = characters.value[0]?.characterId || null
    }
    ElMessage.success('已解绑')
  } catch {}
}

function onCharacterChange() {
  assetsByLocation.value = []
  assetSummary.value = null
  skills.value = []
}

function showAddLineDialog() {
  newLine.value = { name: '', characterId: null, type: 'manufacturing', locationName: '' }
  addLineDialogVisible.value = true
}

async function addProductionLine() {
  if (!newLine.value.name || !newLine.value.characterId) {
    ElMessage.warning('请填写完整信息')
    return
  }
  
  productionLines.value.push({
    ...newLine.value,
    id: Date.now(),
    characterName: characters.value.find(c => c.characterId === newLine.value.characterId)?.characterName,
    active: false
  })
  
  addLineDialogVisible.value = false
  ElMessage.success('产线已创建')
}

async function toggleLine(line: any) {
  line.active = !line.active
  ElMessage.success(line.active ? '产线已启动' : '产线已暂停')
}

async function deleteLine(line: any) {
  try {
    await ElMessageBox.confirm('确定要删除这个产线吗？', '确认删除', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    productionLines.value = productionLines.value.filter(l => l.id !== line.id)
    ElMessage.success('产线已删除')
  } catch {}
}

async function loadAssets() {
  if (!selectedCharacterId.value) {
    ElMessage.warning('请选择角色')
    return
  }
  
  loadingAssets.value = true
  try {
    const [byLocation, summary] = await Promise.all([
      assetApi.getAssetsByLocation(selectedCharacterId.value),
      assetApi.getAssetSummary(selectedCharacterId.value)
    ])
    assetsByLocation.value = byLocation
    assetSummary.value = summary
    
    if (byLocation.length > 0) {
      activeLocationCollapse.value = [byLocation[0].locationId]
    }
  } catch (e: any) {
    console.error('Failed to load assets', e)
    ElMessage.error(e.response?.data?.message || '加载资产失败')
  } finally {
    loadingAssets.value = false
  }
}

async function loadSkills() {
  if (!selectedCharacterId.value) {
    ElMessage.warning('请选择角色')
    return
  }
  
  loadingSkills.value = true
  try {
    await new Promise(resolve => setTimeout(resolve, 500))
    skills.value = [
      { skillName: 'Industry', level: 5, rank: 3, points: 256000 },
      { skillName: 'Advanced Industry', level: 3, rank: 8, points: 226000 },
      { skillName: 'Mass Production', level: 5, rank: 2, points: 170000 },
      { skillName: 'Supply Chain Management', level: 3, rank: 10, points: 480000 }
    ]
  } catch (e) {
    console.error('Failed to load skills', e)
  } finally {
    loadingSkills.value = false
  }
}

onMounted(async () => {
  if (!authStore.isAuthenticated) {
    router.push('/login')
    return
  }
  await loadCharacters()
})
</script>

<style scoped>
.character-page {
  max-width: 1400px;
  margin: 0 auto;
}

.character-tabs :deep(.el-tabs__header) {
  margin-bottom: 24px;
}

.character-tabs :deep(.el-tabs__item) {
  font-size: 14px;
  font-weight: 500;
}

.section-header {
  display: flex;
  gap: 12px;
  margin-bottom: 16px;
}

.character-select {
  width: 200px;
}

.asset-summary {
  margin-bottom: 24px;
}

.summary-card {
  background: #f5f7fa;
  padding: 16px;
  border-radius: 8px;
  text-align: center;
}

.summary-label {
  font-size: 12px;
  color: #909399;
  margin-bottom: 8px;
}

.summary-value {
  font-size: 20px;
  font-weight: 600;
  color: #303133;
}

.summary-value.highlight {
  color: #409eff;
}

.asset-sub-tabs :deep(.el-tabs__content) {
  padding-top: 16px;
}

.location-header {
  display: flex;
  align-items: center;
  gap: 16px;
  width: 100%;
  padding-right: 20px;
}

.location-name {
  flex: 1;
  font-weight: 500;
}

.location-count {
  color: #909399;
  font-size: 13px;
}

.location-value {
  font-weight: 600;
  color: #409eff;
}

:deep(.el-collapse-item__header) {
  padding-left: 16px;
}

:deep(.el-collapse-item__content) {
  padding: 0 16px 16px;
}
</style>

<template>
  <div class="app-container">
    <header class="header">
      <div class="header-left">
        <h1>个人中心</h1>
        <nav class="nav">
          <router-link to="/" class="nav-link">蓝图计算</router-link>
          <router-link to="/market" class="nav-link">市场数据</router-link>
          <router-link to="/profile" class="nav-link active">个人中心</router-link>
        </nav>
      </div>
      <div class="user-info" v-if="authStore.isAuthenticated">
        <el-dropdown trigger="click" @command="handleUserCommand">
          <span class="user-dropdown">
            {{ authStore.username }}
            <el-icon><ArrowDown /></el-icon>
          </span>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="logout">
                <el-icon><SwitchButton /></el-icon>
                退出登录
              </el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </div>
      <div v-else>
        <el-button size="small" @click="router.push('/login')">登录</el-button>
      </div>
    </header>

    <main class="main">
      <el-row :gutter="24">
        <el-col :span="12">
          <el-card>
            <template #header>
              <span>账户信息</span>
            </template>
            <div class="info-item">
              <span class="info-label">用户名</span>
              <span class="info-value">{{ userInfo.username }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">邮箱</span>
              <span class="info-value">{{ userInfo.email }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">注册时间</span>
              <span class="info-value">{{ userInfo.createdAt || '-' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">最后登录</span>
              <span class="info-value">{{ userInfo.lastLoginAt || '-' }}</span>
            </div>
          </el-card>
        </el-col>

        <el-col :span="12">
          <el-card>
            <template #header>
              <span>EVE角色绑定</span>
            </template>
            <p class="desc">
              绑定EVE角色后，可以使用该角色的ESI权限进行游戏内数据查询。
              支持绑定多个EVE角色。
            </p>

            <el-button type="primary" @click="bindCharacter" :loading="binding">
              绑定新角色
            </el-button>

            <div class="character-list" v-if="characters.length > 0">
              <h4>已绑定的角色</h4>
              <el-table :data="characters" size="small">
                <el-table-column prop="characterName" label="角色名称" />
                <el-table-column prop="corporationName" label="公司" width="150">
                  <template #default="{ row }">
                    {{ row.corporationName || '-' }}
                  </template>
                </el-table-column>
                <el-table-column label="绑定时间" width="100">
                  <template #default="{ row }">
                    {{ formatDate(row.createdAt) }}
                  </template>
                </el-table-column>
                <el-table-column label="操作" width="80">
                  <template #default="{ row }">
                    <el-button type="danger" size="small" link @click="unbindCharacter(row.characterId)">
                      解绑
                    </el-button>
                  </template>
                </el-table-column>
              </el-table>
            </div>

            <el-empty v-else description="暂无绑定的角色" :image-size="60" />
          </el-card>
        </el-col>
      </el-row>
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ArrowDown, SwitchButton } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '../stores/auth'
import { authApi, type CharacterInfo } from '../api'

const router = useRouter()
const authStore = useAuthStore()

const binding = ref(false)
const characters = ref<CharacterInfo[]>([])

const userInfo = reactive({
  username: '',
  email: '',
  createdAt: '',
  lastLoginAt: ''
})

function formatDate(dateStr: string): string {
  if (!dateStr) return '-'
  const date = new Date(dateStr)
  return `${date.getMonth() + 1}/${date.getDate()}`
}

async function loadUserInfo() {
  try {
    const user = await authApi.getCurrentUser()
    userInfo.username = user.username
    userInfo.email = user.email
  } catch (e) {
    console.error('Failed to load user info', e)
  }
}

async function loadCharacters() {
  try {
    characters.value = await authApi.getCharacters()
  } catch (e) {
    console.error('Failed to load characters', e)
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
    ElMessage.success('已解绑')
  } catch {
  }
}

async function handleUserCommand(command: string) {
  if (command === 'logout') {
    await authStore.logout()
    ElMessage.success('已退出登录')
    router.push('/login')
  }
}

onMounted(async () => {
  if (!authStore.isAuthenticated) {
    router.push('/login')
    return
  }
  await loadUserInfo()
  await loadCharacters()
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

.header-left {
  display: flex;
  align-items: center;
  gap: 32px;
}

.header h1 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
  letter-spacing: -0.03em;
}

.nav {
  display: flex;
  gap: 24px;
}

.nav-link {
  font-size: 14px;
  color: #666;
  text-decoration: none;
  padding-bottom: 4px;
  border-bottom: 2px solid transparent;
  transition: all 0.2s;
}

.nav-link:hover {
  color: #000;
}

.nav-link.active {
  color: #000;
  border-bottom-color: #000;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 16px;
  font-size: 14px;
}

.main {
  padding: 24px 32px;
  max-width: 1200px;
  margin: 0 auto;
}

.main :deep(.el-card) {
  border: none;
  border-radius: 0;
  box-shadow: none;
  background: #fafafa;
}

.main :deep(.el-card__header) {
  border-bottom: 2px solid #000;
  padding: 16px 0;
  font-weight: 600;
  font-size: 14px;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.info-item {
  display: flex;
  justify-content: space-between;
  padding: 12px 0;
  border-bottom: 1px solid #eee;
}

.info-item:last-child {
  border-bottom: none;
}

.info-label {
  color: #666;
  font-size: 14px;
}

.info-value {
  font-weight: 500;
  font-size: 14px;
}

.desc {
  color: #666;
  font-size: 14px;
  margin-bottom: 20px;
}

.el-button--primary {
  border-radius: 0;
  font-weight: 500;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  background: #000;
  border-color: #000;
}

.el-button--primary:hover {
  background: #333;
  border-color: #333;
}

.character-list {
  margin-top: 24px;
}

.character-list h4 {
  margin: 0 0 12px 0;
  font-size: 14px;
  font-weight: 500;
  color: #333;
}

.user-dropdown {
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 4px;
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
  
  .header-left {
    flex-direction: column;
    gap: 12px;
  }
  
  .nav {
    gap: 16px;
  }
  
  .main :deep(.el-col) {
    margin-bottom: 16px;
  }
}
</style>

<template>
  <div class="app-layout">
    <header class="header">
      <div class="header-left">
        <h1 class="logo">EVE 制造</h1>
        <nav class="nav">
          <router-link to="/" class="nav-link" :class="{ active: route.path === '/' }">
            <el-icon><Grid /></el-icon>
            制造
          </router-link>
          <router-link to="/character" class="nav-link" :class="{ active: route.path === '/character' }">
            <el-icon><User /></el-icon>
            角色
          </router-link>
          <router-link to="/market" class="nav-link" :class="{ active: route.path === '/market' }">
            <el-icon><TrendCharts /></el-icon>
            市场
          </router-link>
        </nav>
      </div>
      <div class="header-right">
        <template v-if="authStore.isAuthenticated">
          <el-dropdown trigger="click" @command="handleUserCommand">
            <span class="user-dropdown">
              <el-icon><UserFilled /></el-icon>
              {{ authStore.username }}
              <el-icon><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="profile">
                  <el-icon><Setting /></el-icon>
                  个人中心
                </el-dropdown-item>
                <el-dropdown-item command="logout" divided>
                  <el-icon><SwitchButton /></el-icon>
                  退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </template>
        <template v-else>
          <el-button size="small" @click="router.push('/login')">登录</el-button>
        </template>
      </div>
    </header>

    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router'
import { Grid, User, TrendCharts, UserFilled, ArrowDown, Setting, SwitchButton } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

async function handleUserCommand(command: string) {
  if (command === 'logout') {
    await authStore.logout()
    ElMessage.success('已退出登录')
    router.push('/login')
  } else if (command === 'profile') {
    router.push('/profile')
  }
}
</script>

<style scoped>
.app-layout {
  min-height: 100vh;
  background: #f5f7fa;
}

.header {
  background: #ffffff;
  border-bottom: 1px solid #e4e7ed;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  height: 60px;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.04);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 32px;
}

.logo {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
  margin: 0;
  letter-spacing: -0.02em;
}

.nav {
  display: flex;
  gap: 4px;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 16px;
  font-size: 14px;
  color: #606266;
  text-decoration: none;
  border-radius: 6px;
  transition: all 0.2s;
}

.nav-link:hover {
  background: #f5f7fa;
  color: #303133;
}

.nav-link.active {
  background: #ecf5ff;
  color: #409eff;
}

.header-right {
  display: flex;
  align-items: center;
}

.user-dropdown {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  font-size: 14px;
  color: #606266;
  cursor: pointer;
  border-radius: 6px;
  transition: all 0.2s;
}

.user-dropdown:hover {
  background: #f5f7fa;
}

.main-content {
  padding: 24px;
}

@media (max-width: 768px) {
  .header {
    padding: 0 12px;
  }
  
  .header-left {
    gap: 16px;
  }
  
  .logo {
    font-size: 16px;
  }
  
  .nav-link {
    padding: 8px 12px;
    font-size: 13px;
  }
  
  .nav-link span:not(.el-icon) {
    display: none;
  }
  
  .main-content {
    padding: 12px;
  }
}
</style>

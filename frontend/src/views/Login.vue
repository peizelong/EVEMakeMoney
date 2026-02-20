<template>
  <div class="login-container">
    <div class="login-card">
      <h1>EVE 制造成本计算器</h1>
      <p class="subtitle">{{ isLogin ? '登录' : '创建账号' }}</p>
      
      <el-form ref="formRef" :model="form" :rules="rules" class="login-form">
        <el-form-item prop="username">
          <el-input 
            v-model="form.username" 
            placeholder="用户名" 
            size="large"
            :prefix-icon="User"
          />
        </el-form-item>
        
        <el-form-item v-if="!isLogin" prop="email">
          <el-input 
            v-model="form.email" 
            placeholder="邮箱" 
            size="large"
            :prefix-icon="Message"
          />
        </el-form-item>
        
        <el-form-item prop="password">
          <el-input 
            v-model="form.password" 
            type="password" 
            placeholder="密码" 
            size="large"
            :prefix-icon="Lock"
            show-password
          />
        </el-form-item>
        
        <el-form-item v-if="!isLogin" prop="confirmPassword">
          <el-input 
            v-model="form.confirmPassword" 
            type="password" 
            placeholder="确认密码" 
            size="large"
            :prefix-icon="Lock"
            show-password
          />
        </el-form-item>
        
        <el-button 
          type="primary" 
          @click="handleSubmit" 
          :loading="loading" 
          size="large"
          class="submit-btn"
        >
          {{ isLogin ? '登录' : '注册' }}
        </el-button>
        
        <div class="switch-mode">
          {{ isLogin ? '没有账号？' : '已有账号？' }}
          <a href="#" @click.prevent="isLogin = !isLogin">{{ isLogin ? '去注册' : '去登录' }}</a>
        </div>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { User, Lock, Message } from '@element-plus/icons-vue'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const formRef = ref<FormInstance>()
const loading = ref(false)
const isLogin = ref(true)

const form = reactive({
  username: '',
  email: '',
  password: '',
  confirmPassword: ''
})

const validateConfirmPassword = (_rule: any, value: string, callback: any) => {
  if (!isLogin.value) {
    if (value !== form.password) {
      callback(new Error('两次输入的密码不一致'))
    } else {
      callback()
    }
  } else {
    callback()
  }
}

const rules = reactive<FormRules>({
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 50, message: '用户名长度在 3 到 50 个字符', trigger: 'blur' }
  ],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入正确的邮箱地址', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于 6 个字符', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请确认密码', trigger: 'blur' },
    { validator: validateConfirmPassword, trigger: 'blur' }
  ]
})

async function handleSubmit() {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid) => {
    if (!valid) return
    
    loading.value = true
    try {
      if (isLogin.value) {
        await authStore.login(form.username, form.password)
        ElMessage.success('登录成功')
      } else {
        await authStore.register(form.username, form.email, form.password)
        ElMessage.success('注册成功')
      }
      
      const redirect = route.query.redirect as string
      router.push(redirect || '/')
    } catch (error: any) {
      const message = error.response?.data?.message || (isLogin.value ? '登录失败' : '注册失败')
      ElMessage.error(message)
    } finally {
      loading.value = false
    }
  })
}
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');

.login-container {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background-color: #ffffff;
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

.login-card {
  width: 360px;
  padding: 48px 40px;
  background: #ffffff;
  border: 1px solid #e5e5e5;
  border-radius: 12px;
}

.login-card h1 {
  margin: 0 0 8px;
  font-size: 24px;
  font-weight: 600;
  color: #171717;
  text-align: center;
  letter-spacing: -0.02em;
}

.subtitle {
  margin: 0 0 32px;
  font-size: 14px;
  color: #737373;
  text-align: center;
}

.login-form :deep(.el-input__wrapper) {
  border-radius: 8px;
  box-shadow: none;
  border: 1px solid #e5e5e5;
}

.login-form :deep(.el-input__wrapper:hover) {
  border-color: #d4d4d4;
}

.login-form :deep(.el-input__wrapper.is-focus) {
  border-color: #171717;
}

.login-form :deep(.el-form-item) {
  margin-bottom: 20px;
}

.submit-btn {
  width: 100%;
  height: 44px;
  border-radius: 8px;
  font-weight: 500;
  background-color: #171717;
  border-color: #171717;
  margin-top: 8px;
}

.submit-btn:hover {
  background-color: #404040;
  border-color: #404040;
}

.switch-mode {
  margin-top: 24px;
  text-align: center;
  font-size: 14px;
  color: #737373;
}

.switch-mode a {
  color: #171717;
  text-decoration: none;
  font-weight: 500;
  margin-left: 4px;
}

.switch-mode a:hover {
  text-decoration: underline;
}
</style>

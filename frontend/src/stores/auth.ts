import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, type User } from '../api'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const accessToken = ref<string | null>(localStorage.getItem('accessToken'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))

  const isAuthenticated = computed(() => !!accessToken.value)

  async function login(username: string, password: string) {
    const response = await authApi.login(username, password)
    setAuth(response.accessToken, response.refreshToken, response.user)
    return response
  }

  async function register(username: string, email: string, password: string) {
    const response = await authApi.register(username, email, password)
    setAuth(response.accessToken, response.refreshToken, response.user)
    return response
  }

  async function logout() {
    try {
      await authApi.logout()
    } catch {
    }
    clearAuth()
  }

  async function fetchUser() {
    if (!accessToken.value) return null
    try {
      const userData = await authApi.getCurrentUser()
      user.value = userData
      return userData
    } catch {
      clearAuth()
      return null
    }
  }

  function setAuth(access: string, refresh: string, userData: User) {
    accessToken.value = access
    refreshToken.value = refresh
    user.value = userData
    localStorage.setItem('accessToken', access)
    localStorage.setItem('refreshToken', refresh)
  }

  function clearAuth() {
    accessToken.value = null
    refreshToken.value = null
    user.value = null
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
  }

  return {
    user,
    accessToken,
    refreshToken,
    isAuthenticated,
    login,
    register,
    logout,
    fetchUser,
    setAuth,
    clearAuth
  }
})

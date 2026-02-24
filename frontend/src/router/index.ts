import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('../views/Login.vue'),
      meta: { guest: true }
    },
    {
      path: '/',
      component: () => import('../views/AppLayout.vue'),
      children: [
        {
          path: '',
          name: 'Manufacturing',
          component: () => import('../views/Manufacturing.vue')
        },
        {
          path: 'assistant',
          name: 'ManufacturingAssistant',
          component: () => import('../views/ManufacturingAssistant.vue')
        },
        {
          path: 'character',
          name: 'Character',
          component: () => import('../views/Character.vue'),
          meta: { requiresAuth: true }
        },
        {
          path: 'market',
          name: 'Market',
          component: () => import('../views/Market.vue')
        },
        {
          path: 'profile',
          name: 'Profile',
          component: () => import('../views/Profile.vue'),
          meta: { requiresAuth: true }
        }
      ]
    }
  ]
})

router.beforeEach(async (to, _from, next) => {
  const authStore = useAuthStore()

  if (authStore.isAuthenticated && !authStore.user) {
    await authStore.fetchUser()
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else if (to.meta.guest && authStore.isAuthenticated) {
    next({ name: 'Manufacturing' })
  } else {
    next()
  }
})

export default router

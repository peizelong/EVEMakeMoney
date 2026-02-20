import axios from 'axios'

const API_BASE_URL = '/api'

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
})

api.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  response => response,
  async error => {
    const originalRequest = error.config
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true
      const refreshToken = localStorage.getItem('refreshToken')
      if (refreshToken) {
        try {
          const response = await axios.post(`${API_BASE_URL}/auth/refresh`, {
            accessToken: localStorage.getItem('accessToken'),
            refreshToken
          })
          const { accessToken, refreshToken: newRefreshToken } = response.data
          localStorage.setItem('accessToken', accessToken)
          localStorage.setItem('refreshToken', newRefreshToken)
          originalRequest.headers.Authorization = `Bearer ${accessToken}`
          return api(originalRequest)
        } catch {
          localStorage.removeItem('accessToken')
          localStorage.removeItem('refreshToken')
          window.location.href = '/login'
        }
      }
    }
    return Promise.reject(error)
  }
)

export interface User {
  id: number
  username: string
  email: string
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: User
}

export interface Blueprint {
  key: number
  blueprintTypeId: number
  maxProductionLimit: number
  name: string
  me: number
  te: number
  manufacturingCost: number
  manufacturingTime: number
  inventionCost: number
  activities?: {
    manufacturing?: {
      products?: Array<{ typeId: number; quantity: number }>
      materials?: Array<{ typeId: number; quantity: number }>
      time: number
    }
    invention?: {
      products?: Array<{ typeId: number; quantity: number; probability?: number }>
      materials?: Array<{ typeId: number; quantity: number }>
      time: number
    }
    reaction?: {
      products?: Array<{ typeId: number; quantity: number }>
      materials?: Array<{ typeId: number; quantity: number }>
      time: number
    }
  }
}

export interface CostBreakdownItem {
  typeId: number
  name: string
  level: number
  quantity: number
  unitCost: number
  totalCost: number
  unitTime: number
  totalTime: number
  isIntermediate: boolean
  children: CostBreakdownItem[]
  inventionCost?: number
}

export interface MarketPopularityResult {
  typeId: number
  typeName: string
  totalOrders: number
  recentOrders: number
  recentOrdersPercentage: number
  totalSupply: number
  recentSupply: number
  recentSupplyPercentage: number
  totalVolume: number
  recentVolume: number
  recentVolumePercentage: number
  hotHours: number[]
  timeClusters: Array<{
    startTime: string
    endTime: string
    orderCount: number
    percentage: number
  }>
  popularityScore: number
}

export interface CalculateCostsRequest {
  ME: number
  TE: number
  StructureBonus: number
  RigBonus: number
  IndustryLevel: number
  AdvancedIndustryLevel: number
  ReactionStructureBonus: number
  ReactionRigBonus: number
  ReactionLevel: number
}

export const authApi = {
  async register(username: string, email: string, password: string): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/register', { username, email, password })
    return response.data
  },

  async login(username: string, password: string): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/login', { username, password })
    return response.data
  },

  async logout(): Promise<void> {
    await api.post('/auth/logout')
  },

  async getCurrentUser(): Promise<User> {
    const response = await api.get<User>('/auth/me')
    return response.data
  }
}

export const blueprintApi = {
  async getBlueprints(search?: string, page: number = 1, pageSize: number = 100): Promise<{ data: Blueprint[], total: number }> {
    const response = await api.get<Blueprint[]>('/blueprints', {
      params: { search, page, pageSize }
    })
    return {
      data: response.data,
      total: parseInt(response.headers['x-total-count'] || '0')
    }
  },

  async getBlueprint(blueprintTypeId: number): Promise<Blueprint> {
    const response = await api.get<Blueprint>(`/blueprints/${blueprintTypeId}`)
    return response.data
  },

  async calculateCosts(request: CalculateCostsRequest): Promise<Blueprint[]> {
    const response = await api.post<Blueprint[]>('/blueprints/calculate', request)
    return response.data
  },

  async getCostBreakdown(blueprintTypeId: number): Promise<CostBreakdownItem> {
    const response = await api.get<CostBreakdownItem>(`/blueprints/${blueprintTypeId}/breakdown`)
    return response.data
  }
}

export const marketApi = {
  async getMarketPopularity(typeId: number, regionId: number = 10000002): Promise<MarketPopularityResult> {
    const response = await api.get<MarketPopularityResult>(`/market/popularity/${typeId}`, {
      params: { regionId }
    })
    return response.data
  },

  async getMarketPopularityReport(typeId: number, regionId: number = 10000002): Promise<string> {
    const response = await api.get<{ report: string }>(`/market/popularity/${typeId}/report`, {
      params: { regionId }
    })
    return response.data.report
  }
}

export const marketDataApi = {
  async getStatus(): Promise<{
    orderCount: number;
    historyCount: number;
    lastOrderFetch: string | null;
    lastHistoryFetch: string | null;
    hasMarketData: boolean;
  }> {
    const response = await api.get<{
      orderCount: number;
      historyCount: number;
      lastOrderFetch: string | null;
      lastHistoryFetch: string | null;
      hasMarketData: boolean;
    }>('/marketdata/status')
    return response.data
  },

  async fetchMarketOrders(regionId: number): Promise<{ message: string }> {
    const response = await api.post<{ message: string }>(`/marketdata/fetch-orders/${regionId}`)
    return response.data
  },

  async fetchMarketHistory(regionId: number): Promise<{ message: string }> {
    const response = await api.post<{ message: string }>(`/marketdata/fetch-history/${regionId}`)
    return response.data
  },

  async fetchMarketHistoryForType(regionId: number, typeId: number): Promise<{ message: string }> {
    const response = await api.post<{ message: string }>(`/marketdata/fetch-history/${regionId}/${typeId}`)
    return response.data
  },

  async getMarketPrices(): Promise<Record<number, number>> {
    const response = await api.get<Record<number, number>>('/marketdata/prices')
    return response.data
  }
}

export default api

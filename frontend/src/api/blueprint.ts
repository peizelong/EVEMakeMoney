import api from './client'
import type { Blueprint, CostBreakdownItem, CalculateCostsRequest, BlueprintSetting } from './types'

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

export const blueprintSettingsApi = {
  async getSettings(): Promise<BlueprintSetting[]> {
    const response = await api.get<BlueprintSetting[]>('/blueprintsettings')
    return response.data
  },

  async getSetting(blueprintTypeId: number): Promise<BlueprintSetting | null> {
    try {
      const response = await api.get<BlueprintSetting>(`/blueprintsettings/${blueprintTypeId}`)
      return response.data
    } catch (e: any) {
      if (e.response?.status === 404) return null
      throw e
    }
  },

  async saveSetting(blueprintTypeId: number, me: number, te: number): Promise<BlueprintSetting> {
    const response = await api.post<BlueprintSetting>('/blueprintsettings', {
      blueprintTypeId,
      me,
      te
    })
    return response.data
  },

  async saveSettingsBatch(settings: Array<{ blueprintTypeId: number; me: number; te: number }>): Promise<BlueprintSetting[]> {
    const response = await api.post<BlueprintSetting[]>('/blueprintsettings/batch', settings)
    return response.data
  },

  async deleteSetting(blueprintTypeId: number): Promise<void> {
    await api.delete(`/blueprintsettings/${blueprintTypeId}`)
  }
}

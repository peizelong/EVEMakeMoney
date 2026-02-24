import api from './client'
import type { MarketPopularityResult } from './types'

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

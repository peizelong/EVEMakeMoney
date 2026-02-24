import api from './client'
import type { AssetWithLocation, AssetTreeNode, AssetGroupedByResolvedLocation, AssetSummaryResponse } from './types'

export const assetApi = {
  async getAssets(characterId: number): Promise<AssetWithLocation[]> {
    const response = await api.get<AssetWithLocation[]>(`/assets/${characterId}`)
    return response.data
  },

  async getAssetsAsTree(characterId: number): Promise<AssetTreeNode[]> {
    const response = await api.get<AssetTreeNode[]>(`/assets/tree/${characterId}`)
    return response.data
  },

  async getAssetsByLocation(characterId: number): Promise<AssetGroupedByResolvedLocation[]> {
    const response = await api.get<AssetGroupedByResolvedLocation[]>(`/assets/by-location/${characterId}`)
    return response.data
  },

  async getAssetSummary(characterId: number): Promise<AssetSummaryResponse> {
    const response = await api.get<AssetSummaryResponse>(`/assets/summary/${characterId}`)
    return response.data
  }
}

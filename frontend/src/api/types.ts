export interface User {
  id: number
  username: string
  email: string
  createdAt?: string
  lastLoginAt?: string
}

export interface CharacterInfo {
  id: number
  characterId: number
  characterName: string
  corporationName?: string
  corporationId?: number
  allianceName?: string
  allianceId?: number
  createdAt: string
  lastUsedAt?: string
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
  customME?: number
  customTE?: number
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
  me: number
  te: number
  structureBonus: number
  rigBonus: number
  industryLevel: number
  advancedIndustryLevel: number
  reactionStructureBonus: number
  reactionRigBonus: number
  reactionLevel: number
}

export interface BlueprintSetting {
  blueprintTypeId: number
  me: number
  te: number
  updatedAt: string
}

export const AssetOwnerType = {
  Character: 0,
  Corporation: 1
} as const

export type AssetOwnerType = typeof AssetOwnerType[keyof typeof AssetOwnerType]

export const AssetLocationType = {
  Container: 0,
  AssetSafety: 1,
  System: 2,
  AbyssalSystem: 3,
  Station: 4,
  Structure: 5,
  Other: 6
} as const

export type AssetLocationType = typeof AssetLocationType[keyof typeof AssetLocationType]

export interface ResolvedLocation {
  locationId: number
  name: string
  typeId: number | null
  systemId: number | null
  locationTypeName: string
}

export interface AssetWithLocation {
  itemId: number
  typeId: number
  quantity: number
  locationId: number
  locationFlag: string
  isSingleton: boolean
  locationType: AssetLocationType
  ownerType: AssetOwnerType
  ownerId: number
  corporationId: number | null
  name: string | null
  typeName: string
  groupName?: string
  categoryName?: string
  volume: number
  iconUrl?: string
  graphicUrl?: string
  estimatedValue: number
  unitPrice: number
  resolvedLocation?: ResolvedLocation
}

export interface AssetTreeNode {
  itemId: number
  typeId: number
  quantity: number
  locationId: number
  locationFlag: string
  isSingleton: boolean
  name: string | null
  typeName: string
  groupName?: string
  categoryName?: string
  volume: number
  iconUrl?: string
  graphicUrl?: string
  estimatedValue: number
  unitPrice: number
  ownerType: AssetOwnerType
  ownerId: number
  resolvedLocation?: ResolvedLocation
  children: AssetTreeNode[]
}

export interface AssetGroupedByResolvedLocation {
  locationId: number
  locationName: string
  locationType: string
  systemId: number | null
  assets: AssetWithLocation[]
  totalValue: number
  itemCount: number
}

export interface AssetSummaryResponse {
  totalItems: number
  totalValue: number
  totalVolume: number
  locationCount: number
}

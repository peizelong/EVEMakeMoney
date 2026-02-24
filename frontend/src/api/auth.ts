import api from './client'
import type { User, CharacterInfo, AuthResponse } from './types'

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
  },

  async getSsoUrl(): Promise<{ url: string }> {
    const response = await api.get<{ url: string }>('/auth/sso/url')
    return response.data
  },

  async getCharacters(): Promise<CharacterInfo[]> {
    const response = await api.get<CharacterInfo[]>('/auth/characters')
    return response.data
  },

  async unbindCharacter(characterId: number): Promise<void> {
    await api.delete(`/auth/characters/${characterId}`)
  }
}

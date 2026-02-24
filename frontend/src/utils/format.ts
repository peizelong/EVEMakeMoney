export function formatNumber(num: number): string {
  if (num >= 1000000000) return (num / 1000000000).toFixed(2) + 'B'
  if (num >= 1000000) return (num / 1000000).toFixed(2) + 'M'
  if (num >= 1000) return (num / 1000).toFixed(2) + 'K'
  return num.toFixed(2)
}

export function formatTime(seconds: number): string {
  if (seconds < 60) return seconds.toFixed(0) + '秒'
  if (seconds < 3600) return (seconds / 60).toFixed(1) + '分钟'
  if (seconds < 86400) return (seconds / 3600).toFixed(1) + '小时'
  return (seconds / 86400).toFixed(1) + '天'
}

export function formatISK(amount: number): string {
  return formatNumber(amount) + ' ISK'
}

export function formatPercent(value: number, decimals: number = 1): string {
  return value.toFixed(decimals) + '%'
}

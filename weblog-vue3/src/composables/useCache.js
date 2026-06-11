const CACHE_PREFIX = 'weblog_'
const DEFAULT_EXPIRE = 30 * 60 * 1000 // 30分钟
let cleanupTimer = null

export function setCache(key, value, expire = DEFAULT_EXPIRE) {
    const data = {
        value,
        expire: Date.now() + expire
    }
    localStorage.setItem(CACHE_PREFIX + key, JSON.stringify(data))
}

export function getCache(key) {
    const item = localStorage.getItem(CACHE_PREFIX + key)
    if (!item) return null
    
    try {
        const data = JSON.parse(item)
        if (Date.now() > data.expire) {
            localStorage.removeItem(CACHE_PREFIX + key)
            return null
        }
        return data.value
    } catch {
        return null
    }
}

export function removeCache(key) {
    localStorage.removeItem(CACHE_PREFIX + key)
}

export function clearCache(key) {
    localStorage.removeItem(CACHE_PREFIX + key)
}

export function clearExpiredCache() {
    const keys = Object.keys(localStorage)
    let cleared = 0
    keys.forEach(key => {
        if (key.startsWith(CACHE_PREFIX)) {
            const item = localStorage.getItem(key)
            if (item) {
                try {
                    const data = JSON.parse(item)
                    if (Date.now() > data.expire) {
                        localStorage.removeItem(key)
                        cleared++
                    }
                } catch {
                    localStorage.removeItem(key)
                    cleared++
                }
            }
        }
    })
    console.log(`[Cache] Cleared ${cleared} expired items`)
}

export function startPeriodicCleanup(intervalMs = 5 * 60 * 1000) {
    stopPeriodicCleanup()
    clearExpiredCache()
    cleanupTimer = setInterval(() => {
        clearExpiredCache()
    }, intervalMs)
    console.log(`[Cache] Started periodic cleanup every ${intervalMs / 1000}s`)
}

export function stopPeriodicCleanup() {
    if (cleanupTimer) {
        clearInterval(cleanupTimer)
        cleanupTimer = null
    }
}

export function clearAllCache() {
    const keys = Object.keys(localStorage)
    keys.forEach(key => {
        if (key.startsWith(CACHE_PREFIX)) {
            localStorage.removeItem(key)
        }
    })
}

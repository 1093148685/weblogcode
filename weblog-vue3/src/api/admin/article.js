import axios from "@/axios";
import { getCache, setCache, clearCache } from '@/composables/useCache'

const CACHE_TTL = 3 * 60 * 1000
const ARTICLE_CACHE_VERSION_KEY = 'admin_article_cache_version'
const ARTICLE_CACHE_VERSION = '2026-04-27-status-sync-v1'

const ARTICLE_CACHE_PREFIXES = [
    'admin_article_',
    'articles_page_',
    'article_detail_',
    'ai_summary_',
    'archives_page_'
]

const ARTICLE_CACHE_KEYS = [
    'admin_dashboard_base',
    'admin_dashboard_publish',
    'admin_dashboard_pv',
    'admin_dashboard_category',
    'admin_dashboard_tag',
    'page_categories_all',
    'page_tags_all',
    'category_list',
    'tag_list',
    'sidebar_categories',
    'sidebar_tags',
    'sidebar_statistics'
]

export function getArticlePageList(data, noCache = false) {
    const cacheKey = `admin_article_list_${JSON.stringify(data)}`
    if (!noCache) {
        const cached = getCache(cacheKey)
        if (cached) return Promise.resolve(cached)
    } else {
        clearCache(cacheKey)
    }

    return axios.post("/admin/article/list", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

export function deleteArticle(id) {
    return axios.post("/admin/article/delete", { id }).then(res => {
        if (res.success) clearAdminArticleCache()
        return res
    })
}

export function publishArticle(data) {
    return axios.post("/admin/article/publish", data).then(res => {
        if (res.success) clearAdminArticleCache()
        return res
    })
}

export function getArticleDetail(id) {
    return axios.post("/admin/article/detail", { id })
}

export function updateArticle(data) {
    return axios.post("/admin/article/update", data).then(res => {
        if (res.success) clearAdminArticleCache()
        return res
    })
}

export function updateArticleIsTop(data) {
    return axios.post("/admin/article/isTop/update", data).then(res => {
        if (res.success) clearAdminArticleCache()
        return res
    })
}

export function updateArticleStatus(data) {
    return axios.post("/admin/article/status/update", data).then(res => {
        if (res.success) clearAdminArticleCache()
        return res
    })
}

export function clearAdminArticleCache() {
    clearArticleRelatedCache()
}

function clearArticleRelatedCache() {
    try {
        const keys = Object.keys(localStorage).filter(k => (
            ARTICLE_CACHE_PREFIXES.some(prefix => k.startsWith(`weblog_${prefix}`)) ||
            ARTICLE_CACHE_KEYS.some(key => k === `weblog_${key}`)
        ))
        keys.forEach(k => localStorage.removeItem(k))
    } catch {}
}

function ensureArticleCacheVersion() {
    try {
        if (localStorage.getItem(ARTICLE_CACHE_VERSION_KEY) !== ARTICLE_CACHE_VERSION) {
            clearArticleRelatedCache()
            localStorage.setItem(ARTICLE_CACHE_VERSION_KEY, ARTICLE_CACHE_VERSION)
        }
    } catch {}
}

ensureArticleCacheVersion()

import axios from "@/axios";
import { getCache, setCache } from '@/composables/useCache'

const CACHE_TTL = 5 * 60 * 1000 // 5分钟

// 获取仪表盘基础信息（文章数、分类数、标签数、总浏览量）
export function getBaseStatisticsInfo(data) {
    const cacheKey = 'admin_dashboard_base'
    const cached = getCache(cacheKey)
    if (cached) return Promise.resolve(cached)

    return axios.post("/admin/dashboard/statistics", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 获取仪表盘文章发布热点统计信息
export function getPublishArticleStatisticsInfo(data) {
    const cacheKey = 'admin_dashboard_publish'
    const cached = getCache(cacheKey)
    if (cached) return Promise.resolve(cached)

    return axios.post("/admin/dashboard/publishArticle/statistics", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 获取仪表盘最近一周 PV 访问量信息
export function getArticlePVStatisticsInfo(data) {
    const cacheKey = 'admin_dashboard_pv'
    const cached = getCache(cacheKey)
    if (cached) return Promise.resolve(cached)

    return axios.post("/admin/dashboard/pv/statistics", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 获取仪表盘分类统计信息（饼图）
export function getCategoryStatisticsInfo(data) {
    const cacheKey = 'admin_dashboard_category'
    const cached = getCache(cacheKey)
    if (cached) return Promise.resolve(cached)

    return axios.post("/admin/dashboard/category/statistics", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 获取仪表盘标签统计信息（条形图）
export function getTagStatisticsInfo(data) {
    const cacheKey = 'admin_dashboard_tag'
    const cached = getCache(cacheKey)
    if (cached) return Promise.resolve(cached)

    return axios.post("/admin/dashboard/tag/statistics", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 清除仪表盘缓存
export function clearDashboardCache() {
    const keys = ['admin_dashboard_base', 'admin_dashboard_publish', 'admin_dashboard_pv', 'admin_dashboard_category', 'admin_dashboard_tag']
    keys.forEach(key => {
        try { localStorage.removeItem(key) } catch {}
    })
}
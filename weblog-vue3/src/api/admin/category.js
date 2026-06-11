import axios from "@/axios";
import { getCache, setCache, clearCache } from '@/composables/useCache'

const CACHE_TTL = 5 * 60 * 1000

// 获取分类分页数据
export function getCategoryPageList(data, noCache = false) {
    const cacheKey = `admin_category_list_${JSON.stringify(data)}`
    if (!noCache) {
        const cached = getCache(cacheKey)
        if (cached) return Promise.resolve(cached)
    } else {
        clearCache(cacheKey)
    }

    return axios.post("/admin/category/list", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 添加分类
export function addCategory(data) {
    return axios.post("/admin/category/add", data).then(res => {
        if (res.success) clearAdminCategoryCache()
        return res
    })
}

// 删除分类
export function deleteCategory(id) {
    return axios.post("/admin/category/delete", {id}).then(res => {
        if (res.success) clearAdminCategoryCache()
        return res
    })
}

// 获取分类 select 数据
export function getCategorySelectList() {
    const cacheKey = 'admin_category_select'
    const cached = getCache(cacheKey)
    if (cached) return Promise.resolve(cached)

    return axios.post("/admin/category/select/list").then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 清除分类相关缓存
export function clearAdminCategoryCache() {
    try {
        const keys = Object.keys(localStorage).filter(k => k.startsWith('admin_category_'))
        keys.forEach(k => localStorage.removeItem(k))
        // 同时清除仪表盘分类统计缓存
        localStorage.removeItem('admin_dashboard_category')
        localStorage.removeItem('admin_dashboard_base')
        // 清除前台分类缓存
        localStorage.removeItem('page_categories_all')
        localStorage.removeItem('sidebar_categories')
    } catch {}
}
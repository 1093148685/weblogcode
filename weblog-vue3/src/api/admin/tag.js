import axios from "@/axios";
import { getCache, setCache, clearCache } from '@/composables/useCache'

const CACHE_TTL = 5 * 60 * 1000

// 获取标签分页数据
export function getTagPageList(data, noCache = false) {
    const cacheKey = `admin_tag_list_${JSON.stringify(data)}`
    if (!noCache) {
        const cached = getCache(cacheKey)
        if (cached) return Promise.resolve(cached)
    } else {
        clearCache(cacheKey)
    }

    return axios.post("/admin/tag/list", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 添加标签
export function addTag(data) {
    return axios.post("/admin/tag/add", data).then(res => {
        if (res.success) clearAdminTagCache()
        return res
    })
}

// 删除标签
export function deleteTag(id) {
    return axios.post("/admin/tag/delete", {id}).then(res => {
        if (res.success) clearAdminTagCache()
        return res
    })
}

// 根据标签名模糊查询
export function searchTags(key) {
    return axios.post("/admin/tag/search", {key})
}

// 获取标签 select 列表数据
export function getTagSelectList() {
    const cacheKey = 'admin_tag_select'
    const cached = getCache(cacheKey)
    if (cached) return Promise.resolve(cached)

    return axios.post("/admin/tag/select/list").then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 清除标签相关缓存
export function clearAdminTagCache() {
    try {
        const keys = Object.keys(localStorage).filter(k => k.startsWith('admin_tag_'))
        keys.forEach(k => localStorage.removeItem(k))
        // 同时清除仪表盘标签统计缓存
        localStorage.removeItem('admin_dashboard_tag')
        localStorage.removeItem('admin_dashboard_base')
        // 清除前台标签缓存
        localStorage.removeItem('page_tags_all')
        localStorage.removeItem('sidebar_tags')
    } catch {}
}
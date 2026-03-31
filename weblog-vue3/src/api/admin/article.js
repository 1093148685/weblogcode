import axios from "@/axios";
import { getCache, setCache } from '@/composables/useCache'

const CACHE_TTL = 3 * 60 * 1000 // 3分钟

// 获取文章分页数据
export function getArticlePageList(data) {
    const cacheKey = `admin_article_list_${JSON.stringify(data)}`
    const cached = getCache(cacheKey)
    if (cached) return Promise.resolve(cached)

    return axios.post("/admin/article/list", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 删除文章
export function deleteArticle(id) {
    return axios.post("/admin/article/delete", {id}).then(res => {
        if (res.success) clearAdminArticleCache()
        return res
    })
}

// 发布文章
export function publishArticle(data) {
    return axios.post("/admin/article/publish", data).then(res => {
        if (res.success) clearAdminArticleCache()
        return res
    })
}

// 获取文章详情
export function getArticleDetail(id) {
    return axios.post("/admin/article/detail", {id})
}

// 更新文章
export function updateArticle(data) {
    return axios.post("/admin/article/update", data).then(res => {
        if (res.success) clearAdminArticleCache()
        return res
    })
}

// 更新文章置顶状态
export function updateArticleIsTop(data) {
    return axios.post("/admin/article/isTop/update", data).then(res => {
        if (res.success) clearAdminArticleCache()
        return res
    })
}

// 清除文章相关缓存
export function clearAdminArticleCache() {
    try {
        const keys = Object.keys(localStorage).filter(k => k.startsWith('admin_article_'))
        keys.forEach(k => localStorage.removeItem(k))
        // 清除仪表盘相关缓存
        localStorage.removeItem('admin_dashboard_base')
        localStorage.removeItem('admin_dashboard_publish')
    } catch {}
}

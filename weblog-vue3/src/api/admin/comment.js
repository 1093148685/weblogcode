import axios from "@/axios";
import { getCache, setCache } from '@/composables/useCache'

const CACHE_TTL = 3 * 60 * 1000 // 3分钟

// 获取评论分页数据
export function getCommentPageList(data) {
    const cacheKey = `admin_comment_list_${JSON.stringify(data)}`
    const cached = getCache(cacheKey)
    if (cached) return Promise.resolve(cached)

    return axios.post("/admin/comment/list", data).then(res => {
        if (res.success) setCache(cacheKey, res, CACHE_TTL)
        return res
    })
}

// 删除评论
export function deleteComment(id) {
    return axios.post("/admin/comment/delete", {id}).then(res => {
        if (res.success) clearAdminCommentCache()
        return res
    })
}

// 批量删除评论
export function batchDeleteComment(ids) {
    return axios.post("/admin/comment/batchDelete", {ids}).then(res => {
        if (res.success) clearAdminCommentCache()
        return res
    })
}

// 审核评论
export function examineComment(data) {
    return axios.post("/admin/comment/examine", data).then(res => {
        if (res.success) clearAdminCommentCache()
        return res
    })
}

// 获取私密评论列表
export function getSecretCommentList(data) {
    return axios.post("/admin/comment/secret/list", data)
}

// 重置私密评论
export function resetSecretComment(data) {
    return axios.post("/admin/comment/secret/reset", data).then(res => {
        if (res.success) clearAdminCommentCache()
        return res
    })
}

// 清除评论相关缓存
export function clearAdminCommentCache() {
    try {
        const keys = Object.keys(localStorage).filter(k => k.startsWith('admin_comment_'))
        keys.forEach(k => localStorage.removeItem(k))
    } catch {}
}
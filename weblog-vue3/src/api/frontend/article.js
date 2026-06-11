import axios from "@/axios";
import { getCache, setCache, clearCache } from '@/composables/useCache'

// 获取文章列表
export function getArticlePageList(data) {
    return axios.post("/article/list", data)
}

// 获取文章详情（带缓存）
export function getArticleDetail(articleId) {
    if (!articleId) {
        return Promise.reject(new Error('articleId is required'))
    }
    
    const cacheKey = 'article_detail_' + articleId
    const cached = getCache(cacheKey)
    if (cached) {
        return Promise.resolve({ success: true, data: cached })
    }
    
    return axios.post("/article/detail", { articleId }).then(res => {
        if (res.success && res.data) {
            setCache(cacheKey, res.data, 30 * 60 * 1000) // 缓存30分钟
        }
        return res
    })
}

// 清除文章详情缓存
export function clearArticleDetailCache(articleId) {
    if (!articleId) return
    const cacheKey = 'article_detail_' + articleId
    clearCache(cacheKey)
}




import axios from "@/axios";
import { getCache, setCache } from '@/composables/useCache'

const CATEGORY_LIST_KEY = 'category_list'

// 获取分类列表（带缓存）
export function getCategoryList(data) {
    const cached = getCache(CATEGORY_LIST_KEY)
    if (cached) {
        return Promise.resolve({ success: true, data: cached })
    }
    
    return axios.post("/category/list", data).then(res => {
        if (res.success && res.data) {
            setCache(CATEGORY_LIST_KEY, res.data, 60 * 60 * 1000) // 缓存1小时
        }
        return res
    })
}

// 获取分类-文章列表
export function getCategoryArticlePageList(data) {
    return axios.post("/category/article/list", data)
}



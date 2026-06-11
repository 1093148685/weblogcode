import axios from "@/axios";
import { getCache, setCache } from '@/composables/useCache'

const TAG_LIST_KEY = 'tag_list'

// 获取标签列表（带缓存）
export function getTagList(data) {
    const cached = getCache(TAG_LIST_KEY)
    if (cached) {
        return Promise.resolve({ success: true, data: cached })
    }
    
    return axios.post("/tag/list", data).then(res => {
        if (res.success && res.data) {
            setCache(TAG_LIST_KEY, res.data, 60 * 60 * 1000) // 缓存1小时
        }
        return res
    })
}

// 获取标签下文章列表
export function getTagArticlePageList(data) {
    return axios.post("/tag/article/list", data)
}



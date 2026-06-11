import axios from "@/axios";
import { getCache, setCache } from '@/composables/useCache'

const BLOG_SETTINGS_KEY = 'blog_settings'

// 获取博客设置详情（带缓存）
export function getBlogSettingsDetail() {
    const cached = getCache(BLOG_SETTINGS_KEY)
    if (cached) {
        return Promise.resolve({ success: true, data: cached })
    }
    
    return axios.post("/blog/settings/detail").then(res => {
        if (res.success && res.data) {
            setCache(BLOG_SETTINGS_KEY, res.data, 60 * 60 * 1000) // 缓存1小时
        }
        return res
    })
}



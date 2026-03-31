import axios from "@/axios";
import { getCache, setCache } from '@/composables/useCache'

const ANNOUNCEMENT_KEY = 'announcement'

// 获取公告（带缓存）
export function getAnnouncement() {
    const cached = getCache(ANNOUNCEMENT_KEY)
    if (cached) {
        return Promise.resolve({ success: true, data: cached })
    }
    
    return axios.get("/announcement").then(res => {
        if (res.success && res.data) {
            setCache(ANNOUNCEMENT_KEY, res.data, 60 * 60 * 1000) // 缓存1小时
        }
        return res
    })
}

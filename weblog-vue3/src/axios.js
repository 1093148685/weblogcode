import axios from "axios";
import { getToken } from "@/composables/cookie"
import { showMessage} from '@/composables/util'
import { useUserStore } from '@/stores/user'

// 创建 Axios 实例
const instance = axios.create({
    baseURL: "/api",
    timeout: 30000
})

// --- 请求去重：同一 GET 请求在 pending 期间共享结果 ---
const inFlight = new Map()

function getRequestKey(config) {
    const { method, url, params, data } = config
    return [method, url, JSON.stringify(params), JSON.stringify(data)].join('&')
}

// 添加请求拦截器
instance.interceptors.request.use(function (config) {
    const token = getToken()
    console.log('统一添加请求头中的 Token:' + token)

    if (token) {
        config.headers['Authorization'] = 'Bearer ' + token
    }

    // GET 请求去重
    if (config.method === 'get') {
        const key = getRequestKey(config)
        config._dedupKey = key
    }

    return config
}, function (error) {
    return Promise.reject(error)
})

// 添加响应拦截器
instance.interceptors.response.use(function (response) {
    // 去重：如果是 GET 请求，缓存 promise 并设置自动清理
    const key = response.config._dedupKey
    if (key) {
        // 请求已完成，清理 pending 记录
        inFlight.delete(key)
    }
    return response.data
}, function (error) {
    const key = error.config?._dedupKey
    if (key) {
        inFlight.delete(key)
    }

    if (!error.response) {
        showMessage('网络错误，请检查后端服务是否运行', 'error')
        return Promise.reject(error)
    }

    let status = error.response.status

    if (status == 401) {
        let userStore = useUserStore()
        userStore.logout()
        location.reload()
    }

    let errorMsg = error.response.data?.message || '请求失败'
    showMessage(errorMsg, 'error')

    return Promise.reject(error)
})

// 暴露出去
export default instance;

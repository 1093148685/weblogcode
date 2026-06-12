import axios from "axios";
import { getToken } from "@/composables/cookie"
import { showMessage} from '@/composables/util'
import { useUserStore } from '@/stores/user'

// 创建 Axios 实例
const instance = axios.create({
    baseURL: "/api",
    timeout: 30000
})

// --- 请求去重：同一 GET 请求在 pending 期间共享同一个 promise ---
const inFlight = new Map()

function getRequestKey(config) {
    const { method, url, params, data } = config
    return [method, url, JSON.stringify(params), JSON.stringify(data)].join('&')
}

// 接管适配器：命中则返回已在进行中的 promise，避免重复请求
const originalAdapter = instance.defaults.adapter
instance.defaults.adapter = (config) => {
    // 只对 GET 请求去重，POST/PUT/DELETE 每次都发
    if (config.method !== 'get') {
        return originalAdapter(config)
    }

    const key = getRequestKey(config)
    if (inFlight.has(key)) {
        return inFlight.get(key)
    }

    const promise = originalAdapter(config).finally(() => {
        inFlight.delete(key)
    })

    inFlight.set(key, promise)
    return promise
}
// --- 请求去重结束 ---

// 添加请求拦截器
instance.interceptors.request.use(function (config) {
    // 在发送请求之前做些什么
    const token = getToken()
    console.log('统一添加请求头中的 Token:' + token)

    // 当 token 不为空时
    if (token) {
        // 添加请求头, key 为 Authorization，value 值的前缀为 'Bearer '
        config.headers['Authorization'] = 'Bearer ' + token
    }

    return config;
}, function (error) {
    // 对请求错误做些什么
    return Promise.reject(error)
});

// 添加响应拦截器
instance.interceptors.response.use(function (response) {
    // 2xx 范围内的状态码都会触发该函数。
    // 对响应数据做点什么
    return response.data
}, function (error) {
    // 超出 2xx 范围的状态码都会触发该函数。
    if (!error.response) {
        showMessage('网络错误，请检查后端服务是否运行', 'error')
        return Promise.reject(error)
    }

    let status = error.response.status

    // 状态码 401
    if (status == 401) {
        // 退出登录
        let userStore = useUserStore()
        userStore.logout()
        // 刷新页面
        location.reload()
    }

    // 若后台有错误提示就用提示文字，默认提示为 '请求失败'
    let errorMsg = error.response.data?.message || '请求失败'
    // 弹错误提示
    showMessage(errorMsg, 'error')

    return Promise.reject(error)
})

// 暴露出去
export default instance;

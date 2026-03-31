import { getToken } from '@/composables/cookie'
import axios from '@/axios'

// 获取可用的 AI 模型列表
export function getAvailableModels() {
    return axios.get('/admin/agent/models')
}

// 发送 Agent 消息（SSE 流式，返回 fetch Response）
export function sendAgentMessage(data) {
    return fetch('/api/admin/agent/chat', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${getToken() || ''}`
        },
        body: JSON.stringify(data)
    })
}

// ──────── 配置文件 API ────────

// 获取所有配置文件列表
export function getAgentConfigs() {
    return axios.get('/admin/agent/configs')
}

// 获取单个配置文件
export function getAgentConfig(fileName) {
    return axios.get(`/admin/agent/configs/${fileName}`)
}

// 保存配置文件
export function saveAgentConfig(fileName, content) {
    return axios.put(`/admin/agent/configs/${fileName}`, { content })
}

// 重置配置文件
export function resetAgentConfig(fileName) {
    return axios.post(`/admin/agent/configs/${fileName}/reset`)
}

// ──────── 操作日志 API ────────

// 获取操作日志列表
export function getAgentLogs(params) {
    return axios.get('/admin/agent/logs', { params })
}

// 清空操作日志
export function clearAgentLogs() {
    return axios.delete('/admin/agent/logs')
}

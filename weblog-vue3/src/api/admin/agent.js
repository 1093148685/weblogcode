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

// ──────── 会话历史 API ────────

export function getAgentSessions() {
    return axios.get('/admin/agent/sessions')
}

export function getAgentSession(sessionId) {
    return axios.get(`/admin/agent/session/${sessionId}`)
}

export function deleteAgentSession(sessionId) {
    return axios.delete(`/admin/agent/session/${sessionId}`)
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

// ──────── 写作助手 API ────────

export function generateArticle(data) {
    return axios.post('/admin/ai/assistant/generate-article', data)
}

export function seoOptimize(data) {
    return axios.post('/admin/ai/assistant/seo-optimize', data)
}

export function moderateContent(data) {
    return axios.post('/admin/ai/assistant/moderate', data)
}

// ──────── Provider 管理 API ────────

export function getProviderHealth() {
    return axios.get('/admin/ai/provider/health')
}

export function getKeyPoolStatus() {
    return axios.get('/admin/ai/provider/key-pool-status')
}

export function resetProviderKeys(name) {
    return axios.post(`/admin/ai/provider/${name}/reset-keys`)
}

export function getTokenStats(days = 7) {
    return axios.get('/admin/ai/assistant/token-stats', { params: { days } })
}

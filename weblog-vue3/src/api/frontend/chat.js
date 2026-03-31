import axios from "@/axios";

export function getAvailableModels() {
    return axios.get('/ai/models')
}

export function sendChatMessage(data) {
    return axios.post('/ai/chat', data, {
        responseType: 'stream'
    })
}

// 保存会话到数据库
export function saveSession(data) {
    return axios.post('/ai/session/save', data)
}

// 获取用户历史会话列表
export function getUserSessions(clientId) {
    return axios.get('/ai/sessions', { params: { clientId } })
}

// 删除用户的某条会话
export function deleteUserSession(sessionId, clientId) {
    return axios.delete(`/ai/session/${sessionId}`, { params: { clientId } })
}

// 获取使用次数信息
export function getUsageInfo(clientId) {
    return axios.get('/ai/usage', { params: { clientId } })
}

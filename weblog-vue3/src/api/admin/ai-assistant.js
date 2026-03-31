import axios from "@/axios";

export function getAiUsageStats() {
    return axios.get('/admin/ai/assistant/usage')
}

export function getAiSessions() {
    return axios.get('/admin/ai/assistant/sessions')
}

export function deleteAiSession(sessionId) {
    return axios.delete(`/admin/ai/assistant/session/${sessionId}`)
}

export function getAiPluginSettings() {
    return axios.get('/admin/ai/assistant/settings')
}

export function updateAiPluginSettings(settings) {
    return axios.put('/admin/ai/assistant/settings', settings)
}
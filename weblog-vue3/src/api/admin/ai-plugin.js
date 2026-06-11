import axios from "@/axios";

export function getAiPlugins() {
    return axios.get('/admin/ai/plugin/list')
}

export function getAiPlugin(pluginId) {
    return axios.get(`/admin/ai/plugin/${pluginId}`)
}

export function updateAiPlugin(pluginId, data) {
    return axios.put(`/admin/ai/plugin/${pluginId}`, data)
}

export function toggleAiPlugin(pluginId, isEnabled) {
    return axios.post(`/admin/ai/plugin/${pluginId}/toggle`, { isEnabled })
}

export function testAiPlugin(pluginId) {
    return axios.post(`/admin/ai/plugin/${pluginId}/test`)
}

export function getAiPluginMetadata(pluginId) {
    return axios.get(`/admin/ai/plugin/${pluginId}/metadata`)
}
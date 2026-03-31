import axios from "@/axios";

export function getAiProviders() {
    return axios.get('/admin/ai/provider/list')
}

export function getAiProvider(id) {
    return axios.get(`/admin/ai/provider/${id}`)
}

export function getEnabledAiProviders() {
    return axios.get('/admin/ai/provider/enabled')
}

export function createAiProvider(data) {
    return axios.post('/admin/ai/provider', data)
}

export function updateAiProvider(id, data) {
    return axios.put(`/admin/ai/provider/${id}`, data)
}

export function deleteAiProvider(id) {
    return axios.delete(`/admin/ai/provider/${id}`)
}

export function testAiProvider(id) {
    return axios.post(`/admin/ai/provider/${id}/test`)
}

export function fetchModels(apiUrl, apiKey) {
    return axios.post('/admin/ai/provider/fetch-models', { apiUrl, apiKey })
}

export function migrateAiProviders() {
    return axios.post('/admin/ai/provider/migrate')
}

export function initAiProvider() {
    return axios.post('/admin/ai/provider/init')
}
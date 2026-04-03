import axios from "@/axios";

// 获取所有模型
export function getAiModelList() {
    return axios.get("/admin/ai-model")
}

// 获取已启用的模型列表
export function getEnabledAiModels() {
    return axios.get("/admin/ai-model/enabled-list")
}

// 获取模型详情
export function getAiModel(id) {
    return axios.get(`/admin/ai-model/${id}`)
}

// 创建模型
export function createAiModel(data) {
    return axios.post("/admin/ai-model", data)
}

// 更新模型
export function updateAiModel(data) {
    return axios.put("/admin/ai-model", data)
}

// 删除模型
export function deleteAiModel(id) {
    return axios.delete(`/admin/ai-model/${id}`)
}

// 测试模型
export function testAiModel(id) {
    return axios.post(`/admin/ai-model/test/${id}`)
}

// 获取模型使用统计
export function getAiModelStats() {
    return axios.get('/admin/ai-model/stats')
}

// 获取用量趋势（最近N天）
export function getAiModelTrend(days = 30) {
    return axios.get('/admin/ai-model/trend', { params: { days } })
}

// 批量启用/禁用
export function batchUpdateModels(ids, isEnabled) {
    return axios.put('/admin/ai-model/batch', { ids, isEnabled })
}

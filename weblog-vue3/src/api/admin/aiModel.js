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

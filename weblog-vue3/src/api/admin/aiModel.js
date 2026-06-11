import axios from "@/axios";

export function getAiModelList() {
    return axios.get("/admin/ai-model");
}

export function getEnabledAiModels() {
    return axios.get("/admin/ai-model/enabled-list");
}

export function getAiModel(id) {
    return axios.get(`/admin/ai-model/${id}`);
}

export function createAiModel(data) {
    return axios.post("/admin/ai-model", data);
}

export function updateAiModel(data) {
    return axios.put("/admin/ai-model", data);
}

export function deleteAiModel(id) {
    return axios.delete(`/admin/ai-model/${id}`);
}

export function testAiModel(id) {
    return axios.post(`/admin/ai-model/test/${id}`);
}

export function getAiModelStats() {
    return axios.get("/admin/ai-model/stats");
}

export function getAiModelTrend(days = 30) {
    return axios.get("/admin/ai-model/trend", { params: { days } });
}

export function batchUpdateModels(ids, isEnabled) {
    return axios.put("/admin/ai-model/batch", { ids, isEnabled });
}

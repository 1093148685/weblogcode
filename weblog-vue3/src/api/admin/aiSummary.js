import axios from "@/axios";

// 获取AI摘要
export function getAiSummary(articleId) {
    return axios.get(`/ai-summary/${articleId}`)
}

// 获取AI摘要（管理员）
export function getAiSummaryAdmin(articleId) {
    return axios.get(`/admin/ai-summary/article/${articleId}`)
}

// 创建AI摘要
export function createAiSummary(data) {
    return axios.post("/admin/ai-summary", data)
}

// 更新AI摘要
export function updateAiSummary(data) {
    return axios.put("/admin/ai-summary", data)
}

// 生成AI摘要（流式）
export function generateAiSummaryApi(articleId) {
    return axios.post(`/admin/ai-summary/generate/${articleId}`, {}, {
        responseType: 'stream'
    })
}

// AI生成文章内容（流式）
export function generateAiContent(data) {
    return axios.post("/admin/ai-summary/generate/content", data, {
        responseType: 'stream'
    })
}
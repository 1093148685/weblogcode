import axios from "@/axios";

// 获取AI摘要
export function getAiSummary(articleId) {
    return axios.get(`/ai-summary/${articleId}`)
}

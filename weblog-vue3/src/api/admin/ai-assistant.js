import axios from "@/axios";

/** 写作助手 API 超时 120 秒（AI 生成需要更长时间） */
const AI_TIMEOUT = 120_000

/** 一键生成文章 */
export function generateArticle(data) {
    return axios.post('/admin/ai/assistant/generate-article', data, { timeout: AI_TIMEOUT })
}

/** SEO 优化建议 */
export function seoOptimize(data) {
    return axios.post('/admin/ai/assistant/seo-optimize', data, { timeout: AI_TIMEOUT })
}

/** 内容安全检测 */
export function moderateContent(data) {
    return axios.post('/admin/ai/assistant/moderate', data, { timeout: AI_TIMEOUT })
}

/** Token 用量统计 */
export function getTokenStats(days = 7) {
    return axios.get('/admin/ai/assistant/token-stats', { params: { days } })
}

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
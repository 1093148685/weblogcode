import axios from "@/axios";

// ── 知识库 ──────────────────────────────────────

/** 获取全部知识库列表 */
export function getKnowledgeBaseList() {
    return axios.get('/admin/rag/knowledge-base/list')
}

/** 创建知识库 */
export function createKnowledgeBase(data) {
    return axios.post('/admin/rag/knowledge-base', data)
}

/** 更新知识库 */
export function updateKnowledgeBase(id, data) {
    return axios.put(`/admin/rag/knowledge-base/${id}`, data)
}

/** 删除知识库 */
export function deleteKnowledgeBase(id) {
    return axios.delete(`/admin/rag/knowledge-base/${id}`)
}

// ── 文档管理 ─────────────────────────────────────

/** 获取知识库下的文档列表 */
export function getDocumentList(kbId, params) {
    return axios.get(`/admin/rag/knowledge-base/${kbId}/documents`, { params })
}

/** 从博客文章批量导入 */
export function importFromArticles(kbId, data) {
    return axios.post(`/admin/rag/knowledge-base/${kbId}/import/articles`, data)
}

/** 从 Wiki 导入 */
export function importFromWiki(kbId, data) {
    return axios.post(`/admin/rag/knowledge-base/${kbId}/import/wiki`, data)
}

/** 手动上传文档 */
export function uploadDocument(kbId, formData) {
    return axios.post(`/admin/rag/knowledge-base/${kbId}/upload`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
    })
}

/** 上传文件（PDF 专用接口） */
export function uploadFile(kbId, formData) {
    return axios.post(`/admin/rag/knowledge-base/${kbId}/upload-file`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
    })
}

/** 删除文档 */
export function deleteDocument(kbId, docId) {
    return axios.delete(`/admin/rag/knowledge-base/${kbId}/documents/${docId}`)
}

/** 重新触发向量化 */
export function reindexDocument(kbId, docId) {
    return axios.post(`/admin/rag/knowledge-base/${kbId}/documents/${docId}/reindex`)
}

/** 重建整个知识库索引 */
export function reindexAll(kbId) {
    return axios.post(`/admin/rag/knowledge-base/${kbId}/reindex`)
}

// ── 检索测试 ─────────────────────────────────────

/** 相似度检索测试 */
export function testRetrieval(kbId, data) {
    return axios.post(`/admin/rag/knowledge-base/${kbId}/retrieval-test`, data)
}

// ── 统计 ─────────────────────────────────────────

/** 获取知识库统计数据 */
export function getKnowledgeBaseStats(kbId) {
    return axios.get(`/admin/rag/knowledge-base/${kbId}/stats`)
}

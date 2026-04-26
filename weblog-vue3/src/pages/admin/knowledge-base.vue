<template>
    <div class="page-shell rag-page">
        <div class="page-hero">
            <div class="page-hero__main">
                <div class="page-hero__icon">
                    <el-icon :size="20"><Collection /></el-icon>
                </div>
                <div>
                    <h1 class="page-hero__title">RAG 知识库管理</h1>
                    <p class="page-hero__desc">管理知识库、文档索引与检索测试，让 AI 回答有可靠的业务上下文。</p>
                </div>
            </div>
            <div class="page-hero__actions">
                <el-button :icon="Refresh" @click="refreshCurrent" :loading="loading">刷新</el-button>
                <el-button type="primary" :icon="Plus" @click="openCreateDialog">新建知识库</el-button>
            </div>
        </div>

        <div class="rag-workspace">
            <aside class="rag-sidebar glass-panel">
                <div class="rag-sidebar__head">
                    <div>
                        <div class="section-title">知识库</div>
                        <div class="section-desc">{{ kbList.length }} 个空间</div>
                    </div>
                    <el-button circle size="small" :icon="Plus" @click="openCreateDialog" />
                </div>

                <el-empty v-if="!loading && kbList.length === 0" description="暂无知识库" :image-size="72" />

                <div v-else class="rag-kb-list">
                    <button
                        v-for="kb in kbList"
                        :key="kb.id"
                        class="rag-kb-card"
                        :class="{ 'is-active': selectedKb?.id === kb.id }"
                        @click="selectKb(kb)"
                    >
                        <div class="rag-kb-card__top">
                            <span class="rag-kb-card__name">{{ kb.name }}</span>
                            <el-tag size="small" :type="kb.isEnabled ? 'success' : 'info'">
                                {{ kb.isEnabled ? '启用' : '停用' }}
                            </el-tag>
                        </div>
                        <p>{{ kb.description || '暂无描述' }}</p>
                        <div class="rag-kb-card__meta">
                            <span>{{ kb.embeddingProvider || 'provider' }}</span>
                            <span>{{ kb.chunkSize || 0 }} chunk</span>
                        </div>
                    </button>
                </div>
            </aside>

            <section class="rag-main">
                <div v-if="!selectedKb" class="rag-empty glass-panel">
                    <el-empty description="选择左侧知识库开始管理文档" :image-size="110">
                        <el-button type="primary" @click="openCreateDialog">创建第一个知识库</el-button>
                    </el-empty>
                </div>

                <template v-else>
                    <div class="rag-detail-head glass-panel">
                        <div>
                            <div class="rag-detail-title">{{ selectedKb.name }}</div>
                            <div class="rag-detail-desc">{{ selectedKb.description || '未填写描述' }}</div>
                        </div>
                        <div class="rag-detail-actions">
                            <el-button @click="openEditDialog(selectedKb)">编辑</el-button>
                            <el-button type="danger" plain @click="deleteKb(selectedKb)">删除</el-button>
                        </div>
                    </div>

                    <div class="page-stats rag-stats">
                        <div class="mini-stat mini-stat--blue">
                            <div class="mini-stat__num">{{ stats?.documentCount ?? docList.length }}</div>
                            <div class="mini-stat__label">文档总数</div>
                        </div>
                        <div class="mini-stat mini-stat--green">
                            <div class="mini-stat__num">{{ stats?.indexedDocumentCount ?? 0 }}</div>
                            <div class="mini-stat__label">已索引</div>
                        </div>
                        <div class="mini-stat mini-stat--violet">
                            <div class="mini-stat__num">{{ stats?.chunkCount ?? 0 }}</div>
                            <div class="mini-stat__label">切片数量</div>
                        </div>
                        <div class="mini-stat mini-stat--orange">
                            <div class="mini-stat__num">{{ failedDocs.length }}</div>
                            <div class="mini-stat__label">失败文档</div>
                        </div>
                    </div>

                    <el-card shadow="never" class="ai-card rag-health-card">
                        <div class="rag-health">
                            <div class="rag-health__main">
                                <div class="rag-health__title">索引准备度 {{ vectorCoverage }}%</div>
                                <el-progress :percentage="vectorCoverage" :stroke-width="10" :status="ragProgressStatus" />
                            </div>
                            <div class="rag-health__chips">
                                <el-tag size="small" type="success">已索引 {{ docStatusCounts.indexed }}</el-tag>
                                <el-tag size="small" type="warning">队列中 {{ docStatusCounts.pending + docStatusCounts.indexing }}</el-tag>
                                <el-tag size="small" type="danger">失败 {{ failedDocs.length }}</el-tag>
                            </div>
                        </div>
                        <el-alert
                            v-if="failedDocs.length > 0"
                            class="mt-3"
                            type="warning"
                            :closable="false"
                            show-icon
                            :title="`有 ${failedDocs.length} 个文档索引失败，可在表格中查看失败原因并重建索引`"
                        />
                    </el-card>

                    <el-card shadow="never" class="ai-card rag-toolbar-card">
                        <div class="rag-toolbar">
                            <div class="rag-toolbar__left">
                                <el-button type="primary" @click="showImportArticles = true">导入文章</el-button>
                                <el-button @click="showImportWiki = true">导入 Wiki</el-button>
                                <el-button @click="uploadRef?.click()">上传文档</el-button>
                                <el-button type="warning" plain @click="reindexAll">重建全部索引</el-button>
                                <el-button @click="showRetrievalTest = true">检索测试</el-button>
                                <input ref="uploadRef" type="file" accept=".txt,.md,.pdf" class="hidden" @change="handleUpload" />
                            </div>
                            <div v-if="selectedDocs.length > 0" class="rag-batch-bar">
                                <el-tag size="small">已选 {{ selectedDocs.length }}</el-tag>
                                <el-button size="small" type="warning" plain @click="batchReindex">批量重索引</el-button>
                                <el-button size="small" type="danger" plain @click="batchDelete">批量删除</el-button>
                            </div>
                        </div>
                    </el-card>

                    <el-card shadow="never" class="ai-card">
                        <template #header>
                            <div class="flex items-center justify-between">
                                <span class="card-title">文档索引</span>
                                <span class="muted-text text-xs">向量覆盖率 {{ vectorCoverage }}%</span>
                            </div>
                        </template>
                        <el-table
                            :data="docList"
                            v-loading="docsLoading"
                            empty-text="暂无文档，先导入文章或上传文件"
                            @selection-change="handleDocSelection"
                        >
                            <el-table-column type="selection" width="44" />
                            <el-table-column prop="title" label="文档标题" min-width="240" show-overflow-tooltip>
                                <template #default="{ row }">
                                    <div class="doc-title-cell">
                                        <span>{{ row.title }}</span>
                                        <el-tooltip v-if="row.errorMessage" :content="row.errorMessage" placement="top">
                                            <el-icon class="doc-error"><Warning /></el-icon>
                                        </el-tooltip>
                                    </div>
                                </template>
                            </el-table-column>
                            <el-table-column prop="sourceType" label="来源" width="110">
                                <template #default="{ row }">
                                    <el-tag size="small" effect="plain">{{ sourceLabel(row.sourceType) }}</el-tag>
                                </template>
                            </el-table-column>
                            <el-table-column label="状态" width="130">
                                <template #default="{ row }">
                                    <div class="status-cell">
                                        <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
                                        <div v-if="row.status === 'indexing' || row.status === 'pending'" class="status-pulse"></div>
                                    </div>
                                </template>
                            </el-table-column>
                            <el-table-column prop="chunkCount" label="切片" width="90" align="center" />
                            <el-table-column prop="updatedAt" label="更新时间" width="170" />
                            <el-table-column label="操作" width="180" fixed="right">
                                <template #default="{ row }">
                                    <div class="table-actions">
                                        <el-button link type="warning" size="small" @click="reindexDoc(row)">重索引</el-button>
                                        <el-button link type="danger" size="small" @click="deleteDoc(row)">删除</el-button>
                                    </div>
                                </template>
                            </el-table-column>
                        </el-table>
                    </el-card>
                </template>
            </section>
        </div>

        <el-dialog v-model="showKbDialog" :title="editingKb ? '编辑知识库' : '新建知识库'" width="520px">
            <el-form :model="kbForm" label-width="130px">
                <el-form-item label="名称" required>
                    <el-input v-model="kbForm.name" placeholder="例如：项目文档知识库" />
                </el-form-item>
                <el-form-item label="描述">
                    <el-input v-model="kbForm.description" type="textarea" :rows="3" placeholder="说明知识库用途，便于后续检索维护" />
                </el-form-item>
                <el-form-item label="Embedding Provider">
                    <el-select v-model="kbForm.embeddingProvider" class="w-full" placeholder="选择已配置的 Provider">
                        <el-option v-for="p in enabledProviders" :key="p.name" :value="p.name" :label="p.displayName || p.name" />
                        <el-option v-if="enabledProviders.length === 0" disabled value="" label="暂无已配置 Provider，请先添加" />
                    </el-select>
                </el-form-item>
                <el-form-item label="Embedding 模型">
                    <el-input v-model="kbForm.embeddingModel" placeholder="text-embedding-3-small" />
                </el-form-item>
                <el-form-item label="切片大小">
                    <el-input-number v-model="kbForm.chunkSize" :min="100" :max="2000" :step="100" />
                </el-form-item>
                <el-form-item label="切片重叠">
                    <el-input-number v-model="kbForm.chunkOverlap" :min="0" :max="200" :step="10" />
                </el-form-item>
            </el-form>
            <template #footer>
                <div class="dialog-footer-actions">
                    <el-button @click="showKbDialog = false">取消</el-button>
                    <el-button type="primary" @click="submitKb" :loading="saving">保存</el-button>
                </div>
            </template>
        </el-dialog>

        <el-dialog v-model="showRetrievalTest" title="检索测试" width="760px">
            <el-input v-model="testQuery" placeholder="输入测试问题，例如：如何配置 AI Provider？" clearable class="mb-3" />
            <div class="retrieval-controls">
                <div>
                    <label>向量权重 {{ testVectorWeight.toFixed(1) }}</label>
                    <el-slider v-model="testVectorWeight" :min="0" :max="1" :step="0.1" />
                </div>
                <div>
                    <label>关键词权重 {{ testKeywordWeight.toFixed(1) }}</label>
                    <el-slider v-model="testKeywordWeight" :min="0" :max="1" :step="0.1" />
                </div>
            </div>
            <div class="flex items-center gap-3 mb-4">
                <el-button type="primary" @click="runRetrievalTest" :loading="testLoading">开始检索</el-button>
                <span v-if="testSearched" class="muted-text text-xs">耗时 {{ testTimeMs }}ms，共 {{ testResults.length }} 条</span>
            </div>
            <div v-if="testResults.length > 0" class="retrieval-results">
                <div v-for="(chunk, i) in testResults" :key="i" class="retrieval-result">
                    <div class="retrieval-result__meta">
                        <span>#{{ i + 1 }} {{ chunk.documentTitle }}</span>
                        <span>{{ formatScore(chunk.score) }}</span>
                    </div>
                    <div class="retrieval-result__content">{{ chunk.content }}</div>
                </div>
            </div>
            <el-empty v-else-if="testSearched" description="未找到相关内容" />
        </el-dialog>

        <el-dialog v-model="showImportArticles" title="从文章导入" width="640px">
            <el-input v-model="articleSearch" placeholder="搜索文章标题" clearable class="mb-3" @input="searchArticles" />
            <div class="pick-list">
                <button v-for="a in filteredArticles" :key="a.id" class="pick-list__item" @click="toggleArticle(a.id)">
                    <el-checkbox :model-value="selectedArticleIds.includes(a.id)" />
                    <span>{{ a.title }}</span>
                </button>
                <el-empty v-if="filteredArticles.length === 0" description="没有可导入的文章" :image-size="72" />
            </div>
            <template #footer>
                <div class="dialog-footer-actions">
                    <el-button @click="showImportArticles = false">取消</el-button>
                    <el-button type="primary" @click="importArticles" :loading="importing">导入 {{ selectedArticleIds.length }} 篇</el-button>
                </div>
            </template>
        </el-dialog>

        <el-dialog v-model="showImportWiki" title="从 Wiki 导入" width="640px">
            <el-input v-model="wikiSearch" placeholder="搜索 Wiki" clearable class="mb-3" @input="searchWiki" />
            <div class="pick-list">
                <button v-for="w in filteredWiki" :key="w.id" class="pick-list__item" @click="toggleWiki(w.id)">
                    <el-checkbox :model-value="selectedWikiIds.includes(w.id)" />
                    <span>{{ w.title }}</span>
                </button>
                <el-empty v-if="filteredWiki.length === 0" description="没有可导入的 Wiki" :image-size="72" />
            </div>
            <template #footer>
                <div class="dialog-footer-actions">
                    <el-button @click="showImportWiki = false">取消</el-button>
                    <el-button type="primary" @click="importWiki" :loading="importingWiki">导入 {{ selectedWikiIds.length }} 篇</el-button>
                </div>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Collection, Plus, Refresh, Warning } from '@element-plus/icons-vue'
import {
    getKnowledgeBaseList, createKnowledgeBase, updateKnowledgeBase, deleteKnowledgeBase,
    getDocumentList, deleteDocument, reindexDocument, reindexAll as reindexAllApi,
    testRetrieval, getKnowledgeBaseStats, importFromArticles, importFromWiki, uploadDocument, uploadFile
} from '@/api/admin/knowledge'
import { getArticlePageList } from '@/api/admin/article'
import { getEnabledAiProviders } from '@/api/admin/ai-provider'

defineOptions({ name: 'AdminKnowledgeBase' })

const loading = ref(false)
const docsLoading = ref(false)
const kbList = ref([])
const selectedKb = ref(null)
const stats = ref(null)
const selectedDocs = ref([])
const enabledProviders = ref([])
const docList = ref([])

const docStatusCounts = computed(() => {
    const counts = { indexed: 0, pending: 0, indexing: 0, failed: 0, other: 0 }
    docList.value.forEach(doc => {
        const key = doc.status || 'other'
        if (Object.prototype.hasOwnProperty.call(counts, key)) counts[key] += 1
        else counts.other += 1
    })
    return counts
})

const failedDocs = computed(() => docList.value.filter(doc => doc.status === 'failed'))

const vectorCoverage = computed(() => {
    const indexed = stats.value?.indexedDocumentCount ?? docStatusCounts.value.indexed
    const total = stats.value?.documentCount ?? docList.value.length
    if (!total) return 0
    return Math.min(100, Math.round((indexed / total) * 100))
})

const ragProgressStatus = computed(() => {
    if (failedDocs.value.length > 0) return 'exception'
    if (vectorCoverage.value >= 100 && docList.value.length > 0) return 'success'
    return ''
})

const loadKbList = async () => {
    loading.value = true
    try {
        const res = await getKnowledgeBaseList()
        if (res.success || res.code === 200) kbList.value = res.data || []
        else ElMessage.error(res.message || '知识库加载失败')
    } catch (e) {
        ElMessage.error(e.message || '知识库加载失败')
    } finally {
        loading.value = false
    }
}

const selectKb = async (kb) => {
    selectedKb.value = kb
    selectedDocs.value = []
    await Promise.all([loadDocs(), loadStats()])
}

const refreshCurrent = async () => {
    await loadKbList()
    if (selectedKb.value) {
        const latest = kbList.value.find(k => k.id === selectedKb.value.id)
        if (latest) selectedKb.value = latest
        await Promise.all([loadDocs(), loadStats()])
    }
}

const showKbDialog = ref(false)
const editingKb = ref(null)
const saving = ref(false)
const kbForm = ref({
    name: '',
    description: '',
    embeddingProvider: 'openai',
    embeddingModel: 'text-embedding-3-small',
    chunkSize: 500,
    chunkOverlap: 50
})

const openCreateDialog = () => {
    editingKb.value = null
    kbForm.value = {
        name: '',
        description: '',
        embeddingProvider: enabledProviders.value[0]?.name || 'openai',
        embeddingModel: 'text-embedding-3-small',
        chunkSize: 500,
        chunkOverlap: 50
    }
    showKbDialog.value = true
}

const openEditDialog = (kb) => {
    editingKb.value = kb
    kbForm.value = { ...kb }
    showKbDialog.value = true
}

const submitKb = async () => {
    if (!kbForm.value.name.trim()) return ElMessage.warning('请输入知识库名称')
    saving.value = true
    try {
        const res = editingKb.value
            ? await updateKnowledgeBase(editingKb.value.id, kbForm.value)
            : await createKnowledgeBase(kbForm.value)
        if (res.success || res.code === 200) {
            ElMessage.success(editingKb.value ? '更新成功' : '创建成功')
            showKbDialog.value = false
            await loadKbList()
            if (editingKb.value && selectedKb.value?.id === editingKb.value.id) {
                selectedKb.value = kbList.value.find(k => k.id === editingKb.value.id) || selectedKb.value
            }
        } else {
            ElMessage.error(res.message || '保存失败')
        }
    } catch (e) {
        ElMessage.error(e.message || '保存失败')
    } finally {
        saving.value = false
    }
}

const deleteKb = async (kb) => {
    try {
        await ElMessageBox.confirm(`确定删除知识库「${kb.name}」及其所有文档和索引？`, '删除知识库', { type: 'warning' })
        const res = await deleteKnowledgeBase(kb.id)
        if (res.success || res.code === 200) {
            ElMessage.success('已删除')
            if (selectedKb.value?.id === kb.id) selectedKb.value = null
            await loadKbList()
        } else {
            ElMessage.error(res.message || '删除失败')
        }
    } catch (e) {
        if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
    }
}

const loadDocs = async () => {
    if (!selectedKb.value) return
    docsLoading.value = true
    try {
        const res = await getDocumentList(selectedKb.value.id)
        if (res.success || res.code === 200) docList.value = res.data || []
        else ElMessage.error(res.message || '文档加载失败')
        resetPolling()
    } finally {
        docsLoading.value = false
    }
}

const loadStats = async () => {
    if (!selectedKb.value) return
    try {
        const res = await getKnowledgeBaseStats(selectedKb.value.id)
        if (res.success || res.code === 200) stats.value = res.data
    } catch {}
}

const deleteDoc = async (doc) => {
    try {
        await ElMessageBox.confirm(`确定删除文档「${doc.title}」？`, '删除文档', { type: 'warning' })
        const res = await deleteDocument(selectedKb.value.id, doc.id)
        if (res.success || res.code === 200) {
            ElMessage.success('已删除')
            await Promise.all([loadDocs(), loadStats()])
        } else {
            ElMessage.error(res.message || '删除失败')
        }
    } catch (e) {
        if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
    }
}

const reindexDoc = async (doc) => {
    const res = await reindexDocument(selectedKb.value.id, doc.id)
    if (res.success || res.code === 200) {
        ElMessage.success('已提交重新索引')
        await loadDocs()
    } else {
        ElMessage.error(res.message || '提交失败')
    }
}

const reindexAll = async () => {
    try {
        await ElMessageBox.confirm('确定重建整个知识库的索引？', '重建索引', { type: 'warning' })
        const res = await reindexAllApi(selectedKb.value.id)
        if (res.success || res.code === 200) {
            ElMessage.success('已提交重建任务')
            await loadDocs()
        } else {
            ElMessage.error(res.message || '提交失败')
        }
    } catch (e) {
        if (e !== 'cancel') ElMessage.error(e.message || '提交失败')
    }
}

const statusTagType = (status) => ({ indexed: 'success', pending: 'info', indexing: 'warning', failed: 'danger' }[status] || 'info')
const statusLabel = (status) => ({ indexed: '已索引', pending: '等待中', indexing: '索引中', failed: '失败' }[status] || status || '未知')
const sourceLabel = (source) => ({ article: '文章', wiki: 'Wiki', upload: '上传', file: '文件' }[source] || source || '-')
const formatScore = (score) => `${((Number(score) || 0) * 100).toFixed(1)}%`

const uploadRef = ref(null)
const handleUpload = async (e) => {
    const file = e.target.files?.[0]
    if (!file || !selectedKb.value) return
    const ext = file.name.split('.').pop()?.toLowerCase()
    const form = new FormData()
    form.append('file', file)
    try {
        const res = ext === 'pdf'
            ? await uploadFile(selectedKb.value.id, form)
            : await uploadDocument(selectedKb.value.id, form)
        if (res.success || res.code === 200) {
            ElMessage.success('上传成功，正在后台索引')
            await Promise.all([loadDocs(), loadStats()])
        } else {
            ElMessage.error(res.message || '上传失败')
        }
    } catch (err) {
        ElMessage.error(err.message || '上传失败')
    } finally {
        e.target.value = ''
    }
}

const showRetrievalTest = ref(false)
const testQuery = ref('')
const testResults = ref([])
const testLoading = ref(false)
const testSearched = ref(false)
const testTimeMs = ref(0)
const testVectorWeight = ref(0.7)
const testKeywordWeight = ref(0.3)

const runRetrievalTest = async () => {
    if (!testQuery.value.trim()) return ElMessage.warning('请输入测试问题')
    testLoading.value = true
    testSearched.value = false
    testTimeMs.value = 0
    try {
        const res = await testRetrieval(selectedKb.value.id, {
            query: testQuery.value,
            topK: 5,
            vectorWeight: testVectorWeight.value,
            keywordWeight: testKeywordWeight.value
        })
        if (res.success || res.code === 200) {
            const d = res.data
            testResults.value = d?.chunks ?? d ?? []
            testTimeMs.value = d?.retrievalTimeMs ?? 0
        } else {
            testResults.value = []
            ElMessage.error(res.message || '检索失败')
        }
        testSearched.value = true
    } catch (e) {
        ElMessage.error(e.message || '检索失败')
    } finally {
        testLoading.value = false
    }
}

const showImportArticles = ref(false)
const showImportWiki = ref(false)
const articleSearch = ref('')
const allArticles = ref([])
const filteredArticles = ref([])
const selectedArticleIds = ref([])
const importing = ref(false)

const searchArticles = () => {
    const q = articleSearch.value.toLowerCase()
    filteredArticles.value = q ? allArticles.value.filter(a => (a.title || '').toLowerCase().includes(q)) : allArticles.value
}

const toggleArticle = (id) => {
    const idx = selectedArticleIds.value.indexOf(id)
    if (idx >= 0) selectedArticleIds.value.splice(idx, 1)
    else selectedArticleIds.value.push(id)
}

const importArticles = async () => {
    if (!selectedArticleIds.value.length) return ElMessage.warning('请至少选择一篇文章')
    importing.value = true
    try {
        const res = await importFromArticles(selectedKb.value.id, { articleIds: selectedArticleIds.value })
        if (res.success || res.code === 200) {
            const d = res.data
            ElMessage.success(`成功导入 ${d?.imported ?? 0} 篇，正在后台索引`)
            showImportArticles.value = false
            selectedArticleIds.value = []
            await Promise.all([loadDocs(), loadStats()])
        } else {
            ElMessage.error(res.message || '导入失败')
        }
    } finally {
        importing.value = false
    }
}

const wikiSearch = ref('')
const allWiki = ref([])
const filteredWiki = ref([])
const selectedWikiIds = ref([])
const importingWiki = ref(false)

const searchWiki = () => {
    const q = wikiSearch.value.toLowerCase()
    filteredWiki.value = q ? allWiki.value.filter(w => (w.title || '').toLowerCase().includes(q)) : allWiki.value
}

const toggleWiki = (id) => {
    const idx = selectedWikiIds.value.indexOf(id)
    if (idx >= 0) selectedWikiIds.value.splice(idx, 1)
    else selectedWikiIds.value.push(id)
}

const importWiki = async () => {
    if (!selectedWikiIds.value.length) return ElMessage.warning('请至少选择一个 Wiki 页面')
    importingWiki.value = true
    try {
        const res = await importFromWiki(selectedKb.value.id, { wikiIds: selectedWikiIds.value })
        if (res.success || res.code === 200) {
            const d = res.data
            ElMessage.success(`成功导入 ${d?.imported ?? 0} 篇，正在后台索引`)
            showImportWiki.value = false
            selectedWikiIds.value = []
            await Promise.all([loadDocs(), loadStats()])
        } else {
            ElMessage.error(res.message || '导入失败')
        }
    } finally {
        importingWiki.value = false
    }
}

const handleDocSelection = (rows) => { selectedDocs.value = rows }

const batchReindex = async () => {
    if (!selectedDocs.value.length) return
    await Promise.all(selectedDocs.value.map(doc => reindexDocument(selectedKb.value.id, doc.id)))
    ElMessage.success(`已提交 ${selectedDocs.value.length} 个文档的重新索引任务`)
    selectedDocs.value = []
    await loadDocs()
}

const batchDelete = async () => {
    if (!selectedDocs.value.length) return
    try {
        await ElMessageBox.confirm(`确定删除选中的 ${selectedDocs.value.length} 个文档？`, '批量删除', { type: 'warning' })
        await Promise.all(selectedDocs.value.map(doc => deleteDocument(selectedKb.value.id, doc.id)))
        ElMessage.success(`已删除 ${selectedDocs.value.length} 个文档`)
        selectedDocs.value = []
        await Promise.all([loadDocs(), loadStats()])
    } catch (e) {
        if (e !== 'cancel') ElMessage.error(e.message || '删除失败')
    }
}

let statusPollTimer = null
const resetPolling = () => {
    stopPoll()
    if (docList.value.some(d => d.status === 'pending' || d.status === 'indexing')) {
        statusPollTimer = setTimeout(async () => {
            await Promise.all([loadDocs(), loadStats()])
        }, 3000)
    }
}
const stopPoll = () => {
    if (statusPollTimer) {
        clearTimeout(statusPollTimer)
        statusPollTimer = null
    }
}

const loadWikiList = async () => {
    try {
        const { getWikiList } = await import('@/api/admin/wiki')
        const res = await getWikiList()
        allWiki.value = res.data?.list || res.data || []
        filteredWiki.value = allWiki.value
    } catch (_) {}
}

onMounted(async () => {
    await loadKbList()
    if (!selectedKb.value && kbList.value.length > 0) {
        await selectKb(kbList.value[0])
    }
    try {
        const res = await getEnabledAiProviders()
        if (res.success || res.code === 200) enabledProviders.value = res.data || []
    } catch (_) {}
    try {
        const res = await getArticlePageList({ pageIndex: 1, pageSize: 200 })
        allArticles.value = res.data?.list || res.data || []
        filteredArticles.value = allArticles.value
    } catch (_) {}
    loadWikiList()
})

onBeforeUnmount(stopPoll)
</script>

<style scoped>
.rag-page {
    min-height: 100%;
}

.rag-workspace {
    display: grid;
    grid-template-columns: minmax(260px, 320px) minmax(0, 1fr);
    gap: 1.25rem;
    align-items: start;
}

.rag-sidebar {
    position: sticky;
    top: 84px;
    padding: 1rem;
    max-height: calc(100vh - 108px);
    overflow: auto;
}

.rag-sidebar__head,
.rag-detail-head,
.rag-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
}

.rag-kb-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    margin-top: 1rem;
}

.rag-kb-card {
    width: 100%;
    text-align: left;
    padding: 0.875rem;
    border: 1px solid var(--admin-border);
    border-radius: 14px;
    background: var(--admin-bg-soft);
    color: var(--admin-text);
    transition: transform 0.18s ease, border-color 0.18s ease, background 0.18s ease;
}

.rag-kb-card:hover,
.rag-kb-card.is-active {
    transform: translateY(-1px);
    border-color: rgba(34, 211, 238, 0.42);
    background: linear-gradient(135deg, rgba(99, 102, 241, 0.16), rgba(34, 211, 238, 0.08));
}

.rag-kb-card__top,
.rag-kb-card__meta,
.retrieval-result__meta {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
}

.rag-kb-card__name {
    font-weight: 700;
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.rag-kb-card p {
    margin: 0.45rem 0;
    font-size: 0.8125rem;
    color: var(--admin-text-muted);
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

.rag-kb-card__meta {
    color: var(--admin-text-muted);
    font-size: 0.75rem;
}

.rag-main {
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 1rem;
}

.rag-empty,
.rag-detail-head {
    padding: 1.25rem;
}

.rag-detail-title {
    color: var(--admin-text);
    font-size: 1.125rem;
    font-weight: 800;
}

.rag-detail-desc {
    margin-top: 0.25rem;
    color: var(--admin-text-muted);
    font-size: 0.875rem;
}

.rag-detail-actions,
.rag-toolbar__left,
.rag-batch-bar {
    display: flex;
    align-items: center;
    gap: 0.625rem;
    flex-wrap: wrap;
}

.rag-stats {
    grid-template-columns: repeat(4, minmax(0, 1fr));
    margin-bottom: 0;
}

.rag-health-card :deep(.el-card__body) {
    padding: 1rem;
}

.rag-health {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 1rem;
    align-items: center;
}

.rag-health__title {
    margin-bottom: 0.5rem;
    color: var(--admin-text);
    font-size: 0.875rem;
    font-weight: 700;
}

.rag-health__chips {
    display: flex;
    flex-wrap: wrap;
    justify-content: flex-end;
    gap: 0.5rem;
}

.rag-toolbar-card :deep(.el-card__body) {
    padding: 1rem;
}

.doc-title-cell,
.status-cell {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    min-width: 0;
}

.doc-title-cell span {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.doc-error {
    color: var(--admin-danger);
    flex-shrink: 0;
}

.status-pulse {
    width: 34px;
    height: 4px;
    border-radius: 999px;
    background: linear-gradient(90deg, transparent, var(--admin-warning), transparent);
    animation: ragPulse 1.2s ease-in-out infinite;
}

.retrieval-controls {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 1.5rem;
    margin-bottom: 1rem;
}

.retrieval-controls label {
    display: block;
    margin-bottom: 0.25rem;
    font-size: 0.75rem;
    color: var(--admin-text-muted);
}

.retrieval-results,
.pick-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    max-height: 420px;
    overflow: auto;
}

.retrieval-result {
    border: 1px solid var(--admin-border);
    border-radius: 14px;
    background: var(--admin-bg-soft);
    padding: 0.875rem;
}

.retrieval-result__meta {
    color: var(--admin-text-muted);
    font-size: 0.75rem;
    margin-bottom: 0.5rem;
}

.retrieval-result__content {
    color: var(--admin-text);
    font-size: 0.875rem;
    line-height: 1.65;
    white-space: pre-wrap;
}

.pick-list__item {
    display: flex;
    align-items: center;
    gap: 0.625rem;
    width: 100%;
    text-align: left;
    padding: 0.75rem;
    border: 1px solid var(--admin-border);
    border-radius: 12px;
    background: var(--admin-bg-soft);
    color: var(--admin-text);
}

.pick-list__item:hover {
    border-color: rgba(34, 211, 238, 0.4);
    background: var(--admin-bg-hover);
}

@keyframes ragPulse {
    0%, 100% { opacity: 0.35; transform: scaleX(0.78); }
    50% { opacity: 1; transform: scaleX(1); }
}

@media (max-width: 1080px) {
    .rag-workspace,
    .rag-stats,
    .retrieval-controls,
    .rag-health {
        grid-template-columns: 1fr;
    }

    .rag-sidebar {
        position: static;
        max-height: none;
    }

    .rag-health__chips {
        justify-content: flex-start;
    }
}
</style>

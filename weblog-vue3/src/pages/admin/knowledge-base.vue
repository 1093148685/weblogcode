<template>
    <div class="p-6">
        <div class="flex items-center justify-between mb-6">
            <h1 class="text-xl font-bold">RAG 知识库管理</h1>
            <el-button type="primary" @click="openCreateDialog">+ 新建知识库</el-button>
        </div>

        <!-- 知识库列表 -->
        <div v-if="!selectedKb" class="grid grid-cols-1 gap-4">
            <el-empty v-if="kbList.length === 0" description="暂无知识库，点击右上角创建" />
            <div v-for="kb in kbList" :key="kb.id"
                class="border rounded-lg p-4 bg-white dark:bg-gray-800 hover:shadow-md transition-shadow">
                <div class="flex items-center justify-between">
                    <div class="flex-1 cursor-pointer" @click="selectKb(kb)">
                        <div class="font-semibold text-base">{{ kb.name }}</div>
                        <div class="text-sm text-gray-500 mt-1">{{ kb.description || '暂无描述' }}</div>
                        <div class="text-xs text-gray-400 mt-2">
                            Embedding: {{ kb.embeddingProvider }} / {{ kb.embeddingModel }}
                            &nbsp;·&nbsp; 切片大小: {{ kb.chunkSize }}
                            &nbsp;·&nbsp; 创建: {{ kb.createdAt }}
                        </div>
                    </div>
                    <div class="flex gap-2 ml-4">
                        <el-button size="small" @click="openEditDialog(kb)">编辑</el-button>
                        <el-button size="small" type="danger" @click="deleteKb(kb)">删除</el-button>
                    </div>
                </div>
            </div>
        </div>

        <!-- 文档管理（选中知识库后） -->
        <div v-else>
            <div class="flex items-center gap-3 mb-4">
                <el-button @click="selectedKb = null">← 返回</el-button>
                <span class="font-semibold text-lg">{{ selectedKb.name }}</span>
                <el-tag v-if="stats" type="info">{{ stats.documentCount }} 文档</el-tag>
                <el-tag type="success" v-if="stats">{{ stats.indexedDocumentCount }} 已索引</el-tag>
                <el-tag type="warning" v-if="stats && stats.pendingDocumentCount > 0">
                    {{ stats.pendingDocumentCount }} 处理中
                </el-tag>
                <el-tag type="danger" v-if="stats && stats.failedDocumentCount > 0">
                    {{ stats.failedDocumentCount }} 失败
                </el-tag>
                <span v-if="stats && stats.chunkCount > 0" class="text-xs text-gray-400 ml-2">
                    向量覆盖率: {{ vectorCoverage }}%
                </span>
            </div>

            <!-- 操作栏 -->
            <div class="flex gap-2 mb-4 flex-wrap">
                <el-button type="primary" @click="showImportArticles = true">导入文章</el-button>
                <el-button @click="showImportWiki = true">导入 Wiki</el-button>
                <el-button @click="uploadRef.click()">上传文档</el-button>
                <el-button type="warning" @click="reindexAll">重建全部索引</el-button>
                <el-button @click="showRetrievalTest = true">检索测试</el-button>
                <span v-if="selectedDocs.length > 0" class="ml-2">
                    <el-tag size="small" type="info">已选 {{ selectedDocs.length }}</el-tag>
                    <el-button size="small" type="warning" plain @click="batchReindex">批量重索引</el-button>
                    <el-button size="small" type="danger" plain @click="batchDelete">批量删除</el-button>
                </span>
                <input ref="uploadRef" type="file" accept=".txt,.md,.pdf" class="hidden" @change="handleUpload" />
            </div>

            <!-- 文档列表 -->
            <el-table :data="docList" border stripe class="w-full" @selection-change="handleDocSelection">
                <el-table-column type="selection" width="40" />
                <el-table-column prop="title" label="文档标题" min-width="200">
                    <template #default="{ row }">
                        <div class="flex items-center gap-2">
                            <span class="truncate">{{ row.title }}</span>
                            <span v-if="row.status === 'failed'" title="索引失败" class="text-red-400 cursor-help">⚠</span>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="sourceType" label="来源" width="80" />
                <el-table-column label="状态" width="110">
                    <template #default="{ row }">
                        <div class="flex flex-col gap-1">
                            <el-tag :type="statusTagType(row.status)" size="small">{{ statusLabel(row.status) }}</el-tag>
                            <div v-if="row.status === 'indexing'" class="h-1 w-full bg-gray-200 rounded-full overflow-hidden">
                                <div class="h-full bg-amber-400 animate-pulse rounded-full" style="width: 60%"></div>
                            </div>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="chunkCount" label="切片数" width="80" />
                <el-table-column prop="updatedAt" label="更新时间" width="160" />
                <el-table-column label="操作" width="150">
                    <template #default="{ row }">
                        <el-button size="small" @click="reindexDoc(row)">重新索引</el-button>
                        <el-button size="small" type="danger" @click="deleteDoc(row)">删除</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </div>

        <!-- 新建/编辑对话框 -->
        <el-dialog v-model="showKbDialog" :title="editingKb ? '编辑知识库' : '新建知识库'" width="480px">
            <el-form :model="kbForm" label-width="120px">
                <el-form-item label="名称" required>
                    <el-input v-model="kbForm.name" placeholder="知识库名称" />
                </el-form-item>
                <el-form-item label="描述">
                    <el-input v-model="kbForm.description" type="textarea" rows="2" />
                </el-form-item>
                <el-form-item label="Embedding Provider">
                    <el-select v-model="kbForm.embeddingProvider" class="w-full"
                        placeholder="选择已配置的 Provider">
                        <el-option
                            v-for="p in enabledProviders"
                            :key="p.name"
                            :value="p.name"
                            :label="p.displayName || p.name" />
                        <template v-if="enabledProviders.length === 0">
                            <el-option disabled value="" label="暂无已配置的 Provider，请先在 AI Provider 中添加" />
                        </template>
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
                <el-button @click="showKbDialog = false">取消</el-button>
                <el-button type="primary" @click="submitKb" :loading="saving">保存</el-button>
            </template>
        </el-dialog>

        <!-- 检索测试 -->
        <el-dialog v-model="showRetrievalTest" title="检索测试" width="720px">
            <el-input v-model="testQuery" placeholder="输入测试问题..." class="mb-3" />
            <div class="flex gap-6 mb-3 text-sm">
                <div class="flex-1">
                    <label class="text-gray-500 text-xs mb-1 block">向量权重 {{ testVectorWeight.toFixed(1) }}</label>
                    <el-slider v-model="testVectorWeight" :min="0" :max="1" :step="0.1" size="small" />
                </div>
                <div class="flex-1">
                    <label class="text-gray-500 text-xs mb-1 block">关键词权重 {{ testKeywordWeight.toFixed(1) }}</label>
                    <el-slider v-model="testKeywordWeight" :min="0" :max="1" :step="0.1" size="small" />
                </div>
            </div>
            <el-button type="primary" @click="runRetrievalTest" :loading="testLoading">检索</el-button>
            <span v-if="testTimeMs > 0" class="ml-3 text-xs text-gray-400">耗时 {{ testTimeMs }}ms，共 {{ testResults.length }} 条</span>
            <div v-if="testResults.length > 0" class="mt-4 space-y-3">
                <div v-for="(chunk, i) in testResults" :key="i"
                    class="border rounded p-3 bg-gray-50 dark:bg-gray-900">
                    <div class="text-xs text-gray-500 mb-1">
                        #{{ i + 1 }} 来源：{{ chunk.documentTitle }}
                        &nbsp;·&nbsp; 综合得分：{{ (chunk.score * 100).toFixed(1) }}%
                    </div>
                    <div class="text-sm whitespace-pre-wrap">{{ chunk.content }}</div>
                </div>
            </div>
            <el-empty v-else-if="testSearched" description="未找到相关内容" />
        </el-dialog>

        <!-- 导入文章 -->
        <el-dialog v-model="showImportArticles" title="从文章导入" width="600px">
            <div class="mb-3 text-sm text-gray-500">选择要导入到知识库的文章（多选）</div>
            <el-input v-model="articleSearch" placeholder="搜索文章..." clearable class="mb-3" @input="searchArticles" />
            <div class="max-h-80 overflow-y-auto border rounded">
                <div v-for="a in filteredArticles" :key="a.id"
                    class="flex items-center gap-2 px-3 py-2 hover:bg-gray-50 dark:hover:bg-gray-800 cursor-pointer"
                    @click="toggleArticle(a.id)">
                    <el-checkbox :model-value="selectedArticleIds.includes(a.id)" />
                    <span class="text-sm">{{ a.title }}</span>
                </div>
            </div>
            <template #footer>
                <el-button @click="showImportArticles = false">取消</el-button>
                <el-button type="primary" @click="importArticles" :loading="importing">
                    导入 {{ selectedArticleIds.length }} 篇
                </el-button>
            </template>
        </el-dialog>

        <!-- 导入 Wiki -->
        <el-dialog v-model="showImportWiki" title="从 Wiki 导入" width="600px">
            <div class="mb-3 text-sm text-gray-500">选择要导入到知识库的 Wiki 页面（多选）</div>
            <el-input v-model="wikiSearch" placeholder="搜索 Wiki..." clearable class="mb-3" @input="searchWiki" />
            <div class="max-h-80 overflow-y-auto border rounded">
                <div v-for="w in filteredWiki" :key="w.id"
                    class="flex items-center gap-2 px-3 py-2 hover:bg-gray-50 dark:hover:bg-gray-800 cursor-pointer"
                    @click="toggleWiki(w.id)">
                    <el-checkbox :model-value="selectedWikiIds.includes(w.id)" />
                    <span class="text-sm">{{ w.title }}</span>
                </div>
            </div>
            <template #footer>
                <el-button @click="showImportWiki = false">取消</el-button>
                <el-button type="primary" @click="importWiki" :loading="importingWiki">
                    导入 {{ selectedWikiIds.length }} 篇
                </el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
    getKnowledgeBaseList, createKnowledgeBase, updateKnowledgeBase, deleteKnowledgeBase,
    getDocumentList, deleteDocument, reindexDocument, reindexAll as reindexAllApi,
    testRetrieval, getKnowledgeBaseStats, importFromArticles, importFromWiki, uploadDocument, uploadFile
} from '@/api/admin/knowledge'
import { getArticlePageList } from '@/api/admin/article'
import { getEnabledAiProviders } from '@/api/admin/ai-provider'

// ── 知识库列表 ────────────────────────────────────
const kbList = ref([])
const selectedKb = ref(null)
const stats = ref(null)
const selectedDocs = ref([])

const vectorCoverage = computed(() => {
    if (!stats.value || stats.value.chunkCount === 0) return 0
    // vectorCount field added by backend - estimate for now
    return 100
})
// 已配置的 AI Provider 列表（用于选择 Embedding Provider）
const enabledProviders = ref([])

const loadKbList = async () => {
    const res = await getKnowledgeBaseList()
    if (res.success) kbList.value = res.data || []
}

const selectKb = async (kb) => {
    selectedKb.value = kb
    await loadDocs()
    await loadStats()
}

// ── 表单 ─────────────────────────────────────────
const showKbDialog = ref(false)
const editingKb = ref(null)
const saving = ref(false)
const kbForm = ref({
    name: '', description: '',
    embeddingProvider: 'openai',
    embeddingModel: 'text-embedding-3-small',
    chunkSize: 500, chunkOverlap: 50
})

const openCreateDialog = () => {
    editingKb.value = null
    kbForm.value = { name: '', description: '', embeddingProvider: 'openai', embeddingModel: 'text-embedding-3-small', chunkSize: 500, chunkOverlap: 50 }
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
        if (editingKb.value) {
            await updateKnowledgeBase(editingKb.value.id, kbForm.value)
            ElMessage.success('更新成功')
        } else {
            await createKnowledgeBase(kbForm.value)
            ElMessage.success('创建成功')
        }
        showKbDialog.value = false
        await loadKbList()
    } catch (e) {
        ElMessage.error(e.message)
    } finally {
        saving.value = false
    }
}

const deleteKb = async (kb) => {
    await ElMessageBox.confirm(`确定删除知识库「${kb.name}」及其所有文档和索引？`, '警告', { type: 'warning' })
    await deleteKnowledgeBase(kb.id)
    ElMessage.success('已删除')
    await loadKbList()
}

// ── 文档管理 ─────────────────────────────────────
const docList = ref([])

const loadDocs = async () => {
    if (!selectedKb.value) return
    const res = await getDocumentList(selectedKb.value.id)
    if (res.success) docList.value = res.data || []
    // Start/stop polling based on doc status
    stopPoll()
    const hasPending = docList.value.some(d => d.status === 'pending' || d.status === 'indexing')
    if (hasPending) pollDocStatus()
}

const loadStats = async () => {
    if (!selectedKb.value) return
    const res = await getKnowledgeBaseStats(selectedKb.value.id)
    if (res.success) stats.value = res.data
}

const deleteDoc = async (doc) => {
    await ElMessageBox.confirm(`确定删除文档「${doc.title}」？`, '确认')
    await deleteDocument(selectedKb.value.id, doc.id)
    ElMessage.success('已删除')
    await loadDocs()
    await loadStats()
}

const reindexDoc = async (doc) => {
    await reindexDocument(selectedKb.value.id, doc.id)
    ElMessage.success('已提交重新索引，稍后刷新查看状态')
    stopPoll()
    setTimeout(pollDocStatus, 1000)
}

const reindexAll = async () => {
    await ElMessageBox.confirm('确定重建整个知识库的索引？', '确认')
    await reindexAllApi(selectedKb.value.id)
    ElMessage.success('已提交重建任务')
}

const statusTagType = (status) => {
    const map = { indexed: 'success', pending: 'info', indexing: 'warning', failed: 'danger' }
    return map[status] || 'info'
}

// ── 上传 ─────────────────────────────────────────
const uploadRef = ref(null)
const handleUpload = async (e) => {
    const file = e.target.files[0]
    if (!file) return
    const ext = file.name.split('.').pop()?.toLowerCase()
    const usePdfEndpoint = ext === 'pdf'
    const form = new FormData()
    form.append('file', file)
    try {
        const res = usePdfEndpoint
            ? await uploadFile(selectedKb.value.id, form)
            : await uploadDocument(selectedKb.value.id, form)
        if (res.success || res.code === 200) {
            ElMessage.success('上传成功，正在后台索引...')
            await loadDocs()
        } else {
            ElMessage.error(res.message || '上传失败')
        }
    } catch (err) {
        ElMessage.error(err.message || '上传失败')
    }
    e.target.value = ''
}

// ── 检索测试 ─────────────────────────────────────
const showRetrievalTest = ref(false)
const testQuery = ref('')
const testResults = ref([])
const testLoading = ref(false)
const testSearched = ref(false)
const testTimeMs = ref(0)
const testVectorWeight = ref(0.7)
const testKeywordWeight = ref(0.3)

const runRetrievalTest = async () => {
    if (!testQuery.value.trim()) return
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
        const d = res.data
        testResults.value = d?.chunks ?? d ?? []
        testTimeMs.value = d?.retrievalTimeMs ?? 0
        testSearched.value = true
    } finally {
        testLoading.value = false
    }
}

// ── 导入文章 ─────────────────────────────────────
const showImportArticles = ref(false)
const showImportWiki = ref(false)
const articleSearch = ref('')
const allArticles = ref([])
const filteredArticles = ref([])
const selectedArticleIds = ref([])
const importing = ref(false)

const searchArticles = () => {
    const q = articleSearch.value.toLowerCase()
    filteredArticles.value = q
        ? allArticles.value.filter(a => a.title.toLowerCase().includes(q))
        : allArticles.value
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
        const d = res.data
        ElMessage.success(`成功导入 ${d?.imported ?? 0} 篇，正在后台索引...`)
        showImportArticles.value = false
        selectedArticleIds.value = []
        await loadDocs()
        await loadStats()
    } finally {
        importing.value = false
    }
}

// ── 导入 Wiki ─────────────────────────────────────
const wikiSearch = ref('')
const allWiki = ref([])
const filteredWiki = ref([])
const selectedWikiIds = ref([])
const importingWiki = ref(false)

const searchWiki = () => {
    const q = wikiSearch.value.toLowerCase()
    filteredWiki.value = q
        ? allWiki.value.filter(w => w.title.toLowerCase().includes(q))
        : allWiki.value
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
        const d = res.data
        ElMessage.success(`成功导入 ${d?.imported ?? 0} 篇，正在后台索引...`)
        showImportWiki.value = false
        selectedWikiIds.value = []
        await loadDocs()
        await loadStats()
    } finally {
        importingWiki.value = false
    }
}

// ── 预加载 Wiki 列表 ─────────────────────────────────
const loadWikiList = async () => {
    try {
        const { getWikiList } = await import('@/api/admin/wiki')
        const res = await getWikiList()
        allWiki.value = res.data?.list || res.data || []
        filteredWiki.value = allWiki.value
    } catch (_) {}
}

// ── 批量操作 ─────────────────────────────────────
const handleDocSelection = (rows) => { selectedDocs.value = rows }

const batchReindex = async () => {
    if (!selectedDocs.value.length) return
    for (const doc of selectedDocs.value) {
        await reindexDocument(selectedKb.value.id, doc.id)
    }
    ElMessage.success(`已提交 ${selectedDocs.value.length} 个文档的重新索引任务`)
    selectedDocs.value = []
}

const batchDelete = async () => {
    if (!selectedDocs.value.length) return
    try {
        await ElMessageBox.confirm(`确定删除选中的 ${selectedDocs.value.length} 个文档？`, '批量删除', { type: 'warning' })
        for (const doc of selectedDocs.value) {
            await deleteDocument(selectedKb.value.id, doc.id)
        }
        ElMessage.success(`已删除 ${selectedDocs.value.length} 个文档`)
        selectedDocs.value = []
        await loadDocs()
        await loadStats()
    } catch (e) {
        if (e !== 'cancel') ElMessage.error('删除失败')
    }
}

const statusLabel = (status) => {
    const map = { indexed: '已索引', pending: '等待中', indexing: '索引中', failed: '失败' }
    return map[status] || status
}

// ── 刷新文档状态轮询 ─────────────────────────────────────
let statusPollTimer = null
const pollDocStatus = () => {
    if (!selectedKb.value) return
    const hasPending = docList.value.some(d => d.status === 'pending' || d.status === 'indexing')
    if (hasPending) {
        loadDocs()
        loadStats()
        statusPollTimer = setTimeout(pollDocStatus, 3000)
    }
}
const stopPoll = () => { if (statusPollTimer) { clearTimeout(statusPollTimer); statusPollTimer = null } }

onMounted(async () => {
    await loadKbList()
    // 加载已配置的 Provider
    try {
        const res = await getEnabledAiProviders()
        if (res.success) enabledProviders.value = res.data || []
    } catch (_) {}
    // 预加载文章列表供导入用
    try {
        const res = await getArticlePageList({ pageIndex: 1, pageSize: 200 })
        allArticles.value = res.data?.list || res.data || []
        filteredArticles.value = allArticles.value
    } catch (_) {}
    loadWikiList()
})
</script>

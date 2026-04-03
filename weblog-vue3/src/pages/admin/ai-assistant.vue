<template>
    <div class="ai-assistant-root p-6">
        <!-- 页头 -->
        <div class="flex items-center justify-between mb-6">
            <div class="flex items-center gap-3">
                <div class="page-header__icon">
                    <el-icon :size="20"><MagicStick /></el-icon>
                </div>
                <div>
                    <h1 class="text-lg font-bold text-slate-800">AI 写作助手</h1>
                    <p class="text-sm text-slate-400 mt-0.5">一键生成文章 · SEO 优化 · 内容安全检测</p>
                </div>
            </div>
            <div class="flex items-center gap-2">
                <el-tag size="small" type="info">Token 统计</el-tag>
                <el-button size="small" @click="loadTokenStats" :loading="statsLoading">
                    <el-icon><Refresh /></el-icon>
                </el-button>
                <el-tag size="small" type="info" class="cursor-pointer" @click="showHistory = !showHistory" v-if="genHistory.length > 0">
                    历史 {{ genHistory.length }}
                </el-tag>
            </div>
        </div>

        <!-- 历史生成记录 -->
        <el-drawer v-model="showHistory" title="生成历史" size="300px" direction="rtl">
            <div v-if="genHistory.length === 0" class="text-center text-gray-400 py-10">暂无历史记录</div>
            <div v-else class="space-y-2">
                <div v-for="(h, i) in genHistory" :key="i"
                    class="p-3 border rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 cursor-pointer transition-colors group"
                    @click="loadFromHistory(h)">
                    <div class="flex items-start justify-between">
                        <div class="flex-1 min-w-0">
                            <div class="text-sm font-medium truncate">{{ h.title }}</div>
                            <div class="text-xs text-gray-400 mt-1">{{ h.style }} · {{ h.time }}</div>
                        </div>
                        <button class="text-gray-300 hover:text-red-400 opacity-0 group-hover:opacity-100 transition-opacity"
                            @click.stop="removeHistory(i)">
                            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24"><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0"/></svg>
                        </button>
                    </div>
                    <div class="text-xs text-gray-500 mt-1 line-clamp-2">{{ h.content?.slice(0, 80) }}...</div>
                </div>
                <el-button size="small" type="danger" plain class="w-full mt-2" @click="clearHistory">清空历史</el-button>
            </div>
        </el-drawer>

        <!-- Token 统计 -->
        <div v-if="tokenStats.length > 0" class="grid grid-cols-4 gap-3 mb-6">
            <div v-for="s in tokenStats" :key="s.date" class="bg-white dark:bg-gray-800 border rounded-lg px-4 py-3">
                <div class="text-xs text-gray-400">{{ s.date }}</div>
                <div class="text-lg font-bold text-blue-600 mt-1">{{ (s.totalTokens / 1000).toFixed(1) }}K</div>
                <div class="text-xs text-gray-400">{{ s.totalRequests }} 次</div>
            </div>
        </div>

        <div class="grid grid-cols-3 gap-5">
            <!-- ── 1. 一键生成文章 ── -->
            <div class="col-span-2">
                <el-card shadow="never" class="ai-card">
                    <template #header>
                        <div class="flex items-center gap-2">
                            <span class="card-title">一键生成文章</span>
                        </div>
                    </template>
                    <el-form :model="genForm" label-width="90px">
                        <el-form-item label="文章标题">
                            <el-input v-model="genForm.title" placeholder="输入文章主题，如：Vue3 响应式原理深度解析" />
                        </el-form-item>
                        <el-form-item label="写作风格">
                            <el-select v-model="genForm.style" class="!w-full">
                                <el-option label="技术教程" value="技术" />
                                <el-option label="随笔感悟" value="随笔" />
                                <el-option label="深度分析" value="分析" />
                                <el-option label="新闻报道" value="新闻" />
                            </el-select>
                        </el-form-item>
                        <el-form-item label="文章大纲">
                            <el-input v-model="genForm.outline" type="textarea" :rows="3"
                                placeholder="可选，描述文章结构或主要论点（JSON 格式或自然语言）" />
                        </el-form-item>
                        <el-form-item label="字数目标">
                            <el-input-number v-model="genForm.wordCount" :min="300" :max="5000" :step="100" />
                            <span class="ml-2 text-sm text-gray-400">字</span>
                        </el-form-item>
                        <el-form-item>
                            <el-button type="primary" @click="generateArticle" :loading="genLoading" icon="MagicStick">
                                生成文章
                            </el-button>
                            <el-button @click="copyGenContent" :disabled="!genContent" class="ml-2">
                                复制内容
                            </el-button>
                            <el-button type="success" @click="publishArticle" :disabled="!genContent" plain class="ml-2" v-if="isAdmin()">
                                发布文章
                            </el-button>
                            <el-button @click="clearGen" :disabled="!genContent" plain class="ml-2">
                                清空
                            </el-button>
                        </el-form-item>
                    </el-form>

                    <!-- 生成结果 -->
                    <div v-if="genContent" class="mt-4 border-t pt-4">
                        <div class="flex items-center justify-between mb-2">
                            <span class="text-sm font-medium text-gray-700">生成结果</span>
                            <span class="text-xs text-gray-400">{{ genContent.length }} 字</span>
                        </div>
                        <div class="result-box" v-html="renderMd(genContent)"></div>
                    </div>
                </el-card>
            </div>

            <!-- ── 右侧两个工具 ── -->
            <div class="flex flex-col gap-5">
                <!-- SEO 优化建议 -->
                <el-card shadow="never" class="ai-card">
                    <template #header>
                        <div class="flex items-center gap-2">
                            <span class="card-title">SEO 优化建议</span>
                        </div>
                    </template>
                    <el-form :model="seoForm" label-width="80px">
                        <el-form-item label="文章标题">
                            <el-input v-model="seoForm.title" placeholder="文章标题" />
                        </el-form-item>
                        <el-form-item label="关键词">
                            <el-input v-model="seoForm.keywords" placeholder="可选，逗号分隔" />
                        </el-form-item>
                        <el-form-item label="文章内容">
                            <el-input v-model="seoForm.content" type="textarea" :rows="4" placeholder="输入文章正文..." />
                        </el-form-item>
                        <el-form-item>
                            <el-button type="warning" @click="runSeo" :loading="seoLoading">
                                分析 SEO
                            </el-button>
                        </el-form-item>
                    </el-form>

                    <!-- SEO 结果 -->
                    <div v-if="seoResult" class="mt-3 border-t pt-3">
                        <div class="flex items-center gap-3 mb-3">
                            <div class="text-3xl font-bold" :class="seoScoreClass">{{ seoResult.score }}</div>
                            <div class="text-sm">
                                <div class="font-medium">SEO 评分</div>
                                <el-rate v-model="seoResult.score" disabled allow-half text-size="12px"
                                    :colors="['#e74c3c','#f39c12','#27ae60']" />
                            </div>
                        </div>
                        <div class="space-y-2">
                            <div v-if="seoResult.titleSuggestion" class="text-sm">
                                <span class="text-gray-500">标题建议：</span>
                                <span class="text-gray-700">{{ seoResult.titleSuggestion }}</span>
                            </div>
                            <div v-if="seoResult.keywordDensity" class="text-sm">
                                <span class="text-gray-500">关键词密度：</span>
                                <span class="text-gray-700">{{ seoResult.keywordDensity }}</span>
                            </div>
                            <div v-if="seoResult.readability" class="text-sm">
                                <span class="text-gray-500">可读性：</span>
                                <span class="text-gray-700">{{ seoResult.readability }}</span>
                            </div>
                            <div v-if="seoResult.suggestions?.length" class="mt-2">
                                <div class="text-xs text-gray-500 font-medium mb-1">优化建议：</div>
                                <ul class="text-xs text-gray-600 space-y-1 list-disc pl-3">
                                    <li v-for="(s, i) in seoResult.suggestions" :key="i">{{ s }}</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </el-card>

                <!-- 内容安全检测 -->
                <el-card shadow="never" class="ai-card">
                    <template #header>
                        <div class="flex items-center gap-2">
                            <span class="card-title">内容安全检测</span>
                        </div>
                    </template>
                    <el-input v-model="modContent" type="textarea" :rows="4" placeholder="输入要检测的文章内容..." class="mb-3" />
                    <el-button type="danger" @click="runModerate" :loading="modLoading" class="mb-3">
                        检测内容安全
                    </el-button>

                    <!-- 检测结果 -->
                    <div v-if="modResult" class="border-t pt-3">
                        <div class="flex items-center gap-3 mb-2">
                            <el-tag :type="modResult.level === 'safe' ? 'success' : modResult.level === 'warning' ? 'warning' : 'danger'">
                                {{ modResult.level === 'safe' ? '安全' : modResult.level === 'warning' ? '需审核' : '有风险' }}
                            </el-tag>
                            <span class="text-sm text-gray-600">{{ modResult.label }}</span>
                        </div>
                        <div v-if="modResult.issues?.length" class="mt-2">
                            <div class="text-xs text-red-500 font-medium mb-1">发现问题：</div>
                            <ul class="text-xs text-red-400 space-y-1 list-disc pl-3">
                                <li v-for="(issue, i) in modResult.issues" :key="i">{{ issue }}</li>
                            </ul>
                        </div>
                    </div>
                </el-card>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { MagicStick, Refresh } from '@element-plus/icons-vue'
import { marked } from 'marked'
import { ElMessage } from 'element-plus'
import { generateArticle as apiGenerateArticle, seoOptimize, moderateContent, getTokenStats } from '@/api/admin/ai-assistant'
import { publishArticle as apiPublishArticle } from '@/api/admin/article'
import { isAdmin } from '@/composables/util'

// ── Token 统计 ─────────────────────────────────────
const tokenStats = ref([])
const statsLoading = ref(false)

const publishArticle = async () => {
    if (!genContent.value.trim()) return ElMessage.warning('先生成文章内容')
    try {
        const res = await apiPublishArticle({
            title: genForm.value.title,
            content: genContent.value,
            isPublish: false,
            isTop: false,
            categoryId: 0,
            tags: []
        })
        if (res.success) {
            ElMessage.success('文章已保存为草稿，可在文章管理中编辑发布')
        } else {
            ElMessage.error(res.message || '发布失败')
        }
    } catch (e) {
        ElMessage.error(e.message || '发布失败')
    }
}

const loadTokenStats = async () => {
    statsLoading.value = true
    try {
        const res = await getTokenStats(7)
        if (res.success) tokenStats.value = res.data || []
    } catch {}
    finally { statsLoading.value = false }
}

// ── 文章生成 ─────────────────────────────────────
const genForm = ref({
    title: '',
    outline: '',
    style: '技术',
    wordCount: 800
})
const genContent = ref('')
const genLoading = ref(false)
const genHistory = ref([])
const showHistory = ref(false)

// Load history from localStorage
const STORAGE_KEY = 'ai_gen_history'
try {
    const saved = localStorage.getItem(STORAGE_KEY)
    if (saved) genHistory.value = JSON.parse(saved)
} catch {}
const saveHistory = () => {
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(genHistory.value)) } catch {}
}
const addToHistory = (title, content, style) => {
    genHistory.value.unshift({ title, content, style, time: new Date().toLocaleString('zh-CN') })
    if (genHistory.value.length > 10) genHistory.value = genHistory.value.slice(0, 10)
    saveHistory()
}
const loadFromHistory = (h) => {
    genForm.value.title = h.title
    genForm.value.style = h.style
    genContent.value = h.content
    showHistory.value = false
}
const removeHistory = (idx) => { genHistory.value.splice(idx, 1); saveHistory() }
const clearHistory = () => { genHistory.value = []; saveHistory(); ElMessage.success('已清空') }

// Auto-save draft to localStorage
const DRAFT_KEY = 'ai_gen_draft'
try {
    const draft = localStorage.getItem(DRAFT_KEY)
    if (draft) {
        const d = JSON.parse(draft)
        if (d.title) genForm.value.title = d.title
        if (d.outline) genForm.value.outline = d.outline
        if (d.style) genForm.value.style = d.style
        if (d.wordCount) genForm.value.wordCount = d.wordCount
    }
} catch {}
const saveDraft = () => {
    try { localStorage.setItem(DRAFT_KEY, JSON.stringify(genForm.value)) } catch {}
}
watch(genForm, saveDraft, { deep: true })

const generateArticle = async () => {
    if (!genForm.value.title.trim()) return ElMessage.warning('请输入文章标题')
    genLoading.value = true
    genContent.value = ''
    try {
        const res = await apiGenerateArticle({
            title: genForm.value.title,
            outline: genForm.value.outline || undefined,
            style: genForm.value.style,
            wordCount: genForm.value.wordCount
        })
        if (res.success && res.data) {
            const content = res.data.content || res.data
            genContent.value = content
            addToHistory(genForm.value.title, content, genForm.value.style)
            // Clear draft after successful generation
            try { localStorage.removeItem(DRAFT_KEY) } catch {}
        } else {
            ElMessage.error(res.message || '生成失败')
        }
    } catch (e) {
        ElMessage.error(e.message || '生成失败')
    } finally {
        genLoading.value = false
    }
}

const copyGenContent = async () => {
    try {
        await navigator.clipboard.writeText(genContent.value)
        ElMessage.success('已复制到剪贴板')
    } catch {
        ElMessage.error('复制失败')
    }
}

const clearGen = () => { genContent.value = '' }

// ── SEO 优化 ─────────────────────────────────────
const seoForm = ref({ title: '', keywords: '', content: '' })
const seoResult = ref(null)
const seoLoading = ref(false)

const seoScoreClass = computed(() => {
    if (!seoResult.value) return ''
    const s = seoResult.value.score
    if (s >= 80) return 'text-green-600'
    if (s >= 60) return 'text-yellow-500'
    return 'text-red-500'
})

const runSeo = async () => {
    if (!seoForm.value.content.trim()) return ElMessage.warning('请输入文章内容')
    seoLoading.value = true
    try {
        const res = await seoOptimize({
            title: seoForm.value.title,
            content: seoForm.value.content,
            keywords: seoForm.value.keywords || undefined
        })
        if (res.success && res.data) {
            let data = res.data
            // 兼容直接返回字符串或 JSON 对象
            if (typeof data === 'string') {
                try { data = JSON.parse(data) } catch { data = { score: 0, label: data } }
            }
            seoResult.value = data
        } else {
            ElMessage.error(res.message || '分析失败')
        }
    } catch (e) {
        ElMessage.error(e.message || '分析失败')
    } finally {
        seoLoading.value = false
    }
}

// ── 内容安全检测 ─────────────────────────────────────
const modContent = ref('')
const modResult = ref(null)
const modLoading = ref(false)

const runModerate = async () => {
    if (!modContent.value.trim()) return ElMessage.warning('请输入要检测的内容')
    modLoading.value = true
    try {
        const res = await moderateContent({ content: modContent.value })
        if (res.success && res.data) {
            let data = res.data
            if (typeof data === 'string') {
                try { data = JSON.parse(data) } catch { data = { level: 'safe', label: data } }
            }
            modResult.value = data
        } else {
            ElMessage.error(res.message || '检测失败')
        }
    } catch (e) {
        ElMessage.error(e.message || '检测失败')
    } finally {
        modLoading.value = false
    }
}

// ── 工具 ─────────────────────────────────────
const renderMd = (text) => {
    if (!text) return ''
    try { return marked.parse(text) } catch { return text }
}

// ── 初始化 ─────────────────────────────────────
loadTokenStats()
</script>

<style scoped>
.ai-assistant-root {
    min-height: 100%;
}

.page-header__icon {
    width: 40px;
    height: 40px;
    border-radius: 10px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
}

.ai-card {
    border-radius: 14px !important;
}

.card-title {
    font-size: 14px;
    font-weight: 600;
    color: var(--text-heading);
}

.result-box {
    background: #f8fafc;
    border: 1px solid #e2e8f0;
    border-radius: 10px;
    padding: 16px;
    max-height: 500px;
    overflow-y: auto;
    font-size: 14px;
    line-height: 1.7;
}

:global(html.dark) .result-box {
    background: #1e293b;
    border-color: #334155;
}

.result-box :deep(h1) { font-size: 1.5em; font-weight: 700; margin: 0.8em 0 0.4em; border-bottom: 1px solid #e5e7eb; padding-bottom: 0.3em; }
.result-box :deep(h2) { font-size: 1.3em; font-weight: 600; margin: 0.7em 0 0.3em; }
.result-box :deep(h3) { font-size: 1.1em; font-weight: 600; margin: 0.6em 0 0.3em; }
.result-box :deep(p) { margin: 0.5em 0; line-height: 1.7; }
.result-box :deep(ul) { list-style: disc; padding-left: 1.5em; margin: 0.5em 0; }
.result-box :deep(ol) { list-style: decimal; padding-left: 1.5em; margin: 0.5em 0; }
.result-box :deep(code) { background: #f1f5f9; color: #e11d48; padding: 0.1em 0.4em; border-radius: 4px; font-size: 0.9em; }
.result-box :deep(pre) { background: #1e293b; color: #e2e8f0; padding: 1em; border-radius: 8px; overflow-x: auto; margin: 0.5em 0; }
.result-box :deep(pre code) { background: transparent; color: inherit; padding: 0; }
.result-box :deep(blockquote) { border-left: 3px solid #3b82f6; padding: 0.5em 1em; margin: 0.5em 0; background: #f8fafc; border-radius: 0 6px 6px 0; }
.result-box :deep(table) { border-collapse: collapse; width: 100%; margin: 0.5em 0; font-size: 0.9em; }
.result-box :deep(th) { background: #f1f5f9; font-weight: 600; padding: 0.5em 0.8em; border: 1px solid #e2e8f0; }
.result-box :deep(td) { padding: 0.4em 0.8em; border: 1px solid #e2e8f0; }
</style>

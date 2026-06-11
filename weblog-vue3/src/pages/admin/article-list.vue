<template>
    <div>
        <!-- 表头分页查询条件 -->
        <el-card class="mb-5">
            <!-- flex 布局，内容垂直居中 -->
            <div class="flex items-center flex-wrap gap-3">
                <span class="text-sm text-[var(--admin-text)] font-medium">文章标题</span>
                <div class="w-52"><el-input v-model="searchArticleTitle" placeholder="请输入（模糊查询）" size="default" /></div>

                <span class="text-sm text-[var(--admin-text)] font-medium">创建日期</span>
                <div class="w-72">
                    <!-- 日期选择组件（区间选择） -->
                    <el-date-picker v-model="pickDate" type="daterange" range-separator="至" start-placeholder="开始时间"
                        end-placeholder="结束时间" size="default" :shortcuts="shortcuts" @change="datepickerChange" style="width: 100%" />
                </div>

                <el-button type="primary" class="ml-auto" :icon="Search" @click="getTableData">查询</el-button>
                <el-button :icon="RefreshRight" @click="reset">重置</el-button>
            </div>
        </el-card>

        <el-card>
            <!-- 写文章按钮 -->
            <div class="mb-5">
                <el-button type="primary" @click="isArticlePublishEditorShow = true">
                    <el-icon class="mr-1">
                        <EditPen />
                    </el-icon>
                    写文章</el-button>
            </div>

            <!-- 分页列表 -->
            <el-table :data="tableData" border stripe style="width: 100%" class="admin-table compact-admin-table" v-loading="tableLoading">
                <el-table-column prop="id" label="ID" width="72" align="center" />
                <el-table-column prop="title" label="标题" min-width="360" show-overflow-tooltip>
                    <template #default="scope">
                        <div class="article-title-cell">
                            <span class="article-title-text">{{ scope.row.title }}</span>
                            <span
                                :class="[
                                    'article-summary-chip',
                                    aiSummaryStatus[scope.row.id]?.hasSummary ? 'is-ready' : 'is-empty'
                                ]"
                            >
                                <el-icon v-if="aiSummaryStatus[scope.row.id]?.loading" class="is-loading"><Loading /></el-icon>
                                <el-icon v-else-if="aiSummaryStatus[scope.row.id]?.hasSummary"><CircleCheck /></el-icon>
                                <el-icon v-else><CircleClose /></el-icon>
                                {{ aiSummaryStatus[scope.row.id]?.loading ? '摘要生成中' : aiSummaryStatus[scope.row.id]?.hasSummary ? '已有 AI 摘要' : '暂无 AI 摘要' }}
                            </span>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="cover" label="封面" width="140" align="center">
                    <template #default="scope">
                        <el-image style="width: 100px;" :src="scope.row.cover" />
                    </template>
                </el-table-column>
                <el-table-column prop="isTop" label="置顶" width="100" align="center">
                    <template #default="scope">
                        <el-switch
                            @change="handleIsTopChange(scope.row)"
                            v-model="scope.row.isTop"
                            inline-prompt
                            :active-icon="Check"
                            :inactive-icon="Close"
                        />
                    </template>
                </el-table-column>
                <el-table-column prop="status" label="发布状态" width="120" align="center">
                    <template #default="scope">
                        <el-switch
                            :model-value="scope.row.status === 1"
                            inline-prompt
                            active-text="发布"
                            inactive-text="草稿"
                            @change="value => handleArticleStatusChange(scope.row, value)"
                        />
                    </template>
                </el-table-column>
                <el-table-column prop="createTime" label="发布时间" width="190" show-overflow-tooltip />
                <el-table-column label="操作" width="230" fixed="right">
                    <template #default="scope">
                        <div class="table-action-group">
                            <el-button size="small" @click="showArticleUpdateEditor(scope.row)">
                                <el-icon class="mr-1"><Edit /></el-icon>
                                编辑
                            </el-button>
                            <el-button size="small" @click="goArticleDetailPage(scope.row.id)">
                                <el-icon class="mr-1"><View /></el-icon>
                                预览
                            </el-button>
                            <el-dropdown trigger="click" placement="bottom-end">
                                <el-button size="small" class="more-action-btn">
                                    更多
                                </el-button>
                                <template #dropdown>
                                    <el-dropdown-menu>
                                        <el-dropdown-item
                                            :disabled="aiSummaryStatus[scope.row.id]?.loading"
                                            @click="generateAiSummary(scope.row)"
                                        >
                                            <el-icon><MagicStick /></el-icon>
                                            {{ aiSummaryStatus[scope.row.id]?.loading ? '摘要生成中' : '生成 AI 摘要' }}
                                        </el-dropdown-item>
                                        <el-dropdown-item class="danger-dropdown-item" @click="deleteArticleSubmit(scope.row)">
                                            <el-icon><Delete /></el-icon>
                                            删除文章
                                        </el-dropdown-item>
                                    </el-dropdown-menu>
                                </template>
                            </el-dropdown>
                        </div>
                    </template>
                </el-table-column>
            </el-table>

            <!-- 分页 -->
            <div class="mt-10 flex justify-center">
                <el-pagination v-model:current-page="current" v-model:page-size="size" :page-sizes="[10, 20, 50]"
                    :small="false" :background="true" layout="total, sizes, prev, pager, next, jumper" :total="total"
                    @size-change="handleSizeChange" @current-change="getTableData" />
            </div>

        </el-card>

<!-- 写博客 -->
        <el-dialog v-model="isArticlePublishEditorShow" class="article-editor-dialog" :fullscreen="true" :show-close="false"
            :close-on-press-escape="false">
            <template #header="{ close, titleId, titleClass }">
                <div class="article-editor-header">
                    <h4 class="font-bold">写文章</h4>
                    <div class="ml-auto flex">
                        <el-button class="ai-btn" @click="showAiAssistant('publish', buildAiPrompt('draft'))">
                            <el-icon class="mr-1"><MagicStick /></el-icon>
                            AI 写作
                        </el-button>
                        <el-button @click="isArticlePublishEditorShow = false">取消</el-button>
                        <el-button type="primary" @click="publishArticleSubmit">
                            <el-icon class="mr-1">
                                <Promotion />
                            </el-icon>
                            发布
                        </el-button>
                    </div>
                </div>
            </template>
            <el-form :model="form" ref="publishArticleFormRef" label-position="top" size="large" :rules="rules"
                class="article-editor-form">
                <div class="article-editor-grid">
                    <section class="article-editor-main">
                        <el-form-item label="标题" prop="title" class="article-title-item">
                            <el-input v-model="form.title" autocomplete="off" size="large" maxlength="40" show-word-limit
                                clearable placeholder="输入一个清晰、有搜索价值的文章标题" />
                        </el-form-item>
                        <el-form-item label="内容" prop="content" class="article-content-item">
                            <div class="article-ai-toolbar">
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('publish', buildAiPrompt('continue'))">
                                    <el-icon><MagicStick /></el-icon>
                                    续写
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('publish', buildAiPrompt('outline'))">
                                    <el-icon><MagicStick /></el-icon>
                                    大纲
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('publish', buildAiPrompt('title'))">
                                    <el-icon><MagicStick /></el-icon>
                                    标题
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('publish', buildAiPrompt('polish'))">
                                    <el-icon><MagicStick /></el-icon>
                                    润色
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('publish', buildAiPrompt('summary'))">
                                    <el-icon><MagicStick /></el-icon>
                                    摘要
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('publish', buildAiPrompt('seo'))">
                                    <el-icon><MagicStick /></el-icon>
                                    SEO
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('publish', buildAiPrompt('keywords'))">
                                    <el-icon><MagicStick /></el-icon>
                                    关键词
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('publish', buildAiPrompt('review'))">
                                    <el-icon><MagicStick /></el-icon>
                                    检查
                                </el-button>
                            </div>
                            <MdEditor v-model="form.content" :theme="editorTheme" @onUploadImg="onUploadImg" editorId="publishArticleEditor" :no-upload-img="true" />
                        </el-form-item>
                    </section>

                    <aside class="article-meta-panel">
                        <div class="meta-panel-title">发布设置</div>
                        <el-form-item label="封面" prop="cover">
                            <el-upload class="avatar-uploader" action="#" :on-change="handleCoverChange" :auto-upload="false"
                                :show-file-list="false">
                                <img v-if="form.cover" :src="form.cover" class="avatar" />
                                <el-icon v-else class="avatar-uploader-icon">
                                    <Plus />
                                </el-icon>
                            </el-upload>
                        </el-form-item>
                        <el-form-item label="摘要" prop="summary">
                            <el-input v-model="form.summary" :rows="4" type="textarea" placeholder="用于首页卡片和 SEO 描述" />
                        </el-form-item>
                        <el-form-item label="分类" prop="categoryId">
                            <el-select v-model="form.categoryId" clearable placeholder="请选择分类" size="large">
                                <el-option v-for="item in categories" :key="item.id" :label="item.name" :value="item.id" />
                            </el-select>
                        </el-form-item>
                        <el-form-item label="标签" prop="tags">
                            <el-select v-model="form.tags" multiple filterable remote reserve-keyword placeholder="输入或选择标签"
                                remote-show-suffix allow-create default-first-option :remote-method="remoteMethod"
                                :loading="tagSelectLoading" size="large">
                                <el-option v-for="item in tags" :key="item.id" :label="item.name" :value="item.id" />
                            </el-select>
                        </el-form-item>
                        <div class="editor-stats">
                            <span>正文 {{ countPlainText(form.content) }} 字</span>
                            <span>摘要 {{ countPlainText(form.summary) }} 字</span>
                        </div>
                    </aside>
                </div>
            </el-form>
        </el-dialog>

<!-- 编辑博客 -->
        <el-dialog v-model="isArticleUpdateEditorShow" class="article-editor-dialog" :fullscreen="true" :show-close="false"
            :close-on-press-escape="false">
            <template #header="{ close, titleId, titleClass }">
                <div class="article-editor-header">
                    <h4 class="font-bold">编辑文章</h4>
                    <div class="ml-auto flex">
                        <el-button class="ai-btn" @click="showAiAssistant('update', buildAiPrompt('draft'))">
                            <el-icon class="mr-1"><MagicStick /></el-icon>
                            AI 写作
                        </el-button>
                        <el-button @click="isArticleUpdateEditorShow = false">取消</el-button>
                        <el-button type="primary" @click="updateSubmit">
                            <el-icon class="mr-1">
                                <Promotion />
                            </el-icon>
                            保存
                        </el-button>
                    </div>
                </div>
            </template>
            <el-form :model="updateArticleForm" ref="updateArticleFormRef" label-position="top" size="large" :rules="rules"
                class="article-editor-form">
                <div class="article-editor-grid">
                    <section class="article-editor-main">
                        <el-form-item label="标题" prop="title" class="article-title-item">
                            <el-input v-model="updateArticleForm.title" autocomplete="off" size="large" maxlength="40"
                                show-word-limit clearable placeholder="输入一个清晰、有搜索价值的文章标题" />
                        </el-form-item>
                        <el-form-item label="内容" prop="content" class="article-content-item">
                            <div class="article-ai-toolbar">
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('update', buildAiPrompt('continue'))">
                                    <el-icon><MagicStick /></el-icon>
                                    续写
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('update', buildAiPrompt('outline'))">
                                    <el-icon><MagicStick /></el-icon>
                                    大纲
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('update', buildAiPrompt('title'))">
                                    <el-icon><MagicStick /></el-icon>
                                    标题
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('update', buildAiPrompt('polish'))">
                                    <el-icon><MagicStick /></el-icon>
                                    润色
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('update', buildAiPrompt('summary'))">
                                    <el-icon><MagicStick /></el-icon>
                                    摘要
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('update', buildAiPrompt('seo'))">
                                    <el-icon><MagicStick /></el-icon>
                                    SEO
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('update', buildAiPrompt('keywords'))">
                                    <el-icon><MagicStick /></el-icon>
                                    关键词
                                </el-button>
                                <el-button size="small" class="ai-btn" @click="showAiAssistant('update', buildAiPrompt('review'))">
                                    <el-icon><MagicStick /></el-icon>
                                    检查
                                </el-button>
                            </div>
                            <MdEditor v-model="updateArticleForm.content" :theme="editorTheme" @onUploadImg="onUploadImg"
                                editorId="updateArticleEditor" :no-upload-img="true" />
                        </el-form-item>
                    </section>

                    <aside class="article-meta-panel">
                        <div class="meta-panel-title">发布设置</div>
                        <el-form-item label="封面" prop="cover">
                            <el-upload class="avatar-uploader" action="#" :on-change="handleUpdateCoverChange" :auto-upload="false"
                                :show-file-list="false">
                                <img v-if="updateArticleForm.cover" :src="updateArticleForm.cover" class="avatar" />
                                <el-icon v-else class="avatar-uploader-icon">
                                    <Plus />
                                </el-icon>
                            </el-upload>
                        </el-form-item>
                        <el-form-item label="摘要" prop="summary">
                            <el-input v-model="updateArticleForm.summary" :rows="4" type="textarea" placeholder="用于首页卡片和 SEO 描述" />
                        </el-form-item>
                        <el-form-item label="分类" prop="categoryId">
                            <el-select v-model="updateArticleForm.categoryId" clearable placeholder="请选择分类" size="large">
                                <el-option v-for="item in categories" :key="item.id" :label="item.name" :value="item.id" />
                            </el-select>
                        </el-form-item>
                        <el-form-item label="标签" prop="tags">
                            <el-select v-model="updateArticleForm.tags" multiple filterable remote reserve-keyword
                                placeholder="输入或选择标签" remote-show-suffix allow-create default-first-option
                                :remote-method="remoteMethod" :loading="tagSelectLoading" size="large">
                                <el-option v-for="item in tags" :key="item.id" :label="item.name" :value="item.id" />
                            </el-select>
                        </el-form-item>
                        <div class="editor-stats">
                            <span>正文 {{ countPlainText(updateArticleForm.content) }} 字</span>
                            <span>摘要 {{ countPlainText(updateArticleForm.summary) }} 字</span>
                        </div>
                    </aside>
                </div>
            </el-form>
        </el-dialog>

        <!-- AI 助手弹窗 -->
        <AiAssistantDialog v-model="isAiAssistantShow" :initial-prompt="aiAssistantPreset" :source-content="getActiveArticleForm().content" @insert-content="handleAiInsertContent" />
    </div>
</template>

<script setup>
defineOptions({ name: 'AdminArticleList' })
import { computed, ref, reactive } from 'vue'
import { Search, RefreshRight, Check, Close, MagicStick, CircleCheck, CircleClose, Loading } from '@element-plus/icons-vue'
import { getArticlePageList, deleteArticle, publishArticle, getArticleDetail, updateArticle, updateArticleIsTop, updateArticleStatus } from '@/api/admin/article'
import { uploadFile } from '@/api/admin/file'
import { getCategorySelectList } from '@/api/admin/category'
import { searchTags, getTagSelectList } from '@/api/admin/tag'
import { getAiSummaryAdmin, generateAiSummaryApi } from '@/api/admin/aiSummary'
import moment from 'moment'
import { showMessage, showModel } from '@/composables/util'
import { getToken } from '@/composables/cookie'
import { MdEditor } from 'md-editor-v3'
import 'md-editor-v3/lib/style.css'
import { useRouter } from 'vue-router'
import AiAssistantDialog from '@/components/AiAssistantDialog.vue'
import { useThemeStore } from '@/stores/theme'

const router = useRouter()
const themeStore = useThemeStore()
const editorTheme = computed(() => themeStore.mode === 'dark' ? 'dark' : 'light')

// AI 助手弹窗
const isAiAssistantShow = ref(false)
const aiAssistantTarget = ref('')
const aiAssistantPreset = ref('')

const showAiAssistant = (target, preset = '') => {
    aiAssistantTarget.value = target
    aiAssistantPreset.value = preset
    isAiAssistantShow.value = true
}

const getActiveArticleForm = () => aiAssistantTarget.value === 'update' ? updateArticleForm : form

const countPlainText = (value) => {
    return (value || '')
        .replace(/```[\s\S]*?```/g, '')
        .replace(/`[^`]*`/g, '')
        .replace(/!\[[^\]]*]\([^)]*\)/g, '')
        .replace(/\[[^\]]*]\([^)]*\)/g, '')
        .replace(/[#>*_\-~|]/g, '')
        .replace(/\s/g, '')
        .length
}

const buildAiPrompt = (type) => {
    const current = isArticleUpdateEditorShow.value ? updateArticleForm : form
    const title = current.title || '未命名文章'
    const content = (current.content || '').trim()
    const summary = current.summary || ''

    const contentBlock = content ? `\n\n当前正文：\n${content.slice(0, 3500)}` : ''
    const summaryBlock = summary ? `\n\n当前摘要：${summary}` : ''

    const prompts = {
        outline: `请为这篇博客生成一份可直接使用的 Markdown 大纲。标题：${title}。要求包含 4-6 个一级/二级小节，每节给出写作要点和建议示例。${summaryBlock}${contentBlock}`,
        title: `请为下面文章生成 8 个适合技术博客的标题。要求标题清晰、有搜索价值、不夸张，每行一个标题，不要编号外的解释。当前标题：${title}${summaryBlock}${contentBlock}`,
        draft: `请帮我写一篇博客文章。标题：${title}。要求结构清晰、适合技术博客阅读，包含小标题、重点说明和必要的示例。${summaryBlock}${contentBlock}`,
        continue: `请基于下面已有正文继续写，保持原有语气和结构，不要重复已有内容。标题：${title}${contentBlock}`,
        polish: `请润色下面这篇博客文章，使表达更清晰、更有技术深度，保留 Markdown 结构。标题：${title}${contentBlock}`,
        summary: `请为下面文章生成 80-140 字摘要。只输出摘要正文，不要输出标题建议或额外解释。标题：${title}${contentBlock}`,
        seo: `请为下面文章生成 SEO 优化建议。标题：${title}。请输出：1. 5 个 SEO 标题；2. 8-12 个关键词；3. meta description；4. 可优化的小标题建议。${summaryBlock}${contentBlock}`,
        keywords: `请根据下面文章生成 8-12 个中文或英文 SEO 关键词。只输出关键词，用逗号分隔，不要解释。标题：${title}${summaryBlock}${contentBlock}`,
        review: `请审查下面这篇文章，重点检查结构完整性、技术准确性、表达清晰度、SEO、读者体验。请给出分点问题和可执行修改建议。标题：${title}${summaryBlock}${contentBlock}`
    }

    return prompts[type] || prompts.draft
}

// AI 摘要状态管理
const aiSummaryStatus = reactive({})

// 初始化 AI 摘要状态
const initAiSummaryStatus = (articles) => {
    articles.forEach(article => {
        if (!aiSummaryStatus[article.id]) {
            aiSummaryStatus[article.id] = { loading: false, hasSummary: null }
        }
    })
}

// 检查文章是否有 AI 摘要
const checkAiSummaryStatus = (articleId) => {
    if (aiSummaryStatus[articleId]?.hasSummary !== null) return
    getAiSummaryAdmin(articleId).then(res => {
        if (res.success && res.data && res.data.id) {
            aiSummaryStatus[articleId] = { loading: false, hasSummary: true }
        } else {
            aiSummaryStatus[articleId] = { loading: false, hasSummary: false }
        }
    }).catch(() => {
        aiSummaryStatus[articleId] = { loading: false, hasSummary: false }
    })
}

// 生成 AI 摘要
const generateAiSummary = async (row) => {
    if (aiSummaryStatus[row.id]?.loading) return
    
    aiSummaryStatus[row.id] = { loading: true, hasSummary: aiSummaryStatus[row.id]?.hasSummary || false }
    
    try {
        const token = getToken()
        if (!token) {
            showMessage('请先登录', 'error')
            aiSummaryStatus[row.id] = { loading: false, hasSummary: aiSummaryStatus[row.id]?.hasSummary || false }
            return
        }
        
        const response = await fetch(`/api/admin/ai-summary/generate/${row.id}`, {
            method: 'POST',
            headers: {
                'Authorization': 'Bearer ' + token,
                'Content-Type': 'application/json'
            }
        })
        
        if (!response.ok) {
            throw new Error('请求失败，状态码: ' + response.status)
        }
        
        const reader = response.body?.getReader()
        if (!reader) {
            throw new Error('获取读取器失败')
        }
        
        const decoder = new TextDecoder()
        let result = ''
        let hasError = false
        
        while (true) {
            const { done, value } = await reader.read()
            if (done) break
            
            const chunk = decoder.decode(value, { stream: true })
            const lines = chunk.split('\n')
            
            for (const line of lines) {
                if (line.startsWith('data: ')) {
                    try {
                        const data = JSON.parse(line.slice(6))
                        if (data.error) {
                            showMessage(data.error, 'error')
                            hasError = true
                            break
                        }
                        if (data.content) {
                            result += data.content
                        }
                        if (data.done) {
                            break
                        }
                    } catch (e) {
                        // Ignore parse errors for incomplete JSON
                    }
                }
            }
            if (hasError) break
        }
        
        if (!hasError && result) {
            aiSummaryStatus[row.id] = { loading: false, hasSummary: true }
            showMessage('AI 摘要生成成功', 'success')
        } else if (!hasError && !result) {
            aiSummaryStatus[row.id] = { loading: false, hasSummary: aiSummaryStatus[row.id]?.hasSummary || false }
        }
    } catch (error) {
        console.error('AI 摘要生成失败:', error)
        showMessage('生成失败: ' + error.message, 'error')
        aiSummaryStatus[row.id] = { loading: false, hasSummary: aiSummaryStatus[row.id]?.hasSummary || false }
    }
}

const normalizeAiPayload = (payload) => {
    if (typeof payload === 'string') {
        return { action: 'append', content: payload }
    }
    return {
        action: payload?.action || 'append',
        content: payload?.content || ''
    }
}

const cleanAiSummary = (content) => {
    return (content || '')
        .replace(/^#+\s*摘要[:：]?\s*/i, '')
        .replace(/^摘要[:：]\s*/i, '')
        .replace(/\*\*/g, '')
        .trim()
        .slice(0, 220)
}

const stripMarkdownLine = (line) => {
    return (line || '')
        .replace(/^#{1,6}\s*/, '')
        .replace(/^[-*]\s*/, '')
        .replace(/^\d+[.、)]\s*/, '')
        .replace(/^标题\s*[:：]\s*/i, '')
        .replace(/^SEO\s*标题\s*[:：]\s*/i, '')
        .replace(/\*\*/g, '')
        .replace(/["'“”]/g, '')
        .trim()
}

const extractAiTitle = (content) => {
    const lines = (content || '')
        .split(/\r?\n/)
        .map(stripMarkdownLine)
        .filter(Boolean)

    const candidate = lines.find(line => {
        if (/^(关键词|meta\s*description|摘要|说明|理由|建议)[:：]/i.test(line)) return false
        return line.length >= 4 && line.length <= 40
    })

    return candidate || ''
}

const extractMetaDescription = (content) => {
    const text = (content || '').replace(/\r/g, '')
    const match = text.match(/meta\s*description\s*[:：]\s*([^\n]+)/i)
        || text.match(/描述\s*[:：]\s*([^\n]+)/i)
        || text.match(/摘要\s*[:：]\s*([^\n]+)/i)

    return cleanAiSummary(match?.[1] || '')
}

const extractKeywords = (content) => {
    const text = (content || '').replace(/\r/g, '')
    const match = text.match(/关键词\s*[:：]\s*([^\n]+)/i)
        || text.match(/keywords\s*[:：]\s*([^\n]+)/i)

    const raw = match?.[1] || text
    return raw
        .split(/[，,、\n]/)
        .map(stripMarkdownLine)
        .filter(Boolean)
        .slice(0, 12)
}

const handleAiInsertContent = (payload) => {
    const { action, content } = normalizeAiPayload(payload)
    if (!content) return

    const target = getActiveArticleForm()
    if (action === 'replace') {
        target.content = content
        return
    }

    if (action === 'summary') {
        target.summary = cleanAiSummary(content)
        return
    }

    if (action === 'title') {
        const title = extractAiTitle(content)
        if (title) {
            target.title = title
            return
        }
    }

    if (action === 'seo') {
        const title = extractAiTitle(content)
        const description = extractMetaDescription(content)
        const keywords = extractKeywords(content)

        if (title) target.title = title
        if (description) target.summary = description
        if (keywords.length > 0) {
            const seoNote = `\n\n<!-- AI SEO 关键词：${keywords.join('，')} -->`
            target.content = target.content ? target.content + seoNote : seoNote.trim()
        }
        return
    }

    target.content = target.content ? target.content + '\n\n' + content : content
}

// 模糊搜索的文章标题
const searchArticleTitle = ref('')
// 日期
const pickDate = ref('')

// 查询条件：开始结束时间
const startDate = reactive({})
const endDate = reactive({})

// 监听日期组件改变事件，并将开始结束时间设置到变量中
const datepickerChange = (e) => {
    startDate.value = moment(e[0]).format('YYYY-MM-DD')
    endDate.value = moment(e[1]).format('YYYY-MM-DD')

}

const shortcuts = [
    {
        text: '最近一周',
        value: () => {
            const end = new Date()
            const start = new Date()
            start.setTime(start.getTime() - 3600 * 1000 * 24 * 7)
            return [start, end]
        },
    },
    {
        text: '最近一个月',
        value: () => {
            const end = new Date()
            const start = new Date()
            start.setTime(start.getTime() - 3600 * 1000 * 24 * 30)
            return [start, end]
        },
    },
    {
        text: '最近三个月',
        value: () => {
            const end = new Date()
            const start = new Date()
            start.setTime(start.getTime() - 3600 * 1000 * 24 * 90)
            return [start, end]
        },
    },
]

// 重置
const reset = () => {
    pickDate.value = ''
    startDate.value = null
    endDate.value = null
    searchArticleTitle.value = ''
}

// 表格加载 Loading
const tableLoading = ref(false)
// 表格数据
const tableData = ref([])
// 当前页码，给了一个默认值 1
const current = ref(1)
// 总数据量，给了个默认值 0
const total = ref(0)
// 每页显示的数据量，给了个默认值 10
const size = ref(10)


// 获取分页数据
function getTableData() {
    // 显示表格 loading
    tableLoading.value = true
    // 调用后台分页接口，并传入所需参数
    getArticlePageList({ pageNum: current.value, pageSize: size.value, startDate: startDate.value, endDate: endDate.value, title: searchArticleTitle.value }, true)
        .then((res) => {
            if (res.success == true) {
                tableData.value = res.data.list
                current.value = res.data.pageNum
                size.value = res.data.pageSize
                total.value = res.data.total
                // 初始化 AI 摘要状态
                initAiSummaryStatus(res.data.list)
                // 检查每篇文章的 AI 摘要状态
                res.data.list.forEach(article => {
                    checkAiSummaryStatus(article.id)
                })
            }
        })
        .finally(() => tableLoading.value = false) // 隐藏表格 loading
}
getTableData()

// 每页展示数量变更事件
const handleSizeChange = (chooseSize) => {
    size.value = chooseSize
    getTableData()
}

// 删除文章
const deleteArticleSubmit = (row) => {
    showModel('是否确定要删除该文章？').then(() => {
        deleteArticle(row.id).then((res) => {
            if (res.success == false) {
                // 获取服务端返回的错误消息
                let message = res.message
                // 提示错误消息
                showMessage(message, 'error')
                return
            }

            showMessage('删除成功')
            // 重新请求分页接口，渲染数据
            getTableData()
        })
    }).catch(() => {
    })
}

// 是否显示文章发布对话框
const isArticlePublishEditorShow = ref(false)
// 发布文章表单引用
const publishArticleFormRef = ref(null)

// 表单对象
const form = reactive({
    id: null,
    title: '',
    content: '请输入内容',
    cover: '',
    categoryId: null,
    tags: [],
    summary: "",
    isPublish: true
})

// 修改文章表单对象
const updateArticleForm = reactive({
    id: null,
    title: '',
    content: '请输入内容',
    cover: '',
    categoryId: null,
    tags: [],
    summary: "",
    isPublish: true
})

// 表单校验规则
const rules = {
    title: [
        { required: true, message: '请输入文章标题', trigger: 'blur' },
        { min: 1, max: 40, message: '文章标题要求大于1个字符，小于40个字符', trigger: 'blur' },
    ],
    content: [{ required: true }],
    cover: [{ required: true }],
    categoryId: [{ required: true, message: '请选择文章分类', trigger: 'blur' }],
    tags: [{ required: true, message: '请选择文章标签', trigger: 'blur' }],
}

// 上传文章封面图片
const handleCoverChange = (file) => {
    // 表单对象
    let formData = new FormData()
    // 添加 file 字段，并将文件传入 
    formData.append('file', file.raw)
    uploadFile(formData).then((e) => {
        // 响参失败，提示错误消息
        if (e.success == false) {
            let message = e.message
            showMessage(message, 'error')
            return
        }

        // 成功则设置表单对象中的封面链接，并提示上传成功
        form.cover = e.data
        showMessage('上传成功')
    })
}

// 编辑文章：上传文章封面图片
const handleUpdateCoverChange = (file) => {
    // 表单对象
    let formData = new FormData()
    // 添加 file 字段，并将文件传入 
    formData.append('file', file.raw)
    uploadFile(formData).then((e) => {
        // 响参失败，提示错误消息
        if (e.success == false) {
            let message = e.message
            showMessage(message, 'error')
            return
        }

        // 成功则设置表单对象中的封面链接，并提示上传成功
        updateArticleForm.cover = e.data
        showMessage('上传成功')
    })
}

// 编辑器图片上传
const onUploadImg = async (files, callback) => {
    const resList = []
    for (const file of files) {
        let formData = new FormData()
        formData.append("file", file)
        try {
            const res = await uploadFile(formData)
            if (res.success && res.data) {
                resList.push(res.data)
            } else {
                showMessage('图片上传失败: ' + (res.message || '未知错误'), 'error')
            }
        } catch (err) {
            showMessage('图片上传失败: ' + err.message, 'error')
        }
    }
    if (resList.length > 0) {
        callback(resList)
    }
}

// 文章分类
const categories = ref([])
getCategorySelectList().then((e) => {
    categories.value = e.data
})

// 标签 select Loading 状态，默认不显示
const tagSelectLoading = ref(false)
// 文章标签
const tags = ref([])
// 渲染标签数据
getTagSelectList().then(res => {
    tags.value = res.data
})


// 根据用户输入的标签名称，远程模糊查询
const remoteMethod = (query) => {
    // 如果用户的查询关键词不为空
    if (query) {
        // 显示 loading
        tagSelectLoading.value = true
        // 调用标签模糊查询接口
        searchTags(query).then((e) => {
            if (e.success) {
                // 设置到 tags 变量中
                tags.value = e.data
            }
        }).finally(() => tagSelectLoading.value = false) // 隐藏 loading
    }
}

// 发布文章
const publishArticleSubmit = () => {
    // 校验表单
    publishArticleFormRef.value.validate((valid) => {
        if (!valid) {
            return false
        }

        const payload = { ...form, status: form.isPublish ? 1 : 0 }
        publishArticle(payload).then((res) => {
            if (res.success == false) {
                // 获取服务端返回的错误消息
                let message = res.message
                // 提示错误消息
                showMessage(message, 'error')
                return
            }

            showMessage('发布成功')
            // 隐藏发布文章对话框
            isArticlePublishEditorShow.value = false
            // 将 form 表单字段置空
            form.title = ''
            form.content = ''
            form.cover = ''
            form.summary = ''
            form.categoryId = null
            form.tags = []
            form.isPublish = true
            // 重新请求分页接口，渲染列表数据
            getTableData()
        })
    })
}


// 是否显示编辑文章对话框
const isArticleUpdateEditorShow = ref(false)
// 编辑文章表单引用
const updateArticleFormRef = ref(null)
// 编辑文章按钮点击事件
const showArticleUpdateEditor = (row) => {
    // 显示编辑文章对话框
    isArticleUpdateEditorShow.value = true
    // 拿到文章 ID
    let articleId = row.id
    getArticleDetail(articleId).then((res) => {
        if (res.success) {
            // 设置表单数据
            updateArticleForm.id = res.data.id
            updateArticleForm.title = res.data.title
            updateArticleForm.cover = res.data.cover
            updateArticleForm.content = res.data.content
            updateArticleForm.categoryId = res.data.categoryId
            updateArticleForm.tags = res.data.tagIds
            updateArticleForm.summary = res.data.summary
            updateArticleForm.isPublish = res.data.status !== 0
        }
    })
}

// 保存文章
const updateSubmit = () => {
    updateArticleFormRef.value.validate((valid) => {
        // 校验表单
        if (!valid) {
            return false
        }

        // 请求更新文章接口
        const payload = { ...updateArticleForm, status: updateArticleForm.isPublish ? 1 : 0 }
        updateArticle(payload).then((res) => {
            if (res.success == false) {
                // 获取服务端返回的错误消息
                let message = res.message
                // 提示错误消息
                showMessage(message, 'error')
                return
            }

            showMessage('保存成功')
            // 隐藏编辑框
            isArticleUpdateEditorShow.value = false
            // 重新请求分页接口，渲染列表数据
            getTableData()
        })
    })
}


// 跳转文章详情页
const goArticleDetailPage = (articleId) => {
    router.push('/article/' + articleId)
}

// 点击置顶
const handleIsTopChange = (row) => {
    updateArticleIsTop({id: row.id, isTop: row.isTop}).then((res) => {
        // 重新请求分页接口，渲染列表数据
        getTableData()

        if (res.success == false) {
            // 获取服务端返回的错误消息
            let message = res.message
            // 提示错误消息
            showMessage(message, 'error')
            return
        }

        showMessage(row.isTop ? '置顶成功' : "已取消置顶")
    })
}

// 点击发布状态
const handleArticleStatusChange = (row, isPublished) => {
    const previousStatus = row.status
    row.status = isPublished ? 1 : 0

    updateArticleStatus({ id: row.id, status: row.status }).then((res) => {
        if (res.success == false) {
            row.status = previousStatus
            let message = res.message
            showMessage(message, 'error')
            return
        }

        showMessage(isPublished ? '已发布到前台' : '已转为草稿')
        getTableData()
    }).catch(() => {
        row.status = previousStatus
        showMessage('更新发布状态失败', 'error')
    })
}
</script>

<style scoped>
/* 封面图片样式 */
.avatar-uploader .avatar {
    width: 200px;
    height: 100px;
    display: block;
}

.el-icon.avatar-uploader-icon {
    font-size: 28px;
    color: #8c939d;
    width: 200px;
    height: 100px;
    text-align: center;
}

/* 指定 select 下拉框宽度 */
/* AI 助手按钮样式 */
.ai-btn {
    background: linear-gradient(135deg, #3b82f6 0%, #06b6d4 100%);
    border: none;
    color: #fff;
    box-shadow: 0 10px 22px rgba(37, 99, 235, 0.18);
}

.ai-btn:hover {
    background: linear-gradient(135deg, #2563eb 0%, #0891b2 100%);
    color: #fff;
    box-shadow: 0 12px 26px rgba(37, 99, 235, 0.24);
}

.article-ai-toolbar {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    width: 100%;
    margin: 0 0 10px;
    padding: 10px;
    border: 1px solid rgba(125, 211, 252, 0.14);
    border-radius: 12px;
    background: linear-gradient(135deg, rgba(59, 130, 246, 0.12), rgba(14, 165, 233, 0.06));
}

.article-ai-toolbar .ai-btn {
    background: rgba(255, 255, 255, 0.72);
    border: 1px solid rgba(59, 130, 246, 0.18);
    color: #2563eb;
    box-shadow: none;
}

.article-ai-toolbar .ai-btn:hover {
    background: rgba(239, 246, 255, 0.96);
    border-color: rgba(14, 165, 233, 0.35);
    color: #0891b2;
    box-shadow: 0 8px 18px rgba(37, 99, 235, 0.10);
}

.table-action-group {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    white-space: nowrap;
}

.table-action-group .el-button + .el-button {
    margin-left: 0;
}

.more-action-btn {
    padding: 5px 10px;
}

.danger-dropdown-item {
    color: #ef4444;
}

.article-title-cell {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
    min-width: 0;
}

.article-title-text {
    max-width: 100%;
    overflow: hidden;
    color: var(--admin-text);
    font-weight: 600;
    line-height: 1.45;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.article-summary-chip {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    height: 24px;
    padding: 0 9px;
    border-radius: 999px;
    font-size: 12px;
    font-weight: 600;
}

.article-summary-chip.is-ready {
    color: #16a34a;
    background: rgba(34, 197, 94, 0.10);
    border: 1px solid rgba(34, 197, 94, 0.26);
}

.article-summary-chip.is-empty {
    color: #64748b;
    background: rgba(148, 163, 184, 0.10);
    border: 1px solid rgba(148, 163, 184, 0.24);
}

.article-editor-form {
    width: 100%;
}

.article-editor-grid {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 340px;
    gap: 22px;
    align-items: start;
}

.article-editor-main,
.article-meta-panel {
    min-width: 0;
}

.article-meta-panel {
    position: sticky;
    top: 82px;
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 18px;
    border: 1px solid var(--admin-border);
    border-radius: 18px;
    background: color-mix(in srgb, var(--admin-bg-card) 92%, transparent);
    box-shadow: var(--admin-shadow-soft);
}

.meta-panel-title {
    margin-bottom: 8px;
    color: var(--admin-text);
    font-size: 16px;
    font-weight: 800;
}

.article-meta-panel .el-select,
.article-meta-panel .el-input,
.article-meta-panel .el-textarea {
    width: 100%;
}

.publish-state-card,
.editor-stats {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 12px 14px;
    border: 1px solid var(--admin-border);
    border-radius: 14px;
    background: var(--admin-bg-soft);
}

.publish-state-title {
    color: var(--admin-text);
    font-size: 14px;
    font-weight: 700;
}

.publish-state-desc {
    margin-top: 3px;
    color: var(--admin-text-muted);
    font-size: 12px;
}

.editor-stats {
    margin-top: 10px;
    color: var(--admin-text-muted);
    font-size: 12px;
}

@media (max-width: 1180px) {
    .article-editor-grid {
        grid-template-columns: 1fr;
    }

    .article-meta-panel {
        position: static;
    }
}
</style>

<style>
.article-editor-dialog.el-dialog.is-fullscreen {
    background:
        radial-gradient(circle at 18% 0%, rgba(34, 211, 238, 0.08), transparent 30%),
        var(--admin-bg-page) !important;
}

.article-editor-dialog .el-dialog__header {
    position: sticky;
    top: 0;
    z-index: 50;
    padding: 0 24px !important;
    background: color-mix(in srgb, var(--admin-bg-card) 92%, transparent) !important;
    border-bottom: 1px solid var(--admin-border) !important;
    backdrop-filter: blur(18px);
    -webkit-backdrop-filter: blur(18px);
}

.article-editor-dialog .el-dialog__body {
    padding: 22px 24px 32px !important;
    color: var(--admin-text);
}

.article-editor-dialog .article-content-item .el-form-item__content {
    display: block;
}

.article-editor-dialog .el-affix--fixed {
    position: static !important;
    width: 100% !important;
    transform: none !important;
}

.article-editor-header {
    display: flex;
    align-items: center;
    height: 58px;
    width: 100%;
    gap: 16px;
    color: var(--admin-text);
    background: transparent;
}

.article-editor-header h4 {
    margin: 0;
    color: var(--admin-text);
    font-size: 20px;
    font-weight: 800;
}

.article-editor-header .ml-auto {
    gap: 12px;
}

.article-editor-dialog .el-form-item {
    margin-bottom: 24px;
}

.article-editor-dialog .el-form-item.is-error {
    margin-bottom: 36px;
}

.article-editor-dialog .el-form-item__error {
    padding-top: 6px;
    line-height: 1.35;
}

.article-editor-dialog .el-form-item__label {
    color: var(--admin-text) !important;
    font-weight: 700;
}

.article-editor-dialog .md-editor {
    overflow: hidden;
    border: 1px solid var(--admin-border);
    border-radius: 14px;
    background: var(--admin-bg-card);
    box-shadow: var(--admin-shadow-soft);
    height: calc(100vh - 252px);
    min-height: 560px;
}

.article-editor-dialog .md-editor-light {
    --md-bk-color: #ffffff;
    --md-bk-color-outstand: #f8fafc;
    --md-border-color: #e2e8f0;
    --md-scrollbar-bg-color: rgba(148, 163, 184, 0.16);
    --md-scrollbar-thumb-color: rgba(100, 116, 139, 0.34);
    --md-theme-color: var(--admin-accent);
    color: var(--admin-text);
}

.article-editor-dialog .md-editor-dark {
    --md-bk-color: var(--admin-bg-card);
    --md-bk-color-outstand: var(--admin-bg-soft);
    --md-border-color: var(--admin-border);
    --md-scrollbar-bg-color: rgba(148, 163, 184, 0.18);
    --md-scrollbar-thumb-color: rgba(148, 163, 184, 0.36);
    --md-theme-color: var(--admin-accent);
    color: var(--admin-text);
}

.article-editor-dialog .md-editor-toolbar-wrapper,
.article-editor-dialog .md-editor-footer {
    background: var(--admin-bg-soft) !important;
    border-color: var(--admin-border) !important;
    color: var(--admin-text-muted) !important;
}

.article-editor-dialog .md-editor-input-wrapper,
.article-editor-dialog .md-editor-preview-wrapper {
    background: var(--admin-bg-card) !important;
}

.article-editor-dialog .md-editor-input,
.article-editor-dialog .md-editor-preview {
    color: var(--admin-text) !important;
}

.article-editor-dialog .md-editor-toolbar-wrapper svg,
.article-editor-dialog .md-editor-footer,
.article-editor-dialog .md-editor-footer label {
    color: var(--admin-text-muted) !important;
}

html:not(.dark) .article-editor-dialog.el-dialog.is-fullscreen {
    background:
        radial-gradient(circle at 18% 0%, rgba(14, 165, 233, 0.10), transparent 30%),
        linear-gradient(180deg, #f8fbff 0%, #f4f7fb 100%) !important;
}

html:not(.dark) .article-editor-dialog .el-dialog__header {
    background: rgba(255, 255, 255, 0.92) !important;
    border-bottom-color: #e2e8f0 !important;
}

html:not(.dark) .article-editor-dialog .article-ai-toolbar {
    background: linear-gradient(135deg, rgba(79, 70, 229, 0.08), rgba(6, 182, 212, 0.08)) !important;
    border-color: #dbeafe !important;
}

html.dark .article-editor-dialog .article-ai-toolbar .ai-btn {
    background: rgba(15, 23, 42, 0.76);
    border: 1px solid rgba(125, 211, 252, 0.22);
    color: #bae6fd;
}

html.dark .article-editor-dialog .article-ai-toolbar .ai-btn:hover {
    background: rgba(14, 165, 233, 0.15);
    border-color: rgba(125, 211, 252, 0.38);
    color: #e0f2fe;
}

html:not(.dark) .article-editor-dialog .md-editor {
    background: #ffffff !important;
    border-color: #dbe4ef !important;
    box-shadow: 0 16px 38px rgba(15, 23, 42, 0.08) !important;
}

html:not(.dark) .article-editor-dialog .md-editor-toolbar-wrapper,
html:not(.dark) .article-editor-dialog .md-editor-footer {
    background: #f8fafc !important;
    border-color: #e2e8f0 !important;
    color: #64748b !important;
}

html:not(.dark) .article-editor-dialog .md-editor-input-wrapper,
html:not(.dark) .article-editor-dialog .md-editor-preview-wrapper {
    background: #ffffff !important;
}

html:not(.dark) .article-editor-dialog .md-editor-input,
html:not(.dark) .article-editor-dialog .md-editor-preview {
    color: #172033 !important;
}

html.dark .article-editor-dialog .md-editor-toolbar-wrapper,
html.dark .article-editor-dialog .md-editor-footer {
    background: rgba(8, 13, 24, 0.88) !important;
    border-color: rgba(71, 85, 105, 0.34) !important;
}

.md-editor-footer {
    height: 40px;
}

/* 表格悬停效果 */
.admin-table {
    --el-table-row-hover-bg-color: var(--admin-bg-hover);
    transition: all 0.2s ease;
}

.admin-table ::v-deep(.el-table__row) {
    transition: all 0.2s ease;
}

.admin-table ::v-deep(.el-table__row:hover) {
    transform: translateX(2px);
}

.admin-table ::v-deep(.el-table__row:hover > td) {
    background: var(--el-table-row-hover-bg-color) !important;
}

.admin-table ::v-deep(.el-table__cell) {
    transition: all 0.2s ease;
}
</style>

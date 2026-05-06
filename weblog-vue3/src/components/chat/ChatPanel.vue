<template>
    <div class="flex h-full chat-panel overflow-hidden">
        <!-- 侧边栏 -->
        <transition name="slide">
            <ChatSidebar
                v-if="showSidebar"
                :sections="sidebarSections"
                :grouped-sessions="groupedSidebarSessions"
                :current-session-id="currentSessionId"
                :more-items="statusMoreActions"
                v-model:search-query="sidebarSearchQuery"
                @close="showSidebar = false"
                @new-chat="createNewChat"
                @open-settings="showSettings = true"
                @select-session="switchSession"
                @pin-session="pinSession"
                @rename-session="renameSession"
                @delete-session="deleteSession"
                @share-session="shareCurrentSession"
                @sidebar-more-action="handleStatusMoreAction"
            />
        </transition>
        <button
            v-if="showSidebar"
            type="button"
            class="chat-sidebar-backdrop"
            aria-label="关闭侧边栏"
            @click="showSidebar = false"
        />

        <!-- 主内容区 -->
        <div class="chat-main flex-1 flex flex-col min-w-0 relative">
            <ChatStatusBar
                :title="currentSessionTitle || '新会话'"
                :subtitle="currentSessionSubtitle"
                :chips="topBarState.primary"
                :sidebar-visible="showSidebar"
                @toggle-sidebar="showSidebar = !showSidebar"
            />

            <!-- 消息区域 -->
            <div class="flex-1 overflow-y-auto px-4 sm:px-6 lg:px-8 pb-4 chat-messages-scroll relative" ref="chatContainer" @scroll="handleScroll">
                <div class="chat-content-shell mx-auto w-full pt-6 sm:pt-8">
                    <!-- 欢迎消息 -->
                    <div v-if="displayMessages.length === 0" class="chat-empty-state">
                        <div class="chat-empty-badge">AI 工作台</div>
                        <h2 class="chat-empty-title">今天想一起做点什么？</h2>
                        <p class="chat-empty-description">{{ welcomeDescription }}</p>

                        <div class="chat-empty-prompt-grid">
                            <button
                                v-for="prompt in quickPrompts"
                                :key="prompt"
                                @click="usePrompt(prompt)"
                                class="quick-prompt-card"
                            >
                                <span class="chat-empty-prompt-text">{{ prompt }}</span>
                                <svg class="chat-empty-prompt-icon" fill="none" viewBox="0 0 24 24">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4.5 12h15m0 0-6.75-6.75M19.5 12l-6.75 6.75"/>
                                </svg>
                            </button>
                        </div>
                    </div>

                    <!-- 聊天记录 -->
                    <template v-else>
                        <template v-for="(chat, index) in displayMessages" :key="index">
                            <!-- 用户消息 -->
                            <div v-if="chat.role === 'user'" class="flex justify-end mb-7 gap-3">
                                <div class="max-w-[85%]">
                                    <div class="user-bubble text-white px-5 py-3 shadow-sm">
                                        <div v-if="chat.quotedArticleTitle" class="mb-2 pb-2 border-b border-white/20">
                                            <div class="flex items-center gap-1 text-xs text-white/75">
                                                <el-icon><Document /></el-icon>
                                                <span>引用文章: {{ chat.quotedArticleTitle }}</span>
                                            </div>
                                        </div>
                                        <p class="text-sm leading-relaxed whitespace-pre-wrap">{{ chat.content }}</p>
                                    </div>
                                    <p class="text-xs text-[var(--text-muted)] mt-1.5 text-right">{{ formatTime(chat.timestamp) }}</p>
                                </div>
                                <div class="user-avatar flex-shrink-0 mt-0.5">你</div>
                            </div>

                            <!-- AI 回复 -->
                            <div v-else class="chat-message chat-message-assistant group">
                                <div class="assistant-avatar assistant-avatar-sm flex items-center justify-center flex-shrink-0 mt-1 shadow-sm">
                                    <span class="text-white text-xs font-bold">J</span>
                                </div>
                                <div class="chat-ai-entry min-w-0 flex-1">
                                    <div class="chat-ai-head">
                                        <div class="chat-ai-headline">
                                            <span class="chat-ai-name">小J 助手</span>
                                            <span class="chat-ai-time">{{ formatTime(chat.timestamp) }}</span>
                                        </div>
                                    </div>

                                    <div class="chat-ai-bubble">
                                        <!-- 流式加载中 -->
                                        <div v-if="!chat.content && index === displayMessages.length - 1 && isStreaming" class="chat-ai-streaming">
                                            <span class="typing-dot" style="animation-delay:0s"></span>
                                            <span class="typing-dot" style="animation-delay:0.15s"></span>
                                            <span class="typing-dot" style="animation-delay:0.3s"></span>
                                        </div>
                                        <StreamMarkdownRender v-else :content="chat.content" />

                                        <div v-if="shouldShowRagStatus(chat)" class="rag-status mt-3">
                                            <span class="rag-dot"></span>
                                            <span>{{ chat.ragStatus }}</span>
                                        </div>

                                        <div v-if="visibleRagSteps(chat).length" class="rag-step-list mt-3">
                                            <div
                                                v-for="step in visibleRagSteps(chat)"
                                                :key="step.key"
                                                class="rag-step-item"
                                                :class="`rag-step-${step.status || 'pending'}`"
                                            >
                                                <span class="rag-step-dot"></span>
                                                <span class="rag-step-label">{{ step.message }}</span>
                                            </div>
                                        </div>

                                        <button
                                            v-if="canUseNormalFallback(chat)"
                                            type="button"
                                            class="rag-fallback-btn mt-3"
                                            @click="answerWithoutKnowledgeBase(index)"
                                        >
                                            用普通 AI 回答
                                        </button>

                                        <div v-if="isConservativeRagAnswer(chat)" class="rag-warning mt-3">
                                            {{ conservativeAnswerTip(chat) }}
                                        </div>

                                        <div v-if="chat.sources && chat.sources.length" class="rag-source-panel mt-4">
                                            <button class="rag-source-title" @click="toggleSourcePanel(chat)" type="button">
                                                <span>引用来源</span>
                                                <span class="rag-source-toggle">
                                                    {{ chat.sources.length }} 条
                                                    <svg class="w-3.5 h-3.5 transition-transform" :class="{ 'rotate-180': chat.sourcesExpanded }" fill="none" viewBox="0 0 24 24">
                                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m6 9 6 6 6-6"/>
                                                    </svg>
                                                </span>
                                            </button>
                                            <div v-if="chat.sourcesExpanded" class="rag-source-list">
                                                <div v-for="source in normalizeSources(chat.sources)" :key="source.chunkId || source.index" class="rag-source-item">
                                                    <div class="rag-source-head">
                                                        <span class="rag-source-index">[{{ source.index }}]</span>
                                                        <a
                                                            v-if="source.url"
                                                            class="rag-source-name rag-source-link"
                                                            :href="source.url"
                                                            target="_blank"
                                                            rel="noopener noreferrer"
                                                        >
                                                            {{ source.title || '联网来源' }}
                                                        </a>
                                                        <span v-else class="rag-source-name">{{ source.title || '未知文档' }}</span>
                                                        <span v-if="source.score !== undefined" class="rag-source-score">{{ formatSourceBadge(source) }}</span>
                                                    </div>
                                                    <p>{{ source.content }}</p>
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    <div v-if="chat.content && !isStreaming && suggestedFollowUps(chat).length" class="ai-follow-up-list">
                                        <button
                                            v-for="item in suggestedFollowUps(chat)"
                                            :key="item"
                                            type="button"
                                            @click="useFollowUp(item, chat)"
                                        >
                                            {{ item }}
                                        </button>
                                    </div>

                                    <div v-if="chat.content && !isStreaming" class="chat-ai-actions">
                                        <button
                                            @click="copyMessage(chat.content)"
                                            class="chat-ai-action-btn"
                                            title="复制回复"
                                        >
                                            <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16h8M8 12h8m-7 8h6a2 2 0 0 0 2-2V7.828a2 2 0 0 0-.586-1.414l-2.828-2.828A2 2 0 0 0 12.172 3H9a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z" />
                                            </svg>
                                            复制
                                        </button>
                                        <button
                                            v-if="index === displayMessages.length - 1"
                                            @click="regenerateResponse(index)"
                                            class="chat-ai-action-btn"
                                            title="重新生成"
                                        >
                                            <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                                            </svg>
                                            重试
                                        </button>
                                    </div>
                                </div>
                            </div>
                        </template>
                    </template>
                </div>
            </div>

            <!-- 回到最新消息按钮 -->
            <Transition name="scroll-btn">
                <button
                    v-if="showScrollBtn"
                    @click="scrollToBottom(true)"
                    class="chat-scroll-latest-btn absolute bottom-28 left-1/2 -translate-x-1/2 z-20 flex items-center gap-1.5 px-3 py-2 text-xs font-medium text-white bg-[var(--color-primary)] rounded-full shadow-lg hover:opacity-90 active:scale-95 transition-all duration-200"
                    title="回到最新消息"
                >
                    <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"/>
                    </svg>
                    最新消息
                </button>
            </Transition>

            <!-- 输入区域 -->
            <div class="chat-composer px-4 sm:px-6 lg:px-8 pb-5 pt-2.5">
                <div class="chat-content-shell chat-composer-content mx-auto w-full">
                    <!-- 使用次数提示 -->
                    <div
                        v-if="usageInfo && !usageInfo.isAdmin && !usageInfo.IsAdmin"
                        class="chat-usage-hint mb-3 text-xs text-center"
                        :class="(usageInfo.remaining === 0 || usageInfo.Remaining === 0) ? 'text-red-500' : 'text-[var(--text-muted)]'"
                    >
                        {{ usageInfo.message || usageInfo.Message }}
                    </div>

                    <!-- 引用文章提示 -->
                    <div v-if="quotedArticle" class="mb-3 p-2.5 bg-[var(--bg-hover)] rounded-2xl border border-[var(--border-base)]">
                        <div class="flex items-center justify-between">
                            <div class="flex items-center gap-2">
                                <div class="w-7 h-7 bg-blue-100 dark:bg-blue-900/30 rounded-full flex items-center justify-center flex-shrink-0">
                                    <el-icon class="text-[var(--color-primary)]"><Document /></el-icon>
                                </div>
                                <span class="text-sm text-[var(--text-body)] truncate max-w-[280px] font-medium">{{ quotedArticle.title }}</span>
                            </div>
                            <button @click="quotedArticle = null" class="p-1 rounded-full text-[var(--text-muted)] hover:text-[var(--text-body)] hover:bg-[var(--bg-active)] transition-colors">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18 18 6M6 6l12 12"/>
                                </svg>
                            </button>
                        </div>
                    </div>

                    <div class="chat-composer-shell">
                        <div class="chat-composer-toolbar">
                            <button
                                type="button"
                                class="chat-composer-tool chat-composer-tool-primary"
                                @click="showSettings = true"
                            >
                                <span class="chat-composer-tool-label">模型</span>
                                <span class="truncate">{{ currentModelName }}</span>
                            </button>

                            <div class="chat-mode-pill-group">
                                <button
                                    v-for="mode in modeOptions"
                                    :key="mode.key"
                                    type="button"
                                    class="chat-mode-pill"
                                    :class="{ 'chat-mode-pill-active': selectedChatMode === mode.key }"
                                    @click="setChatMode(mode.key)"
                                >
                                    {{ mode.label }}
                                </button>
                            </div>

                            <label
                                v-if="kbList.length"
                                class="chat-kb-select"
                                :class="{ 'is-disabled': selectedChatMode !== 'rag' && selectedChatMode !== 'auto' }"
                            >
                                <span class="chat-composer-tool-label">知识库</span>
                                <select
                                    v-model="selectedKbId"
                                    :disabled="selectedChatMode !== 'rag' && selectedChatMode !== 'auto'"
                                    @change="handleKbChange"
                                >
                                    <option value="">不使用知识库</option>
                                    <option v-for="kb in kbList" :key="kb.id" :value="kb.id">{{ kb.name }}</option>
                                </select>
                            </label>
                        </div>

                        <!-- 输入框 -->
                        <div class="chat-input-wrapper">
                        <textarea
                            ref="textareaRef"
                            v-model="inputText"
                            @input="autoResize"
                            @keydown.enter.exact.prevent="sendMessage"
                            @keydown.shift.enter.exact="handleShiftEnter"
                            placeholder="询问任何问题..."
                            class="chat-input-textarea"
                            rows="1"
                            maxlength="2000"
                        ></textarea>
                        </div>

                        <div class="chat-composer-footer">
                            <div class="chat-composer-meta">
                                <button
                                    @click="showArticleDialog = true"
                                    class="chat-composer-icon-btn"
                                    title="引用文章"
                                >
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                                        d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"/>
                                </svg>
                                </button>
                                <span v-if="selectedKbOption" class="chat-composer-state-pill" title="将优先检索知识库后回答">
                                    知识库 {{ selectedKbOption.name }}
                                </span>
                                <span v-if="selectedChatMode === 'web'" class="chat-composer-state-pill">联网检索</span>
                                <span v-if="isStreaming" class="chat-composer-state-pill">正在生成</span>
                            </div>

                            <div class="chat-composer-actions">
                                <span class="chat-composer-counter">{{ inputText.length }}/2000</span>
                                <span
                                    v-if="inputTokenCount > 0"
                                    class="chat-composer-token-pill"
                                    :class="{ 'chat-composer-token-pill-danger': inputTokenCount > 3000 }"
                                    title="预估 Token 消耗"
                                >
                                    ≈{{ inputTokenCount }}
                                </span>
                                <button
                                    v-if="!isStreaming"
                                    :disabled="!inputText.trim()"
                                    @click="sendMessage"
                                    class="chat-send-btn"
                                    :class="inputText.trim() ? 'bg-[var(--color-primary)] text-white shadow-sm' : 'bg-[var(--bg-hover)] text-[var(--text-placeholder)] cursor-not-allowed'"
                                >
                                    <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 12 3.269 3.125A59.769 59.769 0 0 1 21.485 12 59.768 59.768 0 0 1 3.27 20.875L5.999 12Zm0 0h7.5"/>
                                    </svg>
                                </button>
                                <button
                                    v-else
                                    @click="stopStreaming"
                                    class="chat-send-btn bg-red-500 text-white shadow-sm"
                                    title="停止生成"
                                >
                                    <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                                        <rect x="6" y="6" width="12" height="12" rx="2"/>
                                    </svg>
                                </button>
                            </div>
                        </div>
                    </div>

                    <div class="text-center text-xs text-[var(--text-placeholder)] mt-2.5">内容由 AI 生成，请仔细甄别</div>
                </div>
            </div>
        </div>
    </div>

    <!-- 模型设置弹窗 -->
    <ModelSettings v-model="showSettings" :modelOptions="modelOptions" @change="handleSettingsChange" />

    <!-- 引用文章弹窗 -->
    <el-dialog v-model="showArticleDialog" title="引用文章" width="600px">
        <div class="mb-4">
            <el-input v-model="articleSearch" placeholder="搜索文章标题..." clearable>
                <template #prefix>
                    <el-icon><Search /></el-icon>
                </template>
            </el-input>
        </div>
        <div class="max-h-80 overflow-y-auto">
            <div
                v-for="article in filteredArticles"
                :key="article.id || article.Id"
                @click="selectArticle(article)"
                class="p-3 mb-2 rounded-2xl border border-[var(--border-base)] cursor-pointer hover:bg-[var(--bg-hover)] transition-colors"
            >
                <h4 class="font-medium text-[var(--text-heading)] truncate">{{ article.title || article.Title }}</h4>
                <p v-if="article.summary || article.Summary" class="text-sm text-[var(--text-secondary)] mt-1 line-clamp-2">
                    {{ article.summary || article.Summary }}
                </p>
            </div>
            <div v-if="filteredArticles.length === 0" class="text-center text-[var(--text-muted)] py-8">
                暂无文章
            </div>
        </div>
    </el-dialog>
</template>

<script setup>
import { ref, computed, nextTick, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Document, Search } from '@element-plus/icons-vue'
import axios from '@/axios'
import { getToken } from '@/composables/cookie'
import { getArticleDetail } from '@/api/frontend/article'
import { saveSession, getUserSessions, deleteUserSession, getUsageInfo, getPublicKbList } from '@/api/frontend/chat'
import ChatSidebar from './ChatSidebar.vue'
import ChatStatusBar from './ChatStatusBar.vue'
import StreamMarkdownRender from './StreamMarkdownRender.vue'
import ModelSettings from './ModelSettings.vue'
import {
    buildSidebarSections,
    buildTopBarState,
    groupSessionsForSidebar
} from './chatWorkbenchViewModel.js'
import { ElMessage, ElMessageBox } from 'element-plus'

defineOptions({ name: 'ChatPanel' })

const route = useRoute()
const router = useRouter()

// ──────── 匿名用户 clientId（持久化到 localStorage）────────
function getOrCreateClientId() {
    let id = localStorage.getItem('ai_client_id')
    if (!id) {
        id = 'client_' + Date.now() + '_' + Math.random().toString(36).slice(2, 9)
        localStorage.setItem('ai_client_id', id)
    }
    return id
}
const clientId = getOrCreateClientId()

// ──────── 状态 ────────
const inputText = ref('')
const textareaRef = ref(null)
const chatContainer = ref(null)
const isStreaming = ref(false)
const showScrollBtn = ref(false)
const autoFollowScroll = ref(true)
const showSettings = ref(false)
const showSidebar = ref(true)
const showArticleDialog = ref(false)
const articleSearch = ref('')
const sidebarSearchQuery = ref('')
const quotedArticle = ref(null)
const allArticles = ref([])
const usageInfo = ref(null)
const chatSessions = ref([])
const currentSessionId = ref(null)
const routeIntentHandled = ref(false)
let abortController = null

// 流式传输时的临时状态
const streamingSessionId = ref(null)
const streamingMessages = ref(null)
const streamingStoppedByUser = ref(false)

// ──────── 当前选中的模型 ────────
const selectedModelId = ref('deepseek-chat')

// ──────── 知识库 RAG ────────
const kbList = ref([])
const selectedKbId = ref('')
const selectedChatMode = ref('auto')
const sidebarSections = buildSidebarSections()
const modeOptions = [
    { key: 'auto', label: '智能' },
    { key: 'normal', label: '普通' },
    { key: 'rag', label: '知识库' },
    { key: 'web', label: '联网' }
]

onMounted(async () => {
    try {
        const res = await getPublicKbList()
        if (res.success && res.data?.length) {
            kbList.value = res.data
            restoreModeFromSession()
        }
    } catch (_) { /* 忽略，知识库不是必须的 */ }
})


const modelOptions = ref([
    { id: 'deepseek-chat', name: 'DeepSeek V3', provider: 'deepseek' },
    { id: 'deepseek-reasoner', name: 'DeepSeek R1', provider: 'deepseek' },
    { id: 'gpt-4o-mini', name: 'GPT-4o Mini', provider: 'openai' },
    { id: 'gpt-4o', name: 'GPT-4o', provider: 'openai' },
    { id: 'claude-sonnet-4-20250514', name: 'Claude Sonnet 4', provider: 'claude' },
    { id: 'claude-3-5-sonnet-20241022', name: 'Claude 3.5 Sonnet', provider: 'claude' },
    { id: 'gemini-2.0-flash', name: 'Gemini 2.0 Flash', provider: 'gemini' },
    { id: 'glm-4-flash', name: 'GLM-4 Flash', provider: 'zhipu' },
    { id: 'glm-4', name: 'GLM-4', provider: 'zhipu' },
    { id: 'MiniMax-M2.7', name: 'MiniMax M2.7', provider: 'minimax' },
    { id: 'ernie-3.5-8k', name: 'ERNIE 3.5', provider: 'qianfan' }
])

const selectedModelOption = computed(() => modelOptions.value.find(o => o.id === selectedModelId.value) || null)

const getModelProvider = (modelId) => modelOptions.value.find(o => o.id === modelId)?.provider || ''

const currentModelName = computed(() => {
    return selectedModelOption.value?.name || selectedModelId.value
})

const selectedKbOption = computed(() => {
    const id = Number(selectedKbId.value || 0)
    return kbList.value.find(kb => Number(kb.id) === id) || null
})

const selectedKb = computed(() => selectedChatMode.value === 'rag' ? selectedKbOption.value : null)

const activeKbId = computed(() => {
    if (selectedChatMode.value !== 'rag') return null
    const id = Number(selectedKbId.value)
    return Number.isFinite(id) && id > 0 ? id : null
})

const chatMode = computed(() => {
    if (selectedChatMode.value === 'web') return 'web'
    return activeKbId.value ? 'rag' : 'normal'
})

const setChatMode = (mode) => {
    if (mode === 'auto') {
        selectedChatMode.value = 'auto'
        return
    }
    if (mode === 'rag' && kbList.value.length === 0) {
        ElMessage.warning('暂无可用知识库')
        selectedChatMode.value = 'auto'
        return
    }
    selectedChatMode.value = mode
    if (mode === 'rag' && !selectedKbId.value && kbList.value.length > 0) {
        selectedKbId.value = String(kbList.value[0].id)
    }
}

const handleKbChange = () => {
    if (selectedChatMode.value === 'auto') return
    selectedChatMode.value = selectedKbId.value ? 'rag' : 'normal'
}

const quickPrompts = computed(() => {
    if (selectedChatMode.value === 'auto') {
        return ['今天有什么 AI 热点？', 'Django 最新版是多少？', '怎么学习英语？', '根据博客文章总结重点']
    }
    if (selectedChatMode.value === 'rag') {
        return ['总结知识库重点', '这篇文章讲了什么？', '根据知识库解释这个概念', '列出引用来源']
    }
    if (selectedChatMode.value === 'web') {
        return ['搜索最新 AI 新闻', '查一下这个技术的最新版本', '对比两个工具的优缺点', '找资料并总结']
    }
    return ['讲一个笑话', '帮我写一段代码', '解释一个技术概念', '帮我润色一段文字']
})

const welcomeDescription = computed(() => {
    if (selectedChatMode.value === 'auto') {
        return '智能选择会自动判断是否需要联网、知识库或普通回答，减少手动切换。'
    }
    if (selectedChatMode.value === 'rag') {
        return '知识库问答模式会先检索选中的知识库，再基于命中的来源回答。'
    }
    if (selectedChatMode.value === 'web') {
        return '联网搜索模式会先检索公开网页，再结合来源生成回答，适合需要最新资料的问题。'
    }
    return '普通聊天模式不会检索知识库，适合闲聊、写作、代码解释和通用问题。'
})

const filteredSidebarSessions = computed(() => {
    const keyword = sidebarSearchQuery.value.trim().toLowerCase()
    if (!keyword) return chatSessions.value

    return chatSessions.value.filter(session => {
        const title = String(session.title || '').toLowerCase()
        const time = String(session.time || '').toLowerCase()
        return title.includes(keyword) || time.includes(keyword)
    })
})

const groupedSidebarSessions = computed(() => groupSessionsForSidebar(filteredSidebarSessions.value))
const currentSessionSubtitle = computed(() => currentSessionId.value ? '围绕当前会话继续工作' : '从一条新对话开始')
const topBarState = computed(() => buildTopBarState({
    modelName: currentModelName.value,
    chatMode: selectedChatMode.value,
    selectedKbName: selectedKbOption.value?.name,
    webEnabled: selectedChatMode.value === 'web'
}))
const statusMoreActions = computed(() => [
    { key: 'export', label: '导出对话', disabled: !currentSessionId.value },
    { key: 'clear-current', label: '清空当前会话', disabled: !currentSessionId.value || !displayMessages.value.length },
    { key: 'settings', label: '模型设置' },
    { key: 'clear-all', label: '清空全部会话', disabled: !chatSessions.value.length }
])

// ──────── 计算属性 ────────
const currentSessionTitle = computed(() => {
    const session = chatSessions.value.find(s => s.id === currentSessionId.value)
    return session?.title || '新会话'
})

const displayMessages = computed(() => {
    if (streamingMessages.value && streamingSessionId.value === currentSessionId.value) {
        return streamingMessages.value
    }
    const session = chatSessions.value.find(s => s.id === currentSessionId.value)
    return session?.messages || []
})

const restoreModeFromSession = (sessionId = currentSessionId.value) => {
    const session = chatSessions.value.find(s => s.id === sessionId)
    const messages = session?.messages || []
    const lastModeMessage = [...messages].reverse().find(m => m.mode || m.kbId)

    if (!lastModeMessage) {
        selectedChatMode.value = 'auto'
        return
    }

    if (lastModeMessage.mode === 'rag' || lastModeMessage.kbId) {
        if (lastModeMessage.kbId) {
            const kbExists = kbList.value.some(kb => Number(kb.id) === Number(lastModeMessage.kbId))
            if (!kbExists && kbList.value.length > 0) {
                selectedKbId.value = String(kbList.value[0].id)
            } else if (kbExists) {
                selectedKbId.value = String(lastModeMessage.kbId)
            }
        }

        if (selectedKbId.value || kbList.value.length > 0) {
            selectedChatMode.value = 'rag'
        } else {
            selectedChatMode.value = 'normal'
        }
        return
    }

    if (lastModeMessage.mode === 'normal') {
        selectedChatMode.value = 'auto'
        return
    }

    if (lastModeMessage.mode === 'web') {
        selectedChatMode.value = 'auto'
    }
}

const filteredArticles = computed(() => {
    const keyword = articleSearch.value.toLowerCase().trim()
    if (!keyword) return allArticles.value
    return allArticles.value.filter(a =>
        (a.title || a.Title)?.toLowerCase().includes(keyword) ||
        (a.summary || a.Summary)?.toLowerCase().includes(keyword)
    )
})

// ──────── 初始化 ────────
onMounted(async () => {
    await loadModels()
    await loadSessionsFromDb()
    await loadUsageInfo()
    await loadArticles()
    await handleRouteArticleIntent()
    scrollToBottom(true)
})

const loadModels = async () => {
    try {
        const res = await axios.get('/ai/models')
        if (res.success && res.data && res.data.length > 0) {
            modelOptions.value = res.data
            selectedModelId.value = res.data[0].id
        }
    } catch (e) {
        console.warn('加载模型列表失败，使用默认列表')
    }
}

const loadArticles = async () => {
    try {
        const res = await axios.post('/search/article', { keyword: '', pageNum: 1, pageSize: 100 })
        if (res.success && res.data) {
            allArticles.value = res.data.list || []
        }
    } catch (e) {
        console.error('加载文章列表失败:', e)
    }
}

const handleRouteArticleIntent = async () => {
    if (routeIntentHandled.value || !route.query.articleId) return
    routeIntentHandled.value = true

    const articleId = Number(route.query.articleId)
    if (!Number.isFinite(articleId) || articleId <= 0) return

    try {
        const res = await getArticleDetail(articleId)
        if (res.success && res.data) {
            quotedArticle.value = res.data
        }
    } catch (e) {
        console.warn('加载引用文章失败:', e)
    }

    if (!quotedArticle.value) return

    const currentSession = chatSessions.value.find(s => s.id === currentSessionId.value)
    if (!currentSessionId.value || currentSession?.messages?.length) {
        createNewChat()
    }

    selectedChatMode.value = 'normal'
    inputText.value = String(route.query.prompt || `请围绕《${quotedArticle.value.title || '这篇文章'}》进行总结，并列出关键知识点。`)
    await nextTick()
    autoResize()
    textareaRef.value?.focus()

    if (route.query.autoSend === '1') {
        const content = inputText.value
        inputText.value = ''
        await sendMessage({ content, forceMode: 'normal', freshContext: true })
    }

    const nextQuery = { ...route.query }
    delete nextQuery.articleId
    delete nextQuery.prompt
    delete nextQuery.autoSend
    router.replace({ path: route.path, query: nextQuery })
}

const routeArticleIntentKey = computed(() => {
    if (!route.query.articleId) return ''
    return [
        route.query.articleId,
        route.query.prompt || '',
        route.query.autoSend || ''
    ].join('|')
})

watch(routeArticleIntentKey, async (key, previousKey) => {
    if (!key || key === previousKey) return
    routeIntentHandled.value = false
    await handleRouteArticleIntent()
})

const loadUsageInfo = async () => {
    try {
        const res = await getUsageInfo(clientId)
        if (res.success && res.data) {
            usageInfo.value = res.data
        }
    } catch (e) {
        console.error('加载使用次数失败:', e)
    }
}

// ──────── 会话管理（数据库） ────────
const loadSessionsFromDb = async () => {
    try {
        const res = await getUserSessions(clientId)
        if (res.success && res.data) {
            chatSessions.value = res.data.map(s => {
                const rawUpdatedAt = s.updatedAt || s.createdAt || null
                return {
                    id: s.sessionId,
                    title: s.title || '新会话',
                    pinned: false,
                    time: formatDbTime(rawUpdatedAt),
                    createdAt: s.createdAt || rawUpdatedAt,
                    updatedAt: rawUpdatedAt,
                    messages: safeParseMessages(s.messages),
                    model: s.model
                }
            })
            if (chatSessions.value.length > 0 && !currentSessionId.value) {
                currentSessionId.value = chatSessions.value[0].id
            }
            restoreModeFromSession()
            scrollToBottom(true)
        }
    } catch (e) {
        console.error('加载历史会话失败:', e)
        chatSessions.value = []
    }
}

const safeParseMessages = (messagesJson) => {
    try {
        if (!messagesJson || messagesJson === '[]') return []
        const parsed = JSON.parse(messagesJson)
        return Array.isArray(parsed) ? parsed : []
    } catch {
        return []
    }
}

const formatDbTime = (timeStr) => {
    if (!timeStr) return ''
    try {
        const d = new Date(timeStr)
        if (Number.isNaN(d.getTime())) return timeStr
        return d.toLocaleString('zh-CN', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
    } catch {
        return timeStr
    }
}

const stampSessionTime = (session, date = new Date()) => {
    if (!session) return
    const isoTime = date.toISOString()
    if (!session.createdAt) session.createdAt = isoTime
    session.updatedAt = isoTime
    session.time = formatDbTime(isoTime)
}

const saveSessionToDb = async (sessionId, messages, model) => {
    try {
        await saveSession({
            sessionId,
            clientId,
            messages: JSON.stringify(messages.map(m => ({
                role: m.role,
                content: m.content,
                timestamp: m.timestamp,
                quotedArticleTitle: m.quotedArticleTitle || null,
                sources: m.sources || [],
                ragStatus: m.ragStatus || null,
                ragSteps: m.ragSteps || [],
                kbId: m.kbId || null,
                kbName: m.kbName || null,
                mode: m.mode || null,
                smartRouteLabel: m.smartRouteLabel || null
            }))),
            model: model || selectedModelId.value,
            provider: getModelProvider(model || selectedModelId.value)
        })
    } catch (e) {
        console.error('保存会话失败:', e)
    }
}

const createNewChat = () => {
    if (isStreaming.value) return
    const newId = 'session_' + Date.now()
    const now = new Date()
    const nowIso = now.toISOString()
    const newSession = {
        id: newId,
        title: '新会话',
        pinned: false,
        time: formatDbTime(nowIso),
        createdAt: nowIso,
        updatedAt: nowIso,
        messages: [],
        model: selectedModelId.value
    }
    chatSessions.value.unshift(newSession)
    currentSessionId.value = newId
}

const switchSession = (sessionId) => {
    if (isStreaming.value) return
    currentSessionId.value = sessionId
    restoreModeFromSession(sessionId)
    scrollToBottom(true)
}

const clearCurrentSession = async () => {
    if (isStreaming.value || !currentSessionId.value) return
    const session = chatSessions.value.find(s => s.id === currentSessionId.value)
    if (!session || !session.messages?.length) return

    try {
        await ElMessageBox.confirm('确定清空当前会话上下文吗？', '提示', { type: 'warning' })
        session.messages = []
        session.title = '新会话'
        stampSessionTime(session)
        await saveSessionToDb(session.id, session.messages, session.model)
        ElMessage.success('已清空当前上下文')
        scrollToBottom(true)
    } catch (e) {
        if (e !== 'cancel') ElMessage.error('清空失败')
    }
}

const deleteSession = async (sessionId) => {
    try {
        await ElMessageBox.confirm('确定删除该会话吗？', '提示', { type: 'warning' })
        await deleteUserSession(sessionId, clientId)
        chatSessions.value = chatSessions.value.filter(s => s.id !== sessionId)
        if (currentSessionId.value === sessionId) {
            currentSessionId.value = chatSessions.value[0]?.id || null
            restoreModeFromSession()
        }
        ElMessage.success('删除成功')
    } catch (e) {
        if (e !== 'cancel') ElMessage.error('删除失败')
    }
}

const clearAllSessions = async () => {
    if (isStreaming.value) return
    if (!chatSessions.value.length) {
        ElMessage.info('暂无对话')
        return
    }

    try {
        await ElMessageBox.confirm('确定清空所有对话吗？此操作不可恢复。', '提示', { type: 'warning' })
        const sessionIds = chatSessions.value.map(s => s.id)
        await Promise.allSettled(sessionIds.map(id => deleteUserSession(id, clientId)))
        chatSessions.value = []
        currentSessionId.value = null
        createNewChat()
        ElMessage.success('已清空所有对话')
    } catch (e) {
        if (e !== 'cancel') ElMessage.error('清空失败')
    }
}

const renameSession = async (session) => {
    try {
        const newTitle = await ElMessageBox.prompt('请输入会话标题', '重命名会话', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            inputValue: session.title,
            type: 'question'
        })
        session.title = newTitle
        await saveSessionToDb(session.id, session.messages, session.model)
        ElMessage.success('已重命名')
    } catch (e) {
        if (e !== 'cancel') ElMessage.error('重命名失败')
    }
}

const pinSession = (session) => {
    session.pinned = !session.pinned
    // Move pinned to top
    chatSessions.value.sort((a, b) => {
        if (a.pinned && !b.pinned) return -1
        if (!a.pinned && b.pinned) return 1
        return 0
    })
}

const exportSession = (session) => {
    const msgs = session.messages || []
    let md = `# 对话导出\n\n`
    md += `**标题：** ${session.title}\n`
    md += `**模型：** ${session.model || selectedModelId.value}\n`
    md += `**导出时间：** ${new Date().toLocaleString('zh-CN')}\n\n---\n\n`
    for (const msg of msgs) {
        if (msg.role === 'user') {
            md += `## 用户\n\n${msg.content}\n\n`
        } else if (msg.role === 'assistant' && msg.content) {
            md += `## AI 回复\n\n${msg.content}\n\n`
        }
    }
    const blob = new Blob([md], { type: 'text/markdown;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `chat-${session.title.replace(/[^a-z0-9\u4e00-\u9fa5]/gi, '-').slice(0, 20)}.md`
    a.click()
    URL.revokeObjectURL(url)
    ElMessage.success('已导出为 Markdown')
}

const shareCurrentSession = async () => {
    if (!currentSessionId.value) {
        ElMessage.info('请先选择一个会话')
        return
    }

    try {
        const href = router.resolve({
            path: route.path,
            query: {
                ...route.query,
                session: currentSessionId.value
            }
        }).href

        await navigator.clipboard.writeText(`${window.location.origin}${href}`)
        ElMessage.success('会话链接已复制')
    } catch (error) {
        console.error('Share session failed:', error)
        ElMessage.error('分享链接复制失败')
    }
}

const handleStatusMoreAction = (action) => {
    if (action === 'export') {
        exportSession(chatSessions.value.find(session => session.id === currentSessionId.value))
        return
    }

    if (action === 'clear-current') {
        clearCurrentSession()
        return
    }

    if (action === 'settings') {
        showSettings.value = true
        return
    }

    if (action === 'clear-all') {
        clearAllSessions()
    }
}

// ── Token 消耗预估 ─────────────────────────────────────
const estimateTokens = (text) => Math.max(1, Math.round(text.length * 0.4))

const inputTokenCount = computed(() => {
    if (!inputText.value.trim()) return 0
    const session = chatSessions.value.find(s => s.id === currentSessionId.value)
    const historyTokens = (session?.messages || []).reduce((sum, m) => sum + estimateTokens(m.content), 0)
    return historyTokens + estimateTokens(inputText.value)
})

const updateSessionLocally = (sessionId, messages) => {
    const session = chatSessions.value.find(s => s.id === sessionId)
    if (session) {
        session.messages = [...messages]
        const firstUser = messages.find(m => m.role === 'user')
        if (firstUser) {
            const content = firstUser.content.split('\n\n').pop() || firstUser.content
            session.title = content.slice(0, 25) + (content.length > 25 ? '...' : '')
        }
        stampSessionTime(session)
    }
}

const currentAssistantMessage = () => {
    const list = streamingMessages.value || []
    return list[list.length - 1]
}

const finishRagStatusIfNeeded = () => {
    const lastMsg = currentAssistantMessage()
    if (!lastMsg?.kbId || (lastMsg.sources && lastMsg.sources.length > 0)) return

    const waitingMessages = ['准备检索知识库...', '正在检索知识库...']
    if (lastMsg.content && (!lastMsg.ragStatus || waitingMessages.includes(lastMsg.ragStatus))) {
        lastMsg.ragStatus = '知识库未返回引用来源，已按普通对话回答'
        updateRagStep(lastMsg, 'sources', lastMsg.ragStatus, 'warning')
    }
}

const normalizeSource = (source = {}, index = 0) => ({
    index: source.index ?? source.Index ?? index + 1,
    documentId: source.documentId ?? source.DocumentId ?? null,
    chunkId: source.chunkId ?? source.ChunkId ?? `${source.DocumentId || 'source'}-${index}`,
    title: source.title ?? source.Title ?? '未知文档',
    content: source.content ?? source.Content ?? '',
    score: source.score ?? source.Score,
    relevance: source.relevance ?? source.Relevance ?? null,
    contentQuality: source.contentQuality ?? source.ContentQuality ?? null,
    hasUsableContent: source.hasUsableContent ?? source.HasUsableContent ?? false,
    url: source.url ?? source.Url ?? '',
    sourceType: source.sourceType ?? source.SourceType ?? 'rag'
})

const normalizeSources = (sources = []) => sources.map(normalizeSource)

const routeOnlyStatusKeywords = [
    '\u666e\u901a\u804a\u5929',
    '\u666e\u901a\u95ee\u9898',
    '\u76f4\u63a5\u56de\u7b54',
    '\u76f4\u63a5\u7528\u666e\u901a\u804a\u5929',
    '\u4e0d\u4f9d\u8d56\u5b9e\u65f6\u8d44\u6599'
]

const isRouteOnlyStatus = (chat, message = chat?.ragStatus) => {
    const text = `${message || ''}`.trim()
    if (!text) return false

    const routeReason = `${chat?.routeReason || ''}`.trim()
    if (routeReason && text === routeReason) return true

    return chat?.mode === 'normal' && routeOnlyStatusKeywords.some(keyword => text.includes(keyword))
}

const shouldShowRagStatus = (chat) => {
    if (!chat?.ragStatus || (chat.sources && chat.sources.length > 0)) return false
    return !isRouteOnlyStatus(chat)
}

const visibleRagSteps = (chat) => {
    const routeReason = `${chat?.routeReason || ''}`.trim()
    return (chat?.ragSteps || []).filter(step => {
        const message = `${step?.message || ''}`.trim()
        if (!message) return false
        if (step?.key === 'route') return false
        if (routeReason && message === routeReason) return false
        return !isRouteOnlyStatus(chat, message)
    })
}

const toggleSourcePanel = (chat) => {
    chat.sourcesExpanded = !chat.sourcesExpanded
}

const updateRagStep = (chat, key, message, status = 'running') => {
    if (!chat || !key) return
    if (!Array.isArray(chat.ragSteps)) {
        chat.ragSteps = []
    }

    if (status === 'running') {
        chat.ragSteps.forEach(step => {
            if (step.status === 'running' && step.key !== key) {
                step.status = 'completed'
            }
        })
    }

    const existing = chat.ragSteps.find(step => step.key === key)
    if (existing) {
        existing.message = message || existing.message
        existing.status = status
        return
    }

    chat.ragSteps.push({
        key,
        message: message || key,
        status
    })
}

const isConservativeRagAnswer = (chat) => {
    if (!chat?.content || !chat.sources?.length) return false
    return /没有.*(相关|找到|资料|内容|信息)|未找到|无法.*(回答|确定)/.test(chat.content)
}

const conservativeAnswerTip = (chat) => {
    if (chat?.mode === 'web') {
        return '联网来源可能跑偏或相关性不足，可以点击重试，或切回普通聊天直接回答。'
    }
    return '知识库已命中来源，但模型回答偏保守。可以点击重试重新生成。'
}

const canUseNormalFallback = (chat) => {
    if (isStreaming.value || !chat?.kbId) return false
    const noSources = !chat.sources || chat.sources.length === 0
    const noHitStatus = /没有命中|未命中|没有找到|检索失败/.test(chat.ragStatus || '')
    const conservativeAnswer = /知识库.*没有|没有.*相关内容|未找到/.test(chat.content || '')
    return noSources && (noHitStatus || conservativeAnswer)
}

const findPreviousUserMessage = (index) => {
    const messages = displayMessages.value || []
    for (let i = index - 1; i >= 0; i--) {
        if (messages[i]?.role === 'user') return messages[i]
    }
    return null
}

const answerWithoutKnowledgeBase = async (index) => {
    const userMessage = findPreviousUserMessage(index)
    if (!userMessage?.content) {
        ElMessage.warning('没有找到上一条问题')
        return
    }
    await sendMessage({
        content: userMessage.content,
        forceMode: 'normal',
        freshContext: true
    })
}

const buildOutgoingMessages = (messages, mode, freshContext = false) => {
    const cleanMessages = (messages || []).filter(m => ['system', 'user', 'assistant'].includes(m.role))
    const lastUserMessage = [...cleanMessages].reverse().find(m => m.role === 'user')
    let scopedMessages = cleanMessages

    if (freshContext && lastUserMessage) {
        scopedMessages = [lastUserMessage]
    } else if (mode === 'rag' || mode === 'web') {
        const systemMessages = cleanMessages.filter(m => m.role === 'system')
        const recentMessages = cleanMessages
            .filter(m => m.role !== 'system')
            .filter(m => {
                if (mode !== 'web') return true
                const text = `${m.content || ''} ${m.ragStatus || ''}`
                return !(m.role === 'assistant' && /知识库|RAG|检索知识库|没有.*相关内容|未找到/.test(text))
            })
            .slice(-6)
        scopedMessages = [...systemMessages, ...recentMessages]
    } else {
        scopedMessages = cleanMessages
            .filter(m => !(m.role === 'assistant' && m.kbId && /知识库|检索|命中|引用来源|相关内容|没有找到/.test(m.content || m.ragStatus || '')))
            .slice(-10)
    }

    const outgoingMessages = scopedMessages.map(m => ({ role: m.role, content: m.content }))
    if (mode === 'normal') {
        outgoingMessages.unshift({
            role: 'system',
            content: '当前是普通聊天模式，未启用知识库。请像正常 AI 助手一样回答用户，不要因为知识库没有命中或没有启用而拒绝回答。'
        })
    } else if (mode === 'web') {
        outgoingMessages.unshift({
            role: 'system',
            content: '当前是联网搜索模式。请结合联网来源回答；如果没有联网来源，也不要提及知识库，请直接说明联网搜索未返回可用来源。'
        })
    }

    return outgoingMessages
}

const handleStreamPayload = async (raw, appendContent) => {
    if (!raw || raw === '[DONE]') {
        finishRagStatusIfNeeded()
        return
    }

    const parsed = JSON.parse(raw)
    const lastMsg = currentAssistantMessage()
    if (!lastMsg) return

    const applyRoutePayload = (payload = {}) => {
        if (!payload.routeMode && !payload.routeReason && !payload.routeKbId) return
        const routeMode = payload.routeMode || lastMsg.mode
        lastMsg.mode = routeMode
        lastMsg.routeReason = payload.routeReason || lastMsg.routeReason || ''
        lastMsg.smartRouteLabel = smartModeLabel(routeMode)
        if (payload.routeKbId) {
            lastMsg.kbId = payload.routeKbId
            const kb = kbList.value.find(item => Number(item.id) === Number(payload.routeKbId))
            if (kb) lastMsg.kbName = kb.name
        }
        if (routeMode === 'web') {
            lastMsg.sourcesExpanded = true
        }
    }

    if (parsed.type === 'rag_status') {
        const payload = parsed.payload || {}
        applyRoutePayload(payload)
        if (payload.step === 'route') {
            lastMsg.ragStatus = ''
            lastMsg.ragSteps = visibleRagSteps(lastMsg)
            return
        }
        lastMsg.ragStatus = payload.message || (lastMsg.mode === 'web' ? '正在联网搜索...' : '正在检索知识库...')
        updateRagStep(lastMsg, payload.step || 'status', lastMsg.ragStatus, payload.status || 'running')
        return
    }

    if (parsed.type === 'rag_sources') {
        applyRoutePayload(parsed.payload || {})
        const sources = parsed.payload?.sources || []
        lastMsg.sources = normalizeSources(sources)
        lastMsg.ragStatus = sources.length
            ? parsed.payload?.message || `找到 ${sources.length} 条引用来源`
            : parsed.payload?.message || (lastMsg.mode === 'web' ? '没有找到可用联网来源' : '没有命中足够相关的知识库内容')
        updateRagStep(lastMsg, 'sources', lastMsg.ragStatus, sources.length ? 'completed' : 'warning')
        return
    }

    if (parsed.type === 'rag_error') {
        const payload = parsed.payload || {}
        applyRoutePayload(payload)
        lastMsg.ragStatus = payload.message || '知识库检索失败，已切换为普通对话'
        updateRagStep(lastMsg, payload.step || 'error', lastMsg.ragStatus, payload.status || 'error')
        return
    }

    if (parsed.content) {
        if (lastMsg.kbId || lastMsg.mode === 'web') {
            updateRagStep(lastMsg, 'generate', lastMsg.mode === 'web' ? '正在基于联网资料生成回答...' : '正在生成回答...', 'running')
        }
        appendContent(parsed.content)
        finishRagStatusIfNeeded()
        scrollToBottom()
    }

    if (parsed.error) {
        lastMsg.content = '❌ ' + parsed.error
        lastMsg.ragStatus = ''
        await loadUsageInfo()
    }
}

const resolveSmartMode = (content, article = null) => {
    if (selectedChatMode.value !== 'auto') {
        return chatMode.value
    }

    const text = `${content || ''}`.trim().toLowerCase()
    if (!text) return 'normal'

    const webPattern = /(联网|上网|搜索|搜一下|查一下|官网|官方|今天|今日|现在|当前|最近|最新|最新版|版本号|刚刚|实时|热点|新闻|快讯|价格|股价|汇率|天气|气温|温度|下雨|政策|法规|比赛|赛程|榜单|排名|发布|更新|latest|current|news|weather|price|version|release|trending)/i
    if (webPattern.test(text)) {
        return 'web'
    }

    const ragPattern = /(这篇|本文|文章|博客|知识库|站内|根据.*资料|基于.*资料|引用来源|总结.*重点|讲了什么|归纳.*内容|推荐.*文章|相关文章|项目文档|教程里|博客里)/i
    if (!article && selectedKbOption.value && ragPattern.test(text)) {
        return 'rag'
    }

    return 'normal'
}

const smartModeLabel = (mode) => {
    if (selectedChatMode.value !== 'auto') return ''
    if (mode === 'web') return '智能选择：联网搜索'
    if (mode === 'rag') return '智能选择：知识库问答'
    if (mode === 'article') return '智能选择：文章辅助'
    if (mode === 'auto') return '智能选择：判断中'
    return '智能选择：普通聊天'
}

// ──────── 发送消息 ────────
const sendMessage = async (options = {}) => {
    const forcedContent = typeof options.content === 'string' ? options.content.trim() : ''
    if ((!forcedContent && !inputText.value.trim()) || isStreaming.value) return

    // 如果没有会话，先创建一个
    if (!currentSessionId.value) {
        createNewChat()
    }

    const originalChatMode = selectedChatMode.value
    const requestedMode = options.forceMode || selectedChatMode.value
    if (options.forceMode) {
        selectedChatMode.value = options.forceMode
    }

    let userContent = forcedContent || inputText.value.trim()
    const currentQuotedArticle = quotedArticle.value
    const effectiveMode = requestedMode === 'auto' ? 'auto' : chatMode.value
    const candidateKb = (requestedMode === 'rag' || requestedMode === 'auto') ? selectedKbOption.value : null
    const effectiveKb = requestedMode === 'rag' ? candidateKb : null
    quotedArticle.value = null
    if (!forcedContent) {
        inputText.value = ''
    }
    if (textareaRef.value) {
        textareaRef.value.style.height = 'auto'
    }

    const sessionId = currentSessionId.value
    const session = chatSessions.value.find(s => s.id === sessionId)
    const sessionMessages = session ? [...(session.messages || [])] : []

    // 如果引用了文章，在消息里记录
    if (currentQuotedArticle) {
        userContent = `【引用文章】${currentQuotedArticle.title}\n\n请问：${userContent}`
    }

    sessionMessages.push({
        role: 'user',
        content: userContent,
        quotedArticleTitle: currentQuotedArticle?.title || null,
        mode: effectiveMode,
        smartRouteLabel: smartModeLabel(effectiveMode),
        timestamp: Date.now()
    })
    sessionMessages.push({
        role: 'assistant',
        content: '',
        sources: [],
        ragStatus: effectiveMode === 'web' ? '准备联网搜索...' : effectiveKb ? '准备检索知识库...' : '',
        ragSteps: effectiveMode === 'web'
            ? [{ key: 'queued', message: '准备联网搜索...', status: 'running' }]
            : effectiveKb
            ? [{ key: 'queued', message: '准备检索知识库...', status: 'running' }]
            : [],
        kbId: effectiveKb?.id || null,
        kbName: effectiveKb?.name || null,
        mode: effectiveMode,
        smartRouteLabel: smartModeLabel(effectiveMode),
        sourcesExpanded: effectiveMode === 'web',
        timestamp: Date.now()
    })

    // 更新本地会话标题
    if (session) {
        const displayContent = currentQuotedArticle
            ? userContent.split('\n\n')[1] || userContent
            : userContent
        session.title = displayContent.slice(0, 25) + (displayContent.length > 25 ? '...' : '')
        stampSessionTime(session)
    }

    streamingSessionId.value = sessionId
    streamingMessages.value = sessionMessages
    streamingStoppedByUser.value = false
    scrollToBottom(true)

    isStreaming.value = true
    abortController = new AbortController()

    try {
        const token = getToken() || ''
        const outgoingMessages = buildOutgoingMessages(sessionMessages.slice(0, -1), requestedMode, options.freshContext)
        const response = await fetch('/api/ai/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': token ? 'Bearer ' + token : ''
            },
            signal: abortController.signal,
            body: JSON.stringify({
                messages: outgoingMessages,
                model: selectedModelId.value,
                provider: getModelProvider(selectedModelId.value),
                mode: requestedMode,
                enableWebSearch: requestedMode === 'web',
                sessionId: sessionId,
                clientId: clientId,
                articleContent: currentQuotedArticle
                    ? (currentQuotedArticle.content || currentQuotedArticle.Content || currentQuotedArticle.summary || '')
                    : null,
                articleTitle: currentQuotedArticle?.title || currentQuotedArticle?.Title || null,
                articleId: currentQuotedArticle?.id || currentQuotedArticle?.Id || null,
                kbId: candidateKb?.id || null
            })
        })

        if (!response.ok || !response.body) {
            throw new Error(`请求失败：${response.status}`)
        }

        const reader = response.body.getReader()
        const decoder = new TextDecoder()
        let responseText = ''
        let pendingLine = ''

        while (true) {
            const { done, value } = await reader.read()
            if (done) break

            const text = pendingLine + decoder.decode(value, { stream: true })
            const lines = text.split('\n')
            pendingLine = lines.pop() || ''

            for (const line of lines) {
                if (!line.startsWith('data: ')) continue
                const raw = line.slice(6)
                try {
                    await handleStreamPayload(raw, (content) => {
                        responseText += content
                        currentAssistantMessage().content = responseText
                    })
                } catch { /* 忽略解析错误 */ }
            }
        }

        if (pendingLine.startsWith('data: ')) {
            try {
                await handleStreamPayload(pendingLine.slice(6), (content) => {
                    responseText += content
                    currentAssistantMessage().content = responseText
                })
            } catch { /* 忽略最后一段不完整事件 */ }
        }
        finishRagStatusIfNeeded()
    } catch (error) {
        if (error.name === 'AbortError') {
            const lastMsg = streamingMessages.value?.[streamingMessages.value.length - 1]
            if (streamingStoppedByUser.value && lastMsg) {
                lastMsg.content += (lastMsg.content ? '\n\n' : '') + '[ 已停止生成 ]'
            }
        } else {
            console.error('流式请求出错:', error)
            const lastMsg = streamingMessages.value?.[streamingMessages.value.length - 1]
            if (lastMsg) {
                lastMsg.content = `抱歉，请求出错了，请稍后重试。\n\n${error.message || ''}`.trim()
            }
        }
    } finally {
        isStreaming.value = false
        abortController = null

        // 将完整会话同步到本地状态 & 保存到数据库
        const finalSessionId = streamingSessionId.value
        const finalMessages = Array.isArray(streamingMessages.value) ? [...streamingMessages.value] : []
        if (finalSessionId) {
            updateSessionLocally(finalSessionId, finalMessages)
            await saveSessionToDb(finalSessionId, finalMessages, selectedModelId.value)
        }

        streamingSessionId.value = null
        streamingMessages.value = null
        streamingStoppedByUser.value = false

        scrollToBottom()
        await loadUsageInfo()
        if (options.forceMode) {
            selectedChatMode.value = originalChatMode
        }
    }
}

const stopStreaming = () => {
    if (abortController) {
        streamingStoppedByUser.value = true
        abortController.abort()
    }
}

const regenerateResponse = async (index) => {
    if (isStreaming.value) return

    const session = chatSessions.value.find(s => s.id === currentSessionId.value)
    if (!session || !session.messages) return

    // 获取需要重试的最后一条用户消息
    let userMsgIndex = index - 1
    while (userMsgIndex >= 0 && session.messages[userMsgIndex].role !== 'user') {
        userMsgIndex--
    }

    if (userMsgIndex < 0) return

    const userMsg = session.messages[userMsgIndex]
    if (userMsg.mode === 'rag' || userMsg.kbId) {
        selectedChatMode.value = 'rag'
        if (userMsg.kbId) selectedKbId.value = String(userMsg.kbId)
    } else if (userMsg.mode === 'web' || userMsg.mode === 'normal') {
        selectedChatMode.value = 'auto'
    }

    // 如果之前有引用文章，恢复引用状态
    if (userMsg.quotedArticleTitle) {
        const article = allArticles.value.find(a => (a.title || a.Title) === userMsg.quotedArticleTitle)
        if (article) {
            quotedArticle.value = article
        }
    }

    // 截断消息到当前用户消息之前
    session.messages = session.messages.slice(0, userMsgIndex)

    // 把用户的消息重新放回输入框
    let originalText = userMsg.content
    if (userMsg.quotedArticleTitle && originalText.includes('【引用文章】')) {
        // 提取真正的提问部分
        const parts = originalText.split('\n\n请问：')
        if (parts.length > 1) {
            originalText = parts[1]
        }
    }

    inputText.value = originalText
    nextTick(() => {
        textareaRef.value?.focus()
        autoResize()
    })
}

// ──────── 辅助函数 ────────
const handleScroll = () => {
    if (!chatContainer.value) return
    const { scrollTop, scrollHeight, clientHeight } = chatContainer.value
    const distanceToBottom = scrollHeight - scrollTop - clientHeight
    const isNearBottom = distanceToBottom < 96
    showScrollBtn.value = distanceToBottom > 200

    if (isNearBottom) {
        autoFollowScroll.value = true
    } else if (isStreaming.value) {
        autoFollowScroll.value = false
    }
}

const scrollToBottom = async (force = false) => {
    await nextTick()
    if (chatContainer.value) {
        if (!force && !autoFollowScroll.value) {
            return
        }
        chatContainer.value.scrollTop = chatContainer.value.scrollHeight
        showScrollBtn.value = false
        autoFollowScroll.value = true
        if (force) {
            requestAnimationFrame(() => {
                if (!chatContainer.value) return
                chatContainer.value.scrollTop = chatContainer.value.scrollHeight
                showScrollBtn.value = false
                autoFollowScroll.value = true
            })
            setTimeout(() => {
                if (!chatContainer.value) return
                chatContainer.value.scrollTop = chatContainer.value.scrollHeight
                showScrollBtn.value = false
                autoFollowScroll.value = true
            }, 120)
        }
    }
}

const autoResize = () => {
    if (textareaRef.value) {
        textareaRef.value.style.height = 'auto'
        textareaRef.value.style.height = Math.min(textareaRef.value.scrollHeight, 132) + 'px'
    }
}

const handleShiftEnter = () => {
    // Shift+Enter 换行由浏览器默认处理，这里只做 resize
    nextTick(() => autoResize())
}

const formatTime = (timestamp) => {
    if (!timestamp) return ''
    return new Date(timestamp).toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
}

const formatScore = (score) => {
    const value = Number(score)
    if (!Number.isFinite(value)) return ''
    return value >= 1 ? `${value.toFixed(1)} 命中` : `${Math.round(value * 100)}%`
}

const formatSourceBadge = (source = {}) => {
    if (source.sourceType && source.sourceType !== 'rag') {
        if (source.isAuthoritative) return '权威'
        if (source.hasUsableContent) return `正文 ${Math.round(Number(source.contentQuality || 0) * 100)}%`
        return `标题 ${Math.round(Number(source.relevance || source.score || 0) * 100)}%`
    }
    return formatScore(source.score)
}

const copyMessage = async (content) => {
    try {
        await navigator.clipboard.writeText(content || '')
        ElMessage.success('已复制')
    } catch {
        ElMessage.error('复制失败')
    }
}

const suggestedFollowUps = (chat) => {
    if (!chat?.content) return []
    if (chat.mode === 'web') {
        return ['只保留权威来源', '按时间线整理', '继续联网查证']
    }
    if (chat.mode === 'article') {
        return ['总结本文重点', '生成学习路线', '生成面试题']
    }
    if (chat.mode === 'rag' || chat.kbId || chat.sources?.length) {
        return ['只根据来源总结', '列出引用证据', '生成学习路线']
    }
    return ['用更简单的话解释', '整理成表格', '生成面试题']
}

const buildFollowUpPrompt = (text, chat) => {
    if (!chat) return text
    if (text.includes('联网') || text.includes('权威') || text.includes('时间线')) {
        return `${text}：请基于你上一条回答继续处理；如果需要最新信息，请再次联网交叉验证，并标出可靠来源。`
    }
    if (text.includes('来源') || text.includes('引用')) {
        return `${text}：请只基于上一条回答中的来源和证据继续回答，不要扩展没有来源支撑的结论。`
    }
    if (text.includes('本文') || chat.mode === 'article') {
        return `${text}：请围绕刚才引用的文章继续回答，保持结构清晰。`
    }
    return `${text}：请基于你上一条回答继续处理。`
}

const useFollowUp = (text, chat = null) => {
    inputText.value = buildFollowUpPrompt(text, chat)
    nextTick(() => {
        textareaRef.value?.focus()
        autoResize()
    })
}

const usePrompt = (prompt) => {
    inputText.value = prompt
    nextTick(() => textareaRef.value?.focus())
}

const handleSettingsChange = ({ model, temperature }) => {
    if (model) selectedModelId.value = model
}

const selectArticle = async (article) => {
    showArticleDialog.value = false
    articleSearch.value = ''
    try {
        const res = await getArticleDetail(article.id || article.Id)
        quotedArticle.value = res.success && res.data ? res.data : article
    } catch {
        quotedArticle.value = article
    }
}
</script>

<style scoped>
.slide-enter-active,
.slide-leave-active {
    transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
    overflow: hidden;
}
.slide-enter-from,
.slide-leave-to {
    width: 0 !important;
    opacity: 0;
}

/* 快速提示卡片 */
.quick-prompt-card {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    padding: 1rem 1.25rem;
    border-radius: 1rem;
    border: 1px solid var(--border-base);
    background: var(--bg-card);
    transition: all 0.2s ease;
    cursor: pointer;
    text-align: left;
}
.quick-prompt-card:hover {
    border-color: var(--color-primary);
    background: var(--bg-hover);
    transform: translateY(-1px);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

/* AI 回复气泡 */
.chat-message {
    display: flex;
    gap: 0.9rem;
    margin-bottom: 1.9rem;
}

.chat-message-assistant {
    align-items: flex-start;
}

.chat-ai-entry {
    max-width: min(100%, 820px);
}

.chat-ai-head {
    margin-bottom: 0.7rem;
}

.chat-ai-headline {
    display: flex;
    align-items: center;
    gap: 0.65rem;
    margin-bottom: 0.45rem;
}

.chat-ai-name {
    color: var(--text-heading);
    font-size: 0.95rem;
    font-weight: 700;
}

.chat-ai-time {
    color: var(--text-placeholder);
    font-size: 0.75rem;
}

.chat-ai-bubble {
    padding: 0;
    border: 0;
    background: transparent;
    color: var(--text-body);
    box-shadow: none;
}

/* 暗色模式 AI 气泡内 Markdown */
.dark .chat-ai-bubble {
    background: transparent;
}

.dark .chat-ai-bubble :deep(h1),
.dark .chat-ai-bubble :deep(h2),
.dark .chat-ai-bubble :deep(h3),
.dark .chat-ai-bubble :deep(h4),
.dark .chat-ai-bubble :deep(h5),
.dark .chat-ai-bubble :deep(h6) {
    color: #f9fafb;
    border-bottom-color: #253341;
}

.dark .chat-ai-bubble :deep(a) {
    color: #60a5fa;
}

.dark .chat-ai-bubble :deep(code) {
    background-color: #253341;
    color: #93c5fd;
}

.dark .chat-ai-bubble :deep(pre) {
    background-color: #0f1419;
    border: 1px solid #253341;
}

.dark .chat-ai-bubble :deep(pre code) {
    background-color: transparent;
    color: #e5e7eb;
}

.dark .chat-ai-bubble :deep(blockquote) {
    border-left-color: #3b82f6;
    color: #8899a6;
    background-color: rgba(59, 130, 246, 0.05);
}

.dark .chat-ai-bubble :deep(ul li::marker),
.dark .chat-ai-bubble :deep(ol li::marker) {
    color: #3b82f6;
}

.dark .chat-ai-bubble :deep(table) {
    border-color: #253341;
}

.dark .chat-ai-bubble :deep(th),
.dark .chat-ai-bubble :deep(td) {
    border-color: #253341;
    color: #e5e7eb;
}

.dark .chat-ai-bubble :deep(th) {
    background-color: #1c2732;
}

.rag-status {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    padding: 0.35rem 0.65rem;
    border-radius: 999px;
    color: var(--text-muted);
    background: var(--bg-hover);
    border: 1px solid var(--border-base);
    font-size: 0.75rem;
}

.chat-ai-streaming {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    min-height: 2.25rem;
}

.rag-fallback-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-height: 2rem;
    padding: 0 0.8rem;
    border-radius: 999px;
    border: 1px solid rgba(59, 130, 246, 0.24);
    background: rgba(59, 130, 246, 0.10);
    color: var(--color-primary);
    font-size: 0.76rem;
    font-weight: 700;
    transition: all 0.18s ease;
}

.rag-fallback-btn:hover {
    transform: translateY(-1px);
    background: rgba(59, 130, 246, 0.16);
}

.rag-step-list {
    display: grid;
    gap: 0.4rem;
    padding: 0.6rem 0.7rem;
    border-radius: 0.9rem;
    background: var(--bg-hover);
    border: 1px solid var(--border-base);
}

.rag-step-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    min-width: 0;
    color: var(--text-muted);
    font-size: 0.75rem;
    line-height: 1.4;
}

.rag-step-dot {
    width: 0.5rem;
    height: 0.5rem;
    border-radius: 999px;
    background: var(--text-placeholder);
    flex-shrink: 0;
}

.rag-step-running .rag-step-dot {
    background: var(--color-primary);
    box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.12);
    animation: ragPulse 1.15s ease-in-out infinite;
}

.rag-step-completed .rag-step-dot {
    background: #10b981;
}

.rag-step-warning .rag-step-dot {
    background: #f59e0b;
}

.rag-step-error .rag-step-dot {
    background: #ef4444;
}

.rag-step-completed .rag-step-label {
    color: var(--text-body);
}

.rag-step-warning .rag-step-label {
    color: #b45309;
}

.rag-step-error .rag-step-label {
    color: #dc2626;
}

.dark .rag-step-warning .rag-step-label {
    color: #fbbf24;
}

.dark .rag-step-error .rag-step-label {
    color: #f87171;
}

@keyframes ragPulse {
    0%, 100% {
        transform: scale(1);
        opacity: 1;
    }
    50% {
        transform: scale(0.72);
        opacity: 0.72;
    }
}

.rag-warning {
    padding: 0.55rem 0.75rem;
    border-radius: 0.85rem;
    border: 1px solid rgba(245, 158, 11, 0.26);
    background: rgba(245, 158, 11, 0.08);
    color: #b45309;
    font-size: 0.76rem;
    line-height: 1.5;
}

.dark .rag-warning {
    color: #fbbf24;
    background: rgba(245, 158, 11, 0.10);
    border-color: rgba(245, 158, 11, 0.24);
}

.rag-dot {
    width: 0.45rem;
    height: 0.45rem;
    border-radius: 999px;
    background: var(--color-primary);
    box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.12);
}

.rag-source-panel {
    border: 1px solid var(--border-base);
    border-radius: 0.9rem;
    background: var(--bg-hover);
    overflow: hidden;
}

.rag-source-title {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    padding: 0.65rem 0.8rem;
    border-bottom: 1px solid var(--border-base);
    color: var(--text-heading);
    font-size: 0.78rem;
    font-weight: 700;
    background: transparent;
    cursor: pointer;
    text-align: left;
}

.rag-source-title:hover {
    background: rgba(59, 130, 246, 0.05);
}

.rag-source-toggle {
    display: inline-flex;
    align-items: center;
    gap: 0.35rem;
    color: var(--text-muted);
}

.rag-source-list {
    display: grid;
    gap: 0.55rem;
    padding: 0.65rem;
}

.rag-source-item {
    padding: 0.65rem;
    border-radius: 0.75rem;
    background: var(--bg-card);
    border: 1px solid var(--border-light);
}

.rag-source-head {
    display: flex;
    align-items: center;
    gap: 0.45rem;
    min-width: 0;
    margin-bottom: 0.35rem;
}

.rag-source-index {
    color: var(--color-primary);
    font-weight: 800;
    flex-shrink: 0;
}

.rag-source-name {
    color: var(--text-heading);
    font-weight: 700;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.rag-source-link {
    text-decoration: none;
}

.rag-source-link:hover {
    color: var(--color-primary);
}

.rag-source-score {
    margin-left: auto;
    padding: 0.1rem 0.45rem;
    border-radius: 999px;
    background: rgba(59, 130, 246, 0.12);
    color: var(--color-primary);
    font-size: 0.7rem;
    flex-shrink: 0;
}

.rag-source-item p {
    margin: 0;
    color: var(--text-muted);
    font-size: 0.75rem;
    line-height: 1.55;
}

.ai-follow-up-list {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-top: 14px;
}

.ai-follow-up-list button {
    padding: 7px 10px;
    border-radius: 999px;
    color: var(--text-secondary);
    font-size: 12px;
    font-weight: 650;
    background: var(--bg-hover);
    border: 1px solid var(--border-base);
    transition: all 0.18s ease;
}

.ai-follow-up-list button:hover {
    color: var(--color-primary);
    border-color: rgba(59, 130, 246, 0.35);
    background: var(--bg-card);
    transform: translateY(-1px);
}

.chat-ai-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 12px;
    opacity: 0.9;
    transition: opacity 0.18s ease;
}

.group:hover .chat-ai-actions {
    opacity: 1;
}

.chat-ai-action-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    min-height: 30px;
    padding: 0 10px;
    border-radius: 999px;
    border: 1px solid var(--border-base);
    background: var(--bg-card);
    color: var(--text-secondary);
    font-size: 0.76rem;
    transition: all 0.18s ease;
}

.chat-ai-action-btn:hover {
    color: var(--color-primary);
    border-color: rgba(59, 130, 246, 0.28);
    background: color-mix(in srgb, var(--bg-card) 82%, white);
}

.rag-chip {
    background: rgba(59, 130, 246, 0.12);
    color: var(--color-primary);
    border: 1px solid rgba(59, 130, 246, 0.22);
}

/* 打字动画圆点 */
.typing-dot {
    width: 0.375rem;
    height: 0.375rem;
    background: var(--color-primary);
    border-radius: 9999px;
    display: inline-block;
    animation: bounce 1.4s infinite ease-in-out both;
}
@keyframes bounce {
    0%, 80%, 100% { transform: scale(0); }
    40% { transform: scale(1); }
}

/* 输入框容器 */
.chat-input-wrapper {
    position: relative;
    background: var(--bg-card);
    border-radius: 1.25rem;
    border: 1.5px solid var(--border-base);
    transition: all 0.2s ease;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.chat-composer {
    background:
        linear-gradient(180deg, transparent, rgba(15, 23, 42, 0.04)),
        var(--bg-base);
    border-top: 1px solid var(--border-base);
}

.dark .chat-composer {
    background:
        linear-gradient(180deg, rgba(15, 23, 42, 0.18), rgba(2, 6, 23, 0.72)),
        var(--bg-base);
    border-top-color: rgba(148, 163, 184, 0.16);
}
.chat-input-wrapper:focus-within {
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px var(--focus-ring);
}

/* 发送按钮 */
.chat-send-btn {
    width: 2rem;
    height: 2rem;
    border-radius: 0.75rem;
    display: flex;
    align-items: center;
    justify-content: center;
    border: none;
    transition: all 0.2s ease;
    flex-shrink: 0;
}
.chat-send-btn:not(:disabled):hover {
    transform: scale(1.05);
}

.scroll-btn-enter-active,
.scroll-btn-leave-active {
    transition: opacity 0.2s ease, transform 0.2s ease;
}
.scroll-btn-enter-from,
.scroll-btn-leave-to {
    opacity: 0;
    transform: translateY(8px);
}

/* 滚动条 */
.chat-messages-scroll::-webkit-scrollbar,
.chat-sidebar-scroll::-webkit-scrollbar {
    width: 4px;
}
.chat-messages-scroll::-webkit-scrollbar-track,
.chat-sidebar-scroll::-webkit-scrollbar-track {
    background: transparent;
}
.chat-messages-scroll::-webkit-scrollbar-thumb,
.chat-sidebar-scroll::-webkit-scrollbar-thumb {
    background: var(--border-base);
    border-radius: 4px;
}
.chat-messages-scroll::-webkit-scrollbar-thumb:hover,
.chat-sidebar-scroll::-webkit-scrollbar-thumb:hover {
    background: var(--text-placeholder);
}

/* 参考图布局：颜色跟随站点主题变量 */
.chat-panel {
    --chat-primary: var(--color-primary);
    --chat-primary-2: var(--color-accent);
    --chat-mobile-composer-height: 190px;
    width: 100%;
    min-height: 0;
    background: var(--bg-base);
    border: 1px solid var(--border-base);
    border-radius: 0;
    box-shadow: none;
}

.chat-main {
    background: var(--bg-base);
}

.chat-sidebar-backdrop {
    display: none;
}

.assistant-avatar {
    border-radius: 50%;
    background: var(--color-primary);
    box-shadow: var(--shadow-md);
}

.assistant-avatar-sm {
    width: 36px;
    height: 36px;
}

.user-avatar {
    width: 38px;
    height: 38px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #fff;
    font-size: 12px;
    font-weight: 800;
    color: var(--text-heading);
    background: var(--bg-active);
    box-shadow: var(--shadow-sm);
}

.chat-messages-scroll {
    padding-bottom: 18px;
}

.chat-scroll-latest-btn {
    bottom: 9.5rem;
}

.chat-ai-bubble {
    max-width: 100%;
    padding: 0;
    border-radius: 0;
    border: 0;
    background: transparent;
    box-shadow: none;
}

.user-bubble {
    border-radius: 18px;
    border-top-right-radius: 6px;
    background: var(--chat-primary);
    box-shadow: 0 14px 30px rgba(15, 23, 42, 0.14);
}

.rag-step-list,
.rag-source-panel {
    border-radius: 8px;
    border-color: var(--border-base);
    background: var(--bg-hover);
}

.ai-follow-up-list button {
    border-radius: 999px;
    background: var(--bg-hover);
}

.chat-composer {
    border-top: 0;
    background: linear-gradient(180deg, transparent, var(--bg-base) 34%);
}

.chat-content-shell {
    max-width: 1280px;
}

.chat-composer-content {
    max-width: 900px;
}

.chat-empty-state {
    min-height: min(58vh, 620px);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    text-align: center;
    gap: 0.9rem;
    padding: 2.2rem 0 1.2rem;
}

.chat-empty-badge {
    display: inline-flex;
    align-items: center;
    min-height: 2rem;
    padding: 0 0.85rem;
    border-radius: 999px;
    border: 1px solid color-mix(in srgb, var(--color-primary) 16%, var(--border-base));
    background: color-mix(in srgb, var(--bg-card) 90%, transparent);
    color: var(--text-secondary);
    font-size: 0.78rem;
    font-weight: 700;
}

.chat-empty-title {
    margin: 0;
    color: var(--text-heading);
    font-size: clamp(1.85rem, 2.2vw, 2.65rem);
    line-height: 1.16;
    font-weight: 800;
}

.chat-empty-description {
    max-width: 640px;
    margin: 0;
    color: var(--text-muted);
    font-size: 0.98rem;
    line-height: 1.75;
}

.chat-empty-prompt-grid {
    width: 100%;
    max-width: 760px;
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.95rem;
    margin-top: 1rem;
}

.quick-prompt-card {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    min-height: 104px;
    padding: 1.05rem 1.15rem;
    border-radius: 18px;
    border: 1px solid var(--border-base);
    background: color-mix(in srgb, var(--bg-card) 94%, transparent);
    box-shadow: 0 14px 30px rgba(15, 23, 42, 0.06);
    text-align: left;
}

.chat-empty-prompt-text {
    color: var(--text-body);
    font-size: 0.96rem;
    line-height: 1.6;
    font-weight: 600;
}

.chat-empty-prompt-icon {
    width: 1rem;
    height: 1rem;
    margin-top: 0.2rem;
    color: var(--text-placeholder);
    flex-shrink: 0;
}

.chat-composer {
    border-top: 0;
    background: linear-gradient(180deg, transparent, rgba(255, 255, 255, 0.8) 18%, var(--bg-base) 46%);
}

.chat-composer-shell {
    border-radius: 20px;
    border: 1px solid color-mix(in srgb, var(--border-base) 92%, white);
    background: color-mix(in srgb, var(--bg-card) 97%, transparent);
    box-shadow: 0 14px 32px rgba(15, 23, 42, 0.07);
    padding: 0.7rem 0.8rem;
}

.chat-composer-toolbar {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.55rem;
    padding-bottom: 0.55rem;
}

.chat-composer-tool,
.chat-kb-select {
    display: inline-flex;
    align-items: center;
    gap: 0.55rem;
    min-height: 2.08rem;
    padding: 0 0.72rem;
    border-radius: 999px;
    border: 1px solid var(--border-base);
    background: var(--bg-base);
    color: var(--text-body);
    font-size: 0.84rem;
}

.chat-composer-tool {
    transition: all 0.18s ease;
}

.chat-composer-tool:hover {
    border-color: color-mix(in srgb, var(--color-primary) 28%, var(--border-base));
    color: var(--text-heading);
    background: var(--bg-hover);
}

.chat-composer-tool-primary {
    max-width: 260px;
}

.chat-composer-tool-label {
    color: var(--text-muted);
    font-size: 0.73rem;
    font-weight: 700;
}

.chat-mode-pill-group {
    display: inline-flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.45rem;
}

.chat-mode-pill {
    min-height: 2.08rem;
    padding: 0 0.78rem;
    border-radius: 999px;
    border: 1px solid transparent;
    background: transparent;
    color: var(--text-muted);
    font-size: 0.82rem;
    font-weight: 700;
    transition: all 0.18s ease;
}

.chat-mode-pill:hover {
    color: var(--text-heading);
    background: var(--bg-hover);
}

.chat-mode-pill-active {
    color: var(--text-heading);
    border-color: color-mix(in srgb, var(--color-primary) 20%, var(--border-base));
    background: color-mix(in srgb, var(--color-primary) 8%, var(--bg-card));
    box-shadow: 0 10px 22px rgba(59, 130, 246, 0.12);
}

.chat-kb-select {
    min-width: 0;
}

.chat-kb-select select {
    min-width: 170px;
    border: 0;
    outline: 0;
    background: transparent;
    color: var(--text-body);
    font-size: 0.84rem;
}

.chat-kb-select.is-disabled {
    opacity: 0.56;
}

.chat-input-wrapper {
    border-radius: 20px;
    border: 1px solid transparent;
    background: transparent;
    box-shadow: none;
}

.chat-input-wrapper:focus-within {
    border-color: transparent;
    box-shadow: none;
}

.chat-input-textarea {
    width: 100%;
    min-height: 58px;
    max-height: 132px;
    padding: 0.25rem 0.15rem 0.2rem;
    border: 0;
    outline: 0;
    resize: none;
    background: transparent;
    color: var(--text-body);
    font-size: 0.95rem;
    line-height: 1.55;
    overflow-y: auto;
}

.chat-input-textarea::placeholder {
    color: var(--text-placeholder);
}

.chat-composer-footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-top: 0.25rem;
}

.chat-composer-meta,
.chat-composer-actions {
    display: flex;
    align-items: center;
    gap: 0.55rem;
    min-width: 0;
}

.chat-composer-icon-btn {
    width: 2rem;
    height: 2rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 999px;
    border: 1px solid transparent;
    background: transparent;
    color: var(--text-muted);
    transition: all 0.18s ease;
}

.chat-composer-icon-btn:hover {
    color: var(--text-heading);
    border-color: var(--border-base);
    background: var(--bg-hover);
}

.chat-composer-state-pill,
.chat-composer-token-pill {
    display: inline-flex;
    align-items: center;
    min-height: 1.8rem;
    padding: 0 0.65rem;
    border-radius: 999px;
    background: var(--bg-hover);
    color: var(--text-secondary);
    font-size: 0.76rem;
    white-space: nowrap;
}

.chat-composer-token-pill {
    color: var(--text-muted);
}

.chat-composer-token-pill-danger {
    background: rgba(239, 68, 68, 0.12);
    color: #dc2626;
}

.chat-composer-counter {
    color: var(--text-placeholder);
    font-size: 0.78rem;
    font-variant-numeric: tabular-nums;
}

.chat-send-btn {
    width: 36px;
    height: 36px;
    border-radius: 999px;
}

.chat-send-btn.bg-\[var\(--color-primary\)\] {
    background: var(--chat-primary) !important;
}

@media (max-width: 768px) {
    .chat-panel {
        position: relative;
        --chat-mobile-composer-height: 178px;
    }

    .chat-messages-scroll {
        padding-left: 0.9rem;
        padding-right: 0.9rem;
        padding-bottom: 0.65rem;
    }

    .chat-composer {
        padding: 0.35rem 0.65rem calc(0.55rem + env(safe-area-inset-bottom)) !important;
    }

    .chat-composer-content {
        max-width: 100%;
    }

    .chat-usage-hint {
        margin-bottom: 0.3rem !important;
        font-size: 0.68rem;
        line-height: 1.2;
    }

    .chat-scroll-latest-btn {
        bottom: calc(var(--chat-mobile-composer-height) + 0.75rem);
        padding: 0.42rem 0.72rem;
        font-size: 0.75rem;
        box-shadow: 0 10px 24px rgba(15, 23, 42, 0.24);
    }

    .chat-message {
        gap: 0.58rem;
        margin-bottom: 1.35rem;
    }

    .chat-ai-entry {
        max-width: calc(100% - 42px);
    }

    .assistant-avatar-sm {
        width: 32px;
        height: 32px;
    }

    .chat-ai-head {
        margin-bottom: 0.42rem;
    }

    .chat-ai-name {
        font-size: 0.9rem;
    }

    .chat-ai-time {
        font-size: 0.7rem;
    }

    .user-avatar {
        width: 34px;
        height: 34px;
    }

    .user-bubble {
        padding: 0.65rem 0.85rem;
        border-radius: 16px;
        border-top-right-radius: 6px;
    }

    .chat-sidebar-backdrop {
        display: block;
        position: absolute;
        inset: 0;
        z-index: 20;
        background: rgba(15, 23, 42, 0.28);
        backdrop-filter: blur(2px);
    }

    .chat-empty-state {
        min-height: 48vh;
        align-items: flex-start;
        text-align: left;
        padding-top: 1rem;
    }

    .chat-empty-title,
    .chat-empty-description {
        max-width: 100%;
    }

    .chat-empty-prompt-grid {
        grid-template-columns: 1fr;
    }

    .chat-composer-shell {
        border-radius: 14px;
        padding: 0.48rem;
        box-shadow: 0 10px 26px rgba(15, 23, 42, 0.07);
    }

    .chat-composer-toolbar {
        display: grid;
        grid-template-columns: minmax(0, 1fr);
        gap: 0.36rem;
    }

    .chat-composer-footer {
        flex-direction: row;
        align-items: center;
        gap: 0.38rem;
    }

    .chat-composer-tool,
    .chat-kb-select {
        min-height: 1.82rem;
        padding: 0 0.58rem;
        font-size: 0.76rem;
    }

    .chat-composer-tool-label {
        white-space: nowrap;
    }

    .chat-composer-tool-primary {
        width: 100%;
        max-width: none;
        justify-content: space-between;
    }

    .chat-kb-select {
        display: grid;
        grid-template-columns: auto minmax(0, 1fr);
        width: 100%;
    }

    .chat-kb-select select {
        min-width: 0;
        width: 100%;
        font-size: 0.78rem;
        text-overflow: ellipsis;
    }

    .chat-mode-pill-group {
        width: 100%;
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: 0.35rem;
    }

    .chat-mode-pill {
        min-height: 1.82rem;
        padding: 0 0.35rem;
        font-size: 0.74rem;
        justify-content: center;
    }

    .chat-input-textarea {
        min-height: 38px;
        max-height: 92px;
        padding-top: 0.18rem;
        padding-bottom: 0.12rem;
        font-size: 0.9rem;
        line-height: 1.5;
    }

    .chat-composer-footer {
        margin-top: 0.12rem;
    }

    .chat-composer-meta {
        flex: 1 1 auto;
        overflow: hidden;
    }

    .chat-composer-state-pill {
        max-width: 100%;
        overflow: hidden;
        text-overflow: ellipsis;
        min-height: 1.65rem;
        padding: 0 0.58rem;
        font-size: 0.72rem;
    }

    .chat-composer-actions {
        flex-shrink: 0;
        justify-content: flex-end;
    }

    .chat-send-btn {
        width: 32px;
        height: 32px;
    }
}
</style>

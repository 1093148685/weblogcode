<template>
    <div class="flex h-full">
        <!-- 侧边栏 -->
        <transition name="slide">
            <div
                v-if="showSidebar"
                class="w-72 flex-shrink-0 flex flex-col bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 h-full"
            >
                <!-- 侧边栏头部 -->
                <div class="p-3 border-b border-gray-200 dark:border-gray-700">
                    <div class="flex items-center justify-between mb-2">
                        <span class="text-sm font-medium text-gray-500 dark:text-gray-400">会话列表</span>
                    </div>
                    <el-button type="primary" class="w-full" @click="createNewChat" :icon="Plus">
                        新建会话
                    </el-button>
                </div>

                <!-- 会话列表 -->
                <div class="flex-1 overflow-y-auto p-2">
                    <div
                        v-for="session in chatSessions"
                        :key="session.id"
                        class="group mb-1 p-2 rounded-lg cursor-pointer transition-colors flex items-center justify-between"
                        :class="currentSessionId === session.id
                            ? 'bg-blue-50 dark:bg-blue-900/30'
                            : 'hover:bg-gray-100 dark:hover:bg-gray-700'"
                        @click="switchSession(session.id)"
                    >
                        <div class="flex-1 min-w-0">
                            <span class="block text-sm text-gray-700 dark:text-gray-200 truncate">{{ session.title }}</span>
                            <span class="text-xs text-gray-400">{{ session.time }}</span>
                        </div>
                        <el-button
                            text
                            size="small"
                            class="!p-1 opacity-0 group-hover:opacity-100 flex-shrink-0 ml-1"
                            @click.stop="deleteSession(session.id)"
                        >
                            <el-icon class="text-gray-400 hover:text-red-500"><Delete /></el-icon>
                        </el-button>
                    </div>
                    <div v-if="chatSessions.length === 0" class="text-center text-gray-400 text-sm py-8">
                        暂无会话记录
                    </div>
                </div>
            </div>
        </transition>

        <!-- 主内容区 -->
        <div class="flex-1 flex flex-col min-w-0">
            <!-- 标题栏 -->
            <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-700">
                <div class="flex items-center gap-2">
                    <el-button text @click="showSidebar = !showSidebar" class="!p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full">
                        <el-icon :size="18" class="text-gray-500 dark:text-gray-400">
                            <DArrowRight v-if="!showSidebar" /><DArrowLeft v-else />
                        </el-icon>
                    </el-button>
                    <div class="w-7 h-7 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center">
                        <span class="text-white text-xs font-bold">J</span>
                    </div>
                    <div class="flex flex-col">
                        <span class="font-medium text-gray-700 dark:text-gray-200 text-sm">小J智能 AI 助手</span>
                        <span v-if="currentSessionTitle && currentSessionTitle !== '新会话'" class="text-xs text-gray-400 truncate max-w-[200px]">
                            {{ currentSessionTitle }}
                        </span>
                    </div>
                </div>

                <div class="flex items-center gap-1">
                    <!-- 模型显示 -->
                    <el-tag size="small" type="info" class="cursor-pointer" @click="showSettings = true">
                        {{ currentModelName }}
                    </el-tag>
                    <el-button text @click="showSettings = true" class="!p-1.5 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-full">
                        <el-icon :size="18" class="text-gray-500 dark:text-gray-400"><Setting /></el-icon>
                    </el-button>
                </div>
            </div>

            <!-- 消息区域 -->
            <div class="flex-1 overflow-y-auto px-4 pb-2" ref="chatContainer">
                <div class="max-w-3xl mx-auto pt-4">
                    <!-- 欢迎消息 -->
                    <div v-if="displayMessages.length === 0" class="mb-6">
                        <div class="flex items-start gap-3">
                            <div class="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center flex-shrink-0">
                                <span class="text-white text-sm font-bold">J</span>
                            </div>
                            <div class="p-3 bg-gray-50 dark:bg-gray-800 rounded-2xl rounded-tl-none max-w-[85%]">
                                <p class="text-sm text-gray-700 dark:text-gray-300 leading-relaxed">
                                    你好！我是<strong class="text-blue-500">小J</strong>，博客智能问答助手
                                </p>
                                <p class="text-sm text-gray-500 dark:text-gray-400 mt-2 leading-relaxed">我可以帮你：</p>
                                <ul class="text-sm text-gray-500 dark:text-gray-400 mt-1 ml-4 list-disc list-inside space-y-0.5">
                                    <li>解答关于博客内容的问题</li>
                                    <li>推荐相关文章</li>
                                    <li>讨论你感兴趣的话题</li>
                                </ul>
                                <p class="text-sm text-gray-500 dark:text-gray-400 mt-2">有什么我可以帮你的吗？</p>
                            </div>
                        </div>

                        <div class="mt-4 ml-11">
                            <div class="flex flex-wrap gap-2">
                                <el-tag
                                    v-for="prompt in quickPrompts"
                                    :key="prompt"
                                    size="small"
                                    effect="plain"
                                    class="cursor-pointer hover:bg-blue-50 dark:hover:bg-blue-900/30 hover:border-blue-400 transition-colors"
                                    @click="usePrompt(prompt)"
                                >
                                    {{ prompt }}
                                </el-tag>
                            </div>
                        </div>
                    </div>

                    <!-- 聊天记录 -->
                    <template v-else>
                        <template v-for="(chat, index) in displayMessages" :key="index">
                            <!-- 用户消息 -->
                            <div v-if="chat.role === 'user'" class="flex justify-end mb-4">
                                <div class="max-w-[75%]">
                                    <div class="bg-blue-500 text-white px-4 py-2.5 rounded-2xl rounded-br-sm">
                                        <div v-if="chat.quotedArticleTitle" class="mb-2 pb-2 border-b border-blue-400 border-opacity-30">
                                            <div class="flex items-center gap-1 text-xs text-blue-200">
                                                <el-icon><Document /></el-icon>
                                                <span>引用文章: {{ chat.quotedArticleTitle }}</span>
                                            </div>
                                        </div>
                                        <p class="text-sm leading-relaxed whitespace-pre-wrap">{{ chat.content }}</p>
                                    </div>
                                    <p class="text-xs text-gray-400 mt-1 text-right">{{ formatTime(chat.timestamp) }}</p>
                                </div>
                            </div>

                            <!-- AI 回复 -->
                            <div v-else class="flex mb-4">
                                <div class="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-purple-500 flex items-center justify-center flex-shrink-0 mr-2.5 mt-0.5">
                                    <span class="text-white text-xs font-bold">J</span>
                                </div>
                                <div class="max-w-[80%]">
                                    <div class="px-4 py-2.5 rounded-2xl rounded-tl-sm bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 text-sm">
                                        <!-- 流式加载中 -->
                                        <div v-if="!chat.content && index === displayMessages.length - 1 && isStreaming" class="flex items-center gap-1.5 py-1">
                                            <span class="w-1.5 h-1.5 bg-blue-400 rounded-full animate-bounce" style="animation-delay:0s"></span>
                                            <span class="w-1.5 h-1.5 bg-blue-400 rounded-full animate-bounce" style="animation-delay:0.15s"></span>
                                            <span class="w-1.5 h-1.5 bg-blue-400 rounded-full animate-bounce" style="animation-delay:0.3s"></span>
                                        </div>
                                        <StreamMarkdownRender v-else :content="chat.content" />
                                    </div>
                                    <p class="text-xs text-gray-400 mt-1">{{ formatTime(chat.timestamp) }}</p>
                                </div>
                            </div>
                        </template>
                    </template>
                </div>
            </div>

            <!-- 输入区域 -->
            <div class="px-4 pb-4 pt-2 border-t border-gray-100 dark:border-gray-700">
                <div class="max-w-3xl mx-auto">
                    <!-- 使用次数提示 -->
                    <div
                        v-if="usageInfo && !usageInfo.isAdmin && !usageInfo.IsAdmin"
                        class="mb-2 text-xs text-center"
                        :class="(usageInfo.remaining === 0 || usageInfo.Remaining === 0) ? 'text-red-500' : 'text-gray-400 dark:text-gray-500'"
                    >
                        {{ usageInfo.message || usageInfo.Message }}
                    </div>

                    <!-- 引用文章提示 -->
                    <div v-if="quotedArticle" class="mb-2 p-2 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">
                        <div class="flex items-center justify-between">
                            <div class="flex items-center gap-2">
                                <el-icon class="text-blue-500"><Document /></el-icon>
                                <span class="text-sm text-blue-700 dark:text-blue-300 truncate max-w-[200px]">{{ quotedArticle.title }}</span>
                            </div>
                            <button @click="quotedArticle = null" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                                <el-icon><Close /></el-icon>
                            </button>
                        </div>
                    </div>

                    <!-- 输入框 -->
                    <div class="relative bg-white dark:bg-gray-800 rounded-xl border border-gray-300 dark:border-gray-600 focus-within:border-blue-500 transition-colors shadow-sm">
                        <textarea
                            ref="textareaRef"
                            v-model="inputText"
                            @input="autoResize"
                            @keydown.enter.exact.prevent="sendMessage"
                            @keydown.shift.enter.exact="handleShiftEnter"
                            placeholder="输入消息，Enter 发送，Shift+Enter 换行..."
                            class="w-full px-4 py-3 pr-28 bg-transparent border-none outline-none resize-none text-sm text-gray-800 dark:text-gray-200 placeholder-gray-400"
                            rows="2"
                            maxlength="2000"
                        ></textarea>

                        <div class="absolute right-2 bottom-2 flex items-center gap-1">
                            <el-button @click="showArticleDialog = true" class="!h-8 !px-2" title="引用文章">
                                <el-icon class="text-gray-500 dark:text-gray-300"><Document /></el-icon>
                            </el-button>
                            <span class="text-xs text-gray-400 mx-1">{{ inputText.length }}/2000</span>
                            <el-button
                                v-if="!isStreaming"
                                type="primary"
                                :disabled="!inputText.trim()"
                                @click="sendMessage"
                                class="!w-8 !h-8 !rounded-lg !p-0"
                            >
                                <el-icon><Promotion /></el-icon>
                            </el-button>
                            <el-button
                                v-else
                                type="danger"
                                @click="stopStreaming"
                                class="!w-8 !h-8 !rounded-lg !p-0"
                                title="停止生成"
                            >
                                <el-icon><VideoPause /></el-icon>
                            </el-button>
                        </div>
                    </div>

                    <div class="text-center text-xs text-gray-400 mt-2">内容由 AI 生成，请仔细甄别</div>
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
                class="p-3 mb-2 rounded-lg border border-gray-200 dark:border-gray-700 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
            >
                <h4 class="font-medium text-gray-800 dark:text-gray-200 truncate">{{ article.title || article.Title }}</h4>
                <p v-if="article.summary || article.Summary" class="text-sm text-gray-500 dark:text-gray-400 mt-1 line-clamp-2">
                    {{ article.summary || article.Summary }}
                </p>
            </div>
            <div v-if="filteredArticles.length === 0" class="text-center text-gray-400 py-8">
                暂无文章
            </div>
        </div>
    </el-dialog>
</template>

<script setup>
import { ref, computed, nextTick, onMounted } from 'vue'
import {
    Setting, Promotion, Document, Close, Search, Plus,
    DArrowLeft, DArrowRight, VideoPause, Delete
} from '@element-plus/icons-vue'
import axios from '@/axios'
import { getToken } from '@/composables/cookie'
import { getArticleDetail } from '@/api/frontend/article'
import { saveSession, getUserSessions, deleteUserSession, getUsageInfo } from '@/api/frontend/chat'
import StreamMarkdownRender from './StreamMarkdownRender.vue'
import ModelSettings from './ModelSettings.vue'
import { ElMessage, ElMessageBox } from 'element-plus'

defineOptions({ name: 'ChatPanel' })

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
const showSettings = ref(false)
const showSidebar = ref(true)
const showArticleDialog = ref(false)
const articleSearch = ref('')
const quotedArticle = ref(null)
const allArticles = ref([])
const usageInfo = ref(null)
const chatSessions = ref([])
const currentSessionId = ref(null)
let abortController = null

// 流式传输时的临时状态
const streamingSessionId = ref(null)
const streamingMessages = ref(null)

// ──────── 当前选中的模型 ────────
const selectedModelId = ref('deepseek-chat')

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

const currentModelName = computed(() => {
    const m = modelOptions.value.find(o => o.id === selectedModelId.value)
    return m?.name || selectedModelId.value
})

const quickPrompts = ['推荐几篇技术文章', '帮我解释一个概念', '有什么编程建议？', '帮我写一首诗']

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
            chatSessions.value = res.data.map(s => ({
                id: s.sessionId,
                title: s.title || '新会话',
                time: formatDbTime(s.updatedAt),
                messages: safeParseMessages(s.messages),
                model: s.model
            }))
            if (chatSessions.value.length > 0 && !currentSessionId.value) {
                currentSessionId.value = chatSessions.value[0].id
            }
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
        return d.toLocaleString('zh-CN', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
    } catch {
        return timeStr
    }
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
                quotedArticleTitle: m.quotedArticleTitle || null
            }))),
            model: model || selectedModelId.value,
            provider: ''
        })
    } catch (e) {
        console.error('保存会话失败:', e)
    }
}

const createNewChat = () => {
    if (isStreaming.value) return
    const newId = 'session_' + Date.now()
    const newSession = {
        id: newId,
        title: '新会话',
        time: new Date().toLocaleString('zh-CN', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }),
        messages: [],
        model: selectedModelId.value
    }
    chatSessions.value.unshift(newSession)
    currentSessionId.value = newId
}

const switchSession = (sessionId) => {
    if (isStreaming.value) return
    currentSessionId.value = sessionId
}

const deleteSession = async (sessionId) => {
    try {
        await ElMessageBox.confirm('确定删除该会话吗？', '提示', { type: 'warning' })
        await deleteUserSession(sessionId, clientId)
        chatSessions.value = chatSessions.value.filter(s => s.id !== sessionId)
        if (currentSessionId.value === sessionId) {
            currentSessionId.value = chatSessions.value[0]?.id || null
        }
        ElMessage.success('删除成功')
    } catch (e) {
        if (e !== 'cancel') ElMessage.error('删除失败')
    }
}

const updateSessionLocally = (sessionId, messages) => {
    const session = chatSessions.value.find(s => s.id === sessionId)
    if (session) {
        session.messages = [...messages]
        const firstUser = messages.find(m => m.role === 'user')
        if (firstUser) {
            const content = firstUser.content.split('\n\n').pop() || firstUser.content
            session.title = content.slice(0, 25) + (content.length > 25 ? '...' : '')
        }
        session.time = new Date().toLocaleString('zh-CN', {
            month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
        })
    }
}

// ──────── 发送消息 ────────
const sendMessage = async () => {
    if (!inputText.value.trim() || isStreaming.value) return

    // 如果没有会话，先创建一个
    if (!currentSessionId.value) {
        createNewChat()
    }

    let userContent = inputText.value.trim()
    const currentQuotedArticle = quotedArticle.value
    quotedArticle.value = null
    inputText.value = ''
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
        timestamp: Date.now()
    })
    sessionMessages.push({
        role: 'assistant',
        content: '',
        timestamp: Date.now()
    })

    // 更新本地会话标题
    if (session) {
        const displayContent = currentQuotedArticle
            ? userContent.split('\n\n')[1] || userContent
            : userContent
        session.title = displayContent.slice(0, 25) + (displayContent.length > 25 ? '...' : '')
        session.time = new Date().toLocaleString('zh-CN', {
            month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
        })
    }

    streamingSessionId.value = sessionId
    streamingMessages.value = sessionMessages
    scrollToBottom()

    isStreaming.value = true
    abortController = new AbortController()

    try {
        const token = getToken() || ''
        const response = await fetch('/api/ai/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': token ? 'Bearer ' + token : ''
            },
            signal: abortController.signal,
            body: JSON.stringify({
                messages: sessionMessages.slice(0, -1).map(m => ({ role: m.role, content: m.content })),
                model: selectedModelId.value,
                sessionId: sessionId,
                clientId: clientId,
                articleContent: currentQuotedArticle
                    ? (currentQuotedArticle.content || currentQuotedArticle.Content || currentQuotedArticle.summary || '')
                    : null,
                articleTitle: currentQuotedArticle?.title || currentQuotedArticle?.Title || null
            })
        })

        const reader = response.body.getReader()
        const decoder = new TextDecoder()
        let responseText = ''

        while (true) {
            const { done, value } = await reader.read()
            if (done) break

            for (const line of decoder.decode(value, { stream: true }).split('\n')) {
                if (!line.startsWith('data: ')) continue
                const raw = line.slice(6)
                if (raw === '[DONE]') continue
                try {
                    const parsed = JSON.parse(raw)
                    if (parsed.content) {
                        responseText += parsed.content
                        streamingMessages.value[streamingMessages.value.length - 1].content = responseText
                        scrollToBottom()
                    }
                    if (parsed.error) {
                        streamingMessages.value[streamingMessages.value.length - 1].content = '❌ ' + parsed.error
                        await loadUsageInfo()
                    }
                } catch { /* 忽略解析错误 */ }
            }
        }
    } catch (error) {
        if (error.name === 'AbortError') {
            const lastMsg = streamingMessages.value[streamingMessages.value.length - 1]
            lastMsg.content += (lastMsg.content ? '\n\n' : '') + '[ 已停止生成 ]'
        } else {
            console.error('流式请求出错:', error)
            streamingMessages.value[streamingMessages.value.length - 1].content = '抱歉，请求出错了，请稍后重试。'
        }
    } finally {
        isStreaming.value = false
        abortController = null

        // 将完整会话同步到本地状态 & 保存到数据库
        const finalMessages = [...(streamingMessages.value || [])]
        updateSessionLocally(streamingSessionId.value, finalMessages)
        await saveSessionToDb(streamingSessionId.value, finalMessages, selectedModelId.value)

        streamingSessionId.value = null
        streamingMessages.value = null

        scrollToBottom()
        await loadUsageInfo()
    }
}

const stopStreaming = () => {
    if (abortController) abortController.abort()
}

// ──────── 辅助函数 ────────
const scrollToBottom = async () => {
    await nextTick()
    if (chatContainer.value) {
        chatContainer.value.scrollTop = chatContainer.value.scrollHeight
    }
}

const autoResize = () => {
    if (textareaRef.value) {
        textareaRef.value.style.height = 'auto'
        textareaRef.value.style.height = Math.min(textareaRef.value.scrollHeight, 200) + 'px'
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
    transition: all 0.2s ease;
    overflow: hidden;
}
.slide-enter-from,
.slide-leave-to {
    width: 0 !important;
    opacity: 0;
}
</style>

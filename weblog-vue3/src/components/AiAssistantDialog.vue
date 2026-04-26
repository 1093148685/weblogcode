<template>
    <el-dialog 
        v-model="dialogVisible" 
        width="620px"
        :close-on-click-modal="false" 
        class="ai-assistant-dialog"
        destroy-on-close
    >
        <template #header>
            <div class="dialog-header">
                <div class="header-left">
                    <span class="dialog-title">AI 写作助手</span>
                    <el-icon class="settings-btn" @click="showSettings = true"><Setting /></el-icon>
                </div>
                <div class="header-actions">
                    <!-- 会话列表 -->
                    <el-dropdown trigger="click" @command="handleSelectSession">
                        <el-button size="small">
                            <span class="current-session-name">{{ currentSessionName }}</span>
                            <el-icon class="el-icon--right"><ArrowDown /></el-icon>
                        </el-button>
                        <template #dropdown>
                            <el-dropdown-menu>
                                <el-dropdown-item 
                                    v-for="session in sessions" 
                                    :key="session.id" 
                                    :command="session.id"
                                    :class="{ 'is-active': session.id === currentSessionId }"
                                >
                                    <div class="session-item">
                                        <span class="session-title">{{ session.title }}</span>
                                        <el-icon v-if="session.id === currentSessionId" class="check-icon"><Check /></el-icon>
                                    </div>
                                </el-dropdown-item>
                            </el-dropdown-menu>
                        </template>
                    </el-dropdown>
                    
                    <el-button size="small" @click="handleNewConversation">
                        <el-icon><Plus /></el-icon>
                        新建会话
                    </el-button>
                    
                    <el-button size="small" @click="handleClearConversation" :disabled="conversations.length === 0">
                        <el-icon><Delete /></el-icon>
                        清空
                    </el-button>
                </div>
            </div>
        </template>

        <div class="ai-container">
            <!-- 对话历史区域 -->
            <div class="conversation-area" ref="conversationRef">
                <!-- 空状态 -->
                <div v-if="conversations.length === 0" class="empty-state">
                    <div class="empty-icon">
                        <el-icon :size="36"><MagicStick /></el-icon>
                    </div>
                    <p class="empty-text">有什么可以帮你的吗？</p>
                    <p class="empty-hint">试试点击下方模板快速开始</p>
                </div>

                <!-- 对话列表 -->
                <div v-else class="conversation-list">
                    <div 
                        v-for="(msg, index) in conversations" 
                        :key="index"
                        :class="['message-item', msg.role]"
                    >
                        <div class="message-avatar">
                            <el-icon v-if="msg.role === 'user'" :size="16"><UserFilled /></el-icon>
                            <el-icon v-else :size="16"><MagicStick /></el-icon>
                        </div>
                        <div class="message-content">
                            <div class="message-bubble" v-html="msg.role === 'assistant' ? renderMarkdown(msg.content) : escapeHtml(msg.content)"></div>
                            <div class="message-footer">
                                <span class="message-time">{{ formatTime(msg.timestamp) }}</span>
                                <span v-if="msg.role === 'assistant'" class="word-count">{{ countWords(msg.content) }} 字</span>
                            </div>
                            <!-- AI 消息操作按钮 -->
                            <div v-if="msg.role === 'assistant' && index === conversations.length - 1 && !generating" class="message-actions">
                                <el-button size="small" @click="handleRegenerate">
                                    <el-icon><RefreshRight /></el-icon>
                                    重新生成
                                </el-button>
                                <el-button size="small" @click="handleCopyContent(msg.content)">
                                    <el-icon><DocumentCopy /></el-icon>
                                    复制全文
                                </el-button>
                                <el-button size="small" type="primary" @click="handleAppendToEditor(msg.content)">
                                    <el-icon><Plus /></el-icon>
                                    追加到编辑器
                                </el-button>
                                <el-button size="small" @click="handleReplaceEditor(msg.content)">
                                    替换正文
                                </el-button>
                                <el-button size="small" @click="handleFillSummary(msg.content)">
                                    填入摘要
                                </el-button>
                                <el-button size="small" @click="handleFillTitle(msg.content)">
                                    应用标题
                                </el-button>
                                <el-button size="small" @click="handleApplySeo(msg.content)">
                                    应用 SEO
                                </el-button>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- AI 思考动画 -->
                <div v-if="generating" class="message-item assistant thinking">
                    <div class="message-avatar">
                        <el-icon :size="16"><MagicStick /></el-icon>
                    </div>
                    <div class="message-content">
                        <div class="message-bubble thinking-bubble">
                            <span class="thinking-dot"></span>
                            <span class="thinking-dot"></span>
                            <span class="thinking-dot"></span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- 剩余次数提示：管理员不受限制，不显示 -->
            <div v-if="!isAdmin && conversations.length > 0 && conversations.length < MAX_CONVERSATIONS" class="remaining-hint">
                <span v-if="remainingCount <= 3" class="remaining-warning">
                    <el-icon><Warning /></el-icon>
                    剩余 {{ remainingCount }} 次对话
                </span>
                <span v-else class="remaining-normal">
                    剩余 {{ remainingCount }} 次对话
                </span>
            </div>

            <!-- Token 限制警告：管理员不受限制 -->
            <div v-if="!isAdmin && conversations.length >= MAX_CONVERSATIONS" class="token-warning">
                <el-icon><Warning /></el-icon>
                <span>对话次数已用完，请新建会话</span>
            </div>

            <!-- 快速模板 -->
            <div class="template-section">
                <div class="template-scroll">
                    <el-tag 
                        v-for="(template, index) in templates" 
                        :key="index"
                        class="template-tag"
                        @click="useTemplate(template)"
                    >
                        {{ template.title }}
                    </el-tag>
                </div>
            </div>

            <!-- 输入区域 -->
            <div class="input-section">
                <el-input
                    v-model="prompt"
                    type="textarea"
                    :rows="2"
                    :placeholder="generating ? 'AI 正在创作中，请稍候...' : '输入你的问题或创作需求，按 Enter 发送...'"
                    resize="none"
                    :disabled="generating"
                    @keydown.enter.prevent="handleEnter"
                    ref="inputRef"
                />
                <div class="input-actions">
                    <el-button 
                        v-if="!generating"
                        type="primary" 
                        @click="generateContent" 
                        :disabled="!prompt.trim() || (!isAdmin && conversations.length >= MAX_CONVERSATIONS)"
                        class="send-btn"
                    >
                        <el-icon><Promotion /></el-icon>
                        发送
                    </el-button>
                    <el-button 
                        v-else
                        type="danger" 
                        @click="handleStopGeneration"
                        class="stop-btn"
                    >
                        <el-icon><VideoPause /></el-icon>
                        停止
                    </el-button>
                </div>
            </div>
        </div>

        <!-- AI 设置弹窗 -->
        <AiSettingsDialog v-model="showSettings" @change="onSettingsChange" />
    </el-dialog>
</template>

<script setup>
import { ref, computed, watch, nextTick, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { MagicStick, UserFilled, Promotion, Warning, Setting, Plus, Delete, RefreshRight, DocumentCopy, VideoPause, ArrowDown, Check } from '@element-plus/icons-vue'
import { marked } from 'marked'
import { getToken } from '@/composables/cookie'
import { useUserStore } from '@/stores/user'
import AiSettingsDialog from './AiSettingsDialog.vue'

const userStore = useUserStore()
const isAdmin = computed(() => userStore.userInfo?.role === 'admin')

const props = defineProps({
    modelValue: {
        type: Boolean,
        default: false
    },
    initialPrompt: {
        type: String,
        default: ''
    }
})

const emit = defineEmits(['update:modelValue', 'insert-content'])

const dialogVisible = computed({
    get: () => props.modelValue,
    set: (val) => emit('update:modelValue', val)
})

const SESSIONS_STORAGE_KEY = 'ai_sessions'
const SETTINGS_KEY = 'ai_model_settings'
const MAX_CONVERSATIONS = 20

const prompt = ref('')
const generating = ref(false)
const conversations = ref([])
const conversationRef = ref(null)
const inputRef = ref(null)
const showSettings = ref(false)
const abortController = ref(null)

const sessions = ref([])
const currentSessionId = ref(null)

const templates = [
    { title: '生成大纲', prompt: '请根据文章主题生成一份技术博客大纲，包含层级标题、每节要点和建议示例。' },
    { title: '完整草稿', prompt: '请写一篇结构完整的技术博客，包含引言、核心概念、实践步骤、代码示例、常见问题和总结。' },
    { title: '续写内容', prompt: '请基于已有正文继续写，保持原有语气和 Markdown 结构，不要重复已有内容。' },
    { title: '润色改写', prompt: '请润色以下文本，使表达更清晰、更专业、更适合技术博客阅读，保留 Markdown 结构。' },
    { title: '摘要总结', prompt: '请为文章生成 80-140 字摘要，只输出摘要正文，不要输出多余解释。' },
    { title: 'SEO 标题', prompt: '请为文章生成 5 个 SEO 友好的标题，并说明每个标题适合的搜索关键词。' },
    { title: '关键词', prompt: '请根据文章内容生成 8-12 个 SEO 关键词，并按重要性排序。' },
    { title: '检查结构', prompt: '请检查文章结构、逻辑、可读性和技术表达，给出可执行的修改建议。' },
]

const remainingCount = computed(() => MAX_CONVERSATIONS - conversations.value.length)

const currentSessionName = computed(() => {
    if (!currentSessionId.value) return '新会话'
    const session = sessions.value.find(s => s.id === currentSessionId.value)
    if (!session) return '新会话'
    return session.title || '新会话'
})

const currentSettings = computed(() => {
    try {
        const saved = localStorage.getItem(SETTINGS_KEY)
        return saved ? JSON.parse(saved) : null
    } catch {
        return null
    }
})

onMounted(() => {
    loadSessions()
})

watch(dialogVisible, (newVal) => {
    if (newVal) {
        if (props.initialPrompt) {
            prompt.value = props.initialPrompt
        }
        nextTick(() => {
            scrollToBottom()
            inputRef.value?.focus()
        })
    }
})

const loadSessions = () => {
    try {
        const stored = localStorage.getItem(SESSIONS_STORAGE_KEY)
        if (stored) {
            const data = JSON.parse(stored)
            sessions.value = data.sessions || []
            currentSessionId.value = data.currentSessionId
            if (currentSessionId.value) {
                const session = sessions.value.find(s => s.id === currentSessionId.value)
                if (session) {
                    conversations.value = session.conversations || []
                } else {
                    currentSessionId.value = null
                    conversations.value = []
                }
            }
        }
    } catch (e) {
        console.error('加载会话失败:', e)
        sessions.value = []
        currentSessionId.value = null
        conversations.value = []
    }
}

const saveSessions = () => {
    try {
        const data = {
            sessions: sessions.value,
            currentSessionId: currentSessionId.value
        }
        localStorage.setItem(SESSIONS_STORAGE_KEY, JSON.stringify(data))
    } catch (e) {
        console.error('保存会话失败:', e)
    }
}

const createNewSession = () => {
    const newSession = {
        id: 'session_' + Date.now(),
        title: '新会话',
        conversations: [],
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
    }
    sessions.value.unshift(newSession)
    currentSessionId.value = newSession.id
    conversations.value = []
    saveSessions()
    return newSession
}

const handleSelectSession = (sessionId) => {
    if (sessionId === currentSessionId.value) return
    
    saveCurrentSession()
    
    currentSessionId.value = sessionId
    const session = sessions.value.find(s => s.id === sessionId)
    if (session) {
        conversations.value = session.conversations || []
    } else {
        conversations.value = []
    }
    saveSessions()
    
    nextTick(() => {
        scrollToBottom()
    })
}

const saveCurrentSession = () => {
    if (!currentSessionId.value) return
    
    const session = sessions.value.find(s => s.id === currentSessionId.value)
    if (session) {
        session.conversations = conversations.value
        session.updatedAt = new Date().toISOString()
        
        if (conversations.value.length > 0) {
            const firstUserMsg = conversations.value.find(c => c.role === 'user')
            if (firstUserMsg) {
                session.title = firstUserMsg.content.slice(0, 20) + (firstUserMsg.content.length > 20 ? '...' : '')
            }
        }
    }
}

const handleNewConversation = async () => {
    if (conversations.value.length >= MAX_CONVERSATIONS) {
        createNewSession()
        ElMessage.success('已创建新会话')
        return
    }
    
    if (conversations.value.length > 0) {
        try {
            await ElMessageBox.confirm(
                '确定要开启新对话吗？当前对话历史将保存到会话列表。',
                '新建会话',
                {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning'
                }
            )
        } catch {
            return
        }
    }
    
    saveCurrentSession()
    createNewSession()
    ElMessage.success('已创建新会话')
}

const handleClearConversation = async () => {
    try {
        await ElMessageBox.confirm(
            '确定要清除当前会话的所有对话吗？',
            '清空对话',
            {
                confirmButtonText: '确定清除',
                cancelButtonText: '取消',
                type: 'warning'
            }
        )
        conversations.value = []
        if (currentSessionId.value) {
            const session = sessions.value.find(s => s.id === currentSessionId.value)
            if (session) {
                session.conversations = []
            }
            saveSessions()
        }
        ElMessage.success('已清空对话')
    } catch {
    }
}

const handleStopGeneration = () => {
    if (abortController.value) {
        abortController.value.abort()
        abortController.value = null
        generating.value = false
        ElMessage.warning('已停止生成')
    }
}

const handleRegenerate = () => {
    if (conversations.value.length > 0) {
        const lastUserMsg = conversations.value[conversations.value.length - 2]
        if (lastUserMsg && lastUserMsg.role === 'user') {
            prompt.value = lastUserMsg.content
            conversations.value.pop()
            if (conversations.value.length > 0 && conversations.value[conversations.value.length - 1].role === 'assistant') {
                conversations.value.pop()
            }
            generateContent()
        }
    }
}

const handleCopyContent = async (content) => {
    try {
        await navigator.clipboard.writeText(content)
        ElMessage.success('已复制到剪贴板')
    } catch {
        ElMessage.error('复制失败')
    }
}

const handleAppendToEditor = (content) => {
    emit('insert-content', { action: 'append', content })
    ElMessage.success('已追加到正文')
}

const handleReplaceEditor = async (content) => {
    try {
        await ElMessageBox.confirm('确定用 AI 结果替换当前正文吗？', '替换正文', {
            confirmButtonText: '确定替换',
            cancelButtonText: '取消',
            type: 'warning'
        })
        emit('insert-content', { action: 'replace', content })
        ElMessage.success('已替换正文')
    } catch {
    }
}

const handleFillSummary = (content) => {
    emit('insert-content', { action: 'summary', content })
    ElMessage.success('已填入摘要')
}

const handleFillTitle = (content) => {
    emit('insert-content', { action: 'title', content })
    ElMessage.success('已应用标题')
}

const handleApplySeo = (content) => {
    emit('insert-content', { action: 'seo', content })
    ElMessage.success('已应用 SEO 建议')
}

const onSettingsChange = (settings) => {
    console.log('AI 设置已更改:', settings)
}

const useTemplate = (template) => {
    prompt.value = template.prompt
    nextTick(() => {
        inputRef.value?.focus()
    })
}

const escapeHtml = (text) => {
    const div = document.createElement('div')
    div.textContent = text
    return div.innerHTML
}

const renderMarkdown = (content) => {
    if (!content) return ''
    return marked(content, {
        breaks: true,
        gfm: true
    })
}

const countWords = (text) => {
    if (!text) return 0
    return text.replace(/\s/g, '').length
}

const formatTime = (timestamp) => {
    if (!timestamp) return ''
    const date = new Date(timestamp)
    const now = new Date()
    const isToday = date.toDateString() === now.toDateString()
    
    if (isToday) {
        return date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
    }
    return date.toLocaleDateString('zh-CN', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

const scrollToBottom = () => {
    nextTick(() => {
        if (conversationRef.value) {
            conversationRef.value.scrollTop = conversationRef.value.scrollHeight
        }
    })
}

const handleEnter = (e) => {
    if (e.shiftKey) {
        return
    }
    if (prompt.value.trim() && !generating.value && (isAdmin.value || conversations.value.length < MAX_CONVERSATIONS)) {
        generateContent()
    }
}

const generateContent = async () => {
    if (!prompt.value.trim()) {
        ElMessage.warning('请输入创作提示词')
        return
    }

    if (!isAdmin.value && conversations.value.length >= MAX_CONVERSATIONS) {
        ElMessage.warning('对话次数已用完，请新建会话')
        return
    }

    if (!currentSessionId.value) {
        createNewSession()
    }

    const userMessage = {
        role: 'user',
        content: prompt.value.trim(),
        timestamp: new Date().toISOString()
    }

    conversations.value.push(userMessage)
    saveCurrentSession()
    saveSessions()

    const currentPrompt = prompt.value.trim()
    prompt.value = ''
    generating.value = true
    scrollToBottom()

    abortController.value = new AbortController()

    try {
        const token = getToken()
        if (!token) {
            ElMessage.error('请先登录')
            generating.value = false
            return
        }

        const requestBody = { 
            prompt: currentPrompt,
            conversations: conversations.value.slice(0, -1).map(c => ({ role: c.role, content: c.content }))
        }

        if (currentSettings.value) {
            requestBody.serviceType = currentSettings.value.serviceType
            requestBody.modelName = currentSettings.value.modelName
        }

        const response = await fetch('/api/admin/ai-summary/generate/content', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + token
            },
            body: JSON.stringify(requestBody),
            signal: abortController.value.signal
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
                            ElMessage.error(data.error)
                            break
                        }
                        if (data.content) {
                            result += data.content
                            const lastMsg = conversations.value[conversations.value.length - 1]
                            if (lastMsg && lastMsg.role === 'user') {
                                conversations.value.push({
                                    role: 'assistant',
                                    content: result,
                                    timestamp: new Date().toISOString()
                                })
                            } else if (lastMsg && lastMsg.role === 'assistant') {
                                lastMsg.content = result
                            }
                            scrollToBottom()
                        }
                        if (data.done) {
                            saveCurrentSession()
                            saveSessions()
                            abortController.value = null
                        }
                    } catch (e) {
                    }
                }
            }
        }

        if (!result) {
            conversations.value.pop()
        } else {
            ElMessageBox.confirm('内容已生成，是否追加到编辑器？', '生成完成', {
                confirmButtonText: '是',
                cancelButtonText: '否',
                type: 'success'
            }).then(() => {
                emit('insert-content', { action: 'append', content: result })
            }).catch(() => {})
        }

    } catch (error) {
        if (error.name === 'AbortError') {
            ElMessage.warning('生成已停止')
        } else {
            console.error('AI 生成失败:', error)
            ElMessage.error('生成失败: ' + error.message)
        }
        conversations.value.pop()
    } finally {
        generating.value = false
        abortController.value = null
        nextTick(() => {
            inputRef.value?.focus()
        })
    }
}
</script>

<style scoped>
.dialog-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
    padding-right: 40px;
}

.header-left {
    display: flex;
    align-items: center;
    gap: 8px;
}

.settings-btn {
    font-size: 18px;
    color: #667eea;
    cursor: pointer;
    transition: all 0.2s;
    padding: 6px 8px;
    border-radius: 6px;
    background: rgba(102, 126, 234, 0.08);
    display: inline-flex;
    align-items: center;
    justify-content: center;
}

.settings-btn:hover {
    color: #5a6fd6;
    background: rgba(102, 126, 234, 0.15);
    transform: rotate(30deg);
}

.dialog-title {
    font-weight: 600;
    font-size: 16px;
    color: #1f2937;
}

.header-actions {
    display: flex;
    gap: 8px;
    align-items: center;
}

.header-actions .el-button {
    display: flex;
    align-items: center;
    gap: 4px;
}

.current-session-name {
    max-width: 100px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.session-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
    gap: 12px;
}

.session-title {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.check-icon {
    color: #667eea;
    flex-shrink: 0;
}

.is-active {
    background: #f5f7fa;
}

.ai-container {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

/* 对话区域 - 竖长形 */
.conversation-area {
    background: #fafbfc;
    border: 1px solid #e5e7eb;
    border-radius: 16px;
    height: 420px;
    min-height: 420px;
    overflow-y: auto;
    padding: 16px;
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    height: 100%;
    color: #999;
}

.empty-icon {
    width: 64px;
    height: 64px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border-radius: 18px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #fff;
    margin-bottom: 14px;
}

.empty-text {
    font-size: 15px;
    color: #374151;
    margin: 0 0 6px 0;
    font-weight: 500;
}

.empty-hint {
    font-size: 13px;
    color: #9ca3af;
    margin: 0;
}

/* 对话列表 */
.conversation-list {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.message-item {
    display: flex;
    gap: 10px;
    max-width: 88%;
}

.message-item.user {
    flex-direction: row-reverse;
    align-self: flex-end;
}

.message-item.assistant {
    flex-direction: row;
    align-self: flex-start;
}

.message-avatar {
    width: 32px;
    height: 32px;
    border-radius: 10px;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

.user .message-avatar {
    background: linear-gradient(135deg, #409eff 0%, #66b1ff 100%);
    color: #fff;
}

.assistant .message-avatar {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: #fff;
}

.message-content {
    display: flex;
    flex-direction: column;
    gap: 4px;
    max-width: calc(100% - 42px);
}

.message-bubble {
    padding: 10px 14px;
    border-radius: 14px;
    font-size: 14px;
    line-height: 1.6;
    word-break: break-word;
}

.user .message-bubble {
    background: linear-gradient(135deg, #409eff 0%, #66b1ff 100%);
    color: #fff;
    border-bottom-right-radius: 6px;
}

.assistant .message-bubble {
    background: #ffffff;
    color: #374151;
    border: 1px solid #e5e7eb;
    border-bottom-left-radius: 6px;
}

.message-footer {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 0 4px;
}

.message-time {
    font-size: 10px;
    color: #bdbdbd;
}

.word-count {
    font-size: 10px;
    color: #9ca3af;
}

.user .message-footer {
    flex-direction: row-reverse;
}

.message-actions {
    display: flex;
    gap: 8px;
    margin-top: 8px;
    padding: 0 4px;
}

.message-actions .el-button {
    font-size: 12px;
    padding: 6px 10px;
    display: flex;
    align-items: center;
    gap: 4px;
}

/* 思考动画 */
.thinking-bubble {
    display: flex;
    align-items: center;
    gap: 4px;
    padding: 10px 14px;
}

.thinking-dot {
    width: 7px;
    height: 7px;
    background: #667eea;
    border-radius: 50%;
    animation: thinking 1.4s infinite ease-in-out both;
}

.thinking-dot:nth-child(1) { animation-delay: -0.32s; }
.thinking-dot:nth-child(2) { animation-delay: -0.16s; }

@keyframes thinking {
    0%, 80%, 100% { transform: scale(0.6); opacity: 0.4; }
    40% { transform: scale(1); opacity: 1; }
}

/* 剩余次数提示 */
.remaining-hint {
    display: flex;
    justify-content: center;
    font-size: 12px;
    padding: 4px 0;
}

.remaining-normal {
    color: #9ca3af;
}

.remaining-warning {
    color: #e6a23c;
    display: flex;
    align-items: center;
    gap: 4px;
}

/* Token 警告 */
.token-warning {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    padding: 8px 12px;
    background: #fef0f0;
    border: 1px solid #fde2e2;
    border-radius: 10px;
    font-size: 12px;
    color: #f56c6c;
}

/* 模板区域 */
.template-section {
    padding: 0 2px;
}

.template-scroll {
    display: flex;
    gap: 8px;
    overflow-x: auto;
    padding: 2px 0;
}

.template-scroll::-webkit-scrollbar {
    height: 3px;
}

.template-scroll::-webkit-scrollbar-thumb {
    background: #ddd;
    border-radius: 2px;
}

.template-tag {
    cursor: pointer;
    white-space: nowrap;
    border: none;
    background: #f3f4f6;
    color: #6b7280;
    transition: all 0.2s;
    font-size: 12px;
    padding: 6px 12px;
    border-radius: 6px;
}

.template-tag:hover {
    background: #e8eaf0;
    color: #667eea;
}

/* 输入区域 */
.input-section {
    display: flex;
    flex-direction: column;
    gap: 10px;
    padding: 0 2px;
}

.input-section :deep(.el-textarea__inner) {
    border-radius: 12px;
    padding: 12px 14px;
    font-size: 14px;
    line-height: 1.5;
    border: 1px solid #e5e7eb;
}

.input-section :deep(.el-textarea__inner:focus) {
    border-color: #667eea;
    box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.input-actions {
    display: flex;
    justify-content: flex-end;
}

.send-btn {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border: none;
    border-radius: 10px;
    padding: 10px 20px;
}

.send-btn:hover {
    background: linear-gradient(135deg, #5a6fd6 0%, #6a4190 100%);
}

.stop-btn {
    border-radius: 10px;
    padding: 10px 20px;
}

/* Markdown 样式 */
.message-bubble :deep(h1) {
    font-size: 1.1em;
    margin: 0.4em 0 0.2em;
}

.message-bubble :deep(h2) {
    font-size: 1em;
    margin: 0.4em 0 0.2em;
}

.message-bubble :deep(h3) {
    font-size: 0.95em;
    margin: 0.4em 0 0.2em;
}

.message-bubble :deep(p) {
    margin: 0.4em 0;
}

.message-bubble :deep(code) {
    background: rgba(0,0,0,0.05);
    padding: 0.15em 0.3em;
    border-radius: 4px;
    font-size: 0.9em;
}

.message-bubble :deep(pre) {
    background: #1f2937;
    color: #e5e7eb;
    padding: 0.8em;
    border-radius: 10px;
    overflow-x: auto;
    margin: 0.5em 0;
}

.message-bubble :deep(pre code) {
    background: transparent;
    padding: 0;
}

.message-bubble :deep(ul),
.message-bubble :deep(ol) {
    padding-left: 1.3em;
    margin: 0.4em 0;
}

.message-bubble :deep(blockquote) {
    border-left: 3px solid #667eea;
    padding-left: 1em;
    margin: 0.4em 0;
    color: #6b7280;
}

.user .message-bubble :deep(code),
.user .message-bubble :deep(pre),
.user .message-bubble :deep(pre code) {
    background: rgba(255,255,255,0.2);
    color: #fff;
}
</style>

<style>
.ai-assistant-dialog {
    border-radius: 16px;
    overflow: hidden;
}

.ai-assistant-dialog .el-dialog {
    border-radius: 16px;
    overflow: hidden;
}

.ai-assistant-dialog .el-dialog__header {
    padding: 16px 20px;
    margin: 0;
    background: #fff;
}

.ai-assistant-dialog .el-dialog__headerbtn {
    top: 16px;
    right: 20px;
}

.ai-assistant-dialog .el-dialog__headerbtn .el-dialog__close {
    color: #9ca3af;
}

.ai-assistant-dialog .el-dialog__body {
    padding: 16px 20px;
    background: #fff;
}

.ai-assistant-dialog .el-dialog__footer {
    display: none;
}
</style>

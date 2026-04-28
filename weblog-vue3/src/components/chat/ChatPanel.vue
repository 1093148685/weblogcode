<template>
    <div class="flex h-full chat-panel panel-card overflow-hidden">
        <!-- 侧边栏 -->
        <transition name="slide">
            <div
                v-if="showSidebar"
                class="w-[280px] flex-shrink-0 flex flex-col bg-[var(--bg-card)] border-r border-[var(--border-base)] h-full"
            >
                <!-- 侧边栏头部：Logo + 操作按钮 -->
                <div class="flex items-center justify-between px-5 py-4 border-b border-[var(--border-base)] bg-[var(--bg-card)]/90">
                    <div class="flex items-center gap-2.5">
                        <div class="w-9 h-9 bg-gradient-to-br from-blue-500 to-indigo-600 rounded-full flex items-center justify-center shadow-sm">
                            <span class="text-white text-sm font-bold">J</span>
                        </div>
                        <span class="text-lg font-bold text-[var(--text-heading)]">小J 助手</span>
                    </div>
                    <button @click="showSidebar = false"
                        class="p-2 rounded-full hover:bg-[var(--bg-hover)] transition-colors text-[var(--text-muted)]">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
                        </svg>
                    </button>
                </div>

                <!-- 新对话按钮 -->
                <div class="px-4 mb-4">
                    <button @click="createNewChat"
                        class="sidebar-item sidebar-item-new w-full justify-center">
                        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v14m-7-7h14"/>
                        </svg>
                        <span>新对话</span>
                    </button>
                </div>

                <!-- 模型选择 -->
                <div class="px-4 mb-4">
                    <button @click="showSettings = true"
                        class="sidebar-item w-full">
                        <div class="w-8 h-8 bg-violet-100 dark:bg-violet-900/30 rounded-full flex items-center justify-center flex-shrink-0">
                            <svg class="w-4 h-4 text-violet-500" fill="none" viewBox="0 0 24 24">
                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                    d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.325.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 0 1 1.37.49l1.296 2.247a1.125 1.125 0 0 1-.26 1.431l-1.003.827c-.293.241-.438.613-.43.992a7.723 7.723 0 0 1 0 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.955.26 1.43l-1.298 2.247a1.125 1.125 0 0 1-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.47 6.47 0 0 1-.22.128c-.331.183-.581.495-.644.869l-.213 1.281c-.09.543-.56.94-1.11.94h-2.594c-.55 0-1.019-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 0 1-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 0 1-1.369-.49l-1.297-2.247a1.125 1.125 0 0 1 .26-1.431l1.004-.827c.292-.24.437-.613.43-.991a6.932 6.932 0 0 1 0-.255c.007-.38-.138-.751-.43-.992l-1.004-.827a1.125 1.125 0 0 1-.26-1.43l1.297-2.247a1.125 1.125 0 0 1 1.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.086.22-.128.332-.183.582-.495.644-.869l.214-1.28Z"/>
                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z"/>
                            </svg>
                        </div>
                        <div class="flex-1 min-w-0 text-left">
                            <span class="text-sm text-[var(--text-body)]">模型设置</span>
                            <span class="block text-xs text-[var(--text-muted)] truncate">{{ currentModelName }}</span>
                        </div>
                    </button>
                </div>

                <!-- AI 对话模式 -->
                <div class="px-4 mb-4">
                    <div class="text-xs text-[var(--text-muted)] mb-1 px-1">AI 模式</div>
                    <div class="chat-mode-switch">
                        <button
                            type="button"
                            class="chat-mode-option"
                            :class="{ 'chat-mode-option-active': selectedChatMode === 'auto' }"
                            @click="setChatMode('auto')"
                        >
                            智能选择
                        </button>
                        <button
                            type="button"
                            class="chat-mode-option"
                            :class="{ 'chat-mode-option-active': selectedChatMode === 'normal' }"
                            @click="setChatMode('normal')"
                        >
                            普通聊天
                        </button>
                        <button
                            type="button"
                            class="chat-mode-option"
                            :class="{ 'chat-mode-option-active': selectedChatMode === 'rag' }"
                            @click="setChatMode('rag')"
                        >
                            知识库问答
                        </button>
                        <button
                            type="button"
                            class="chat-mode-option"
                            :class="{ 'chat-mode-option-active': selectedChatMode === 'web' }"
                            @click="setChatMode('web')"
                        >
                            联网搜索
                        </button>
                    </div>
                </div>

                <!-- 知识库选择（RAG） -->
                <div class="px-4 mb-4" :class="{ 'opacity-55': selectedChatMode !== 'rag' && selectedChatMode !== 'auto' }">
                    <div class="flex items-center justify-between text-xs text-[var(--text-muted)] mb-1 px-1">
                        <span>知识库</span>
                        <span v-if="selectedChatMode === 'auto' && selectedKbOption" class="text-[var(--color-primary)]">智能可用</span>
                        <span v-else-if="selectedKb" class="text-[var(--color-primary)]">RAG 已启用</span>
                    </div>
                    <select v-model="selectedKbId"
                        :disabled="selectedChatMode !== 'rag' && selectedChatMode !== 'auto'"
                        @change="handleKbChange"
                        class="w-full text-sm rounded-lg px-3 py-2 bg-[var(--bg-card)] border border-[var(--border-light)] text-[var(--text-body)] focus:outline-none focus:ring-2 focus:ring-violet-400">
                        <option value="">不使用知识库</option>
                        <option v-for="kb in kbList" :key="kb.id" :value="kb.id">{{ kb.name }}</option>
                    </select>
                    <p v-if="selectedKb?.description" class="mt-1.5 px-1 text-xs text-[var(--text-placeholder)] line-clamp-2">
                        {{ selectedKb.description }}
                    </p>
                </div>

                <!-- 最近对话标题 -->
                <div class="px-5 mb-2 text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">最近对话</div>

                <!-- 会话列表 -->
                <div class="flex-1 overflow-y-auto px-4 pb-4 chat-sidebar-scroll">
                    <div class="space-y-1">
                        <div
                            v-for="session in chatSessions"
                            :key="session.id"
                            class="group sidebar-item-session"
                            :class="currentSessionId === session.id
                                ? 'sidebar-item-session-active'
                                : 'hover:bg-[var(--bg-hover)]'"
                            @click="switchSession(session.id)"
                        >
                            <div class="flex-1 min-w-0 flex items-center gap-1">
                                <span v-if="session.pinned" title="已置顶" class="text-amber-400 flex-shrink-0">📌</span>
                                <span class="block text-sm text-[var(--text-body)] truncate font-medium leading-snug flex-1">{{ session.title }}</span>
                            </div>
                            <span class="text-xs text-[var(--text-muted)] mt-0.5 block">{{ session.time }}</span>
                            <div class="flex gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0 -mr-1">
                                <button
                                    class="p-1 rounded-lg text-[var(--text-muted)] hover:text-amber-500 hover:bg-amber-50 dark:hover:bg-amber-900/20 transition-all"
                                    @click.stop="pinSession(session)"
                                    :title="session.pinned ? '取消置顶' : '置顶'"
                                >
                                    <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24"><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z"/></svg>
                                </button>
                                <button
                                    class="p-1 rounded-lg text-[var(--text-muted)] hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-all"
                                    @click.stop="renameSession(session)"
                                    title="重命名"
                                >
                                    <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24"><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m15.232 5.232 3.536 3.536m-2.036-5.036a2.5 2.5 0 1 1 3.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"/></svg>
                                </button>
                                <button
                                    class="p-1 rounded-lg text-[var(--text-muted)] hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-all"
                                    @click.stop="deleteSession(session.id)"
                                    title="删除"
                                >
                                    <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24"><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0"/></svg>
                                </button>
                            </div>
                        </div>
                    </div>
                    <div v-if="chatSessions.length === 0" class="text-center py-10">
                        <div class="w-12 h-12 mx-auto mb-3 rounded-full bg-[var(--bg-hover)] flex items-center justify-center">
                            <svg class="w-5 h-5 text-[var(--text-placeholder)]" fill="none" viewBox="0 0 24 24">
                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                                    d="M20 13V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v7m16 0v5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-5m16 0h-2.586a1 1 0 0 0-.707.293l-2.414 2.414a1 1 0 0 1-.707.293h-3.172a1 1 0 0 1-.707-.293l-2.414-2.414A1 1 0 0 0 6.586 13H4"/>
                            </svg>
                        </div>
                        <p class="text-sm text-[var(--text-muted)]">暂无会话记录</p>
                        <p class="text-xs text-[var(--text-placeholder)] mt-1">点击上方开启新对话</p>
                    </div>
                </div>
            </div>
        </transition>

        <!-- 主内容区 -->
        <div class="flex-1 flex flex-col min-w-0 relative bg-[var(--bg-base)]">
            <!-- 标题栏 -->
            <div class="flex items-center justify-between px-5 py-3 border-b border-[var(--border-base)] bg-[var(--bg-card)]">
                <div class="flex items-center gap-3">
                    <!-- 展开侧边栏按钮（侧边栏隐藏时显示） -->
                    <button v-if="!showSidebar" @click="showSidebar = true"
                        class="p-2 rounded-lg hover:bg-[var(--bg-hover)] transition-colors text-[var(--text-muted)]">
                        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5"/>
                        </svg>
                    </button>
                    <div class="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center shadow-sm">
                        <span class="text-white text-xs font-bold">J</span>
                    </div>
                    <div class="flex flex-col">
                        <span class="font-semibold text-[var(--text-heading)] text-sm">小J智能 AI 助手</span>
                        <span v-if="currentSessionTitle && currentSessionTitle !== '新会话'" class="text-xs text-[var(--text-muted)] truncate max-w-[300px]">
                            {{ currentSessionTitle }}
                        </span>
                    </div>
                    <div class="flex items-center gap-2">
                        <el-button
                            v-if="currentSessionId && displayMessages.length"
                            size="small"
                            @click="clearCurrentSession"
                            title="清空当前上下文"
                        >
                            清空
                        </el-button>
                        <el-button
                            v-if="currentSessionId"
                            size="small"
                            @click="exportSession(chatSessions.find(s => s.id === currentSessionId))"
                            title="导出对话"
                        >
                            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24"><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/></svg>
                            导出
                        </el-button>
                    </div>
                </div>

            </div>

            <!-- 消息区域 -->
            <div class="flex-1 overflow-y-auto px-4 sm:px-6 lg:px-8 pb-4 chat-messages-scroll relative" ref="chatContainer" @scroll="handleScroll">
                <div class="max-w-4xl mx-auto pt-6">
                    <!-- 欢迎消息 -->
                    <div v-if="displayMessages.length === 0" class="flex flex-col items-center justify-center min-h-[50vh]">
                        <div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center shadow-lg mb-6">
                            <span class="text-white text-2xl font-bold">J</span>
                        </div>
                        <h2 class="text-xl font-bold text-[var(--text-heading)] mb-2">你好，有什么可以帮你的？</h2>
                        <p class="text-sm text-[var(--text-muted)] mb-8 text-center max-w-md">我是小J，博客智能问答助手。可以解答博客内容、推荐文章、讨论话题。</p>

                        <div class="grid grid-cols-2 gap-3 w-full max-w-lg">
                            <button
                                v-for="prompt in quickPrompts"
                                :key="prompt"
                                @click="usePrompt(prompt)"
                                class="quick-prompt-card"
                            >
                                <span class="text-sm text-[var(--text-body)]">{{ prompt }}</span>
                                <svg class="w-4 h-4 text-[var(--text-placeholder)] mt-1" fill="none" viewBox="0 0 24 24">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4.5 12h15m0 0-6.75-6.75M19.5 12l-6.75 6.75"/>
                                </svg>
                            </button>
                        </div>
                    </div>

                    <!-- 聊天记录 -->
                    <template v-else>
                        <template v-for="(chat, index) in displayMessages" :key="index">
                            <!-- 用户消息 -->
                            <div v-if="chat.role === 'user'" class="flex justify-end mb-6">
                                <div class="max-w-[85%]">
                                    <div class="bg-[var(--color-primary)] text-white px-4 py-3 rounded-2xl rounded-br-md shadow-sm">
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
                            </div>

                            <!-- AI 回复 -->
                            <div v-else class="flex mb-6 gap-3 group">
                                <div class="w-8 h-8 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center flex-shrink-0 mt-0.5 shadow-sm">
                                    <span class="text-white text-xs font-bold">J</span>
                                </div>
                                <div class="min-w-0 flex-1">
                                    <div class="chat-ai-bubble">
                                        <div v-if="chat.mode || chat.kbName" class="chat-answer-meta">
                                            <span class="chat-answer-mode" :class="answerModeClass(chat.mode)">
                                                {{ answerModeLabel(chat.mode) }}
                                            </span>
                                            <span v-if="chat.smartRouteLabel" class="chat-answer-kb">{{ chat.smartRouteLabel }}</span>
                                            <span v-if="chat.kbName" class="chat-answer-kb">{{ chat.kbName }}</span>
                                        </div>
                                        <p v-if="chat.routeReason" class="chat-route-reason">{{ chat.routeReason }}</p>

                                        <!-- 流式加载中 -->
                                        <div v-if="!chat.content && index === displayMessages.length - 1 && isStreaming" class="flex items-center gap-2 py-1">
                                            <span class="typing-dot" style="animation-delay:0s"></span>
                                            <span class="typing-dot" style="animation-delay:0.15s"></span>
                                            <span class="typing-dot" style="animation-delay:0.3s"></span>
                                        </div>
                                        <StreamMarkdownRender v-else :content="chat.content" />

                                        <div v-if="chat.ragStatus && (!chat.sources || chat.sources.length === 0)" class="rag-status mt-3">
                                            <span class="rag-dot"></span>
                                            <span>{{ chat.ragStatus }}</span>
                                        </div>

                                        <div v-if="chat.ragSteps && chat.ragSteps.length" class="rag-step-list mt-3">
                                            <div
                                                v-for="step in chat.ragSteps"
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

                                        <!-- 操作按钮 -->
                                        <div v-if="chat.content && !isStreaming" class="mt-3 flex items-center justify-end gap-2 border-t border-[var(--border-base)] pt-2 opacity-0 group-hover:opacity-100 transition-opacity">
                                            <button
                                                @click="copyMessage(chat.content)"
                                                class="flex items-center gap-1.5 px-2 py-1 text-xs text-[var(--text-muted)] hover:text-[var(--color-primary)] hover:bg-[var(--bg-hover)] rounded-md transition-colors"
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
                                                class="flex items-center gap-1.5 px-2 py-1 text-xs text-[var(--text-muted)] hover:text-[var(--color-primary)] hover:bg-[var(--bg-hover)] rounded-md transition-colors"
                                                title="重新生成"
                                            >
                                                <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                                                </svg>
                                                重试
                                            </button>
                                        </div>
                                    </div>
                                    <p class="text-xs text-[var(--text-muted)] mt-1.5">{{ formatTime(chat.timestamp) }}</p>
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
                    class="absolute bottom-28 left-1/2 -translate-x-1/2 z-20 flex items-center gap-1.5 px-3 py-2 text-xs font-medium text-white bg-[var(--color-primary)] rounded-full shadow-lg hover:opacity-90 active:scale-95 transition-all duration-200"
                    title="回到最新消息"
                >
                    <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"/>
                    </svg>
                    最新消息
                </button>
            </Transition>

            <!-- 输入区域 -->
            <div class="chat-composer px-4 sm:px-6 lg:px-8 pb-5 pt-3">
                <div class="max-w-4xl mx-auto">
                    <!-- 使用次数提示 -->
                    <div
                        v-if="usageInfo && !usageInfo.isAdmin && !usageInfo.IsAdmin"
                        class="mb-2 text-xs text-center"
                        :class="(usageInfo.remaining === 0 || usageInfo.Remaining === 0) ? 'text-red-500' : 'text-[var(--text-muted)]'"
                    >
                        {{ usageInfo.message || usageInfo.Message }}
                    </div>

                    <!-- 引用文章提示 -->
                    <div v-if="quotedArticle" class="mb-2 p-2.5 bg-[var(--bg-hover)] rounded-2xl border border-[var(--border-base)]">
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

                    <!-- 输入框 -->
                    <div class="chat-input-wrapper">
                        <textarea
                            ref="textareaRef"
                            v-model="inputText"
                            @input="autoResize"
                            @keydown.enter.exact.prevent="sendMessage"
                            @keydown.shift.enter.exact="handleShiftEnter"
                            placeholder="输入消息，Enter 发送，Shift+Enter 换行..."
                            class="w-full px-5 py-3.5 pr-32 bg-transparent border-none outline-none resize-none text-sm text-[var(--text-body)] placeholder-[var(--text-placeholder)] leading-relaxed"
                            rows="2"
                            maxlength="2000"
                        ></textarea>

                        <div class="absolute right-3 bottom-3 flex items-center gap-1.5">
                            <button @click="showArticleDialog = true"
                                class="p-2 rounded-xl text-[var(--text-muted)] hover:text-[var(--text-body)] hover:bg-[var(--bg-hover)] transition-colors"
                                title="引用文章">
                                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                                        d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z"/>
                                </svg>
                            </button>
                            <span class="text-xs text-[var(--text-placeholder)] tabular-nums">{{ inputText.length }}/2000</span>
                            <span v-if="selectedKb" class="hidden sm:inline-flex items-center gap-1 text-xs px-1.5 py-0.5 rounded-full rag-chip" title="将优先检索知识库后回答">
                                RAG
                            </span>
                            <span v-if="inputTokenCount > 0" class="text-xs px-1.5 py-0.5 rounded-full" :class="inputTokenCount > 3000 ? 'bg-red-100 text-red-500' : 'bg-slate-100 text-slate-500'" title="预估 Token 消耗">
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
import {
    Setting, Promotion, Document, Close, Search, Plus,
    DArrowLeft, DArrowRight, VideoPause, Delete
} from '@element-plus/icons-vue'
import axios from '@/axios'
import { getToken } from '@/composables/cookie'
import { getArticleDetail } from '@/api/frontend/article'
import { saveSession, getUserSessions, deleteUserSession, getUsageInfo, getPublicKbList } from '@/api/frontend/chat'
import StreamMarkdownRender from './StreamMarkdownRender.vue'
import ModelSettings from './ModelSettings.vue'
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

// ──────── 当前选中的模型 ────────
const selectedModelId = ref('deepseek-chat')

// ──────── 知识库 RAG ────────
const kbList = ref([])
const selectedKbId = ref('')
const selectedChatMode = ref('auto')

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

const currentModelName = computed(() => {
    const m = modelOptions.value.find(o => o.id === selectedModelId.value)
    return m?.name || selectedModelId.value
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
                pinned: false,
                time: formatDbTime(s.updatedAt),
                messages: safeParseMessages(s.messages),
                model: s.model
            }))
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
        pinned: false,
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
        session.time = new Date().toLocaleString('zh-CN', {
            month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
        })
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
        session.time = new Date().toLocaleString('zh-CN', {
            month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
        })
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

const answerModeLabel = (mode) => {
    if (mode === 'auto') return '智能选择'
    if (mode === 'rag') return '知识库问答'
    if (mode === 'web') return '联网搜索'
    if (mode === 'article') return '文章辅助'
    return '普通聊天'
}

const answerModeClass = (mode) => {
    if (mode === 'rag') return 'chat-answer-mode-rag'
    if (mode === 'web') return 'chat-answer-mode-web'
    if (mode === 'article' || mode === 'auto') return 'chat-answer-mode-rag'
    return 'chat-answer-mode-normal'
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
            const kb = knowledgeBases.value.find(item => item.id === payload.routeKbId)
            if (kb) lastMsg.kbName = kb.name
        }
        if (routeMode === 'web') {
            lastMsg.sourcesExpanded = true
        }
    }

    if (parsed.type === 'rag_status') {
        const payload = parsed.payload || {}
        applyRoutePayload(payload)
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
        ragStatus: effectiveMode === 'auto' ? '正在智能判断...' : effectiveMode === 'web' ? '准备联网搜索...' : effectiveKb ? '准备检索知识库...' : '',
        ragSteps: effectiveMode === 'auto'
            ? [{ key: 'route', message: '正在智能判断...', status: 'running' }]
            : effectiveMode === 'web'
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
        session.time = new Date().toLocaleString('zh-CN', {
            month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit'
        })
    }

    streamingSessionId.value = sessionId
    streamingMessages.value = sessionMessages
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
            const lastMsg = streamingMessages.value[streamingMessages.value.length - 1]
            lastMsg.content += (lastMsg.content ? '\n\n' : '') + '[ 已停止生成 ]'
        } else {
            console.error('流式请求出错:', error)
            streamingMessages.value[streamingMessages.value.length - 1].content = `抱歉，请求出错了，请稍后重试。\n\n${error.message || ''}`.trim()
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
        if (options.forceMode) {
            selectedChatMode.value = originalChatMode
        }
    }
}

const stopStreaming = () => {
    if (abortController) {
        abortController.abort()
        abortController = null
        isStreaming.value = false
        const lastMsg = streamingMessages.value[streamingMessages.value.length - 1]
        lastMsg.content += (lastMsg.content ? '\n\n' : '') + '> [ ⚠️ 已手动停止生成 ]'

        // Save the stopped state
        const finalMessages = [...(streamingMessages.value || [])]
        updateSessionLocally(streamingSessionId.value, finalMessages)
        saveSessionToDb(streamingSessionId.value, finalMessages, selectedModelId.value)

        streamingSessionId.value = null
        streamingMessages.value = null
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

/* 侧边栏项目 - 清言风格 */
.sidebar-item {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    padding: 0.75rem 1rem;
    border-radius: 1rem;
    transition: all 0.2s ease;
    cursor: pointer;
    color: var(--text-body);
}
.sidebar-item:hover {
    background: var(--bg-hover);
}

.sidebar-item-new {
    background: var(--bg-hover);
    font-weight: 500;
    font-size: 0.875rem;
}
.sidebar-item-new:hover {
    background: var(--bg-active);
}

/* 会话列表项 */
.sidebar-item-session {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
    padding: 0.625rem 0.875rem;
    border-radius: 0.875rem;
    transition: all 0.2s ease;
    cursor: pointer;
}
.sidebar-item-session-active {
    background: var(--bg-active);
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
.chat-ai-bubble {
    padding: 1rem 1.25rem;
    border-radius: 1rem;
    border-top-left-radius: 0.25rem;
    background: var(--bg-card);
    border: 1px solid var(--border-base);
    color: var(--text-body);
    font-size: 0.875rem;
    line-height: 1.7;
}

/* 暗色模式 AI 气泡内 Markdown */
.dark .chat-ai-bubble {
    background: #1c2732;
    border-color: #253341;
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

.chat-mode-switch {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 0.35rem;
    padding: 0.25rem;
    border-radius: 0.85rem;
    background: var(--bg-hover);
    border: 1px solid var(--border-base);
}

@media (min-width: 1280px) {
    .chat-mode-switch {
        grid-template-columns: 1fr 1fr;
    }
}

.chat-mode-option {
    min-height: 2.15rem;
    border: 0;
    border-radius: 0.65rem;
    background: transparent;
    color: var(--text-muted);
    font-size: 0.8rem;
    font-weight: 700;
    transition: all 0.18s ease;
}

.chat-mode-option:hover {
    color: var(--text-heading);
    background: rgba(59, 130, 246, 0.08);
}

.chat-mode-option-active {
    color: #fff;
    background: linear-gradient(135deg, #4f7cff, #20c7df);
    box-shadow: 0 8px 22px rgba(59, 130, 246, 0.22);
}

.chat-mode-option-active:hover {
    color: #fff;
    background: linear-gradient(135deg, #4f7cff, #20c7df);
}

.chat-answer-meta {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 0.4rem;
    margin-bottom: 0.65rem;
}

.chat-answer-mode,
.chat-answer-kb {
    display: inline-flex;
    align-items: center;
    min-height: 1.45rem;
    padding: 0 0.5rem;
    border-radius: 999px;
    font-size: 0.7rem;
    font-weight: 700;
}

.chat-answer-mode-normal {
    color: #475569;
    background: rgba(100, 116, 139, 0.10);
    border: 1px solid rgba(100, 116, 139, 0.18);
}

.chat-answer-mode-rag {
    color: var(--color-primary);
    background: rgba(59, 130, 246, 0.10);
    border: 1px solid rgba(59, 130, 246, 0.20);
}

.chat-answer-mode-web {
    color: #0f766e;
    background: rgba(20, 184, 166, 0.12);
    border: 1px solid rgba(20, 184, 166, 0.24);
}

.chat-answer-kb {
    color: #64748b;
    background: var(--bg-hover);
    border: 1px solid var(--border-base);
}

.chat-route-reason {
    margin: -0.25rem 0 0.75rem;
    color: var(--text-muted);
    font-size: 0.76rem;
    line-height: 1.45;
}

.dark .chat-answer-mode-normal,
.dark .chat-answer-kb {
    color: #94a3b8;
}

.dark .chat-answer-mode-web {
    color: #5eead4;
    background: rgba(20, 184, 166, 0.14);
    border-color: rgba(45, 212, 191, 0.26);
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
    padding-top: 12px;
    border-top: 1px solid var(--border-base);
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
</style>

<template>
    <div class="flex h-full chat-panel">
        <!-- 侧边栏 -->
        <transition name="slide">
            <div
                v-if="showSidebar"
                class="w-[280px] flex-shrink-0 flex flex-col bg-[var(--bg-card)] border-r border-[var(--border-base)] h-full"
            >
                <!-- 侧边栏头部：Logo + 操作按钮 -->
                <div class="flex items-center justify-between px-5 py-4">
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
                        class="sidebar-item sidebar-item-new w-full">
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

                <!-- 知识库选择（RAG） -->
                <div class="px-4 mb-4">
                    <div class="text-xs text-[var(--text-muted)] mb-1 px-1">知识库</div>
                    <select v-model="selectedKbId"
                        class="w-full text-sm rounded-lg px-3 py-2 bg-[var(--bg-card)] border border-[var(--border-light)] text-[var(--text-body)] focus:outline-none focus:ring-2 focus:ring-violet-400">
                        <option :value="null">不使用知识库</option>
                        <option v-for="kb in kbList" :key="kb.id" :value="kb.id">{{ kb.name }}</option>
                    </select>
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
                                        <!-- 流式加载中 -->
                                        <div v-if="!chat.content && index === displayMessages.length - 1 && isStreaming" class="flex items-center gap-2 py-1">
                                            <span class="typing-dot" style="animation-delay:0s"></span>
                                            <span class="typing-dot" style="animation-delay:0.15s"></span>
                                            <span class="typing-dot" style="animation-delay:0.3s"></span>
                                        </div>
                                        <StreamMarkdownRender v-else :content="chat.content" />

                                        <!-- 操作按钮 -->
                                        <div v-if="chat.content && !isStreaming" class="mt-3 flex items-center justify-end gap-2 border-t border-[var(--border-base)] pt-2 opacity-0 group-hover:opacity-100 transition-opacity">
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
                    @click="scrollToBottom"
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
            <div class="px-4 sm:px-6 lg:px-8 pb-5 pt-3 bg-[var(--bg-base)]">
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

// ──────── 知识库 RAG ────────
const kbList = ref([])
const selectedKbId = ref(null)

onMounted(async () => {
    try {
        const res = await getPublicKbList()
        if (res.success && res.data?.length) {
            kbList.value = res.data
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
                pinned: false,
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
                articleTitle: currentQuotedArticle?.title || currentQuotedArticle?.Title || null,
                kbId: selectedKbId.value || null
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
    showScrollBtn.value = scrollHeight - scrollTop - clientHeight > 200
}

const scrollToBottom = async () => {
    await nextTick()
    if (chatContainer.value) {
        chatContainer.value.scrollTop = chatContainer.value.scrollHeight
        showScrollBtn.value = false
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

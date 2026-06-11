<template>
    <aside class="chat-sidebar-shell flex h-full min-h-0 w-[320px] flex-shrink-0 flex-col">
        <div class="chat-sidebar-header">
            <div class="flex min-w-0 items-center gap-3">
                <div class="chat-sidebar-avatar">J</div>
                <div class="min-w-0">
                    <p class="chat-sidebar-title">AI 工作台</p>
                    <p class="chat-sidebar-subtitle">以会话为中心的工作区</p>
                </div>
            </div>
            <button type="button" class="chat-sidebar-icon-btn" @click="$emit('close')" aria-label="收起侧边栏">
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
                </svg>
            </button>
        </div>

        <div class="chat-sidebar-actions">
            <button
                v-if="sections.primary.includes('new-chat')"
                type="button"
                class="chat-sidebar-primary-btn"
                @click="$emit('new-chat')"
            >
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 5v14m-7-7h14" />
                </svg>
                <span>新对话</span>
            </button>

            <label v-if="sections.primary.includes('search')" class="chat-sidebar-search">
                <svg class="h-4 w-4 text-[var(--text-muted)]" fill="none" viewBox="0 0 24 24">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m21 21-4.35-4.35m1.85-5.15a7 7 0 1 1-14 0 7 7 0 0 1 14 0Z" />
                </svg>
                <input
                    :value="searchQuery"
                    type="text"
                    placeholder="搜索会话"
                    @input="$emit('update:searchQuery', $event.target.value)"
                >
            </label>
        </div>

        <div class="chat-sidebar-history">
            <div class="chat-sidebar-history-head">
                <span>历史会话</span>
                <span class="chat-sidebar-count">{{ totalSessions }}</span>
            </div>

            <div v-if="hasSessions" class="chat-sidebar-groups">
                <section
                    v-for="group in groupedSessions"
                    :key="group.label"
                    class="chat-sidebar-group"
                >
                    <h3 class="chat-sidebar-group-label">{{ group.label }}</h3>
                    <div class="space-y-1.5">
                        <button
                            v-for="session in group.items"
                            :key="session.id"
                            type="button"
                            class="chat-sidebar-session group"
                            :class="{ 'chat-sidebar-session-active': currentSessionId === session.id }"
                            @click="$emit('select-session', session.id)"
                        >
                            <div class="min-w-0 flex-1 text-left">
                                <div class="flex min-w-0 items-center gap-1.5">
                                    <span v-if="session.pinned" class="chat-sidebar-pin">置顶</span>
                                    <span class="truncate text-sm font-medium text-[var(--text-heading)]">{{ session.title }}</span>
                                </div>
                                <span class="mt-1 block truncate text-xs text-[var(--text-muted)]">{{ session.time }}</span>
                            </div>
                            <div class="chat-sidebar-session-actions">
                                <button
                                    type="button"
                                    class="chat-sidebar-session-btn"
                                    :title="session.pinned ? '取消置顶' : '置顶'"
                                    @click.stop="$emit('pin-session', session)"
                                >
                                    <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
                                    </svg>
                                </button>
                                <button
                                    type="button"
                                    class="chat-sidebar-session-btn"
                                    title="重命名"
                                    @click.stop="$emit('rename-session', session)"
                                >
                                    <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m15.232 5.232 3.536 3.536m-2.036-5.036a2.5 2.5 0 1 1 3.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                                    </svg>
                                </button>
                                <button
                                    type="button"
                                    class="chat-sidebar-session-btn"
                                    title="删除"
                                    @click.stop="$emit('delete-session', session.id)"
                                >
                                    <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0" />
                                    </svg>
                                </button>
                            </div>
                        </button>
                    </div>
                </section>
            </div>

            <div v-else class="chat-sidebar-empty">
                <div class="chat-sidebar-empty-icon">
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24">
                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.6" d="M20 13V6a2 2 0 0 0-2-2H6a2 2 0 0 0-2 2v7m16 0v5a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-5m16 0h-2.586a1 1 0 0 0-.707.293l-2.414 2.414a1 1 0 0 1-.707.293h-3.172a1 1 0 0 1-.707-.293l-2.414-2.414A1 1 0 0 0 6.586 13H4" />
                    </svg>
                </div>
                <p class="text-sm text-[var(--text-heading)]">还没有历史会话</p>
                <p class="mt-1 text-xs text-[var(--text-muted)]">从一条新对话开始。</p>
            </div>
        </div>

        <div class="chat-sidebar-footer">
            <div class="chat-sidebar-footer-grid">
                <button
                    type="button"
                    class="chat-sidebar-footer-btn"
                    @click="$emit('share-session')"
                >
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24">
                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M7.217 10.907a2.25 2.25 0 1 0 0 2.186m0-2.186 9.566-5.314m-9.566 7.5 9.566 5.314m0 0a2.25 2.25 0 1 0 0-2.186m0 2.186a2.25 2.25 0 1 0 0-2.186m0-10.628a2.25 2.25 0 1 0 0 2.186" />
                    </svg>
                    <span>分享</span>
                </button>

                <el-dropdown trigger="click" @command="$emit('sidebar-more-action', $event)">
                    <button type="button" class="chat-sidebar-footer-btn">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M12 6.75h.008v.008H12V6.75Zm0 5.25h.008v.008H12V12Zm0 5.25h.008v.008H12v-.008Z" />
                        </svg>
                        <span>更多</span>
                    </button>
                    <template #dropdown>
                        <el-dropdown-menu>
                            <el-dropdown-item
                                v-for="item in moreItems"
                                :key="item.key"
                                :command="item.key"
                                :disabled="item.disabled"
                            >
                                {{ item.label }}
                            </el-dropdown-item>
                        </el-dropdown-menu>
                    </template>
                </el-dropdown>
            </div>

            <button
                v-if="sections.secondary.includes('settings')"
                type="button"
                class="chat-sidebar-footer-btn chat-sidebar-footer-btn-wide"
                @click="$emit('open-settings')"
            >
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.325.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 0 1 1.37.49l1.296 2.247a1.125 1.125 0 0 1-.26 1.431l-1.003.827c-.293.241-.438.613-.43.992a7.723 7.723 0 0 1 0 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.955.26 1.43l-1.298 2.247a1.125 1.125 0 0 1-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.47 6.47 0 0 1-.22.128c-.331.183-.581.495-.644.869l-.213 1.281c-.09.543-.56.94-1.11.94h-2.594c-.55 0-1.019-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 0 1-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 0 1-1.369-.49l-1.297-2.247a1.125 1.125 0 0 1 .26-1.431l1.004-.827c.292-.24.437-.613.43-.991a6.932 6.932 0 0 1 0-.255c.007-.38-.138-.751-.43-.992l-1.004-.827a1.125 1.125 0 0 1-.26-1.43l1.297-2.247a1.125 1.125 0 0 1 1.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.086.22-.128.332-.183.582-.495.644-.869l.214-1.28Z" />
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                </svg>
                <span>设置</span>
            </button>
        </div>
    </aside>
</template>

<script setup>
import { computed } from 'vue'

defineOptions({ name: 'ChatSidebar' })

const props = defineProps({
    sections: {
        type: Object,
        default: () => ({
            primary: [],
            secondary: []
        })
    },
    groupedSessions: {
        type: Array,
        default: () => []
    },
    currentSessionId: {
        type: [String, Number, null],
        default: null
    },
    moreItems: {
        type: Array,
        default: () => []
    },
    searchQuery: {
        type: String,
        default: ''
    }
})

defineEmits([
    'close',
    'delete-session',
    'new-chat',
    'open-settings',
    'pin-session',
    'rename-session',
    'share-session',
    'sidebar-more-action',
    'select-session',
    'update:searchQuery'
])

const hasSessions = computed(() => props.groupedSessions.some(group => group.items?.length))
const totalSessions = computed(() => props.groupedSessions.reduce((total, group) => total + (group.items?.length || 0), 0))
</script>

<style scoped>
.chat-sidebar-shell {
    padding: 18px 16px 16px;
    background: var(--bg-card);
    border-right: 1px solid var(--border-base);
}

.chat-sidebar-header,
.chat-sidebar-footer {
    display: flex;
    align-items: center;
    gap: 12px;
}

.chat-sidebar-header {
    justify-content: space-between;
    margin-bottom: 16px;
}

.chat-sidebar-avatar {
    width: 40px;
    height: 40px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 8px;
    color: #fff;
    font-weight: 800;
    background: var(--color-primary);
    box-shadow: var(--shadow-sm);
}

.chat-sidebar-title {
    margin: 0;
    color: var(--text-heading);
    font-size: 0.95rem;
    font-weight: 700;
}

.chat-sidebar-subtitle {
    margin: 2px 0 0;
    color: var(--text-muted);
    font-size: 0.75rem;
}

.chat-sidebar-icon-btn,
.chat-sidebar-session-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 8px;
    border: 1px solid transparent;
    color: var(--text-muted);
    background: transparent;
    transition: all 0.18s ease;
}

.chat-sidebar-icon-btn {
    width: 32px;
    height: 32px;
}

.chat-sidebar-icon-btn:hover,
.chat-sidebar-session-btn:hover {
    color: var(--text-heading);
    background: var(--bg-hover);
    border-color: var(--border-base);
}

.chat-sidebar-actions {
    display: grid;
    gap: 10px;
    margin-bottom: 18px;
}

.chat-sidebar-primary-btn,
.chat-sidebar-footer-btn {
    min-height: 42px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    border-radius: 8px;
    border: 1px solid var(--border-base);
    color: var(--text-heading);
    background: var(--bg-card);
    transition: all 0.18s ease;
}

.chat-sidebar-primary-btn {
    color: #fff;
    border-color: transparent;
    background: var(--color-primary);
    box-shadow: var(--shadow-sm);
}

.chat-sidebar-primary-btn:hover {
    opacity: 0.94;
}

.chat-sidebar-search {
    display: flex;
    align-items: center;
    gap: 8px;
    min-height: 42px;
    padding: 0 12px;
    border-radius: 8px;
    border: 1px solid var(--border-base);
    background: var(--bg-base);
}

.chat-sidebar-search input {
    width: 100%;
    border: 0;
    outline: 0;
    background: transparent;
    color: var(--text-body);
    font-size: 0.875rem;
}

.chat-sidebar-search input::placeholder {
    color: var(--text-placeholder);
}

.chat-sidebar-history {
    flex: 1;
    min-height: 0;
    display: flex;
    flex-direction: column;
}

.chat-sidebar-history-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 12px;
    color: var(--text-muted);
    font-size: 0.76rem;
    font-weight: 700;
}

.chat-sidebar-count {
    min-width: 1.5rem;
    height: 1.5rem;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 999px;
    background: var(--bg-hover);
    color: var(--text-secondary);
    font-size: 0.72rem;
}

.chat-sidebar-groups {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
    padding-right: 2px;
}

.chat-sidebar-group + .chat-sidebar-group {
    margin-top: 14px;
}

.chat-sidebar-group-label {
    margin: 0 0 8px;
    color: var(--text-placeholder);
    font-size: 0.72rem;
    font-weight: 700;
}

.chat-sidebar-session {
    width: 100%;
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 10px 10px 12px;
    border-radius: 8px;
    border: 1px solid transparent;
    background: transparent;
    transition: all 0.18s ease;
}

.chat-sidebar-session:hover,
.chat-sidebar-session-active {
    background: var(--bg-hover);
    border-color: var(--border-base);
}

.chat-sidebar-session-active {
    box-shadow: inset 2px 0 0 var(--color-primary);
}

.chat-sidebar-pin {
    display: inline-flex;
    align-items: center;
    height: 18px;
    padding: 0 6px;
    border-radius: 999px;
    background: rgba(245, 158, 11, 0.12);
    color: #b45309;
    font-size: 0.68rem;
    font-weight: 700;
}

.chat-sidebar-session-actions {
    display: flex;
    align-items: center;
    gap: 2px;
    opacity: 0;
    transition: opacity 0.18s ease;
}

.chat-sidebar-session:hover .chat-sidebar-session-actions,
.chat-sidebar-session-active .chat-sidebar-session-actions {
    opacity: 1;
}

.chat-sidebar-session-btn {
    width: 28px;
    height: 28px;
}

.chat-sidebar-empty {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 24px 12px;
    text-align: center;
}

.chat-sidebar-empty-icon {
    width: 40px;
    height: 40px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    margin-bottom: 12px;
    border-radius: 999px;
    background: var(--bg-hover);
    color: var(--text-placeholder);
}

.chat-sidebar-footer {
    flex-direction: column;
    align-items: stretch;
    gap: 10px;
    padding-top: 14px;
    margin-top: 14px;
    border-top: 1px solid var(--border-base);
}

.chat-sidebar-footer-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 8px;
}

.chat-sidebar-footer :deep(.el-dropdown) {
    width: 100%;
}

.chat-sidebar-footer-btn {
    width: 100%;
    color: var(--text-secondary);
    background: var(--bg-base);
}

.chat-sidebar-footer-btn-wide {
    grid-column: 1 / -1;
}

.chat-sidebar-footer-btn:hover {
    color: var(--text-heading);
    background: var(--bg-hover);
}

@media (max-width: 768px) {
    .chat-sidebar-shell {
        position: absolute;
        inset: 0 auto 0 0;
        z-index: 30;
        width: min(88vw, 320px);
        padding-inline: 14px;
        box-shadow: 0 18px 40px rgba(15, 23, 42, 0.18);
    }
}
</style>

<template>
    <div class="chat-status-bar">
        <div class="chat-status-head">
            <div class="flex min-w-0 items-center gap-3">
                <button
                    type="button"
                    class="chat-status-icon-btn"
                    @click="$emit('toggle-sidebar')"
                    :aria-label="sidebarVisible ? '收起侧边栏' : '展开侧边栏'"
                >
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24">
                        <path
                            stroke="currentColor"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            stroke-width="2"
                            :d="sidebarVisible ? 'M15 19l-7-7 7-7' : 'M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5'"
                        />
                    </svg>
                </button>
                <div class="min-w-0">
                    <p class="chat-status-title">{{ title }}</p>
                    <p v-if="subtitle" class="chat-status-subtitle">{{ subtitle }}</p>
                </div>
            </div>
        </div>

        <div v-if="chips.length" class="chat-status-chips">
            <span v-for="chip in chips" :key="chip.key" class="chat-status-chip">
                <span class="chat-status-chip-label">{{ chip.label }}</span>
                <strong class="chat-status-chip-value">{{ chip.value }}</strong>
            </span>
        </div>
    </div>
</template>

<script setup>
defineOptions({ name: 'ChatStatusBar' })

const props = defineProps({
    chips: {
        type: Array,
        default: () => []
    },
    sidebarVisible: {
        type: Boolean,
        default: true
    },
    subtitle: {
        type: String,
        default: ''
    },
    title: {
        type: String,
        default: '新会话'
    }
})

defineEmits(['toggle-sidebar'])
</script>

<style scoped>
.chat-status-bar {
    padding: 16px 24px 14px;
    border-bottom: 1px solid var(--border-base);
    background: color-mix(in srgb, var(--bg-card) 88%, transparent);
    backdrop-filter: blur(10px);
}

.chat-status-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
}

.chat-status-title {
    margin: 0;
    color: var(--text-heading);
    font-size: 1rem;
    font-weight: 700;
}

.chat-status-subtitle {
    margin: 2px 0 0;
    color: var(--text-muted);
    font-size: 0.76rem;
}

.chat-status-icon-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    min-height: 34px;
    padding: 0 12px;
    border-radius: 8px;
    border: 1px solid var(--border-base);
    color: var(--text-secondary);
    background: var(--bg-card);
    transition: all 0.18s ease;
}

.chat-status-icon-btn {
    width: 34px;
    padding: 0;
}

.chat-status-icon-btn:hover {
    color: var(--text-heading);
    background: var(--bg-hover);
}

.chat-status-chips {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-top: 12px;
}

.chat-status-chip {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    min-height: 28px;
    padding: 0 10px;
    border-radius: 999px;
    border: 1px solid var(--border-base);
    background: var(--bg-base);
    color: var(--text-secondary);
    font-size: 0.76rem;
}

.chat-status-chip-label {
    color: var(--text-muted);
}

.chat-status-chip-value {
    color: var(--text-heading);
    font-weight: 700;
}

@media (max-width: 768px) {
    .chat-status-bar {
        padding: 10px 14px;
    }

    .chat-status-head {
        align-items: center;
    }

    .chat-status-chips {
        display: none;
    }

    .chat-status-title {
        font-size: 0.98rem;
    }

    .chat-status-subtitle {
        font-size: 0.72rem;
    }
}
</style>

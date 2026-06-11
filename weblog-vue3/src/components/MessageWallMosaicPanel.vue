<template>
    <div class="sticky-wall">
        <!-- 标题栏（始终显示） -->
        <div class="wall-header mb-4">
            <div class="flex items-center gap-3">
                <div class="h-px flex-1 bg-gray-200 dark:bg-gray-700"></div>
                <button 
                    @click="collapsed = !collapsed"
                    class="text-gray-500 dark:text-gray-400 text-sm flex items-center gap-2 hover:text-gray-700 dark:hover:text-gray-200 transition-colors">
                    <span class="w-2 h-2 rounded-full bg-gray-400" :class="{ 'bg-blue-500': !collapsed }"></span>
                    <span>精选留言</span>
                    <span v-if="firstLevelComments.length > 0" class="text-gray-400">({{ firstLevelComments.length }})</span>
                    <i :class="collapsed ? 'fas fa-angle-down' : 'fas fa-angle-up'" class="text-xs ml-1"></i>
                </button>
                <div class="h-px flex-1 bg-gray-200 dark:bg-gray-700"></div>
            </div>
        </div>

        <!-- 留言网格 -->
        <template v-if="!collapsed">
            <div v-if="loading" class="flex justify-center items-center py-10">
                <div class="w-5 h-5 border-2 border-gray-300 border-t-gray-600 rounded-full animate-spin"></div>
            </div>

            <div v-else-if="firstLevelComments.length > 0" class="sticky-notes-container">
                <div v-for="(comment, index) in firstLevelComments" 
                    :key="comment.id"
                    :class="['sticky-note', getStickyColorClass(index)]"
                    :style="getStickyStyle(comment, index)"
                    @click="scrollToComment(comment.id)">
                    
                    <!-- 内容 -->
                    <div class="sticky-content">
                        <div class="flex items-center gap-2 mb-2">
                            <img v-if="comment.avatar && comment.avatar.length > 0" 
                                :src="comment.avatar" 
                                class="w-5 h-5 rounded-full border border-gray-200 dark:border-gray-600">
                            <div v-else class="w-5 h-5 rounded-full border border-gray-200 dark:border-gray-600 bg-gray-100 dark:bg-gray-700 flex items-center justify-center">
                                <i class="fas fa-user text-[8px] text-gray-400"></i>
                            </div>
                            <span class="text-xs text-gray-600 dark:text-gray-300 truncate max-w-[80px]">{{ comment.nickname }}</span>
                            <span class="text-[10px] text-gray-400 font-mono">{{ formatTime(comment.createTime) }}</span>
                        </div>
                        <p class="text-xs text-gray-700 dark:text-gray-300 leading-relaxed whitespace-pre-wrap break-words" :class="getLineClamp(index)">
                            {{ comment.content }}
                        </p>
                    </div>
                </div>
            </div>

            <!-- 空状态 -->
            <div v-else class="text-center py-10">
                <div class="text-2xl mb-2">📝</div>
                <p class="text-gray-400 text-sm">暂无精选留言</p>
            </div>
        </template>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { getMessageWallComments } from '@/api/frontend/message-wall'
import { showMessage } from '@/composables/util'

const loading = ref(true)
const allComments = ref([])
const collapsed = ref(true)

const ACCENT_COLORS = ['blue', 'green', 'purple', 'orange', 'pink', 'cyan']

const firstLevelComments = computed(() => {
    return allComments.value.filter(c => {
        if (c.parentCommentId && c.parentCommentId !== 0) return false
        
        const now = new Date()
        const createTime = new Date(c.createTime)
        const minutesDiff = (now - createTime) / 60000
        
        return minutesDiff <= 10
    })
})

const getStickyColorClass = (index) => {
    return `accent-${ACCENT_COLORS[index % ACCENT_COLORS.length]}`
}

const getStickyStyle = (comment, index) => {
    const rotate = ((index % 5) - 2) * 1.5
    
    return {
        '--rotate': `${rotate}deg`,
    }
}

const getLineClamp = (index) => {
    return index % 3 === 0 ? 'line-clamp-4' : 'line-clamp-3'
}

const formatTime = (time) => {
    if (!time) return ''
    
    const date = new Date(time)
    const now = new Date()
    const diff = now - date
    
    const minutes = Math.floor(diff / 60000)
    const hours = Math.floor(diff / 3600000)
    const days = Math.floor(diff / 86400000)
    
    if (minutes < 1) return 'now'
    if (minutes < 60) return `${minutes}m`
    if (hours < 24) return `${hours}h`
    if (days < 7) return `${days}d`
    
    return date.toLocaleDateString('zh-CN', { month: 'short', day: 'numeric' })
}

const scrollToComment = (commentId) => {
    const element = document.querySelector(`[data-comment-id="${commentId}"]`)
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' })
        element.classList.add('highlight-comment')
        setTimeout(() => {
            element.classList.remove('highlight-comment')
        }, 2000)
    }
}

const initComments = async () => {
    loading.value = true
    try {
        const res = await getMessageWallComments()
        if (res.success) {
            allComments.value = res.data.comments || []
        }
    } catch (e) {
        showMessage('加载留言失败', 'error')
    } finally {
        loading.value = false
    }
}

onMounted(() => {
    initComments()
})

defineExpose({
    refresh: initComments
})
</script>

<style scoped>
/* ==================== 墙壁背景 ==================== */
.sticky-wall {
    position: relative;
    background-color: #f6f8fa;
    border-bottom: 1px solid #dfe2e5;
    padding: 20px 16px;
}

.dark .sticky-wall {
    background-color: #0d1117;
    border-bottom-color: #30363d;
}

/* ==================== 极简卡片容器 ==================== */
.sticky-notes-container {
    display: flex;
    flex-wrap: wrap;
    gap: 12px;
    justify-content: center;
    align-content: flex-start;
    max-height: 280px;
    overflow-y: auto;
    padding: 4px;
}

/* ==================== 极简便利贴 ==================== */
.sticky-note {
    position: relative;
    width: 140px;
    min-height: 90px;
    padding: 10px;
    border-radius: 6px;
    transform: rotate(var(--rotate, 0deg));
    transition: all 0.15s ease;
    cursor: pointer;
    background: white;
    border: 1px solid #dfe2e5;
}

.dark .sticky-note {
    background: #21262d;
    border-color: #30363d;
}

.sticky-note:hover {
    transform: rotate(0deg) translateY(-2px);
    border-color: #0969da;
    z-index: 10;
}

/* ==================== Accent 色点 ==================== */
.sticky-note::before {
    content: '';
    position: absolute;
    top: 8px;
    left: 8px;
    width: 6px;
    height: 6px;
    border-radius: 50%;
    background: #0969da;
}

.accent-blue::before { background: #0969da; }
.accent-green::before { background: #2da44e; }
.accent-purple::before { background: #8250df; }
.accent-orange::before { background: #bf8700; }
.accent-pink::before { background: #bf3989; }
.accent-cyan::before { background: #0891b2; }

/* ==================== 内容 ==================== */
.sticky-content {
    padding-left: 4px;
}

/* ==================== 行数限制 ==================== */
.line-clamp-3 {
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

.line-clamp-4 {
    display: -webkit-box;
    -webkit-line-clamp: 4;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

/* ==================== 滚动条 ==================== */
.sticky-notes-container::-webkit-scrollbar {
    width: 6px;
}

.sticky-notes-container::-webkit-scrollbar-track {
    background: transparent;
}

.sticky-notes-container::-webkit-scrollbar-thumb {
    background: #d0d7de;
    border-radius: 3px;
}

.dark .sticky-notes-container::-webkit-scrollbar-thumb {
    background: #30363d;
}

.sticky-notes-container::-webkit-scrollbar-thumb:hover {
    background: #afb8c1;
}

.dark .sticky-notes-container::-webkit-scrollbar-thumb:hover {
    background: #484f58;
}

/* ==================== 高亮动画 ==================== */
@keyframes highlight-pulse {
    0%, 100% {
        border-color: #dfe2e5;
    }
    50% {
        border-color: #0969da;
        box-shadow: 0 0 0 2px rgba(9, 105, 218, 0.2);
    }
}

.dark .highlight-comment,
:deep(.highlight-comment) {
    animation: highlight-pulse 0.6s ease-in-out 3;
}

.dark :deep(.highlight-comment) {
    animation: highlight-pulse-dark 0.6s ease-in-out 3;
}

@keyframes highlight-pulse-dark {
    0%, 100% {
        border-color: #30363d;
    }
    50% {
        border-color: #58a6ff;
        box-shadow: 0 0 0 2px rgba(88, 166, 255, 0.2);
    }
}
</style>

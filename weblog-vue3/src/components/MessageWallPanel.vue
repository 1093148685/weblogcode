<template>
    <div class="message-wall-panel" ref="panelRef">
        <!-- 标题栏 -->
        <div class="panel-header mb-4">
            <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                    <span class="w-2 h-2 rounded-full bg-gray-300 dark:bg-gray-600"></span>
                    <span class="text-sm font-medium text-gray-600 dark:text-gray-300">{{ comments.length }} 条留言</span>
                </div>
                <div class="flex items-center gap-2">
                    <!-- 排序按钮 -->
                    <button v-for="option in sortOptions" :key="option.value"
                        @click="currentSort = option.value"
                        :class="['px-2 py-1 text-xs rounded transition-colors',
                            currentSort === option.value 
                                ? 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-200' 
                                : 'text-gray-400 hover:text-gray-600 dark:hover:text-gray-300']">
                        {{ option.label }}
                    </button>
                    <!-- 刷新按钮 -->
                    <button @click="handleRefresh" :disabled="loading"
                        class="p-1.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors disabled:opacity-50">
                        <i class="fas fa-sync-alt text-xs" :class="{ 'animate-spin': loading }"></i>
                    </button>
                </div>
            </div>
        </div>
        
        <!-- 加载状态 -->
        <div v-if="loading" class="flex justify-center items-center py-12">
            <div class="w-5 h-5 border-2 border-gray-200 border-t-gray-600 rounded-full animate-spin"></div>
        </div>
        
        <!-- 留言列表 -->
        <div v-else-if="sortedComments && sortedComments.length > 0" class="comments-container">
            <div v-for="(comment, index) in visibleComments" :key="comment.id" 
                :class="['mb-3', { 'animate-new-comment': newCommentIds.has(comment.id) }]"
                :ref="el => setCommentRef(el, index)">
                <MessageWallCard 
                    :comment="comment"
                    :has-current-user-flower="flowerStatus[comment.id] || false"
                    @reply-submitted="handleReply"
                    @flower-changed="handleFlowerChange" />
                <div v-if="index < visibleComments.length - 1" class="comment-divider"></div>
            </div>
            
            <!-- 加载更多indicator -->
            <div v-if="hasMoreComments" class="flex justify-center items-center py-6">
                <div class="w-5 h-5 border-2 border-gray-200 border-t-gray-600 rounded-full animate-spin"></div>
            </div>
        </div>
        
        <!-- 空状态 -->
        <div v-else class="text-center py-12">
            <div class="text-3xl mb-3">📝</div>
            <p class="text-gray-400 text-sm mb-1">暂无留言</p>
            <p class="text-gray-400 text-xs">快来发表第一条留言吧</p>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, reactive, nextTick } from 'vue'
import MessageWallCard from '@/components/MessageWallCard.vue'
import { getMessageWallComments, publishMessageWallComment, getFlowerStatus } from '@/api/frontend/message-wall'
import { useCommentStore } from '@/stores/comment'
import { showMessage } from '@/composables/util'
import { setCache, getCache } from '@/composables/useCache'

const commentStore = useCommentStore()
const comments = ref([])
const loading = ref(true)
const flowerStatus = reactive({})
const newCommentIds = reactive(new Set())
const currentSort = ref('latest')
const panelRef = ref(null)
const commentRefs = ref([])
const visibleCount = ref(20)
const loadingMore = ref(false)
const hasMoreComments = computed(() => visibleCount.value < sortedComments.value.length)

const sortOptions = [
    { value: 'latest', label: '最新', icon: 'fas fa-clock', field: 'createTime', order: 'desc' },
    { value: 'earliest', label: '最早', icon: 'fas fa-clock', field: 'createTime', order: 'asc' },
    { value: 'popular', label: '热门', icon: 'fas fa-fire', field: 'flowerCount', order: 'desc' }
]

const sortedComments = computed(() => {
    const sorted = [...comments.value]
    const option = sortOptions.find(o => o.value === currentSort.value) || sortOptions[0]
    
    sorted.sort((a, b) => {
        let valA = a[option.field] || 0
        let valB = b[option.field] || 0
        
        if (option.field === 'createTime') {
            valA = new Date(valA).getTime()
            valB = new Date(valB).getTime()
        }
        
        if (option.order === 'desc') {
            return valB - valA
        } else {
            return valA - valB
        }
    })
    
    return sorted
})

const visibleComments = computed(() => {
    return sortedComments.value.slice(0, visibleCount.value)
})

const setCommentRef = (el, index) => {
    if (el) {
        commentRefs.value[index] = el
    }
}

const loadMoreIfNeeded = () => {
    if (loadingMore.value || !hasMoreComments.value) return

    // 使用 window 级别的滚动位置来判断是否需要加载更多
    const scrollTop = window.scrollY || document.documentElement.scrollTop
    const windowHeight = window.innerHeight
    const documentHeight = document.documentElement.scrollHeight

    const threshold = 300
    if (scrollTop + windowHeight >= documentHeight - threshold) {
        loadingMore.value = true
        setTimeout(() => {
            visibleCount.value = Math.min(visibleCount.value + 20, sortedComments.value.length)
            loadingMore.value = false
        }, 300)
    }
}

const handleScroll = () => {
    loadMoreIfNeeded()
}

const emit = defineEmits(['reply'])

onMounted(() => {
    initComments()
    window.addEventListener('scroll', handleScroll)
})

onUnmounted(() => {
    window.removeEventListener('scroll', handleScroll)
})

const initComments = async () => {
    const savedScrollTop = panelRef.value?.scrollTop ?? 0
    visibleCount.value = 20

    // 尝试读取缓存（首次加载快速展示）
    const cached = getCache('message_wall_comments')
    if (cached && comments.value.length === 0) {
        comments.value = cached
        loading.value = false
    } else {
        loading.value = true
    }

    try {
        const res = await getMessageWallComments()
        if (res.success) {
            const oldIds = new Set(comments.value.map(c => c.id))
            comments.value = res.data.comments || []

            // 缓存留言数据 3 分钟
            setCache('message_wall_comments', comments.value, 3 * 60 * 1000)

            comments.value.forEach(c => {
                if (!oldIds.has(c.id)) {
                    newCommentIds.add(c.id)
                    setTimeout(() => {
                        newCommentIds.delete(c.id)
                    }, 2000)
                }
            })
            
            const commentIds = comments.value.map(c => c.id)
            if (commentIds.length > 0) {
                const statusRes = await getFlowerStatus(commentIds)
                if (statusRes.success && statusRes.data) {
                    Object.assign(flowerStatus, statusRes.data)
                }
            }
        }
        
        await nextTick()
        await nextTick()
        
        requestAnimationFrame(() => {
            if (panelRef.value) {
                panelRef.value.scrollTop = savedScrollTop
            }
        })
    } catch (e) {
        showMessage('加载留言失败', 'error')
    } finally {
        loading.value = false
    }
}

const handleRefresh = async () => {
    visibleCount.value = 20
    await initComments()
}

const handleReply = async ({ replyCommentId, parentCommentId, content, replyNickname, images }) => {
    const data = {
        content,
        avatar: commentStore.userInfo.avatar,
        nickname: commentStore.userInfo.nickname,
        mail: commentStore.userInfo.mail,
        website: commentStore.userInfo.website,
        replyCommentId,
        parentCommentId,
        images
    }
    
    try {
        const res = await publishMessageWallComment(data)
        if (res.success) {
            showMessage('回复成功')
            const newComment = res.data
            newComment.hasCurrentUserFlower = false
            newComment.flowerCount = 0
            newComment.childComments = []
            flowerStatus[newComment.id] = false
            
            const findAndAddComment = (commentList) => {
                for (const comment of commentList) {
                    if (comment.id === parentCommentId) {
                        if (!comment.childComments) {
                            comment.childComments = []
                        }
                        comment.childComments.push(newComment)
                        return true
                    }
                    if (comment.childComments && comment.childComments.length > 0) {
                        if (findAndAddComment(comment.childComments)) {
                            return true
                        }
                    }
                }
                return false
            }
            findAndAddComment(comments.value)
            comments.value = [...comments.value]
        } else {
            showMessage(res.message, 'error')
        }
    } catch (e) {
        showMessage('回复失败', 'error')
    }
}

const handleFlowerChange = ({ commentId, hasFlower, count }) => {
    flowerStatus[commentId] = hasFlower
    const updateCommentFlower = (comments) => {
        for (const comment of comments) {
            if (comment.id === commentId) {
                comment.hasCurrentUserFlower = hasFlower
                if (count !== undefined) {
                    comment.flowerCount = count
                }
                return true
            }
            if (comment.childComments && comment.childComments.length > 0) {
                if (updateCommentFlower(comment.childComments)) {
                    return true
                }
            }
        }
        return false
    }
    updateCommentFlower(comments.value)
}

defineExpose({
    refresh: initComments
})
</script>

<style scoped>
@import '@fortawesome/fontawesome-free/css/all.min.css';

.message-wall-panel {
    width: 100%;
}

.panel-header {
    padding-bottom: 12px;
    border-bottom: 1px solid #dfe2e5;
}

.dark .panel-header {
    border-bottom-color: #30363d;
}

.comments-container {
    width: 100%;
}

@keyframes slide-in-from-top {
    0% {
        opacity: 0;
        transform: translateY(-10px);
    }
    100% {
        opacity: 1;
        transform: translateY(0);
    }
}

.animate-new-comment {
    animation: slide-in-from-top 0.3s ease-out;
}

.comment-divider {
    height: 1px;
    width: 90%;
    background: #e5e7eb;
    margin: 16px auto;
}

.dark .comment-divider {
    background: #30363d;
}

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

<template>
    <div class="message-wall-panel" ref="panelRef">
        <!-- 标题栏 -->
        <div class="panel-header mb-4">
            <div class="panel-header-row">
                <div class="flex items-center gap-2">
                    <span class="panel-title-icon"><i class="fas fa-comment-dots"></i></span>
                    <span class="panel-title hidden">最新留言</span>
                </div>
                <div class="panel-actions">
                    <!-- 排序按钮 -->
                    <div class="sort-tabs" role="tablist" aria-label="留言排序">
                        <button v-for="option in sortOptions" :key="option.value"
                            type="button"
                            @click="currentSort = option.value"
                            :class="['sort-tab',
                                currentSort === option.value
                                    ? 'sort-tab-active'
                                    : 'sort-tab-idle']">
                            {{ option.label }}
                        </button>
                    </div>
                    <!-- 刷新按钮 -->
                    <button @click="handleRefresh" :disabled="loading"
                        class="refresh-btn text-[var(--text-muted)] hover:text-[var(--text-secondary)] transition-colors disabled:opacity-50">
                        <i class="fas fa-sync-alt text-xs" :class="{ 'animate-spin': loading }"></i>
                    </button>
                </div>
            </div>
        </div>
        
        <!-- 加载状态 -->
        <div v-if="loading" class="flex justify-center items-center py-12">
            <div class="w-5 h-5 border-2 border-[var(--border-base)] border-t-[var(--text-secondary)] rounded-full animate-spin"></div>
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
            <div v-if="totalPages > 1" class="comment-pagination" aria-label="评论分页">
                <button type="button" class="pagination-btn" :disabled="currentPage === 1" @click="goPage(currentPage - 1)">
                    <i class="fas fa-chevron-left"></i>
                </button>
                <button v-for="page in paginationPages" :key="page" type="button"
                    :class="['pagination-page', { 'pagination-page-active': currentPage === page }]"
                    @click="goPage(page)">
                    {{ page }}
                </button>
                <button type="button" class="pagination-btn" :disabled="currentPage === totalPages" @click="goPage(currentPage + 1)">
                    <i class="fas fa-chevron-right"></i>
                </button>
                <span class="pagination-total">共 {{ sortedComments.length }} 条</span>
            </div>
        </div>
        
        <!-- 空状态 -->
        <div v-else class="text-center py-12">
            <div class="text-3xl mb-3">📝</div>
            <p class="text-[var(--text-muted)] text-sm mb-1">暂无留言</p>
            <p class="text-[var(--text-muted)] text-xs">快来发表第一条留言吧</p>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, reactive, nextTick, watch } from 'vue'
import MessageWallCard from '@/components/MessageWallCard.vue'
import { getMessageWallComments, publishMessageWallComment, getFlowerStatus } from '@/api/frontend/message-wall'
import { useCommentStore } from '@/stores/comment'
import { showMessage } from '@/composables/util'
import { setCache, getCache } from '@/composables/useCache'

const props = defineProps({
    routerUrl: {
        type: String,
        default: '/message-wall'
    },
    title: {
        type: String,
        default: '最新评论'
    }
})

const commentStore = useCommentStore()
const comments = ref([])
const loading = ref(true)
const flowerStatus = reactive({})
const newCommentIds = reactive(new Set())
const currentSort = ref('latest')
const panelRef = ref(null)
const commentRefs = ref([])
const pageSize = 15
const currentPage = ref(1)
const guestNames = ['路过的风', '山海访客', '星河旅人', '云边来客', '代码旅人', '清晨来信', '晚风同学', '蓝色便签']
const isQQNumber = (value) => /^\d+$/.test(String(value || '').trim())
const generateGuestName = () => `${guestNames[Math.floor(Math.random() * guestNames.length)]}${Math.floor(Math.random() * 900 + 100)}`
const normalizeWebsite = (value) => {
    const website = String(value || '').trim()
    if (!website) return ''
    return /^https?:\/\//i.test(website) ? website : `https://${website}`
}

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
    const start = (currentPage.value - 1) * pageSize
    return sortedComments.value.slice(start, start + pageSize)
})

const totalPages = computed(() => Math.max(1, Math.ceil(sortedComments.value.length / pageSize)))

const paginationPages = computed(() => {
    const total = totalPages.value
    const current = currentPage.value
    const windowSize = 5
    let start = Math.max(1, current - 2)
    let end = Math.min(total, start + windowSize - 1)
    start = Math.max(1, end - windowSize + 1)
    return Array.from({ length: end - start + 1 }, (_, index) => start + index)
})

const setCommentRef = (el, index) => {
    if (el) {
        commentRefs.value[index] = el
    }
}

const loadMoreIfNeeded = () => {
    return

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

const emit = defineEmits(['reply', 'stats-change'])

const formatPanelTime = (time) => {
    if (!time) return ''
    const date = new Date(time)
    if (Number.isNaN(date.getTime())) return ''
    return date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
}

const emitStats = () => {
    const list = comments.value || []
    const todayKey = new Date().toDateString()
    const today = list.filter(comment => {
        const date = new Date(comment.createTime)
        return !Number.isNaN(date.getTime()) && date.toDateString() === todayKey
    }).length
    const visitors = new Set(list.map(comment => comment.nickname || comment.mail || comment.id).filter(Boolean)).size
    const latest = [...list].sort((a, b) => new Date(b.createTime) - new Date(a.createTime))[0]

    emit('stats-change', {
        total: list.length,
        today,
        visitors,
        lastTime: formatPanelTime(latest?.createTime)
    })
}

onMounted(() => {
    initComments()
})

watch(currentSort, () => {
    currentPage.value = 1
})

watch(totalPages, (total) => {
    if (currentPage.value > total) {
        currentPage.value = total
    }
})

const initComments = async () => {
    const savedScrollTop = panelRef.value?.scrollTop ?? 0
    currentPage.value = 1

    // 尝试读取缓存（首次加载快速展示）
    const cacheKey = `message_wall_comments:${props.routerUrl}`
    const cached = getCache(cacheKey)
    if (cached && comments.value.length === 0) {
        comments.value = cached
        emitStats()
        loading.value = false
    } else {
        loading.value = true
    }

    try {
        const res = await getMessageWallComments(props.routerUrl)
        if (res.success) {
            const oldIds = new Set(comments.value.map(c => c.id))
            comments.value = res.data.comments || []
            emitStats()

            // 缓存留言数据 3 分钟
            setCache(cacheKey, comments.value, 3 * 60 * 1000)

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
    currentPage.value = 1
    await initComments()
}

const goPage = async (page) => {
    currentPage.value = Math.min(Math.max(page, 1), totalPages.value)
    await nextTick()
    panelRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

const handleReply = async ({ replyCommentId, parentCommentId, content, replyNickname, images }) => {
    const data = {
        content,
        avatar: commentStore.userInfo.avatar,
        nickname: isQQNumber(commentStore.userInfo.nickname) ? generateGuestName() : commentStore.userInfo.nickname,
        mail: commentStore.userInfo.mail,
        website: normalizeWebsite(commentStore.userInfo.website),
        replyCommentId,
        parentCommentId,
        images
    }
    
    try {
        const res = await publishMessageWallComment(data, props.routerUrl)
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
    min-width: 0;
    overflow: hidden;
    padding: 18px;
    border: 1px solid var(--border-base);
    border-radius: 8px;
    background: var(--bg-card);
    box-shadow: var(--shadow-sm);
}

.panel-header {
    padding-bottom: 12px;
    border-bottom: 1px solid var(--border-light);
}

.panel-header-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 14px;
    min-width: 0;
}

.panel-title-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 28px;
    height: 28px;
    border-radius: 8px;
    color: #fff;
    background: var(--color-primary);
}

.panel-title {
    color: var(--text-heading);
    font-size: 18px;
    font-weight: 800;
}

.panel-header button {
    border-radius: 8px;
}

.panel-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    flex: 0 0 280px;
    width: 280px;
    min-width: 280px;
    max-width: 280px;
    justify-content: flex-end;
}

.sort-tabs {
    display: grid;
    grid-template-columns: repeat(3, 72px);
    gap: 6px;
    align-items: center;
    justify-content: center;
    flex: 0 0 234px;
    width: 234px !important;
    min-width: 234px !important;
    max-width: 234px !important;
    padding: 4px;
    border: 1px solid var(--border-light);
    border-radius: 8px;
    background: var(--bg-base);
    overflow: hidden;
}

.sort-tab {
    box-sizing: border-box;
    width: 72px !important;
    min-width: 72px !important;
    max-width: 72px !important;
    flex: none;
    height: 32px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    padding: 0;
    margin: 0;
    border: 0;
    border-radius: 6px !important;
    color: var(--text-muted);
    font-size: 12px;
    font-weight: 700;
    line-height: 1;
    white-space: nowrap;
    overflow: hidden;
    text-align: center;
    appearance: none;
    -webkit-appearance: none;
    transition: color 0.18s ease, background-color 0.18s ease, border-color 0.18s ease;
}

.refresh-btn {
    width: 32px;
    height: 32px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    flex: 0 0 32px;
    padding: 0;
}

.sort-tab-idle:hover {
    color: var(--text-secondary);
    background: var(--bg-hover);
}

.sort-tab-active {
    color: var(--color-primary);
    background: var(--bg-hover);
    box-shadow: inset 0 0 0 1px var(--border-base);
}

@media (min-width: 769px) {
    .panel-header-row {
        display: grid;
        grid-template-columns: minmax(0, 1fr) auto;
    }

    .panel-actions {
        width: 280px;
        min-width: 280px;
        max-width: 280px;
        justify-content: flex-end;
    }

    .sort-tabs {
        grid-template-columns: repeat(3, 72px) !important;
    }

    .sort-tab {
        width: 72px !important;
        min-width: 72px !important;
    }
}

.comments-container {
    width: 100%;
    min-width: 0;
    overflow: hidden;
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
    display: none;
}

.comment-pagination {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    flex-wrap: wrap;
    padding: 18px 0 4px;
}

.pagination-btn,
.pagination-page {
    min-width: 34px;
    height: 34px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border: 1px solid var(--border-base);
    border-radius: 8px;
    background: var(--bg-card);
    color: var(--text-secondary);
    font-size: 13px;
    font-weight: 700;
    transition: background-color 0.18s ease, color 0.18s ease, border-color 0.18s ease;
}

.pagination-btn:not(:disabled):hover,
.pagination-page:hover {
    background: var(--bg-hover);
    color: var(--text-heading);
}

.pagination-btn:disabled {
    cursor: not-allowed;
    opacity: 0.45;
}

.pagination-page-active {
    background: var(--bg-active);
    border-color: var(--border-heavy);
    color: var(--text-heading);
}

.pagination-total {
    color: var(--text-muted);
    font-size: 12px;
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

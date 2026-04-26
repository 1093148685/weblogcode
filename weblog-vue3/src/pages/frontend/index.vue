<template>
    <Header></Header>

    <!-- 主内容区域 -->
    <main :class="currentView === 'ai-chat'
        ? 'h-[calc(100vh-72px)] overflow-hidden'
        : 'max-w-content mx-auto px-6 py-8'">

        <!-- 公告区域 -->
        <div v-if="announcement && announcement.isEnabled && !announcementHidden && currentView === 'article'" class="mb-6 animate-fade-in">
            <div class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card p-4 shadow-card hover:shadow-card-hover transition-shadow duration-300">
                <div class="flex items-start justify-between">
                    <div class="flex items-start flex-1">
                        <svg class="w-5 h-5 text-blue-500 mr-3 mt-0.5 flex-shrink-0 animate-pulse" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
                        </svg>
                        <div class="markdown-body text-[var(--text-body)] announcement-content" v-html="renderedAnnouncementContent"></div>
                    </div>
                    <div class="flex items-center ml-3 flex-shrink-0 gap-2">
                        <button @click="showAnnouncementModal = true" class="text-sm text-blue-500 hover:text-blue-600 transition-colors">
                            展开
                        </button>
                        <button @click="hideAnnouncement" class="text-[var(--text-muted)] hover:text-[var(--text-body)] dark:hover:text-white transition-colors">
                            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
                            </svg>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <!-- 公告弹窗 -->
        <el-dialog v-model="showAnnouncementModal" width="600px" class="announcement-dialog" :show-close="true">
            <template #header>
                <div class="flex items-center py-1">
                    <svg class="w-4 h-4 text-blue-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
                    </svg>
                    <span class="text-base font-medium text-[var(--text-heading)]">公告</span>
                </div>
            </template>
            <div class="markdown-body text-[var(--text-body)] max-h-[60vh] overflow-y-auto pr-1" v-html="renderedAnnouncementContent"></div>
            <template #footer>
                <div class="flex justify-end py-1">
                    <el-button type="primary" size="small" @click="showAnnouncementModal = false">知道了</el-button>
                </div>
            </template>
        </el-dialog>

        <!-- 文章视图 -->
        <div v-if="currentView === 'article'" class="grid grid-cols-1 lg:grid-cols-[1fr_280px] gap-6">
            <!-- 文章列表 -->
            <div class="min-w-0">
                <div class="space-y-4">
                    <!-- 骨架屏 -->
                    <div v-if="isLoading" class="space-y-4">
                        <div v-for="i in 5" :key="i" class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card p-4 flex gap-4">
                            <Skeleton width="176px" height="144px" border-radius="8px" class="hidden sm:block flex-shrink-0" />
                            <div class="flex-1 space-y-3 py-1">
                                <Skeleton width="80%" height="1.5rem" />
                                <Skeleton width="100%" height="3rem" />
                                <div class="flex gap-4">
                                    <Skeleton width="80px" height="1rem" />
                                    <Skeleton width="60px" height="1rem" />
                                </div>
                            </div>
                        </div>
                    </div>

                    <template v-else>
                    <div v-for="(article, index) in articles" :key="index"
                        ref="articleCardRefs"
                        class="article-card relative bg-[var(--bg-card)] border border-[var(--border-base)] rounded-xl shadow-card hover:shadow-card-hover transition-all duration-200 overflow-hidden group scroll-animate"
                        :style="{ animationDelay: `${index * 0.05}s` }">
                        <!-- 置顶标记 -->
                        <div v-if="article.isTop === true"
                            class="absolute top-0 right-0 z-10 flex items-center gap-1 px-3 py-2.5 text-xs font-medium text-amber-600 bg-amber-50 border-l border-b border-amber-100 rounded-bl-lg"
                            title="置顶">
                            <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20"><path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 0 0 .95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 0 0-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 0 0-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 0 0-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 0 0 .951-.69l1.07-3.292Z"/></svg>
                            置顶
                        </div>
                        <div class="flex flex-col sm:flex-row gap-0 sm:gap-6 items-stretch">
                            <!-- 封面图 -->
                            <a v-if="article.cover" @click="goArticleDetailPage(article.id)"
                                class="cursor-pointer flex-shrink-0 w-full sm:w-[260px] h-[180px] sm:h-auto overflow-hidden relative rounded-t-lg sm:rounded-l-lg sm:rounded-tr-none">
                                <div class="w-full h-full min-h-[180px] sm:min-h-[140px] relative overflow-hidden">
                                    <img
                                        class="w-full h-full object-cover transition-transform duration-500 group-hover:scale-105"
                                        :src="article.cover" alt="" />
                                    <div class="absolute inset-0 bg-gradient-to-t from-black/40 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300"></div>
                                </div>
                            </a>
                            <!-- 无封面占位图 -->
                            <div v-if="!article.cover" @click="goArticleDetailPage(article.id)"
                                class="cursor-pointer flex-shrink-0 w-full sm:w-[260px] h-[180px] sm:min-h-[140px] bg-gradient-to-br from-[#253341] to-[#1c2732] flex items-center justify-center rounded-t-lg sm:rounded-l-lg sm:rounded-tr-none">
                                <svg class="w-12 h-12 text-[#38444d]" fill="none" viewBox="0 0 24 24">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="m3 16 5-7 6 6.5m6.5 2.5L16 13l-4.286 6M14 10h.01M4 19h16a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1Z"/>
                                </svg>
                            </div>
                            <!-- 内容区 -->
                            <div class="flex-1 min-w-0 p-5 sm:py-5 sm:pr-6 sm:pl-0 flex flex-col justify-center">
                                <div>
                                    <!-- 标签 -->
                                    <div v-if="article.tags && article.tags.length > 0" class="flex flex-wrap gap-2 mb-3">
                                        <span v-for="(tag, tagIndex) in article.tags.slice(0, 3)" :key="tagIndex"
                                            @click.stop="goTagArticleListPage(tag.id, tag.name)"
                                            class="cursor-pointer px-2.5 py-1 text-xs bg-[var(--bg-hover)] text-[var(--text-secondary)] rounded-md hover:bg-[var(--color-primary)] hover:text-white transition-colors">
                                            {{ tag.name }}
                                        </span>
                                    </div>
                                    <!-- 标题 -->
                                    <a @click="goArticleDetailPage(article.id)" class="cursor-pointer">
                                        <h2 class="text-lg font-semibold text-[var(--text-heading)] hover:text-[var(--color-primary)] transition-colors duration-200 line-clamp-2 leading-snug mb-2">
                                            {{ article.title }}
                                        </h2>
                                    </a>
                                    <!-- 摘要 -->
                                    <p v-if="article.summary"
                                        class="text-sm text-[var(--text-secondary)] line-clamp-2 leading-relaxed">
                                        {{ article.summary }}
                                    </p>
                                </div>
                                <!-- 底部信息 -->
                                <div class="flex items-center gap-4 mt-4 pt-4 border-t border-[var(--border-light)] text-xs text-[var(--text-muted)]">
                                    <span class="flex items-center gap-1.5">
                                        <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 20 20">
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 1v3m5-3v3m5-3v3M1 7h18M5 11h10M2 3h16a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z" />
                                        </svg>
                                        {{ article.createTime }}
                                    </span>
                                    <a v-if="article.category"
                                        @click.stop="goCategoryArticleListPage(article.category.id, article.category.name)"
                                        class="flex items-center gap-1 cursor-pointer hover:text-[var(--color-primary)] transition-colors">
                                        <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 18 18">
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M1 5v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H1Zm0 0V2a1 1 0 0 1 1-1h5.443a1 1 0 0 1 .8.4l2.7 3.6H1Z" />
                                        </svg>
                                        {{ article.category.name }}
                                    </a>

                                    <!-- 阅读量 -->
                                    <span class="flex items-center gap-1" title="阅读量">
                                        <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24">
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                                                d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7Z"/>
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                                                d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z"/>
                                        </svg>
                                        {{ article.readNum }}
                                    </span>

                                </div>
                            </div>
                        </div>
                    </div>
                    </template>
                </div>

                <!-- 分页 -->
                <nav v-if="pages >= 1" class="mt-8 flex justify-center animate-fade-in">
                    <ul class="flex items-center gap-1.5">
                        <li>
                            <button @click="getArticles(current - 1)" :disabled="current <= 1"
                                :class="['flex items-center justify-center w-9 h-9 rounded-lg border transition-all text-sm',
                                    current > 1 ? 'text-[var(--text-body)] bg-[var(--bg-card)] border-[var(--border-base)] hover:bg-[var(--color-primary)] hover:text-white hover:border-[var(--color-primary)] cursor-pointer hover:scale-105 active:scale-95'
                                              : 'text-[var(--text-placeholder)] bg-[var(--bg-base)] border-[var(--border-light)] cursor-not-allowed']">
                                <svg class="w-3 h-3" fill="none" viewBox="0 0 6 10"><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 1 1 5l4 4" /></svg>
                            </button>
                        </li>
                        <li v-for="(pageNo, index) in pages" :key="index">
                            <button @click="getArticles(pageNo)"
                                :class="['flex items-center justify-center w-9 h-9 rounded-lg border transition-all text-sm hover:scale-105 active:scale-95',
                                    pageNo == current ? 'text-white bg-[var(--color-primary)] border-[var(--color-primary)] font-medium shadow-md'
                                                      : 'text-[var(--text-body)] bg-[var(--bg-card)] border-[var(--border-base)] hover:bg-[var(--bg-hover)] cursor-pointer']">
                                {{ index + 1 }}
                            </button>
                        </li>
                        <li>
                            <button @click="getArticles(current + 1)" :disabled="current >= pages"
                                :class="['flex items-center justify-center w-9 h-9 rounded-lg border transition-all text-sm',
                                    current < pages ? 'text-[var(--text-body)] bg-[var(--bg-card)] border-[var(--border-base)] hover:bg-[var(--color-primary)] hover:text-white hover:border-[var(--color-primary)] cursor-pointer hover:scale-105 active:scale-95'
                                                    : 'text-[var(--text-placeholder)] bg-[var(--bg-base)] border-[var(--border-light)] cursor-not-allowed']">
                                <svg class="w-3 h-3" fill="none" viewBox="0 0 6 10"><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m1 9 4-4-4-4" /></svg>
                            </button>
                        </li>
                    </ul>
                </nav>
            </div>

            <!-- 侧边栏 -->
            <aside class="hidden lg:block w-[280px] flex-shrink-0">
                <div class="sticky top-24 space-y-4">
                    <UserInfoCard></UserInfoCard>
                    <!-- 侧边栏公告 -->
                    <div v-if="announcement && announcement.isEnabled && announcementHidden"
                        class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card shadow-card p-4">
                        <div class="flex items-center justify-between mb-2">
                            <span class="text-sm font-medium text-[var(--text-heading)] flex items-center gap-1.5">
                                <svg class="w-4 h-4 text-blue-500" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" /></svg>
                                公告
                            </span>
                            <button @click="showAnnouncement" class="text-[var(--text-muted)] hover:text-[var(--text-body)] transition-colors">
                                <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
                            </button>
                        </div>
                        <div class="text-xs text-[var(--text-secondary)] markdown-body max-h-28 overflow-y-auto" v-html="renderedAnnouncementContent"></div>
                        <button @click="showAnnouncementModal = true" class="mt-2 text-xs text-[var(--color-primary)] hover:underline transition-colors">阅读全文 →</button>
                    </div>
                    <CategoryListCard></CategoryListCard>
                    <TagListCard></TagListCard>
                </div>
            </aside>
        </div>

        <!-- 留言墙视图 -->
        <div v-else-if="currentView === 'message-wall'" class="max-w-4xl mx-auto">
            <div class="space-y-5">
                <div class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card p-5 shadow-card">
                    <TypingPoem />
                    <MessageWallForm @comment-published="handleMessageWallPublished" />
                </div>
                <div class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card p-5 shadow-card">
                    <MessageWallPanel ref="messageWallRef" />
                </div>
            </div>
        </div>

        <!-- AI 聊天视图 -->
        <div v-else-if="currentView === 'ai-chat'" class="h-full">
            <ChatPanel />
        </div>
    </main>

    <!-- 返回顶部 -->
    <ScrollToTopButton v-if="currentView !== 'ai-chat'"></ScrollToTopButton>

    <Footer v-if="currentView !== 'ai-chat'"></Footer>
</template>

<script setup>
import Header from '@/layouts/frontend/components/Header.vue'
import Footer from '@/layouts/frontend/components/Footer.vue'
import Skeleton from '@/components/Skeleton.vue'
import UserInfoCard from '@/layouts/frontend/components/UserInfoCard.vue'
import CategoryListCard from '@/layouts/frontend/components/CategoryListCard.vue'
import TagListCard from '@/layouts/frontend/components/TagListCard.vue'
import ScrollToTopButton from '@/layouts/frontend/components/ScrollToTopButton.vue'
import MessageWallPanel from '@/components/MessageWallPanel.vue'
import MessageWallForm from '@/components/MessageWallForm.vue'
import TypingPoem from '@/components/TypingPoem.vue'
import ChatPanel from '@/components/chat/ChatPanel.vue'
import { onMounted, ref, computed, watch, nextTick } from 'vue'
import { marked } from 'marked'
import { setCache, getCache } from '@/composables/useCache'
import { useBlogSettingsStore } from '@/stores/blogsettings'

defineOptions({
    name: 'index'
})
import { getArticlePageList } from '@/api/frontend/article'
import { getAnnouncement } from '@/api/frontend/announcement'
import { useRouter, useRoute } from 'vue-router'

const router = useRouter()
const route = useRoute()
const blogSettingsStore = useBlogSettingsStore()

// 公告
const announcement = ref(null)
const announcementHidden = ref(localStorage.getItem('announcementHidden') === 'true')
const showAnnouncementModal = ref(false)

// 当前视图：article 或 message-wall
const currentView = ref('article')
const messageWallRef = ref(null)

// 打字机效果
const slogans = [
    '代码改变世界，分享成就未来',
    '技术人的精神角落',
    '记录成长的每一步',
    '分享技术与生活'
]
const currentSloganIndex = ref(0)
const currentSlogan = ref('')
const sloganTyping = ref(true)

const typeSlogan = () => {
    const fullSlogan = slogans[currentSloganIndex.value]
    if (sloganTyping.value) {
        if (currentSlogan.value.length < fullSlogan.length) {
            currentSlogan.value = fullSlogan.slice(0, currentSlogan.value.length + 1)
            setTimeout(typeSlogan, 80)
        } else {
            sloganTyping.value = false
            setTimeout(typeSlogan, 2000)
        }
    } else {
        if (currentSlogan.value.length > 0) {
            currentSlogan.value = currentSlogan.value.slice(0, -1)
            setTimeout(typeSlogan, 40)
        } else {
            sloganTyping.value = true
            currentSloganIndex.value = (currentSloganIndex.value + 1) % slogans.length
            setTimeout(typeSlogan, 300)
        }
    }
}

// 分类和标签数量
const categoryCount = ref(0)
const tagCount = ref(0)

// 文章卡片 Intersection Observer 动画
const articleCardRefs = ref([])
let observer = null

const initScrollAnimation = () => {
    observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate-in')
                observer.unobserve(entry.target)
            }
        })
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    })

    nextTick(() => {
        articleCardRefs.value.forEach(card => {
            if (card) observer.observe(card)
        })
    })
}

// 根据 URL query 参数切换视图
const updateViewFromQuery = () => {
    if (route.query.view === 'message-wall') {
        currentView.value = 'message-wall'
    } else if (route.query.view === 'ai-chat') {
        currentView.value = 'ai-chat'
    } else {
        currentView.value = 'article'
    }
}

// 初始化和监听路由变化
onMounted(() => {
    updateViewFromQuery();
    typeSlogan();
    // 获取分类和标签数量用于显示
    categoryCount.value = blogSettingsStore.blogSettings.categoryCount || 0
    tagCount.value = blogSettingsStore.blogSettings.tagCount || 0
    initScrollAnimation()
})

watch(() => route.query, updateViewFromQuery, { immediate: false })

const renderedAnnouncementContent = computed(() => {
    if (announcement.value?.content) {
        return marked(announcement.value.content)
    }
    return ''
})

function hideAnnouncement() {
    announcementHidden.value = true
    localStorage.setItem('announcementHidden', 'true')
}

function showAnnouncement() {
    announcementHidden.value = false
    localStorage.setItem('announcementHidden', 'false')
}

// 获取公告（带缓存）
const loadAnnouncement = () => {
    const cached = getCache('announcement')
    if (cached) {
        announcement.value = cached
        return
    }
    getAnnouncement().then(res => {
        if (res.success && res.data && res.data.id) {
            announcement.value = res.data
            setCache('announcement', res.data, 10 * 60 * 1000) // 缓存10分钟
        }
    })
}
loadAnnouncement()

// 跳转分类文章列表页
const goCategoryArticleListPage = (id, name) => {
    // 跳转时通过 query 携带参数（分类 ID、分类名称）
    router.push({path: '/category/article/list', query: {id, name}})
}

// 留言墙发布成功后的回调
const handleMessageWallPublished = () => {
    if (messageWallRef.value) {
        messageWallRef.value.refresh()
    }
}

// 文章集合
const articles = ref([])
// 当前页码
const current = ref(1)
// 每页显示的文章数
const size = ref(10)
// 总文章数
const total = ref(0)
// 总共多少页
const pages = ref(0)
// 是否已初始加载
const articlesLoaded = ref(false)
// 是否正在加载
const isLoading = ref(false)


function getArticles(currentNo) {
    // 上下页是否能点击判断，当要跳转上一页且页码小于 1 时，则不允许跳转；当要跳转下一页且页码大于总页数时，则不允许跳转
    if (currentNo < 1 || (pages.value > 0 && currentNo > pages.value)) return

    // 尝试读取缓存
    const cacheKey = `articles_page_${currentNo}_${size.value}`
    const cached = getCache(cacheKey)
    if (cached) {
        articles.value = cached.list
        current.value = cached.pageNum
        size.value = cached.pageSize
        total.value = cached.total
        pages.value = Math.ceil(cached.total / cached.pageSize)
        articlesLoaded.value = true
        isLoading.value = false
        return
    }

    isLoading.value = true
    // 调用分页接口渲染数据
    getArticlePageList({current: currentNo, size: size.value}).then((res) => {
        isLoading.value = false
        if (res.success) {
            articles.value = res.data.list || []
            current.value = res.data.pageNum || 1
            size.value = res.data.pageSize || 10
            total.value = res.data.total || 0
            pages.value = Math.ceil(total.value / size.value)
            articlesLoaded.value = true

            // 缓存文章列表数据 5 分钟
            setCache(cacheKey, {
                list: articles.value,
                pageNum: current.value,
                pageSize: size.value,
                total: total.value
            }, 5 * 60 * 1000)
        }
    })
}

// 仅在文章视图下加载文章（如果还没加载过）
watch(currentView, (newView) => {
    if (newView === 'article' && !articlesLoaded.value) {
        getArticles(current.value)
    }
}, { immediate: true })

// 跳转文章详情页
const goArticleDetailPage = (articleId) => {
    router.push('/article/' + articleId)
}

// 跳转标签文章列表页
const goTagArticleListPage = (id, name) => {
    // 跳转时通过 query 携带参数（标签 ID、标签名称）
    router.push({path: '/tag/article/list', query: {id, name}})
}
    </script>

    <style scoped>
/* 打字机光标闪烁 */
@keyframes blink {
    0%, 50% { opacity: 1; }
    51%, 100% { opacity: 0; }
}
.animate-blink {
    animation: blink 1s infinite;
}

/* 淡入动画 */
@keyframes fadeIn {
    from {
        opacity: 0;
        transform: translateY(10px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
.animate-fade-in {
    animation: fadeIn 0.4s ease-out forwards;
}

/* 文章卡片加载动画 */
@keyframes slideUp {
    from {
        opacity: 0;
        transform: translateY(20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.article-card {
    animation: slideUp 0.5s ease-out forwards;
    opacity: 0;
}

/* 滚动淡入动画 */
.scroll-animate {
    opacity: 0;
    transform: translateY(30px);
    transition: opacity 0.6s ease-out, transform 0.6s ease-out;
}

.scroll-animate.animate-in {
    opacity: 1;
    transform: translateY(0);
}
</style>

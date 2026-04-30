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
        <div v-if="currentView === 'article'" class="grid grid-cols-1 xl:grid-cols-[1fr_320px] gap-6">
            <!-- 文章列表 -->
            <div class="min-w-0 article-list-panel">
                <div class="article-section-heading">
                    <h1>最新文章</h1>
                    <p>记录技术点滴，分享学习与实践经验</p>
                </div>
                <div class="article-grid">
                    <!-- 骨架屏 -->
                    <template v-if="isLoading">
                        <div v-for="i in 6" :key="i" class="article-grid-skeleton">
                            <Skeleton width="100%" height="170px" border-radius="8px" />
                            <div class="space-y-3 p-4">
                                <Skeleton width="80%" height="1.5rem" />
                                <Skeleton width="100%" height="2.5rem" />
                                <div class="flex gap-4">
                                    <Skeleton width="80px" height="1rem" />
                                    <Skeleton width="60px" height="1rem" />
                                </div>
                            </div>
                        </div>
                    </template>

                    <template v-else>
                    <article v-for="(article, index) in articles" :key="index"
                        ref="articleCardRefs"
                        class="article-card article-stream-card scroll-animate"
                        :style="{ animationDelay: `${index * 0.05}s` }">
                        <!-- 置顶标记 -->
                        <div v-if="article.isTop === true"
                            class="article-pin-badge"
                            title="置顶">
                            <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20"><path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 0 0 .95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 0 0-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 0 0-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 0 0-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 0 0 .951-.69l1.07-3.292Z"/></svg>
                            置顶
                        </div>
                        <div class="article-stream-card__inner">
                            <!-- 封面图 -->
                            <a v-if="article.cover" @click="goArticleDetailPage(article.id)"
                                class="article-stream-card__cover">
                                <div class="article-stream-card__cover-inner">
                                    <img
                                        class="article-stream-card__image"
                                        @error="article.cover = ''"
                                        :src="article.cover" alt="" />
                                    <div class="article-stream-card__image-mask"></div>
                                </div>
                            </a>
                            <!-- 无封面占位图 -->
                            <div v-if="!article.cover" @click="goArticleDetailPage(article.id)"
                                class="article-stream-card__cover article-stream-card__cover--empty">
                                <svg class="w-12 h-12 text-[#38444d]" fill="none" viewBox="0 0 24 24">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="m3 16 5-7 6 6.5m6.5 2.5L16 13l-4.286 6M14 10h.01M4 19h16a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1Z"/>
                                </svg>
                            </div>
                            <!-- 内容区 -->
                            <div class="article-stream-card__body">
                                <div>
                                    <!-- 标签 -->
                                    <div v-if="article.tags && article.tags.length > 0" class="article-stream-card__tags">
                                        <span v-for="(tag, tagIndex) in article.tags.slice(0, 3)" :key="tagIndex"
                                            @click.stop="goTagArticleListPage(tag.id, tag.name)"
                                            class="article-stream-card__tag">
                                            {{ tag.name }}
                                        </span>
                                    </div>
                                    <!-- 标题 -->
                                    <a @click="goArticleDetailPage(article.id)" class="cursor-pointer">
                                        <h2 class="article-stream-card__title">
                                            {{ article.title }}
                                        </h2>
                                    </a>
                                    <!-- 摘要 -->
                                    <p v-if="article.summary"
                                        class="article-stream-card__summary">
                                        {{ article.summary }}
                                    </p>
                                </div>
                                <!-- 底部信息 -->
                                <div class="article-stream-card__meta">
                                    <span>
                                        <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 20 20">
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 1v3m5-3v3m5-3v3M1 7h18M5 11h10M2 3h16a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z" />
                                        </svg>
                                        {{ formatArticleDate(article.createTime || article.createDate) }}
                                    </span>
                                    <a v-if="article.category"
                                        @click.stop="goCategoryArticleListPage(article.category.id, article.category.name)"
                                        class="article-stream-card__category">
                                        <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 18 18">
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M1 5v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H1Zm0 0V2a1 1 0 0 1 1-1h5.443a1 1 0 0 1 .8.4l2.7 3.6H1Z" />
                                        </svg>
                                        {{ article.category.name }}
                                    </a>

                                    <!-- 阅读量 -->
                                    <span title="阅读量">
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
                    </article>
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
            <aside class="block w-full xl:w-[320px] xl:flex-shrink-0">
                <div class="space-y-4 lg:sticky lg:top-24">
                    <UserInfoCard></UserInfoCard>
                    <section v-if="subscribeCardVisible" class="subscribe-card">
                        <div class="subscribe-card__title">
                            <i class="far fa-envelope"></i>
                            <span>{{ subscribeCard.title }}</span>
                        </div>
                        <p>{{ subscribeCard.description }}</p>
                        <form class="subscribe-card__form" @submit.prevent="handleSubscribeSubmit">
                            <input
                                v-model.trim="subscribeEmail"
                                type="email"
                                :placeholder="subscribeCard.placeholder"
                                autocomplete="email" />
                            <button type="submit" :disabled="subscribeSubmitting">
                                {{ subscribeSubmitting ? '订阅中...' : subscribeCard.buttonText }}
                            </button>
                        </form>
                    </section>
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
        <div v-else-if="currentView === 'message-wall'" class="message-wall-page">
            <section class="message-wall-hero">
                <div class="message-wall-hero__content">
                    <div class="message-wall-hero__title-row">
                        <h1>留言板</h1>
                    </div>
                    <p>路过山海，欢迎在这里留下你的足迹与想法。</p>
                    <div class="message-wall-stats">
                        <div class="message-wall-stat">
                            <span>
                                <small>留言总数</small>
                                <strong>{{ messageWallStats.total }}</strong>
                            </span>
                        </div>
                        <div class="message-wall-stat">
                            <span>
                                <small>今日留言</small>
                                <strong>{{ messageWallStats.today }}</strong>
                            </span>
                        </div>
                        <div class="message-wall-stat">
                            <span>
                                <small>活跃访客</small>
                                <strong>{{ messageWallStats.visitors }}</strong>
                            </span>
                        </div>
                    </div>
                </div>
                <div class="message-wall-hero__visual" aria-hidden="true">
                    <img :src="messageWallHeroImage" alt="" />
                </div>
            </section>

            <div class="message-wall-layout">
                <div class="message-wall-main">
                    <MessageWallForm @comment-published="handleMessageWallPublished" />
                    <MessageWallPanel ref="messageWallRef" @stats-change="handleMessageWallStatsChange" />
                </div>

                <aside class="message-wall-aside">
                    <section class="message-wall-side-card message-wall-about">
                        <img :src="blogSettingsStore.blogSettings.avatar" alt="" />
                        <div>
                            <h2>关于留言板</h2>
                            <p>每一条留言都是一份温暖的遇见，感谢你的到来与支持。</p>
                        </div>
                    </section>

                    <section class="message-wall-side-card">
                        <h2><i class="fas fa-bullhorn"></i> 留言须知</h2>
                        <ul>
                            <li>请文明留言，友善交流</li>
                            <li>禁止发布广告、违法及敏感信息</li>
                            <li>尊重他人观点，理性讨论</li>
                            <li>有问题请先搜索，感谢理解</li>
                        </ul>
                    </section>

                    <section class="message-wall-side-card">
                        <h2><i class="fas fa-chart-line"></i> 今日动态</h2>
                        <div class="message-wall-dynamics">
                            <span>今日留言</span>
                            <strong>{{ messageWallStats.today }}</strong>
                        </div>
                        <div class="message-wall-dynamics">
                            <span>最新评论</span>
                            <strong>{{ messageWallStats.lastTime || '--' }}</strong>
                        </div>
                        <div class="message-wall-dynamics">
                            <span>活跃访客</span>
                            <strong>{{ messageWallStats.visitors }}</strong>
                        </div>
                    </section>
                </aside>
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
import ChatPanel from '@/components/chat/ChatPanel.vue'
import messageWallHeroImage from '@/assets/liuyanban.png'
import { onMounted, ref, computed, watch, nextTick } from 'vue'
import { marked } from 'marked'
import { setCache, getCache } from '@/composables/useCache'
import { useBlogSettingsStore } from '@/stores/blogsettings'
import { showMessage } from '@/composables/util'
import { subscribeByEmail } from '@/api/frontend/subscribe'

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
const messageWallStats = ref({
    total: 0,
    today: 0,
    visitors: 0,
    lastTime: ''
})
const subscribeEmail = ref('')
const subscribeSubmitting = ref(false)

const subscribeCard = computed(() => {
    const settings = blogSettingsStore.blogSettings || {}
    return {
        title: '订阅更新',
        description: '订阅后，最新文章将通过邮件发送给你',
        placeholder: '输入你的邮箱地址',
        buttonText: '订阅'
    }
})

const subscribeCardVisible = computed(() => {
    const settings = blogSettingsStore.blogSettings || {}
    return settings.isSubscribeCardOpen !== false
})

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
const formatArticleDate = (time) => {
    if (!time) return ''
    const date = new Date(time)
    if (Number.isNaN(date.getTime())) return time
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    return `${year}-${month}-${day}`
}

const handleSubscribeSubmit = async () => {
    const email = subscribeEmail.value.trim()
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
        showMessage('请输入正确的邮箱地址', 'warning')
        return
    }
    if (subscribeSubmitting.value) return
    subscribeSubmitting.value = true
    try {
        const res = await subscribeByEmail(email)
        if (res.success) {
            subscribeEmail.value = ''
            showMessage(res.message || '订阅成功，请查收确认邮件', 'success')
        } else {
            showMessage(res.message || '订阅失败', 'warning')
        }
    } finally {
        subscribeSubmitting.value = false
    }
}

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

const handleMessageWallStatsChange = (stats) => {
    messageWallStats.value = {
        ...messageWallStats.value,
        ...stats
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

.article-list-panel {
    padding: 28px;
    border: 1px solid var(--border-base);
    border-radius: 8px;
    background: var(--bg-card);
    box-shadow: var(--shadow-sm);
}

.article-section-heading {
    margin-bottom: 22px;
}

.article-section-heading h1 {
    margin: 0 0 6px;
    color: var(--text-heading);
    font-size: 22px;
    line-height: 1.25;
    font-weight: 800;
    letter-spacing: 0;
}

.article-section-heading p {
    margin: 0;
    color: var(--text-secondary);
    font-size: 14px;
}

.article-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
    gap: 22px;
}

.article-grid-skeleton {
    overflow: hidden;
    border: 1px solid var(--border-base);
    border-radius: 8px;
    background: var(--bg-card);
}

.article-stream-card {
    position: relative;
    overflow: hidden;
    display: flex;
    flex-direction: column;
    border-radius: 8px;
    background: var(--bg-card);
    border: 1px solid var(--border-base);
    box-shadow: var(--shadow-sm);
    transition: transform 0.22s ease, border-color 0.22s ease, box-shadow 0.22s ease, background 0.22s ease;
}

.article-stream-card:hover {
    transform: translateY(-3px);
    border-color: var(--border-heavy);
    box-shadow: var(--shadow-md);
}

.article-stream-card__inner {
    display: grid;
    grid-template-columns: 1fr;
    height: 100%;
}

.article-stream-card__cover {
    position: relative;
    display: block;
    aspect-ratio: 16 / 8;
    min-height: 0;
    cursor: pointer;
    overflow: hidden;
    background: linear-gradient(135deg, #1f2937, #111827);
}

.article-stream-card__cover-inner,
.article-stream-card__image {
    width: 100%;
    height: 100%;
}

.article-stream-card__image {
    display: block;
    object-fit: cover;
    transition: transform 0.55s ease;
}

.article-stream-card:hover .article-stream-card__image {
    transform: scale(1.055);
}

.article-stream-card__image-mask {
    position: absolute;
    inset: 0;
    background: linear-gradient(180deg, transparent 42%, rgba(15, 23, 42, 0.26));
    opacity: 0;
    transition: opacity 0.22s ease;
}

.article-stream-card:hover .article-stream-card__image-mask {
    opacity: 1;
}

.article-stream-card__cover--empty {
    display: flex;
    align-items: center;
    justify-content: center;
    background:
        linear-gradient(135deg, rgba(59, 130, 246, 0.2), rgba(16, 185, 129, 0.16)),
        var(--bg-hover);
    color: var(--text-muted);
}

.article-stream-card__body {
    display: flex;
    min-width: 0;
    flex-direction: column;
    justify-content: space-between;
    gap: 22px;
    min-height: 210px;
    padding: 18px;
}

.article-stream-card__tags {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-bottom: 12px;
}

.article-stream-card__tag {
    cursor: pointer;
    padding: 5px 10px;
    border-radius: 999px;
    color: var(--text-secondary);
    background: var(--bg-hover);
    border: 1px solid var(--border-light);
    font-size: 12px;
    font-weight: 650;
    line-height: 1;
    transition: all 0.18s ease;
}

.article-stream-card__tag:hover {
    color: #fff;
    background: var(--color-primary);
    border-color: var(--color-primary);
}

.article-stream-card__title {
    display: -webkit-box;
    margin: 0 0 10px;
    overflow: hidden;
    color: var(--text-heading);
    font-size: 18px;
    font-weight: 800;
    line-height: 1.42;
    letter-spacing: 0;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    transition: color 0.18s ease;
}

.article-stream-card__title:hover {
    color: var(--color-primary);
}

.article-stream-card__summary {
    display: -webkit-box;
    margin: 0;
    overflow: hidden;
    color: var(--text-secondary);
    font-size: 14px;
    line-height: 1.65;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
}

.article-stream-card__meta {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 12px;
    padding-top: 0;
    border-top: 0;
    color: var(--text-muted);
    font-size: 12px;
}

.article-stream-card__meta span,
.article-stream-card__meta a {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    min-width: 0;
}

.article-stream-card__category {
    cursor: pointer;
    transition: color 0.18s ease;
}

.article-stream-card__category:hover {
    color: var(--color-primary);
}

.article-pin-badge {
    position: absolute;
    top: 14px;
    right: 14px;
    z-index: 10;
    display: inline-flex;
    align-items: center;
    gap: 5px;
    padding: 7px 10px;
    border-radius: 999px;
    color: #b45309;
    background: rgba(255, 251, 235, 0.94);
    border: 1px solid rgba(251, 191, 36, 0.35);
    box-shadow: 0 10px 22px rgba(15, 23, 42, 0.12);
    font-size: 12px;
    font-weight: 800;
}

.dark .article-stream-card {
    background: var(--bg-card);
    box-shadow: var(--shadow-sm);
}

.dark .article-stream-card:hover {
    box-shadow: var(--shadow-md);
}

.dark .article-pin-badge {
    color: #fbbf24;
    background: rgba(120, 53, 15, 0.78);
    border-color: rgba(251, 191, 36, 0.28);
}

/* 留言板布局 */
.subscribe-card {
    padding: 20px;
    border: 1px solid var(--border-base);
    border-radius: 8px;
    background: var(--bg-card);
    box-shadow: var(--shadow-sm);
}

.subscribe-card__title {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 12px;
    color: var(--text-heading);
    font-size: 18px;
    line-height: 1.3;
    font-weight: 800;
}

.subscribe-card__title i {
    color: var(--text-heading);
    font-size: 18px;
}

.subscribe-card p {
    margin: 0 0 16px;
    color: var(--text-secondary);
    font-size: 14px;
    line-height: 1.7;
}

.subscribe-card__form {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 10px;
}

.subscribe-card__form input {
    min-width: 0;
    height: 40px;
    padding: 0 14px;
    border: 1px solid var(--border-base);
    border-radius: 6px;
    color: var(--text-heading);
    background: var(--bg-base);
    outline: none;
    transition: border-color 0.18s ease, box-shadow 0.18s ease;
}

.subscribe-card__form input::placeholder {
    color: var(--text-placeholder);
}

.subscribe-card__form input:focus {
    border-color: var(--color-primary);
    box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.12);
}

.subscribe-card__form button {
    height: 40px;
    padding: 0 18px;
    border: 0;
    border-radius: 6px;
    color: #fff;
    background: var(--color-primary);
    font-weight: 700;
    white-space: nowrap;
    transition: transform 0.18s ease, filter 0.18s ease;
}

.subscribe-card__form button:hover {
    filter: brightness(1.04);
    transform: translateY(-1px);
}

.subscribe-card__form button:disabled {
    cursor: not-allowed;
    opacity: 0.72;
    transform: none;
}

.message-wall-page {
    max-width: 1280px;
    margin: 0 auto;
}

.message-wall-hero {
    position: relative;
    display: grid;
    grid-template-columns: minmax(390px, 0.78fr) minmax(0, 1.22fr);
    align-items: center;
    gap: 18px;
    min-height: 250px;
    margin-bottom: 24px;
    padding: 34px 40px 28px;
    border: 1px solid var(--border-base);
    border-radius: 12px;
    background:
        radial-gradient(circle at 63% 12%, rgba(59, 130, 246, 0.12), transparent 32%),
        linear-gradient(115deg, var(--bg-card) 0%, var(--bg-card) 46%, rgba(239, 246, 255, 0.56) 100%),
        var(--bg-card);
    box-shadow: var(--shadow-md);
    overflow: hidden;
}

.message-wall-hero::after {
    display: none;
}

.message-wall-hero__content {
    position: relative;
    z-index: 3;
    max-width: 560px;
    min-width: 0;
}

.message-wall-hero__title-row {
    display: flex;
    align-items: center;
    gap: 14px;
    margin-bottom: 12px;
}

.message-wall-hero__title-row h1 {
    margin: 0;
    color: var(--text-heading);
    font-size: 34px;
    line-height: 1.2;
    font-weight: 800;
    letter-spacing: 0;
}

.message-wall-hero p {
    margin: 0 0 28px;
    color: var(--text-secondary);
    font-size: 15px;
}

.message-wall-stats {
    display: grid;
    grid-template-columns: repeat(3, minmax(118px, 1fr));
    gap: 14px;
}

.message-wall-stat {
    display: flex;
    align-items: center;
    gap: 12px;
    min-width: 0;
    padding: 13px 15px;
    border-radius: 8px;
    border: 1px solid var(--border-light);
    background: rgba(255, 255, 255, 0.66);
    backdrop-filter: blur(12px);
    box-shadow: 0 12px 28px rgba(15, 23, 42, 0.05);
}

.message-wall-stat::before {
    content: "";
    width: 34px;
    height: 34px;
    border-radius: 50%;
    flex: 0 0 34px;
    background:
        linear-gradient(135deg, rgba(255, 255, 255, 0.86), rgba(255, 255, 255, 0.22)),
        var(--color-primary);
    box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.45), 0 10px 20px rgba(37, 99, 235, 0.16);
}

.message-wall-stat:nth-child(2)::before {
    background:
        linear-gradient(135deg, rgba(255, 255, 255, 0.86), rgba(255, 255, 255, 0.2)),
        #fb7185;
    box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.45), 0 10px 20px rgba(251, 113, 133, 0.16);
}

.message-wall-stat:nth-child(3)::before {
    background:
        linear-gradient(135deg, rgba(255, 255, 255, 0.86), rgba(255, 255, 255, 0.2)),
        #22c55e;
    box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.45), 0 10px 20px rgba(34, 197, 94, 0.16);
}

.message-wall-stat small,
.message-wall-dynamics span {
    display: block;
    color: var(--text-secondary);
    font-size: 12px;
}

.message-wall-stat strong,
.message-wall-dynamics strong {
    display: block;
    color: var(--text-heading);
    font-size: 19px;
    line-height: 1.2;
    font-weight: 800;
}

.message-wall-hero__visual {
    position: relative;
    z-index: 2;
    min-height: 214px;
    align-self: stretch;
    pointer-events: none;
    overflow: hidden;
    border-radius: 10px;
    background: rgba(239, 246, 255, 0.42);
}

.message-wall-hero__visual::before {
    content: "";
    position: absolute;
    inset: 0;
    z-index: 2;
    pointer-events: none;
    background:
        linear-gradient(90deg, rgba(255, 255, 255, 0.84) 0%, rgba(255, 255, 255, 0.34) 18%, transparent 42%),
        linear-gradient(180deg, rgba(255, 255, 255, 0.18) 0%, transparent 20%, transparent 82%, rgba(255, 255, 255, 0.18) 100%);
}

.message-wall-hero__visual img {
    position: absolute;
    inset: 0;
    display: block;
    width: 100%;
    max-width: none;
    height: 100%;
    object-fit: cover;
    object-position: center center;
    transform: none;
    filter: brightness(1.08) saturate(0.92) contrast(0.96);
    opacity: 0.92;
    mix-blend-mode: normal;
}

.dark .message-wall-hero {
    background:
        radial-gradient(circle at 64% 14%, rgba(96, 165, 250, 0.12), transparent 34%),
        linear-gradient(115deg, var(--bg-card) 0%, var(--bg-card) 46%, rgba(30, 41, 59, 0.48) 100%),
        var(--bg-card);
}

.dark .message-wall-stat {
    background: rgba(15, 23, 42, 0.5);
}

.dark .message-wall-hero__visual {
    background: rgba(15, 23, 42, 0.3);
}

.dark .message-wall-hero__visual::before {
    background:
        linear-gradient(90deg, rgba(15, 23, 42, 0.9) 0%, rgba(15, 23, 42, 0.42) 18%, transparent 42%),
        linear-gradient(180deg, rgba(15, 23, 42, 0.18) 0%, transparent 22%, transparent 82%, rgba(15, 23, 42, 0.18) 100%);
}

.dark .message-wall-hero__visual img {
    filter: brightness(0.82) saturate(0.96) contrast(1.02);
    opacity: 0.7;
    mix-blend-mode: normal;
}

.message-wall-layout {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 340px;
    gap: 28px;
    align-items: start;
}

.message-wall-main {
    display: grid;
    gap: 18px;
    min-width: 0;
}

.message-wall-aside {
    display: grid;
    gap: 18px;
    position: sticky;
    top: 96px;
}

.message-wall-side-card {
    padding: 22px;
    border: 1px solid var(--border-base);
    border-radius: 8px;
    background: var(--bg-card);
    box-shadow: var(--shadow-sm);
}

.message-wall-side-card h2 {
    display: flex;
    align-items: center;
    gap: 10px;
    margin: 0 0 14px;
    color: var(--text-heading);
    font-size: 18px;
    line-height: 1.3;
    font-weight: 800;
}

.message-wall-side-card h2 i {
    color: var(--color-primary);
}

.message-wall-about {
    display: flex;
    align-items: center;
    gap: 16px;
}

.message-wall-about img {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    object-fit: cover;
    border: 1px solid var(--border-base);
    background: var(--bg-hover);
    flex-shrink: 0;
}

.message-wall-about p,
.message-wall-side-card li {
    color: var(--text-secondary);
    font-size: 14px;
    line-height: 1.7;
}

.message-wall-about p {
    margin: 0;
}

.message-wall-side-card ul {
    display: grid;
    gap: 9px;
    margin: 0;
    padding: 0;
    list-style: none;
}

.message-wall-side-card li {
    position: relative;
    padding-left: 18px;
}

.message-wall-side-card li::before {
    content: "";
    position: absolute;
    left: 2px;
    top: 0.72em;
    width: 5px;
    height: 5px;
    border-radius: 50%;
    background: var(--color-primary);
}

.message-wall-dynamics {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 11px 0;
    border-top: 1px solid var(--border-light);
}

.message-wall-dynamics:first-of-type {
    border-top: 0;
    padding-top: 0;
}

.message-wall-dynamics span {
    font-size: 13px;
    color: var(--text-secondary);
}

.message-wall-dynamics strong {
    min-width: 46px;
    padding: 3px 8px;
    border-radius: 999px;
    color: var(--color-primary);
    background: var(--bg-hover);
    font-size: 16px;
    font-weight: 700;
    text-align: center;
}

.message-wall-side-card:last-child {
    padding-bottom: 16px;
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

@media (max-width: 640px) {
    .article-stream-card__inner {
        grid-template-columns: 1fr;
    }

    .article-stream-card__cover {
        min-height: 190px;
        aspect-ratio: 16 / 9;
    }

    .article-stream-card__body {
        padding: 20px;
    }

    .article-stream-card__title {
        font-size: 19px;
    }

    .article-pin-badge {
        top: 10px;
        right: 10px;
    }
}

@media (max-width: 1024px) {
    .message-wall-layout {
        grid-template-columns: 1fr;
    }

    .message-wall-hero {
        grid-template-columns: minmax(0, 1fr);
        min-height: 0;
        padding: 30px 32px;
    }

    .message-wall-hero::after {
        display: none;
    }

    .message-wall-hero__content {
        max-width: none;
    }

    .message-wall-hero__visual {
        position: absolute;
        inset: 0;
        z-index: 1;
        min-height: 0;
        border-radius: inherit;
    }

    .message-wall-hero__visual::before {
        inset: 0;
        background:
            linear-gradient(90deg, var(--bg-card) 0%, rgba(255, 255, 255, 0.44) 34%, transparent 82%);
    }

    .message-wall-hero__visual img {
        inset: 0;
        width: 100%;
        height: 100%;
        object-fit: cover;
        object-position: center center;
        transform: none;
        opacity: 0.42;
        filter: brightness(1.08) saturate(0.86) contrast(0.94);
    }

    .message-wall-aside {
        position: static;
        grid-template-columns: 1fr;
    }
}

@media (max-width: 640px) {
    .message-wall-hero {
        padding: 26px 20px 22px;
        border-radius: 10px;
    }

    .message-wall-hero__visual img {
        inset: 0;
        width: 100%;
        height: 100%;
        object-fit: cover;
        object-position: center top;
        opacity: 0.48;
    }

    .message-wall-stats {
        grid-template-columns: 1fr;
        gap: 12px;
        margin-top: 168px;
    }

    .message-wall-stat {
        padding: 15px 16px;
        background: rgba(255, 255, 255, 0.82);
    }

    .message-wall-hero__title-row h1 {
        font-size: 30px;
    }

    .message-wall-hero p {
        max-width: 23em;
        margin-bottom: 0;
        line-height: 1.75;
    }

    .dark .message-wall-hero__visual::before {
        background:
            linear-gradient(90deg, var(--bg-card) 0%, rgba(15, 23, 42, 0.46) 34%, transparent 82%);
    }

    .dark .message-wall-stat {
        background: rgba(15, 23, 42, 0.68);
    }
}
</style>

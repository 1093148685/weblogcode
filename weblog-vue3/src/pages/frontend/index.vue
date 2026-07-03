<template>
    <Header></Header>

    <!-- 主内容区域 -->
    <main class="container max-w-screen-xl mx-auto px-4 md:px-6 py-4">

        <!-- 公告区域 -->
        <div v-if="announcement && announcement.isEnabled && !announcementHidden && currentView === 'article'" class="mb-6">
            <div class="bg-white border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700 p-4">
                <div class="flex items-start justify-between">
                    <div class="flex items-start flex-1">
                        <svg class="w-5 h-5 text-blue-500 mr-3 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
                        </svg>
                        <div class="markdown-body text-gray-700 dark:text-gray-300 announcement-content" v-html="renderedAnnouncementContent"></div>
                    </div>
                    <div class="flex items-center ml-3 flex-shrink-0 gap-2">
                        <button @click="showAnnouncementModal = true" class="text-sm text-blue-500 hover:text-blue-600 transition-colors">展开</button>
                        <button @click="hideAnnouncement" class="text-gray-400 hover:text-gray-600 dark:hover:text-white transition-colors">
                            <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
                            </svg>
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <!-- 公告弹窗 -->
        <el-dialog v-model="showAnnouncementModal" width="600px" :show-close="true">
            <template #header>
                <div class="flex items-center">
                    <svg class="w-4 h-4 text-blue-500 mr-2" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
                    </svg>
                    <span class="text-base font-medium">公告</span>
                </div>
            </template>
            <div class="markdown-body text-gray-700 dark:text-gray-300 max-h-[60vh] overflow-y-auto" v-html="renderedAnnouncementContent"></div>
            <template #footer>
                <el-button type="primary" size="small" @click="showAnnouncementModal = false">知道了</el-button>
            </template>
        </el-dialog>

        <!-- 文章视图 -->
        <div v-if="currentView === 'article'">
            <div class="grid grid-cols-4 gap-7">
                <!-- 左边栏，占用 3 列 -->
                <div class="col-span-4 md:col-span-3 mb-3">
                    <div class="grid grid-cols-2 gap-4 slide-up-stagger">
                        <!-- 骨架屏 -->
                        <template v-if="isLoading">
                            <div v-for="i in 6" :key="i" class="col-span-2 md:col-span-1">
                                <div class="bg-white border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700">
                                    <Skeleton width="100%" height="190px" border-radius="8px 8px 0 0" />
                                    <div class="p-5 space-y-3">
                                        <Skeleton width="80%" height="1.5rem" />
                                        <Skeleton width="100%" height="2.5rem" />
                                        <div class="flex gap-4">
                                            <Skeleton width="80px" height="1rem" />
                                            <Skeleton width="60px" height="1rem" />
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </template>

                        <template v-else>
                            <div v-for="(article, index) in articles" :key="index" class="col-span-2 md:col-span-1">
                                <div class="relative bg-white hover:scale-[1.03] h-full border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700 transition-transform duration-200">
                                    <!-- 文章封面 -->
                                    <a @click="goArticleDetailPage(article.id)" class="cursor-pointer">
                                        <img class="rounded-t-lg h-48 w-full object-cover" :src="article.cover" @error="article.cover = ''" />
                                    </a>
                                    <div class="p-5 flex flex-col min-h-max">
                                        <!-- 标签 -->
                                        <div class="mb-3">
                                            <span v-for="(tag, tagIndex) in article.tags" :key="tagIndex" @click="goTagArticleListPage(tag.id, tag.name)"
                                                class="cursor-pointer bg-green-100 text-green-800 text-xs font-medium mr-2 px-2.5 py-0.5 rounded hover:bg-green-200 hover:text-green-900 dark:bg-green-900 dark:hover:bg-green-950 dark:text-green-300">
                                                {{ tag.name }}
                                            </span>
                                        </div>
                                        <!-- 文章标题 -->
                                        <a @click="goArticleDetailPage(article.id)" class="cursor-pointer">
                                            <h2 class="mb-2 text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
                                                <span class="hover:border-gray-600 hover:border-b-2 dark:hover:border-gray-400">{{ article.title }}</span>
                                            </h2>
                                        </a>
                                        <!-- 文章摘要 -->
                                        <p v-if="article.summary" class="mb-3 font-normal text-gray-500 dark:text-gray-400">{{ article.summary }}</p>
                                        <!-- 文章发布时间、所属分类 -->
                                        <p class="mt-auto flex items-center font-normal text-gray-400 text-sm dark:text-gray-400">
                                            <svg class="inline w-3 h-3 mr-2 text-gray-400" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
                                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 1v3m5-3v3m5-3v3M1 7h18M5 11h10M2 3h16a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z" />
                                            </svg>
                                            {{ formatArticleDate(article.createTime || article.createDate) }}
                                            <svg class="inline w-3 h-3 ml-5 mr-2 text-gray-400" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 18 18">
                                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M1 5v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H1Zm0 0V2a1 1 0 0 1 1-1h5.443a1 1 0 0 1 .8.4l2.7 3.6H1Z" />
                                            </svg>
                                            <a v-if="article.category" @click="goCategoryArticleListPage(article.category.id, article.category.name)" class="cursor-pointer text-gray-400 hover:underline">{{ article.category.name }}</a>
                                        </p>
                                    </div>
                                    <!-- 是否置顶 -->
                                    <div v-if="article.isTop" class="absolute inline-flex items-center justify-center w-14 h-7 text-xs font-bold text-white bg-red-500 border-2 border-white rounded-full -top-2 -end-2 dark:border-gray-900">
                                        置顶
                                    </div>
                                </div>
                            </div>
                        </template>
                    </div>

                    <!-- 分页 -->
                    <nav v-if="pages >= 1" aria-label="Page navigation" class="mt-10 flex justify-center">
                        <ul class="flex items-center -space-x-px h-10 text-base">
                            <li>
                                <a @click="getArticles(current - 1)"
                                    class="flex items-center justify-center px-4 h-10 ml-0 leading-tight text-gray-500 bg-white border border-gray-300 rounded-l-lg hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white"
                                    :class="[current > 1 ? 'cursor-pointer' : 'cursor-not-allowed']">
                                    <span class="sr-only">上一页</span>
                                    <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 6 10">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 1 1 5l4 4" />
                                    </svg>
                                </a>
                            </li>
                            <li v-for="(pageNo, index) in pages" :key="index">
                                <a @click="getArticles(pageNo)"
                                    class="flex items-center justify-center px-4 h-10 leading-tight border cursor-pointer dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white"
                                    :class="[pageNo == current ? 'text-sky-600 bg-sky-50 border-sky-500 hover:bg-sky-100 hover:text-sky-700' : 'text-gray-500 border-gray-300 bg-white hover:bg-gray-100 hover:text-gray-700']">
                                    {{ index + 1 }}
                                </a>
                            </li>
                            <li>
                                <a @click="getArticles(current + 1)"
                                    class="flex items-center justify-center px-4 h-10 leading-tight text-gray-500 bg-white border border-gray-300 rounded-r-lg hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white"
                                    :class="[current < pages ? 'cursor-pointer' : 'cursor-not-allowed']">
                                    <span class="sr-only">下一页</span>
                                    <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 6 10">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m1 9 4-4-4-4" />
                                    </svg>
                                </a>
                            </li>
                        </ul>
                    </nav>
                </div>

                <!-- 右边侧边栏 -->
                <aside class="col-span-4 md:col-span-1">
                    <div class="sticky top-[5.5rem] space-y-4 slide-up-enter">
                        <UserInfoCard></UserInfoCard>

                        <!-- 订阅卡片 -->
                        <section v-if="subscribeCardVisible" class="bg-white border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700 p-5">
                            <div class="flex items-center gap-2 mb-3">
                                <i class="far fa-envelope text-gray-700 dark:text-gray-300"></i>
                                <span class="text-lg font-bold text-gray-900 dark:text-white">{{ subscribeCard.title }}</span>
                            </div>
                            <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">{{ subscribeCard.description }}</p>
                            <form class="flex gap-2" @submit.prevent="handleSubscribeSubmit">
                                <input v-model.trim="subscribeEmail" type="email" :placeholder="subscribeCard.placeholder" autocomplete="email"
                                    class="min-w-0 flex-1 h-10 px-3 border border-gray-300 rounded-lg text-sm text-gray-900 dark:text-white dark:bg-gray-700 dark:border-gray-600 focus:ring-blue-500 focus:border-blue-500" />
                                <button type="submit" :disabled="subscribeSubmitting"
                                    class="h-10 px-4 text-sm font-bold text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-70 transition-colors">
                                    {{ subscribeSubmitting ? '订阅中...' : subscribeCard.buttonText }}
                                </button>
                            </form>
                        </section>

                        <!-- 侧边栏公告（收起后显示） -->
                        <div v-if="announcement && announcement.isEnabled && announcementHidden" class="bg-white border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700 p-4">
                            <div class="flex items-center justify-between mb-2">
                                <span class="text-sm font-medium text-gray-900 dark:text-white flex items-center gap-1.5">
                                    <svg class="w-4 h-4 text-blue-500" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" /></svg>
                                    公告
                                </span>
                                <button @click="showAnnouncement" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors">
                                    <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
                                </button>
                            </div>
                            <div class="text-xs text-gray-500 dark:text-gray-400 markdown-body max-h-28 overflow-y-auto" v-html="renderedAnnouncementContent"></div>
                            <button @click="showAnnouncementModal = true" class="mt-2 text-xs text-blue-600 hover:underline transition-colors">阅读全文 →</button>
                        </div>

                        <CategoryListCard></CategoryListCard>
                        <TagListCard></TagListCard>
                    </div>
                </aside>
            </div>
        </div>

        <!-- 留言墙视图 -->
        <div v-else-if="currentView === 'message-wall'">
            <div class="max-w-6xl mx-auto">
                <section class="relative grid grid-cols-1 md:grid-cols-[1fr_1.2fr] items-center gap-5 min-h-[250px] mb-6 p-8 border border-gray-200 rounded-xl bg-white dark:bg-gray-800 dark:border-gray-700 shadow-sm overflow-hidden">
                    <div class="relative z-10">
                        <h1 class="text-4xl font-extrabold text-gray-900 dark:text-white mb-3">留言板</h1>
                        <p class="text-gray-500 dark:text-gray-400 mb-7">路过山海，欢迎在这里留下你的足迹与想法。</p>
                        <div class="grid grid-cols-3 md:grid-cols-3 gap-4">
                            <div class="flex items-center gap-3 p-3 rounded-lg border border-gray-100 dark:border-gray-700 bg-white/80 dark:bg-gray-700/80">
                                <div class="w-9 h-9 rounded-full bg-blue-500 flex items-center justify-center flex-shrink-0"></div>
                                <div><small class="text-gray-500 dark:text-gray-400 text-xs">留言总数</small><strong class="block text-gray-900 dark:text-white text-lg">{{ messageWallStats.total }}</strong></div>
                            </div>
                            <div class="flex items-center gap-3 p-3 rounded-lg border border-gray-100 dark:border-gray-700 bg-white/80 dark:bg-gray-700/80">
                                <div class="w-9 h-9 rounded-full bg-rose-500 flex items-center justify-center flex-shrink-0"></div>
                                <div><small class="text-gray-500 dark:text-gray-400 text-xs">今日留言</small><strong class="block text-gray-900 dark:text-white text-lg">{{ messageWallStats.today }}</strong></div>
                            </div>
                            <div class="flex items-center gap-3 p-3 rounded-lg border border-gray-100 dark:border-gray-700 bg-white/80 dark:bg-gray-700/80">
                                <div class="w-9 h-9 rounded-full bg-emerald-500 flex items-center justify-center flex-shrink-0"></div>
                                <div><small class="text-gray-500 dark:text-gray-400 text-xs">活跃访客</small><strong class="block text-gray-900 dark:text-white text-lg">{{ messageWallStats.visitors }}</strong></div>
                            </div>
                        </div>
                    </div>
                    <div class="relative z-10 h-full min-h-[220px] rounded-xl overflow-hidden">
                        <img :src="messageWallHeroImage" alt="" class="absolute inset-0 w-full h-full object-cover opacity-90" />
                    </div>
                </section>

                <div class="grid grid-cols-[1fr_340px] gap-7 items-start max-lg:grid-cols-1">
                    <div class="space-y-5">
                        <MessageWallForm @comment-published="handleMessageWallPublished" />
                        <MessageWallPanel ref="messageWallRef" @stats-change="handleMessageWallStatsChange" />
                    </div>
                    <aside class="space-y-5 max-lg:static sticky top-24">
                        <div class="flex items-center gap-4 p-5 border border-gray-200 rounded-lg bg-white dark:bg-gray-800 dark:border-gray-700">
                            <img :src="blogSettingsStore.blogSettings.avatar" class="w-16 h-16 rounded-full object-cover flex-shrink-0" />
                            <div>
                                <h2 class="text-lg font-extrabold text-gray-900 dark:text-white">关于留言板</h2>
                                <p class="text-sm text-gray-500 dark:text-gray-400">每一条留言都是一份温暖的遇见，感谢你的到来与支持。</p>
                            </div>
                        </div>
                        <div class="p-5 border border-gray-200 rounded-lg bg-white dark:bg-gray-800 dark:border-gray-700">
                            <h2 class="flex items-center gap-2 text-lg font-extrabold text-gray-900 dark:text-white mb-4"><i class="fas fa-bullhorn text-blue-500"></i> 留言须知</h2>
                            <ul class="space-y-2 text-sm text-gray-500 dark:text-gray-400">
                                <li class="flex items-start gap-2"><span class="w-1.5 h-1.5 rounded-full bg-blue-500 mt-2 flex-shrink-0"></span> 请文明留言，友善交流</li>
                                <li class="flex items-start gap-2"><span class="w-1.5 h-1.5 rounded-full bg-blue-500 mt-2 flex-shrink-0"></span> 禁止发布广告、违法及敏感信息</li>
                                <li class="flex items-start gap-2"><span class="w-1.5 h-1.5 rounded-full bg-blue-500 mt-2 flex-shrink-0"></span> 尊重他人观点，理性讨论</li>
                                <li class="flex items-start gap-2"><span class="w-1.5 h-1.5 rounded-full bg-blue-500 mt-2 flex-shrink-0"></span> 有问题请先搜索，感谢理解</li>
                            </ul>
                        </div>
                    </aside>
                </div>
            </div>
        </div>

    </main>

    <ScrollToTopButton></ScrollToTopButton>
    <Footer></Footer>
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
import messageWallHeroImage from '@/assets/liuyanban.png'
import { onMounted, ref, computed, watch } from 'vue'
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

// 当前视图
const currentView = ref('article')
const messageWallRef = ref(null)
const messageWallStats = ref({ total: 0, today: 0, visitors: 0, lastTime: '' })
const subscribeEmail = ref('')
const subscribeSubmitting = ref(false)

const subscribeCard = computed(() => ({
    title: '订阅更新',
    description: '订阅后，最新文章将通过邮件发送给你',
    placeholder: '输入你的邮箱地址',
    buttonText: '订阅'
}))

const subscribeCardVisible = computed(() => {
    const settings = blogSettingsStore.blogSettings || {}
    return settings.isSubscribeCardOpen !== false
})

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
            setCache('announcement', res.data, 10 * 60 * 1000)
        }
    })
}
loadAnnouncement()

const goCategoryArticleListPage = (id, name) => {
    router.push({ path: '/category/article/list', query: { id, name } })
}

const handleMessageWallPublished = () => {
    if (messageWallRef.value) messageWallRef.value.refresh()
}

const handleMessageWallStatsChange = (stats) => {
    messageWallStats.value = { ...messageWallStats.value, ...stats }
}

// 文章数据
const articles = ref([])
const current = ref(1)
const size = ref(10)
const total = ref(0)
const pages = ref(0)
const articlesLoaded = ref(false)
const isLoading = ref(false)
const ARTICLE_LIST_CACHE_VERSION = 'v2'

function getArticles(currentNo) {
    if (currentNo < 1 || (pages.value > 0 && currentNo > pages.value)) return

    const cacheKey = `articles_page_${ARTICLE_LIST_CACHE_VERSION}_${currentNo}_${size.value}`
    const cached = getCache(cacheKey)

    // SWR: 有缓存立即展示，再后台刷新
    if (cached) {
        articles.value = cached.list
        current.value = cached.pageNum
        size.value = cached.pageSize
        total.value = cached.total
        pages.value = Math.ceil(cached.total / cached.pageSize)
        articlesLoaded.value = true
        isLoading.value = false
        // 后台静默刷新（不显示 loading）
        getArticlePageList({ pageNum: currentNo, pageSize: size.value }).then((res) => {
            if (res.success) {
                articles.value = res.data.list || []
                current.value = res.data.pageNum || 1
                size.value = res.data.pageSize || 10
                total.value = res.data.total || 0
                pages.value = Math.ceil(total.value / size.value)
                setCache(cacheKey, {
                    list: articles.value,
                    pageNum: current.value,
                    pageSize: size.value,
                    total: total.value
                }, 5 * 60 * 1000)
            }
        })
        return
    }

    isLoading.value = true
    getArticlePageList({ pageNum: currentNo, pageSize: size.value }).then((res) => {
        isLoading.value = false
        if (res.success) {
            articles.value = res.data.list || []
            current.value = res.data.pageNum || 1
            size.value = res.data.pageSize || 10
            total.value = res.data.total || 0
            pages.value = Math.ceil(total.value / size.value)
            articlesLoaded.value = true

            setCache(cacheKey, {
                list: articles.value,
                pageNum: current.value,
                pageSize: size.value,
                total: total.value
            }, 5 * 60 * 1000)
        }
    })
}

watch(currentView, (newView) => {
    if (newView === 'article' && !articlesLoaded.value) {
        getArticles(current.value)
    }
}, { immediate: true })

const goArticleDetailPage = (articleId) => {
    router.push('/article/' + articleId)
}

const goTagArticleListPage = (id, name) => {
    router.push({ path: '/tag/article/list', query: { id, name } })
}

const updateViewFromQuery = () => {
    if (route.query.view === 'message-wall') {
        currentView.value = 'message-wall'
    } else {
        currentView.value = 'article'
    }
}

onMounted(() => {
    updateViewFromQuery()
})

watch(() => route.query, updateViewFromQuery, { immediate: false })
</script>

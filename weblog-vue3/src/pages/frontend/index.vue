<template>
    <Header></Header>

    <!-- 主内容区域 -->
    <main class="container max-w-screen-xl mx-auto px-4 md:px-6 py-4">
        <!-- 公告区域（AI聊天视图不显示） -->
        <div v-if="announcement && announcement.isEnabled && !announcementHidden && currentView !== 'ai-chat'" class="mb-4">
            <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4">
                <div class="flex items-start justify-between">
                    <div class="flex items-start flex-1">
                        <svg class="w-5 h-5 text-blue-500 mr-2 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
                        </svg>
                        <div class="markdown-body text-gray-700 dark:text-gray-300 announcement-content" v-html="renderedAnnouncementContent"></div>
                    </div>
                    <div class="flex items-center ml-2 flex-shrink-0">
                        <button @click="showAnnouncementModal = true" class="text-blue-500 hover:text-blue-600 dark:text-blue-400 dark:hover:text-blue-300 text-sm mr-2">
                            展开
                        </button>
                        <button @click="hideAnnouncement" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
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
                    <span class="text-base font-medium text-gray-800 dark:text-gray-200">公告</span>
                </div>
            </template>
            <div class="markdown-body text-gray-700 dark:text-gray-300 max-h-[60vh] overflow-y-auto pr-1" v-html="renderedAnnouncementContent"></div>
            <template #footer>
                <div class="flex justify-end py-1">
                    <el-button type="primary" size="small" @click="showAnnouncementModal = false">知道了</el-button>
                </div>
            </template>
        </el-dialog>

        <!-- 文章视图：保持原来的 4 列布局 -->
        <div v-if="currentView === 'article'" class="grid grid-cols-4 gap-7">
            <div class="col-span-4 md:col-span-3 mb-3">
                <div class="grid grid-cols-2 gap-4">
                    <div v-for="(article, index) in articles" :key="index" class="col-span-2 md:col-span-1 animate__animated animate__fadeInUp">
                        <div class=" relative bg-white hover:scale-[1.03] h-full border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700">
                            <a @click="goArticleDetailPage(article.id)" class="cursor-pointer">
                                <img class="rounded-t-lg h-48 w-full" :src="article.cover" />
                            </a>
                            <div class="p-5 flex flex-col min-h-max">
                                <div class="mb-3">
                                    <span v-if="article.tags && article.tags.length > 0" v-for="(tag, tagIndex) in article.tags" :key="tagIndex" @click="goTagArticleListPage(tag.id, tag.name)" class="cursor-pointer bg-green-100 text-green-800 text-xs font-medium mr-2 px-2.5 py-0.5 rounded hover:bg-green-200 hover:text-green-900 dark:bg-green-900 dark:hover:bg-green-950 dark:text-green-300">{{ tag.name }}</span>
                                </div>
                                <a @click="goArticleDetailPage(article.id)" class="cursor-pointer">
                                    <h2 class="mb-2 text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
                                        <span class="hover:border-gray-600 hover:border-b-2 dark:hover:border-gray-400">{{ article.title }}</span>
                                    </h2>
                                </a>
                                <p v-if="article.summary" class="mb-3 font-normal text-gray-500 dark:text-gray-400">{{ article.summary }}</p>
                                <p class="mt-auto flex items-center font-normal text-gray-400 text-sm dark:text-gray-400">
                                    <svg class="inline w-3 h-3 mr-2 text-gray-400" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 20 20">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 1v3m5-3v3m5-3v3M1 7h18M5 11h10M2 3h16a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z" />
                                    </svg>
                                    {{ article.createTime }}
                                    <svg v-if="article.category" class="inline w-3 h-3 ml-5 mr-2 text-gray-400" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 18 18">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M1 5v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H1Zm0 0V2a1 1 0 0 1 1-1h5.443a1 1 0 0 1 .8.4l2.7 3.6H1Z" />
                                    </svg>
                                    <a v-if="article.category" @click="goCategoryArticleListPage(article.category.id, article.category.name)" class="cursor-pointer text-gray-400 hover:underline">{{ article.category.name }}</a>
                                </p>
                            </div>
                            <div v-if="article.isTop === true" class="absolute inline-flex items-center justify-center w-14 h-7 text-xs font-bold text-white bg-red-500 border-2 border-white rounded-full -top-2 -end-2 dark:border-gray-900">置顶</div>
                        </div>
                    </div>
                </div>
                <nav aria-label="Page navigation example" class="mt-10 flex justify-center">
                    <ul class="flex items-center -space-x-px h-10 text-base">
                        <li>
                            <a @click="getArticles(current - 1)" :class="['flex items-center justify-center px-4 h-10 ml-0 leading-tight border rounded-l-lg transition-colors', current > 1 ? 'text-dark-300 bg-dark-800 border-dark-600 hover:bg-dark-700 hover:text-primary cursor-pointer' : 'text-dark-500 bg-dark-800 border-dark-600 cursor-not-allowed']">
                                <span class="sr-only">上一页</span>
                                <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 6 10"><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 1 1 5l4 4" /></svg>
                            </a>
                        </li>
                        <li v-for="(pageNo, index) in pages" :key="index">
                            <a @click="getArticles(pageNo)" :class="['flex items-center justify-center px-4 h-10 leading-tight border transition-colors', pageNo == current ? 'text-dark-900 bg-primary border-primary hover:bg-primary/80' : 'text-dark-300 bg-dark-800 border-dark-600 hover:bg-dark-700 hover:text-primary cursor-pointer']">{{ index + 1 }}</a>
                        </li>
                        <li>
                            <a @click="getArticles(current + 1)" :class="['flex items-center justify-center px-4 h-10 leading-tight border rounded-r-lg transition-colors', current < pages ? 'text-dark-300 bg-dark-800 border-dark-600 hover:bg-dark-700 hover:text-primary cursor-pointer' : 'text-dark-500 bg-dark-800 border-dark-600 cursor-not-allowed']">
                                <span class="sr-only">下一页</span>
                                <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 6 10"><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m1 9 4-4-4-4" /></svg>
                            </a>
                        </li>
                    </ul>
                </nav>
            </div>
            <aside class="col-span-4 md:col-span-1 animate__animated animate__fadeInUp">
                <div class="sticky top-[5.5rem]">
                    <UserInfoCard></UserInfoCard>
                    <div v-if="announcement && announcement.isEnabled && announcementHidden" class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 mb-4">
                        <div class="flex items-center justify-between mb-2">
                            <div class="flex items-center">
                                <svg class="w-4 h-4 text-blue-500 mr-1" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" /></svg>
                                <span class="text-sm font-medium text-gray-700 dark:text-gray-300">公告</span>
                            </div>
                            <button @click="showAnnouncement" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                                <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
                            </button>
                        </div>
                        <div class="text-sm text-gray-600 dark:text-gray-400 markdown-body max-h-32 overflow-y-auto" v-html="renderedAnnouncementContent"></div>
                        <div class="mt-2 flex justify-end">
                            <button @click="showAnnouncementModal = true" class="text-xs text-blue-500 hover:text-blue-600 dark:text-blue-400 dark:hover:text-blue-300">阅读全文 →</button>
                        </div>
                    </div>
                    <CategoryListCard></CategoryListCard>
                    <TagListCard></TagListCard>
                </div>
            </aside>
        </div>

        <!-- 留言墙视图：全宽布局 -->
        <div v-else-if="currentView === 'message-wall'" class="message-wall-fullpage">
            <!-- 留言表单和列表 - 居中窄宽 -->
            <div class="max-w-4xl mx-auto px-4 py-6">
                <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 mb-4">
                    <MessageWallForm @comment-published="handleMessageWallPublished" />
                </div>
                
                <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4">
                    <MessageWallPanel ref="messageWallRef" />
                </div>
            </div>
        </div>

        <!-- AI 聊天视图 -->
        <div v-else-if="currentView === 'ai-chat'" class="ai-chat-container">
            <ChatPanel />
        </div>

    </main>

    <!-- 返回顶部 -->
    <ScrollToTopButton></ScrollToTopButton>

    <Footer></Footer>
</template>

<script setup>
import Header from '@/layouts/frontend/components/Header.vue'
import Footer from '@/layouts/frontend/components/Footer.vue'
import UserInfoCard from '@/layouts/frontend/components/UserInfoCard.vue'
import CategoryListCard from '@/layouts/frontend/components/CategoryListCard.vue'
import TagListCard from '@/layouts/frontend/components/TagListCard.vue'
import ScrollToTopButton from '@/layouts/frontend/components/ScrollToTopButton.vue'
import MessageWallPanel from '@/components/MessageWallPanel.vue'
import MessageWallForm from '@/components/MessageWallForm.vue'
import ChatPanel from '@/components/chat/ChatPanel.vue'
import { initTooltips } from 'flowbite'
import { onMounted, ref, computed, watch } from 'vue'
import { marked } from 'marked'
import { setCache, getCache } from '@/composables/useCache'

defineOptions({
    name: 'index'
})
import { getArticlePageList } from '@/api/frontend/article'
import { getAnnouncement } from '@/api/frontend/announcement'
import { useRouter, useRoute } from 'vue-router'

const router = useRouter()
const route = useRoute()

// 公告
const announcement = ref(null)
const announcementHidden = ref(localStorage.getItem('announcementHidden') === 'true')
const showAnnouncementModal = ref(false)

// 当前视图：article 或 message-wall
const currentView = ref('article')
const messageWallRef = ref(null)

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
    initTooltips();
    updateViewFromQuery();
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
        return
    }

    // 调用分页接口渲染数据
    getArticlePageList({current: currentNo, size: size.value}).then((res) => {
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
.ai-chat-container {
    @apply h-[calc(100vh-64px-32px)] -mx-4 overflow-hidden;
}
</style>

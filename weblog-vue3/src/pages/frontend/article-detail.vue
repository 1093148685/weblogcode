<template>
    <Header></Header>

    <!-- 阅读进度条 -->
    <div class="fixed top-0 left-0 right-0 z-[70] h-1 bg-[var(--bg-hover)]">
        <div class="h-full bg-gradient-to-r from-blue-500 to-blue-400 transition-all duration-150 ease-out"
            :style="{ width: `${readingProgress}%` }"></div>
    </div>


    <!-- 文章标题、标签、Meta 信息 -->
    <div class="article-hero" :class="{ 'is-loading': articleLoading }">
        <div v-if="articleLoading" class="article-hero__inner max-w-content flex flex-col mx-auto px-6 pb-10 pt-8">
            <div class="article-hero-skeleton article-hero-skeleton__tag"></div>
            <div class="article-hero-skeleton article-hero-skeleton__title"></div>
            <div class="article-hero-skeleton article-hero-skeleton__meta"></div>
        </div>
        <div v-else class="article-hero__inner max-w-content flex flex-col mx-auto px-6 pb-10 pt-8 article-reveal">
            <!-- 标签集合 -->
            <div v-if="article.tags && article.tags.length > 0" class="flex flex-wrap gap-1.5 mb-4">
                <span @click="goTagArticleListPage(tag.id, tag.name)" v-for="(tag, index) in article.tags" :key="index"
                    class="cursor-pointer inline-block px-2.5 py-0.5 text-xs font-medium
                           text-[var(--text-secondary)] bg-[var(--bg-hover)] border border-[var(--border-base)]
                           rounded-full hover:bg-[var(--bg-active)] hover:text-[var(--color-primary)]
                           hover:border-[var(--color-primary)] transition-all duration-200">
                    # {{ tag.name }}
                </span>
            </div>

            <!-- 文章标题 -->
            <h1 class="article-title">{{ article.title }}</h1>

            <!-- Meta 信息 -->
            <div class="flex flex-wrap gap-4 text-[var(--text-muted)] items-center text-sm">

                <!-- 发布时间 -->
                <span class="flex items-center gap-1" title="发布时间">
                    <svg class="w-4 h-4" fill="none" viewBox="0 0 20 20">
                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                            d="M5 1v3m5-3v3m5-3v3M1 7h18M5 11h10M2 3h16a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z"/>
                    </svg>
                    {{ article.createTime }}
                </span>

                <!-- 分类 -->
                <a v-if="article.categoryName"
                    @click="goCategoryArticleListPage(article.categoryId, article.categoryName)"
                    class="cursor-pointer flex items-center gap-1 hover:text-[var(--color-primary)] transition-colors" title="分类">
                    <svg class="w-4 h-4" fill="none" viewBox="0 0 18 18">
                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                            d="M1 5v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H1Zm0 0V2a1 1 0 0 1 1-1h5.443a1 1 0 0 1 .8.4l2.7 3.6H1Z"/>
                    </svg>
                    {{ article.categoryName }}
                </a>

                <!-- 阅读量 -->
                <span class="flex items-center gap-1" title="阅读量">
                    <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24">
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

    <!-- 主内容区域 -->
    <main class="article-main max-w-content mx-auto px-6 py-6">
        <div class="grid grid-cols-1 lg:grid-cols-[minmax(0,1fr)_280px] gap-6">
            <!-- 左边：文章内容 -->
            <div class="min-w-0">
                <!-- 文章卡片 -->
                <div v-if="articleLoading" class="article-card article-card--loading">
                    <div class="article-loading-block article-loading-block--summary"></div>
                    <div class="article-loading-line w-4/5"></div>
                    <div class="article-loading-line"></div>
                    <div class="article-loading-line w-11/12"></div>
                    <div class="article-loading-line w-3/4"></div>
                    <div class="article-loading-paragraph"></div>
                    <div class="article-loading-line"></div>
                    <div class="article-loading-line w-5/6"></div>
                </div>
                <div v-else class="article-card article-reveal">
                    <article>
                        <!-- AI 摘要 -->
                        <AiSummaryCard ref="aiSummaryRef" :content="article.content" :ready="articleRenderReady" />

                        <section v-if="article.id" class="article-ai-hub">
                            <div class="article-ai-hub__head">
                                <div>
                                    <p class="article-ai-hub__eyebrow">AI 阅读助手</p>
                                    <h2>围绕这篇文章继续探索</h2>
                                </div>
                                <button class="article-ai-hub__primary" type="button" @click="askArticleAi('请用通俗语言总结这篇文章，并列出 5 个关键知识点。')">
                                    问这篇文章
                                </button>
                            </div>
                            <div class="article-ai-hub__actions">
                                <button type="button" @click="askArticleAi('请把这篇文章整理成适合复习的学习路线，按先后顺序列出。')">生成学习路线</button>
                                <button type="button" @click="askArticleAi('请根据这篇文章生成 8 道面试题，并附上参考答案。')">生成面试题</button>
                                <button type="button" @click="askArticleAi('请提取这篇文章中的代码、架构或技术要点，并解释容易踩坑的地方。')">解释技术要点</button>
                                <button type="button" @click="askArticleAi('请基于这篇文章推荐下一步应该阅读或学习的方向。')">推荐下一步</button>
                            </div>
                        </section>

                        <!-- 正文 -->
                        <div :class="{ 'dark': isDark }">
                            <div ref="articleContentRef" class="mt-5 article-content" v-viewer v-html="renderedContent"></div>
                        </div>

                        <!-- 上下篇 -->
                        <nav class="article-pager">
                            <div class="basis-1/2">
                                <a v-if="article.preArticle"
                                    @click="router.push('/article/' + article.preArticle.articleId)"
                                    class="article-pager-link">
                                    <div class="flex items-center gap-1 mb-1 text-xs text-[var(--text-muted)]">
                                        <svg class="w-3 h-3" fill="none" viewBox="0 0 14 10">
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"
                                                stroke-width="2" d="M13 5H1m0 0 4 4M1 5l4-4"/>
                                        </svg>
                                        上一篇
                                    </div>
                                    <div class="line-clamp-2 leading-snug">{{ article.preArticle.articleTitle }}</div>
                                </a>
                            </div>
                            <div class="basis-1/2">
                                <a v-if="article.nextArticle"
                                    @click="router.push('/article/' + article.nextArticle.articleId)"
                                    class="article-pager-link article-pager-link--next">
                                    <div class="flex items-center justify-end gap-1 mb-1 text-xs text-[var(--text-muted)]">
                                        下一篇
                                        <svg class="w-3 h-3" fill="none" viewBox="0 0 14 10">
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"
                                                stroke-width="2" d="M1 5h12m0 0L9 1m4 4L9 9"/>
                                        </svg>
                                    </div>
                                    <div class="line-clamp-2 leading-snug">{{ article.nextArticle.articleTitle }}</div>
                                </a>
                            </div>
                        </nav>
                    </article>
                </div>

                <!-- 评论组件 -->
                <div id="comments">
                    <Comment></Comment>
                </div>
            </div>

            <!-- 右边侧边栏 -->
            <aside class="hidden lg:block w-[280px] flex-shrink-0">
                <div class="sticky top-[80px]">
                    <Toc :ready="articleRenderReady"></Toc>
                </div>
            </aside>
        </div>
    </main>

    <!-- 返回顶部 -->
    <ScrollToTopButton :show-comment-button="true"></ScrollToTopButton>

    <Footer></Footer>
</template>

<script setup>
import Header from '@/layouts/frontend/components/Header.vue'
import Footer from '@/layouts/frontend/components/Footer.vue'
import ScrollToTopButton from '@/layouts/frontend/components/ScrollToTopButton.vue'
import Toc from '@/layouts/frontend/components/Toc.vue'
import { getArticleDetail, clearArticleDetailCache } from '@/api/frontend/article'
import { useRoute, useRouter } from 'vue-router'
import { ref, watch, onMounted, onBeforeUnmount, nextTick, computed } from 'vue'
import hljs from 'highlight.js'
import 'highlight.js/styles/tokyo-night-dark.css'
import { marked } from 'marked'
import Comment from '@/components/Comment.vue'
import AiSummaryCard from '@/components/AiSummaryCard.vue'

// 是否是暗黑模式（通过 html.dark class 判断）
const isDark = ref(document.documentElement.classList.contains('dark'))
// 监听 dark 类的变化
const darkObserver = new MutationObserver(() => {
    isDark.value = document.documentElement.classList.contains('dark')
})

// 阅读进度
const readingProgress = ref(0)
let scrollHandler = null

const route = useRoute()
const router = useRouter()

// 文章数据
const article = ref({})

// 文章主体加载状态，AI 摘要等正文稳定后再加载
const articleLoading = ref(true)

// 文章是否渲染完毕
const articleRenderReady = ref(false)

// AI 摘要组件引用
const aiSummaryRef = ref(null)

// Markdown 转 HTML
const renderedContent = computed(() => {
    if (!article.value.content) return ''
    const content = article.value.content
    return /<\/?[a-z][\s\S]*>/i.test(content)
        ? content
        : marked.parse(content, { breaks: true, gfm: true })
})

// 获取文章详情
function refreshArticleDetail(articleId) {
    if (!articleId) {
        console.error('articleId is required')
        return
    }

    articleLoading.value = true
    articleRenderReady.value = false
    article.value = {}
    
    // 清除缓存，确保获取最新数据
    clearArticleDetailCache(articleId)
    
    getArticleDetail(articleId).then((res) => {
        // 该文章不存在(错误码为 20010)
        if (!res.success && res.errorCode == '20010') {
            // 手动跳转 404 页面
            router.push({ name: 'NotFound' })
            return
        }

        article.value = res.data

        nextTick(() => {
            // 获取所有 pre code 节点
            let highlight = document.querySelectorAll('pre code')
            // 循环高亮
            highlight.forEach((block) => {
                hljs.highlightElement(block)
            })

            // 获取所有的 pre 节点
            let preElements = document.querySelectorAll('pre')
            preElements.forEach(preElement => {
                // 找到第一个 code 元素
                let firstCode = preElement.querySelector('code');
                if (firstCode) {
                    let copyCodeBtn = '<button class="hidden copy-code-btn flex items-center justify-center"><div class="copy-icon"></div></button>'
                    firstCode.insertAdjacentHTML('beforebegin', copyCodeBtn);

                    // 获取刚插入的按钮
                    let copyBtn = firstCode.previousSibling;
                    copyBtn.addEventListener('click', () => {
                        // 添加 copied 样式
                        copyBtn.classList.add('copied');
                        copyToClipboard(preElement.textContent)
                        // 1.5 秒后移除 copied 样式
                        setTimeout(() => {
                            copyBtn.classList.remove('copied');
                        }, 1500);
                    });
                }

                // 添加事件监听器
                preElement.addEventListener('mouseenter', handleMouseEnter);
                preElement.addEventListener('mouseleave', handleMouseLeave);
            })

            // 文章渲染完毕后，加载 AI 摘要
            articleLoading.value = false
            articleRenderReady.value = true
        })
    }).catch((error) => {
        console.error('load article detail failed:', error)
        articleLoading.value = false
        articleRenderReady.value = false
    })
}
refreshArticleDetail(route.params.articleId)

const askArticleAi = (prompt) => {
    router.push({
        path: '/',
        query: {
            view: 'ai-chat',
            articleId: article.value.id || route.params.articleId,
            prompt,
            autoSend: '1'
        }
    })
}

// 启动暗色模式监听
onMounted(() => {
    darkObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] })

    // 阅读进度监听
    scrollHandler = () => {
        const scrollTop = window.scrollY
        const docHeight = document.documentElement.scrollHeight - window.innerHeight
        readingProgress.value = docHeight > 0 ? Math.min((scrollTop / docHeight) * 100, 100) : 0
    }
    window.addEventListener('scroll', scrollHandler, { passive: true })
})
onBeforeUnmount(() => {
    darkObserver.disconnect()
    if (scrollHandler) window.removeEventListener('scroll', scrollHandler)
})

// 跳转分类文章列表页
const goCategoryArticleListPage = (id, name) => {
    // 跳转时通过 query 携带参数（分类 ID、分类名称）
    router.push({ path: '/category/article/list', query: { id, name } })
}

// 跳转标签文章列表页
const goTagArticleListPage = (id, name) => {
    // 跳转时通过 query 携带参数（标签 ID、标签名称）
    router.push({ path: '/tag/article/list', query: { id, name } })
}

// 监听路由
watch(route, (newRoute, oldRoute) => {
    // 重新渲染文章详情
    refreshArticleDetail(newRoute.params.articleId)
})

// 复制内容到剪切板
function copyToClipboard(text) {
    const textarea = document.createElement('textarea');
    textarea.value = text;
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand('copy');
    document.body.removeChild(textarea);
}

const handleMouseEnter = (event) => {
    // 鼠标移入，显示按钮
    let copyBtn = event.target.querySelector('button');
    if (copyBtn) {
        copyBtn.classList.remove('hidden');
        copyBtn.classList.add('block');
    }
}

const handleMouseLeave = (event) => {
    // 鼠标移出，隐藏按钮
    let copyBtn = event.target.querySelector('button');
    if (copyBtn) {
        copyBtn.classList.add('hidden');
    }
}
</script>

<style scoped>
.article-hero {
    position: relative;
    overflow: hidden;
    background:
        radial-gradient(circle at 18% 0%, rgba(59, 130, 246, 0.10), transparent 34%),
        linear-gradient(180deg, var(--bg-card), var(--bg-body));
    border-bottom: 1px solid var(--border-base);
}

.article-hero::after {
    content: '';
    position: absolute;
    left: 0;
    right: 0;
    bottom: 0;
    height: 1px;
    background: linear-gradient(90deg, transparent, rgba(59, 130, 246, 0.42), transparent);
}

.article-hero__inner {
    position: relative;
    z-index: 1;
}

.article-title {
    max-width: 920px;
    margin-bottom: 20px;
    color: var(--text-heading);
    font-size: 34px;
    font-weight: 800;
    line-height: 1.24;
    letter-spacing: 0;
}

.article-card {
    margin-bottom: 20px;
    padding: 30px;
    border-radius: 18px;
    background: var(--bg-card);
    border: 1px solid var(--border-base);
    box-shadow: 0 18px 50px rgba(15, 23, 42, 0.08);
}

.article-reveal {
    animation: articleReveal 0.42s ease both;
}

.article-hero.is-loading {
    min-height: 178px;
}

.article-hero-skeleton,
.article-loading-line,
.article-loading-block,
.article-loading-paragraph {
    position: relative;
    overflow: hidden;
    border-radius: 999px;
    background: linear-gradient(90deg, var(--bg-hover) 25%, var(--bg-active) 50%, var(--bg-hover) 75%);
    background-size: 240% 100%;
    animation: articleSkeleton 1.35s ease-in-out infinite;
}

.article-hero-skeleton__tag {
    width: 128px;
    height: 26px;
    margin-bottom: 22px;
}

.article-hero-skeleton__title {
    width: min(760px, 82%);
    height: 44px;
    margin-bottom: 22px;
    border-radius: 14px;
}

.article-hero-skeleton__meta {
    width: 360px;
    max-width: 70%;
    height: 18px;
}

.article-card--loading {
    display: flex;
    flex-direction: column;
    gap: 16px;
    min-height: 520px;
}

.article-loading-block--summary {
    height: 112px;
    margin-bottom: 18px;
    border-radius: 18px;
}

.article-loading-line {
    height: 18px;
}

.article-loading-paragraph {
    height: 160px;
    margin: 12px 0;
    border-radius: 18px;
}

@keyframes articleSkeleton {
    0% { background-position: 120% 0; }
    100% { background-position: -120% 0; }
}

@keyframes articleReveal {
    from {
        opacity: 0;
        transform: translateY(10px);
        filter: blur(4px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
        filter: blur(0);
    }
}

.article-ai-hub {
    margin-top: 18px;
    margin-bottom: 24px;
    padding: 18px;
    border-radius: 18px;
    border: 1px solid rgba(59, 130, 246, 0.18);
    background:
        linear-gradient(135deg, rgba(59, 130, 246, 0.08), rgba(34, 211, 238, 0.08)),
        var(--bg-card);
}

.article-ai-hub__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 14px;
}

.article-ai-hub__eyebrow {
    margin: 0 0 4px;
    color: var(--color-primary);
    font-size: 12px;
    font-weight: 700;
}

.article-ai-hub h2 {
    margin: 0;
    color: var(--text-heading);
    font-size: 18px;
    font-weight: 800;
}

.article-ai-hub__primary,
.article-ai-hub__actions button {
    border-radius: 12px;
    transition: transform 0.18s ease, border-color 0.18s ease, background 0.18s ease;
}

.article-ai-hub__primary {
    flex-shrink: 0;
    padding: 10px 16px;
    color: #fff;
    font-size: 14px;
    font-weight: 700;
    background: linear-gradient(135deg, #2563eb, #06b6d4);
    box-shadow: 0 12px 26px rgba(37, 99, 235, 0.22);
}

.article-ai-hub__actions {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 10px;
}

.article-ai-hub__actions button {
    min-height: 44px;
    padding: 10px 12px;
    color: var(--text-secondary);
    font-size: 13px;
    font-weight: 650;
    background: rgba(255, 255, 255, 0.58);
    border: 1px solid var(--border-base);
}

.article-ai-hub__primary:hover,
.article-ai-hub__actions button:hover {
    transform: translateY(-1px);
}

.article-ai-hub__actions button:hover {
    color: var(--color-primary);
    border-color: rgba(59, 130, 246, 0.35);
    background: var(--bg-card);
}

.dark .article-ai-hub__actions button {
    background: rgba(15, 23, 42, 0.54);
}

.article-pager {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
    margin-top: 32px;
}

.article-pager-link {
    display: flex;
    min-height: 88px;
    height: 100%;
    cursor: pointer;
    flex-direction: column;
    justify-content: center;
    padding: 16px;
    border-radius: 14px;
    background: var(--bg-hover);
    border: 1px solid var(--border-base);
    color: var(--text-secondary);
    font-size: 14px;
    font-weight: 600;
    transition: transform 0.2s ease, border-color 0.2s ease, background 0.2s ease, color 0.2s ease;
}

.article-pager-link:hover {
    transform: translateY(-2px);
    color: var(--color-primary);
    background: var(--bg-active);
    border-color: var(--color-primary);
}

.article-pager-link--next {
    text-align: right;
}

/* h1, h2, h3, h4, h5, h6 标题样式 */
::v-deep(.article-content h1,
.article-content h2,
.article-content h3,
.article-content h4,
.article-content h5,
.article-content h6) {
    color: #292525;
    line-height: 150%;
    font-family: PingFang SC, Helvetica Neue, Helvetica, Hiragino Sans GB, Microsoft YaHei, "\5FAE\8F6F\96C5\9ED1", Arial, sans-serif;
}

::v-deep(.article-content h1) {
    margin-top: 18px;
    margin-bottom: 28px;
    padding-bottom: 16px;
    color: var(--text-heading);
    font-size: 32px;
    font-weight: 850;
    line-height: 1.28;
    border-bottom: 1px solid var(--border-base);
}

::v-deep(.dark .article-content h1) {
    color: rgb(241 245 249);
    border-bottom-color: rgb(55 65 81 / 1);
}

::v-deep(.article-content h2) {
    line-height: 1.5;
    font-weight: 700;
    font-synthesis: style;
    font-size: 24px;
    margin-top: 40px;
    margin-bottom: 26px;
    line-height: 140%;
    border-bottom: 1px solid rgb(241 245 249);
    padding-bottom: 15px;
}

::v-deep(.dark .article-content h2) {
    --tw-text-opacity: 1;
    color: rgb(226 232 240/var(--tw-text-opacity));
    border-bottom: 1px solid;
    border-color: rgb(55 65 81 / 1);
}

::v-deep(.article-content h3) {
    font-size: 20px;
    margin-top: 40px;
    margin-bottom: 16px;
    font-weight: 600;
}

::v-deep(.dark .article-content h3) {
    --tw-text-opacity: 1;
    color: rgb(226 232 240/var(--tw-text-opacity));
}

::v-deep(.article-content h4) {
    font-size: 18px;
    margin-top: 30px;
    margin-bottom: 16px;
    font-weight: 600;
}

::v-deep(.dark .article-content h4) {
    --tw-text-opacity: 1;
    color: rgb(226 232 240/var(--tw-text-opacity));
}

::v-deep(.article-content h5) {
    font-size: 16px;
    margin-top: 30px;
    margin-bottom: 14px;
    font-weight: 600;
}

::v-deep(.dark .article-content h5) {
    --tw-text-opacity: 1;
    color: rgb(226 232 240/var(--tw-text-opacity));
}

::v-deep(.article-content h6) {
    font-size: 16px;
    margin-top: 30px;
    margin-bottom: 14px;
    font-weight: 600;
}

::v-deep(.dark .article-content h6) {
    --tw-text-opacity: 1;
    color: rgb(226 232 240/var(--tw-text-opacity));
}

/* p 段落样式 */
::v-deep(.article-content p) {
    letter-spacing: .3px;
    margin: 0 0 20px;
    line-height: 30px;
    color: #4c4e4d;
    font-weight: 400;
    word-break: normal;
    word-wrap: break-word;
    font-family: -apple-system, BlinkMacSystemFont, PingFang SC, Hiragino Sans GB, Microsoft Yahei, Arial, sans-serif;
}

::v-deep(.dark .article-content p) {
    color: #9e9e9e;
}

/* blockquote 引用样式 */
::v-deep(.article-content blockquote) {
    border-left: 2.3px solid rgb(52, 152, 219);
    quotes: none;
    background: rgb(236, 240, 241);
    color: #777;
    font-size: 16px;
    margin-bottom: 20px;
    padding: 24px;
}

::v-deep(.dark .article-content blockquote) {
    quotes: none;
    --tw-bg-opacity: 1;
    background-color: rgb(31 41 55 / var(--tw-bg-opacity));
    border-left: 2.3px solid #555;
    color: #666;
    font-size: 16px;
    margin-bottom: 20px;
    padding: 0.25rem 0 0.25rem 1rem;
}

/* 设置 blockquote 中最后一个 p 标签的 margin-bottom 为 0 */
::v-deep(.article-content blockquote p:last-child) {
    margin-bottom: 0;
}

/* 斜体样式 */
::v-deep(.article-content em) {
    color: #c849ff;
}

/* 超链接样式 */
::v-deep(.article-content a) {
    color: #167bc2;
}

::v-deep(.article-content a:hover) {
    text-decoration: underline;
}

/* ul 样式 */
::v-deep(.article-content ul) {
    padding-left: 2rem;
}

::v-deep(.dark .article-content ul) {
    padding-left: 2rem;
    color: #9e9e9e;
}

::v-deep(.article-content > ul) {
    margin-bottom: 20px;
}

::v-deep(.article-content ul li) {
    list-style-type: disc;
    padding-top: 5px;
    padding-bottom: 5px;
    font-size: 16px;
}

::v-deep(.article-content ul li p) {
    margin-bottom: 0!important;
}

::v-deep(.article-content ul ul li) {
    list-style-type: square;
}

/* ol 样式 */
::v-deep(.article-content ol) {
    list-style-type: decimal;
    padding-left: 2rem;
}

::v-deep(.dark .article-content ol) {
    color: #9e9e9e;
}

/* 图片样式 */
::v-deep(.article-content img) {
    max-width: 100%;
    overflow: hidden;
    display: block;
    margin: 24px auto;
    border-radius: 14px;
    border: 1px solid var(--border-base);
    box-shadow: 0 14px 34px rgba(15, 23, 42, 0.10);
}

::v-deep(.article-content img:hover,
img:focus) {
    box-shadow: 2px 2px 10px 0 rgba(0, 0, 0, .15);
}

/* 图片描述文字 */
::v-deep(.image-caption) {
    min-width: 20%;
    max-width: 80%;
    min-height: 43px;
    display: block;
    padding: 10px;
    margin: 0 auto;
    font-size: 13px;
    color: #999;
    text-align: center;
}

/* code 样式 */
::v-deep(.article-content code:not(pre code)) {
    padding: 2px 4px;
    margin: 0 2px;
    font-size: 95% !important;
    border-radius: 4px;
    color: rgb(41, 128, 185);
    background-color: rgba(27, 31, 35, 0.05);
    font-family: Operator Mono, Consolas, Monaco, Menlo, monospace;
}

::v-deep(.dark .article-content code:not(pre code)) {
    padding: 2px 4px;
    margin: 0 2px;
    font-size: .85em;
    border-radius: 5px;
    color: #abb2bf;
    background: #333;
    font-family: Operator Mono, Consolas, Monaco, Menlo, monospace;
}

::v-deep(code) {
    font-size: 98%;
}

/* pre 样式 */
::v-deep(.article-content pre) {
    margin-bottom: 20px;
    padding-top: 30px;
    background: #21252b;
    border-radius: 12px;
    position: relative;
    box-shadow: 0 18px 38px rgba(15, 23, 42, 0.18);
    overflow: hidden;
}

::v-deep(.article-content pre code),
::v-deep(.article-content pre code.hljs) {
    display: block;
    padding: 0.7rem 1rem;
    border-bottom-left-radius: 6px;
    border-bottom-right-radius: 6px;
    color: #c9d1d9;
    background: transparent;
    font-family: Operator Mono, Consolas, Monaco, Menlo, monospace;
    font-size: 15px;
    line-height: 1.8;
    white-space: pre;
    overflow-x: auto;
}

::v-deep(.article-content pre:before) {
    background: #fc625d;
    border-radius: 50%;
    box-shadow: 20px 0 #fdbc40, 40px 0 #35cd4b;
    content: ' ';
    height: 10px;
    margin-top: -19px;
    margin-left: 10px;
    position: absolute;
    width: 10px;
}

::v-deep(.article-content pre code *) {
    font-family: inherit;
}

::v-deep(.article-content pre code ul),
::v-deep(.article-content pre code ol),
::v-deep(.article-content pre code li),
::v-deep(.article-content pre code p) {
    margin: 0;
    padding: 0;
    list-style: none;
}

/* 表格样式 */
::v-deep(table) {
    margin-bottom: 20px;
    width: 100%;
}

::v-deep(table tr) {
    background-color: #fff;
    border-top: 1px solid #c6cbd1;
}

::v-deep(table th) {
    padding: 6px 13px;
    border: 1px solid #dfe2e5;
}

::v-deep(table td) {
    padding: 6px 13px;
    border: 1px solid #dfe2e5;
}

::v-deep(table tr:nth-child(2n)) {
    background-color: #f6f8fa;
}

::v-deep(.dark table tr) {
    background-color: rgb(31 41 55 / 1);
}

::v-deep(.dark table) {
    color: #9e9e9e;
}

::v-deep(.dark table th) {
    border: 1px solid #394048;
}

::v-deep(.dark table td) {
    border: 1px solid #394048;
}

::v-deep(.dark table tr:nth-child(2n)) {
    background-color: rgb(21 41 55 / 1);
}

/* hr 横线 */
::v-deep(hr) {
    margin-bottom: 20px;
}

::v-deep(.dark hr) {
    --tw-border-opacity: 1;
    border-color: rgb(55 65 81 / var(--tw-border-opacity));
}

::v-deep(.copy-code-btn) {
    border-width: 0;
    cursor: pointer;
    position: absolute;
    top: 0.5em;
    right: 0.5em;
    z-index: 5;
    width: 2.5rem;
    height: 2.5rem;
    padding: 0;
    border-radius: 0.5rem;
    opacity: 0;
    transition: opacity .4s;
    opacity: 1
}

::v-deep(.copy-code-btn:hover) {
    background: #2f3542;
}

::v-deep(.copy-icon) {
    --copy-icon: url("data:image/svg+xml;utf8,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' height='20' width='20' stroke='rgba(128,128,128,1)' stroke-width='2'%3E%3Cpath stroke-linecap='round' stroke-linejoin='round' d='M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2'/%3E%3C/svg%3E");
    background: currentcolor;
    -webkit-mask-image: var(--copy-icon);
    mask-image: var(--copy-icon);
    -webkit-mask-position: 50%;
    mask-position: 50%;
    -webkit-mask-repeat: no-repeat;
    mask-repeat: no-repeat;
    -webkit-mask-size: 1em;
    mask-size: 1em;
    width: 1.25rem;
    height: 1.25rem;
    padding: 0.625rem;
    color: #9e9e9e;
    font-size: 1.25rem;
}

::v-deep(.copied) {
    display: flex;
    background: #2f3542;
}

::v-deep(.copied:after) {
    content: "已复制";
    position: absolute;
    top: 0;
    right: calc(100% + .25rem);
    display: block;
    height: 2.5rem;
    padding: .625rem;
    border-radius: .5rem;
    background: #2f3542;
    color: #9e9e9e;
    font-weight: 500;
    line-height: 1.25rem;
    white-space: nowrap;
    font-size: 14px;
    font-family: -apple-system, BlinkMacSystemFont, PingFang SC, Hiragino Sans GB, Microsoft Yahei, Arial, sans-serif;
}

::v-deep(.copied .copy-icon) {
    --copied-icon: url("data:image/svg+xml;utf8,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' height='20' width='20' stroke='rgba(128,128,128,1)' stroke-width='2'%3E%3Cpath stroke-linecap='round' stroke-linejoin='round' d='M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2m-6 9 2 2 4-4'/%3E%3C/svg%3E");
    -webkit-mask-image: var(--copied-icon);
    mask-image: var(--copied-icon);
}

/* 阅读标题栏动画 */
.reading-bar-enter-active,
.reading-bar-leave-active {
    transition: transform 0.25s cubic-bezier(0.4, 0, 0.2, 1), opacity 0.25s ease;
}
.reading-bar-enter-from,
.reading-bar-leave-to {
    transform: translateY(100%);
    opacity: 0;
}

@media (max-width: 768px) {
    .article-title {
        font-size: 28px;
    }

    .article-main {
        padding-left: 16px;
        padding-right: 16px;
    }

    .article-card {
        padding: 20px;
        border-radius: 14px;
    }

    .article-ai-hub__head {
        align-items: stretch;
        flex-direction: column;
    }

    .article-ai-hub__actions {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    .article-pager {
        grid-template-columns: 1fr;
    }

    .article-pager-link--next {
        text-align: left;
    }
}
</style>

<template>
    <Header></Header>

    <main class="container max-w-screen-xl mx-auto px-4 md:px-6 py-4">
        <!-- 加载中 -->
        <template v-if="isLoading">
            <div class="max-w-3xl mx-auto">
                <Skeleton width="80%" height="2.5rem" />
                <div class="mt-4 flex gap-4">
                    <Skeleton width="120px" height="1.2rem" />
                    <Skeleton width="80px" height="1.2rem" />
                </div>
                <div class="mt-8 space-y-3">
                    <Skeleton width="100%" height="1rem" />
                    <Skeleton width="100%" height="1rem" />
                    <Skeleton width="70%" height="1rem" />
                </div>
            </div>
        </template>

        <!-- 日志内容 -->
        <template v-else-if="journal">
            <div class="max-w-3xl mx-auto">
                <!-- 标题 -->
                <h1 class="text-3xl font-bold text-gray-900 dark:text-white">{{ journal.title }}</h1>

                <!-- Meta 信息 -->
                <div class="mt-4 flex items-center gap-5 text-sm text-gray-400 dark:text-gray-500">
                    <div class="flex items-center">
                        <svg class="w-4 h-4 mr-1.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                        </svg>
                        {{ formatDate(journal.createTime) }}
                    </div>
                    <div class="flex items-center">
                        <svg class="w-4 h-4 mr-1.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                        </svg>
                        {{ journal.readNum }} 次阅读
                    </div>
                </div>

                <!-- 分割线 -->
                <hr class="my-6 border-gray-200 dark:border-gray-700">

                <!-- 正文 -->
                <div :class="{ 'dark': isDark }">
                    <div ref="journalContentRef" class="mt-5 article-content" v-viewer v-html="journal.content"></div>
                </div>

                <!-- 底部分割线 -->
                <hr class="my-8 border-gray-200 dark:border-gray-700">

                <!-- 评论组件 -->
                <Comment></Comment>
            </div>
        </template>

        <!-- 日志不存在 -->
        <template v-else>
            <div class="text-center py-20">
                <p class="text-gray-400 dark:text-gray-500 text-lg">日志不存在</p>
            </div>
        </template>
    </main>

    <Footer></Footer>
</template>

<script setup>
import Header from '@/layouts/frontend/components/Header.vue'
import Footer from '@/layouts/frontend/components/Footer.vue'
import Skeleton from '@/components/Skeleton.vue'
import Comment from '@/components/Comment.vue'
import { getJournalDetail } from '@/api/frontend/journal'
import { useRoute } from 'vue-router'
import { ref, onMounted, nextTick } from 'vue'
import hljs from 'highlight.js'
import 'highlight.js/styles/tokyo-night-dark.css'
import { useDark } from '@vueuse/core'
import moment from 'moment'

// 是否是暗黑模式
const isDark = useDark()

const route = useRoute()

// 日志数据
const journal = ref(null)
const isLoading = ref(true)
const journalContentRef = ref(null)

function formatDate(date) {
    if (!date) return ''
    return moment(date).format('YYYY-MM-DD HH:mm')
}

// 获取日志详情
function refreshJournalDetail(journalId) {
    getJournalDetail(journalId).then((res) => {
        journal.value = res.data

        // 必须先关闭 loading 让 v-else-if="journal" 分支渲染，再在 nextTick 中高亮代码
        isLoading.value = false

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
        })
    })
}

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

onMounted(() => {
    refreshJournalDetail(route.params.journalId)
})
</script>

<style scoped>
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
    font-size: 28px;
    font-weight: 700;
    margin-top: 44px;
    margin-bottom: 28px;
    padding-bottom: 16px;
    border-bottom: 1px solid rgb(241 245 249);
}

::v-deep(.dark .article-content h1) {
    --tw-text-opacity: 1;
    color: rgb(226 232 240/var(--tw-text-opacity));
    border-bottom-color: rgb(55 65 81);
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

::v-deep(.article-content blockquote p:last-child) {
    margin-bottom: 0;
}

::v-deep(.article-content em) {
    color: #c849ff;
}

::v-deep(.article-content a) {
    color: #167bc2;
}

::v-deep(.article-content a:hover) {
    text-decoration: underline;
}

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

::v-deep(.article-content ol) {
    list-style-type: decimal;
    padding-left: 2rem;
}

::v-deep(.dark .article-content ol) {
    color: #9e9e9e;
}

::v-deep(.article-content img) {
    max-width: 100%;
    overflow: hidden;
    display: block;
    margin: 0 auto;
    border-radius: 8px;
}

::v-deep(.article-content img:hover,
img:focus) {
    box-shadow: 2px 2px 10px 0 rgba(0, 0, 0, .15);
}

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

::v-deep(pre) {
    margin-bottom: 20px;
    padding-top: 30px;
    background: #21252b;
    border-radius: 6px;
    position: relative;
    overflow-x: auto;
    max-width: 100%;
}

::v-deep(pre code.hljs) {
    padding: 0.7rem 1rem;
    border-bottom-left-radius: 6px;
    border-bottom-right-radius: 6px;
}

::v-deep(pre:before) {
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

::v-deep(table) {
    margin-bottom: 20px;
    width: 100%;
    display: block;
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
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
</style>

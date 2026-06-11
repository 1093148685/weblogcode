<template>
    <header class="sticky top-0 z-30 bg-[var(--bg-card)]/95 backdrop-blur-md border-b border-[var(--border-base)] shadow-nav transition-all duration-300">
        <nav class="max-w-[1600px] mx-auto px-6">
            <div class="flex items-center justify-between h-header">

                <!-- 左侧：移动端目录菜单按钮 + Logo -->
                <div class="flex items-center gap-3">
                    <!-- 移动端目录按钮 -->
                    <button @click="drawerOpen = !drawerOpen"
                        class="lg:hidden p-2 text-[var(--text-secondary)] hover:bg-[var(--bg-hover)] rounded-btn transition-all duration-200"
                        aria-label="目录">
                        <svg class="w-5 h-5" fill="none" viewBox="0 0 17 14">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="M1 1h15M1 7h15M1 13h15" />
                        </svg>
                    </button>

                    <!-- Logo -->
                    <a href="/" class="flex items-center gap-3 group">
                        <img :src="blogSettingsStore.blogSettings.logo"
                            class="h-9 w-9 rounded-full ring-2 ring-transparent group-hover:ring-[var(--color-primary)] transition-all duration-300"
                            alt="Logo" />
                        <span class="text-lg font-semibold text-[var(--text-heading)] tracking-tight hidden sm:block">{{
                            blogSettingsStore.blogSettings.name }}</span>
                    </a>
                </div>

                <!-- 中间：知识库名称 -->
                <div v-if="props.wikiTitle" class="hidden md:flex items-center gap-2 flex-1 min-w-0 mx-4">
                    <svg class="w-4 h-4 flex-shrink-0 text-[var(--text-muted)]" fill="none" viewBox="0 0 24 24">
                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                            d="M12 6.042A8.967 8.967 0 0 0 6 3.75c-1.052 0-2.062.18-3 .512v14.25A8.987 8.987 0 0 1 6 18c2.305 0 4.408.867 6 2.292m0-14.25a8.966 8.966 0 0 1 6-2.292c1.052 0 2.062.18 3 .512v14.25A8.987 8.987 0 0 0 18 18a8.967 8.967 0 0 0-6 2.292m0-14.25v14.25"/>
                    </svg>
                    <span class="text-sm font-medium text-[var(--text-secondary)] truncate">{{ props.wikiTitle }}</span>
                </div>

                <!-- 右侧操作区 -->
                <div class="flex items-center gap-2">
                    <!-- 搜索按钮 -->
                    <button @click="openSearch"
                        class="hidden md:flex items-center gap-2 px-3 py-1.5 text-sm text-[var(--text-muted)] bg-[var(--bg-hover)] rounded-btn hover:bg-[var(--bg-active)] transition-all duration-200">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 20 20">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z" />
                        </svg>
                        <span>搜索</span>
                        <kbd class="px-1.5 py-0.5 text-xs bg-[var(--bg-card)] border border-[var(--border-base)] rounded">Ctrl K</kbd>
                    </button>

                    <!-- 主题切换 -->
                    <button @click="toggleDark()"
                        class="p-2 text-[var(--text-secondary)] hover:bg-[var(--bg-hover)] rounded-btn transition-all duration-200"
                        aria-label="切换主题">
                        <svg v-if="!isDark" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <circle cx="12" cy="12" r="5" stroke-width="2"/>
                            <line x1="12" y1="1" x2="12" y2="3" stroke-width="2" stroke-linecap="round"/>
                            <line x1="12" y1="21" x2="12" y2="23" stroke-width="2" stroke-linecap="round"/>
                            <line x1="4.22" y1="4.22" x2="5.64" y2="5.64" stroke-width="2" stroke-linecap="round"/>
                            <line x1="18.36" y1="18.36" x2="19.78" y2="19.78" stroke-width="2" stroke-linecap="round"/>
                            <line x1="1" y1="12" x2="3" y2="12" stroke-width="2" stroke-linecap="round"/>
                            <line x1="21" y1="12" x2="23" y2="12" stroke-width="2" stroke-linecap="round"/>
                            <line x1="4.22" y1="19.78" x2="5.64" y2="18.36" stroke-width="2" stroke-linecap="round"/>
                            <line x1="18.36" y1="5.64" x2="19.78" y2="4.22" stroke-width="2" stroke-linecap="round"/>
                        </svg>
                        <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
                                d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
                        </svg>
                    </button>

                    <!-- 未登录 -->
                    <div v-if="!isLogined"
                        @click="$router.push('/login')"
                        class="px-4 py-2 text-sm font-medium text-[var(--text-body)] hover:bg-[var(--bg-hover)] rounded-btn cursor-pointer transition-all duration-200">
                        登录
                    </div>

                    <!-- 已登录：用户头像 -->
                    <div v-else class="relative" ref="dropdownRef">
                        <button @click="toggleDropdown"
                            class="p-0.5 rounded-full ring-2 ring-transparent hover:ring-[var(--color-primary)] transition-all duration-200">
                            <img class="w-9 h-9 rounded-full object-cover"
                                :src="blogSettingsStore.blogSettings.avatar" alt="用户头像">
                        </button>
                        <Transition name="dropdown">
                            <div v-if="dropdownOpen"
                                class="absolute right-0 top-full mt-2 w-44 bg-[var(--bg-card)] rounded-card shadow-elevated border border-[var(--border-base)] overflow-hidden z-50">
                                <ul class="py-1 text-sm">
                                    <li>
                                        <a @click="goAdmin"
                                            class="flex items-center gap-2.5 px-4 py-2.5 text-[var(--text-body)] hover:bg-[var(--bg-hover)] cursor-pointer transition-colors">
                                            <svg class="w-4 h-4 text-[var(--text-muted)]" fill="none" viewBox="0 0 20 20">
                                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                                    d="M10 14v4m-4 1h8M1 10h18M2 1h16a1 1 0 0 1 1 1v11a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1Z" />
                                            </svg>
                                            进入后台
                                        </a>
                                    </li>
                                    <li class="border-t border-[var(--border-light)]">
                                        <a @click="showLogoutConfirm = true; dropdownOpen = false"
                                            class="flex items-center gap-2.5 px-4 py-2.5 text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 cursor-pointer transition-colors">
                                            <svg class="w-4 h-4" fill="none" viewBox="0 0 16 16">
                                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                                    d="M4 8h11m0 0-4-4m4 4-4 4m-5 3H3a2 2 0 0 1-2-2V3a2 2 0 0 1 2-2h3" />
                                            </svg>
                                            退出登录
                                        </a>
                                    </li>
                                </ul>
                            </div>
                        </Transition>
                    </div>

                    <!-- 移动端搜索按钮 -->
                    <button @click="openSearch"
                        class="md:hidden p-2 text-[var(--text-secondary)] hover:bg-[var(--bg-hover)] rounded-btn transition-all duration-200">
                        <svg class="w-5 h-5" fill="none" viewBox="0 0 20 20">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z" />
                        </svg>
                    </button>
                </div>
            </div>
        </nav>
    </header>

    <!-- 阅读标题栏：向上滚动过标题后，从下往上滑入 -->
    <Transition name="reading-bar">
        <div v-if="props.showReadingTitle && props.articleTitle"
            class="fixed top-0 left-0 right-0 z-[60] h-header bg-[var(--bg-card)]/95 backdrop-blur-md border-b border-[var(--border-base)] shadow-nav flex items-center">
            <div class="max-w-[1600px] mx-auto w-full px-6 flex items-center gap-3">
                <svg class="w-4 h-4 flex-shrink-0 text-[var(--text-muted)]" fill="none" viewBox="0 0 24 24">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                        d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2"/>
                </svg>
                <span class="text-sm font-semibold text-[var(--text-heading)] truncate">{{ props.articleTitle }}</span>
            </div>
        </div>
    </Transition>

    <!-- 移动端目录侧边栏遮罩 -->
    <Transition name="modal-fade">
        <div v-if="drawerOpen"
            class="fixed inset-0 z-[45] lg:hidden"
            @click.self="drawerOpen = false">
            <div class="absolute inset-0 bg-gray-900/50 backdrop-blur-sm"></div>
            <!-- 侧边栏内容 -->
            <div class="absolute left-0 top-0 h-full w-72 bg-[var(--bg-card)] shadow-lg overflow-y-auto">
                <div class="flex items-center justify-between px-4 py-3 border-b border-[var(--border-base)]">
                    <span class="text-sm font-semibold text-[var(--text-heading)]">知识库目录</span>
                    <button @click="drawerOpen = false"
                        class="p-1.5 text-[var(--text-muted)] hover:bg-[var(--bg-hover)] rounded transition-colors">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 14 14">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="m1 1 6 6m0 0 6 6M7 7l6-6M7 7l-6 6" />
                        </svg>
                    </button>
                </div>
                <div class="p-3">
                    <ul class="space-y-0.5">
                        <template v-for="(catalog, index) in props.catalogs" :key="index">
                            <li>
                                <div class="text-xs font-semibold uppercase tracking-wider text-[var(--text-muted)] px-3 py-1.5 mt-2">
                                    {{ catalog.title }}
                                </div>
                            </li>
                            <li v-for="(childCatalog, childIndex) in catalog.children" :key="childIndex">
                                <div
                                    :class="childCatalog.articleId == route.query.articleId
                                        ? 'bg-[var(--bg-hover)] text-[var(--color-primary)] font-medium'
                                        : 'text-[var(--text-secondary)] hover:bg-[var(--bg-hover)] hover:text-[var(--text-body)]'"
                                    class="px-3 py-2 text-sm rounded-btn cursor-pointer transition-colors"
                                    @click="goWikiArticleDetailPage(childCatalog.articleId); drawerOpen = false"
                                    v-html="childCatalog.title">
                                </div>
                            </li>
                        </template>
                    </ul>
                </div>
            </div>
        </div>
    </Transition>

    <!-- 退出登录确认弹窗 -->
    <Transition name="modal-fade">
        <div v-if="showLogoutConfirm"
            class="fixed inset-0 z-[100] flex items-center justify-center p-4"
            @click.self="showLogoutConfirm = false">
            <div class="absolute inset-0 bg-gray-900/50 backdrop-blur-sm"></div>
            <div class="relative bg-[var(--bg-card)] rounded-card shadow-lg p-6 w-full max-w-sm text-center">
                <div class="mx-auto mb-4 w-12 h-12 flex items-center justify-center rounded-full bg-red-50 dark:bg-red-900/20">
                    <svg class="w-6 h-6 text-red-500" fill="none" viewBox="0 0 20 20">
                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                            d="M10 11V6m0 8h.01M19 10a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
                    </svg>
                </div>
                <h3 class="mb-2 text-lg font-semibold text-[var(--text-heading)]">确认退出登录？</h3>
                <p class="mb-6 text-sm text-[var(--text-secondary)]">退出后需要重新登录才能访问后台管理。</p>
                <div class="flex gap-3 justify-center">
                    <button @click="showLogoutConfirm = false"
                        class="px-5 py-2 text-sm font-medium text-[var(--text-body)] bg-[var(--bg-hover)] hover:bg-[var(--bg-active)] rounded-btn transition-all duration-200">
                        取消
                    </button>
                    <button @click="logout"
                        class="px-5 py-2 text-sm font-medium text-white bg-red-500 hover:bg-red-600 rounded-btn transition-all duration-200">
                        确认退出
                    </button>
                </div>
            </div>
        </div>
    </Transition>

    <!-- 站内搜索弹窗 -->
    <Transition name="search-modal">
        <div v-if="searchOpen"
            class="fixed inset-0 z-[100] flex items-start justify-center pt-[8vh] sm:pt-[12vh] px-4"
            @click.self="closeSearch">
            <div class="absolute inset-0 bg-gray-900/60 backdrop-blur-md"></div>
            <div class="search-modal-content relative w-full max-w-2xl bg-[var(--bg-card)] rounded-2xl shadow-elevated overflow-hidden border border-[var(--border-base)]">
                <!-- 搜索输入区 -->
                <div class="flex items-center px-5 gap-3 bg-[var(--bg-card)]">
                    <div class="flex-shrink-0">
                        <svg v-if="!searchLoading" class="w-5 h-5 text-[var(--color-primary)]" fill="none" viewBox="0 0 20 20">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z" />
                        </svg>
                        <svg v-else class="w-5 h-5 animate-spin text-[var(--color-primary)]" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                            <path class="opacity-75" fill="currentColor"
                                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"/>
                        </svg>
                    </div>
                    <input ref="searchInputRef" v-model="searchWord" type="text" placeholder="搜索文章标题、内容..."
                        class="flex-1 py-4 text-base text-[var(--text-body)] bg-transparent outline-none placeholder:text-[var(--text-placeholder)] font-medium" />
                    <button @click="closeSearch"
                        class="flex-shrink-0 p-2 text-[var(--text-muted)] hover:text-[var(--text-body)] hover:bg-[var(--bg-hover)] rounded-lg transition-all duration-200">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 14 14">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="m1 1 6 6m0 0 6 6M7 7l6-6M7 7l-6 6" />
                        </svg>
                    </button>
                </div>

                <div class="h-px bg-gradient-to-r from-transparent via-[var(--border-base)] to-transparent"></div>

                <!-- 搜索结果 -->
                <div class="max-h-[55vh] overflow-y-auto search-results-scroll">
                    <div v-if="searchArticles && searchArticles.length > 0" class="p-3">
                        <p class="text-xs text-[var(--text-muted)] mb-2 px-2">共找到 <span class="font-semibold text-[var(--color-primary)]">{{ total }}</span> 篇相关文章</p>
                        <ol class="space-y-0.5">
                            <li v-for="(article, index) in searchArticles" :key="index">
                                <a @click="jumpToArticleDetailPage(article.id)"
                                    class="flex items-center gap-3.5 p-3 rounded-xl hover:bg-[var(--bg-hover)] cursor-pointer transition-all duration-200 group">
                                    <div class="relative flex-shrink-0 w-[72px] h-[52px] rounded-lg overflow-hidden bg-[var(--bg-hover)] ring-1 ring-[var(--border-light)]">
                                        <img class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" :src="article.cover" alt="">
                                    </div>
                                    <div class="flex-1 min-w-0">
                                        <h3 class="text-sm font-semibold text-[var(--text-heading)] group-hover:text-[var(--color-primary)] transition-colors truncate leading-snug" v-html="article.title"></h3>
                                        <span class="inline-flex items-center gap-1.5 mt-1.5 text-xs text-[var(--text-muted)]">
                                            <svg class="w-3 h-3" fill="none" viewBox="0 0 20 20">
                                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                                    d="M5 1v3m5-3v3m5-3v3M1 7h18M5 11h10M2 3h16a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z" />
                                            </svg>
                                            {{ article.createDate }}
                                        </span>
                                    </div>
                                    <svg class="w-4 h-4 text-[var(--text-placeholder)] group-hover:text-[var(--color-primary)] group-hover:translate-x-0.5 transition-all duration-200 flex-shrink-0" fill="none" viewBox="0 0 24 24">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m9 5 7 7-7 7"/>
                                    </svg>
                                </a>
                            </li>
                        </ol>
                        <div class="flex items-center justify-between mt-3 pt-3 mx-2 border-t border-[var(--border-light)]">
                            <button v-if="current > 1" @click="prePage"
                                class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-[var(--text-secondary)] bg-[var(--bg-hover)] rounded-lg hover:bg-[var(--bg-active)] transition-colors">
                                <svg class="w-3 h-3" fill="none" viewBox="0 0 14 10">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 5H1m0 0 4 4M1 5l4-4" />
                                </svg>
                                上一页
                            </button>
                            <span v-else class="w-16"></span>
                            <span class="text-xs font-medium text-[var(--text-muted)]">{{ current }} / {{ pages }}</span>
                            <button v-if="current < pages" @click="nextPage"
                                class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-[var(--text-secondary)] bg-[var(--bg-hover)] rounded-lg hover:bg-[var(--bg-active)] transition-colors">
                                下一页
                                <svg class="w-3 h-3" fill="none" viewBox="0 0 14 10">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M1 5h12m0 0L9 1m4 4L9 9" />
                                </svg>
                            </button>
                            <span v-else class="w-16"></span>
                        </div>
                    </div>
                    <div v-else-if="searchWord && !searchLoading" class="flex flex-col items-center justify-center py-14">
                        <div class="w-16 h-16 mb-4 rounded-full bg-[var(--bg-hover)] flex items-center justify-center">
                            <svg class="w-8 h-8 text-[var(--text-placeholder)]" fill="none" viewBox="0 0 24 24">
                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                            </svg>
                        </div>
                        <p class="text-sm font-medium text-[var(--text-secondary)]">未找到相关文章</p>
                        <p class="text-xs text-[var(--text-muted)] mt-1">换个关键词试试</p>
                    </div>
                    <div v-else-if="!searchWord" class="flex flex-col items-center justify-center py-12">
                        <div class="w-14 h-14 mb-3 rounded-full bg-[var(--bg-hover)] flex items-center justify-center">
                            <svg class="w-6 h-6 text-[var(--text-muted)]" fill="none" viewBox="0 0 24 24">
                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                            </svg>
                        </div>
                        <p class="text-sm text-[var(--text-muted)]">输入关键词开始搜索</p>
                    </div>
                </div>
                <div class="flex items-center gap-4 px-5 py-2.5 border-t border-[var(--border-base)] bg-[var(--bg-base)] text-xs text-[var(--text-muted)]">
                    <span class="flex items-center gap-1.5">
                        <kbd class="search-kbd">Esc</kbd>
                        关闭
                    </span>
                    <span class="ml-auto">基于 <a href="https://lucene.apache.org/" target="_blank" class="underline hover:text-[var(--color-primary)] transition-colors">Apache Lucene</a> 全文检索</span>
                </div>
            </div>
        </div>
    </Transition>
</template>

<script setup>
import { onMounted, onBeforeUnmount, ref, watch, nextTick } from 'vue'
import { useBlogSettingsStore } from '@/stores/blogsettings'
import { useUserStore } from '@/stores/user'
import { useRouter, useRoute } from 'vue-router'
import { showMessage } from '@/composables/util'
import { getArticleSearchPageList } from '@/api/frontend/search'

const router = useRouter()
const route = useRoute()

// 对外暴露属性，将目录数据传进来
const props = defineProps({
    catalogs: {
        type: Array,
        default: () => []
    },
    wikiTitle: {
        type: String,
        default: ''
    },
    articleTitle: {
        type: String,
        default: ''
    },
    showReadingTitle: {
        type: Boolean,
        default: false
    }
})

// ── Store ────────────────────────────────────────────────────────────
const blogSettingsStore = useBlogSettingsStore()
const userStore = useUserStore()

// ── 深色模式 ─────────────────────────────────────────────────────────
const isDark = ref(document.documentElement.classList.contains('dark'))
const toggleDark = () => {
    isDark.value = !isDark.value
    if (isDark.value) {
        document.documentElement.classList.add('dark')
        localStorage.setItem('color-scheme', 'dark')
    } else {
        document.documentElement.classList.remove('dark')
        localStorage.setItem('color-scheme', 'light')
    }
}

// ── 登录状态 ─────────────────────────────────────────────────────────
const isLogined = ref(Object.keys(userStore.userInfo).length > 0)

// ── 退出登录 ─────────────────────────────────────────────────────────
const showLogoutConfirm = ref(false)
const logout = () => {
    userStore.logout()
    isLogined.value = false
    showLogoutConfirm.value = false
    showMessage('退出登录成功')
}

// ── 用户下拉菜单 ─────────────────────────────────────────────────────
const dropdownOpen = ref(false)
const dropdownRef = ref(null)
const toggleDropdown = () => { dropdownOpen.value = !dropdownOpen.value }
const goAdmin = () => {
    dropdownOpen.value = false
    router.push('/admin/index')
}
const handleClickOutside = (e) => {
    if (dropdownRef.value && !dropdownRef.value.contains(e.target)) {
        dropdownOpen.value = false
    }
}

// ── 移动端知识库目录 Drawer ───────────────────────────────────────────
const drawerOpen = ref(false)

// ── 知识库文章跳转 ───────────────────────────────────────────────────
const goWikiArticleDetailPage = (articleId) => {
    router.push({ path: '/wiki/' + route.params.wikiId, query: { articleId } })
}

// ── 搜索弹窗 ─────────────────────────────────────────────────────────
const searchOpen = ref(false)
const searchInputRef = ref(null)

const openSearch = async () => {
    searchOpen.value = true
    await nextTick()
    searchInputRef.value?.focus()
}

const closeSearch = () => {
    searchOpen.value = false
    searchWord.value = ''
    searchArticles.value = []
    current.value = 1
    total.value = 0
    pages.value = 0
}

// ── 搜索数据 ─────────────────────────────────────────────────────────
const searchArticles = ref([])
const current = ref(1)
const total = ref(0)
const size = ref(5)
const pages = ref(0)
const searchWord = ref('')
const searchLoading = ref(false)

watch(searchWord, (newVal, oldVal) => {
    if (newVal && newVal !== oldVal) {
        current.value = 1
        renderSearchArticles({ current: 1, size: size.value, word: newVal })
    } else if (!newVal) {
        searchArticles.value = []
        total.value = 0
        pages.value = 0
    }
})

function renderSearchArticles(data) {
    searchLoading.value = true
    getArticleSearchPageList(data).then(res => {
        if (res.success) {
            searchArticles.value = res.data.list || []
            current.value = res.data.pageNum || 1
            size.value = res.data.pageSize || 5
            total.value = res.data.total || 0
            pages.value = Math.ceil((res.data.total || 0) / (res.data.pageSize || 5))
        }
    }).finally(() => { searchLoading.value = false })
}

const nextPage = () => { renderSearchArticles({ current: current.value + 1, size: size.value, word: searchWord.value }) }
const prePage = () => { renderSearchArticles({ current: current.value - 1, size: size.value, word: searchWord.value }) }

const jumpToArticleDetailPage = (articleId) => {
    closeSearch()
    router.push('/article/' + articleId)
}

// ── 键盘快捷键 ───────────────────────────────────────────────────────
const handleKeyDown = (e) => {
    if (e.ctrlKey && e.key === 'k') {
        e.preventDefault()
        openSearch()
    }
    if (e.key === 'Escape') {
        if (searchOpen.value) closeSearch()
        if (showLogoutConfirm.value) showLogoutConfirm.value = false
        if (dropdownOpen.value) dropdownOpen.value = false
        if (drawerOpen.value) drawerOpen.value = false
    }
}

// ── 生命周期 ─────────────────────────────────────────────────────────
onMounted(() => {
    blogSettingsStore.getBlogSettings()
    window.addEventListener('keydown', handleKeyDown)
    document.addEventListener('click', handleClickOutside)
})

onBeforeUnmount(() => {
    window.removeEventListener('keydown', handleKeyDown)
    document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped>
.dropdown-enter-active,
.dropdown-leave-active {
    transition: opacity 0.15s ease, transform 0.15s ease;
}
.dropdown-enter-from,
.dropdown-leave-to {
    opacity: 0;
    transform: translateY(-6px) scale(0.97);
}

.modal-fade-enter-active,
.modal-fade-leave-active {
    transition: opacity 0.2s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
    opacity: 0;
}

/* 阅读标题栏动画：从下往上滑入 */
.reading-bar-enter-active,
.reading-bar-leave-active {
    transition: transform 0.25s cubic-bezier(0.4, 0, 0.2, 1), opacity 0.25s ease;
}
.reading-bar-enter-from,
.reading-bar-leave-to {
    transform: translateY(100%);
    opacity: 0;
}

/* 搜索弹窗动画 */
.search-modal-enter-active,
.search-modal-leave-active {
    transition: opacity 0.25s ease;
}
.search-modal-enter-from,
.search-modal-leave-to {
    opacity: 0;
}
.search-modal-enter-active .search-modal-content,
.search-modal-leave-active .search-modal-content {
    transition: opacity 0.25s ease, transform 0.25s cubic-bezier(0.16, 1, 0.3, 1);
}
.search-modal-enter-from .search-modal-content,
.search-modal-leave-to .search-modal-content {
    opacity: 0;
    transform: translateY(-20px) scale(0.95);
}

/* 搜索快捷键样式 */
.search-kbd {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 1.5rem;
    padding: 2px 6px;
    font-size: 11px;
    font-family: inherit;
    background: var(--bg-card);
    border: 1px solid var(--border-base);
    border-radius: 4px;
    box-shadow: 0 1px 0 var(--border-base);
    line-height: 1.4;
}

/* 搜索结果滚动条 */
.search-results-scroll::-webkit-scrollbar {
    width: 4px;
}
.search-results-scroll::-webkit-scrollbar-track {
    background: transparent;
}
.search-results-scroll::-webkit-scrollbar-thumb {
    background: var(--border-base);
    border-radius: 4px;
}
.search-results-scroll::-webkit-scrollbar-thumb:hover {
    background: var(--text-placeholder);
}
</style>

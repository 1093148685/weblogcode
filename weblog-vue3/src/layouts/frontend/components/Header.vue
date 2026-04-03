<template>
    <header class="sticky top-0 z-50 bg-[var(--bg-card)]/95 backdrop-blur-md border-b border-[var(--border-base)] shadow-nav transition-all duration-300">
        <nav class="max-w-content mx-auto px-6">
            <div class="flex items-center justify-between h-header">

                <!-- Logo -->
                <a href="/" class="flex items-center gap-3 group flex-shrink-0">
                    <img :src="blogSettingsStore.blogSettings.logo"
                        class="h-9 w-9 rounded-full ring-2 ring-transparent group-hover:ring-[var(--color-primary)] transition-all duration-300"
                        alt="Logo" />
                    <span class="site-name hidden sm:block">{{
                        blogSettingsStore.blogSettings.name }}</span>
                </a>

                <!-- 桌面端导航 -->
                <div class="hidden md:flex items-center">
                    <ul class="flex items-center gap-1">
                        <li v-for="item in navItems" :key="item.label">
                            <a @click="navigateTo(item)"
                                :class="isActive(item)
                                    ? 'text-[var(--color-primary)] bg-[var(--bg-hover)] font-semibold'
                                    : 'text-[var(--text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--bg-hover)]'"
                                class="px-4 py-2 rounded-btn text-sm font-medium transition-all duration-200 cursor-pointer block">
                                {{ item.label }}
                            </a>
                        </li>
                    </ul>
                </div>

                <!-- 右侧操作区 -->
                <div class="flex items-center gap-2">

                    <!-- 搜索按钮（桌面端） -->
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
                        <!-- 太阳（亮色模式） -->
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
                        <!-- 月亮（深色模式） -->
                        <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
                                d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
                        </svg>
                    </button>

                    <!-- 未登录：登录按钮 -->
                    <div v-if="!isLogined"
                        @click="$router.push('/login')"
                        class="px-4 py-2 text-sm font-medium text-[var(--text-body)] hover:bg-[var(--bg-hover)] rounded-btn cursor-pointer transition-all duration-200">
                        登录
                    </div>

                    <!-- 已登录：用户头像 + 下拉菜单 -->
                    <div v-else class="relative" ref="dropdownRef">
                        <button @click="toggleDropdown"
                            class="p-0.5 rounded-full ring-2 ring-transparent hover:ring-[var(--color-primary)] transition-all duration-200">
                            <img class="w-9 h-9 rounded-full object-cover"
                                :src="blogSettingsStore.blogSettings.avatar" alt="用户头像">
                        </button>
                        <!-- 下拉菜单 -->
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

                    <!-- 移动端菜单按钮 -->
                    <button @click="toggleMobileMenu" type="button"
                        class="md:hidden p-2 text-[var(--text-secondary)] hover:bg-[var(--bg-hover)] rounded-btn transition-all duration-200">
                        <svg v-if="!mobileMenuOpen" class="w-5 h-5" fill="none" viewBox="0 0 17 14">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="M1 1h15M1 7h15M1 13h15" />
                        </svg>
                        <svg v-else class="w-5 h-5" fill="none" viewBox="0 0 14 14">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="m1 1 6 6m0 0 6 6M7 7l6-6M7 7l-6 6" />
                        </svg>
                    </button>
                </div>
            </div>

            <!-- 移动端导航菜单 -->
            <Transition name="slide-down">
                <div v-if="mobileMenuOpen" class="md:hidden border-t border-[var(--border-light)] py-3">
                    <!-- 移动端搜索 -->
                    <button @click="openSearch; mobileMenuOpen = false"
                        class="w-full flex items-center gap-2 px-4 py-2.5 mb-2 text-sm text-[var(--text-muted)] bg-[var(--bg-hover)] rounded-btn transition-all duration-200">
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 20 20">
                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                d="m19 19-4-4m0-7A7 7 0 1 1 1 8a7 7 0 0 1 14 0Z" />
                        </svg>
                        <span>搜索文章...</span>
                        <span class="ml-auto text-xs px-1.5 py-0.5 bg-[var(--bg-card)] border border-[var(--border-base)] rounded">Ctrl K</span>
                    </button>
                    <!-- 移动端导航项 -->
                    <ul class="space-y-1">
                        <li v-for="item in navItems" :key="item.label">
                            <a @click="navigateTo(item); mobileMenuOpen = false"
                                :class="isActive(item)
                                    ? 'text-[var(--color-primary)] bg-[var(--bg-hover)] font-semibold'
                                    : 'text-[var(--text-secondary)] hover:text-[var(--color-primary)] hover:bg-[var(--bg-hover)]'"
                                class="block px-4 py-2.5 text-sm rounded-btn transition-all duration-200 cursor-pointer">
                                {{ item.label }}
                            </a>
                        </li>
                    </ul>
                </div>
            </Transition>
        </nav>
    </header>

    <!-- 退出登录确认弹窗 -->
    <Transition name="modal-fade">
        <div v-if="showLogoutConfirm"
            class="fixed inset-0 z-[100] flex items-center justify-center p-4"
            @click.self="showLogoutConfirm = false">
            <!-- 背景遮罩 -->
            <div class="absolute inset-0 bg-gray-900/50 backdrop-blur-sm"></div>
            <!-- 弹窗内容 -->
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
            <!-- 背景遮罩 -->
            <div class="absolute inset-0 bg-black/50"></div>
            <!-- 搜索框主体 -->
            <div class="search-modal-content relative w-full max-w-2xl bg-[var(--bg-card)] rounded-2xl shadow-elevated overflow-hidden border border-[var(--border-base)]">
                <!-- 搜索输入区 -->
                <div class="flex items-center px-5 gap-3 bg-[var(--bg-card)]">
                    <!-- 搜索图标 / Loading -->
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
                    <input
                        ref="searchInputRef"
                        v-model="searchWord"
                        type="text"
                        placeholder="搜索文章标题、内容..."
                        class="flex-1 py-4 text-base text-[var(--text-body)] bg-transparent outline-none placeholder:text-[var(--text-placeholder)] font-medium"
                    />
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
                    <!-- 有结果 -->
                    <div v-if="searchArticles && searchArticles.length > 0" class="p-3">
                        <p class="text-xs text-[var(--text-muted)] mb-2 px-2">共找到 <span class="font-semibold text-[var(--color-primary)]">{{ total }}</span> 篇相关文章</p>
                        <ol class="space-y-0.5">
                            <li v-for="(article, index) in searchArticles" :key="index">
                                <a @click="jumpToArticleDetailPage(article.id)"
                                    class="search-result-item flex items-center gap-3.5 p-3 rounded-xl hover:bg-[var(--bg-hover)] cursor-pointer transition-all duration-200 group">
                                    <div class="relative flex-shrink-0 w-[72px] h-[52px] rounded-lg overflow-hidden bg-[var(--bg-hover)] ring-1 ring-[var(--border-light)]">
                                        <img class="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                                            :src="article.cover" alt="">
                                    </div>
                                    <div class="flex-1 min-w-0">
                                        <h3 class="text-sm font-semibold text-[var(--text-heading)] group-hover:text-[var(--color-primary)] transition-colors truncate leading-snug"
                                            v-html="article.title"></h3>
                                        <span class="inline-flex items-center gap-1.5 mt-1.5 text-xs text-[var(--text-muted)]">
                                            <svg class="w-3 h-3" fill="none" viewBox="0 0 20 20">
                                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                                    d="M5 1v3m5-3v3m5-3v3M1 7h18M5 11h10M2 3h16a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z" />
                                            </svg>
                                            {{ article.createDate }}
                                        </span>
                                    </div>
                                    <!-- 跳转箭头 -->
                                    <svg class="w-4 h-4 text-[var(--text-placeholder)] group-hover:text-[var(--color-primary)] group-hover:translate-x-0.5 transition-all duration-200 flex-shrink-0" fill="none" viewBox="0 0 24 24">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m9 5 7 7-7 7"/>
                                    </svg>
                                </a>
                            </li>
                        </ol>
                        <!-- 分页 -->
                        <div class="flex items-center justify-between mt-3 pt-3 mx-2 border-t border-[var(--border-light)]">
                            <button v-if="current > 1" @click="prePage"
                                class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-[var(--text-secondary)] bg-[var(--bg-hover)] rounded-lg hover:bg-[var(--bg-active)] transition-colors">
                                <svg class="w-3 h-3" fill="none" viewBox="0 0 14 10">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                        d="M13 5H1m0 0 4 4M1 5l4-4" />
                                </svg>
                                上一页
                            </button>
                            <span v-else class="w-16"></span>
                            <span class="text-xs font-medium text-[var(--text-muted)]">{{ current }} / {{ pages }}</span>
                            <button v-if="current < pages" @click="nextPage"
                                class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-[var(--text-secondary)] bg-[var(--bg-hover)] rounded-lg hover:bg-[var(--bg-active)] transition-colors">
                                下一页
                                <svg class="w-3 h-3" fill="none" viewBox="0 0 14 10">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                        d="M1 5h12m0 0L9 1m4 4L9 9" />
                                </svg>
                            </button>
                            <span v-else class="w-16"></span>
                        </div>
                    </div>

                    <!-- 无结果 -->
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

                    <!-- 初始提示 -->
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

                <!-- 底部提示栏 -->
                <div class="flex items-center gap-4 px-5 py-2.5 border-t border-[var(--border-base)] bg-[var(--bg-base)] text-xs text-[var(--text-muted)]">
                    <span class="flex items-center gap-1.5">
                        <kbd class="search-kbd">Esc</kbd>
                        关闭
                    </span>
                    <span class="flex items-center gap-1.5">
                        <kbd class="search-kbd">↵</kbd>
                        跳转
                    </span>
                    <span class="ml-auto">基于 <a href="https://lucene.apache.org/" target="_blank" class="underline hover:text-[var(--color-primary)] transition-colors">Apache Lucene</a> 全文检索</span>
                </div>
            </div>
        </div>
    </Transition>
</template>

<script setup>
import { onMounted, onBeforeUnmount, ref, watch, computed, nextTick } from 'vue'
import { useBlogSettingsStore } from '@/stores/blogsettings'
import { useUserStore } from '@/stores/user'
import { useRouter, useRoute } from 'vue-router'
import { showMessage } from '@/composables/util'
import { getArticleSearchPageList } from '@/api/frontend/search'

const router = useRouter()
const route = useRoute()

// ── Store ────────────────────────────────────────────────────────────
const blogSettingsStore = useBlogSettingsStore()
const userStore = useUserStore()

// ── 深色模式（不依赖 @vueuse/core，直接操作 html.dark）──────────────
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
// 初始化时同步本地存储的主题
const initTheme = () => {
    const saved = localStorage.getItem('color-scheme')
    if (saved === 'dark' || (!saved && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
        document.documentElement.classList.add('dark')
        isDark.value = true
    } else {
        document.documentElement.classList.remove('dark')
        isDark.value = false
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
// 点击外部关闭下拉菜单
const handleClickOutside = (e) => {
    if (dropdownRef.value && !dropdownRef.value.contains(e.target)) {
        dropdownOpen.value = false
    }
}

// ── 移动端菜单 ───────────────────────────────────────────────────────
const mobileMenuOpen = ref(false)
const toggleMobileMenu = () => { mobileMenuOpen.value = !mobileMenuOpen.value }

// ── 导航项配置 ───────────────────────────────────────────────────────
const navItems = [
    { label: '首页', path: '/', exact: true },
    { label: '分类', path: '/category/list', prefix: '/category' },
    { label: '标签', path: '/tag/list', prefix: '/tag' },
    { label: '归档', path: '/archive/list' },
    { label: '知识库', path: '/wiki/list', prefix: '/wiki' },
    { label: '留言板', path: '/', query: { view: 'message-wall' }, viewKey: 'message-wall' },
    { label: 'AI 聊天', path: '/', query: { view: 'ai-chat' }, viewKey: 'ai-chat' },
]

const isActive = (item) => {
    if (item.viewKey) {
        return route.path === '/' && route.query.view === item.viewKey
    }
    if (item.exact) {
        return route.path === '/' && !route.query.view
    }
    if (item.prefix) {
        return route.path.startsWith(item.prefix)
    }
    return route.path === item.path
}

const navigateTo = (item) => {
    if (item.query) {
        router.push({ path: item.path, query: item.query })
    } else {
        router.push(item.path)
    }
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

const nextPage = () => {
    renderSearchArticles({ current: current.value + 1, size: size.value, word: searchWord.value })
}

const prePage = () => {
    renderSearchArticles({ current: current.value - 1, size: size.value, word: searchWord.value })
}

const jumpToArticleDetailPage = (articleId) => {
    closeSearch()
    router.push('/article/' + articleId)
}

// ── 键盘快捷键 ───────────────────────────────────────────────────────
const handleKeyDown = (e) => {
    // Ctrl+K 打开搜索
    if (e.ctrlKey && e.key === 'k') {
        e.preventDefault()
        openSearch()
    }
    // Esc 关闭搜索
    if (e.key === 'Escape') {
        if (searchOpen.value) closeSearch()
        if (showLogoutConfirm.value) showLogoutConfirm.value = false
        if (dropdownOpen.value) dropdownOpen.value = false
    }
}

// ── 生命周期 ─────────────────────────────────────────────────────────
onMounted(() => {
    initTheme()
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
/* 网站名称风格文字 */
.site-name {
    font-size: 1.15rem;
    font-weight: 700;
    letter-spacing: -0.02em;
    background: linear-gradient(135deg, var(--color-primary, #3b82f6) 0%, #60a5fa 50%, #93c5fd 100%);
    background-size: 200% 200%;
    -webkit-background-clip: text;
    background-clip: text;
    -webkit-text-fill-color: transparent;
    animation: gradient-shift 6s ease infinite;
    transition: transform 0.3s ease, filter 0.3s ease;
}

.group:hover .site-name {
    transform: translateY(-1px);
    filter: brightness(1.1);
    animation-duration: 2s;
}

@keyframes gradient-shift {
    0%, 100% { background-position: 0% 50%; }
    50% { background-position: 100% 50%; }
}

/* 下拉菜单动画 */
.dropdown-enter-active,
.dropdown-leave-active {
    transition: opacity 0.15s ease, transform 0.15s ease;
}
.dropdown-enter-from,
.dropdown-leave-to {
    opacity: 0;
    transform: translateY(-6px) scale(0.97);
}

/* 移动端菜单动画 */
.slide-down-enter-active,
.slide-down-leave-active {
    transition: opacity 0.2s ease, transform 0.2s ease;
}
.slide-down-enter-from,
.slide-down-leave-to {
    opacity: 0;
    transform: translateY(-8px);
}

/* 弹窗动画 */
.modal-fade-enter-active,
.modal-fade-leave-active {
    transition: opacity 0.2s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
    opacity: 0;
}
.modal-fade-enter-active > div:last-child,
.modal-fade-leave-active > div:last-child {
    transition: opacity 0.2s ease, transform 0.2s ease;
}
.modal-fade-enter-from > div:last-child,
.modal-fade-leave-to > div:last-child {
    opacity: 0;
    transform: translateY(-12px) scale(0.97);
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

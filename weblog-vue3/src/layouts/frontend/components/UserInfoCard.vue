<template>
    <div class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card shadow-card overflow-hidden">
        <!-- 顶部装饰条 -->
        <div class="h-1.5 bg-gradient-to-r from-blue-600 via-blue-500 to-blue-400"></div>
        <div class="p-5">
            <div class="flex flex-col items-center">
                <!-- 博主头像 -->
                <div class="relative mb-4">
                    <img class="w-20 h-20 rounded-full object-cover ring-4 ring-[var(--border-light)] shadow-md"
                        :src="blogSettingsStore.blogSettings.avatar" alt="博主头像" />
                    <span class="absolute bottom-1 right-1 w-3.5 h-3.5 bg-green-400 border-2 border-[var(--bg-card)] rounded-full shadow"></span>
                </div>

                <!-- 博主昵称 -->
                <h5 class="mb-1 text-lg font-semibold text-[var(--text-heading)]">
                    {{ blogSettingsStore.blogSettings.author }}
                </h5>

                <!-- 介绍语 -->
                <p class="mb-5 text-sm text-[var(--text-secondary)] text-center leading-relaxed px-2"
                    :title="blogSettingsStore.blogSettings.introduction">
                    {{ blogSettingsStore.blogSettings.introduction }}
                </p>

                <!-- 分隔线 -->
                <div class="w-full border-t border-[var(--border-light)] mb-4"></div>

                <!-- 统计数据 -->
                <div class="grid grid-cols-4 w-full gap-1 mb-5">
                    <div class="flex flex-col items-center gap-1 py-2 rounded-btn hover:bg-[var(--bg-hover)] transition-colors cursor-default group">
                        <CountTo :value="statisticsInfo.articleCount" customClass="text-lg font-bold text-[var(--text-heading)] group-hover:text-[var(--color-primary)] transition-colors"></CountTo>
                        <div class="text-xs text-[var(--text-muted)]">文章</div>
                    </div>
                    <div class="flex flex-col items-center gap-1 py-2 rounded-btn hover:bg-[var(--bg-hover)] transition-colors cursor-default group">
                        <CountTo :value="statisticsInfo.categoryCount" customClass="text-lg font-bold text-[var(--text-heading)] group-hover:text-[var(--color-primary)] transition-colors"></CountTo>
                        <div class="text-xs text-[var(--text-muted)]">分类</div>
                    </div>
                    <div class="flex flex-col items-center gap-1 py-2 rounded-btn hover:bg-[var(--bg-hover)] transition-colors cursor-default group">
                        <CountTo :value="statisticsInfo.tagCount" customClass="text-lg font-bold text-[var(--text-heading)] group-hover:text-[var(--color-primary)] transition-colors"></CountTo>
                        <div class="text-xs text-[var(--text-muted)]">标签</div>
                    </div>
                    <div class="flex flex-col items-center gap-1 py-2 rounded-btn hover:bg-[var(--bg-hover)] transition-colors cursor-default group">
                        <CountTo :value="statisticsInfo.totalPv" customClass="text-lg font-bold text-[var(--text-heading)] group-hover:text-[var(--color-primary)] transition-colors"></CountTo>
                        <div class="text-xs text-[var(--text-muted)]">访问</div>
                    </div>
                </div>

                <!-- 社交链接 -->
                <div v-if="hasSocialLinks" class="flex justify-center gap-3 flex-wrap">
                    <!-- GitHub -->
                    <button v-if="blogSettingsStore.blogSettings.githubHomepage"
                        @click="jump(blogSettingsStore.blogSettings.githubHomepage)"
                        title="我的 GitHub"
                        class="w-9 h-9 flex items-center justify-center rounded-full bg-[var(--bg-hover)] hover:bg-[#24292e] hover:text-white text-[var(--text-secondary)] transition-all duration-200 hover:scale-110">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                            <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0 0 24 12c0-6.63-5.37-12-12-12z"/>
                        </svg>
                    </button>
                    <!-- Gitee -->
                    <button v-if="blogSettingsStore.blogSettings.giteeHomepage"
                        @click="jump(blogSettingsStore.blogSettings.giteeHomepage)"
                        title="我的 Gitee"
                        class="w-9 h-9 flex items-center justify-center rounded-full bg-[var(--bg-hover)] hover:bg-[#C71D23] hover:text-white text-[var(--text-secondary)] transition-all duration-200 hover:scale-110">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                            <path d="M11.984 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0a12 12 0 0 0-.016 0zm6.09 5.333c.328 0 .593.266.592.593v1.482a.594.594 0 0 1-.593.592H9.777c-.982 0-1.778.796-1.778 1.778v5.63c0 .327.266.592.593.592h5.63c.982 0 1.778-.796 1.778-1.778v-.296a.593.593 0 0 0-.592-.593h-4.15a.592.592 0 0 1-.592-.592v-1.482a.593.593 0 0 1 .593-.592h6.815c.327 0 .593.265.593.592v3.408a4 4 0 0 1-4 4H5.926a.593.593 0 0 1-.593-.593V9.778a4.444 4.444 0 0 1 4.445-4.444h8.296z"/>
                        </svg>
                    </button>
                    <!-- 知乎 -->
                    <button v-if="blogSettingsStore.blogSettings.zhihuHomepage"
                        @click="jump(blogSettingsStore.blogSettings.zhihuHomepage)"
                        title="我的知乎"
                        class="w-9 h-9 flex items-center justify-center rounded-full bg-[var(--bg-hover)] hover:bg-[#0084ff] hover:text-white text-[var(--text-secondary)] transition-all duration-200 hover:scale-110">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                            <path d="M5.721 0C2.251 0 0 2.25 0 5.719V18.28C0 21.751 2.252 24 5.721 24H18.28C21.751 24 24 21.75 24 18.281V5.72C24 2.249 21.75 0 18.281 0zm6.964 5.6c-.085.688-.143 1.463-.171 2.315h3.514l-.343 1.029H12.51c-.03.83-.065 1.728-.107 2.7h3.205c-.11 2.668-.323 4.58-.64 5.74-.145.53-.347.882-.607 1.058-.258.176-.68.265-1.264.265a12.3 12.3 0 0 1-1.773-.16l.009 1.025c.648.118 1.258.177 1.831.177.908 0 1.58-.165 2.016-.495.437-.33.75-.899.937-1.707.37-1.578.575-3.781.618-6.608l.005-.295H18.4l-.343-1.029h-2.098c.028-.85.058-1.627.089-2.314h2.723V5.6zm-4.84.019H9.49v4.5H7.14c.32-1.503.512-3.008.576-4.5H6.645c-.2 2.393-.889 4.67-2.065 6.484.287.083.803.315 1.074.497.408-.671.759-1.398 1.05-2.177H9.49v4.72c-.827.12-1.659.21-2.49.261l.376 1.205c.68-.093 1.374-.208 2.081-.343l.036-.007v4.241h1.083v-4.51c.72-.168 1.437-.37 2.148-.601l-.165-1.045c-.647.234-1.313.438-1.984.605v-4.526h2.005V6.648H10.58V5.619z"/>
                        </svg>
                    </button>
                    <!-- CSDN -->
                    <button v-if="blogSettingsStore.blogSettings.csdnHomepage"
                        @click="jump(blogSettingsStore.blogSettings.csdnHomepage)"
                        title="我的 CSDN"
                        class="w-9 h-9 flex items-center justify-center rounded-full bg-[var(--bg-hover)] hover:bg-[#FC5531] hover:text-white text-[var(--text-secondary)] transition-all duration-200 hover:scale-110">
                        <svg class="w-5 h-5" viewBox="0 0 24 24" fill="currentColor">
                            <path d="M12 0a12 12 0 1 0 0 24A12 12 0 0 0 12 0zm-.5 17.5H8a.5.5 0 0 1-.5-.5V7a.5.5 0 0 1 .5-.5h3.5c2.485 0 4.5 2.015 4.5 4.5v2c0 2.485-2.015 4.5-4.5 4.5zm.5-9H8.5v7H12a3 3 0 0 0 3-3v-1a3 3 0 0 0-3-3z"/>
                        </svg>
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useBlogSettingsStore } from '@/stores/blogsettings'
import { getStatisticsInfo } from '@/api/frontend/statistics'
import CountTo from '@/components/CountTo.vue'
import { setCache, getCache } from '@/composables/useCache'

const blogSettingsStore = useBlogSettingsStore()

onMounted(() => {
    blogSettingsStore.getBlogSettings()
})

const jump = (url) => {
    window.open(url, '_blank')
}

// 统计信息
const statisticsInfo = ref({
    articleCount: 0,
    categoryCount: 0,
    tagCount: 0,
    totalPv: 0
})

// 是否有社交链接
const hasSocialLinks = computed(() => {
    const s = blogSettingsStore.blogSettings
    return s.githubHomepage || s.giteeHomepage || s.zhihuHomepage || s.csdnHomepage
})

// 优先从缓存读取
const cachedStats = getCache('sidebar_statistics')
if (cachedStats) {
    statisticsInfo.value = cachedStats
} else {
    getStatisticsInfo().then(res => {
        if (res.success) {
            statisticsInfo.value = res.data
            setCache('sidebar_statistics', res.data, 10 * 60 * 1000)
        }
    })
}
</script>

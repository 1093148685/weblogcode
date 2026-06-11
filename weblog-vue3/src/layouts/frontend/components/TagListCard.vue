<template>
    <div v-if="tags && tags.length > 0"
        class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card shadow-card p-4">
        <!-- 标题行 -->
        <div class="flex items-center justify-between mb-3">
            <h2 class="flex items-center gap-1.5 text-sm font-semibold text-[var(--text-heading)] uppercase tracking-wide">
                <svg class="w-4 h-4 flex-shrink-0" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                        d="M5 5h1.5M5 8h1.5m-1.5 3h1.5M9 5h6m-6 3h6M9 11h6M3 3h14a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z" />
                </svg>
                标签
            </h2>
            <a @click="router.push('/tag/list')"
                class="cursor-pointer flex items-center justify-center w-6 h-6 rounded-full bg-[var(--bg-hover)] hover:bg-[var(--bg-active)] text-[var(--text-muted)] hover:text-[var(--color-primary)] transition-colors">
                <svg class="w-3 h-3" fill="none" viewBox="0 0 8 14">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="m1 13 5.7-5.326a.909.909 0 0 0 0-1.348L1 1" />
                </svg>
            </a>
        </div>

        <!-- 标签列表 -->
        <div class="flex flex-wrap gap-1.5">
            <span v-for="(tag, index) in tags" :key="index"
                @click="goTagArticleListPage(tag.id, tag.name)"
                class="cursor-pointer inline-block px-2.5 py-0.5 text-xs font-medium
                       text-[var(--text-secondary)] bg-[var(--bg-hover)] border border-[var(--border-base)]
                       rounded-full hover:bg-[var(--bg-active)] hover:text-[var(--color-primary)]
                       hover:border-[var(--color-primary)] transition-all duration-200">
                {{ tag.name }}
            </span>
        </div>
    </div>
</template>

<script setup>
import { getTagList } from '@/api/frontend/tag'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { setCache, getCache } from '@/composables/useCache'

const router = useRouter()

// 所有标签
const tags = ref([])
// 一次显示的标签数
const size = ref(20)

// 优先从缓存读取
const cached = getCache('sidebar_tags')
if (cached) {
    tags.value = cached
} else {
    getTagList({ size: size.value }).then((res) => {
        if (res.success) {
            tags.value = res.data
            setCache('sidebar_tags', res.data, 10 * 60 * 1000)
        }
    })
}

// 跳转标签文章列表页
const goTagArticleListPage = (id, name) => {
    router.push({ path: '/tag/article/list', query: { id, name } })
}
</script>

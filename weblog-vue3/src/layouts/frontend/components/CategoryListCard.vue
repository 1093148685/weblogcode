<template>
    <div v-if="categories && categories.length > 0"
        class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card shadow-card p-4">
        <!-- 标题行 -->
        <div class="flex items-center justify-between mb-3">
            <h2 class="flex items-center gap-1.5 text-sm font-semibold text-[var(--text-heading)] uppercase tracking-wide">
                <svg class="w-4 h-4 flex-shrink-0" viewBox="0 0 18 18" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                        d="M1 5v11a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H1Zm0 0V2a1 1 0 0 1 1-1h5.443a1 1 0 0 1 .8.4l2.7 3.6H1Z" />
                </svg>
                分类
            </h2>
            <a @click="router.push('/category/list')"
                class="cursor-pointer flex items-center justify-center w-6 h-6 rounded-full bg-[var(--bg-hover)] hover:bg-[var(--bg-active)] text-[var(--text-muted)] hover:text-[var(--color-primary)] transition-colors">
                <svg class="w-3 h-3" fill="none" viewBox="0 0 8 14">
                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="m1 13 5.7-5.326a.909.909 0 0 0 0-1.348L1 1" />
                </svg>
            </a>
        </div>

        <!-- 分类列表 -->
        <div class="flex flex-wrap gap-2">
            <a @click="goCategoryArticleListPage(category.id, category.name)"
                v-for="(category, index) in categories" :key="index"
                class="cursor-pointer inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-medium
                       text-[var(--text-secondary)] bg-[var(--bg-hover)] border border-[var(--border-base)]
                       rounded-full hover:bg-[var(--bg-active)] hover:text-[var(--color-primary)]
                       hover:border-[var(--color-primary)] transition-all duration-200">
                {{ category.name }}
                <span class="inline-flex items-center justify-center min-w-[18px] h-[18px] px-1
                             text-[10px] font-semibold text-[var(--color-primary)]
                             bg-[var(--bg-card)] border border-[var(--border-base)] rounded-full">
                    {{ category.articlesTotal }}
                </span>
            </a>
        </div>
    </div>
</template>

<script setup>
import { getCategoryList } from '@/api/frontend/category'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { setCache, getCache } from '@/composables/useCache'

const router = useRouter()

// 跳转分类文章列表页
const goCategoryArticleListPage = (id, name) => {
    router.push({ path: '/category/article/list', query: { id, name } })
}

// 所有分类
const categories = ref([])
// 一次显示的分类数
const size = ref(10)

// 优先从缓存读取
const cached = getCache('sidebar_categories')
if (cached) {
    categories.value = cached
} else {
    getCategoryList({ size: size.value }).then((res) => {
        if (res.success) {
            categories.value = res.data
            setCache('sidebar_categories', res.data, 10 * 60 * 1000)
        }
    })
}
</script>

<template>
    <Header></Header>

    <!-- 主内容区域 -->
    <main class="container max-w-screen-xl mx-auto px-4 md:px-6 py-4">
        <!-- grid 表格布局，分为 4 列 -->
        <div class="grid grid-cols-4 gap-7">
            <!-- 左边栏，占用 3 列 -->
            <div class="col-span-4 md:col-span-3 mb-3">

                <!-- 骨架屏 -->
                <div v-if="isLoading">
                    <div v-for="i in 4" :key="i" class="p-5 mb-4 border border-[var(--border-base)] rounded-lg bg-[var(--bg-card)] shadow-card">
                        <Skeleton width="140px" height="1.5rem" class="mb-4" />
                        <div class="divide-y divide-[var(--border-base)]">
                            <div v-for="j in 3" :key="j" class="flex items-center gap-3 p-3">
                                <Skeleton width="96px" height="48px" border-radius="8px" class="flex-shrink-0" />
                                <div class="flex-1 min-w-0">
                                    <Skeleton width="60%" height="1rem" class="mb-2" />
                                    <Skeleton width="100px" height="0.75rem" />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- 归档列表 -->
                <template v-else>
                    <div v-for="(archive, index) in archives" :key="index" class="p-5 mb-4 border border-[var(--border-base)] rounded-lg bg-[var(--bg-card)] shadow-card">
                        <time class="text-lg font-semibold text-[var(--text-heading)]">{{ archive.month }}</time>
                        <ol class="mt-3 divide-y divide-[var(--border-base)]">
                            <li v-for="(article, index2) in archive.articles" :key="index2">
                                <a @click="goArticleDetailPage(article.id)" class="items-center block p-3 sm:flex hover:bg-[var(--bg-hover)] hover:rounded-lg cursor-pointer">
                                    <img v-if="article.cover" class="w-24 h-12 mb-3 mr-3 rounded-lg sm:mb-0 object-cover flex-shrink-0"
                                        :src="article.cover"/>
                                    <div v-else class="w-24 h-12 mb-3 mr-3 rounded-lg sm:mb-0 bg-[var(--bg-hover)] flex items-center justify-center flex-shrink-0">
                                        <svg class="w-5 h-5 text-[var(--text-placeholder)]" fill="none" viewBox="0 0 24 24">
                                            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="m3 16 5-7 6 6.5m6.5 2.5L16 13l-4.286 6M14 10h.01M4 19h16a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1Z"/>
                                        </svg>
                                    </div>
                                    <div class="text-[var(--text-secondary)] flex-1 min-w-0">
                                        <h2 class="text-base font-normal text-[var(--text-heading)] truncate">
                                            {{ article.title }}
                                        </h2>
                                        <span class="inline-flex items-center text-xs font-normal text-[var(--text-muted)]">
                                            <svg class="inline w-2.5 h-2.5 mr-2 text-[var(--text-muted)]"
                                                aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none"
                                                viewBox="0 0 20 20">
                                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round"
                                                    stroke-width="2"
                                                    d="M5 1v3m5-3v3m5-3v3M1 7h18M5 11h10M2 3h16a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z" />
                                            </svg>
                                            {{ article.createDate }}
                                        </span>
                                    </div>
                                </a>
                            </li>
                        </ol>
                    </div>

                    <!-- 分页 -->
                    <nav aria-label="Page navigation example" class="mt-10 flex justify-center" v-if="pages > 1">
                        <ul class="flex items-center -space-x-px h-10 text-base">
                            <li>
                                <a @click="getArchives(current - 1)"
                                    class="flex items-center justify-center px-4 h-10 ml-0 leading-tight text-[var(--text-muted)] bg-[var(--bg-card)] border border-[var(--border-base)] rounded-l-lg hover:bg-[var(--bg-hover)] hover:text-[var(--text-heading)]"
                                    :class="[current > 1 ? 'cursor-pointer' : 'cursor-not-allowed']">
                                    <span class="sr-only">上一页</span>
                                    <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 6 10">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 1 1 5l4 4" />
                                    </svg>
                                </a>
                            </li>
                            <li v-for="(pageNo, index) in pages" :key="index">
                                <a @click="getArchives(pageNo)"
                                    class="flex items-center justify-center px-4 h-10 leading-tight border cursor-pointer"
                                    :class="[pageNo == current ? 'text-[var(--color-primary)] bg-[var(--bg-hover)] border-[var(--border-base)] hover:bg-[var(--bg-active)] hover:text-[var(--text-heading)]' : 'text-[var(--text-muted)] border-[var(--border-base)] bg-[var(--bg-card)] hover:bg-[var(--bg-hover)] hover:text-[var(--text-heading)]']">
                                    {{ index + 1 }}
                                </a>
                            </li>
                            <li>
                                <a @click="getArchives(current + 1)"
                                    class="flex items-center justify-center px-4 h-10 leading-tight text-[var(--text-muted)] bg-[var(--bg-card)] border border-[var(--border-base)] rounded-r-lg hover:bg-[var(--bg-hover)] hover:text-[var(--text-heading)]"
                                    :class="[current < pages ? 'cursor-pointer' : 'cursor-not-allowed']">
                                    <span class="sr-only">下一页</span>
                                    <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 6 10">
                                        <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m1 9 4-4-4-4" />
                                    </svg>
                                </a>
                            </li>
                        </ul>
                    </nav>
                </template>

            </div>

            <!-- 右边侧边栏，占用一列 -->
            <aside class="col-span-4 md:col-span-1">
                <div class="sticky top-[5.5rem]">
                    <!-- 博主信息 -->
                    <UserInfoCard></UserInfoCard>

                    <!-- 分类 -->
                    <CategoryListCard></CategoryListCard>

                    <!-- 标签 -->
                    <TagListCard></TagListCard>
                </div>
            </aside>
        </div>

    </main>

    <!-- 返回顶部 -->
    <ScrollToTopButton></ScrollToTopButton>

    <Footer></Footer>
</template>

<script setup>
import Header from '@/layouts/frontend/components/Header.vue'
import Footer from '@/layouts/frontend/components/Footer.vue'
import Skeleton from '@/components/Skeleton.vue'
import UserInfoCard from '@/layouts/frontend/components/UserInfoCard.vue'
import TagListCard from '@/layouts/frontend/components/TagListCard.vue'
import CategoryListCard from '@/layouts/frontend/components/CategoryListCard.vue'
import ScrollToTopButton from '@/layouts/frontend/components/ScrollToTopButton.vue'
import { getArchivePageList } from '@/api/frontend/archive'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { setCache, getCache } from '@/composables/useCache'

const router = useRouter()

// 文章归档
const archives = ref([])
const current = ref(1)
const size = ref(10)
const total = ref(0)
const pages = ref(0)
const isLoading = ref(true)

function getArchives(currentNo) {
    if (currentNo < 1 || (pages.value > 0 && currentNo > pages.value)) return

    const cacheKey = `archives_page_${currentNo}_${size.value}`
    const cached = getCache(cacheKey)
    if (cached) {
        archives.value = cached.data
        current.value = cached.current
        size.value = cached.size
        total.value = cached.total
        pages.value = cached.pages
        isLoading.value = false
        return
    }

    isLoading.value = true
    getArchivePageList({current: currentNo, size: size.value}).then((res) => {
        isLoading.value = false
        if (res.success) {
            archives.value = res.data
            current.value = res.current
            size.value = res.size
            total.value = res.total
            pages.value = res.pages

            setCache(cacheKey, {
                data: res.data,
                current: res.current,
                size: res.size,
                total: res.total,
                pages: res.pages
            }, 5 * 60 * 1000)
        }
    })
}
getArchives(current.value)

const goArticleDetailPage = (articleId) => {
    router.push('/article/' + articleId)
}
</script>

<template>
    <Header></Header>

    <!-- 主内容区域 -->
    <main class="container max-w-screen-xl mx-auto px-4 md:px-6 py-4">
        <!-- grid 表格布局，分为 12 列 -->
        <div class="grid grid-cols-12 gap-7">
            <div class="col-span-12 md:col-span-8 lg:col-span-9 mb-3">

                <!-- 骨架屏：和实际内容完全相同的网格 -->
                <div v-if="isLoading">
                    <!-- grid 表格布局，分为 12 列 -->
                    <div class="grid grid-cols-12 gap-7">
                        <div v-for="i in 6" :key="i" class="col-span-12 md:col-span-6 lg:col-span-4">
                            <div class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card overflow-hidden">
                                <Skeleton width="100%" height="160px" border-radius="0px" />
                                <div class="p-4">
                                    <Skeleton width="75%" height="1.25rem" class="mb-3" />
                                    <Skeleton width="100%" height="1rem" class="mb-1" />
                                    <Skeleton width="85%" height="1rem" />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- 实际知识库列表 -->
                <div v-else>
                    <!-- grid 表格布局，分为 12 列 -->
                    <div class="grid grid-cols-12 gap-7">
                        <template v-if="wikis && wikis.length > 0">
                            <div v-for="(wiki, index) in wikis" :key="index" class="col-span-12 md:col-span-6 lg:col-span-4 animate__animated animate__fadeInUp">
                                <div class="relative bg-[var(--bg-card)] h-full border border-[var(--border-base)] rounded-card shadow-card hover:shadow-card-hover hover:scale-[1.02] transition-all duration-300 overflow-hidden group">
                                    <!-- 知识库封面 -->
                                    <a @click="goWikiArticleDetailPage(wiki.id, wiki.firstArticleId)" class="wiki-cover">
                                        <img v-if="wiki.cover"
                                            class="wiki-cover__image"
                                            :src="wiki.cover"
                                            alt=""
                                            @error="wiki.cover = ''" />
                                        <div v-else class="wiki-cover__placeholder">
                                            <svg class="wiki-cover__icon" fill="none" viewBox="0 0 24 24">
                                                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="m3 16 5-7 6 6.5m6.5 2.5L16 13l-4.286 6M14 10h.01M4 19h16a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1Z"/>
                                            </svg>
                                        </div>
                                    </a>
                                    <div class="p-4">
                                        <!-- 知识库标题 -->
                                        <a @click="goWikiArticleDetailPage(wiki.id, wiki.firstArticleId)" class="cursor-pointer">
                                            <h2 class="mb-2 text-base font-semibold tracking-tight text-[var(--text-heading)] hover:text-[var(--color-primary)] transition-colors line-clamp-1">
                                                {{ wiki.title }}
                                            </h2>
                                        </a>
                                        <!-- 知识库摘要 -->
                                        <p class="text-sm font-normal text-[var(--text-secondary)] line-clamp-2 leading-relaxed">
                                            {{ wiki.summary }}
                                        </p>
                                    </div>

                                    <!-- 是否置顶 -->
                                    <div v-if="wiki.isTop"
                                        class="absolute top-0 right-0 z-10 flex items-center gap-1 px-3 py-2.5 text-xs font-medium text-amber-600 bg-amber-50 border-l border-b border-amber-100 rounded-bl-lg"
                                        title="置顶">
                                        <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20"><path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 0 0 .95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 0 0-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 0 0-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 0 0-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 0 0 .951-.69l1.07-3.292Z"/></svg>
                                        置顶
                                    </div>
                                </div>
                            </div>
                        </template>
                    </div>
                </div>

            </div>

            <!-- 右边侧边栏 -->
            <aside class="col-span-12 md:col-span-4 lg:col-span-3">
                <div class="sticky top-[5.5rem] space-y-4">
                    <UserInfoCard></UserInfoCard>
                    <CategoryListCard></CategoryListCard>
                    <TagListCard></TagListCard>
                </div>
            </aside>
        </div>
    </main>

    <ScrollToTopButton></ScrollToTopButton>
    <Footer></Footer>
</template>

<script setup>
import Header from '@/layouts/frontend/components/Header.vue'
import Skeleton from '@/components/Skeleton.vue'
import Footer from '@/layouts/frontend/components/Footer.vue'
import UserInfoCard from '@/layouts/frontend/components/UserInfoCard.vue'
import TagListCard from '@/layouts/frontend/components/TagListCard.vue'
import CategoryListCard from '@/layouts/frontend/components/CategoryListCard.vue'
import ScrollToTopButton from '@/layouts/frontend/components/ScrollToTopButton.vue'
import { getWikiList } from '@/api/frontend/wiki'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { setCache, getCache } from '@/composables/useCache'

const router = useRouter()

const wikis = ref([])
const isLoading = ref(true)

const cachedWikis = getCache('page_wikis')
if (cachedWikis) {
    wikis.value = cachedWikis
    isLoading.value = false
} else {
    getWikiList().then(res => {
        isLoading.value = false
        if (res.success) {
            wikis.value = res.data
            setCache('page_wikis', res.data, 5 * 60 * 1000)
        }
    })
}

const goWikiArticleDetailPage = (wikiId, articleId) => {
    router.push({path: '/wiki/' + wikiId, query: {articleId}})
}
</script>

<style scoped>
.wiki-cover {
    position: relative;
    display: block;
    height: 160px;
    overflow: hidden;
    cursor: pointer;
    background: var(--bg-hover);
}

.wiki-cover__image {
    display: block;
    width: 100%;
    height: 100%;
    object-fit: cover;
    transition: transform 0.5s ease;
}

.group:hover .wiki-cover__image {
    transform: scale(1.03);
}

.wiki-cover__placeholder {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
    color: var(--text-muted);
    background:
        linear-gradient(135deg, rgba(59, 130, 246, 0.2), rgba(16, 185, 129, 0.16)),
        var(--bg-hover);
}

.wiki-cover__icon {
    width: 44px;
    height: 44px;
    opacity: 0.9;
}
</style>

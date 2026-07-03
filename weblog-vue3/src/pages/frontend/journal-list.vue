<template>
    <Header></Header>

    <main class="container max-w-screen-xl mx-auto px-4 md:px-6 py-4">
        <div class="grid grid-cols-4 gap-7">
            <!-- 左边栏：日志列表 -->
            <div class="col-span-4 md:col-span-3 mb-3">
                <!-- 标题 -->
                <div class="mb-6">
                    <h1 class="text-3xl font-bold text-gray-900 dark:text-white">日志</h1>
                    <p class="mt-2 text-gray-500 dark:text-gray-400">记录生活与技术的点滴</p>
                </div>

                <!-- 骨架屏 -->
                <template v-if="isLoading">
                    <div v-for="i in 5" :key="i" class="mb-4">
                        <div class="bg-white border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700 p-5">
                            <Skeleton width="60%" height="1.5rem" />
                            <div class="mt-3">
                                <Skeleton width="120px" height="1rem" />
                            </div>
                        </div>
                    </div>
                </template>

                <!-- 日志列表 -->
                <template v-else>
                    <!-- 日期筛选提示 -->
                    <div v-if="filterDate" class="mb-4 flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
                        <span>筛选日期：<strong>{{ filterDate }}</strong></span>
                        <el-button size="small" @click="clearDateFilter" plain>清除筛选</el-button>
                    </div>

                    <div v-if="journals.length === 0" class="text-center py-20 text-gray-400 dark:text-gray-500">
                        {{ filterDate ? '该日期暂无日志' : '暂无日志' }}
                    </div>
                    <div v-for="(journal, index) in journals" :key="index" class="mb-4 slide-up-stagger">
                        <div class="bg-white border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700 p-5 hover:shadow-md transition-shadow duration-200 cursor-pointer"
                            @click="goJournalDetailPage(journal.id)">
                            <h2 class="text-xl font-bold text-gray-900 dark:text-white hover:text-sky-600 dark:hover:text-sky-400">
                                {{ journal.title }}
                            </h2>
                            <div class="mt-3 flex items-center text-sm text-gray-400 dark:text-gray-500">
                                <svg class="w-4 h-4 mr-1.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                        d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                                </svg>
                                {{ formatDate(journal.createTime) }}
                                <svg class="w-4 h-4 ml-5 mr-1.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                                </svg>
                                {{ journal.readNum }} 次阅读
                            </div>
                        </div>
                    </div>
                </template>

                <!-- 分页 -->
                <nav v-if="pages >= 1" aria-label="Page navigation" class="mt-10 flex justify-center">
                    <ul class="flex items-center -space-x-px h-10 text-base">
                        <li>
                            <a @click="goToPage(current - 1)"
                                class="flex items-center justify-center px-4 h-10 ml-0 leading-tight text-gray-500 bg-white border border-gray-300 rounded-l-lg hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white"
                                :class="[current > 1 ? 'cursor-pointer' : 'cursor-not-allowed']">
                                <span class="sr-only">上一页</span>
                                <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 6 10">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 1 1 5l4 4" />
                                </svg>
                            </a>
                        </li>
                        <li v-for="(pageNo, index) in pages" :key="index">
                            <a @click="goToPage(pageNo)"
                                class="flex items-center justify-center px-4 h-10 leading-tight border cursor-pointer dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white"
                                :class="[pageNo == current ? 'text-sky-600 bg-sky-50 border-sky-500 hover:bg-sky-100 hover:text-sky-700' : 'text-gray-500 border-gray-300 bg-white hover:bg-gray-100 hover:text-gray-700']">
                                {{ pageNo }}
                            </a>
                        </li>
                        <li>
                            <a @click="goToPage(current + 1)"
                                class="flex items-center justify-center px-4 h-10 leading-tight text-gray-500 bg-white border border-gray-300 rounded-r-lg hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white"
                                :class="[current < pages ? 'cursor-pointer' : 'cursor-not-allowed']">
                                <span class="sr-only">下一页</span>
                                <svg class="w-3 h-3" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 6 10">
                                    <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m1 9 4-4-4-4" />
                                </svg>
                            </a>
                        </li>
                    </ul>
                </nav>
            </div>

            <!-- 右边侧边栏 -->
            <aside class="col-span-4 md:col-span-1">
                <div class="sticky top-[5.5rem] space-y-4">
                    <UserInfoCard></UserInfoCard>
                    <JournalCalendarCard
                        :selected-date="filterDate"
                        @date-filter="onDateFilter"
                        @date-clear="clearDateFilter">
                    </JournalCalendarCard>
                    <CategoryListCard></CategoryListCard>
                    <TagListCard></TagListCard>
                </div>
            </aside>
        </div>
    </main>

    <Footer></Footer>
</template>

<script setup>
import { ref, computed } from 'vue'
import Header from '@/layouts/frontend/components/Header.vue'
import Footer from '@/layouts/frontend/components/Footer.vue'
import UserInfoCard from '@/layouts/frontend/components/UserInfoCard.vue'
import CategoryListCard from '@/layouts/frontend/components/CategoryListCard.vue'
import TagListCard from '@/layouts/frontend/components/TagListCard.vue'
import JournalCalendarCard from '@/components/JournalCalendarCard.vue'
import Skeleton from '@/components/Skeleton.vue'
import { getJournalList } from '@/api/frontend/journal'
import { useRouter } from 'vue-router'
import { setCache, getCache } from '@/composables/useCache'
import moment from 'moment'

const router = useRouter()

const isLoading = ref(true)
const journals = ref([])
const current = ref(1)
const total = ref(0)
const size = ref(10)
const filterDate = ref(null)

const pages = computed(() => Math.ceil(total.value / size.value))

function getJournals() {
    const cacheKey = `journals_page_${current.value}_${size.value}_${filterDate.value || ''}`

    const cached = getCache(cacheKey)
    if (cached) {
        journals.value = cached.list
        current.value = cached.pageNum
        size.value = cached.pageSize
        total.value = cached.total
        isLoading.value = false
    }

    const params = { pageNum: current.value, pageSize: size.value }
    if (filterDate.value) {
        params.date = filterDate.value
    }

    getJournalList(params)
        .then((res) => {
            if (res.success) {
                journals.value = res.data.list
                current.value = res.data.pageNum
                size.value = res.data.pageSize
                total.value = res.data.total
                setCache(cacheKey, {
                    list: res.data.list,
                    pageNum: res.data.pageNum,
                    pageSize: res.data.pageSize,
                    total: res.data.total
                }, 30 * 1000)
            }
        })
        .finally(() => isLoading.value = false)
}
getJournals()

function goToPage(pageNo) {
    if (pageNo < 1 || (pages.value > 0 && pageNo > pages.value)) return
    current.value = pageNo
    getJournals()
}

const goJournalDetailPage = (id) => {
    router.push('/journal/' + id)
}

const formatDate = (date) => {
    if (!date) return ''
    return moment(date).format('YYYY-MM-DD')
}

function onDateFilter(date) {
    filterDate.value = date
    current.value = 1
    getJournals()
}

function clearDateFilter() {
    filterDate.value = null
    current.value = 1
    getJournals()
}
</script>

<template>
    <main class="dashboard p-5">

        <!-- 统计卡片行 -->
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-5">

            <!-- 文章数 -->
            <div class="stat-card stat-card--article">
                <div class="stat-icon-wrap">
                    <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                </div>
                <div class="stat-content">
                    <div class="stat-label">文章总数</div>
                    <CountTo :value="articleTotalCount" customClass="stat-value"></CountTo>
                </div>
                <div class="stat-bg-icon">
                    <svg fill="currentColor" viewBox="0 0 24 24"><path d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
                </div>
            </div>

            <!-- 分类数 -->
            <div class="stat-card stat-card--category">
                <div class="stat-icon-wrap">
                    <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
                    </svg>
                </div>
                <div class="stat-content">
                    <div class="stat-label">分类总数</div>
                    <CountTo :value="categoryTotalCount" customClass="stat-value"></CountTo>
                </div>
                <div class="stat-bg-icon">
                    <svg fill="currentColor" viewBox="0 0 24 24"><path d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" /></svg>
                </div>
            </div>

            <!-- 标签数 -->
            <div class="stat-card stat-card--tag">
                <div class="stat-icon-wrap">
                    <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />
                    </svg>
                </div>
                <div class="stat-content">
                    <div class="stat-label">标签总数</div>
                    <CountTo :value="tagTotalCount" customClass="stat-value"></CountTo>
                </div>
                <div class="stat-bg-icon">
                    <svg fill="currentColor" viewBox="0 0 24 24"><path d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" /></svg>
                </div>
            </div>

            <!-- 浏览量 -->
            <div class="stat-card stat-card--pv">
                <div class="stat-icon-wrap">
                    <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path stroke-linecap="round" stroke-linejoin="round" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                </div>
                <div class="stat-content">
                    <div class="stat-label">总浏览量</div>
                    <CountTo :value="pvTotalCount" customClass="stat-value"></CountTo>
                </div>
                <div class="stat-bg-icon">
                    <svg fill="currentColor" viewBox="0 0 24 24"><path d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /><path d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" /></svg>
                </div>
            </div>
        </div>

        <!-- 图表第一行 -->
        <div class="grid grid-cols-1 xl:grid-cols-2 gap-4 mb-4">
            <div class="chart-card">
                <div class="chart-card__header">
                    <div class="chart-card__title">
                        <div class="chart-card__dot dot--blue"></div>
                        近半年发布热点
                    </div>
                </div>
                <ArticlePublishCalendar :value="articlePublishInfo"></ArticlePublishCalendar>
            </div>

            <div class="chart-card">
                <div class="chart-card__header">
                    <div class="chart-card__title">
                        <div class="chart-card__dot dot--violet"></div>
                        近一周 PV 访问量
                    </div>
                </div>
                <ArticlePVLineChat :value="articlePVInfo"></ArticlePVLineChat>
            </div>
        </div>

        <!-- 图表第二行 -->
        <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
            <div class="chart-card">
                <div class="chart-card__header">
                    <div class="chart-card__title">
                        <div class="chart-card__dot dot--emerald"></div>
                        分类统计
                    </div>
                </div>
                <CategoryPieChart :value="categoryStatsInfo"></CategoryPieChart>
            </div>

            <div class="chart-card">
                <div class="chart-card__header">
                    <div class="chart-card__title">
                        <div class="chart-card__dot dot--orange"></div>
                        标签统计
                    </div>
                </div>
                <TagBarChart :value="tagStatsInfo"></TagBarChart>
            </div>
        </div>

    </main>
</template>

<script setup>
defineOptions({
    name: 'AdminIndex'
})
import { ref, onMounted, computed } from 'vue'
import { getBaseStatisticsInfo, getPublishArticleStatisticsInfo, getArticlePVStatisticsInfo, getCategoryStatisticsInfo, getTagStatisticsInfo } from '@/api/admin/dashboard'
import CountTo from '@/components/CountTo.vue'
import ArticlePublishCalendar from '@/components/ArtilcePublishCalendar.vue'
import ArticlePVLineChat from '@/components/ArticlePVLineChat.vue'
import CategoryPieChart from '@/components/CategoryPieChart.vue'
import TagBarChart from '@/components/TagBarChart.vue'
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()

const currentDate = computed(() => {
    const d = new Date()
    return `${d.getFullYear()} 年 ${d.getMonth() + 1} 月 ${d.getDate()} 日`
})

const articleTotalCount = ref(0)
const categoryTotalCount = ref(0)
const tagTotalCount = ref(0)
const pvTotalCount = ref(0)
const articlePublishInfo = ref({})
const articlePVInfo = ref({})
const categoryStatsInfo = ref([])
const tagStatsInfo = ref([])

onMounted(() => {
    getBaseStatisticsInfo().then(res => {
        if (res.success) {
            articleTotalCount.value = res.data.articleTotalCount
            categoryTotalCount.value = res.data.categoryTotalCount
            tagTotalCount.value = res.data.tagTotalCount
            pvTotalCount.value = res.data.pvTotalCount
        }
    })

    getPublishArticleStatisticsInfo({}).then((res) => {
        if (res.success && res.data) {
            const map = {}
            res.data.forEach(item => {
                const dateKey = item.date + '-01'
                map[dateKey] = item.count
            })
            articlePublishInfo.value = map
        }
    })

    getArticlePVStatisticsInfo({}).then((res) => {
        if (res.success && res.data) {
            articlePVInfo.value = {
                pvDates: res.data.map(item => item.date),
                pvCounts: res.data.map(item => item.viewCount)
            }
        }
    })

    getCategoryStatisticsInfo({}).then((res) => {
        if (res.success && res.data) categoryStatsInfo.value = res.data
    })

    getTagStatisticsInfo({}).then((res) => {
        if (res.success && res.data) tagStatsInfo.value = res.data
    })
})
</script>

<style scoped>
/* ===== 统计卡片（参考 FeiTwnd 简约白卡风格） ===== */
.stat-card {
    position: relative;
    overflow: hidden;
    padding: 20px 16px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    gap: 14px;
    transition: box-shadow 0.2s ease, transform 0.2s ease;
    cursor: default;
    border: 1px solid #e4e7ed;
    background: #ffffff;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
    height: 88px;
}

.stat-card::after {
    content: '';
    position: absolute;
    left: 0;
    top: 0;
    bottom: 0;
    width: 3px;
    border-radius: 3px 0 0 3px;
}

.stat-card:hover {
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
    transform: translateY(-1px);
}

.stat-card--article::after { background: linear-gradient(180deg, #0ea5e9, #38bdf8); }
.stat-card--category::after { background: linear-gradient(180deg, #3b82f6, #60a5fa); }
.stat-card--tag::after { background: linear-gradient(180deg, #6366f1, #818cf8); }
.stat-card--pv::after { background: linear-gradient(180deg, #8b5cf6, #a78bfa); }

.stat-icon-wrap {
    flex-shrink: 0;
    width: 48px;
    height: 48px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: #f5f7fa;
}

.stat-card--article .stat-icon-wrap { background: rgba(14, 165, 233, 0.08); color: #0ea5e9; }
.stat-card--category .stat-icon-wrap { background: rgba(59, 130, 246, 0.08); color: #3b82f6; }
.stat-card--tag .stat-icon-wrap { background: rgba(99, 102, 241, 0.08); color: #6366f1; }
.stat-card--pv .stat-icon-wrap { background: rgba(139, 92, 246, 0.08); color: #8b5cf6; }

.stat-content {
    flex: 1;
    min-width: 0;
    position: relative;
    z-index: 1;
}

.stat-label {
    font-size: 12px;
    color: #909399;
    margin-bottom: 4px;
    letter-spacing: 0.02em;
    font-weight: 500;
}

:deep(.stat-value) {
    font-size: 22px;
    font-weight: 700;
    line-height: 1.2;
    letter-spacing: -0.02em;
    color: #303133;
}

.stat-card--article :deep(.stat-value) { color: #0ea5e9; }
.stat-card--category :deep(.stat-value) { color: #3b82f6; }
.stat-card--tag :deep(.stat-value) { color: #6366f1; }
.stat-card--pv :deep(.stat-value) { color: #8b5cf6; }

/* ===== 图表卡片（参考 FeiTwnd 简约白卡风格） ===== */
.chart-card {
    background: #ffffff;
    border: 1px solid #e4e7ed;
    border-radius: 8px;
    padding: 20px 24px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
    transition: box-shadow 0.2s ease, transform 0.2s ease;
    position: relative;
    overflow: hidden;
}

.chart-card:hover {
    box-shadow: 0 2px 12px rgba(0, 0, 0, 0.08);
    transform: translateY(-1px);
}

.chart-card__header {
    display: flex;
    align-items: center;
    margin-bottom: 16px;
}

.chart-card__title {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 14px;
    font-weight: 600;
    color: #303133;
    letter-spacing: 0.01em;
}

.chart-card__dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    flex-shrink: 0;
}

.dot--blue    { background: linear-gradient(135deg, #3b82f6, #60a5fa); }
.dot--violet  { background: linear-gradient(135deg, #6366f1, #818cf8); }
.dot--emerald { background: linear-gradient(135deg, #0ea5e9, #38bdf8); }
.dot--orange  { background: linear-gradient(135deg, #3b82f6, #60a5fa); }
</style>

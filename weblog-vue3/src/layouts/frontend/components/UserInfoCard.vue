<template>
    <div class="w-full py-5 px-2 mb-3 bg-white border border-gray-200 rounded-lg dark:bg-gray-800 dark:border-gray-700">
        <div class="flex flex-col items-center">
            <!-- 博主头像 -->
            <div class="relative mb-4">
                <img class="w-14 h-14 rounded-full shadow"
                :src="blogSettingsStore.blogSettings.avatar"/>    
                <span
                    class="bottom-0 left-10 absolute w-3.5 h-3.5 bg-green-400 border-2 border-white dark:border-gray-800 rounded-full"></span>
            </div>
            
            <!-- 博主昵称 -->
            <h5 class="mb-2 text-xl font-medium text-gray-900 dark:text-white">{{ blogSettingsStore.blogSettings.author }}</h5>
            <!-- 介绍语 -->
            <span class="mb-6 text-sm text-gray-500 dark:text-gray-400" data-tooltip-target="introduction-tooltip-bottom"
                data-tooltip-placement="bottom">{{ blogSettingsStore.blogSettings.introduction }}</span>
            <div id="introduction-tooltip-bottom" role="tooltip"
                class="absolute z-10 invisible inline-block px-3 py-2 text-xs font-medium text-white bg-gray-900 rounded shadow-sm opacity-0 tooltip dark:bg-gray-700">
                介绍语
                <div class="tooltip-arrow" data-popper-arrow></div>
            </div>


             <!-- 文章数量、分类数量、标签数量、总访问量 -->
             <!-- flex 布局，justify-center 水平居中，gap-5 设置 flex 内子元素的间距 -->
             <div class="flex justify-center gap-5 mb-2 dark:text-gray-400">
                <!-- flex 布局，items-center 垂直居中，flex-col 设置子元素上下排列，hover: 用于设置鼠标移动到上面的样式，字体颜色、放大效果，cursor-pointer 指定鼠标移动到上面为小手指样式 -->
                <div 
                    class="flex items-center flex-col gap-1 hover:text-sky-600 hover:scale-110 cursor-pointer">
                    <!-- 字体大小为 text-lg , 字体加粗 -->
                    <CountTo :value="statisticsInfo.articleCount" customClass="text-lg font-bold"></CountTo>
                    <!-- 字体大小为 text-sm -->
                    <div class="text-sm">文章</div>
                </div>
                <div 
                    class="flex items-center flex-col gap-1 hover:text-sky-600 hover:scale-110 cursor-pointer">
                    <CountTo :value="statisticsInfo.categoryCount" customClass="text-lg font-bold"></CountTo>
                    <div class="text-sm">分类</div>
                </div>
                <div
                    class="flex items-center flex-col gap-1 hover:text-sky-600 hover:scale-110 cursor-pointer">
                    <CountTo :value="statisticsInfo.tagCount" customClass="text-lg font-bold"></CountTo>
                    <div class="text-sm">标签</div>
                </div>
                <div class="flex items-center flex-col gap-1">
                    <CountTo :value="statisticsInfo.totalPv" customClass="text-lg font-bold"></CountTo>
                    <div class="text-sm">总访问量</div>
                </div>
            </div>

            <!-- 第三方平台主页跳转（如 GitHub 等） -->
            <div class="flex justify-center gap-2">
                <!-- GitHub -->
                <svg @click="jump(blogSettingsStore.blogSettings.githubHomepage)" v-if="blogSettingsStore.blogSettings.githubHomepage" t="1698029949662" data-tooltip-target="github-tooltip-bottom" data-tooltip-placement="bottom"
                    class="hover:scale-110 icon mt-5 w-7 h-7" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"
                    p-id="1447" width="200" height="200">
                    <path d="M512 512m-512 0a512 512 0 1 0 1024 0 512 512 0 1 0-1024 0Z" fill="#4186F5" p-id="1448"></path>
                    <path
                        d="M611.944 302.056c0-15.701 2.75-30.802 7.816-44.917a384.238 384.238 0 0 0-186.11 2.956c-74.501-50.063-93.407-71.902-107.975-39.618a136.243 136.243 0 0 0-3.961 102.287 149.515 149.515 0 0 0-39.949 104.806c0 148.743 92.139 181.875 179.961 191.61a83.898 83.898 0 0 0-25.192 51.863c-40.708 22.518-91.94 8.261-115.181-32.058a83.117 83.117 0 0 0-60.466-39.98s-38.871-0.361-2.879 23.408a102.97 102.97 0 0 1 43.912 56.906s23.398 75.279 133.531 51.863v65.913c0 10.443 13.548 42.63 102.328 42.63 71.275 0 94.913-30.385 94.913-42.987V690.485a90.052 90.052 0 0 0-26.996-72.03c83.996-9.381 173.328-40.204 179.6-176.098a164.706 164.706 0 0 1-21.129 1.365c-84.07 0-152.223-63.426-152.223-141.666z"
                        fill="#FFFFFF" p-id="1449"></path>
                    <path
                        d="M743.554 322.765a136.267 136.267 0 0 0-3.961-102.289s-32.396-10.445-107.979 39.618a385.536 385.536 0 0 0-11.853-2.956 132.623 132.623 0 0 0-7.816 44.917c0 78.24 68.152 141.667 152.222 141.667 7.171 0 14.222-0.472 21.129-1.365 0.231-5.03 0.363-10.187 0.363-15.509a149.534 149.534 0 0 0-42.105-104.083z"
                        fill="#FFFFFF" opacity=".4" p-id="1450"></path>
                </svg>
                <div id="github-tooltip-bottom" role="tooltip"
                    class="absolute z-10 invisible inline-block px-3 py-2 text-xs font-medium text-white bg-gray-900 rounded shadow-sm opacity-0 tooltip dark:bg-gray-700">
                    我的 GitHub
                    <div class="tooltip-arrow" data-popper-arrow></div>
                </div>
                <!-- Gitee -->
                <svg @click="jump(blogSettingsStore.blogSettings.giteeHomepage)" v-if="blogSettingsStore.blogSettings.giteeHomepage" t="1698030969736" data-tooltip-target="gitee-tooltip-bottom" data-tooltip-placement="bottom"
                    class="hover:scale-110 icon mt-5 w-7 h-7" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"
                    p-id="2438" width="200" height="200">
                    <path
                        d="M512 1024C229.222 1024 0 794.778 0 512S229.222 0 512 0s512 229.222 512 512-229.222 512-512 512z m259.149-568.883h-290.74a25.293 25.293 0 0 0-25.292 25.293l-0.026 63.206c0 13.952 11.315 25.293 25.267 25.293h177.024c13.978 0 25.293 11.315 25.293 25.267v12.646a75.853 75.853 0 0 1-75.853 75.853h-240.23a25.293 25.293 0 0 1-25.267-25.293V417.203a75.853 75.853 0 0 1 75.827-75.853h353.946a25.293 25.293 0 0 0 25.267-25.292l0.077-63.207a25.293 25.293 0 0 0-25.268-25.293H417.152a189.62 189.62 0 0 0-189.62 189.645V771.15c0 13.977 11.316 25.293 25.294 25.293h372.94a170.65 170.65 0 0 0 170.65-170.65V480.384a25.293 25.293 0 0 0-25.293-25.267z"
                        fill="#C71D23" p-id="2439"></path>
                </svg>
                <div id="gitee-tooltip-bottom" role="tooltip"
                    class="absolute z-10 invisible inline-block px-3 py-2 text-xs font-medium text-white bg-gray-900 rounded shadow-sm opacity-0 tooltip dark:bg-gray-700">
                    我的 Gitee
                    <div class="tooltip-arrow" data-popper-arrow></div>
                </div>
                <!-- 知乎 -->
                <svg @click="jump(blogSettingsStore.blogSettings.zhihuHomepage)" v-if="blogSettingsStore.blogSettings.zhihuHomepage" t="1698031258903" data-tooltip-target="zhihu-tooltip-bottom" data-tooltip-placement="bottom"
                    class="hover:scale-110 icon mt-5 w-7 h-7" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"
                    p-id="3419" width="200" height="200">
                    <path d="M512 512m-512 0a512 512 0 1 0 1024 0 512 512 0 1 0-1024 0Z" fill="#FFFFFF" p-id="3420"></path>
                    <path
                        d="M512 1024A512 512 0 1 1 512 0a512 512 0 0 1 0 1024zM382.08 267.52s-36.16 2.112-48.96 24.512c-12.8 22.336-54.272 137.344-54.272 137.344s13.824 6.4 37.248-10.624c23.424-17.088 30.912-46.848 30.912-46.848l42.56-2.176 1.088 121.408s-73.472-1.088-88.384 0c-14.912 1.088-23.424 40.448-23.424 40.448h111.808s-9.6 67.136-38.4 116.096c-28.672 49.024-83.008 87.296-83.008 87.296s39.424 16 77.696-6.4c38.4-22.336 66.688-120.64 66.688-120.64l89.92 110.08s8.192-52.48-1.472-67.2c-9.664-14.848-62.208-74.304-62.208-74.304l-22.976 20.224 16.32-65.088H531.2s0-38.4-19.2-40.512c-19.136-2.112-78.72 0-78.72 0V371.84h88.32s-1.024-39.424-18.048-39.424H359.68l22.4-64.96z m170.048 61.184v358.592h35.968l13.12 44.992 63.36-45.056h89.088V328.704h-201.6z"
                        fill="#0F84FD" p-id="3421"></path>
                    <path d="M594.752 368.64h117.952v277.888h-41.92l-53.376 40.256-11.648-40.32h-11.008V368.64z"
                        fill="#0F84FD" p-id="3422"></path>
                </svg>
                <div id="zhihu-tooltip-bottom" role="tooltip"
                    class="absolute z-10 invisible inline-block px-3 py-2 text-xs rounded font-medium text-white bg-gray-900 shadow-sm opacity-0 tooltip dark:bg-gray-700">
                    我的知乎
                    <div class="tooltip-arrow" data-popper-arrow></div>
                </div>
                <!-- CSDN -->
                <svg @click="jump(blogSettingsStore.blogSettings.csdnHomepage)" v-if="blogSettingsStore.blogSettings.csdnHomepage" t="1698031311586" data-tooltip-target="csdn-tooltip-bottom" data-tooltip-placement="bottom"
                    class="hover:scale-110 icon mt-5 w-7 h-7" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"
                    p-id="4386" width="200" height="200">
                    <path
                        d="M512 1024C229.222 1024 0 794.778 0 512S229.222 0 512 0s512 229.222 512 512-229.222 512-512 512z"
                        fill="#DD1700" p-id="4387"></path>
                </svg>
                <div id="csdn-tooltip-bottom" role="tooltip"
                    class="absolute z-10 invisible inline-block px-3 py-2 text-xs rounded font-medium text-white bg-gray-900 shadow-sm opacity-0 tooltip dark:bg-gray-700">
                    我的 CSDN
                    <div class="tooltip-arrow" data-popper-arrow></div>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { useBlogSettingsStore } from '@/stores/blogsettings'
import { initTooltips } from 'flowbite'
import { onMounted, ref } from 'vue'
import { getStatisticsInfo } from '@/api/frontend/statistics'
import CountTo from '@/components/CountTo.vue'
import { setCache, getCache } from '@/composables/useCache'

// 初始化 Flowbit 组件
onMounted(() => {
    initTooltips();
    blogSettingsStore.getBlogSettings();
})

// 引入博客设置信息 store
const blogSettingsStore = useBlogSettingsStore()

const jump = (url) => {
    // 在新窗口访问新的链接地址
    window.open(url, '_blank');
}

// 统计信息(文章、分类、标签数量、总访问量)
const statisticsInfo = ref({})

// 优先从缓存读取
const cachedStats = getCache('sidebar_statistics')
if (cachedStats) {
    statisticsInfo.value = cachedStats
} else {
    getStatisticsInfo().then(res => {
        if (res.success) {
            statisticsInfo.value = res.data
            setCache('sidebar_statistics', res.data, 10 * 60 * 1000) // 缓存10分钟
        }
    })
}
</script>

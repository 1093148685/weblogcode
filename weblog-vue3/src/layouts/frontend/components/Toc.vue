<template>
    <div v-if="titles && titles.length > 0"
        class="bg-[var(--bg-card)] border border-[var(--border-base)] rounded-card shadow-card p-4">
        <!-- 目录标题 -->
        <h2 class="flex items-center gap-1.5 mb-3 text-sm font-semibold text-[var(--text-heading)] uppercase tracking-wide">
            <svg class="w-4 h-4 flex-shrink-0" fill="none" viewBox="0 0 20 20">
                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8"
                    d="M5 5h1.5M5 8h1.5m-1.5 3h1.5M9 5h6m-6 3h6M9 11h6M3 3h14a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1Z"/>
            </svg>
            目录
        </h2>
        <div class="toc-wrapper" :class="[isDark ? 'dark' : '']">
            <ul class="toc">
                <!-- 二级标题 -->
                <li v-for="(h2, index) in titles" :key="index">
                    <span @click="scrollToView(h2.offsetTop)"
                        class="block cursor-pointer py-0.5 text-xs leading-5 truncate transition-colors"
                        :class="[h2.index == activeHeadingIndex
                            ? 'text-[var(--color-primary)] border-l-2 border-[var(--color-primary)] font-semibold pl-3'
                            : 'text-[var(--text-secondary)] hover:text-[var(--color-primary)] pl-4']">
                        {{ h2.text }}
                    </span>
                    <!-- 三级标题 -->
                    <ul v-if="h2.children && h2.children.length > 0">
                        <li v-for="(h3, index2) in h2.children" :key="index2">
                            <span @click="scrollToView(h3.offsetTop)"
                                class="block cursor-pointer py-0.5 text-xs leading-5 truncate transition-colors"
                                :class="[h3.index == activeHeadingIndex
                                    ? 'text-[var(--color-primary)] border-l-2 border-[var(--color-primary)] font-semibold pl-7'
                                    : 'text-[var(--text-muted)] hover:text-[var(--color-primary)] pl-8']">
                                {{ h3.text }}
                            </span>
                        </li>
                    </ul>
                </li>
            </ul>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'

// 是否是暗黑模式（通过 html.dark class 判断）
const isDark = ref(document.documentElement.classList.contains('dark'))
const darkObserver = new MutationObserver(() => {
    isDark.value = document.documentElement.classList.contains('dark')
})

// 响应式的目录数据
const titles = ref([])
onMounted(() => {
    darkObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] })
    // 通过 .artilce-content 样式来获取父级 div
    const container = document.querySelector('.article-content')

    // 使用 MutationObserver 监视 DOM 的变化
    const observer = new MutationObserver(mutationsList => {
        for (let mutation of mutationsList) {
            if (mutation.type === 'childList') {
                // 先清空目录缓存数据
                titles.value = []
                // 计算目录数据
                initTocData(container)

                // 监听所有图片的加载事件
                const images = container.querySelectorAll('img');
                images.forEach(img => {
                    img.addEventListener('load', () => {
                        // 图片加载完成后重新计算标题的 offsetTop
                        initTocData(container)
                    })
                })

                // 添加滚动事件监听
                window.addEventListener('scroll', handleContentScroll);
            }
        }
    })

    // 配置监视子节点的变化
    const config = { childList: true, subtree: true }
    // 开始观察正文 div 的内容变化
    observer.observe(container, config)
})

// 记录当前被选中的标题下标
const activeHeadingIndex = ref(-1)
// 处理滚动事件
function handleContentScroll() {
    // 当前的滚动位置
    let scrollY = window.scrollY
    // 循环目录
    titles.value.forEach(title => {
        // 获取每个标题的 offset
        let offsetTop = title.offsetTop
        // 如果当前位置大于等于标题位置，则标记选中，记录被选中标题的下标
        if (scrollY >= offsetTop) {
            activeHeadingIndex.value = title.index
        }

        // 处理3级标题, 同样的逻辑
        let children = title.children
        if (children && children.length > 0) {
            children.forEach(child => {
                let childOffsetTop = child.offsetTop
                if (scrollY >= childOffsetTop) {
                    activeHeadingIndex.value = child.index
                }
            })
        }
    })
}

// 移除滚动监听
onBeforeUnmount(() => {
    window.removeEventListener('scroll', handleContentScroll)
    darkObserver.disconnect()
})

// 滚动到指定的位置
function scrollToView(offsetTop) {
    window.scrollTo({ top: offsetTop, behavior: "smooth" });
}

// 初始化标题数据
function initTocData(container) {
    // 只提取二级、三级标题
    let levels = ['h2', 'h3']
    let headings = container.querySelectorAll(levels)

    // 存放组装后的目录标题数据
    let titlesArr = []

    // 下标
    let index = 1
    headings.forEach(heading => {
        // 标题等级， h2 -> 级别 2 ； h3 -> 级别3
        let headingLevel = parseInt(heading.tagName.substring(1))
        // 标题文字
        let headingText = heading.innerText
        // 标题的位置（距离顶部的距离）
        let offsetTop = heading.offsetTop - 95

        if (headingLevel === 2) { // 二级标题
            titlesArr.push({
                index,
                level: headingLevel,
                text: headingText,
                offsetTop,
                children: [] // 二级标题下的子标题
            })
        } else { // 三级标题
            // 父级标题
            let parentHeading = titlesArr[titlesArr.length - 1]
            // 设置父级标题的 children
            parentHeading.children.push({
                index,
                level: headingLevel,
                text: headingText,
                offsetTop
            })
        }
        // 下标 +1
        index++
    })

    // 设置数据
    titles.value = titlesArr
}
</script>

<style scoped>
::v-deep(.toc-wrapper) {
    position: relative;
    overflow-x: hidden;
    overflow-y: auto;
    max-height: 60vh;
    text-overflow: ellipsis;
    white-space: nowrap;
    scroll-behavior: smooth;
}

::v-deep(.toc:before) {
    content: " ";
    position: absolute;
    top: 0;
    bottom: 0;
    left: 0;
    z-index: -1;
    width: 2px;
    background: var(--border-base);
}

::v-deep(.dark .toc:before) {
    background: var(--border-base);
}

::v-deep(.dark .toc li span) {
    color: var(--text-secondary);
}

::v-deep(.dark .toc li .active) {
    color: var(--color-primary);
}
</style>

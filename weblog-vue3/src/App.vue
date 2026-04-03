
<template>
   <!-- 设置语言为中文 -->
   <el-config-provider :locale="locale">
      <div class="flex flex-col min-h-screen bg-[var(--bg-base)] transition-colors duration-300">
         <router-view v-slot="{ Component }">
            <keep-alive :include="cachedViews">
               <component :is="Component" />
            </keep-alive>
         </router-view>
      </div>
   </el-config-provider>
</template>

<script setup>
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
import { computed, onMounted, onUnmounted } from 'vue'
import { startPeriodicCleanup, stopPeriodicCleanup } from '@/composables/useCache'

const locale = zhCn

onMounted(() => {
    startPeriodicCleanup(5 * 60 * 1000)
})

onUnmounted(() => {
    stopPeriodicCleanup()
})

const cachedViews = computed(() => {
   const views = ['index', 'archive-list', 'category-list', 'tag-list', 'wiki-list']
   return views
})
</script>

<style>
/* 页面切换过渡动画 */
.page-fade-enter-active,
.page-fade-leave-active {
   transition: opacity 0.25s ease, transform 0.25s ease;
}
.page-fade-enter-from {
   opacity: 0;
   transform: translateY(10px);
}
.page-fade-leave-to {
   opacity: 0;
   transform: translateY(-10px);
}

/* NProgress 加载条 - Cool Gray 主色 */
#nprogress .bar {
   background: var(--color-primary) !important;
}
#nprogress .peg {
   box-shadow: 0 0 10px var(--color-primary), 0 0 5px var(--color-primary) !important;
}

/* Markdown 内容样式 */
.markdown-body {
    font-size: 14px;
    line-height: 1.7;
    color: var(--text-body);
}
.markdown-body p {
    margin-bottom: 0.6em;
}
.markdown-body p:last-child {
    margin-bottom: 0;
}
.markdown-body ul, .markdown-body ol {
    padding-left: 1.5em;
    margin-bottom: 0.5em;
}
.markdown-body code {
    background-color: var(--bg-hover);
    color: var(--text-heading);
    padding: 0.15em 0.4em;
    border-radius: 4px;
    font-size: 0.88em;
    font-family: var(--font-mono);
}
.markdown-body pre {
    background-color: var(--bg-hover);
    padding: 0.75em 1em;
    border-radius: var(--radius-sm);
    overflow-x: auto;
    margin: 0.6em 0;
    border: 1px solid var(--border-base);
}
.markdown-body pre code {
    background: none;
    padding: 0;
    border-radius: 0;
}
.markdown-body a {
    color: var(--color-primary);
    text-decoration: underline;
    text-decoration-color: var(--border-heavy);
    text-underline-offset: 2px;
}
.markdown-body a:hover {
    color: var(--color-accent);
    text-decoration-color: var(--color-accent);
}
.markdown-body blockquote {
    border-left: 3px solid var(--border-heavy);
    padding-left: 1em;
    margin: 0.8em 0;
    color: var(--text-secondary);
}

/* 公告内容限制高度 */
.announcement-content {
    max-height: 120px;
    overflow: hidden;
}

/* 公告弹窗样式 */
.announcement-dialog .el-dialog {
    border-radius: var(--radius-lg);
    overflow: hidden;
    border: 1px solid var(--border-base);
}
.announcement-dialog .el-dialog__header {
    background: var(--bg-card);
    border-bottom: 1px solid var(--border-base);
    padding: 14px 20px;
    margin-right: 0;
}
.announcement-dialog .el-dialog__body {
    padding: 16px 20px;
    background: var(--bg-card);
}
.announcement-dialog .el-dialog__footer {
    background: var(--bg-hover);
    border-top: 1px solid var(--border-base);
    padding: 10px 20px;
}
.announcement-dialog .el-dialog__headerbtn .el-dialog__close {
    color: var(--text-muted);
    transition: color var(--transition-fast);
}
.announcement-dialog .el-dialog__headerbtn:hover .el-dialog__close {
    color: var(--text-heading);
}

/* Element Plus 全局微调 */
.el-button--primary {
    border-radius: var(--radius-sm) !important;
}
.el-dialog {
    border-radius: var(--radius-lg) !important;
}
.el-pagination.is-background .el-pager li:not(.is-disabled).is-active {
    background-color: var(--color-primary) !important;
}

/* 自定义滚动条 */
::-webkit-scrollbar {
    width: 5px;
    height: 5px;
}
::-webkit-scrollbar-track {
    background: transparent;
}
::-webkit-scrollbar-thumb {
    background: var(--border-heavy);
    border-radius: 3px;
}
::-webkit-scrollbar-thumb:hover {
    background: var(--text-muted);
}
</style>

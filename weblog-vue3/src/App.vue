
<template>
   <!-- 设置语言为中文 -->
   <el-config-provider :locale="locale">
      <StarryCanvas />
      <div class="flex flex-col min-h-screen">
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
import StarryCanvas from '@/components/StarryCanvas.vue'

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

console.log(import.meta.env)
</script>

<style>
/* 自定义顶部加载 Loading 颜色 */
#nprogress .bar {
   background: #409eff!important;
}

/* 暗黑模式 body 背景色 */
.dark body {
   --tw-bg-opacity: 1;
    background-color: rgb(17 24 39 / var(--tw-bg-opacity));
}

/* Markdown 内容样式 */
.markdown-body {
    font-size: 14px;
    line-height: 1.6;
}
.markdown-body p {
    margin-bottom: 0.5em;
}
.markdown-body p:last-child {
    margin-bottom: 0;
}
.markdown-body ul, .markdown-body ol {
    padding-left: 1.5em;
    margin-bottom: 0.5em;
}
.markdown-body code {
    background-color: #f3f4f6;
    padding: 0.1em 0.3em;
    border-radius: 3px;
    font-size: 0.9em;
}
.markdown-body pre {
    background-color: #f3f4f6;
    padding: 0.5em;
    border-radius: 5px;
    overflow-x: auto;
    margin: 0.5em 0;
}
.dark .markdown-body code,
.dark .markdown-body pre {
    background-color: #374151;
}

/* 公告内容限制高度 */
.announcement-content {
    max-height: 120px;
    overflow: hidden;
}

/* 公告弹窗样式 */
.announcement-dialog .el-dialog {
    border-radius: 12px;
    overflow: hidden;
}
.announcement-dialog .el-dialog__header {
    background: linear-gradient(to right, #f8fafc, #fff);
    border-bottom: 1px solid #e5e7eb;
    padding: 12px 20px;
    margin-right: 0;
}
.dark .announcement-dialog .el-dialog__header {
    background: linear-gradient(to right, #1f2937, #374151);
    border-bottom-color: #4b5563;
}
.announcement-dialog .el-dialog__body {
    padding: 16px 20px;
    background: #fff;
}
.dark .announcement-dialog .el-dialog__body {
    background: #1f2937;
}
.announcement-dialog .el-dialog__footer {
    background: #f9fafb;
    border-top: 1px solid #e5e7eb;
    padding: 8px 20px;
}
.dark .announcement-dialog .el-dialog__footer {
    background: #374151;
    border-top-color: #4b5563;
}
.announcement-dialog .el-dialog__headerbtn {
    top: 12px;
}
.announcement-dialog .el-dialog__headerbtn .el-dialog__close {
    color: #9ca3af;
}
.announcement-dialog .el-dialog__headerbtn:hover .el-dialog__close {
    color: #4b5563;
}
.dark .announcement-dialog .el-dialog__headerbtn .el-dialog__close {
    color: #9ca3af;
}
.dark .announcement-dialog .el-dialog__headerbtn:hover .el-dialog__close {
    color: #fff;
}

/* 自定义滚动条 - 更细更美观 */
::-webkit-scrollbar {
    width: 4px;
    height: 4px;
}
::-webkit-scrollbar-track {
    background: transparent;
}
::-webkit-scrollbar-thumb {
    background: #d1d5db;
    border-radius: 4px;
}
::-webkit-scrollbar-thumb:hover {
    background: #9ca3af;
}
.dark ::-webkit-scrollbar-thumb {
    background: #4b5563;
}
.dark ::-webkit-scrollbar-thumb:hover {
    background: #6b7280;
}
</style>

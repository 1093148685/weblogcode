<template>
 <div class="ai-summary-card rounded-lg p-4 border transition-all duration-300" :class="{ loading }">
 <div class="flex items-center gap-2 mb-3">
 <svg class="w-5 h-5 text-[var(--color-primary)] flex-shrink-0" fill="none" viewBox="0 0 24 24">
 <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M7 4.75h7.586a2 2 0 0 1 1.414.586l2.664 2.664A2 2 0 0 1 19.25 9.414V18A2.25 2.25 0 0 1 17 20.25H7A2.25 2.25 0 0 1 4.75 18V7A2.25 2.25 0 0 1 7 4.75Z" />
 <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 10h6M9 14h6M9 18h4" />
 </svg>
 <span v-if="loading" class="text-xs text-[var(--color-primary)] animate-pulse">摘要生成中...</span>
 </div>

 <transition name="fade">
 <div v-if="loading" class="space-y-2">
 <div class="h-4 rounded loading-shimmer" style="width:80%"></div>
 <div class="h-4 rounded loading-shimmer" style="width:60%"></div>
 <div class="h-4 rounded loading-shimmer" style="width:70%"></div>
 </div>
 <div
 v-else-if="showMarkdown && displayContent"
 class="text-[var(--text-body)] text-sm leading-relaxed markdown-body"
 v-html="renderedContent"
 ></div>
 <div v-else class="text-[var(--text-secondary)] text-sm">
 暂时没有摘要
 </div>
 </transition>
 </div>
</template>

<script setup>
import { ref, onMounted, watch, computed } from 'vue'
import axios from '@/axios'
import { useRoute } from 'vue-router'
import { marked } from 'marked'
import { setCache, getCache } from '@/composables/useCache'

const route = useRoute()

const props = defineProps({
 articleId: {
 type: Number,
 required: false
 },
 content: {
 type: String,
 default: ''
 },
 ready: {
 type: Boolean,
 default: false
 }
})

const loading = ref(false)
const summary = ref(null)
const displayContent = ref('')
const showMarkdown = ref(false)

const renderedContent = computed(() => {
 const content = displayContent.value || ''
 return content ? marked(content) : ''
})

function resetState() {
 summary.value = null
 displayContent.value = ''
 showMarkdown.value = false
}

async function loadSummary() {
 const id = props.articleId || route.params.articleId
 if (!id) return

 const articleId = parseInt(id)
 if (isNaN(articleId)) return

 const cacheKey = `ai_summary_${articleId}`
 const cached = getCache(cacheKey)
 if (cached) {
 summary.value = cached
 displayContent.value = cached.content || ''
 showMarkdown.value = true
 loading.value = false
 return
 }

 loading.value = true

 try {
 const res = await axios.get(`/ai-summary/${articleId}`)

 if (res.success && res.data && res.data.content) {
 summary.value = res.data
 displayContent.value = res.data.content
 showMarkdown.value = true
 setCache(cacheKey, res.data,30 *60 *1000)
 } else {
 displayContent.value = ''
 showMarkdown.value = false
 }
 } catch (e) {
 console.warn('AI summary not available:', e.message || e)
 displayContent.value = ''
 showMarkdown.value = false
 } finally {
 loading.value = false
 }
}

watch(() => props.content, (newContent) => {
 if (newContent) {
 loading.value = false
 }
})

watch(() => props.ready, (isReady) => {
 if (isReady && !summary.value && !displayContent.value) {
 loadSummary()
 }
})

watch(() => route.params.articleId, (newId) => {
 if (newId) {
 resetState()
 loadSummary()
 }
})

onMounted(() => {
 loadSummary()
})

defineExpose({
 refresh: () => {
 resetState()
 loadSummary()
 }
})
</script>

<style>
.ai-summary-card {
 background: linear-gradient(135deg, #e0f2fe0%, #f0f9ff100%);
 border-color: #bae6fd;
}

.dark .ai-summary-card {
 background: linear-gradient(135deg, #0c4a6e0%, #0f172a100%);
 border-color: #1e3a5f;
}

.ai-summary-card.loading {
 animation: borderColorChange1.5s ease-in-out infinite;
}

@keyframes borderColorChange {
0%,100% {
 border-color: #38bdf8;
 box-shadow:008px rgba(56,189,248,0.3);
 }

50% {
 border-color: #0ea5e9;
 box-shadow:0020px rgba(14,165,233,0.5);
 }
}

@keyframes shimmer {
0% {
 background-position: -200%0;
 }

100% {
 background-position:200%0;
 }
}

.loading-shimmer {
 background: linear-gradient(90deg, #e0f2fe25%, #f0f9ff50%, #e0f2fe75%);
 background-size:200%100%;
 animation: shimmer1.5s infinite;
}

.dark .loading-shimmer {
 background: linear-gradient(90deg, #1e3a5f25%, #0c4a6e50%, #1e3a5f75%);
 background-size:200%100%;
}

.markdown-body {
 font-size:14px;
 line-height:1.6;
}

.markdown-body p {
 margin-bottom:0.5em;
}

.markdown-body ul,
.markdown-body ol {
 padding-left:1.5em;
 margin-bottom:0.5em;
}

.markdown-body code {
 background-color: #f3f4f6;
 padding:0.1em0.3em;
 border-radius:3px;
 font-size:0.9em;
}

.markdown-body pre {
 background-color: #f3f4f6;
 padding:0.5em;
 border-radius:5px;
 overflow-x: auto;
}

.dark .markdown-body code,
.dark .markdown-body pre {
 background-color: #374151;
}

.fade-enter-active,
.fade-leave-active {
 transition: opacity0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
 opacity:0;
}
</style>

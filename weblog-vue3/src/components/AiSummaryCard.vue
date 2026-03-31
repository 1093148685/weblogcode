<template>
    <div class="ai-summary-card rounded-lg p-4 border-2 transition-all duration-300" 
         :class="isTyping ? 'loading' : ''">
        <div class="flex items-center gap-2 mb-3">
            <svg class="w-5 h-5 text-sky-600 dark:text-sky-400" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM9.555 7.168A1 1 0 008 8v4a1 1 0 001.555.832l3-2a1 1 0 000-1.664l-3-2z" clip-rule="evenodd" />
            </svg>
            <span class="font-semibold text-sky-800 dark:text-sky-300">AI 摘要</span>
            <span v-if="isTyping" class="text-xs text-sky-500 dark:text-sky-400 animate-pulse">生成中...</span>
        </div>
        
        <transition name="fade">
            <div v-if="loading" class="space-y-2">
                <div class="h-4 rounded loading-shimmer" style="width: 80%"></div>
                <div class="h-4 rounded loading-shimmer" style="width: 60%"></div>
                <div class="h-4 rounded loading-shimmer" style="width: 70%"></div>
            </div>
            <div v-else-if="isTyping" id="ai-summary-typing" class="text-gray-700 dark:text-gray-300 text-sm leading-relaxed"></div>
            <div v-else-if="showMarkdown && displayContent" class="text-gray-700 dark:text-gray-300 text-sm leading-relaxed markdown-body" v-html="renderedContent"></div>
            <div v-else-if="hasError" class="text-gray-500 dark:text-gray-400 text-sm">
                暂时没有摘要
            </div>
            <div v-else class="text-gray-500 dark:text-gray-400 text-sm">
                暂时没有摘要
            </div>
        </transition>
    </div>
</template>

<script setup>
import { ref, onMounted, watch, computed, onBeforeUnmount, nextTick } from 'vue'
import axios from '@/axios'
import { useRoute } from 'vue-router'
import { marked } from 'marked'
import TypeIt from 'typeit'
import { setCache, getCache, clearCache } from '@/composables/useCache'

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
const isTyping = ref(false)
const hasError = ref(false)

let typeItInstance = null

const renderedContent = computed(() => {
    const content = displayContent.value || ''
    if (content) {
        return marked(content)
    }
    return ''
})

function destroyTypeIt() {
    if (typeItInstance) {
        try {
            typeItInstance.destroy()
        } catch (e) {
            // Ignore destroy errors
        }
        typeItInstance = null
    }
}

async function startTypingAnimation(content) {
    destroyTypeIt()
    showMarkdown.value = false
    isTyping.value = true
    
    await nextTick()
    
    const container = document.getElementById('ai-summary-typing')
    if (!container) {
        console.error('Typing container not found')
        isTyping.value = false
        return
    }
    
    container.innerHTML = ''
    
    typeItInstance = new TypeIt('#ai-summary-typing', {
        strings: content,
        speed: 20,
        lifeLike: true,
        cursor: true,
        cursorChar: '▊',
        cursorSpeed: 400,
        breakLines: false,
        html: false,
        waitUntilVisible: false,
        afterComplete: (instance) => {
            instance.destroy()
            displayContent.value = content
            showMarkdown.value = true
            isTyping.value = false
        }
    }).go()
}

async function loadSummary() {
    const id = props.articleId || route.params.articleId
    if (!id) return

    const articleId = parseInt(id)
    if (isNaN(articleId)) return

    // 优先从缓存读取 AI 摘要
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
    hasError.value = false

    try {
        const res = await axios.get(`/ai-summary/${articleId}`)

        if (res.success && res.data && res.data.content) {
            const content = res.data.content
            summary.value = res.data

            // 缓存 AI 摘要 30 分钟
            setCache(cacheKey, res.data, 30 * 60 * 1000)

            if (content.length < 100) {
                displayContent.value = content
                showMarkdown.value = true
            } else {
                displayContent.value = content
                await nextTick()
                startTypingAnimation(content)
            }
        } else {
            // API returned success but no summary data — not an error, just no summary
            displayContent.value = ''
            showMarkdown.value = false
        }
    } catch (e) {
        console.warn('AI summary not available:', e.message || e)
        // Treat as "no summary" rather than hard error for better UX
        hasError.value = true
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

onMounted(() => {
    loadSummary()
})

onBeforeUnmount(() => {
    destroyTypeIt()
})

watch(() => route.params.articleId, (newId) => {
    if (newId) {
        destroyTypeIt()
        summary.value = null
        displayContent.value = ''
        showMarkdown.value = false
        hasError.value = false
        loadSummary()
    }
})

defineExpose({
    refresh: () => {
        destroyTypeIt()
        summary.value = null
        displayContent.value = ''
        showMarkdown.value = false
        hasError.value = false
        loadSummary()
    }
})
</script>

<style>
.ai-summary-card {
    background: linear-gradient(135deg, #e0f2fe 0%, #f0f9ff 100%);
    border: 1px solid #bae6fd;
}
.dark .ai-summary-card {
    background: linear-gradient(135deg, #0c4a6e 0%, #0f172a 100%);
    border: 1px solid #1e3a5f;
}
.ai-summary-card.loading {
    animation: borderColorChange 1.5s ease-in-out infinite;
}
@keyframes borderColorChange {
    0%, 100% {
        border-color: #38bdf8;
        box-shadow: 0 0 8px rgba(56, 189, 248, 0.3);
    }
    50% {
        border-color: #0ea5e9;
        box-shadow: 0 0 20px rgba(14, 165, 233, 0.5);
    }
}
@keyframes shimmer {
    0% {
        background-position: -200% 0;
    }
    100% {
        background-position: 200% 0;
    }
}
.loading-shimmer {
    background: linear-gradient(90deg, #e0f2fe 25%, #f0f9ff 50%, #e0f2fe 75%);
    background-size: 200% 100%;
    animation: shimmer 1.5s infinite;
}
.dark .loading-shimmer {
    background: linear-gradient(90deg, #1e3a5f 25%, #0c4a6e 50%, #1e3a5f 75%);
    background-size: 200% 100%;
}
.markdown-body {
    font-size: 14px;
    line-height: 1.6;
}
.markdown-body p {
    margin-bottom: 0.5em;
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
}
.dark .markdown-body code,
.dark .markdown-body pre {
    background-color: #374151;
}

.fade-enter-active,
.fade-leave-active {
    transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
    opacity: 0;
}

#ai-summary-typing {
    font-family: inherit;
    color: inherit;
    white-space: pre-wrap;
    word-break: break-word;
}

#ai-summary-typing .ti-cursor {
    display: inline-block;
    color: #38bdf8;
    font-weight: bold;
}

@keyframes ti-cursor-blink {
    0%, 50% {
        opacity: 1;
    }
    51%, 100% {
        opacity: 0;
    }
}
</style>

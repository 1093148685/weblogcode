<template>
  <section class="ai-summary-card" :class="{ loading, typing: isTyping }">
    <div class="ai-summary-card__head">
      <div class="ai-summary-card__icon">
        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24">
          <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M7 4.75h7.586a2 2 0 0 1 1.414.586l2.664 2.664A2 2 0 0 1 19.25 9.414V18A2.25 2.25 0 0 1 17 20.25H7A2.25 2.25 0 0 1 4.75 18V7A2.25 2.25 0 0 1 7 4.75Z" />
          <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 10h6M9 14h6M9 18h4" />
        </svg>
      </div>
      <div>
        <div class="ai-summary-card__title">AI 摘要</div>
        <div class="ai-summary-card__desc">{{ loading ? '正在整理文章要点' : '快速了解本文重点' }}</div>
      </div>
    </div>

    <transition name="summary-fade">
      <div v-if="loading" class="summary-skeleton">
        <div class="loading-shimmer" style="width: 88%"></div>
        <div class="loading-shimmer" style="width: 72%"></div>
        <div class="loading-shimmer" style="width: 80%"></div>
      </div>
      <div v-else-if="showMarkdown && displayContent" class="markdown-body" v-html="renderedContent"></div>
      <div v-else class="summary-empty">暂时没有摘要</div>
    </transition>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { marked } from 'marked'
import axios from '@/axios'
import { setCache, getCache } from '@/composables/useCache'

const route = useRoute()

const props = defineProps({
  articleId: { type: Number, required: false },
  content: { type: String, default: '' },
  ready: { type: Boolean, default: false }
})

const loading = ref(false)
const summary = ref(null)
const displayContent = ref('')
const showMarkdown = ref(false)
const isTyping = ref(false)
let typingTimer = null

const renderedContent = computed(() => {
  const content = displayContent.value || ''
  return content ? marked(content) : ''
})

function resetState() {
  clearInterval(typingTimer)
  summary.value = null
  displayContent.value = ''
  showMarkdown.value = false
  isTyping.value = false
}

function typeContent(text, animated = true) {
  return new Promise(resolve => {
    clearInterval(typingTimer)
    if (!animated || !text) {
      displayContent.value = text || ''
      isTyping.value = false
      resolve()
      return
    }

    displayContent.value = ''
    showMarkdown.value = true
    isTyping.value = true
    let index = 0
    const step = () => {
      const chunkSize = text.length > 240 ? 4 : 2
      index = Math.min(text.length, index + chunkSize)
      displayContent.value = text.slice(0, index)
      if (index >= text.length) {
        clearInterval(typingTimer)
        isTyping.value = false
        resolve()
      }
    }
    typingTimer = setInterval(step, 18)
    step()
  })
}

async function loadSummary() {
  if (!props.ready) return

  const id = props.articleId || route.params.articleId
  const articleId = Number(id)
  if (!articleId || Number.isNaN(articleId)) return

  const cacheKey = `ai_summary_${articleId}`
  const cached = getCache(cacheKey)
  if (cached) {
    summary.value = cached
    await typeContent(cached.content || '', false)
    showMarkdown.value = true
    loading.value = false
    return
  }

  loading.value = true
  try {
    const res = await axios.get(`/ai-summary/${articleId}`)
    if (res.success && res.data?.content) {
      summary.value = res.data
      loading.value = false
      await typeContent(res.data.content)
      showMarkdown.value = true
      setCache(cacheKey, res.data, 30 * 60 * 1000)
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
  if (newContent) loading.value = false
})

watch(() => props.ready, (isReady) => {
  if (isReady && !summary.value && !displayContent.value) loadSummary()
})

watch(() => route.params.articleId, (newId) => {
  if (newId) {
    resetState()
    if (props.ready) loadSummary()
  }
})

onMounted(() => {
  if (props.ready) loadSummary()
})

onBeforeUnmount(() => clearInterval(typingTimer))

defineExpose({
  refresh: () => {
    resetState()
    loadSummary()
  }
})
</script>

<style scoped>
.ai-summary-card {
  position: relative;
  overflow: hidden;
  margin-bottom: 28px;
  padding: 16px 18px;
  border: 1px solid rgba(59, 130, 246, 0.18);
  border-radius: 16px;
  background: linear-gradient(135deg, rgba(248, 250, 252, 0.98), rgba(239, 246, 255, 0.88));
  transition: border-color 0.25s ease, box-shadow 0.25s ease;
}

.dark .ai-summary-card {
  border-color: #444c56;
  background: #2d333b;
}

.ai-summary-card.loading {
  animation: summaryPulse 1.5s ease-in-out infinite;
}

.ai-summary-card__head {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.ai-summary-card__icon {
  display: flex;
  width: 34px;
  height: 34px;
  align-items: center;
  justify-content: center;
  border-radius: 12px;
  color: #2563eb;
  background: rgba(59, 130, 246, 0.12);
}

.ai-summary-card__title {
  color: #0f172a;
  font-size: 14px;
  font-weight: 800;
}

.dark .ai-summary-card__title {
  color: #f0f6fc;
}

.dark .ai-summary-card__desc,
.dark .summary-empty,
.dark .markdown-body {
  color: #768390;
}

.ai-summary-card__desc {
  margin-top: 2px;
  color: #64748b;
  font-size: 12px;
}

.summary-skeleton {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.loading-shimmer {
  height: 12px;
  border-radius: 999px;
  background: linear-gradient(90deg, rgba(147, 197, 253, 0.24) 25%, rgba(255, 255, 255, 0.72) 50%, rgba(147, 197, 253, 0.24) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
}

.markdown-body {
  color: #334155;
  font-size: 14px;
  line-height: 1.8;
}

.markdown-body :deep(p) {
  margin-bottom: 0.65em;
}

.summary-empty {
  color: #64748b;
  font-size: 14px;
}

.ai-summary-card.typing .markdown-body::after {
  content: '';
  display: inline-block;
  width: 7px;
  height: 1.05em;
  margin-left: 2px;
  vertical-align: -2px;
  border-radius: 2px;
  background: #2563eb;
  animation: caretBlink 1s steps(2, start) infinite;
}

@keyframes summaryPulse {
  0%, 100% { border-color: rgba(59, 130, 246, 0.18); }
  50% { border-color: rgba(14, 165, 233, 0.48); }
}

@keyframes shimmer {
  0% { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}

@keyframes caretBlink {
  0%, 45% { opacity: 1; }
  46%, 100% { opacity: 0; }
}

.summary-fade-enter-active,
.summary-fade-leave-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}

.summary-fade-enter-from,
.summary-fade-leave-to {
  opacity: 0;
  transform: translateY(4px);
}
</style>

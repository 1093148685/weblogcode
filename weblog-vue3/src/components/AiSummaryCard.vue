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

const SUMMARY_MIN_LENGTH = 30
const SUMMARY_MAX_LENGTH = 230

const stripSummaryMarkdown = (text = '') => {
  return String(text)
    .replace(/```[\s\S]*?```/g, '')
    .replace(/`([^`]+)`/g, '$1')
    .replace(/!\[[^\]]*]\([^)]+\)/g, '')
    .replace(/\[([^\]]+)]\([^)]+\)/g, '$1')
    .replace(/^#{1,6}\s+/gm, '')
    .replace(/^\s*[-*+]\s+/gm, '')
    .replace(/^\s*\d+\.\s+/gm, '')
    .replace(/[*_~>#]/g, '')
    .replace(/<[^>]+>/g, '')
    .replace(/\s+/g, ' ')
    .trim()
}

const normalizeSummaryText = (text = '') => {
  const plain = stripSummaryMarkdown(text)
  if (!plain) return ''
  if (plain.length <= SUMMARY_MAX_LENGTH) return plain

  const sliced = plain.slice(0, SUMMARY_MAX_LENGTH - 1)
  const punctuationIndex = Math.max(
    sliced.lastIndexOf('。'),
    sliced.lastIndexOf('！'),
    sliced.lastIndexOf('？'),
    sliced.lastIndexOf(';'),
    sliced.lastIndexOf('；')
  )

  if (punctuationIndex >= SUMMARY_MIN_LENGTH) {
    return sliced.slice(0, punctuationIndex + 1)
  }

  return `${sliced.trim()}…`
}

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
    const normalizedText = normalizeSummaryText(text)
    if (!animated || !text) {
      displayContent.value = normalizedText
      isTyping.value = false
      resolve()
      return
    }

    displayContent.value = ''
    showMarkdown.value = true
    isTyping.value = true
    let index = 0
    const step = () => {
      const chunkSize = normalizedText.length > 160 ? 4 : 2
      index = Math.min(normalizedText.length, index + chunkSize)
      displayContent.value = normalizedText.slice(0, index)
      if (index >= normalizedText.length) {
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
  padding: 18px 20px;
  border: 1px solid #e8ded0;
  border-radius: 16px;
  background: linear-gradient(135deg, rgba(255, 253, 249, 0.98), rgba(247, 240, 230, 0.78));
  box-shadow: 0 12px 34px rgba(73, 52, 24, 0.06);
  transition: border-color 0.25s ease, box-shadow 0.25s ease, background-color 0.25s ease;
}

.dark .ai-summary-card {
  border-color: #3a332a;
  background: linear-gradient(135deg, #1b2027, #22252b);
  box-shadow: 0 16px 42px rgba(0, 0, 0, 0.2);
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
  width: 36px;
  height: 36px;
  align-items: center;
  justify-content: center;
  border: 1px solid rgba(200, 163, 109, 0.32);
  border-radius: 12px;
  color: #8f6428;
  background: rgba(247, 240, 230, 0.92);
}

.ai-summary-card__title {
  color: #201b17;
  font-size: 14px;
  font-weight: 900;
}

.ai-summary-card__desc {
  margin-top: 2px;
  color: #6b6258;
  font-size: 12px;
}

.dark .ai-summary-card__icon {
  border-color: rgba(214, 181, 116, 0.28);
  color: #d6b574;
  background: rgba(214, 181, 116, 0.12);
}

.dark .ai-summary-card__title {
  color: #f2eadf;
}

.dark .ai-summary-card__desc,
.dark .summary-empty,
.dark .markdown-body {
  color: #afa79c;
}

.summary-skeleton {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.loading-shimmer {
  height: 12px;
  border-radius: 999px;
  background: linear-gradient(90deg, rgba(200, 163, 109, 0.18) 25%, rgba(255, 253, 249, 0.82) 50%, rgba(200, 163, 109, 0.18) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
}

.markdown-body {
  color: #2a2a2a;
  font-size: 14px;
  line-height: 1.8;
}

.markdown-body :deep(p) {
  margin-bottom: 0.65em;
}

.summary-empty {
  color: #6b6258;
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
  background: #c8a36d;
  animation: caretBlink 1s steps(2, start) infinite;
}

@keyframes summaryPulse {
  0%, 100% { border-color: rgba(200, 163, 109, 0.24); }
  50% { border-color: rgba(200, 163, 109, 0.58); }
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

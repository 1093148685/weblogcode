<template>
  <Teleport to="body">
    <transition name="snippet-panel">
      <aside
        v-if="visible"
        ref="panelRef"
        class="snippet-ai-panel fixed z-[110] w-[360px] rounded-2xl border border-[#e5e7eb] bg-white p-4 shadow-[0_18px_56px_rgba(15,23,42,.16)]"
        :class="{ 'snippet-ai-panel--moved': hasCustomPosition }"
        :style="panelStyle"
      >
        <div class="snippet-ai-drag-handle mb-3 flex cursor-move select-none items-start justify-between gap-3" @pointerdown="startDrag">
          <div class="min-w-0">
            <h2 class="text-lg font-black text-[#0f172a]">问问 AI</h2>
            <p class="text-xs text-[#64748b]">基于你选中的正文片段回答</p>
          </div>
          <button class="grid h-8 w-8 shrink-0 cursor-pointer place-items-center rounded-full bg-slate-100 text-slate-500 transition hover:bg-slate-200" @pointerdown.stop @click="$emit('close')" aria-label="关闭">
            <i class="fas fa-times"></i>
          </button>
        </div>

        <label class="mb-3 block">
          <span class="mb-1 block text-xs font-bold text-[#64748b]">选择模型</span>
          <select
            v-model="selectedModelId"
            class="h-9 w-full rounded-xl border border-[#e5e7eb] bg-white px-3 text-xs font-bold text-[#334155] outline-none transition focus:border-[#2563eb] focus:ring-4 focus:ring-blue-500/10"
            :disabled="modelLoading || !modelOptions.length"
          >
            <option v-if="modelLoading" value="">正在加载模型...</option>
            <option v-else-if="!modelOptions.length" value="">默认模型</option>
            <option v-for="model in modelOptions" :key="model.id" :value="model.id">
              {{ model.name }}{{ model.provider ? ` · ${model.provider}` : '' }}
            </option>
          </select>
        </label>

        <div class="mb-3 rounded-xl border border-blue-100 bg-blue-50/70 p-3">
          <p class="mb-1 text-xs font-bold text-[#2563eb]">引用片段</p>
          <blockquote class="line-clamp-4 text-xs leading-5 text-[#334155]">{{ selectedText }}</blockquote>
        </div>

        <div class="mb-3 flex flex-wrap gap-2">
          <button v-for="item in quickQuestions" :key="item" class="rounded-full border border-[#e5e7eb] px-3 py-1.5 text-xs font-bold text-[#64748b] transition hover:border-[#2563eb] hover:text-[#2563eb]" @click="ask(item)">
            {{ item }}
          </button>
        </div>

        <textarea
          v-model="question"
          class="h-20 w-full resize-none rounded-xl border border-[#e5e7eb] bg-white p-3 text-sm leading-5 text-[#0f172a] outline-none transition focus:border-[#2563eb] focus:ring-4 focus:ring-blue-500/10"
          placeholder="围绕这段内容问点什么..."
        ></textarea>

        <button
          class="mt-3 h-9 w-full rounded-xl bg-[#2563eb] text-sm font-black text-white shadow-md shadow-blue-500/20 transition hover:bg-blue-600 disabled:cursor-not-allowed disabled:opacity-60"
          :disabled="answerLoading"
          @click="ask(question || '解释这段')"
        >
          {{ answerLoading ? 'AI 正在思考...' : '发送给 AI' }}
        </button>

        <div v-if="answer" class="mt-4 rounded-xl border border-[#e5e7eb] bg-[#f8fafc] p-3">
          <p class="mb-1 text-xs font-black text-[#0f172a]">AI 回复</p>
          <p class="whitespace-pre-wrap text-sm leading-6 text-[#334155]">{{ answer }}</p>
        </div>
      </aside>
    </transition>
  </Teleport>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { getAvailableModels } from '@/api/frontend/chat'

const props = defineProps({
  visible: { type: Boolean, default: false },
  selectedText: { type: String, default: '' }
})

defineEmits(['close'])

const fallbackModels = [
  { id: 'deepseek-chat', name: 'DeepSeek V3', provider: 'deepseek' },
  { id: 'deepseek-reasoner', name: 'DeepSeek R1', provider: 'deepseek' },
  { id: 'gpt-4o-mini', name: 'GPT-4o Mini', provider: 'openai' }
]

const panelRef = ref(null)
const position = ref({ x: 0, y: 0 })
const hasCustomPosition = ref(false)
const dragging = ref(false)
const dragOffset = ref({ x: 0, y: 0 })
const question = ref('')
const answer = ref('')
const answerLoading = ref(false)
const modelLoading = ref(false)
const modelOptions = ref([])
const selectedModelId = ref('')
const quickQuestions = ['解释这段', '总结要点', '举个例子', '有什么问题']

const isMobile = () => window.innerWidth <= 768

const panelStyle = computed(() => {
  if (hasCustomPosition.value) {
    return {
      left: `${position.value.x}px`,
      top: `${position.value.y}px`,
      right: 'auto',
      bottom: 'auto'
    }
  }
  if (isMobile()) return {}
  return {
    left: `${position.value.x}px`,
    top: `${position.value.y}px`
  }
})

watch(() => props.visible, async (value) => {
  if (!value) return
  question.value = ''
  answer.value = ''
  await loadModels()
  await nextTick()
  if (!hasCustomPosition.value) {
    const width = panelRef.value?.offsetWidth || 360
    position.value = {
      x: Math.max(16, window.innerWidth - width - 128),
      y: 92
    }
  }
})

watch(() => props.selectedText, () => {
  question.value = ''
  answer.value = ''
})

const normalizeModel = (model) => ({
  id: model.id || model.model || model.modelName || model.name,
  name: model.name || model.model || model.modelName || model.id,
  provider: model.provider || model.type || model.serviceType || ''
})

const loadModels = async () => {
  if (modelOptions.value.length || modelLoading.value) return
  modelLoading.value = true
  try {
    const res = await getAvailableModels()
    const list = Array.isArray(res?.data) ? res.data : []
    modelOptions.value = list.map(normalizeModel).filter(item => item.id)
  } catch (error) {
    console.warn('load snippet ai models failed:', error)
  } finally {
    if (!modelOptions.value.length) modelOptions.value = fallbackModels
    if (!selectedModelId.value) selectedModelId.value = modelOptions.value[0]?.id || ''
    modelLoading.value = false
  }
}

const clampPosition = (x, y) => {
  const rect = panelRef.value?.getBoundingClientRect()
  const width = rect?.width || 360
  const height = rect?.height || 420
  return {
    x: Math.min(Math.max(12, x), Math.max(12, window.innerWidth - width - 12)),
    y: Math.min(Math.max(12, y), Math.max(12, window.innerHeight - height - 12))
  }
}

const startDrag = (event) => {
  const rect = panelRef.value?.getBoundingClientRect()
  if (rect && !hasCustomPosition.value) {
    position.value = {
      x: rect.left,
      y: rect.top
    }
  }
  dragging.value = true
  hasCustomPosition.value = true
  event.currentTarget?.setPointerCapture?.(event.pointerId)
  dragOffset.value = {
    x: event.clientX - position.value.x,
    y: event.clientY - position.value.y
  }
  window.addEventListener('pointermove', onDrag)
  window.addEventListener('pointerup', stopDrag)
}

const onDrag = (event) => {
  if (!dragging.value) return
  position.value = clampPosition(
    event.clientX - dragOffset.value.x,
    event.clientY - dragOffset.value.y
  )
}

const stopDrag = () => {
  dragging.value = false
  window.removeEventListener('pointermove', onDrag)
  window.removeEventListener('pointerup', stopDrag)
}

const readStream = async (response) => {
  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let text = ''
  let pendingLine = ''

  while (true) {
    const { done, value } = await reader.read()
    if (done) break

    const chunk = pendingLine + decoder.decode(value, { stream: true })
    const lines = chunk.split('\n')
    pendingLine = lines.pop() || ''

    for (const line of lines) {
      if (!line.startsWith('data: ')) continue
      const raw = line.slice(6).trim()
      if (!raw || raw === '[DONE]') continue
      try {
        const payload = JSON.parse(raw)
        const delta = payload.content || payload.delta || payload.text || payload.choices?.[0]?.delta?.content || payload.choices?.[0]?.message?.content || ''
        if (delta) {
          text += delta
          answer.value = text
        }
      } catch {
        text += raw
        answer.value = text
      }
    }
  }

  if (!text && pendingLine.startsWith('data: ')) {
    answer.value = pendingLine.slice(6).trim()
  }
}

const ask = async (prompt) => {
  const text = String(props.selectedText || '').trim()
  const q = String(prompt || '').trim() || '解释这段'
  if (!text || answerLoading.value) return
  await loadModels()
  question.value = q
  answer.value = ''
  answerLoading.value = true

  try {
    const response = await fetch('/api/ai/chat', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        model: selectedModelId.value,
        mode: 'normal',
        messages: [
          {
            role: 'system',
            content: '你是技术博客里的阅读助手。请只基于用户选中的正文片段回答，表达清楚、简洁。'
          },
          {
            role: 'user',
            content: `选中的正文片段：\n${text}\n\n用户问题：${q}`
          }
        ]
      })
    })

    if (!response.ok || !response.body) throw new Error(`请求失败：${response.status}`)
    await readStream(response)
    if (!answer.value) answer.value = '基于选中片段，我暂时没有生成到有效回复。'
  } catch (error) {
    console.warn('snippet ai request failed:', error)
    answer.value = `基于选中片段回答：这段内容的核心是“${text.slice(0, 48)}${text.length > 48 ? '...' : ''}”。你可以继续追问细节，我会围绕这段内容解释、总结或举例。`
  } finally {
    answerLoading.value = false
  }
}

onBeforeUnmount(stopDrag)
</script>

<style scoped>
@import '@fortawesome/fontawesome-free/css/all.min.css';

.snippet-panel-enter-active,
.snippet-panel-leave-active {
  transition: opacity 0.18s ease, transform 0.18s ease;
}

.snippet-panel-enter-from,
.snippet-panel-leave-to {
  opacity: 0;
  transform: translateX(16px);
}

.snippet-ai-drag-handle {
  touch-action: none;
}

:global(html.dark) .snippet-ai-panel {
  border-color: #444c56;
  background: #2d333b;
  color: #c9d1d9;
}

:global(html.dark) .snippet-ai-panel .bg-white,
:global(html.dark) .snippet-ai-panel .bg-\[\#f8fafc\] {
  background: #22272e !important;
}

:global(html.dark) .snippet-ai-panel select,
:global(html.dark) .snippet-ai-panel textarea {
  border-color: #444c56 !important;
  background: #22272e !important;
  color: #f0f6fc !important;
}

:global(html.dark) .snippet-ai-panel .border-\[\#e5e7eb\],
:global(html.dark) .snippet-ai-panel .border-blue-100 {
  border-color: #444c56 !important;
}

:global(html.dark) .snippet-ai-panel .text-\[\#0f172a\],
:global(html.dark) .snippet-ai-panel .text-\[\#334155\] {
  color: #f0f6fc !important;
}

:global(html.dark) .snippet-ai-panel .text-\[\#64748b\] {
  color: #c9d1d9 !important;
}

:global(html.dark) .snippet-ai-panel .bg-blue-50\/70 {
  background: rgba(88, 166, 255, 0.12) !important;
}

@media (max-width: 768px) {
  .snippet-ai-panel:not(.snippet-ai-panel--moved) {
    left: 14px !important;
    right: 14px !important;
    top: auto !important;
    bottom: calc(env(safe-area-inset-bottom) + 82px) !important;
    width: auto !important;
  }

  .snippet-ai-panel {
    width: min(360px, calc(100vw - 28px)) !important;
    max-height: min(54vh, 390px);
    overflow-y: auto;
    border-radius: 18px;
    padding: 12px;
  }

  .snippet-ai-drag-handle {
    cursor: default;
  }

  .snippet-ai-panel h2 {
    font-size: 16px;
  }

  .snippet-ai-panel textarea {
    height: 62px;
    font-size: 13px;
  }
}
</style>

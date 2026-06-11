<template>
  <Teleport to="body">
    <transition name="note-slide">
      <div v-if="visible" class="pointer-events-none fixed inset-0 z-[80]">
        <section class="note-panel pointer-events-auto fixed right-[128px] top-[92px] max-h-[calc(100vh-112px)] w-[340px] overflow-y-auto rounded-2xl border border-[#e5e7eb] bg-white p-4 shadow-[0_18px_56px_rgba(15,23,42,.16)]">
          <div class="mb-3 flex items-start justify-between gap-3">
            <div class="min-w-0">
              <h2 class="text-lg font-black leading-6 text-[#0f172a]">我的笔记</h2>
              <p class="mt-0.5 line-clamp-1 text-xs text-[#64748b]">记录阅读时的想法，稍后继续整理。</p>
            </div>
            <button class="grid h-8 w-8 flex-none place-items-center rounded-full bg-slate-100 text-slate-500 transition hover:bg-slate-200" @click="$emit('close')" aria-label="关闭">
              ×
            </button>
          </div>

          <div class="mb-3 flex h-9 items-center gap-2 rounded-xl border border-[#e5e7eb] bg-[#f8fafc] px-3 text-xs text-[#94a3b8]">
            <svg class="h-3.5 w-3.5 flex-none" viewBox="0 0 24 24" fill="none" aria-hidden="true">
              <circle cx="11" cy="11" r="6" stroke="currentColor" stroke-width="2" />
              <path d="M16 16l4 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
            </svg>
            <span class="truncate">搜索笔记内容或标签...</span>
          </div>

          <div class="mb-4 grid grid-cols-4 gap-1.5 text-xs font-bold text-[#64748b]">
            <button class="h-8 rounded-full bg-blue-50 text-[#2563eb]">全部</button>
            <button class="h-8 rounded-full transition hover:bg-slate-50">高亮</button>
            <button class="h-8 rounded-full transition hover:bg-slate-50">草稿</button>
            <button class="h-8 rounded-full transition hover:bg-slate-50">私密</button>
          </div>

          <label class="mb-1.5 block text-xs font-bold text-[#64748b]">我的笔记</label>
          <textarea
            v-model="noteText"
            maxlength="1000"
            class="h-24 w-full resize-none rounded-xl border border-[#e5e7eb] bg-white p-3 text-sm leading-5 text-[#0f172a] outline-none transition focus:border-[#2563eb] focus:ring-4 focus:ring-blue-500/10"
            placeholder="写下你的想法、心得或补充..."
          ></textarea>
          <div class="mt-1.5 text-right text-[11px] text-[#94a3b8]">{{ noteText.length }} / 1000</div>

          <div class="mt-3">
            <label class="mb-2 block text-xs font-bold text-[#64748b]">颜色</label>
            <div class="flex gap-2">
              <button
                v-for="color in colors"
                :key="color"
                :style="{ backgroundColor: color }"
                :class="['h-7 w-7 rounded-lg border-2 transition', activeColor === color ? 'border-[#0f172a] scale-105' : 'border-transparent']"
                @click="activeColor = color"
              ></button>
            </div>
          </div>

          <div class="mt-4 grid grid-cols-2 gap-2">
            <button class="h-9 rounded-xl bg-[#2563eb] text-xs font-black text-white shadow-md shadow-blue-500/20 transition hover:bg-blue-600" @click="saveNote">保存笔记</button>
            <button class="h-9 rounded-xl border border-[#e5e7eb] bg-white text-xs font-black text-[#64748b] transition hover:border-[#2563eb] hover:text-[#2563eb]" @click="$emit('bookmark')">加入收藏</button>
          </div>

          <section class="mt-4 border-t border-[#e5e7eb] pt-4">
            <div class="mb-2 flex items-center justify-between">
              <h3 class="text-xs font-black text-[#0f172a]">我的笔记列表</h3>
              <span class="text-[11px] text-[#94a3b8]">最新</span>
            </div>
            <div class="max-h-40 space-y-2 overflow-y-auto pr-1">
              <article
                v-for="note in savedNotes"
                :key="note.id"
                class="rounded-xl border border-[#e5e7eb] bg-[#f8fafc] p-3"
              >
                <p class="line-clamp-2 text-xs leading-5 text-[#334155]">{{ note.content }}</p>
                <div class="mt-2 flex items-center justify-between text-[11px] text-[#94a3b8]">
                  <span>{{ note.time }}</span>
                  <button class="text-[#64748b]">...</button>
                </div>
              </article>
            </div>
          </section>
        </section>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { ref } from 'vue'
import { showMessage } from '@/composables/util'

defineProps({
  visible: { type: Boolean, default: false }
})

defineEmits(['close', 'bookmark'])

const noteText = ref('')
const activeColor = ref('#facc15')
const colors = ['#facc15', '#fb7185', '#c084fc', '#93c5fd', '#86efac']
const savedNotes = ref([
  { id: 1, content: '官方 ChatGPT Plus 订阅价格不低，轻度使用者可以考虑按量调用 API。', time: '今天 13:46' },
  { id: 2, content: '重点不是“更便宜”，而是把调用频率、场景和预算拆清楚。', time: '今天 13:40' }
])

const saveNote = () => {
  if (!noteText.value.trim()) {
    showMessage('请先写一点笔记内容', 'warning')
    return
  }

  savedNotes.value.unshift({
    id: Date.now(),
    content: noteText.value.trim(),
    time: new Date().toLocaleString()
  })
  noteText.value = ''
  showMessage('笔记已保存', 'success')
}
</script>

<style scoped>
.note-slide-enter-active,
.note-slide-leave-active {
  transition: opacity 0.2s ease;
}

.note-slide-enter-from,
.note-slide-leave-to {
  opacity: 0;
}

.note-slide-enter-active section,
.note-slide-leave-active section {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.note-slide-enter-from section,
.note-slide-leave-to section {
  transform: translateX(18px);
  opacity: 0;
}

:global(html.dark) .note-panel {
  border-color: #444c56;
  background: #2d333b;
  color: #c9d1d9;
}

:global(html.dark) .note-panel h2,
:global(html.dark) .note-panel h3,
:global(html.dark) .note-panel .text-\[\#0f172a\] {
  color: #f0f6fc !important;
}

:global(html.dark) .note-panel .text-\[\#64748b\],
:global(html.dark) .note-panel .text-\[\#94a3b8\] {
  color: #9ca3af !important;
}

:global(html.dark) .note-panel .bg-\[\#f8fafc\],
:global(html.dark) .note-panel .bg-white {
  background: #22272e !important;
}

:global(html.dark) .note-panel .border-\[\#e5e7eb\] {
  border-color: #444c56 !important;
}

@media (max-width: 1280px) {
  .note-slide-enter-active section,
  .note-slide-leave-active section {
    transition: transform 0.2s ease, opacity 0.2s ease;
  }

  .note-slide-enter-from section,
  .note-slide-leave-to section {
    transform: translateY(100%);
    opacity: 1;
  }

  .note-panel {
    left: 0;
    right: 0;
    top: auto;
    bottom: 0;
    width: auto;
    max-height: min(78vh, 640px);
    border-radius: 24px 24px 0 0;
    border-bottom: 0;
    padding-bottom: calc(18px + env(safe-area-inset-bottom));
  }
}
</style>

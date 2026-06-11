<template>
  <Teleport to="body">
    <transition name="selection-toolbar">
      <div
        v-if="visible"
        class="floating-selection-toolbar"
        :class="placement === 'bottom' ? 'is-bottom' : 'is-top'"
        :style="{ left: `${x}px`, top: `${y}px` }"
        @mousedown.prevent
      >
        <button type="button" @click="$emit('search')">
          <SearchIcon />
          <span>搜索</span>
        </button>
        <button type="button" @click="$emit('copy')">
          <CopyIcon />
          <span>复制</span>
        </button>
        <button type="button" @click="$emit('translate')">
          <TranslateIcon />
          <span>翻译</span>
        </button>
        <button type="button" @click="$emit('ask-ai')">
          <SparkIcon />
          <span>问问 AI</span>
        </button>
        <button type="button" @click="$emit('comment')">
          <CommentIcon />
          <span>片段评论</span>
        </button>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { h } from 'vue'

defineProps({
  visible: { type: Boolean, default: false },
  x: { type: Number, default: 0 },
  y: { type: Number, default: 0 },
  placement: { type: String, default: 'top' }
})

defineEmits(['search', 'copy', 'translate', 'ask-ai', 'comment'])

const iconBase = {
  viewBox: '0 0 24 24',
  fill: 'none',
  class: 'h-3.5 w-3.5',
  'aria-hidden': 'true'
}

const path = (d, extra = {}) => h('path', {
  d,
  stroke: 'currentColor',
  'stroke-width': 2,
  'stroke-linecap': 'round',
  'stroke-linejoin': 'round',
  ...extra
})

const SearchIcon = () => h('svg', iconBase, [
  h('circle', { cx: 11, cy: 11, r: 6, stroke: 'currentColor', 'stroke-width': 2 }),
  path('M16 16l4 4')
])

const CopyIcon = () => h('svg', iconBase, [
  path('M8 8h10v12H8z'),
  path('M6 16H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1')
])

const TranslateIcon = () => h('svg', iconBase, [
  path('M4 5h9M8 3v2M10.5 5c-.8 3-2.4 5.5-5.5 7.5'),
  path('M6.5 8.5c1.2 1.8 2.8 3 5 4M14 20l4-9 4 9M15.5 17h5')
])

const SparkIcon = () => h('svg', iconBase, [
  path('M12 3l1.5 4.5L18 9l-4.5 1.5L12 15l-1.5-4.5L6 9l4.5-1.5L12 3Z'),
  path('M19 15l.8 2.2L22 18l-2.2.8L19 21l-.8-2.2L16 18l2.2-.8L19 15Z')
])

const CommentIcon = () => h('svg', iconBase, [
  path('M21 11.5a8.5 8.5 0 0 1-8.5 8.5 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7A8.5 8.5 0 1 1 21 11.5Z')
])
</script>

<style scoped>
.floating-selection-toolbar {
  position: fixed;
  z-index: 120;
  display: flex;
  align-items: center;
  gap: 4px;
  border: 1px solid #e5e7eb;
  border-radius: 999px;
  background: #ffffff;
  padding: 5px;
  color: #334155;
  box-shadow: 0 14px 40px rgba(15, 23, 42, 0.18);
  transform: translate(-50%, calc(-100% - 10px));
  user-select: none;
}

.floating-selection-toolbar.is-bottom {
  transform: translate(-50%, 12px);
}

.floating-selection-toolbar button {
  display: inline-flex;
  height: 30px;
  align-items: center;
  gap: 4px;
  border-radius: 999px;
  padding: 0 9px;
  font-size: 12px;
  font-weight: 700;
  transition: background-color 0.16s ease, color 0.16s ease;
  white-space: nowrap;
}

.floating-selection-toolbar button:hover {
  background: #eff6ff;
  color: #2563eb;
}

.selection-toolbar-enter-active,
.selection-toolbar-leave-active {
  transition: opacity 0.14s ease, transform 0.14s ease;
}

.selection-toolbar-enter-from,
.selection-toolbar-leave-to {
  opacity: 0;
}

:global(html.dark) .floating-selection-toolbar {
  border-color: #444c56;
  background: #2d333b;
  color: #c9d1d9;
  box-shadow: 0 18px 50px rgba(0, 0, 0, 0.34);
}

:global(html.dark) .floating-selection-toolbar button:hover {
  background: rgba(88, 166, 255, 0.14);
  color: #58a6ff;
}
</style>

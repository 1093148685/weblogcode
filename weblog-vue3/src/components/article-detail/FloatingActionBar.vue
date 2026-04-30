<template>
  <div class="article-floating-actions fixed right-10 top-40 z-40 flex w-[72px] flex-col items-center gap-7">
    <nav class="action-group rounded-2xl border border-[#e5e7eb] bg-white p-3 shadow-[0_12px_40px_rgba(15,23,42,.08)]">
      <button :class="btnClass(activePanel === 'share')" @click="$emit('toggle-panel', 'share')" aria-label="分享">
        <ShareIcon />
        <small>分享</small>
      </button>
      <button :class="btnClass(liked)" @click="$emit('like')" aria-label="点赞">
        <LikeIcon />
        <small>点赞</small>
        <em>{{ likeCount }}</em>
      </button>
      <button :class="btnClass(bookmarked)" @click="$emit('bookmark')" aria-label="收藏">
        <StarIcon />
        <small>收藏</small>
      </button>
      <button :class="btnClass(activePanel === 'notes')" @click="$emit('toggle-panel', 'notes')" aria-label="笔记">
        <NoteIcon />
        <small>笔记</small>
      </button>
    </nav>

    <div class="utility-group flex flex-col items-center gap-4">
      <button class="utility-button" @click="$emit('comment')" aria-label="评论">
        <CommentIcon />
        <small>评论</small>
      </button>
      <button class="utility-button" @click="$emit('top')" aria-label="回到顶部">
        <ArrowUpIcon />
      </button>
    </div>
  </div>
</template>

<script setup>
import { h } from 'vue'

defineProps({
  activePanel: { type: String, default: 'share' },
  liked: { type: Boolean, default: false },
  bookmarked: { type: Boolean, default: false },
  likeCount: { type: Number, default: 23 }
})

defineEmits(['toggle-panel', 'like', 'bookmark', 'top', 'comment'])

const btnClass = (active) => [
  'mb-2 flex h-[62px] w-[52px] flex-col items-center justify-center gap-1 rounded-xl text-xs transition last:mb-0',
  active ? 'is-active bg-slate-100 text-[#334155]' : 'text-[#64748b] hover:bg-slate-50 hover:text-[#334155]'
]

const iconBase = {
  viewBox: '0 0 24 24',
  fill: 'none',
  class: 'h-5 w-5',
  'aria-hidden': 'true'
}

const path = (d, extra = {}) => h('path', { d, stroke: 'currentColor', 'stroke-width': 2, 'stroke-linecap': 'round', 'stroke-linejoin': 'round', ...extra })

const ShareIcon = () => h('svg', iconBase, [
  path('M18 8a3 3 0 1 0-2.83-4'),
  path('M6 14a3 3 0 1 0 2.83 4'),
  path('M8.6 13.1l6.8 3.8M15.4 7.1L8.6 10.9')
])

const LikeIcon = () => h('svg', iconBase, [
  path('M7 11v9H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h3Z'),
  path('M7 11l4.2-7.4A2 2 0 0 1 15 4.6V9h4.2a2 2 0 0 1 2 2.3l-1 6A3 3 0 0 1 17.2 20H7')
])

const StarIcon = () => h('svg', iconBase, [
  path('M12 3.5l2.7 5.47 6.03.88-4.36 4.25 1.03 6-5.4-2.84-5.4 2.84 1.03-6-4.36-4.25 6.03-.88L12 3.5Z')
])

const NoteIcon = () => h('svg', iconBase, [
  path('M6 4h9l3 3v13H6z'),
  path('M15 4v4h4M9 12h6M9 16h4')
])

const CommentIcon = () => h('svg', iconBase, [
  path('M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6A8.38 8.38 0 0 1 12.5 3H13a8.48 8.48 0 0 1 8 8v.5Z')
])

const ArrowUpIcon = () => h('svg', { ...iconBase, class: 'h-5 w-5' }, [
  path('M12 19V5M5 12l7-7 7 7')
])
</script>

<style scoped>
.utility-button {
  display: grid;
  width: 52px;
  min-height: 52px;
  place-items: center;
  border: 1px solid #e5e7eb;
  border-radius: 999px;
  background: #ffffff;
  color: #64748b;
  box-shadow: 0 12px 40px rgba(15, 23, 42, 0.08);
  transition: background-color 0.16s ease, color 0.16s ease, border-color 0.16s ease, transform 0.16s ease;
}

.utility-button:hover {
  color: #334155;
  transform: translateY(-1px);
}

.utility-button small {
  margin-top: -8px;
  font-size: 11px;
  font-weight: 700;
}

:global(html.dark) .article-floating-actions nav,
:global(html.dark) .article-floating-actions .utility-button {
  border-color: #444c56;
  background: #2d333b;
  color: #c9d1d9;
}

:global(html.dark) .article-floating-actions button:hover {
  background: #373e47;
  color: #58a6ff;
}

:global(html.dark) .article-floating-actions .is-active {
  background: rgba(56, 139, 253, 0.14) !important;
  color: #58a6ff !important;
}
</style>

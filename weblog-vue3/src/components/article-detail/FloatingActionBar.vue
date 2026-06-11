<template>
  <div v-if="isDesktop" class="article-floating-actions" :style="rootStyle">
    <div class="floating-actions-stack" :style="stackStyle">
      <transition name="reading-menu">
        <section v-if="moreVisible" class="reading-menu" @click.stop>
          <header>阅读设置</header>

          <button class="menu-row" type="button" @click="$emit('template')">
            <span>正文模板</span>
            <ChevronRightIcon />
          </button>

          <div class="menu-row menu-row--static">
            <span>字号</span>
            <div class="font-tools">
              <button type="button" aria-label="减小字号" @click="$emit('font-decrease')">A-</button>
              <button type="button" aria-label="增大字号" @click="$emit('font-increase')">A+</button>
            </div>
          </div>

          <div class="menu-row menu-row--static">
            <span>行距</span>
            <strong>舒适</strong>
          </div>

          <div class="menu-divider"></div>

          <button class="menu-row menu-row--copy" type="button" @click="copyLink">
            <LinkIcon />
            <span>复制链接</span>
          </button>
        </section>
      </transition>

      <nav class="action-rail" :style="railStyle" aria-label="文章工具栏">
        <button
          :class="['rail-icon', activePanel === 'share' ? 'is-active' : '']"
          :style="iconButtonStyle(activePanel === 'share')"
          type="button"
          aria-label="分享"
          title="分享"
          @click.stop="$emit('toggle-panel', 'share')"
        >
          <ShareIcon />
        </button>

        <button
          :class="['rail-icon', activePanel === 'notes' ? 'is-active' : '']"
          :style="iconButtonStyle(activePanel === 'notes')"
          type="button"
          aria-label="笔记"
          title="笔记"
          @click.stop="$emit('toggle-panel', 'notes')"
        >
          <NoteIcon />
        </button>

        <button class="rail-icon" :style="iconButtonStyle(false)" type="button" aria-label="评论" title="评论" @click.stop="$emit('comment')">
          <CommentIcon />
        </button>

        <button class="rail-icon" :style="iconButtonStyle(false)" type="button" aria-label="回到顶部" title="回到顶部" @click.stop="$emit('top')">
          <ArrowUpIcon />
        </button>

        <button
          :class="['rail-icon rail-icon--more', moreVisible ? 'is-active' : '']"
          :style="iconButtonStyle(moreVisible)"
          type="button"
          aria-label="更多功能"
          title="更多功能"
          @click.stop="moreVisible = !moreVisible"
        >
          <MoreIcon />
        </button>
      </nav>
    </div>
  </div>
</template>

<script setup>
import { h, onBeforeUnmount, onMounted, ref } from 'vue'

defineProps({
  activePanel: { type: String, default: '' }
})

const emit = defineEmits(['toggle-panel', 'top', 'comment', 'copy-link', 'font-decrease', 'font-increase', 'template'])

const moreVisible = ref(false)
const isDesktop = ref(true)

const rootStyle = {
  position: 'fixed',
  right: '40px',
  top: '160px',
  zIndex: 40,
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'flex-end',
  gap: '12px'
}

const stackStyle = {
  position: 'relative',
  display: 'flex',
  alignItems: 'center'
}

const railStyle = {
  display: 'flex',
  width: '56px',
  flexDirection: 'column',
  alignItems: 'center',
  gap: '12px',
  border: '1px solid #e8ded0',
  borderRadius: '24px',
  background: '#fffdf9',
  padding: '12px 8px',
  boxShadow: '0 18px 48px rgba(73, 52, 24, 0.12)'
}

const iconButtonStyle = (active) => ({
  display: 'inline-grid',
  width: '38px',
  height: '38px',
  placeItems: 'center',
  borderRadius: '14px',
  color: active ? '#8f6428' : '#72665a',
  background: active ? '#f7f0e6' : 'transparent'
})

const syncViewport = () => {
  isDesktop.value = typeof window === 'undefined' || window.innerWidth >= 1024
  if (!isDesktop.value) {
    moreVisible.value = false
  }
}

const closeMore = () => {
  moreVisible.value = false
}

const copyLink = () => {
  emit('copy-link')
  closeMore()
}

onMounted(() => {
  syncViewport()
  window.addEventListener('resize', syncViewport)
  document.addEventListener('click', closeMore)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', syncViewport)
  document.removeEventListener('click', closeMore)
})

const iconBase = {
  viewBox: '0 0 24 24',
  fill: 'none',
  class: 'h-5 w-5',
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

const ShareIcon = () => h('svg', iconBase, [
  path('M18 8a3 3 0 1 0-2.83-4'),
  path('M6 14a3 3 0 1 0 2.83 4'),
  path('M8.6 13.1l6.8 3.8M15.4 7.1L8.6 10.9')
])

const NoteIcon = () => h('svg', iconBase, [
  path('M6 4h9l3 3v13H6z'),
  path('M15 4v4h4M9 12h6M9 16h4')
])

const CommentIcon = () => h('svg', iconBase, [
  path('M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6A8.38 8.38 0 0 1 12.5 3H13a8.48 8.48 0 0 1 8 8v.5Z')
])

const ArrowUpIcon = () => h('svg', iconBase, [
  path('M12 19V5M5 12l7-7 7 7')
])

const MoreIcon = () => h('svg', iconBase, [
  h('circle', { cx: 5, cy: 12, r: 1.4, fill: 'currentColor' }),
  h('circle', { cx: 12, cy: 12, r: 1.4, fill: 'currentColor' }),
  h('circle', { cx: 19, cy: 12, r: 1.4, fill: 'currentColor' })
])

const ChevronRightIcon = () => h('svg', { ...iconBase, class: 'h-4 w-4' }, [
  path('M9 18l6-6-6-6')
])

const LinkIcon = () => h('svg', { ...iconBase, class: 'h-4 w-4' }, [
  path('M10 13a5 5 0 0 0 7.07 0l2.12-2.12a5 5 0 0 0-7.07-7.07L10.9 5'),
  path('M14 11a5 5 0 0 0-7.07 0L4.8 13.12a5 5 0 0 0 7.07 7.07L13.1 19')
])
</script>

<style scoped>
.action-rail {
  display: flex;
  width: 54px;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  border: 1px solid #e8ded0;
  border-radius: 22px;
  background: #fffdf9;
  padding: 12px 8px;
  box-shadow: 0 18px 48px rgba(73, 52, 24, 0.12);
}

.floating-actions-stack {
  position: relative;
  display: flex;
  align-items: center;
}

.rail-icon {
  display: inline-grid;
  width: 38px;
  height: 38px;
  place-items: center;
  border-radius: 14px;
  color: #72665a;
  transition: background-color 0.16s ease, color 0.16s ease, transform 0.16s ease;
}

.rail-icon:hover,
.rail-icon.is-active {
  background: #f7f0e6 !important;
  color: #8f6428 !important;
  transform: translateY(-1px);
}

.reading-menu {
  position: absolute;
  right: 70px;
  top: 50%;
  width: 188px;
  transform: translateY(-50%);
  border: 1px solid #e8ded0;
  border-radius: 16px;
  background: #fffdf9;
  padding: 12px;
  color: #2a2a2a;
  box-shadow: 0 20px 56px rgba(73, 52, 24, 0.16);
}

.reading-menu::after {
  position: absolute;
  top: 50%;
  right: -7px;
  width: 12px;
  height: 12px;
  content: "";
  transform: translateY(-50%) rotate(45deg);
  border-top: 1px solid #e8ded0;
  border-right: 1px solid #e8ded0;
  background: #fffdf9;
}

.reading-menu header {
  margin-bottom: 8px;
  padding: 2px 4px 8px;
  border-bottom: 1px solid #efe3d2;
  color: #1f1a16;
  font-size: 13px;
  font-weight: 900;
}

.menu-row {
  display: flex;
  min-height: 38px;
  width: 100%;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  border-radius: 10px;
  padding: 0 8px;
  color: #5f5145;
  font-size: 13px;
  font-weight: 700;
  transition: background-color 0.16s ease, color 0.16s ease;
}

.menu-row:not(.menu-row--static):hover {
  background: #f7f0e6;
  color: #8f6428;
}

.menu-row--static {
  cursor: default;
}

.menu-row--static strong {
  color: #8f6428;
  font-size: 12px;
}

.font-tools {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.font-tools button {
  display: inline-grid;
  min-width: 30px;
  height: 28px;
  place-items: center;
  border-radius: 8px;
  color: #5f5145;
  font-size: 12px;
  font-weight: 900;
  transition: background-color 0.16s ease, color 0.16s ease;
}

.font-tools button:hover {
  background: #efe3d2;
  color: #8f6428;
}

.menu-divider {
  margin: 8px 4px;
  height: 1px;
  background: #efe3d2;
}

.menu-row--copy {
  justify-content: flex-start;
}

.reading-menu-enter-active,
.reading-menu-leave-active {
  transition: opacity 0.16s ease, transform 0.16s ease;
}

.reading-menu-enter-from,
.reading-menu-leave-to {
  opacity: 0;
  transform: translate(8px, -50%);
}

:global(html.dark) .article-floating-actions .action-rail,
:global(html.dark) .article-floating-actions .reading-menu {
  border-color: #3a332a;
  background: #1b2027;
  color: #afa79c;
}

:global(html.dark) .article-floating-actions .action-rail {
  background: #1b2027 !important;
}

:global(html.dark) .article-floating-actions .reading-menu::after {
  border-color: #3a332a;
  background: #1b2027;
}

:global(html.dark) .article-floating-actions .reading-menu header,
:global(html.dark) .article-floating-actions .menu-row {
  border-color: #3a332a;
  color: #e8e2d8;
}

:global(html.dark) .article-floating-actions .menu-divider {
  background: #3a332a;
}

:global(html.dark) .article-floating-actions .rail-icon {
  color: #afa79c !important;
}

:global(html.dark) .article-floating-actions .menu-row:not(.menu-row--static):hover,
:global(html.dark) .article-floating-actions .font-tools button:hover,
:global(html.dark) .article-floating-actions .rail-icon:hover,
:global(html.dark) .article-floating-actions .rail-icon.is-active {
  background: rgba(214, 181, 116, 0.12) !important;
  color: #d6b574 !important;
}
</style>

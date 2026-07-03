<template>
  <Teleport to="body">
    <transition name="share-slide">
      <aside
        v-if="visible"
        class="share-drawer fixed right-[128px] top-24 z-[80] max-h-[calc(100vh-112px)] w-[420px] overflow-y-auto rounded-2xl border border-[#e5e7eb] bg-white p-5 shadow-[0_24px_70px_rgba(15,23,42,.18)]"
      >
        <div class="mb-5 flex items-center justify-between">
          <h2 class="text-lg font-black text-[#0f172a]">分享文章</h2>
          <button
            class="grid h-8 w-8 place-items-center rounded-full bg-slate-100 text-slate-500 transition hover:bg-slate-200"
            aria-label="关闭"
            @click="$emit('close')"
          >
            ×
          </button>
        </div>

        <section class="grid grid-cols-[112px_1fr] gap-3 rounded-2xl border border-[#e5e7eb] p-3">
          <div class="h-[86px] overflow-hidden rounded-xl bg-[#0f172a]">
            <img
              v-if="article.cover && !coverError"
              :src="article.cover"
              class="h-full w-full object-cover"
              alt=""
              @error="coverError = true"
            >
            <div v-else class="grid h-full place-items-center bg-gradient-to-br from-[#020617] via-[#0f3d8a] to-[#2563eb] text-center text-xs font-black leading-5 text-white">
              技术博客<br>ARTICLE
            </div>
          </div>
          <div class="min-w-0">
            <h3 class="line-clamp-2 text-sm font-black leading-5 text-[#0f172a]">{{ article.title || '文章标题' }}</h3>
            <p class="mt-2 text-xs text-[#64748b]">作者：{{ article.author || '博主' }} · {{ dateText }}</p>
            <p class="mt-2 line-clamp-2 text-xs leading-5 text-[#64748b]">{{ excerpt || '这篇文章正在等待更多内容摘要。' }}</p>
          </div>
        </section>

        <section class="mt-5">
          <div class="mb-2 flex items-center gap-1 text-sm font-bold text-[#0f172a]">
            复制链接
            <svg class="h-3.5 w-3.5 text-emerald-500" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm3.7-9.7a1 1 0 0 0-1.4-1.4L9 10.17 7.7 8.9a1 1 0 0 0-1.4 1.42l2 2a1 1 0 0 0 1.4 0l4-4Z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="flex gap-2 rounded-xl border border-[#e5e7eb] bg-[#f8fafc] p-2">
            <input :value="url" readonly class="min-w-0 flex-1 bg-transparent px-2 text-sm text-[#64748b] outline-none">
            <button class="rounded-lg bg-blue-50 px-3 text-sm font-bold text-[#2563eb] transition hover:bg-blue-100" @click="$emit('copy')">复制</button>
          </div>
        </section>
      </aside>
    </transition>
  </Teleport>
</template>

<script setup>
import { computed, ref, watch } from 'vue'

const props = defineProps({
  article: { type: Object, default: () => ({}) },
  url: { type: String, default: '' },
  excerpt: { type: String, default: '' },
  visible: { type: Boolean, default: false }
})

defineEmits(['close', 'copy'])

const coverError = ref(false)

watch(() => props.article.cover, () => {
  coverError.value = false
})

const dateText = computed(() => String(props.article.createTime || '').replace('T', ' ').slice(0, 10) || '2026-04-27')
</script>

<style scoped>
.share-slide-enter-active,
.share-slide-leave-active {
  transition: opacity 0.2s ease;
}

.share-slide-enter-from,
.share-slide-leave-to {
  opacity: 0;
}

.share-slide-enter-active,
.share-slide-leave-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.share-slide-enter-from,
.share-slide-leave-to {
  transform: translateX(18px);
}

.share-drawer {
  scrollbar-width: thin;
  scrollbar-color: transparent transparent;
}

.share-drawer:hover {
  scrollbar-color: #cbd5e1 transparent;
}

.share-drawer::-webkit-scrollbar {
  width: 6px;
}

.share-drawer::-webkit-scrollbar-thumb {
  background: transparent;
  border-radius: 999px;
}

.share-drawer:hover::-webkit-scrollbar-thumb {
  background: #cbd5e1;
}

:global(html.dark) .share-drawer {
  border-color: #444c56;
  background: #2d333b;
  color: #adbac7;
}

:global(html.dark) .share-drawer h2,
:global(html.dark) .share-drawer h3 {
  color: #f0f6fc;
}

:global(html.dark) .share-drawer section,
:global(html.dark) .share-drawer .border-\[\#e5e7eb\] {
  border-color: #444c56;
}

:global(html.dark) .share-drawer .bg-\[\#f8fafc\] {
  background: #22272e;
}

:global(html.dark) .share-drawer input,
:global(html.dark) .share-drawer p {
  color: #768390;
}

@media (max-width: 1280px) {
  .share-drawer {
    left: 16px;
    right: 16px;
    top: 88px;
    width: auto;
    max-height: calc(100vh - 112px);
  }
}
</style>

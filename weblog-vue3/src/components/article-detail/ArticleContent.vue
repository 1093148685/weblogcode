<template>
  <article class="article-content-shell">
    <div class="mb-5 flex flex-wrap gap-2">
      <button
        v-for="tag in normalizedTags"
        :key="tag.id || tag.name"
        class="rounded-full bg-blue-50 px-3 py-1 text-sm font-bold text-[#2563eb] transition hover:bg-blue-100"
        @click="$emit('go-tag', tag.id, tag.name)"
      >
        # {{ tag.name }}
      </button>
    </div>

    <h1 class="mb-5 text-[38px] font-black leading-tight tracking-tight text-[#0f172a]">{{ article.title }}</h1>

    <div class="mb-8 flex flex-wrap items-center gap-6 text-sm text-[#64748b]">
      <span class="inline-flex items-center gap-2">
        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <rect x="4" y="5" width="16" height="15" rx="2" stroke="currentColor" stroke-width="2" />
          <path d="M8 3v4M16 3v4M4 10h16" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
        </svg>
        {{ dateText }}
      </span>

      <button class="hover:text-[#2563eb]" @click="$emit('go-category', article.categoryId, article.categoryName)">
        <span class="inline-flex items-center gap-2">
          <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path d="M3 7.5A2.5 2.5 0 0 1 5.5 5H10l2 2h6.5A2.5 2.5 0 0 1 21 9.5v7A2.5 2.5 0 0 1 18.5 19h-13A2.5 2.5 0 0 1 3 16.5v-9Z" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
          </svg>
          {{ article.categoryName || '未分类' }}
        </span>
      </button>

      <span class="inline-flex items-center gap-2">
        <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" stroke="currentColor" stroke-width="2" />
          <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2" />
        </svg>
        {{ article.readNum || 15 }}
      </span>
    </div>

    <slot />

    <div class="article-prose markdown-body markdown-dark mt-8" v-html="cleanContent"></div>
  </article>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  article: { type: Object, default: () => ({}) },
  renderedContent: { type: String, default: '' }
})

defineEmits(['go-tag', 'go-category'])

const normalizedTags = computed(() => {
  const tags = props.article.tags || []
  return tags.length ? tags : [{ name: 'python' }]
})

const dateText = computed(() => String(props.article.createTime || '').replace('T', ' ').slice(0, 19))

const cleanContent = computed(() => {
  const title = String(props.article.title || '').trim()
  if (!title) return props.renderedContent

  const escaped = title.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  const firstTitleReg = new RegExp(`^\\s*<h1[^>]*>\\s*${escaped}\\s*</h1>`, 'i')
  return props.renderedContent.replace(firstTitleReg, '')
})
</script>

<style scoped>
.article-content-shell {
  width: 100%;
}

.article-prose {
  color: #24292f;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans SC", "PingFang SC", "Microsoft YaHei", sans-serif;
  font-size: 16px;
  line-height: 1.65;
  word-wrap: break-word;
}

.article-prose :deep(h1),
.article-prose :deep(h2) {
  margin: 24px 0 16px;
  padding-bottom: 0.3em;
  border-bottom: 1px solid #d8dee4;
  color: #24292f;
  font-weight: 600;
  line-height: 1.25;
}

.article-prose :deep(h1) {
  font-size: 2em;
}

.article-prose :deep(h2) {
  font-size: 1.5em;
}

.article-prose :deep(h3) {
  margin: 24px 0 16px;
  color: #24292f;
  font-size: 1.25em;
  font-weight: 600;
  line-height: 1.25;
}

.article-prose :deep(h4) {
  margin: 24px 0 16px;
  color: #24292f;
  font-size: 1em;
  font-weight: 600;
  line-height: 1.25;
}

.article-prose :deep(h5),
.article-prose :deep(h6) {
  margin: 24px 0 16px;
  color: #24292f;
  font-size: 0.875em;
  font-weight: 600;
  line-height: 1.25;
}

.article-prose :deep(p) {
  margin: 0 0 16px;
}

.article-prose :deep(a) {
  color: #0969da;
  text-decoration: underline;
  text-underline-offset: 2px;
}

.article-prose :deep(ul),
.article-prose :deep(ol) {
  margin: 0 0 16px;
  padding-left: 2em;
}

.article-prose :deep(li) {
  margin: 0.25em 0;
}

.article-prose :deep(li > p) {
  margin-top: 16px;
}

.article-prose :deep(table) {
  display: block;
  width: 100%;
  max-width: 100%;
  margin: 0 0 16px;
  border-collapse: collapse;
  overflow: hidden;
  border: 0;
  font-size: 16px;
}

.article-prose :deep(thead) {
  background: transparent;
}

.article-prose :deep(th),
.article-prose :deep(td) {
  border: 1px solid #d0d7de;
  padding: 6px 13px;
  text-align: left;
  vertical-align: top;
}

.article-prose :deep(th) {
  color: #24292f;
  font-weight: 600;
}

.article-prose :deep(tr) {
  background-color: #ffffff;
  border-top: 1px solid #d8dee4;
}

.article-prose :deep(tr:nth-child(2n)) {
  background-color: #f6f8fa;
}

.article-prose :deep(td code),
.article-prose :deep(th code) {
  white-space: nowrap;
}

.article-prose :deep(blockquote) {
  margin: 0 0 16px;
  padding: 0 1em;
  border-left: 0.25em solid #d0d7de;
  color: #57606a;
}

.article-prose :deep(hr) {
  height: 0.25em;
  margin: 24px 0;
  padding: 0;
  border: 0;
  background-color: #d8dee4;
}

.article-prose :deep(code:not(pre code)) {
  border-radius: 6px;
  background: rgba(175, 184, 193, 0.26);
  padding: 0.2em 0.4em;
  color: #24292f;
  font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", Menlo, monospace;
  font-size: 85%;
}

.article-prose :deep(pre) {
  position: relative;
  max-width: 100%;
  max-height: 640px;
  margin: 20px 0;
  overflow-x: auto;
  overflow-y: auto;
  border: 1px solid #d0d7de;
  border-radius: 10px;
  background: #f6f8fa;
  padding: 16px 56px 16px 16px;
  box-shadow: none;
  scrollbar-width: thin;
  scrollbar-color: #c9d1d9 transparent;
}

.article-prose :deep(pre:hover .code-copy-btn) {
  opacity: 1;
}

.article-prose :deep(.code-copy-btn) {
  position: absolute;
  top: 8px;
  right: 8px;
  z-index: 2;
  height: 28px;
  border: 1px solid #d0d7de;
  border-radius: 6px;
  background: #ffffff;
  padding: 0 10px;
  color: #57606a;
  font-size: 12px;
  font-weight: 600;
  line-height: 26px;
  opacity: 0;
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.08);
  transition: opacity 0.16s ease, border-color 0.16s ease, color 0.16s ease, background-color 0.16s ease;
}

.article-prose :deep(.code-copy-btn:hover),
.article-prose :deep(.code-copy-btn.copied) {
  border-color: #0969da;
  color: #0969da;
}

.article-prose :deep(pre::before) {
  content: none;
}

.article-prose :deep(pre code) {
  display: block;
  min-width: max-content;
  background: transparent;
  padding: 0;
  color: #24292f;
  font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", Menlo, monospace;
  font-size: 85%;
  line-height: 1.45;
  white-space: pre;
}

.article-prose :deep(img) {
  margin: 16px auto;
  max-width: 100%;
  border-radius: 6px;
  border: 0;
}

:global(html.dark) .article-prose {
  color: #adbac7;
}

:global(html.dark) .article-prose :deep(h1),
:global(html.dark) .article-prose :deep(h2),
:global(html.dark) .article-prose :deep(h3),
:global(html.dark) .article-prose :deep(h4) {
  color: #f0f6fc;
}

:global(html.dark) .article-prose :deep(p),
:global(html.dark) .article-prose :deep(li),
:global(html.dark) .article-prose :deep(td) {
  color: #adbac7;
}

:global(html.dark) .article-prose :deep(a) {
  color: #58a6ff;
}

:global(html.dark) .article-prose :deep(blockquote) {
  border-left-color: #444c56;
  background: transparent;
  color: #768390;
}

:global(html.dark) .article-prose :deep(hr) {
  background-color: #444c56;
}

:global(html.dark) .article-prose :deep(code:not(pre code)) {
  background: rgba(110, 118, 129, 0.4);
  color: #c9d1d9;
}

:global(html.dark) .article-prose :deep(pre) {
  border-color: #444c56;
  background: #2d333b;
  scrollbar-color: #6e7681 transparent;
}

:global(html.dark) .article-prose :deep(pre code) {
  color: #c9d1d9;
}

:global(html.dark) .article-prose :deep(.code-copy-btn) {
  border-color: #444c56;
  background: #22272e;
  color: #c9d1d9;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.28);
}

:global(html.dark) .article-prose :deep(.code-copy-btn:hover),
:global(html.dark) .article-prose :deep(.code-copy-btn.copied) {
  border-color: #58a6ff;
  color: #58a6ff;
}

:global(html.dark) .article-prose :deep(table) {
  border-color: #444c56;
}

:global(html.dark) .article-prose :deep(thead) {
  background: transparent;
}

:global(html.dark) .article-prose :deep(th),
:global(html.dark) .article-prose :deep(td) {
  border-color: #444c56;
}

:global(html.dark) .article-prose :deep(th) {
  color: #adbac7;
}

:global(html.dark) .article-prose :deep(tr) {
  background-color: #22272e;
  border-top-color: #444c56;
}

:global(html.dark) .article-prose :deep(tr:nth-child(2n)) {
  background-color: #2d333b;
}
</style>

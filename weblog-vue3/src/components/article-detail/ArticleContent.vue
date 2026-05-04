<template>
  <article class="article-content-shell">
    <div class="mb-5 flex flex-wrap gap-2">
      <button
        v-for="tag in normalizedTags"
        :key="tag.id || tag.name"
        class="article-tag-pill rounded-full px-3 py-1 text-sm font-bold transition"
        @click="$emit('go-tag', tag.id, tag.name)"
      >
        # {{ tag.name }}
      </button>
    </div>

    <h1 class="mb-5 text-[38px] font-black leading-tight tracking-tight text-[#0f172a]">{{ article.title }}</h1>

    <div class="article-meta-bar" aria-label="文章信息">
      <span class="article-meta-author">{{ authorName }}</span>

      <template v-if="categoryName">
        <span class="article-meta-divider"></span>

        <button
          class="article-meta-item article-meta-category"
          type="button"
          @click="$emit('go-category', categoryId, categoryName)"
        >
          <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path d="M4 6.5A2.5 2.5 0 0 1 6.5 4H10l2 2h5.5A2.5 2.5 0 0 1 20 8.5v8A2.5 2.5 0 0 1 17.5 19h-11A2.5 2.5 0 0 1 4 16.5v-10Z" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
          </svg>
          <span class="article-meta-category-name">{{ categoryName }}</span>
        </button>
      </template>

      <span class="article-meta-divider"></span>

      <span class="article-meta-item">
        <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="2" />
          <path d="M12 7v5l3 2" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        {{ dateText }}
      </span>

      <span class="article-meta-divider"></span>

      <span class="article-meta-item">
        <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <path d="M6 3h9l4 4v14H6z" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
          <path d="M15 3v5h5M9 13h6M9 17h4" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
        </svg>
        {{ formattedWordCount }}字
      </span>

      <span class="article-meta-divider"></span>

      <span class="article-meta-item">
        <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <path d="M4 5.5A2.5 2.5 0 0 1 6.5 3H20v15H7a3 3 0 0 0-3 3V5.5Z" stroke="currentColor" stroke-width="2" stroke-linejoin="round" />
          <path d="M7 18h13" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
        </svg>
        {{ readingMinutes }}分钟
      </span>

      <span v-if="imageCount > 0" class="article-meta-divider"></span>

      <span v-if="imageCount > 0" class="article-meta-item">
        <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <rect x="4" y="5" width="16" height="14" rx="2" stroke="currentColor" stroke-width="2" />
          <path d="M8 14l2.2-2.2a1 1 0 0 1 1.4 0L14 14l1.2-1.2a1 1 0 0 1 1.4 0L20 16M8 9h.01" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        {{ imageCount }}张图
      </span>

      <span class="article-meta-divider"></span>

      <span class="article-meta-item">
        <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
          <path d="M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6Z" stroke="currentColor" stroke-width="2" />
          <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="2" />
        </svg>
        {{ readCountText }}
      </span>
    </div>

    <slot />

    <div class="article-prose markdown-body markdown-dark mt-8" v-html="cleanContent"></div>
  </article>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import katex from 'katex'
import 'katex/dist/katex.min.css'
import { useBlogSettingsStore } from '@/stores/blogsettings'

const props = defineProps({
  article: { type: Object, default: () => ({}) },
  renderedContent: { type: String, default: '' }
})

defineEmits(['go-tag', 'go-category'])

const blogSettingsStore = useBlogSettingsStore()

onMounted(() => {
  if (!blogSettingsStore.blogSettings?.author) {
    blogSettingsStore.getBlogSettings()
  }
})

const normalizedTags = computed(() => {
  const tags = props.article.tags || []
  return tags.length ? tags : [{ name: 'python' }]
})

const sourceContent = computed(() => String(
  props.article.content
  || props.article.articleContent
  || props.article.markdown
  || props.article.mdContent
  || props.renderedContent
  || ''
))

const authorName = computed(() => (
  props.article.authorName
  || props.article.author
  || props.article.authorNickname
  || props.article.user?.nickname
  || props.article.user?.username
  || props.article.nickname
  || props.article.createBy
  || props.article.userName
  || props.article.username
  || blogSettingsStore.blogSettings?.author
  || '博主'
))

const categoryName = computed(() => String(
  props.article.categoryName
  || props.article.categoryTitle
  || props.article.category?.name
  || props.article.category?.title
  || props.article.category?.categoryName
  || ''
).trim())

const categoryId = computed(() => (
  props.article.categoryId
  || props.article.category?.id
  || props.article.category?.categoryId
  || props.article.category?.value
  || ''
))

const dateText = computed(() => {
  const raw = String(props.article.createTime || props.article.publishTime || props.article.createDate || '').trim()
  if (!raw) return ''
  const datePart = raw.replace('T', ' ').slice(0, 10)
  const [year, month, day] = datePart.split(/[-/]/)
  return year && month && day ? `${year}年${month}月${day}日` : datePart
})

const plainArticleText = computed(() => sourceContent.value
  .replace(/```[\s\S]*?```/g, ' ')
  .replace(/<pre[\s\S]*?<\/pre>/gi, ' ')
  .replace(/!\[[^\]]*]\([^)]+\)/g, ' ')
  .replace(/<img\b[^>]*>/gi, ' ')
  .replace(/<[^>]+>/g, ' ')
  .replace(/[#>*_`~\-[\]()|:]/g, ' ')
  .replace(/&nbsp;|&lt;|&gt;|&amp;/g, ' ')
  .trim())

const wordCount = computed(() => {
  const explicit = Number(props.article.wordCount || props.article.words || props.article.contentWordCount)
  if (Number.isFinite(explicit) && explicit > 0) return explicit

  const text = plainArticleText.value
  const cjkCount = (text.match(/[\u4e00-\u9fa5]/g) || []).length
  const wordMatches = text
    .replace(/[\u4e00-\u9fa5]/g, ' ')
    .match(/[A-Za-z0-9_+#.-]+/g) || []
  return cjkCount + wordMatches.length
})

const formattedWordCount = computed(() => formatNumber(wordCount.value || 0))

const readingMinutes = computed(() => {
  const explicit = Number(props.article.readTime || props.article.readingTime || props.article.readMinute)
  if (Number.isFinite(explicit) && explicit > 0) return Math.ceil(explicit)
  return Math.max(1, Math.ceil((wordCount.value || 0) / 300))
})

const imageCount = computed(() => {
  const explicit = Number(props.article.imageCount || props.article.imgCount || props.article.pictureCount)
  if (Number.isFinite(explicit) && explicit > 0) return explicit
  const content = sourceContent.value
  const markdownImages = content.match(/!\[[^\]]*]\([^)]+\)/g) || []
  const htmlImages = content.match(/<img\b[^>]*>/gi) || []
  return markdownImages.length + htmlImages.length
})

const readCountText = computed(() => formatNumber(
  props.article.readNum
  || props.article.readCount
  || props.article.viewCount
  || props.article.views
  || 0
))

const formatNumber = (value) => {
  const num = Number(value || 0)
  if (!Number.isFinite(num)) return '0'
  return num.toLocaleString('zh-CN')
}

const stripDuplicatedTitle = (html, title) => {
  if (!title) return html
  const escaped = title.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  const firstTitleReg = new RegExp(`^\\s*<h1[^>]*>\\s*${escaped}\\s*</h1>`, 'i')
  return html.replace(firstTitleReg, '')
}

const appendText = (fragment, text) => {
  if (text) fragment.appendChild(document.createTextNode(text))
}

const replaceInlineMarkdownText = (node) => {
  const text = node.nodeValue || ''
  if (!text.includes('**') && !text.includes('[^')) return

  const fragment = document.createDocumentFragment()
  const inlineReg = /(\*\*([^*\n]+?)\*\*|\[\^([^\]\s]+)\])/g
  let lastIndex = 0
  let changed = false
  let match

  while ((match = inlineReg.exec(text)) !== null) {
    appendText(fragment, text.slice(lastIndex, match.index))

    if (match[2]) {
      const strong = document.createElement('strong')
      strong.textContent = match[2]
      fragment.appendChild(strong)
      changed = true
    } else if (match[3]) {
      const sup = document.createElement('sup')
      sup.className = 'md-footnote-ref'
      sup.textContent = `[${match[3]}]`
      fragment.appendChild(sup)
      changed = true
    }

    lastIndex = inlineReg.lastIndex
  }

  if (!changed) return
  appendText(fragment, text.slice(lastIndex))
  node.parentNode?.replaceChild(fragment, node)
}

const normalizeRenderedHtml = (html) => {
  if (!html || typeof document === 'undefined' || typeof NodeFilter === 'undefined') return html

  const template = document.createElement('template')
  template.innerHTML = html

  template.content.querySelectorAll('p').forEach((paragraph) => {
    if (paragraph.textContent?.trim() === '[TOC]') paragraph.remove()
  })

  normalizeBrokenStrong(template.content)

  const blockedSelector = 'code, pre, script, style, textarea, kbd, .mermaid, .math'
  const walker = document.createTreeWalker(template.content, NodeFilter.SHOW_TEXT, {
    acceptNode(node) {
      const text = node.nodeValue || ''
      if (!text.includes('**') && !text.includes('[^')) return NodeFilter.FILTER_REJECT
      if (node.parentElement?.closest(blockedSelector)) return NodeFilter.FILTER_REJECT
      return NodeFilter.FILTER_ACCEPT
    }
  })

  const nodes = []
  while (walker.nextNode()) nodes.push(walker.currentNode)
  nodes.forEach(replaceInlineMarkdownText)

  renderMathElements(template.content)
  wrapMarkdownTables(template.content)

  return template.innerHTML
}

const unwrapElement = (element) => {
  const fragment = document.createDocumentFragment()
  while (element.firstChild) fragment.appendChild(element.firstChild)
  element.replaceWith(fragment)
}

const normalizeBrokenStrong = (root) => {
  root.querySelectorAll('p').forEach((paragraph) => {
    const strongNodes = [...paragraph.querySelectorAll('strong')]
    const totalTextLength = (paragraph.textContent || '').trim().length
    const strongTextLength = strongNodes.reduce((sum, node) => sum + (node.textContent || '').trim().length, 0)
    const looksOverBold = totalTextLength > 0
      && strongNodes.length > 1
      && strongTextLength / totalTextLength > 0.72

    if (!paragraph.querySelector('strong strong') && !looksOverBold) return
    paragraph.querySelectorAll('strong').forEach(unwrapElement)
  })
}

const wrapMarkdownTables = (root) => {
  root.querySelectorAll('table').forEach((table) => {
    if (table.parentElement?.classList.contains('md-table-scroll')) return

    const wrapper = document.createElement('div')
    wrapper.className = 'md-table-scroll'
    table.replaceWith(wrapper)
    wrapper.appendChild(table)
  })
}

const stripMathDelimiters = (source) => {
  let expression = String(source || '').trim()
  expression = expression
    .replace(/^\\\[/, '')
    .replace(/\\\]$/, '')
    .replace(/^\\\(/, '')
    .replace(/\\\)$/, '')
    .replace(/^\$\$/, '')
    .replace(/\$\$$/, '')
    .trim()

  return expression.replace(/\\part\b/g, '\\partial')
}

const renderMathElements = (root) => {
  root.querySelectorAll('.math').forEach((element) => {
    const source = element.textContent || ''
    const expression = stripMathDelimiters(source)
    if (!expression) return

    const displayMode = element.tagName.toLowerCase() === 'div'
      || source.trim().startsWith('\\[')
      || source.trim().startsWith('$$')

    try {
      element.innerHTML = katex.renderToString(expression, {
        displayMode,
        throwOnError: false,
        strict: 'ignore',
        output: 'html'
      })
      element.classList.add('math-rendered')
    } catch (error) {
      console.warn('render math failed:', error)
      element.classList.add('math-fallback')
    }
  })
}

const cleanContent = computed(() => {
  const title = String(props.article.title || '').trim()
  const content = stripDuplicatedTitle(props.renderedContent, title)
  return normalizeRenderedHtml(content)
})
</script>

<style scoped>
.article-content-shell {
  --article-font-sans: "PingFang SC", "Microsoft YaHei", "HarmonyOS Sans SC", system-ui, sans-serif;
  --article-font-serif: "Noto Serif SC", "Source Han Serif SC", "Songti SC", "SimSun", serif;
  --article-font-mono: "JetBrains Mono", "Fira Code", "SFMono-Regular", Consolas, monospace;
  width: 100%;
  max-width: 980px;
  margin: 0 auto;
  color: #2a2a2a;
  font-family: var(--article-font-sans);
}

.article-content-shell > h1 {
  color: #201b17;
  font-family: var(--article-font-serif);
  letter-spacing: 0;
}

.article-content-shell > div:not(.article-prose) {
  color: #6b6258;
}

.article-content-shell button:hover {
  color: #b98a3a;
}

.article-tag-pill {
  border: 1px solid #eadccb;
  background: #f7f0e6;
  color: #7a6242;
}

.article-tag-pill:hover {
  border-color: #d8bf94;
  background: #efe3d2;
  color: #8f6428;
}

.article-meta-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0;
  margin: 0 0 32px;
  color: #6b6258;
  font-size: 14px;
  line-height: 1.6;
}

.article-meta-author,
.article-meta-item {
  display: inline-flex;
  align-items: center;
  min-height: 24px;
  white-space: nowrap;
}

.article-meta-author {
  max-width: 180px;
  overflow: hidden;
  color: #3d342d;
  font-weight: 700;
  text-overflow: ellipsis;
}

.article-meta-item {
  gap: 7px;
  color: inherit;
  font-weight: 500;
}

.article-meta-category {
  max-width: 220px;
  border: 0;
  background: transparent;
  padding: 0;
  cursor: pointer;
}

.article-meta-category:hover {
  color: #8f6428;
}

.article-meta-category-name {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

.article-meta-item svg {
  width: 16px;
  height: 16px;
  flex: 0 0 16px;
  color: #837567;
}

.article-meta-divider {
  width: 1px;
  height: 16px;
  margin: 0 16px;
  flex: 0 0 auto;
  background: #d9c9b7;
}

.article-prose {
  --md-text: #2a2a2a;
  --md-heading: #1f1a16;
  --md-muted: #6b6258;
  --md-subtle: #8a7d70;
  --md-bg: #fcfaf9;
  --md-bg-soft: #f7f0e6;
  --md-bg-softer: #fffdf9;
  --md-border: #e8ded0;
  --md-border-strong: #d8c4a8;
  --md-accent: #c8a36d;
  --md-accent-strong: #b98a3a;
  --md-code-bg: #faf7f1;
  --md-code-border: #e8ded0;
  --md-code-text: #2a2a2a;
  --md-table-alt: #fbf5ec;
  --md-mark-bg: #fff0b8;
  --md-selection: rgba(200, 163, 109, 0.28);
  --color-2-0-c: rgba(255, 230, 170, 0.74);
  max-width: 860px;
  margin: 32px auto 0;
  color: var(--md-text);
  font-family: var(--article-font-sans);
  font-size: var(--article-reading-font-size, 16px);
  line-height: var(--article-reading-line-height, 1.82);
  overflow-wrap: break-word;
  word-break: normal;
}

.article-prose :deep(*)::selection {
  background: var(--md-selection);
}

.article-prose :deep(h1),
.article-prose :deep(h2),
.article-prose :deep(h3),
.article-prose :deep(h4),
.article-prose :deep(h5),
.article-prose :deep(h6) {
  color: var(--md-heading);
  font-family: var(--article-font-serif);
  font-weight: 700;
  letter-spacing: 0;
  line-height: 1.45;
}

.article-prose :deep(h1) {
  margin: 42px 0 24px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--md-border);
  font-size: 30px;
}

.article-prose :deep(h2) {
  position: relative;
  margin: 38px 0 18px;
  padding: 0 0 12px 16px;
  border-bottom: 1px solid var(--md-border);
  font-size: 24px;
}

.article-prose :deep(h2)::before {
  position: absolute;
  top: 0.4em;
  left: 0;
  width: 4px;
  height: 1em;
  border-radius: 999px;
  background: var(--md-accent);
  content: "";
}

.article-prose :deep(h3) {
  margin: 32px 0 14px;
  color: var(--md-accent-strong);
  font-size: 20px;
}

.article-prose :deep(h4) {
  margin: 28px 0 12px;
  font-size: 18px;
}

.article-prose :deep(h5),
.article-prose :deep(h6) {
  margin: 24px 0 10px;
  color: var(--md-muted);
  font-size: 15px;
}

.article-prose :deep(p) {
  margin: 0 0 1.15em;
  color: var(--md-text);
  font-size: 1em;
  font-weight: 400;
  line-height: var(--article-reading-line-height, 1.82);
}

.article-prose :deep(p:empty) {
  display: none;
}

.article-prose :deep(strong),
.article-prose :deep(b) {
  color: var(--md-heading);
  font-weight: 700;
}

.article-prose :deep(em) {
  color: var(--md-muted);
  font-style: italic;
}

.article-prose :deep(u) {
  text-decoration-color: rgba(200, 163, 109, 0.65);
  text-decoration-thickness: 2px;
  text-underline-offset: 3px;
}

.article-prose :deep(a) {
  color: var(--md-accent-strong);
  text-decoration: none;
  border-bottom: 1px solid rgba(185, 138, 58, 0.35);
  transition: border-color 0.16s ease, color 0.16s ease, background-color 0.16s ease;
}

.article-prose :deep(a:hover) {
  color: #9a6924;
  border-bottom-color: currentColor;
  background: rgba(200, 163, 109, 0.12);
}

.article-prose :deep(ul),
.article-prose :deep(ol) {
  margin: 0 0 1.25em;
  padding-left: 1.65em;
  color: var(--md-text);
}

.article-prose :deep(li) {
  margin: 0.25em 0;
  padding-left: 0.15em;
  line-height: 1.78;
}

.article-prose :deep(li > p) {
  margin: 0.35em 0;
}

.article-prose :deep(li::marker) {
  color: var(--md-accent-strong);
}

.article-prose :deep(input[type="checkbox"]) {
  width: 15px;
  height: 15px;
  margin: 0 0.45em 0.15em -1.35em;
  vertical-align: middle;
  accent-color: var(--md-accent-strong);
}

.article-prose :deep(.contains-task-list) {
  padding-left: 1.85em;
  list-style: none;
}

.article-prose :deep(.task-list-item) {
  position: relative;
}

.article-prose :deep(.task-list-item input[type="checkbox"]) {
  transform: translateY(-1px);
}

.article-prose :deep(blockquote) {
  margin: 22px 0;
  border: 1px solid var(--md-border);
  border-left: 4px solid var(--md-accent);
  border-radius: 10px;
  background: linear-gradient(90deg, rgba(200, 163, 109, 0.14), rgba(247, 240, 230, 0.78));
  padding: 16px 18px 16px 22px;
  color: var(--md-muted);
}

.article-prose :deep(blockquote p) {
  margin-bottom: 0.75em;
  color: var(--md-muted);
}

.article-prose :deep(blockquote p:last-child) {
  margin-bottom: 0;
}

.article-prose :deep(.markdown-alert) {
  position: relative;
  margin: 22px 0;
  overflow: hidden;
  border: 1px solid var(--md-border);
  border-left: 4px solid var(--alert-accent, var(--md-accent));
  border-radius: 12px;
  background:
    linear-gradient(135deg, rgba(255, 255, 255, 0.52), transparent 42%),
    var(--alert-bg, var(--md-bg-soft));
  padding: 16px 18px 16px 20px;
  color: var(--md-text);
  box-shadow: 0 12px 28px rgba(73, 52, 24, 0.06);
}

.article-prose :deep(.markdown-alert::after) {
  position: absolute;
  right: -18px;
  bottom: -22px;
  width: 92px;
  height: 92px;
  border-radius: 50%;
  background: var(--alert-accent, var(--md-accent));
  content: "";
  opacity: 0.08;
}

.article-prose :deep(.markdown-alert-title) {
  display: inline-flex;
  position: relative;
  z-index: 1;
  align-items: center;
  gap: 8px;
  margin: 0 0 10px;
  color: var(--alert-accent, var(--md-accent-strong));
  font-weight: 800;
  line-height: 1.4;
}

.article-prose :deep(.markdown-alert-title svg) {
  width: 17px;
  height: 17px;
  fill: currentColor;
}

.article-prose :deep(.markdown-alert > :not(.markdown-alert-title)) {
  position: relative;
  z-index: 1;
}

.article-prose :deep(.markdown-alert p),
.article-prose :deep(.markdown-alert li) {
  color: var(--md-text);
}

.article-prose :deep(.markdown-alert-note) {
  --alert-accent: #b98a3a;
  --alert-bg: #fbf4e9;
}

.article-prose :deep(.markdown-alert-tip) {
  --alert-accent: #5f8f54;
  --alert-bg: #f3f7ea;
}

.article-prose :deep(.markdown-alert-important) {
  --alert-accent: #8f6fc8;
  --alert-bg: #f5f1fb;
}

.article-prose :deep(.markdown-alert-warning) {
  --alert-accent: #c48232;
  --alert-bg: #fff4e3;
}

.article-prose :deep(.markdown-alert-caution) {
  --alert-accent: #bd4f47;
  --alert-bg: #fff0ec;
}

.article-prose :deep(hr) {
  height: 1px;
  margin: 34px 0;
  border: 0;
  background: linear-gradient(90deg, transparent, var(--md-border-strong), transparent);
}

.article-prose :deep(.md-table-scroll) {
  width: 100%;
  max-width: 100%;
  margin: 24px 0;
  overflow-x: auto;
  overflow-y: hidden;
  border: 1px solid var(--md-border);
  border-radius: 12px;
  background: var(--md-bg-softer);
  scrollbar-width: thin;
  scrollbar-color: var(--md-border-strong) transparent;
}

.article-prose :deep(.md-table-scroll::-webkit-scrollbar) {
  height: 8px;
}

.article-prose :deep(.md-table-scroll::-webkit-scrollbar-track) {
  background: transparent;
}

.article-prose :deep(.md-table-scroll::-webkit-scrollbar-thumb) {
  border-radius: 999px;
  background: var(--md-border-strong);
}

.article-prose :deep(table) {
  display: table;
  width: 100%;
  min-width: 640px;
  max-width: none;
  margin: 0;
  border: 0;
  border-spacing: 0;
  border-collapse: separate;
  background: transparent;
  font-size: 14px;
  table-layout: auto;
}

.article-prose :deep(thead) {
  background: var(--md-bg-soft);
}

.article-prose :deep(tbody tr:nth-child(odd) td) {
  background: var(--md-bg-softer);
}

.article-prose :deep(tbody tr:nth-child(even) td) {
  background: var(--md-table-alt);
}

.article-prose :deep(th),
.article-prose :deep(td) {
  min-width: 140px;
  border-right: 1px solid var(--md-border);
  border-bottom: 1px solid var(--md-border);
  padding: 10px 12px;
  color: var(--md-text);
  text-align: left;
  vertical-align: top;
}

.article-prose :deep(th:last-child),
.article-prose :deep(td:last-child) {
  width: 100%;
  border-right: 0;
}

.article-prose :deep(tr:last-child td) {
  border-bottom: 0;
}

.article-prose :deep(th) {
  color: var(--md-heading);
  font-weight: 700;
}

.article-prose :deep(tr:nth-child(2n) td) {
  background: var(--md-table-alt);
}

.article-prose :deep(td code),
.article-prose :deep(th code) {
  white-space: nowrap;
}

.article-prose :deep(code:not(pre code)) {
  border: 1px solid rgba(216, 196, 168, 0.72);
  border-radius: 6px;
  background: rgba(247, 240, 230, 0.9);
  padding: 0.16em 0.38em;
  color: #8f6428;
  font-family: var(--article-font-mono);
  font-size: 0.92em;
}

.article-prose :deep(pre) {
  position: relative;
  max-width: 100%;
  max-height: 640px;
  margin: 24px 0;
  overflow-x: auto;
  overflow-y: auto;
  border: 1px solid var(--md-code-border);
  border-radius: 10px;
  background: var(--md-code-bg);
  padding: 42px 58px 18px 18px;
  color: var(--md-code-text);
  box-shadow: 0 16px 42px rgba(73, 52, 24, 0.08);
  scrollbar-width: thin;
  scrollbar-color: var(--md-border-strong) transparent;
}

.article-prose :deep(pre::-webkit-scrollbar),
.article-prose :deep(pre code::-webkit-scrollbar) {
  height: 8px;
  width: 8px;
}

.article-prose :deep(pre::-webkit-scrollbar-track),
.article-prose :deep(pre code::-webkit-scrollbar-track) {
  background: transparent;
}

.article-prose :deep(pre::-webkit-scrollbar-thumb),
.article-prose :deep(pre code::-webkit-scrollbar-thumb) {
  border-radius: 999px;
  background: var(--md-border-strong);
}

.article-prose :deep(pre::before) {
  position: absolute;
  top: 14px;
  left: 14px;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #f36b5f;
  box-shadow: 18px 0 #e8b64b, 36px 0 #4fb66a;
  content: "";
}

.article-prose :deep(pre:hover .code-copy-btn) {
  opacity: 1;
}

.article-prose :deep(.code-copy-btn) {
  position: absolute;
  top: 10px;
  right: 10px;
  z-index: 2;
  height: 30px;
  border: 1px solid var(--md-code-border);
  border-radius: 7px;
  background: rgba(255, 253, 249, 0.96);
  padding: 0 12px;
  color: var(--md-muted);
  font-size: 12px;
  font-weight: 700;
  line-height: 28px;
  box-shadow: 0 4px 14px rgba(73, 52, 24, 0.08);
  transition: border-color 0.16s ease, color 0.16s ease, background-color 0.16s ease, transform 0.16s ease;
}

.article-prose :deep(.code-copy-btn:hover),
.article-prose :deep(.code-copy-btn.copied) {
  border-color: var(--md-accent);
  color: var(--md-accent-strong);
  transform: translateY(-1px);
}

.article-prose :deep(pre code) {
  display: block;
  min-width: max-content;
  background: transparent;
  padding: 0;
  color: var(--md-code-text);
  font-family: var(--article-font-mono);
  font-size: 14px;
  line-height: 1.72;
  white-space: pre;
}

.article-prose :deep(.hljs),
.article-prose :deep(pre code.hljs) {
  background: transparent;
  color: var(--md-code-text);
}

.article-prose :deep(.hljs-comment),
.article-prose :deep(.hljs-quote) {
  color: #8a7d70;
}

.article-prose :deep(.hljs-keyword),
.article-prose :deep(.hljs-selector-tag),
.article-prose :deep(.hljs-subst) {
  color: #a35c2a;
}

.article-prose :deep(.hljs-number),
.article-prose :deep(.hljs-literal),
.article-prose :deep(.hljs-variable),
.article-prose :deep(.hljs-template-variable) {
  color: #b87333;
}

.article-prose :deep(.hljs-string),
.article-prose :deep(.hljs-doctag) {
  color: #4f7a38;
}

.article-prose :deep(.hljs-title),
.article-prose :deep(.hljs-section),
.article-prose :deep(.hljs-selector-id) {
  color: #7a5aa6;
}

.article-prose :deep(.hljs-type),
.article-prose :deep(.hljs-class .hljs-title) {
  color: #8f6428;
}

.article-prose :deep(.hljs-tag),
.article-prose :deep(.hljs-name),
.article-prose :deep(.hljs-attribute),
.article-prose :deep(.hljs-tag .hljs-attr) {
  color: #a35c2a;
}

.article-prose :deep(.hljs-regexp),
.article-prose :deep(.hljs-link) {
  color: #32706a;
}

.article-prose :deep(.hljs-symbol),
.article-prose :deep(.hljs-bullet),
.article-prose :deep(.hljs-built_in),
.article-prose :deep(.hljs-builtin-name),
.article-prose :deep(.hljs-meta) {
  color: #8f6428;
}

.article-prose :deep(img) {
  display: block;
  max-width: 100%;
  height: auto;
  margin: 24px auto;
  border: 1px solid var(--md-border);
  border-radius: 10px;
  background: var(--md-bg-soft);
  box-shadow: 0 14px 36px rgba(73, 52, 24, 0.08);
}

.article-prose :deep(section) {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 14px;
  align-items: center;
  margin: 24px 0;
  border: 1px solid rgba(232, 222, 208, 0.72);
  border-radius: 12px;
  background: rgba(255, 253, 249, 0.58);
  padding: 14px;
}

.article-prose :deep(section > span) {
  display: grid;
  gap: 10px;
  min-width: 0;
  max-width: 100%;
  color: var(--md-text);
  line-height: 1.75;
}

.article-prose :deep(section > span > span) {
  display: block;
  color: var(--md-text);
}

.article-prose :deep(section img) {
  width: 100%;
  margin: 0;
  box-shadow: none;
}

.article-prose :deep(.math) {
  color: var(--md-heading);
  font-family: var(--article-font-mono);
}

.article-prose :deep(.math.math-rendered) {
  color: var(--md-heading);
  font-family: var(--article-font-serif);
  white-space: normal;
}

.article-prose :deep(p > .math) {
  border-radius: 6px;
  background: rgba(247, 240, 230, 0.72);
  padding: 0.05em 0.28em;
  color: var(--md-accent-strong);
}

.article-prose :deep(p > .math.math-rendered) {
  display: inline-flex;
  align-items: center;
  max-width: 100%;
  overflow-x: auto;
  border: 1px solid rgba(200, 163, 109, 0.2);
  background: rgba(200, 163, 109, 0.1);
  color: var(--md-heading);
  vertical-align: middle;
}

.article-prose :deep(div.math) {
  display: block;
  margin: 20px 0;
  overflow-x: auto;
  border: 1px solid var(--md-border);
  border-radius: 10px;
  background: var(--md-bg-soft);
  padding: 16px;
  color: var(--md-heading);
  font-size: 14px;
  line-height: 1.7;
  white-space: pre;
}

.article-prose :deep(div.math.math-rendered) {
  padding: 18px 20px;
  color: var(--md-heading);
  font-size: 16px;
  text-align: center;
  white-space: normal;
}

.article-prose :deep(.math-rendered .katex) {
  color: inherit;
  font-size: 1.04em;
}

.article-prose :deep(.math-rendered .katex-display) {
  margin: 0;
  overflow-x: auto;
  overflow-y: hidden;
  padding: 2px 0;
}

.article-prose :deep(.mermaid) {
  position: relative;
  margin: 22px 0;
  overflow-x: auto;
  border: 1px dashed var(--md-border-strong);
  border-radius: 12px;
  background: var(--md-bg-soft);
  padding: 42px 16px 16px;
  color: var(--md-muted);
  font-family: var(--article-font-mono);
  font-size: 13px;
  line-height: 1.7;
  white-space: pre;
}

.article-prose :deep(.mermaid.mermaid-rendered) {
  display: flex;
  justify-content: center;
  padding: 18px;
  white-space: normal;
}

.article-prose :deep(.mermaid.mermaid-rendered svg) {
  max-width: 100%;
  height: auto;
}

.article-prose :deep(.mermaid.mermaid-error) {
  border-color: rgba(229, 138, 127, 0.45);
}

.article-prose :deep(.mermaid:not(.mermaid-rendered))::before {
  position: absolute;
  top: 12px;
  left: 14px;
  border-radius: 999px;
  background: rgba(200, 163, 109, 0.16);
  padding: 3px 10px;
  color: var(--md-accent-strong);
  content: "Mermaid";
  font-family: var(--article-font-sans);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 0;
}

.article-prose :deep(span[style*="background"]) {
  border-radius: 4px;
  padding: 0 0.18em;
  color: var(--md-heading);
}

.article-prose :deep(mark) {
  border-radius: 4px;
  background: var(--md-mark-bg);
  padding: 0 0.18em;
  color: var(--md-heading);
}

.article-prose :deep(kbd) {
  display: inline-block;
  min-width: 1.65em;
  border: 1px solid var(--md-border-strong);
  border-bottom-width: 2px;
  border-radius: 6px;
  background: var(--md-bg-soft);
  padding: 0.12em 0.45em;
  color: var(--md-heading);
  font-family: var(--article-font-mono);
  font-size: 0.85em;
  line-height: 1.35;
  text-align: center;
}

.article-prose :deep(del) {
  color: var(--md-subtle);
}

.article-prose :deep(sup),
.article-prose :deep(sub) {
  color: var(--md-muted);
  font-size: 0.78em;
}

.article-prose :deep(.md-footnote-ref) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.35em;
  margin-left: 2px;
  transform: translateY(-0.16em);
  border-radius: 999px;
  background: rgba(200, 163, 109, 0.16);
  padding: 0 0.32em;
  color: var(--md-accent-strong);
  font-family: var(--article-font-sans);
  font-size: 0.72em;
  font-weight: 800;
  line-height: 1.55;
}

.article-prose :deep(details) {
  margin: 18px 0;
  border: 1px solid var(--md-border);
  border-radius: 10px;
  background: var(--md-bg-softer);
  padding: 12px 14px;
}

.article-prose :deep(summary) {
  cursor: pointer;
  color: var(--md-accent-strong);
  font-weight: 700;
}

.article-prose :deep(details[open] summary) {
  margin-bottom: 10px;
}

.article-prose :deep(details > :last-child) {
  margin-bottom: 0;
}

.article-prose :deep(li:has(> pre > code:empty)) {
  display: none;
}

:global(html.dark) .article-content-shell {
  color: #e8e2d8;
}

:global(html.dark) .article-content-shell > h1 {
  color: #f2eadf;
}

:global(html.dark) .article-content-shell > div:not(.article-prose) {
  color: #afa79c;
}

:global(html.dark) .article-tag-pill {
  border-color: #3a332a;
  background: rgba(214, 181, 116, 0.12);
  color: #d6b574;
}

:global(html.dark) .article-tag-pill:hover {
  border-color: rgba(214, 181, 116, 0.55);
  background: rgba(214, 181, 116, 0.18);
  color: #efd39a;
}

:global(html.dark) .article-meta-bar {
  color: #c9c0b6;
}

:global(html.dark) .article-meta-author {
  color: #f2eadf;
}

:global(html.dark) .article-meta-item svg {
  color: #b7aa9a;
}

:global(html.dark) .article-meta-divider {
  background: #4a4034;
}

:global(html.dark) .article-prose,
:global(.dark) .article-prose {
  --md-text: #e8e2d8;
  --md-heading: #f2eadf;
  --md-muted: #afa79c;
  --md-subtle: #8f887d;
  --md-bg: #151a20;
  --md-bg-soft: #22252b;
  --md-bg-softer: #1b2027;
  --md-border: #3a332a;
  --md-border-strong: #514636;
  --md-accent: #d6b574;
  --md-accent-strong: #e3c680;
  --md-code-bg: #151a20;
  --md-code-border: #2a313a;
  --md-code-text: #d8dee9;
  --md-table-alt: #20252c;
  --md-mark-bg: rgba(214, 181, 116, 0.24);
  --md-selection: rgba(214, 181, 116, 0.32);
  --color-2-0-c: rgba(214, 181, 116, 0.26);
}

:global(html.dark .article-prose h1),
:global(html.dark .article-prose h2),
:global(html.dark .article-prose h4),
:global(html.dark .article-prose strong),
:global(html.dark .article-prose b),
:global(html.dark .article-prose mark),
:global(html.dark .article-prose kbd) {
  color: #f2eadf;
}

:global(html.dark .article-prose h3),
:global(html.dark .article-prose a),
:global(html.dark .article-prose summary) {
  color: #e3c680;
}

:global(html.dark .article-prose p),
:global(html.dark .article-prose li),
:global(html.dark .article-prose td),
:global(html.dark .article-prose th),
:global(html.dark .article-prose section > span),
:global(html.dark .article-prose section > span > span),
:global(html.dark .article-prose .math),
:global(html.dark .article-prose .markdown-alert p),
:global(html.dark .article-prose .markdown-alert li) {
  color: #e8e2d8 !important;
}

:global(html.dark .article-prose h5),
:global(html.dark .article-prose h6),
:global(html.dark .article-prose em),
:global(html.dark .article-prose blockquote),
:global(html.dark .article-prose blockquote p),
:global(html.dark .article-prose .mermaid) {
  color: #afa79c;
}

:global(html.dark .article-prose a:hover) {
  color: #efd39a;
  background: rgba(214, 181, 116, 0.12);
}

:global(html.dark .article-prose blockquote) {
  background: linear-gradient(90deg, rgba(214, 181, 116, 0.13), rgba(34, 37, 43, 0.92));
}

:global(html.dark .article-prose .markdown-alert) {
  border-color: #3a332a;
  background:
    linear-gradient(135deg, rgba(255, 255, 255, 0.035), transparent 42%),
    rgba(34, 37, 43, 0.92);
  box-shadow: 0 16px 38px rgba(0, 0, 0, 0.2);
}

:global(html.dark .article-prose .markdown-alert-title) {
  color: var(--alert-accent, #d6b574) !important;
}

:global(html.dark .article-prose .markdown-alert-note) {
  --alert-accent: #d6b574;
  --alert-bg: rgba(214, 181, 116, 0.12);
}

:global(html.dark .article-prose .markdown-alert-tip) {
  --alert-accent: #a8c778;
  --alert-bg: rgba(168, 199, 120, 0.12);
}

:global(html.dark .article-prose .markdown-alert-important) {
  --alert-accent: #c8b5ff;
  --alert-bg: rgba(200, 181, 255, 0.12);
}

:global(html.dark .article-prose .markdown-alert-warning) {
  --alert-accent: #e8c07d;
  --alert-bg: rgba(232, 192, 125, 0.12);
}

:global(html.dark .article-prose .markdown-alert-caution) {
  --alert-accent: #e58a7f;
  --alert-bg: rgba(229, 138, 127, 0.12);
}

:global(html.dark .article-prose section),
:global(html.dark .article-prose div.math),
:global(html.dark .article-prose .mermaid),
:global(html.dark .article-prose details) {
  border-color: #3a332a;
  background: #1b2027;
}

:global(html.dark .article-prose .md-table-scroll),
:global(html.dark .article-prose table) {
  border-color: #3a332a;
  background: #1b2027;
}

:global(html.dark .article-prose thead),
:global(html.dark .article-prose th) {
  background: #242a32 !important;
}

:global(html.dark .article-prose tbody tr:nth-child(odd) td) {
  background: #1b2027 !important;
}

:global(html.dark .article-prose tbody tr:nth-child(even) td) {
  background: #20252c !important;
}

:global(html.dark .article-prose th),
:global(html.dark .article-prose td) {
  border-color: #3a332a !important;
}

:global(html.dark .article-prose p > .math) {
  border-color: rgba(81, 70, 54, 0.82);
  background: rgba(110, 96, 73, 0.24);
  color: #e3c680 !important;
}

:global(html.dark .article-prose .math.math-rendered),
:global(html.dark .article-prose .math-rendered .katex) {
  color: #f2eadf !important;
}

:global(html.dark .article-prose div.math.math-rendered) {
  background: #1b2027;
}

:global(html.dark .article-prose .mermaid:not(.mermaid-rendered)::before) {
  background: rgba(214, 181, 116, 0.16);
  color: #e3c680;
}

:global(html.dark .article-prose code:not(pre code)) {
  border-color: rgba(81, 70, 54, 0.82);
  background: rgba(110, 96, 73, 0.24);
  color: #e3c680;
}

:global(html.dark .article-prose pre) {
  border-color: #2a313a;
  background: #151a20;
  color: #d8dee9;
  box-shadow: 0 16px 44px rgba(0, 0, 0, 0.22);
}

:global(html.dark .article-prose pre code) {
  color: #d8dee9;
}

:global(html.dark .article-prose .code-copy-btn) {
  border-color: var(--md-code-border);
  background: rgba(34, 37, 43, 0.96);
  color: #d8dee9;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.25);
}

:global(html.dark .article-prose .hljs),
:global(html.dark .article-prose pre code.hljs) {
  color: #d8dee9;
}

:global(html.dark .article-prose .hljs-comment),
:global(html.dark .article-prose .hljs-quote) {
  color: #8f9aa7;
}

:global(html.dark .article-prose .hljs-keyword),
:global(html.dark .article-prose .hljs-selector-tag),
:global(html.dark .article-prose .hljs-subst) {
  color: #e6a36a;
}

:global(html.dark .article-prose .hljs-number),
:global(html.dark .article-prose .hljs-literal),
:global(html.dark .article-prose .hljs-variable),
:global(html.dark .article-prose .hljs-template-variable) {
  color: #d6b574;
}

:global(html.dark .article-prose .hljs-string),
:global(html.dark .article-prose .hljs-doctag) {
  color: #b7d28a;
}

:global(html.dark .article-prose .hljs-title),
:global(html.dark .article-prose .hljs-section),
:global(html.dark .article-prose .hljs-selector-id) {
  color: #bba7ff;
}

:global(html.dark .article-prose .hljs-type),
:global(html.dark .article-prose .hljs-class .hljs-title),
:global(html.dark .article-prose .hljs-symbol),
:global(html.dark .article-prose .hljs-bullet),
:global(html.dark .article-prose .hljs-built_in),
:global(html.dark .article-prose .hljs-builtin-name),
:global(html.dark .article-prose .hljs-meta) {
  color: #e8c07d;
}

:global(html.dark .article-prose .hljs-tag),
:global(html.dark .article-prose .hljs-name),
:global(html.dark .article-prose .hljs-attribute),
:global(html.dark .article-prose .hljs-tag .hljs-attr) {
  color: #e6a36a;
}

:global(html.dark .article-prose .hljs-regexp),
:global(html.dark .article-prose .hljs-link) {
  color: #8dd6c9;
}

:global(html.dark .article-prose .md-footnote-ref) {
  background: rgba(214, 181, 116, 0.16);
  color: #e3c680;
}

@media (max-width: 768px) {
  .article-content-shell {
    max-width: none;
  }

  .article-content-shell > h1 {
    font-size: 30px;
  }

  .article-meta-bar {
    margin-bottom: 26px;
    font-size: 13px;
    row-gap: 8px;
  }

  .article-meta-author {
    max-width: 140px;
  }

  .article-meta-category {
    max-width: 150px;
  }

  .article-meta-divider {
    height: 14px;
    margin: 0 10px;
  }

  .article-meta-item {
    gap: 6px;
  }

  .article-meta-item svg {
    width: 15px;
    height: 15px;
    flex-basis: 15px;
  }

  .article-prose {
    max-width: none;
    margin-top: 28px;
    font-size: 15px;
    line-height: 1.78;
  }

  .article-prose :deep(h1) {
    font-size: 26px;
  }

  .article-prose :deep(h2) {
    font-size: 22px;
  }

  .article-prose :deep(h3) {
    font-size: 19px;
  }

  .article-prose :deep(pre) {
    margin-right: -2px;
    margin-left: -2px;
    padding-right: 18px;
  }

  .article-prose :deep(section) {
    grid-template-columns: 1fr;
    padding: 12px;
  }

  .article-prose :deep(.code-copy-btn) {
    top: 8px;
    right: 8px;
  }
}
</style>

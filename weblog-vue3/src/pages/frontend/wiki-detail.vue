<template>
  <Header fixed />
  <div class="wiki-article-page min-h-screen bg-[#FCFAF9] text-[#0f172a]">
    <main class="wiki-article-layout">
      <aside class="wiki-left-rail" aria-label="知识库章节导航">
        <div class="wiki-left-sidebar">
          <section class="wiki-left-card">
            <div class="wiki-left-head">
              <span class="wiki-left-icon"><i class="far fa-compass"></i></span>
              <div>
                <h2>知识库</h2>
                <p>系统化学习{{ wikiTitle || '知识内容' }}</p>
              </div>
            </div>

            <label class="wiki-left-search">
              <i class="fas fa-search"></i>
              <input v-model.trim="wikiSearchKeyword" type="search" placeholder="搜索知识库内容..." />
            </label>

            <nav class="wiki-catalog-tree">
              <button
                v-for="item in wikiVisibleCatalogItems"
                :key="item.uid"
                type="button"
                :class="[
                  'wiki-catalog-item',
                  `depth-${item.depth}`,
                  { active: item.isActive, expanded: item.expanded, folder: item.hasChildren }
                ]"
                :style="{ '--depth': item.depth }"
                @click="selectWikiCatalogItem(item)"
              >
                <span
                  v-if="item.hasChildren"
                  class="wiki-catalog-arrow"
                  @click.stop="toggleWikiCatalogItem(item)"
                >
                  {{ item.expanded ? '⌄' : '›' }}
                </span>
                <span v-else class="wiki-catalog-arrow is-empty"></span>
                <span class="wiki-catalog-label">{{ item.title }}</span>
                <span v-if="item.articleCount && item.hasChildren" class="wiki-catalog-count">{{ item.articleCount }}</span>
                <span v-if="item.isActive" class="wiki-catalog-dot"></span>
              </button>
              <p v-if="!wikiVisibleCatalogItems.length" class="wiki-empty-catalog">暂无匹配内容</p>
            </nav>
          </section>
        </div>
      </aside>

      <section class="wiki-article-main-column">
        <ArticleContent
          v-if="!loading && article.id"
          :article="articleForContent"
          :rendered-content="renderedContent"
          :style="readingStyle"
          @go-tag="goWikiListPage"
          @go-category="goWikiListPage"
        />

        <section
          v-else-if="loading"
          class="space-y-5 rounded-2xl border border-[#e8ded0] bg-[#fffdf9] p-8 shadow-[0_12px_40px_rgba(73,52,24,.08)]"
        >
          <div class="h-7 w-24 animate-pulse rounded-full bg-[#f7f0e6]"></div>
          <div class="h-12 w-4/5 animate-pulse rounded-xl bg-[#f7f0e6]"></div>
          <div class="h-5 w-80 animate-pulse rounded-full bg-[#f7f0e6]"></div>
          <div class="h-28 animate-pulse rounded-2xl bg-[#f7f0e6]"></div>
          <div class="h-72 animate-pulse rounded-2xl bg-[#f7f0e6]"></div>
        </section>

        <section
          v-else
          class="rounded-2xl border border-[#e8ded0] bg-[#fffdf9] p-8 text-center shadow-[0_12px_40px_rgba(73,52,24,.08)]"
        >
          <h1 class="text-2xl font-black text-[#201b17]">这个知识库还没有可阅读的文章</h1>
          <p class="mt-3 text-sm text-[#6b6258]">添加知识库文章后，这里会自动使用新的阅读页布局展示。</p>
        </section>

        <div v-if="article.id" ref="commentSectionRef" class="wiki-article-comments mt-12 space-y-6 pb-16">
          <MessageWallForm
            :router-url="commentRouterUrl"
            title="发表评论"
            collapsible
            @comment-published="handleArticleCommentPublished"
          />
          <MessageWallPanel
            ref="wikiCommentsPanelRef"
            :router-url="commentRouterUrl"
            title="全部评论"
          />
        </div>
      </section>

      <aside v-if="article.id" class="wiki-right-rail" aria-label="本文目录">
        <section class="wiki-reading-sidebar">
        <div class="wiki-side-outline">
          <div class="wiki-side-title">本节目录</div>
          <nav>
            <button
              v-for="item in sidebarTocItems"
              :key="item.id"
              :class="['wiki-side-toc-item', `level-${item.level}`, { active: activeHeadingId === item.id }]"
              type="button"
              @click="scrollToHeading(item.id)"
            >
              {{ item.title }}
            </button>
            <p v-if="!sidebarTocItems.length" class="wiki-side-empty">暂无目录</p>
          </nav>
        </div>
        </section>
      </aside>

      <ShareDrawer
        :article="articleForContent"
        :url="shareUrl"
        :excerpt="plainExcerpt"
        :visible="activePanel === 'share' && !isMobileViewport"
        @close="closeShare"
        @copy="copyShareLink"
      />

      <NotesDrawer
        :visible="notesVisible"
        @close="closeNotes"
        @bookmark="showMessage('知识库阅读笔记已加入收藏', 'success')"
      />

      <FloatingActionBar
        v-if="article.id"
        :active-panel="activePanel"
        @toggle-panel="togglePanel"
        @top="scrollToTop"
        @comment="scrollToComments"
        @copy-link="copyShareLink"
        @font-decrease="decreaseReadingFont"
        @font-increase="increaseReadingFont"
        @template="showMessage('知识库正文模板切换后续会接入更多样式', 'info')"
      />

      <SelectionToolbar
        :visible="selectionToolbar.visible"
        :x="selectionToolbar.x"
        :y="selectionToolbar.y"
        :placement="selectionToolbar.placement"
        @search="searchSelectedText"
        @copy="copySelectedText"
        @translate="translateSelectedText"
        @ask-ai="openSnippetAi"
        @comment="openSnippetComment"
      />

      <SnippetAiPanel
        :visible="snippetAiVisible"
        :selected-text="selectedText"
        @close="snippetAiVisible = false"
      />

      <SnippetCommentPanel
        :visible="snippetCommentVisible"
        :selected-text="selectedText"
        :comments="activeSnippetComments"
        @close="snippetCommentVisible = false"
        @ask-ai="openSnippetAi"
        @submit="submitSnippetComment"
      />

      <Teleport to="body">
        <transition name="mobile-sheet-fade">
          <div v-if="mobileTocVisible" class="mobile-sheet-layer" @click.self="mobileTocVisible = false">
            <section class="mobile-sheet">
              <div class="mobile-sheet-handle"></div>
              <div class="mobile-sheet-header">
                <div>
                  <h2>文章大纲</h2>
                  <p>当前章节会跟随阅读位置高亮</p>
                </div>
                <button @click="mobileTocVisible = false" aria-label="关闭"><i class="fas fa-times"></i></button>
              </div>
              <nav class="mobile-toc-list">
                <button
                  v-for="item in tocItems"
                  :key="item.id"
                  :class="['mobile-toc-item', `level-${item.level}`, { active: activeHeadingId === item.id }]"
                  @click="scrollToHeadingFromMobile(item.id)"
                >
                  <span>{{ item.title }}</span>
                </button>
              </nav>
            </section>
          </div>
        </transition>
      </Teleport>

      <Teleport to="body">
        <transition name="mobile-sheet-fade">
          <div v-if="mobileShareVisible" class="mobile-sheet-layer" @click.self="closeMobileShare">
            <section class="mobile-sheet mobile-share-sheet">
              <div class="mobile-sheet-handle"></div>
              <div class="mobile-sheet-header">
                <div>
                  <h2>分享知识库文章</h2>
                  <p>复制当前文章链接</p>
                </div>
                <button @click="closeMobileShare" aria-label="关闭"><i class="fas fa-times"></i></button>
              </div>
              <div class="mobile-copy-box">
                <input :value="shareUrl" readonly>
                <button @click="copyShareLink">复制</button>
              </div>
            </section>
          </div>
        </transition>
      </Teleport>

      <Teleport to="body">
        <transition name="mobile-sheet-fade">
          <div v-if="mobileMoreVisible" class="mobile-sheet-layer mobile-more-layer" @click.self="mobileMoreVisible = false">
            <section class="mobile-more-sheet">
              <div class="mobile-sheet-handle"></div>
              <div class="mobile-more-grid">
                <button @click="handleMobileShare">
                  <span><i class="fas fa-share-nodes"></i></span>
                  <em>分享</em>
                </button>
                <button :class="{ active: notesVisible }" @click="handleMobileNotes">
                  <span><i class="far fa-pen-to-square"></i></span>
                  <em>笔记</em>
                </button>
                <button @click="handleMobileCopyLink">
                  <span><i class="fas fa-link"></i></span>
                  <em>复制链接</em>
                </button>
                <button @click="decreaseReadingFont">
                  <span><i class="fas fa-font"></i></span>
                  <em>A-</em>
                </button>
                <button @click="increaseReadingFont">
                  <span><i class="fas fa-font"></i></span>
                  <em>A+</em>
                </button>
                <button @click="toggleMobileTheme">
                  <span><i class="far fa-moon"></i></span>
                  <em>夜间</em>
                </button>
              </div>
            </section>
          </div>
        </transition>
      </Teleport>

      <nav class="mobile-action-bar" aria-label="知识库文章操作">
        <button @click="mobileTocVisible = true">
          <i class="fas fa-list-ul"></i>
          <span>目录</span>
        </button>
        <button class="mobile-comment-action" @click="scrollToComments">
          <i class="far fa-comment-dots"></i>
          <span>评论</span>
        </button>
        <button :class="{ active: notesVisible }" @click="handleMobileNotes">
          <i class="far fa-pen-to-square"></i>
          <span>笔记</span>
        </button>
        <button :class="{ active: mobileMoreVisible }" @click="mobileMoreVisible = true">
          <i class="fas fa-ellipsis"></i>
          <span>更多</span>
        </button>
      </nav>

      <button v-show="showMobileTopButton" class="mobile-top-button" aria-label="回到顶部" @click="scrollToTop">
        <i class="fas fa-arrow-up"></i>
      </button>
    </main>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { marked } from 'marked'
import hljs from 'highlight.js'
import 'highlight.js/styles/github.css'
import Header from '@/layouts/frontend/components/Header.vue'
import ArticleContent from '@/components/article-detail/ArticleContent.vue'
import ShareDrawer from '@/components/article-detail/ShareDrawer.vue'
import NotesDrawer from '@/components/article-detail/NotesDrawer.vue'
import FloatingActionBar from '@/components/article-detail/FloatingActionBar.vue'
import SelectionToolbar from '@/components/article-detail/SelectionToolbar.vue'
import SnippetAiPanel from '@/components/article-detail/SnippetAiPanel.vue'
import SnippetCommentPanel from '@/components/article-detail/SnippetCommentPanel.vue'
import MessageWallForm from '@/components/MessageWallForm.vue'
import MessageWallPanel from '@/components/MessageWallPanel.vue'
import { getArticleDetail, clearArticleDetailCache } from '@/api/frontend/article'
import { getWikiCatalogs, getWikiList } from '@/api/frontend/wiki'
import { getCache, setCache } from '@/composables/useCache'
import { showMessage } from '@/composables/util'

const route = useRoute()
const router = useRouter()

const catalogs = ref([])
const wikiTitle = ref('')
const article = ref({})
const loading = ref(true)
const articleReady = ref(false)
const activeHeadingId = ref('')
const tocAutoExpand = ref(false)
const wikiSearchKeyword = ref('')
const expandedCatalogIds = ref(new Set())
const activePanel = ref('')
const notesVisible = ref(false)
const readingFontSize = ref(Number(localStorage.getItem('wikiReadingFontSize') || localStorage.getItem('articleReadingFontSize') || 16))
const commentSectionRef = ref(null)
const wikiCommentsPanelRef = ref(null)
const selectedText = ref('')
const selectedRange = ref(null)
const selectedAnchor = ref(null)
const selectionToolbar = ref({ visible: false, x: 0, y: 0, placement: 'top' })
const snippetAiVisible = ref(false)
const snippetCommentVisible = ref(false)
const snippetComments = ref([])
const activeSnippetComments = ref([])
const mobileTocVisible = ref(false)
const mobileShareVisible = ref(false)
const mobileMoreVisible = ref(false)
const showMobileTopButton = ref(false)
const isMobileViewport = ref(false)

const wikiId = computed(() => route.params.wikiId)
const currentArticleId = computed(() => Number(route.query.articleId || article.value.id || 0))
const commentRouterUrl = computed(() => `/wiki/${wikiId.value}?articleId=${currentArticleId.value || route.query.articleId || ''}`)
const shareUrl = computed(() => (typeof window === 'undefined' ? '' : window.location.href))
const snippetCommentKey = computed(() => `wiki_snippet_comments_${wikiId.value}_${currentArticleId.value}`)
const readingStyle = computed(() => ({
  '--article-reading-font-size': `${readingFontSize.value}px`,
  '--article-reading-line-height': '1.82'
}))

const normalizedTags = computed(() => {
  const tags = article.value.tags || []
  if (Array.isArray(tags) && tags.length) return tags
  return [{ id: `wiki-${wikiId.value}`, name: wikiTitle.value || '知识库' }]
})

const articleForContent = computed(() => ({
  ...article.value,
  tags: normalizedTags.value,
  categoryName: article.value.categoryName || wikiTitle.value || '知识库'
}))

const renderedContent = computed(() => {
  const content = article.value.content || ''
  return /<\/?[a-z][\s\S]*>/i.test(content)
    ? content
    : marked.parse(content, { breaks: true, gfm: true })
})

const plainExcerpt = computed(() => {
  const raw = article.value.summary || article.value.description || article.value.content || ''
  return String(raw)
    .replace(/<[^>]+>/g, '')
    .replace(/[#*_`>[\]()]/g, '')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 96)
})

const stripDuplicatedTitleHeading = (html, title) => {
  const text = String(title || '').trim()
  if (!text) return html
  const escaped = text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
  const firstTitleReg = new RegExp(`^\\s*<h1([^>]*)>\\s*${escaped}\\s*</h1>`, 'i')
  return String(html || '').replace(firstTitleReg, '')
}

const articleHeadingSelector = '.article-prose h1, .article-prose h2, .article-prose h3'

const tocItems = computed(() => {
  const html = stripDuplicatedTitleHeading(renderedContent.value || '', article.value.title)
  const matches = [...html.matchAll(/<h([1-3])([^>]*)>(.*?)<\/h\1>/gi)]
  return matches
    .map((match, index) => {
      const idMatch = match[2].match(/id=["']?([^"'>\s]+)["']?/i)
      return {
        id: idMatch?.[1] || `heading-${index}`,
        level: Number(match[1]),
        title: match[3].replace(/<[^>]+>/g, '').trim()
      }
    })
    .filter(item => item.title)
})

const sidebarTocItems = computed(() => tocItems.value)

const stripHtml = (value) => String(value || '').replace(/<[^>]+>/g, '').trim()

const getCatalogArticleId = (catalog) => catalog.articleId || catalog.article?.id || catalog.article?.articleId

const getCatalogTitle = (catalog) => stripHtml(
  catalog.title
  || catalog.articleTitle
  || catalog.name
  || catalog.article?.title
  || '未命名'
)

const getCatalogUid = (catalog, path) => `wiki-catalog-${catalog.id || getCatalogArticleId(catalog) || path}`

const countCatalogArticles = (catalog) => {
  const selfCount = getCatalogArticleId(catalog) ? 1 : 0
  return selfCount + (catalog.children || []).reduce((total, child) => total + countCatalogArticles(child), 0)
}

const catalogMatchesKeyword = (catalog, keyword) => {
  if (!keyword) return true
  const title = getCatalogTitle(catalog).toLowerCase()
  if (title.includes(keyword)) return true
  return (catalog.children || []).some(child => catalogMatchesKeyword(child, keyword))
}

const flattenWikiCatalogs = (nodes = [], depth = 0, path = 'root') => {
  const keyword = wikiSearchKeyword.value.trim().toLowerCase()

  return nodes.flatMap((catalog, index) => {
    if (keyword && !catalogMatchesKeyword(catalog, keyword)) return []

    const uid = getCatalogUid(catalog, `${path}-${index}`)
    const children = catalog.children || []
    const articleId = getCatalogArticleId(catalog)
    const hasChildren = children.length > 0
    const isActive = articleId && String(articleId) === String(currentArticleId.value)
    const expanded = keyword ? true : expandedCatalogIds.value.has(uid)
    const item = {
      uid,
      catalog,
      title: getCatalogTitle(catalog),
      depth,
      articleId,
      hasChildren,
      expanded,
      isActive,
      articleCount: hasChildren ? countCatalogArticles(catalog) : 0
    }

    return [
      item,
      ...(hasChildren && expanded ? flattenWikiCatalogs(children, depth + 1, uid) : [])
    ]
  })
}

const wikiVisibleCatalogItems = computed(() => flattenWikiCatalogs(catalogs.value))

const toggleWikiCatalogItem = (item) => {
  if (!item.hasChildren) return
  const next = new Set(expandedCatalogIds.value)
  next.has(item.uid) ? next.delete(item.uid) : next.add(item.uid)
  expandedCatalogIds.value = next
}

const selectWikiCatalogItem = (item) => {
  if (item.articleId) {
    goWikiArticleDetailPage(item.articleId)
    return
  }
  toggleWikiCatalogItem(item)
}

const findFirstArticleId = (nodes = []) => {
  for (const node of nodes) {
    if (node.articleId) return node.articleId
    const found = findFirstArticleId(node.children || [])
    if (found) return found
  }
  return null
}

const loadWikiMeta = async () => {
  const cachedWikis = getCache('page_wikis')
  if (cachedWikis) {
    const wiki = cachedWikis.find(item => String(item.id) === String(wikiId.value))
    if (wiki) {
      wikiTitle.value = wiki.title
      return
    }
  }

  try {
    const res = await getWikiList()
    if (res.success) {
      const list = res.data || []
      const wiki = list.find(item => String(item.id) === String(wikiId.value))
      if (wiki) wikiTitle.value = wiki.title
      setCache('page_wikis', list, 5 * 60 * 1000)
    }
  } catch (error) {
    console.warn('load wiki meta failed:', error)
  }
}

const loadWikiCatalogs = async () => {
  try {
    const res = await getWikiCatalogs(wikiId.value)
    if (res.success) {
      catalogs.value = res.data || []
      if (!route.query.articleId) {
        const firstArticleId = findFirstArticleId(catalogs.value)
        if (firstArticleId) {
          router.replace({ path: `/wiki/${wikiId.value}`, query: { articleId: firstArticleId } })
          return
        }
      }
    }
  } catch (error) {
    console.error('load wiki catalogs failed:', error)
    showMessage('知识库目录加载失败', 'error')
  } finally {
    if (!route.query.articleId) loading.value = false
  }
}

const loadArticle = async (articleId) => {
  if (!articleId) {
    article.value = {}
    loading.value = false
    articleReady.value = false
    return
  }

  loading.value = true
  articleReady.value = false
  try {
    clearArticleDetailCache(articleId)
    const res = await getArticleDetail(articleId)
    if (!res.success) {
      router.push({ name: 'NotFound' })
      return
    }

    article.value = res.data || {}
    loading.value = false
    await nextTick()
    decorateArticle()
    loadSnippetComments()
    applySnippetHighlights()
    articleReady.value = true
  } catch (error) {
    console.error('load wiki article failed:', error)
    showMessage('知识库文章加载失败，请检查接口服务', 'error')
    loading.value = false
    articleReady.value = false
  }
}

const decorateArticle = () => {
  document.querySelectorAll('.article-prose pre code').forEach((block) => hljs.highlightElement(block))
  document.querySelectorAll('.article-prose pre').forEach((pre) => {
    if (pre.querySelector('.code-toolbar')) return

    const toolbar = document.createElement('div')
    toolbar.className = 'code-toolbar'

    const dots = document.createElement('span')
    dots.className = 'code-window-dots'
    dots.setAttribute('aria-hidden', 'true')
    for (let index = 0; index < 3; index += 1) {
      dots.appendChild(document.createElement('span'))
    }

    const button = document.createElement('button')
    button.type = 'button'
    button.className = 'code-copy-btn'
    button.textContent = '复制'
    button.addEventListener('click', async () => {
      const code = pre.querySelector('code')?.innerText || ''
      if (!code) return
      try {
        await navigator.clipboard?.writeText(code)
        button.textContent = '已复制'
        button.classList.add('copied')
        window.setTimeout(() => {
          button.textContent = '复制'
          button.classList.remove('copied')
        }, 1400)
      } catch (error) {
        console.error('copy code failed:', error)
        showMessage('复制失败，请手动选择代码', 'warning')
      }
    })

    toolbar.append(dots, button)
    pre.insertBefore(toolbar, pre.firstChild)
  })

  document.querySelectorAll(articleHeadingSelector).forEach((heading, index) => {
    if (!heading.id) heading.id = `heading-${index}`
  })
  updateActiveHeading()
}

const updateActiveHeading = () => {
  if (typeof window === 'undefined') return
  showMobileTopButton.value = window.scrollY > 520
  const headings = [...document.querySelectorAll(articleHeadingSelector)]
  if (!headings.length) return
  const current = headings.filter((heading) => heading.getBoundingClientRect().top <= 120).pop()
  activeHeadingId.value = current?.id || headings[0]?.id || ''
  tocAutoExpand.value = true
}

const scrollToHeading = (id) => {
  const heading = document.getElementById(id)
  if (!heading) return
  const targetTop = heading.getBoundingClientRect().top + window.scrollY - 92
  window.scrollTo({ top: targetTop, behavior: 'smooth' })
  activeHeadingId.value = id
}

const scrollToHeadingFromMobile = (id) => {
  scrollToHeading(id)
  mobileTocVisible.value = false
}

const getProseRoot = () => document.querySelector('.wiki-article-page .article-prose')

const getSnippetBlocks = () => {
  const prose = getProseRoot()
  if (!prose) return []
  return [...prose.querySelectorAll('p, li, blockquote, td')]
    .filter((node) => !node.closest('pre'))
}

const assignSnippetAnchors = () => {
  getSnippetBlocks().forEach((node, index) => {
    node.dataset.snippetAnchor = String(index)
  })
}

const loadSnippetComments = () => {
  try {
    snippetComments.value = JSON.parse(localStorage.getItem(snippetCommentKey.value) || '[]')
  } catch {
    snippetComments.value = []
  }
}

const saveSnippetComments = () => {
  localStorage.setItem(snippetCommentKey.value, JSON.stringify(snippetComments.value))
}

const clearSnippetHighlights = () => {
  getProseRoot()?.querySelectorAll('.snippet-comment-highlight').forEach((node) => {
    const text = document.createTextNode(node.textContent || '')
    node.replaceWith(text)
  })
}

const wrapTextInNode = (root, targetText, commentId, count) => {
  if (!root || !targetText) return false
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
    acceptNode(node) {
      if (!node.nodeValue?.includes(targetText)) return NodeFilter.FILTER_REJECT
      if (node.parentElement?.closest('.snippet-comment-highlight, pre, code')) return NodeFilter.FILTER_REJECT
      return NodeFilter.FILTER_ACCEPT
    }
  })
  const textNode = walker.nextNode()
  if (!textNode) return false
  const start = textNode.nodeValue.indexOf(targetText)
  if (start < 0) return false
  const range = document.createRange()
  range.setStart(textNode, start)
  range.setEnd(textNode, start + targetText.length)
  const mark = document.createElement('span')
  mark.className = 'snippet-comment-highlight'
  mark.dataset.commentId = String(commentId)
  mark.title = `${count} 条片段评论`
  range.surroundContents(mark)
  return true
}

const applySnippetHighlights = () => {
  clearSnippetHighlights()
  assignSnippetAnchors()
  const groups = new Map()
  snippetComments.value.forEach((item) => {
    const key = item.anchorId || item.selectedText
    if (!groups.has(key)) groups.set(key, [])
    groups.get(key).push(item)
  })
  groups.forEach((comments) => {
    const first = comments[0]
    const anchor = document.querySelector(`[data-snippet-anchor="${first.anchorId}"]`) || getProseRoot()
    wrapTextInNode(anchor, first.selectedText, first.id, comments.length)
  })
}

const getSelectionAnchor = (range, text) => {
  const blocks = getSnippetBlocks()
  const block = blocks.find((node) => node.contains(range.commonAncestorContainer))
  const anchorIndex = block?.dataset.snippetAnchor || ''
  const blockText = block?.innerText || ''
  const startOffset = blockText.indexOf(text)
  return {
    anchorId: anchorIndex,
    paragraphIndex: anchorIndex ? Number(anchorIndex) : -1,
    startOffset,
    endOffset: startOffset >= 0 ? startOffset + text.length : -1
  }
}

const hideSelectionToolbar = () => {
  selectionToolbar.value = { ...selectionToolbar.value, visible: false }
}

const updateSelectionToolbar = () => {
  const selection = window.getSelection()
  const text = selection?.toString().trim() || ''
  const prose = getProseRoot()
  if (!selection || selection.rangeCount === 0 || !text || !prose) {
    hideSelectionToolbar()
    return
  }

  const range = selection.getRangeAt(0)
  const container = range.commonAncestorContainer.nodeType === Node.TEXT_NODE
    ? range.commonAncestorContainer.parentElement
    : range.commonAncestorContainer

  if (!container || !prose.contains(container) || container.closest('pre, code')) {
    hideSelectionToolbar()
    return
  }

  const rect = range.getBoundingClientRect()
  if (!rect.width && !rect.height) {
    hideSelectionToolbar()
    return
  }

  const placement = rect.top > 86 ? 'top' : 'bottom'
  selectedText.value = text.slice(0, 1000)
  selectedRange.value = range.cloneRange()
  selectedAnchor.value = getSelectionAnchor(range, selectedText.value)
  selectionToolbar.value = {
    visible: true,
    x: Math.min(Math.max(rect.left + rect.width / 2, 220), window.innerWidth - 220),
    y: placement === 'top' ? rect.top : rect.bottom,
    placement
  }
}

const handleSelectionChange = () => {
  window.requestAnimationFrame(updateSelectionToolbar)
}

const handleDocumentMouseDown = (event) => {
  if (event.target.closest('.floating-selection-toolbar, .snippet-ai-panel, .snippet-comment-panel, .snippet-comment-highlight')) return
  window.setTimeout(() => {
    if (!window.getSelection()?.toString().trim()) hideSelectionToolbar()
  }, 0)
}

const searchSelectedText = () => {
  if (!selectedText.value) return
  window.open(`https://www.bing.com/search?q=${encodeURIComponent(selectedText.value)}`, '_blank', 'noopener,noreferrer')
  hideSelectionToolbar()
}

const copySelectedText = async () => {
  if (!selectedText.value) return
  await navigator.clipboard?.writeText(selectedText.value)
  showMessage('选中文字已复制', 'success')
  hideSelectionToolbar()
}

const translateSelectedText = () => {
  if (!selectedText.value) return
  window.open(`https://www.bing.com/translator?from=auto&to=zh-Hans&text=${encodeURIComponent(selectedText.value)}`, '_blank', 'noopener,noreferrer')
  hideSelectionToolbar()
}

const openSnippetAi = () => {
  if (!selectedText.value) return
  snippetAiVisible.value = true
  snippetCommentVisible.value = false
  hideSelectionToolbar()
}

const openSnippetComment = () => {
  if (!selectedText.value) return
  activeSnippetComments.value = snippetComments.value.filter((item) =>
    item.selectedText === selectedText.value || item.anchorId === selectedAnchor.value?.anchorId
  )
  snippetCommentVisible.value = true
  snippetAiVisible.value = false
  hideSelectionToolbar()
}

const submitSnippetComment = (payload) => {
  const content = typeof payload === 'string' ? payload : payload?.content
  const userInfo = typeof payload === 'string'
    ? { nickname: '读者' }
    : (payload?.userInfo || { nickname: '读者' })
  if (!String(content || '').trim()) return
  const createTime = new Date().toLocaleString()

  if (payload?.mode === 'reply' && payload?.parentCommentId) {
    const replyRecord = {
      id: Date.now(),
      parentCommentId: payload.parentCommentId,
      replyToUserId: payload.replyToUserId || null,
      replyToNickname: payload.replyToNickname || '',
      commentContent: String(content).trim(),
      images: Array.isArray(payload.images) ? payload.images.join(',') : (payload.images || ''),
      userInfo,
      createTime,
      likes: 0
    }

    const appendReply = (items) => {
      for (const item of items) {
        if (String(item.id) === String(payload.parentCommentId)) {
          item.replies = Array.isArray(item.replies) ? item.replies : []
          item.replies.push(replyRecord)
          return true
        }
        if (Array.isArray(item.replies) && appendReply(item.replies)) return true
      }
      return false
    }

    if (appendReply(snippetComments.value)) {
      activeSnippetComments.value = snippetComments.value.filter((item) =>
        item.selectedText === selectedText.value || item.anchorId === selectedAnchor.value?.anchorId
      )
      saveSnippetComments()
      showMessage('回复已发送', 'success')
      return
    }
  }

  const record = {
    id: Date.now(),
    articleId: currentArticleId.value,
    wikiId: wikiId.value,
    selectedText: selectedText.value,
    anchorId: selectedAnchor.value?.anchorId || '',
    paragraphIndex: selectedAnchor.value?.paragraphIndex ?? -1,
    startOffset: selectedAnchor.value?.startOffset ?? -1,
    endOffset: selectedAnchor.value?.endOffset ?? -1,
    commentContent: String(content).trim(),
    images: Array.isArray(payload?.images) ? payload.images.join(',') : (payload?.images || ''),
    userInfo,
    createTime,
    likes: 0,
    replies: []
  }
  snippetComments.value.unshift(record)
  activeSnippetComments.value = snippetComments.value.filter((item) =>
    item.selectedText === record.selectedText || item.anchorId === record.anchorId
  )
  saveSnippetComments()
  nextTick(applySnippetHighlights)
  showMessage('片段评论已发布', 'success')
}

const handleSnippetHighlightClick = (event) => {
  const target = event.target.closest('.snippet-comment-highlight')
  if (!target) return
  const id = Number(target.dataset.commentId)
  const seed = snippetComments.value.find((item) => item.id === id)
  if (!seed) return
  selectedText.value = seed.selectedText
  selectedAnchor.value = {
    anchorId: seed.anchorId,
    paragraphIndex: seed.paragraphIndex,
    startOffset: seed.startOffset,
    endOffset: seed.endOffset
  }
  activeSnippetComments.value = snippetComments.value.filter((item) =>
    item.selectedText === seed.selectedText || item.anchorId === seed.anchorId
  )
  snippetCommentVisible.value = true
  snippetAiVisible.value = false
}

const goWikiArticleDetailPage = (articleId) => {
  if (!articleId) return
  router.push({ path: `/wiki/${wikiId.value}`, query: { articleId } })
}

const goWikiListPage = () => router.push({ path: '/wiki/list' })

const copyShareLink = async () => {
  await navigator.clipboard?.writeText(shareUrl.value)
  showMessage('链接已复制', 'success')
}

const closeShare = () => {
  activePanel.value = ''
  mobileShareVisible.value = false
}

const closeMobileShare = () => {
  mobileShareVisible.value = false
  if (activePanel.value === 'share') activePanel.value = ''
}

const closeNotes = () => {
  notesVisible.value = false
  activePanel.value = ''
}

const togglePanel = (panel) => {
  if (panel === 'share') {
    if (isMobileViewport.value) {
      mobileShareVisible.value = !mobileShareVisible.value
      notesVisible.value = false
      activePanel.value = mobileShareVisible.value ? 'share' : ''
      return
    }
    notesVisible.value = false
    activePanel.value = activePanel.value === 'share' ? '' : 'share'
    return
  }

  if (panel === 'notes') {
    notesVisible.value = !notesVisible.value
    activePanel.value = notesVisible.value ? 'notes' : ''
    return
  }

  notesVisible.value = false
  activePanel.value = ''
}

const updateReadingFontSize = (nextSize) => {
  readingFontSize.value = Math.min(20, Math.max(14, nextSize))
  localStorage.setItem('wikiReadingFontSize', String(readingFontSize.value))
}

const decreaseReadingFont = () => updateReadingFontSize(readingFontSize.value - 1)
const increaseReadingFont = () => updateReadingFontSize(readingFontSize.value + 1)

const scrollToTop = () => window.scrollTo({ top: 0, behavior: 'smooth' })

const scrollToComments = () => {
  mobileTocVisible.value = false
  mobileShareVisible.value = false
  mobileMoreVisible.value = false
  commentSectionRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

const handleArticleCommentPublished = () => {
  wikiCommentsPanelRef.value?.refresh?.()
}

const handleMobileShare = () => {
  mobileMoreVisible.value = false
  mobileShareVisible.value = true
  notesVisible.value = false
  activePanel.value = 'share'
}

const handleMobileNotes = () => {
  mobileMoreVisible.value = false
  togglePanel('notes')
}

const handleMobileCopyLink = async () => {
  await copyShareLink()
  mobileMoreVisible.value = false
}

const toggleMobileTheme = () => {
  document.documentElement.classList.toggle('dark')
  mobileMoreVisible.value = false
}

const updateViewportState = () => {
  isMobileViewport.value = window.innerWidth <= 768
  if (!isMobileViewport.value) {
    mobileTocVisible.value = false
    mobileShareVisible.value = false
    mobileMoreVisible.value = false
  }
}

onMounted(() => {
  updateViewportState()
  loadWikiMeta()
  loadWikiCatalogs()
  window.addEventListener('scroll', updateActiveHeading, { passive: true })
  window.addEventListener('resize', updateViewportState, { passive: true })
  document.addEventListener('selectionchange', handleSelectionChange)
  document.addEventListener('mousedown', handleDocumentMouseDown)
  document.addEventListener('click', handleSnippetHighlightClick)
})

onBeforeUnmount(() => {
  window.removeEventListener('scroll', updateActiveHeading)
  window.removeEventListener('resize', updateViewportState)
  document.removeEventListener('selectionchange', handleSelectionChange)
  document.removeEventListener('mousedown', handleDocumentMouseDown)
  document.removeEventListener('click', handleSnippetHighlightClick)
})

watch(
  () => route.query.articleId,
  (articleId) => {
    snippetAiVisible.value = false
    snippetCommentVisible.value = false
    activeSnippetComments.value = []
    selectedText.value = ''
    selectedRange.value = null
    selectedAnchor.value = null
    hideSelectionToolbar()
    loadArticle(articleId)
  },
  { immediate: true }
)

watch(
  () => route.params.wikiId,
  () => {
    catalogs.value = []
    wikiTitle.value = ''
    expandedCatalogIds.value = new Set()
    loadWikiMeta()
    loadWikiCatalogs()
  }
)
</script>

<style scoped>
@import '@fortawesome/fontawesome-free/css/all.min.css';

:global(html) {
  scroll-padding-top: 92px;
}

.mobile-action-bar,
.mobile-top-button {
  display: none;
}

.wiki-article-page {
  --wiki-sticky-top: 96px;
  --wiki-page-bg: #faf7f2;
  --wiki-card-bg: rgba(255, 253, 248, 0.94);
  --wiki-card-solid: #fffdf8;
  --wiki-text: #243142;
  --wiki-heading: #182433;
  --wiki-muted: #7c8794;
  --wiki-soft: #f3eee4;
  --wiki-border: #eadfcd;
  --wiki-accent: #2f8f7b;
  --wiki-accent-soft: #e8f3ec;
  --wiki-shadow: 0 14px 36px rgba(74, 57, 32, 0.08);
  background:
    radial-gradient(circle at 30% 0%, rgba(255, 255, 255, 0.82), transparent 34rem),
    var(--wiki-page-bg);
  color: var(--wiki-text);
}

:global(html.dark) .wiki-article-page {
  --wiki-page-bg: #11151b;
  --wiki-card-bg: #1b2027;
  --wiki-card-solid: #22252b;
  --wiki-text: #e8e2d8;
  --wiki-heading: #f2eadf;
  --wiki-muted: #afa79c;
  --wiki-soft: #22252b;
  --wiki-border: #3a332a;
  --wiki-accent: #79c7ae;
  --wiki-accent-soft: rgba(95, 168, 142, 0.16);
  --wiki-shadow: 0 18px 44px rgba(0, 0, 0, 0.22);
  background:
    radial-gradient(circle at 30% 0%, rgba(255, 255, 255, 0.04), transparent 34rem),
    var(--wiki-page-bg) !important;
  color: var(--wiki-text) !important;
}

.wiki-article-layout {
  display: grid;
  grid-template-columns: minmax(320px, 0.82fr) minmax(0, 860px) minmax(240px, 0.82fr);
  align-items: start;
  column-gap: clamp(22px, 2vw, 34px);
  width: 100%;
  max-width: none;
  margin: 0 auto;
  padding: 34px clamp(18px, 2.5vw, 56px) 84px;
}

.wiki-article-main-column {
  grid-column: 2;
  min-width: 0;
  width: 100%;
  max-width: 860px;
  justify-self: center;
}

.wiki-article-comments {
  width: 100%;
}

.wiki-left-rail,
.wiki-right-rail {
  position: relative;
  align-self: start;
  min-width: 0;
  width: 100%;
  height: 0;
  z-index: 5;
}

.wiki-left-rail {
  justify-self: stretch;
}

.wiki-right-rail {
  justify-self: start;
}

.wiki-left-sidebar,
.wiki-reading-sidebar {
  position: fixed;
  top: var(--wiki-sticky-top);
  height: auto;
  max-height: calc(100vh - var(--wiki-sticky-top) - 24px);
  overflow-x: hidden;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding-right: 4px;
  scrollbar-color: rgba(47, 143, 123, 0.34) transparent;
  scrollbar-gutter: stable;
  scrollbar-width: thin;
  z-index: 6;
}

.wiki-reading-sidebar::-webkit-scrollbar {
  width: 6px;
}

.wiki-reading-sidebar::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(47, 143, 123, 0.28);
}

.wiki-left-sidebar {
  display: flex;
  flex-direction: column;
  width: min(100%, 290px);
  height: calc(100vh - var(--wiki-sticky-top) - 24px);
  overflow: hidden;
  margin-inline: auto;
}

.wiki-left-card {
  display: flex;
  flex: 1;
  flex-direction: column;
  min-height: 0;
  border: 0;
  background: transparent;
  box-shadow: none;
  backdrop-filter: none;
}

.wiki-left-card {
  padding: 12px 10px 14px;
}

.wiki-left-head {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  margin-bottom: 14px;
}

.wiki-left-icon {
  display: grid;
  width: 30px;
  height: 30px;
  flex: 0 0 30px;
  place-items: center;
  border-radius: 10px;
  background: var(--wiki-accent-soft);
  color: var(--wiki-accent);
  font-size: 14px;
}

.wiki-left-head h2 {
  margin: 0;
  color: var(--wiki-heading);
  font-size: 16px;
  font-weight: 900;
  line-height: 1.25;
}

.wiki-left-head p {
  margin: 4px 0 0;
  color: var(--wiki-muted);
  font-size: 12px;
  line-height: 1.45;
}

.wiki-left-search {
  display: flex;
  align-items: center;
  gap: 8px;
  height: 36px;
  margin-bottom: 14px;
  border: 1px solid var(--wiki-border);
  border-radius: 12px;
  background: var(--wiki-card-solid);
  color: var(--wiki-muted);
  padding: 0 11px;
}

.wiki-left-search input {
  min-width: 0;
  flex: 1;
  background: transparent;
  color: var(--wiki-text);
  font-size: 12px;
  outline: none;
}

.wiki-left-search input::placeholder {
  color: var(--wiki-muted);
}

.wiki-catalog-tree {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  flex: 1;
  gap: 4px;
  min-height: 0;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding-right: 4px;
  scrollbar-color: rgba(47, 143, 123, 0.34) transparent;
  scrollbar-gutter: stable;
  scrollbar-width: thin;
}

.wiki-catalog-tree::-webkit-scrollbar {
  width: 6px;
}

.wiki-catalog-tree::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(47, 143, 123, 0.28);
}

.wiki-catalog-item {
  --depth: 0;
  position: relative;
  display: grid;
  grid-template-columns: 14px minmax(0, 1fr) auto 8px;
  align-items: center;
  gap: 5px;
  min-height: 33px;
  border-radius: 10px;
  color: #667382;
  font-size: 12.5px;
  font-weight: 700;
  line-height: 1.5;
  padding: 7px 8px 7px calc(7px + (var(--depth) * 13px));
  text-align: left;
  transition: background-color 0.16s ease, color 0.16s ease;
}

.wiki-catalog-item.depth-0 {
  min-height: 36px;
  padding-top: 8px;
  padding-bottom: 8px;
}

.wiki-catalog-item:not(.folder) {
  min-height: 38px;
  margin: 1px 0;
  padding-top: 8px;
  padding-bottom: 8px;
  line-height: 1.58;
}

.wiki-catalog-item:hover {
  background: rgba(232, 243, 236, 0.72);
  color: #315d52;
}

.wiki-catalog-item.active {
  background: var(--wiki-accent-soft);
  color: var(--wiki-accent);
  font-weight: 900;
}

.wiki-catalog-arrow {
  display: grid;
  width: 14px;
  place-items: center;
  color: #82909f;
  font-size: 14px;
  line-height: 1;
}

.wiki-catalog-item.active .wiki-catalog-arrow,
.wiki-catalog-item:hover .wiki-catalog-arrow {
  color: var(--wiki-accent);
}

.wiki-catalog-arrow.is-empty {
  opacity: 0;
}

.wiki-catalog-label {
  min-width: 0;
  overflow-wrap: anywhere;
}

.wiki-catalog-count {
  min-width: 18px;
  border-radius: 999px;
  background: rgba(47, 143, 123, 0.1);
  color: var(--wiki-accent);
  font-size: 11px;
  font-weight: 900;
  line-height: 1;
  padding: 4px 6px;
  text-align: center;
}

.wiki-catalog-dot {
  width: 6px;
  height: 6px;
  border-radius: 999px;
  background: var(--wiki-accent);
  box-shadow: 0 0 0 3px rgba(47, 143, 123, 0.12);
}

.wiki-empty-catalog {
  color: var(--wiki-muted);
  font-size: 12px;
  padding: 10px 8px;
}

.wiki-reading-sidebar {
  width: min(100%, 260px);
  padding-top: 6px;
}

.wiki-side-title {
  margin-bottom: 12px;
  color: var(--wiki-heading);
  font-size: 14px;
  font-weight: 900;
  letter-spacing: 0;
}

.wiki-side-outline {
  border-left: 1px solid rgba(95, 168, 142, 0.28);
  padding-left: 14px;
}

.wiki-side-outline nav {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.wiki-side-toc-item {
  position: relative;
  border-radius: 0;
  color: var(--wiki-muted);
  font-size: 12px;
  font-weight: 650;
  line-height: 1.55;
  padding: 5px 8px 5px 12px;
  text-align: left;
  transition: background-color 0.16s ease, color 0.16s ease;
}

.wiki-side-toc-item.level-2 { padding-left: 18px; color: #74808d; }
.wiki-side-toc-item.level-3 { padding-left: 28px; color: #87919d; }
.wiki-side-toc-item.level-4,
.wiki-side-toc-item.level-5,
.wiki-side-toc-item.level-6 { display: none; }

.wiki-side-toc-item:hover {
  color: #4f5f6f;
}

.wiki-side-toc-item.active {
  color: var(--wiki-heading);
  font-weight: 900;
}

.wiki-side-toc-item.active::before {
  position: absolute;
  left: 1px;
  top: 7px;
  bottom: 7px;
  width: 2px;
  border-radius: 999px;
  background: var(--wiki-accent);
  content: "";
}

.wiki-side-empty {
  color: var(--wiki-muted);
  font-size: 12px;
  padding: 8px 0;
}

:deep(.article-prose h1),
:deep(.article-prose h2),
:deep(.article-prose h3) {
  scroll-margin-top: calc(var(--wiki-sticky-top) + 12px);
}

@media (max-width: 1500px) {
  .wiki-article-layout {
    grid-template-columns: minmax(290px, 0.72fr) minmax(0, 860px) minmax(220px, 0.58fr);
    column-gap: 22px;
  }

  .wiki-reading-sidebar {
    width: min(100%, 240px);
  }

  .wiki-left-sidebar {
    width: min(100%, 280px);
  }
}

@media (max-width: 1280px) {
  .wiki-article-layout {
    grid-template-columns: minmax(0, 1fr);
    max-width: 860px;
    padding: 28px 20px 70px;
  }

  .wiki-article-main-column {
    grid-column: 1;
  }

  :deep(.article-floating-actions),
  .wiki-left-rail,
  .wiki-right-rail,
  .wiki-left-sidebar,
  .wiki-reading-sidebar {
    display: none;
  }
}

@media (max-width: 768px) {
  :global(html) {
    scroll-padding-top: 72px;
  }

  .wiki-article-layout {
    max-width: none;
    padding: 18px 12px 96px;
  }

  .wiki-article-main-column {
    max-width: none;
  }

  .wiki-article-comments {
    padding-bottom: 86px;
  }

  .mobile-action-bar {
    position: fixed;
    left: 24px;
    right: 24px;
    bottom: calc(env(safe-area-inset-bottom) + 16px);
    z-index: 70;
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    border: 1px solid rgba(232, 222, 208, 0.9);
    border-radius: 999px;
    background: rgba(255, 253, 249, 0.94);
    padding: 10px 8px;
    box-shadow: 0 16px 44px rgba(73, 52, 24, 0.16);
    backdrop-filter: blur(18px);
  }

  .mobile-action-bar button {
    display: flex;
    min-width: 0;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 4px;
    border-radius: 999px;
    color: #72665a;
    font-size: 11px;
    font-weight: 800;
    line-height: 1;
    padding: 8px 0;
    transition: background-color 0.16s ease, color 0.16s ease;
  }

  .mobile-action-bar i {
    font-size: 16px;
  }

  .mobile-action-bar button.active,
  .mobile-action-bar button:active {
    background: #f7f0e6;
    color: #8f6428;
  }

  .mobile-top-button {
    position: fixed;
    right: 24px;
    bottom: calc(env(safe-area-inset-bottom) + 110px);
    z-index: 68;
    display: grid;
    height: 44px;
    width: 44px;
    place-items: center;
    border: 1px solid #e8ded0;
    border-radius: 999px;
    background: #fffdf9;
    color: #8f6428;
    box-shadow: 0 12px 34px rgba(73, 52, 24, 0.16);
  }
}

.mobile-sheet-layer {
  position: fixed;
  inset: 0;
  z-index: 90;
  display: none;
  align-items: flex-end;
  background: rgba(15, 23, 42, 0.34);
  backdrop-filter: blur(3px);
}

.mobile-sheet,
.mobile-more-sheet {
  width: 100%;
  border: 1px solid #e8ded0;
  border-bottom: 0;
  border-radius: 24px 24px 0 0;
  background: #fffdf9;
  box-shadow: 0 -18px 60px rgba(73, 52, 24, 0.18);
}

.mobile-sheet {
  max-height: min(76vh, 640px);
  overflow: hidden;
  padding: 8px 16px 18px;
}

.mobile-more-sheet {
  padding: 10px 22px calc(env(safe-area-inset-bottom) + 22px);
}

.mobile-sheet-handle {
  margin: 0 auto 10px;
  height: 4px;
  width: 42px;
  border-radius: 999px;
  background: #d8c4a8;
}

.mobile-sheet-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  border-bottom: 1px solid #efe3d2;
  padding-bottom: 12px;
}

.mobile-sheet-header h2 {
  color: #201b17;
  font-size: 18px;
  font-weight: 900;
}

.mobile-sheet-header p {
  margin-top: 3px;
  color: #6b6258;
  font-size: 12px;
}

.mobile-sheet-header button {
  display: grid;
  height: 34px;
  width: 34px;
  place-items: center;
  border-radius: 999px;
  background: #f7f0e6;
  color: #72665a;
}

.mobile-toc-list {
  max-height: calc(min(76vh, 640px) - 92px);
  overflow-y: auto;
  padding: 12px 0 6px;
}

.mobile-toc-item {
  position: relative;
  display: block;
  width: 100%;
  border-radius: 12px;
  color: #6b6258;
  font-size: 14px;
  font-weight: 700;
  line-height: 1.55;
  margin-bottom: 6px;
  padding: 9px 12px;
  text-align: left;
}

.mobile-toc-item.level-3 { padding-left: 28px; font-size: 13px; }
.mobile-toc-item.level-4 { padding-left: 44px; font-size: 12px; color: #8a7d70; }
.mobile-toc-item.level-5 { padding-left: 58px; font-size: 12px; color: #8a7d70; }
.mobile-toc-item.level-6 { padding-left: 72px; font-size: 12px; color: #8a7d70; }

.mobile-toc-item.active {
  background: #f7f0e6;
  color: #8f6428;
}

.mobile-toc-item.active::before {
  position: absolute;
  left: 0;
  top: 10px;
  bottom: 10px;
  width: 3px;
  border-radius: 999px;
  background: #c8a36d;
  content: '';
}

.mobile-copy-box {
  display: flex;
  gap: 8px;
  border: 1px solid #e8ded0;
  border-radius: 14px;
  background: #f7f0e6;
  margin-top: 14px;
  padding: 8px;
}

.mobile-copy-box input {
  min-width: 0;
  flex: 1;
  background: transparent;
  color: #6b6258;
  font-size: 13px;
  outline: none;
}

.mobile-copy-box button {
  border-radius: 10px;
  background: #8f6428;
  color: #fff;
  font-size: 13px;
  font-weight: 900;
  padding: 0 14px;
}

.mobile-more-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 18px 22px;
}

.mobile-more-grid button {
  display: flex;
  min-height: 76px;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border-radius: 18px;
  color: #5f5145;
  font-size: 13px;
  font-weight: 800;
}

.mobile-more-grid button span {
  display: grid;
  height: 44px;
  width: 44px;
  place-items: center;
  border: 1px solid #e8ded0;
  border-radius: 16px;
  background: #fffdf9;
  box-shadow: 0 8px 22px rgba(73, 52, 24, 0.06);
}

.mobile-more-grid button.active,
.mobile-more-grid button:active {
  color: #8f6428;
}

.mobile-sheet-fade-enter-active,
.mobile-sheet-fade-leave-active {
  transition: opacity 0.18s ease;
}

.mobile-sheet-fade-enter-active .mobile-sheet,
.mobile-sheet-fade-leave-active .mobile-sheet,
.mobile-sheet-fade-enter-active .mobile-more-sheet,
.mobile-sheet-fade-leave-active .mobile-more-sheet {
  transition: transform 0.2s ease;
}

.mobile-sheet-fade-enter-from,
.mobile-sheet-fade-leave-to {
  opacity: 0;
}

.mobile-sheet-fade-enter-from .mobile-sheet,
.mobile-sheet-fade-leave-to .mobile-sheet,
.mobile-sheet-fade-enter-from .mobile-more-sheet,
.mobile-sheet-fade-leave-to .mobile-more-sheet {
  transform: translateY(100%);
}

@media (max-width: 768px) {
  .mobile-sheet-layer {
    display: flex;
  }
}

:global(html.dark .wiki-article-page),
:global(html.dark .wiki-article-layout) {
  background: #11151b !important;
  color: #e8e2d8 !important;
}

:global(html.dark .wiki-article-page .bg-\[\#FCFAF9\]),
:global(html.dark .wiki-article-page .bg-\[\#fffdf9\]) {
  background-color: #1b2027 !important;
}

:global(html.dark .wiki-article-page .bg-\[\#f7f0e6\]) {
  background-color: #22252b !important;
}

:global(html.dark .wiki-article-page .border-\[\#e8ded0\]) {
  border-color: #3a332a !important;
}

:global(html.dark .wiki-article-page .text-\[\#201b17\]),
:global(html.dark .wiki-article-page .text-\[\#0f172a\]) {
  color: #f2eadf !important;
}

:global(html.dark .wiki-article-page .text-\[\#6b6258\]) {
  color: #afa79c !important;
}

:global(html.dark .wiki-article-page .bg-\[\#fffdf9\]) {
  border-color: #3a332a;
  background: #1b2027 !important;
  box-shadow: 0 18px 44px rgba(0, 0, 0, 0.22);
}

:global(html.dark) .wiki-left-card {
  background: transparent !important;
  box-shadow: none;
}

:global(html.dark) .wiki-left-head h2,
:global(html.dark) .wiki-side-title {
  color: #f2eadf;
}

:global(html.dark) .wiki-left-icon,
:global(html.dark) .wiki-tip-visual {
  border-color: #3a332a;
  background: linear-gradient(135deg, #22252b, #2c2a23) !important;
  color: #79c7ae;
}

:global(html.dark) .wiki-left-head p,
:global(html.dark) .wiki-side-toc-item {
  color: #afa79c;
}

:global(html.dark) .wiki-left-search {
  border-color: #3a332a;
  background: #22252b !important;
  color: #8f9aa6;
}

:global(html.dark) .wiki-left-search input {
  color: #e8e2d8;
}

:global(html.dark) .wiki-catalog-item {
  color: #a8b3be;
}

:global(html.dark) .wiki-catalog-item:hover {
  background: rgba(95, 168, 142, 0.12);
  color: #bfe7da;
}

:global(html.dark) .wiki-catalog-item.active {
  background: rgba(95, 168, 142, 0.16);
  color: #79c7ae;
}

:global(html.dark) .wiki-catalog-count {
  background: rgba(95, 168, 142, 0.16);
  color: #79c7ae;
}

:global(html.dark) .wiki-catalog-dot {
  background: #79c7ae;
}

:global(html.dark) .wiki-side-outline {
  border-left-color: rgba(121, 199, 174, 0.28);
}

:global(html.dark) .wiki-side-toc-item.level-3,
:global(html.dark) .wiki-side-toc-item.level-4,
:global(html.dark) .wiki-side-toc-item.level-5,
:global(html.dark) .wiki-side-toc-item.level-6 {
  color: #948b82;
}

:global(html.dark) .wiki-side-toc-item:hover,
:global(html.dark) .wiki-side-toc-item.active {
  background: transparent;
  color: #79c7ae;
}

:global(html.dark) .wiki-side-toc-item.active::before {
  background: #79c7ae;
}

:global(html.dark) .mobile-action-bar,
:global(html.dark) .mobile-top-button,
:global(html.dark) .mobile-sheet,
:global(html.dark) .mobile-more-sheet,
:global(html.dark) .mobile-more-grid button span {
  border-color: #3a332a;
  background: #1b2027;
  color: #e8e2d8;
}

:global(html.dark) .mobile-action-bar button,
:global(html.dark) .mobile-sheet-header p,
:global(html.dark) .mobile-toc-item,
:global(html.dark) .mobile-copy-box input,
:global(html.dark) .mobile-more-grid button {
  color: #afa79c;
}

:global(html.dark) .mobile-action-bar button.active,
:global(html.dark) .mobile-action-bar button:active,
:global(html.dark) .mobile-toc-item.active {
  background: rgba(214, 181, 116, 0.14);
  color: #d6b574;
}

:global(html.dark) .mobile-sheet-header {
  border-color: #3a332a;
}

:global(html.dark) .mobile-sheet-header h2 {
  color: #f2eadf;
}

:global(html.dark) .mobile-sheet-header button,
:global(html.dark) .mobile-copy-box {
  border-color: #3a332a;
  background: #22252b;
  color: #e8e2d8;
}

:global(.wiki-article-page .article-prose .snippet-comment-highlight) {
  border-bottom: 2px solid rgba(47, 143, 123, 0.68);
  background: rgba(167, 243, 208, 0.38);
  border-radius: 3px;
  cursor: pointer;
}

:global(.wiki-article-page .article-prose .snippet-comment-highlight:hover) {
  background: rgba(167, 243, 208, 0.58);
}

:global(html.dark .wiki-article-page .article-prose .snippet-comment-highlight) {
  border-bottom-color: rgba(121, 199, 174, 0.78);
  background: rgba(95, 168, 142, 0.2);
  color: #f0f6fc !important;
}

:global(html.dark .wiki-article-page .article-prose .snippet-comment-highlight:hover) {
  background: rgba(95, 168, 142, 0.28);
  color: #ffffff !important;
}
</style>

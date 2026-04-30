<template>
  <div class="article-detail-page min-h-screen bg-[#f8fafc] text-[#0f172a]">
    <Header />

    <main class="article-detail-layout">
      <ArticleToc
        :title="article.title"
        :items="tocItems"
        :active-id="activeHeadingId"
        :auto-expand="tocAutoExpand"
        :article-tree="articleTree"
        :current-article-id="articleIdNumber"
        @navigate="scrollToHeading"
        @article="goArticleDetail"
      />

      <section class="article-detail-main-column">
        <ArticleContent
          v-if="!loading"
          ref="articleContentRef"
          :article="article"
          :rendered-content="renderedContent"
          @go-tag="goTagArticleListPage"
          @go-category="goCategoryArticleListPage"
        >
          <AuthorCard :article="article" />
          <AiSummaryCard :article-id="articleIdNumber" :content="article.content" :ready="articleReady" />
        </ArticleContent>

        <div v-else class="space-y-5 rounded-2xl border border-[#e5e7eb] bg-white p-8 shadow-[0_12px_40px_rgba(15,23,42,.08)]">
          <div class="h-7 w-24 animate-pulse rounded-full bg-slate-100"></div>
          <div class="h-12 w-4/5 animate-pulse rounded-xl bg-slate-100"></div>
          <div class="h-5 w-80 animate-pulse rounded-full bg-slate-100"></div>
          <div class="h-28 animate-pulse rounded-2xl bg-slate-100"></div>
          <div class="h-72 animate-pulse rounded-2xl bg-slate-100"></div>
        </div>

        <div ref="commentSectionRef" class="article-detail-comments mt-12 space-y-6 pb-16">
          <MessageWallForm
            :router-url="commentRouterUrl"
            title="发表评论"
            collapsible
            @comment-published="handleArticleCommentPublished"
          />
          <MessageWallPanel
            ref="articleCommentsPanelRef"
            :router-url="commentRouterUrl"
            title="全部评论"
          />
        </div>
      </section>

      <ShareDrawer
        :article="article"
        :url="shareUrl"
        :excerpt="plainExcerpt"
        :visible="activePanel === 'share' && !isMobileViewport"
        @close="closeShare"
        @copy="copyShareLink"
      />

      <NotesDrawer
        :visible="notesVisible"
        @close="closeNotes"
        @bookmark="toggleBookmark"
      />

      <FloatingActionBar
        :active-panel="activePanel"
        :liked="liked"
        :bookmarked="bookmarked"
        :like-count="likeCount"
        @toggle-panel="togglePanel"
        @like="toggleLike"
        @bookmark="toggleBookmark"
        @top="scrollToTop"
        @comment="scrollToComments"
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
                  <h2>文章目录</h2>
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
                  <h2>分享文章</h2>
                  <p>复制链接，或选择一个分享方式</p>
                </div>
                <button @click="closeMobileShare" aria-label="关闭"><i class="fas fa-times"></i></button>
              </div>
              <div class="mobile-copy-box">
                <input :value="shareUrl" readonly>
                <button @click="copyShareLink">复制</button>
              </div>
              <div class="mobile-share-grid">
                <button v-for="item in mobileShareActions" :key="item.label">
                  <i :class="item.icon"></i>
                  <span>{{ item.label }}</span>
                </button>
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
                <button :class="{ active: liked }" @click="handleMobileLike">
                  <span><i :class="liked ? 'fas fa-thumbs-up' : 'far fa-thumbs-up'"></i></span>
                  <em>点赞</em>
                </button>
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
                <button @click="showMessage('字体设置稍后开放', 'info')">
                  <span><i class="fas fa-font"></i></span>
                  <em>字体</em>
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

      <nav class="mobile-action-bar" aria-label="文章操作">
        <button @click="mobileTocVisible = true">
          <i class="fas fa-list-ul"></i>
          <span>目录</span>
        </button>
        <button class="mobile-comment-action" @click="scrollToComments">
          <i class="far fa-comment-dots"></i>
          <em>{{ commentCount }}</em>
          <span>评论</span>
        </button>
        <button :class="{ active: bookmarked }" @click="toggleBookmark">
          <i :class="bookmarked ? 'fas fa-star' : 'far fa-star'"></i>
          <span>收藏</span>
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
import ArticleToc from '@/components/article-detail/ArticleToc.vue'
import ArticleContent from '@/components/article-detail/ArticleContent.vue'
import AuthorCard from '@/components/article-detail/AuthorCard.vue'
import ShareDrawer from '@/components/article-detail/ShareDrawer.vue'
import NotesDrawer from '@/components/article-detail/NotesDrawer.vue'
import FloatingActionBar from '@/components/article-detail/FloatingActionBar.vue'
import SelectionToolbar from '@/components/article-detail/SelectionToolbar.vue'
import SnippetAiPanel from '@/components/article-detail/SnippetAiPanel.vue'
import SnippetCommentPanel from '@/components/article-detail/SnippetCommentPanel.vue'
import AiSummaryCard from '@/components/AiSummaryCard.vue'
import MessageWallForm from '@/components/MessageWallForm.vue'
import MessageWallPanel from '@/components/MessageWallPanel.vue'
import { getArticleDetail, getArticlePageList, clearArticleDetailCache } from '@/api/frontend/article'
import { showMessage } from '@/composables/util'

const route = useRoute()
const router = useRouter()

const article = ref({})
const loading = ref(true)
const articleReady = ref(false)
const activePanel = ref('')
const notesVisible = ref(false)
const tocAutoExpand = ref(false)
const activeHeadingId = ref('')
const liked = ref(false)
const bookmarked = ref(false)
const likeCount = ref(23)
const articleContentRef = ref(null)
const articleNavList = ref([])
const commentSectionRef = ref(null)
const articleCommentsPanelRef = ref(null)
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

const mobileShareActions = [
  { label: '微信', icon: 'fab fa-weixin text-green-500' },
  { label: '微博', icon: 'fab fa-weibo text-red-500' },
  { label: 'QQ', icon: 'fab fa-qq text-blue-500' },
  { label: '二维码', icon: 'fas fa-qrcode text-slate-600' },
  { label: '海报', icon: 'far fa-image text-slate-600' }
]

const articleIdNumber = computed(() => Number(route.params.articleId || article.value.id || 0))
const commentRouterUrl = computed(() => `/article/${articleIdNumber.value || route.params.articleId}`)
const shareUrl = computed(() => window.location.href)
const storageKey = computed(() => `article_actions_${route.params.articleId}`)
const snippetCommentKey = computed(() => `article_snippet_comments_${route.params.articleId}`)
const commentCount = computed(() => Number(article.value.commentCount || article.value.commentNum || article.value.comments || article.value.commentTotal || 0))

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

const tocItems = computed(() => {
  const html = renderedContent.value || ''
  const matches = [...html.matchAll(/<h([2-4])([^>]*)>(.*?)<\/h\1>/gi)]
  const items = matches
    .map((match, index) => {
      const idMatch = match[2].match(/id=["']?([^"'>\s]+)["']?/i)
      return {
        id: idMatch?.[1] || `heading-${index}`,
        level: Number(match[1]),
        title: match[3].replace(/<[^>]+>/g, '').trim()
      }
    })
    .filter(item => item.title)

  if (items.length) return items
  return [
    { id: 'intro', level: 2, title: '引言：ChatGPT很好，但贵是个问题' },
    { id: 'principle', level: 2, title: '一、核心原理：API按量计费 vs 固定月费' },
    { id: 'prepare', level: 2, title: '二、准备工作：所需工具' },
    { id: 'steps', level: 2, title: '三、实操步骤：从注册到接入' },
    { id: 'tips', level: 2, title: '四、省钱技巧：如何把成本降到5元以内' }
  ]
})

const fallbackArticleTree = computed(() => [
  {
    id: 'category-root',
    title: '分类',
    kind: 'group',
    children: [
      {
        id: 'category-backend',
        title: '后端开发',
        kind: 'folder',
        children: [
          { id: 'sample-aspnet', articleId: article.value.id || route.params.articleId || 1, title: 'ASP.NET Core 8.0 全栈博客系统', kind: 'article' },
          { id: 'sample-springboot', articleId: 'springboot-demo', title: 'SpringBoot 项目实战', kind: 'article' },
          { id: 'sample-fastapi', articleId: 'fastapi-demo', title: 'FastAPI 入门教程', kind: 'article' }
        ]
      },
      {
        id: 'category-frontend',
        title: '前端开发',
        kind: 'folder',
        children: [
          { id: 'sample-vue3', articleId: 'vue3-demo', title: 'Vue3 博客前台设计', kind: 'article' },
          { id: 'sample-react', articleId: 'react-demo', title: 'React 管理后台布局', kind: 'article' }
        ]
      },
      {
        id: 'category-db',
        title: '数据库',
        kind: 'folder',
        children: [
          { id: 'sample-mysql', articleId: 'mysql-demo', title: 'MySQL 索引优化', kind: 'article' },
          { id: 'sample-redis', articleId: 'redis-demo', title: 'Redis 缓存设计', kind: 'article' }
        ]
      }
    ]
  },
  {
    id: 'tag-root',
    title: '标签',
    kind: 'group',
    children: [
      {
        id: 'tag-python',
        title: 'python',
        kind: 'folder',
        children: [
          { id: 'sample-chatgpt', articleId: article.value.id || route.params.articleId || 1, title: '如何实现6块钱以内使用官方正版ChatGPT', kind: 'article' },
          { id: 'sample-fastapi-api', articleId: 'fastapi-api-demo', title: 'FastAPI 接口开发', kind: 'article' }
        ]
      },
      {
        id: 'tag-aspnet',
        title: 'Asp.Net Core',
        kind: 'folder',
        children: [
          { id: 'sample-aspnet-tag', articleId: article.value.id || route.params.articleId || 1, title: 'ASP.NET Core 8.0 全栈博客系统', kind: 'article' },
          { id: 'sample-sqlsugar', articleId: 'sqlsugar-demo', title: 'SqlSugar 使用笔记', kind: 'article' }
        ]
      },
      {
        id: 'tag-springboot',
        title: 'SpringBoot',
        kind: 'folder',
        children: [
          { id: 'sample-spring-login', articleId: 'spring-login-demo', title: 'SpringBoot 登录认证', kind: 'article' },
          { id: 'sample-spring-deploy', articleId: 'spring-deploy-demo', title: 'SpringBoot 项目部署', kind: 'article' }
        ]
      }
    ]
  }
])

const articleTree = computed(() => {
  if (!articleNavList.value.length) return fallbackArticleTree.value
  const categoryMap = new Map()
  const tagMap = new Map()

  articleNavList.value.forEach((item) => {
    const articleId = item.id || item.articleId
    if (!articleId) return
    const title = item.title || '未命名文章'
    const category = item.category || { id: item.categoryId || 'uncategorized', name: item.categoryName || '未分类' }
    const categoryId = `category-${category.id || category.name}`
    if (!categoryMap.has(categoryId)) {
      categoryMap.set(categoryId, { id: categoryId, title: category.name || '未分类', kind: 'folder', children: [] })
    }
    categoryMap.get(categoryId).children.push({ id: `category-article-${articleId}`, articleId, title, kind: 'article' })

    const tags = item.tags?.length ? item.tags : [{ id: item.tagId || 'untagged', name: item.tagName || '未标记' }]
    tags.forEach((tag) => {
      const tagId = `tag-${tag.id || tag.name}`
      if (!tagMap.has(tagId)) {
        tagMap.set(tagId, { id: tagId, title: tag.name || '未标记', kind: 'folder', children: [] })
      }
      tagMap.get(tagId).children.push({ id: `tag-article-${tagId}-${articleId}`, articleId, title, kind: 'article' })
    })
  })

  return [
    { id: 'category-root', title: '分类', kind: 'group', children: [...categoryMap.values()] },
    { id: 'tag-root', title: '标签', kind: 'group', children: [...tagMap.values()] }
  ]
})

const loadArticleNav = async () => {
  try {
    const res = await getArticlePageList({ current: 1, size: 50 })
    if (res.success) {
      articleNavList.value = res.data?.list || []
    }
  } catch (error) {
    console.warn('load article navigation failed:', error)
  }
}

const loadArticle = async (articleId) => {
  if (!articleId) return
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
    likeCount.value = Number(article.value.readNum || 23)
    loadActionState()
    loading.value = false
    await nextTick()
    decorateArticle()
    loadSnippetComments()
    applySnippetHighlights()
    articleReady.value = true
  } catch (error) {
    console.error('load article detail failed:', error)
    showMessage('文章加载失败，请检查接口服务', 'error')
    loading.value = false
    articleReady.value = false
  }
}

const decorateArticle = () => {
  document.querySelectorAll('.article-prose pre code').forEach((block) => hljs.highlightElement(block))
  document.querySelectorAll('.article-prose pre').forEach((pre) => {
    if (pre.querySelector('.code-copy-btn')) return
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
    pre.appendChild(button)
  })
  document.querySelectorAll('.article-prose h2, .article-prose h3, .article-prose h4').forEach((heading, index) => {
    if (!heading.id) heading.id = `heading-${index}`
  })
  assignSnippetAnchors()
  updateActiveHeading()
}

const updateActiveHeading = () => {
  if (window.scrollY > 20) {
    tocAutoExpand.value = true
  }
  showMobileTopButton.value = window.scrollY > 520
  const headings = [...document.querySelectorAll('.article-prose h2, .article-prose h3, .article-prose h4')]
  const current = headings.filter((heading) => heading.getBoundingClientRect().top <= 120).pop()
  activeHeadingId.value = current?.id || tocItems.value[0]?.id || ''
}

const scrollToHeading = (id) => {
  const heading = document.getElementById(id)
  if (!heading) return
  tocAutoExpand.value = true
  activeHeadingId.value = id
  const headerOffset = isMobileViewport.value ? 76 : 96
  const targetTop = heading.getBoundingClientRect().top + window.scrollY - headerOffset
  window.scrollTo({
    top: Math.max(0, targetTop),
    behavior: 'smooth'
  })
}

const scrollToHeadingFromMobile = (id) => {
  scrollToHeading(id)
  mobileTocVisible.value = false
}

const loadActionState = () => {
  const state = JSON.parse(localStorage.getItem(storageKey.value) || '{}')
  liked.value = !!state.liked
  bookmarked.value = !!state.bookmarked
  likeCount.value = Number(state.likeCount || likeCount.value || 23)
}

const getProseRoot = () => document.querySelector('.article-prose')

const getSnippetBlocks = () => [...document.querySelectorAll('.article-prose p, .article-prose li, .article-prose blockquote, .article-prose td')]
  .filter((node) => !node.closest('pre'))

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
  document.querySelectorAll('.snippet-comment-highlight').forEach((node) => {
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
  return {
    anchorId: anchorIndex,
    paragraphIndex: anchorIndex ? Number(anchorIndex) : -1,
    startOffset: blockText.indexOf(text),
    endOffset: blockText.indexOf(text) + text.length
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
  window.open(`https://www.baidu.com/s?wd=${encodeURIComponent(selectedText.value)}`, '_blank', 'noopener,noreferrer')
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
  snippetAiVisible.value = true
  snippetCommentVisible.value = false
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
    articleId: article.value.id || route.params.articleId,
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

const saveActionState = () => {
  localStorage.setItem(storageKey.value, JSON.stringify({
    liked: liked.value,
    bookmarked: bookmarked.value,
    likeCount: likeCount.value
  }))
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

const toggleLike = () => {
  liked.value = !liked.value
  likeCount.value += liked.value ? 1 : -1
  saveActionState()
  showMessage(liked.value ? '已点赞' : '已取消点赞', 'success')
}

const toggleBookmark = () => {
  bookmarked.value = !bookmarked.value
  saveActionState()
  showMessage(bookmarked.value ? '已收藏' : '已取消收藏', 'success')
}

const copyShareLink = async () => {
  await navigator.clipboard?.writeText(shareUrl.value)
  showMessage('链接已复制', 'success')
}

const toggleMobileTheme = () => {
  document.documentElement.classList.toggle('dark')
  mobileMoreVisible.value = false
}

const handleMobileLike = () => {
  toggleLike()
  mobileMoreVisible.value = false
}

const handleMobileShare = () => {
  mobileMoreVisible.value = false
  openMobileShare()
}

const handleMobileNotes = () => {
  mobileMoreVisible.value = false
  togglePanel('notes')
}

const handleMobileCopyLink = async () => {
  await copyShareLink()
  mobileMoreVisible.value = false
}

const handleArticleCommentPublished = () => {
  articleCommentsPanelRef.value?.refresh?.()
}

const scrollToTop = () => window.scrollTo({ top: 0, behavior: 'smooth' })

const scrollToComments = () => {
  mobileTocVisible.value = false
  mobileShareVisible.value = false
  mobileMoreVisible.value = false
  commentSectionRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

const openMobileShare = () => {
  mobileShareVisible.value = true
  notesVisible.value = false
  activePanel.value = 'share'
}

const updateViewportState = () => {
  isMobileViewport.value = window.innerWidth <= 768
  if (!isMobileViewport.value) {
    mobileTocVisible.value = false
    mobileShareVisible.value = false
    mobileMoreVisible.value = false
  }
}

const goArticleDetail = (id) => {
  if (!id || String(id).includes('demo')) return
  router.push({ path: `/article/${id}` })
}

const goCategoryArticleListPage = (id, name) => router.push({ path: '/category/article/list', query: { id, name } })
const goTagArticleListPage = (id, name) => router.push({ path: '/tag/article/list', query: { id, name } })

onMounted(() => {
  updateViewportState()
  loadArticle(route.params.articleId)
  loadArticleNav()
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

watch(() => route.params.articleId, (id) => {
  snippetAiVisible.value = false
  snippetCommentVisible.value = false
  mobileTocVisible.value = false
  mobileShareVisible.value = false
  mobileMoreVisible.value = false
  hideSelectionToolbar()
  loadArticle(id)
})
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

.article-detail-layout {
  display: grid;
  grid-template-columns: 280px minmax(0, 1fr);
  align-items: start;
  column-gap: 24px;
  width: 100%;
  max-width: 1680px;
  margin: 0 auto;
  padding: 42px 156px 80px 28px;
}

.article-detail-main-column {
  min-width: 0;
  width: 100%;
  max-width: 960px;
  justify-self: center;
}

:deep(.article-toc) {
  position: sticky;
  top: 88px;
  align-self: start;
  width: 280px;
  max-height: calc(100vh - 88px - 24px);
}

.article-detail-comments {
  width: 100%;
}

@media (max-width: 1500px) {
  .article-detail-layout {
    grid-template-columns: 260px minmax(0, 1fr);
    column-gap: 22px;
    padding-right: 132px;
  }

  :deep(.article-toc) {
    width: 260px;
  }

}

@media (max-width: 1280px) {
  .article-detail-layout {
    grid-template-columns: minmax(0, 1fr);
    max-width: 860px;
    padding: 28px 20px 70px;
  }

  :deep(.article-toc),
  :deep(.article-floating-actions) {
    display: none;
  }
}

@media (max-width: 768px) {
  :global(html) {
    scroll-padding-top: 72px;
  }

  .article-detail-layout {
    max-width: none;
    padding: 18px 12px 96px;
  }

  .article-detail-main-column {
    max-width: none;
  }

  .article-detail-comments {
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
    gap: 0;
    border: 1px solid rgba(226, 232, 240, 0.9);
    border-radius: 999px;
    background: rgba(255, 255, 255, 0.94);
    padding: 10px 8px;
    box-shadow: 0 16px 44px rgba(15, 23, 42, 0.16);
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
    color: #64748b;
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
    background: #eff6ff;
    color: #2563eb;
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
    border: 1px solid #dbe3ee;
    border-radius: 999px;
    background: #ffffff;
    color: #2563eb;
    box-shadow: 0 12px 34px rgba(15, 23, 42, 0.16);
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

.mobile-sheet {
  width: 100%;
  max-height: min(76vh, 640px);
  overflow: hidden;
  border-radius: 24px 24px 0 0;
  border: 1px solid #e5e7eb;
  border-bottom: 0;
  background: #ffffff;
  padding: 8px 16px 18px;
  box-shadow: 0 -18px 60px rgba(15, 23, 42, 0.18);
}

.mobile-sheet-handle {
  margin: 0 auto 10px;
  height: 4px;
  width: 42px;
  border-radius: 999px;
  background: #cbd5e1;
}

.mobile-sheet-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  border-bottom: 1px solid #edf2f7;
  padding-bottom: 12px;
}

.mobile-sheet-header h2 {
  color: #0f172a;
  font-size: 18px;
  font-weight: 900;
}

.mobile-sheet-header p {
  margin-top: 3px;
  color: #64748b;
  font-size: 12px;
}

.mobile-sheet-header button {
  display: grid;
  height: 34px;
  width: 34px;
  place-items: center;
  border-radius: 999px;
  background: #f1f5f9;
  color: #64748b;
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
  color: #64748b;
  font-size: 14px;
  font-weight: 700;
  line-height: 1.55;
  margin-bottom: 6px;
  padding: 9px 12px;
  text-align: left;
}

.mobile-toc-item.level-3 {
  padding-left: 28px;
  font-size: 13px;
  font-weight: 650;
}

.mobile-toc-item.level-4 {
  padding-left: 44px;
  font-size: 12px;
  color: #94a3b8;
}

.mobile-toc-item.active {
  background: #f1f5f9;
  color: #334155;
}

.mobile-toc-item.active::before {
  position: absolute;
  left: 0;
  top: 10px;
  bottom: 10px;
  width: 3px;
  border-radius: 999px;
  background: #64748b;
  content: '';
}

.mobile-copy-box {
  display: flex;
  gap: 8px;
  border: 1px solid #e5e7eb;
  border-radius: 14px;
  background: #f8fafc;
  margin-top: 14px;
  padding: 8px;
}

.mobile-copy-box input {
  min-width: 0;
  flex: 1;
  background: transparent;
  color: #64748b;
  font-size: 13px;
  outline: none;
}

.mobile-copy-box button {
  border-radius: 10px;
  background: #2563eb;
  color: #fff;
  font-size: 13px;
  font-weight: 900;
  padding: 0 14px;
}

.mobile-share-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 10px;
  margin-top: 16px;
}

.mobile-share-grid button {
  display: flex;
  min-height: 72px;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border: 1px solid #e5e7eb;
  border-radius: 16px;
  background: #ffffff;
  color: #475569;
  font-size: 12px;
  font-weight: 800;
}

.mobile-share-grid i {
  font-size: 20px;
}

.mobile-more-sheet {
  width: 100%;
  border-radius: 24px 24px 0 0;
  border: 1px solid #e5e7eb;
  border-bottom: 0;
  background: #ffffff;
  padding: 10px 22px calc(env(safe-area-inset-bottom) + 22px);
  box-shadow: 0 -18px 60px rgba(15, 23, 42, 0.18);
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
  color: #475569;
  font-size: 13px;
  font-weight: 800;
}

.mobile-more-grid button span {
  display: grid;
  height: 44px;
  width: 44px;
  place-items: center;
  border: 1px solid #e5e7eb;
  border-radius: 16px;
  background: #ffffff;
  box-shadow: 0 8px 22px rgba(15, 23, 42, 0.06);
}

.mobile-more-grid button.active,
.mobile-more-grid button:active {
  color: #2563eb;
}

.mobile-comment-action {
  position: relative;
}

.mobile-comment-action em {
  position: absolute;
  top: 5px;
  left: 50%;
  min-width: 18px;
  border: 1px solid #2563eb;
  border-radius: 999px;
  background: #ffffff;
  color: #2563eb;
  font-size: 10px;
  font-style: normal;
  line-height: 14px;
  padding: 0 4px;
  transform: translateX(8px);
}

.mobile-sheet-fade-enter-active,
.mobile-sheet-fade-leave-active {
  transition: opacity 0.18s ease;
}

.mobile-sheet-fade-enter-active .mobile-sheet,
.mobile-sheet-fade-leave-active .mobile-sheet {
  transition: transform 0.2s ease;
}

.mobile-sheet-fade-enter-from,
.mobile-sheet-fade-leave-to {
  opacity: 0;
}

.mobile-sheet-fade-enter-from .mobile-sheet,
.mobile-sheet-fade-leave-to .mobile-sheet {
  transform: translateY(100%);
}

@media (max-width: 768px) {
  .mobile-sheet-layer {
    display: flex;
  }
}

:global(html.dark .article-detail-page) {
  background: #22272e !important;
  color: #adbac7 !important;
}

:global(html.dark) .mobile-header-btn,
:global(html.dark) .mobile-action-bar,
:global(html.dark) .mobile-top-button,
:global(html.dark) .mobile-sheet,
:global(html.dark) .mobile-more-sheet,
:global(html.dark) .mobile-share-grid button,
:global(html.dark) .mobile-more-grid button span {
  border-color: #444c56;
  background: #2d333b;
  color: #c9d1d9;
}

:global(html.dark) .mobile-action-bar button,
:global(html.dark) .mobile-sheet-header p,
:global(html.dark) .mobile-toc-item,
:global(html.dark) .mobile-copy-box input {
  color: #adbac7;
}

:global(html.dark) .mobile-action-bar button.active,
:global(html.dark) .mobile-action-bar button:active,
:global(html.dark) .mobile-toc-item.active {
  background: rgba(56, 139, 253, 0.14);
  color: #58a6ff;
}

:global(html.dark) .mobile-sheet-header {
  border-color: #444c56;
}

:global(html.dark) .mobile-sheet-header h2 {
  color: #f0f6fc;
}

:global(html.dark) .mobile-sheet-header button,
:global(html.dark) .mobile-copy-box {
  border-color: #444c56;
  background: #22272e;
  color: #c9d1d9;
}

:global(html.dark .article-detail-layout) {
  background: #22272e;
}

:global(html.dark .article-detail-page .article-detail-main-column) {
  color: #adbac7;
}

:global(html.dark .article-detail-page .rounded-2xl),
:global(html.dark .article-detail-page .article-content-shell > div:not(.article-prose)) {
  border-color: #444c56 !important;
}

:global(html.dark .article-detail-page .bg-white) {
  background-color: #2d333b !important;
}

:global(html.dark .article-detail-page .bg-\[\#f8fafc\]) {
  background-color: #22272e !important;
}

:global(html.dark .article-detail-page .text-\[\#0f172a\]),
:global(html.dark .article-detail-page .text-\[\#111827\]) {
  color: #f0f6fc !important;
}

:global(html.dark .article-detail-page .text-\[\#64748b\]),
:global(html.dark .article-detail-page .text-slate-500),
:global(html.dark .article-detail-page .text-slate-600) {
  color: #94a3b8 !important;
}

:global(html.dark .article-detail-page .border-\[\#e5e7eb\]) {
  border-color: #444c56 !important;
}

:global(html.dark .article-detail-page .bg-slate-100),
:global(html.dark .article-detail-page .bg-slate-50) {
  background-color: #373e47 !important;
}

:global(html.dark .article-detail-page .hover\:bg-slate-50:hover),
:global(html.dark .article-detail-page .hover\:bg-slate-100:hover),
:global(html.dark .article-detail-page .hover\:bg-slate-200:hover) {
  background-color: #444c56 !important;
}

:global(html.dark .article-detail-page .shadow-sm),
:global(html.dark .article-detail-page .shadow-\[0_12px_40px_rgba\(15\,23\,42\,\.08\)\]) {
  box-shadow: 0 18px 50px rgba(0, 0, 0, 0.22) !important;
}

:global(html.dark .article-detail-page .article-prose) {
  color: #adbac7 !important;
}

:global(html.dark .article-detail-page .article-prose h1),
:global(html.dark .article-detail-page .article-prose h2),
:global(html.dark .article-detail-page .article-prose h3),
:global(html.dark .article-detail-page .article-prose h4) {
  color: #f0f6fc !important;
}

:global(html.dark .article-detail-page .article-prose p),
:global(html.dark .article-detail-page .article-prose li) {
  color: #adbac7 !important;
}

:global(html.dark .article-detail-page .article-prose table) {
  background: transparent;
  border-color: #444c56 !important;
}

:global(html.dark .article-detail-page .article-prose th),
:global(html.dark .article-detail-page .article-prose td) {
  border-color: #444c56 !important;
}

:global(html.dark .article-detail-page .article-prose th) {
  background: transparent;
  color: #adbac7 !important;
}

:global(html.dark .article-detail-page .article-prose tr) {
  background: #22272e !important;
}

:global(html.dark .article-detail-page .article-prose tr:nth-child(2n)) {
  background: #2d333b !important;
}

:global(html.dark .article-detail-page .article-prose pre) {
  background: #2d333b !important;
  border-color: #444c56 !important;
}

:global(html.dark .article-detail-page .article-prose code:not(pre code)) {
  background: rgba(110, 118, 129, 0.4) !important;
  color: #c9d1d9 !important;
}

:global(.article-prose .snippet-comment-highlight) {
  border-bottom: 2px solid rgba(234, 179, 8, 0.72);
  background: rgba(254, 240, 138, 0.42);
  border-radius: 3px;
  cursor: pointer;
}

:global(.article-prose .snippet-comment-highlight:hover) {
  background: rgba(254, 240, 138, 0.7);
}

:global(html.dark .article-prose .snippet-comment-highlight) {
  border-bottom-color: rgba(250, 204, 21, 0.78);
  background: rgba(234, 179, 8, 0.18);
}

:global(html.dark .article-prose .snippet-comment-highlight:hover) {
  background: rgba(234, 179, 8, 0.26);
}
</style>

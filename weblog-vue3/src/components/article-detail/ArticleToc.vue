<template>
  <div>
    <div class="toc-placeholder"></div>
    <aside class="article-toc article-toc--fixed border-r border-[#e5e7eb] bg-[#f8fafc] px-5 py-5">
      <div v-if="showTabs" class="toc-tabs">
        <button
          type="button"
          :class="['toc-tab', activeTab === 'articles' ? 'toc-tab-active' : '']"
          @click="activeTab = 'articles'"
        >
          {{ treeTabLabel }}
        </button>
        <button
          type="button"
          :class="['toc-tab', activeTab === 'outline' ? 'toc-tab-active' : '']"
          @click="activeTab = 'outline'"
        >
          大纲
        </button>
      </div>
      <div v-else class="toc-static-head">
        <div class="toc-static-title">{{ treeTabLabel }}</div>
        <p v-if="staticSubtitle" class="toc-static-subtitle">{{ staticSubtitle }}</p>
      </div>

      <nav v-if="activeTab === 'articles'" class="article-toc__scroll">
        <TreeNode
          v-for="node in articleTree"
          :key="node.id"
          :node="node"
          :active-id="currentArticleId"
          :expanded-ids="articleExpandedIds"
          :depth="0"
          mode="articles"
          @toggle="toggleArticleNode"
          @article="$emit('article', $event)"
        />
      </nav>

      <nav v-else class="article-toc__scroll">
        <TreeNode
          :node="outlineTree"
          :active-id="activeId"
          :expanded-ids="outlineExpandedIds"
          :depth="0"
          mode="outline"
          @toggle="toggleOutlineNode"
          @navigate="$emit('navigate', $event)"
        />
      </nav>
    </aside>
  </div>
</template>

<script setup>
import { computed, defineComponent, h, onBeforeUnmount, onMounted, ref, watch } from 'vue'

const props = defineProps({
  title: { type: String, default: '文章目录' },
  items: { type: Array, default: () => [] },
  activeId: { type: String, default: '' },
  autoExpand: { type: Boolean, default: false },
  articleTree: { type: Array, default: () => [] },
  currentArticleId: { type: [String, Number], default: '' },
  treeTabLabel: { type: String, default: '文章' },
  staticSubtitle: { type: String, default: '' },
  defaultTab: { type: String, default: 'outline' },
  defaultExpandedArticleIds: { type: Array, default: () => ['category-root', 'tag-root'] },
  showTabs: { type: Boolean, default: true }
})

defineEmits(['navigate', 'article'])

const activeTab = ref(props.defaultTab === 'articles' ? 'articles' : 'outline')
const outlineExpandedIds = ref(new Set())
const articleExpandedIds = ref(new Set(props.defaultExpandedArticleIds))

const outlineTree = computed(() => {
  const root = {
    id: '__root',
    title: props.title || '文章目录',
    level: 1,
    kind: 'root',
    children: []
  }
  const stack = [root]

  props.items.forEach((item) => {
    const node = { ...item, children: [], kind: 'heading' }
    while (stack.length > 1 && stack[stack.length - 1].level >= node.level) {
      stack.pop()
    }
    stack[stack.length - 1].children.push(node)
    stack.push(node)
  })

  return root
})

const toggleOutlineNode = (id) => {
  const next = new Set(outlineExpandedIds.value)
  next.has(id) ? next.delete(id) : next.add(id)
  outlineExpandedIds.value = next
}

const toggleArticleNode = (id) => {
  const next = new Set(articleExpandedIds.value)
  next.has(id) ? next.delete(id) : next.add(id)
  articleExpandedIds.value = next
}

const findPath = (node, id, path = []) => {
  if (String(node.id) === String(id)) return [...path, node.id]
  for (const child of node.children || []) {
    const found = findPath(child, id, [...path, node.id])
    if (found) return found
  }
  return null
}

watch(
  () => [props.activeId, props.autoExpand, props.items, props.title],
  () => {
    if (!props.autoExpand || !props.activeId) return
    const path = findPath(outlineTree.value, props.activeId)
    if (!path) return
    outlineExpandedIds.value = new Set(path)
  },
  { deep: true, immediate: true }
)

watch(
  () => [props.currentArticleId, props.articleTree],
  () => {
    if (!props.currentArticleId) return
    for (const root of props.articleTree) {
      const path = findPath(root, props.currentArticleId)
      if (path) {
        articleExpandedIds.value = new Set([...articleExpandedIds.value, ...path])
      }
    }
  },
  { deep: true, immediate: true }
)

watch(
  () => props.defaultExpandedArticleIds,
  (ids) => {
    articleExpandedIds.value = new Set([...articleExpandedIds.value, ...(ids || [])])
  },
  { deep: true, immediate: true }
)

watch(
  () => props.defaultTab,
  (tab) => {
    activeTab.value = tab === 'articles' ? 'articles' : 'outline'
  }
)

const TreeNode = defineComponent({
  name: 'TreeNode',
  props: {
    node: { type: Object, required: true },
    activeId: { type: [String, Number], default: '' },
    expandedIds: { type: Object, required: true },
    depth: { type: Number, default: 0 },
    mode: { type: String, default: 'outline' }
  },
  emits: ['toggle', 'navigate', 'article'],
  setup(nodeProps, { emit }) {
    const isExpanded = computed(() => nodeProps.expandedIds.has(nodeProps.node.id))
    const hasChildren = computed(() => !!nodeProps.node.children?.length)
    const isArticle = computed(() => nodeProps.node.kind === 'article')
    const isActive = computed(() => isArticle.value
      ? String(nodeProps.activeId) === String(nodeProps.node.articleId || nodeProps.node.id)
      : String(nodeProps.activeId) === String(nodeProps.node.id))
    const articleCount = computed(() => {
      const count = (node) => {
        if (node.kind === 'article') return 1
        return (node.children || []).reduce((total, child) => total + count(child), 0)
      }
      return isArticle.value ? 0 : count(nodeProps.node)
    })

    const rowClass = computed(() => [
      'toc-row',
      `toc-depth-${nodeProps.depth}`,
      `toc-kind-${nodeProps.node.kind || 'node'}`,
      isActive.value ? 'toc-row-active' : ''
    ])

    const rowStyle = computed(() => ({
      paddingLeft: `${nodeProps.depth * 16}px`
    }))

    const iconText = computed(() => {
      if (hasChildren.value) return isExpanded.value ? '⌄' : '›'
      if (isArticle.value) return '·'
      return ''
    })

    const toggle = (event) => {
      event.stopPropagation()
      if (hasChildren.value) emit('toggle', nodeProps.node.id)
    }

    const select = () => {
      if (isArticle.value && nodeProps.node.articleId) {
        emit('article', nodeProps.node.articleId)
        return
      }
      if (nodeProps.mode === 'outline' && nodeProps.node.id !== '__root') {
        emit('navigate', nodeProps.node.id)
        return
      }
      if (hasChildren.value) emit('toggle', nodeProps.node.id)
    }

    return () => h('div', { class: 'toc-node' }, [
      h('div', { class: rowClass.value, style: rowStyle.value }, [
        h('button', {
          class: ['toc-chevron', hasChildren.value ? '' : 'toc-chevron-leaf'],
          type: 'button',
          onClick: toggle
        }, iconText.value),
        h('button', {
          class: 'toc-title',
          type: 'button',
          title: nodeProps.node.title,
          onClick: select
        }, nodeProps.node.title),
        articleCount.value
          ? h('span', { class: 'toc-count' }, String(articleCount.value))
          : null
      ]),
      hasChildren.value && isExpanded.value
        ? h('div', { class: 'toc-children' }, nodeProps.node.children.map(child =>
            h(TreeNode, {
              key: child.id,
              node: child,
              activeId: nodeProps.activeId,
              expandedIds: nodeProps.expandedIds,
              depth: nodeProps.depth + 1,
              mode: nodeProps.mode,
              onToggle: id => emit('toggle', id),
              onNavigate: id => emit('navigate', id),
              onArticle: id => emit('article', id)
            })
          ))
        : null
    ])
  }
})

const updateFixedPosition = () => {
  const aside = document.querySelector('.article-toc')
  if (!aside) return
  const rect = aside.getBoundingClientRect()
  aside.style.left = rect.left + 'px'
}

onMounted(() => {
  updateFixedPosition()
  window.addEventListener('resize', updateFixedPosition, { passive: true })
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateFixedPosition)
})
</script>

<style scoped>
.toc-placeholder {
  width: 280px;
  flex-shrink: 0;
}

.article-toc--fixed {
  position: fixed;
  top: 72px;
  width: 280px;
  z-index: 40;
  height: calc(100vh - 72px);
  overflow-y: auto;
  border-color: #eee4d7;
  background: #fcfaf9;
}

@media (max-width: 1500px) {
  .article-toc--fixed {
    width: 260px;
  }
  .toc-placeholder {
    width: 260px;
  }
}

@media (max-width: 1280px) {
  .article-toc--fixed {
    display: none;
  }
  .toc-placeholder {
    display: none;
  }
}

.toc-tabs {
  display: grid;
  grid-template-columns: 1fr 1fr;
  margin-bottom: 18px;
  color: #7a6a5d;
  font-size: 14px;
  font-weight: 600;
  text-align: center;
}

.toc-tab {
  position: relative;
  height: 36px;
  color: inherit;
  transition: color 0.16s ease;
}

.toc-tab:hover {
  color: #8f6428;
}

.toc-tab-active {
  color: #201b17;
  font-weight: 800;
}

.toc-tab-active::after {
  position: absolute;
  right: 24%;
  bottom: 0;
  left: 24%;
  height: 3px;
  border-radius: 999px;
  background: #c8a36d;
  content: "";
}

.toc-static-head {
  margin-bottom: 16px;
  border-bottom: 1px solid #efe3d2;
  padding-bottom: 14px;
}

.toc-static-title {
  color: #201b17;
  font-size: 18px;
  font-weight: 900;
  letter-spacing: 0;
}

.toc-static-subtitle {
  margin-top: 5px;
  color: #8a7d70;
  font-size: 12px;
  line-height: 1.55;
}

.article-toc__scroll {
  max-height: calc(100vh - 88px - 24px - 72px);
  overflow-y: auto;
  padding: 2px 8px 16px 0;
  scrollbar-width: thin;
  scrollbar-color: #d8c4a8 transparent;
}

.toc-static-head + .article-toc__scroll {
  max-height: calc(100vh - 88px - 24px - 58px);
}

.article-toc__scroll::-webkit-scrollbar {
  width: 8px;
}

.article-toc__scroll::-webkit-scrollbar-track {
  background: transparent;
}

.article-toc__scroll::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: #d8c4a8;
}

:deep(.toc-row) {
  position: relative;
  display: flex;
  align-items: flex-start;
  gap: 8px;
  margin: 3px 0;
  border-left: 3px solid transparent;
  border-radius: 8px;
  padding-top: 6px;
  padding-right: 8px;
  padding-bottom: 6px;
  color: #4a4037;
  font-size: 14px;
  font-weight: 400;
  line-height: 1.65;
  transition: background-color 0.14s ease, border-color 0.14s ease, color 0.14s ease;
}

:deep(.toc-row:hover) {
  background: #f7f0e6;
  color: #201b17;
}

:deep(.toc-depth-0) {
  margin-top: 8px;
  color: #2a2a2a;
  font-size: 14px;
  font-weight: 700;
}

:deep(.toc-depth-1),
:deep(.toc-depth-2),
:deep(.toc-depth-3),
:deep(.toc-depth-4),
:deep(.toc-kind-article) {
  color: #5f554b;
  font-size: 14px;
  font-weight: 400;
}

:deep(.toc-depth-3),
:deep(.toc-depth-4),
:deep(.toc-depth-5),
:deep(.toc-depth-6) {
  color: #8a7d70;
}

:deep(.toc-row-active) {
  border-left-color: #c8a36d;
  background: #efe3d2;
  color: #8f6428;
  font-weight: 700;
}

:deep(.toc-chevron) {
  display: inline-grid;
  width: 18px;
  height: 24px;
  flex: 0 0 18px;
  place-items: center;
  color: currentColor;
  font-size: 14px;
  line-height: 1;
}

:deep(.toc-chevron-leaf) {
  font-size: 15px;
  opacity: 0.6;
}

:deep(.toc-title) {
  min-width: 0;
  flex: 1;
  overflow: hidden;
  color: inherit;
  line-height: 1.65;
  overflow-wrap: anywhere;
  text-align: left;
  white-space: normal;
}

:deep(.toc-count) {
  min-width: 22px;
  height: 20px;
  flex: 0 0 auto;
  border-radius: 999px;
  background: #f7f0e6;
  padding: 0 7px;
  color: #8a7d70;
  font-size: 12px;
  font-weight: 800;
  line-height: 20px;
  text-align: center;
}

:deep(.toc-row-active .toc-count) {
  background: #f8e8ca;
  color: #8f6428;
}

:global(html.dark) .article-toc {
  border-color: #2a313a;
  background: #151a20;
}

:global(html.dark) .toc-tabs {
  color: #afa79c;
}

:global(html.dark) .toc-tab:hover {
  color: #efd39a;
}

:global(html.dark) .toc-tab-active {
  color: #f2eadf;
}

:global(html.dark) .toc-tab-active::after {
  background: #d6b574;
}

:global(html.dark) .toc-static-head {
  border-color: #3a332a;
}

:global(html.dark) .toc-static-title {
  color: #f2eadf;
}

:global(html.dark) .toc-static-subtitle {
  color: #afa79c;
}

:global(html.dark) .article-toc :deep(.toc-row) {
  color: #cfc7bb;
}

:global(html.dark) .article-toc :deep(.toc-row:hover) {
  background: rgba(214, 181, 116, 0.1);
  color: #f2eadf;
}

:global(html.dark) .article-toc :deep(.toc-depth-0) {
  color: #e8e2d8;
}

:global(html.dark) .article-toc :deep(.toc-depth-1),
:global(html.dark) .article-toc :deep(.toc-depth-2),
:global(html.dark) .article-toc :deep(.toc-kind-article) {
  color: #cfc7bb;
}

:global(html.dark) .article-toc :deep(.toc-depth-3),
:global(html.dark) .article-toc :deep(.toc-depth-4),
:global(html.dark) .article-toc :deep(.toc-depth-5),
:global(html.dark) .article-toc :deep(.toc-depth-6) {
  color: #afa79c;
}

:global(html.dark) .article-toc :deep(.toc-row-active) {
  border-left-color: #d6b574;
  background: rgba(214, 181, 116, 0.14);
  color: #e3c680;
}

:global(html.dark) .article-toc :deep(.toc-count) {
  background: rgba(214, 181, 116, 0.1);
  color: #afa79c;
}

:global(html.dark) .article-toc :deep(.toc-row-active .toc-count) {
  background: rgba(214, 181, 116, 0.2);
  color: #e3c680;
}

:global(html.dark) .article-toc__scroll::-webkit-scrollbar-thumb {
  background: #514636;
}
</style>

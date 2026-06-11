<template>
  <div>
    <div class="toc-placeholder"></div>
    <aside class="article-toc article-toc--fixed">
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
      paddingLeft: `${7 + nodeProps.depth * 13}px`
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
  width: 290px;
  flex-shrink: 0;
}

.article-toc--fixed {
  position: fixed;
  top: 96px;
  width: 290px;
  z-index: 40;
  height: calc(100vh - 120px);
  max-height: calc(100vh - 120px);
  overflow-x: hidden;
  overflow-y: auto;
  border-right: 0;
  border-color: transparent;
  background: transparent;
  padding: 12px 10px 14px;
  color: #243142;
  font-family: "PingFang SC", "Microsoft YaHei", "HarmonyOS Sans SC", system-ui, sans-serif;
  scrollbar-color: rgba(17, 24, 39, 0.34) transparent;
  scrollbar-gutter: stable;
  scrollbar-width: thin;
}

.article-toc--fixed::-webkit-scrollbar {
  width: 6px;
}

.article-toc--fixed::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(17, 24, 39, 0.28);
}

@media (max-width: 1500px) {
  .article-toc--fixed {
    width: 280px;
  }
  .toc-placeholder {
    width: 280px;
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
  gap: 4px;
  margin-bottom: 14px;
  color: #667382;
  font-size: 12.5px;
  font-weight: 800;
  text-align: center;
}

.toc-tab {
  position: relative;
  height: 33px;
  border-radius: 10px;
  color: inherit;
  transition: background-color 0.16s ease, color 0.16s ease;
}

.toc-tab:hover {
  background: rgba(17, 24, 39, 0.06);
  color: #111827;
}

.toc-tab-active {
  background: #f0f1f3;
  color: #111827;
  font-weight: 900;
}

.toc-tab-active::after {
  display: none;
}

.toc-static-head {
  margin-bottom: 14px;
  border-bottom: 0;
  padding-bottom: 0;
}

.toc-static-title {
  color: #182433;
  font-size: 16px;
  font-weight: 900;
  letter-spacing: 0;
  line-height: 1.25;
}

.toc-static-subtitle {
  margin-top: 4px;
  color: #7c8794;
  font-size: 12px;
  line-height: 1.45;
}

.article-toc__scroll {
  max-height: calc(100vh - 172px);
  overflow-y: auto;
  padding: 0 4px 14px 0;
  scrollbar-width: thin;
  scrollbar-color: rgba(17, 24, 39, 0.34) transparent;
}

.toc-static-head + .article-toc__scroll {
  max-height: calc(100vh - 166px);
}

.article-toc__scroll::-webkit-scrollbar {
  width: 6px;
}

.article-toc__scroll::-webkit-scrollbar-track {
  background: transparent;
}

.article-toc__scroll::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: rgba(17, 24, 39, 0.28);
}

:deep(.toc-row) {
  position: relative;
  display: grid;
  grid-template-columns: 14px minmax(0, 1fr) auto 8px;
  align-items: center;
  gap: 5px;
  min-height: 33px;
  margin: 1px 0;
  border-left: 0;
  border-radius: 10px;
  padding-top: 7px;
  padding-right: 8px;
  padding-bottom: 7px;
  color: #667382;
  font-size: 12.5px;
  font-weight: 700;
  line-height: 1.5;
  transition: background-color 0.14s ease, border-color 0.14s ease, color 0.14s ease;
}

:deep(.toc-row:hover) {
  background: rgba(17, 24, 39, 0.06);
  color: #111827;
}

:deep(.toc-depth-0) {
  min-height: 36px;
  color: #4f5f6f;
  font-size: 12.5px;
  font-weight: 800;
}

:deep(.toc-depth-1),
:deep(.toc-depth-2),
:deep(.toc-depth-3),
:deep(.toc-depth-4),
:deep(.toc-kind-article) {
  color: #667382;
  font-size: 12.5px;
  font-weight: 700;
}

:deep(.toc-kind-article) {
  min-height: 38px;
  line-height: 1.58;
}

:deep(.toc-depth-3),
:deep(.toc-depth-4),
:deep(.toc-depth-5),
:deep(.toc-depth-6) {
  color: #87919d;
}

:deep(.toc-row-active) {
  background: #f0f1f3;
  color: #111827;
  font-weight: 900;
}

:deep(.toc-row-active::after) {
  width: 6px;
  height: 6px;
  border-radius: 999px;
  background: #111827;
  box-shadow: 0 0 0 3px rgba(17, 24, 39, 0.12);
  content: "";
}

:deep(.toc-chevron) {
  display: inline-grid;
  width: 14px;
  height: 18px;
  place-items: center;
  color: #82909f;
  font-size: 14px;
  line-height: 1;
}

:deep(.toc-row-active .toc-chevron),
:deep(.toc-row:hover .toc-chevron) {
  color: #111827;
}

:deep(.toc-chevron-leaf) {
  font-size: 13px;
  opacity: 0.45;
}

:deep(.toc-title) {
  min-width: 0;
  overflow: hidden;
  color: inherit;
  line-height: 1.58;
  overflow-wrap: anywhere;
  text-align: left;
  white-space: normal;
}

:deep(.toc-count) {
  min-width: 18px;
  border-radius: 999px;
  background: rgba(17, 24, 39, 0.08);
  padding: 4px 6px;
  color: #111827;
  font-size: 11px;
  font-weight: 900;
  line-height: 1;
  text-align: center;
}

:deep(.toc-row-active .toc-count) {
  background: rgba(17, 24, 39, 0.08);
  color: #111827;
}

:global(html.dark) .article-toc {
  border-color: transparent;
  background: transparent;
  color: #e8e2d8;
  scrollbar-color: rgba(255, 255, 255, 0.18) transparent;
}

:global(html.dark) .toc-tabs {
  color: #a8b3be;
}

:global(html.dark) .toc-tab:hover {
  background: rgba(255, 255, 255, 0.08);
  color: #f2eadf;
}

:global(html.dark) .toc-tab-active {
  background: rgba(255, 255, 255, 0.1);
  color: #f2eadf;
}

:global(html.dark) .toc-tab-active::after {
  display: none;
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
  color: #a8b3be;
}

:global(html.dark) .article-toc :deep(.toc-row:hover) {
  background: rgba(255, 255, 255, 0.08);
  color: #f2eadf;
}

:global(html.dark) .article-toc :deep(.toc-depth-0) {
  color: #f2eadf;
}

:global(html.dark) .article-toc :deep(.toc-depth-1),
:global(html.dark) .article-toc :deep(.toc-depth-2),
:global(html.dark) .article-toc :deep(.toc-kind-article) {
  color: #a8b3be;
}

:global(html.dark) .article-toc :deep(.toc-depth-3),
:global(html.dark) .article-toc :deep(.toc-depth-4),
:global(html.dark) .article-toc :deep(.toc-depth-5),
:global(html.dark) .article-toc :deep(.toc-depth-6) {
  color: #afa79c;
}

:global(html.dark) .article-toc :deep(.toc-row-active) {
  background: rgba(255, 255, 255, 0.1);
  color: #f2eadf;
}

:global(html.dark) .article-toc :deep(.toc-row-active::after) {
  background: #f2eadf;
  box-shadow: 0 0 0 3px rgba(255, 255, 255, 0.12);
}

:global(html.dark) .article-toc :deep(.toc-row-active .toc-chevron),
:global(html.dark) .article-toc :deep(.toc-row:hover .toc-chevron) {
  color: #f2eadf;
}

:global(html.dark) .article-toc :deep(.toc-count) {
  background: rgba(255, 255, 255, 0.1);
  color: #f2eadf;
}

:global(html.dark) .article-toc :deep(.toc-row-active .toc-count) {
  background: rgba(255, 255, 255, 0.1);
  color: #f2eadf;
}

:global(html.dark) .article-toc--fixed::-webkit-scrollbar-thumb,
:global(html.dark) .article-toc__scroll::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.18);
}
</style>

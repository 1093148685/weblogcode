<template>
  <aside class="article-toc overflow-hidden border-r border-[#e5e7eb] bg-[#f8fafc] px-5 py-5">
    <div class="toc-tabs">
      <button
        type="button"
        :class="['toc-tab', activeTab === 'articles' ? 'toc-tab-active' : '']"
        @click="activeTab = 'articles'"
      >
        文章
      </button>
      <button
        type="button"
        :class="['toc-tab', activeTab === 'outline' ? 'toc-tab-active' : '']"
        @click="activeTab = 'outline'"
      >
        大纲
      </button>
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
</template>

<script setup>
import { computed, defineComponent, h, ref, watch } from 'vue'

const props = defineProps({
  title: { type: String, default: '文章目录' },
  items: { type: Array, default: () => [] },
  activeId: { type: String, default: '' },
  autoExpand: { type: Boolean, default: false },
  articleTree: { type: Array, default: () => [] },
  currentArticleId: { type: [String, Number], default: '' }
})

defineEmits(['navigate', 'article'])

const activeTab = ref('outline')
const outlineExpandedIds = ref(new Set())
const articleExpandedIds = ref(new Set(['category-root', 'tag-root']))

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
      if (isArticle.value) return '•'
      return ''
    })

    const toggle = () => {
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
</script>

<style scoped>
.toc-tabs {
  display: grid;
  grid-template-columns: 1fr 1fr;
  margin-bottom: 16px;
  color: #64748b;
  font-size: 14px;
  font-weight: 600;
  text-align: center;
}

.toc-tab {
  position: relative;
  height: 36px;
  color: inherit;
}

.toc-tab-active {
  color: #0f172a;
  font-weight: 900;
}

.toc-tab-active::after {
  position: absolute;
  right: 20%;
  bottom: 0;
  left: 20%;
  height: 3px;
  border-radius: 999px;
  background: #64748b;
  content: "";
}

.article-toc__scroll {
  max-height: calc(100vh - 88px - 24px - 72px);
  overflow-y: auto;
  padding: 2px 8px 16px 0;
  scrollbar-width: thin;
  scrollbar-color: #d1d5db transparent;
}

.article-toc__scroll::-webkit-scrollbar {
  width: 8px;
}

.article-toc__scroll::-webkit-scrollbar-track {
  background: transparent;
}

.article-toc__scroll::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: #d1d5db;
}

:deep(.toc-row) {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  margin: 3px 0;
  border-left: 3px solid transparent;
  border-radius: 8px;
  padding-top: 5px;
  padding-right: 8px;
  padding-bottom: 5px;
  color: #475569;
  font-size: 13px;
  line-height: 1.6;
  transition: background-color 0.16s ease, color 0.16s ease;
}

:deep(.toc-row:hover) {
  background: rgba(100, 116, 139, 0.1);
  color: #334155;
}

:deep(.toc-depth-0) {
  margin-top: 8px;
  color: #0f172a;
  font-size: 13.5px;
  font-weight: 800;
}

:deep(.toc-depth-1) {
  color: #334155;
  font-weight: 600;
}

:deep(.toc-depth-2) {
  color: #64748b;
}

:deep(.toc-depth-3),
:deep(.toc-depth-4) {
  color: #94a3b8;
  font-size: 12.5px;
}

:deep(.toc-kind-article) {
  color: #64748b;
  font-weight: 500;
}

:deep(.toc-row-active) {
  border-left-color: #64748b;
  background: rgba(100, 116, 139, 0.12);
  color: #334155;
  font-weight: 800;
}

:deep(.toc-chevron) {
  display: inline-grid;
  width: 18px;
  height: 21px;
  flex: 0 0 18px;
  place-items: center;
  color: currentColor;
  font-size: 15px;
  line-height: 1;
}

:deep(.toc-chevron-leaf) {
  font-size: 14px;
  opacity: 0.7;
}

:deep(.toc-title) {
  min-width: 0;
  flex: 1;
  overflow: hidden;
  color: inherit;
  line-height: 1.6;
  overflow-wrap: anywhere;
  text-align: left;
  white-space: normal;
}

:deep(.toc-count) {
  min-width: 22px;
  height: 20px;
  flex: 0 0 auto;
  border-radius: 999px;
  background: #f1f5f9;
  padding: 0 7px;
  color: #64748b;
  font-size: 12px;
  font-weight: 800;
  line-height: 20px;
  text-align: center;
}

:deep(.toc-row-active .toc-count) {
  background: #e2e8f0;
  color: #334155;
}

:global(html.dark) .article-toc {
  border-color: #444c56;
  background: #22272e;
}

:global(html.dark) .toc-tabs {
  color: #c9d1d9;
}

:global(html.dark) .toc-tab-active {
  color: #ffffff;
}

:global(html.dark) .toc-tab-active::after {
  background: #58a6ff;
}

:global(html.dark) .article-toc :deep(.toc-row) {
  color: #c9d1d9;
}

:global(html.dark) .article-toc :deep(.toc-row:hover) {
  background: rgba(88, 166, 255, 0.1);
  color: #f0f6fc;
}

:global(html.dark) .article-toc :deep(.toc-depth-0) {
  color: #f0f6fc;
}

:global(html.dark) .article-toc :deep(.toc-depth-1) {
  color: #d1d5db;
}

:global(html.dark) .article-toc :deep(.toc-depth-2),
:global(html.dark) .article-toc :deep(.toc-kind-article) {
  color: #c9d1d9;
}

:global(html.dark) .article-toc :deep(.toc-depth-3),
:global(html.dark) .article-toc :deep(.toc-depth-4) {
  color: #9ca3af;
}

:global(html.dark) .article-toc :deep(.toc-row-active) {
  border-left-color: #58a6ff;
  background: rgba(88, 166, 255, 0.14);
  color: #58a6ff;
}

:global(html.dark) .article-toc :deep(.toc-count) {
  background: rgba(88, 166, 255, 0.14);
  color: #58a6ff;
}

:global(html.dark) .article-toc :deep(.toc-row-active .toc-count) {
  background: rgba(88, 166, 255, 0.22);
  color: #79c0ff;
}

:global(html.dark) .article-toc__scroll::-webkit-scrollbar-thumb {
  background: #444c56;
}
</style>

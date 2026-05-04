import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'

const source = readFileSync(new URL('../src/pages/frontend/wiki-detail.vue', import.meta.url), 'utf8')
const style = source.match(/<style scoped>([\s\S]*)<\/style>/)?.[1] ?? ''

const extractRule = (pattern, label) => {
  const match = style.match(pattern)
  assert.ok(match, `Missing CSS rule for ${label}`)
  return match[1]
}

test('desktop wiki sidebars are fixed to the viewport', () => {
  const sidebarRule = extractRule(
    /\.wiki-left-sidebar,\s*\.wiki-reading-sidebar\s*\{([\s\S]*?)\n\}/,
    'wiki desktop sidebars'
  )

  assert.match(sidebarRule, /position:\s*fixed;/)
  assert.match(sidebarRule, /top:\s*var\(--wiki-sticky-top\);/)
  assert.match(sidebarRule, /max-height:\s*calc\(100vh - var\(--wiki-sticky-top\) - 24px\);/)
  assert.match(sidebarRule, /overflow-y:\s*auto;/)
  assert.doesNotMatch(sidebarRule, /position:\s*static;/)
})

test('wiki sidebars remain hidden below the desktop breakpoint', () => {
  const tabletBlock = extractRule(
    /@media \(max-width:\s*1280px\)\s*\{([\s\S]*?)@media \(max-width:\s*768px\)/,
    'tablet breakpoint'
  )

  assert.match(tabletBlock, /\.wiki-left-rail,\s*\.wiki-right-rail,/)
  assert.match(tabletBlock, /\.wiki-left-sidebar,\s*\.wiki-reading-sidebar\s*\{[\s\S]*?display:\s*none;/)
})

test('wiki detail wires platform article floating and selection tools', () => {
  assert.match(source, /import FloatingActionBar from '@\/components\/article-detail\/FloatingActionBar\.vue'/)
  assert.match(source, /import SelectionToolbar from '@\/components\/article-detail\/SelectionToolbar\.vue'/)
  assert.match(source, /import SnippetAiPanel from '@\/components\/article-detail\/SnippetAiPanel\.vue'/)
  assert.match(source, /import SnippetCommentPanel from '@\/components\/article-detail\/SnippetCommentPanel\.vue'/)

  assert.match(source, /<FloatingActionBar[\s\S]*:active-panel="activePanel"[\s\S]*@toggle-panel="togglePanel"[\s\S]*@comment="scrollToComments"[\s\S]*@copy-link="copyShareLink"/)
  assert.match(source, /<SelectionToolbar[\s\S]*:visible="selectionToolbar\.visible"[\s\S]*@search="searchSelectedText"[\s\S]*@copy="copySelectedText"[\s\S]*@translate="translateSelectedText"[\s\S]*@ask-ai="openSnippetAi"[\s\S]*@comment="openSnippetComment"/)
  assert.match(source, /<SnippetAiPanel[\s\S]*:visible="snippetAiVisible"[\s\S]*:selected-text="selectedText"/)
  assert.match(source, /<SnippetCommentPanel[\s\S]*:visible="snippetCommentVisible"[\s\S]*:selected-text="selectedText"[\s\S]*:comments="activeSnippetComments"[\s\S]*@submit="submitSnippetComment"/)
})

test('wiki snippet comments use wiki article storage and document listeners', () => {
  assert.match(source, /const snippetCommentKey = computed\(\(\) => `wiki_snippet_comments_\$\{wikiId\.value\}_\$\{currentArticleId\.value\}`\)/)

  assert.match(source, /document\.addEventListener\('selectionchange', handleSelectionChange\)/)
  assert.match(source, /document\.addEventListener\('mousedown', handleDocumentMouseDown\)/)
  assert.match(source, /document\.addEventListener\('click', handleSnippetHighlightClick\)/)

  assert.match(source, /document\.removeEventListener\('selectionchange', handleSelectionChange\)/)
  assert.match(source, /document\.removeEventListener\('mousedown', handleDocumentMouseDown\)/)
  assert.match(source, /document\.removeEventListener\('click', handleSnippetHighlightClick\)/)
})

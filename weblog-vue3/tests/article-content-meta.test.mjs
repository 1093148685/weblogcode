import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'

const source = readFileSync(new URL('../src/components/article-detail/ArticleContent.vue', import.meta.url), 'utf8')

test('article content meta bar renders category information', () => {
  assert.match(source, /const categoryName = computed/)
  assert.match(source, /const categoryId = computed/)
  assert.match(source, /v-if="categoryName"/)
  assert.match(source, /@click="\$emit\('go-category', categoryId, categoryName\)"/)
  assert.match(source, /{{ categoryName }}/)
})

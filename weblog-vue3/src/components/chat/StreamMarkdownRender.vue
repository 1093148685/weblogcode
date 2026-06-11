<template>
    <div class="markdown-container">
        <!-- 思考过程折叠面板 -->
        <div v-if="thinkContent" class="think-panel">
            <div
                class="think-header cursor-pointer text-sm text-[var(--text-muted)] hover:text-[var(--text-body)] transition-colors"
                @click="isThinkExpanded = !isThinkExpanded"
            >
                <svg
                    class="w-4 h-4 transition-transform duration-200"
                    :class="{ 'rotate-90': isThinkExpanded }"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
                <span class="font-medium flex items-center gap-1.5">
                    <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 18v-5.25m0 0a6.01 6.01 0 001.5-.189m-1.5.189a6.01 6.01 0 01-1.5-.189m3.75 7.478a12.06 12.06 0 01-4.5 0m3.75 2.383a14.406 14.406 0 01-3 0M14.25 18v-.192c0-.983.658-1.82 1.508-2.316a7.5 7.5 0 10-7.517 0c.85.496 1.509 1.333 1.509 2.316V18" />
                    </svg>
                    思考过程 <span v-if="!isThinkExpanded && isThinking" class="typing-dot-small ml-1"></span>
                </span>
            </div>

            <div v-show="isThinkExpanded" class="think-content text-sm text-[var(--text-muted)]">
                <div v-html="renderedThinkContent" class="think-markdown"></div>
            </div>
        </div>

        <div v-html="renderedContent"></div>
    </div>
</template>

<script setup>
import { ref, watch, nextTick, computed } from 'vue'
import { ElMessage } from 'element-plus'
import MarkdownIt from 'markdown-it'
import hljs from 'highlight.js'
import 'highlight.js/styles/github.css'
import markdownItHighlightJs from 'markdown-it-highlightjs'

const props = defineProps({
    content: {
        type: String,
        default: ''
    }
})

const renderedContent = ref('')
const renderedThinkContent = ref('')
const thinkContent = ref('')
const isThinkExpanded = ref(true)
const isThinking = ref(false)

const md = new MarkdownIt({
    html: true,
    xhtmlOut: true,
    linkify: true,
    typographer: true,
    breaks: true,
    langPrefix: 'language-'
})

md.use(markdownItHighlightJs, {
    hljs,
    auto: true,
    code: true
})

const defaultRender = md.renderer.rules.fence || function(tokens, idx, options, env, renderer) {
    return renderer.renderToken(tokens, idx, options)
}

md.renderer.rules.fence = function (tokens, idx, options, env, renderer) {
    const token = tokens[idx]
    const info = token.info ? md.utils.unescapeAll(token.info).trim() : ''
    let langName = ''

    if (info) {
        const langCode = info.split(/\s+/g)[0]
        langName = langCode.toLowerCase()
    }

    const originalContent = defaultRender(tokens, idx, options, env, renderer)
    const codeContent = token.content
    const codeId = `code-${Math.random().toString(36).substr(2, 9)}`

    let finalContent = `<div class="code-block-wrapper">
        <div class="code-header">`

    if (langName) {
        finalContent += `<div class="code-language-label">${langName}</div>`
    }

    finalContent += `
          <button class="copy-code-btn" onclick="window.copyCode('${codeId}')">
            <svg class="copy-icon" viewBox="0 0 1024 1024" width="15" height="15">
              <path d="M761.088 715.3152a38.7072 38.7072 0 0 1 0-77.4144 37.4272 37.4272 0 0 0 37.4272-37.4272V265.0112a37.4272 37.4272 0 0 0-37.4272-37.4272H425.6256a37.4272 37.4272 0 0 0-37.4272 37.4272 38.7072 38.7072 0 1 1-77.4144 0 115.0976 115.0976 0 0 1 114.8416-114.8416h335.4624a115.0976 115.0976 0 0 1 114.8416 114.8416v335.4624a115.0976 115.0976 0 0 1-114.8416 114.8416z"/>
              <path d="M589.4656 883.0976H268.1856a121.1392 121.1392 0 0 1-121.2928-121.2928v-322.56a121.1392 121.1392 0 0 1 121.2928-121.344h321.28a121.1392 121.1392 0 0 1 121.2928 121.2928v322.56c1.28 67.1232-54.1696 121.344-121.2928 121.344z"/>
            </svg>
            <span class="copy-text">复制</span>
          </button>
        </div>
        <div class="code-content" id="${codeId}" data-code="${encodeURIComponent(codeContent)}">
          ${originalContent}
        </div>
      </div>`
    return finalContent
}

const setupCopyFunction = () => {
    if (!window.copyCode) {
        window.copyCode = async (codeId) => {
            try {
                const codeElement = document.getElementById(codeId)
                if (!codeElement) return

                const codeContent = decodeURIComponent(codeElement.getAttribute('data-code'))
                await navigator.clipboard.writeText(codeContent)

                const btn = codeElement.parentElement.querySelector('.copy-code-btn')
                if (btn) {
                    const originalIcon = btn.querySelector('.copy-icon').innerHTML
                    btn.querySelector('.copy-icon').innerHTML = `<path d="M912 190h-69.9c-9.8 0-19.1 4.5-25.1 12.2L404.7 724.5 207 474c-6.1-7.7-15.3-12.2-25.1-12.2H112c-6.7 0-10.4 7.7-6.3 12.9L357.1 864c12.6 16.1 35.5 16.1 48.1 0L918.3 202.9c4.1-5.2 0.4-12.9-6.3-12.9z"/>`
                    btn.classList.add('copied')

                    ElMessage.success('复制成功')

                    setTimeout(() => {
                        btn.querySelector('.copy-icon').innerHTML = originalIcon
                        btn.classList.remove('copied')
                    }, 1000)
                }
            } catch (err) {
                console.error('复制失败:', err)
            }
        }
    }
}

watch(() => props.content, (newVal) => {
    if (newVal) {
        // Extract <think>...</think> blocks
        const thinkRegex = /<think>([\s\S]*?)(?:<\/think>|$)/
        const match = newVal.match(thinkRegex)

        let displayContent = newVal

        if (match) {
            thinkContent.value = match[1]
            isThinking.value = !newVal.includes('</think>')
            renderedThinkContent.value = md.render(thinkContent.value || '')

            // Remove think block from main content display
            displayContent = newVal.replace(/<think>[\s\S]*?(?:<\/think>|$)/, '').trim()
        } else {
            thinkContent.value = ''
            isThinking.value = false
            renderedThinkContent.value = ''
        }

        renderedContent.value = md.render(displayContent)

        nextTick(() => {
            setupCopyFunction()
        })
    } else {
        renderedContent.value = ''
        thinkContent.value = ''
        isThinking.value = false
        renderedThinkContent.value = ''
    }
}, { immediate: true })
</script>

<style scoped>
.think-panel {
    margin-bottom: 12px;
}

.think-header {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    min-height: 2rem;
    padding: 0 0.85rem;
    border-radius: 999px;
    border: 1px solid var(--border-base);
    background: var(--bg-hover);
    user-select: none;
}

.think-content {
    margin-top: 0.75rem;
    padding: 0.85rem 1rem 0.9rem 1rem;
    border-left: 2px solid rgba(59, 130, 246, 0.24);
    border-radius: 0 14px 14px 0;
    background: color-mix(in srgb, var(--bg-hover) 88%, transparent);
    font-size: 0.88em;
    line-height: 1.72;
}

.think-markdown :deep(p) {
    margin: 0.5em 0;
    font-size: 1em;
}

.typing-dot-small {
    display: inline-block;
    width: 3px;
    height: 3px;
    background-color: currentColor;
    border-radius: 50%;
    animation: typing 1s infinite alternate;
}

@keyframes typing {
    0% { transform: translateY(0px) }
    100% { transform: translateY(-3px) }
}

.markdown-container {
    width: 100%;
    color: var(--text-body);
    font-size: 15px;
    line-height: 1.85;
}

.dark .markdown-container {
    color: var(--text-body);
}

:deep(.markdown-container > p:first-child),
:deep(p:first-child) {
    margin-top: 0;
}

:deep(h1), :deep(h2), :deep(h3), :deep(h4), :deep(h5), :deep(h6) {
    color: var(--text-heading);
    font-weight: 700;
    margin: 1.35em 0 0.55em;
    letter-spacing: 0;
}

:deep(h1) {
    font-size: 1.45rem;
    line-height: 1.4;
}

:deep(h2) {
    font-size: 1.2rem;
    line-height: 1.45;
}

:deep(h3) {
    font-size: 1.05rem;
    line-height: 1.5;
}

:deep(p) {
    margin: 0.78em 0;
    line-height: 1.85;
    font-size: 1rem;
}

:deep(ul), :deep(ol) {
    list-style: disc;
    margin: 0.9em 0;
    padding-left: 1.45rem;
}

:deep(ol) {
    list-style: decimal;
}

:deep(li) {
    margin-bottom: 0.45em;
    line-height: 1.8;
}

:deep(ol li::marker) {
    line-height: calc(1.143 * 25px);
    color: rgb(139 139 139);
}

:deep(ul li::marker) {
    color: rgb(139 139 139);
}

:deep(ul ul) {
    list-style: circle;
    margin-top: 0.3em;
    margin-bottom: 0.3em;
}

:deep(ul ul ul) {
    list-style: square;
}

:deep(.code-block-wrapper) {
    margin: 1.2em 0;
    border: 1px solid var(--border-base);
    border-radius: 14px;
    overflow: hidden;
    background: color-mix(in srgb, var(--bg-hover) 92%, transparent);
}

:deep(.code-header) {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 9px 12px;
    border-bottom: 1px solid var(--border-base);
    background: color-mix(in srgb, var(--bg-card) 78%, transparent);
}

:deep(.code-language-label) {
    color: var(--text-muted);
    margin-left: 4px;
    font-size: 12px;
    font-weight: 700;
    line-height: 18px;
}

:deep(.hljs) {
    background: transparent !important;
    padding: 0 !important;
}

:deep(.copy-code-btn) {
    display: flex;
    align-items: center;
    gap: 4px;
    border: 1px solid transparent;
    border-radius: 12px;
    padding: 0 8px;
    background: transparent;
    color: var(--text-secondary);
    font-size: 12px;
    height: 28px;
    cursor: pointer;
    transition: all 0.2s ease;
}

:deep(.copy-code-btn.copied .copy-icon) {
    fill: #22c55e;
}

:deep(.copy-code-btn:hover) {
    border-color: var(--border-base);
    background: color-mix(in srgb, var(--bg-hover) 86%, white);
}

:deep(.copy-icon) {
    fill: currentColor;
    flex-shrink: 0;
}

:deep(.copy-text) {
    white-space: nowrap;
}

:deep(pre) {
    margin: 0;
    padding: 14px 16px 16px;
    border-radius: 0;
    background: transparent;
    overflow-x: auto;
    max-width: 100%;
    white-space: pre;
    word-wrap: normal;
}

:deep(:not(pre) > code) {
    font-size: 0.875em;
    font-weight: 600;
    color: var(--text-heading);
    background: color-mix(in srgb, var(--bg-hover) 82%, transparent);
    border-radius: 6px;
    padding: 0.15rem 0.38rem;
    margin: 0;
}

:deep(pre > code) {
    font-size: 0.875em;
    background-color: transparent;
    padding: 0;
    border-radius: 0;
    font-weight: normal;
    color: inherit;
    display: block;
    width: 100%;
}

:deep(a) {
    color: var(--color-primary);
    text-decoration: none;
    transition: color 0.18s ease, text-decoration-color 0.18s ease;
}

:deep(a:hover) {
    text-decoration: underline;
    text-decoration-color: currentColor;
}

:deep(blockquote) {
    margin: 1.1em 0;
    padding: 0.8rem 1rem;
    border-left: 3px solid rgba(59, 130, 246, 0.38);
    border-radius: 0 12px 12px 0;
    background: color-mix(in srgb, var(--bg-hover) 90%, transparent);
    color: var(--text-secondary);
}

:deep(table) {
    border-collapse: collapse;
    width: 100%;
    margin: 1.1em 0;
    font-size: 0.94em;
    overflow: hidden;
    border-radius: 12px;
    border-style: hidden;
    box-shadow: 0 0 0 1px var(--border-base);
}

:deep(th), :deep(td) {
    border: 1px solid var(--border-base);
    padding: 0.72em 0.8em;
    text-align: left;
}

:deep(th) {
    color: var(--text-heading);
    background: color-mix(in srgb, var(--bg-hover) 84%, transparent);
}

:deep(hr) {
    background-color: var(--border-base);
    margin: 1.5em 0;
    height: 1px;
    border: none;
}

:deep(h1 + p),
:deep(h2 + p),
:deep(h3 + p) {
    margin-top: 0.5em;
}

:deep(p + ul),
:deep(p + ol) {
    margin-top: 0.5em;
}

:deep(ul + p),
:deep(ol + p) {
    margin-top: 0.7em;
}

/* Dark mode styles - 深空黑蓝主题 */
:deep(.dark) .markdown-container,
.dark :deep(.markdown-container) {
    color: #e5e7eb;
}

:deep(.dark) .markdown-container h1,
:deep(.dark) .markdown-container h2,
:deep(.dark) .markdown-container h3,
:deep(.dark) .markdown-container h4,
:deep(.dark) .markdown-container h5,
:deep(.dark) .markdown-container h6,
.dark :deep(.markdown-container h1),
.dark :deep(.markdown-container h2),
.dark :deep(.markdown-container h3),
.dark :deep(.markdown-container h4),
.dark :deep(.markdown-container h5),
.dark :deep(.markdown-container h6) {
    color: #f9fafb;
    border-bottom-color: #253341;
}

:deep(.dark) .markdown-container a,
.dark :deep(.markdown-container a) {
    color: #60a5fa;
}

:deep(.dark) .markdown-container blockquote,
.dark :deep(.markdown-container blockquote) {
    border-left-color: #3b82f6;
    color: #8899a6;
    background-color: rgba(59, 130, 246, 0.05);
}

:deep(.dark) .markdown-container .code-block-wrapper,
.dark :deep(.markdown-container .code-block-wrapper) {
    background-color: #0f1419;
    border: 1px solid #253341;
}

:deep(.dark) .markdown-container .code-header,
.dark :deep(.markdown-container .code-header) {
    background-color: #1c2732;
    border-bottom: 1px solid #253341;
}

:deep(.dark) .markdown-container .code-language-label,
.dark :deep(.markdown-container .code-language-label) {
    color: #8899a6;
}

:deep(.dark) .markdown-container .copy-code-btn,
.dark :deep(.markdown-container .copy-code-btn) {
    color: #657786;
}

:deep(.dark) .markdown-container .copy-code-btn:hover,
.dark :deep(.markdown-container .copy-code-btn:hover) {
    color: #3b82f6;
    background-color: rgba(59, 130, 246, 0.1);
}

:deep(.dark) .markdown-container pre,
:deep(.dark) .markdown-container pre > code,
.dark :deep(.markdown-container pre),
.dark :deep(.markdown-container pre > code) {
    background-color: #0f1419;
    color: #e5e7eb;
}

:deep(.dark) .markdown-container :not(pre) > code,
.dark :deep(.markdown-container :not(pre) > code) {
    background-color: #253341;
    color: #93c5fd;
}

:deep(.dark) .markdown-container table,
.dark :deep(.markdown-container table) {
    border-color: #253341;
}

:deep(.dark) .markdown-container th,
:deep(.dark) .markdown-container td,
.dark :deep(.markdown-container th),
.dark :deep(.markdown-container td) {
    border-color: #253341;
    color: #e5e7eb;
}

:deep(.dark) .markdown-container th,
.dark :deep(.markdown-container th) {
    background-color: #1c2732;
}

:deep(.dark) .markdown-container ol li::marker,
:deep(.dark) .markdown-container ul li::marker,
.dark :deep(.markdown-container ol li::marker),
.dark :deep(.markdown-container ul li::marker) {
    color: #3b82f6;
}

:deep(.dark) .markdown-container hr,
.dark :deep(.markdown-container hr) {
    border-color: #253341;
}
</style>

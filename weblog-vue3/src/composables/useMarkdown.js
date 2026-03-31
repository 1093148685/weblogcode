import { marked } from 'marked'
import DOMPurify from 'dompurify'

marked.setOptions({
    breaks: true,
    gfm: true
})

const ALLOWED_TAGS = [
    'strong', 'b', 'em', 'i', 'u', 's', 'del',
    'a', 'code', 'pre', 'blockquote',
    'br', 'p', 'hr',
    'ul', 'ol', 'li',
    'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
    'table', 'thead', 'tbody', 'tr', 'th', 'td',
    'img', 'video'
]

const ALLOWED_ATTR = [
    'href', 'title', 'target', 'rel',
    'src', 'alt', 'class',
    'autoplay', 'loop', 'muted', 'playsinline'
]

const STICKER_GIPHY_PATTERN = /\[(?:sticker|giphy):([^\]]+)\]/g

export function useMarkdown() {
    const renderMarkdown = (content) => {
        if (!content) return ''
        
        if (STICKER_GIPHY_PATTERN.test(content)) {
            const clean = DOMPurify.sanitize(content, {
                ALLOWED_TAGS,
                ALLOWED_ATTR,
                ALLOW_DATA_ATTR: false
            })
            return clean
        }
        
        const html = marked.parse(content)
        
        const clean = DOMPurify.sanitize(html, {
            ALLOWED_TAGS,
            ALLOWED_ATTR,
            ALLOW_DATA_ATTR: false
        })
        
        return clean
    }

    return {
        renderMarkdown
    }
}

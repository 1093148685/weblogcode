import { ref, computed } from 'vue'
import DOMPurify from 'dompurify'

const BILIBILI_EMOJI_MAP = {}
const emojiReady = ref(false)
const renderKey = ref(0)

const sharedBilibiliEmojis = ref([])
const sharedEmojiEmojis = ref([])

const BILIBILI_HOT_EMOJIS = [
    { icon: 'https://cdn.jsdelivr.net/gh/2x-ercha/twikoo-magic@master/image/bilibiliHotKey/1.gif', text: 'awsl' },
    { icon: 'https://cdn.jsdelivr.net/gh/2x-ercha/twikoo-magic@master/image/bilibiliHotKey/2.gif', text: 'doge' },
    { icon: 'https://cdn.jsdelivr.net/gh/2x-ercha/twikoo-magic@master/image/bilibiliHotKey/3.gif', text: '妙啊' },
    { icon: 'https://cdn.jsdelivr.net/gh/2x-ercha/twikoo-magic@master/image/bilibiliHotKey/4.gif', text: '委屈' },
    { icon: 'https://cdn.jsdelivr.net/gh/2x-ercha/twikoo-magic@master/image/bilibiliHotKey/5.gif', text: '酸了' },
    { icon: 'https://cdn.jsdelivr.net/gh/2x-ercha/twikoo-magic@master/image/bilibiliHotKey/6.gif', text: '打call' },
    { icon: 'https://cdn.jsdelivr.net/gh/2x-ercha/twikoo-magic@master/image/bilibiliHotKey/7.gif', text: '哭' },
    { icon: 'https://cdn.jsdelivr.net/gh/2x-ercha/twikoo-magic@master/image/bilibiliHotKey/8.gif', text: '真香' },
]

const KOMOJI_LIST = ['(☆ω☆)', '(´∀｀)', '(≧∇≦)', 'ヾ(≧∇≦*)ゝ', '(≧▽≦)', '(´,,•ω•,,)', '(๑•̀ㅂ•́)و✧', '(｡•́︿•̀｡)', '(╯°□°)╯︵ ┻━┻', '┬─┬ノ( º _ ºノ)', '(´◡`)', '(´・ω・`)']

const NATIVE_EMOJIS = ['😂', '🤣', '😍', '🥰', '😅', '😭', '👍', '👏', '🙏', '💕', '❤️', '🔥', '✨', '🌟', '💯', '🎉', '🎊', '✅', '❌', '💪', '🙌', '⭐', '🌈', '☀️', '🌙', '🌸', '🌺', '🍀', '🦋', '🐱', '🐶', '🐰']

const emojiCategories = computed(() => ({
    bilibili: {
        icon: 'BL',
        name: 'B站',
        type: 'image',
        emojis: sharedBilibiliEmojis.value.length > 0 ? sharedBilibiliEmojis.value : BILIBILI_HOT_EMOJIS
    },
    frequently: {
        icon: '⭐',
        name: '常用',
        type: 'image',
        emojis: BILIBILI_HOT_EMOJIS
    },
    smile: {
        icon: '😃',
        name: '颜文字',
        type: 'emoji',
        emojis: KOMOJI_LIST
    },
    emoji: {
        icon: '😀',
        name: 'Emoji',
        type: 'emoji',
        emojis: sharedEmojiEmojis.value.length > 0 ? sharedEmojiEmojis.value : NATIVE_EMOJIS
    }
}))

const simpleEmojiCategories = computed(() => ({
    bilibili: {
        icon: 'BL',
        name: 'B站',
        type: 'image',
        emojis: sharedBilibiliEmojis.value.length > 0 ? sharedBilibiliEmojis.value : BILIBILI_HOT_EMOJIS
    },
    emoji: {
        icon: '😀',
        name: 'Emoji',
        type: 'emoji',
        emojis: sharedEmojiEmojis.value.length > 0 ? sharedEmojiEmojis.value : NATIVE_EMOJIS
    }
}))

export function useEmoji() {
    const loadBilibiliEmoji = async () => {
        if (emojiReady.value) return
        
        try {
            const res = await fetch('https://owo.imaegoo.com/owo.json')
            const data = await res.json()
            
            if (data['Bilibili']) {
                sharedBilibiliEmojis.value = data['Bilibili'].container.map(item => {
                    const url = item.icon.replace(/<img src="([^"]*)">/, '$1')
                    const text = item.text
                    BILIBILI_EMOJI_MAP[text] = url
                    return { icon: url, text }
                })
            } else {
                for (const key in data) {
                    if (data[key].type === 'image' || data[key].container?.[0]?.icon?.includes('<img')) {
                        sharedBilibiliEmojis.value = data[key].container.map(item => {
                            const match = item.icon.match(/src="([^"]*)"/)
                            const url = match ? match[1] : item.icon
                            const text = item.text
                            BILIBILI_EMOJI_MAP[text] = url
                            return { icon: url, text }
                        })
                        break
                    }
                }
            }
            
            if (sharedBilibiliEmojis.value.length === 0) {
                BILIBILI_HOT_EMOJIS.forEach(e => { BILIBILI_EMOJI_MAP[e.text] = e.icon })
                sharedBilibiliEmojis.value = [...BILIBILI_HOT_EMOJIS]
            }
            
            if (data['Emoji']) {
                sharedEmojiEmojis.value = data['Emoji'].container.map(item => {
                    return item.icon
                })
            }
            
            if (sharedEmojiEmojis.value.length === 0) {
                sharedEmojiEmojis.value = [...NATIVE_EMOJIS]
            }
            
            emojiReady.value = true
            renderKey.value++
        } catch (e) {
            console.error('Failed to load Bilibili emoji:', e)
            BILIBILI_HOT_EMOJIS.forEach(e => { BILIBILI_EMOJI_MAP[e.text] = e.icon })
            sharedBilibiliEmojis.value = [...BILIBILI_HOT_EMOJIS]
            sharedEmojiEmojis.value = [...NATIVE_EMOJIS]
            emojiReady.value = true
            renderKey.value++
        }
    }
    
    const renderEmoticonContent = (content) => {
        if (!content) return ''
        let safe = content
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;')
        
        safe = safe.replace(/\[sticker:([^\]]+)\]/g, (match, url) => {
            const lower = url.toLowerCase()
            if (lower.endsWith('.webm') || lower.endsWith('.mp4') || lower.endsWith('.gif')) {
                return `<video class="sticker-img" src="${url}" autoplay loop muted playsinline></video>`
            }
            return `<img class="sticker-img" src="${url}" alt="sticker">`
        })
        
        safe = safe.replace(/\[giphy:([^\]]+)\]/g, (match, url) => {
            const lower = url.toLowerCase()
            if (lower.endsWith('.webm') || lower.endsWith('.mp4') || lower.endsWith('.gif')) {
                return `<video class="sticker-img" src="${url}" autoplay loop muted playsinline></video>`
            }
            return `<img class="sticker-img" src="${url}" alt="gif">`
        })
        
        safe = safe.replace(/\[([^\]]+)\]/g, (match, key) => {
            if (BILIBILI_EMOJI_MAP[key]) {
                return `<img class="emoticon-16" src="${BILIBILI_EMOJI_MAP[key]}" alt="${key}">`
            }
            return match
        })
        
        return safe
    }
    
    const renderEmoticonAndMarkdown = (content) => {
        if (!content) return ''
        
        let safe = content
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;')
        
        safe = safe.replace(/\[sticker:([^\]]+)\]/g, (match, url) => {
            const lower = url.toLowerCase()
            if (lower.endsWith('.webm') || lower.endsWith('.mp4') || lower.endsWith('.gif')) {
                return `<div class="sticker-block"><video class="sticker-img" src="${url}" autoplay loop muted playsinline></video></div>`
            }
            return `<div class="sticker-block"><img class="sticker-img" src="${url}" alt="sticker"></div>`
        })
        
        safe = safe.replace(/\[giphy:([^\]]+)\]/g, (match, url) => {
            const lower = url.toLowerCase()
            if (lower.endsWith('.webm') || lower.endsWith('.mp4') || lower.endsWith('.gif')) {
                return `<div class="sticker-block"><video class="sticker-img" src="${url}" autoplay loop muted playsinline></video></div>`
            }
            return `<div class="sticker-block"><img class="sticker-img" src="${url}" alt="gif"></div>`
        })
        
        safe = safe.replace(/\[([^\]]+)\]/g, (match, key) => {
            if (BILIBILI_EMOJI_MAP[key]) {
                return `<img class="emoticon-16" src="${BILIBILI_EMOJI_MAP[key]}" alt="${key}">`
            }
            return match
        })
        
        const clean = DOMPurify.sanitize(safe, {
            ALLOWED_TAGS: ['p', 'br', 'strong', 'b', 'em', 'i', 'u', 's', 'del', 'a', 'code', 'pre', 'blockquote', 'ul', 'ol', 'li', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'table', 'thead', 'tbody', 'tr', 'th', 'td', 'img', 'video', 'span', 'div'],
            ALLOWED_ATTR: ['href', 'title', 'target', 'rel', 'src', 'alt', 'class', 'autoplay', 'loop', 'muted', 'playsinline'],
            ALLOW_DATA_ATTR: false
        })
        
        return clean
    }
    
    return {
        BILIBILI_EMOJI_MAP,
        emojiReady,
        renderKey,
        emojiCategories,
        simpleEmojiCategories,
        loadBilibiliEmoji,
        renderEmoticonContent,
        renderEmoticonAndMarkdown
    }
}
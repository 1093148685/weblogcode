<template>
    <div class="comment-card" :data-comment-id="comment.id">
        <!-- 一级评论内容 -->
        <div class="flex items-start gap-3">
            <!-- 头像 -->
            <div class="flex-shrink-0 group">
                <img v-if="comment.avatar && comment.avatar.length > 0"
                    :src="comment.avatar"
                    class="w-10 h-10 rounded-full border-2 border-[var(--border-base)] group-hover:border-blue-500 transition-all duration-200 group-hover:scale-110 shadow-sm">
                <div v-else class="w-10 h-10 rounded-full border-2 border-[var(--border-base)] bg-[var(--bg-hover)] flex items-center justify-center text-[var(--text-muted)] group-hover:border-blue-500 transition-all duration-200">
                    <i class="fas fa-user"></i>
                </div>
            </div>
            
            <!-- 内容区域 -->
            <div class="flex-1 min-w-0">
                <!-- 元信息 -->
                <div class="flex items-center flex-wrap gap-x-2 gap-y-1 mb-2">
                    <span class="text-sm font-medium text-blue-600">{{ comment.nickname }}</span>
                    <el-tag v-if="comment.isAdmin" size="small" type="warning" class="ml-1">
                        <i class="fas fa-crown mr-1"></i>管理员
                    </el-tag>
                    <span class="text-xs text-[var(--text-muted)] font-mono">{{ formatTime(comment.createTime) }}</span>
                    <span v-if="comment.ipLocation || comment.deviceType || comment.browser" class="text-xs text-[var(--text-muted)]">
                        <span v-if="comment.ipLocation">{{ comment.ipLocation }}</span>
                        <span v-if="comment.deviceType"> · {{ comment.deviceType }}</span>
                        <span v-if="comment.browser"> · {{ comment.browser }}</span>
                    </span>
                </div>
                
                <!-- 内容 -->
                <p class="text-sm text-[var(--text-body)] leading-relaxed">
                    <ParsedContent :content="comment.content" :key="'parsed-' + comment.id"></ParsedContent>
                </p>
                
                <!-- 私密内容 -->
                <div v-if="comment.isSecret" class="mt-2">
                    <SpoilerContent 
                        ref="spoilerRefs"
                        :comment-id="comment.id"
                        :is-expired="comment.isExpired"
                        :is-reset="comment.isReset"
                        :key="'spoiler-' + comment.id"
                        @verify-start="openSecretModal(comment.id)"
                        @reveal-complete="handleRevealComplete">
                        <div class="text-sm text-[var(--text-body)]" v-html="renderEmoticonAndMarkdown(secretContent || '**************')"></div>
                    </SpoilerContent>
                </div>
                
                <!-- 评论图片 -->
                <div v-if="commentImages && commentImages.length > 0" class="mt-2 flex flex-wrap gap-2">
                    <img v-for="(img, index) in commentImages" :key="index"
                        :src="img"
                        class="max-w-[120px] max-h-[120px] object-cover rounded-md cursor-pointer hover:opacity-80 transition-opacity border border-[var(--border-base)]"
                        @click="previewImage(img)">
                </div>
                
                <!-- 网站链接预览 -->
                <div v-if="websitePreviews.length > 0" class="mt-2">
                    <LinkPreviewCard 
                        v-for="(preview, index) in websitePreviews" 
                        :key="index"
                        :preview="preview"
                        class="mb-2 last:mb-0"
                    />
                </div>
                
                <!-- 操作按钮 -->
                <div class="flex items-center gap-4 mt-3">
                    <button @click="toggleReply(comment)"
                        class="text-xs text-[var(--text-secondary)] hover:text-blue-500 transition-colors flex items-center gap-1">
                        <i class="far fa-comment"></i>
                        回复
                    </button>
                    <button @click="toggleFlower"
                        :class="['text-xs flex items-center gap-1 transition-colors', 
                            hasCurrentUserFlower
                                ? 'text-pink-500'
                                : 'text-[var(--text-secondary)] hover:text-pink-500']">
                        <i :class="hasCurrentUserFlower ? 'fas fa-heart' : 'far fa-heart'"></i>
                        <span>{{ flowerCount }}</span>
                    </button>
                </div>
                
                <!-- 回复表单 -->
                <div v-if="showReplyForm" class="mt-3 p-3 bg-[var(--bg-base)] rounded-md border border-[var(--border-base)]">
                    <div class="text-xs text-blue-600 mb-2">回复 @{{ replyTarget.nickname }}</div>
                    <div class="relative">
                        <textarea v-model="replyContent" rows="2" :maxlength="MAX_CONTENT_LENGTH"
                            class="w-full bg-[var(--bg-card)] border border-[var(--border-base)] rounded-md px-3 py-2 text-sm text-[var(--text-heading)] placeholder-[var(--text-placeholder)] focus:outline-none focus:border-blue-500 resize-none"
                            :placeholder="`回复 @${replyTarget.nickname}...`"></textarea>
                        <button type="button" @click.stop="toggleReplyEmojiPicker"
                            class="absolute bottom-2 right-2 p-1.5 text-[var(--text-muted)] hover:text-[var(--text-secondary)] transition-colors">
                            <i class="far fa-laugh"></i>
                        </button>
                        <div v-if="showReplyEmojiPicker" class="absolute bottom-10 right-0 z-20 bg-[var(--bg-card)] border border-[var(--border-base)] rounded-lg w-64 shadow-sm">
                            <div class="flex border-b border-[var(--border-base)]">
                                <button v-for="(cat, key) in simpleEmojiCategories" :key="key"
                                    type="button"
                                    @click="activeReplyEmojiCategory = key"
                                    :class="['flex-1 py-2 text-center text-xs hover:bg-[var(--bg-base)] transition-colors whitespace-nowrap',
                                        activeReplyEmojiCategory === key ? 'text-blue-500 border-b-2 border-blue-500' : 'text-[var(--text-secondary)]']">
                                    {{ cat.icon }} {{ cat.name }}
                                </button>
                                <button type="button" @click="openReplyStickerPicker"
                                    :class="['flex-1 py-2 text-center text-xs hover:bg-[var(--bg-base)] transition-colors whitespace-nowrap',
                                        (hasReplySticker || hasReplyGiphy) ? 'text-gray-300 cursor-not-allowed' : 'text-[var(--text-secondary)]']">
                                    <i class="fas fa-image mr-1"></i>贴纸
                                </button>
                                <button type="button" @click="openReplyGiphyPicker"
                                    :class="['flex-1 py-2 text-center text-xs hover:bg-[var(--bg-base)] transition-colors whitespace-nowrap',
                                        (hasReplySticker || hasReplyGiphy) ? 'text-gray-300 cursor-not-allowed' : 'text-[var(--text-secondary)]']">
                                    <i class="fas fa-gift mr-1"></i>GIF
                                </button>
                            </div>
                            <div class="p-2 max-h-32 overflow-y-auto">
                                <div v-if="simpleEmojiCategories[activeReplyEmojiCategory]?.type === 'emoji'" class="grid grid-cols-8 gap-1">
                                    <span v-for="(emoji, index) in simpleEmojiCategories[activeReplyEmojiCategory].emojis" :key="index"
                                        class="hover:bg-[var(--bg-hover)] rounded cursor-pointer p-1 transition-colors text-center text-sm"
                                        @click="addReplyEmoji(emoji)">{{ emoji }}</span>
                                </div>
                                <div v-else-if="simpleEmojiCategories[activeReplyEmojiCategory]?.type === 'image'" class="grid grid-cols-8 gap-1">
                                    <img v-for="(emoji, index) in simpleEmojiCategories[activeReplyEmojiCategory].emojis" :key="index"
                                        :src="emoji.icon"
                                        :alt="emoji.text"
                                        class="w-7 h-7 object-contain hover:bg-[var(--bg-hover)] rounded cursor-pointer p-0.5 transition-colors"
                                        @click="addReplyEmoji(emoji)">
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="flex items-center justify-between mt-2">
                        <div class="flex items-center gap-2">
                            <button type="button" @click="triggerReplyImageUpload"
                                :disabled="hasReplySticker || hasReplyGiphy"
                                :class="['text-xs flex items-center gap-1 transition-colors',
                                    (hasReplySticker || hasReplyGiphy) ? 'text-gray-300 cursor-not-allowed' : 'text-[var(--text-muted)] hover:text-blue-500']">
                                <i class="far fa-image"></i>
                                <span>图片{{ hasReplyImages ? `(${replySelectedMedia.filter(m => m.type === 'image').length}/${MAX_MEDIA_COUNT})` : '' }}</span>
                            </button>
                            <input type="file" ref="replyImageInputRef" @change="handleReplyImageChange" accept="image/*" multiple class="hidden">
                        </div>
                        <div class="flex items-center gap-2">
                            <span :class="['text-xs', replyContent.length > 450 ? 'text-red-500' : 'text-[var(--text-muted)]']">{{ replyContent.length }}/{{ MAX_CONTENT_LENGTH }}</span>
                            <button @click="cancelReply"
                                class="px-3 py-1 text-xs text-[var(--text-secondary)] hover:text-[var(--text-heading)] transition-colors">取消</button>
                            <button @click="submitReply"
                                class="px-3 py-1 text-xs bg-blue-600 hover:bg-blue-500 text-white rounded-md transition-colors">发送</button>
                        </div>
                    </div>
                    
                    <!-- 媒体预览区域 -->
                    <div v-if="replySelectedMedia.length > 0" class="mt-2">
                        <div class="flex flex-wrap gap-2">
                            <div v-for="(media, index) in replySelectedMedia" :key="index" class="relative group">
                                <video v-if="isAnimatedUrl(media.url)"
                                    :src="media.url"
                                    class="w-12 h-12 object-cover rounded-md border border-[var(--border-base)]"
                                    autoplay loop muted playsinline></video>
                                <img v-else
                                    :src="media.url"
                                    class="w-12 h-12 object-cover rounded-md border border-[var(--border-base)]">
                                <button @click.stop="removeReplyMedia(index)" type="button"
                                    class="absolute -top-2 -right-2 w-4 h-4 bg-red-500 text-white rounded-full text-xs flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                                    <i class="fas fa-times"></i>
                                </button>
                                <span v-if="media.type === 'sticker'" 
                                    class="absolute -bottom-1 -left-1 px-1 py-0.5 bg-blue-500 text-white text-[10px] rounded">贴</span>
                                <span v-else-if="media.type === 'giphy'" 
                                    class="absolute -bottom-1 -left-1 px-1 py-0.5 bg-purple-500 text-white text-[10px] rounded">GIF</span>
                                <span v-else 
                                    class="absolute -bottom-1 -left-1 px-1 py-0.5 bg-green-500 text-white text-[10px] rounded">图</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        
        <!-- 子评论区域（一级评论展开/收起） -->
        <div v-if="comment.childComments && comment.childComments.length > 0" class="mt-4 ml-12">
            <!-- 收起状态：显示展开按钮 -->
            <div v-if="childCollapsed" class="py-2">
                <button 
                    @click="expandChildComments"
                    class="text-sm text-blue-500 hover:text-blue-600 transition-colors flex items-center gap-1">
                    <i class="fas fa-angle-down"></i>
                    展开{{ totalChildCount }}条回复
                </button>
            </div>
            
            <!-- 展开状态 -->
            <div v-else class="space-y-2">
                <!-- 显示前3条（扁平化后的）- 使用NestedCommentItem统一渲染 -->
                <template v-for="(child, index) in visibleChildComments" :key="'child-' + index">
                    <NestedCommentItem 
                        :comment="child"
                        :border-top="index > 0"
                        @reply-submitted="$emit('reply-submitted', $event)"
                        @flower-changed="$emit('flower-changed', $event)"
                    />
                </template>
                
                <!-- 展开更多按钮（点击加载全部） -->
                <button 
                    v-if="hasMoreChildComments"
                    @click="showAllChildComments"
                    class="text-sm text-blue-500 hover:text-blue-600 transition-colors flex items-center gap-1 py-1">
                    <i class="fas fa-angle-down"></i>
                    展开更多
                </button>
                
                <!-- 收起按钮（只有全部展开后才显示） -->
                <button 
                    v-else
                    @click="collapseChildComments"
                    class="text-sm text-[var(--text-secondary)] hover:text-[var(--text-heading)] transition-colors flex items-center gap-1 py-1">
                    <i class="fas fa-angle-up"></i>
                    收起
                </button>
            </div>
        </div>
    </div>

    <SecretVerifyModal 
        :show="showSecretModal" 
        :comment-id="currentSecretCommentId"
        @close="closeSecretModal"
        @success="handleSecretSuccess"
    />

    <StickerPicker 
        :show="showReplyStickerPicker" 
        @close="showReplyStickerPicker = false"
        @select="addReplySticker"
    />

    <GiphyPicker 
        :show="showReplyGiphyPicker" 
        @close="showReplyGiphyPicker = false"
        @select="addReplyGiphy"
    />
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import Viewer from 'viewerjs'
import NestedCommentItem from './NestedCommentItem.vue'
import SecretVerifyModal from './SecretVerifyModal.vue'
import SpoilerContent from './SpoilerContent.vue'
import ParsedContent from './ParsedContent.vue'
import LinkPreviewCard from './LinkPreviewCard.vue'
import StickerPicker from './StickerPicker.vue'
import GiphyPicker from './GiphyPicker.vue'
import { sendFlower, cancelFlower } from '@/api/frontend/message-wall'
import { showMessage } from '@/composables/util'
import { useEmoji } from '@/composables/useEmoji'
import { useCommentStore } from '@/stores/comment'
import axios from '@/axios'

const commentStore = useCommentStore()
const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const props = defineProps({
    comment: {
        type: Object,
        required: true
    },
    hasCurrentUserFlower: {
        type: Boolean,
        default: false
    }
})

const emit = defineEmits(['reply-submitted', 'flower-changed'])

const { emojiCategories, simpleEmojiCategories, loadBilibiliEmoji, renderEmoticonContent, renderEmoticonAndMarkdown, renderKey } = useEmoji()

const MAX_CONTENT_LENGTH = 500
const MAX_LINK_PREVIEWS = 2
const MAX_MEDIA_COUNT = 3

const showReplyForm = ref(false)
const replyContent = ref('')
const replyTarget = ref({})
const replyImageInputRef = ref(null)
const showReplyEmojiPicker = ref(false)
const activeReplyEmojiCategory = ref('bilibili')
const showReplyStickerPicker = ref(false)
const showReplyGiphyPicker = ref(false)
const replySelectedMedia = ref([])

const hasReplyImages = computed(() => replySelectedMedia.value.some(m => m.type === 'image'))
const hasReplySticker = computed(() => replySelectedMedia.value.some(m => m.type === 'sticker'))
const hasReplyGiphy = computed(() => replySelectedMedia.value.some(m => m.type === 'giphy'))

const flowerCount = computed(() => props.comment.flowerCount || 0)

const isAnimatedUrl = (url) => {
    if (!url) return false
    const lower = url.toLowerCase()
    return lower.endsWith('.webm') || lower.endsWith('.mp4') || lower.endsWith('.gif')
}

const toggleReplyEmojiPicker = () => {
    showReplyEmojiPicker.value = !showReplyEmojiPicker.value
}

const addReplyEmoji = (emoji) => {
    if (replyContent.value.length < MAX_CONTENT_LENGTH) {
        if (typeof emoji === 'object' && emoji.text) {
            replyContent.value += `[${emoji.text}]`
        } else {
            replyContent.value += emoji
        }
    }
    showReplyEmojiPicker.value = false
}

const openReplyStickerPicker = () => {
    if (hasReplyImages.value) {
        showMessage('图片和贴纸不可同时发送', 'warning')
        return
    }
    showReplyEmojiPicker.value = false
    showReplyStickerPicker.value = true
}

const openReplyGiphyPicker = () => {
    if (hasReplyImages.value) {
        showMessage('图片和GIF不可同时发送', 'warning')
        return
    }
    showReplyEmojiPicker.value = false
    showReplyGiphyPicker.value = true
}

const addReplySticker = (stickerCode) => {
    if (hasReplyImages.value) {
        showMessage('图片和贴纸不可同时发送', 'warning')
        showReplyStickerPicker.value = false
        return
    }
    
    replySelectedMedia.value = replySelectedMedia.value.filter(m => m.type !== 'sticker')
    
    const url = stickerCode.replace('[sticker:', '').replace(']', '')
    replySelectedMedia.value.push({ type: 'sticker', url })
    
    showReplyStickerPicker.value = false
}

const addReplyGiphy = (giphyCode) => {
    if (hasReplyImages.value) {
        showMessage('图片和GIF不可同时发送', 'warning')
        showReplyGiphyPicker.value = false
        return
    }
    
    replySelectedMedia.value = replySelectedMedia.value.filter(m => m.type !== 'giphy')
    
    const url = giphyCode.replace('[giphy:', '').replace(']', '')
    replySelectedMedia.value.push({ type: 'giphy', url })
    
    showReplyGiphyPicker.value = false
}

const triggerReplyImageUpload = () => {
    replyImageInputRef.value?.click()
}

const handleReplyImageChange = async (event) => {
    const files = event.target.files
    if (!files || files.length === 0) return
    
    if (hasReplySticker.value || hasReplyGiphy.value) {
        showMessage('图片和贴纸/GIF不可同时发送', 'warning')
        event.target.value = ''
        return
    }
    
    for (let i = 0; i < files.length; i++) {
        const file = files[i]
        
        if (replySelectedMedia.value.filter(m => m.type === 'image').length >= MAX_MEDIA_COUNT) {
            showMessage(`最多只能上传${MAX_MEDIA_COUNT}张图片`, 'warning')
            break
        }
        
        if (file.size > 5 * 1024 * 1024) {
            showMessage('图片大小不能超过 5MB', 'warning')
            continue
        }
        
        const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/bmp']
        if (!allowedTypes.includes(file.type)) {
            showMessage('只允许上传图片文件', 'warning')
            continue
        }
        
        try {
            const formData = new FormData()
            formData.append('file', file)
            
            const res = await axios.post('/comment/file/upload', formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            })
            
            if (res.success && res.data) {
                replySelectedMedia.value.push({ type: 'image', url: res.data })
            } else {
                showMessage(res.message || '图片上传失败', 'error')
            }
        } catch (e) {
            showMessage('图片上传失败', 'error')
        }
    }
    
    event.target.value = ''
}

const removeReplyMedia = (index) => {
    replySelectedMedia.value.splice(index, 1)
}

const commentImages = computed(() => {
    if (!props.comment.images) return []
    return props.comment.images.split(',').filter(img => img.trim() !== '')
})

const showSecretModal = ref(false)
const secretContent = ref('')
const revealedSecrets = ref({})
const websitePreviews = ref([])
const spoilerRefs = ref({})

const isLocalUrl = (url) => {
    try {
        const urlLower = url.toLowerCase()
        if (urlLower.includes('localhost') || 
            urlLower.includes('127.0.0.1') ||
            urlLower.includes('192.168.') ||
            urlLower.includes('10.') ||
            urlLower.includes('172.16.') ||
            urlLower.startsWith('http://10.') ||
            urlLower.startsWith('http://192.') ||
            urlLower.startsWith('https://192.') ||
            urlLower.startsWith('https://10.') ||
            urlLower.includes('.local') ||
            urlLower.includes(':9002') ||
            urlLower.includes(':8000') ||
            urlLower.includes(':3000') ||
            urlLower.includes(':8080'))
            return true
        
        const isMedia = /\.(png|jpg|jpeg|gif|webp|webm|mp4|svg|ico)(?:\?|$)/i.test(url)
        if (isMedia) return true
        
        return false
    } catch {
        return true
    }
}

const extractUrls = (content) => {
    if (!content) return []
    const urlRegex = /https?:\/\/[^\s<"\]]+/gi
    const matches = content.match(urlRegex) || []
    const filtered = matches.filter(url => !isLocalUrl(url))
    const uniqueUrls = [...new Set(filtered)]
    return uniqueUrls.slice(0, MAX_LINK_PREVIEWS)
}

const fetchLinkPreviews = async (urls) => {
    if (!urls || urls.length === 0) return
    
    const previews = []
    for (const url of urls) {
        try {
            const res = await axios.get(`/link-preview?url=${encodeURIComponent(url)}`)
            if (res.success && res.data) {
                previews.push(res.data)
            }
        } catch (e) {
            console.error('Failed to fetch link preview:', e)
        }
    }
    websitePreviews.value = previews
}

const initLinkPreviews = () => {
    const urls = extractUrls(props.comment.content)
    if (urls.length > 0) {
        fetchLinkPreviews(urls)
    }
}

onMounted(() => {
    loadBilibiliEmoji()
    initLinkPreviews()
})

const previewImage = (img) => {
    const container = document.createElement('div')
    container.style.display = 'none'
    commentImages.value.forEach(src => {
        const imgEl = document.createElement('img')
        imgEl.src = src
        container.appendChild(imgEl)
    })
    document.body.appendChild(container)
    
    const index = commentImages.value.findIndex(src => src === img)
    
    const viewer = new Viewer(container, {
        initialViewIndex: index,
        toolbar: true,
        navbar: true,
        title: false,
        hide: () => {
            viewer.destroy()
            document.body.removeChild(container)
        }
    })
    viewer.show()
}

const childComments = computed(() => {
    if (!props.comment.childComments) return []
    return props.comment.childComments
})

const totalChildCount = computed(() => {
    if (!props.comment.childComments) return 0
    
    const countAll = (comments) => {
        let count = 0
        for (const c of comments) {
            count += 1
            if (c.childComments && c.childComments.length > 0) {
                count += countAll(c.childComments)
            }
        }
        return count
    }
    
    return countAll(props.comment.childComments)
})

const childCollapsed = ref(true)
const showAll = ref(false)

const flattenAllChildComments = (comments) => {
    const result = []
    const traverse = (list) => {
        for (const c of list) {
            result.push(c)
            if (c.childComments && c.childComments.length > 0) {
                traverse(c.childComments)
            }
        }
    }
    traverse(comments)
    return result
}

const flattenedAllComments = computed(() => {
    return flattenAllChildComments(childComments.value)
})

const visibleChildComments = computed(() => {
    if (showAll.value) {
        return flattenedAllComments.value
    }
    return flattenedAllComments.value.slice(0, 3)
})

const hasMoreChildComments = computed(() => {
    return flattenedAllComments.value.length > 3 && !showAll.value
})

const expandChildComments = () => {
    childCollapsed.value = false
}

const showAllChildComments = () => {
    showAll.value = true
}

const collapseChildComments = () => {
    childCollapsed.value = true
    showAll.value = false
}

const toggleReply = (target) => {
    if (showReplyForm.value && replyTarget.value.id === target.id) {
        showReplyForm.value = false
        replyContent.value = ''
    } else {
        replyTarget.value = target
        showReplyForm.value = true
        replyContent.value = ''
    }
}

const cancelReply = () => {
    showReplyForm.value = false
    replyContent.value = ''
    replySelectedMedia.value = []
    showReplyEmojiPicker.value = false
}

const submitReply = async () => {
    if (commentStore.userInfo.nickname.length === 0) {
        showMessage('请填写昵称', 'warning')
        return
    }
    if (commentStore.userInfo.mail.length === 0 || !emailRegex.test(commentStore.userInfo.mail)) {
        showMessage('邮箱格式不正确', 'warning')
        return
    }
    if (!replyContent.value.trim() && replySelectedMedia.value.length === 0) {
        showMessage('请输入回复内容', 'warning')
        return
    }
    
    let content = replyContent.value
    let images = null
    
    const stickers = replySelectedMedia.value.filter(m => m.type === 'sticker')
    const giphys = replySelectedMedia.value.filter(m => m.type === 'giphy')
    const mediaImages = replySelectedMedia.value.filter(m => m.type === 'image')
    
    let mediaContent = ''
    stickers.forEach(m => { mediaContent += `[sticker:${m.url}]` })
    giphys.forEach(m => { mediaContent += `[giphy:${m.url}]` })
    
    if (mediaImages.length > 0) {
        images = mediaImages.map(m => m.url).join(',')
    }
    
    if (mediaContent) {
        content = content + '\n\n' + mediaContent
    }
    
    emit('reply-submitted', {
        replyCommentId: replyTarget.value.id,
        parentCommentId: props.comment.id,
        content: content,
        replyNickname: replyTarget.value.nickname,
        images: images
    })
    
    showReplyForm.value = false
    replyContent.value = ''
    replySelectedMedia.value = []
    showReplyEmojiPicker.value = false
    
    if (childCollapsed.value) {
        childCollapsed.value = false
    }
}

const toggleFlower = async () => {
    const isCurrentlyLiked = props.hasCurrentUserFlower
    try {
        if (isCurrentlyLiked) {
            const res = await cancelFlower(props.comment.id)
            if (res.success) {
                emit('flower-changed', { commentId: props.comment.id, hasFlower: false, count: res.data?.flowerCount ?? Math.max(0, (props.comment.flowerCount || 1) - 1) })
            }
        } else {
            const res = await sendFlower(props.comment.id)
            if (res.success) {
                emit('flower-changed', { commentId: props.comment.id, hasFlower: true, count: res.data?.flowerCount ?? ((props.comment.flowerCount || 0) + 1) })
            }
        }
    } catch (e) {
        showMessage('操作失败', 'error')
    }
}

const formatTime = (time) => {
    if (!time) return ''
    const date = new Date(time)
    const now = new Date()
    const diff = now - date
    if (diff < 60000) return '刚刚'
    if (diff < 3600000) return `${Math.floor(diff / 60000)} 分钟前`
    if (diff < 86400000) return `${Math.floor(diff / 3600000)} 小时前`
    if (diff < 604800000) return `${Math.floor(diff / 86400000)} 天前`
    return date.toLocaleDateString('zh-CN')
}

const currentSecretCommentId = ref(null)

const openSecretModal = (commentId) => {
    currentSecretCommentId.value = commentId
    showSecretModal.value = true
}

const handleSecretSuccess = (content) => {
    if (currentSecretCommentId.value) {
        secretContent.value = content
        const spoilerRef = spoilerRefs.value
        if (spoilerRef && spoilerRef.reveal) {
            spoilerRef.reveal()
        }
    }
}

const handleRevealComplete = () => {
}

const closeSecretModal = () => {
    showSecretModal.value = false
    currentSecretCommentId.value = null
}
</script>

<style scoped>
@import '@fortawesome/fontawesome-free/css/all.min.css';

.comment-card {
    padding: 16px 0;
}

.comment-card:first-child {
    padding-top: 0;
}
</style>

<style>
.sticker-img {
    width: 96px !important;
    height: 96px !important;
    object-fit: contain !important;
    vertical-align: middle !important;
    display: inline-block !important;
}

.sticker-block {
    display: block !important;
    margin-top: 12px !important;
}

.animate-reveal {
    animation: reveal-content 0.5s ease-out forwards;
}

@keyframes reveal-content {
    0% {
        opacity: 0;
        filter: blur(8px);
        transform: scale(0.95);
    }
    50% {
        filter: blur(4px);
    }
    100% {
        opacity: 1;
        filter: blur(0);
        transform: scale(1);
    }
}
</style>

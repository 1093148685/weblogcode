<template>
    <div class="nested-comment-item flex items-start gap-2 py-3"
        :class="[borderTop ? 'border-t border-gray-100 dark:border-gray-700/50' : '']">
        <img v-if="comment.avatar && comment.avatar.length > 0"
            :src="comment.avatar"
            class="w-7 h-7 rounded-full border border-gray-200 dark:border-gray-600 flex-shrink-0">
        <div v-else class="w-7 h-7 rounded-full border border-gray-200 dark:border-gray-600 bg-gray-100 dark:bg-gray-700 flex items-center justify-center flex-shrink-0 text-gray-400">
            <i class="fas fa-user"></i>
        </div>
        
        <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-1 flex-wrap">
                <span class="text-sm font-medium text-blue-600 dark:text-blue-400">{{ comment.nickname }}</span>
                <el-tag v-if="comment.isAdmin" size="small" type="warning" class="ml-1">
                    <i class="fas fa-crown mr-1"></i>管理员
                </el-tag>
                <span v-if="isThirdLevelAndDeeper" class="text-sm text-gray-400">@ {{ comment.replyNickname }}</span>
                <span class="text-sm text-gray-400 font-mono">{{ formatTime(comment.createTime) }}</span>
                <span v-if="comment.ipLocation || comment.deviceType || comment.browser" class="text-xs text-gray-400">
                    <span v-if="comment.ipLocation">{{ comment.ipLocation }}</span>
                    <span v-if="comment.deviceType"> · {{ comment.deviceType }}</span>
                    <span v-if="comment.browser"> · {{ comment.browser }}</span>
                </span>
            </div>
            <p class="text-sm text-gray-700 dark:text-gray-300 leading-relaxed">
                <ParsedContent :content="comment.content" :key="'parsed-' + comment.id"></ParsedContent>
            </p>
            
            <!-- 评论图片 -->
            <div v-if="commentImages && commentImages.length > 0" class="mt-2 flex flex-wrap gap-2">
                <img v-for="(img, index) in commentImages" :key="index"
                    :src="img"
                    class="max-w-[100px] max-h-[100px] object-cover rounded-md cursor-pointer hover:opacity-80 transition-opacity border border-gray-200 dark:border-gray-600"
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
            
            <!-- 私密内容 -->
            <div v-if="comment.isSecret" class="mt-2">
                <SpoilerContent ref="spoilerRefs" :comment-id="comment.id" :key="'spoiler-' + comment.id" @verify-start="openSecretModal(comment.id)" @reveal-complete="handleRevealComplete">
                    <div class="text-sm text-gray-700 dark:text-gray-300" v-html="renderMarkdown(secretContent || '私密内容')"></div>
                </SpoilerContent>
            </div>
            
            <div class="flex items-center gap-4 mt-3">
                <button @click="toggleReply(comment)"
                    class="text-sm text-gray-500 dark:text-gray-400 hover:text-blue-500 transition-colors">回复</button>
                <button @click="toggleFlower(comment)"
                    :class="['text-sm flex items-center gap-1 transition-colors',
                        comment.hasCurrentUserFlower 
                            ? 'text-pink-500' 
                            : 'text-gray-500 dark:text-gray-400 hover:text-pink-500']">
                    <i :class="comment.hasCurrentUserFlower ? 'fas fa-heart' : 'far fa-heart'"></i>
                    <span>{{ comment.flowerCount || 0 }}</span>
                </button>
            </div>
            
            <div v-if="showReplyForm" class="mt-3 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-md border border-gray-200 dark:border-gray-700">
                <div class="text-sm text-blue-600 dark:text-blue-400 mb-2">回复 @{{ replyTarget.nickname }}</div>
                <div class="relative">
                    <textarea ref="replyTextareaRef" v-model="replyContent" rows="2" :maxlength="MAX_CONTENT_LENGTH"
                        class="w-full bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-md px-3 py-2 text-sm text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:border-blue-500 resize-none"></textarea>
                    <button type="button" @click.stop="toggleReplyEmojiPicker"
                        class="absolute bottom-2 right-2 p-1.5 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors">
                        <i class="far fa-laugh"></i>
                    </button>
                    <div v-if="showReplyEmojiPicker" class="absolute bottom-10 right-0 z-20 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg w-64 shadow-sm">
                        <div class="flex border-b border-gray-100 dark:border-gray-700">
                            <button v-for="(cat, key) in simpleEmojiCategories" :key="key"
                                type="button"
                                @click="activeReplyEmojiCategory = key"
                                :class="['flex-1 py-2 text-center text-xs hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors whitespace-nowrap',
                                    activeReplyEmojiCategory === key ? 'text-blue-500 border-b-2 border-blue-500' : 'text-gray-500']">
                                {{ cat.icon }} {{ cat.name }}
                            </button>
                            <button type="button" @click="openReplyStickerPicker"
                                :class="['flex-1 py-2 text-center text-xs hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors whitespace-nowrap',
                                    (hasSticker || hasGiphy) ? 'text-gray-300 cursor-not-allowed' : 'text-gray-500']">
                                <i class="fas fa-image mr-1"></i>贴纸
                            </button>
                            <button type="button" @click="openReplyGiphyPicker"
                                :class="['flex-1 py-2 text-center text-xs hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors whitespace-nowrap',
                                    (hasSticker || hasGiphy) ? 'text-gray-300 cursor-not-allowed' : 'text-gray-500']">
                                <i class="fas fa-gift mr-1"></i>GIF
                            </button>
                        </div>
                        <div class="p-2 max-h-32 overflow-y-auto">
                            <div v-if="simpleEmojiCategories[activeReplyEmojiCategory]?.type === 'emoji'" class="grid grid-cols-8 gap-1">
                                <span v-for="(emoji, index) in simpleEmojiCategories[activeReplyEmojiCategory].emojis" :key="index"
                                    class="hover:bg-gray-100 dark:hover:bg-gray-700 rounded cursor-pointer p-1 transition-colors text-center text-sm"
                                    @click="addReplyEmoji(emoji)">{{ emoji }}</span>
                            </div>
                            <div v-else-if="simpleEmojiCategories[activeReplyEmojiCategory]?.type === 'image'" class="grid grid-cols-8 gap-1">
                                <img v-for="(emoji, index) in simpleEmojiCategories[activeReplyEmojiCategory].emojis" :key="index"
                                    :src="emoji.icon"
                                    :alt="emoji.text"
                                    class="w-7 h-7 object-contain hover:bg-gray-100 dark:hover:bg-gray-700 rounded cursor-pointer p-0.5 transition-colors"
                                    @click="addReplyEmoji(emoji)">
                            </div>
                        </div>
                    </div>
                </div>
                <div class="flex items-center justify-between mt-2">
                    <div class="flex items-center gap-2">
                        <button type="button" @click="triggerReplyImageUpload"
                            :disabled="hasSticker || hasGiphy"
                            :class="['text-xs flex items-center gap-1 transition-colors', 
                                (hasSticker || hasGiphy) ? 'text-gray-300 cursor-not-allowed' : 'text-gray-400 hover:text-blue-500']">
                            <i class="far fa-image"></i>
                            <span>图片{{ hasImages ? `(${replySelectedMedia.filter(m => m.type === 'image').length}/${MAX_MEDIA_COUNT})` : '' }}</span>
                        </button>
                        <input type="file" ref="replyImageInputRef" @change="handleReplyImageChange" accept="image/*" multiple class="hidden">
                    </div>
                    <div class="flex items-center gap-2">
                        <span :class="['text-sm', replyContent.length > 450 ? 'text-red-500' : 'text-gray-400']">{{ replyContent.length }}/{{ MAX_CONTENT_LENGTH }}</span>
                        <button @click="cancelReply"
                            class="px-3 py-1 text-sm text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 transition-colors">取消</button>
                        <button @click="submitReply"
                            class="px-3 py-1 text-sm bg-blue-600 hover:bg-blue-500 text-white rounded-md transition-colors">发送</button>
                    </div>
                </div>
                
                <!-- 媒体预览区域 -->
                <div v-if="replySelectedMedia.length > 0" class="mt-2">
                    <VueDraggable v-model="replySelectedMedia" class="flex flex-wrap gap-2" :animation="200">
                        <div v-for="(media, index) in replySelectedMedia" :key="index" class="relative group">
                            <video v-if="isAnimatedUrl(media.url)" 
                                :src="media.url" 
                                class="w-12 h-12 object-cover rounded-md border border-gray-200 dark:border-gray-600"
                                autoplay loop muted playsinline></video>
                            <img v-else 
                                :src="media.url" 
                                class="w-12 h-12 object-cover rounded-md border border-gray-200 dark:border-gray-600">
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
                    </VueDraggable>
                </div>
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
import { sendFlower, cancelFlower } from '@/api/frontend/message-wall'
import { showMessage } from '@/composables/util'
import { useEmoji } from '@/composables/useEmoji'
import { useMarkdown } from '@/composables/useMarkdown'
import axios from '@/axios'
import { VueDraggable } from 'vue-draggable-plus'
import SecretVerifyModal from './SecretVerifyModal.vue'
import SpoilerContent from './SpoilerContent.vue'
import ParsedContent from './ParsedContent.vue'
import StickerPicker from './StickerPicker.vue'
import GiphyPicker from './GiphyPicker.vue'
import LinkPreviewCard from './LinkPreviewCard.vue'

const MAX_MEDIA_COUNT = 3
const MAX_LINK_PREVIEWS = 2
const replySelectedMedia = ref([])
const showReplyGiphyPicker = ref(false)
const websitePreviews = ref([])

const hasImages = computed(() => replySelectedMedia.value.some(m => m.type === 'image'))
const hasSticker = computed(() => replySelectedMedia.value.some(m => m.type === 'sticker'))
const hasGiphy = computed(() => replySelectedMedia.value.some(m => m.type === 'giphy'))

const isAnimatedUrl = (url) => {
    if (!url) return false
    const lower = url.toLowerCase()
    return lower.endsWith('.webm') || lower.endsWith('.mp4') || lower.endsWith('.gif')
}

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

const props = defineProps({
    comment: {
        type: Object,
        required: true
    },
    borderTop: {
        type: Boolean,
        default: false
    }
})

const emit = defineEmits(['reply-submitted', 'flower-changed'])

const { simpleEmojiCategories, loadBilibiliEmoji, renderEmoticonContent, renderEmoticonAndMarkdown, renderKey } = useEmoji()
const { renderMarkdown } = useMarkdown()
const MAX_CONTENT_LENGTH = 500

const showReplyForm = ref(false)
const replyContent = ref('')
const replyTarget = ref({})
const replyTextareaRef = ref(null)
const replyImageInputRef = ref(null)
const showReplyEmojiPicker = ref(false)
const activeReplyEmojiCategory = ref('emoji')
const replyUploadingImages = ref(false)
const replyUploadedImages = ref([])
const showReplyStickerPicker = ref(false)

const revealedSecrets = ref({})
const secretContent = ref('')
const showSecretModal = ref(false)
const currentSecretCommentId = ref(null)
const spoilerRefs = ref({})

const isDirectReplyToRoot = computed(() => {
    return props.comment.parentCommentId === props.comment.replyCommentId
})

const isThirdLevelAndDeeper = computed(() => {
    return !isDirectReplyToRoot.value
})

const commentImages = computed(() => {
    if (!props.comment.images) return []
    return props.comment.images.split(',').filter(img => img.trim() !== '')
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

const toggleReply = (target) => {
    if (showReplyForm.value && replyTarget.value.id === target.id) {
        showReplyForm.value = false
        replyContent.value = ''
        replySelectedMedia.value = []
    } else {
        replyTarget.value = target
        showReplyForm.value = true
        replyContent.value = ''
        replySelectedMedia.value = []
    }
}

const cancelReply = () => {
    showReplyForm.value = false
    replyContent.value = ''
    replySelectedMedia.value = []
}

const submitReply = async () => {
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
    
    content = mediaContent + content
    
    emit('reply-submitted', {
        replyCommentId: replyTarget.value.id,
        parentCommentId: props.comment.parentCommentId || props.comment.id,
        content: content,
        replyNickname: replyTarget.value.nickname,
        images: images
    })
    
    showReplyForm.value = false
    replyContent.value = ''
    replySelectedMedia.value = []
}

const toggleFlower = async (comment) => {
    const isCurrentlyLiked = comment.hasCurrentUserFlower
    try {
        if (isCurrentlyLiked) {
            const res = await cancelFlower(comment.id)
            if (res.success) {
                emit('flower-changed', { commentId: comment.id, hasFlower: false, count: res.data?.flowerCount ?? (comment.flowerCount || 1) - 1 })
            }
        } else {
            const res = await sendFlower(comment.id)
            if (res.success) {
                emit('flower-changed', { commentId: comment.id, hasFlower: true, count: res.data?.flowerCount ?? (comment.flowerCount || 0) + 1 })
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

const toggleReplyEmojiPicker = () => {
    showReplyEmojiPicker.value = !showReplyEmojiPicker.value
}

const addReplyEmoji = (emoji) => {
    if (replyContent.value.length < MAX_CONTENT_LENGTH) {
        replyContent.value += emoji
    }
    showReplyEmojiPicker.value = false
}

const openReplyStickerPicker = () => {
    if (hasImages.value) {
        showMessage('图片和贴纸不可同时发送', 'warning')
        return
    }
    showReplyEmojiPicker.value = false
    showReplyStickerPicker.value = true
}

const openReplyGiphyPicker = () => {
    if (hasImages.value) {
        showMessage('图片和GIF不可同时发送', 'warning')
        return
    }
    showReplyEmojiPicker.value = false
    showReplyGiphyPicker.value = true
}

const addReplySticker = (stickerCode) => {
    if (hasImages.value) {
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
    if (hasImages.value) {
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
    
    if (hasSticker.value || hasGiphy.value) {
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
</script>

<style scoped>
.sticker-img {
    width: 96px;
    height: 96px;
    object-fit: contain;
    vertical-align: middle;
    display: inline-block;
}

.sticker-block {
    display: block;
    margin-top: 12px;
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

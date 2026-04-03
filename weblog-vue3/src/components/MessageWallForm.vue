<template>
    <div class="message-form-container">
        <div class="message-form">
            <!-- 头部 -->
            <div class="form-header mb-4 flex items-center justify-between">
                <span class="text-sm font-medium text-[var(--text-body)]">发表留言</span>
                <CommentAdminLogin />
            </div>
            
            <form>
                <div class="flex gap-3 mb-3">
                    <!-- 头像 -->
                    <div class="w-10 h-10 flex-shrink-0">
                        <img v-if="commentStore.userInfo.avatar && commentStore.userInfo.avatar.length > 0"
                            :src="commentStore.userInfo.avatar"
                            class="w-10 h-10 rounded-full border border-[var(--border-base)]">
                        <div v-else class="w-10 h-10 rounded-full border border-[var(--border-base)] bg-[var(--bg-hover)] flex items-center justify-center text-[var(--text-muted)]">
                            <i class="fas fa-user text-sm"></i>
                        </div>
                    </div>
                    
                    <!-- 输入区域 -->
                    <div class="flex-1 min-w-0">
                        <!-- 三列输入框 -->
                        <div class="flex flex-col sm:flex-row gap-2 sm:gap-3 mb-3">
                            <input @blur="onNicknameInputBlur" v-model="commentStore.userInfo.nickname"
                                type="text"
                                placeholder="QQ号/昵称"
                                class="flex-1 min-w-0 bg-[var(--bg-base)] border border-[var(--border-base)] rounded-md px-3 py-2 text-sm text-[var(--text-heading)] placeholder-[var(--text-placeholder)] focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]">
                            <input v-model="commentStore.userInfo.mail"
                                type="text"
                                placeholder="邮箱 (选填)"
                                class="flex-1 min-w-0 bg-[var(--bg-base)] border border-[var(--border-base)] rounded-md px-3 py-2 text-sm text-[var(--text-heading)] placeholder-[var(--text-placeholder)] focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]">
                            <input v-model="commentStore.userInfo.website"
                                type="text"
                                placeholder="网站 (选填)"
                                class="flex-1 min-w-0 bg-[var(--bg-base)] border border-[var(--border-base)] rounded-md px-3 py-2 text-sm text-[var(--text-heading)] placeholder-[var(--text-placeholder)] focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)]">
                        </div>
                        
                        <!-- 文本框 -->
                        <div class="relative">
                            <textarea v-model="commentForm.content" rows="3" :maxlength="MAX_CONTENT_LENGTH"
                                class="w-full bg-[var(--bg-base)] border border-[var(--border-base)] rounded-md px-3 py-2 text-sm text-[var(--text-heading)] placeholder-[var(--text-placeholder)] focus:outline-none focus:border-[var(--color-primary)] focus:ring-1 focus:ring-[var(--color-primary)] resize-none"
                                placeholder="说点什么..."></textarea>
                            
                            <!-- 工具按钮组 -->
                            <div class="absolute bottom-2 right-2 flex items-center gap-1">
                                <!-- 打码标签按钮 -->
                                <button type="button" @click.stop="insertSpoilerTag"
                                    class="p-1.5 text-[var(--text-muted)] hover:text-purple-500 transition-colors"
                                    title="插入打码文字">
                                    <i class="fas fa-eye-slash text-xs"></i>
                                </button>
                                <!-- 表情按钮 -->
                                <button type="button" @click.stop="toggleEmojiPicker"
                                    class="p-1.5 text-[var(--text-muted)] hover:text-[var(--text-secondary)] transition-colors">
                                    <i class="far fa-laugh"></i>
                                </button>
                            </div>
                            
                            <!-- 表情选择器 -->
                            <div v-if="showEmojiPicker" class="absolute bottom-10 right-0 z-20 bg-[var(--bg-card)] border border-[var(--border-base)] rounded-lg w-80 shadow-sm">
                                <!-- 分类切换 -->
                                <div class="flex border-b border-[var(--border-base)]">
                                    <button v-for="(category, key) in simpleEmojiCategories" :key="key"
                                        type="button"
                                        @click="activeEmojiCategory = key"
                                        :class="['flex-1 py-2 text-center text-xs hover:bg-[var(--bg-base)] transition-colors whitespace-nowrap',
                                            activeEmojiCategory === key ? 'text-blue-500 border-b-2 border-blue-500' : 'text-[var(--text-secondary)]']">
                                        {{ category.icon }} {{ category.name }}
                                    </button>
                                    <button type="button" @click="activeEmojiCategory = 'sticker'"
                                        :class="['flex-1 py-2 text-center text-xs hover:bg-[var(--bg-base)] transition-colors whitespace-nowrap',
                                            activeEmojiCategory === 'sticker' ? 'text-blue-500 border-b-2 border-blue-500' : 'text-[var(--text-secondary)]']">
                                        <i class="fas fa-image mr-1"></i>贴纸
                                    </button>
                                    <button type="button" @click="openGiphyPicker"
                                        class="flex-1 py-2 text-center text-xs hover:bg-[var(--bg-base)] transition-colors whitespace-nowrap text-[var(--text-secondary)]">
                                        <i class="fas fa-gift mr-1"></i>GIF
                                    </button>
                                </div>
                                <!-- 表情网格 -->
                                <div class="p-2 max-h-40 overflow-y-auto">
                                    <div v-if="activeEmojiCategory === 'sticker'">
                                        <div class="text-center text-xs text-[var(--text-secondary)] py-4">
                                            <button @click="openStickerPicker" class="text-blue-500 hover:text-blue-600">
                                                <i class="fas fa-images mr-1"></i>点击选择贴纸
                                            </button>
                                        </div>
                                    </div>
                                    <div v-else-if="simpleEmojiCategories[activeEmojiCategory]?.type === 'image'" class="grid grid-cols-8 gap-1">
                                        <img v-for="(emoji, index) in simpleEmojiCategories[activeEmojiCategory].emojis" :key="index"
                                            :src="emoji.icon"
                                            :alt="emoji.text"
                                            class="w-7 h-7 object-contain hover:bg-[var(--bg-hover)] rounded cursor-pointer p-0.5 transition-colors"
                                            @click="addEmoji(emoji)">
                                    </div>
                                    <div v-else class="grid grid-cols-8 gap-1">
                                        <span v-for="(emoji, index) in simpleEmojiCategories[activeEmojiCategory].emojis" :key="index"
                                            class="hover:bg-[var(--bg-hover)] rounded cursor-pointer p-1 transition-colors text-center text-base"
                                            @click="addEmoji(emoji)">{{ emoji }}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                        
                        <!-- 管理员私密内容设置 -->
                        <div v-if="commentAdminStore.isLoggedIn" class="mt-3 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg">
                            <div class="flex items-center gap-2 mb-2">
                                <input 
                                    type="checkbox" 
                                    v-model="secretForm.isSecret" 
                                    id="secret-checkbox"
                                    class="w-4 h-4 text-amber-500 rounded focus:ring-amber-400"
                                >
                                <label for="secret-checkbox" class="text-sm text-amber-700 dark:text-amber-400 font-medium cursor-pointer">
                                    <i class="fas fa-lock mr-1"></i>设为私密内容
                                </label>
                            </div>
                            
                            <div v-if="secretForm.isSecret" class="space-y-3 mt-3">
                                <div>
                                    <label class="block text-xs text-amber-600 dark:text-amber-400 mb-1">私密内容</label>
                                    <textarea
                                        v-model="secretForm.secretContent"
                                        rows="2"
                                        class="w-full px-3 py-2 text-sm border border-amber-200 dark:border-amber-700 rounded-md bg-[var(--bg-card)] text-[var(--text-heading)] focus:outline-none focus:border-amber-400 resize-none"
                                        placeholder="输入仅管理员可见的私密内容"
                                    ></textarea>
                                </div>
                                <div class="grid grid-cols-2 gap-3">
                                    <div>
                                        <label class="block text-xs text-amber-600 dark:text-amber-400 mb-1">密钥</label>
                                        <input
                                            v-model="secretForm.secretKey"
                                            type="text"
                                            class="w-full px-3 py-2 text-sm border border-amber-200 dark:border-amber-700 rounded-md bg-[var(--bg-card)] text-[var(--text-heading)] focus:outline-none focus:border-amber-400"
                                            placeholder="查看密钥"
                                        >
                                    </div>
                                    <div>
                                        <label class="block text-xs text-amber-600 dark:text-amber-400 mb-1">过期时间</label>
                                        <select
                                            v-model="secretForm.expirationOption"
                                            class="w-full px-3 py-2 text-sm border border-amber-200 dark:border-amber-700 rounded-md bg-[var(--bg-card)] text-[var(--text-heading)] focus:outline-none focus:border-amber-400"
                                        >
                                            <option value="never">永不过期</option>
                                            <option value="1day">1天后过期</option>
                                            <option value="7days">7天后过期</option>
                                            <option value="30days">30天后过期</option>
                                            <option value="custom">自定义时间</option>
                                        </select>
                                    </div>
                                </div>
                                <div v-if="secretForm.expirationOption === 'custom'">
                                    <label class="block text-xs text-amber-600 dark:text-amber-400 mb-1">自定义过期时间</label>
                                    <input
                                        v-model="secretForm.customExpiration"
                                        type="datetime-local"
                                        class="w-full px-3 py-2 text-sm border border-amber-200 dark:border-amber-700 rounded-md bg-[var(--bg-card)] text-[var(--text-heading)] focus:outline-none focus:border-amber-400"
                                    >
                                </div>
                            </div>
                        </div>

                        <!-- 底部操作栏 -->
                        <div class="flex items-center justify-between mt-2">
                            <div class="flex items-center gap-3">
                                <!-- 图片上传按钮 -->
                                <button type="button" @click="triggerImageUpload"
                                    :disabled="hasSticker || hasGiphy"
                                    :class="['text-xs flex items-center gap-1 transition-colors',
                                        (hasSticker || hasGiphy) ? 'text-gray-300 cursor-not-allowed' : 'text-[var(--text-muted)] hover:text-blue-500']">
                                    <i class="far fa-image"></i>
                                    <span>图片{{ hasImages ? `(${selectedMedia.filter(m => m.type === 'image').length}/${MAX_IMAGES})` : '' }}</span>
                                </button>
                                <input type="file" ref="imageInputRef" @change="handleImageChange" accept="image/*" multiple class="hidden">
                                <span class="text-xs text-[var(--text-muted)]">输入 QQ 号可自动获取信息</span>
                                <span :class="['text-xs', contentLength > warningThreshold ? 'text-red-500' : 'text-[var(--text-muted)]']">
                                    {{ contentLength }}/{{ MAX_CONTENT_LENGTH }}
                                </span>
                            </div>
                            <button type="button" @click="onPublishClick"
                                class="px-4 py-1.5 bg-green-600 hover:bg-green-500 text-white text-sm font-medium rounded-md transition-colors focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2">
                                <i class="fas fa-paper-plane mr-1.5"></i>发布
                            </button>
                        </div>
                        
                        <!-- 媒体预览区域（图片/贴纸/GIF） -->
                        <div v-if="selectedMedia.length > 0" class="mt-3">
                            <VueDraggable v-model="selectedMedia" class="flex flex-wrap gap-2" :animation="200">
                                <div v-for="(media, index) in selectedMedia" :key="index" class="relative group">
                                    <video v-if="isAnimatedUrl(media.url)"
                                        :src="media.url"
                                        class="w-16 h-16 object-cover rounded-md border border-[var(--border-base)]"
                                        autoplay loop muted playsinline></video>
                                    <img v-else
                                        :src="media.url"
                                        class="w-16 h-16 object-cover rounded-md border border-[var(--border-base)]">
                                    <button @click.stop="removeMedia(index)" type="button"
                                        class="absolute -top-2 -right-2 w-5 h-5 bg-red-500 text-white rounded-full text-xs flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
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
            </form>
        </div>
        
        <!-- 成功动画 -->
        <transition name="fade">
            <div v-if="showSuccessAnimation" class="fixed top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 z-50 bg-[var(--bg-card)] border border-[var(--border-base)] rounded-lg p-6 shadow-lg">
                <div class="text-center">
                    <div class="text-4xl mb-2">🎉</div>
                    <div class="text-[var(--text-body)] text-sm font-medium">留言发布成功</div>
                </div>
            </div>
        </transition>
    </div>

    <!-- 贴纸选择器 -->
    <StickerPicker 
        :show="showStickerPicker" 
        @close="showStickerPicker = false"
        @select="addSticker"
    />

    <!-- GIPHY选择器 -->
    <GiphyPicker 
        :show="showGiphyPicker" 
        @close="showGiphyPicker = false"
        @select="addGiphy"
    />
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { useCommentStore } from '@/stores/comment'
import { useCommentAdminStore } from '@/stores/commentAdmin'
import { publishMessageWallComment } from '@/api/frontend/message-wall'
import { getUserInfoByQQ } from '@/api/frontend/comment'
import { showMessage } from '@/composables/util'
import { useEmoji } from '@/composables/useEmoji'
import axios from '@/axios'
import { VueDraggable } from 'vue-draggable-plus'
import CommentAdminLogin from './CommentAdminLogin.vue'
import StickerPicker from './StickerPicker.vue'
import GiphyPicker from './GiphyPicker.vue'

const emit = defineEmits(['comment-published'])

const commentStore = useCommentStore()
const commentAdminStore = useCommentAdminStore()

const commentForm = reactive({
    content: '',
    replyCommentId: null,
    parentCommentId: null
})

const secretForm = reactive({
    isSecret: false,
    secretContent: '',
    secretKey: '',
    expirationOption: 'never',
    customExpiration: ''
})

const showEmojiPicker = ref(false)
const activeEmojiCategory = ref('bilibili')
const showSuccessAnimation = ref(false)
const imageInputRef = ref(null)
const showStickerPicker = ref(false)
const showGiphyPicker = ref(false)

const MAX_CONTENT_LENGTH = 500
const warningThreshold = 450
const MAX_IMAGES = 3

const selectedMedia = ref([])

const isAnimatedUrl = (url) => {
    if (!url) return false
    const lower = url.toLowerCase()
    return lower.endsWith('.webm') || lower.endsWith('.mp4') || lower.endsWith('.gif')
}

const getMediaType = (url) => {
    if (!url) return 'image'
    const lower = url.toLowerCase()
    if (lower.includes('giphy') || lower.includes('media.giphy')) return 'giphy'
    if (lower.endsWith('.webm') || lower.endsWith('.mp4') || lower.endsWith('.gif') || lower.endsWith('.png') || lower.endsWith('.jpg') || lower.endsWith('.jpeg')) return 'image'
    return 'image'
}

const hasImages = computed(() => selectedMedia.value.some(m => m.type === 'image'))
const hasSticker = computed(() => selectedMedia.value.some(m => m.type === 'sticker'))
const hasGiphy = computed(() => selectedMedia.value.some(m => m.type === 'giphy'))

const getExpirationDate = () => {
    if (secretForm.expirationOption === 'never') return null
    const now = new Date()
    switch (secretForm.expirationOption) {
        case '1day': return new Date(now.getTime() + 24 * 60 * 60 * 1000)
        case '7days': return new Date(now.getTime() + 7 * 24 * 60 * 60 * 1000)
        case '30days': return new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000)
        case 'custom': return secretForm.customExpiration ? new Date(secretForm.customExpiration) : null
        default: return null
    }
}

const contentLength = computed(() => commentForm.content.length)

const { simpleEmojiCategories, loadBilibiliEmoji } = useEmoji()

const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const toggleEmojiPicker = () => {
    showEmojiPicker.value = !showEmojiPicker.value
}

const closeEmojiPicker = (e) => {
    if (!e.target.closest('.relative')) {
        showEmojiPicker.value = false
    }
}

onMounted(() => {
    document.addEventListener('click', closeEmojiPicker)
    loadBilibiliEmoji()
})

onUnmounted(() => {
    document.removeEventListener('click', closeEmojiPicker)
})

const onPublishClick = () => {
    if (commentStore.userInfo.nickname.length === 0) {
        showMessage('请填写昵称', 'warning')
        return
    }
    if (commentStore.userInfo.mail.length === 0 || !emailRegex.test(commentStore.userInfo.mail)) {
        showMessage('邮箱格式不正确', 'warning')
        return
    }
    if (commentForm.content.length === 0 && selectedMedia.value.length === 0) {
        showMessage('请填写留言内容', 'warning')
        return
    }
    if (commentForm.content.length > MAX_CONTENT_LENGTH) {
        showMessage('留言内容过长', 'warning')
        return
    }

    let content = commentForm.content
    let images = null
    
    const stickers = selectedMedia.value.filter(m => m.type === 'sticker')
    const giphys = selectedMedia.value.filter(m => m.type === 'giphy')
    const mediaImages = selectedMedia.value.filter(m => m.type === 'image')
    
    let mediaContent = ''
    stickers.forEach(m => { mediaContent += `[sticker:${m.url}]` })
    giphys.forEach(m => { mediaContent += `[giphy:${m.url}]` })
    
    if (mediaImages.length > 0) {
        images = mediaImages.map(m => m.url).join(',')
    }
    
    if (mediaContent) {
        content = content + '\n\n' + mediaContent
    }

    const data = {
        content: content,
        avatar: commentStore.userInfo.avatar,
        nickname: commentStore.userInfo.nickname,
        mail: commentStore.userInfo.mail,
        website: commentStore.userInfo.website,
        replyCommentId: commentForm.replyCommentId,
        parentCommentId: commentForm.parentCommentId,
        images: images
    }

    if (secretForm.isSecret && commentAdminStore.isLoggedIn) {
        if (!secretForm.secretContent) {
            showMessage('请填写私密内容', 'warning')
            return
        }
        if (!secretForm.secretKey) {
            showMessage('请填写密钥', 'warning')
            return
        }
        data.isSecret = true
        data.secretContent = secretForm.secretContent
        data.secretKey = secretForm.secretKey
        data.expiresAt = getExpirationDate()
    }

    publishMessageWallComment(data).then(res => {
        if (!res.success) {
            if (res.message && res.message.includes('敏感词')) {
                showMessage('留言包含敏感词，请修改后重试', 'warning')
            } else {
                showMessage(res.message || '发布失败', 'error')
            }
            return
        }
        
        showSuccessAnimation.value = true
        setTimeout(() => {
            showSuccessAnimation.value = false
        }, 1500)
        
        commentForm.content = ''
        selectedMedia.value = []
        emit('comment-published')
    })
}

const onNicknameInputBlur = () => {
    const nickname = commentStore.userInfo.nickname
    if (!checkIfPureNumber(nickname)) {
        return
    }
    getUserInfoByQQ(nickname).then(res => {
        if (!res.success) return
        commentStore.userInfo.avatar = res.data.avatar
        commentStore.userInfo.mail = res.data.mail
        if (res.data.nickname) {
            commentStore.userInfo.nickname = res.data.nickname
        }
    })
}

function checkIfPureNumber(text) {
    const trimmed = text.trim()
    return /^\d+$/.test(trimmed)
}

const addEmoji = (emoji) => {
    if (commentForm.content.length < MAX_CONTENT_LENGTH) {
        if (typeof emoji === 'object' && emoji.text) {
            commentForm.content += `[${emoji.text}]`
        } else {
            commentForm.content += emoji
        }
    }
    showEmojiPicker.value = false
}

const insertSpoilerTag = () => {
    const textarea = document.querySelector('.message-form-container textarea')
    if (!textarea) return
    
    const start = textarea.selectionStart
    const end = textarea.selectionEnd
    const text = commentForm.content
    const selectedText = text.substring(start, end)
    
    if (selectedText) {
        commentForm.content = text.substring(0, start) + `<s>${selectedText}</s>` + text.substring(end)
    } else {
        commentForm.content = text.substring(0, start) + '<s>选中文本</s>' + text.substring(end)
    }
    
    showEmojiPicker.value = false
}

const triggerImageUpload = () => {
    imageInputRef.value?.click()
}

const handleImageChange = async (event) => {
    const files = event.target.files
    if (!files || files.length === 0) return
    
    if (hasSticker.value || hasGiphy.value) {
        showMessage('图片和贴纸/GIF不可同时发送', 'warning')
        event.target.value = ''
        return
    }
    
    for (let i = 0; i < files.length; i++) {
        const file = files[i]
        
        if (selectedMedia.value.filter(m => m.type === 'image').length >= MAX_IMAGES) {
            showMessage(`最多只能上传${MAX_IMAGES}张图片`, 'warning')
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
                selectedMedia.value.push({ type: 'image', url: res.data })
            } else {
                showMessage(res.message || '图片上传失败', 'error')
            }
        } catch (e) {
            showMessage('图片上传失败', 'error')
        }
    }
    
    event.target.value = ''
}

const removeMedia = (index) => {
    selectedMedia.value.splice(index, 1)
}

const openStickerPicker = () => {
    if (hasImages.value) {
        showMessage('图片和贴纸不可同时发送', 'warning')
        return
    }
    showStickerPicker.value = true
}

const openGiphyPicker = () => {
    if (hasImages.value) {
        showMessage('图片和GIF不可同时发送', 'warning')
        return
    }
    showGiphyPicker.value = true
}

const addSticker = (stickerCode) => {
    if (hasImages.value) {
        showMessage('图片和贴纸不可同时发送', 'warning')
        showEmojiPicker.value = false
        showStickerPicker.value = false
        return
    }
    
    selectedMedia.value = selectedMedia.value.filter(m => m.type !== 'sticker')
    
    const url = stickerCode.replace('[sticker:', '').replace(']', '')
    selectedMedia.value.push({ type: 'sticker', url })
    
    showEmojiPicker.value = false
    showStickerPicker.value = false
}

const addGiphy = (giphyCode) => {
    if (hasImages.value) {
        showMessage('图片和GIF不可同时发送', 'warning')
        showGiphyPicker.value = false
        return
    }
    
    selectedMedia.value = selectedMedia.value.filter(m => m.type !== 'giphy')
    
    const url = giphyCode.replace('[giphy:', '').replace(']', '')
    selectedMedia.value.push({ type: 'giphy', url })
    
    showEmojiPicker.value = false
    showGiphyPicker.value = false
}
</script>

<style scoped>
@import '@fortawesome/fontawesome-free/css/all.min.css';

.message-form-container {
    width: 100%;
}

.message-form {
    border: 1px solid var(--border-base);
    border-radius: 12px;
    padding: 20px;
    background-color: var(--bg-card);
    transition: box-shadow 0.3s ease;
}

.message-form:focus-within {
    box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.15);
}

.form-header {
    padding-bottom: 16px;
    border-bottom: 1px solid var(--border-light);
}

.emoticon-16 {
    width: 16px;
    height: 16px;
    vertical-align: middle;
    display: inline-block;
}

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

.fade-enter-active,
.fade-leave-active {
    transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
    opacity: 0;
}

@keyframes fade-in {
    from { opacity: 0; transform: translateY(-5px); }
    to { opacity: 1; transform: translateY(0); }
}

.animate-fade-in {
    animation: fade-in 0.3s ease-out;
}
</style>

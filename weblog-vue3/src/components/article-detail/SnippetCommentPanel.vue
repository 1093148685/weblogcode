<template>
  <Teleport to="body">
    <transition name="snippet-comment-modal">
      <div
        v-if="visible"
        class="snippet-comment-layer fixed inset-0 z-[118] flex items-center justify-center bg-slate-950/30 px-4 py-5 backdrop-blur-[3px]"
        @mousedown.self="$emit('close')"
      >
        <section class="snippet-comment-panel flex max-h-[min(760px,calc(100vh-40px))] w-full max-w-[720px] flex-col overflow-hidden rounded-[20px] border border-[var(--border-base,#e5e7eb)] bg-[var(--bg-card,#fff)] shadow-[0_26px_90px_rgba(15,23,42,.22)]">
          <header class="shrink-0 border-b border-[var(--border-light,#edf2f7)] px-5 pb-3 pt-5">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h2 class="text-xl font-black tracking-tight text-[var(--text-heading,#0f172a)]">片段评论</h2>
                <p class="mt-1 text-sm text-[var(--text-secondary,#64748b)]">围绕这段内容展开讨论</p>
              </div>
              <button class="grid h-9 w-9 place-items-center rounded-full bg-[var(--bg-hover,#f1f5f9)] text-[var(--text-muted,#64748b)] transition hover:text-[var(--text-heading,#0f172a)]" @click="$emit('close')" aria-label="关闭">
                <i class="fas fa-times"></i>
              </button>
            </div>

            <div class="mt-3 flex flex-wrap items-center gap-4 text-sm text-[var(--text-secondary,#475569)]">
              <span class="inline-flex items-center gap-1.5"><i class="far fa-comment-dots text-[14px]"></i>{{ totalComments }} 条评论</span>
              <span class="inline-flex items-center gap-1.5"><i class="far fa-user text-[14px]"></i>{{ participantCount }} 人参与</span>
            </div>
          </header>

          <div class="min-h-0 flex-1 overflow-y-auto px-5 py-4">
            <section class="snippet-quote-card relative overflow-hidden rounded-xl border border-amber-200 bg-amber-50 p-4">
              <div class="relative">
                <div class="flex items-center justify-between gap-3">
                  <p class="text-sm font-black text-amber-700">原文片段</p>
                  <button class="inline-flex h-8 items-center gap-1.5 rounded-md border border-blue-100 bg-white/85 px-3 text-xs font-bold text-[#2563eb] transition hover:bg-blue-50" @click="$emit('ask-ai')">
                    <i class="fas fa-wand-magic-sparkles text-[13px]"></i>
                    问问 AI
                  </button>
                </div>
                <blockquote class="mt-2 line-clamp-4 text-[15px] font-semibold leading-7 text-[#1f2937]">
                  {{ displaySelectedText }}
                </blockquote>
              </div>
            </section>

            <div class="mt-4 flex items-center justify-between">
              <h3 class="inline-flex items-center gap-2 text-base font-black text-[var(--text-heading,#0f172a)]">
                <i class="fas fa-message text-[#2563eb]"></i>
                评论
              </h3>
              <div class="inline-flex rounded-lg bg-[var(--bg-base,#f8fafc)] p-1">
                <button
                  v-for="tab in tabs"
                  :key="tab.value"
                  class="h-8 rounded-md px-4 text-xs font-bold transition"
                  :class="activeTab === tab.value ? 'bg-[var(--bg-card,#fff)] text-[#2563eb] shadow-sm' : 'text-[var(--text-secondary,#64748b)] hover:text-[var(--text-heading,#0f172a)]'"
                  @click="activeTab = tab.value"
                >
                  {{ tab.label }}
                </button>
              </div>
            </div>

            <div v-if="sortedComments.length" class="mt-3 space-y-3">
              <article
                v-for="item in sortedComments"
                :key="item.id"
                class="snippet-message-card rounded-lg border border-[var(--border-base,#e5e7eb)] bg-[var(--bg-card,#fff)] p-4 transition hover:border-[var(--border-heavy,#cbd5e1)]"
              >
                <div class="flex items-start gap-3">
                  <img v-if="item.avatar" :src="item.avatar" class="h-10 w-10 shrink-0 rounded-full border-2 border-[var(--border-base,#e5e7eb)] object-cover shadow-sm" alt="" @error="handleAvatarError" />
                  <div v-else class="grid h-10 w-10 shrink-0 place-items-center rounded-full border-2 border-[var(--border-base,#e5e7eb)] bg-[var(--bg-hover,#f1f5f9)] text-[var(--text-muted,#94a3b8)]">
                    <i class="fas fa-user text-sm"></i>
                  </div>

                  <div class="min-w-0 flex-1">
                    <div class="mb-2 flex flex-wrap items-center gap-x-2 gap-y-1">
                      <a v-if="item.website" :href="item.website" target="_blank" rel="noopener noreferrer" class="text-sm font-semibold text-blue-600 hover:text-blue-500 hover:underline underline-offset-2">{{ item.nickname }}</a>
                      <span v-else class="text-sm font-semibold text-blue-600">{{ item.nickname }}</span>
                      <span class="text-xs text-[var(--text-muted,#94a3b8)]">{{ formatTime(item.time) }}</span>
                    </div>

                    <p v-if="item.content" class="text-sm leading-relaxed text-[var(--text-body,#334155)]">
                      <ParsedContent :content="item.content" />
                    </p>
                    <div v-if="item.images?.length" class="mt-3 flex flex-wrap gap-2">
                      <img v-for="image in item.images" :key="image" :src="image" class="h-20 w-20 rounded-md border border-[var(--border-base,#e5e7eb)] object-cover" alt="" @error="handleAvatarError" />
                    </div>

                    <div v-if="item.replies?.length" class="mt-3 overflow-hidden rounded-md border border-[var(--border-base,#e5e7eb)] bg-[var(--bg-base,#f8fafc)]">
                      <div v-for="reply in item.replies" :key="reply.id" class="border-b border-[var(--border-light,#edf2f7)] px-3 py-2 last:border-b-0">
                        <div class="flex items-start gap-2">
                          <img :src="reply.avatar || fallbackAvatar" class="mt-0.5 h-7 w-7 shrink-0 rounded-full border border-[var(--border-base,#e5e7eb)] object-cover" alt="" @error="handleAvatarError" />
                          <div class="min-w-0 flex-1">
                            <div class="flex flex-wrap items-center gap-1.5 text-xs">
                              <span class="font-bold text-blue-600">{{ reply.nickname }}</span>
                              <span v-if="reply.replyToNickname" class="text-[var(--text-muted,#94a3b8)]">回复 @{{ reply.replyToNickname }}</span>
                              <span class="text-[var(--text-muted,#94a3b8)]">{{ formatTime(reply.time) }}</span>
                            </div>
                            <p v-if="reply.content" class="mt-1 text-sm leading-6 text-[var(--text-body,#475569)]">
                              <ParsedContent :content="reply.content" />
                            </p>
                            <div v-if="reply.images?.length" class="mt-2 flex flex-wrap gap-2">
                              <img v-for="image in reply.images" :key="image" :src="image" class="h-16 w-16 rounded-md border border-[var(--border-base,#e5e7eb)] object-cover" alt="" @error="handleAvatarError" />
                            </div>
                            <div class="mt-1 flex items-center gap-3">
                              <button class="inline-flex items-center gap-1 text-xs text-[var(--text-secondary,#64748b)] transition hover:text-blue-500" @click="startReply(reply, item.id)">
                                <i class="far fa-comment"></i>
                                回复
                              </button>
                              <button class="inline-flex items-center gap-1 text-xs text-[var(--text-secondary,#64748b)] transition hover:text-pink-500">
                                <i class="far fa-heart"></i>
                                <span>{{ reply.likes || 0 }}</span>
                              </button>
                              <button class="ml-auto text-xs text-[var(--text-muted,#94a3b8)] hover:text-blue-500" title="举报/更多">
                                <i class="fas fa-ellipsis"></i>
                              </button>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>

                    <div class="mt-3 flex items-center gap-4">
                      <button class="inline-flex items-center gap-1 text-xs text-[var(--text-secondary,#64748b)] transition hover:text-blue-500" @click="startReply(item)">
                        <i class="far fa-comment"></i>
                        回复
                      </button>
                      <button :class="['inline-flex items-center gap-1 text-xs transition', item.liked ? 'text-pink-500' : 'text-[var(--text-secondary,#64748b)] hover:text-pink-500']" @click="toggleLike(item)">
                        <i :class="item.liked ? 'fas fa-heart' : 'far fa-heart'"></i>
                        <span>{{ item.likes }}</span>
                      </button>
                      <button class="ml-auto text-xs text-[var(--text-muted,#94a3b8)] hover:text-blue-500" title="举报/更多">
                        <i class="fas fa-ellipsis"></i>
                      </button>
                    </div>
                  </div>
                </div>
              </article>
            </div>

            <div v-else class="mt-3 rounded-lg border border-dashed border-[var(--border-base,#dbe3ee)] bg-[var(--bg-base,#f8fafc)] px-4 py-8 text-center text-sm text-[var(--text-secondary,#64748b)]">
              还没有片段评论，来写第一条吧。
            </div>
          </div>

          <footer ref="composerRef" class="shrink-0 border-t border-[var(--border-light,#edf2f7)] bg-[var(--bg-card,#fff)] px-5 py-4">
            <form class="snippet-message-form" @submit.prevent="submit">
              <div class="flex gap-3">
                <div class="h-10 w-10 shrink-0">
                  <img v-if="commentStore.userInfo.avatar" :src="commentStore.userInfo.avatar" class="h-10 w-10 rounded-full border border-[var(--border-base,#e5e7eb)] object-cover" alt="" @error="handleAvatarError" />
                  <div v-else class="grid h-10 w-10 place-items-center rounded-full border border-[var(--border-base,#e5e7eb)] bg-[var(--bg-hover,#f1f5f9)] text-[var(--text-muted,#94a3b8)]">
                    <i class="fas fa-user text-sm"></i>
                  </div>
                </div>

                <div class="min-w-0 flex-1">
                  <div v-if="!identityCollapsed" class="mb-3 grid grid-cols-1 gap-2 sm:grid-cols-3">
                    <input v-model="formState.nickname" @blur="onNicknameInputBlur" type="text" placeholder="QQ号/昵称" class="snippet-input" />
                    <input v-model="formState.email" type="text" placeholder="邮箱（选填）" class="snippet-input" />
                    <input v-model="formState.website" type="text" placeholder="网站（选填）" class="snippet-input" />
                    <button v-if="hasIdentityInfo" type="button" class="snippet-collapse-btn" @click="identityCollapsed = true">收起信息</button>
                  </div>

                  <div v-else class="snippet-identity-row mb-3">
                    <span class="min-w-0 truncate">{{ formState.nickname }}</span>
                    <button type="button" @click="identityCollapsed = false">修改信息</button>
                  </div>

                  <div v-if="isReplyMode" class="mb-2 flex items-center justify-between gap-3 rounded-lg border border-blue-100 bg-blue-50 px-3 py-2 text-xs text-blue-700">
                    <span class="min-w-0 truncate">
                      <i class="fas fa-reply mr-1"></i>
                      正在回复 <strong>@{{ formState.replyToNickname }}</strong>
                    </span>
                    <button type="button" class="shrink-0 font-bold hover:text-blue-900" @click="cancelReply">
                      取消回复
                      <i class="fas fa-times ml-1"></i>
                    </button>
                  </div>

                  <div class="relative">
                    <textarea ref="textareaRef" v-model="formState.content" rows="3" maxlength="500" class="snippet-textarea" :placeholder="composerPlaceholder"></textarea>
                    <div class="absolute bottom-2 right-2 flex items-center gap-1">
                      <button type="button" class="p-1.5 text-[var(--text-muted,#94a3b8)] transition hover:text-blue-500" title="表情" @click.stop="toggleEmojiPicker">
                        <i class="far fa-laugh"></i>
                      </button>
                    </div>

                    <div v-if="showEmojiPicker" class="snippet-emoji-picker absolute bottom-10 right-0 z-20 w-80 rounded-lg border border-[var(--border-base,#e5e7eb)] bg-[var(--bg-card,#fff)] shadow-lg">
                      <div class="flex border-b border-[var(--border-base,#e5e7eb)]">
                        <button v-for="(category, key) in snippetEmojiCategories" :key="key" type="button" class="flex-1 whitespace-nowrap py-2 text-center text-xs transition hover:bg-[var(--bg-base,#f8fafc)]" :class="activeEmojiCategory === key ? 'border-b-2 border-blue-500 text-blue-500' : 'text-[var(--text-secondary,#64748b)]'" @click="activeEmojiCategory = key">
                          {{ category.icon }} {{ category.name }}
                        </button>
                      </div>
                      <div class="max-h-40 overflow-y-auto p-2">
                        <div class="grid grid-cols-8 gap-1">
                          <span v-for="(emoji, index) in snippetEmojiCategories[activeEmojiCategory]?.emojis || []" :key="index" class="cursor-pointer rounded p-1 text-center text-base transition hover:bg-[var(--bg-hover,#f1f5f9)]" @click="addEmoji(emoji)">{{ emoji }}</span>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div class="mt-2 flex items-center justify-between gap-3">
                    <div class="flex min-w-0 flex-wrap items-center gap-3">
                      <button type="button" class="inline-flex items-center gap-1 text-xs text-[var(--text-muted,#94a3b8)] transition hover:text-blue-500" @click="triggerImageUpload">
                        <i class="far fa-image"></i>
                        <span>图片{{ selectedMedia.length ? `(${selectedMedia.length}/3)` : '' }}</span>
                      </button>
                      <input ref="imageInputRef" type="file" accept="image/*" multiple class="hidden" @change="handleImageChange" />
                      <span class="text-xs text-[var(--text-muted,#94a3b8)]">输入 QQ 号可自动获取信息</span>
                      <span :class="['text-xs', formState.content.length > 450 ? 'text-red-500' : 'text-[var(--text-muted,#94a3b8)]']">{{ formState.content.length }}/500</span>
                    </div>
                    <button type="submit" class="publish-icon-btn bg-green-600 text-white transition hover:bg-green-500 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2" :title="submitButtonText" :aria-label="submitButtonText">
                      <i class="fas fa-paper-plane"></i>
                    </button>
                  </div>

                  <div v-if="selectedMedia.length" class="mt-3 flex flex-wrap gap-2">
                    <div v-for="(media, index) in selectedMedia" :key="media.url" class="group relative">
                      <img :src="media.url" class="h-16 w-16 rounded-md border border-[var(--border-base,#e5e7eb)] object-cover" alt="" />
                      <button type="button" class="absolute -right-2 -top-2 grid h-5 w-5 place-items-center rounded-full bg-red-500 text-[10px] text-white opacity-0 transition group-hover:opacity-100" @click="removeMedia(index)">
                        <i class="fas fa-times"></i>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </form>
          </footer>
        </section>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import axios from '@/axios'
import { getUserInfoByQQ } from '@/api/frontend/comment'
import { useCommentStore } from '@/stores/comment'
import ParsedContent from '@/components/ParsedContent.vue'
import { showMessage } from '@/composables/util'
import { useEmoji } from '@/composables/useEmoji'

const props = defineProps({
  visible: { type: Boolean, default: false },
  selectedText: { type: String, default: '' },
  comments: { type: Array, default: () => [] }
})

const emit = defineEmits(['close', 'submit', 'ask-ai'])
const commentStore = useCommentStore()
const { simpleEmojiCategories, loadBilibiliEmoji } = useEmoji()

const activeTab = ref('hot')
const localLikes = ref({})
const composerRef = ref(null)
const textareaRef = ref(null)
const imageInputRef = ref(null)
const showEmojiPicker = ref(false)
const activeEmojiCategory = ref('emoji')
const identityCollapsed = ref(false)
const selectedMedia = ref([])
const formState = ref({
  mode: 'comment',
  replyToCommentId: null,
  replyToUserId: null,
  replyToNickname: '',
  nickname: '',
  email: '',
  website: '',
  content: ''
})

const tabs = [
  { label: '热门', value: 'hot' },
  { label: '最新', value: 'new' }
]

const fallbackAvatar = 'data:image/svg+xml;utf8,%3Csvg xmlns="http://www.w3.org/2000/svg" width="80" height="80" viewBox="0 0 80 80"%3E%3Crect width="80" height="80" rx="40" fill="%23e0f2fe"/%3E%3Ccircle cx="40" cy="30" r="14" fill="%2360a5fa"/%3E%3Cpath d="M18 66c4-14 15-22 22-22s18 8 22 22" fill="%2360a5fa"/%3E%3C/svg%3E'
const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const guestNames = ['路过的风', '山海访客', '星河旅人', '云边来客', '代码旅人', '清晨来信', '晚风同学', '蓝色便签']

const isQQNumber = (value) => /^\d+$/.test(String(value || '').trim())
const generateGuestName = () => `${guestNames[Math.floor(Math.random() * guestNames.length)]}${Math.floor(Math.random() * 900 + 100)}`
const normalizeWebsite = (value) => {
  const website = String(value || '').trim()
  if (!website) return ''
  return /^https?:\/\//i.test(website) ? website : `https://${website}`
}
const normalizeImages = (value) => {
  if (Array.isArray(value)) return value.filter(Boolean)
  return String(value || '').split(',').map((item) => item.trim()).filter(Boolean)
}
const makeUserInfo = () => ({
  nickname: isQQNumber(formState.value.nickname) ? generateGuestName() : (formState.value.nickname || '读者'),
  mail: formState.value.email,
  email: formState.value.email,
  avatar: commentStore.userInfo.avatar || fallbackAvatar,
  website: normalizeWebsite(formState.value.website)
})

const normalizeReply = (reply, index) => ({
  id: reply.id || `reply-${index}`,
  userId: reply.userInfo?.id || reply.userId || reply.id || `reply-user-${index}`,
  nickname: reply.userInfo?.nickname || reply.nickname || '读者',
  avatar: reply.userInfo?.avatar || reply.avatar || fallbackAvatar,
  website: normalizeWebsite(reply.userInfo?.website || reply.website),
  time: reply.createTime || reply.time || '刚刚',
  content: reply.commentContent || reply.content || '',
  images: normalizeImages(reply.images),
  likes: reply.likes ?? 0,
  replyToNickname: reply.replyToNickname || ''
})

const normalizedComments = computed(() => props.comments.map((item, index) => ({
  id: item.id || `user-${index}`,
  userId: item.userInfo?.id || item.userId || item.id || `user-${index}`,
  nickname: item.userInfo?.nickname || item.nickname || '读者',
  avatar: item.userInfo?.avatar || item.avatar || fallbackAvatar,
  website: normalizeWebsite(item.userInfo?.website || item.website),
  time: item.createTime || item.time || '刚刚',
  content: item.commentContent || item.content || '',
  images: normalizeImages(item.images),
  likes: item.likes ?? 0,
  liked: Boolean(localLikes.value[item.id]),
  replies: (item.replies || []).map(normalizeReply)
})))

const sortedComments = computed(() => {
  const list = normalizedComments.value.map((item) => ({
    ...item,
    liked: Boolean(localLikes.value[item.id]) || item.liked,
    likes: item.likes + (localLikes.value[item.id] ? 1 : 0)
  }))
  if (activeTab.value === 'new') return list
  return [...list].sort((a, b) => b.likes - a.likes)
})

const totalComments = computed(() => normalizedComments.value.reduce((sum, item) => sum + 1 + (item.replies?.length || 0), 0))
const participantCount = computed(() => {
  const names = new Set()
  normalizedComments.value.forEach((item) => {
    names.add(item.nickname)
    item.replies?.forEach((reply) => names.add(reply.nickname))
  })
  return names.size
})
const displaySelectedText = computed(() => props.selectedText || '当前选中的正文片段')
const isReplyMode = computed(() => formState.value.mode === 'reply')
const hasIdentityInfo = computed(() => Boolean(formState.value.nickname?.trim()) && Boolean(formState.value.email?.trim()) && Boolean(formState.value.website?.trim()))
const composerPlaceholder = computed(() => isReplyMode.value ? `回复 @${formState.value.replyToNickname}...` : '写下你对这段内容的看法...')
const submitButtonText = computed(() => isReplyMode.value ? '发送回复' : '发布片段评论')
const snippetEmojiCategories = computed(() => {
  const result = {}
  Object.entries(simpleEmojiCategories.value || {}).forEach(([key, category]) => {
    if (category?.type === 'emoji') {
      result[key] = category
    }
  })
  return Object.keys(result).length ? result : {
    emoji: {
      icon: '😊',
      name: 'Emoji',
      type: 'emoji',
      emojis: ['😀', '😁', '😂', '😊', '😍', '😎', '😭', '👍', '🙏', '🎉', '🔥', '💡', '❤️', '🤔', '👌', '🙌']
    }
  }
})

const syncIdentityFromStore = () => {
  formState.value.nickname = commentStore.userInfo.nickname || formState.value.nickname || ''
  formState.value.email = commentStore.userInfo.mail || formState.value.email || ''
  formState.value.website = commentStore.userInfo.website || formState.value.website || ''
}

const resetForm = () => {
  formState.value = {
    mode: 'comment',
    replyToCommentId: null,
    replyToUserId: null,
    replyToNickname: '',
    nickname: commentStore.userInfo.nickname || formState.value.nickname || '',
    email: commentStore.userInfo.mail || formState.value.email || '',
    website: commentStore.userInfo.website || formState.value.website || '',
    content: ''
  }
  selectedMedia.value = []
}

watch(() => props.visible, (value) => {
  if (value) {
    activeTab.value = 'hot'
    showEmojiPicker.value = false
    syncIdentityFromStore()
    identityCollapsed.value = hasIdentityInfo.value
    cancelReply()
  }
})

watch(hasIdentityInfo, (ready) => {
  if (ready) {
    identityCollapsed.value = true
  }
})

const toggleEmojiPicker = () => {
  showEmojiPicker.value = !showEmojiPicker.value
}

const closeEmojiPicker = (event) => {
  if (!event.target.closest('.snippet-emoji-picker') && !event.target.closest('.snippet-textarea')) {
    showEmojiPicker.value = false
  }
}

onMounted(() => {
  loadBilibiliEmoji()
  document.addEventListener('click', closeEmojiPicker)
})

onUnmounted(() => {
  document.removeEventListener('click', closeEmojiPicker)
})

const addEmoji = (emoji) => {
  if (formState.value.content.length >= 500) return
  formState.value.content += typeof emoji === 'object' && emoji.text ? `[${emoji.text}]` : emoji
  showEmojiPicker.value = false
  textareaRef.value?.focus()
}

const appendQuickReply = (text) => {
  formState.value.content = formState.value.content ? `${formState.value.content} ${text}` : text
  textareaRef.value?.focus()
}

const onNicknameInputBlur = async () => {
  const nickname = formState.value.nickname
  if (!isQQNumber(nickname)) {
    commentStore.userInfo.nickname = nickname
    return
  }

  try {
    const res = await getUserInfoByQQ(nickname)
    if (!res.success) return
    commentStore.userInfo.avatar = res.data.avatar
    commentStore.userInfo.mail = res.data.mail
    formState.value.email = res.data.mail || formState.value.email
    if (res.data.nickname && !isQQNumber(res.data.nickname)) {
      formState.value.nickname = res.data.nickname
      commentStore.userInfo.nickname = res.data.nickname
    } else {
      formState.value.nickname = generateGuestName()
      commentStore.userInfo.nickname = formState.value.nickname
    }
  } catch {
    formState.value.nickname = generateGuestName()
    commentStore.userInfo.nickname = formState.value.nickname
  }
}

const triggerImageUpload = () => {
  imageInputRef.value?.click()
}

const handleImageChange = async (event) => {
  const files = event.target.files
  if (!files?.length) return

  for (const file of files) {
    if (selectedMedia.value.length >= 3) {
      showMessage('最多只能上传3张图片', 'warning')
      break
    }
    if (file.size > 5 * 1024 * 1024) {
      showMessage('图片大小不能超过 5MB', 'warning')
      continue
    }
    if (!['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/bmp'].includes(file.type)) {
      showMessage('只允许上传图片文件', 'warning')
      continue
    }

    try {
      const formData = new FormData()
      formData.append('file', file)
      const res = await axios.post('/comment/file/upload', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      if (res.success && res.data) {
        selectedMedia.value.push({ type: 'image', url: res.data })
      } else {
        showMessage(res.message || '图片上传失败', 'error')
      }
    } catch {
      showMessage('图片上传失败', 'error')
    }
  }

  event.target.value = ''
}

const removeMedia = (index) => {
  selectedMedia.value.splice(index, 1)
}

const toggleLike = (item) => {
  localLikes.value = {
    ...localLikes.value,
    [item.id]: !localLikes.value[item.id]
  }
}

const startReply = (item, parentId = null) => {
  formState.value.mode = 'reply'
  formState.value.replyToCommentId = parentId || item.id
  formState.value.replyToUserId = item.userId || item.id
  formState.value.replyToNickname = item.nickname || '读者'
  nextTick(() => {
    composerRef.value?.scrollIntoView({ behavior: 'smooth', block: 'end' })
    textareaRef.value?.focus()
  })
}

const cancelReply = () => {
  formState.value.mode = 'comment'
  formState.value.replyToCommentId = null
  formState.value.replyToUserId = null
  formState.value.replyToNickname = ''
}

const submit = async () => {
  if (isQQNumber(formState.value.nickname) && (!formState.value.email || !commentStore.userInfo.avatar)) {
    await onNicknameInputBlur()
  }

  const text = formState.value.content.trim()
  if (!formState.value.nickname.trim()) {
    showMessage('请填写 QQ 号或昵称', 'warning')
    return
  }
  if (!formState.value.email.trim() || !emailRegex.test(formState.value.email)) {
    showMessage('邮箱格式不正确', 'warning')
    return
  }
  if (!text && selectedMedia.value.length === 0) {
    showMessage(isReplyMode.value ? '请填写回复内容' : '请填写片段评论内容', 'warning')
    return
  }

  commentStore.userInfo.nickname = formState.value.nickname
  commentStore.userInfo.mail = formState.value.email
  commentStore.userInfo.website = formState.value.website

  emit('submit', {
    mode: formState.value.mode,
    content: text,
    images: selectedMedia.value.map((item) => item.url),
    parentCommentId: isReplyMode.value ? formState.value.replyToCommentId : null,
    replyToUserId: isReplyMode.value ? formState.value.replyToUserId : null,
    replyToNickname: isReplyMode.value ? formState.value.replyToNickname : '',
    userInfo: makeUserInfo()
  })

  resetForm()
}

const handleAvatarError = (event) => {
  event.target.src = fallbackAvatar
}

const formatTime = (time) => {
  if (!time) return ''
  if (time === '刚刚') return time
  const date = new Date(time)
  if (Number.isNaN(date.getTime())) return time
  const diff = Date.now() - date.getTime()
  if (diff < 60000) return '刚刚'
  if (diff < 3600000) return `${Math.floor(diff / 60000)} 分钟前`
  if (diff < 86400000) return `${Math.floor(diff / 3600000)} 小时前`
  if (diff < 604800000) return `${Math.floor(diff / 86400000)} 天前`
  return date.toLocaleDateString('zh-CN')
}
</script>

<style scoped>
@import '@fortawesome/fontawesome-free/css/all.min.css';

.snippet-comment-modal-enter-active,
.snippet-comment-modal-leave-active {
  transition: opacity 0.2s ease;
}

.snippet-comment-modal-enter-active .snippet-comment-panel,
.snippet-comment-modal-leave-active .snippet-comment-panel {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.snippet-comment-modal-enter-from,
.snippet-comment-modal-leave-to {
  opacity: 0;
}

.snippet-comment-modal-enter-from .snippet-comment-panel,
.snippet-comment-modal-leave-to .snippet-comment-panel {
  opacity: 0;
  transform: translateY(18px) scale(0.98);
}

.snippet-input,
.snippet-textarea {
  width: 100%;
  border: 1px solid var(--border-base, #e5e7eb);
  border-radius: 8px;
  background: var(--bg-base, #f8fafc);
  color: var(--text-heading, #0f172a);
  font-size: 14px;
  outline: none;
  transition: border-color 0.18s ease, box-shadow 0.18s ease;
}

.snippet-input {
  height: 42px;
  padding: 0 12px;
}

.snippet-textarea {
  min-height: 86px;
  resize: none;
  padding: 12px;
}

.snippet-input::placeholder,
.snippet-textarea::placeholder {
  color: var(--text-placeholder, #94a3b8);
}

.snippet-input:focus,
.snippet-textarea:focus {
  border-color: var(--color-primary, #2563eb);
  box-shadow: 0 0 0 3px var(--focus-ring, rgba(37, 99, 235, 0.12));
}

.snippet-identity-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  border: 1px solid var(--border-base, #e5e7eb);
  border-radius: 8px;
  background: var(--bg-base, #f8fafc);
  padding: 9px 12px;
  color: var(--text-secondary, #64748b);
  font-size: 13px;
}

.snippet-identity-row button {
  flex: 0 0 auto;
  color: var(--color-primary, #2563eb);
  font-size: 12px;
  font-weight: 800;
}

.snippet-collapse-btn {
  height: 38px;
  border: 1px solid var(--border-base, #e5e7eb);
  border-radius: 8px;
  color: var(--text-secondary, #64748b);
  font-size: 12px;
  font-weight: 800;
  transition: border-color 0.2s ease, color 0.2s ease;
}

.snippet-collapse-btn:hover {
  border-color: var(--color-primary, #2563eb);
  color: var(--color-primary, #2563eb);
}

.publish-icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 38px;
  flex: 0 0 44px;
  border-radius: 8px;
}

.publish-icon-btn i {
  font-size: 15px;
}

.snippet-message-card :deep(img),
.snippet-message-card :deep(video) {
  max-width: 100%;
}

@media (max-width: 768px) {
  .snippet-comment-layer {
    align-items: flex-end;
    padding: 0 10px 10px;
  }

  .snippet-comment-panel {
    max-height: min(82vh, 680px);
    max-width: 100%;
    border-radius: 18px;
  }

  .snippet-comment-panel header,
  .snippet-comment-panel footer,
  .snippet-comment-panel > div {
    padding-left: 14px;
    padding-right: 14px;
  }

  .snippet-comment-panel h2 {
    font-size: 20px;
  }

  .snippet-input {
    height: 38px;
  }

  .snippet-emoji-picker {
    right: auto;
    left: -54px;
    width: min(320px, calc(100vw - 40px));
  }
}

:global(html.dark) .snippet-comment-layer {
  background: rgba(13, 17, 23, 0.58);
}

:global(html.dark) .snippet-quote-card {
  border-color: rgba(245, 158, 11, 0.34);
  background: rgba(234, 179, 8, 0.12);
}

:global(html.dark) .snippet-quote-card blockquote {
  color: #f0f6fc;
}

:global(html.dark) .snippet-message-card {
  background: var(--bg-card, #161b22);
}

:global(html.dark) .snippet-comment-panel .bg-blue-50 {
  border-color: rgba(88, 166, 255, 0.28);
  background: rgba(88, 166, 255, 0.1);
  color: #58a6ff;
}
</style>

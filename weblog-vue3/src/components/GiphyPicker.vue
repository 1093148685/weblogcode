<template>
    <div v-if="show" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" @click.self="$emit('close')">
        <div class="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-[600px] max-h-[500px] flex flex-col">
            <div class="p-4 border-b border-gray-200 dark:border-gray-700">
                <div class="flex items-center gap-2">
                    <input 
                        v-model="searchQuery"
                        @keyup.enter="search"
                        type="text" 
                        placeholder="搜索 GIF..."
                        class="flex-1 px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:border-blue-500"
                    >
                    <button 
                        @click="search"
                        class="px-4 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-md transition-colors">
                        <i class="fas fa-search"></i>
                    </button>
                    <button @click="$emit('close')" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 ml-2">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                <div class="flex gap-2 mt-3 overflow-x-auto pb-1">
                    <button 
                        v-for="tag in hotTags" 
                        :key="tag"
                        @click="searchByTag(tag)"
                        class="px-2 py-1 text-xs bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded hover:bg-gray-200 dark:hover:bg-gray-600 whitespace-nowrap">
                        {{ tag }}
                    </button>
                </div>
            </div>
            
            <div class="flex-1 overflow-y-auto p-4">
                <div v-if="loading" class="flex items-center justify-center h-32">
                    <i class="fas fa-spinner fa-spin text-gray-400"></i>
                </div>
                <div v-else-if="gifs.length > 0" class="grid grid-cols-3 gap-2">
                    <div 
                        v-for="gif in gifs" 
                        :key="gif.giphyId"
                        @click="selectGif(gif)"
                        class="cursor-pointer hover:opacity-80 transition-opacity rounded overflow-hidden"
                    >
                        <img 
                            :src="gif.previewUrl || gif.originalUrl" 
                            :alt="gif.title"
                            class="w-full object-contain bg-gray-100 dark:bg-gray-700"
                            loading="lazy"
                        >
                    </div>
                </div>
                <div v-else class="text-center text-gray-500 py-8">
                    {{ searchQuery ? '未找到相关 GIF' : '搜索 GIF...' }}
                </div>
                
                <div v-if="hasMore" class="text-center mt-4">
                    <button 
                        @click="loadMore"
                        :disabled="loadingMore"
                        class="px-4 py-2 text-sm bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 rounded-md hover:bg-gray-200 dark:hover:bg-gray-600 disabled:opacity-50">
                        {{ loadingMore ? '加载中...' : '加载更多' }}
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref } from 'vue'
import { searchGiphy, getTrendingGiphy } from '@/api/frontend/sticker'

const props = defineProps({
    show: {
        type: Boolean,
        default: false
    }
})

const emit = defineEmits(['close', 'select'])

const searchQuery = ref('')
const gifs = ref([])
const loading = ref(false)
const loadingMore = ref(false)
const offset = ref(0)
const hasMore = ref(false)

const hotTags = ['doge', 'happy', 'sad', 'laugh', 'cry', 'love', 'ok', 'yes', 'no', 'wow']

const search = async () => {
    if (!searchQuery.value.trim()) return
    
    loading.value = true
    offset.value = 0
    try {
        const res = await searchGiphy(searchQuery.value, 30, 0)
        if (res.success && res.data) {
            gifs.value = res.data
            hasMore.value = res.data.length === 30
        }
    } catch (e) {
        console.error('搜索 GIF 失败', e)
    } finally {
        loading.value = false
    }
}

const searchByTag = async (tag) => {
    searchQuery.value = tag
    await search()
}

const loadMore = async () => {
    if (loadingMore.value || !hasMore.value) return
    
    loadingMore.value = true
    offset.value += 30
    try {
        const res = await searchGiphy(searchQuery.value, 30, offset.value)
        if (res.success && res.data) {
            gifs.value.push(...res.data)
            hasMore.value = res.data.length === 30
        }
    } catch (e) {
        console.error('加载更多失败', e)
    } finally {
        loadingMore.value = false
    }
}

const selectGif = (gif) => {
    const url = gif.previewUrl || gif.originalUrl
    emit('select', `[giphy:${url}]`)
    emit('close')
}

const loadTrending = async () => {
    loading.value = true
    try {
        const res = await getTrendingGiphy(30)
        if (res.success && res.data) {
            gifs.value = res.data
            hasMore.value = res.data.length === 30
        }
    } catch (e) {
        console.error('加载热门 GIF 失败', e)
    } finally {
        loading.value = false
    }
}
</script>
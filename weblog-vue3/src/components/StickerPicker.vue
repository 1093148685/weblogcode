<template>
    <div v-if="show" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" @click.self="$emit('close')">
        <div class="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-[500px] max-h-[500px] flex flex-col">
            <div class="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
                <div class="flex items-center gap-4">
                    <button 
                        v-for="pack in packs" 
                        :key="pack.id"
                        @click="selectedPackId = pack.id"
                        :class="[
                            'px-3 py-1 text-sm rounded-md transition-colors',
                            selectedPackId === pack.id 
                                ? 'bg-blue-500 text-white' 
                                : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'
                        ]">
                        {{ pack.name }}
                    </button>
                </div>
                <button @click="$emit('close')" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            
            <div class="flex-1 overflow-y-auto p-4">
                <div v-if="loading" class="flex items-center justify-center h-32">
                    <i class="fas fa-spinner fa-spin text-gray-400"></i>
                </div>
                <div v-else-if="currentPack" class="space-y-3">
                    <div v-for="category in currentPack.categories" :key="category.category">
                        <div v-if="category.category !== '默认'" class="text-xs text-gray-500 dark:text-gray-400 mb-2">{{ category.category }}</div>
                            <div class="grid grid-cols-6 gap-2">
                            <div 
                                v-for="sticker in category.stickers" 
                                :key="sticker.id"
                                @click="selectSticker(sticker)"
                                class="cursor-pointer hover:opacity-80 transition-opacity p-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700"
                            >
                                <video v-if="isAnimatedSticker(sticker)"
                                    :src="sticker.thumbnailUrl || sticker.imageUrl"
                                    class="w-full aspect-square object-contain"
                                    autoplay loop muted playsinline></video>
                                <img v-else 
                                    :src="sticker.thumbnailUrl || sticker.imageUrl" 
                                    :alt="sticker.category"
                                    class="w-full aspect-square object-contain"
                                    loading="lazy"
                                >
                            </div>
                        </div>
                    </div>
                    <div v-if="!hasStickers" class="text-center text-gray-500 py-8">
                        该贴纸包暂无贴纸
                    </div>
                </div>
                <div v-else class="text-center text-gray-500 py-8">
                    暂无可用贴纸包
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { getStickerPacks } from '@/api/frontend/sticker'

const props = defineProps({
    show: {
        type: Boolean,
        default: false
    }
})

const emit = defineEmits(['close', 'select'])

const packs = ref([])
const selectedPackId = ref(null)
const loading = ref(false)

const currentPack = computed(() => {
    return packs.value.find(p => p.id === selectedPackId.value)
})

const hasStickers = computed(() => {
    return currentPack.value?.categories?.some(c => c.stickers?.length > 0)
})

const loadPacks = async () => {
    loading.value = true
    try {
        const res = await getStickerPacks()
        if (res.success && res.data) {
            packs.value = res.data
            if (packs.value.length > 0 && !selectedPackId.value) {
                selectedPackId.value = packs.value[0].id
            }
        }
    } catch (e) {
        console.error('加载贴纸包失败', e)
    } finally {
        loading.value = false
    }
}

const selectSticker = (sticker) => {
    const url = sticker.thumbnailUrl || sticker.imageUrl
    emit('select', `[sticker:${url}]`)
    emit('close')
}

const isAnimatedSticker = (sticker) => {
    if (sticker.isAnimated) return true
    const url = sticker.imageUrl || ''
    const lower = url.toLowerCase()
    return lower.endsWith('.webm') || lower.endsWith('.gif') || lower.endsWith('.mp4')
}

watch(() => props.show, async (val) => {
    if (val && packs.value.length === 0) {
        await loadPacks()
    }
})
</script>
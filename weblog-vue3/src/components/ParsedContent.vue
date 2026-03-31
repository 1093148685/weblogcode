<template>
    <span class="parsed-content dark:text-gray-300">
        <template v-for="(part, index) in parsedParts" :key="index">
            <SpoilerText 
                v-if="part.isSpoiler"
                :scale="part.scale"
                :density="part.density"
                :particle-lifetime="part.lifetime"
                :min-velocity="part.minVelocity"
                :max-velocity="part.maxVelocity"
                :reveal-duration="part.revealDuration"
            >
                {{ part.content }}
            </SpoilerText>
            <span v-else v-html="renderEmoticonAndMarkdown(part.content)" class="dark:text-gray-300"></span>
        </template>
    </span>
</template>

<script setup>
import { computed } from 'vue'
import SpoilerText from './SpoilerText.vue'
import { useEmoji } from '@/composables/useEmoji'

const props = defineProps({
    content: { type: String, required: true }
})

const { renderEmoticonAndMarkdown } = useEmoji()

const parsedParts = computed(() => {
    if (!props.content) return []
    
    const parts = []
    const regex = /<s(?:\s+([^>]*))?>(.*?)<\/s>/gi
    let lastIndex = 0
    let match
    
    while ((match = regex.exec(props.content)) !== null) {
        if (match.index > lastIndex) {
            parts.push({
                isSpoiler: false,
                content: props.content.substring(lastIndex, match.index)
            })
        }
        
        const attrs = match[1] || ''
        const text = match[2]
        
        const scale = extractAttr(attrs, 'scale', 1)
        const density = extractAttr(attrs, 'density', 8)
        const lifetime = extractAttr(attrs, 'lifetime', 120)
        const minVelocity = extractAttr(attrs, 'minvelocity', 0.01)
        const maxVelocity = extractAttr(attrs, 'maxvelocity', 0.05)
        const revealDuration = extractAttr(attrs, 'reveal', 300)
        
        parts.push({
            isSpoiler: true,
            content: text,
            scale,
            density,
            lifetime,
            minVelocity,
            maxVelocity,
            revealDuration
        })
        
        lastIndex = match.index + match[0].length
    }
    
    if (lastIndex < props.content.length) {
        parts.push({
            isSpoiler: false,
            content: props.content.substring(lastIndex)
        })
    }
    
    return parts
})

const extractAttr = (str, name, defaultVal) => {
    const match = str.match(new RegExp(`${name}=([\\d.]+)`))
    return match ? parseFloat(match[1]) : defaultVal
}
</script>

<style scoped>
.parsed-content {
    display: inline-block;
}

.parsed-content :deep(a) {
    @apply text-blue-600 dark:text-blue-400;
}

.parsed-content :deep(strong),
.parsed-content :deep(b) {
    @apply text-gray-900 dark:text-gray-100;
}

.parsed-content :deep(code) {
    @apply bg-gray-100 dark:bg-gray-700 text-pink-600 dark:text-pink-400 px-1 py-0.5 rounded text-sm;
}

.parsed-content :deep(pre) {
    @apply bg-gray-100 dark:bg-gray-800 p-3 rounded overflow-x-auto;
}

.parsed-content :deep(blockquote) {
    @apply border-l-4 border-gray-300 dark:border-gray-600 pl-4 my-2 text-gray-600 dark:text-gray-400;
}

.parsed-content :deep(p) {
    @apply text-gray-700 dark:text-gray-300;
}
</style>

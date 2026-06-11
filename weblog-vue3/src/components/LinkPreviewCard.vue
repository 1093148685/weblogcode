<template>
    <div v-if="preview" class="link-preview-card" @click="openUrl">
        <div class="link-content">
            <div class="link-header">
                <img v-if="faviconLoaded" :src="preview.faviconUrl" class="favicon" @error="handleFaviconError">
                <span v-else class="favicon-placeholder">
                    <i class="fas fa-globe"></i>
                </span>
                <span class="domain">{{ preview.domain }}</span>
            </div>
            <div class="link-title">{{ preview.title }}</div>
            <div v-if="preview.description" class="link-desc">{{ preview.description }}</div>
        </div>
        <img v-if="preview.imageUrl" :src="preview.imageUrl" class="preview-image" @error="handleImageError">
    </div>
</template>

<script setup>
import { ref } from 'vue'

const props = defineProps({
    preview: {
        type: Object,
        required: true
    }
})

const faviconLoaded = ref(true)

const handleFaviconError = () => {
    faviconLoaded.value = false
}

const handleImageError = (e) => {
    e.target.style.display = 'none'
}

const openUrl = () => {
    window.open(props.preview.url, '_blank')
}
</script>

<style scoped>
.link-preview-card {
    display: flex;
    gap: 12px;
    padding: 12px;
    background: #f9fafb;
    border: 1px solid #e5e7eb;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.2s;
    max-width: 400px;
}

.link-preview-card:hover {
    border-color: #3b82f6;
    background: #f3f4f6;
}

.dark .link-preview-card {
    background: #1f2937;
    border-color: #374151;
}

.dark .link-preview-card:hover {
    border-color: #3b82f6;
    background: #374151;
}

.link-content {
    flex: 1;
    min-width: 0;
}

.link-header {
    display: flex;
    align-items: center;
    gap: 6px;
    margin-bottom: 4px;
}

.favicon {
    width: 16px;
    height: 16px;
    object-fit: contain;
}

.favicon-placeholder {
    width: 16px;
    height: 16px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #9ca3af;
}

.domain {
    font-size: 12px;
    color: #6b7280;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.dark .domain {
    color: #9ca3af;
}

.link-title {
    font-size: 14px;
    font-weight: 500;
    color: #111827;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    margin-bottom: 2px;
}

.dark .link-title {
    color: #f9fafb;
}

.link-desc {
    font-size: 12px;
    color: #6b7280;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
    line-height: 1.4;
}

.dark .link-desc {
    color: #9ca3af;
}

.preview-image {
    width: 80px;
    height: 80px;
    object-fit: cover;
    border-radius: 4px;
    flex-shrink: 0;
}

@media (max-width: 480px) {
    .preview-image {
        width: 60px;
        height: 60px;
    }
}
</style>
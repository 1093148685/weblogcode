<template>
    <div class="float-actions">
        <button v-if="showCommentButton" class="float-action" title="到评论区" @click="scrollToComments">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24">
                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h6m-8 8 4-4h8a4 4 0 0 0 4-4V7a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v5a4 4 0 0 0 4 4" />
            </svg>
        </button>
        <button v-show="showScrollToTopBtn" class="float-action" title="回到顶部" @click="scrollToTop">
            <svg class="w-4 h-4" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 10 14">
                <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13V1m0 0L1 5m4-4 4 4"></path>
            </svg>
        </button>
    </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'

defineProps({
    showCommentButton: {
        type: Boolean,
        default: false
    }
})

const showScrollToTopBtn = ref(false)

onMounted(() => window.addEventListener('scroll', handleScroll, { passive: true }))
onBeforeUnmount(() => window.removeEventListener('scroll', handleScroll))

const handleScroll = () => {
    showScrollToTopBtn.value = window.scrollY > 300
}

const scrollToTop = () => {
    window.scrollTo({
        top: 0,
        behavior: 'smooth'
    })
}

const scrollToComments = () => {
    document.getElementById('comments')?.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
    })
}
</script>

<style scoped>
.float-actions {
    position: fixed;
    right: 0.5rem;
    bottom: 0.5rem;
    z-index: 50;
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.float-action {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 44px;
    height: 44px;
    cursor: pointer;
    border: 1px solid var(--border-base);
    border-radius: 14px;
    color: var(--text-secondary);
    background: color-mix(in srgb, var(--bg-card) 92%, transparent);
    box-shadow: var(--shadow-lg);
    backdrop-filter: blur(12px);
    transition: transform 0.2s ease, background 0.2s ease, color 0.2s ease;
}

.float-action:hover {
    color: var(--color-primary);
    background: var(--bg-hover);
    transform: translateY(-2px);
}

@media (min-width: 768px) {
    .float-actions {
        right: 2.5rem;
        bottom: 2.5rem;
    }
}
</style>

<template>
    <div
        class="skeleton-box"
        :style="{
            width: width,
            height: height,
            borderRadius: borderRadius,
        }"
    ></div>
</template>

<script setup>
defineProps({
    width: {
        type: String,
        default: '100%'
    },
    height: {
        type: String,
        default: '1rem'
    },
    borderRadius: {
        type: String,
        default: '6px'
    }
})
</script>

<style scoped>
.skeleton-box {
    display: block;
    position: relative;
    overflow: hidden;
    background-color: var(--border-base, #e2e8f0);
    flex-shrink: 0;
}

.skeleton-box::after {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: linear-gradient(
        90deg,
        transparent 0%,
        rgba(255, 255, 255, 0.35) 50%,
        transparent 100%
    );
    animation: skeleton-shimmer 1.4s ease-in-out infinite;
}

@keyframes skeleton-shimmer {
    0% {
        transform: translateX(-100%);
    }
    100% {
        transform: translateX(100%);
    }
}
</style>

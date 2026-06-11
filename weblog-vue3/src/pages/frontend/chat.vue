<template>
    <div class="page-shell chat-page !p-0">
        <div class="chat-page-frame">
            <ChatPanel />
        </div>
    </div>
</template>

<script setup>
import ChatPanel from '@/components/chat/ChatPanel.vue'

defineOptions({ name: 'FrontendChat' })
</script>

<style scoped>
.chat-page {
    min-height: 100vh;
    background: var(--bg-base);
}

.chat-page-frame {
    height: 100vh;
    min-height: 0;
}
</style>

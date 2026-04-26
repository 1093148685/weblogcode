<template>
    <div class="h-screen flex flex-col overflow-hidden bg-[var(--bg-base)] page-shell !p-0">
        <ChatPanel />
    </div>
</template>

<script setup>
import ChatPanel from '@/components/chat/ChatPanel.vue'

defineOptions({ name: 'FrontendChat' })
</script>

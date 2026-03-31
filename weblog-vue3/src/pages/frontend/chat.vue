<template>
    <div class="h-screen flex flex-col overflow-hidden bg-white dark:bg-gray-900">
        <ChatPanel />
    </div>
</template>

<script setup>
import ChatPanel from '@/components/chat/ChatPanel.vue'

defineOptions({ name: 'FrontendChat' })
</script>

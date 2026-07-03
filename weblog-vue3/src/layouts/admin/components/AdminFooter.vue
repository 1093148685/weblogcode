<template>
    <div class="bg-white py-5 flex items-center justify-center text-sm text-gray-500 shadow-none">
        Copyright © {{ currentYear }}. All rights reserved. Powered by&nbsp;<a class="underline" href="https://github.com/1093148685/weblogcode" target="_blank">前锦阁</a>
    </div>
</template>

<script setup>
const currentYear = new Date().getFullYear()
</script>
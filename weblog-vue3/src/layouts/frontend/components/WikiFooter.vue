<template>
    <footer class="bg-white border-gray-200 border-t dark:bg-gray-900 dark:border-gray-800 mt-auto">
        <div class="w-full mx-auto max-w-screen-xl p-4 md:flex md:items-center md:justify-between">
            <span class="text-sm text-gray-500 sm:text-center dark:text-gray-400">© {{ currentYear }} <a href="https://github.com/1093148685/weblogcode"
                    class="hover:underline">前锦阁</a>. All Rights Reserved.
            </span>
            <ul class="flex flex-wrap items-center mt-3 text-sm text-gray-500 dark:text-gray-400 sm:mt-0">
                <li>
                    <a href="https://beian.miit.gov.cn/" target="_blank" class="mr-4 hover:underline md:mr-6">桂ICP备2025052831号</a>
                </li>
            </ul>
        </div>
    </footer>
</template>

<script setup>
const currentYear = new Date().getFullYear()
</script>
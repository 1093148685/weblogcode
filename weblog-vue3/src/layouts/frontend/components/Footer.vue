<template>
    <footer class="site-footer mt-auto border-t border-[var(--border-base)] bg-[var(--bg-card)]">
        <div class="max-w-content mx-auto px-6 py-5">
            <div class="flex flex-col sm:flex-row items-center justify-between gap-2 text-sm text-[var(--text-muted)]">
                <span>© {{ currentYear }} {{ blogSettingsStore.blogSettings.name || 'Weblog' }}. All Rights Reserved.</span>
                <span class="flex items-center gap-1.5">
                    <a href="https://beian.miit.gov.cn/" target="_blank"
                        class="hover:text-[var(--color-primary)] transition-colors">
                        桂ICP备2025052831号
                    </a>
                </span>
            </div>
        </div>
    </footer>
</template>

<script setup>
import { useBlogSettingsStore } from '@/stores/blogsettings'

const blogSettingsStore = useBlogSettingsStore()
const currentYear = new Date().getFullYear()
</script>

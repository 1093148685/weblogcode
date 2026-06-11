import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export const useThemeStore = defineStore('adminTheme', () => {
    // 后台主题：'light' | 'dark'
    const mode = ref('light')

    // 应用主题到 html 元素
    function applyTheme(val) {
        if (val === 'dark') {
            document.documentElement.classList.add('dark')
        } else {
            document.documentElement.classList.remove('dark')
        }
    }

    // 切换夜晚/白天
    function toggle() {
        mode.value = mode.value === 'light' ? 'dark' : 'light'
    }

    // 初始化时应用（页面刷新后由 persist 恢复值，再执行 applyTheme）
    function init() {
        applyTheme(mode.value)
    }

    // 监听变化实时切换
    watch(mode, (val) => {
        applyTheme(val)
    })

    return { mode, toggle, init }
}, {
    persist: true,
})

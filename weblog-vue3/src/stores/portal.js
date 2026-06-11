import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getCategoryList } from '@/api/frontend/category'
import { getTagList } from '@/api/frontend/tag'

export const usePortalStore = defineStore('portal', () => {
    const categories = ref([])
    const tags = ref([])
    let loaded = false
    let loading = false

    async function loadSidebarData() {
        if (loaded || loading) return
        loading = true
        try {
            const [catRes, tagRes] = await Promise.all([
                getCategoryList({ size: 10 }),
                getTagList({ size: 20 })
            ])
            if (catRes.success) categories.value = catRes.data
            if (tagRes.success) tags.value = tagRes.data
            loaded = true
        } finally {
            loading = false
        }
    }

    return { categories, tags, loadSidebarData }
})

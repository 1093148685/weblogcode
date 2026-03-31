<template>
    <div class="comment-admin-login">
        <!-- 未登录状态 -->
        <div v-if="!commentAdminStore.isLoggedIn" class="flex items-center gap-2">
            <button @click="showLoginModal = true"
                class="px-3 py-1 text-xs bg-blue-600 hover:bg-blue-500 text-white rounded-md transition-colors">
                管理员登录
            </button>
        </div>

        <!-- 已登录状态 -->
        <div v-else class="flex items-center gap-2">
            <span class="text-xs text-gray-500">欢迎，{{ commentAdminStore.adminInfo?.nickname || commentAdminStore.adminInfo?.username }}</span>
            <button @click="handleLogout"
                class="px-2 py-1 text-xs text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 transition-colors">
                退出
            </button>
        </div>

        <!-- 登录弹窗 -->
        <div v-if="showLoginModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" @click.self="showLoginModal = false">
            <div class="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-80 p-6">
                <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">评论管理员登录</h3>
                
                <form @submit.prevent="handleLogin">
                    <div class="space-y-4">
                        <div>
                            <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">用户名</label>
                            <input v-model="loginForm.username" type="text" required
                                class="w-full px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:border-blue-500">
                        </div>
                        <div>
                            <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">密码</label>
                            <input v-model="loginForm.password" type="password" required
                                class="w-full px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:border-blue-500">
                        </div>
                    </div>
                    
                    <div class="mt-6 flex justify-end gap-2">
                        <button type="button" @click="showLoginModal = false"
                            class="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 transition-colors">
                            取消
                        </button>
                        <button type="submit" :disabled="loading"
                            class="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-500 text-white rounded-md transition-colors disabled:opacity-50">
                            {{ loading ? '登录中...' : '登录' }}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useCommentAdminStore } from '@/stores/commentAdmin'
import { commentAdminLogin, commentAdminLogout } from '@/api/frontend/commentAdmin'
import { showMessage } from '@/composables/util'

const commentAdminStore = useCommentAdminStore()

const showLoginModal = ref(false)
const loading = ref(false)
const loginForm = reactive({
    username: '',
    password: ''
})

const handleLogin = async () => {
    if (!loginForm.username || !loginForm.password) {
        showMessage('请填写用户名和密码', 'warning')
        return
    }

    loading.value = true
    try {
        const res = await commentAdminLogin(loginForm)
        if (res.success) {
            commentAdminStore.isLoggedIn = true
            commentAdminStore.adminInfo = res.data
            commentAdminStore.token = res.data.token
            showLoginModal.value = false
            loginForm.username = ''
            loginForm.password = ''
            showMessage('登录成功', 'success')
        } else {
            showMessage(res.message || '登录失败', 'error')
        }
    } catch (e) {
        showMessage('登录失败', 'error')
    } finally {
        loading.value = false
    }
}

const handleLogout = async () => {
    try {
        await commentAdminLogout()
    } catch (e) {
        // ignore
    }
    commentAdminStore.isLoggedIn = false
    commentAdminStore.adminInfo = null
    commentAdminStore.token = null
    showMessage('已退出登录', 'success')
}
</script>

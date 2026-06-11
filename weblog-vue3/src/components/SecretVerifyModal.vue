<template>
    <div v-if="show" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" @click.self="handleClose">
        <div class="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-96 p-6">
            <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">输入密钥查看内容</h3>
            
            <div class="space-y-4">
                <div>
                    <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">密钥</label>
                    <input v-model="form.secretKey" type="password" 
                        class="w-full px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:border-blue-500"
                        placeholder="请输入密钥">
                </div>
                <div>
                    <label class="block text-sm text-gray-700 dark:text-gray-300 mb-1">验证码：{{ captchaQuestion }}</label>
                    <div class="flex gap-2">
                        <input v-model="form.captcha" type="text"
                            class="flex-1 px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:border-blue-500"
                            placeholder="请输入验证码"
                            @keydown.enter.prevent="handleVerify">
                        <button @click="loadCaptcha" :disabled="captchaLoading"
                            class="px-3 py-2 text-sm bg-gray-100 dark:bg-gray-600 hover:bg-gray-200 dark:hover:bg-gray-500 rounded-md transition-colors disabled:opacity-50">
                            {{ captchaLoading ? '加载中...' : '刷新' }}
                        </button>
                    </div>
                </div>
            </div>
            
            <div v-if="error" class="mt-3 text-sm text-red-500">{{ error }}</div>
            
            <div class="mt-6 flex justify-end gap-2">
                <button type="button" @click="handleClose"
                    class="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 transition-colors">
                    取消
                </button>
                <button @click="handleVerify" :disabled="loading"
                    class="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-500 text-white rounded-md transition-colors disabled:opacity-50">
                    {{ loading ? '验证中...' : '验证' }}
                </button>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, watch } from 'vue'
import { getCaptcha, verifySecret } from '@/api/frontend/commentAdmin'
import { showMessage } from '@/composables/util'

const props = defineProps({
    show: {
        type: Boolean,
        default: false
    },
    commentId: {
        type: Number,
        default: null
    }
})

const emit = defineEmits(['close', 'success'])

const form = reactive({
    secretKey: '',
    captcha: ''
})

const captchaQuestion = ref('')
const captchaId = ref('')
const loading = ref(false)
const captchaLoading = ref(false)
const error = ref('')

const loadCaptcha = async () => {
    if (captchaLoading.value) return
    captchaLoading.value = true
    error.value = ''
    try {
        const res = await getCaptcha()
        if (res.success) {
            captchaId.value = res.data.captchaId
            captchaQuestion.value = res.data.question
            form.captcha = ''
        } else {
            showMessage(res.message || '获取验证码失败', 'error')
        }
    } catch (e) {
        showMessage('获取验证码失败', 'error')
    } finally {
        captchaLoading.value = false
    }
}

const handleVerify = async () => {
    if (!props.commentId) {
        error.value = '评论ID无效'
        return
    }
    if (!form.secretKey) {
        error.value = '请输入密钥'
        return
    }
    if (!form.captcha) {
        error.value = '请输入验证码'
        return
    }

    loading.value = true
    error.value = ''
    try {
        const res = await verifySecret({
            commentId: props.commentId,
            secretKey: form.secretKey,
            captcha: form.captcha,
            captchaId: captchaId.value
        })
        if (res.success) {
            emit('success', res.data.content)
            handleClose()
        } else {
            error.value = res.message || '验证失败'
            loadCaptcha()
        }
    } catch (e) {
        error.value = '验证失败'
        loadCaptcha()
    } finally {
        loading.value = false
    }
}

const handleClose = () => {
    form.secretKey = ''
    form.captcha = ''
    error.value = ''
    emit('close')
}

watch(() => props.show, (newVal) => {
    if (newVal) {
        loadCaptcha()
    }
})
</script>

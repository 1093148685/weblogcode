import { computed, ref } from 'vue'
import { getBlogSettingsDetail } from '@/api/frontend/blogsettings'

const DEFAULT_COMMENT_IMAGE_MAX_SIZE_MB = 5
const COMMENT_IMAGE_HARD_MAX_SIZE_MB = 20
const UPLOAD_TIMEOUT_MS = 60000

const commentImageMaxSizeMb = ref(DEFAULT_COMMENT_IMAGE_MAX_SIZE_MB)
let loadingPromise = null

const normalizeSizeMb = (value) => {
    const numberValue = Number(value)
    if (!Number.isFinite(numberValue) || numberValue <= 0) {
        return DEFAULT_COMMENT_IMAGE_MAX_SIZE_MB
    }
    return Math.min(Math.max(Math.round(numberValue), 1), COMMENT_IMAGE_HARD_MAX_SIZE_MB)
}

const readUploadErrorMessage = (error) => {
    if (error?.code === 'ECONNABORTED') {
        return '图片上传超时，请检查网络或压缩图片后重试'
    }
    if (error?.response?.status === 413) {
        return '图片体积超过服务器允许的最大限制，请压缩后再上传'
    }
    if (error?.message === 'Network Error') {
        return '图片上传失败，可能超过服务器上传限制或网络中断，请压缩后再试'
    }
    return error?.response?.data?.message || error?.message || '图片上传失败，请稍后再试'
}

export function useCommentUploadLimit() {
    const commentImageMaxSizeBytes = computed(() => commentImageMaxSizeMb.value * 1024 * 1024)
    const commentImageMaxSizeText = computed(() => `${commentImageMaxSizeMb.value}MB`)

    const loadCommentUploadLimit = async () => {
        if (!loadingPromise) {
            loadingPromise = getBlogSettingsDetail()
                .then((res) => {
                    if (res?.success && res.data) {
                        commentImageMaxSizeMb.value = normalizeSizeMb(res.data.commentImageMaxSizeMb)
                    }
                })
                .catch(() => {
                    commentImageMaxSizeMb.value = DEFAULT_COMMENT_IMAGE_MAX_SIZE_MB
                })
                .finally(() => {
                    loadingPromise = null
                })
        }
        return loadingPromise
    }

    return {
        commentImageMaxSizeMb,
        commentImageMaxSizeBytes,
        commentImageMaxSizeText,
        loadCommentUploadLimit,
        readUploadErrorMessage,
        uploadTimeoutMs: UPLOAD_TIMEOUT_MS
    }
}

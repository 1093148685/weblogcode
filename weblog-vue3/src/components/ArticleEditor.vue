<template>
    <el-dialog v-model="dialogVisible" class="article-editor-dialog" :fullscreen="true" :show-close="false"
        :close-on-press-escape="false" @open="handleOpen">
        <template #header>
            <div class="article-editor-header">
                <h4 class="font-bold">{{ isEdit ? '编辑文章' : '写文章' }}</h4>
                <div class="ml-auto flex gap-3">
                    <el-button @click="dialogVisible = false">取消</el-button>
                    <el-button type="primary" @click="submit">
                        <el-icon class="mr-1"><Promotion /></el-icon>
                        {{ isEdit ? '保存' : '发布' }}
                    </el-button>
                </div>
            </div>
        </template>
        <el-form :model="form" ref="formRef" label-position="top" size="large" :rules="rules" class="article-editor-form">
            <div class="article-editor-grid">
                <section class="article-editor-main">
                    <el-form-item label="标题" prop="title" class="article-title-item">
                        <el-input v-model="form.title" autocomplete="off" size="large" maxlength="40" show-word-limit
                            clearable placeholder="输入一个清晰、有搜索价值的文章标题" />
                    </el-form-item>
                    <el-form-item label="内容" prop="content" class="article-content-item">
                        <MdEditor v-model="form.content" :theme="editorTheme" @onUploadImg="onUploadImg"
                            editorId="articleEditor" :no-upload-img="true" />
                    </el-form-item>
                </section>

                <aside class="article-meta-panel">
                    <div class="meta-panel-title">发布设置</div>
                    <el-form-item label="封面" prop="cover">
                        <el-upload class="avatar-uploader" action="#" :on-change="handleCoverChange" :auto-upload="false"
                            :show-file-list="false">
                            <img v-if="form.cover" :src="form.cover" class="avatar" />
                            <el-icon v-else class="avatar-uploader-icon"><Plus /></el-icon>
                        </el-upload>
                    </el-form-item>
                    <el-form-item label="摘要" prop="summary">
                        <el-input v-model="form.summary" :rows="4" type="textarea" placeholder="用于首页卡片和 SEO 描述" />
                    </el-form-item>
                    <el-form-item label="分类" prop="categoryId">
                        <el-select v-model="form.categoryId" clearable placeholder="请选择分类" size="large">
                            <el-option v-for="item in categories" :key="item.id" :label="item.name" :value="item.id" />
                        </el-select>
                    </el-form-item>
                    <el-form-item label="标签" prop="tags">
                        <el-select v-model="form.tags" multiple filterable remote reserve-keyword
                            placeholder="输入或选择标签" remote-show-suffix allow-create default-first-option
                            :remote-method="remoteMethod" :loading="tagSelectLoading" size="large">
                            <el-option v-for="item in tags" :key="item.id" :label="item.name" :value="item.id" />
                        </el-select>
                    </el-form-item>
                    <div class="editor-stats">
                        <span>正文 {{ countPlainText(form.content) }} 字</span>
                        <span>摘要 {{ countPlainText(form.summary) }} 字</span>
                    </div>
                </aside>
            </div>
        </el-form>
    </el-dialog>
</template>

<script setup>
import { ref, reactive, computed, watch } from 'vue'
import { publishArticle, getArticleDetail, updateArticle } from '@/api/admin/article'
import { uploadFile } from '@/api/admin/file'
import { getCategorySelectList } from '@/api/admin/category'
import { searchTags, getTagSelectList } from '@/api/admin/tag'
import { showMessage } from '@/composables/util'
import { MdEditor } from 'md-editor-v3'
import 'md-editor-v3/lib/style.css'
import { useThemeStore } from '@/stores/theme'

const props = defineProps({
    articleId: { type: Number, default: null },
    modelValue: { type: Boolean, default: false }
})

const emit = defineEmits(['saved', 'update:modelValue'])

const themeStore = useThemeStore()
const editorTheme = computed(() => themeStore.mode === 'dark' ? 'dark' : 'light')
const isEdit = computed(() => !!props.articleId)

const dialogVisible = computed({
    get: () => props.modelValue,
    set: (val) => emit('update:modelValue', val)
})

const formRef = ref(null)
const form = reactive({
    id: null, title: '', content: '请输入内容', cover: '',
    categoryId: null, tags: [], summary: '', isPublish: true
})

const rules = {
    title: [
        { required: true, message: '请输入文章标题', trigger: 'blur' },
        { min: 1, max: 40, message: '文章标题要求1-40个字符', trigger: 'blur' }
    ],
    content: [{ required: true }],
    cover: [{ required: true }],
    categoryId: [{ required: true, message: '请选择文章分类', trigger: 'blur' }],
    tags: [{ required: true, message: '请选择文章标签', trigger: 'blur' }]
}

const categories = ref([])
getCategorySelectList().then(e => { categories.value = e.data })

const tagSelectLoading = ref(false)
const tags = ref([])
getTagSelectList().then(res => { tags.value = res.data })

const remoteMethod = (query) => {
    if (query) {
        tagSelectLoading.value = true
        searchTags(query).then(e => { if (e.success) tags.value = e.data })
            .finally(() => tagSelectLoading.value = false)
    }
}

const countPlainText = (value) => {
    return (value || '').replace(/```[\s\S]*?```/g, '').replace(/`[^`]*`/g, '')
        .replace(/!\[[^\]]*]\([^)]*\)/g, '').replace(/\[[^\]]*]\([^)]*\)/g, '')
        .replace(/[#>*_\-~|]/g, '').replace(/\s/g, '').length
}

const handleCoverChange = (file) => {
    const fd = new FormData()
    fd.append('file', file.raw)
    uploadFile(fd).then(e => {
        if (e.success == false) { showMessage(e.message, 'error'); return }
        form.cover = e.data
        showMessage('上传成功')
    })
}

const onUploadImg = async (files, callback) => {
    const resList = []
    for (const file of files) {
        const fd = new FormData()
        fd.append("file", file)
        try {
            const res = await uploadFile(fd)
            if (res.success && res.data) resList.push(res.data)
            else showMessage('图片上传失败: ' + (res.message || '未知错误'), 'error')
        } catch (err) { showMessage('图片上传失败: ' + err.message, 'error') }
    }
    if (resList.length > 0) callback(resList)
}

const resetForm = () => {
    form.id = null; form.title = ''; form.content = '请输入内容'
    form.cover = ''; form.summary = ''; form.categoryId = null
    form.tags = []; form.isPublish = true
}

const handleOpen = () => {
    if (props.articleId) {
        getArticleDetail(props.articleId).then(res => {
            if (res.success) {
                form.id = res.data.id; form.title = res.data.title
                form.cover = res.data.cover; form.content = res.data.content
                form.categoryId = res.data.categoryId; form.tags = res.data.tagIds
                form.summary = res.data.summary
                form.isPublish = res.data.status !== 0
            }
        })
    } else {
        resetForm()
    }
}

const submit = () => {
    formRef.value.validate((valid) => {
        if (!valid) return false
        const payload = { ...form, status: form.isPublish ? 1 : 0 }
        const api = isEdit.value ? updateArticle(payload) : publishArticle(payload)
        api.then(res => {
            if (res.success == false) { showMessage(res.message, 'error'); return }
            showMessage(isEdit.value ? '保存成功' : '发布成功')
            dialogVisible.value = false
            emit('saved')
        })
    })
}
</script>

<style scoped>
.avatar-uploader .avatar {
    width: 200px; height: 100px; display: block;
}
.el-icon.avatar-uploader-icon {
    font-size: 28px; color: #8c939d; width: 200px; height: 100px; text-align: center;
}
.article-editor-form { width: 100%; }
.article-editor-grid {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 340px;
    gap: 22px;
    align-items: start;
}
.article-meta-panel {
    position: sticky; top: 82px;
    display: flex; flex-direction: column; gap: 4px;
    padding: 18px;
    border: 1px solid var(--el-border-color);
    border-radius: 18px;
    background: var(--el-bg-color);
    box-shadow: var(--el-box-shadow-light);
}
.meta-panel-title {
    margin-bottom: 8px; color: var(--el-text-color-primary);
    font-size: 16px; font-weight: 800;
}
.article-meta-panel .el-select,
.article-meta-panel .el-input,
.article-meta-panel .el-textarea { width: 100%; }
.editor-stats {
    display: flex; align-items: center; justify-content: space-between;
    padding: 12px 14px; margin-top: 10px;
    border: 1px solid var(--el-border-color); border-radius: 14px;
    background: var(--el-fill-color-light);
    color: var(--el-text-color-secondary); font-size: 12px;
}
@media (max-width: 1180px) {
    .article-editor-grid { grid-template-columns: 1fr; }
    .article-meta-panel { position: static; }
}
</style>

<style>
.article-editor-dialog.el-dialog.is-fullscreen {
    background: var(--el-bg-color-page);
}
.article-editor-dialog .el-dialog__header {
    position: sticky; top: 0; z-index: 50;
    padding: 0 24px;
    background: var(--el-bg-color);
    border-bottom: 1px solid var(--el-border-color);
    backdrop-filter: blur(18px);
}
.article-editor-dialog .el-dialog__body {
    padding: 22px 24px 32px;
    color: var(--el-text-color-primary);
}
.article-editor-dialog .el-form-item { margin-bottom: 24px; }
.article-editor-dialog .el-form-item.is-error { margin-bottom: 36px; }
.article-editor-dialog .el-form-item__error { padding-top: 6px; line-height: 1.35; }
.article-editor-dialog .el-form-item__label {
    font-weight: 700;
}
.article-editor-dialog .md-editor {
    overflow: hidden;
    border: 1px solid var(--el-border-color);
    border-radius: 14px;
    height: calc(100vh - 252px);
    min-height: 560px;
}
.article-editor-header {
    display: flex; align-items: center;
    height: 58px; width: 100%; gap: 16px;
    background: transparent;
}
.article-editor-header h4 {
    margin: 0; font-size: 20px; font-weight: 800;
}
.article-editor-header .ml-auto { gap: 12px; }
</style>

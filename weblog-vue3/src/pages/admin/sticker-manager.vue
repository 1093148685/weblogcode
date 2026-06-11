<template>
    <div>
        <el-card shadow="never" class="mb-5">
            <div class="flex items-center gap-4">
                <el-button type="primary" @click="showCreateDialog">
                    <i class="fas fa-plus mr-2"></i>新建贴纸包
                </el-button>
            </div>
        </el-card>

        <el-card shadow="never" v-loading="loading">
            <el-table :data="tableData" border stripe table-layout="auto">
                <el-table-column prop="id" label="ID" width="80" />
                <el-table-column prop="name" label="名称" />
                <el-table-column label="封面" width="100">
                    <template #default="scope">
                        <video v-if="scope.row.icon && isAnimatedUrl(scope.row.icon)"
                            :src="scope.row.icon"
                            class="w-12 h-12 rounded border cursor-pointer object-contain"
                            autoplay loop muted playsinline
                            @click="previewImage(scope.row.icon)"></video>
                        <el-image 
                            v-else-if="scope.row.icon" 
                            :src="scope.row.icon" 
                            fit="contain"
                            class="w-12 h-12 rounded border cursor-pointer"
                            :preview-src-list="[scope.row.icon]"
                            :preview-teleported="true"
                        />
                        <span v-else class="text-gray-400 text-xs">未设置</span>
                    </template>
                </el-table-column>
                <el-table-column prop="description" label="描述" show-overflow-tooltip />
                <el-table-column label="状态" width="100">
                    <template #default="scope">
                        <el-tag :type="scope.row.isActive ? 'success' : 'info'">
                            {{ scope.row.isActive ? '启用' : '禁用' }}
                        </el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="createTime" label="创建时间" width="180" />
                <el-table-column fixed="right" label="操作" width="280">
                    <template #default="scope">
                        <el-button size="small" @click="showStickersDialog(scope.row)">
                            <i class="fas fa-images mr-1"></i>查看贴纸
                        </el-button>
                        <el-button size="small" @click="showUploadDialog(scope.row)">
                            <i class="fas fa-upload mr-1"></i>上传
                        </el-button>
                        <el-button size="small" type="primary" @click="editPack(scope.row)">
                            <i class="fas fa-edit mr-1"></i>编辑
                        </el-button>
                        <el-button size="small" type="danger" @click="deletePack(scope.row)">
                            <i class="fas fa-trash mr-1"></i>删除
                        </el-button>
                    </template>
                </el-table-column>
            </el-table>
        </el-card>

        <!-- 创建/编辑贴纸包对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="500px">
            <el-form :model="form" label-width="80px">
                <el-form-item label="名称">
                    <el-input v-model="form.name" placeholder="请输入贴纸包名称" />
                </el-form-item>
                <el-form-item label="描述">
                    <el-input v-model="form.description" type="textarea" rows="3" placeholder="请输入描述" />
                </el-form-item>
                <el-form-item label="封面URL">
                    <el-input v-model="form.icon" placeholder="请输入封面图片URL" />
                </el-form-item>
                <el-form-item label="启用状态">
                    <el-switch v-model="form.isActive" />
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" @click="submitForm">确定</el-button>
            </template>
        </el-dialog>

        <!-- 查看贴纸对话框 -->
        <el-dialog v-model="stickersDialogVisible" :title="currentPack?.name + ' - 贴纸列表'" width="800px">
            <div v-if="currentPackStickers.length > 0" class="grid grid-cols-6 gap-3">
                    <div v-for="sticker in currentPackStickers" :key="sticker.id" 
                    class="relative group border rounded-lg p-2 hover:border-blue-400 transition-colors">
                    <video v-if="isAnimatedSticker(sticker)"
                        :src="sticker.thumbnailUrl || sticker.imageUrl"
                        class="w-full aspect-square object-contain"
                        autoplay loop muted playsinline></video>
                    <img v-else :src="sticker.thumbnailUrl || sticker.imageUrl" 
                        class="w-full aspect-square object-contain">
                    <div class="absolute top-1 right-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <el-button 
                            size="small" 
                            type="danger" 
                            circle 
                            @click="removeSticker(sticker)">
                            <i class="fas fa-times text-xs"></i>
                        </el-button>
                    </div>
                    <div v-if="!currentPack?.icon" class="mt-1">
                        <el-button size="small" type="text" @click="setCover(sticker)">设为封面</el-button>
                    </div>
                    <div class="text-xs text-gray-400 text-center mt-1 truncate">
                        {{ sticker.category || '默认' }}
                    </div>
                </div>
            </div>
            <div v-else class="text-center py-8 text-gray-500">
                暂无贴纸，请上传 ZIP 文件
            </div>
            <template #footer>
                <el-button @click="stickersDialogVisible = false">关闭</el-button>
            </template>
        </el-dialog>

        <!-- 上传 ZIP 对话框 -->
        <el-dialog v-model="uploadDialogVisible" title="上传贴纸" width="500px">
            <div class="text-sm text-gray-600 mb-4">
                <p>请上传 ZIP 文件，系统会自动解压并导入图片。</p>
                <p class="mt-1">支持的图片格式：jpg, jpeg, png, gif, webp, webm, mp4</p>
                <p class="mt-1">每个文件最大 10MB</p>
                <p class="mt-1">每个贴纸包最多 100 张贴纸</p>
            </div>
            <el-upload
                ref="uploadRef"
                :auto-upload="false"
                :limit="1"
                accept=".zip"
                :on-change="handleFileChange"
                drag
            >
                <i class="fas fa-cloud-upload-alt text-4xl text-gray-400"></i>
                <div class="el-upload__text">拖拽 ZIP 文件到此处，或 <em>点击上传</em></div>
            </el-upload>
            <template #footer>
                <el-button @click="uploadDialogVisible = false">取消</el-button>
                <el-button type="primary" :loading="uploading" @click="submitUpload">上传</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { getStickerPacks, getStickerPack } from '@/api/frontend/sticker'
import { createStickerPack, getAllStickerPacks, updateStickerPack, deleteStickerPack, uploadStickerZip, deleteSticker, setStickerCover } from '@/api/admin/sticker'
import { showMessage } from '@/composables/util'
import { useUserStore } from '@/stores/user'

const loading = ref(false)
const tableData = ref([])
const dialogVisible = ref(false)
const dialogTitle = ref('新建贴纸包')
const stickersDialogVisible = ref(false)
const uploadDialogVisible = ref(false)
const uploading = ref(false)
const uploadRef = ref(null)
const currentPack = ref(null)
const currentPackStickers = ref([])
const selectedFile = ref(null)
const userStore = useUserStore()
const isAdmin = computed(() => userStore.userInfo?.role === 'admin')

const ensureAdmin = () => {
    if (!isAdmin.value) {
        showMessage('演示账号仅支持查询操作！', 'error')
        return false
    }
    return true
}

const form = reactive({
    id: null,
    name: '',
    description: '',
    icon: '',
    isActive: true
})

const loadData = async () => {
    loading.value = true
    try {
        const res = await getAllStickerPacks()
        if (res.success) {
            tableData.value = res.data
        }
    } catch (e) {
        showMessage('加载失败', 'error')
    } finally {
        loading.value = false
    }
}

const showCreateDialog = () => {
    if (!ensureAdmin()) return
    form.id = null
    form.name = ''
    form.description = ''
    form.icon = ''
    form.isActive = true
    dialogTitle.value = '新建贴纸包'
    dialogVisible.value = true
}

const editPack = (pack) => {
    if (!ensureAdmin()) return
    form.id = pack.id
    form.name = pack.name
    form.description = pack.description || ''
    form.icon = pack.icon || ''
    form.isActive = pack.isActive
    dialogTitle.value = '编辑贴纸包'
    dialogVisible.value = true
}

const submitForm = async () => {
    if (!ensureAdmin()) return
    if (!form.name) {
        showMessage('请输入名称', 'warning')
        return
    }
    
    try {
        if (form.id) {
            const res = await updateStickerPack(form.id, form)
            if (res.success) {
                showMessage('更新成功', 'success')
                dialogVisible.value = false
                loadData()
            }
        } else {
            const res = await createStickerPack(form)
            if (res.success) {
                showMessage('创建成功', 'success')
                dialogVisible.value = false
                loadData()
            }
        }
    } catch (e) {
        showMessage('操作失败', 'error')
    }
}

const deletePack = async (pack) => {
    if (!ensureAdmin()) return
    try {
        await ElMessageBox.confirm(`确定要删除贴纸包 "${pack.name}" 吗？`, '提示', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        })
        
        const res = await deleteStickerPack(pack.id)
        if (res.success) {
            showMessage('删除成功', 'success')
            loadData()
        }
    } catch (e) {
        if (e !== 'cancel') {
            showMessage('删除失败', 'error')
        }
    }
}

const showStickersDialog = async (pack) => {
    try {
        const res = await getStickerPack(pack.id)
        if (res.success && res.data) {
            currentPack.value = res.data
            currentPackStickers.value = []
            if (res.data.categories) {
                res.data.categories.forEach(cat => {
                    currentPackStickers.value.push(...cat.stickers)
                })
            }
        }
    } catch (e) {
        currentPack.value = pack
        currentPackStickers.value = []
    }
    
    stickersDialogVisible.value = true
}

const showUploadDialog = (pack) => {
    if (!ensureAdmin()) return
    currentPack.value = pack
    selectedFile.value = null
    uploadRef.value?.clearFiles()
    uploadDialogVisible.value = true
}

const handleFileChange = (file) => {
    selectedFile.value = file.raw
}

const submitUpload = async () => {
    if (!ensureAdmin()) return
    if (!selectedFile.value) {
        showMessage('请选择文件', 'warning')
        return
    }
    
    uploading.value = true
    try {
        const res = await uploadStickerZip(currentPack.value.id, selectedFile.value)
        if (res.success) {
            if (res.data && res.data.length > 0) {
                showMessage(`上传成功，共${res.data.length}张贴纸`, 'success')
                uploadDialogVisible.value = false
                await loadData()
                // 更新 currentPack 以显示新上传的贴纸
                const updatedPack = tableData.value.find(p => p.id === currentPack.value.id)
                if (updatedPack) {
                    currentPack.value = updatedPack
                    currentPackStickers.value = []
                    if (updatedPack.categories) {
                        updatedPack.categories.forEach(cat => {
                            currentPackStickers.value.push(...cat.stickers)
                        })
                    }
                }
            } else {
                showMessage('上传成功但未找到任何贴纸，请检查ZIP文件内容', 'warning')
            }
        } else {
            showMessage(res.message || '上传失败', 'error')
        }
    } catch (e) {
        showMessage('上传失败', 'error')
    } finally {
        uploading.value = false
    }
}

const removeSticker = async (stickerItem) => {
    if (!ensureAdmin()) return
    try {
        await ElMessageBox.confirm('确定要删除这个贴纸吗？', '提示', {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        })
        
        const res = await deleteSticker(stickerItem.id)
        if (res.success) {
            showMessage('删除成功', 'success')
            currentPackStickers.value = currentPackStickers.value.filter(s => s.id !== stickerItem.id)
        }
    } catch (e) {
        if (e !== 'cancel') {
            showMessage('删除失败', 'error')
        }
    }
}

const isAnimatedSticker = (sticker) => {
    if (sticker.isAnimated) return true
    const url = sticker.imageUrl || ''
    const lower = url.toLowerCase()
    return lower.endsWith('.webm') || lower.endsWith('.gif') || lower.endsWith('.mp4')
}

const isAnimatedUrl = (url) => {
    if (!url) return false
    const lower = url.toLowerCase()
    return lower.endsWith('.webm') || lower.endsWith('.mp4')
}

const setCover = async (stickerItem) => {
    if (!ensureAdmin()) return
    const res = await setStickerCover(currentPack.value.id, stickerItem.id)
    if (res.success) {
        showMessage('设置封面成功', 'success')
        currentPack.value.icon = stickerItem.thumbnailUrl || stickerItem.imageUrl
        loadData()
    } else {
        showMessage('设置封面失败', 'error')
    }
}

const previewImage = (url) => {
    window.open(url, '_blank')
}

onMounted(() => {
    loadData()
})
</script>

<template>
    <div>
        <el-card shadow="never">
            <div class="flex justify-between items-center mb-4">
                <h2 class="font-bold text-base">AI Provider 管理</h2>
                <div class="flex gap-2">
                    <el-button type="info" :icon="RefreshRight" @click="handleMigrate" :loading="migrating">迁移旧数据</el-button>
                    <el-button type="primary" :icon="Plus" :disabled="!isAdmin()" @click="handleAdd">新增 Provider</el-button>
                </div>
            </div>

            <el-table :data="tableData" border stripe v-loading="loading">
                <el-table-column prop="displayName" label="名称" width="150">
                    <template #default="{ row }">
                        <div class="flex items-center gap-2">
                            <span class="font-medium">{{ row.displayName }}</span>
                            <el-tag size="small" type="info">{{ row.name }}</el-tag>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="type" label="类型" width="80">
                    <template #default="{ row }">
                        <el-tag>{{ row.type }}</el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="apiUrl" label="API 地址" min-width="180" show-overflow-tooltip />
                <el-table-column prop="priority" label="优先级" width="80">
                    <template #default="{ row }">
                        <span :class="row.priority <= 10 ? 'text-green-600 font-bold' : 'text-gray-500'">{{ row.priority }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="isEnabled" label="状态" width="80">
                    <template #default="{ row }">
                        <el-switch v-model="row.isEnabled" @change="handleToggleEnable(row)" :disabled="!isAdmin()" />
                    </template>
                </el-table-column>
                <el-table-column prop="updatedAt" label="更新时间" width="160" />
                <el-table-column label="操作" width="200" fixed="right">
                    <template #default="{ row }">
                        <el-button type="success" link size="small" @click="handleTest(row)">测试</el-button>
                        <el-button type="primary" link size="small" @click="handleEdit(row)">编辑</el-button>
                        <el-button type="danger" link size="small" :disabled="!isAdmin()" @click="handleDelete(row)">删除</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </el-card>

        <!-- 新增/编辑对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px">
            <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
                <el-form-item label="Provider" prop="name">
                    <el-select v-model="form.name" placeholder="选择 Provider 类型" :disabled="isEditing">
                        <el-option label="OpenAI" value="openai" />
                        <el-option label="Claude (Anthropic)" value="claude" />
                        <el-option label="DeepSeek" value="deepseek" />
                        <el-option label="Azure OpenAI" value="azure" />
                        <el-option label="Google Gemini" value="gemini" />
                        <el-option label="智谱 AI (GLM)" value="zhipu" />
                        <el-option label="百度千帆" value="qianfan" />
                        <el-option label="MiniMax" value="minimax" />
                    </el-select>
                </el-form-item>
                <el-form-item label="显示名称" prop="displayName">
                    <el-input v-model="form.displayName" placeholder="如：OpenAI GPT-4" />
                </el-form-item>
                <el-form-item label="类型" prop="type">
                    <el-select v-model="form.type">
                        <el-option label="对话 (Chat)" value="chat" />
                        <el-option label="图片 (Image)" value="image" />
                        <el-option label="音频 (Audio)" value="audio" />
                    </el-select>
                </el-form-item>
                <el-form-item label="API 地址" prop="apiUrl">
                    <el-input v-model="form.apiUrl" placeholder="留空使用默认地址" />
                </el-form-item>
                <el-form-item label="API Key" prop="apiKey">
                    <el-input v-model="form.apiKey" type="password" show-password placeholder="请输入 API Key" />
                </el-form-item>
                <el-form-item label="优先级" prop="priority">
                    <el-input-number v-model="form.priority" :min="1" :max="100" />
                    <div class="text-xs text-gray-500 mt-1">数字越小优先级越高，支持主备 Provider</div>
                </el-form-item>
                <el-form-item label="启用状态">
                    <el-switch v-model="form.isEnabled" />
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { Plus, RefreshRight } from '@element-plus/icons-vue'
import { getAiProviders, createAiProvider, updateAiProvider, deleteAiProvider, testAiProvider, migrateAiProviders } from '@/api/admin/ai-provider'
import { showMessage, isAdmin } from '@/composables/util'

defineOptions({ name: 'AdminAiProvider' })

const loading = ref(false)
const submitting = ref(false)
const migrating = ref(false)
const tableData = ref([])
const dialogVisible = ref(false)
const isEditing = ref(false)
const formRef = ref(null)

const form = reactive({
    id: null,
    name: '',
    displayName: '',
    type: 'chat',
    apiUrl: '',
    apiKey: '',
    priority: 100,
    isEnabled: true
})

const rules = {
    name: [{ required: true, message: '请选择 Provider', trigger: 'change' }],
    displayName: [{ required: true, message: '请输入显示名称', trigger: 'blur' }]
}

const loadData = async () => {
    loading.value = true
    try {
        const res = await getAiProviders()
        if (res.success) {
            tableData.value = res.data || []
        }
    } catch (e) {
        showMessage('加载失败', 'error')
    } finally {
        loading.value = false
    }
}

const handleMigrate = async () => {
    migrating.value = true
    try {
        const res = await migrateAiProviders()
        if (res.success) {
            showMessage('迁移成功')
            loadData()
        } else {
            showMessage(res.message || '迁移失败', 'error')
        }
    } catch (e) {
        showMessage('迁移失败', 'error')
    } finally {
        migrating.value = false
    }
}

const handleAdd = () => {
    isEditing.value = false
    Object.assign(form, {
        id: null,
        name: '',
        displayName: '',
        type: 'chat',
        apiUrl: '',
        apiKey: '',
        priority: 100,
        isEnabled: true
    })
    dialogVisible.value = true
}

const handleEdit = (row) => {
    isEditing.value = true
    Object.assign(form, {
        id: row.id,
        name: row.name,
        displayName: row.displayName,
        type: row.type,
        apiUrl: row.apiUrl,
        apiKey: '',
        priority: row.priority,
        isEnabled: row.isEnabled
    })
    dialogVisible.value = true
}

const handleSubmit = async () => {
    if (!form.name || !form.displayName) {
        showMessage('请填写完整信息', 'warning')
        return
    }

    if (!isEditing.value && !form.apiKey) {
        showMessage('请输入 API Key', 'warning')
        return
    }

    submitting.value = true
    try {
        const data = { ...form }
        if (isEditing.value && !data.apiKey) {
            delete data.apiKey
        }

        let res
        if (isEditing.value) {
            res = await updateAiProvider(form.id, data)
        } else {
            res = await createAiProvider(data)
        }

        if (res.success) {
            showMessage(isEditing.value ? '更新成功' : '创建成功')
            dialogVisible.value = false
            loadData()
        } else {
            showMessage(res.message || '操作失败', 'error')
        }
    } catch (e) {
        showMessage('操作失败', 'error')
    } finally {
        submitting.value = false
    }
}

const handleDelete = async (row) => {
    try {
        await ElMessageBox.confirm(`确定要删除 "${row.displayName}" 吗？`, '提示', { type: 'warning' })
        
        const res = await deleteAiProvider(row.id)
        if (res.success) {
            showMessage('删除成功')
            loadData()
        } else {
            showMessage(res.message || '删除失败', 'error')
        }
    } catch (e) {
        if (e !== 'cancel') {
            showMessage('删除失败', 'error')
        }
    }
}

const handleTest = async (row) => {
    try {
        const res = await testAiProvider(row.id)
        if (res.success && res.data) {
            showMessage('连接成功', 'success')
        } else {
            showMessage('连接失败', 'error')
        }
    } catch (e) {
        showMessage('连接失败', 'error')
    }
}

const handleToggleEnable = async (row) => {
    await updateAiProvider(row.id, {
        displayName: row.displayName,
        type: row.type,
        apiUrl: row.apiUrl,
        isEnabled: row.isEnabled,
        priority: row.priority
    })
}

onMounted(() => {
    loadData()
})
</script>
<template>
    <div>
        <el-card shadow="never">
            <div class="flex justify-between items-center mb-4">
                <h2 class="font-bold text-base">AI 模型管理</h2>
                <el-button type="primary" :icon="Plus" :disabled="!isAdmin()" @click="handleAdd">新增模型</el-button>
            </div>

            <el-table :data="tableData" border stripe>
                <el-table-column prop="name" label="模型名称" width="120" />
                <el-table-column prop="type" label="模型类型" width="100">
                    <template #default="{ row }">
                        <el-tag>{{ row.type }}</el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="model" label="模型标识" width="120" />
                <el-table-column prop="apiUrl" label="API 地址" min-width="150" show-overflow-tooltip />
                <el-table-column prop="isDefault" label="默认" width="60">
                    <template #default="{ row }">
                        <el-tag v-if="row.isDefault" type="success">是</el-tag>
                        <span v-else class="text-gray-400">否</span>
                    </template>
                </el-table-column>
                <el-table-column prop="isEnabled" label="状态" width="60">
                    <template #default="{ row }">
                        <el-tag v-if="row.isEnabled" type="success">启用</el-tag>
                        <el-tag v-else type="info">禁用</el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="remark" label="备注" min-width="100" show-overflow-tooltip />
                <el-table-column prop="createTime" label="创建时间" width="160" />
                <el-table-column label="操作" width="240" fixed="right">
                    <template #default="{ row }">
                        <el-button type="success" link :disabled="!isAdmin()" @click="handleTest(row)">测试</el-button>
                        <el-button type="primary" link :disabled="!isAdmin()" @click="handleEdit(row)">编辑</el-button>
                        <el-button type="danger" link :disabled="!isAdmin()" @click="handleDelete(row)">删除</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </el-card>

        <!-- 新增/编辑对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="600px">
            <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
                <el-form-item label="模型名称" prop="name">
                    <el-input v-model="form.name" placeholder="如：GPT-4" />
                </el-form-item>
                <el-form-item label="模型类型" prop="type">
                    <el-select v-model="form.type" placeholder="请选择模型类型">
                        <el-option label="OpenAI" value="openai" />
                        <el-option label="Claude" value="claude" />
                        <el-option label="Azure OpenAI" value="azure" />
                        <el-option label="MiniMax" value="minimax" />
                        <el-option label="Gemini" value="gemini" />
                        <el-option label="百度千帆" value="qianfan" />
                        <el-option label="智谱AI" value="zhipu" />
                        <el-option label="其他" value="other" />
                    </el-select>
                </el-form-item>
                <el-form-item label="模型标识" prop="model">
                    <el-input v-model="form.model" placeholder="如：gpt-4、claude-3-opus" />
                </el-form-item>
                <el-form-item label="API 地址" prop="apiUrl">
                    <el-input v-model="form.apiUrl" placeholder="如：https://api.openai.com/v1" :disabled="!isAdmin()" />
                </el-form-item>
                <el-form-item v-if="isAdmin()" label="API Key" prop="apiKey">
                    <el-input v-model="form.apiKey" type="password" show-password placeholder="请输入 API Key" />
                </el-form-item>
                <el-form-item label="设为默认">
                    <el-switch v-model="form.isDefault" />
                </el-form-item>
                <el-form-item label="是否启用">
                    <el-switch v-model="form.isEnabled" />
                </el-form-item>
                <el-form-item label="备注">
                    <el-input v-model="form.remark" type="textarea" :rows="2" />
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" @click="handleSubmit">确定</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
defineOptions({
    name: 'AdminAiModel'
})
import { ref, onMounted } from 'vue'
import { Plus } from '@element-plus/icons-vue'
import { getAiModelList, createAiModel, updateAiModel, deleteAiModel, testAiModel } from '@/api/admin/aiModel'
import { showMessage, isAdmin } from '@/composables/util'
import { ElMessageBox } from 'element-plus'

const tableData = ref([])
const dialogVisible = ref(false)
const dialogTitle = ref('新增模型')
const formRef = ref(null)

const form = ref({
    id: null,
    name: '',
    type: 'openai',
    model: '',
    apiUrl: '',
    apiKey: '',
    isDefault: false,
    isEnabled: true,
    remark: ''
})

const rules = {
    name: [{ required: true, message: '请输入模型名称', trigger: 'blur' }],
    type: [{ required: true, message: '请选择模型类型', trigger: 'change' }],
    model: [{ required: true, message: '请输入模型标识', trigger: 'blur' }],
    apiUrl: [{ required: true, message: '请输入 API 地址', trigger: 'blur' }],
    apiKey: [{ required: true, message: '请输入 API Key', trigger: 'blur' }]
}

onMounted(() => {
    initData()
})

// 数据是否已加载
const dataLoaded = ref(false)

function initData() {
    // 如果数据已加载且有数据，不重复加载
    if (dataLoaded.value && tableData.value.length > 0) {
        return
    }
    getAiModelList().then(res => {
        if (res.success) {
            tableData.value = res.data || []
            dataLoaded.value = true
        }
    })
}

function handleAdd() {
    if (!isAdmin()) {
        showMessage('演示账号仅支持查询操作！', 'error')
        return
    }
    dialogTitle.value = '新增模型'
    form.value = {
        id: null,
        name: '',
        type: 'openai',
        model: '',
        apiUrl: '',
        apiKey: '',
        isDefault: false,
        isEnabled: true,
        remark: ''
    }
    dialogVisible.value = true
}

function handleEdit(row) {
    if (!isAdmin()) {
        showMessage('演示账号仅支持查询操作！', 'error')
        return
    }
    dialogTitle.value = '编辑模型'
    form.value = { ...row }
    dialogVisible.value = true
}

function handleTest(row) {
    if (!isAdmin()) {
        showMessage('演示账号仅支持查询操作！', 'error')
        return
    }
    testAiModel(row.id).then(res => {
        if (res.success) {
            showMessage(res.data || '测试成功', 'success')
        } else {
            showMessage(res.message || '测试失败', 'error')
        }
    })
}

function handleSubmit() {
    if (!isAdmin()) {
        showMessage('演示账号仅支持查询操作！', 'error')
        return
    }
    formRef.value.validate(valid => {
        if (valid) {
            if (form.value.id) {
                updateAiModel(form.value).then(res => {
                    if (res.success) {
                        showMessage('更新成功', 'success')
                        dialogVisible.value = false
                        dataLoaded.value = false
                        initData()
                    }
                })
            } else {
                createAiModel(form.value).then(res => {
                    if (res.success) {
                        showMessage('创建成功', 'success')
                        dialogVisible.value = false
                        dataLoaded.value = false
                        initData()
                    }
                })
            }
        }
    })
}

function handleDelete(row) {
    if (!isAdmin()) {
        showMessage('演示账号仅支持查询操作！', 'error')
        return
    }
    ElMessageBox.confirm('确定要删除该模型吗？', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
    }).then(() => {
        deleteAiModel(row.id).then(res => {
            if (res.success) {
                showMessage('删除成功', 'success')
                dataLoaded.value = false
                initData()
            }
        })
    })
}
</script>

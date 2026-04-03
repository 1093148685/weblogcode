<template>
    <div class="ai-page p-5">

        <!-- 页头 -->
        <div class="page-header mb-5">
            <div class="flex items-center gap-3 mb-1">
                <div class="page-header__icon">
                    <el-icon :size="20"><Connection /></el-icon>
                </div>
                <h1 class="text-lg font-bold text-slate-800">AI Provider 管理</h1>
            </div>
            <p class="text-sm text-slate-400 ml-11">配置大模型服务提供商，支持多 Provider 主备切换</p>
        </div>

        <!-- 统计卡片 -->
        <div class="grid grid-cols-3 gap-4 mb-5">
            <div class="mini-stat mini-stat--blue">
                <div class="mini-stat__num">{{ tableData.length }}</div>
                <div class="mini-stat__label">全部 Provider</div>
            </div>
            <div class="mini-stat mini-stat--green">
                <div class="mini-stat__num">{{ tableData.filter(r => r.isEnabled).length }}</div>
                <div class="mini-stat__label">已启用</div>
            </div>
            <div class="mini-stat mini-stat--violet">
                <div class="mini-stat__num">{{ tableData.filter(r => r.priority <= 10).length }}</div>
                <div class="mini-stat__label">高优先级</div>
            </div>
        </div>

        <!-- 健康检查卡片 -->
        <el-card shadow="never" class="ai-card mb-5">
            <template #header>
                <div class="flex justify-between items-center">
                    <span class="card-title">Provider 健康检查</span>
                    <el-button size="small" :loading="healthLoading" @click="checkHealth" type="primary" plain>检查连通性</el-button>
                </div>
            </template>
            <div v-if="healthList.length === 0" class="text-center text-gray-400 py-4 text-sm">
                点击「检查连通性」测试所有已启用 Provider
            </div>
            <div v-else class="flex flex-wrap gap-3">
                <div v-for="h in healthList" :key="h.name"
                     class="flex items-center gap-2 px-4 py-2.5 rounded-xl border text-sm"
                     :class="h.status === 'healthy' ? 'bg-green-50 border-green-200 text-green-700' : 'bg-red-50 border-red-200 text-red-600'">
                    <span class="w-2 h-2 rounded-full" :class="h.status === 'healthy' ? 'bg-green-500' : 'bg-red-500'"></span>
                    <span class="font-medium">{{ h.name }}</span>
                    <span class="text-xs opacity-70">{{ h.latencyMs }}ms</span>
                    <span v-if="h.error" class="text-xs" :title="h.error">⚠</span>
                </div>
            </div>
        </el-card>

        <!-- 主卡片 -->
        <el-card shadow="never" class="ai-card">
            <template #header>
                <div class="flex justify-between items-center">
                    <div class="flex items-center gap-2">
                        <span class="card-title">Provider 列表</span>
                        <el-tag size="small" class="ml-1">{{ tableData.length }} 条</el-tag>
                    </div>
                    <div class="flex gap-2">
                        <el-button :icon="RefreshRight" @click="handleMigrate" :loading="migrating" plain>迁移旧数据</el-button>
                        <el-button type="primary" :icon="Plus" :disabled="!isAdmin()" @click="handleAdd">新增 Provider</el-button>
                    </div>
                </div>
            </template>

            <el-table :data="tableData" v-loading="loading" style="width: 100%">
                <el-table-column prop="displayName" label="名称" min-width="180">
                    <template #default="{ row }">
                        <div class="flex items-center gap-2">
                            <div class="provider-avatar">{{ row.displayName?.charAt(0) }}</div>
                            <div>
                                <div class="font-medium text-slate-700 text-sm">{{ row.displayName }}</div>
                                <el-tag size="small" class="mt-0.5">{{ row.name }}</el-tag>
                            </div>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="type" label="类型" width="100">
                    <template #default="{ row }">
                        <el-tag :type="row.type === 'chat' ? '' : 'warning'" size="small">{{ row.type }}</el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="apiUrl" label="API 地址" min-width="200" show-overflow-tooltip>
                    <template #default="{ row }">
                        <span class="text-xs text-slate-400 font-mono">{{ row.apiUrl || '使用默认地址' }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="priority" label="优先级" width="90" align="center">
                    <template #default="{ row }">
                        <span :class="row.priority <= 10 ? 'priority-high' : 'priority-normal'">
                            {{ row.priority }}
                        </span>
                    </template>
                </el-table-column>
                <el-table-column prop="isEnabled" label="状态" width="80" align="center">
                    <template #default="{ row }">
                        <el-switch v-model="row.isEnabled" @change="handleToggleEnable(row)" :disabled="!isAdmin()" />
                    </template>
                </el-table-column>
                <el-table-column prop="updatedAt" label="更新时间" width="160">
                    <template #default="{ row }">
                        <span class="text-xs text-slate-400">{{ row.updatedAt }}</span>
                    </template>
                </el-table-column>
                <el-table-column label="操作" width="240" fixed="right">
                    <template #default="{ row }">
                        <el-button type="success" link size="small" @click="handleTest(row)">
                            <el-icon class="mr-0.5"><Connection /></el-icon>测试
                        </el-button>
                        <el-button type="info" link size="small" @click="handleViewModels(row)">
                            <el-icon class="mr-0.5"><Cpu /></el-icon>查看模型
                        </el-button>
                        <el-button type="primary" link size="small" @click="handleEdit(row)">编辑</el-button>
                        <el-button type="danger" link size="small" :disabled="!isAdmin()" @click="handleDelete(row)">删除</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </el-card>

        <!-- Key Pool 状态卡片 -->
        <el-card shadow="never" class="ai-card mt-5">
            <template #header>
                <div class="flex justify-between items-center">
                    <span class="card-title">Key Pool 状态</span>
                    <el-button size="small" @click="loadKeyPool">刷新</el-button>
                </div>
            </template>
            <div v-if="keyPoolList.length === 0" class="text-center text-gray-400 py-4 text-sm">暂无数据</div>
            <div v-else class="flex flex-col gap-4">
                <div v-for="pool in keyPoolList" :key="pool.providerName">
                    <div class="flex justify-between items-center mb-2">
                        <span class="font-medium text-sm">{{ pool.providerName }}</span>
                        <div class="flex items-center gap-2">
                            <el-tag size="small" :type="pool.healthyKeys === pool.totalKeys ? 'success' : 'warning'">
                                {{ pool.healthyKeys }}/{{ pool.totalKeys }} 健康
                            </el-tag>
                            <el-button size="small" text @click="resetKeys(pool.providerName)">重置</el-button>
                        </div>
                    </div>
                    <div class="flex flex-wrap gap-2">
                        <div v-for="k in pool.keys" :key="k.keyPrefix"
                             class="px-3 py-1.5 rounded-lg text-xs font-mono border"
                             :class="k.isHealthy ? 'bg-green-50 border-green-200 text-green-700' : 'bg-red-50 border-red-200 text-red-600'">
                            {{ k.keyPrefix }}
                            <span v-if="!k.isHealthy" class="ml-1 opacity-70">(失败{{ k.failCount }}次)</span>
                        </div>
                    </div>
                </div>
            </div>
        </el-card>

        <!-- 查看模型弹窗 -->
        <el-dialog v-model="modelsDialogVisible" :title="`${currentProviderName} 的模型列表`" width="700px">
            <div v-if="providerModels.length === 0" class="text-center text-gray-400 py-8 text-sm">该提供商下暂无模型</div>
            <el-table v-else :data="providerModels" size="small">
                <el-table-column prop="name" label="模型名称" min-width="160">
                    <template #default="{ row }">
                        <span class="font-medium text-slate-700 text-sm">{{ row.name }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="id" label="模型 ID" min-width="200">
                    <template #default="{ row }">
                        <span class="text-xs font-mono text-slate-500">{{ row.id }}</span>
                    </template>
                </el-table-column>
            </el-table>
            <template #footer>
                <el-button @click="modelsDialogVisible = false">关闭</el-button>
            </template>
        </el-dialog>

        <!-- 新增/编辑对话框 -->
        <el-dialog v-model="dialogVisible" :title="isEditing ? '编辑 Provider' : '新增 Provider'" width="560px">
            <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
                <el-form-item label="Provider" prop="name">
                    <el-select v-model="form.name" placeholder="选择 Provider 类型" :disabled="isEditing" style="width:100%"
                        @change="onProviderChange">
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
                    <el-select v-model="form.type" style="width:100%">
                        <el-option label="对话 (Chat)" value="chat" />
                        <el-option label="图片 (Image)" value="image" />
                        <el-option label="音频 (Audio)" value="audio" />
                    </el-select>
                </el-form-item>
                <el-form-item label="API 地址">
                    <el-input v-model="form.apiUrl" placeholder="留空使用默认地址" />
                    <div class="text-xs text-slate-400 mt-1">留空将自动使用官方默认接口地址</div>
                </el-form-item>
                <el-form-item label="API Key" prop="apiKey">
                    <el-input v-model="form.apiKey" type="password" show-password
                        :placeholder="isEditing ? '不修改请留空' : '请输入 API Key'" />
                </el-form-item>
                <el-form-item label="优先级" prop="priority">
                    <el-input-number v-model="form.priority" :min="1" :max="100" />
                    <div class="text-xs text-slate-400 mt-1">数值越小优先级越高，用于主备 Provider 切换</div>
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
import { Plus, RefreshRight, Connection, Cpu } from '@element-plus/icons-vue'
import { getAiProviders, createAiProvider, updateAiProvider, deleteAiProvider, testAiProvider, migrateAiProviders } from '@/api/admin/ai-provider'
import { getProviderHealth, getKeyPoolStatus, resetProviderKeys } from '@/api/admin/agent'
import { getAvailableModels } from '@/api/frontend/chat'
import { showMessage, isAdmin } from '@/composables/util'

defineOptions({ name: 'AdminAiProvider' })

const loading = ref(false)
const submitting = ref(false)
const migrating = ref(false)
const tableData = ref([])
const dialogVisible = ref(false)
const isEditing = ref(false)
const formRef = ref(null)

// 健康检查
const healthList = ref([])
const healthLoading = ref(false)
const checkHealth = async () => {
    healthLoading.value = true
    try {
        const res = await getProviderHealth()
        if (res.code === 200) healthList.value = res.data || []
    } catch { showMessage('健康检查失败', 'error') }
    finally { healthLoading.value = false }
}

// Key Pool
const keyPoolList = ref([])
const loadKeyPool = async () => {
    try {
        const res = await getKeyPoolStatus()
        if (res.code === 200) keyPoolList.value = res.data || []
    } catch {}
}
const resetKeys = async (name) => {
    try {
        await resetProviderKeys(name)
        showMessage(`${name} Key 已重置`)
        loadKeyPool()
    } catch { showMessage('重置失败', 'error') }
}

const PROVIDER_DEFAULTS = {
    openai:   { displayName: 'OpenAI', apiUrl: 'https://api.openai.com/v1' },
    claude:   { displayName: 'Claude (Anthropic)', apiUrl: 'https://api.anthropic.com' },
    deepseek: { displayName: 'DeepSeek', apiUrl: 'https://api.deepseek.com/v1' },
    gemini:   { displayName: 'Google Gemini', apiUrl: '' },
    zhipu:    { displayName: '智谱 AI', apiUrl: 'https://open.bigmodel.cn/api/paas/v4' },
    qianfan:  { displayName: '百度千帆', apiUrl: '' },
    minimax:  { displayName: 'MiniMax', apiUrl: '' },
}

const form = reactive({
    id: null, name: '', displayName: '', type: 'chat',
    apiUrl: '', apiKey: '', priority: 100, isEnabled: true
})

const rules = {
    name: [{ required: true, message: '请选择 Provider', trigger: 'change' }],
    displayName: [{ required: true, message: '请输入显示名称', trigger: 'blur' }]
}

const onProviderChange = (val) => {
    const def = PROVIDER_DEFAULTS[val]
    if (def) {
        form.displayName = def.displayName
        form.apiUrl = def.apiUrl
    }
}

const loadData = async () => {
    loading.value = true
    try {
        const res = await getAiProviders()
        if (res.success || res.code === 200) tableData.value = res.data || []
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
        if (res.success || res.code === 200) { showMessage('迁移成功'); loadData() }
        else showMessage(res.message || '迁移失败', 'error')
    } catch (e) {
        showMessage('迁移失败', 'error')
    } finally {
        migrating.value = false
    }
}

const handleAdd = () => {
    isEditing.value = false
    Object.assign(form, { id: null, name: '', displayName: '', type: 'chat', apiUrl: '', apiKey: '', priority: 100, isEnabled: true })
    dialogVisible.value = true
}

const handleEdit = (row) => {
    isEditing.value = true
    Object.assign(form, { id: row.id, name: row.name, displayName: row.displayName, type: row.type, apiUrl: row.apiUrl, apiKey: '', priority: row.priority, isEnabled: row.isEnabled })
    dialogVisible.value = true
}

const handleSubmit = async () => {
    if (!form.name || !form.displayName) { showMessage('请填写完整信息', 'warning'); return }
    if (!isEditing.value && !form.apiKey) { showMessage('请输入 API Key', 'warning'); return }
    submitting.value = true
    try {
        const data = { ...form }
        if (isEditing.value && !data.apiKey) delete data.apiKey
        const res = isEditing.value ? await updateAiProvider(form.id, data) : await createAiProvider(data)
        if (res.success || res.code === 200) { showMessage(isEditing.value ? '更新成功' : '创建成功'); dialogVisible.value = false; loadData() }
        else showMessage(res.message || '操作失败', 'error')
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
        if (res.success || res.code === 200) { showMessage('删除成功'); loadData() }
        else showMessage(res.message || '删除失败', 'error')
    } catch (e) {
        if (e !== 'cancel') showMessage('删除失败', 'error')
    }
}

const handleTest = async (row) => {
    const msg = showMessage('正在连接测试...', 'info')
    try {
        const res = await testAiProvider(row.id)
        if ((res.success || res.code === 200) && res.data) showMessage('✓ 连接成功', 'success')
        else showMessage('✗ 连接失败', 'error')
    } catch (e) {
        showMessage('✗ 连接失败', 'error')
    }
}

const handleToggleEnable = async (row) => {
    await updateAiProvider(row.id, { displayName: row.displayName, type: row.type, apiUrl: row.apiUrl, isEnabled: row.isEnabled, priority: row.priority })
}

// 查看模型
const modelsDialogVisible = ref(false)
const currentProviderName = ref('')
const providerModels = ref([])
let allModels = []

const handleViewModels = async (row) => {
    currentProviderName.value = row.displayName
    // 每次都重新拉取，确保数据最新
    if (allModels.length === 0) {
        try {
            const res = await getAvailableModels()
            if (res.success || res.code === 200) allModels = res.data || []
        } catch { showMessage('加载模型列表失败', 'error'); return }
    }
    // 用 provider 字段匹配（大小写不敏感）
    const providerName = (row.name || '').toLowerCase()
    providerModels.value = allModels.filter(m => (m.provider || '').toLowerCase() === providerName)
    modelsDialogVisible.value = true
}

onMounted(() => { loadData(); loadKeyPool() })
</script>

<style scoped>
.ai-page {
    min-height: 100%;
}

/* 页头图标 */
.page-header__icon {
    width: 36px;
    height: 36px;
    border-radius: 10px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    flex-shrink: 0;
}

/* 迷你统计卡 */
.mini-stat {
    border-radius: 12px;
    padding: 16px 20px;
    display: flex;
    flex-direction: column;
    gap: 4px;
    border: 1px solid transparent;
}

.mini-stat__num {
    font-size: 28px;
    font-weight: 700;
    line-height: 1;
}

.mini-stat__label {
    font-size: 12px;
    opacity: 0.75;
}

.mini-stat--blue   { background: linear-gradient(135deg,#eef2ff,#e0e7ff); color:#4338ca; border-color:#c7d2fe; }
.mini-stat--green  { background: linear-gradient(135deg,#f0fdf4,#dcfce7); color:#16a34a; border-color:#bbf7d0; }
.mini-stat--violet { background: linear-gradient(135deg,#fdf4ff,#f3e8ff); color:#9333ea; border-color:#e9d5ff; }

:global(html.dark) .mini-stat--blue   { background: rgba(67, 56, 202, 0.15) !important; border-color: rgba(99, 102, 241, 0.3) !important; color: #a5b4fc !important; }
:global(html.dark) .mini-stat--green  { background: rgba(22, 163, 74, 0.15) !important;  border-color: rgba(52, 211, 153, 0.3) !important; color: #34d399 !important; }
:global(html.dark) .mini-stat--violet { background: rgba(147, 51, 234, 0.15) !important; border-color: rgba(167, 139, 250, 0.3) !important; color: #c084fc !important; }

/* 主卡片 */
.ai-card { border-radius: 14px !important; }

.card-title {
    font-size: 14px;
    font-weight: 600;
    color: var(--text-heading);
}

/* Provider 头像 */
.provider-avatar {
    width: 32px;
    height: 32px;
    border-radius: 8px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    color: white;
    font-size: 14px;
    font-weight: 700;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}

/* 优先级样式 */
.priority-high {
    color: #16a34a;
    font-weight: 700;
    font-size: 15px;
}
.priority-normal {
    color: #94a3b8;
    font-size: 13px;
}
</style>

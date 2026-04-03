<template>
    <div class="ai-page p-5">

        <!-- 页头 -->
        <div class="page-header mb-5">
            <div class="flex items-center gap-3 mb-1">
                <div class="page-header__icon">
                    <el-icon :size="20"><Cpu /></el-icon>
                </div>
                <h1 class="text-lg font-bold text-slate-800">AI 模型管理</h1>
            </div>
            <p class="text-sm text-slate-400 ml-11">管理可用的 AI 模型配置，支持多种大模型服务</p>
        </div>

        <!-- 统计卡片 -->
        <div class="grid grid-cols-4 gap-4 mb-5">
            <div class="mini-stat mini-stat--blue">
                <div class="mini-stat__num">{{ tableData.length }}</div>
                <div class="mini-stat__label">全部模型</div>
            </div>
            <div class="mini-stat mini-stat--green">
                <div class="mini-stat__num">{{ tableData.filter(r => r.isEnabled).length }}</div>
                <div class="mini-stat__label">已启用</div>
            </div>
            <div class="mini-stat mini-stat--violet">
                <div class="mini-stat__num">{{ modelStats.totalTokens > 0 ? (modelStats.totalTokens / 1000).toFixed(0) + 'K' : '-' }}</div>
                <div class="mini-stat__label">总 Token</div>
            </div>
            <div class="mini-stat mini-stat--orange">
                <div class="mini-stat__num">{{ modelStats.totalRequests || 0 }}</div>
                <div class="mini-stat__label">总请求</div>
            </div>
        </div>

        <!-- 用量趋势图 -->
        <el-card shadow="never" class="ai-card mb-5" v-if="trendData.length > 0">
            <template #header>
                <div class="flex justify-between items-center">
                    <span class="card-title">用量趋势（近 30 天）</span>
                    <el-select v-model="trendDays" size="small" class="!w-28" @change="loadTrend">
                        <el-option label="7 天" :value="7" />
                        <el-option label="14 天" :value="14" />
                        <el-option label="30 天" :value="30" />
                    </el-select>
                </div>
            </template>
            <div class="flex items-end gap-1 h-32">
                <div v-for="(item, idx) in trendData" :key="idx"
                    class="flex-1 flex flex-col items-center gap-1 group relative cursor-default">
                    <div class="w-full bg-gradient-to-t from-blue-400 to-indigo-500 rounded-t-sm transition-all hover:opacity-80"
                        :style="{ height: barHeight(item.tokens) + 'px', minHeight: '4px' }"
                        :title="`${item.date}: ${item.tokens} Token / ${item.requests} 次`">
                    </div>
                    <span v-if="idx % Math.max(1, Math.floor(trendData.length / 7)) === 0"
                        class="text-[10px] text-slate-400">{{ item.date.slice(5) }}</span>
                    <div class="absolute -top-8 left-1/2 -translate-x-1/2 bg-slate-800 text-white text-[10px] px-2 py-1 rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none z-10">
                        {{ item.date }}: {{ item.tokens }} Token
                    </div>
                </div>
            </div>
        </el-card>

        <!-- 主卡片 -->
        <el-card shadow="never" class="ai-card">
            <template #header>
                <div class="flex justify-between items-center flex-wrap gap-2">
                    <div class="flex items-center gap-2">
                        <span class="card-title">模型列表</span>
                        <el-tag size="small" class="ml-1">{{ tableData.length }} 条</el-tag>
                        <span v-if="selectedModels.length > 0" class="ml-2">
                            <el-tag size="small" type="info">已选 {{ selectedModels.length }}</el-tag>
                            <el-button size="small" type="success" plain class="ml-1" @click="batchEnable(true)">批量启用</el-button>
                            <el-button size="small" type="warning" plain @click="batchEnable(false)">批量禁用</el-button>
                        </span>
                    </div>
                    <el-button type="primary" :icon="Plus" :disabled="!isAdmin()" @click="handleAdd">新增模型</el-button>
                </div>
            </template>

            <el-table :data="tableData" style="width: 100%" @selection-change="handleSelectionChange">
                <el-table-column type="selection" width="40" :selectable="row => isAdmin()" />
                <el-table-column type="expand" width="40">
                    <template #default="{ row }">
                        <div class="px-8 py-4 bg-slate-50 dark:bg-slate-900">
                            <div class="flex items-center gap-2 mb-2">
                                <span class="text-sm font-medium">测试「{{ row.name }}」：</span>
                                <el-input v-model="row.testPrompt" size="small" placeholder="输入测试问题" class="!w-64" />
                                <el-button size="small" type="primary" @click="runInlineTest(row)" :loading="row.testing">发送</el-button>
                            </div>
                            <div v-if="row.testResult !== undefined" class="mt-2 p-3 bg-white dark:bg-gray-800 rounded-lg border border-slate-200 dark:border-slate-700 text-sm max-h-48 overflow-y-auto">
                                <div v-if="row.testResult === null" class="text-red-500">测试失败，请检查配置</div>
                                <div v-else-if="typeof row.testResult === 'string'" v-html="renderTestMd(row.testResult)"></div>
                                <div v-else class="text-slate-600 dark:text-slate-300">{{ row.testResult }}</div>
                            </div>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="name" label="模型名称" width="140">
                    <template #default="{ row }">
                        <div class="flex items-center gap-2">
                            <div class="model-avatar">{{ row.name?.charAt(0)?.toUpperCase() }}</div>
                            <span class="font-medium text-slate-700 text-sm">{{ row.name }}</span>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="type" label="类型" width="110">
                    <template #default="{ row }">
                        <el-tag size="small" :type="typeTagMap[row.type] || ''">{{ row.type }}</el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="model" label="模型标识" width="150">
                    <template #default="{ row }">
                        <span class="text-xs font-mono text-slate-500">{{ row.model }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="apiUrl" label="API 地址" min-width="160" show-overflow-tooltip>
                    <template #default="{ row }">
                        <span class="text-xs text-slate-400 font-mono">{{ row.apiUrl }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="isDefault" label="默认" width="70" align="center">
                    <template #default="{ row }">
                        <el-icon v-if="row.isDefault" class="text-amber-400" :size="16"><StarFilled /></el-icon>
                        <span v-else class="text-slate-300">—</span>
                    </template>
                </el-table-column>
                <el-table-column label="用量" width="100" align="center">
                    <template #default="{ row }">
                        <span class="text-xs text-slate-500">{{ row.usageTokens > 0 ? (row.usageTokens / 1000).toFixed(1) + 'K' : '-' }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="isEnabled" label="状态" width="80" align="center">
                    <template #default="{ row }">
                        <el-tag v-if="row.isEnabled" type="success" size="small">启用</el-tag>
                        <el-tag v-else type="info" size="small">禁用</el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="remark" label="备注" min-width="100" show-overflow-tooltip>
                    <template #default="{ row }">
                        <span class="text-xs text-slate-400">{{ row.remark || '—' }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="createTime" label="创建时间" width="160">
                    <template #default="{ row }">
                        <span class="text-xs text-slate-400">{{ row.createTime }}</span>
                    </template>
                </el-table-column>
                <el-table-column label="操作" width="200" fixed="right">
                    <template #default="{ row }">
                        <el-button type="success" link :disabled="!isAdmin()" @click="handleTest(row)">测试</el-button>
                        <el-button type="primary" link :disabled="!isAdmin()" @click="handleEdit(row)">编辑</el-button>
                        <el-button type="danger" link :disabled="!isAdmin()" @click="handleDelete(row)">删除</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </el-card>

        <!-- 新增/编辑对话框 -->
        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="580px">
            <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
                <el-form-item label="模型名称" prop="name">
                    <el-input v-model="form.name" placeholder="如：GPT-4 Turbo" />
                </el-form-item>
                <el-form-item label="模型类型" prop="type">
                    <el-select v-model="form.type" placeholder="请选择模型类型" style="width:100%">
                        <el-option label="OpenAI" value="openai" />
                        <el-option label="Claude" value="claude" />
                        <el-option label="DeepSeek" value="deepseek" />
                        <el-option label="Azure OpenAI" value="azure" />
                        <el-option label="MiniMax" value="minimax" />
                        <el-option label="Gemini" value="gemini" />
                        <el-option label="百度千帆" value="qianfan" />
                        <el-option label="智谱AI" value="zhipu" />
                        <el-option label="其他" value="other" />
                    </el-select>
                </el-form-item>
                <el-form-item label="模型标识" prop="model">
                    <el-input v-model="form.model" placeholder="如：gpt-4-turbo、claude-3-opus-20240229" />
                    <div class="text-xs text-slate-400 mt-1">填写官方 API 文档中的模型 ID</div>
                </el-form-item>
                <el-form-item label="API 地址" prop="apiUrl">
                    <el-input v-model="form.apiUrl" placeholder="如：https://api.openai.com/v1" :disabled="!isAdmin()" />
                </el-form-item>
                <el-form-item v-if="isAdmin()" label="API Key" prop="apiKey">
                    <el-input v-model="form.apiKey" type="password" show-password placeholder="请输入 API Key" />
                </el-form-item>
                <div class="grid grid-cols-2 gap-0">
                    <el-form-item label="设为默认">
                        <el-switch v-model="form.isDefault" />
                    </el-form-item>
                    <el-form-item label="是否启用">
                        <el-switch v-model="form.isEnabled" />
                    </el-form-item>
                </div>
                <el-form-item label="备注">
                    <el-input v-model="form.remark" type="textarea" :rows="2" placeholder="选填，便于识别用途" />
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
defineOptions({ name: 'AdminAiModel' })
import { ref, onMounted } from 'vue'
import { Plus, Cpu, StarFilled } from '@element-plus/icons-vue'
import { getAiModelList, createAiModel, updateAiModel, deleteAiModel, testAiModel, getAiModelStats, getAiModelTrend, batchUpdateModels } from '@/api/admin/aiModel'
import { showMessage, isAdmin } from '@/composables/util'
import { marked } from 'marked'
import { ElMessageBox } from 'element-plus'

const typeTagMap = { openai: '', claude: 'warning', gemini: 'success', azure: 'info', zhipu: 'danger', qianfan: 'warning', minimax: '', other: 'info' }

const tableData = ref([])
const selectedModels = ref([])
const modelStats = ref({ totalTokens: 0, totalRequests: 0 })
const trendData = ref([])
const trendDays = ref(14)
const dialogVisible = ref(false)
const dialogTitle = ref('新增模型')
const formRef = ref(null)
const dataLoaded = ref(false)

const form = ref({
    id: null, name: '', type: 'openai', model: '',
    apiUrl: '', apiKey: '', isDefault: false, isEnabled: true, remark: ''
})

const rules = {
    name: [{ required: true, message: '请输入模型名称', trigger: 'blur' }],
    type: [{ required: true, message: '请选择模型类型', trigger: 'change' }],
    model: [{ required: true, message: '请输入模型标识', trigger: 'blur' }],
    apiUrl: [{ required: true, message: '请输入 API 地址', trigger: 'blur' }],
    apiKey: [{ required: true, message: '请输入 API Key', trigger: 'blur' }]
}

function initData() {
    if (dataLoaded.value && tableData.value.length > 0) return
    getAiModelList().then(res => {
        if (res.success) { tableData.value = res.data || []; dataLoaded.value = true }
    })
}

function handleAdd() {
    if (!isAdmin()) { showMessage('演示账号仅支持查询操作！', 'error'); return }
    dialogTitle.value = '新增模型'
    form.value = { id: null, name: '', type: 'openai', model: '', apiUrl: '', apiKey: '', isDefault: false, isEnabled: true, remark: '' }
    dialogVisible.value = true
}

function handleEdit(row) {
    if (!isAdmin()) { showMessage('演示账号仅支持查询操作！', 'error'); return }
    dialogTitle.value = '编辑模型'
    form.value = { ...row }
    dialogVisible.value = true
}

function handleTest(row) {
    if (!isAdmin()) { showMessage('演示账号仅支持查询操作！', 'error'); return }
    showMessage('正在测试连接...', 'info')
    testAiModel(row.id).then(res => {
        if (res.success) showMessage(res.data || '✓ 测试成功', 'success')
        else showMessage(res.message || '✗ 测试失败', 'error')
    })
}

function handleSubmit() {
    if (!isAdmin()) { showMessage('演示账号仅支持查询操作！', 'error'); return }
    formRef.value.validate(valid => {
        if (valid) {
            const action = form.value.id ? updateAiModel(form.value) : createAiModel(form.value)
            action.then(res => {
                if (res.success) {
                    showMessage(form.value.id ? '更新成功' : '创建成功', 'success')
                    dialogVisible.value = false
                    dataLoaded.value = false
                    initData()
                }
            })
        }
    })
}

function handleDelete(row) {
    if (!isAdmin()) { showMessage('演示账号仅支持查询操作！', 'error'); return }
    ElMessageBox.confirm('确定要删除该模型吗？', '提示', { confirmButtonText: '确定', cancelButtonText: '取消', type: 'warning' }).then(() => {
        deleteAiModel(row.id).then(res => {
            if (res.success) { showMessage('删除成功', 'success'); dataLoaded.value = false; initData() }
        })
    })
}


const loadStats = async () => {
    try {
        const res = await getAiModelStats()
        if (res.success && res.data) {
            modelStats.value = res.data
            // Also update per-model usage
            if (res.data.perModel) {
                for (const row of tableData.value) {
                    const stat = res.data.perModel.find(p => p.model === row.model || p.name === row.name)
                    if (stat) {
                        row.usageTokens = stat.tokens
                        row.usageRequests = stat.requests
                    }
                }
            }
        }
    } catch {}
}

const loadTrend = async () => {
    try {
        const res = await getAiModelTrend(trendDays.value)
        if (res.success && res.data) {
            trendData.value = res.data
        }
    } catch {}
}

const barHeight = (tokens) => {
    if (!trendData.value.length) return 4
    const max = Math.max(...trendData.value.map(d => d.tokens))
    if (max === 0) return 4
    return Math.max(4, Math.round((tokens / max) * 120))
}

const handleSelectionChange = (rows) => {
    selectedModels.value = rows
}

const batchEnable = async (enable) => {
    if (!selectedModels.value.length) return
    const action = enable ? '启用' : '禁用'
    try {
        await ElMessageBox.confirm(`确定${action}选中的 ${selectedModels.value.length} 个模型？`, `批量${action}`, { type: 'warning' })
        await batchUpdateModels(selectedModels.value.map(r => r.id), enable)
        showMessage(`已${action}`)
        dataLoaded.value = false
        initData()
    } catch (e) {
        if (e !== 'cancel') showMessage('操作失败', 'error')
    }
}

const runInlineTest = async (row) => {
    if (!row.testPrompt?.trim()) { showMessage('请输入测试内容', 'warning'); return }
    row.testing = true
    row.testResult = undefined
    try {
        const res = await testAiModel(row.id)
        if (res.success) {
            row.testResult = res.data || '测试成功'
        } else {
            row.testResult = null
            showMessage(res.message || '测试失败', 'error')
        }
    } catch {
        row.testResult = null
        showMessage('测试失败', 'error')
    } finally {
        row.testing = false
    }
}

const renderTestMd = (text) => {
    try { return marked.parse(text) } catch { return text }
}

onMounted(() => { initData(); loadStats(); loadTrend() })
</script>

<style scoped>
.ai-page { min-height: 100%; }

.page-header__icon {
    width: 36px; height: 36px; border-radius: 10px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    display: flex; align-items: center; justify-content: center;
    color: white; flex-shrink: 0;
}

.mini-stat {
    border-radius: 12px; padding: 16px 20px;
    display: flex; flex-direction: column; gap: 4px;
    border: 1px solid transparent;
}
.mini-stat__num { font-size: 28px; font-weight: 700; line-height: 1; }
.mini-stat__label { font-size: 12px; opacity: 0.75; }
.mini-stat--blue   { background: linear-gradient(135deg,#eef2ff,#e0e7ff); color:#4338ca; border-color:#c7d2fe; }
.mini-stat--green  { background: linear-gradient(135deg,#f0fdf4,#dcfce7); color:#16a34a; border-color:#bbf7d0; }
.mini-stat--violet { background: linear-gradient(135deg,#fffbeb,#fef9c3); color:#b45309; border-color:#fde68a; }
.mini-stat--orange { background: linear-gradient(135deg,#fff7ed,#ffedd5); color:#c2410c; border-color:#fed7aa; }
.mini-stat--orange { background: linear-gradient(135deg,#fff7ed,#ffedd5); color:#c2410c; border-color:#fed7aa; }

:global(html.dark) .mini-stat--blue   { background: rgba(67, 56, 202, 0.15) !important; border-color: rgba(99, 102, 241, 0.3) !important; color: #a5b4fc !important; }
:global(html.dark) .mini-stat--green  { background: rgba(22, 163, 74, 0.15) !important;  border-color: rgba(52, 211, 153, 0.3) !important; color: #34d399 !important; }
:global(html.dark) .mini-stat--violet { background: rgba(147, 51, 234, 0.15) !important; border-color: rgba(167, 139, 250, 0.3) !important; color: #c084fc !important; }

.ai-card { border-radius: 14px !important; }
.card-title { font-size: 14px; font-weight: 600; color: var(--text-heading); }

.model-avatar {
    width: 28px; height: 28px; border-radius: 7px;
    background: linear-gradient(135deg,#6366f1,#8b5cf6);
    color: white; font-size: 13px; font-weight: 700;
    display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
</style>

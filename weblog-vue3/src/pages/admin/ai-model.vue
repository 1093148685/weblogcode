<template>
    <div class="page-shell ai-model-page">
        <div class="page-hero">
            <div class="page-hero__main">
                <div class="page-hero__icon">
                    <el-icon :size="20"><Cpu /></el-icon>
                </div>
                <div>
                    <h1 class="page-hero__title">AI 模型管理</h1>
                    <p class="page-hero__desc">集中管理旧版模型配置，检查默认模型、启用状态和连接可用性。</p>
                </div>
            </div>
            <div class="page-hero__actions">
                <el-button :icon="Refresh" @click="loadAll">刷新</el-button>
                <el-button type="primary" :icon="Plus" :disabled="!isAdmin()" @click="handleAdd">新增模型</el-button>
            </div>
        </div>

        <div class="ai-model-health">
            <div class="ai-model-health__main">
                <div class="ai-model-health__title">模型就绪度</div>
                <div class="ai-model-health__desc">{{ readinessText }}</div>
            </div>
            <div class="ai-model-health__score" :class="readinessClass">{{ readinessScore }}%</div>
        </div>

        <div class="page-stats">
            <div class="mini-stat mini-stat--blue">
                <div class="mini-stat__num">{{ tableData.length }}</div>
                <div class="mini-stat__label">全部模型</div>
            </div>
            <div class="mini-stat mini-stat--green">
                <div class="mini-stat__num">{{ enabledCount }}</div>
                <div class="mini-stat__label">已启用</div>
            </div>
            <div class="mini-stat mini-stat--violet">
                <div class="mini-stat__num">{{ defaultModel ? 1 : 0 }}</div>
                <div class="mini-stat__label">默认模型</div>
            </div>
            <div class="mini-stat mini-stat--orange">
                <div class="mini-stat__num">{{ formatNumber(modelStats.totalRequests || 0) }}</div>
                <div class="mini-stat__label">总请求</div>
            </div>
        </div>

        <el-alert
            v-if="readinessIssues.length"
            class="mb-4"
            type="warning"
            :closable="false"
            show-icon
        >
            <template #title>
                <span>{{ readinessIssues.join('；') }}</span>
            </template>
        </el-alert>

        <el-card shadow="never" class="ai-card" v-if="trendData.length > 0">
            <template #header>
                <div class="flex items-center justify-between">
                    <span class="card-title">用量趋势</span>
                    <el-select v-model="trendDays" size="small" class="!w-28" @change="loadTrend">
                        <el-option label="7 天" :value="7" />
                        <el-option label="14 天" :value="14" />
                        <el-option label="30 天" :value="30" />
                    </el-select>
                </div>
            </template>
            <div class="trend-bars">
                <div v-for="item in trendData" :key="item.date" class="trend-bar" :title="`${item.date}: ${item.tokens || 0} Token / ${item.requests || 0} 次`">
                    <div class="trend-bar__inner" :style="{ height: barHeight(item.tokens || 0) + 'px' }"></div>
                    <span>{{ item.date?.slice(5) }}</span>
                </div>
            </div>
        </el-card>

        <div class="section-toolbar">
            <el-input v-model="keyword" :prefix-icon="Search" clearable class="!w-64" placeholder="搜索名称、模型 ID、接口地址" />
            <el-select v-model="typeFilter" clearable class="!w-40" placeholder="全部类型">
                <el-option v-for="type in typeOptions" :key="type" :label="type" :value="type" />
            </el-select>
            <el-select v-model="statusFilter" class="!w-36" placeholder="状态">
                <el-option label="全部状态" value="all" />
                <el-option label="已启用" value="enabled" />
                <el-option label="已禁用" value="disabled" />
                <el-option label="默认模型" value="default" />
                <el-option label="缺少 Key" value="missingKey" />
            </el-select>
            <span class="section-toolbar__meta">共 {{ filteredModels.length }} 个模型</span>
        </div>

        <el-card shadow="never" class="ai-card admin-table-panel">
            <template #header>
                <div class="flex items-center justify-between gap-3 flex-wrap">
                    <div class="flex items-center gap-2">
                        <span class="card-title">模型列表</span>
                        <el-tag size="small">{{ filteredModels.length }} 个</el-tag>
                        <el-tag v-if="selectedModels.length" size="small" type="info">已选 {{ selectedModels.length }}</el-tag>
                    </div>
                    <div class="flex items-center gap-2">
                        <el-button v-if="selectedModels.length" size="small" plain type="success" @click="batchEnable(true)">批量启用</el-button>
                        <el-button v-if="selectedModels.length" size="small" plain type="warning" @click="batchEnable(false)">批量禁用</el-button>
                    </div>
                </div>
            </template>

            <el-table :data="filteredModels" v-loading="loading" style="width: 100%" @selection-change="handleSelectionChange" empty-text="暂无 AI 模型">
                <el-table-column type="selection" width="42" :selectable="() => isAdmin()" />
                <el-table-column type="expand" width="42">
                    <template #default="{ row }">
                        <div class="model-expand">
                            <div class="model-expand__grid">
                                <div>
                                    <span>接口地址</span>
                                    <strong>{{ row.apiUrl || '未配置' }}</strong>
                                </div>
                                <div>
                                    <span>备注</span>
                                    <strong>{{ row.remark || '暂无备注' }}</strong>
                                </div>
                                <div>
                                    <span>最后更新</span>
                                    <strong>{{ formatDate(row.updateTime) }}</strong>
                                </div>
                            </div>
                            <el-alert
                                v-if="!row.apiKey"
                                type="warning"
                                :closable="false"
                                title="该模型缺少 API Key，前台聊天和写作助手可能无法调用。"
                            />
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="name" label="模型" min-width="190">
                    <template #default="{ row }">
                        <div class="model-name">
                            <div class="model-avatar">{{ modelInitial(row) }}</div>
                            <div>
                                <div class="model-name__title">{{ row.name || row.model }}</div>
                                <div class="model-name__sub">{{ row.model || '未填写模型 ID' }}</div>
                            </div>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="type" label="类型" width="120">
                    <template #default="{ row }">
                        <el-tag size="small" :type="typeTagMap[row.type] || ''">{{ row.type || 'other' }}</el-tag>
                    </template>
                </el-table-column>
                <el-table-column label="角色" width="120" align="center">
                    <template #default="{ row }">
                        <el-tag v-if="row.isDefault" type="warning" size="small">默认</el-tag>
                        <span v-else class="muted-dash">-</span>
                    </template>
                </el-table-column>
                <el-table-column label="状态" width="120" align="center">
                    <template #default="{ row }">
                        <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small">
                            {{ row.isEnabled ? '启用' : '禁用' }}
                        </el-tag>
                    </template>
                </el-table-column>
                <el-table-column label="Key" width="110" align="center">
                    <template #default="{ row }">
                        <el-tag :type="row.apiKey ? 'success' : 'danger'" size="small" effect="plain">
                            {{ row.apiKey ? '已配置' : '缺失' }}
                        </el-tag>
                    </template>
                </el-table-column>
                <el-table-column label="连通性" width="150">
                    <template #default="{ row }">
                        <div class="test-status">
                            <span :class="['test-dot', testState(row).className]"></span>
                            <span>{{ testState(row).text }}</span>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="createTime" label="创建时间" width="170">
                    <template #default="{ row }">
                        <span class="text-xs text-slate-400">{{ formatDate(row.createTime) }}</span>
                    </template>
                </el-table-column>
                <el-table-column label="操作" width="220" fixed="right">
                    <template #default="{ row }">
                        <el-button link type="success" :disabled="!isAdmin()" :loading="testingMap[row.id]" @click="handleTest(row)">测试</el-button>
                        <el-button link type="primary" :disabled="!isAdmin()" @click="handleEdit(row)">编辑</el-button>
                        <el-button link type="danger" :disabled="!isAdmin()" @click="handleDelete(row)">删除</el-button>
                    </template>
                </el-table-column>
            </el-table>
        </el-card>

        <el-dialog v-model="dialogVisible" :title="dialogTitle" width="620px" class="admin-dialog">
            <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
                <el-form-item label="模型名称" prop="name">
                    <el-input v-model="form.name" placeholder="例如：DeepSeek V3" />
                </el-form-item>
                <el-form-item label="模型类型" prop="type">
                    <el-select v-model="form.type" placeholder="请选择模型类型" style="width: 100%">
                        <el-option label="OpenAI" value="openai" />
                        <el-option label="Claude" value="claude" />
                        <el-option label="DeepSeek" value="deepseek" />
                        <el-option label="Azure OpenAI" value="azure" />
                        <el-option label="MiniMax" value="minimax" />
                        <el-option label="Gemini" value="gemini" />
                        <el-option label="百度千帆" value="qianfan" />
                        <el-option label="智谱 AI" value="zhipu" />
                        <el-option label="其他" value="other" />
                    </el-select>
                </el-form-item>
                <el-form-item label="模型 ID" prop="model">
                    <el-input v-model="form.model" placeholder="例如：deepseek-chat、gpt-4o-mini" />
                </el-form-item>
                <el-form-item label="API 地址" prop="apiUrl">
                    <el-input v-model="form.apiUrl" placeholder="例如：https://api.deepseek.com/v1" />
                </el-form-item>
                <el-form-item label="API Key" prop="apiKey">
                    <el-input v-model="form.apiKey" type="password" show-password placeholder="请输入 API Key" />
                </el-form-item>
                <div class="model-switch-row">
                    <el-form-item label="默认模型">
                        <el-switch v-model="form.isDefault" />
                    </el-form-item>
                    <el-form-item label="启用">
                        <el-switch v-model="form.isEnabled" />
                    </el-form-item>
                </div>
                <el-form-item label="备注">
                    <el-input v-model="form.remark" type="textarea" :rows="3" placeholder="用于记录模型用途、费用、限制或调用场景" />
                </el-form-item>
            </el-form>
            <template #footer>
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" :loading="submitting" @click="handleSubmit">保存</el-button>
            </template>
        </el-dialog>
    </div>
</template>

<script setup>
defineOptions({ name: 'AdminAiModel' })

import { computed, onMounted, reactive, ref } from 'vue'
import { Cpu, Plus, Refresh, Search } from '@element-plus/icons-vue'
import { ElMessageBox } from 'element-plus'
import {
    batchUpdateModels,
    createAiModel,
    deleteAiModel,
    getAiModelList,
    getAiModelStats,
    getAiModelTrend,
    testAiModel,
    updateAiModel
} from '@/api/admin/aiModel'
import { showMessage, isAdmin } from '@/composables/util'

const typeTagMap = {
    openai: '',
    claude: 'warning',
    deepseek: 'success',
    gemini: 'success',
    azure: 'info',
    zhipu: 'danger',
    qianfan: 'warning',
    minimax: '',
    other: 'info'
}

const loading = ref(false)
const submitting = ref(false)
const tableData = ref([])
const selectedModels = ref([])
const modelStats = ref({ totalTokens: 0, totalRequests: 0 })
const trendData = ref([])
const trendDays = ref(14)
const keyword = ref('')
const typeFilter = ref('')
const statusFilter = ref('all')
const dialogVisible = ref(false)
const dialogTitle = ref('新增模型')
const formRef = ref(null)
const testingMap = reactive({})
const lastTestMap = reactive({})

const emptyForm = () => ({
    id: null,
    name: '',
    type: 'deepseek',
    model: '',
    apiUrl: '',
    apiKey: '',
    isDefault: false,
    isEnabled: true,
    remark: ''
})

const form = ref(emptyForm())

const rules = {
    name: [{ required: true, message: '请输入模型名称', trigger: 'blur' }],
    type: [{ required: true, message: '请选择模型类型', trigger: 'change' }],
    model: [{ required: true, message: '请输入模型 ID', trigger: 'blur' }],
    apiUrl: [{ required: true, message: '请输入 API 地址', trigger: 'blur' }],
    apiKey: [{ required: true, message: '请输入 API Key', trigger: 'blur' }]
}

const enabledCount = computed(() => tableData.value.filter(item => item.isEnabled).length)
const defaultModel = computed(() => tableData.value.find(item => item.isDefault))
const typeOptions = computed(() => [...new Set(tableData.value.map(item => item.type).filter(Boolean))])
const readinessIssues = computed(() => {
    const issues = []
    if (!tableData.value.length) issues.push('还没有配置模型')
    if (!enabledCount.value) issues.push('没有已启用模型')
    if (!defaultModel.value) issues.push('没有默认模型')
    if (tableData.value.some(item => item.isEnabled && !item.apiKey)) issues.push('存在已启用但缺少 Key 的模型')
    return issues
})
const readinessScore = computed(() => {
    let score = 0
    if (tableData.value.length) score += 25
    if (enabledCount.value) score += 30
    if (defaultModel.value) score += 25
    if (!tableData.value.some(item => item.isEnabled && !item.apiKey)) score += 20
    return score
})
const readinessClass = computed(() => readinessScore.value >= 80 ? 'is-good' : readinessScore.value >= 50 ? 'is-warn' : 'is-danger')
const readinessText = computed(() => readinessIssues.value.length ? readinessIssues.value[0] : `默认使用 ${defaultModel.value?.name || defaultModel.value?.model}`)

const filteredModels = computed(() => {
    const kw = keyword.value.trim().toLowerCase()
    return tableData.value.filter(item => {
        const matchesKeyword = !kw ||
            item.name?.toLowerCase().includes(kw) ||
            item.model?.toLowerCase().includes(kw) ||
            item.apiUrl?.toLowerCase().includes(kw)
        const matchesType = !typeFilter.value || item.type === typeFilter.value
        const matchesStatus =
            statusFilter.value === 'all' ||
            (statusFilter.value === 'enabled' && item.isEnabled) ||
            (statusFilter.value === 'disabled' && !item.isEnabled) ||
            (statusFilter.value === 'default' && item.isDefault) ||
            (statusFilter.value === 'missingKey' && !item.apiKey)
        return matchesKeyword && matchesType && matchesStatus
    })
})

const loadModels = async () => {
    loading.value = true
    try {
        const res = await getAiModelList()
        if (res.success) tableData.value = res.data || []
        else showMessage(res.message || '模型列表加载失败', 'error')
    } catch {
        showMessage('模型列表加载失败', 'error')
    } finally {
        loading.value = false
    }
}

const loadStats = async () => {
    try {
        const res = await getAiModelStats()
        if (res.success && res.data) modelStats.value = res.data
    } catch {}
}

const loadTrend = async () => {
    try {
        const res = await getAiModelTrend(trendDays.value)
        if (res.success && Array.isArray(res.data)) trendData.value = res.data
    } catch {}
}

const loadAll = async () => {
    await Promise.all([loadModels(), loadStats(), loadTrend()])
}

const handleAdd = () => {
    if (!isAdmin()) {
        showMessage('当前账号没有新增权限', 'error')
        return
    }
    dialogTitle.value = '新增模型'
    form.value = emptyForm()
    dialogVisible.value = true
}

const handleEdit = (row) => {
    if (!isAdmin()) {
        showMessage('当前账号没有编辑权限', 'error')
        return
    }
    dialogTitle.value = '编辑模型'
    form.value = { ...row }
    dialogVisible.value = true
}

const handleSubmit = async () => {
    if (!isAdmin()) {
        showMessage('当前账号没有保存权限', 'error')
        return
    }
    const valid = await formRef.value?.validate().catch(() => false)
    if (!valid) return

    submitting.value = true
    try {
        const action = form.value.id ? updateAiModel(form.value) : createAiModel(form.value)
        const res = await action
        if (res.success) {
            showMessage(form.value.id ? '模型已更新' : '模型已创建', 'success')
            dialogVisible.value = false
            await loadModels()
        } else {
            showMessage(res.message || '保存失败', 'error')
        }
    } finally {
        submitting.value = false
    }
}

const handleDelete = async (row) => {
    if (!isAdmin()) {
        showMessage('当前账号没有删除权限', 'error')
        return
    }
    try {
        await ElMessageBox.confirm(`确定删除模型“${row.name || row.model}”吗？`, '删除模型', { type: 'warning' })
        const res = await deleteAiModel(row.id)
        if (res.success) {
            showMessage('模型已删除', 'success')
            await loadModels()
        } else {
            showMessage(res.message || '删除失败', 'error')
        }
    } catch (e) {
        if (e !== 'cancel') showMessage('删除失败', 'error')
    }
}

const handleTest = async (row) => {
    if (!isAdmin()) {
        showMessage('当前账号没有测试权限', 'error')
        return
    }
    testingMap[row.id] = true
    const startedAt = performance.now()
    try {
        const res = await testAiModel(row.id)
        const latency = Math.max(1, Math.round(performance.now() - startedAt))
        lastTestMap[row.id] = {
            ok: !!res.success,
            message: res.success ? '连接正常' : (res.message || '连接失败'),
            latency
        }
        showMessage(res.success ? `连接正常，耗时 ${latency}ms` : (res.message || '连接失败'), res.success ? 'success' : 'error')
    } catch (e) {
        lastTestMap[row.id] = { ok: false, message: '连接失败', latency: 0 }
        showMessage('连接失败', 'error')
    } finally {
        testingMap[row.id] = false
    }
}

const batchEnable = async (enable) => {
    if (!selectedModels.value.length) return
    const action = enable ? '启用' : '禁用'
    try {
        await ElMessageBox.confirm(`确定${action}选中的 ${selectedModels.value.length} 个模型吗？`, `批量${action}`, { type: 'warning' })
        const res = await batchUpdateModels(selectedModels.value.map(row => row.id), enable)
        if (res.success) {
            showMessage(`已批量${action}`, 'success')
            await loadModels()
        } else {
            showMessage(res.message || '批量操作失败', 'error')
        }
    } catch (e) {
        if (e !== 'cancel') showMessage('批量操作失败', 'error')
    }
}

const handleSelectionChange = rows => {
    selectedModels.value = rows
}

const modelInitial = row => (row.name || row.model || 'AI').slice(0, 1).toUpperCase()
const formatNumber = value => Number(value || 0).toLocaleString()
const formatDate = value => value ? String(value).replace('T', ' ').slice(0, 19) : '-'

const testState = row => {
    if (testingMap[row.id]) return { text: '测试中', className: 'is-testing' }
    const last = lastTestMap[row.id]
    if (!last) return { text: row.apiKey ? '未测试' : '缺少 Key', className: row.apiKey ? 'is-idle' : 'is-fail' }
    return {
        text: last.ok ? `${last.latency}ms` : '失败',
        className: last.ok ? 'is-ok' : 'is-fail'
    }
}

const barHeight = tokens => {
    const max = Math.max(...trendData.value.map(item => item.tokens || 0), 0)
    if (!max) return 4
    return Math.max(4, Math.round((tokens / max) * 110))
}

onMounted(loadAll)
</script>

<style scoped>
.ai-model-page {
    min-height: 100%;
}

.card-title {
    color: var(--admin-text);
    font-size: 14px;
    font-weight: 700;
}

.ai-model-page :deep(.page-stats) {
    grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
}

.ai-model-health {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 20px;
    padding: 18px 20px;
    margin-bottom: 16px;
    border: 1px solid var(--admin-border);
    border-radius: 14px;
    background:
        radial-gradient(circle at 12% 0%, rgba(34, 211, 238, 0.16), transparent 32%),
        linear-gradient(135deg, rgba(15, 23, 42, 0.86), rgba(17, 24, 39, 0.72));
    box-shadow: var(--admin-shadow);
}

.ai-model-health__title {
    color: var(--admin-text);
    font-size: 15px;
    font-weight: 800;
}

.ai-model-health__desc {
    margin-top: 4px;
    color: var(--admin-text-muted);
    font-size: 13px;
}

.ai-model-health__score {
    min-width: 86px;
    text-align: right;
    font-size: 28px;
    font-weight: 900;
}

.ai-model-health__score.is-good {
    color: #34d399;
}

.ai-model-health__score.is-warn {
    color: #fbbf24;
}

.ai-model-health__score.is-danger {
    color: #fb7185;
}

.trend-bars {
    display: flex;
    align-items: flex-end;
    gap: 6px;
    height: 142px;
}

.trend-bar {
    display: flex;
    flex: 1;
    min-width: 0;
    align-items: center;
    flex-direction: column;
    justify-content: flex-end;
    gap: 6px;
}

.trend-bar__inner {
    width: 100%;
    min-height: 4px;
    border-radius: 6px 6px 2px 2px;
    background: linear-gradient(180deg, #22d3ee, #6366f1);
}

.trend-bar span {
    color: var(--admin-text-muted);
    font-size: 10px;
}

.model-name {
    display: flex;
    align-items: center;
    gap: 10px;
    min-width: 0;
}

.model-avatar {
    display: flex;
    width: 32px;
    height: 32px;
    flex-shrink: 0;
    align-items: center;
    justify-content: center;
    border-radius: 10px;
    color: white;
    background: linear-gradient(135deg, #6366f1, #22d3ee);
    font-size: 13px;
    font-weight: 800;
}

.model-name__title {
    color: var(--admin-text);
    font-size: 13px;
    font-weight: 800;
}

.model-name__sub {
    color: var(--admin-text-muted);
    font-size: 12px;
    font-family: 'SF Mono', 'Fira Code', monospace;
}

.muted-dash {
    color: var(--admin-text-muted);
}

.test-status {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    color: var(--admin-text-muted);
    font-size: 12px;
}

.test-dot {
    width: 8px;
    height: 8px;
    border-radius: 999px;
    background: #64748b;
}

.test-dot.is-ok {
    background: #34d399;
    box-shadow: 0 0 0 4px rgba(52, 211, 153, 0.12);
}

.test-dot.is-fail {
    background: #fb7185;
    box-shadow: 0 0 0 4px rgba(251, 113, 133, 0.12);
}

.test-dot.is-testing {
    background: #38bdf8;
    box-shadow: 0 0 0 4px rgba(56, 189, 248, 0.14);
}

.model-expand {
    display: flex;
    flex-direction: column;
    gap: 12px;
    padding: 16px 22px;
}

.model-expand__grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 12px;
}

.model-expand__grid div {
    padding: 12px;
    border: 1px solid var(--admin-border);
    border-radius: 10px;
    background: rgba(15, 23, 42, 0.4);
}

.model-expand__grid span {
    display: block;
    margin-bottom: 6px;
    color: var(--admin-text-muted);
    font-size: 12px;
}

.model-expand__grid strong {
    display: block;
    color: var(--admin-text);
    font-size: 13px;
    font-weight: 700;
    word-break: break-all;
}

.model-switch-row {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
}

@media (max-width: 900px) {
    .model-expand__grid,
    .model-switch-row {
        grid-template-columns: 1fr;
    }

    .ai-model-health {
        align-items: flex-start;
        flex-direction: column;
    }
}
</style>

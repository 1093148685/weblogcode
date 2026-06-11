<template>
    <div class="page-shell ai-access-page">
        <div class="page-hero">
            <div class="page-hero__main">
                <div class="page-hero__icon">
                    <el-icon :size="20"><Connection /></el-icon>
                </div>
                <div>
                    <h1 class="page-hero__title">AI 接入配置</h1>
                    <p class="page-hero__desc">统一管理提供商、Base URL、模型、请求头和多 API Key</p>
                </div>
            </div>
            <div class="page-hero__actions">
                <el-button :icon="Refresh" @click="loadData" :loading="loading">刷新</el-button>
                <el-button type="primary" :icon="Plus" :disabled="!isAdmin()" @click="handleAdd">新增提供商</el-button>
            </div>
        </div>

        <div class="page-stats">
            <div class="mini-stat mini-stat--blue">
                <div class="mini-stat__num">{{ providers.length }}</div>
                <div class="mini-stat__label">提供商</div>
            </div>
            <div class="mini-stat mini-stat--green">
                <div class="mini-stat__num">{{ enabledProviders }}</div>
                <div class="mini-stat__label">已启用</div>
            </div>
            <div class="mini-stat mini-stat--amber">
                <div class="mini-stat__num">{{ totalModels }}</div>
                <div class="mini-stat__label">可选模型</div>
            </div>
            <div class="mini-stat mini-stat--cyan">
                <div class="mini-stat__num">{{ totalKeys }}</div>
                <div class="mini-stat__label">密钥</div>
            </div>
        </div>

        <div class="access-layout">
            <aside class="provider-panel">
                <div class="panel-toolbar">
                    <el-input v-model="keyword" :prefix-icon="Search" clearable placeholder="搜索提供商" />
                </div>

                <div v-loading="loading" class="provider-list">
                    <button
                        v-for="item in filteredProviders"
                        :key="item.id"
                        type="button"
                        class="provider-item"
                        :class="{ 'is-active': selectedProvider?.id === item.id }"
                        @click="selectProvider(item)"
                    >
                        <span class="provider-item__avatar">{{ providerInitial(item) }}</span>
                        <span class="provider-item__main">
                            <strong>{{ item.displayName || item.name }}</strong>
                            <small>{{ item.prefix ? `${item.prefix}/` : item.name }} · {{ item.protocol || 'openai-compatible' }}</small>
                        </span>
                        <el-tag size="small" :type="item.isEnabled ? 'success' : 'info'">
                            {{ item.isEnabled ? '启用' : '停用' }}
                        </el-tag>
                    </button>

                    <div v-if="!filteredProviders.length" class="empty-panel">
                        暂无提供商
                    </div>
                </div>
            </aside>

            <main class="editor-panel">
                <div class="editor-header">
                    <div>
                        <h2>{{ isEditing ? form.displayName || form.name : '新增提供商' }}</h2>
                        <p>{{ form.name ? `${form.name}${form.configData.prefix ? ` · ${form.configData.prefix}/<model>` : ''}` : '自由命名，不限制厂商列表' }}</p>
                    </div>
                    <div class="editor-actions">
                        <el-button :icon="Connection" :disabled="!isEditing" :loading="testing" @click="handleTest">连通性测试</el-button>
                        <el-button type="primary" :disabled="!isAdmin()" :loading="submitting" @click="handleSubmit">保存</el-button>
                    </div>
                </div>

                <el-form ref="formRef" :model="form" :rules="rules" label-position="top" class="access-form">
                    <el-tabs v-model="activeTab">
                        <el-tab-pane label="基础" name="basic">
                            <div class="form-grid">
                                <el-form-item label="配置模板">
                                    <el-select v-model="templateName" clearable filterable placeholder="选择后自动填充，可继续修改" @change="applyTemplate">
                                        <el-option v-for="tpl in providerTemplates" :key="tpl.name" :label="tpl.label" :value="tpl.name" />
                                    </el-select>
                                </el-form-item>
                                <el-form-item label="提供商名称" prop="name">
                                    <el-input v-model="form.name" :disabled="isEditing" placeholder="例如 openrouter、team-a、my-proxy" />
                                </el-form-item>
                                <el-form-item label="显示名称" prop="displayName">
                                    <el-input v-model="form.displayName" placeholder="例如 OpenRouter 主账号" />
                                </el-form-item>
                                <el-form-item label="协议">
                                    <el-select v-model="form.configData.protocol">
                                        <el-option label="OpenAI Compatible" value="openai-compatible" />
                                        <el-option label="Anthropic" value="anthropic" />
                                        <el-option label="Gemini" value="gemini" />
                                        <el-option label="Azure OpenAI" value="azure" />
                                    </el-select>
                                </el-form-item>
                                <el-form-item label="用途类型">
                                    <el-select v-model="form.type">
                                        <el-option label="对话 Chat" value="chat" />
                                        <el-option label="Embedding" value="embedding" />
                                        <el-option label="图片 Image" value="image" />
                                        <el-option label="音频 Audio" value="audio" />
                                    </el-select>
                                </el-form-item>
                                <el-form-item label="前缀">
                                    <el-input v-model="form.configData.prefix" placeholder="例如 team-a，模型可显示为 team-a/gpt-4o" />
                                </el-form-item>
                                <el-form-item label="Base URL" prop="apiUrl" class="form-grid__wide">
                                    <el-input v-model="form.apiUrl" placeholder="例如 https://api.openai.com/v1" />
                                </el-form-item>
                                <el-form-item label="优先级">
                                    <el-input-number v-model="form.priority" :min="1" :max="999" />
                                </el-form-item>
                                <el-form-item label="启用">
                                    <el-switch v-model="form.isEnabled" />
                                </el-form-item>
                            </div>
                        </el-tab-pane>

                        <el-tab-pane label="请求头" name="headers">
                            <div class="section-head">
                                <span>自定义 Headers</span>
                                <el-button :icon="Plus" @click="addHeader">添加请求头</el-button>
                            </div>
                            <div class="rows">
                                <div v-for="(header, index) in form.configData.headers" :key="index" class="config-row config-row--header">
                                    <el-switch v-model="header.enabled" />
                                    <el-input v-model="header.name" placeholder="Header 名称，例如 X-Custom-Header" />
                                    <el-input v-model="header.value" placeholder="Header 值，支持 {apiKey}" />
                                    <el-button :icon="Delete" circle text type="danger" @click="removeHeader(index)" />
                                </div>
                                <div v-if="!form.configData.headers.length" class="empty-panel">没有自定义请求头时会默认使用 Authorization: Bearer Key</div>
                            </div>
                        </el-tab-pane>

                        <el-tab-pane label="模型" name="models">
                            <div class="model-tools">
                                <el-input v-model="form.configData.modelsPath" placeholder="/models" class="model-tools__path">
                                    <template #prepend>模型路径</template>
                                </el-input>
                                <el-button :icon="Download" :loading="fetchingModels" @click="handleFetchModels">从 Base URL 获取</el-button>
                                <el-button :icon="Plus" @click="addModel">手动添加</el-button>
                            </div>

                            <div v-if="fetchedModels.length" class="fetched-box">
                                <el-select v-model="selectedFetchedModels" multiple filterable collapse-tags collapse-tags-tooltip placeholder="选择要加入配置的模型">
                                    <el-option v-for="model in fetchedModels" :key="model.id" :label="model.name || model.id" :value="model.id" />
                                </el-select>
                                <el-button type="primary" @click="addFetchedModels">加入模型列表</el-button>
                            </div>

                            <div class="rows">
                                <div v-for="(model, index) in form.configData.models" :key="index" class="config-row config-row--model">
                                    <el-switch v-model="model.isEnabled" />
                                    <el-input v-model="model.id" placeholder="模型 ID，例如 gpt-4o-mini" />
                                    <el-input v-model="model.name" placeholder="显示名" />
                                    <el-input v-model="model.alias" placeholder="别名，可选" />
                                    <el-checkbox v-model="model.isDefault" @change="setDefaultModel(index)">默认</el-checkbox>
                                    <el-button :icon="Delete" circle text type="danger" @click="removeModel(index)" />
                                </div>
                                <div v-if="!form.configData.models.length" class="empty-panel">还没有模型，可以手动添加或从 Base URL 获取</div>
                            </div>
                        </el-tab-pane>

                        <el-tab-pane label="密钥" name="keys">
                            <div class="section-head">
                                <span>API Keys</span>
                                <el-button :icon="Plus" @click="addKey">添加密钥</el-button>
                            </div>
                            <div class="rows">
                                <div v-for="(key, index) in form.configData.keys" :key="key.id || index" class="config-row config-row--key">
                                    <el-switch v-model="key.isEnabled" />
                                    <el-input
                                        v-model="key.value"
                                        type="password"
                                        show-password
                                        :placeholder="key.maskedValue || '输入 API Key'"
                                    />
                                    <el-input v-model="key.proxyUrl" placeholder="代理 URL，可选，例如 socks5://127.0.0.1:7890" />
                                    <el-tag size="small" :type="key.status === 'healthy' ? 'success' : 'info'">{{ key.status || 'unknown' }}</el-tag>
                                    <el-button :icon="Delete" circle text type="danger" @click="removeKey(index)" />
                                </div>
                                <div v-if="!form.configData.keys.length" class="empty-panel">至少添加一个密钥后才能测试和调用</div>
                            </div>
                        </el-tab-pane>
                    </el-tabs>
                </el-form>

                <div class="editor-footer">
                    <div class="route-preview">
                        <span>模型选择示例</span>
                        <strong>{{ modelPreview }}</strong>
                    </div>
                    <el-button v-if="isEditing" type="danger" plain :disabled="!isAdmin()" @click="handleDelete">删除提供商</el-button>
                </div>
            </main>
        </div>
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessageBox } from 'element-plus'
import { Connection, Delete, Download, Plus, Refresh, Search } from '@element-plus/icons-vue'
import {
    createAiProvider,
    deleteAiProvider,
    fetchModelsWithConfig,
    fetchProviderModels,
    getAiProviders,
    testAiProviderWithOptions,
    updateAiProvider
} from '@/api/admin/ai-provider'
import { showMessage, isAdmin } from '@/composables/util'

defineOptions({ name: 'AdminAiProvider' })

const providerTemplates = [
    { name: 'openai', label: 'OpenAI', displayName: 'OpenAI', apiUrl: 'https://api.openai.com/v1', protocol: 'openai-compatible', modelsPath: '/models', chatPath: '/chat/completions' },
    { name: 'deepseek', label: 'DeepSeek', displayName: 'DeepSeek', apiUrl: 'https://api.deepseek.com/v1', protocol: 'openai-compatible', modelsPath: '/models', chatPath: '/chat/completions' },
    { name: 'openrouter', label: 'OpenRouter', displayName: 'OpenRouter', apiUrl: 'https://openrouter.ai/api/v1', protocol: 'openai-compatible', modelsPath: '/models', chatPath: '/chat/completions' },
    { name: 'siliconflow', label: 'SiliconFlow', displayName: 'SiliconFlow', apiUrl: 'https://api.siliconflow.cn/v1', protocol: 'openai-compatible', modelsPath: '/models', chatPath: '/chat/completions' },
    { name: 'claude', label: 'Claude', displayName: 'Claude', apiUrl: 'https://api.anthropic.com/v1', protocol: 'anthropic', modelsPath: '/models', chatPath: '/messages' },
    { name: 'custom', label: '自定义兼容接口', displayName: 'Custom AI Provider', apiUrl: '', protocol: 'openai-compatible', modelsPath: '/models', chatPath: '/chat/completions' }
]

const loading = ref(false)
const submitting = ref(false)
const testing = ref(false)
const fetchingModels = ref(false)
const providers = ref([])
const selectedProvider = ref(null)
const keyword = ref('')
const templateName = ref('')
const activeTab = ref('basic')
const formRef = ref(null)
const fetchedModels = ref([])
const selectedFetchedModels = ref([])

const emptyConfig = () => ({
    protocol: 'openai-compatible',
    prefix: '',
    modelsPath: '/models',
    chatPath: '/chat/completions',
    headers: [],
    models: [],
    keys: []
})

const emptyForm = () => ({
    id: null,
    name: '',
    displayName: '',
    type: 'chat',
    apiUrl: '',
    priority: 100,
    isEnabled: true,
    configData: emptyConfig()
})

const form = reactive(emptyForm())

const rules = {
    name: [{ required: true, message: '请输入提供商名称', trigger: 'blur' }],
    displayName: [{ required: true, message: '请输入显示名称', trigger: 'blur' }],
    apiUrl: [{ required: true, message: '请输入 Base URL', trigger: 'blur' }]
}

const isEditing = computed(() => !!form.id)
const enabledProviders = computed(() => providers.value.filter(item => item.isEnabled).length)
const totalModels = computed(() => providers.value.reduce((sum, item) => sum + (item.modelCount || item.configData?.models?.filter(m => m.isEnabled).length || 0), 0))
const totalKeys = computed(() => providers.value.reduce((sum, item) => sum + (item.keyCount || item.configData?.keys?.filter(k => k.isEnabled).length || 0), 0))
const filteredProviders = computed(() => {
    const kw = keyword.value.trim().toLowerCase()
    if (!kw) return providers.value
    return providers.value.filter(item =>
        item.name?.toLowerCase().includes(kw) ||
        item.displayName?.toLowerCase().includes(kw) ||
        item.prefix?.toLowerCase().includes(kw)
    )
})
const modelPreview = computed(() => {
    const firstModel = form.configData.models.find(item => item.isEnabled && item.id)
    const modelId = firstModel?.id || '<model>'
    return form.configData.prefix ? `${form.configData.prefix}/${modelId}` : modelId
})

const loadData = async () => {
    loading.value = true
    try {
        const res = await getAiProviders()
        if (res.success || res.code === 200) {
            providers.value = (res.data || []).map(normalizeProvider)
            if (!selectedProvider.value && providers.value.length) selectProvider(providers.value[0])
            if (selectedProvider.value) {
                const fresh = providers.value.find(item => item.id === selectedProvider.value.id)
                if (fresh) selectProvider(fresh)
            }
        } else {
            showMessage(res.message || '加载 AI 接入配置失败', 'error')
        }
    } catch (e) {
        showMessage(e.message || '加载 AI 接入配置失败', 'error')
    } finally {
        loading.value = false
    }
}

const normalizeProvider = item => ({
    ...item,
    configData: {
        ...emptyConfig(),
        ...(item.configData || {}),
        headers: item.configData?.headers || [],
        models: item.configData?.models || [],
        keys: item.configData?.keys || []
    }
})

const resetForm = data => {
    Object.assign(form, emptyForm(), data)
    form.configData = {
        ...emptyConfig(),
        ...(data?.configData || {}),
        headers: [...(data?.configData?.headers || [])],
        models: [...(data?.configData?.models || [])],
        keys: [...(data?.configData?.keys || [])]
    }
}

const selectProvider = item => {
    selectedProvider.value = item
    templateName.value = ''
    fetchedModels.value = []
    selectedFetchedModels.value = []
    resetForm(normalizeProvider(item))
}

const handleAdd = () => {
    selectedProvider.value = null
    templateName.value = ''
    fetchedModels.value = []
    selectedFetchedModels.value = []
    resetForm(emptyForm())
    activeTab.value = 'basic'
}

const applyTemplate = name => {
    const tpl = providerTemplates.find(item => item.name === name)
    if (!tpl) return
    if (!isEditing.value && (!form.name || form.name === 'custom')) form.name = tpl.name
    form.displayName = tpl.displayName
    form.apiUrl = tpl.apiUrl
    form.configData.protocol = tpl.protocol
    form.configData.modelsPath = tpl.modelsPath
    form.configData.chatPath = tpl.chatPath
}

const buildPayload = () => ({
    name: form.name.trim(),
    displayName: form.displayName.trim(),
    type: form.type,
    protocol: form.configData.protocol,
    prefix: form.configData.prefix,
    apiUrl: form.apiUrl.trim(),
    priority: form.priority,
    isEnabled: form.isEnabled,
    configData: {
        ...form.configData,
        headers: form.configData.headers.filter(item => item.name || item.value),
        models: form.configData.models.filter(item => item.id),
        keys: form.configData.keys.filter(item => item.value || item.maskedValue)
    }
})

const handleSubmit = async () => {
    const valid = await formRef.value?.validate().catch(() => false)
    if (!valid) return
    submitting.value = true
    try {
        const payload = buildPayload()
        const res = isEditing.value
            ? await updateAiProvider(form.id, payload)
            : await createAiProvider(payload)
        if (res.success || res.code === 200) {
            showMessage('AI 接入配置已保存', 'success')
            await loadData()
            if (res.data?.id) {
                const saved = providers.value.find(item => item.id === res.data.id)
                if (saved) selectProvider(saved)
            }
        } else {
            showMessage(res.message || '保存失败', 'error')
        }
    } catch (e) {
        showMessage(e.message || '保存失败', 'error')
    } finally {
        submitting.value = false
    }
}

const handleDelete = async () => {
    if (!form.id) return
    try {
        await ElMessageBox.confirm(`确定删除「${form.displayName || form.name}」吗？`, '删除提供商', { type: 'warning' })
        const res = await deleteAiProvider(form.id)
        if (res.success || res.code === 200) {
            showMessage('提供商已删除', 'success')
            selectedProvider.value = null
            resetForm(emptyForm())
            await loadData()
        } else {
            showMessage(res.message || '删除失败', 'error')
        }
    } catch (e) {
        if (e !== 'cancel') showMessage(e.message || '删除失败', 'error')
    }
}

const handleTest = async () => {
    if (!form.id) {
        showMessage('请先保存提供商后再测试', 'warning')
        return
    }
    testing.value = true
    try {
        const defaultModel = form.configData.models.find(item => item.isDefault && item.isEnabled)?.id
            || form.configData.models.find(item => item.isEnabled)?.id
        const res = await testAiProviderWithOptions(form.id, { model: defaultModel })
        showMessage((res.success || res.code === 200) && res.data ? '连通性测试成功' : (res.message || '连通性测试失败'), (res.success || res.code === 200) && res.data ? 'success' : 'error')
    } catch (e) {
        showMessage(e.message || '连通性测试失败', 'error')
    } finally {
        testing.value = false
    }
}

const handleFetchModels = async () => {
    if (!form.apiUrl) {
        showMessage('请先填写 Base URL', 'warning')
        return
    }
    fetchingModels.value = true
    try {
        const payload = {
            apiUrl: form.apiUrl,
            modelsPath: form.configData.modelsPath,
            headers: form.configData.headers,
            apiKey: form.configData.keys.find(item => item.value)?.value
        }
        const res = form.id ? await fetchProviderModels(form.id, payload) : await fetchModelsWithConfig(payload)
        if (res.success || res.code === 200) {
            fetchedModels.value = res.data || []
            selectedFetchedModels.value = fetchedModels.value.map(item => item.id)
            showMessage(`获取到 ${fetchedModels.value.length} 个模型`, 'success')
        } else {
            showMessage(res.message || '获取模型失败', 'error')
        }
    } catch (e) {
        showMessage(e.message || '获取模型失败', 'error')
    } finally {
        fetchingModels.value = false
    }
}

const addFetchedModels = () => {
    const existing = new Set(form.configData.models.map(item => item.id))
    fetchedModels.value
        .filter(item => selectedFetchedModels.value.includes(item.id) && !existing.has(item.id))
        .forEach(item => {
            form.configData.models.push({
                id: item.id,
                name: item.name || item.id,
                alias: '',
                isEnabled: true,
                isDefault: form.configData.models.length === 0
            })
        })
    selectedFetchedModels.value = []
}

const addHeader = () => form.configData.headers.push({ name: '', value: '', enabled: true })
const removeHeader = index => form.configData.headers.splice(index, 1)
const addModel = () => form.configData.models.push({ id: '', name: '', alias: '', isEnabled: true, isDefault: form.configData.models.length === 0 })
const removeModel = index => form.configData.models.splice(index, 1)
const addKey = () => form.configData.keys.push({ id: `key_${Date.now()}`, value: '', proxyUrl: '', isEnabled: true, status: 'unknown' })
const removeKey = index => form.configData.keys.splice(index, 1)

const setDefaultModel = index => {
    form.configData.models.forEach((model, idx) => { model.isDefault = idx === index })
}

const providerInitial = item => (item.displayName || item.name || 'AI').slice(0, 1).toUpperCase()

onMounted(loadData)
</script>

<style scoped>
.ai-access-page {
    min-height: 100%;
}

.mini-stat {
    border: 1px solid transparent;
    border-radius: 10px;
    padding: 16px 18px;
}

.mini-stat__num {
    font-size: 26px;
    font-weight: 800;
    line-height: 1;
}

.mini-stat__label {
    margin-top: 6px;
    font-size: 12px;
    opacity: 0.72;
}

.mini-stat--blue { background: rgba(59, 130, 246, 0.10); border-color: rgba(59, 130, 246, 0.22); color: #2563eb; }
.mini-stat--green { background: rgba(16, 185, 129, 0.10); border-color: rgba(16, 185, 129, 0.22); color: #059669; }
.mini-stat--amber { background: rgba(245, 158, 11, 0.12); border-color: rgba(245, 158, 11, 0.24); color: #b45309; }
.mini-stat--cyan { background: rgba(6, 182, 212, 0.10); border-color: rgba(6, 182, 212, 0.22); color: #0891b2; }

.access-layout {
    display: grid;
    grid-template-columns: minmax(260px, 320px) minmax(0, 1fr);
    gap: 18px;
    align-items: start;
}

.provider-panel,
.editor-panel {
    border: 1px solid var(--admin-border);
    border-radius: 10px;
    background: var(--admin-bg-card);
    box-shadow: var(--admin-shadow);
}

.provider-panel {
    position: sticky;
    top: 78px;
    overflow: hidden;
}

.panel-toolbar {
    padding: 14px;
    border-bottom: 1px solid var(--admin-border);
}

.provider-list {
    display: flex;
    max-height: calc(100vh - 260px);
    min-height: 320px;
    flex-direction: column;
    gap: 8px;
    overflow-y: auto;
    padding: 12px;
}

.provider-item {
    display: grid;
    width: 100%;
    grid-template-columns: 36px minmax(0, 1fr) auto;
    align-items: center;
    gap: 10px;
    border: 1px solid transparent;
    border-radius: 8px;
    padding: 10px;
    background: transparent;
    text-align: left;
    transition: all 0.18s ease;
}

.provider-item:hover,
.provider-item.is-active {
    border-color: rgba(6, 182, 212, 0.28);
    background: rgba(6, 182, 212, 0.08);
}

.provider-item__avatar {
    display: flex;
    width: 36px;
    height: 36px;
    align-items: center;
    justify-content: center;
    border-radius: 8px;
    color: #0f766e;
    background: rgba(20, 184, 166, 0.14);
    font-weight: 800;
}

.provider-item__main {
    min-width: 0;
}

.provider-item__main strong,
.provider-item__main small {
    display: block;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.provider-item__main strong {
    color: var(--admin-text);
    font-size: 13px;
}

.provider-item__main small {
    margin-top: 3px;
    color: var(--admin-text-muted);
    font-size: 12px;
}

.editor-panel {
    overflow: hidden;
}

.editor-header,
.editor-footer,
.section-head,
.model-tools,
.fetched-box {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
}

.editor-header {
    padding: 18px 20px;
    border-bottom: 1px solid var(--admin-border);
}

.editor-header h2 {
    margin: 0;
    color: var(--admin-text);
    font-size: 18px;
    font-weight: 800;
}

.editor-header p {
    margin: 5px 0 0;
    color: var(--admin-text-muted);
    font-size: 13px;
}

.editor-actions {
    display: flex;
    gap: 10px;
}

.access-form {
    padding: 0 20px 18px;
}

.form-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 14px 16px;
    padding-top: 12px;
}

.form-grid__wide {
    grid-column: 1 / -1;
}

.section-head,
.model-tools,
.fetched-box {
    padding: 12px 0;
}

.section-head span {
    color: var(--admin-text);
    font-weight: 700;
}

.model-tools__path {
    max-width: 360px;
}

.fetched-box {
    justify-content: flex-start;
    border: 1px solid rgba(6, 182, 212, 0.18);
    border-radius: 8px;
    padding: 12px;
    background: rgba(6, 182, 212, 0.06);
}

.fetched-box .el-select {
    width: min(560px, 100%);
}

.rows {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.config-row {
    display: grid;
    align-items: center;
    gap: 10px;
    border: 1px solid var(--admin-border);
    border-radius: 8px;
    padding: 10px;
    background: var(--admin-bg-soft);
}

.config-row--header {
    grid-template-columns: auto minmax(160px, 0.8fr) minmax(200px, 1.2fr) auto;
}

.config-row--model {
    grid-template-columns: auto minmax(160px, 1fr) minmax(140px, 0.8fr) minmax(120px, 0.7fr) auto auto;
}

.config-row--key {
    grid-template-columns: auto minmax(220px, 1fr) minmax(220px, 1fr) auto auto;
}

.empty-panel {
    border: 1px dashed var(--admin-border);
    border-radius: 8px;
    padding: 20px;
    color: var(--admin-text-muted);
    text-align: center;
    font-size: 13px;
}

.editor-footer {
    border-top: 1px solid var(--admin-border);
    padding: 14px 20px;
}

.route-preview {
    display: flex;
    flex-direction: column;
    gap: 4px;
    color: var(--admin-text-muted);
    font-size: 12px;
}

.route-preview strong {
    color: var(--admin-text);
    font-family: 'SF Mono', 'Fira Code', monospace;
    font-size: 13px;
}

@media (max-width: 1080px) {
    .access-layout,
    .form-grid {
        grid-template-columns: 1fr;
    }

    .provider-panel {
        position: static;
    }

    .provider-list {
        max-height: 360px;
    }

    .config-row,
    .config-row--header,
    .config-row--model,
    .config-row--key {
        grid-template-columns: 1fr;
    }

    .editor-header,
    .editor-footer,
    .model-tools,
    .fetched-box {
        align-items: stretch;
        flex-direction: column;
    }

    .editor-actions {
        width: 100%;
    }
}
</style>

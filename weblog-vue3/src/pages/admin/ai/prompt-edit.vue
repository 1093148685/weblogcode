<template>
    <div class="page-shell prompt-edit-page">
        <div class="page-hero">
            <div class="page-hero__main">
                <el-button @click="$router.back()" circle plain>
                    <el-icon><ArrowLeft /></el-icon>
                </el-button>
                <div class="page-hero__icon">
                    <el-icon :size="18"><EditPen /></el-icon>
                </div>
                <div>
                    <h1 class="page-hero__title">{{ isNew ? '新建 Prompt 模板' : '编辑 Prompt 模板' }}</h1>
                    <p class="page-hero__desc">把角色、约束、输出格式和变量放在同一处管理，降低 AI 回答跑偏的概率。</p>
                </div>
            </div>
            <div class="page-hero__actions">
                <el-button @click="$router.back()">取消</el-button>
                <el-button type="primary" @click="handleSave" :icon="Check">保存模板</el-button>
            </div>
        </div>

        <div class="prompt-editor-grid">
            <div class="prompt-editor-main">
                <el-card shadow="never" class="ai-card">
                    <template #header>
                        <div class="card-section-title">
                            <el-icon class="text-indigo-500"><InfoFilled /></el-icon>
                            基础信息
                        </div>
                    </template>
                    <el-form :model="form" label-position="top">
                        <div class="prompt-form-grid">
                            <el-form-item label="模板名称" required>
                                <el-input v-model="form.name" placeholder="例如：博客问答助手" />
                            </el-form-item>
                            <el-form-item label="标识 Code" required>
                                <el-input v-model="form.code" placeholder="全局唯一，例如 blog_assistant" :disabled="!isNew" />
                            </el-form-item>
                        </div>
                        <div class="prompt-form-grid">
                            <el-form-item label="AI 角色">
                                <el-input v-model="form.role" placeholder="例如：你是专业技术写作助手" />
                            </el-form-item>
                            <el-form-item label="使用场景">
                                <el-select v-model="form.scene" placeholder="请选择场景" style="width: 100%">
                                    <el-option label="前台聊天" value="chat" />
                                    <el-option label="RAG 问答" value="rag" />
                                    <el-option label="写作助手" value="writing" />
                                    <el-option label="Agent 工具" value="agent" />
                                    <el-option label="通用模板" value="general" />
                                </el-select>
                            </el-form-item>
                        </div>
                        <el-form-item label="简短描述">
                            <el-input v-model="form.description" type="textarea" :rows="2" placeholder="这个模板解决什么问题，适合在哪些页面使用？" />
                        </el-form-item>
                    </el-form>
                </el-card>

                <el-card shadow="never" class="ai-card flex-1">
                    <template #header>
                        <div class="flex items-center justify-between gap-3 flex-wrap">
                            <div class="card-section-title">
                                <el-icon class="text-indigo-500"><Document /></el-icon>
                                System Prompt
                            </div>
                            <div class="prompt-toolbar">
                                <el-button size="small" plain @click="insertSnippet('role')">角色框架</el-button>
                                <el-button size="small" plain @click="insertSnippet('rag')">RAG 约束</el-button>
                                <el-button size="small" plain @click="insertSnippet('format')">输出格式</el-button>
                                <el-button size="small" plain @click="insertSnippet('safety')">安全边界</el-button>
                            </div>
                        </div>
                    </template>

                    <el-input
                        v-model="form.prompt"
                        type="textarea"
                        :rows="16"
                        placeholder="在这里输入 System Prompt。可以使用 {current_time}、{user_question}、{rag_context} 等变量。"
                        class="prompt-textarea"
                    />

                    <div class="prompt-variable-row">
                        <span>变量：</span>
                        <el-tag
                            v-for="v in extractedVariables"
                            :key="v"
                            size="small"
                            type="info"
                            class="cursor-pointer font-mono"
                            @click="addVariableToTest(v)"
                        >{{ '{' + v + '}' }}</el-tag>
                        <span v-if="extractedVariables.length === 0" class="prompt-muted">暂无变量，使用 {变量名} 可自动提取。</span>
                    </div>
                </el-card>
            </div>

            <div class="prompt-preview-side">
                <el-card shadow="never" class="ai-card prompt-side-card">
                    <template #header>
                        <div class="card-section-title">
                            <el-icon class="text-violet-500"><Operation /></el-icon>
                            调试与质检
                        </div>
                    </template>

                    <div class="quality-score">
                        <div>
                            <div class="quality-score__label">Prompt 完整度</div>
                            <div class="quality-score__desc">{{ qualitySummary }}</div>
                        </div>
                        <strong :class="qualityClass">{{ qualityScore }}%</strong>
                    </div>

                    <div class="quality-list">
                        <div v-for="item in qualityItems" :key="item.key" class="quality-item" :class="{ 'is-ok': item.ok }">
                            <el-icon><CircleCheckFilled v-if="item.ok" /><WarningFilled v-else /></el-icon>
                            <span>{{ item.label }}</span>
                        </div>
                    </div>

                    <el-divider />

                    <div class="variable-panel">
                        <div class="panel-label">变量模拟值</div>
                        <div class="variable-list">
                            <div v-for="v in extractedVariables" :key="v" class="variable-field">
                                <label>{{ '{' + v + '}' }}</label>
                                <el-input v-model="testVariables[v]" size="small" :placeholder="`输入 ${v} 的测试值`" />
                            </div>
                            <div v-if="extractedVariables.length === 0" class="empty-hint">
                                <el-icon :size="28"><Document /></el-icon>
                                <span>左侧加入变量后，可以在这里模拟替换效果。</span>
                            </div>
                        </div>
                    </div>

                    <el-divider />

                    <div class="preview-panel">
                        <div class="preview-panel__head">
                            <div class="panel-label">合成后的 Prompt</div>
                            <el-button size="small" text type="primary" @click="copyFinalPrompt" :icon="CopyDocument">复制</el-button>
                        </div>
                        <div class="preview-box">{{ finalPrompt || 'Prompt 为空' }}</div>
                    </div>
                </el-card>
            </div>
        </div>
    </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
    ArrowLeft,
    Check,
    CircleCheckFilled,
    CopyDocument,
    Document,
    EditPen,
    InfoFilled,
    Operation,
    WarningFilled
} from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'

const route = useRoute()
const router = useRouter()

const isNew = computed(() => route.params.id === 'new')
const form = ref({ name: '', code: '', role: '', scene: 'chat', description: '', prompt: '' })
const testVariables = ref({})

const snippets = {
    role: `你是一个专业、可靠、表达清晰的技术博客 AI 助手。
你需要优先理解用户真实意图，回答时保持准确、友好、可执行。`,
    rag: `当提供 {rag_context} 时：
1. 优先基于知识库内容回答；
2. 如果知识库只命中相关线索，可以先说明“根据现有资料推断”；
3. 如果知识库不足，但问题属于通用知识，可以继续用普通 AI 能力回答，并明确区分知识库信息与通用补充。`,
    format: `输出要求：
- 先给直接结论；
- 再给关键步骤或依据；
- 涉及代码时使用 Markdown 代码块；
- 不确定的信息要明确标注。`,
    safety: `边界要求：
- 不编造不存在的来源、接口或配置；
- 不泄露密钥、Token、连接串等敏感信息；
- 遇到高风险操作时先提示风险和回退方式。`
}

const extractedVariables = computed(() => {
    if (!form.value.prompt) return []
    const matches = [...form.value.prompt.matchAll(/\{([a-zA-Z0-9_]+)\}/g)]
    const vars = [...new Set(matches.map(match => match[1]))]
    for (const key in testVariables.value) {
        if (!vars.includes(key)) delete testVariables.value[key]
    }
    return vars
})

const finalPrompt = computed(() => {
    let result = form.value.prompt || ''
    for (const v of extractedVariables.value) {
        const val = testVariables.value[v] || `[${v}]`
        result = result.replace(new RegExp(`\\{${v}\\}`, 'g'), val)
    }
    if (form.value.role) {
        result = `[角色设定]\n${form.value.role}\n\n[系统指令]\n${result}`
    }
    return result
})

const qualityItems = computed(() => [
    { key: 'role', label: '包含清晰角色', ok: Boolean(form.value.role || /你是|角色|assistant/i.test(form.value.prompt)) },
    { key: 'goal', label: '说明任务目标', ok: /目标|任务|需要|回答|生成|分析/.test(form.value.prompt) },
    { key: 'format', label: '约定输出格式', ok: /格式|输出|Markdown|步骤|列表|代码块/.test(form.value.prompt) },
    { key: 'boundary', label: '包含边界或失败处理', ok: /不确定|不足|不要|禁止|风险|如果|无法|缺少/.test(form.value.prompt) },
    { key: 'variables', label: '变量可调试', ok: extractedVariables.value.length > 0 },
    { key: 'length', label: '内容长度适中', ok: form.value.prompt.length >= 80 && form.value.prompt.length <= 4000 }
])

const qualityScore = computed(() => Math.round((qualityItems.value.filter(item => item.ok).length / qualityItems.value.length) * 100))
const qualityClass = computed(() => qualityScore.value >= 80 ? 'is-good' : qualityScore.value >= 50 ? 'is-warn' : 'is-danger')
const qualitySummary = computed(() => {
    if (qualityScore.value >= 80) return '结构比较完整，可以进入页面联调。'
    if (qualityScore.value >= 50) return '已经有基础框架，建议补齐约束和输出格式。'
    return '模板还比较松散，建议先插入角色框架。'
})

const insertSnippet = key => {
    const snippet = snippets[key]
    if (!snippet) return
    form.value.prompt = [form.value.prompt?.trim(), snippet].filter(Boolean).join('\n\n')
}

const addVariableToTest = v => {
    if (!testVariables.value[v]) testVariables.value[v] = `测试数据_${v}`
}

const copyFinalPrompt = async () => {
    await navigator.clipboard.writeText(finalPrompt.value)
    ElMessage.success('已复制到剪贴板')
}

const handleSave = () => {
    if (!form.value.name || !form.value.code) {
        ElMessage.warning('请填写模板名称和标识 Code')
        return
    }
    if (qualityScore.value < 50) {
        ElMessage.warning('建议先补齐 Prompt 角色、目标和输出要求')
        return
    }
    ElMessage.success('模板已保存')
    setTimeout(() => router.push('/admin/ai/prompt-list'), 500)
}

onMounted(() => {
    if (!isNew.value) {
        form.value = {
            name: '博客问答助手',
            code: 'blog_assistant',
            role: '你是一个专业的技术博客问答助手。',
            scene: 'chat',
            description: '用于前台聊天和知识库问答的默认系统提示词。',
            prompt: `请根据用户问题 {user_question} 给出专业、友好的回答。

如果存在知识库内容 {rag_context}，优先使用知识库；如果知识库不足，但问题可以用通用知识回答，请继续回答并说明信息来源边界。

输出要求：
- 先给结论；
- 再给关键依据；
- 涉及代码时使用 Markdown 代码块。`
        }
    }
})
</script>

<style scoped>
.prompt-edit-page {
    min-height: 100%;
}

.prompt-editor-grid {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 420px;
    gap: 20px;
}

.prompt-editor-main,
.prompt-side-card {
    display: flex;
    min-width: 0;
    flex-direction: column;
    gap: 16px;
}

.prompt-form-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 16px;
}

.ai-card {
    border-radius: 14px !important;
}

.card-section-title {
    display: flex;
    align-items: center;
    gap: 6px;
    color: var(--admin-text);
    font-size: 13px;
    font-weight: 700;
}

.prompt-toolbar {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
}

:deep(.prompt-textarea textarea) {
    color: var(--text-body);
    font-family: 'SF Mono', 'Fira Code', 'Fira Mono', 'Roboto Mono', monospace !important;
    font-size: 13px !important;
    line-height: 1.7 !important;
}

.prompt-variable-row {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 8px;
    margin-top: 12px;
    color: var(--admin-text-muted);
    font-size: 12px;
}

.prompt-muted {
    color: var(--admin-text-muted);
}

.quality-score {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    padding: 14px;
    border: 1px solid var(--admin-border);
    border-radius: 12px;
    background: rgba(15, 23, 42, 0.38);
}

.quality-score__label {
    color: var(--admin-text);
    font-size: 14px;
    font-weight: 800;
}

.quality-score__desc {
    margin-top: 4px;
    color: var(--admin-text-muted);
    font-size: 12px;
}

.quality-score strong {
    font-size: 26px;
    font-weight: 900;
}

.quality-score strong.is-good {
    color: #34d399;
}

.quality-score strong.is-warn {
    color: #fbbf24;
}

.quality-score strong.is-danger {
    color: #fb7185;
}

.quality-list {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 8px;
}

.quality-item {
    display: flex;
    align-items: center;
    gap: 7px;
    padding: 9px 10px;
    border: 1px solid var(--admin-border);
    border-radius: 10px;
    color: var(--admin-text-muted);
    background: rgba(15, 23, 42, 0.28);
    font-size: 12px;
}

.quality-item.is-ok {
    color: #86efac;
    border-color: rgba(52, 211, 153, 0.24);
    background: rgba(16, 185, 129, 0.08);
}

.panel-label {
    margin-bottom: 10px;
    color: var(--admin-text-muted);
    font-size: 12px;
    font-weight: 800;
    text-transform: uppercase;
}

.variable-list {
    display: flex;
    max-height: 220px;
    flex-direction: column;
    gap: 10px;
    overflow-y: auto;
    padding-right: 2px;
}

.variable-field label {
    display: inline-block;
    margin-bottom: 5px;
    color: var(--admin-text-muted);
    font-family: 'SF Mono', 'Fira Code', monospace;
    font-size: 12px;
}

.empty-hint {
    display: flex;
    align-items: center;
    flex-direction: column;
    gap: 8px;
    padding: 28px 12px;
    color: var(--admin-text-muted);
    text-align: center;
    font-size: 13px;
}

.preview-panel {
    min-height: 0;
}

.preview-panel__head {
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.preview-box {
    min-height: 220px;
    max-height: 360px;
    overflow-y: auto;
    padding: 14px;
    border: 1px solid var(--admin-border);
    border-radius: 12px;
    color: #dbeafe;
    background:
        radial-gradient(circle at top right, rgba(99, 102, 241, 0.18), transparent 36%),
        #07111f;
    font-family: 'SF Mono', 'Fira Code', monospace;
    font-size: 12px;
    line-height: 1.7;
    white-space: pre-wrap;
}

@media (max-width: 1180px) {
    .prompt-editor-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 768px) {
    .prompt-form-grid,
    .quality-list {
        grid-template-columns: 1fr;
    }

    .page-hero__actions {
        width: 100%;
        justify-content: flex-start;
    }
}
</style>

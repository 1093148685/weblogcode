<template>
    <div class="prompt-edit-page p-5 min-h-screen">

        <!-- 页头 -->
        <div class="flex items-center justify-between mb-5">
            <div class="flex items-center gap-3">
                <el-button @click="$router.back()" circle plain>
                    <el-icon><ArrowLeft /></el-icon>
                </el-button>
                <div class="page-header__icon">
                    <el-icon :size="18"><EditPen /></el-icon>
                </div>
                <div>
                    <h1 class="text-lg font-bold text-slate-800">{{ isNew ? '新建 Prompt 模板' : '编辑 Prompt 模板' }}</h1>
                    <p class="text-sm text-slate-400 mt-0.5">设置 AI 的角色和背景知识，支持动态变量注入</p>
                </div>
            </div>
            <div class="flex gap-2">
                <el-button @click="$router.back()">取消</el-button>
                <el-button type="primary" @click="handleSave" :icon="Check">保存模板</el-button>
            </div>
        </div>

        <!-- 主体双栏 -->
        <div class="flex gap-5 flex-1 min-h-0">

            <!-- 左侧：编辑区 -->
            <div class="flex-1 flex flex-col gap-4 min-w-0">

                <!-- 基础信息卡片 -->
                <el-card shadow="never" class="ai-card">
                    <template #header>
                        <div class="card-section-title">
                            <el-icon class="text-indigo-500"><InfoFilled /></el-icon>
                            基础信息
                        </div>
                    </template>
                    <el-form :model="form" label-position="top">
                        <div class="grid grid-cols-2 gap-4">
                            <el-form-item label="模板名称" required>
                                <el-input v-model="form.name" placeholder="例如：博客问答助手" />
                            </el-form-item>
                            <el-form-item label="标识 (Code)" required>
                                <el-input v-model="form.code" placeholder="全局唯一，如：blog_assistant" :disabled="!isNew" />
                            </el-form-item>
                        </div>
                        <el-form-item label="AI 角色设定">
                            <el-input v-model="form.role" placeholder="例如：你是一个专业的技术专家和博客助手..." />
                        </el-form-item>
                        <el-form-item label="简短描述">
                            <el-input v-model="form.description" type="textarea" :rows="2" placeholder="这个模板的用途是什么？" />
                        </el-form-item>
                    </el-form>
                </el-card>

                <!-- Prompt 编辑卡片 -->
                <el-card shadow="never" class="ai-card flex-1">
                    <template #header>
                        <div class="flex items-center justify-between">
                            <div class="card-section-title">
                                <el-icon class="text-indigo-500"><Document /></el-icon>
                                System Prompt（系统提示词）
                            </div>
                            <el-tooltip content="支持使用 {var_name} 语法注入动态变量，变量会在右侧调试面板自动提取" placement="top">
                                <el-tag size="small" type="info" class="cursor-help">
                                    <el-icon class="mr-1"><QuestionFilled /></el-icon>
                                    支持变量
                                </el-tag>
                            </el-tooltip>
                        </div>
                    </template>

                    <el-input
                        v-model="form.prompt"
                        type="textarea"
                        :rows="14"
                        placeholder="在此输入 System Prompt...

支持动态变量，例如：
当前时间：{current_time}
文章内容：{article_content}
作者信息：{author_info}"
                        class="prompt-textarea"
                    />

                    <!-- 已提取变量 -->
                    <div class="mt-3 flex flex-wrap items-center gap-2">
                        <span class="text-xs text-slate-400">已提取变量：</span>
                        <el-tag
                            v-for="v in extractedVariables"
                            :key="v"
                            size="small"
                            type="info"
                            class="cursor-pointer font-mono"
                            @click="addVariableToTest(v)"
                        >{{ '{' + v + '}' }}</el-tag>
                        <span v-if="extractedVariables.length === 0" class="text-xs text-slate-300">无（使用 {变量名} 语法添加）</span>
                    </div>
                </el-card>
            </div>

            <!-- 右侧：调试面板 -->
            <div class="w-[400px] flex-shrink-0">
                <el-card shadow="never" class="ai-card h-full flex flex-col">
                    <template #header>
                        <div class="card-section-title">
                            <el-icon class="text-violet-500"><Operation /></el-icon>
                            模板调试面板
                        </div>
                    </template>

                    <div class="flex flex-col gap-4 h-full">
                        <!-- 变量输入区 -->
                        <div>
                            <div class="text-xs font-semibold text-slate-500 mb-3 uppercase tracking-wide">变量模拟值</div>
                            <div class="space-y-3 max-h-[240px] overflow-y-auto pr-1">
                                <div v-for="v in extractedVariables" :key="v" class="flex flex-col gap-1">
                                    <label class="text-xs text-slate-500 font-mono bg-slate-100 px-2 py-0.5 rounded w-fit">{{ '{' + v + '}' }}</label>
                                    <el-input v-model="testVariables[v]" size="small" :placeholder="`输入 ${v} 的测试值`" />
                                </div>
                                <div v-if="extractedVariables.length === 0" class="text-center py-8 text-slate-300 text-sm">
                                    <el-icon :size="28" class="mb-2 block mx-auto"><Document /></el-icon>
                                    在左侧 Prompt 中<br>输入 {变量名} 即可在此测试
                                </div>
                            </div>
                        </div>

                        <el-divider class="my-0" />

                        <!-- 最终预览区 -->
                        <div class="flex-1 flex flex-col min-h-0">
                            <div class="flex items-center justify-between mb-2">
                                <div class="text-xs font-semibold text-slate-500 uppercase tracking-wide">合成后的 Prompt</div>
                                <el-button size="small" text type="primary" @click="copyFinalPrompt" :icon="CopyDocument">复制</el-button>
                            </div>
                            <div class="preview-box flex-1 font-mono text-xs text-slate-300 whitespace-pre-wrap overflow-y-auto">{{ finalPrompt || '（Prompt 为空）' }}</div>
                        </div>
                    </div>
                </el-card>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, QuestionFilled, EditPen, Check, Document, InfoFilled, Operation, CopyDocument } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'

const route = useRoute()
const router = useRouter()

const isNew = computed(() => route.params.id === 'new')

const form = ref({ name: '', code: '', role: '', description: '', prompt: '' })
const testVariables = ref({})

const extractedVariables = computed(() => {
    if (!form.value.prompt) return []
    const regex = /\{([a-zA-Z0-9_]+)\}/g
    const matches = [...form.value.prompt.matchAll(regex)]
    const vars = [...new Set(matches.map(m => m[1]))]
    for (const key in testVariables.value) {
        if (!vars.includes(key)) delete testVariables.value[key]
    }
    return vars
})

const finalPrompt = computed(() => {
    let result = form.value.prompt
    if (!result) return ''
    for (const v of extractedVariables.value) {
        const val = testVariables.value[v] || `[${v}]`
        result = result.replace(new RegExp(`\\{${v}\\}`, 'g'), val)
    }
    if (form.value.role) {
        result = `[角色设定]\n${form.value.role}\n\n[指令]\n${result}`
    }
    return result
})

const addVariableToTest = (v) => {
    if (!testVariables.value[v]) testVariables.value[v] = `测试数据_${v}`
}

const copyFinalPrompt = () => {
    navigator.clipboard.writeText(finalPrompt.value)
    ElMessage.success('已复制到剪贴板')
}

const handleSave = () => {
    if (!form.value.name || !form.value.code) {
        ElMessage.warning('请填写模板名称和标识')
        return
    }
    ElMessage.success('保存成功')
    setTimeout(() => { router.push('/admin/ai/prompt-list') }, 500)
}

onMounted(() => {
    if (!isNew.value && route.params.id === '1') {
        form.value = {
            name: '博客问答助手 (默认)', code: 'blog_assistant',
            role: '你是一个专业的技术专家和博客助手。',
            description: '用于前台聊天面板的默认系统提示词',
            prompt: '请基于以下用户的提问，提供专业、友好的回答。\n如果用户询问当前时间，告诉他：{current_time}\n如果用户询问关于博客作者的信息，请参考：{author_info}'
        }
    }
})
</script>

<style scoped>
.prompt-edit-page { background: var(--admin-bg-page); min-height: 100%; }

.page-header__icon {
    width: 34px; height: 34px; border-radius: 10px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    display: flex; align-items: center; justify-content: center;
    color: white; flex-shrink: 0;
}

.ai-card { border-radius: 14px !important; }

.card-section-title {
    display: flex;
    align-items: center;
    gap: 6px;
    font-size: 13px;
    font-weight: 600;
    color: var(--text-heading);
}

/* Prompt 文本域等宽字体 */
:deep(.prompt-textarea textarea) {
    font-family: 'SF Mono', 'Fira Code', 'Fira Mono', 'Roboto Mono', monospace !important;
    font-size: 13px !important;
    line-height: 1.65 !important;
    color: var(--text-body);
}

/* 右侧预览区 */
.preview-box {
    background: #0f172a;
    border-radius: 10px;
    padding: 14px;
    min-height: 200px;
    line-height: 1.65;
}
</style>

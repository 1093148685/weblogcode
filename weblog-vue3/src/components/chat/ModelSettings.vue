<template>
    <el-dialog
        v-model="visible"
        title="AI 模型设置"
        width="460px"
        :close-on-click-modal="true"
        class="model-settings-dialog"
    >
        <div class="space-y-5">
            <!-- 模型选择 -->
            <div>
                <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400 mb-3">选择 AI 模型</h3>
                <div class="max-h-80 overflow-y-auto space-y-1 pr-1">
                    <template v-for="(group, provider) in groupedModels" :key="provider">
                        <div class="text-xs text-gray-400 uppercase tracking-wider mt-3 mb-1.5 first:mt-0">{{ provider }}</div>
                        <div
                            v-for="model in group"
                            :key="model.id"
                            @click="selectModel(model.id)"
                            :class="[
                                'p-2.5 rounded-lg border cursor-pointer transition-all duration-200',
                                selectedModelId === model.id
                                    ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
                                    : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'
                            ]"
                        >
                            <div class="flex items-center justify-between">
                                <div>
                                    <h4 class="font-medium text-sm text-gray-800 dark:text-gray-200">{{ model.name }}</h4>
                                    <p class="text-xs text-gray-400 dark:text-gray-500 mt-0.5 font-mono">{{ model.id }}</p>
                                </div>
                                <el-icon
                                    v-if="selectedModelId === model.id"
                                    class="text-blue-500"
                                >
                                    <Check />
                                </el-icon>
                            </div>
                        </div>
                    </template>
                </div>
            </div>

            <!-- 分割线 -->
            <div class="border-t border-gray-200 dark:border-gray-700"></div>

            <!-- 温度设置 -->
            <div>
                <div class="flex items-center justify-between mb-2">
                    <h3 class="text-sm font-semibold text-gray-500 dark:text-gray-400">温度参数</h3>
                    <el-tag size="small" :type="localTemperature <= 0.3 ? 'success' : localTemperature <= 1.0 ? 'warning' : 'danger'">
                        {{ localTemperature.toFixed(1) }}
                    </el-tag>
                </div>
                <el-slider
                    v-model="localTemperature"
                    :min="0"
                    :max="2"
                    :step="0.1"
                    :show-tooltip="false"
                />
                <div class="flex justify-between text-xs text-gray-400 mt-1">
                    <span>精确</span>
                    <span>均衡</span>
                    <span>创意</span>
                </div>
            </div>
        </div>

        <template #footer>
            <span class="dialog-footer">
                <el-button @click="visible = false">取消</el-button>
                <el-button type="primary" @click="saveSettings">保存</el-button>
            </span>
        </template>
    </el-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { Check } from '@element-plus/icons-vue'

const props = defineProps({
    modelValue: {
        type: Boolean,
        default: false
    },
    modelOptions: {
        type: Array,
        default: () => [
            { id: 'deepseek-chat', name: 'DeepSeek V3', provider: 'deepseek' },
            { id: 'deepseek-reasoner', name: 'DeepSeek R1', provider: 'deepseek' },
            { id: 'gpt-4o-mini', name: 'GPT-4o Mini', provider: 'openai' },
            { id: 'gpt-4o', name: 'GPT-4o', provider: 'openai' },
            { id: 'claude-sonnet-4-20250514', name: 'Claude Sonnet 4', provider: 'claude' },
            { id: 'claude-3-5-sonnet-20241022', name: 'Claude 3.5 Sonnet', provider: 'claude' },
            { id: 'gemini-2.0-flash', name: 'Gemini 2.0 Flash', provider: 'gemini' },
            { id: 'glm-4-flash', name: 'GLM-4 Flash', provider: 'zhipu' }
        ]
    }
})

const emit = defineEmits(['update:modelValue', 'change'])

const visible = computed({
    get: () => props.modelValue,
    set: (val) => emit('update:modelValue', val)
})

const selectedModelId = ref(props.modelOptions[0]?.id || 'deepseek-chat')
const localTemperature = ref(0.7)

// 按 provider 分组展示
const groupedModels = computed(() => {
    const groups = {}
    for (const model of props.modelOptions) {
        const provider = model.provider || 'other'
        if (!groups[provider]) groups[provider] = []
        groups[provider].push(model)
    }
    return groups
})

// 当 modelOptions 变化时重置选中状态（比如从服务器加载了新列表）
watch(() => props.modelOptions, (newOpts) => {
    if (newOpts && newOpts.length > 0 && !newOpts.find(m => m.id === selectedModelId.value)) {
        selectedModelId.value = newOpts[0].id
    }
}, { deep: true })

const selectModel = (modelId) => {
    selectedModelId.value = modelId
}

const saveSettings = () => {
    emit('change', {
        model: selectedModelId.value,
        temperature: localTemperature.value
    })
    visible.value = false
}
</script>

<style scoped>
.model-settings-dialog :deep(.el-dialog__header) {
    padding: 16px 20px;
    border-bottom: 1px solid var(--el-border-color-lighter);
}

.model-settings-dialog :deep(.el-dialog__title) {
    font-weight: 600;
    color: var(--el-text-color-primary);
}

.model-settings-dialog :deep(.el-dialog__body) {
    padding: 20px;
}

.model-settings-dialog :deep(.el-dialog__footer) {
    padding: 12px 20px;
    border-top: 1px solid var(--el-border-color-lighter);
}
</style>

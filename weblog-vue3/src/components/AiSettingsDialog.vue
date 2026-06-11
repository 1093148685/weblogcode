<template>
    <el-dialog 
        v-model="dialogVisible" 
        title="AI 模型设置" 
        width="420px"
        class="ai-settings-dialog"
        :close-on-click-modal="true"
    >
        <div class="settings-form">
            <el-form label-position="top">
                <el-form-item label="服务类型">
                    <el-select 
                        v-model="settings.serviceType" 
                        placeholder="请选择服务类型" 
                        @change="onServiceTypeChange"
                        class="full-width"
                    >
                        <el-option 
                            v-for="type in serviceTypes" 
                            :key="type" 
                            :label="getServiceTypeLabel(type)" 
                            :value="type" 
                        />
                    </el-select>
                </el-form-item>
                
                <el-form-item label="模型名称">
                    <el-select 
                        v-model="settings.modelName" 
                        placeholder="请选择模型" 
                        filterable
                        allow-create
                        default-first-option
                        class="full-width"
                        :disabled="!settings.serviceType"
                    >
                        <el-option 
                            v-for="model in filteredModels" 
                            :key="model.model" 
                            :label="model.model" 
                            :value="model.model" 
                        />
                    </el-select>
                </el-form-item>

                <div v-if="currentModelInfo" class="model-info">
                    <div class="info-item">
                        <span class="info-label">服务商：</span>
                        <span class="info-value">{{ currentModelInfo.name }}</span>
                    </div>
                    <div class="info-item">
                        <span class="info-label">接口地址：</span>
                        <span class="info-value">{{ currentModelInfo.apiUrl }}</span>
                    </div>
                </div>
            </el-form>
        </div>
        
        <template #footer>
            <div class="dialog-footer">
                <el-button @click="dialogVisible = false">取消</el-button>
                <el-button type="primary" @click="saveSettings">确定</el-button>
            </div>
        </template>
    </el-dialog>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getEnabledAiModels } from '@/api/admin/aiModel'

const props = defineProps({
    modelValue: Boolean
})

const emit = defineEmits(['update:modelValue', 'change'])

const dialogVisible = computed({
    get: () => props.modelValue,
    set: (val) => emit('update:modelValue', val)
})

const STORAGE_KEY = 'ai_model_settings'

const serviceTypes = ref([])
const modelList = ref([])
const settings = ref({
    serviceType: '',
    modelName: ''
})

const filteredModels = computed(() => {
    if (!settings.value.serviceType) return []
    return modelList.value.filter(m => 
        m.type.toLowerCase() === settings.value.serviceType.toLowerCase()
    )
})

const currentModelInfo = computed(() => {
    if (!settings.value.modelName || !settings.value.serviceType) return null
    return filteredModels.value.find(m => m.model === settings.value.modelName)
})

const getServiceTypeLabel = (type) => {
    const labels = {
        'openai': 'OpenAI',
        'deepseek': 'DeepSeek',
        'qwen': '通义千问',
        'zhipu': '智谱AI',
        'claude': 'Claude',
        'azure': 'Azure OpenAI',
        'minimax': 'MiniMax',
        'gemini': 'Google Gemini',
        'qianfan': '百度千帆',
        'other': '其他'
    }
    return labels[type.toLowerCase()] || type
}

const onServiceTypeChange = () => {
    settings.value.modelName = ''
}

const loadModels = async () => {
    try {
        const res = await getEnabledAiModels()
        if (res.success) {
            // 按类型分组，去重
            const typeSet = new Set()
            modelList.value = res.data.filter(m => {
                typeSet.add(m.type)
                return m.isEnabled
            })
            serviceTypes.value = Array.from(typeSet)
        }
    } catch (e) {
        console.error('加载模型列表失败:', e)
    }
}

const saveSettings = () => {
    if (!settings.value.serviceType) {
        ElMessage.warning('请选择服务类型')
        return
    }
    if (!settings.value.modelName) {
        ElMessage.warning('请选择或输入模型名称')
        return
    }
    localStorage.setItem(STORAGE_KEY, JSON.stringify(settings.value))
    emit('change', settings.value)
    dialogVisible.value = false
    ElMessage.success('设置已保存')
}

const loadSavedSettings = () => {
    try {
        const saved = localStorage.getItem(STORAGE_KEY)
        if (saved) {
            settings.value = JSON.parse(saved)
        }
    } catch (e) {
        console.error('加载设置失败:', e)
    }
}

watch(dialogVisible, (val) => {
    if (val) {
        loadSavedSettings()
    }
})

onMounted(() => {
    loadModels()
    loadSavedSettings()
})

defineExpose({
    loadModels
})
</script>

<style scoped>
.settings-form {
    padding: 8px 0;
}

.full-width {
    width: 100%;
}

.model-info {
    background: #f8f9fa;
    border-radius: 8px;
    padding: 12px 16px;
    margin-top: 8px;
}

.info-item {
    display: flex;
    font-size: 13px;
    line-height: 1.8;
}

.info-label {
    color: #909399;
    flex-shrink: 0;
}

.info-value {
    color: #606266;
    word-break: break-all;
}

.dialog-footer {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
}
</style>

<style>
.ai-settings-dialog .el-dialog__header {
    border-bottom: 1px solid #eee;
    padding: 16px 20px;
    margin: 0;
}

.ai-settings-dialog .el-dialog__title {
    font-weight: 600;
    color: #333;
}

.ai-settings-dialog .el-dialog__body {
    padding: 20px;
}

.ai-settings-dialog .el-dialog__footer {
    padding: 12px 20px;
    border-top: 1px solid #eee;
}
</style>
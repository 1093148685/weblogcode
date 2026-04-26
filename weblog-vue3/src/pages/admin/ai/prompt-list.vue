<template>
    <div class="page-shell prompt-page">

        <!-- 页头 -->
        <div class="page-hero">
            <div class="page-hero__main">
                <div class="page-hero__icon">
                    <el-icon :size="20"><Document /></el-icon>
                </div>
                <div>
                    <h1 class="page-hero__title">Prompt 模板管理</h1>
                    <p class="page-hero__desc">管理系统、插件和 Agent 的提示词资产</p>
                </div>
            </div>
            <div class="page-hero__actions">
                <el-button :icon="Plus" type="primary" @click="$router.push('/admin/ai/prompt/edit/new')">
                    新建模板
                </el-button>
            </div>
        </div>

        <div class="page-stats">
            <div class="mini-stat mini-stat--blue">
                <div class="mini-stat__num">{{ tableData.length }}</div>
                <div class="mini-stat__label">模板总数</div>
            </div>
            <div class="mini-stat mini-stat--green">
                <div class="mini-stat__num">{{ systemPromptCount }}</div>
                <div class="mini-stat__label">系统模板</div>
            </div>
            <div class="mini-stat mini-stat--violet">
                <div class="mini-stat__num">{{ variablePromptCount }}</div>
                <div class="mini-stat__label">变量模板</div>
            </div>
        </div>

        <div class="section-toolbar">
            <el-input
                v-model="keyword"
                :prefix-icon="Search"
                clearable
                class="!w-64"
                placeholder="搜索名称、标识或描述"
            />
            <el-select v-model="roleFilter" clearable class="!w-40" placeholder="全部角色">
                <el-option v-for="role in roleOptions" :key="role" :label="role" :value="role" />
            </el-select>
            <span class="section-toolbar__meta">共 {{ filteredPrompts.length }} 条</span>
        </div>

        <!-- 主卡片 -->
        <el-card shadow="never" class="ai-card admin-table-panel">
            <template #header>
                <div class="flex items-center justify-between">
                    <div class="flex items-center gap-2">
                        <span class="card-title">模板列表</span>
                        <el-tag size="small">{{ filteredPrompts.length }} 条</el-tag>
                    </div>
                    <el-button type="primary" :icon="Plus" @click="$router.push('/admin/ai/prompt/edit/new')">
                        新建模板
                    </el-button>
                </div>
            </template>

            <el-table :data="filteredPrompts" v-loading="loading" style="width: 100%" empty-text="暂无 Prompt 模板">
                <el-table-column prop="name" label="模板名称" min-width="180">
                    <template #default="{ row }">
                        <div class="flex items-center gap-2">
                            <div class="prompt-avatar">P</div>
                            <span class="prompt-name">{{ row.name }}</span>
                        </div>
                    </template>
                </el-table-column>
                <el-table-column prop="code" label="标识 (Code)" min-width="160">
                    <template #default="{ row }">
                        <el-tag size="small" type="info" class="font-mono">{{ row.code }}</el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="role" label="AI 角色" min-width="120">
                    <template #default="{ row }">
                        <el-tag size="small">{{ row.role || '助手' }}</el-tag>
                    </template>
                </el-table-column>
                <el-table-column label="变量" width="90" align="center">
                    <template #default="{ row }">
                        <el-tag size="small" :type="getVariableCount(row) > 0 ? 'success' : 'info'" effect="plain">
                            {{ getVariableCount(row) }}
                        </el-tag>
                    </template>
                </el-table-column>
                <el-table-column prop="description" label="描述" min-width="220" show-overflow-tooltip>
                    <template #default="{ row }">
                        <span class="prompt-desc">{{ row.description }}</span>
                    </template>
                </el-table-column>
                <el-table-column prop="updateTime" label="更新时间" width="180">
                    <template #default="{ row }">
                        <span class="text-xs text-slate-400">{{ row.updateTime }}</span>
                    </template>
                </el-table-column>
                <el-table-column label="操作" width="160" fixed="right">
                    <template #default="{ row }">
                        <el-button link type="primary" @click="$router.push(`/admin/ai/prompt/edit/${row.id}`)">编辑</el-button>
                        <el-button link type="danger" @click="handleDelete(row)">删除</el-button>
                    </template>
                </el-table-column>
            </el-table>

            <div class="mt-4 flex justify-end">
                <el-pagination background layout="prev, pager, next" :total="filteredPrompts.length" :page-size="10" />
            </div>
        </el-card>
    </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { Plus, Document, Search } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const tableData = ref([
    { id: 1, name: '博客问答助手 (默认)', code: 'blog_assistant', role: '技术专家', description: '用于前台聊天面板的默认系统提示词', updateTime: '2024-03-01 12:00:00' },
    { id: 2, name: '文章摘要生成', code: 'article_summary', role: '文章编辑', description: '用于在发布文章时自动生成简短摘要', updateTime: '2024-03-01 12:05:00' }
])
const keyword = ref('')
const roleFilter = ref('')

const systemPromptCount = computed(() => tableData.value.filter(item => item.code?.includes('assistant')).length)
const variablePromptCount = computed(() => tableData.value.filter(item => /\{[a-zA-Z0-9_]+\}/.test(item.prompt || item.description || '')).length)
const roleOptions = computed(() => [...new Set(tableData.value.map(item => item.role || '助手'))])
const filteredPrompts = computed(() => {
    const kw = keyword.value.trim().toLowerCase()
    return tableData.value.filter(item => {
        const matchesKeyword = !kw ||
            item.name?.toLowerCase().includes(kw) ||
            item.code?.toLowerCase().includes(kw) ||
            item.description?.toLowerCase().includes(kw)
        const matchesRole = !roleFilter.value || (item.role || '助手') === roleFilter.value
        return matchesKeyword && matchesRole
    })
})

const getVariableCount = (row) => {
    const text = `${row.prompt || ''}\n${row.description || ''}`
    return new Set([...text.matchAll(/\{([a-zA-Z0-9_]+)\}/g)].map(match => match[1])).size
}

const handleDelete = (row) => {
    ElMessageBox.confirm(`确定要删除模板 "${row.name}" 吗？`, '提示', { type: 'warning' }).then(() => {
        tableData.value = tableData.value.filter(item => item.id !== row.id)
        ElMessage.success('删除成功')
    }).catch(() => {})
}
</script>

<style scoped>
.prompt-page { min-height: 100%; }

.card-title { font-size: 14px; font-weight: 700; color: var(--admin-text); }

.prompt-avatar {
    width: 28px; height: 28px; border-radius: 7px;
    background: linear-gradient(135deg, #8b5cf6, #a78bfa);
    color: white; font-size: 13px; font-weight: 700;
    display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}

.prompt-name {
    color: var(--admin-text);
    font-size: 13px;
    font-weight: 700;
}

.prompt-desc {
    color: var(--admin-text-muted);
    font-size: 13px;
}

@media (max-width: 768px) {
    .section-toolbar {
        align-items: stretch;
        flex-direction: column;
    }

    .section-toolbar :deep(.el-input) {
        width: 100% !important;
    }
}
</style>

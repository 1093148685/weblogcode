<template>
    <div class="ai-page p-5">

        <!-- 页头 -->
        <div class="page-header mb-5">
            <div class="flex items-center justify-between">
                <div class="flex items-center gap-3">
                    <div class="page-header__icon">
                        <el-icon :size="20"><Document /></el-icon>
                    </div>
                    <div>
                        <h1 class="text-lg font-bold text-slate-800">Prompt 模板管理</h1>
                        <p class="text-sm text-slate-400 mt-0.5">管理系统和 Agent 的各种 AI 提示词</p>
                    </div>
                </div>
                <el-button type="primary" :icon="Plus" @click="$router.push('/admin/ai/prompt/edit/new')">
                    新建模板
                </el-button>
            </div>
        </div>

        <!-- 主卡片 -->
        <el-card shadow="never" class="ai-card">
            <template #header>
                <div class="flex items-center gap-2">
                    <span class="card-title">模板列表</span>
                    <el-tag size="small">{{ tableData.length }} 条</el-tag>
                </div>
            </template>

            <el-table :data="tableData" v-loading="loading" style="width: 100%">
                <el-table-column prop="name" label="模板名称" min-width="180">
                    <template #default="{ row }">
                        <div class="flex items-center gap-2">
                            <div class="prompt-avatar">P</div>
                            <span class="font-medium text-slate-700 text-sm">{{ row.name }}</span>
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
                <el-table-column prop="description" label="描述" min-width="220" show-overflow-tooltip>
                    <template #default="{ row }">
                        <span class="text-sm text-slate-500">{{ row.description }}</span>
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
                <el-pagination background layout="prev, pager, next" :total="tableData.length" :page-size="10" />
            </div>
        </el-card>
    </div>
</template>

<script setup>
import { ref } from 'vue'
import { Plus, Document } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'

const loading = ref(false)
const tableData = ref([
    { id: 1, name: '博客问答助手 (默认)', code: 'blog_assistant', role: '技术专家', description: '用于前台聊天面板的默认系统提示词', updateTime: '2024-03-01 12:00:00' },
    { id: 2, name: '文章摘要生成', code: 'article_summary', role: '文章编辑', description: '用于在发布文章时自动生成简短摘要', updateTime: '2024-03-01 12:05:00' }
])

const handleDelete = (row) => {
    ElMessageBox.confirm(`确定要删除模板 "${row.name}" 吗？`, '提示', { type: 'warning' }).then(() => {
        tableData.value = tableData.value.filter(item => item.id !== row.id)
        ElMessage.success('删除成功')
    }).catch(() => {})
}
</script>

<style scoped>
.ai-page { min-height: 100%; }

.page-header__icon {
    width: 36px; height: 36px; border-radius: 10px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    display: flex; align-items: center; justify-content: center;
    color: white; flex-shrink: 0;
}

.ai-card { border-radius: 14px !important; }
.card-title { font-size: 14px; font-weight: 600; color: var(--text-heading); }

.prompt-avatar {
    width: 28px; height: 28px; border-radius: 7px;
    background: linear-gradient(135deg, #8b5cf6, #a78bfa);
    color: white; font-size: 13px; font-weight: 700;
    display: flex; align-items: center; justify-content: center; flex-shrink: 0;
}
</style>

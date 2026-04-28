<template>
    <div class="page-shell min-h-full">

        <!-- 页头 -->
        <div class="page-hero">
            <div class="page-hero__main">
                <div class="page-hero__icon">
                    <el-icon :size="20"><Grid /></el-icon>
                </div>
                <div>
                    <h1 class="page-hero__title">AI 插件市场</h1>
                    <p class="page-hero__desc">管理并配置博客的 AI 能力插件</p>
                </div>
            </div>
            <div class="page-hero__actions">
                <el-button :icon="Refresh" @click="loadData" :loading="loading" plain>刷新</el-button>
            </div>
        </div>

        <!-- 统计卡片 -->
        <div class="page-stats">
            <div class="mini-stat mini-stat--blue">
                <div class="mini-stat__num">{{ tableData.length }}</div>
                <div class="mini-stat__label">已安装插件</div>
            </div>
            <div class="mini-stat mini-stat--green">
                <div class="mini-stat__num">{{ enabledCount }}</div>
                <div class="mini-stat__label">已启用插件</div>
            </div>
            <div class="mini-stat mini-stat--violet">
                <div class="mini-stat__num">{{ enabledProviders.length }}</div>
                <div class="mini-stat__label">可用 Provider</div>
            </div>
            <div class="mini-stat mini-stat--cyan">
                <div class="mini-stat__num">{{ readyPluginCount }}</div>
                <div class="mini-stat__label">依赖就绪</div>
            </div>
        </div>

        <!-- ── 搜索 & 筛选栏 ── -->
        <div class="section-toolbar">
            <el-input
                v-model="searchKeyword"
                placeholder="搜索插件名称或描述…"
                clearable
                :prefix-icon="Search"
                class="!w-56"
            />
            <el-select v-model="filterCategory" placeholder="全部分类" clearable class="!w-36">
                <el-option label="全部分类" value="" />
                <el-option label="对话助手" value="assistant" />
                <el-option label="内容处理" value="content" />
                <el-option label="通用" value="general" />
            </el-select>
            <el-select v-model="filterStatus" placeholder="全部状态" clearable class="!w-32">
                <el-option label="全部状态" value="" />
                <el-option label="已启用" value="enabled" />
                <el-option label="已停用" value="disabled" />
            </el-select>
            <span class="section-toolbar__meta">共 {{ filteredPlugins.length }} 个插件</span>
        </div>

        <!-- ── 插件卡片区域 ── -->
        <div v-loading="loading" class="plugin-market-grid grid grid-cols-1 gap-4" :class="filteredPlugins.length > 1 ? 'md:grid-cols-2 xl:grid-cols-3' : ''">
            <div
                v-for="plugin in filteredPlugins"
                :key="plugin.pluginId"
                class="plugin-market-card"
                @click="openDrawer(plugin)"
            >
                <!-- 卡片顶部色条 -->
                <div class="plugin-market-card__bar" :class="getCategoryColor(plugin.category)"></div>

                <div class="p-5">
                    <!-- 插件头部信息 -->
                    <div class="flex items-start justify-between mb-3">
                        <div class="flex items-center gap-3">
                            <div class="w-11 h-11 rounded-xl flex items-center justify-center"
                                 :class="getCategoryBgClass(plugin.category)">
                                <el-icon :size="22" :class="getCategoryIconClass(plugin.category)">
                                    <component :is="getCategoryIcon(plugin.category)" />
                                </el-icon>
                            </div>
                            <div class="min-w-0">
                                <div class="plugin-market-card__title">{{ plugin.name }}</div>
                                <div class="plugin-market-card__id">{{ plugin.pluginId }}</div>
                            </div>
                        </div>
                        <!-- 启用开关 -->
                        <el-switch
                            v-model="plugin.isEnabled"
                            size="small"
                            @change="handleToggle(plugin)"
                            @click.stop
                        />
                    </div>

                    <!-- 描述 -->
                    <p class="plugin-market-card__desc line-clamp-2">
                        {{ plugin.description }}
                    </p>

                    <!-- 标签行 -->
                    <div class="flex items-center flex-wrap gap-1.5 mb-4">
                        <el-tag size="small" :type="getCategoryTagType(plugin.category)" effect="plain">
                            {{ getCategoryName(plugin.category) }}
                        </el-tag>
                        <el-tag
                            v-for="p in plugin.requiredProviders.slice(0, 3)"
                            :key="p"
                            size="small"
                            type="info"
                            effect="plain"
                        >
                            {{ p }}
                        </el-tag>
                        <el-tag v-if="plugin.requiredProviders.length > 3" size="small" type="info" effect="plain">
                            +{{ plugin.requiredProviders.length - 3 }}
                        </el-tag>
                    </div>

                    <!-- 底部状态栏 -->
                    <div class="plugin-market-card__footer">
                        <div class="flex items-center gap-2">
                            <span class="w-2 h-2 rounded-full" :class="plugin.isEnabled ? 'bg-green-500' : 'bg-gray-300'"></span>
                            <span class="text-xs" :class="plugin.isEnabled ? 'text-emerald-400' : 'text-gray-400'">
                                {{ plugin.isEnabled ? '运行中' : '已停用' }}
                            </span>
                            <el-tag size="small" :type="pluginReady(plugin) ? 'success' : 'warning'" effect="plain">
                                {{ pluginReady(plugin) ? '依赖就绪' : `缺少 ${missingProviders(plugin).length}` }}
                            </el-tag>
                            <span v-if="plugin.version" class="text-xs text-gray-300 dark:text-gray-600 font-mono">v{{ plugin.version }}</span>
                        </div>
                        <el-button text size="small" class="!text-blue-500 !p-0" @click.stop="openDrawer(plugin)">
                            配置 <el-icon class="ml-0.5"><ArrowRight /></el-icon>
                        </el-button>
                    </div>
                </div>
            </div>

            <!-- 空状态 -->
            <el-empty
                v-if="!loading && filteredPlugins.length === 0"
                :description="tableData.length === 0 ? '暂无插件' : '没有符合条件的插件'"
                class="col-span-full py-16"
            />
        </div>

        <!-- ══════════════ 插件详情抽屉 ══════════════ -->
        <el-drawer
            v-model="drawerVisible"
            :title="currentPlugin?.name || '插件详情'"
            direction="rtl"
            size="520px"
            destroy-on-close
        >
            <template #header>
                <div class="flex items-center gap-3">
                    <div class="w-9 h-9 rounded-lg flex items-center justify-center"
                         :class="getCategoryBgClass(currentPlugin?.category)">
                        <el-icon :size="18" :class="getCategoryIconClass(currentPlugin?.category)">
                            <component :is="getCategoryIcon(currentPlugin?.category)" />
                        </el-icon>
                    </div>
                    <div>
                        <div class="font-semibold text-gray-800 dark:text-gray-100">{{ currentPlugin?.name }}</div>
                        <div class="flex items-center gap-2 mt-0.5">
                            <span class="text-xs text-gray-400 font-mono">{{ currentPlugin?.pluginId }}</span>
                            <span v-if="currentPlugin?.version" class="text-xs text-gray-300 dark:text-gray-600 font-mono">v{{ currentPlugin.version }}</span>
                        </div>
                    </div>
                </div>
            </template>

            <div v-if="currentPlugin" class="px-1">

                <!-- ── 插件状态 ── -->
                <div class="plugin-drawer-status">
                    <div>
                        <div class="text-sm font-medium text-gray-700 dark:text-gray-300">插件状态</div>
                        <div class="text-xs text-gray-500 mt-0.5">控制该插件是否对外提供服务</div>
                    </div>
                    <el-switch v-model="drawerForm.isEnabled" @change="handleToggleInDrawer" />
                </div>

                <!-- ── 分类标签区 ── -->
                <div class="flex flex-wrap gap-2 mb-5">
                    <el-tag :type="getCategoryTagType(currentPlugin.category)" effect="light" size="default">
                        {{ getCategoryName(currentPlugin.category) }}
                    </el-tag>
                    <el-tag
                        v-for="p in currentPlugin.requiredProviders"
                        :key="p"
                        type="info"
                        effect="plain"
                        size="default"
                    >{{ p }}</el-tag>
                </div>

                <!-- ── 标签页 ── -->
                <el-tabs v-model="drawerTab" class="plugin-drawer-tabs">

                    <!-- 【配置】标签 -->
                    <el-tab-pane label="插件配置" name="config">
                        <el-form label-position="top" class="mt-2">

                            <!-- 模型选择（所有插件通用） -->
                            <el-form-item label="使用模型">
                                <el-select v-model="drawerForm.model" class="w-full" clearable placeholder="默认（使用系统默认模型）">
                                    <el-option
                                        v-for="m in availableModels"
                                        :key="m.id"
                                        :label="`${m.name} (${m.provider})`"
                                        :value="m.id"
                                    />
                                </el-select>
                                <div class="text-xs text-gray-400 mt-1">为该插件指定模型，留空则使用系统默认模型</div>
                            </el-form-item>

                            <!-- chat_assistant 专属配置 -->
                            <template v-if="currentPlugin.pluginId === 'chat_assistant'">
                                <el-form-item label="系统提示词">
                                    <el-input
                                        v-model="drawerForm.systemPrompt"
                                        type="textarea"
                                        :rows="4"
                                        placeholder="设置 AI 的角色定位、行为约束和回答风格，留空则使用默认行为"
                                    />
                                    <div class="text-xs text-gray-400 mt-1">该提示词会在每次对话开始时注入</div>
                                </el-form-item>

                                <el-form-item label="每日使用限制（次/人）">
                                    <div class="flex items-center gap-3 w-full">
                                        <el-input-number
                                            v-model="drawerForm.dailyLimit"
                                            :min="1" :max="500"
                                            class="!w-36"
                                        />
                                        <span class="text-sm text-gray-500">管理员不受限制</span>
                                    </div>
                                </el-form-item>

                                <el-form-item>
                                    <template #label>
                                        <div class="flex items-center justify-between w-full">
                                            <span>温度参数 (Temperature)</span>
                                            <el-tag size="small" :type="getTempType(drawerForm.temperature)">
                                                {{ drawerForm.temperature.toFixed(1) }}
                                            </el-tag>
                                        </div>
                                    </template>
                                    <el-slider
                                        v-model="drawerForm.temperature"
                                        :min="0" :max="2" :step="0.1"
                                        :marks="{ 0: '严谨', 1: '均衡', 2: '创意' }"
                                        class="!mx-2"
                                    />
                                    <div class="text-xs text-gray-400 mt-4">
                                        值越低输出越稳定确定，值越高越有创意但可能不准确
                                    </div>
                                </el-form-item>

                                <el-form-item label="最大回复长度（Token）">
                                    <div class="flex items-center gap-3">
                                        <el-input-number
                                            v-model="drawerForm.maxTokens"
                                            :min="256" :max="32000" :step="256"
                                            class="!w-40"
                                        />
                                        <span class="text-sm text-gray-500">≈ {{ Math.round(drawerForm.maxTokens * 0.75) }} 汉字</span>
                                    </div>
                                </el-form-item>

                                <el-divider content-position="left">智能路由</el-divider>
                                <el-form-item label="允许自动联网">
                                    <el-switch v-model="drawerForm.allowAutoWebSearch" />
                                    <div class="text-xs text-gray-400 mt-1">智能模式下，只有实时信息、版本、天气、热点等问题会自动走联网。</div>
                                </el-form-item>
                                <el-form-item label="优先知识库">
                                    <el-switch v-model="drawerForm.preferKnowledgeBase" />
                                    <div class="text-xs text-gray-400 mt-1">当问题明显要求“根据博客/知识库/文章”回答时，优先检索站内资料。</div>
                                </el-form-item>

                                <el-divider content-position="left">联网搜索</el-divider>
                                <el-form-item label="Tavily API Key">
                                    <el-input
                                        v-model="drawerForm.tavilyApiKey"
                                        type="password"
                                        show-password
                                        clearable
                                        placeholder="留空则使用后端配置或免费兜底"
                                    />
                                    <div class="text-xs text-gray-400 mt-1">配置后优先使用 Tavily；适合热点新闻、趋势、政策、价格等实时问题。</div>
                                </el-form-item>
                                <el-form-item label="搜索结果数量">
                                    <el-input-number v-model="drawerForm.webSearchTopK" :min="1" :max="8" class="!w-40" />
                                </el-form-item>
                                <el-form-item label="免费搜索兜底">
                                    <el-switch v-model="drawerForm.enableFreeSearchFallback" />
                                </el-form-item>
                            </template>

                            <!-- article_summary 专属配置 -->
                            <template v-else-if="currentPlugin.pluginId === 'article_summary'">
                                <el-form-item label="最大处理字数">
                                    <el-input-number v-model="drawerForm.maxContentLength" :min="1000" :max="50000" :step="1000" class="!w-40" />
                                    <div class="text-xs text-gray-400 mt-1">超出部分将被截断，避免超出 Token 限制</div>
                                </el-form-item>
                                <el-form-item label="摘要长度范围">
                                    <el-select v-model="drawerForm.summaryLength" class="!w-40">
                                        <el-option label="简短 (100-150字)" value="100-150" />
                                        <el-option label="标准 (200-300字)" value="200-300" />
                                        <el-option label="详细 (400-500字)" value="400-500" />
                                    </el-select>
                                </el-form-item>
                            </template>

                            <!-- 通用 JSON 配置（兜底） -->
                            <template v-else>
                                <el-form-item label="配置（JSON）">
                                    <el-input
                                        v-model="drawerForm.rawConfig"
                                        type="textarea"
                                        :rows="8"
                                        placeholder='{}'
                                        class="font-mono text-sm"
                                    />
                                    <div class="text-xs text-gray-400 mt-1">直接编辑插件的 JSON 配置，请确保格式正确</div>
                                </el-form-item>
                            </template>

                        </el-form>
                    </el-tab-pane>

                    <!-- 【使用统计】标签 -->
                    <el-tab-pane name="stats">
                        <template #label>
                            <span>使用统计</span>
                            <el-badge v-if="usageStats.length" :value="usageStats.length" class="ml-1" />
                        </template>
                        <div v-loading="statsLoading" class="mt-2">
                            <!-- 概览数字 -->
                            <div class="grid grid-cols-3 gap-3 mb-4">
                                <div class="bg-blue-50 dark:bg-blue-900/20 rounded-xl p-3 text-center">
                                    <div class="text-xl font-bold text-blue-600">{{ usageStats.length }}</div>
                                    <div class="text-xs text-gray-500 mt-0.5">使用用户</div>
                                </div>
                                <div class="bg-green-50 dark:bg-green-900/20 rounded-xl p-3 text-center">
                                    <div class="text-xl font-bold text-green-600">{{ todayTotal }}</div>
                                    <div class="text-xs text-gray-500 mt-0.5">今日次数</div>
                                </div>
                                <div class="bg-purple-50 dark:bg-purple-900/20 rounded-xl p-3 text-center">
                                    <div class="text-xl font-bold text-purple-600">{{ allTimeTotal }}</div>
                                    <div class="text-xs text-gray-500 mt-0.5">累计次数</div>
                                </div>
                            </div>
                            <!-- 用户列表 -->
                            <el-table :data="usageStats" size="small" stripe max-height="320">
                                <el-table-column label="用户" min-width="120">
                                    <template #default="{ row }">
                                        <span class="font-mono text-xs text-gray-600 dark:text-gray-300">
                                            {{ row.userId ? (row.userId.length > 16 ? row.userId.slice(0, 16) + '…' : row.userId) : '匿名用户' }}
                                        </span>
                                    </template>
                                </el-table-column>
                                <el-table-column label="今日" width="70" align="center">
                                    <template #default="{ row }">
                                        <el-tag type="success" size="small" effect="plain">{{ row.usedCount }}</el-tag>
                                    </template>
                                </el-table-column>
                                <el-table-column label="累计" width="70" align="center">
                                    <template #default="{ row }">
                                        <el-tag type="info" size="small" effect="plain">{{ row.totalCount }}</el-tag>
                                    </template>
                                </el-table-column>
                                <el-table-column prop="lastUsedTime" label="最后使用" min-width="100" show-overflow-tooltip />
                            </el-table>
                            <el-empty v-if="!statsLoading && usageStats.length === 0" description="暂无使用记录" :image-size="60" />
                        </div>
                    </el-tab-pane>

                    <!-- 【测试运行】标签 -->
                    <el-tab-pane label="测试" name="test">
                        <div class="mt-2 space-y-4">
                            <el-alert type="info" :closable="false" show-icon>
                                <template #title>点击下方按钮对插件执行一次测试调用，验证当前 Provider 是否正常工作</template>
                            </el-alert>

                            <el-button
                                type="primary"
                                :loading="testing"
                                @click="runTest"
                                class="w-full"
                                :icon="VideoPlay"
                            >
                                {{ testing ? '测试中…' : '立即测试' }}
                            </el-button>

                            <transition name="el-fade-in">
                                <div v-if="testResult !== null">
                                    <el-alert
                                        :type="testResult.success ? 'success' : 'error'"
                                        :title="testResult.success ? '测试通过' : '测试失败'"
                                        show-icon
                                        :closable="false"
                                        class="mb-2"
                                    />
                                    <div v-if="testResult.message" class="bg-gray-100 dark:bg-gray-700 rounded-lg p-3 text-sm text-gray-700 dark:text-gray-300 font-mono whitespace-pre-wrap">
                                        {{ testResult.message }}
                                    </div>
                                </div>
                            </transition>
                        </div>
                    </el-tab-pane>

                    <!-- 【关于】标签 -->
                    <el-tab-pane label="关于" name="about">
                        <div class="mt-2 space-y-4">
                            <!-- 插件描述 -->
                            <div class="bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4">
                                <div class="text-xs text-gray-400 mb-1">插件描述</div>
                                <p class="text-sm text-gray-700 dark:text-gray-300 leading-relaxed">{{ currentPlugin?.description }}</p>
                            </div>

                            <!-- 基本信息表格 -->
                            <div class="bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-xl overflow-hidden">
                                <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-700">
                                    <span class="text-xs text-gray-400">插件 ID</span>
                                    <span class="text-xs font-mono text-gray-600 dark:text-gray-300">{{ currentPlugin?.pluginId }}</span>
                                </div>
                                <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-700">
                                    <span class="text-xs text-gray-400">版本</span>
                                    <el-tag size="small" type="info" effect="plain" class="font-mono">v{{ currentPlugin?.version || '1.0.0' }}</el-tag>
                                </div>
                                <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-700">
                                    <span class="text-xs text-gray-400">作者</span>
                                    <span class="text-xs text-gray-600 dark:text-gray-300">{{ currentPlugin?.author || 'Weblog' }}</span>
                                </div>
                                <div class="flex items-center justify-between px-4 py-3">
                                    <span class="text-xs text-gray-400">分类</span>
                                    <el-tag size="small" :type="getCategoryTagType(currentPlugin?.category)" effect="light">
                                        {{ getCategoryName(currentPlugin?.category) }}
                                    </el-tag>
                                </div>
                            </div>

                            <!-- 依赖 Provider -->
                            <div>
                                <div class="text-xs text-gray-400 mb-2">支持的 Provider</div>
                                <div class="flex flex-wrap gap-2">
                                    <el-tag
                                        v-for="p in currentPlugin?.requiredProviders"
                                        :key="p"
                                        size="small"
                                        :type="isProviderEnabled(p) ? 'success' : 'info'"
                                        effect="plain"
                                    >
                                        <span class="flex items-center gap-1">
                                            <span class="w-1.5 h-1.5 rounded-full" :class="isProviderEnabled(p) ? 'bg-green-500' : 'bg-gray-400'"></span>
                                            {{ p }}
                                        </span>
                                    </el-tag>
                                </div>
                                <p class="text-xs text-gray-400 mt-2">
                                    {{ pluginReady(currentPlugin) ? '依赖已满足，可以直接测试运行' : `还缺少：${missingProviders(currentPlugin).join('、')}` }}
                                </p>
                            </div>
                        </div>
                    </el-tab-pane>

                </el-tabs>
            </div>

            <!-- 抽屉底部操作 -->
            <template #footer>
                <div class="dialog-footer-actions">
                    <el-button @click="drawerVisible = false">取消</el-button>
                    <el-button type="primary" @click="saveDrawerConfig" :loading="saving" :icon="Check">
                        保存配置
                    </el-button>
                </div>
            </template>
        </el-drawer>

    </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed, watch } from 'vue'
import {
    Refresh, Cpu, CircleCheck, DataBoard, ArrowRight,
    VideoPlay, Check, Search, Grid
} from '@element-plus/icons-vue'
import { getAiPlugins, updateAiPlugin, toggleAiPlugin, testAiPlugin } from '@/api/admin/ai-plugin'
import { getEnabledAiProviders } from '@/api/admin/ai-provider'
import { getAiUsageStats, getAiPluginSettings, updateAiPluginSettings } from '@/api/admin/ai-assistant'
import { getAvailableModels } from '@/api/admin/agent'
import { showMessage } from '@/composables/util'
import { ElMessage } from 'element-plus'

defineOptions({ name: 'AdminAiPlugin' })

// ──────── 状态 ────────
const loading = ref(false)
const tableData = ref([])
const enabledProviders = ref([])
const availableModels = ref([])

// 搜索 & 筛选
const searchKeyword = ref('')
const filterCategory = ref('')
const filterStatus = ref('')

// 抽屉
const drawerVisible = ref(false)
const drawerTab = ref('config')
const currentPlugin = ref(null)
const drawerForm = reactive({
    isEnabled: true,
    model: '', // 插件使用的模型
    // chat_assistant
    systemPrompt: '',
    dailyLimit: 10,
    temperature: 0.7,
    maxTokens: 4096,
    allowAutoWebSearch: true,
    preferKnowledgeBase: true,
    tavilyApiKey: '',
    webSearchTopK: 5,
    enableFreeSearchFallback: true,
    // article_summary
    maxContentLength: 8000,
    summaryLength: '200-300',
    // raw JSON fallback
    rawConfig: '{}'
})
const saving = ref(false)

// 统计
const statsLoading = ref(false)
const usageStats = ref([])

// 测试
const testing = ref(false)
const testResult = ref(null)

// ──────── 计算属性 ────────
const enabledCount = computed(() => tableData.value.filter(p => p.isEnabled).length)
const readyPluginCount = computed(() => tableData.value.filter(plugin => pluginReady(plugin)).length)
const todayTotal = computed(() => usageStats.value.reduce((s, u) => s + (u.usedCount || 0), 0))
const allTimeTotal = computed(() => usageStats.value.reduce((s, u) => s + (u.totalCount || 0), 0))

const filteredPlugins = computed(() => {
    let list = tableData.value
    const kw = searchKeyword.value.trim().toLowerCase()
    if (kw) {
        list = list.filter(p =>
            p.name?.toLowerCase().includes(kw) ||
            p.description?.toLowerCase().includes(kw) ||
            p.pluginId?.toLowerCase().includes(kw)
        )
    }
    if (filterCategory.value) {
        list = list.filter(p => p.category === filterCategory.value)
    }
    if (filterStatus.value === 'enabled') {
        list = list.filter(p => p.isEnabled)
    } else if (filterStatus.value === 'disabled') {
        list = list.filter(p => !p.isEnabled)
    }
    return list
})

// Provider 是否已启用（用于"关于"Tab 的依赖状态展示）
const isProviderEnabled = (providerName) => {
    return enabledProviders.value.some(p =>
        (p.name || p.providerName || p)?.toLowerCase() === providerName.toLowerCase()
    )
}

const missingProviders = (plugin) => {
    const providers = plugin?.requiredProviders || []
    if (providers.length === 0) return []
    return providers.filter(provider => !isProviderEnabled(provider))
}

const pluginReady = (plugin) => {
    if (!plugin) return false
    return missingProviders(plugin).length === 0
}

// ──────── 分类映射 ────────
const categoryMeta = {
    assistant: {
        name: '对话助手', color: 'bg-blue-500', bg: 'bg-blue-100 dark:bg-blue-900/40',
        icon: 'ChatDotRound', iconClass: 'text-blue-500', tagType: 'primary'
    },
    content: {
        name: '内容处理', color: 'bg-amber-500', bg: 'bg-amber-100 dark:bg-amber-900/40',
        icon: 'Document', iconClass: 'text-amber-500', tagType: 'warning'
    },
    general: {
        name: '通用', color: 'bg-gray-400', bg: 'bg-gray-100 dark:bg-gray-700',
        icon: 'Grid', iconClass: 'text-gray-500', tagType: 'info'
    }
}

const meta = (category) => categoryMeta[category] || categoryMeta.general
const getCategoryColor = (c) => meta(c).color
const getCategoryBgClass = (c) => meta(c).bg
const getCategoryIcon = (c) => meta(c).icon
const getCategoryIconClass = (c) => meta(c).iconClass
const getCategoryName = (c) => meta(c).name
const getCategoryTagType = (c) => meta(c).tagType
const getTempType = (t) => t <= 0.3 ? 'success' : t <= 1.0 ? 'warning' : 'danger'

// ──────── 数据加载 ────────
const loadData = async () => {
    loading.value = true
    try {
        const [pluginRes, providerRes, modelRes] = await Promise.all([
            getAiPlugins(),
            getEnabledAiProviders(),
            getAvailableModels()
        ])
        if (pluginRes.success) tableData.value = pluginRes.data || []
        if (providerRes.success) enabledProviders.value = providerRes.data || []
        if (modelRes.code === 200 && modelRes.data) availableModels.value = modelRes.data
    } catch (e) {
        showMessage('加载失败', 'error')
    } finally {
        loading.value = false
    }
}

// ──────── 抽屉操作 ────────
const openDrawer = (plugin) => {
    currentPlugin.value = plugin
    drawerTab.value = 'config'
    testResult.value = null

    // 重置表单
    drawerForm.isEnabled = plugin.isEnabled

    // 解析已保存的 config
    let saved = {}
    try { saved = JSON.parse(plugin.config || '{}') } catch { /**/ }

    // 所有插件都可以选择模型
    drawerForm.model = saved.model || ''

    if (plugin.pluginId === 'chat_assistant') {
        drawerForm.systemPrompt = saved.systemPrompt || ''
        drawerForm.dailyLimit = saved.dailyLimit ?? 10
        drawerForm.temperature = saved.temperature ?? 0.7
        drawerForm.maxTokens = saved.maxTokens ?? 4096
        drawerForm.allowAutoWebSearch = saved.allowAutoWebSearch ?? true
        drawerForm.preferKnowledgeBase = saved.preferKnowledgeBase ?? true
        drawerForm.tavilyApiKey = saved.tavilyApiKey || ''
        drawerForm.webSearchTopK = saved.webSearchTopK ?? 5
        drawerForm.enableFreeSearchFallback = saved.enableFreeSearchFallback ?? true
    } else if (plugin.pluginId === 'article_summary') {
        drawerForm.maxContentLength = saved.maxContentLength ?? 8000
        drawerForm.summaryLength = saved.summaryLength || '200-300'
    } else {
        drawerForm.rawConfig = plugin.config || '{}'
    }

    drawerVisible.value = true

    // chat_assistant 顺带加载使用统计
    if (plugin.pluginId === 'chat_assistant') loadStats()
}

const loadStats = async () => {
    statsLoading.value = true
    try {
        const res = await getAiUsageStats()
        if (res.success) usageStats.value = res.data || []
    } catch { /**/ } finally {
        statsLoading.value = false
    }
}

const buildConfigJson = () => {
    if (!currentPlugin.value) return '{}'
    const id = currentPlugin.value.pluginId
    const modelField = drawerForm.model ? { model: drawerForm.model } : {}
    if (id === 'chat_assistant') {
        return JSON.stringify({
            ...modelField,
            systemPrompt: drawerForm.systemPrompt,
            dailyLimit: drawerForm.dailyLimit,
            temperature: drawerForm.temperature,
            maxTokens: drawerForm.maxTokens,
            allowAutoWebSearch: drawerForm.allowAutoWebSearch,
            preferKnowledgeBase: drawerForm.preferKnowledgeBase,
            tavilyApiKey: drawerForm.tavilyApiKey,
            webSearchTopK: drawerForm.webSearchTopK,
            enableFreeSearchFallback: drawerForm.enableFreeSearchFallback
        })
    }
    if (id === 'article_summary') {
        return JSON.stringify({
            ...modelField,
            maxContentLength: drawerForm.maxContentLength,
            summaryLength: drawerForm.summaryLength
        })
    }
    // 兜底：尝试合并 model 到 rawConfig
    try {
        const raw = JSON.parse(drawerForm.rawConfig || '{}')
        if (drawerForm.model) raw.model = drawerForm.model
        return JSON.stringify(raw)
    } catch {
        return drawerForm.rawConfig
    }
}

const saveDrawerConfig = async () => {
    saving.value = true
    try {
        const configJson = buildConfigJson()
        const res = await updateAiPlugin(currentPlugin.value.pluginId, {
            isEnabled: drawerForm.isEnabled,
            config: configJson
        })
        if (res.success) {
            ElMessage.success('配置已保存')
            // 同步到列表
            const idx = tableData.value.findIndex(p => p.pluginId === currentPlugin.value.pluginId)
            if (idx !== -1) {
                tableData.value[idx].isEnabled = drawerForm.isEnabled
                tableData.value[idx].config = configJson
            }
            // chat_assistant 同步到 ai-assistant 设置接口（dailyLimit 等）
            if (currentPlugin.value.pluginId === 'chat_assistant') {
                await updateAiPluginSettings({
                    enabled: drawerForm.isEnabled,
                    dailyLimit: drawerForm.dailyLimit,
                    systemPrompt: drawerForm.systemPrompt,
                    temperature: drawerForm.temperature,
                    maxTokens: drawerForm.maxTokens
                }).catch(() => { /**/ })
            }
            drawerVisible.value = false
        } else {
            ElMessage.error(res.message || '保存失败')
        }
    } catch (e) {
        ElMessage.error('保存失败')
    } finally {
        saving.value = false
    }
}

// ──────── 快捷启用/禁用（卡片 Switch） ────────
const handleToggle = async (row) => {
    try {
        const res = await toggleAiPlugin(row.pluginId, row.isEnabled)
        if (!res.success) {
            row.isEnabled = !row.isEnabled
            ElMessage.error(res.message || '操作失败')
        } else {
            ElMessage.success(row.isEnabled ? '已启用' : '已停用')
        }
    } catch {
        row.isEnabled = !row.isEnabled
        ElMessage.error('操作失败')
    }
}

// 抽屉内启用开关
const handleToggleInDrawer = async () => {
    if (!currentPlugin.value) return
    const row = tableData.value.find(p => p.pluginId === currentPlugin.value.pluginId)
    if (row) {
        row.isEnabled = drawerForm.isEnabled
        await handleToggle(row)
        row.isEnabled = drawerForm.isEnabled  // 保持抽屉和列表一致
    }
}

// ──────── 测试 ────────
const runTest = async () => {
    testing.value = true
    testResult.value = null
    try {
        const res = await testAiPlugin(currentPlugin.value.pluginId)
        testResult.value = {
            success: res.success,
            message: res.success ? '插件调用成功，Provider 工作正常！' : (res.message || '测试失败，请检查 Provider 配置')
        }
    } catch (e) {
        testResult.value = { success: false, message: e.message || '网络异常' }
    } finally {
        testing.value = false
    }
}

onMounted(loadData)
</script>

<style scoped>
/* 让标签切换区域贴合抽屉边距 */
.plugin-drawer-tabs :deep(.el-tabs__header) {
    margin-bottom: 0;
}
.plugin-drawer-tabs :deep(.el-tabs__content) {
    padding-top: 12px;
}

.plugin-market-grid {
    align-items: stretch;
}

.plugin-market-card {
    position: relative;
    overflow: hidden;
    min-height: 212px;
    cursor: pointer;
    border-radius: 16px;
    background:
        radial-gradient(circle at top right, rgba(34, 211, 238, 0.12), transparent 38%),
        var(--admin-bg-card);
    border: 1px solid var(--admin-border);
    box-shadow: var(--admin-shadow-soft);
    transition: transform 0.22s ease, border-color 0.22s ease, box-shadow 0.22s ease;
}

.plugin-market-card::after {
    content: '';
    position: absolute;
    inset: 1px;
    border-radius: 15px;
    pointer-events: none;
    background: linear-gradient(135deg, rgba(255,255,255,0.08), transparent 45%);
}

.plugin-market-card:hover {
    transform: translateY(-3px);
    border-color: rgba(96, 165, 250, 0.45);
    box-shadow: var(--admin-shadow);
}

.plugin-market-card__bar {
    height: 3px;
    opacity: 0.95;
}

.plugin-market-card__title {
    max-width: 180px;
    overflow: hidden;
    color: var(--admin-text);
    font-size: 14px;
    font-weight: 700;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.plugin-market-card__id {
    margin-top: 2px;
    color: var(--admin-text-faint);
    font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
    font-size: 11px;
}

.plugin-market-card__desc {
    min-height: 44px;
    margin-bottom: 16px;
    color: var(--admin-text-muted);
    font-size: 13px;
    line-height: 1.7;
}

.plugin-market-card__footer {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding-top: 12px;
    border-top: 1px solid var(--admin-border);
}

.plugin-drawer-status {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 20px;
    padding: 16px;
    border-radius: 16px;
    background: var(--admin-bg-soft);
    border: 1px solid var(--admin-border);
}

/* 卡片悬浮阴影 */
.hover\:shadow-md:hover {
    box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
}

/* 渐变过渡 */
.el-fade-in-enter-active,
.el-fade-in-leave-active {
    transition: opacity 0.25s;
}
.el-fade-in-enter-from,
.el-fade-in-leave-to {
    opacity: 0;
}

/* 页头图标 */
.page-header__icon {
    width: 36px; height: 36px; border-radius: 10px;
    background: linear-gradient(135deg, #6366f1, #8b5cf6);
    display: flex; align-items: center; justify-content: center;
    color: white; flex-shrink: 0;
}

/* 迷你统计卡 */
.mini-stat {
    border-radius: 12px; padding: 16px 20px;
    display: flex; flex-direction: column; gap: 4px;
    border: 1px solid transparent;
}
.mini-stat__num  { font-size: 28px; font-weight: 700; line-height: 1; }
.mini-stat__label { font-size: 12px; opacity: 0.75; }
.mini-stat--blue   { background: linear-gradient(135deg,#eef2ff,#e0e7ff); color:#4338ca; border-color:#c7d2fe; }
.mini-stat--green  { background: linear-gradient(135deg,#f0fdf4,#dcfce7); color:#16a34a; border-color:#bbf7d0; }
.mini-stat--violet { background: linear-gradient(135deg,#fdf4ff,#f3e8ff); color:#9333ea; border-color:#e9d5ff; }
.mini-stat--cyan   { background: linear-gradient(135deg,#ecfeff,#cffafe); color:#0891b2; border-color:#a5f3fc; }

:global(html.dark) .mini-stat--cyan {
    background: rgba(8, 145, 178, 0.15) !important;
    border-color: rgba(34, 211, 238, 0.28) !important;
    color: #67e8f9 !important;
}
</style>

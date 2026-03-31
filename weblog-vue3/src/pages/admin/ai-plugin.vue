<template>
    <div class="p-6 min-h-full bg-gray-50 dark:bg-gray-900">

        <!-- ── 顶部标题栏 ── -->
        <div class="flex items-center justify-between mb-6">
            <div>
                <h1 class="text-xl font-bold text-gray-800 dark:text-gray-100">AI 插件市场</h1>
                <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">管理并配置博客的 AI 能力插件</p>
            </div>
            <el-button :icon="Refresh" @click="loadData" :loading="loading" circle />
        </div>

        <!-- ── 统计概览 ── -->
        <el-row :gutter="16" class="mb-6">
            <el-col :xs="24" :sm="8">
                <div class="bg-white dark:bg-gray-800 rounded-xl p-4 border border-gray-200 dark:border-gray-700 flex items-center gap-4">
                    <div class="w-10 h-10 rounded-lg bg-blue-100 dark:bg-blue-900/40 flex items-center justify-center">
                        <el-icon :size="20" class="text-blue-500"><Cpu /></el-icon>
                    </div>
                    <div>
                        <div class="text-2xl font-bold text-gray-800 dark:text-gray-100">{{ tableData.length }}</div>
                        <div class="text-xs text-gray-500">已安装插件</div>
                    </div>
                </div>
            </el-col>
            <el-col :xs="24" :sm="8">
                <div class="bg-white dark:bg-gray-800 rounded-xl p-4 border border-gray-200 dark:border-gray-700 flex items-center gap-4">
                    <div class="w-10 h-10 rounded-lg bg-green-100 dark:bg-green-900/40 flex items-center justify-center">
                        <el-icon :size="20" class="text-green-500"><CircleCheck /></el-icon>
                    </div>
                    <div>
                        <div class="text-2xl font-bold text-gray-800 dark:text-gray-100">{{ enabledCount }}</div>
                        <div class="text-xs text-gray-500">已启用插件</div>
                    </div>
                </div>
            </el-col>
            <el-col :xs="24" :sm="8">
                <div class="bg-white dark:bg-gray-800 rounded-xl p-4 border border-gray-200 dark:border-gray-700 flex items-center gap-4">
                    <div class="w-10 h-10 rounded-lg bg-purple-100 dark:bg-purple-900/40 flex items-center justify-center">
                        <el-icon :size="20" class="text-purple-500"><DataBoard /></el-icon>
                    </div>
                    <div>
                        <div class="text-2xl font-bold text-gray-800 dark:text-gray-100">{{ enabledProviders.length }}</div>
                        <div class="text-xs text-gray-500">可用 Provider</div>
                    </div>
                </div>
            </el-col>
        </el-row>

        <!-- ── 搜索 & 筛选栏 ── -->
        <div class="flex flex-wrap items-center gap-3 mb-5">
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
                <el-option label="编辑辅助" value="editor" />
                <el-option label="通用" value="general" />
            </el-select>
            <el-select v-model="filterStatus" placeholder="全部状态" clearable class="!w-32">
                <el-option label="全部状态" value="" />
                <el-option label="已启用" value="enabled" />
                <el-option label="已停用" value="disabled" />
            </el-select>
            <span class="text-sm text-gray-400 ml-auto">共 {{ filteredPlugins.length }} 个插件</span>
        </div>

        <!-- ── 插件卡片区域 ── -->
        <div v-loading="loading" class="grid grid-cols-1 gap-4" :class="filteredPlugins.length > 1 ? 'md:grid-cols-2 xl:grid-cols-3' : ''">
            <div
                v-for="plugin in filteredPlugins"
                :key="plugin.pluginId"
                class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden
                       hover:shadow-md hover:border-blue-300 dark:hover:border-blue-700 transition-all duration-200 cursor-pointer"
                @click="openDrawer(plugin)"
            >
                <!-- 卡片顶部色条 -->
                <div class="h-1" :class="getCategoryColor(plugin.category)"></div>

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
                            <div>
                                <div class="font-semibold text-gray-800 dark:text-gray-100 text-sm">{{ plugin.name }}</div>
                                <div class="text-xs text-gray-400 font-mono mt-0.5">{{ plugin.pluginId }}</div>
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
                    <p class="text-sm text-gray-600 dark:text-gray-400 leading-relaxed mb-4 line-clamp-2">
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
                    <div class="flex items-center justify-between pt-3 border-t border-gray-100 dark:border-gray-700">
                        <div class="flex items-center gap-2">
                            <span class="w-2 h-2 rounded-full" :class="plugin.isEnabled ? 'bg-green-500' : 'bg-gray-300'"></span>
                            <span class="text-xs" :class="plugin.isEnabled ? 'text-green-600' : 'text-gray-400'">
                                {{ plugin.isEnabled ? '运行中' : '已停用' }}
                            </span>
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
                <div class="flex items-center justify-between bg-gray-50 dark:bg-gray-700/50 rounded-xl p-4 mb-5">
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

                            <!-- editor_assistant 专属配置 -->
                            <template v-else-if="currentPlugin.pluginId === 'editor_assistant'">
                                <el-form-item label="温度参数">
                                    <el-slider v-model="drawerForm.temperature" :min="0" :max="2" :step="0.1" show-input />
                                </el-form-item>
                                <el-form-item label="最大回复 Token">
                                    <el-input-number v-model="drawerForm.maxTokens" :min="256" :max="8000" :step="256" class="!w-40" />
                                </el-form-item>
                            </template>

                            <!-- tag_recommend 专属配置 -->
                            <template v-else-if="currentPlugin.pluginId === 'tag_recommend'">
                                <el-form-item label="推荐标签数量">
                                    <div class="flex items-center gap-3">
                                        <el-input-number
                                            v-model="drawerForm.tagCount"
                                            :min="1" :max="20"
                                            class="!w-36"
                                        />
                                        <span class="text-sm text-gray-500">每篇文章推荐的标签数</span>
                                    </div>
                                </el-form-item>
                                <el-form-item label="最大处理字数">
                                    <el-input-number v-model="drawerForm.maxContentLength" :min="1000" :max="50000" :step="1000" class="!w-40" />
                                    <div class="text-xs text-gray-400 mt-1">文章内容超出该字数将被截断后再分析</div>
                                </el-form-item>
                            </template>

                            <!-- translation 专属配置 -->
                            <template v-else-if="currentPlugin.pluginId === 'translation'">
                                <el-form-item label="默认目标语言">
                                    <el-select v-model="drawerForm.defaultTargetLanguage" class="!w-52">
                                        <el-option label="英语 (English)" value="en" />
                                        <el-option label="中文 (Chinese)" value="zh" />
                                        <el-option label="日语 (Japanese)" value="ja" />
                                        <el-option label="韩语 (Korean)" value="ko" />
                                        <el-option label="法语 (French)" value="fr" />
                                        <el-option label="德语 (German)" value="de" />
                                        <el-option label="西班牙语 (Spanish)" value="es" />
                                        <el-option label="俄语 (Russian)" value="ru" />
                                    </el-select>
                                    <div class="text-xs text-gray-400 mt-1">用户未指定目标语言时的默认翻译方向</div>
                                </el-form-item>
                                <el-form-item label="最大处理字数">
                                    <el-input-number v-model="drawerForm.maxContentLength" :min="1000" :max="50000" :step="1000" class="!w-40" />
                                    <div class="text-xs text-gray-400 mt-1">翻译内容超出该字数将被截断</div>
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
                                <p class="text-xs text-gray-400 mt-2">绿色表示该 Provider 当前已启用并可用</p>
                            </div>
                        </div>
                    </el-tab-pane>

                </el-tabs>
            </div>

            <!-- 抽屉底部操作 -->
            <template #footer>
                <div class="flex items-center justify-between">
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
    VideoPlay, Check, Search
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
    // article_summary / tag_recommend / translation
    maxContentLength: 8000,
    summaryLength: '200-300',
    // tag_recommend
    tagCount: 5,
    // translation
    defaultTargetLanguage: 'en',
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
    editor: {
        name: '编辑辅助', color: 'bg-purple-500', bg: 'bg-purple-100 dark:bg-purple-900/40',
        icon: 'Edit', iconClass: 'text-purple-500', tagType: 'danger'
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
    } else if (plugin.pluginId === 'article_summary') {
        drawerForm.maxContentLength = saved.maxContentLength ?? 8000
        drawerForm.summaryLength = saved.summaryLength || '200-300'
    } else if (plugin.pluginId === 'editor_assistant') {
        drawerForm.temperature = saved.temperature ?? 0.8
        drawerForm.maxTokens = saved.maxTokens ?? 2000
    } else if (plugin.pluginId === 'tag_recommend') {
        drawerForm.tagCount = saved.tagCount ?? 5
        drawerForm.maxContentLength = saved.maxContentLength ?? 5000
    } else if (plugin.pluginId === 'translation') {
        drawerForm.defaultTargetLanguage = saved.defaultTargetLanguage || 'en'
        drawerForm.maxContentLength = saved.maxContentLength ?? 10000
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
            maxTokens: drawerForm.maxTokens
        })
    }
    if (id === 'article_summary') {
        return JSON.stringify({
            ...modelField,
            maxContentLength: drawerForm.maxContentLength,
            summaryLength: drawerForm.summaryLength
        })
    }
    if (id === 'editor_assistant') {
        return JSON.stringify({
            ...modelField,
            temperature: drawerForm.temperature,
            maxTokens: drawerForm.maxTokens
        })
    }
    if (id === 'tag_recommend') {
        return JSON.stringify({
            ...modelField,
            tagCount: drawerForm.tagCount,
            maxContentLength: drawerForm.maxContentLength
        })
    }
    if (id === 'translation') {
        return JSON.stringify({
            ...modelField,
            defaultTargetLanguage: drawerForm.defaultTargetLanguage,
            maxContentLength: drawerForm.maxContentLength
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
</style>

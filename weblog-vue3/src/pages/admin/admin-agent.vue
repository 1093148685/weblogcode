<template>
    <div class="agent-page-root flex flex-col">
        <!-- 顶部标题栏 -->
        <header class="agent-topbar flex justify-between items-center px-5 py-3">
            <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-xl bg-slate-800 flex items-center justify-center">
                    <el-icon :size="20" class="text-white"><MagicStick /></el-icon>
                </div>
                <div>
                    <h1 class="text-base font-semibold m-0">AI 管理助手</h1>
                    <span class="flex items-center gap-1.5 text-xs" :class="connected ? 'text-emerald-500' : 'text-gray-500'">
                        <span class="w-2 h-2 rounded-full bg-current animate-pulse"></span>
                        {{ connected ? '运行中' : '连接中...' }}
                    </span>
                </div>
            </div>
            <div class="flex items-center gap-3">
                <el-tag v-if="currentModelName" size="small" type="info">模型: {{ currentModelName }}</el-tag>
                <el-tag v-else size="small" type="warning">未配置模型</el-tag>
                <el-button-group>
                    <el-button :type="showHistory ? 'primary' : 'default'" @click="showHistory = !showHistory; if(showHistory) loadSessions()" size="small" title="历史会话">
                        <el-icon><Clock /></el-icon>
                    </el-button>
                    <el-button type="default" @click="newSession" size="small" title="新建会话">
                        <el-icon><Plus /></el-icon>
                    </el-button>
                    <el-button :type="activeTab === 'config' ? 'primary' : 'default'" @click="activeTab = activeTab === 'config' ? 'chat' : 'config'" size="small" title="配置文件">
                        <el-icon><Document /></el-icon>
                    </el-button>
                    <el-button :type="activeTab === 'logs' ? 'primary' : 'default'" @click="activeTab = activeTab === 'logs' ? 'chat' : 'logs'" size="small" title="操作日志">
                        <el-icon><Tickets /></el-icon>
                    </el-button>
                    <el-button :type="showSettings ? 'primary' : 'default'" @click="showSettings = !showSettings" size="small">
                        <el-icon><Setting /></el-icon>
                    </el-button>
                    <el-button @click="clearChat" size="small" title="新建会话">
                        <el-icon><Delete /></el-icon>
                    </el-button>
                </el-button-group>
            </div>
        </header>

        <!-- 主体区域 -->
        <main class="flex-1 flex overflow-hidden relative">
            <!-- 消息区域 -->
            <div class="flex-1 flex flex-col overflow-hidden">
                <!-- 欢迎页 -->
                <div v-if="messages.length === 0" class="agent-welcome flex-1 flex flex-col items-center justify-center p-10 text-center">
                    <div class="w-20 h-20 rounded-2xl bg-slate-800 flex items-center justify-center mb-6 shadow-lg">
                        <el-icon :size="48" class="text-white"><MagicStick /></el-icon>
                    </div>
                    <h2 class="text-2xl font-semibold text-slate-800 mb-2">您好，我是您的博客管理助手</h2>
                    <p class="text-slate-500 mb-8">我可以帮您管理文章、分类、标签和评论</p>

                    <div class="agent-readiness-grid">
                        <div class="agent-readiness-card">
                            <span>当前模型</span>
                            <strong>{{ currentModelName || '未配置' }}</strong>
                        </div>
                        <div class="agent-readiness-card">
                            <span>配置文件</span>
                            <strong>{{ configFiles.length }} 个</strong>
                        </div>
                        <div class="agent-readiness-card">
                            <span>历史会话</span>
                            <strong>{{ sessionList.length }} 条</strong>
                        </div>
                        <div class="agent-readiness-card">
                            <span>操作日志</span>
                            <strong>{{ logTotal }} 条</strong>
                        </div>
                    </div>

                    <!-- 快捷操作 -->
                    <div class="mb-8">
                        <div class="flex gap-3 flex-wrap justify-center">
                            <button v-for="(action, idx) in quickActions" :key="idx"
                                    class="flex flex-col items-center gap-2 px-6 py-4 bg-white rounded-xl cursor-pointer border border-slate-200 text-slate-700 transition-all duration-300 hover:-translate-y-1 hover:shadow-md hover:border-blue-300"
                                    @click="sendExample(action.prompt)">
                                <el-icon :size="20" class="text-blue-500"><component :is="action.icon" /></el-icon>
                                <span class="text-[13px] font-medium">{{ action.label }}</span>
                            </button>
                        </div>
                    </div>

                    <!-- 安全说明 -->
                    <div class="bg-slate-50 border border-slate-200 rounded-xl px-7 py-5 text-left max-w-[400px]">
                        <h3 class="text-slate-800 text-sm font-semibold mb-3">安全说明</h3>
                        <div class="flex flex-col gap-2">
                            <div class="flex items-center gap-2 text-amber-600 text-[13px]">
                                <el-icon><Warning /></el-icon>
                                删除和创建操作需要您的二次确认
                            </div>
                            <div class="flex items-center gap-2 text-slate-600 text-[13px]">
                                <el-icon><InfoFilled /></el-icon>
                                所有操作都会记录日志
                            </div>
                        </div>
                    </div>
                </div>

                <!-- 消息列表 -->
                <div ref="msgRef" class="flex-1 overflow-y-auto p-5 flex flex-col gap-4 min-h-0">
                    <template v-for="(msg, idx) in messages" :key="idx">
                        <!-- 用户消息 -->
                        <div v-if="msg.role === 'user'" class="flex gap-3 max-w-[85%] self-end flex-row-reverse animate-msg-in-right">
                            <div class="px-4 py-3 rounded-2xl rounded-br-sm leading-relaxed text-sm bg-blue-500 text-white">
                                {{ msg.content }}
                            </div>
                            <div class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 bg-slate-700 text-white">
                                <el-icon><UserFilled /></el-icon>
                            </div>
                        </div>

                        <!-- AI 消息 -->
                        <div v-else-if="msg.role === 'assistant'" class="flex gap-3 max-w-[85%] self-start animate-msg-in-left">
                            <div class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 bg-slate-800">
                                <el-icon :size="14" class="text-white"><MagicStick /></el-icon>
                            </div>
                            <div class="px-4 py-3 rounded-2xl rounded-bl-sm leading-relaxed text-sm bg-white text-gray-700 shadow-sm border border-slate-200 overflow-x-auto agent-md">
                                <div v-html="renderMd(msg.content)"></div>
                            </div>
                        </div>

                        <!-- 工具调用卡片 -->
                        <div v-else-if="msg.role === 'tool_call'" class="flex gap-3 max-w-full self-start animate-msg-in-left">
                            <div class="bg-white rounded-2xl overflow-hidden shadow-sm border border-slate-200 min-w-[300px]">
                                <!-- 工具头部 -->
                                <div class="flex justify-between items-center px-4 py-3 bg-amber-500 text-white cursor-pointer" @click="msg.expanded = !msg.expanded">
                                    <div class="flex items-center gap-2">
                                        <el-icon class="text-base"><Tools /></el-icon>
                                        <span class="font-medium text-[13px]">{{ translateToolName(msg.toolName) }}</span>
                                        <el-tag v-if="isDestructiveTool(msg.toolName)" size="small" type="danger">危险操作</el-tag>
                                    </div>
                                    <div class="flex items-center gap-2">
                                        <el-tag v-if="!msg.expanded && msg.result" size="small" type="success">已完成</el-tag>
                                        <el-icon class="text-xs transition-transform duration-200" :class="{ 'rotate-180': msg.expanded }"><CaretBottom /></el-icon>
                                    </div>
                                </div>
                                <!-- 工具详情 -->
                                <div v-show="msg.expanded" class="p-4 flex flex-col gap-2.5">
                                    <div class="flex gap-2 text-xs">
                                        <span class="text-gray-500 font-medium">参数：</span>
                                        <pre class="bg-gray-100 px-3 py-2 rounded-md font-mono text-xs m-0 overflow-x-auto">{{ formatArgs(msg.toolArgs) }}</pre>
                                    </div>
                                    <div v-if="msg.result">
                                        <span class="text-xs text-gray-500 font-medium">结果：</span>
                                        <div class="text-xs mt-1.5 max-h-[200px] overflow-y-auto agent-md" v-html="renderMd(truncateResult(msg.result))"></div>
                                        <el-button v-if="msg.result.length > 300" size="small" text @click="msg.resultExpanded = !msg.resultExpanded">
                                            {{ msg.resultExpanded ? '收起' : '展开全部' }}
                                        </el-button>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- 思考中 -->
                        <div v-else-if="msg.role === 'thinking'" class="flex gap-3 max-w-[85%] self-start animate-msg-in-left">
                            <div class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 bg-slate-800">
                                <el-icon :size="14" class="text-white"><MagicStick /></el-icon>
                            </div>
                            <div class="flex items-center gap-2.5 text-blue-500 bg-white px-4 py-3 rounded-2xl rounded-bl-sm border border-slate-200">
                                <div class="flex gap-1">
                                    <span class="w-1.5 h-1.5 rounded-full bg-blue-500 animate-thinking-dot-1"></span>
                                    <span class="w-1.5 h-1.5 rounded-full bg-blue-500 animate-thinking-dot-2"></span>
                                    <span class="w-1.5 h-1.5 rounded-full bg-blue-500 animate-thinking-dot-3"></span>
                                </div>
                                <span>{{ msg.content }}</span>
                            </div>
                        </div>

                    </template>

                    <!-- 流式输出 -->
                    <div v-if="streaming" class="flex gap-3 max-w-[85%] self-start">
                        <div class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 bg-slate-800">
                            <el-icon :size="14" class="text-white"><MagicStick /></el-icon>
                        </div>
                        <div class="px-4 py-3 rounded-2xl rounded-bl-sm leading-relaxed text-sm bg-white text-gray-700 shadow-sm border border-slate-200 overflow-x-auto agent-md">
                            <span v-html="renderMd(streamBuffer)"></span>
                            <span class="inline-block w-0.5 h-4 bg-blue-500 ml-0.5 animate-blink align-middle"></span>
                        </div>
                    </div>
                </div>

                <!-- 实时处理日志 -->
                <div v-if="realtimeLogs.length > 0" class="border-t border-gray-200 bg-gray-900">
                    <div class="flex justify-between items-center px-3 py-1.5 cursor-pointer" @click="showRealtimeLogs = !showRealtimeLogs">
                        <span class="text-xs text-gray-400 flex items-center gap-1.5">
                            <span class="w-1.5 h-1.5 rounded-full bg-green-400 animate-pulse"></span>
                            处理日志 ({{ realtimeLogs.length }})
                        </span>
                        <el-icon class="text-gray-400 text-xs transition-transform" :class="{ 'rotate-180': showRealtimeLogs }"><CaretBottom /></el-icon>
                    </div>
                    <div v-show="showRealtimeLogs" ref="logPanelRef" class="max-h-[120px] overflow-y-auto px-3 pb-2">
                        <div v-for="(log, idx) in realtimeLogs" :key="idx" class="text-xs font-mono leading-5"
                            :class="log.level === 'error' ? 'text-red-400' : 'text-green-400'">
                            <span class="text-gray-500 mr-2">{{ log.time }}</span>{{ log.message }}
                        </div>
                    </div>
                </div>

                <!-- 输入区域 -->
                <div class="px-5 pt-2 pb-3 bg-transparent">
                    <div class="max-w-3xl mx-auto">
                        <div class="relative bg-white dark:bg-gray-800 rounded-xl border border-gray-300 dark:border-gray-600 focus-within:border-blue-500 transition-colors shadow-sm">
                            <textarea
                                ref="textareaRef"
                                v-model="inputText"
                                @input="autoResize"
                                @keydown.enter.exact.prevent="send"
                                placeholder="输入您的指令，Enter 发送..."
                                class="w-full px-4 py-3 pr-24 bg-transparent border-none outline-none resize-none text-sm text-gray-800 dark:text-gray-200 placeholder-gray-400"
                                rows="2"
                                :disabled="loading"
                            ></textarea>
                            <div class="absolute right-2 bottom-2 flex items-center gap-1.5">
                                <span class="text-xs text-gray-300 dark:text-gray-500">{{ inputText.length }}</span>
                                <el-button
                                    v-if="!loading"
                                    type="primary"
                                    :disabled="!inputText.trim()"
                                    @click="send"
                                    class="!w-8 !h-8 !rounded-lg !p-0"
                                >
                                    <el-icon><Promotion /></el-icon>
                                </el-button>
                                <el-button
                                    v-else
                                    type="info"
                                    class="!w-8 !h-8 !rounded-lg !p-0"
                                    disabled
                                >
                                    <el-icon class="animate-spin"><Loading /></el-icon>
                                </el-button>
                            </div>
                        </div>
                        <div class="flex justify-center gap-2 mt-1.5 text-xs text-gray-400">
                            <span>Enter 发送</span>
                            <span>·</span>
                            <span class="text-amber-500">危险操作需二次确认</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- 设置面板 -->
            <aside v-if="showSettings" class="w-[280px] bg-white border-l border-slate-200 flex flex-col">
                <div class="flex justify-between items-center p-4 border-b border-gray-200">
                    <h3 class="text-sm font-semibold text-gray-800 m-0">设置</h3>
                    <el-button text @click="showSettings = false">
                        <el-icon><Close /></el-icon>
                    </el-button>
                </div>

                <div class="flex-1 overflow-y-auto p-4 flex flex-col gap-4">
                    <!-- 模型 -->
                    <div class="flex flex-col gap-1.5">
                        <label class="text-xs font-medium text-gray-700 flex justify-between">模型</label>
                        <el-select v-model="settings.model" class="w-full" size="small" :placeholder="availableModels.length === 0 ? '无可用模型' : '请选择模型'">
                            <el-option v-for="model in availableModels" :key="model.id" :label="`${model.name} (${model.provider})`" :value="model.id" />
                        </el-select>
                        <div v-if="availableModels.length === 0" class="text-xs text-red-500">
                            请先在 AI Provider 中启用模型
                        </div>
                    </div>

                    <!-- 温度 -->
                    <div class="flex flex-col gap-1.5">
                        <label class="text-xs font-medium text-gray-700 flex justify-between">
                            温度
                            <span class="text-blue-500 font-semibold">{{ settings.temperature.toFixed(2) }}</span>
                        </label>
                        <el-slider v-model="settings.temperature" :min="0" :max="1" :step="0.05" size="small" />
                        <div class="flex justify-between text-[11px] text-gray-400">
                            <span>精确</span>
                            <span>创造</span>
                        </div>
                    </div>

                    <!-- Max Tokens -->
                    <div class="flex flex-col gap-1.5">
                        <label class="text-xs font-medium text-gray-700 flex justify-between">
                            Max Tokens
                            <span class="text-blue-500 font-semibold">{{ settings.maxTokens }}</span>
                        </label>
                        <el-slider v-model="settings.maxTokens" :min="100" :max="8000" :step="100" size="small" />
                    </div>

                    <!-- Max Turns -->
                    <div class="flex flex-col gap-1.5">
                        <label class="text-xs font-medium text-gray-700 flex justify-between">
                            最大轮次
                            <span class="text-blue-500 font-semibold">{{ settings.maxTurns }}</span>
                        </label>
                        <el-slider v-model="settings.maxTurns" :min="1" :max="10" :step="1" size="small" />
                    </div>

                    <!-- 系统提示词 -->
                    <div class="flex flex-col gap-1.5">
                        <label class="text-xs font-medium text-gray-700">系统提示词</label>
                        <el-input v-model="settings.systemPrompt" type="textarea" :rows="3" placeholder="自定义..." size="small" />
                        <el-button text size="small" @click="settings.systemPrompt = ''">重置默认</el-button>
                    </div>

                    <!-- 保存提示 -->
                    <div class="text-xs text-gray-400 pt-2 border-t border-gray-200">
                        删除/创建操作需要二次确认
                    </div>
                </div>
            </aside>

            <!-- 历史会话侧边栏 -->
            <aside v-if="showHistory" class="w-[260px] bg-white border-l border-slate-200 flex flex-col z-20">
                <div class="flex justify-between items-center p-4 border-b border-gray-200">
                    <h3 class="text-sm font-semibold text-gray-800 m-0">历史会话</h3>
                    <el-button text @click="showHistory = false"><el-icon><Close /></el-icon></el-button>
                </div>
                <div class="flex-1 overflow-y-auto" v-loading="sessionLoading">
                    <div v-if="sessionList.length === 0" class="flex items-center justify-center h-full text-gray-400 text-sm">暂无历史会话</div>
                    <div v-for="s in sessionList" :key="s.sessionId"
                         class="flex items-start justify-between gap-2 px-4 py-3 cursor-pointer border-b border-gray-100 hover:bg-slate-50 transition-colors"
                         :class="sessionId === s.sessionId ? 'bg-blue-50 border-l-2 border-l-blue-500' : ''"
                         @click="loadSession(s.sessionId)">
                        <div class="flex-1 min-w-0">
                            <div class="text-sm font-medium text-gray-800 truncate">{{ s.title }}</div>
                            <div class="text-xs text-gray-400 mt-0.5">{{ s.updatedAt }}</div>
                        </div>
                        <el-button text size="small" class="!text-gray-400 hover:!text-red-500 shrink-0"
                                   @click.stop="removeSession(s.sessionId)">
                            <el-icon><Delete /></el-icon>
                        </el-button>
                    </div>
                </div>
            </aside>
            <div v-if="activeTab === 'config'" class="absolute inset-0 bg-white z-10 flex flex-col overflow-hidden">
                <div class="flex justify-between items-center px-5 py-3 border-b border-gray-200">
                    <h3 class="text-sm font-semibold text-gray-800 m-0">AI Agent 配置文件</h3>
                    <el-button text @click="activeTab = 'chat'">
                        <el-icon><Close /></el-icon>
                    </el-button>
                </div>
                <div class="flex flex-1 overflow-hidden">
                    <!-- 配置文件列表 -->
                    <div class="w-[200px] border-r border-gray-200 bg-gray-50 overflow-y-auto">
                        <div v-for="config in configFiles" :key="config.fileName"
                            class="px-4 py-3 cursor-pointer border-b border-gray-100 transition-colors"
                            :class="selectedConfig === config.fileName ? 'bg-blue-50 text-blue-600 border-l-2 border-l-blue-500' : 'hover:bg-gray-100'"
                            @click="selectConfig(config.fileName)">
                            <div class="text-sm font-medium">{{ config.fileName }}.md</div>
                            <div class="text-xs text-gray-400 mt-1 truncate">{{ config.description }}</div>
                        </div>
                    </div>
                    <!-- 编辑区域 -->
                    <div class="flex-1 flex flex-col overflow-hidden">
                        <div v-if="selectedConfig" class="flex-1 flex flex-col overflow-hidden p-4">
                            <div class="flex justify-between items-center mb-3">
                                <div>
                                    <span class="text-base font-semibold text-gray-800">{{ selectedConfig }}.md</span>
                                    <span class="text-xs text-gray-400 ml-3">{{ getSelectedConfigDescription() }}</span>
                                </div>
                                <div class="flex gap-2">
                                    <el-button size="small" @click="resetSelectedConfig" :loading="configSaving">
                                        <el-icon><RefreshRight /></el-icon>
                                        重置默认
                                    </el-button>
                                    <el-button size="small" type="primary" @click="saveSelectedConfig" :loading="configSaving">
                                        <el-icon><Check /></el-icon>
                                        保存
                                    </el-button>
                                </div>
                            </div>
                            <el-input
                                v-model="configContent"
                                type="textarea"
                                :rows="20"
                                resize="none"
                                class="flex-1 config-editor"
                                placeholder="输入配置内容（Markdown 格式）..."
                            />
                        </div>
                        <div v-else class="flex-1 flex items-center justify-center text-gray-400">
                            请从左侧选择一个配置文件
                        </div>
                    </div>
                </div>
            </div>

            <!-- 操作日志面板 -->
            <div v-if="activeTab === 'logs'" class="absolute inset-0 bg-white z-10 flex flex-col overflow-hidden">
                <div class="flex justify-between items-center px-5 py-3 border-b border-gray-200">
                    <div class="flex items-center gap-3">
                        <h3 class="text-sm font-semibold text-gray-800 m-0">AI Agent 操作日志</h3>
                        <el-tag size="small" type="info">共 {{ logTotal }} 条</el-tag>
                    </div>
                    <div class="flex items-center gap-2">
                        <el-select v-model="logFilter.action" placeholder="操作类型" size="small" clearable class="!w-[120px]" @change="loadLogs(1)">
                            <el-option label="全部" value="" />
                            <el-option label="创建" value="create" />
                            <el-option label="删除" value="delete" />
                            <el-option label="更新" value="update" />
                            <el-option label="置顶" value="toggle" />
                            <el-option label="审核" value="approve" />
                            <el-option label="拒绝" value="reject" />
                            <el-option label="重置" value="reset" />
                        </el-select>
                        <el-popconfirm title="确定清空所有日志？" @confirm="clearLogs">
                            <template #reference>
                                <el-button size="small" type="danger" plain>
                                    <el-icon><Delete /></el-icon>
                                    清空
                                </el-button>
                            </template>
                        </el-popconfirm>
                        <el-button text @click="activeTab = 'chat'">
                            <el-icon><Close /></el-icon>
                        </el-button>
                    </div>
                </div>
                <div class="flex-1 overflow-y-auto p-4">
                    <el-table :data="logList" border stripe size="small" v-loading="logLoading" empty-text="暂无操作日志">
                        <el-table-column prop="createdAt" label="时间" width="170">
                            <template #default="{ row }">
                                {{ formatDate(row.createdAt) }}
                            </template>
                        </el-table-column>
                        <el-table-column prop="action" label="操作" width="80">
                            <template #default="{ row }">
                                <el-tag :type="getActionType(row.action)" size="small">{{ getActionLabel(row.action) }}</el-tag>
                            </template>
                        </el-table-column>
                        <el-table-column prop="target" label="目标" width="80">
                            <template #default="{ row }">
                                {{ getTargetLabel(row.target) }}
                            </template>
                        </el-table-column>
                        <el-table-column prop="description" label="描述" min-width="200" show-overflow-tooltip />
                        <el-table-column prop="status" label="状态" width="80">
                            <template #default="{ row }">
                                <el-tag :type="row.status === 'success' ? 'success' : 'danger'" size="small">
                                    {{ row.status === 'success' ? '成功' : '失败' }}
                                </el-tag>
                            </template>
                        </el-table-column>
                    </el-table>
                    <div class="flex justify-center mt-4" v-if="logTotal > logPageSize">
                        <el-pagination
                            v-model:current-page="logPage"
                            :page-size="logPageSize"
                            :total="logTotal"
                            layout="prev, pager, next"
                            @current-change="loadLogs"
                        />
                    </div>
                </div>
            </div>
        </main>
    </div>
</template>

<script setup>
import { ref, reactive, computed, nextTick, onMounted, onUnmounted, watch } from 'vue'
import { MagicStick, UserFilled, Promotion, Delete, Setting, Tools, Close, Check, DataAnalysis, Document, FolderOpened, Message, Scissor, Warning, InfoFilled, CaretBottom, Tickets, RefreshRight, Loading, Clock, Plus } from '@element-plus/icons-vue'
import { marked } from 'marked'
import { ElMessage } from 'element-plus'
import {
    getAvailableModels, sendAgentMessage, getAgentConfigs, getAgentConfig, saveAgentConfig, resetAgentConfig,
    getAgentLogs, clearAgentLogs as clearLogsApi, getAgentSessions, getAgentSession, deleteAgentSession
} from '@/api/admin/agent'

// ──────── 状态 ────────
const inputText    = ref('')
const messages      = ref([])
const loading       = ref(false)
const streaming      = ref(false)
const streamBuffer   = ref('')
const msgRef         = ref(null)
const textareaRef    = ref(null)
const showSettings   = ref(false)
const connected      = ref(true)
const availableModels = ref([])

// ──────── 标签页 ────────
const activeTab = ref('chat') // chat, config, logs

// ──────── 会话历史 ────────
const sessionId = ref('')
const showHistory = ref(false)
const sessionList = ref([])
const sessionLoading = ref(false)

const newSession = () => {
    sessionId.value = 'sess-' + Date.now()
    messages.value = []
    streamBuffer.value = ''
    realtimeLogs.value = []
}

const loadSessions = async () => {
    sessionLoading.value = true
    try {
        const res = await getAgentSessions()
        if (res.code === 200) sessionList.value = res.data || []
    } catch {}
    finally { sessionLoading.value = false }
}

const loadSession = async (sid) => {
    try {
        const res = await getAgentSession(sid)
        if (res.code === 200 && res.data) {
            sessionId.value = sid
            messages.value = (res.data.history || []).map(h => ({ role: h.role, content: h.content }))
            showHistory.value = false
            await scrollBottom()
        }
    } catch { ElMessage.error('加载会话失败') }
}

const removeSession = async (sid) => {
    try {
        await deleteAgentSession(sid)
        sessionList.value = sessionList.value.filter(s => s.sessionId !== sid)
        if (sessionId.value === sid) newSession()
        ElMessage.success('已删除')
    } catch { ElMessage.error('删除失败') }
}

// ──────── 配置文件 ────────
const configFiles = ref([])
const selectedConfig = ref('')
const configContent = ref('')
const configSaving = ref(false)

// ──────── 操作日志 ────────
const logList = ref([])
const logTotal = ref(0)
const logPage = ref(1)
const logPageSize = ref(20)
const logLoading = ref(false)
const logFilter = reactive({
    action: ''
})

// ──────── 实时处理日志 ────────
const realtimeLogs = ref([])
const showRealtimeLogs = ref(true)
const logPanelRef = ref(null)

// ──────── 设置 ────────
const settings = reactive({
    model: '',
    temperature: 0.2,
    maxTokens: 4096,
    maxTurns: 5,
    systemPrompt: ''
})

const currentModelName = computed(() => {
    if (!settings.model) return ''
    const model = availableModels.value.find(m => m.id === settings.model)
    return model ? `${model.name} (${model.provider})` : settings.model
})

// ──────── 快捷操作 ────────
const quickActions = [
    { label: '仪表盘', prompt: '显示博客仪表盘数据', icon: DataAnalysis },
    { label: '文章列表', prompt: '显示最近的文章列表', icon: Document },
    { label: '分类管理', prompt: '显示所有分类', icon: FolderOpened },
    { label: '评论管理', prompt: '显示最近的评论', icon: Message },
    { label: '标签管理', prompt: '显示所有标签', icon: Scissor },
]

// ──────── 工具函数 ────────
const isDestructiveTool = (name) => {
    const dangerousTools = ['delete_article', 'delete_category', 'delete_tag', 'delete_comment', 'create_category', 'create_tag', 'toggle_article_top', 'approve_comment', 'reject_comment', 'create_article', 'update_article', 'batch_delete_articles']
    return dangerousTools.includes(name)
}

const translateToolName = (name) => {
    const map = {
        'get_dashboard': '查询仪表盘',
        'get_articles': '查询文章列表',
        'search_articles': '搜索文章',
        'get_article': '获取文章基本信息',
        'get_article_content': '读取文章全文',
        'delete_article': '删除文章',
        'toggle_article_top': '设置置顶',
        'create_article': '创建文章',
        'update_article': '更新文章',
        'batch_delete_articles': '批量删除文章',
        'get_categories': '获取分类',
        'create_category': '创建分类',
        'delete_category': '删除分类',
        'get_tags': '获取标签',
        'create_tag': '创建标签',
        'delete_tag': '删除标签',
        'get_comments': '获取评论',
        'delete_comment': '删除评论',
        'approve_comment': '审核评论',
        'reject_comment': '拒绝评论',
    }
    return map[name] || name
}

const formatArgs = (args) => {
    if (!args) return '{}'
    try {
        const obj = JSON.parse(args)
        return JSON.stringify(obj, null, 2)
    } catch {
        return args
    }
}

const truncateResult = (result) => {
    if (!result) return ''
    if (result.length > 500) return result.slice(0, 500) + '...'
    return result
}

const renderMd = (text) => {
    if (!text) return ''
    try { return marked.parse(text) } catch { return text }
}

const scrollBottom = () => {
    nextTick(() => {
        if (msgRef.value) msgRef.value.scrollTop = msgRef.value.scrollHeight
    })
}

const autoResize = () => {
    if (textareaRef.value) {
        textareaRef.value.style.height = 'auto'
        textareaRef.value.style.height = Math.min(textareaRef.value.scrollHeight, 160) + 'px'
    }
}

// ──────── 发送消息 ────────
const send = async () => {
    const text = inputText.value.trim()
    if (!text || loading.value) return

    if (!settings.model) {
        ElMessage.warning('请先在设置中选择一个模型')
        return
    }

    inputText.value = ''
    loading.value = true
    realtimeLogs.value = []

    // Reset textarea height
    if (textareaRef.value) textareaRef.value.style.height = 'auto'

    const history = messages.value
        .filter(m => m.role === 'user' || m.role === 'assistant')
        .slice(-10)
        .map(m => ({ role: m.role, content: m.content }))

    messages.value.push({ role: 'user', content: text })
    await scrollBottom()

    try {
        const resp = await fetch('/api/admin/agent/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${(await import('@/composables/cookie')).getToken() || ''}`
            },
            body: JSON.stringify({
                message: text,
                history,
                model: settings.model,
                sessionId: sessionId.value,
                settings: {
                    temperature: settings.temperature,
                    maxTokens: settings.maxTokens,
                    maxTurns: settings.maxTurns,
                    systemPrompt: buildSystemPrompt(),
                    enabledTools: null
                }
            })
        })

        if (!resp.ok) throw new Error(`HTTP ${resp.status}`)

        const reader = resp.body.getReader()
        const decoder = new TextDecoder()
        let buf = ''
        streaming.value = true
        streamBuffer.value = ''

        while (true) {
            const { done, value } = await reader.read()
            if (done) break

            buf += decoder.decode(value, { stream: true })
            const lines = buf.split('\n')
            buf = lines.pop() ?? ''

            for (const line of lines) {
                if (!line.startsWith('data: ')) continue
                const raw = line.slice(6).trim()
                if (!raw) continue

                try {
                    const evt = JSON.parse(raw)

                    if (evt.type === 'thinking') {
                        const lastIdx = messages.value.findLastIndex(m => m.role === 'thinking')
                        if (lastIdx !== -1) messages.value.splice(lastIdx, 1)
                        messages.value.push({ role: 'thinking', content: evt.message, expanded: false })
                        addRealtimeLog('info', evt.message)
                        await scrollBottom()
                    }
                    else if (evt.type === 'tool_result' || evt.type === 'tool_call') {
                        const lastThinking = messages.value.findLastIndex(m => m.role === 'thinking')
                        if (lastThinking !== -1) messages.value.splice(lastThinking, 1)
                        const toolMsg = {
                            role: 'tool_call',
                            toolName: evt.tool,
                            toolArgs: evt.args || '',
                            result: evt.result || null,
                            expanded: !isDestructiveTool(evt.tool),
                            resultExpanded: false
                        }
                        messages.value.push(toolMsg)
                        await scrollBottom()
                    }
                    else if (evt.type === 'chunk') {
                        streamBuffer.value += evt.content
                        await scrollBottom()
                    }
                    else if (evt.type === 'done') {
                        messages.value = messages.value.filter(m => m.role !== 'thinking')
                        if (streamBuffer.value) {
                            messages.value.push({ role: 'assistant', content: streamBuffer.value })
                        }
                        streaming.value = false
                        streamBuffer.value = ''
                        await scrollBottom()
                    }
                    else if (evt.type === 'error') {
                        messages.value = messages.value.filter(m => m.role !== 'thinking')
                        messages.value.push({ role: 'assistant', content: `\u274C ${evt.message}` })
                        addRealtimeLog('error', evt.message)
                        await scrollBottom()
                    }
                    else if (evt.type === 'log') {
                        addRealtimeLog(evt.level || 'info', evt.message)
                    }
                } catch { }
            }
        }
    } catch (e) {
        messages.value = messages.value.filter(m => m.role !== 'thinking')
        messages.value.push({ role: 'assistant', content: `\u274C 请求失败：${e.message}` })
        await scrollBottom()
    } finally {
        loading.value = false
        streaming.value = false
    }
}

const buildSystemPrompt = () => {
    var basePrompt = `你是一个精确的博客管理助手。

## 核心规则：
1. **必须使用工具**：任何关于文章���分类、标签、评论、统计数据的问题，你必须调用相应工具获取真实数据
2. **禁止编造**：永远不要自己生成文章列表、统计数据等，必须通过工具查询
3. **二次确认**：对于删除、创建等危险操作，必须先向用户确认才能执行
4. **危险操作前询问**：当执行 delete_、create_、toggle_、approve_、reject_ 等操作时，必须先发送 confirm 事件询问用户

## 确认格式：
当需要用户确认时，发送格式：
{"type": "confirm", "message": "您确定要删除文章 #123 吗？此操作不可撤销。"}

## 可用工具及使用场景：
- get_dashboard：查询博客统计（文章数、分类数、标签数、PV）
- get_articles：查询文章列表（支持分页、搜索）
- get_article：查询单个文章详情
- delete_article：删除文章（需确认）
- toggle_article_top：设置文章置顶（需确认）
- get_categories：查询所有分类
- create_category：创建分类（需确认）
- delete_category：删除分类（需确认）
- get_tags：查询所有标签
- create_tag：创建标签（需确认）
- delete_tag：删除标签（需确认）
- get_comments：查询评论列表
- delete_comment：删除评论（需确认）
- approve_comment：通过审核评论（需确认）
- reject_comment：拒绝评论（需确认）

## 回答格式：
- 直接展示工具返回的真实数据
- 不要添加任何虚假信息
- 用表格或列表展示结构化数据
- 操作完成后说明执行结果`

    if (settings.systemPrompt) {
        return settings.systemPrompt + "\n\n" + basePrompt
    }
    return basePrompt
}

const sendExample = (text) => {
    inputText.value = text
    send()
}

const clearChat = () => {
    newSession()
    ElMessage.success('已开启新会话')
}

// 添加实时日志
const addRealtimeLog = (level, message) => {
    const now = new Date()
    const time = now.toTimeString().split(' ')[0]
    realtimeLogs.value.push({ level, message, time })
    // 最多保留 100 条
    if (realtimeLogs.value.length > 100) {
        realtimeLogs.value = realtimeLogs.value.slice(-50)
    }
    // 滚动到底部
    nextTick(() => {
        if (logPanelRef.value) logPanelRef.value.scrollTop = logPanelRef.value.scrollHeight
    })
}

// ──────── 配置文件操作 ────────
const loadConfigs = async () => {
    try {
        const res = await getAgentConfigs()
        if (res.code === 200 && res.data) {
            configFiles.value = res.data
            if (configFiles.value.length > 0 && !selectedConfig.value) {
                selectConfig(configFiles.value[0].fileName)
            }
        }
    } catch (e) {
        console.error('Failed to load configs:', e)
    }
}

const selectConfig = async (fileName) => {
    selectedConfig.value = fileName
    try {
        const res = await getAgentConfig(fileName)
        if (res.code === 200 && res.data) {
            configContent.value = res.data.content
        }
    } catch (e) {
        console.error('Failed to load config:', e)
    }
}

const getSelectedConfigDescription = () => {
    const config = configFiles.value.find(c => c.fileName === selectedConfig.value)
    return config?.description || ''
}

const saveSelectedConfig = async () => {
    if (!selectedConfig.value) return
    configSaving.value = true
    try {
        const res = await saveAgentConfig(selectedConfig.value, configContent.value)
        if (res.code === 200) {
            ElMessage.success('配置保存成功')
            await loadConfigs()
        } else {
            ElMessage.error(res.message || '保存失败')
        }
    } catch (e) {
        ElMessage.error('保存失败')
    } finally {
        configSaving.value = false
    }
}

const resetSelectedConfig = async () => {
    if (!selectedConfig.value) return
    configSaving.value = true
    try {
        const res = await resetAgentConfig(selectedConfig.value)
        if (res.code === 200 && res.data) {
            configContent.value = res.data.content
            ElMessage.success('已重置为默认配置')
            await loadConfigs()
        }
    } catch (e) {
        ElMessage.error('重置失败')
    } finally {
        configSaving.value = false
    }
}

// ──────── 操作日志 ────────
const loadLogs = async (page) => {
    if (page) logPage.value = page
    logLoading.value = true
    try {
        const res = await getAgentLogs({
            page: logPage.value,
            pageSize: logPageSize.value,
            action: logFilter.action || undefined
        })
        if (res.code === 200 && res.data) {
            logList.value = res.data.list || []
            logTotal.value = res.data.total || 0
        }
    } catch (e) {
        console.error('Failed to load logs:', e)
    } finally {
        logLoading.value = false
    }
}

const clearLogs = async () => {
    try {
        const res = await clearLogsApi()
        if (res.code === 200) {
            ElMessage.success('日志已清空')
            logList.value = []
            logTotal.value = 0
        }
    } catch (e) {
        ElMessage.error('清空失败')
    }
}

const formatDate = (dateStr) => {
    if (!dateStr) return ''
    const d = new Date(dateStr)
    return d.toLocaleString('zh-CN', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

const getActionType = (action) => {
    const map = { create: 'success', delete: 'danger', update: 'warning', toggle: 'info', approve: 'success', reject: 'danger', reset: 'info' }
    return map[action] || 'info'
}

const getActionLabel = (action) => {
    const map = { create: '创建', delete: '删除', update: '更新', toggle: '置顶', approve: '审核', reject: '拒绝', reset: '重置' }
    return map[action] || action
}

const getTargetLabel = (target) => {
    const map = { article: '文章', category: '分类', tag: '标签', comment: '评论', config: '配置' }
    return map[target] || target
}

onMounted(async () => {
    // 隐藏页脚，避免 Agent 页面出现外层滚动条
    const footer = document.querySelector('.el-footer')
    if (footer) footer.style.display = 'none'
    const elMain = document.querySelector('.el-main')
    if (elMain) elMain.style.minHeight = '0'

    // 初始化会话 ID
    newSession()

    try {
        const res = await getAvailableModels()
        if (res.code === 200 && res.data) {
            availableModels.value = res.data
            if (availableModels.value.length > 0) {
                settings.model = availableModels.value[0].id
            }
        }
    } catch {
        ElMessage.warning('模型列表加载失败，请检查 Provider 配置')
    }

    loadConfigs()
    loadLogs()
    loadSessions()
})

onUnmounted(() => {
    // 恢复页脚
    const footer = document.querySelector('.el-footer')
    if (footer) footer.style.display = ''
    const elMain = document.querySelector('.el-main')
    if (elMain) elMain.style.minHeight = ''
})

// 切换标签页时刷新数据
watch(activeTab, (tab) => {
    if (tab === 'config') loadConfigs()
    if (tab === 'logs') loadLogs(1)
})
</script>

<style scoped>
/* 页面根容器：撑满 el-main 可用空间，禁止外层滚动 */
.agent-page-root {
    height: calc(100vh - 64px - 44px);
    overflow: hidden;
    background:
        radial-gradient(circle at 20% 0%, rgba(34, 211, 238, 0.12), transparent 34%),
        radial-gradient(circle at 88% 12%, rgba(139, 92, 246, 0.16), transparent 32%),
        var(--admin-bg-page);
    color: var(--admin-text);
}

.agent-readiness-grid {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 12px;
    width: min(760px, 100%);
    margin: 0 auto 28px;
}

.agent-readiness-card {
    display: flex;
    min-width: 0;
    flex-direction: column;
    gap: 6px;
    padding: 14px 16px;
    border: 1px solid var(--admin-border);
    border-radius: 14px;
    background: var(--admin-bg-card);
    box-shadow: var(--admin-shadow-soft);
    text-align: left;
}

.agent-readiness-card span {
    color: var(--admin-text-muted);
    font-size: 12px;
}

.agent-readiness-card strong {
    overflow: hidden;
    color: var(--admin-text);
    font-size: 14px;
    font-weight: 800;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.agent-topbar {
    background: rgba(8, 13, 24, 0.82);
    border-bottom: 1px solid var(--admin-border);
    box-shadow: 0 14px 34px rgba(0, 0, 0, 0.24);
    backdrop-filter: blur(18px);
}

.agent-topbar h1 {
    color: var(--admin-text);
}

.agent-welcome {
    color: var(--admin-text);
}

.agent-welcome h2 {
    color: var(--admin-text);
}

.agent-welcome p {
    color: var(--admin-text-muted);
}

/* 输入框原生样式 */
.agent-page-root textarea:focus {
    outline: none;
}

/* 消息滑入动画 */
@keyframes msg-in-left {
    from { opacity: 0; transform: translateX(-16px); }
    to   { opacity: 1; transform: translateX(0); }
}
@keyframes msg-in-right {
    from { opacity: 0; transform: translateX(16px); }
    to   { opacity: 1; transform: translateX(0); }
}
.animate-msg-in-left {
    animation: msg-in-left 0.3s ease-out both;
}
.animate-msg-in-right {
    animation: msg-in-right 0.3s ease-out both;
}

/* 思考动画 */
@keyframes thinking-bounce {
    0%, 80%, 100% { transform: scale(0); }
    40% { transform: scale(1); }
}

.animate-thinking-dot-1 {
    animation: thinking-bounce 1.4s infinite ease-in-out both;
    animation-delay: 0s;
}

.animate-thinking-dot-2 {
    animation: thinking-bounce 1.4s infinite ease-in-out both;
    animation-delay: 0.16s;
}

.animate-thinking-dot-3 {
    animation: thinking-bounce 1.4s infinite ease-in-out both;
    animation-delay: 0.32s;
}

/* 消息气泡样式优化 */
.agent-page-root .flex.gap-3.max-w-\[85\%\].self-end > div:first-child {
    background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
    box-shadow: 0 4px 12px rgba(59, 130, 246, 0.35);
    border: none;
}

.agent-page-root .flex.gap-3.max-w-\[85\%\].self-start > div:last-child:not(.w-9) {
    background: var(--admin-bg-card);
    border: 1px solid var(--admin-border);
    box-shadow: var(--admin-shadow-soft);
    color: var(--admin-text);
}

/* 工具调用卡片样式 */
.agent-page-root .bg-white.rounded-2xl.overflow-hidden.shadow-sm.border.border-slate-200 {
    background: var(--admin-bg-card) !important;
    border-color: var(--admin-border) !important;
    border-radius: 16px;
    box-shadow: var(--admin-shadow-soft);
    transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.agent-page-root .bg-white.rounded-2xl.overflow-hidden.shadow-sm.border.border-slate-200:hover {
    transform: translateY(-1px);
    box-shadow: var(--admin-shadow);
}

/* 工具头部渐变 */
.agent-page-root .flex.justify-between.items-center.px-4.py-3.bg-amber-500 {
    background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
}

/* 用户头像样式 */
.agent-page-root .w-9.h-9.rounded-xl.flex.items-center.justify-center.shrink-0.bg-slate-700 {
    background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
    box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
}

/* AI 头像样式 */
.agent-page-root .w-9.h-9.rounded-xl.flex.items-center.justify-center.shrink-0.bg-slate-800 {
    background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
    box-shadow: 0 2px 8px rgba(99, 102, 241, 0.3);
}

/* 光标闪烁 */
@keyframes blink {
    0%, 100% { opacity: 1; }
    50% { opacity: 0; }
}

.animate-blink {
    animation: blink 1s infinite;
}

/* 配置编辑器 */
.config-editor :deep(.el-textarea__inner) {
    font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
    font-size: 14px;
    line-height: 1.6;
    height: 100% !important;
}

/* ──────── Markdown 渲染样式 ──────── */
.agent-md :deep(h1) {
    font-size: 1.5em;
    font-weight: 700;
    margin: 0.8em 0 0.4em;
    padding-bottom: 0.3em;
    border-bottom: 1px solid #e5e7eb;
    line-height: 1.3;
}

.agent-md :deep(h2) {
    font-size: 1.3em;
    font-weight: 600;
    margin: 0.7em 0 0.3em;
    line-height: 1.3;
}

.agent-md :deep(h3) {
    font-size: 1.15em;
    font-weight: 600;
    margin: 0.6em 0 0.3em;
    line-height: 1.3;
}

.agent-md :deep(h4),
.agent-md :deep(h5),
.agent-md :deep(h6) {
    font-size: 1em;
    font-weight: 600;
    margin: 0.5em 0 0.2em;
    line-height: 1.3;
}

.agent-md :deep(p) {
    margin: 0.5em 0;
    line-height: 1.7;
}

.agent-md :deep(ul) {
    list-style: disc;
    padding-left: 1.5em;
    margin: 0.5em 0;
}

.agent-md :deep(ol) {
    list-style: decimal;
    padding-left: 1.5em;
    margin: 0.5em 0;
}

.agent-md :deep(li) {
    margin: 0.25em 0;
    line-height: 1.6;
}

.agent-md :deep(li > ul),
.agent-md :deep(li > ol) {
    margin: 0.15em 0;
}

.agent-md :deep(blockquote) {
    border-left: 3px solid #3b82f6;
    padding: 0.5em 1em;
    margin: 0.5em 0;
    background: #f8fafc;
    color: #475569;
    border-radius: 0 6px 6px 0;
}

.agent-md :deep(blockquote p) {
    margin: 0.25em 0;
}

.agent-md :deep(code) {
    background: #f1f5f9;
    color: #e11d48;
    padding: 0.15em 0.4em;
    border-radius: 4px;
    font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
    font-size: 0.9em;
}

.agent-md :deep(pre) {
    background: #1e293b;
    color: #e2e8f0;
    padding: 1em;
    border-radius: 8px;
    overflow-x: auto;
    margin: 0.5em 0;
    line-height: 1.5;
}

.agent-md :deep(pre code) {
    background: transparent;
    color: inherit;
    padding: 0;
    border-radius: 0;
    font-size: 0.85em;
}

.agent-md :deep(table) {
    border-collapse: collapse;
    width: 100%;
    margin: 0.5em 0;
    font-size: 0.9em;
}

.agent-md :deep(th) {
    background: #f1f5f9;
    font-weight: 600;
    text-align: left;
    padding: 0.6em 0.8em;
    border: 1px solid #e2e8f0;
}

.agent-md :deep(td) {
    padding: 0.5em 0.8em;
    border: 1px solid #e2e8f0;
}

.agent-md :deep(tr:nth-child(even)) {
    background: #f8fafc;
}

.agent-md :deep(a) {
    color: #3b82f6;
    text-decoration: none;
}

.agent-md :deep(a:hover) {
    text-decoration: underline;
}

.agent-md :deep(hr) {
    border: none;
    border-top: 1px solid #e2e8f0;
    margin: 1em 0;
}

.agent-md :deep(strong) {
    font-weight: 600;
    color: #1e293b;
}

.agent-md :deep(em) {
    font-style: italic;
}

.agent-md :deep(img) {
    max-width: 100%;
    border-radius: 8px;
    margin: 0.5em 0;
}

/* 首个和末尾元素的 margin 清零 */
.agent-md :deep(> :first-child) {
    margin-top: 0;
}

.agent-md :deep(> :last-child) {
    margin-bottom: 0;
}

@media (max-width: 960px) {
    .agent-readiness-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}

@media (max-width: 560px) {
    .agent-readiness-grid {
        grid-template-columns: 1fr;
    }
}
</style>

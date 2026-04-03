<template>
    <!-- 标签导航栏 -->
    <div
        class="tag-nav fixed top-[64px] h-[44px] px-3 right-0 z-50 flex items-center transition-all duration-300"
        :style="{left: menuStore.menuWidth}"
    >
        <el-tabs
            v-model="activeTab"
            type="card"
            class="nav-tabs flex-1"
            @tab-remove="removeTab"
            @tab-change="tabChange"
            style="min-width: 10px;"
        >
            <el-tab-pane
                v-for="item in tabList"
                :key="item.path"
                :label="item.title"
                :name="item.path"
                :closable="item.path != '/admin/index'"
            >
            </el-tab-pane>
        </el-tabs>

        <!-- 右侧操作下拉 -->
        <el-dropdown @command="handleCloseTab" class="ml-2 flex-shrink-0">
            <div class="nav-action-btn flex items-center justify-center w-[30px] h-[30px] rounded-md cursor-pointer">
                <el-icon class="text-slate-400 text-[14px]"><arrow-down /></el-icon>
            </div>
            <template #dropdown>
                <el-dropdown-menu>
                    <el-dropdown-item command="closeOthers">
                        <el-icon class="mr-1"><Close /></el-icon>
                        关闭其他
                    </el-dropdown-item>
                    <el-dropdown-item command="closeAll">
                        <el-icon class="mr-1"><CircleClose /></el-icon>
                        关闭全部
                    </el-dropdown-item>
                </el-dropdown-menu>
            </template>
        </el-dropdown>
    </div>

    <!-- 占位高度 -->
    <div class="h-[44px]"></div>
</template>

<script setup>
import { useTabList } from '@/composables/useTagList.js'

const { menuStore, activeTab, tabList, tabChange, removeTab, handleCloseTab } = useTabList()
</script>

<style>
/* ===== 标签栏容器（参考 FeiTwnd 简约灰白） ===== */
.tag-nav {
    background: #ffffff;
    border-bottom: 1px solid #e4e7ed;
    box-shadow: 0 1px 0 rgba(0, 0, 0, 0.04);
    animation: slideDown 0.3s ease-out;
}

@keyframes slideDown {
    from {
        opacity: 0;
        transform: translateY(-100%);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

/* ===== Tabs 整体 ===== */
.nav-tabs.el-tabs {
    height: 32px;
}

.nav-tabs .el-tabs__header {
    margin-bottom: 0;
    border: none !important;
}

.nav-tabs .el-tabs--card > .el-tabs__header {
    border: none !important;
}

.nav-tabs .el-tabs--card > .el-tabs__header .el-tabs__nav {
    border: none !important;
}

/* ===== Tab 项 ===== */
.nav-tabs .el-tabs__item {
    height: 28px !important;
    line-height: 28px !important;
    font-size: 12px !important;
    padding: 0 12px !important;
    margin: 0 2px !important;
    border-radius: 6px !important;
    border: 1px solid rgba(226, 232, 240, 0.8) !important;
    background: #ffffff !important;
    color: #64748b !important;
    transition: all 0.18s ease !important;
    animation: tabFadeIn 0.25s ease-out;
    position: relative;
    overflow: hidden;
}

@keyframes tabFadeIn {
    from {
        opacity: 0;
        transform: scale(0.9) translateY(-2px);
    }
    to {
        opacity: 1;
        transform: scale(1) translateY(0);
    }
}

.nav-tabs .el-tabs__item::after {
    content: '';
    position: absolute;
    bottom: 0;
    left: 50%;
    width: 0;
    height: 2px;
    background: linear-gradient(90deg, #3b82f6, #60a5fa);
    transition: all 0.2s ease;
    transform: translateX(-50%);
    border-radius: 2px 2px 0 0;
}

.nav-tabs .el-tabs__item:hover {
    background: #f8fafc !important;
    color: #475569 !important;
    border-color: rgba(203, 213, 225, 0.8) !important;
    transform: translateY(-1px);
}

.nav-tabs .el-tabs__item:hover::after {
    width: 40%;
}

/* ===== 激活 Tab ===== */
.nav-tabs .el-tabs__item.is-active {
    background: #303133 !important;
    color: #fff !important;
    border-color: transparent !important;
    animation: tabFadeIn 0.25s ease-out;
}

.nav-tabs .el-tabs__item.is-active::before {
    display: none;
}

.nav-tabs .el-tabs__item.is-active::after {
    display: none;
}

.nav-tabs .el-tabs__item.is-active:hover {
    background: #000000 !important;
    transform: translateY(-1px);
}

/* ===== 关闭按钮 ===== */
.nav-tabs .el-tabs__item .is-icon-close {
    font-size: 10px !important;
    width: 14px !important;
    height: 14px !important;
    margin-left: 4px !important;
    border-radius: 50% !important;
    transition: all 0.15s ease !important;
    animation: closeBtnFadeIn 0.15s ease-out;
}

@keyframes closeBtnFadeIn {
    from {
        opacity: 0;
        transform: scale(0.5);
    }
    to {
        opacity: 1;
        transform: scale(1);
    }
}

.nav-tabs .el-tabs__item .is-icon-close:hover {
    background: rgba(255, 255, 255, 0.25) !important;
    color: #fff !important;
    transform: scale(1.15);
}

/* ===== Tab 导航箭头 ===== */
.nav-tabs .el-tabs__nav-prev,
.nav-tabs .el-tabs__nav-next {
    line-height: 32px;
    color: #94a3b8;
    transition: all 0.18s ease;
}

.nav-tabs .el-tabs__nav-prev:hover,
.nav-tabs .el-tabs__nav-next:hover {
    color: #3b82f6;
    transform: scale(1.1);
}

/* ===== 操作按钮 ===== */
.nav-action-btn {
    transition: all 0.18s ease;
}

.nav-action-btn:hover {
    background: #f1f5f9;
    transform: scale(1.05);
}

.nav-action-btn:hover .el-icon {
    color: #3b82f6;
}

/* ===== 禁用状态 ===== */
.is-disabled {
    cursor: not-allowed;
    color: #d1d5db;
}
</style>
